import { Server as HttpServer } from 'http';
import { WebSocketServer, WebSocket } from 'ws';
import url from 'url';
import { store } from './store';
import { logAudit } from './audit';
import { verifyJwtToken } from './auth';
import { DeviceInfo, WSMessage } from './types';

interface AgentSocket {
  ws: WebSocket;
  deviceId: string;
  isAlive: boolean;
}

interface AdminSessionSocket {
  ws: WebSocket;
  adminUser: string;
  deviceId: string;
  sessionId: string;
  sessionType: 'desktop' | 'terminal' | 'files' | 'manager';
}

export class MeshGateway {
  private wss: WebSocketServer;
  private agentSockets = new Map<string, AgentSocket>(); // deviceId -> AgentSocket
  private adminSessions = new Map<string, Set<AdminSessionSocket>>(); // deviceId -> Set of admin sockets

  constructor(server: HttpServer) {
    this.wss = new WebSocketServer({ server, path: '/ws' });
    this.setupListeners();
    this.startHeartbeatCheck();
  }

  private setupListeners() {
    this.wss.on('connection', (ws: WebSocket, req) => {
      const parsedUrl = url.parse(req.url || '', true);
      const pathname = parsedUrl.pathname;
      const query = parsedUrl.query;

      const role = query.role as string; // 'agent' | 'admin'
      const deviceId = query.deviceId as string;
      const token = query.token as string;
      const clientIp = (req.headers['x-forwarded-for'] as string) || req.socket.remoteAddress || '127.0.0.1';

      if (role === 'agent') {
        this.handleAgentConnection(ws, query, clientIp);
      } else if (role === 'admin') {
        this.handleAdminConnection(ws, query, clientIp);
      } else {
        ws.close(4001, 'Invalid connection role specified');
      }
    });
  }

  // --- AGENT HANDSHAKE & MESSAGES ---
  private handleAgentConnection(ws: WebSocket, query: Record<string, any>, clientIp: string) {
    const enrollmentToken = query.token as string;
    const deviceId = query.deviceId as string;

    if (!deviceId) {
      ws.close(4002, 'Device ID required');
      return;
    }

    // Verify enrollment or existing registration
    let device = store.getDevice(deviceId);
    if (!device) {
      if (!enrollmentToken || !store.useToken(enrollmentToken)) {
        console.warn(`[GATEWAY] Enrollment rejected for unregistered device ${deviceId} with invalid token`);
        ws.close(4003, 'Invalid or expired enrollment token');
        return;
      }

      // Initial registration placeholder; will be populated on first DEVICE_REGISTER message
      console.log(`[GATEWAY] New device enrollment initiated for ${deviceId} using token: ${enrollmentToken}`);
    }

    const agentSocket: AgentSocket = { ws, deviceId, isAlive: true };
    this.agentSockets.set(deviceId, agentSocket);

    // Update status to online
    if (device) {
      device.status = 'online';
      device.lastSeen = Date.now();
      device.ipAddress = clientIp;
      store.upsertDevice(device);
      this.broadcastDeviceUpdate(device);
    }

    console.log(`[GATEWAY] Agent connected: ${deviceId} (${clientIp})`);

    ws.on('pong', () => {
      agentSocket.isAlive = true;
    });

    ws.on('message', (data: Buffer | string, isBinary: boolean) => {
      try {
        if (isBinary) {
          // Binary frame (e.g. raw JPEG/WebP screen capture from agent)
          this.routeBinaryFrameToAdmins(deviceId, data as Buffer);
          return;
        }

        const msg: WSMessage = JSON.parse(data.toString());
        this.handleAgentMessage(deviceId, msg, clientIp);
      } catch (err) {
        console.error(`[GATEWAY] Error processing message from agent ${deviceId}:`, err);
      }
    });

    ws.on('close', () => {
      console.log(`[GATEWAY] Agent disconnected: ${deviceId}`);
      this.agentSockets.delete(deviceId);
      const dev = store.getDevice(deviceId);
      if (dev) {
        dev.status = 'offline';
        dev.lastSeen = Date.now();
        store.upsertDevice(dev);
        this.broadcastDeviceUpdate(dev);
      }

      // Notify any connected admins
      this.notifyAdminsOfDeviceStatus(deviceId, 'offline');
    });
  }

