# SVLL Workstation / AegisMesh Enterprise Deployment Guide
## Windows Server Hosting & Direct Web-Based Agent Installation

---

### Overview

This guide details how to host the **SVLL Workstation / AegisMesh Remote Device Management Platform** on your Windows Server, connect target client workstations across the office network, and install the workstation agent directly from any client computer's web browser so devices automatically reflect in your Administrator Console.

---

### Server System Network Information (This Server)

- **Primary IP Address**: `192.168.10.11`
- **Secondary IP Address**: `192.168.100.25`
- **Subnet Mask**: `255.255.254.0`
- **Gateway**: `192.168.100.1`
- **Unified Service Port**: `5000` (Serves Admin Web Console, WebSocket Gateway & Agent Downloads)

> [!NOTE]
> **Why Port 5000 Instead of Port 3000?**
> This server already hosts the `shreevasu-itsm` Helpdesk on port 3000 (`127.0.0.1:3000`), and Windows Firewall contains an active block rule for external port 3000 traffic. 
> To eliminate port conflicts and firewall blocks, the AegisMesh server now **serves both the Admin Web Console and the Agent Gateway on Port 5000**, which is already allowed in Windows Firewall!

---

### Step 1: Server Status & Startup (Already Configured on This Machine)

The backend server is running 24/7 as a managed background process under **PM2**:

```powershell
# Check server status
pm2 list

# View live server logs
pm2 logs AegisMesh-Server

# Restart server if needed
pm2 restart AegisMesh-Server
```

Windows Firewall has port 5000 opened and active:
```powershell
New-NetFirewallRule -DisplayName "AegisMesh Gateway (Port 5000)" -Direction Inbound -LocalPort 5000 -Protocol TCP -Action Allow
```

---

### Step 2: Accessing the Admin Console

From this server or **from any computer on your office network**, open Google Chrome, Microsoft Edge, or Firefox and go to:

👉 **`http://192.168.10.11:5000`**  
*(or `http://192.168.100.25:5000`)*

**Default Login Credentials:**
- **Username**: `admin`
- **Password**: `AdminPassword123!`

---

### Step 3: Installing the Agent on Target Workstations (SVLL Tool)

You have three convenient ways to install the agent onto target employee or client computers across the network:

#### Method 1: One-Click Batch Installer (`install.bat` - Fastest & Easiest for Users)
1. On the client computer, open the browser and visit:
   `http://192.168.10.11:5000/api/agent/install.bat`
2. Save the downloaded **`install.bat`** file.
3. Right-click **`install.bat`** and click **"Run as administrator"**.
4. The script automatically:
   - Connects to `192.168.10.11:5000`.
   - Downloads and unpacks the agent package to `C:\Program Files\AegisMesh`.
   - Registers and starts the Windows Service (`AegisMeshAgent`).
   - The device **instantly appears online** on your Admin Console!

---

#### Method 2: One-Line PowerShell Command (For IT Administrators)
On the client computer, open PowerShell as Administrator and run:
```powershell
irm http://192.168.10.11:5000/api/agent/install.ps1 | iex
```
*(The script automatically resolves the server IP, downloads the agent, and starts the service.)*

---

#### Method 3: Manual Browser ZIP Download
1. On the client computer, visit:
   `http://192.168.10.11:5000/api/agent/windows-agent`
2. Extract **`AegisMesh-Agent-Windows.zip`**.
3. Inside the extracted folder:
   - **For 24/7 background service**: Right-click **`Install-Service.bat`** and select **"Run as administrator"**.
   - **For temporary / on-demand remote support**: Double-click **`Run-Agent-Interactive.bat`**.

---

### Step 4: Verification & Live Remote Management

Once the agent starts on the client computer:
1. Refresh your Administrator Console at `http://192.168.10.11:5000`.
2. The client workstation will appear in your **Device Inventory** with:
   - Green **ONLINE** indicator.
   - Real-time CPU, RAM, Disk, Hostname, and OS version.
3. Click any device to access:
   - 🖥️ **Remote Desktop**: Real-time interactive screen streaming and mouse/keyboard control.
   - 💻 **Terminal**: Interactive remote PowerShell console.
   - 📁 **File Manager**: Browse, upload, and download files from the remote computer.
   - ⚙️ **Tasks & Services**: View running processes, stop tasks, and manage Windows services.
