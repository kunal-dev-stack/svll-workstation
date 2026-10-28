"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.gateway = exports.server = exports.app = void 0;
const http_1 = __importDefault(require("http"));
const express_1 = __importDefault(require("express"));
const cors_1 = __importDefault(require("cors"));
const dotenv_1 = __importDefault(require("dotenv"));
const path_1 = __importDefault(require("path"));
const fs_1 = __importDefault(require("fs"));
const authRoutes_1 = __importDefault(require("./routes/authRoutes"));
const deviceRoutes_1 = require("./routes/deviceRoutes");
const auditRoutes_1 = __importDefault(require("./routes/auditRoutes"));
const downloadRoutes_1 = __importDefault(require("./routes/downloadRoutes"));
const meshGateway_1 = require("./meshGateway");
dotenv_1.default.config();
const app = (0, express_1.default)();
exports.app = app;
const port = process.env.PORT || 5000;
app.use((0, cors_1.default)({ origin: '*' }));
app.use(express_1.default.json({ limit: '50mb' }));
app.use(express_1.default.urlencoded({ extended: true, limit: '50mb' }));
// Health Check
app.get('/health', (req, res) => {
    res.json({
        status: 'ONLINE',
        service: 'Enterprise Mesh Gateway & Device Management Server',
        version: '1.0.0',
        timestamp: Date.now(),
    });
});
// Create HTTP Server
const server = http_1.default.createServer(app);
exports.server = server;
// Attach Mesh Gateway WebSocket Engine
const gateway = new meshGateway_1.MeshGateway(server);
exports.gateway = gateway;
// Mount REST Routes
app.use('/api/auth', authRoutes_1.default);
app.use('/api/agent', downloadRoutes_1.default);
app.use('/api', (0, deviceRoutes_1.createDeviceRoutes)(gateway));
app.use('/api/audit-logs', auditRoutes_1.default);
// Serve static client-web production build if available
const CLIENT_DIST = path_1.default.join(__dirname, '..', '..', 'client-web', 'dist');
if (fs_1.default.existsSync(CLIENT_DIST)) {
    console.log(`📦 Serving Admin Console UI from: ${CLIENT_DIST}`);
    app.use(express_1.default.static(CLIENT_DIST));
    app.use((req, res, next) => {
        if (req.method === 'GET' && !req.path.startsWith('/api') && !req.path.startsWith('/ws')) {
            return res.sendFile(path_1.default.join(CLIENT_DIST, 'index.html'));
        }
        next();
    });
}
server.listen(port, () => {
    console.log(`=======================================================`);
    console.log(`🚀 Mesh Device Management Gateway running on port ${port}`);
    console.log(`📡 WebSocket Gateway endpoint: ws://localhost:${port}/ws`);
    console.log(`🔒 Default Admin: admin / AdminPassword123!`);
    console.log(`=======================================================`);
});
