using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace SVLL_IT_Workstation.Models;

public enum NvrBrand
{
    Hikvision,
    CpPlus,
    Dahua,
    Uniview,
    GenericRtsp,
    Custom
}

public enum StreamQuality
{
    SubStream,
    MainStream
}

public class NvrChannelConfig
{
    public int ChannelNumber { get; set; } = 1;
    public string CustomName { get; set; } = "";
    public bool IsEnabled { get; set; } = true;
}

public class NvrDevice
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = "Warehouse NVR";
    public string Location { get; set; } = "Main Warehouse";
    public string SubLocation { get; set; } = "";
    public string OwnerName { get; set; } = "";
    public string OwnerContact { get; set; } = "";
    public int PlaybackDays { get; set; } = 30;
    public string VerificationCode { get; set; } = "";
    public string WebLink { get; set; } = "";
    public string SoftwareType { get; set; } = "";
    public NvrBrand Brand { get; set; } = NvrBrand.CpPlus;
    public string Host { get; set; } = "192.168.1.100";
    public int HttpPort { get; set; } = 80;
    public int RtspPort { get; set; } = 554;
    public string Username { get; set; } = "admin";
    
    // Encrypted or protected password storage
    public string Password { get; set; } = "";
    
    public int ChannelCount { get; set; } = 8;
    public StreamQuality Quality { get; set; } = StreamQuality.SubStream;
    public string CustomRtspTemplate { get; set; } = "";
    public string CustomSnapshotTemplate { get; set; } = "";
    public List<NvrChannelConfig> Channels { get; set; } = new();
    public bool IsFavorite { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [JsonIgnore]
    public bool IsOnline { get; set; } = false;

    [JsonIgnore]
    public long LatencyMs { get; set; } = -1;

    [JsonIgnore]
    public DateTime? LastChecked { get; set; }

    [JsonIgnore]
    public string CleanHost
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Host)) return "127.0.0.1";
            string h = Host.Trim();
            if (h.StartsWith("http://", StringComparison.OrdinalIgnoreCase)) h = h.Substring(7);
            else if (h.StartsWith("https://", StringComparison.OrdinalIgnoreCase)) h = h.Substring(8);

            int slashIdx = h.IndexOf('/');
            if (slashIdx >= 0) h = h.Substring(0, slashIdx);

            int colonIdx = h.IndexOf(':');
            if (colonIdx >= 0) h = h.Substring(0, colonIdx);

            return h.Trim();
        }
    }

    public string GetChannelName(int channel)
    {
        var ch = Channels.Find(c => c.ChannelNumber == channel);
        if (ch != null && !string.IsNullOrWhiteSpace(ch.CustomName))
        {
            return ch.CustomName;
        }
        return $"Camera {channel:D2}";
    }

    public string GetBrandDisplayName()
    {
        return Brand switch
        {
            NvrBrand.Hikvision => "Hikvision / HiLook",
            NvrBrand.CpPlus => "CP PLUS (Indigo/Orange)",
            NvrBrand.Dahua => "Dahua Technology",
            NvrBrand.Uniview => "Uniview (UNV)",
            NvrBrand.GenericRtsp => "Generic ONVIF / RTSP",
            NvrBrand.Custom => "Custom Profile",
            _ => "Unknown"
        };
    }
}

public class CctvGridSlot
{
    public int SlotIndex { get; set; }
    public string? NvrId { get; set; }
    public int ChannelNumber { get; set; } = 1;
    public bool IsActive => !string.IsNullOrEmpty(NvrId);
}

