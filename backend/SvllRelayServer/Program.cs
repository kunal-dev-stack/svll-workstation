// =============================================================================
// SVLL IT Relay Server v1.0
// ASP.NET Core 8 Minimal API — Lightweight fleet sync + ticket relay
//
// Deploy on your office NAS / Server PC:
//   dotnet publish -c Release -o publish
//   dotnet SvllRelayServer.dll --urls "http://0.0.0.0:5000"
//
// Or install as Windows Service:
//   sc create SvllRelay binpath="dotnet C:\svll-relay\SvllRelayServer.dll --urls http://0.0.0.0:5000"
//   sc start SvllRelay
//
// Configure SVLL Admin Workstation: Admin Security Config → Relay Server URL: http://<server-ip>:5000
// =============================================================================

using Microsoft.Data.Sqlite;
using System.Text.Json;
using SvllRelayServer;

var builder = WebApplication.CreateBuilder(args);

// ── Configuration ────────────────────────────────────────────────────────────
// Config is in RelayConfig class below

builder.Host.UseWindowsService();
builder.Logging.AddConsole();
builder.Services.AddSingleton<DatabaseService>();

var app = builder.Build();

// Initialize DB on startup
app.Services.GetRequiredService<DatabaseService>().Initialize();

// ── Middleware: API Key Authentication ───────────────────────────────────────
app.Use(async (ctx, next) =>
{
    string path = ctx.Request.Path.Value ?? "";
    // Public routes: health check, web dashboard, panel APIs, downloads, install scripts
    if (path.StartsWith("/api/health") ||
        path == "/" ||
        path == "/panel" ||
        path.StartsWith("/panel/") ||
        path.StartsWith("/download/") ||
        path == "/install.ps1" ||
        path == "/install-admin.ps1" ||
        path == "/install-server.ps1")
    {
        await next();
        return;
    }

    string? key = ctx.Request.Headers["X-SVLL-Key"].FirstOrDefault();
    if (key != RelayConfig.ApiKey)
    {
        ctx.Response.StatusCode = 401;
        await ctx.Response.WriteAsync("{\"error\":\"Unauthorized\"}");
        return;
    }
    await next();
});

// ── Web Dashboard Panel & Public Routes ───────────────────────────────────────
app.MapGet("/", () => Results.Content(DashboardHtml.Render(), "text/html"));
app.MapGet("/panel", () => Results.Content(DashboardHtml.Render(), "text/html"));

app.MapGet("/panel/api/fleet", (DatabaseService db) =>
{
    var devices = db.GetAllDevices();
    foreach (var d in devices)
    {
        if ((DateTime.UtcNow - d.LastSeen).TotalMinutes > RelayConfig.OfflineMinutes &&
            d.Status != "SupportRequested")
        {
            d.Status = "Offline";
        }
    }
    var sorted = devices
        .OrderBy(d => d.Status == "SupportRequested" ? 0 : d.Status == "Online" ? 1 : 2)
        .ThenBy(d => d.Hostname)
        .ToList();
    return Results.Ok(sorted);
});

app.MapGet("/panel/api/tickets", (DatabaseService db) => Results.Ok(db.GetAllTickets()));

app.MapPost("/panel/api/ticket/reply", (DatabaseService db, PanelReplyRequest req) =>
{
    if (string.IsNullOrEmpty(req.CloudTicketId)) return Results.BadRequest("Missing ticket ID");
    bool ok = db.UpdateTicket(req.CloudTicketId, new TicketUpdateRequest
    {
        Status = req.Status ?? "Resolved",
        AdminReply = req.AdminReply,
        AdminNotes = $"Replied via Web Panel: {req.AdminReply}"
    });
    return ok ? Results.Ok(new { updated = true }) : Results.NotFound();
});

// Download endpoints
app.MapGet("/download/user-setup", () => Results.Redirect("https://github.com/kunal-dev-stack/svll-workstation/releases/download/v8.0/SVLL-IT-Workstation-User-v8.0-Setup.exe"));
app.MapGet("/download/meshagent", () => Results.Redirect("https://github.com/Ylianst/MeshAgent/releases/download/legacy-1.2.6/MeshService64.exe"));

