// =============================================================================
// SVLL IT Workstation v8.0 - Build Identity Constants
// Controls which features are compiled in for Admin vs User editions.
// This file is intentionally simple — logic lives in #if BUILD_ADMIN blocks.
// =============================================================================

namespace SVLL_IT_Workstation;

public static class BuildConstants
{
#if BUILD_ADMIN
    public const bool IsAdminBuild = true;
    public const string BuildEdition = "Admin Edition";
    public const string BuildEditionBadge = "👑 ADMIN EDITION";
    public const string BuildEditionColor = "#166534"; // dark green
#else
    public const bool IsAdminBuild = false;
    public const string BuildEdition = "User Edition";
    public const string BuildEditionBadge = "🔵 USER EDITION";
    public const string BuildEditionColor = "#1A4BB2"; // SVLL blue
#endif

    public const string AppVersion = "8.3";
    public const string RelayApiKey = "SVLL-RELAY-KEY-2026-KUNAL";  // shared secret, same in both builds

    /// <summary>Default corporate server IP for Shree Vasu Logistics</summary>
    public const string DefaultRelayUrl = "http://192.168.10.222:5000";
    public const string DefaultMeshCentralUrl = "http://192.168.10.222:8086";

    /// <summary>Corporate unattended access password for 1-click zero-password remote control via AnyDesk</summary>
    public const string DefaultRemotePassword = "SVLL@Remote2026";
}
