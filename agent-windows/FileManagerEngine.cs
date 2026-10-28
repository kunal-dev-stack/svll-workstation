using System;
using System.Collections.Generic;
using System.IO;

namespace AegisMeshAgent
{
    public class RemoteFileItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long SizeBytes { get; set; }
        public long ModifiedAt { get; set; }
    }

    public static class FileManagerEngine
    {
        public static List<RemoteFileItem> ListDirectory(string targetPath)
        {
            var list = new List<RemoteFileItem>();

            if (string.IsNullOrWhiteSpace(targetPath) || !Directory.Exists(targetPath))
            {
                targetPath = @"C:\";
            }

            try
            {
                var dirInfo = new DirectoryInfo(targetPath);

                // Subdirectories
                foreach (var dir in dirInfo.GetDirectories())
                {
                    try
                    {
                        list.Add(new RemoteFileItem
                        {
                            Name = dir.Name,
                            Path = dir.FullName,
                            IsDirectory = true,
                            SizeBytes = 0,
                            ModifiedAt = new DateTimeOffset(dir.LastWriteTimeUtc).ToUnixTimeMilliseconds()
                        });
                    }
                    catch { }
                }

                // Files
                foreach (var file in dirInfo.GetFiles())
                {
                    try
                    {
                        list.Add(new RemoteFileItem
                        {
                            Name = file.Name,
                            Path = file.FullName,
                            IsDirectory = false,
                            SizeBytes = file.Length,
                            ModifiedAt = new DateTimeOffset(file.LastWriteTimeUtc).ToUnixTimeMilliseconds()
                        });
                    }
                    catch { }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FILES] Access error for path {targetPath}: {ex.Message}");
            }

            return list;
        }
    }
}