// Direct installer script endpoints
app.MapGet("/install.ps1", async (HttpContext ctx) =>
{
    string scriptPath = Path.Combine(AppContext.BaseDirectory, "install-client.ps1");
    if (!File.Exists(scriptPath)) scriptPath = Path.Combine(AppContext.BaseDirectory, "install.ps1");
    if (File.Exists(scriptPath))
    {
        ctx.Response.ContentType = "text/plain";
        await ctx.Response.SendFileAsync(scriptPath);
    }
    else
    {
        ctx.Response.Redirect("https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install.ps1");
    }
});

app.MapGet("/install-admin.ps1", async (HttpContext ctx) =>
{
    string scriptPath = Path.Combine(AppContext.BaseDirectory, "install-admin.ps1");
    if (File.Exists(scriptPath))
    {
        ctx.Response.ContentType = "text/plain";
        await ctx.Response.SendFileAsync(scriptPath);
    }
    else
    {
        ctx.Response.Redirect("https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-admin.ps1");
    }
});

app.MapGet("/install-server.ps1", async (HttpContext ctx) =>
{
    string scriptPath = Path.Combine(AppContext.BaseDirectory, "install-server.ps1");
    if (File.Exists(scriptPath))
    {
        ctx.Response.ContentType = "text/plain";
        await ctx.Response.SendFileAsync(scriptPath);
    }
    else
    {
        ctx.Response.Redirect("https://raw.githubusercontent.com/kunal-dev-stack/svll-workstation/main/install-server.ps1");
    }
});

// ── Health Check ─────────────────────────────────────────────────────────────
app.MapGet("/api/health", () => Results.Ok(new
{
    status = "ok",
    server = "SVLL IT Relay Server v1.0",
    timestamp = DateTime.UtcNow,
    uptime = Environment.TickCount64 / 1000 + "s"
}));

