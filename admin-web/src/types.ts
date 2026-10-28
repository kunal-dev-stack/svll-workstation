export type DeviceOS = 'windows' | 'android' | 'linux' | 'macos' | 'ios';
export type DeviceStatus = 'online' | 'offline' | 'busy';

export interface DeviceTelemetry {
  cpuUsagePercent: number;
  memoryUsedMb: number;
  memoryTotalMb: number;
  diskUsedGb: number;
  diskTotalGb: number;
  batteryPercent?: number;
  isCharging?: boolean;
  networkType?: string;
  ipAddress: string;
  uptimeSeconds: number;
  timestamp: number;
}

export interface DeviceInfo {
  id: string;
  hostname: string;
  os: DeviceOS;
  osVersion: string;
  arch: string;
  agentVersion: string;
  ipAddress: string;
  status: DeviceStatus;
  lastSeen: number;
  enrolledAt: number;
  group: string;
  tags: string[];
  unattendedAllowed: boolean;
  isLiveSocket?: boolean;
  capabilities: {
    remoteDesktop: boolean;
    remoteTerminal: boolean;
    fileTransfer: boolean;
    processManager: boolean;
    serviceManager: boolean;
    diagnostics: boolean;
  };
  latestTelemetry?: DeviceTelemetry;
}

export interface EnrollmentToken {
  token: string;
  createdBy: string;
  createdAt: number;
  expiresAt: number;
  maxUses: number;
  usedCount: number;
  group: string;
  unattendedAllowed: boolean;
}

export interface AuditLogEntry {
  id: string;
  timestamp: number;
  adminUser: string;
  action: string;
  targetDeviceId?: string;
  details: string;
  ipAddress: string;
  status: 'SUCCESS' | 'FAILED' | 'DENIED';
}

export interface FileItem {
  name: string;
  path: string;
  isDirectory: boolean;
  sizeBytes: number;
  modifiedAt: number;
}

export interface ProcessItem {
  pid: number;
  name: string;
  cpuPercent?: number;
  memoryMb?: number;
}

export interface ServiceItem {
  name: string;
  displayName: string;
  status: 'Running' | 'Stopped' | 'Paused' | 'Unknown';
}
