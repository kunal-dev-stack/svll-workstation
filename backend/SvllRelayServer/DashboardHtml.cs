namespace SvllRelayServer;

public static class DashboardHtml
{
    public static string Render()
    {
        return """
<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>SVLL IT Relay Panel - Fleet Management</title>
    <style>
        :root {
            --bg-base: #0b1120;
            --bg-card: #131c31;
            --bg-card-hover: #1b2640;
            --border: #243252;
            --text-main: #f8fafc;
            --text-muted: #94a3b8;
            --svll-blue: #2563eb;
            --svll-blue-glow: rgba(37, 99, 235, 0.3);
            --accent-cyan: #06b6d4;
            --status-green: #10b981;
            --status-red: #ef4444;
            --status-amber: #f59e0b;
        }
        * { box-sizing: border-box; margin: 0; padding: 0; }
        body {
            font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, Helvetica, Arial, sans-serif;
            background-color: var(--bg-base);
            color: var(--text-main);
            min-height: 100vh;
            padding: 24px;
        }
        .header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            padding-bottom: 20px;
            border-bottom: 1px solid var(--border);
            margin-bottom: 24px;
            flex-wrap: wrap;
            gap: 16px;
        }
        .brand {
            display: flex;
            align-items: center;
            gap: 12px;
        }
        .brand-badge {
            background: linear-gradient(135deg, #1e3a8a, #2563eb);
            color: white;
            font-weight: 800;
            font-size: 14px;
            padding: 8px 14px;
            border-radius: 8px;
            letter-spacing: 1px;
            box-shadow: 0 4px 12px var(--svll-blue-glow);
        }
        .brand-title h1 {
            font-size: 20px;
            font-weight: 700;
            letter-spacing: -0.5px;
        }
        .brand-title p {
            font-size: 12px;
            color: var(--text-muted);
        }
        .header-meta {
            display: flex;
            align-items: center;
            gap: 16px;
        }
        .live-pill {
            display: inline-flex;
            align-items: center;
            gap: 6px;
            background: rgba(16, 185, 129, 0.15);
            color: var(--status-green);
            padding: 6px 12px;
            border-radius: 20px;
            font-size: 12px;
            font-weight: 600;
            border: 1px solid rgba(16, 185, 129, 0.3);
        }
        .pulse {
            width: 8px;
            height: 8px;
            background-color: var(--status-green);
            border-radius: 50%;
            animation: pulse-animation 2s infinite;
        }
        @keyframes pulse-animation {
            0% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0.7); }
            70% { transform: scale(1); box-shadow: 0 0 0 6px rgba(16, 185, 129, 0); }
            100% { transform: scale(0.95); box-shadow: 0 0 0 0 rgba(16, 185, 129, 0); }
        }

        /* Stats Grid */
        .stats-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(220px, 1fr));
            gap: 16px;
            margin-bottom: 24px;
        }
        .stat-card {
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 12px;
            padding: 16px 20px;
            display: flex;
            flex-direction: column;
            gap: 6px;
        }
        .stat-label {
            font-size: 12px;
            color: var(--text-muted);
            text-transform: uppercase;
            letter-spacing: 0.5px;
            font-weight: 600;
        }
        .stat-value {
            font-size: 28px;
            font-weight: 800;
            color: var(--text-main);
        }

        /* Quick Install Strip */
        .install-box {
            background: linear-gradient(135deg, rgba(37,99,235,0.1), rgba(6,182,212,0.1));
            border: 1px solid rgba(37,99,235,0.3);
            border-radius: 12px;
            padding: 16px 20px;
            margin-bottom: 24px;
            display: flex;
            justify-content: space-between;
            align-items: center;
            flex-wrap: wrap;
            gap: 16px;
        }
        .install-info h3 {
            font-size: 14px;
            font-weight: 700;
            margin-bottom: 4px;
            color: #60a5fa;
        }
        .install-info p {
            font-size: 12px;
            color: var(--text-muted);
        }
        .code-snippet {
            display: flex;
            align-items: center;
            background: #090e1a;
            border: 1px solid #1e293b;
            border-radius: 8px;
            padding: 8px 12px;
            font-family: monospace;
            font-size: 13px;
            color: #38bdf8;
            gap: 12px;
        }
        .copy-btn {
            background: #1e293b;
            color: var(--text-main);
            border: 1px solid #334155;
            padding: 4px 10px;
            border-radius: 6px;
            cursor: pointer;
            font-size: 11px;
            font-weight: 600;
            transition: all 0.2s;
        }
        .copy-btn:hover {
            background: var(--svll-blue);
            border-color: var(--svll-blue);
        }

        /* Main Section */
        .section-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 14px;
            flex-wrap: wrap;
            gap: 12px;
        }
        .section-title {
            font-size: 16px;
            font-weight: 700;
            display: flex;
            align-items: center;
            gap: 8px;
        }
        .filter-input {
            background: var(--bg-card);
            border: 1px solid var(--border);
            color: var(--text-main);
            padding: 8px 14px;
            border-radius: 8px;
            font-size: 13px;
            width: 260px;
            outline: none;
        }
        .filter-input:focus {
            border-color: var(--svll-blue);
            box-shadow: 0 0 0 2px var(--svll-blue-glow);
        }

        /* Table */
        .table-wrap {
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 12px;
            overflow-x: auto;
            margin-bottom: 32px;
        }
        table {
            width: 100%;
            border-collapse: collapse;
            font-size: 13px;
            text-align: left;
        }
        th {
            background: #0d1527;
            padding: 12px 16px;
            color: var(--text-muted);
            font-weight: 600;
            font-size: 11px;
            text-transform: uppercase;
            letter-spacing: 0.5px;
            border-bottom: 1px solid var(--border);
        }
        td {
            padding: 14px 16px;
            border-bottom: 1px solid var(--border);
            vertical-align: middle;
        }
        tr:last-child td {
            border-bottom: none;
        }
        tr:hover td {
            background: var(--bg-card-hover);
        }
        .badge {
            display: inline-flex;
            align-items: center;
            gap: 4px;
            padding: 4px 8px;
            border-radius: 6px;
            font-size: 11px;
            font-weight: 600;
        }
        .badge-online { background: rgba(16, 185, 129, 0.15); color: #34d399; }
        .badge-offline { background: rgba(148, 163, 184, 0.1); color: #94a3b8; }
        .badge-alert { background: rgba(245, 158, 11, 0.15); color: #fbbf24; }
        .progress-mini {
            width: 70px;
            height: 6px;
            background: #1e293b;
            border-radius: 3px;
            overflow: hidden;
            display: inline-block;
            margin-right: 6px;
            vertical-align: middle;
        }
        .progress-bar {
            height: 100%;
            border-radius: 3px;
        }
        .anydesk-link {
            color: #60a5fa;
            text-decoration: none;
            font-family: monospace;
            font-weight: 600;
        }
        .anydesk-link:hover {
            text-decoration: underline;
        }

        /* Modal */
        .modal-overlay {
            position: fixed;
            top: 0; left: 0; right: 0; bottom: 0;
            background: rgba(0,0,0,0.7);
            display: none;
            justify-content: center;
            align-items: center;
            z-index: 100;
        }
        .modal {
            background: var(--bg-card);
            border: 1px solid var(--border);
            border-radius: 12px;
            width: 500px;
            max-width: 90%;
            padding: 24px;
            box-shadow: 0 20px 40px rgba(0,0,0,0.5);
        }
        .modal h2 { font-size: 16px; margin-bottom: 12px; }
        .modal textarea {
            width: 100%;
            height: 100px;
            background: #0b1120;
            border: 1px solid var(--border);
            color: var(--text-main);
            padding: 10px;
            border-radius: 8px;
            font-size: 13px;
            margin-bottom: 16px;
            outline: none;
            resize: vertical;
        }
        .modal-actions {
            display: flex;
            justify-content: flex-end;
            gap: 10px;
        }
        .btn-cancel {
            background: #1e293b;
            color: var(--text-muted);
            border: none;
            padding: 8px 16px;
            border-radius: 6px;
            cursor: pointer;
        }
        .btn-submit {
            background: var(--svll-blue);
            color: white;
            border: none;
            padding: 8px 16px;
            border-radius: 6px;
            font-weight: 600;
            cursor: pointer;
        }
    </style>
</head>
<body>
    <div class="header">
        <div class="brand">
            <div class="brand-badge">SVLL IT</div>
            <div class="brand-title">
                <h1>Fleet Relay & Telemetry Server</h1>
                <p>Version 1.1 &bull; Central Control Hub for Shree Vasu Logistics Workstations</p>
            </div>
        </div>
        <div class="header-meta">
            <div class="live-pill">
                <div class="pulse"></div>
                <span>RELAY ACTIVE</span>
            </div>
            <div id="clock" style="font-size: 13px; color: var(--text-muted); font-family: monospace;"></div>
        </div>
    </div>

    <!-- Quick Deployment Box -->
    <div style="display: grid; grid-template-columns: repeat(auto-fit, minmax(400px, 1fr)); gap: 16px; margin-bottom: 24px;">
        <div class="install-box" style="margin-bottom: 0;">
            <div class="install-info">
                <h3>⚡ User Workstation One-Liner (User v8.0)</h3>
                <p>Deploy on employee PCs (auto-reflects & installs MeshAgent):</p>
            </div>
            <div class="code-snippet">
                <span id="clientCmd">irm https://tinyurl.com/svll-tool | iex</span>
                <button class="copy-btn" onclick="copyText('clientCmd')">Copy</button>
            </div>
        </div>
        <div class="install-box" style="margin-bottom: 0;">
            <div class="install-info">
                <h3>👑 Admin Console One-Liner (Admin v8.0)</h3>
                <p>Deploy on your Admin PC for fleet control & remote management:</p>
            </div>
            <div class="code-snippet">
                <span id="adminCmd">irm https://tinyurl.com/svll-admin | iex</span>
                <button class="copy-btn" onclick="copyText('adminCmd')">Copy</button>
            </div>
        </div>
    </div>

    <!-- Stats Grid -->
    <div class="stats-grid">
        <div class="stat-card">
            <div class="stat-label">Total Workstations</div>
            <div class="stat-value" id="totalCount">0</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Online Now</div>
            <div class="stat-value" style="color: var(--status-green);" id="onlineCount">0</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Attention / Support</div>
            <div class="stat-value" style="color: var(--status-amber);" id="alertCount">0</div>
        </div>
        <div class="stat-card">
            <div class="stat-label">Open Tickets</div>
            <div class="stat-value" style="color: var(--accent-cyan);" id="ticketCount">0</div>
        </div>
    </div>

    <!-- Fleet Table -->
    <div class="section-header">
        <div class="section-title">
            <span>🖥️ Connected Workstations</span>
            <span style="font-size: 12px; color: var(--text-muted); font-weight: normal;" id="lastRefresh">Auto-updating every 4s</span>
        </div>
        <input type="text" id="filterInput" class="filter-input" placeholder="Search hostname, IP, user, serial..." oninput="renderFleet()">
    </div>

    <div class="table-wrap">
        <table>
            <thead>
                <tr>
                    <th>Hostname</th>
                    <th>Status</th>
                    <th>User / Branch</th>
                    <th>IP &amp; MAC Address</th>
                    <th>Serial Number</th>
                    <th>OS &amp; Build</th>
                    <th>CPU / RAM</th>
                    <th>⚡ Ultra Remote / Access</th>
                    <th>Last Heartbeat</th>
                </tr>
            </thead>
            <tbody id="fleetTableBody">
                <tr><td colspan="9" style="text-align:center; color:var(--text-muted); padding:30px;">Loading fleet telemetry...</td></tr>
            </tbody>
        </table>
    </div>

    <!-- Tickets Section -->
    <div class="section-header">
        <div class="section-title">
            <span>🎫 Live Complaint &amp; Support Tickets</span>
        </div>
    </div>

    <div class="table-wrap">
        <table>
            <thead>
                <tr>
                    <th>Ticket ID</th>
                    <th>Submitter</th>
                    <th>Issue Title</th>
                    <th>Priority</th>
                    <th>Status</th>
                    <th>Admin Reply</th>
                    <th>Action</th>
                </tr>
            </thead>
            <tbody id="ticketsTableBody">
                <tr><td colspan="7" style="text-align:center; color:var(--text-muted); padding:20px;">No open tickets</td></tr>
            </tbody>
        </table>
    </div>

    <!-- Reply Modal -->
    <div class="modal-overlay" id="replyModal">
        <div class="modal">
            <h2 id="modalTicketTitle">Reply to Ticket</h2>
            <p style="font-size: 12px; color: var(--text-muted); margin-bottom: 12px;" id="modalTicketMeta"></p>
            <textarea id="replyText" placeholder="Type resolution or instructions for the employee..."></textarea>
            <div class="modal-actions">
                <button class="btn-cancel" onclick="closeModal()">Cancel</button>
                <button class="btn-submit" onclick="submitReply()">Send Reply &amp; Resolve</button>
            </div>
        </div>
    </div>

    <script>
        let devicesData = [];
        let ticketsData = [];
        let currentTicketId = null;

        function updateClock() {
            const now = new Date();
            document.getElementById('clock').innerText = now.toLocaleTimeString() + ' | ' + now.toLocaleDateString();
        }
        setInterval(updateClock, 1000);
        updateClock();

        function copyText(elementId) {
            const text = document.getElementById(elementId).innerText;
            navigator.clipboard.writeText(text).then(() => {
                alert('Copied command: ' + text);
            });
        }

        async function fetchData() {
            try {
                // Fetch fleet
                const fRes = await fetch('/panel/api/fleet');
                if (fRes.ok) {
                    devicesData = await fRes.json();
                    renderFleet();
                }

                // Fetch tickets
                const tRes = await fetch('/panel/api/tickets');
                if (tRes.ok) {
                    ticketsData = await tRes.json();
                    renderTickets();
                }

                document.getElementById('lastRefresh').innerText = 'Last updated: ' + new Date().toLocaleTimeString();
            } catch (e) {
                console.error(e);
            }
        }

        function renderFleet() {
            const query = (document.getElementById('filterInput').value || '').toLowerCase();
            const tbody = document.getElementById('fleetTableBody');

            let total = devicesData.length;
            let online = 0;
            let alert = 0;

            const filtered = devicesData.filter(d => {
                const isOnline = d.status === 'Online';
                const isAlert = d.status === 'SupportRequested';
                if (isOnline) online++;
                if (isAlert) alert++;

                const fullStr = `${d.hostname} ${d.loggedInUser} ${d.branchLocation} ${d.ipAddress} ${d.macAddress} ${d.serialNumber}`.toLowerCase();
                return fullStr.includes(query);
            });

            document.getElementById('totalCount').innerText = total;
            document.getElementById('onlineCount').innerText = online;
            document.getElementById('alertCount').innerText = alert;

            if (filtered.length === 0) {
                tbody.innerHTML = '<tr><td colspan="9" style="text-align:center; color:var(--text-muted); padding:30px;">No workstations match filter.</td></tr>';
                return;
            }

            tbody.innerHTML = filtered.map(d => {
                const isOnline = d.status === 'Online';
                const isAlert = d.status === 'SupportRequested';
                let statusBadge = '<span class="badge badge-offline">Offline</span>';
                if (isAlert) {
                    statusBadge = '<span class="badge badge-alert">⚠️ Support Req</span>';
                } else if (isOnline) {
                    statusBadge = '<span class="badge badge-online">🟢 Online</span>';
                }

                const cpuBarColor = d.cpuLoadPercent > 80 ? 'var(--status-red)' : d.cpuLoadPercent > 50 ? 'var(--status-amber)' : 'var(--accent-cyan)';
                const ramBarColor = d.ramUsagePercent > 85 ? 'var(--status-red)' : 'var(--svll-blue)';

                const meshBadge = d.meshAgentRunning
                    ? '<span style="color:var(--status-green); font-size:11px;">✓ Mesh Active</span>'
                    : '<span style="color:var(--text-muted); font-size:11px;">Inactive</span>';

                const anydesk = d.anyDeskId
                    ? `<a href="anydesk:${d.anyDeskId}" class="anydesk-link">${d.anyDeskId}</a>`
                    : '<span style="color:var(--text-muted);">-</span>';

                let ultraRemoteHtml = '';
                if (d.remoteId) {
                    const cleanId = d.remoteId.replace(/ /g, '');
                    ultraRemoteHtml = `<div><strong style="color:#34d399;">⚡ Ultra:</strong> <code style="color:#6ee7b7; cursor:pointer;" onclick="navigator.clipboard.writeText('${cleanId}');alert('Copied Ultra ID: ${d.remoteId}');" title="Click to copy Ultra ID">${d.remoteId}</code></div><div style="font-size:10px; color:#94a3b8;">🔑 <code>${d.remotePassword || 'SVLL@Remote2026'}</code></div>`;
                } else if (isOnline) {
                    ultraRemoteHtml = `<div style="font-size:10px; color:#fbbf24; font-style:italic;">⚡ Initializing...</div>`;
                }

                return `
                    <tr>
                        <td><strong>${d.hostname}</strong></td>
                        <td>${statusBadge}</td>
                        <td>${d.loggedInUser || '-'}<br><small style="color:var(--text-muted);">${d.branchLocation || 'HQ'}</small></td>
                        <td><code>${d.ipAddress || '-'}</code><br><small style="color:var(--text-muted);">${d.macAddress || '-'}</small></td>
                        <td><code>${d.serialNumber || '-'}</code></td>
                        <td>${d.osVersion || 'Windows'}<br><small style="color:var(--text-muted);">${d.osBuild || ''}</small></td>
                        <td>
                            <div><div class="progress-mini"><div class="progress-bar" style="width:${d.cpuLoadPercent}%; background:${cpuBarColor};"></div></div><small>CPU ${d.cpuLoadPercent}%</small></div>
                            <div><div class="progress-mini"><div class="progress-bar" style="width:${d.ramUsagePercent}%; background:${ramBarColor};"></div></div><small>RAM ${d.ramUsagePercent}%</small></div>
                        </td>
                        <td>${ultraRemoteHtml}${anydesk !== '<span style="color:var(--text-muted);">-</span>' ? '<div style="font-size:10px; margin-top:2px;">AnyDesk: ' + anydesk + '</div>' : ''}${meshBadge}</td>
                        <td style="color:var(--text-muted); font-size:12px;">${formatDate(d.lastSeen)}</td>
                    </tr>
                `;
            }).join('');
        }

        function renderTickets() {
            const tbody = document.getElementById('ticketsTableBody');
            document.getElementById('ticketCount').innerText = ticketsData.filter(t => t.status !== 'Resolved').length;

            if (ticketsData.length === 0) {
                tbody.innerHTML = '<tr><td colspan="7" style="text-align:center; color:var(--text-muted); padding:20px;">No tickets submitted.</td></tr>';
                return;
            }

            tbody.innerHTML = ticketsData.map(t => {
                const isResolved = t.status === 'Resolved';
                const statusBadge = isResolved
                    ? '<span class="badge badge-online">Resolved</span>'
                    : '<span class="badge badge-alert">Pending</span>';

                const replyDisplay = t.adminReply
                    ? `<span style="color:#38bdf8;">${t.adminReply}</span>`
                    : '<span style="color:var(--text-muted); font-style:italic;">No reply yet</span>';

                return `
                    <tr>
                        <td><code>${t.ticketId || t.cloudTicketId}</code></td>
                        <td><strong>${t.createdBy || 'User'}</strong><br><small style="color:var(--text-muted);">${t.hostname || ''}</small></td>
                        <td>${t.title}</td>
                        <td>${t.priority || 'Normal'}</td>
                        <td>${statusBadge}</td>
                        <td>${replyDisplay}</td>
                        <td>
                            <button class="copy-btn" onclick="openReplyModal('${t.cloudTicketId || t.ticketId}', '${t.title}', '${t.createdBy}')">Reply</button>
                        </td>
                    </tr>
                `;
            }).join('');
        }

        function openReplyModal(id, title, user) {
            currentTicketId = id;
            document.getElementById('modalTicketTitle').innerText = 'Reply to ' + user;
            document.getElementById('modalTicketMeta').innerText = 'Ticket: ' + title + ' (' + id + ')';
            document.getElementById('replyText').value = '';
            document.getElementById('replyModal').style.display = 'flex';
        }

        function closeModal() {
            document.getElementById('replyModal').style.display = 'none';
        }

        async function submitReply() {
            const reply = document.getElementById('replyText').value.trim();
            if (!reply) return;

            try {
                const res = await fetch('/panel/api/ticket/reply', {
                    method: 'POST',
                    headers: { 'Content-Type': 'application/json' },
                    body: JSON.stringify({
                        cloudTicketId: currentTicketId,
                        adminReply: reply,
                        status: 'Resolved'
                    })
                });
                if (res.ok) {
                    closeModal();
                    fetchData();
                } else {
                    alert('Failed to send reply');
                }
            } catch (e) {
                alert('Error sending reply: ' + e);
            }
        }

        function formatDate(isoStr) {
            if (!isoStr) return '-';
            try {
                const d = new Date(isoStr);
                const diffMs = new Date() - d;
                const diffMins = Math.floor(diffMs / 60000);
                if (diffMins < 1) return 'Just now';
                if (diffMins === 1) return '1 min ago';
                if (diffMins < 60) return diffMins + ' mins ago';
                return d.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
            } catch {
                return isoStr;
            }
        }

        setInterval(fetchData, 4000);
        fetchData();
    </script>
</body>
</html>
""";
    }
}