// ── Heartbeat ────────────────────────────────────────────────────────────────
app.MapPost("/api/heartbeat", async (DatabaseService db, HttpContext ctx) =>
{
    try
    {
        var device = await ctx.Request.ReadFromJsonAsync<DeviceRecord>();
        if (device == null || string.IsNullOrEmpty(device.Hostname))
            return Results.BadRequest("Invalid device data");

        device.LastSeen = DateTime.UtcNow;
        device.Status = "Online";
        db.UpsertDevice(device);

        app.Logger.LogInformation("Heartbeat: {Host} ({IP}) CPU:{CPU}% RAM:{RAM}%",
            device.Hostname, device.IpAddress, device.CpuLoadPercent, device.RamUsagePercent);

        return Results.Ok(new { registered = true, serverTime = DateTime.UtcNow });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ── Fleet List ───────────────────────────────────────────────────────────────
app.MapGet("/api/fleet", (DatabaseService db) =>
{
    var devices = db.GetAllDevices();
    // Mark devices as offline if last heartbeat was >N minutes ago
    foreach (var d in devices)
    {
        if ((DateTime.UtcNow - d.LastSeen).TotalMinutes > RelayConfig.OfflineMinutes &&
            d.Status != "SupportRequested")
        {
            d.Status = "Offline";
        }
    }
    // Sort: SupportRequested first, then Online, then Offline; alpha by hostname
    devices = devices
        .OrderBy(d => d.Status == "SupportRequested" ? 0 : d.Status == "Online" ? 1 : 2)
        .ThenBy(d => d.Hostname)
        .ToList();
    return Results.Ok(devices);
});

// ── Submit Ticket ─────────────────────────────────────────────────────────────
app.MapPost("/api/ticket", async (DatabaseService db, HttpContext ctx) =>
{
    try
    {
        var ticket = await ctx.Request.ReadFromJsonAsync<TicketRecord>();
        if (ticket == null || string.IsNullOrEmpty(ticket.Title))
            return Results.BadRequest("Invalid ticket");

        string cloudId = db.InsertTicket(ticket);
        app.Logger.LogInformation("New Ticket: [{CloudId}] {Title} from {Host} (Priority: {Priority})",
            cloudId, ticket.Title, ticket.Hostname, ticket.Priority);

        return Results.Ok(new { cloudTicketId = cloudId });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ── Get All Tickets (Admin) ───────────────────────────────────────────────────
app.MapGet("/api/tickets", (DatabaseService db) =>
{
    var tickets = db.GetAllTickets();
    return Results.Ok(tickets);
});

// ── Get Specific Ticket ───────────────────────────────────────────────────────
app.MapGet("/api/ticket/{cloudTicketId}", (DatabaseService db, string cloudTicketId) =>
{
    var ticket = db.GetTicket(cloudTicketId);
    return ticket != null ? Results.Ok(ticket) : Results.NotFound();
});

// ── Get Ticket Reply (User polls for admin reply) ─────────────────────────────
app.MapGet("/api/ticket/{cloudTicketId}/reply", (DatabaseService db, string cloudTicketId) =>
{
    var ticket = db.GetTicket(cloudTicketId);
    if (ticket == null) return Results.NotFound();
    if (string.IsNullOrEmpty(ticket.AdminReply)) return Results.NoContent();
    return Results.Ok(ticket);
});

// ── Resolve Ticket + Send Reply (Admin) ──────────────────────────────────────
app.MapMethods("/api/ticket/{cloudTicketId}", ["PATCH"], async (DatabaseService db, string cloudTicketId, HttpContext ctx) =>
{
    try
    {
        var update = await ctx.Request.ReadFromJsonAsync<TicketUpdateRequest>();
        if (update == null) return Results.BadRequest("Invalid update");

        bool ok = db.UpdateTicket(cloudTicketId, update);
        if (!ok) return Results.NotFound();

        app.Logger.LogInformation("Ticket [{CloudId}] updated: Status={Status}, Reply sent: {HasReply}",
            cloudTicketId, update.Status, !string.IsNullOrEmpty(update.AdminReply));

        return Results.Ok(new { updated = true });
    }
    catch (Exception ex)
    {
        return Results.Problem(ex.Message);
    }
});

// ── Delete Device (housekeeping) ──────────────────────────────────────────────
app.MapDelete("/api/device/{hostname}", (DatabaseService db, string hostname) =>
{
    db.DeleteDevice(hostname);
    return Results.Ok();
});

app.Logger.LogInformation("=======================================================");
app.Logger.LogInformation("  SVLL IT Relay Server v1.0 - Starting");
app.Logger.LogInformation("  API Key: {Key}", RelayConfig.ApiKey[..8] + "...");
app.Logger.LogInformation("  Database: {Db}", Path.GetFullPath(RelayConfig.DbPath));
app.Logger.LogInformation("=======================================================");

app.Run();

// =============================================================================
// DATABASE SERVICE
// =============================================================================

public static class RelayConfig
{
    public const string ApiKey = "SVLL-RELAY-KEY-2026-KUNAL";
    public const string DbPath = "svll_relay.db";
    public const int OfflineMinutes = 5;
}

public class DatabaseService
{
    private readonly string _connectionString = $"Data Source={RelayConfig.DbPath}";

    public void Initialize()
    {
        using var conn = GetConnection();
        conn.Open();

        // Devices table
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            CREATE TABLE IF NOT EXISTS Devices (
                Hostname TEXT PRIMARY KEY,
                Id TEXT,
                IpAddress TEXT,
                MacAddress TEXT,
                SerialNumber TEXT,
                BranchLocation TEXT,
                LoggedInUser TEXT,
                AnyDeskId TEXT,
                AnyDeskInstalled INTEGER,
                RdpEnabled INTEGER,
                Status TEXT,
                SupportRequestMessage TEXT,
                CpuLoadPercent REAL,
                RamUsagePercent REAL,
                DiskFreePercent REAL,
                OsVersion TEXT,
                OsBuild TEXT,
                MeshNodeId TEXT,
                MeshAgentRunning INTEGER,
                LastSeen TEXT
            );

            CREATE TABLE IF NOT EXISTS Tickets (
                CloudTicketId TEXT PRIMARY KEY,
                TicketId TEXT,
                Title TEXT,
                Description TEXT,
                Category TEXT,
                Priority TEXT,
                Status TEXT,
                CreatedAt TEXT,
                UpdatedAt TEXT,
                CreatedBy TEXT,
                UserContact TEXT,
                BranchLocation TEXT,
                Hostname TEXT,
                IpAddress TEXT,
                AnyDeskId TEXT,
                AttachedDiagnosticsJson TEXT,
                AiTriageTranscript TEXT,
                AdminNotes TEXT,
                AdminReply TEXT,
                AdminReplyAt TEXT,
                AssignedTechnician TEXT,
                FiledViaAi INTEGER
            );
            """;
        cmd.ExecuteNonQuery();
    }

    public void UpsertDevice(DeviceRecord d)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Devices (Hostname,Id,IpAddress,MacAddress,SerialNumber,BranchLocation,LoggedInUser,
                AnyDeskId,AnyDeskInstalled,RdpEnabled,Status,SupportRequestMessage,CpuLoadPercent,
                RamUsagePercent,DiskFreePercent,OsVersion,OsBuild,MeshNodeId,MeshAgentRunning,LastSeen)
            VALUES ($Hostname,$Id,$IpAddress,$MacAddress,$SerialNumber,$BranchLocation,$LoggedInUser,
                $AnyDeskId,$AnyDeskInstalled,$RdpEnabled,$Status,$SupportRequestMessage,$CpuLoadPercent,
                $RamUsagePercent,$DiskFreePercent,$OsVersion,$OsBuild,$MeshNodeId,$MeshAgentRunning,$LastSeen)
            ON CONFLICT(Hostname) DO UPDATE SET
                Id=$Id, IpAddress=$IpAddress, MacAddress=$MacAddress, SerialNumber=$SerialNumber,
                BranchLocation=$BranchLocation, LoggedInUser=$LoggedInUser, AnyDeskId=$AnyDeskId,
                AnyDeskInstalled=$AnyDeskInstalled, RdpEnabled=$RdpEnabled, Status=$Status,
                SupportRequestMessage=$SupportRequestMessage, CpuLoadPercent=$CpuLoadPercent,
                RamUsagePercent=$RamUsagePercent, DiskFreePercent=$DiskFreePercent, OsVersion=$OsVersion,
                OsBuild=$OsBuild, MeshNodeId=$MeshNodeId, MeshAgentRunning=$MeshAgentRunning, LastSeen=$LastSeen;
            """;
        cmd.Parameters.AddWithValue("$Hostname", d.Hostname);
        cmd.Parameters.AddWithValue("$Id", d.Id ?? "");
        cmd.Parameters.AddWithValue("$IpAddress", d.IpAddress ?? "");
        cmd.Parameters.AddWithValue("$MacAddress", d.MacAddress ?? "");
        cmd.Parameters.AddWithValue("$SerialNumber", d.SerialNumber ?? "");
        cmd.Parameters.AddWithValue("$BranchLocation", d.BranchLocation ?? "");
        cmd.Parameters.AddWithValue("$LoggedInUser", d.LoggedInUser ?? "");
        cmd.Parameters.AddWithValue("$AnyDeskId", d.AnyDeskId ?? "");
        cmd.Parameters.AddWithValue("$AnyDeskInstalled", d.AnyDeskInstalled ? 1 : 0);
        cmd.Parameters.AddWithValue("$RdpEnabled", d.RdpEnabled ? 1 : 0);
        cmd.Parameters.AddWithValue("$Status", d.Status ?? "Online");
        cmd.Parameters.AddWithValue("$SupportRequestMessage", d.SupportRequestMessage ?? "");
        cmd.Parameters.AddWithValue("$CpuLoadPercent", d.CpuLoadPercent);
        cmd.Parameters.AddWithValue("$RamUsagePercent", d.RamUsagePercent);
        cmd.Parameters.AddWithValue("$DiskFreePercent", d.DiskFreePercent);
        cmd.Parameters.AddWithValue("$OsVersion", d.OsVersion ?? "");
        cmd.Parameters.AddWithValue("$OsBuild", d.OsBuild ?? "");
        cmd.Parameters.AddWithValue("$MeshNodeId", d.MeshNodeId ?? "");
        cmd.Parameters.AddWithValue("$MeshAgentRunning", d.MeshAgentRunning ? 1 : 0);
        cmd.Parameters.AddWithValue("$LastSeen", d.LastSeen.ToString("O"));
        cmd.ExecuteNonQuery();
    }

    public List<DeviceRecord> GetAllDevices()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Devices ORDER BY LastSeen DESC;";
        using var reader = cmd.ExecuteReader();
        var list = new List<DeviceRecord>();
        while (reader.Read())
        {
            list.Add(new DeviceRecord
            {
                Hostname = reader.GetString(reader.GetOrdinal("Hostname")),
                Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? "" : reader.GetString(reader.GetOrdinal("Id")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? "" : reader.GetString(reader.GetOrdinal("IpAddress")),
                MacAddress = reader.IsDBNull(reader.GetOrdinal("MacAddress")) ? "" : reader.GetString(reader.GetOrdinal("MacAddress")),
                SerialNumber = reader.IsDBNull(reader.GetOrdinal("SerialNumber")) ? "" : reader.GetString(reader.GetOrdinal("SerialNumber")),
                BranchLocation = reader.IsDBNull(reader.GetOrdinal("BranchLocation")) ? "" : reader.GetString(reader.GetOrdinal("BranchLocation")),
                LoggedInUser = reader.IsDBNull(reader.GetOrdinal("LoggedInUser")) ? "" : reader.GetString(reader.GetOrdinal("LoggedInUser")),
                AnyDeskId = reader.IsDBNull(reader.GetOrdinal("AnyDeskId")) ? "" : reader.GetString(reader.GetOrdinal("AnyDeskId")),
                AnyDeskInstalled = reader.GetInt64(reader.GetOrdinal("AnyDeskInstalled")) == 1,
                RdpEnabled = reader.GetInt64(reader.GetOrdinal("RdpEnabled")) == 1,
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Offline" : reader.GetString(reader.GetOrdinal("Status")),
                SupportRequestMessage = reader.IsDBNull(reader.GetOrdinal("SupportRequestMessage")) ? "" : reader.GetString(reader.GetOrdinal("SupportRequestMessage")),
                CpuLoadPercent = reader.GetDouble(reader.GetOrdinal("CpuLoadPercent")),
                RamUsagePercent = reader.GetDouble(reader.GetOrdinal("RamUsagePercent")),
                DiskFreePercent = reader.GetDouble(reader.GetOrdinal("DiskFreePercent")),
                OsVersion = reader.IsDBNull(reader.GetOrdinal("OsVersion")) ? "" : reader.GetString(reader.GetOrdinal("OsVersion")),
                OsBuild = reader.IsDBNull(reader.GetOrdinal("OsBuild")) ? "" : reader.GetString(reader.GetOrdinal("OsBuild")),
                MeshNodeId = reader.IsDBNull(reader.GetOrdinal("MeshNodeId")) ? "" : reader.GetString(reader.GetOrdinal("MeshNodeId")),
                MeshAgentRunning = reader.GetInt64(reader.GetOrdinal("MeshAgentRunning")) == 1,
                LastSeen = DateTime.Parse(reader.GetString(reader.GetOrdinal("LastSeen")))
            });
        }
        return list;
    }

    public void DeleteDevice(string hostname)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM Devices WHERE Hostname=$h;";
        cmd.Parameters.AddWithValue("$h", hostname);
        cmd.ExecuteNonQuery();
    }

    public string InsertTicket(TicketRecord t)
    {
        string cloudId = "CLT-" + DateTime.UtcNow.ToString("yyMMdd-HHmmss-fff") + "-" + t.Hostname[..Math.Min(4, t.Hostname.Length)];
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            INSERT INTO Tickets (CloudTicketId,TicketId,Title,Description,Category,Priority,Status,
                CreatedAt,UpdatedAt,CreatedBy,UserContact,BranchLocation,Hostname,IpAddress,AnyDeskId,
                AttachedDiagnosticsJson,AiTriageTranscript,AdminNotes,AdminReply,AdminReplyAt,
                AssignedTechnician,FiledViaAi)
            VALUES ($CloudTicketId,$TicketId,$Title,$Description,$Category,$Priority,$Status,
                $CreatedAt,$UpdatedAt,$CreatedBy,$UserContact,$BranchLocation,$Hostname,$IpAddress,$AnyDeskId,
                $AttachedDiagnosticsJson,$AiTriageTranscript,$AdminNotes,$AdminReply,$AdminReplyAt,
                $AssignedTechnician,$FiledViaAi);
            """;
        cmd.Parameters.AddWithValue("$CloudTicketId", cloudId);
        cmd.Parameters.AddWithValue("$TicketId", t.TicketId ?? "");
        cmd.Parameters.AddWithValue("$Title", t.Title ?? "");
        cmd.Parameters.AddWithValue("$Description", t.Description ?? "");
        cmd.Parameters.AddWithValue("$Category", t.Category ?? "General");
        cmd.Parameters.AddWithValue("$Priority", t.Priority ?? "Medium");
        cmd.Parameters.AddWithValue("$Status", "Open");
        cmd.Parameters.AddWithValue("$CreatedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$CreatedBy", t.CreatedBy ?? "");
        cmd.Parameters.AddWithValue("$UserContact", t.UserContact ?? "");
        cmd.Parameters.AddWithValue("$BranchLocation", t.BranchLocation ?? "");
        cmd.Parameters.AddWithValue("$Hostname", t.Hostname ?? "");
        cmd.Parameters.AddWithValue("$IpAddress", t.IpAddress ?? "");
        cmd.Parameters.AddWithValue("$AnyDeskId", t.AnyDeskId ?? "");
        cmd.Parameters.AddWithValue("$AttachedDiagnosticsJson", t.AttachedDiagnosticsJson ?? "");
        cmd.Parameters.AddWithValue("$AiTriageTranscript", t.AiTriageTranscript ?? "");
        cmd.Parameters.AddWithValue("$AdminNotes", "");
        cmd.Parameters.AddWithValue("$AdminReply", "");
        cmd.Parameters.AddWithValue("$AdminReplyAt", DBNull.Value);
        cmd.Parameters.AddWithValue("$AssignedTechnician", "Kunal Turkar (SVLL IT)");
        cmd.Parameters.AddWithValue("$FiledViaAi", t.FiledViaAi ? 1 : 0);
        cmd.ExecuteNonQuery();
        return cloudId;
    }

    public List<TicketRecord> GetAllTickets()
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Tickets ORDER BY CreatedAt DESC;";
        return ReadTickets(cmd);
    }

    public TicketRecord? GetTicket(string cloudId)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM Tickets WHERE CloudTicketId=$id;";
        cmd.Parameters.AddWithValue("$id", cloudId);
        var list = ReadTickets(cmd);
        return list.FirstOrDefault();
    }

    public bool UpdateTicket(string cloudId, TicketUpdateRequest update)
    {
        using var conn = GetConnection();
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = """
            UPDATE Tickets SET
                Status=$Status, AdminNotes=$AdminNotes, AdminReply=$AdminReply,
                AdminReplyAt=$AdminReplyAt, UpdatedAt=$UpdatedAt,
                AssignedTechnician=$AssignedTechnician
            WHERE CloudTicketId=$id;
            """;
        cmd.Parameters.AddWithValue("$Status", update.Status ?? "Resolved");
        cmd.Parameters.AddWithValue("$AdminNotes", update.AdminNotes ?? "");
        cmd.Parameters.AddWithValue("$AdminReply", update.AdminReply ?? "");
        cmd.Parameters.AddWithValue("$AdminReplyAt", string.IsNullOrEmpty(update.AdminReply) ? DBNull.Value : DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$UpdatedAt", DateTime.UtcNow.ToString("O"));
        cmd.Parameters.AddWithValue("$AssignedTechnician", update.AssignedTechnician ?? "Kunal Turkar (SVLL IT)");
        cmd.Parameters.AddWithValue("$id", cloudId);
        return cmd.ExecuteNonQuery() > 0;
    }

    private static List<TicketRecord> ReadTickets(SqliteCommand cmd)
    {
        using var reader = cmd.ExecuteReader();
        var list = new List<TicketRecord>();
        while (reader.Read())
        {
            list.Add(new TicketRecord
            {
                CloudTicketId = reader.GetString(reader.GetOrdinal("CloudTicketId")),
                TicketId = reader.IsDBNull(reader.GetOrdinal("TicketId")) ? "" : reader.GetString(reader.GetOrdinal("TicketId")),
                Title = reader.IsDBNull(reader.GetOrdinal("Title")) ? "" : reader.GetString(reader.GetOrdinal("Title")),
                Description = reader.IsDBNull(reader.GetOrdinal("Description")) ? "" : reader.GetString(reader.GetOrdinal("Description")),
                Category = reader.IsDBNull(reader.GetOrdinal("Category")) ? "General" : reader.GetString(reader.GetOrdinal("Category")),
                Priority = reader.IsDBNull(reader.GetOrdinal("Priority")) ? "Medium" : reader.GetString(reader.GetOrdinal("Priority")),
                Status = reader.IsDBNull(reader.GetOrdinal("Status")) ? "Open" : reader.GetString(reader.GetOrdinal("Status")),
                CreatedAt = DateTime.Parse(reader.GetString(reader.GetOrdinal("CreatedAt"))),
                UpdatedAt = reader.IsDBNull(reader.GetOrdinal("UpdatedAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("UpdatedAt"))),
                CreatedBy = reader.IsDBNull(reader.GetOrdinal("CreatedBy")) ? "" : reader.GetString(reader.GetOrdinal("CreatedBy")),
                UserContact = reader.IsDBNull(reader.GetOrdinal("UserContact")) ? "" : reader.GetString(reader.GetOrdinal("UserContact")),
                BranchLocation = reader.IsDBNull(reader.GetOrdinal("BranchLocation")) ? "" : reader.GetString(reader.GetOrdinal("BranchLocation")),
                Hostname = reader.IsDBNull(reader.GetOrdinal("Hostname")) ? "" : reader.GetString(reader.GetOrdinal("Hostname")),
                IpAddress = reader.IsDBNull(reader.GetOrdinal("IpAddress")) ? "" : reader.GetString(reader.GetOrdinal("IpAddress")),
                AnyDeskId = reader.IsDBNull(reader.GetOrdinal("AnyDeskId")) ? "" : reader.GetString(reader.GetOrdinal("AnyDeskId")),
                AttachedDiagnosticsJson = reader.IsDBNull(reader.GetOrdinal("AttachedDiagnosticsJson")) ? "" : reader.GetString(reader.GetOrdinal("AttachedDiagnosticsJson")),
                AiTriageTranscript = reader.IsDBNull(reader.GetOrdinal("AiTriageTranscript")) ? "" : reader.GetString(reader.GetOrdinal("AiTriageTranscript")),
                AdminNotes = reader.IsDBNull(reader.GetOrdinal("AdminNotes")) ? "" : reader.GetString(reader.GetOrdinal("AdminNotes")),
                AdminReply = reader.IsDBNull(reader.GetOrdinal("AdminReply")) ? "" : reader.GetString(reader.GetOrdinal("AdminReply")),
                AdminReplyAt = reader.IsDBNull(reader.GetOrdinal("AdminReplyAt")) ? null : DateTime.Parse(reader.GetString(reader.GetOrdinal("AdminReplyAt"))),
                AssignedTechnician = reader.IsDBNull(reader.GetOrdinal("AssignedTechnician")) ? "" : reader.GetString(reader.GetOrdinal("AssignedTechnician")),
                FiledViaAi = reader.GetInt64(reader.GetOrdinal("FiledViaAi")) == 1,
            });
        }
        return list;
    }

    private SqliteConnection GetConnection() => new(_connectionString);
}

// =============================================================================
// DATA TRANSFER OBJECTS (matches SVLL client models)
// =============================================================================

public class DeviceRecord
{
    public string Hostname { get; set; } = "";
    public string Id { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string MacAddress { get; set; } = "";
    public string SerialNumber { get; set; } = "";
    public string BranchLocation { get; set; } = "";
    public string LoggedInUser { get; set; } = "";
    public string AnyDeskId { get; set; } = "";
    public bool AnyDeskInstalled { get; set; }
    public bool RdpEnabled { get; set; }
    public string Status { get; set; } = "Online";
    public string SupportRequestMessage { get; set; } = "";
    public double CpuLoadPercent { get; set; }
    public double RamUsagePercent { get; set; }
    public double DiskFreePercent { get; set; }
    public string OsVersion { get; set; } = "";
    public string OsBuild { get; set; } = "";
    public string MeshNodeId { get; set; } = "";
    public bool MeshAgentRunning { get; set; }
    public DateTime LastSeen { get; set; } = DateTime.UtcNow;
}

public class TicketRecord
{
    public string CloudTicketId { get; set; } = "";
    public string TicketId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Category { get; set; } = "General";
    public string Priority { get; set; } = "Medium";
    public string Status { get; set; } = "Open";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string CreatedBy { get; set; } = "";
    public string UserContact { get; set; } = "";
    public string BranchLocation { get; set; } = "";
    public string Hostname { get; set; } = "";
    public string IpAddress { get; set; } = "";
    public string AnyDeskId { get; set; } = "";
    public string AttachedDiagnosticsJson { get; set; } = "";
    public string AiTriageTranscript { get; set; } = "";
    public string AdminNotes { get; set; } = "";
    public string AdminReply { get; set; } = "";
    public DateTime? AdminReplyAt { get; set; }
    public string AssignedTechnician { get; set; } = "";
    public bool FiledViaAi { get; set; }
}

public class TicketUpdateRequest
{
    public string? Status { get; set; }
    public string? AdminNotes { get; set; }
    public string? AdminReply { get; set; }
    public string? AssignedTechnician { get; set; }
}

public class PanelReplyRequest
{
    public string CloudTicketId { get; set; } = "";
    public string AdminReply { get; set; } = "";
    public string Status { get; set; } = "Resolved";
}

