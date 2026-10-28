import React, { useState, useEffect, useRef } from 'react';
import { Terminal as TerminalIcon, Play, Trash2, RotateCcw, ShieldCheck, ChevronRight } from 'lucide-react';
import { DeviceInfo } from '../types';

interface RemoteTerminalProps {
  device: DeviceInfo;
  socket: WebSocket | null;
}

export const RemoteTerminal: React.FC<RemoteTerminalProps> = ({ device, socket }) => {
  const [outputLines, setOutputLines] = useState<Array<{ text: string; type: 'stdout' | 'stderr' | 'stdin' | 'system' }>>([
    { text: `[SYSTEM] Authenticated shell session initialized for ${device.hostname} (${device.ipAddress})`, type: 'system' },
    { text: `[SYSTEM] Operating System: ${device.os.toUpperCase()} | Terminal Security: Admin token verified`, type: 'system' },
    { text: `Type a command below and press Enter:`, type: 'system' },
  ]);
  const [currentInput, setCurrentInput] = useState('');
  const [commandHistory, setCommandHistory] = useState<string[]>([]);
  const [historyIndex, setHistoryIndex] = useState<number>(-1);
  const [shellType, setShellType] = useState<'powershell' | 'cmd' | 'bash'>('powershell');
  const scrollRef = useRef<HTMLDivElement | null>(null);

  useEffect(() => {
    if (!socket) return;

    const handleMessage = (event: MessageEvent) => {
      try {
        if (typeof event.data !== 'string') return;
        const msg = JSON.parse(event.data);

        if (msg.type === 'TERMINAL_OUTPUT' && msg.payload?.text) {
          setOutputLines((prev) => [...prev, { text: msg.payload.text, type: msg.payload.isError ? 'stderr' : 'stdout' }]);
        } else if (msg.type === 'COMMAND_EXEC_RESP') {
          const resp = msg.payload || {};
          const stdoutText = resp.stdout || resp.Stdout || '';
          const stderrText = resp.stderr || resp.Stderr || '';
          const exitCode = resp.exitCode ?? resp.ExitCode ?? 0;
          setOutputLines((prev) => [
            ...prev,
            ...(stdoutText ? [{ text: stdoutText, type: 'stdout' as const }] : []),
            ...(stderrText ? [{ text: stderrText, type: 'stderr' as const }] : []),
            { text: `[Process exited with code ${exitCode}]`, type: 'system' as const },
          ]);
        }
      } catch {
        // ignore
      }
    };

    socket.addEventListener('message', handleMessage);
    return () => socket.removeEventListener('message', handleMessage);
  }, [socket]);

  useEffect(() => {
    if (scrollRef.current) {
      scrollRef.current.scrollTop = scrollRef.current.scrollHeight;
    }
  }, [outputLines]);

  const runCommand = (cmdToRun?: string) => {
    const cmd = (cmdToRun !== undefined ? cmdToRun : currentInput).trim();
    if (!cmd) return;

    setOutputLines((prev) => [...prev, { text: `> ${cmd}`, type: 'stdin' }]);
    setCommandHistory((prev) => [cmd, ...prev]);
    setHistoryIndex(-1);
    setCurrentInput('');

    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(
        JSON.stringify({
          type: 'COMMAND_EXEC_REQ',
          payload: { command: cmd, shell: shellType },
          timestamp: Date.now(),
        })
      );
    } else {
      setOutputLines((prev) => [
        ...prev,
        { text: `[ERROR] Cannot send command: Device connection socket is closed`, type: 'stderr' },
      ]);
    }
  };

  const handleKeyDown = (e: React.KeyboardEvent<HTMLInputElement>) => {
    if (e.key === 'Enter') {
      runCommand();
    } else if (e.key === 'ArrowUp') {
      if (commandHistory.length > 0 && historyIndex < commandHistory.length - 1) {
        const newIdx = historyIndex + 1;
        setHistoryIndex(newIdx);
        setCurrentInput(commandHistory[newIdx]);
      }
    } else if (e.key === 'ArrowDown') {
      if (historyIndex > 0) {
        const newIdx = historyIndex - 1;
        setHistoryIndex(newIdx);
        setCurrentInput(commandHistory[newIdx]);
      } else if (historyIndex === 0) {
        setHistoryIndex(-1);
        setCurrentInput('');
      }
    }
  };

  const clearOutput = () => {
    setOutputLines([{ text: '[Screen cleared]', type: 'system' }]);
  };

  return (
    <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 180px)', overflow: 'hidden' }}>
      {/* Header */}
      <div style={{ padding: '12px 18px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: 'rgba(0,0,0,0.3)' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '10px' }}>
          <TerminalIcon size={18} color="#22d3ee" />
          <span style={{ fontWeight: 700, fontSize: '0.9rem' }}>Remote Terminal — {device.hostname}</span>
          <span className="badge badge-windows" style={{ fontSize: '0.7rem' }}>{shellType}</span>
        </div>

        {/* Quick Snippets & Shell selector */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <select
            value={shellType}
            onChange={(e) => setShellType(e.target.value as any)}
            className="input-field"
            style={{ padding: '4px 8px', fontSize: '0.75rem', width: 'auto' }}
          >
            <option value="powershell">PowerShell</option>
            <option value="cmd">Command Prompt</option>
            <option value="bash">Bash / Sh</option>
          </select>

          <button className="btn btn-secondary btn-sm" onClick={() => runCommand('Get-Process | Select-Object -First 10')} title="List Processes">
            Top Processes
          </button>
          <button className="btn btn-secondary btn-sm" onClick={() => runCommand('Get-Service | Where-Object Status -eq Running | Select-Object -First 10')} title="List Services">
            Active Services
          </button>
          <button className="btn btn-secondary btn-sm" onClick={() => runCommand('ipconfig /all')} title="Network Config">
            IP Config
          </button>

          <button className="btn btn-secondary btn-sm" onClick={clearOutput} title="Clear Screen">
            <Trash2 size={14} />
          </button>
        </div>
      </div>

      {/* Terminal Output Area */}
      <div
        ref={scrollRef}
        style={{
          flex: 1,
          padding: '16px',
          background: '#040711',
          fontFamily: 'var(--font-mono)',
          fontSize: '0.825rem',
          overflowY: 'auto',
          display: 'flex',
          flexDirection: 'column',
          gap: '4px',
          color: '#cbd5e1',
          lineHeight: '1.6',
        }}
      >
        {outputLines.map((line, idx) => {
          let color = '#cbd5e1';
          if (line.type === 'stdin') color = '#38bdf8';
          else if (line.type === 'stderr') color = '#f87171';
          else if (line.type === 'system') color = '#818cf8';

          return (
            <div key={idx} style={{ color, whiteSpace: 'pre-wrap', wordBreak: 'break-all' }}>
              {line.text}
            </div>
          );
        })}
      </div>

      {/* Command Input Prompt */}
      <div style={{ padding: '10px 16px', background: 'rgba(0, 0, 0, 0.4)', borderTop: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', gap: '8px' }}>
        <ChevronRight size={18} color="#38bdf8" />
        <input
          type="text"
          value={currentInput}
          onChange={(e) => setCurrentInput(e.target.value)}
          onKeyDown={handleKeyDown}
          placeholder="Enter command (e.g. dir, hostname, systeminfo, Get-ComputerInfo)..."
          className="input-field"
          style={{
            fontFamily: 'var(--font-mono)',
            fontSize: '0.85rem',
            background: 'transparent',
            border: 'none',
            padding: '4px 0',
            boxShadow: 'none',
          }}
          autoFocus
        />
        <button className="btn btn-primary btn-sm" onClick={() => runCommand()}>
          <Play size={14} />
          <span>Execute</span>
        </button>
      </div>
    </div>
  );
};
