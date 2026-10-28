import React, { useState, useEffect } from 'react';
import {
  Folder,
  File,
  ArrowUp,
  Download,
  Upload,
  Trash2,
  RefreshCw,
  HardDrive,
  FileText,
  FileCode,
  Image as ImageIcon,
  FolderPlus,
} from 'lucide-react';
import { DeviceInfo, FileItem } from '../types';

interface FileManagerProps {
  device: DeviceInfo;
  socket: WebSocket | null;
}

export const FileManager: React.FC<FileManagerProps> = ({ device, socket }) => {
  const [currentPath, setCurrentPath] = useState<string>(device.os === 'windows' ? 'C:\\' : '/sdcard');
  const [files, setFiles] = useState<FileItem[]>([
    { name: 'Windows', path: 'C:\\Windows', isDirectory: true, sizeBytes: 0, modifiedAt: Date.now() - 36000000 },
    { name: 'Program Files', path: 'C:\\Program Files', isDirectory: true, sizeBytes: 0, modifiedAt: Date.now() - 72000000 },
    { name: 'Users', path: 'C:\\Users', isDirectory: true, sizeBytes: 0, modifiedAt: Date.now() - 10000000 },
    { name: 'AegisMesh-Agent.log', path: 'C:\\AegisMesh-Agent.log', isDirectory: false, sizeBytes: 42800, modifiedAt: Date.now() - 500000 },
    { name: 'config.json', path: 'C:\\config.json', isDirectory: false, sizeBytes: 1540, modifiedAt: Date.now() - 900000 },
  ]);
  const [isLoading, setIsLoading] = useState(false);

  useEffect(() => {
    if (!socket) return;

    const handleMessage = (event: MessageEvent) => {
      try {
        if (typeof event.data !== 'string') return;
        const msg = JSON.parse(event.data);

        if (msg.type === 'FILE_LIST_RESP') {
          setIsLoading(false);
          const rawFiles = msg.payload?.files || msg.payload?.Files;
          if (rawFiles && Array.isArray(rawFiles)) {
            const normalized = rawFiles.map((f: any) => ({
              name: f.name || f.Name || 'Unnamed',
              path: f.path || f.Path || '',
              isDirectory: f.isDirectory ?? f.IsDirectory ?? false,
              sizeBytes: f.sizeBytes ?? f.SizeBytes ?? 0,
              modifiedAt: f.modifiedAt ?? f.ModifiedAt ?? Date.now(),
            }));
            setFiles(normalized);
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
      requestDirectory(currentPath);
    } else if (socket) {
      socket.addEventListener('open', () => requestDirectory(currentPath), { once: true });
    }
  }, [socket]);

  const requestDirectory = (path: string) => {
    setCurrentPath(path);
    setIsLoading(true);

    if (socket && socket.readyState === WebSocket.OPEN) {
      socket.send(
        JSON.stringify({
          type: 'FILE_LIST_REQ',
          payload: { path },
          timestamp: Date.now(),
        })
      );
    } else {
      setTimeout(() => setIsLoading(false), 500);
    }
  };

  const handleNavigateUp = () => {
    if (device.os === 'windows') {
      const parts = currentPath.replace(/\\$/, '').split('\\');
      if (parts.length > 1) {
        parts.pop();
        const newPath = parts.join('\\') + (parts.length === 1 ? '\\' : '');
        requestDirectory(newPath);
      }
    } else {
      const parts = currentPath.replace(/\/$/, '').split('/');
      if (parts.length > 1) {
        parts.pop();
        requestDirectory(parts.join('/') || '/');
      }
    }
  };

  const formatSize = (bytes: number) => {
    if (bytes === 0) return '—';
    if (bytes < 1024) return `${bytes} B`;
    if (bytes < 1024 * 1024) return `${(bytes / 1024).toFixed(1)} KB`;
    if (bytes < 1024 * 1024 * 1024) return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
    return `${(bytes / (1024 * 1024 * 1024)).toFixed(2)} GB`;
  };

  const getFileIcon = (file: FileItem) => {
    if (file.isDirectory) return <Folder size={18} color="#f59e0b" />;
    const ext = file.name.split('.').pop()?.toLowerCase();
    if (['jpg', 'png', 'jpeg', 'webp'].includes(ext || '')) return <ImageIcon size={18} color="#38bdf8" />;
    if (['json', 'js', 'ts', 'cs', 'xml'].includes(ext || '')) return <FileCode size={18} color="#a855f7" />;
    return <FileText size={18} color="#94a3b8" />;
  };

  return (
    <div className="glass-panel" style={{ display: 'flex', flexDirection: 'column', height: 'calc(100vh - 180px)', overflow: 'hidden' }}>
      {/* Path Toolbar */}
      <div style={{ padding: '12px 18px', borderBottom: '1px solid var(--border-subtle)', display: 'flex', alignItems: 'center', justifyContent: 'space-between', background: 'rgba(0,0,0,0.3)', gap: '12px' }}>
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px', flex: 1 }}>
          <button className="btn btn-secondary btn-sm" onClick={handleNavigateUp} title="Up Directory">
            <ArrowUp size={16} />
          </button>
          <div style={{ display: 'flex', alignItems: 'center', gap: '6px', background: 'var(--bg-input)', padding: '6px 12px', borderRadius: '8px', border: '1px solid var(--border-subtle)', flex: 1, maxWidth: '600px' }}>
            <HardDrive size={16} color="var(--text-muted)" />
            <input
              type="text"
              value={currentPath}
              onChange={(e) => setCurrentPath(e.target.value)}
              onKeyDown={(e) => e.key === 'Enter' && requestDirectory(currentPath)}
              style={{ background: 'transparent', border: 'none', color: '#ffffff', width: '100%', outline: 'none', fontFamily: 'var(--font-mono)', fontSize: '0.85rem' }}
            />
          </div>
          <button className="btn btn-secondary btn-sm" onClick={() => requestDirectory(currentPath)} title="Refresh">
            <RefreshCw size={14} className={isLoading ? 'animate-spin' : ''} />
          </button>
        </div>

        {/* Action buttons */}
        <div style={{ display: 'flex', alignItems: 'center', gap: '8px' }}>
          <button className="btn btn-primary btn-sm" onClick={() => alert('File upload dialog opened')}>
            <Upload size={14} />
            <span>Upload File</span>
          </button>
        </div>
      </div>

      {/* File List Table */}
      <div style={{ flex: 1, overflowY: 'auto', padding: '12px' }}>
        <table style={{ width: '100%', borderCollapse: 'collapse', fontSize: '0.875rem' }}>
          <thead>
            <tr style={{ borderBottom: '1px solid var(--border-subtle)', color: 'var(--text-muted)', textAlign: 'left', fontSize: '0.75rem', textTransform: 'uppercase' }}>
              <th style={{ padding: '8px 12px' }}>Name</th>
              <th style={{ padding: '8px 12px', width: '120px' }}>Size</th>
              <th style={{ padding: '8px 12px', width: '180px' }}>Last Modified</th>
              <th style={{ padding: '8px 12px', width: '100px', textAlign: 'right' }}>Actions</th>
            </tr>
          </thead>
          <tbody>
            {files.map((file, idx) => (
              <tr
                key={idx}
                onDoubleClick={() => file.isDirectory && requestDirectory(file.path)}
                style={{
                  borderBottom: '1px solid rgba(255,255,255,0.03)',
                  cursor: file.isDirectory ? 'pointer' : 'default',
                  transition: 'background var(--transition-fast)',
                }}
                onMouseEnter={(e) => (e.currentTarget.style.backgroundColor = 'rgba(255, 255, 255, 0.04)')}
                onMouseLeave={(e) => (e.currentTarget.style.backgroundColor = 'transparent')}
              >
                <td style={{ padding: '10px 12px', display: 'flex', alignItems: 'center', gap: '10px' }}>
                  {getFileIcon(file)}
                  <span style={{ fontWeight: file.isDirectory ? 600 : 400, color: file.isDirectory ? '#f8fafc' : 'var(--text-secondary)' }}>
                    {file.name}
                  </span>
                </td>
                <td style={{ padding: '10px 12px', color: 'var(--text-muted)', fontFamily: 'var(--font-mono)', fontSize: '0.8rem' }}>
                  {formatSize(file.sizeBytes)}
                </td>
                <td style={{ padding: '10px 12px', color: 'var(--text-muted)', fontSize: '0.8rem' }}>
                  {new Date(file.modifiedAt).toLocaleString()}
                </td>
                <td style={{ padding: '10px 12px', textAlign: 'right' }}>
                  <div style={{ display: 'flex', justifyContent: 'flex-end', gap: '6px' }}>
                    {!file.isDirectory && (
                      <button
                        className="btn btn-secondary btn-sm"
                        style={{ padding: '4px 6px' }}
                        title="Download File"
                        onClick={() => alert(`Initiated authorized download of ${file.name}`)}
                      >
                        <Download size={13} color="#38bdf8" />
                      </button>
                    )}
                    <button
                      className="btn btn-secondary btn-sm"
                      style={{ padding: '4px 6px' }}
                      title="Delete File"
                      onClick={() => alert(`Audit log recorded: Request to delete ${file.name}`)}
                    >
                      <Trash2 size={13} color="#fb7185" />
                    </button>
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};
