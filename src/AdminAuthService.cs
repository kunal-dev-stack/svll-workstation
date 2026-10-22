using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class AdminAuthService
{
    private static AdminAuthService? _instance;
    public static AdminAuthService Instance => _instance ??= new AdminAuthService();

    public const string DefaultAdminPassword = "SVLL@Admin2026";
    private readonly string _configFilePath;
    private AdminSecurityConfig _config = new();

    public bool IsAdminMode { get; private set; } = false;
    public event Action<bool>? AdminModeChanged;

    private AdminAuthService()
    {
        _configFilePath = Path.Combine(MainWindow.AuditLogDirectory, "admin_security.json");
        LoadConfig();
    }

    private void LoadConfig()
    {
        try
        {
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
            {
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            }

            if (File.Exists(_configFilePath))
            {
                string json = File.ReadAllText(_configFilePath);
                var loaded = JsonSerializer.Deserialize<AdminSecurityConfig>(json);
                if (loaded != null && !string.IsNullOrEmpty(loaded.PasswordHash))
                {
                    _config = loaded;
                    return;
                }
            }

            // Set default password
            SetPasswordInternal(DefaultAdminPassword);
        }
        catch
        {
            SetPasswordInternal(DefaultAdminPassword);
        }
    }

    private void SaveConfig()
    {
        try
        {
            if (!Directory.Exists(MainWindow.AuditLogDirectory))
            {
                Directory.CreateDirectory(MainWindow.AuditLogDirectory);
            }
            string json = JsonSerializer.Serialize(_config, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_configFilePath, json);
        }
        catch { }
    }

    private void SetPasswordInternal(string plainPassword)
    {
        byte[] saltBytes = RandomNumberGenerator.GetBytes(16);
        string salt = Convert.ToBase64String(saltBytes);
        string hash = HashPasswordWithSalt(plainPassword, salt);

        _config.PasswordSalt = salt;
        _config.PasswordHash = hash;
        _config.LastPasswordChange = DateTime.Now;
        SaveConfig();
    }

    private static string HashPasswordWithSalt(string password, string salt)
    {
        using var sha256 = SHA256.Create();
        byte[] combined = Encoding.UTF8.GetBytes(password + salt + "SVLL_SECURE_ADMIN_SALT_v7");
        byte[] hash = sha256.ComputeHash(combined);
        return Convert.ToBase64String(hash);
    }

    public bool VerifyPassword(string inputPassword)
    {
        if (string.IsNullOrEmpty(inputPassword)) return false;
        string computed = HashPasswordWithSalt(inputPassword, _config.PasswordSalt);
        return string.Equals(computed, _config.PasswordHash, StringComparison.Ordinal);
    }

    public bool UnlockAdmin(string inputPassword)
    {
        if (VerifyPassword(inputPassword))
        {
            IsAdminMode = true;
            AdminModeChanged?.Invoke(true);
            return true;
        }
        return false;
    }

    public void LockAdmin()
    {
        IsAdminMode = false;
        AdminModeChanged?.Invoke(false);
    }

    public bool ChangePassword(string oldPassword, string newPassword)
    {
        if (!VerifyPassword(oldPassword)) return false;
        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Length < 4) return false;

        SetPasswordInternal(newPassword);
        return true;
    }

    public AdminSecurityConfig GetConfig() => _config;

    public void UpdateSharedPaths(string fleetDir, string ticketDir)
    {
        _config.SharedFleetDirectory = fleetDir;
        _config.SharedTicketDirectory = ticketDir;
        SaveConfig();
    }
}
