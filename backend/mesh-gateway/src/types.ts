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
  id: string; // Unique HWID or UUID
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

export interface AdminUser {
  id: string;
  username: string;
  role: 'superadmin' | 'admin' | 'technician' | 'auditor';
  passwordHash: string;
  createdAt: number;
}

export type WSMessageType =
  | 'DEVICE_REGISTER'
  | 'DEVICE_HEARTBEAT'
  | 'FRAME_DESKTOP'
  | 'INPUT_EVENT'
  | 'TERMINAL_START'
  | 'TERMINAL_INPUT'
  | 'TERMINAL_OUTPUT'
  | 'TERMINAL_STOP'
  | 'FILE_LIST_REQ'
  | 'FILE_LIST_RESP'
  | 'FILE_DOWNLOAD_REQ'
  | 'FILE_CHUNK'
  | 'FILE_UPLOAD_REQ'
  | 'COMMAND_EXEC_REQ'
  | 'COMMAND_EXEC_RESP'
  | 'PROCESS_LIST_REQ'
  | 'PROCESS_LIST_RESP'
  | 'PROCESS_KILL_REQ'
  | 'SERVICE_LIST_REQ'
  | 'SERVICE_LIST_RESP'
  | 'SERVICE_CONTROL_REQ'
  | 'SESSION_DISCONNECT'
  | 'SESSION_APPROVAL_REQ'
  | 'SESSION_APPROVAL_RESP';

export interface WSMessage<T = any> {
  type: WSMessageType;
  deviceId?: string;
  sessionId?: string;
  payload: T;
  timestamp: number;
}

export interface InputEventPayload {
  kind: 'mousemove' | 'mousedown' | 'mouseup' | 'keydown' | 'keyup' | 'wheel';
  x?: number; // Normalized 0.0 to 1.0 or pixel coordinates
  y?: number;
  button?: number; // 0: left, 1: middle, 2: right
  key?: string;
  keyCode?: number;
  deltaY?: number;
}
