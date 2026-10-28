import React from 'react';
import { Shield, Monitor, Key, FileText, CheckCircle2, LogOut, Radio } from 'lucide-react';

interface NavbarProps {
  currentTab: 'devices' | 'session' | 'enrollment' | 'audit' | 'compliance';
  setCurrentTab: (tab: 'devices' | 'session' | 'enrollment' | 'audit' | 'compliance') => void;
  deviceStats: { total: number; online: number; inSession: number };
  activeDeviceName?: string;
  adminUser: string;
  onLogout: () => void;
  gatewayConnected: boolean;
}

export const Navbar: React.FC<NavbarProps> = ({
  currentTab,
  setCurrentTab,
  deviceStats,
  activeDeviceName,
  adminUser,
  onLogout,
  gatewayConnected,
}) => {
  return (
    <header className="glass-nav" style={{ position: 'sticky', top: 0, zIndex: 100, padding: '12px 24px' }}>
      <div style={{ maxWidth: '1600px', margin: '0 auto', display: 'flex', alignItems: 'center', justifyContent: 'space-between' }}>
        {/* Brand */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '14px' }}>
          <div style={{
            width: '42px',
            height: '42px',
            borderRadius: '12px',
            background: 'linear-gradient(135deg, #6366f1 0%, #06b6d4 100%)',
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'center',
            boxShadow: '0 0 20px rgba(99, 102, 241, 0.5)',
          }}>
            <Shield size={24} color="#ffffff" />
          </div>
          <div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
              <span style={{ fontFamily: 'var(--font-display)', fontWeight: 800, fontSize: '1.25rem', letterSpacing: '-0.02em', background: 'linear-gradient(90deg, #ffffff, #cbd5e1)', WebkitBackgroundClip: 'text', WebkitTextFillColor: 'transparent' }}>
                AegisMesh
              </span>
              <span style={{ fontSize: '0.65rem', padding: '2px 6px', borderRadius: '4px', background: 'rgba(99, 102, 241, 0.2)', color: '#818cf8', fontWeight: 700, border: '1px solid rgba(99, 102, 241, 0.4)' }}>
                ENTERPRISE
              </span>
            </div>
            <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
              <span className={`pulse-dot ${gatewayConnected ? 'online' : 'offline'}`} />
              <span>Gateway: {gatewayConnected ? 'Connected (TLS/WSS)' : 'Connecting...'}</span>
            </div>
          </div>
        </div>

        {/* Navigation Tabs */}
        <nav style={{ display: 'flex', alignItems: 'center', gap: '6px', background: 'rgba(0, 0, 0, 0.3)', padding: '4px', borderRadius: '12px', border: '1px solid var(--border-subtle)' }}>
          <button
            className={`btn btn-sm ${currentTab === 'devices' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setCurrentTab('devices')}
            style={{ border: currentTab === 'devices' ? 'none' : 'transparent' }}
          >
            <Monitor size={16} />
            <span>Devices</span>
            <span style={{ padding: '1px 6px', borderRadius: '9999px', fontSize: '0.7rem', background: 'rgba(255, 255, 255, 0.2)' }}>
              {deviceStats.total}
            </span>
          </button>

          {activeDeviceName && (
            <button
              className={`btn btn-sm ${currentTab === 'session' ? 'btn-primary' : 'btn-secondary'}`}
              onClick={() => setCurrentTab('session')}
              style={{
                border: currentTab === 'session' ? 'none' : 'transparent',
                background: currentTab === 'session' ? 'linear-gradient(135deg, #06b6d4, #0284c7)' : 'rgba(6, 182, 212, 0.15)',
                color: '#38bdf8',
              }}
            >
              <Radio size={16} />
              <span>Session: {activeDeviceName}</span>
            </button>
          )}

          <button
            className={`btn btn-sm ${currentTab === 'enrollment' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setCurrentTab('enrollment')}
            style={{ border: currentTab === 'enrollment' ? 'none' : 'transparent' }}
          >
            <Key size={16} />
            <span>Enrollment</span>
          </button>

          <button
            className={`btn btn-sm ${currentTab === 'audit' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setCurrentTab('audit')}
            style={{ border: currentTab === 'audit' ? 'none' : 'transparent' }}
          >
            <FileText size={16} />
            <span>Audit Trail</span>
          </button>

          <button
            className={`btn btn-sm ${currentTab === 'compliance' ? 'btn-primary' : 'btn-secondary'}`}
            onClick={() => setCurrentTab('compliance')}
            style={{ border: currentTab === 'compliance' ? 'none' : 'transparent' }}
          >
            <CheckCircle2 size={16} color="#34d399" />
            <span>AV & Security</span>
          </button>
        </nav>

        {/* User & Quick Stats */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ display: 'flex', gap: '12px', fontSize: '0.8rem', borderRight: '1px solid var(--border-subtle)', paddingRight: '16px' }}>
            <div>
              <span style={{ color: 'var(--text-muted)' }}>Online: </span>
              <span style={{ color: '#34d399', fontWeight: 700 }}>{deviceStats.online}</span>
            </div>
            <div>
              <span style={{ color: 'var(--text-muted)' }}>Active Sessions: </span>
              <span style={{ color: '#38bdf8', fontWeight: 700 }}>{deviceStats.inSession}</span>
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <div style={{ textAlign: 'right' }}>
              <div style={{ fontSize: '0.85rem', fontWeight: 600 }}>{adminUser}</div>
              <div style={{ fontSize: '0.7rem', color: '#818cf8', textTransform: 'uppercase' }}>SuperAdmin</div>
            </div>
            <button
              className="btn btn-secondary btn-sm"
              onClick={onLogout}
              title="Sign Out"
              style={{ padding: '8px', borderRadius: '8px' }}
            >
              <LogOut size={16} color="#fb7185" />
            </button>
          </div>
        </div>
      </div>
    </header>
  );
};
