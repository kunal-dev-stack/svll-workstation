"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.createDeviceRoutes = createDeviceRoutes;
const crypto_1 = __importDefault(require("crypto"));
const express_1 = require("express");
const store_1 = require("../store");
const auth_1 = require("../auth");
const audit_1 = require("../audit");
function createDeviceRoutes(gateway) {
    const router = (0, express_1.Router)();
    // All endpoints require admin authentication
    router.use(auth_1.requireAuth);
    // --- Device Inventory ---
    router.get('/devices', (req, res) => {
        const devices = store_1.store.getDevices();
        // Update live connection status based on active socket
        const enriched = devices.map((d) => ({
            ...d,
            isLiveSocket: gateway.isDeviceConnected(d.id),
        }));
        res.json(enriched);
    });
    router.get('/devices/:id', (req, res) => {
        const deviceId = String(req.params.id);
        const device = store_1.store.getDevice(deviceId);
        if (!device) {
            res.status(404).json({ error: 'Device not found' });
            return;
        }
        res.json({
            ...device,
            isLiveSocket: gateway.isDeviceConnected(device.id),
        });
    });
    // Emergency Revocation / De-enrollment
    router.delete('/devices/:id', (0, auth_1.requireRole)(['superadmin', 'admin']), (req, res) => {
        const deviceId = String(req.params.id);
        const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
        const device = store_1.store.getDevice(deviceId);
        if (!device) {
            res.status(404).json({ error: 'Device not found' });
            return;
        }
        // Send de-enrollment notice to agent if connected
        gateway.sendToAgent(deviceId, {
            type: 'SESSION_DISCONNECT',
            payload: { action: 'REVOKE_ENROLLMENT', reason: 'Administrator de-enrolled device' },
            timestamp: Date.now(),
        });
        store_1.store.removeDevice(deviceId);
        (0, audit_1.logAudit)(req.user.username, 'DEVICE_REVOKE', deviceId, `Device ${device.hostname} revoked and removed`, clientIp);
        res.json({ success: true, message: `Device ${deviceId} successfully revoked` });
    });
    // --- Enrollment Tokens ---
    router.get('/enrollment/tokens', (req, res) => {
        const tokens = store_1.store.getAllTokens();
        res.json(tokens);
    });
    router.post('/enrollment/tokens', (0, auth_1.requireRole)(['superadmin', 'admin']), (req, res) => {
        const { group, maxUses, hoursValid, unattendedAllowed } = req.body;
        const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
        const token = {
            token: `MESH-${crypto_1.default.randomUUID().substring(0, 8).toUpperCase()}-${Date.now().toString(36).toUpperCase()}`,
            createdBy: req.user.username,
            createdAt: Date.now(),
            expiresAt: Date.now() + (hoursValid || 48) * 3600 * 1000,
            maxUses: maxUses || 10,
            usedCount: 0,
            group: group || 'Corporate Endpoints',
            unattendedAllowed: unattendedAllowed ?? true,
        };
        store_1.store.createToken(token);
        (0, audit_1.logAudit)(req.user.username, 'TOKEN_CREATED', undefined, `Created token ${token.token} for group ${token.group}`, clientIp);
        res.status(201).json(token);
    });
    // --- Command Execution Dispatch ---
    router.post('/devices/:id/command', (0, auth_1.requireRole)(['superadmin', 'admin']), (req, res) => {
        const deviceId = String(req.params.id);
        const { command, shell } = req.body;
        const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
        if (!command) {
            res.status(400).json({ error: 'Command string is required' });
            return;
        }
        if (!gateway.isDeviceConnected(deviceId)) {
            res.status(503).json({ error: 'Device is currently offline or unreachable' });
            return;
        }
        const commandId = `cmd-${Date.now()}-${crypto_1.default.randomUUID().substring(0, 6)}`;
        gateway.sendToAgent(deviceId, {
            type: 'COMMAND_EXEC_REQ',
            payload: { commandId, command, shell: shell || 'powershell' },
            timestamp: Date.now(),
        });
        (0, audit_1.logAudit)(req.user.username, 'COMMAND_DISPATCH', deviceId, `Command dispatched: ${command}`, clientIp);
        res.json({ success: true, commandId });
    });
    // --- Process Management ---
    router.get('/devices/:id/processes', (req, res) => {
        const deviceId = String(req.params.id);
        if (!gateway.isDeviceConnected(deviceId)) {
            res.status(503).json({ error: 'Device is currently offline' });
            return;
        }
        gateway.sendToAgent(deviceId, {
            type: 'PROCESS_LIST_REQ',
            payload: {},
            timestamp: Date.now(),
        });
        res.json({ message: 'Process list request dispatched to agent' });
    });
    router.post('/devices/:id/processes/:pid/kill', (0, auth_1.requireRole)(['superadmin', 'admin']), (req, res) => {
        const deviceId = String(req.params.id);
        const pid = parseInt(String(req.params.pid), 10);
        const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
        if (isNaN(pid)) {
            res.status(400).json({ error: 'Invalid PID' });
            return;
        }
        gateway.sendToAgent(deviceId, {
            type: 'PROCESS_KILL_REQ',
            payload: { pid },
            timestamp: Date.now(),
        });
        (0, audit_1.logAudit)(req.user.username, 'PROCESS_KILL_DISPATCH', deviceId, `Kill PID ${pid}`, clientIp);
        res.json({ success: true, message: `Kill request for PID ${pid} dispatched` });
    });
    // --- Service Management ---
    router.get('/devices/:id/services', (req, res) => {
        const deviceId = String(req.params.id);
        if (!gateway.isDeviceConnected(deviceId)) {
            res.status(503).json({ error: 'Device is offline' });
            return;
        }
        gateway.sendToAgent(deviceId, {
            type: 'SERVICE_LIST_REQ',
            payload: {},
            timestamp: Date.now(),
        });
        res.json({ message: 'Service list request dispatched to agent' });
    });
    router.post('/devices/:id/services/:name/:action', (0, auth_1.requireRole)(['superadmin', 'admin']), (req, res) => {
        const deviceId = String(req.params.id);
        const serviceName = String(req.params.name);
        const action = String(req.params.action);
        const clientIp = req.headers['x-forwarded-for'] || req.socket.remoteAddress || '127.0.0.1';
        if (!['start', 'stop', 'restart'].includes(action)) {
            res.status(400).json({ error: 'Invalid action. Must be start, stop, or restart' });
            return;
        }
        gateway.sendToAgent(deviceId, {
            type: 'SERVICE_CONTROL_REQ',
            payload: { serviceName, action },
            timestamp: Date.now(),
        });
        (0, audit_1.logAudit)(req.user.username, 'SERVICE_CONTROL_DISPATCH', deviceId, `Service ${serviceName} -> ${action}`, clientIp);
        res.json({ success: true, message: `Service control request sent` });
    });
    // --- File Management ---
    router.get('/devices/:id/files', (req, res) => {
        const deviceId = String(req.params.id);
        const targetPath = req.query.path || 'C:\\';
        if (!gateway.isDeviceConnected(deviceId)) {
            res.status(503).json({ error: 'Device is offline' });
            return;
        }
        gateway.sendToAgent(deviceId, {
            type: 'FILE_LIST_REQ',
            payload: { path: targetPath },
            timestamp: Date.now(),
        });
        res.json({ message: `File listing request for ${targetPath} dispatched` });
    });
    return router;
}
