import http from 'http';
import express from 'express';
import cors from 'cors';
import dotenv from 'dotenv';
import path from 'path';
import fs from 'fs';
import authRoutes from './routes/authRoutes';
import { createDeviceRoutes } from './routes/deviceRoutes';
import auditRoutes from './routes/auditRoutes';
import downloadRoutes from './routes/downloadRoutes';
import { MeshGateway } from './meshGateway';

dotenv.config();

const app = express();
const port = process.env.PORT || 5000;

app.use(cors({ origin: '*' }));
app.use(express.json({ limit: '50mb' }));
app.use(express.urlencoded({ extended: true, limit: '50mb' }));

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
const server = http.createServer(app);

// Attach Mesh Gateway WebSocket Engine
const gateway = new MeshGateway(server);

// Mount REST Routes
app.use('/api/auth', authRoutes);
app.use('/api/agent', downloadRoutes);
app.use('/api', createDeviceRoutes(gateway));
app.use('/api/audit-logs', auditRoutes);

// Serve static client-web production build if available
const CLIENT_DIST = path.join(__dirname, '..', '..', 'client-web', 'dist');
if (fs.existsSync(CLIENT_DIST)) {
  console.log(`📦 Serving Admin Console UI from: ${CLIENT_DIST}`);
  app.use(express.static(CLIENT_DIST));
  app.use((req, res, next) => {
    if (req.method === 'GET' && !req.path.startsWith('/api') && !req.path.startsWith('/ws')) {
      return res.sendFile(path.join(CLIENT_DIST, 'index.html'));
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

export { app, server, gateway };
