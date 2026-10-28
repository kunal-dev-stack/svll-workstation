import React, { useState } from 'react';
import {
  Monitor,
  Smartphone,
  Cpu,
  HardDrive,
  Activity,
  Terminal,
  FolderTree,
  Sliders,
  Trash2,
  Search,
  Filter,
  PlusCircle,
  Wifi,
  WifiOff,
  BatteryCharging,
  Battery,
} from 'lucide-react';
import { DeviceInfo } from '../types';

interface DeviceGridProps {
  devices: DeviceInfo[];
  onSelectSession: (device: DeviceInfo, subTab: 'desktop' | 'terminal' | 'files' | 'processes') => void;
  onRevokeDevice: (device: DeviceInfo) => void;
  onOpenEnrollment: () => void;
}

export const DeviceGrid: React.FC<DeviceGridProps> = ({
  devices,
  onSelectSession,
  onRevokeDevice,
  onOpenEnrollment,
}) => {
  const [search, setSearch] = useState('');
  const [osFilter, setOsFilter] = useState<'all' | 'windows' | 'android'>('all');
  const [statusFilter, setStatusFilter] = useState<'all' | 'online' | 'offline'>('all');

  const filtered = devices.filter((dev) => {
    const matchesSearch =
      dev.hostname.toLowerCase().includes(search.toLowerCase()) ||
      dev.ipAddress.toLowerCase().includes(search.toLowerCase()) ||
      dev.group.toLowerCase().includes(search.toLowerCase()) ||
      dev.tags.some((t) => t.toLowerCase().includes(search.toLowerCase()));

    const matchesOs = osFilter === 'all' || dev.os === osFilter;
    const matchesStatus = statusFilter === 'all' || dev.status === statusFilter;

    return matchesSearch && matchesOs && matchesStatus;
  });

  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '20px' }}>
      {/* Control Bar */}
      <div className="glass-panel" style={{ padding: '16px 20px', display: 'flex', flexWrap: 'wrap', alignItems: 'center', justifyContent: 'space-between', gap: '16px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px', flex: '1', minWidth: '280px' }}>
          <div style={{ position: 'relative', width: '100%', maxWidth: '400px' }}>
            <Search size={18} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)' }} />
            <input
              type="text"
              placeholder="Search by hostname, IP address, tag, or group..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="input-field"
              style={{ paddingLeft: '38px' }}
            />
          </div>
        </div>

        {/* Filter Badges */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flexWrap: 'wrap' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', background: 'rgba(0,0,0,0.3)', padding: '4px', borderRadius: '8px', border: '1px solid var(--border-subtle)' }}>
            <Filter size={14} color="var(--text-muted)" style={{ marginLeft: '4px' }} />
            <button
              className={`btn btn-sm ${osFilter === 'all' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setOsFilter('all')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              All OS
            </button>
            <button
              className={`btn btn-sm ${osFilter === 'windows' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setOsFilter('windows')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              Windows
            </button>
            <button
              className={`btn btn-sm ${osFilter === 'android' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setOsFilter('android')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              Android
            </button>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '4px', background: 'rgba(0,0,0,0.3)', padding: '4px', borderRadius: '8px', border: '1px solid var(--border-subtle)' }}>
            <button
              className={`btn btn-sm ${statusFilter === 'all' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setStatusFilter('all')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              All Status
            </button>
            <button
              className={`btn btn-sm ${statusFilter === 'online' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setStatusFilter('online')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              Online
            </button>
            <button
              className={`btn btn-sm ${statusFilter === 'offline' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setStatusFilter('offline')}
              style={{ border: 'none', padding: '4px 8px', fontSize: '0.75rem' }}
            >
              Offline
            </button>
          </div>

          <button className="btn btn-primary" onClick={onOpenEnrollment}>
            <PlusCircle size={16} />
            <span>Enroll Device</span>
          </button>
        </div>
      </div>

      {/* Device Cards Grid */}
      {filtered.length === 0 ? (
        <div className="glass-panel" style={{ padding: '60px 20px', textAlign: 'center' }}>
          <Monitor size={48} color="var(--text-muted)" style={{ marginBottom: '16px', opacity: 0.5 }} />
          <h3 style={{ fontSize: '1.25rem', marginBottom: '8px' }}>No Managed Devices Found</h3>
          <p style={{ color: 'var(--text-secondary)', maxWidth: '480px', margin: '0 auto 20px auto', fontSize: '0.9rem' }}>
            {devices.length === 0
              ? 'No devices are currently enrolled in your mesh network. Deploy a lightweight agent to your Windows PC or Android phone.'
              : 'No devices match your current search and filter criteria.'}
          </p>
          <button className="btn btn-primary" onClick={onOpenEnrollment}>
            <PlusCircle size={16} />
            <span>Generate Enrollment Token</span>
          </button>
        </div>
      ) : (
        <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fill, minmax(360px, 1fr))', gap: '20px' }}>
          {filtered.map((device) => {
            const isOnline = device.status === 'online' || device.isLiveSocket;
            const telemetry = device.latestTelemetry;
            const cpu = telemetry ? Math.round(telemetry.cpuUsagePercent) : 0;
            const ramPct = telemetry && telemetry.memoryTotalMb ? Math.round((telemetry.memoryUsedMb / telemetry.memoryTotalMb) * 100) : 0;
            const diskPct = telemetry && telemetry.diskTotalGb ? Math.round((telemetry.diskUsedGb / telemetry.diskTotalGb) * 100) : 0;

            return (
              <div
                key={device.id}
                className="glass-panel animate-fade-in"
                style={{
                  padding: '20px',
                  display: 'flex',
                  flexDirection: 'column',
                  gap: '16px',
                  borderTop: isOnline ? '3px solid var(--accent-emerald)' : '3px solid var(--text-muted)',
                  transition: 'transform var(--transition-normal), box-shadow var(--transition-normal)',
                }}
              >
                {/* Header */}
                <div style={{ display: 'flex', alignItems: 'flex-start', justifyContent: 'space-between', gap: '12px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                    <div style={{
                      width: '44px',
                      height: '44px',
                      borderRadius: '12px',
                      background: device.os === 'windows' ? 'rgba(14, 165, 233, 0.15)' : 'rgba(34, 197, 94, 0.15)',
                      border: `1px solid ${device.os === 'windows' ? 'rgba(14, 165, 233, 0.3)' : 'rgba(34, 197, 94, 0.3)'}`,
                      display: 'flex',
                      alignItems: 'center',
                      justifyContent: 'center',
                    }}>
                      {device.os === 'windows' ? (
                        <Monitor size={24} color="#38bdf8" />
                      ) : (
                        <Smartphone size={24} color="#4ade80" />
                      )}
                    </div>
                    <div>
                      <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                        <span style={{ fontWeight: 700, fontSize: '1.05rem' }}>{device.hostname}</span>
                        <span className={`badge ${isOnline ? 'badge-online' : 'badge-offline'}`}>
                          <span className={`pulse-dot ${isOnline ? 'online' : 'offline'}`} />
                          {isOnline ? 'Online' : 'Offline'}
                        </span>
                      </div>
                      <div style={{ fontSize: '0.8rem', color: 'var(--text-secondary)' }}>
                        {device.ipAddress} • {device.os.toUpperCase()} {device.osVersion} ({device.arch})
                      </div>
                    </div>
                  </div>

                  <span className={`badge ${device.os === 'windows' ? 'badge-windows' : 'badge-android'}`}>
                    {device.os}
                  </span>
                </div>

                {/* Tags & Group */}
                <div style={{ display: 'flex', alignItems: 'center', gap: '6px', flexWrap: 'wrap' }}>
                  <span style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '4px', background: 'rgba(255, 255, 255, 0.05)', color: 'var(--text-secondary)', border: '1px solid var(--border-subtle)' }}>
                    Group: {device.group}
                  </span>
                  {device.unattendedAllowed && (
                    <span style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '4px', background: 'rgba(99, 102, 241, 0.15)', color: '#818cf8', border: '1px solid rgba(99, 102, 241, 0.3)' }}>
                      Unattended Access
                    </span>
                  )}
                  {telemetry?.batteryPercent !== undefined && (
                    <span style={{ fontSize: '0.7rem', padding: '2px 8px', borderRadius: '4px', background: 'rgba(16, 185, 129, 0.15)', color: '#34d399', display: 'flex', alignItems: 'center', gap: '4px' }}>
                      {telemetry.isCharging ? <BatteryCharging size={12} /> : <Battery size={12} />}
                      {telemetry.batteryPercent}%
                    </span>
                  )}
                </div>

                {/* Telemetry Gauges */}
                {isOnline && telemetry ? (
                  <div style={{ background: 'rgba(0, 0, 0, 0.25)', padding: '12px', borderRadius: '10px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                    {/* CPU */}
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '4px' }}>
                        <span style={{ color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                          <Cpu size={12} /> CPU Usage
                        </span>
                        <span style={{ fontWeight: 600 }}>{cpu}%</span>
                      </div>
                      <div className="progress-container">
                        <div className="progress-bar progress-cpu" style={{ width: `${Math.min(cpu, 100)}%` }} />
                      </div>
                    </div>

                    {/* RAM */}
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '4px' }}>
                        <span style={{ color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                          <Activity size={12} /> Memory ({Math.round(telemetry.memoryUsedMb / 1024)}GB / {Math.round(telemetry.memoryTotalMb / 1024)}GB)
                        </span>
                        <span style={{ fontWeight: 600 }}>{ramPct}%</span>
                      </div>
                      <div className="progress-container">
                        <div className="progress-bar progress-ram" style={{ width: `${Math.min(ramPct, 100)}%` }} />
                      </div>
                    </div>

                    {/* Disk */}
                    <div>
                      <div style={{ display: 'flex', justifyContent: 'space-between', fontSize: '0.75rem', marginBottom: '4px' }}>
                        <span style={{ color: 'var(--text-secondary)', display: 'flex', alignItems: 'center', gap: '4px' }}>
                          <HardDrive size={12} /> Storage ({Math.round(telemetry.diskUsedGb)}GB / {Math.round(telemetry.diskTotalGb)}GB)
                        </span>
                        <span style={{ fontWeight: 600 }}>{diskPct}%</span>
                      </div>
                      <div className="progress-container">
                        <div className="progress-bar progress-disk" style={{ width: `${Math.min(diskPct, 100)}%` }} />
                      </div>
                    </div>
                  </div>
                ) : (
                  <div style={{ background: 'rgba(0, 0, 0, 0.2)', padding: '12px', borderRadius: '10px', textAlign: 'center', fontSize: '0.8rem', color: 'var(--text-muted)' }}>
                    Last seen: {new Date(device.lastSeen).toLocaleTimeString()} ({new Date(device.lastSeen).toLocaleDateString()})
                  </div>
                )}

                {/* Action Buttons */}
                <div style={{ display: 'flex', gap: '8px', flexWrap: 'wrap', marginTop: 'auto', paddingTop: '8px', borderTop: '1px solid var(--border-subtle)' }}>
                  <button
                    className="btn btn-primary btn-sm"
                    style={{ flex: '1', minWidth: '120px' }}
                    onClick={() => onSelectSession(device, 'desktop')}
                    disabled={!isOnline}
                  >
                    <Monitor size={14} />
                    <span>Remote Desktop</span>
                  </button>

                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={() => onSelectSession(device, 'terminal')}
                    disabled={!isOnline}
                    title="Interactive Shell"
                  >
                    <Terminal size={14} />
                    <span>Shell</span>
                  </button>

                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={() => onSelectSession(device, 'files')}
                    disabled={!isOnline}
                    title="File Transfer"
                  >
                    <FolderTree size={14} />
                    <span>Files</span>
                  </button>

                  <button
                    className="btn btn-secondary btn-sm"
                    onClick={() => onSelectSession(device, 'processes')}
                    disabled={!isOnline}
                    title="Processes & Services"
                  >
                    <Sliders size={14} />
                    <span>Tasks</span>
                  </button>

                  <button
                    className="btn btn-danger btn-sm"
                    onClick={() => onRevokeDevice(device)}
                    title="Emergency Revoke Enrollment"
                    style={{ padding: '6px' }}
                  >
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
};
