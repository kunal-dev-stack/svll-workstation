"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.MeshGateway = void 0;
const ws_1 = require("ws");
const url_1 = __importDefault(require("url"));
const store_1 = require("./store");
const audit_1 = require("./audit");
const auth_1 = require("./auth");
class MeshGateway {
    wss;
    agentSockets = new Map(); // deviceId -> AgentSocket
    adminSessions = new Map(); // deviceId -> Set of admin sockets
    constructor(server) {
        this.wss = new ws_1.WebSocketServer({ server, path: '/ws' });
        this.setupListeners();
        this.startHeartbeatCheck();
    }
    setupListeners() {
        this.wss.on('connection', (ws, req) => {
            const parsedUrl = url_1.default.parse(req.url || '', true);
            const pathname = parsedUrl.pathname;
            const query = parsedUrl.query;
            const role = query.role; // 'agent' | 'admin'
            const deviceId = query.deviceId;
            const token = query.token;
            const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
            if (role === 'agent') {
                this.handleAgentConnection(ws, query, clientIp);
            }
            else if (role === 'admin') {
                this.handleAdminConnection(ws, query, clientIp);
            }
            else {
                ws.close(4001, 'Invalid connection role specified');
            }
        });
    }
    // --- AGENT HANDSHAKE & MESSAGES ---
    handleAgentConnection(ws, query, clientIp) {
        const enrollmentToken = query.token;
        const deviceId = query.deviceId;
        if (!deviceId) {
            ws.close(4002, 'Device ID required');
            return;
        }
        // Verify enrollment or existing registration
        let device = store_1.store.getDevice(deviceId);
        if (!device) {
            if (!enrollmentToken || !store_1.store.useToken(enrollmentToken)) {
                console.warn(`[GATEWAY] Enrollment rejected for unregistered device ${deviceId} with invalid token`);
                ws.close(4003, 'Invalid or expired enrollment token');
                return;
            }
            // Initial registration placeholder; will be populated on first DEVICE_REGISTER message
            console.log(`[GATEWAY] New device enrollment initiated for ${deviceId} using token: ${enrollmentToken}`);
        }
        const agentSocket = { ws, deviceId, isAlive: true };
        this.agentSockets.set(deviceId, agentSocket);
        // Update status to online
        if (device) {
            device.status = 'online';
            device.lastSeen = Date.now();
            device.ipAddress = clientIp;
            store_1.store.upsertDevice(device);
            this.broadcastDeviceUpdate(device);
        }
        console.log(`[GATEWAY] Agent connected: ${deviceId} (${clientIp})`);
        ws.on('pong', () => {
            agentSocket.isAlive = true;
        });
        ws.on('message', (data, isBinary) => {
            try {
                if (isBinary) {
                    // Binary frame (e.g. raw JPEG/WebP screen capture from agent)
                    this.routeBinaryFrameToAdmins(deviceId, data);
                    return;
                }
                const msg = JSON.parse(data.toString());
                this.handleAgentMessage(deviceId, msg, clientIp);
            }
            catch (err) {
                console.error(`[GATEWAY] Error processing message from agent ${deviceId}:`, err);
            }
        });
        ws.on('close', () => {
            console.log(`[GATEWAY] Agent disconnected: ${deviceId}`);
            this.agentSockets.delete(deviceId);
            const dev = store_1.store.getDevice(deviceId);
            if (dev) {
                dev.status = 'offline';
                dev.lastSeen = Date.now();
                store_1.store.upsertDevice(dev);
                this.broadcastDeviceUpdate(dev);
            }
            // Notify any connected admins
            this.notifyAdminsOfDeviceStatus(deviceId, 'offline');
        });
    }
    handleAgentMessage(deviceId, msg, clientIp) {
        switch (msg.type) {
            case 'DEVICE_REGISTER': {
                const payload = msg.payload;
                const existing = store_1.store.getDevice(deviceId);
                const updated = {
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
                store_1.store.upsertDevice(updated);
                this.broadcastDeviceUpdate(updated);
                (0, audit_1.logAudit)('AGENT', 'DEVICE_ENROLLED', deviceId, `Device registered: ${updated.hostname} (${updated.os})`, clientIp);
                break;
            }
            case 'DEVICE_HEARTBEAT': {
                const dev = store_1.store.getDevice(deviceId);
                if (dev) {
                    dev.lastSeen = Date.now();
                    dev.status = 'online';
                    if (msg.payload?.telemetry) {
                        dev.latestTelemetry = msg.payload.telemetry;
                    }
                    store_1.store.upsertDevice(dev);
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
    handleAdminConnection(ws, query, clientIp) {
        const token = query.token;
        const deviceId = query.deviceId;
        const sessionType = query.sessionType || 'desktop';
        const user = (0, auth_1.verifyJwtToken)(token);
        if (!user) {
            ws.close(4004, 'Admin authorization required');
            return;
        }
        const sessionId = `sess-${Date.now()}-${Math.random().toString(36).substring(2, 8)}`;
        const adminSession = {
            ws,
            adminUser: user.username,
            deviceId,
            sessionId,
            sessionType,
        };
        if (!this.adminSessions.has(deviceId)) {
            this.adminSessions.set(deviceId, new Set());
        }
        this.adminSessions.get(deviceId).add(adminSession);
        (0, audit_1.logAudit)(user.username, `SESSION_START_${sessionType.toUpperCase()}`, deviceId, `Session started ID: ${sessionId}`, clientIp);
        // If device is online, signal agent that an admin session is active
        const agent = this.agentSockets.get(deviceId);
        if (agent && agent.ws.readyState === ws_1.WebSocket.OPEN) {
            const startMsg = {
                type: sessionType === 'desktop' ? 'FRAME_DESKTOP' : 'TERMINAL_START',
                sessionId,
                payload: { action: 'start', adminUser: user.username },
                timestamp: Date.now(),
            };
            agent.ws.send(JSON.stringify(startMsg));
        }
        ws.on('message', (data) => {
            try {
                const msg = JSON.parse(data.toString());
                this.handleAdminMessage(deviceId, adminSession, msg, clientIp);
            }
            catch (err) {
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
                    if (agentSocket && agentSocket.ws.readyState === ws_1.WebSocket.OPEN) {
                        agentSocket.ws.send(JSON.stringify({
                            type: 'SESSION_DISCONNECT',
                            payload: { reason: 'No active administrators' },
                            timestamp: Date.now(),
                        }));
                    }
                }
            }
            (0, audit_1.logAudit)(user.username, `SESSION_END_${sessionType.toUpperCase()}`, deviceId, `Session ended ID: ${sessionId}`, clientIp);
        });
    }
    handleAdminMessage(deviceId, admin, msg, clientIp) {
        const agent = this.agentSockets.get(deviceId);
        if (!agent || agent.ws.readyState !== ws_1.WebSocket.OPEN) {
            admin.ws.send(JSON.stringify({
                type: 'SESSION_DISCONNECT',
                payload: { error: 'Target device is offline or unavailable' },
                timestamp: Date.now(),
            }));
            return;
        }
        // Attach sender context
        msg.sessionId = admin.sessionId;
        // Log high-risk actions
        if (msg.type === 'COMMAND_EXEC_REQ') {
            if (msg.payload && !msg.payload.commandId) {
                msg.payload.commandId = `cmd-${Date.now()}`;
            }
            (0, audit_1.logAudit)(admin.adminUser, 'COMMAND_EXEC', deviceId, `Executed: ${msg.payload?.command}`, clientIp);
        }
        else if (msg.type === 'PROCESS_KILL_REQ') {
            (0, audit_1.logAudit)(admin.adminUser, 'PROCESS_KILL', deviceId, `Killed PID: ${msg.payload?.pid}`, clientIp);
        }
        else if (msg.type === 'SERVICE_CONTROL_REQ') {
            (0, audit_1.logAudit)(admin.adminUser, 'SERVICE_CONTROL', deviceId, `Action: ${msg.payload?.action} on service: ${msg.payload?.serviceName}`, clientIp);
        }
        // Forward directly to agent
        agent.ws.send(JSON.stringify(msg));
    }
    // --- RELAY UTILITIES ---
    relayToAdmins(deviceId, msg) {
        const admins = this.adminSessions.get(deviceId);
        if (!admins || admins.size === 0)
            return;
        const data = JSON.stringify(msg);
        for (const session of admins) {
            if (session.ws.readyState === ws_1.WebSocket.OPEN) {
                session.ws.send(data);
            }
        }
    }
    routeBinaryFrameToAdmins(deviceId, frameBuffer) {
        const admins = this.adminSessions.get(deviceId);
        if (!admins || admins.size === 0)
            return;
        for (const session of admins) {
            if (session.ws.readyState === ws_1.WebSocket.OPEN) {
                session.ws.send(frameBuffer);
            }
        }
    }
    broadcastDeviceUpdate(device) {
        // Notify all admin sessions
        const updateMsg = JSON.stringify({
            type: 'DEVICE_UPDATE',
            payload: device,
            timestamp: Date.now(),
        });
        for (const sessions of this.adminSessions.values()) {
            for (const s of sessions) {
                if (s.ws.readyState === ws_1.WebSocket.OPEN) {
                    s.ws.send(updateMsg);
                }
            }
        }
    }
    notifyAdminsOfDeviceStatus(deviceId, status) {
        const admins = this.adminSessions.get(deviceId);
        if (!admins)
            return;
        const msg = JSON.stringify({
            type: 'DEVICE_STATUS',
            deviceId,
            payload: { status },
            timestamp: Date.now(),
        });
        for (const s of admins) {
            if (s.ws.readyState === ws_1.WebSocket.OPEN) {
                s.ws.send(msg);
            }
        }
    }
    sendToAgent(deviceId, msg) {
        const agent = this.agentSockets.get(deviceId);
        if (agent && agent.ws.readyState === ws_1.WebSocket.OPEN) {
            agent.ws.send(JSON.stringify(msg));
            return true;
        }
        return false;
    }
    isDeviceConnected(deviceId) {
        const agent = this.agentSockets.get(deviceId);
        return !!agent && agent.ws.readyState === ws_1.WebSocket.OPEN;
    }
    startHeartbeatCheck() {
        setInterval(() => {
            const now = Date.now();
            for (const [deviceId, agent] of this.agentSockets.entries()) {
                if (!agent.isAlive) {
                    console.log(`[GATEWAY] Heartbeat timeout for agent ${deviceId}`);
                    agent.ws.terminate();
                    this.agentSockets.delete(deviceId);
                    const dev = store_1.store.getDevice(deviceId);
                    if (dev) {
                        dev.status = 'offline';
                        store_1.store.upsertDevice(dev);
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
exports.MeshGateway = MeshGateway;
