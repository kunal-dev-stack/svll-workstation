import React, { useState, useEffect } from 'react';
import { Key, Copy, Check, X, Shield, Plus, Clock, Users, Terminal } from 'lucide-react';
import { EnrollmentToken } from '../types';

interface EnrollmentModalProps {
  isOpen: boolean;
  onClose: () => void;
  token: string;
}

export const EnrollmentModal: React.FC<EnrollmentModalProps> = ({ isOpen, onClose, token: authToken }) => {
  const [tokens, setTokens] = useState<EnrollmentToken[]>([]);
  const [group, setGroup] = useState('Corporate Workstations');
  const [hoursValid, setHoursValid] = useState(48);
  const [maxUses, setMaxUses] = useState(25);
  const [unattendedAllowed, setUnattendedAllowed] = useState(true);
  const [copiedToken, setCopiedToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (isOpen) {
      fetchTokens();
    }
  }, [isOpen]);

  const fetchTokens = async () => {
    try {
      const res = await fetch('/api/enrollment/tokens', {
        headers: { Authorization: `Bearer ${authToken}` },
      });
      if (res.ok) {
        const data = await res.json();
        setTokens(data);
      }
    } catch (err) {
      console.error('Failed to fetch tokens:', err);
    }
  };

  const handleCreateToken = async (e: React.FormEvent) => {
    e.preventDefault();
    setIsLoading(true);
    try {
      const res = await fetch('/api/enrollment/tokens', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          Authorization: `Bearer ${authToken}`,
        },
        body: JSON.stringify({ group, hoursValid, maxUses, unattendedAllowed }),
      });
      if (res.ok) {
        fetchTokens();
      }
    } catch (err) {
      console.error('Error creating token:', err);
    } finally {
      setIsLoading(false);
    }
  };

  const copyToClipboard = (text: string, id: string) => {
    navigator.clipboard.writeText(text);
    setCopiedToken(id);
    setTimeout(() => setCopiedToken(null), 2000);
  };

  if (!isOpen) return null;

  return (
    <div style={{ position: 'fixed', inset: 0, zIndex: 1000, background: 'rgba(0, 0, 0, 0.75)', backdropFilter: 'blur(8px)', display: 'flex', alignItems: 'center', justifyContent: 'center', padding: '20px' }}>
      <div className="glass-panel animate-fade-in" style={{ width: '100%', maxWidth: '800px', maxHeight: '90vh', overflowY: 'auto', padding: '28px', border: '1px solid rgba(99, 102, 241, 0.3)', boxShadow: '0 20px 50px rgba(0,0,0,0.8)' }}>
        {/* Header */}
        <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', marginBottom: '24px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '12px' }}>
            <div style={{ width: '40px', height: '40px', borderRadius: '10px', background: 'rgba(99, 102, 241, 0.2)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid rgba(99, 102, 241, 0.4)' }}>
              <Key size={22} color="#818cf8" />
            </div>
            <div>
              <h2 style={{ fontSize: '1.25rem', fontWeight: 700 }}>Device Enrollment & Token Manager</h2>
              <p style={{ fontSize: '0.8rem', color: 'var(--text-muted)' }}>Issue secure, time-bound deployment credentials for Windows and Android endpoints</p>
            </div>
          </div>
          <button className="btn btn-secondary btn-sm" onClick={onClose} style={{ padding: '6px' }}>
            <X size={18} />
          </button>
        </div>

        {/* Create Token Form */}
        <form onSubmit={handleCreateToken} style={{ background: 'rgba(0,0,0,0.3)', padding: '16px', borderRadius: '12px', border: '1px solid var(--border-subtle)', marginBottom: '24px' }}>
          <h4 style={{ fontSize: '0.9rem', marginBottom: '12px', color: '#cbd5e1', display: 'flex', alignItems: 'center', gap: '6px' }}>
            <Plus size={16} color="#6366f1" /> Create New Enrollment Token
          </h4>

          <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(180px, 1fr))', gap: '12px', marginBottom: '14px' }}>
            <div>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', marginBottom: '4px' }}>Device Group</label>
              <input
                type="text"
                value={group}
                onChange={(e) => setGroup(e.target.value)}
                className="input-field"
                required
              />
            </div>
            <div>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', marginBottom: '4px' }}>Valid For (Hours)</label>
              <input
                type="number"
                value={hoursValid}
                onChange={(e) => setHoursValid(parseInt(e.target.value, 10))}
                className="input-field"
                min={1}
                max={720}
                required
              />
            </div>
            <div>
              <label style={{ fontSize: '0.75rem', color: 'var(--text-muted)', display: 'block', marginBottom: '4px' }}>Max Uses</label>
              <input
                type="number"
                value={maxUses}
                onChange={(e) => setMaxUses(parseInt(e.target.value, 10))}
                className="input-field"
                min={1}
                max={1000}
                required
              />
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '12px' }}>
            <label style={{ display: 'flex', alignItems: 'center', gap: '8px', fontSize: '0.85rem', cursor: 'pointer' }}>
              <input
                type="checkbox"
                checked={unattendedAllowed}
                onChange={(e) => setUnattendedAllowed(e.target.checked)}
                style={{ accentColor: 'var(--accent-primary)', width: '16px', height: '16px' }}
              />
              <span>Grant pre-authorized unattended access permission</span>
            </label>

            <button type="submit" className="btn btn-primary btn-sm" disabled={isLoading}>
              <Key size={14} />
              <span>{isLoading ? 'Issuing...' : 'Generate Token'}</span>
            </button>
          </div>
        </form>

        {/* Existing Tokens */}
        <h4 style={{ fontSize: '0.9rem', marginBottom: '12px', color: '#cbd5e1' }}>Active Enrollment Credentials</h4>
        <div style={{ display: 'flex', flexDirection: 'column', gap: '10px' }}>
          {tokens.map((t) => {
            const isExpired = t.expiresAt < Date.now();
            return (
              <div key={t.token} style={{ background: 'rgba(0,0,0,0.25)', border: '1px solid var(--border-subtle)', borderRadius: '10px', padding: '14px', display: 'flex', flexDirection: 'column', gap: '8px' }}>
                <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '8px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
                    <code style={{ fontFamily: 'var(--font-mono)', fontWeight: 700, color: isExpired ? '#94a3b8' : '#38bdf8', fontSize: '0.9rem' }}>
                      {t.token}
                    </code>
                    <span className={`badge ${isExpired ? 'badge-offline' : 'badge-online'}`} style={{ fontSize: '0.65rem' }}>
                      {isExpired ? 'Expired' : 'Active'}
                    </span>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', gap: '6px', fontSize: '0.75rem', color: 'var(--text-muted)' }}>
                    <span>Used: {t.usedCount} / {t.maxUses}</span>
                    <span>•</span>
                    <span>Expires: {new Date(t.expiresAt).toLocaleDateString()}</span>
                  </div>
                </div>

                {/* Web installer and direct download actions */}
                <div style={{ display: 'flex', flexDirection: 'column', gap: '8px' }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: '8px', background: 'rgba(0,0,0,0.4)', padding: '6px 10px', borderRadius: '6px', border: '1px solid rgba(99,102,241,0.2)' }}>
                    <Terminal size={14} color="#38bdf8" />
                    <span style={{ fontFamily: 'var(--font-mono)', fontSize: '0.75rem', color: '#cbd5e1', flex: 1, overflow: 'hidden', textOverflow: 'ellipsis', whiteSpace: 'nowrap' }}>
                      irm http://{window.location.host}/api/agent/install.ps1?token={t.token} | iex
                    </span>
                    <button
                      className="btn btn-secondary btn-sm"
                      style={{ padding: '3px 8px', fontSize: '0.7rem' }}
                      onClick={() => copyToClipboard(`irm http://${window.location.host}/api/agent/install.ps1?token=${t.token} | iex`, `cmd-${t.token}`)}
                    >
                      {copiedToken === `cmd-${t.token}` ? <Check size={12} color="#34d399" /> : <Copy size={12} />}
                      <span>{copiedToken === `cmd-${t.token}` ? 'Copied' : 'Copy'}</span>
                    </button>
                  </div>

                  <div style={{ display: 'flex', alignItems: 'center', justifyContent: 'space-between', flexWrap: 'wrap', gap: '8px', paddingTop: '4px' }}>
                    <div style={{ display: 'flex', gap: '8px' }}>
                      <a
                        href={`/api/agent/install.bat?token=${t.token}`}
                        download="install.bat"
                        className="btn btn-primary btn-sm"
                        style={{ fontSize: '0.75rem', textDecoration: 'none', padding: '4px 10px' }}
                      >
                        Download 1-Click Installer (install.bat)
                      </a>
                      <a
                        href="/api/agent/windows-agent"
                        download="AegisMesh-Agent-Windows.zip"
                        className="btn btn-secondary btn-sm"
                        style={{ fontSize: '0.75rem', textDecoration: 'none', padding: '4px 10px' }}
                      >
                        Download Agent ZIP
                      </a>
                    </div>
                    <span style={{ fontSize: '0.7rem', color: 'var(--text-muted)' }}>
                      Right-click .bat & select "Run as administrator"
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      </div>
    </div>
  );
};
