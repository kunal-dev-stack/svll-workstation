"use strict";
var __importDefault = (this && this.__importDefault) || function (mod) {
    return (mod && mod.__esModule) ? mod : { "default": mod };
};
Object.defineProperty(exports, "__esModule", { value: true });
exports.logAudit = logAudit;
const crypto_1 = __importDefault(require("crypto"));
const store_1 = require("./store");
function logAudit(adminUser, action, targetDeviceId, details, ipAddress, status = 'SUCCESS') {
    const entry = {
        id: `audit-${Date.now()}-${crypto_1.default.randomUUID().substring(0, 8)}`,
        timestamp: Date.now(),
        adminUser,
        action,
        targetDeviceId,
        details,
        ipAddress,
        status,
    };
    store_1.store.addAuditLog(entry);
    console.log(`[AUDIT] [${status}] ${adminUser} -> ${action} on ${targetDeviceId || 'GLOBAL'}: ${details}`);
    return entry;
}