  private handleAgentMessage(deviceId: string, msg: WSMessage, clientIp: string) {
    switch (msg.type) {
      case 'DEVICE_REGISTER': {
        const payload = msg.payload as Partial<DeviceInfo>;
        const existing = store.getDevice(deviceId);
        const updated: DeviceInfo = {
          id: deviceId,
          hostname: payload.hostname || existing?.hostname || 'Unknown-Host',
          os: payload.os || existing?.os || 'windows',
          osVersion: payload.osVersion || existing?.osVersion || 'Unknown',
          arch: payload.arch || existing?.arch || 'x64',
          agentVersion: payload.agentVersion || existing?.agentVersion || '1.0.0',
          ipAddress: clientIp,
          status: 'online',
          lastSeen: Date.now(),
          enrolledAt: existing?.enrolledAt || Date.now(),
          group: existing?.group || 'Default Endpoints',
          tags: existing?.tags || ['auto-enrolled'],
          unattendedAllowed: existing?.unattendedAllowed ?? true,
          capabilities: payload.capabilities || {
            remoteDesktop: true,
            remoteTerminal: true,
            fileTransfer: true,
            processManager: true,
            serviceManager: true,
            diagnostics: true,
          },
          latestTelemetry: payload.latestTelemetry || existing?.latestTelemetry,
        };

        store.upsertDevice(updated);
        this.broadcastDeviceUpdate(updated);
        logAudit('AGENT', 'DEVICE_ENROLLED', deviceId, `Device registered: ${updated.hostname} (${updated.os})`, clientIp);
        break;
      }

      case 'DEVICE_HEARTBEAT': {
        const dev = store.getDevice(deviceId);
        if (dev) {
          dev.lastSeen = Date.now();
          dev.status = 'online';
          if (msg.payload?.telemetry) {
            dev.latestTelemetry = msg.payload.telemetry;
          }
          store.upsertDevice(dev);
          this.broadcastDeviceUpdate(dev);
        }
        break;
      }

      case 'FRAME_DESKTOP':
      case 'TERMINAL_OUTPUT':
      case 'FILE_LIST_RESP':
      case 'FILE_CHUNK':
      case 'COMMAND_EXEC_RESP':
      case 'PROCESS_LIST_RESP':
      case 'SERVICE_LIST_RESP':
      case 'SESSION_APPROVAL_RESP': {
        // Forward message to all admins viewing this device
        this.relayToAdmins(deviceId, msg);
        break;
      }

      default:
        console.log(`[GATEWAY] Unhandled message type from agent ${deviceId}:`, msg.type);
    }
  }

  // --- ADMIN CONNECTIONS & MESSAGES ---
  private handleAdminConnection(ws: WebSocket, query: Record<string, any>, clientIp: string) {
    const token = query.token as string;
    const deviceId = query.deviceId as string;
    const sessionType = (query.sessionType as any) || 'desktop';

    const user = verifyJwtToken(token);
    if (!user) {
      ws.close(4004, 'Admin authorization required');
      return;
    }

    const sessionId = `sess-${Date.now()}-${Math.random().toString(36).substring(2, 8)}`;
    const adminSession: AdminSessionSocket = {
      ws,
      adminUser: user.username,
      deviceId,
      sessionId,
      sessionType,
    };

    if (!this.adminSessions.has(deviceId)) {
      this.adminSessions.set(deviceId, new Set());
    }
    this.adminSessions.get(deviceId)!.add(adminSession);

    logAudit(user.username, `SESSION_START_${sessionType.toUpperCase()}`, deviceId, `Session started ID: ${sessionId}`, clientIp);

    // If device is online, signal agent that an admin session is active
    const agent = this.agentSockets.get(deviceId);
    if (agent && agent.ws.readyState === WebSocket.OPEN) {
      const startMsg: WSMessage = {
        type: sessionType === 'desktop' ? 'FRAME_DESKTOP' : 'TERMINAL_START',
        sessionId,
        payload: { action: 'start', adminUser: user.username },
        timestamp: Date.now(),
      };
      agent.ws.send(JSON.stringify(startMsg));
    }

    ws.on('message', (data: Buffer | string) => {
      try {
        const msg: WSMessage = JSON.parse(data.toString());
        this.handleAdminMessage(deviceId, adminSession, msg, clientIp);
      } catch (err) {
        console.error(`[GATEWAY] Error processing admin message:`, err);
      }
    });

    ws.on('close', () => {
      const set = this.adminSessions.get(deviceId);
      if (set) {
        set.delete(adminSession);
        if (set.size === 0) {
          this.adminSessions.delete(deviceId);
          // Tell agent to stop desktop capture if no admins remain
          const agentSocket = this.agentSockets.get(deviceId);
          if (agentSocket && agentSocket.ws.readyState === WebSocket.OPEN) {
            agentSocket.ws.send(
              JSON.stringify({
                type: 'SESSION_DISCONNECT',
                payload: { reason: 'No active administrators' },
                timestamp: Date.now(),
              })
            );
          }
        }
      }

      logAudit(user.username, `SESSION_END_${sessionType.toUpperCase()}`, deviceId, `Session ended ID: ${sessionId}`, clientIp);
    });
  }

