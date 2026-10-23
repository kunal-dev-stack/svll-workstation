# SVLL IT Relay Server — Deployment Guide
> Version 1.1 | Developed by Kunal Turkar | Shree Vasu Logistics Limited

## What is this?

A lightweight REST API server that runs on your **office NAS or server PC** (the machine that is always on). It acts as the central relay for:

- **Fleet heartbeats** → Every 60 seconds, each user's SVLL Workstation sends their machine info (name, serial, MAC, IP, CPU, RAM, etc.) to this server
- **Live fleet dashboard** → Your Admin build polls this server every 30 seconds and shows all registered machines live
- **Complaint tickets** → Users submit tickets through this server; you see them in real-time in the Admin build
- **Admin replies** → You type a reply in the Admin build; it's stored here; users get a Windows toast popup when they have a reply

---

## Prerequisites

- Windows Server / NAS running Windows or any Linux box
- .NET 8.0 Runtime installed: https://dotnet.microsoft.com/download/dotnet/8.0

---

## Step 1: Build the Server

On your development machine, run:

```powershell
cd "c:\IT PROJECTS\svll-workstation\backend\SvllRelayServer"
dotnet publish -c Release -o C:\svll-relay
```

---

## Step 2: Deploy to Your Server/NAS

Copy the `C:\svll-relay\` folder to your office server.

---

## Step 3: Install as Windows Service (Recommended)

Run these commands **as Administrator** on the server machine:

```powershell
# Install as Windows Service
sc.exe create SvllRelay binpath="dotnet C:\svll-relay\SvllRelayServer.dll --urls http://0.0.0.0:5000" start=auto
sc.exe description SvllRelay "SVLL IT Workstation v8.0 - Fleet Relay Server"
sc.exe start SvllRelay
```

The server will now:
- Start automatically on system boot
- Listen on port **5000** on all network interfaces
- Be accessible from all office PCs at `http://<server-ip>:5000`

---

## Step 4: Configure the Admin Build

1. Open your **SVLL IT Workstation (Admin Edition)**
2. Go to **Hub 8 → Admin Security Config** (or wherever the relay URL setting is)
3. Enter: `http://192.168.1.10:5000` (replace with your actual server IP)
4. Click **Save & Test Connection**

The relay URL is automatically distributed to user builds via a config that's baked in at build time **OR** you can hardcode it in `BuildConstants.cs`:

```csharp
public const string DefaultRelayUrl = "http://192.168.1.10:5000";
```

---

## Step 5: Verify It's Working

Open a browser on any office PC and visit:

```
http://<server-ip>:5000/api/health
```

You should see:
```json
{
  "status": "ok",
  "server": "SVLL IT Relay Server v1.0",
  "timestamp": "2026-10-23T12:00:00Z"
}
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/health` | Health check (no auth needed) |
| `POST` | `/api/heartbeat` | Machine sends system info every 60s |
| `GET` | `/api/fleet` | Admin gets live list of all machines |
| `POST` | `/api/ticket` | User submits a complaint ticket |
| `GET` | `/api/tickets` | Admin gets all tickets |
| `PATCH` | `/api/ticket/{id}` | Admin resolves + replies |
| `GET` | `/api/ticket/{id}/reply` | User polls for admin reply |
| `DELETE` | `/api/device/{hostname}` | Remove a stale device |

All endpoints (except `/api/health`) require the header:
```
X-SVLL-Key: SVLL-RELAY-KEY-2026-KUNAL
```

---

## Firewall Rule (if Windows Firewall is active on the server)

```powershell
New-NetFirewallRule -DisplayName "SVLL Relay Server" -Direction Inbound -Protocol TCP -LocalPort 5000 -Action Allow
```

---

## Service Management

```powershell
sc.exe start SvllRelay    # Start
sc.exe stop SvllRelay     # Stop
sc.exe query SvllRelay    # Check status

# View logs
Get-EventLog -LogName Application -Source "SvllRelay" -Newest 50

# Uninstall
sc.exe stop SvllRelay
sc.exe delete SvllRelay
```

---

## Database

The server creates `svll_relay.db` (SQLite) in the working directory (`C:\svll-relay\`).

To browse the database, use [DB Browser for SQLite](https://sqlitebrowser.org/) — free tool, open `svll_relay.db`.

Tables:
- `Devices` — one row per machine, upserted on each heartbeat
- `Tickets` — all complaint tickets with admin replies

**Backup:** Simply copy `svll_relay.db` to a safe location. Schedule a nightly task if needed.
