using System;

namespace SVLL_IT_Workstation;

public class NetworkShareItem
{
    public string Name { get; set; } = "";
    public string FullPath { get; set; } = "";
    public bool IsDirectory { get; set; } = false;
    public long FileSizeBytes { get; set; } = 0;
    public DateTime LastModified { get; set; } = DateTime.MinValue;

    public string SizeFormatted
    {
        get
        {
            if (IsDirectory) return "<DIR>";
            if (FileSizeBytes < 1024) return $"{FileSizeBytes} B";
            if (FileSizeBytes < 1048576) return $"{Math.Round(FileSizeBytes / 1024.0, 1)} KB";
            if (FileSizeBytes < 1073741824) return $"{Math.Round(FileSizeBytes / 1048576.0, 1)} MB";
            return $"{Math.Round(FileSizeBytes / 1073741824.0, 2)} GB";
        }
    }
}