  private handleAdminMessage(deviceId: string, admin: AdminSessionSocket, msg: WSMessage, clientIp: string) {
    const agent = this.agentSockets.get(deviceId);
    if (!agent || agent.ws.readyState !== WebSocket.OPEN) {
      admin.ws.send(
        JSON.stringify({
          type: 'SESSION_DISCONNECT',
          payload: { error: 'Target device is offline or unavailable' },
          timestamp: Date.now(),
        })
      );
      return;
    }

    // Attach sender context
    msg.sessionId = admin.sessionId;

    // Log high-risk actions
    if (msg.type === 'COMMAND_EXEC_REQ') {
      if (msg.payload && !msg.payload.commandId) {
        msg.payload.commandId = `cmd-${Date.now()}`;
      }
      logAudit(admin.adminUser, 'COMMAND_EXEC', deviceId, `Executed: ${msg.payload?.command}`, clientIp);
    } else if (msg.type === 'PROCESS_KILL_REQ') {
      logAudit(admin.adminUser, 'PROCESS_KILL', deviceId, `Killed PID: ${msg.payload?.pid}`, clientIp);
    } else if (msg.type === 'SERVICE_CONTROL_REQ') {
      logAudit(admin.adminUser, 'SERVICE_CONTROL', deviceId, `Action: ${msg.payload?.action} on service: ${msg.payload?.serviceName}`, clientIp);
    }

    // Forward directly to agent
    agent.ws.send(JSON.stringify(msg));
  }

  // --- RELAY UTILITIES ---
  public relayToAdmins(deviceId: string, msg: WSMessage) {
    const admins = this.adminSessions.get(deviceId);
    if (!admins || admins.size === 0) return;

    const data = JSON.stringify(msg);
    for (const session of admins) {
      if (session.ws.readyState === WebSocket.OPEN) {
        session.ws.send(data);
      }
    }
  }

  public routeBinaryFrameToAdmins(deviceId: string, frameBuffer: Buffer) {
    const admins = this.adminSessions.get(deviceId);
    if (!admins || admins.size === 0) return;

    for (const session of admins) {
      if (session.ws.readyState === WebSocket.OPEN) {
        session.ws.send(frameBuffer);
      }
    }
  }

  private broadcastDeviceUpdate(device: DeviceInfo) {
    // Notify all admin sessions
    const updateMsg = JSON.stringify({
      type: 'DEVICE_UPDATE',
      payload: device,
      timestamp: Date.now(),
    });

    for (const sessions of this.adminSessions.values()) {
      for (const s of sessions) {
        if (s.ws.readyState === WebSocket.OPEN) {
          s.ws.send(updateMsg);
        }
      }
    }
  }

  private notifyAdminsOfDeviceStatus(deviceId: string, status: string) {
    const admins = this.adminSessions.get(deviceId);
    if (!admins) return;
    const msg = JSON.stringify({
      type: 'DEVICE_STATUS',
      deviceId,
      payload: { status },
      timestamp: Date.now(),
    });

    for (const s of admins) {
      if (s.ws.readyState === WebSocket.OPEN) {
        s.ws.send(msg);
      }
    }
  }

  public sendToAgent(deviceId: string, msg: WSMessage): boolean {
    const agent = this.agentSockets.get(deviceId);
    if (agent && agent.ws.readyState === WebSocket.OPEN) {
      agent.ws.send(JSON.stringify(msg));
      return true;
    }
    return false;
  }

  public isDeviceConnected(deviceId: string): boolean {
    const agent = this.agentSockets.get(deviceId);
    return !!agent && agent.ws.readyState === WebSocket.OPEN;
  }

  private startHeartbeatCheck() {
    setInterval(() => {
      const now = Date.now();
      for (const [deviceId, agent] of this.agentSockets.entries()) {
        if (!agent.isAlive) {
          console.log(`[GATEWAY] Heartbeat timeout for agent ${deviceId}`);
          agent.ws.terminate();
          this.agentSockets.delete(deviceId);
          const dev = store.getDevice(deviceId);
          if (dev) {
            dev.status = 'offline';
            store.upsertDevice(dev);
            this.broadcastDeviceUpdate(dev);
          }
          continue;
        }

        agent.isAlive = false;
        agent.ws.ping();
      }
    }, 15000);
  }
}
