import React from 'react';
import { ShieldCheck, CheckCircle2, AlertCircle, FileCode, Lock, Key, Award, ExternalLink, HelpCircle } from 'lucide-react';

export const AntivirusCompliance: React.FC = () => {
  return (
    <div style={{ display: 'flex', flexDirection: 'column', gap: '24px', maxWidth: '1200px', margin: '0 auto' }}>
      {/* Banner */}
      <div className="glass-panel" style={{ padding: '24px', borderLeft: '4px solid #10b981', display: 'flex', alignItems: 'center', justifyContent: 'space-between', gap: '20px', flexWrap: 'wrap' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '16px' }}>
          <div style={{ width: '48px', height: '48px', borderRadius: '12px', background: 'rgba(16, 185, 129, 0.15)', display: 'flex', alignItems: 'center', justifyContent: 'center', border: '1px solid rgba(16, 185, 129, 0.3)' }}>
            <ShieldCheck size={28} color="#34d399" />
          </div>
          <div>
            <h2 style={{ fontSize: '1.25rem', fontWeight: 800 }}>Endpoint Protection & Antivirus Compliance Center</h2>
            <p style={{ fontSize: '0.85rem', color: 'var(--text-secondary)' }}>
              Built for seamless coexistence with Microsoft Defender, CrowdStrike Falcon, SentinelOne, and enterprise EDRs through legitimate software design and digital signatures.
            </p>
          </div>
        </div>

        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <span className="badge badge-online" style={{ padding: '6px 12px', fontSize: '0.8rem' }}>
            <CheckCircle2 size={14} /> 100% AV-Compatible Architecture
          </span>
        </div>
      </div>

      {/* 4 Pillars Grid */}
      <div style={{ display: 'grid', gridTemplateColumns: 'repeat(auto-fit, minmax(280px, 1fr))', gap: '20px' }}>
        {/* Pillar 1 */}
        <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <FileCode size={20} color="#38bdf8" />
            <h3 style={{ fontSize: '1rem', fontWeight: 700 }}>Clean Native OS APIs</h3>
          </div>
          <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)', lineHeight: '1.6' }}>
            The Windows agent utilizes standard documented Win32 APIs (Desktop Duplication API, GDI, and <code style={{ color: '#38bdf8' }}>SendInput</code>). It strictly avoids packers (UPX), code obfuscators, memory injection, or API hooking that trigger heuristic EDR alerts.
          </p>
        </div>

        {/* Pillar 2 */}
        <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Award size={20} color="#818cf8" />
            <h3 style={{ fontSize: '1rem', fontWeight: 700 }}>Authenticode Digital Signing</h3>
          </div>
          <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)', lineHeight: '1.6' }}>
            All agent binaries and service executables contain full PE version metadata and are signed with an Authenticode certificate with SHA-256 and RFC 3161 timestamping, establishing verifiable publisher trust.
          </p>
        </div>

        {/* Pillar 3 */}
        <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <Lock size={20} color="#34d399" />
            <h3 style={{ fontSize: '1rem', fontWeight: 700 }}>Standard Service SCM</h3>
          </div>
          <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)', lineHeight: '1.6' }}>
            Persistence is managed through the legitimate Windows Service Control Manager (<code style={{ color: '#34d399' }}>services.exe</code>) under standard service accounts, without hidden run keys, scheduled task tricks, or process hollows.
          </p>
        </div>

        {/* Pillar 4 */}
        <div className="glass-panel" style={{ padding: '20px', display: 'flex', flexDirection: 'column', gap: '10px' }}>
          <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
            <AlertCircle size={20} color="#f59e0b" />
            <h3 style={{ fontSize: '1rem', fontWeight: 700 }}>User Consent & Transparency</h3>
          </div>
          <p style={{ fontSize: '0.825rem', color: 'var(--text-secondary)', lineHeight: '1.6' }}>
            All interactive support sessions broadcast an on-screen toast and persistent taskbar indicator. Android requires the system-standard <code style={{ color: '#f59e0b' }}>MediaProjection</code> permission prompt and foreground notification.
          </p>
        </div>
      </div>

      {/* Defender Whitelisting & False Positive Submission Guide */}
      <div className="glass-panel" style={{ padding: '24px' }}>
        <h3 style={{ fontSize: '1.1rem', fontWeight: 700, marginBottom: '14px', display: 'flex', alignItems: 'center', gap: '8px' }}>
          <HelpCircle size={20} color="#38bdf8" />
          Enterprise Deployment & Microsoft Defender Submission Checklist
        </h3>

        <div style={{ display: 'flex', flexDirection: 'column', gap: '12px', fontSize: '0.875rem', color: '#cbd5e1' }}>
          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
            <span style={{ color: '#34d399', fontWeight: 700 }}>✓</span>
            <div>
              <strong>Digital Certificate Enrollment:</strong> Ensure your corporate code-signing certificate is installed in the endpoint's <em>Trusted Publishers</em> or <em>Trusted Root Certification Authorities</em> store via Group Policy (GPO) or Intune.
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
            <span style={{ color: '#34d399', fontWeight: 700 }}>✓</span>
            <div>
              <strong>Microsoft Defender Security Intelligence Submission:</strong> If deploying across thousands of endpoints with a new certificate, submit the signed installer to the Microsoft Security Intelligence portal (<a href="https://www.microsoft.com/en-us/wdsi/filesubmission" target="_blank" rel="noreferrer" style={{ color: '#38bdf8' }}>microsoft.com/wdsi/filesubmission</a>) as a "Software Developer" to rapidly build SmartScreen reputation.
            </div>
          </div>

          <div style={{ display: 'flex', alignItems: 'flex-start', gap: '10px' }}>
            <span style={{ color: '#34d399', fontWeight: 700 }}>✓</span>
            <div>
              <strong>Clean Removal Guarantee:</strong> The provided PowerShell script <code style={{ color: '#a855f7' }}>scripts/uninstall-agent.ps1</code> completely stops the Windows service, deletes the registration from SCM, unregisters device tokens, and purges working binaries.
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};
