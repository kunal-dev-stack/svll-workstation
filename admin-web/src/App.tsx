import React, { useState, useEffect, useRef } from 'react';
import {
  Shield,
  Monitor,
  Terminal,
  FolderTree,
  Sliders,
  ArrowLeft,
  Lock,
  User,
  Radio,
  CheckCircle2,
  AlertCircle,
} from 'lucide-react';
import { Navbar } from './components/Navbar';
import { DeviceGrid } from './components/DeviceGrid';
import { RemoteDesktop } from './components/RemoteDesktop';
import { RemoteTerminal } from './components/RemoteTerminal';
import { FileManager } from './components/FileManager';
import { ProcessServiceManager } from './components/ProcessServiceManager';
import { EnrollmentModal } from './components/EnrollmentModal';
import { AuditLogViewer } from './components/AuditLogViewer';
import { AntivirusCompliance } from './components/AntivirusCompliance';
import { DeviceInfo } from './types';

export const App: React.FC = () => {
  // Auth state
  const [authToken, setAuthToken] = useState<string | null>(localStorage.getItem('aegis_token'));
  const [adminUser, setAdminUser] = useState<string>(localStorage.getItem('aegis_user') || 'admin');
  const [loginUsername, setLoginUsername] = useState('admin');
  const [loginPassword, setLoginPassword] = useState('AdminPassword123!');
  const [loginError, setLoginError] = useState<string | null>(null);

  // App navigation
  const [currentTab, setCurrentTab] = useState<'devices' | 'session' | 'enrollment' | 'audit' | 'compliance'>('devices');
  const [sessionSubTab, setSessionSubTab] = useState<'desktop' | 'terminal' | 'files' | 'processes'>('desktop');
  const [activeDevice, setActiveDevice] = useState<DeviceInfo | null>(null);
  const [isEnrollmentOpen, setIsEnrollmentOpen] = useState(false);

  // Device & gateway data
  const [devices, setDevices] = useState<DeviceInfo[]>([]);
  const [gatewayConnected, setGatewayConnected] = useState(false);
  const sessionSocketRef = useRef<WebSocket | null>(null);

  // --- Auth Handlers ---
  const handleLogin = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoginError(null);
    try {
      const res = await fetch('/api/auth/login', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ username: loginUsername, password: loginPassword }),
      });
      const data = await res.json();
      if (!res.ok) {
        setLoginError(data.error || 'Authentication failed');
        return;
      }

      setAuthToken(data.token);
      setAdminUser(data.user.username);
      localStorage.setItem('aegis_token', data.token);
      localStorage.setItem('aegis_user', data.user.username);
    } catch (err: any) {
      setLoginError('Could not connect to Mesh Server gateway');
    }
  };

  const handleLogout = () => {
    setAuthToken(null);
    localStorage.removeItem('aegis_token');
    localStorage.removeItem('aegis_user');
    if (sessionSocketRef.current) {
      sessionSocketRef.current.close();
    }
  };

  // --- Device List Polling & Refresh ---
  const fetchDevices = async () => {
    if (!authToken) return;
    try {
      const res = await fetch('/api/devices', {
        headers: { Authorization: `Bearer ${authToken}` },
      });
      if (res.ok) {
        const data = await res.json();
        setDevices(data);
        setGatewayConnected(true);
      }
    } catch (err) {
      setGatewayConnected(false);
    }
  };

  useEffect(() => {
    if (authToken) {
      fetchDevices();
      const interval = setInterval(fetchDevices, 4000);
      return () => clearInterval(interval);
    }
  }, [authToken]);

  // --- Session Initiation ---
  const handleSelectSession = (device: DeviceInfo, subTab: 'desktop' | 'terminal' | 'files' | 'processes') => {
    setActiveDevice(device);
    setSessionSubTab(subTab);
    setCurrentTab('session');

    // Close previous session socket if open
    if (sessionSocketRef.current) {
      sessionSocketRef.current.close();
    }

    // Connect admin WebSocket session for the selected device
    const protocol = window.location.protocol === 'https:' ? 'wss:' : 'ws:';
    const wsUrl = `${protocol}//${window.location.host}/ws?role=admin&token=${authToken}&deviceId=${device.id}&sessionType=${subTab}`;
    const ws = new WebSocket(wsUrl);

    ws.onopen = () => {
      console.log(`[SESSION] WebSocket connected to device: ${device.id}`);
    };

    ws.onerror = (err) => {
      console.error('[SESSION] WebSocket error:', err);
    };

    ws.onclose = () => {
      console.log('[SESSION] WebSocket closed');
    };

    sessionSocketRef.current = ws;
  };

  const handleDisconnectSession = () => {
    if (sessionSocketRef.current) {
      sessionSocketRef.current.close();
      sessionSocketRef.current = null;
    }
    setActiveDevice(null);
    setCurrentTab('devices');
  };

  const handleRevokeDevice = async (device: DeviceInfo) => {
    if (confirm(`EMERGENCY REVOCATION: Are you sure you want to de-enroll and disconnect ${device.hostname}? This action terminates all sessions and deregisters the agent.`)) {
      try {
        const res = await fetch(`/api/devices/${device.id}`, {
          method: 'DELETE',
          headers: { Authorization: `Bearer ${authToken}` },
        });
        if (res.ok) {
          fetchDevices();
          if (activeDevice?.id === device.id) {
            handleDisconnectSession();
          }
        }
      } catch (err) {
        console.error('Revocation error:', err);
      }
    }
  };

  // --- Calculate Statistics ---
  const deviceStats = {
    total: devices.length,
    online: devices.filter((d) => d.status === 'online' || d.isLiveSocket).length,
    inSession: activeDevice ? 1 : 0,
  };

  // --- LOGIN SCREEN ---
  if (!authToken) {
    return (
      <div style={{ minHeight: '100vh', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px' }}>
        <div className="glass-panel animate-fade-in" style={{ width: '100%', maxWidth: '440px', padding: '36px', border: '1px solid rgba(99, 102, 241, 0.3)', boxShadow: '0 25px 60px rgba(0,0,0,0.8)' }}>
          <div style={{ textAlign: 'center', marginBottom: '28px' }}>
            <div style={{
              width: '60px',
              height: '60px',
              borderRadius: '16px',
              background: 'linear-gradient(135deg, #6366f1 0%, #06b6d4 100%)',
              display: 'flex',
              alignItems: 'center',
              justifyContent: 'center',
              margin: '0 auto 16px auto',
              boxShadow: '0 0 30px rgba(99, 102, 241, 0.5)',
            }}>
              <Shield size={32} color="#ffffff" />
            </div>
            <h1 style={{ fontFamily: 'var(--font-display)', fontSize: '1.75rem', fontWeight: 800, letterSpacing: '-0.02em', marginBottom: '4px' }}>
              AegisMesh Admin Console
            </h1>
            <p style={{ color: 'var(--text-secondary)', fontSize: '0.85rem' }}>
              Cross-Platform Enterprise Remote Device Management
            </p>
          </div>

          {loginError && (
            <div style={{ background: 'rgba(244, 63, 94, 0.15)', border: '1px solid rgba(244, 63, 94, 0.3)', color: '#fb7185', padding: '10px 14px', borderRadius: '8px', fontSize: '0.825rem', marginBottom: '18px', display: 'flex', alignItems: 'center', gap: '8px' }}>
              <AlertCircle size={16} />
              <span>{loginError}</span>
            </div>
          )}

          <form onSubmit={handleLogin} style={{ display: 'flex', flexDirection: 'column', gap: '16px' }}>
            <div>
              <label style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '6px' }}>
                Administrator Username
              </label>
              <div style={{ position: 'relative' }}>
                <User size={16} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)' }} />
                <input
                  type="text"
                  value={loginUsername}
                  onChange={(e) => setLoginUsername(e.target.value)}
                  className="input-field"
                  style={{ paddingLeft: '38px' }}
                  required
                />
              </div>
            </div>

            <div>
              <label style={{ fontSize: '0.8rem', color: 'var(--text-secondary)', display: 'block', marginBottom: '6px' }}>
                Password
              </label>
              <div style={{ position: 'relative' }}>
                <Lock size={16} color="var(--text-muted)" style={{ position: 'absolute', left: '12px', top: '50%', transform: 'translateY(-50%)' }} />
                <input
                  type="password"
                  value={loginPassword}
                  onChange={(e) => setLoginPassword(e.target.value)}
                  className="input-field"
                  style={{ paddingLeft: '38px' }}
                  required
                />
              </div>
            </div>

            <div style={{ background: 'rgba(255,255,255,0.03)', padding: '10px 12px', borderRadius: '8px', fontSize: '0.75rem', color: 'var(--text-muted)', border: '1px solid var(--border-subtle)' }}>
              🔒 Default Demo Credentials: <strong style={{ color: '#818cf8' }}>admin</strong> / <strong style={{ color: '#818cf8' }}>AdminPassword123!</strong>
            </div>

            <button type="submit" className="btn btn-primary" style={{ padding: '12px', width: '100%', fontSize: '0.95rem' }}>
              Sign In to Mesh Gateway
            </button>
          </form>
        </div>
      </div>
    );
  }

  // --- AUTHENTICATED DASHBOARD ---
  return (
    <div style={{ minHeight: '100vh', display: 'flex', flexDirection: 'column' }}>
      <Navbar
        currentTab={currentTab}
        setCurrentTab={setCurrentTab}
        deviceStats={deviceStats}
        activeDeviceName={activeDevice?.hostname}
        adminUser={adminUser}
        onLogout={handleLogout}
        gatewayConnected={gatewayConnected}
      />

      <main style={{ maxWidth: '1600px', margin: '0 auto', width: '100%', padding: '24px', flex: 1 }}>
        {/* TAB 1: DEVICE INVENTORY */}
        {currentTab === 'devices' && (
          <DeviceGrid
            devices={devices}
            onSelectSession={handleSelectSession}
            onRevokeDevice={handleRevokeDevice}
            onOpenEnrollment={() => setIsEnrollmentOpen(true)}
          />
        )}

        {/* TAB 2: ACTIVE REMOTE SUPPORT SESSION */}
        {currentTab === 'session' && activeDevice && (
          <div style={{ display: 'flex', flexDirection: 'column', gap: '14px' }}>
            {/* Session Sub-Navbar */}
            <div className="glass-panel" style={{ padding: '8px 16px', display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '12px' }}>
              <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
                <button
                  className="btn btn-secondary btn-sm"
                  onClick={() => setCurrentTab('devices')}
                  title="Return to Device List"
                >
                  <ArrowLeft size={16} />
                  <span>Devices</span>
                </button>

                <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                  <Radio size={16} color="#38bdf8" />
                  <span style={{ fontWeight: 700, fontSize: '0.95rem' }}>{activeDevice.hostname}</span>
                  <span className={`badge ${activeDevice.os === 'windows' ? 'badge-windows' : 'badge-android'}`} style={{ fontSize: '0.7rem' }}>
                    {activeDevice.os}
                  </span>
                </div>
              </div>

              {/* Sub-tools switcher */}
              <div style={{ display: 'flex', gap: '4px', background: 'rgba(0,0,0,0.3)', padding: '2px', borderRadius: '8px', border: '1px solid var(--border-subtle)' }}>
                <button
                  className={`btn btn-sm ${sessionSubTab === 'desktop' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setSessionSubTab('desktop')}
                  style={{ border: 'none' }}
                >
                  <Monitor size={14} />
                  <span>Desktop Control</span>
                </button>

                <button
                  className={`btn btn-sm ${sessionSubTab === 'terminal' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setSessionSubTab('terminal')}
                  style={{ border: 'none' }}
                >
                  <Terminal size={14} />
                  <span>Terminal</span>
                </button>

                <button
                  className={`btn btn-sm ${sessionSubTab === 'files' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setSessionSubTab('files')}
                  style={{ border: 'none' }}
                >
                  <FolderTree size={14} />
                  <span>File Explorer</span>
                </button>

                <button
                  className={`btn btn-sm ${sessionSubTab === 'processes' ? 'btn-primary' : 'btn-secondary'}`}
                  onClick={() => setSessionSubTab('processes')}
                  style={{ border: 'none' }}
                >
                  <Sliders size={14} />
                  <span>Tasks & Services</span>
                </button>
              </div>
            </div>

            {/* Active tool content */}
            {sessionSubTab === 'desktop' && (
              <RemoteDesktop
                device={activeDevice}
                socket={sessionSocketRef.current}
                onDisconnect={handleDisconnectSession}
              />
            )}

            {sessionSubTab === 'terminal' && (
              <RemoteTerminal
                device={activeDevice}
                socket={sessionSocketRef.current}
              />
            )}

            {sessionSubTab === 'files' && (
              <FileManager
                device={activeDevice}
                socket={sessionSocketRef.current}
              />
            )}

            {sessionSubTab === 'processes' && (
              <ProcessServiceManager
                device={activeDevice}
                socket={sessionSocketRef.current}
              />
            )}
          </div>
        )}

        {/* TAB 3: ENROLLMENT (View or Modal) */}
        {currentTab === 'enrollment' && (
          <div style={{ maxWidth: '900px', margin: '0 auto' }}>
            <EnrollmentModal
              isOpen={true}
              onClose={() => setCurrentTab('devices')}
              token={authToken}
            />
          </div>
        )}

        {/* TAB 4: AUDIT LEDGER */}
        {currentTab === 'audit' && <AuditLogViewer token={authToken} />}

        {/* TAB 5: ANTIVIRUS & COMPLIANCE */}
        {currentTab === 'compliance' && <AntivirusCompliance />}
      </main>

      {/* Floating Enrollment Modal when triggered from DeviceGrid button */}
      <EnrollmentModal
        isOpen={isEnrollmentOpen}
        onClose={() => setIsEnrollmentOpen(false)}
        token={authToken}
      />
    </div>
  );
};

export default App;
