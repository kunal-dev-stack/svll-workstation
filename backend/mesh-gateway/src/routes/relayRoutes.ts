import { Router, Request, Response } from 'express';
import { store } from '../store';
import { DeviceInfo } from '../types';

export function createRelayRoutes(): Router {
  const router = Router();

  // In-memory / persisted store for legacy SVLL tickets
  const tickets: Record<string, any> = {};

  // GET /api/health
  router.get('/health', (req: Request, res: Response) => {
    res.json({
      status: 'ONLINE',
      server: 'SVLL AegisMesh Relay Gateway v8.4',
      timestamp: new Date().toISOString(),
      devicesCount: store.getDevices().length,
    });
  });

  // POST /api/heartbeat (Called by SVLL-IT-Workstation-User and install.ps1)
  router.post('/heartbeat', (req: Request, res: Response) => {
    try {
      const b = req.body || {};
      const hostname = b.Hostname || b.hostname || 'UNKNOWN-HOST';
      const id = b.Id || b.id || `win-${hostname.toLowerCase()}`;
      const clientIp = (req.headers['x-forwarded-for'] as string) || req.socket.remoteAddress || b.IpAddress || '127.0.0.1';

      // Map to AegisMesh DeviceInfo
      const existing = store.getDevice(id) || store.getDevice(`win-${hostname.toLowerCase()}`);
      const dev: DeviceInfo = {
        id: existing?.id || id,
        hostname: hostname,
        os: 'windows',
        osVersion: b.OsVersion || b.osVersion || 'Windows 10/11',
        arch: 'x64',
        agentVersion: '8.4.0',
        ipAddress: (b.IpAddress && b.IpAddress !== '127.0.0.1') ? b.IpAddress : clientIp.replace(/^::ffff:/, ''),
        status: 'online',
        lastSeen: Date.now(),
        enrolledAt: existing?.enrolledAt || Date.now(),
        group: b.BranchLocation || 'Corporate Endpoints',
        tags: ['svll-workstation', b.BranchLocation || 'HQ'],
        unattendedAllowed: true,
        capabilities: {
          remoteDesktop: true,
          remoteTerminal: true,
          fileTransfer: true,
          processManager: true,
          serviceManager: true,
          diagnostics: true,
        },
        latestTelemetry: {
          cpuUsagePercent: Number(b.CpuLoadPercent || 5),
          memoryUsedMb: Math.round(Number(b.RamUsagePercent || 40) * 160),
          memoryTotalMb: 16384,
          diskUsedGb: Math.round((100 - Number(b.DiskFreePercent || 50)) * 4.65),
          diskTotalGb: 465,
          ipAddress: (b.IpAddress && b.IpAddress !== '127.0.0.1') ? b.IpAddress : clientIp.replace(/^::ffff:/, ''),
          uptimeSeconds: 3600,
          timestamp: Date.now(),
        },
      };

      store.upsertDevice(dev);

      res.json({
        registered: true,
        serverTime: new Date().toISOString(),
        command: null,
      });
    } catch (err: any) {
      res.status(500).json({ error: err.message });
    }
  });

  // GET /api/fleet (Polled by SVLL-IT-Workstation-Admin)
  router.get('/fleet', (req: Request, res: Response) => {
    try {
      const devices = store.getDevices();
      const legacyFleet = devices.map((d) => {
        const isRecent = (Date.now() - d.lastSeen) < 180000; // 3 minutes
        const status = isRecent ? 'Online' : 'Offline';

        return {
          Id: d.id,
          Hostname: d.hostname,
          IpAddress: d.latestTelemetry?.ipAddress || d.ipAddress || '127.0.0.1',
          MacAddress: '00-00-00-00-00-00',
          BranchLocation: d.group || 'HQ',
          LoggedInUser: 'SVLL-User',
          AnyDeskId: '',
          AnyDeskInstalled: false,
          RdpEnabled: true,
          QuickAssistAvailable: true,
          Status: status,
          SupportRequestMessage: '',
          SupportRequestedAt: null,
          CpuLoadPercent: d.latestTelemetry?.cpuUsagePercent ?? 5.0,
          RamUsagePercent: d.latestTelemetry ? Math.round((d.latestTelemetry.memoryUsedMb / (d.latestTelemetry.memoryTotalMb || 1)) * 100) : 40.0,
          DiskFreePercent: d.latestTelemetry ? Math.round(((d.latestTelemetry.diskTotalGb - d.latestTelemetry.diskUsedGb) / (d.latestTelemetry.diskTotalGb || 1)) * 100) : 50.0,
          OsVersion: d.osVersion || 'Windows',
          OsBuild: '22631',
          LastSeen: new Date(d.lastSeen).toISOString(),
          IsLocalHost: false,
          SerialNumber: d.id,
          MeshNodeId: d.id,
          MeshAgentRunning: isRecent,
          RemoteId: d.id,
          RemotePassword: 'SVLL@Remote2026',
          RemoteEngine: 'AegisMesh',
        };
      });

      res.json(legacyFleet);
    } catch (err: any) {
      res.status(500).json({ error: err.message });
    }
  });

  // POST /api/ticket (Submit IT ticket)
  router.post('/ticket', (req: Request, res: Response) => {
    try {
      const ticket = req.body || {};
      const cloudTicketId = ticket.TicketId || `SVLL-TKT-${Date.now().toString(36).toUpperCase()}`;
      ticket.CloudTicketId = cloudTicketId;
      ticket.CreatedAt = new Date().toISOString();
      ticket.Status = ticket.Status || 'Open';
      tickets[cloudTicketId] = ticket;

      res.json({ cloudTicketId });
    } catch (err: any) {
      res.status(500).json({ error: err.message });
    }
  });

  // GET /api/tickets (Admin lists tickets)
  router.get('/tickets', (req: Request, res: Response) => {
    res.json(Object.values(tickets));
  });

  // GET /api/ticket/:id/reply
  router.get('/ticket/:id/reply', (req: Request, res: Response) => {
    const id = Array.isArray(req.params.id) ? req.params.id[0] : req.params.id;
    const ticket = tickets[id];
    if (!ticket) {
      res.status(404).json({ error: 'Ticket not found' });
      return;
    }
    if (!ticket.AdminReply) {
      res.status(204).send();
      return;
    }
    res.json(ticket);
  });

  // POST /api/ticket/:id/reply
  router.post('/ticket/:id/reply', (req: Request, res: Response) => {
    const id = Array.isArray(req.params.id) ? req.params.id[0] : req.params.id;
    const { reply } = req.body;
    const ticket = tickets[id];
    if (!ticket) {
      res.status(404).json({ error: 'Ticket not found' });
      return;
    }
    ticket.AdminReply = reply;
    ticket.AdminReplyAt = new Date().toISOString();
    ticket.Status = 'Answered';
    res.json({ success: true, ticket });
  });

  return router;
}
