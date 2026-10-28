import React, { useState, useEffect } from 'react';
import { FileText, Download, Search, ShieldCheck, AlertTriangle, XCircle, RefreshCw } from 'lucide-react';
import { AuditLogEntry } from '../types';

interface AuditLogViewerProps {
  token: string;
}

export const AuditLogViewer: React.FC<AuditLogViewerProps> = ({ token }) => {
  const [logs, setLogs] = useState<AuditLogEntry[]>([]);
  const [search, setSearch] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'SUCCESS' | 'FAILED' | 'DENIED'>('all');
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    fetchLogs();
  }, []);

  const fetchLogs = async () => {
    setIsLoading(true);
    try {
      const res = await fetch('/api/audit-logs?limit=250', {
        headers: { Authorization: `Bearer ${token}` },
      });
      if (res.ok) {
        const data = await res.json();
        setLogs(data);
      }
    } catch (err) {
      console.error('Error fetching audit logs:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const exportCsv = () => {
    const headers = ['ID', 'Timestamp', 'AdminUser', 'Action', 'TargetDevice', 'Status', 'Details', 'IPAddress'];
    const rows = logs.map((l) => [
      l.id,
      new Date(l.timestamp).toISOString(),
      l.adminUser,
      l.action,
      l.targetDeviceId || 'GLOBAL',
      l.status,
      `"${l.details.replace(/"/g, '""')}"`,
      l.ipAddress,
    ]);
    const csvContent = [headers.join(','), ...rows.map((r) => r.join(','))].join('\n');
    const blob = new Blob([csvContent], { type: 'text/csv' });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = `aegismesh-audit-log-${Date.now()}.csv`;
    a.click();
    URL.revokeObjectURL(url);
  };

  const filtered = logs.filter((l) => {
    const matchesSearch =
      l.action.toLowerCase().includes(search.toLowerCase()) ||
      l.adminUser.toLowerCase().includes(search.toLowerCase()) ||
      l.details.toLowerCase().includes(search.toLowerCase()) ||
      (l.targetDeviceId && l.targetDeviceId.toLowerCase().includes(search.toLowerCase()));

    const matchesStatus = statusFilter === 'all' || l.status === statusFilter;
    return matchesSearch && matchesStatus;
  });

  return (
    <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 140px)', overflow: 'hidden' }}>
      {/* Header Toolbar */}
      <div style={{ padding: '16px 20px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: 'rgba(0,0,0,0.3)', gap: '16px', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
          <div style={{ width: '36px', height: '36px', borderRadius: '10px', background: 'rgba(99, 102, 241, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid rgba(99, 102, 241, 0.3)' }}>
            <FileText size={18} color="#818cf8" />
          </div>
          <div>
            <h3 style={{ fontSize: '1.05rem', fontWeight: 700 }}>Security & Compliance Audit Ledger</h3>
            <p style={{ fontSize: '0.75rem', color: 'var(--text-muted)' }}>Tamper-evident record of administrative operations, session initiations, and commands</p>
          </div>
        </div>

        {/* Filter controls */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px', flexWrap: 'wrap' }}>
          <div style={{ position: 'relative', width: '240px' }}>
            <Search size={14} color="var(--text-muted)" style={{ position: 'absolute', left: '10px', top: '50%', transform: 'translateY(-50%)' }} />
            <input
              type="text"
              placeholder="Search audit trail..."
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="input-field"
              style={{ paddingLeft: '32px', padding: '6px 10px 6px 32px', fontSize: '0.8rem' }}
            />
          </div>

          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value as any)}
            className="input-field"
            style={{ width: 'auto', padding: '6px 12px', fontSize: '0.8rem' }}
          >
            <option value="all">All Outcomes</option>
            <option value="SUCCESS">Success Only</option>
            <option value="FAILED">Failed Only</option>
            <option value="DENIED">Denied Only</option>
          </select>

          <button className="btn btn-secondary btn-sm" onClick={fetchLogs} title="Refresh">
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
          </button>

          <button className="btn btn-secondary btn-sm" onClick={exportCsv} title="Export CSV">
            <Download size={14} />
            <span>Export CSV</span>
          </button>
        </div>
      </div>

      {/* Ledger Table */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '16px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.825rem' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', textAlign: 'left', fontSize: '0.75rem', textTransform: 'uppercase' }}>
              <th style={{ padding: '8px 12px', width: '150px' }}>Timestamp</th>
              <th style={{ padding: '8px 12px', width: '120px' }}>Admin</th>
              <th style={{ padding: '8px 12px', width: '180px' }}>Action</th>
              <th style={{ padding: '8px 12px', width: '140px' }}>Target Device</th>
              <th style={{ padding: '8px 12px' }}>Event Details</th>
              <th style={{ padding: '8px 12px', width: '120px' }}>Source IP</th>
              <th style={{ padding: '8px 12px', width: '90px', textAlign: 'right' }}>Status</th>
            </tr>
          </thead>
          <tbody>
            {filtered.map((log) => {
              let badgeClass = 'badge-online';
              if (log.status === 'FAILED') badgeClass = 'btn-danger';
              if (log.status === 'DENIED') badgeClass = 'badge-offline';

              return (
                <tr key={log.id} style={{ borderBottom: '1px solid rgba(255,255,255,0.03)' }}>
                  <td style={{ padding: '10px 12px', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)', fontSize: '0.75rem' }}>
                    {new Date(log.timestamp).toLocaleTimeString()} {new Date(log.timestamp).toLocaleDateString()}
                  </td>
                  <td style={{ padding: '10px 12px', fontWeight: 600 }}>{log.adminUser}</td>
                  <td style={{ padding: '10px 12px', fontFamily: 'var(--font-mono)', color: '#38bdf8' }}>{log.action}</td>
                  <td style={{ padding: '10px 12px', fontFamily: 'var(--font-mono)', color: 'var(--text-secondary)' }}>
                    {log.targetDeviceId || 'GLOBAL'}
                  </td>
                  <td style={{ padding: '10px 12px', color: '#cbd5e1' }}>{log.details}</td>
                  <td style={{ padding: '10px 12px', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)', fontSize: '0.75rem' }}>
                    {log.ipAddress}
                  </td>
                  <td style={{ padding: '10px 12px', textAlign: 'right' }}>
                    <span className={`badge ${badgeClass}`} style={{ fontSize: '0.65rem' }}>
                      {log.status}
                    </span>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
};
