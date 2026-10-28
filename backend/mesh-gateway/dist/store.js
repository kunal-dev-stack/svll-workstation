"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.store = void 0;
const fs_1 = __importDefault(require("fs"));
const path_1 = __importDefault(require("path"));
const bcryptjs_1 = __importDefault(require("bcryptjs"));
const DATA_DIR = path_1.default.join(__dirname, '..', 'data');
const DB_FILE = path_1.default.join(DATA_DIR, 'mesh_db.json');
class DataStore {
    db = {
        devices: {},
        enrollmentTokens: {},
        auditLogs: [],
        users: {},
    };
    constructor() {
        this.init();
    }
    init() {
        if (!fs_1.default.existsSync(DATA_DIR)) {
            fs_1.default.mkdirSync(DATA_DIR, { recursive: true });
        }
        if (fs_1.default.existsSync(DB_FILE)) {
            try {
                const raw = fs_1.default.readFileSync(DB_FILE, 'utf-8');
                this.db = JSON.parse(raw);
            }
            catch (err) {
                console.error('Failed to parse database file, initializing defaults:', err);
                this.seedDefaults();
            }
        }
        else {
            this.seedDefaults();
        }
    }
    seedDefaults() {
        // Seed default administrator: username: "admin", password: "AdminPassword123!"
        const salt = bcryptjs_1.default.genSaltSync(10);
        const hash = bcryptjs_1.default.hashSync('AdminPassword123!', salt);
        const adminUser = {
            id: 'usr-admin-01',
            username: 'admin',
            role: 'superadmin',
            passwordHash: hash,
            createdAt: Date.now(),
        };
        this.db.users[adminUser.username] = adminUser;
        // Seed a standard enrollment token valid for 7 days
        const defaultToken = {
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
    save() {
        try {
            const tempPath = `${DB_FILE}.tmp`;
            fs_1.default.writeFileSync(tempPath, JSON.stringify(this.db, null, 2), 'utf-8');
            fs_1.default.renameSync(tempPath, DB_FILE);
        }
        catch (err) {
            console.error('Error saving database:', err);
        }
    }
    // --- Users ---
    getUser(username) {
        return this.db.users[username];
    }
    // --- Devices ---
    getDevices() {
        return Object.values(this.db.devices);
    }
    getDevice(id) {
        return this.db.devices[id];
    }
    upsertDevice(device) {
        this.db.devices[device.id] = device;
        this.save();
    }
    removeDevice(id) {
        if (this.db.devices[id]) {
            delete this.db.devices[id];
            this.save();
            return true;
        }
        return false;
    }
    // --- Enrollment Tokens ---
    createToken(token) {
        this.db.enrollmentTokens[token.token] = token;
        this.save();
    }
    getToken(token) {
        return this.db.enrollmentTokens[token];
    }
    useToken(token) {
        const record = this.db.enrollmentTokens[token];
        if (!record)
            return false;
        if (record.expiresAt < Date.now())
            return false;
        if (record.usedCount >= record.maxUses)
            return false;
        record.usedCount += 1;
        this.save();
        return true;
    }
    getAllTokens() {
        return Object.values(this.db.enrollmentTokens);
    }
    // --- Audit Logs ---
    addAuditLog(entry) {
        this.db.auditLogs.unshift(entry);
        // Keep last 5000 audit records
        if (this.db.auditLogs.length > 5000) {
            this.db.auditLogs.pop();
        }
        this.save();
    }
    getAuditLogs(limit = 100) {
        return this.db.auditLogs.slice(0, limit);
    }
}
exports.store = new DataStore();
