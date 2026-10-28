import React, { useState, useEffect } from 'react';
import { Sliders, Cpu, Play, Square, RotateCcw, XCircle, Search, RefreshCw } from 'lucide-react';
import { DeviceInfo, ProcessItem, ServiceItem } from '../types';

interface ProcessServiceManagerProps {
  device: DeviceInfo;
  socket: WebSocket | null;
}

export const ProcessServiceManager: React.FC<ProcessServiceManagerProps> = ({ device, socket }) => {
  const [tab, setTab] = useState<'processes' | 'services'>('processes');
  const [search, setSearch] = useState('');
  const [processes, setProcesses] = useState<ProcessItem[]>([
    { pid: 4, name: 'System', cpuPercent: 0.8, memoryMb: 85 },
    { pid: 840, name: 'services.exe', cpuPercent: 0.1, memoryMb: 14 },
    { pid: 1024, name: 'svchost.exe', cpuPercent: 1.4, memoryMb: 120 },
    { pid: 1420, name: 'explorer.exe', cpuPercent: 2.1, memoryMb: 245 },
    { pid: 3200, name: 'MeshAgentService.exe', cpuPercent: 0.4, memoryMb: 38 },
    { pid: 4120, name: 'chrome.exe', cpuPercent: 4.8, memoryMb: 580 },
    { pid: 5612, name: 'code.exe', cpuPercent: 3.2, memoryMb: 410 },
  ]);

  const [services, setServices] = useState<ServiceItem[]>([
    { name: 'MeshAgent', displayName: 'AegisMesh Endpoint Agent Service', status: 'Running' },
    { name: 'WinDefend', displayName: 'Microsoft Defender Antivirus Service', status: 'Running' },
    { name: 'wuauserv', displayName: 'Windows Update', status: 'Running' },
    { name: 'Spooler', displayName: 'Print Spooler', status: 'Stopped' },
    { name: 'EventLog', displayName: 'Windows Event Log', status: 'Running' },
    { name: 'LanmanServer', displayName: 'Server Service', status: 'Running' },
    { name: 'Dhcp', displayName: 'DHCP Client', status: 'Running' },
  ]);

  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!socket) return;

    const handleMessage = (event: MessageEvent) => {
      try {
        if (typeof event.data !== 'string') return;
        const msg = JSON.parse(event.data);

        if (msg.type === 'PROCESS_LIST_RESP') {
          const raw = msg.payload?.processes || msg.payload?.Processes;
          if (raw && Array.isArray(raw)) {
            const normalized = raw.map((p: any) => ({
              pid: p.pid ?? p.Pid ?? 0,
              name: p.name || p.Name || 'Unknown',
              cpuPercent: p.cpuPercent ?? p.CpuPercent ?? 0.5,
              memoryMb: p.memoryMb ?? p.MemoryMb ?? 10,
            }));
            setProcesses(normalized);
            setIsLoading(false);
          }
        } else if (msg.type === 'SERVICE_LIST_RESP') {
          const raw = msg.payload?.services || msg.payload?.Services;
          if (raw && Array.isArray(raw)) {
            const normalized = raw.map((s: any) => ({
              name: s.name || s.Name || '',
              displayName: s.displayName || s.DisplayName || s.name || s.Name || '',
              status: s.status || s.Status || 'Unknown',
            }));
            setServices(normalized);
            setIsLoading(false);
          }
        }
      } catch {
        // ignore
      }
    };

    socket.addEventListener('message', handleMessage);
    return () => socket.removeEventListener('message', handleMessage);
  }, [socket]);

  useEffect(() => {
    if (socket && socket.readyState === WebSocket.OPEN) {
      refresh();
    } else if (socket) {
      socket.addEventListener('open', () => refresh(), { once: true });
    }
  }, [socket, tab]);

  const refresh = () => {
    setIsLoading(true);
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(
        JSON.stringify({
          type: tab === 'processes' ? 'PROCESS_LIST_REQ' : 'SERVICE_LIST_REQ',
          payload: {},
          timestamp: Date.now(),
        })
      );
    }
    setTimeout(() => setIsLoading(false), 500);
  };

  const handleKillProcess = (pid: number, name: string) => {
    if (confirm(`Are you sure you want to terminate process ${name} (PID: ${pid})? This action is recorded in the security audit log.`)) {
      if (socket && socket.readyState === WebSocket.OPEN) {
        socket.send(
          JSON.stringify({
            type: 'PROCESS_KILL_REQ',
            payload: { pid },
            timestamp: Date.now(),
          })
        );
      }
      setProcesses((prev) => prev.filter((p) => p.pid !== pid));
    }
  };

  const handleServiceControl = (serviceName: string, action: 'start' | 'stop' | 'restart') => {
    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(
        JSON.stringify({
          type: 'SERVICE_CONTROL_REQ',
          payload: { serviceName, action },
          timestamp: Date.now(),
        })
      );
    }

    setServices((prev) =>
      prev.map((s) => {
        if (s.name === serviceName) {
          return {
            ...s,
            status: action === 'stop' ? 'Stopped' : 'Running',
          };
        }
        return s;
      })
    );
  };

  const filteredProcesses = processes.filter(
    (p) => p.name.toLowerCase().includes(search.toLowerCase()) || p.pid.toString().includes(search)
  );

  const filteredServices = services.filter(
    (s) =>
      s.name.toLowerCase().includes(search.toLowerCase()) ||
      s.displayName.toLowerCase().includes(search.toLowerCase())
  );

  return (
    <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 180px)', overflow: 'hidden' }}>
      {/* Top Bar */}
      <div style={{ padding: '12px 18px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: 'rgba(0,0,0,0.3)', gap: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div style={{ display: 'flex', background: 'rgba(0,0,0,0.4)', padding: '2px', borderRadius: '8px', border: '1px solid var(--border-subtle)' }}>
            <button
              className={`btn btn-sm ${tab === 'processes' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setTab('processes')}
              style={{ border: 'none' }}
            >
              <Cpu size={14} />
              <span>Processes ({processes.length})</span>
            </button>
            <button
              className={`btn btn-sm ${tab === 'services' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setTab('services')}
              style={{ border: 'none' }}
            >
              <Sliders size={14} />
              <span>Services ({services.length})</span>
            </button>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <div style={{ position: 'relative', width: '220px' }}>
            <Search size={14} color="var(--text-muted)" style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)' }} />
            <input
              type="text"
              placeholder="Search..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="input-field"
              style={{ paddingLeft: '32px', padding: '6px 10px 6px 32px', fontSize: '0.8rem' }}
            />
          </div>
          <button className="btn btn-secondary btn-sm" onClick={refresh} title="Refresh">
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
          </button>
        </div>
      </div>

      {/* Content Table */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '12px' }}>
        {tab === 'processes' ? (
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', textAlign: 'left', fontSize: '0.75rem', textTransform: 'uppercase' }}>
                <th style={{ padding: '8px 12px', width: '100px' }}>PID</th>
                <th style={{ padding: '8px 12px' }}>Process Name</th>
                <th style={{ padding: '8px 12px', width: '120px' }}>CPU</th>
                <th style={{ padding: '8px 12px', width: '140px' }}>Memory</th>
                <th style={{ padding: '8px 12px', width: '100px', textAlign: 'right' }}>Action</th>
              </tr>
            </thead>
            <tbody>
              {filteredProcesses.map((p) => (
                <tr key={p.pid} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                  <td style={{ padding: '8px 12px', fontFamily: 'var(--font-mono)', color: 'var(--text-muted)' }}>{p.pid}</td>
                  <td style={{ padding: '8px 12px', fontWeight: 600 }}>{p.name}</td>
                  <td style={{ padding: '8px 12px', color: '#38bdf8' }}>{p.cpuPercent ?? 0}%</td>
                  <td style={{ padding: '8px 12px', color: '#34d399' }}>{p.memoryMb ?? 0} MB</td>
                  <td style={{ padding: '8px 12px', textAlign: 'right' }}>
                    <button
                      className="btn btn-danger btn-sm"
                      style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                      onClick={() => handleKillProcess(p.pid, p.name)}
                    >
                      <XCircle size={12} />
                      <span>End Task</span>
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        ) : (
          <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
            <thead>
              <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', textAlign: 'left', fontSize: '0.75rem', textTransform: 'uppercase' }}>
                <th style={{ padding: '8px 12px' }}>Service Name</th>
                <th style={{ padding: '8px 12px' }}>Display Name</th>
                <th style={{ padding: '8px 12px', width: '120px' }}>Status</th>
                <th style={{ padding: '8px 12px', width: '180px', textAlign: 'right' }}>Controls</th>
              </tr>
            </thead>
            <tbody>
              {filteredServices.map((s) => (
                <tr key={s.name} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                  <td style={{ padding: '8px 12px', fontFamily: 'var(--font-mono)', fontWeight: 600 }}>{s.name}</td>
                  <td style={{ padding: '8px 12px', color: 'var(--text-secondary)' }}>{s.displayName}</td>
                  <td style={{ padding: '8px 12px' }}>
                    <span className={`badge ${s.status === 'Running' ? 'badge-online' : 'badge-offline'}`}>
                      {s.status}
                    </span>
                  </td>
                  <td style={{ padding: '8px 12px', textAlign: 'right' }}>
                    <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '6px' }}>
                      {s.status !== 'Running' ? (
                        <button
                          className="btn btn-success btn-sm"
                          style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                          onClick={() => handleServiceControl(s.name, 'start')}
                        >
                          <Play size={12} /> Start
                        </button>
                      ) : (
                        <button
                          className="btn btn-danger btn-sm"
                          style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                          onClick={() => handleServiceControl(s.name, 'stop')}
                        >
                          <Square size={12} /> Stop
                        </button>
                      )}
                      <button
                        className="btn btn-secondary btn-sm"
                        style={{ padding: '4px 8px', fontSize: '0.75rem' }}
                        onClick={() => handleServiceControl(s.name, 'restart')}
                        title="Restart Service"
                      >
                        <RotateCcw size={12} />
                      </button>
                    </div>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </div>
    </div>
  );
};
