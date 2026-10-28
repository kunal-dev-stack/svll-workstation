import crypto from 'crypto';
import { store } from './store';
import { AuditLogEntry } from './types';

export function logAudit(
  adminUser: string,
  action: string,
  targetDeviceId: string | undefined,
  details: string,
  ipAddress: string,
  status: 'SUCCESS' | 'FAILED' | 'DENIED' = 'SUCCESS'
): AuditLogEntry {
  const entry: AuditLogEntry = {
    id: `audit-${Date.now()}-${crypto.randomUUID().substring(0, 8)}`,
    timestamp: Date.now(),
    adminUser,
    action,
    targetDeviceId,
    details,
    ipAddress,
    status,
  };

  store.addAuditLog(entry);
  console.log(`[AUDIT] [${status}] ${adminUser} -> ${action} on ${targetDeviceId || 'GLOBAL'}: ${details}`);
  return entry;
}
