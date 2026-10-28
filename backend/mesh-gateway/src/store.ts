import fs from 'fs';
import path from 'path';
import bcrypt from 'bcryptjs';
import { DeviceInfo, EnrollmentToken, AuditLogEntry, AdminUser } from './types';

interface DatabaseSchema {
  devices: Record<string, DeviceInfo>;
  enrollmentTokens: Record<string, EnrollmentToken>;
  auditLogs: AuditLogEntry[];
  users: Record<string, AdminUser>;
}

const DATA_DIR = path.join(__dirname, '..', 'data');
const DB_FILE = path.join(DATA_DIR, 'mesh_db.json');

class DataStore {
  private db: DatabaseSchema = {
    devices: {},
    enrollmentTokens: {},
    auditLogs: [],
    users: {},
  };

  constructor() {
    this.init();
  }

  private init() {
    if (!fs.existsSync(DATA_DIR)) {
      fs.mkdirSync(DATA_DIR, { recursive: true });
    }

    if (fs.existsSync(DB_FILE)) {
      try {
        const raw = fs.readFileSync(DB_FILE, 'utf-8');
        this.db = JSON.parse(raw);
      } catch (err) {
        console.error('Failed to parse database file, initializing defaults:', err);
        this.seedDefaults();
      }
    } else {
      this.seedDefaults();
    }
  }

  private seedDefaults() {
    // Seed default administrator: username: "admin", password: "AdminPassword123!"
    const salt = bcrypt.genSaltSync(10);
    const hash = bcrypt.hashSync('AdminPassword123!', salt);
    const adminUser: AdminUser = {
      id: 'usr-admin-01',
      username: 'admin',
      role: 'superadmin',
      passwordHash: hash,
      createdAt: Date.now(),
    };

    this.db.users[adminUser.username] = adminUser;

    // Seed a standard enrollment token valid for 7 days
    const defaultToken: EnrollmentToken = {
      token: 'MESH-ENTERPRISE-TOKEN-2026',
      createdBy: 'system',
      createdAt: Date.now(),
      expiresAt: Date.now() + 7 * 24 * 60 * 60 * 1000,
      maxUses: 100,
      usedCount: 0,
      group: 'Default Endpoints',
      unattendedAllowed: true,
    };
    this.db.enrollmentTokens[defaultToken.token] = defaultToken;

    this.save();
  }

  public save() {
    try {
      const tempPath = `${DB_FILE}.tmp`;
      fs.writeFileSync(tempPath, JSON.stringify(this.db, null, 2), 'utf-8');
      fs.renameSync(tempPath, DB_FILE);
    } catch (err) {
      console.error('Error saving database:', err);
    }
  }

  // --- Users ---
  public getUser(username: string): AdminUser | undefined {
    return this.db.users[username];
  }

  // --- Devices ---
  public getDevices(): DeviceInfo[] {
    return Object.values(this.db.devices);
  }

  public getDevice(id: string): DeviceInfo | undefined {
    return this.db.devices[id];
  }

  public upsertDevice(device: DeviceInfo): void {
    this.db.devices[device.id] = device;
    this.save();
  }

  public removeDevice(id: string): boolean {
    if (this.db.devices[id]) {
      delete this.db.devices[id];
      this.save();
      return true;
    }
    return false;
  }

  // --- Enrollment Tokens ---
  public createToken(token: EnrollmentToken): void {
    this.db.enrollmentTokens[token.token] = token;
    this.save();
  }

  public getToken(token: string): EnrollmentToken | undefined {
    return this.db.enrollmentTokens[token];
  }

  public useToken(token: string): boolean {
    const record = this.db.enrollmentTokens[token];
    if (!record) return false;
    if (record.expiresAt < Date.now()) return false;
    if (record.usedCount >= record.maxUses) return false;

    record.usedCount += 1;
    this.save();
    return true;
  }

  public getAllTokens(): EnrollmentToken[] {
    return Object.values(this.db.enrollmentTokens);
  }

  // --- Audit Logs ---
  public addAuditLog(entry: AuditLogEntry): void {
    this.db.auditLogs.unshift(entry);
    // Keep last 5000 audit records
    if (this.db.auditLogs.length > 5000) {
      this.db.auditLogs.pop();
    }
    this.save();
  }

  public getAuditLogs(limit: number = 100): AuditLogEntry[] {
    return this.db.auditLogs.slice(0, limit);
  }
}

export const store = new DataStore();
