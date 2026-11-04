using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using SVLL_IT_Workstation.Models;

namespace SVLL_IT_Workstation;

public class NvrDeviceStorage
{
    private static readonly Lazy<NvrDeviceStorage> _lazy = new(() => new NvrDeviceStorage());
    public static NvrDeviceStorage Instance => _lazy.Value;

    private readonly object _lock = new();
    private readonly string _storagePath;
    private readonly List<NvrDevice> _devices = new();

    public event Action? DevicesUpdated;

    private NvrDeviceStorage()
    {
        string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SVLL_IT_Workstation");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
        _storagePath = Path.Combine(dir, "cctv_nvr_devices.json");
        Load();
    }

    public IReadOnlyList<NvrDevice> GetDevices()
    {
        lock (_lock)
        {
            return new List<NvrDevice>(_devices);
        }
    }

    public NvrDevice? GetDeviceById(string id)
    {
        lock (_lock)
        {
            return _devices.Find(d => d.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }
    }

    public void SaveDevice(NvrDevice device)
    {
        lock (_lock)
        {
            int index = _devices.FindIndex(d => d.Id == device.Id);
            if (index >= 0)
            {
                _devices[index] = device;
            }
            else
            {
                _devices.Add(device);
            }
            Persist();
        }
        DevicesUpdated?.Invoke();
    }

    public void DeleteDevice(string id)
    {
        lock (_lock)
        {
            int removed = _devices.RemoveAll(d => d.Id == id);
            if (removed > 0)
            {
                Persist();
            }
        }
        DevicesUpdated?.Invoke();
    }

    private void Persist()
    {
        try
        {
            var copyList = new List<NvrDevice>();
            foreach (var d in _devices)
            {
                var copy = JsonSerializer.Deserialize<NvrDevice>(JsonSerializer.Serialize(d))!;
                if (!string.IsNullOrEmpty(copy.Password))
                {
                    copy.Password = EncryptString(copy.Password);
                }
                copyList.Add(copy);
            }

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(copyList, options);
            File.WriteAllText(_storagePath, json);
        }
        catch { }
    }

    private void Load()
    {
        lock (_lock)
        {
            _devices.Clear();
            if (File.Exists(_storagePath))
            {
                try
                {
                    string json = File.ReadAllText(_storagePath);
                    var list = JsonSerializer.Deserialize<List<NvrDevice>>(json);
                    if (list != null && list.Count >= 40)
                    {
                        foreach (var d in list)
                        {
                            if (!string.IsNullOrEmpty(d.Password))
                            {
                                d.Password = DecryptString(d.Password);
                            }
                            _devices.Add(d);
                        }
                        return;
                    }
                }
                catch { }
            }

            // Seed full 46 real SVLL campus cameras and NVRs
            SeedDefaultDevices();
            Persist();
        }
    }

    private static List<NvrChannelConfig> GenerateChannels(string subLocation, int count)
    {
        var list = new List<NvrChannelConfig>();
        for (int i = 1; i <= count; i++)
        {
            list.Add(new NvrChannelConfig
            {
                ChannelNumber = i,
                CustomName = string.IsNullOrWhiteSpace(subLocation) ? $"Camera {i:D2}" : $"{subLocation} - Cam {i:D2}",
                IsEnabled = true
            });
        }
        return list;
    }

    private void SeedDefaultDevices()
    {
        _devices.Clear();

        void AddNvr(
            string id, string name, string loc, string subLoc, int chCount,
            string owner, string phone, int pbDays, string host, int httpPort,
            string user, string pass, NvrBrand brand, string verCode = "",
            string webLink = "", string softType = "")
        {
            _devices.Add(new NvrDevice
            {
                Id = id,
                Name = name,
                Location = loc,
                SubLocation = subLoc,
                ChannelCount = chCount,
                OwnerName = owner,
                OwnerContact = phone,
                PlaybackDays = pbDays,
                Host = host,
                HttpPort = httpPort,
                RtspPort = 554,
                Username = user,
                Password = pass,
                Brand = brand,
                VerificationCode = verCode,
                WebLink = webLink,
                SoftwareType = softType,
                Quality = StreamQuality.SubStream,
                Channels = GenerateChannels(subLoc, chCount)
            });
        }

        // 1..46 Shree Vasu Logistics Enterprise CCTV Fleet
        AddNvr("svll-nvr-01", "RAIPURA CAMPUS_1", "RAIPURA CAMPUS", "POLYCAB DEPO", 2, "PRMOAD UPADHAY", "7000681473", 27, "192.168.10.22", 80, "admin", "admin@12345", NvrBrand.Hikvision, "CBVMKF", "http://192.168.10.22");
        AddNvr("svll-nvr-02", "RAIPURA CAMPUS_2", "RAIPURA CAMPUS", "TTK PRESTIGE", 6, "SATISH SAHU", "7000681485", 29, "192.168.10.23", 94, "admin", "admin@123", NvrBrand.Hikvision, "", "http://192.168.10.23:94/Login.htm");
        AddNvr("svll-nvr-03", "RAIPURA CAMPUS_3", "RAIPURA CAMPUS", "NEW HOLLAND", 7, "PITAMBER SAHU", "7000681460", 21, "192.168.10.20", 96, "admin", "admin", NvrBrand.CpPlus, "CP1L0419FPBQ04333", "http://192.168.10.20:96/");
        AddNvr("svll-nvr-04", "PARLE CAMPUS_1", "PARLE CAMPUS", "RAIPUR PARLE CAMPUS", 16, "MANNU SINGH", "7000681360", 36, "192.168.10.27", 80, "admin", "admin@123", NvrBrand.Dahua, "", "http://192.168.10.27", "SmartPSS");
        AddNvr("svll-nvr-05", "BILASPUR PARLE_1", "BILASPUR PARLE", "BSP PARLE BISCUITS", 13, "JWALA", "7000681328", 11, "182.74.0.166", 80, "admin", "netzen@12345", NvrBrand.CpPlus, "CP2L0188DPBQ01007", "http://182.74.0.166:80", "KVMS PRO");
        AddNvr("svll-nvr-06", "BILASPUR PARLE_2", "BILASPUR PARLE", "BSP PARLE AGRO", 8, "RAJESH", "7000681326", 12, "182.74.0.167", 80, "admin", "admin@123", NvrBrand.CpPlus, "2302012438029720", "", "KVMS");
        AddNvr("svll-nvr-07", "TENDUA_1", "TENDUA", "TENDUA CAMPUS", 15, "MEENAKSHI KURRE", "7000681381", 40, "172.16.1.10", 80, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.10/doc/page/login.asp");
        AddNvr("svll-nvr-08", "TENDUA_2", "TENDUA", "RECKIT DEPO", 25, "SHUBHAM", "7000681478", 56, "172.16.2.4", 80, "admin", "admin@12345", NvrBrand.Hikvision, "", "http://172.16.2.4/doc/page/login.asp?_1653127405097");
        AddNvr("svll-nvr-09", "TENDUA_3", "TENDUA", "ADORE DEPO", 26, "TARENDRA", "7000681336", 45, "172.16.1.3", 80, "admin", "admin@321", NvrBrand.Hikvision, "", "http://172.16.1.3/doc/page/login.asp?_1601349549332");
        AddNvr("svll-nvr-10", "TENDUA_4", "TENDUA", "GODREJ / COLGATE / VST / KELLOGG", 24, "DULESH / SANJU / DIPESH", "7000681433 / 7000681476 / 7000681504", 25, "172.16.1.5", 80, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.5/doc/page/preview.asp");
        AddNvr("svll-nvr-11", "TENDUA_5", "TENDUA", "JK TYRE / JYOTHY", 20, "DHEERAJ / BHUPENDRA SAHU", "7000681488 / 7000681467", 56, "172.16.1.6", 80, "admin", "netzen@12345", NvrBrand.CpPlus, "", "http://172.16.1.6/cgi-bin/main-cgi");
        AddNvr("svll-nvr-12", "TENDUA_6", "TENDUA", "PERFETTI / MOTHERDAIRY", 8, "AKHILESH KUMAR / TRILOK SHARMA", "7000681332 / 7000681483", 78, "172.16.1.7", 80, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.7/doc/page/preview.asp");
        AddNvr("svll-nvr-13", "JABALPUR_1", "JABALPUR", "JABALPUR GARAGE", 6, "AMAR SINGH", "8770898250", 40, "192.168.1.13", 80, "admin", "admin@12345", NvrBrand.CpPlus, "2207012437072880", "", "KVMS");
        AddNvr("svll-nvr-14", "BILASPUR_1", "BILASPUR", "BILASPUR GARAGE", 2, "UMESH DINKAR", "7000197776", 82, "192.168.1.14", 80, "admin", "admin", NvrBrand.CpPlus, "CP2J009A2PBQ00592", "", "KVMS");
        AddNvr("svll-nvr-15", "NAGPUR_1", "NAGPUR", "NAGPUR GARAGE", 4, "HANSHRAJ", "7828900341", 263, "192.168.1.15", 80, "itexe@shreevasulogistics.com", "Squad@321", NvrBrand.Hikvision, "c36648731", "", "iVMS (SN: C36648731)");
        AddNvr("svll-nvr-16", "AMBIKAPUR_1", "AMBIKAPUR", "AMBIKAPUR GARAGE", 4, "DIPESH SAHU", "8770898245", 15, "192.168.1.16", 80, "itexe@shreevasulogistics.com", "Squad@321", NvrBrand.Hikvision, "27121984", "", "iVMS (SN: D90344913)");
        AddNvr("svll-nvr-17", "ORISSA_1", "ORISSA", "KATABHANJI GARAGE", 4, "AJIT DIGAL", "6260877007", 25, "192.168.100.100", 80, "admin", "admin@123", NvrBrand.Dahua, "6GC9B178PA504566", "", "GDMSS (192.168.100.100)");
        AddNvr("svll-nvr-18", "DABUR_1", "DABUR", "DABUR DEPOT", 30, "MANOJ SUTAR", "6370711379", 25, "172.16.1.8", 80, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.8/doc/page/login.asp");
        AddNvr("svll-nvr-19", "PIDILITE_1", "PIDILITE", "PIDILITE DEPO", 30, "DUSHYANT SAHU", "7000681466", 18, "172.16.1.9", 80, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.9/doc/page/preview.asp");
        AddNvr("svll-nvr-20", "TENDUA_7", "TENDUA", "SYNGENTA DEPO - 01", 16, "NAVEEN KALVEEJ", "99814 32985", 37, "172.16.1.202", 80, "admin", "admin@321", NvrBrand.Hikvision, "", "http://172.16.1.202");
        AddNvr("svll-nvr-21", "TENDUA_8", "TENDUA", "SYNGENTA DEPO - 02", 8, "NAVEEN KALVEEJ", "99814 32985", 28, "172.16.2.202", 80, "admin", "admin@123", NvrBrand.Hikvision, "", "http://172.16.2.202");
        AddNvr("svll-nvr-22", "TENDUA_9", "TENDUA", "JOCKEY DEPO", 23, "SATYAM PATEL", "6268297031", 24, "192.168.11.135", 80, "admin", "vasu@12345", NvrBrand.Hikvision, "", "http://192.168.11.135/doc/page/login.asp?_1790839618514");
        AddNvr("svll-nvr-23", "TENDUA_10", "TENDUA", "KIMBERLY", 8, "PARASH", "7000681519", 24, "172.16.1.227", 85, "admin", "netzen@12345", NvrBrand.Hikvision, "", "http://172.16.1.227:85/doc/page/login.asp?_1700909626345");
        AddNvr("svll-nvr-24", "BARGARH_1", "BARGARH", "BARGARH", 1, "DINESH", "6370142296", 19, "192.168.1.24", 80, "it2@logisticpark.biz", "Svll@2021", NvrBrand.Dahua, "", "", "Imou Camera");
        AddNvr("svll-nvr-25", "JAGDALPUR_1", "JAGDALPUR", "JAGDALPUR GARAGE", 2, "ANKUR SHARMA", "9407620504", 20, "192.168.1.25", 80, "admin", "admin123", NvrBrand.CpPlus, "CP2J01FF6PBQ09872", "", "KVMS Pro");
        AddNvr("svll-nvr-26", "TENDUA CAMPUS_1", "TENDUA CAMPUS", "JIVAGARO / PI DEPO", 9, "DINESH SAHU / GURAV KAUSHAL", "7000681496", 30, "172.16.1.26", 80, "admin", "netzen@12", NvrBrand.Hikvision, "", "");
        AddNvr("svll-nvr-27", "TENDUA CAMPUS_2", "TENDUA CAMPUS", "NEW OFFICE BUILDING", 32, "LEENA SINHASAR", "7000681419", 16, "192.168.11.100", 80, "admin", "office@123", NvrBrand.Hikvision, "", "http://192.168.11.100/doc/page/login.asp?_1698400299531");
        AddNvr("svll-nvr-28", "SARONA_1", "SARONA", "SARONA CAMPUS", 13, "SATISH SAHU", "7000681485", 24, "192.168.11.240", 80, "admin", "iview@123", NvrBrand.Uniview, "", "http://192.168.11.240/doc/page/login.asp");
        AddNvr("svll-nvr-29", "Tendua campus_4", "Tendua campus", "CONFERENCE HALL", 1, "LEENA SINHASAR", "7000681419", 26, "192.168.11.241", 80, "itexe@shreevasulogistics.com", "Squad@123", NvrBrand.Hikvision, "", "", "Ezviz Camera");
        AddNvr("svll-nvr-30", "Tendua campus_5", "Tendua campus", "PENTRY", 1, "LEENA SINHASAR", "7000681419", 26, "192.168.11.242", 80, "itexe@shreevasulogistics.com", "Squad@123", NvrBrand.Hikvision, "", "", "Ezviz Camera");
        AddNvr("svll-nvr-31", "Tendua Campus_6", "Tendua Campus", "ORIENT DEPO", 8, "TILAK RAM SAHU", "7000681425", 10, "172.16.1.31", 80, "admin", "Orient@1311", NvrBrand.CpPlus, "2111010390001600", "");
        AddNvr("svll-nvr-32", "Raigarh Garage_1", "Raigarh Garage", "RAIGARH GARAGE", 3, "KISHAN JAISHAWAL", "6266544067", 12, "192.168.1.32", 80, "admin", "admin123", NvrBrand.CpPlus, "2310013063013120", "");
        AddNvr("svll-nvr-33", "Rajnand Goan_1", "Rajnand Goan", "RAJNAND GAON GARAGE", 4, "HEMANT SONKAR", "9977308310", 12, "192.168.1.33", 80, "admin", "admin123", NvrBrand.CpPlus, "2402013063029970", "");
        AddNvr("svll-nvr-34", "Jeypore_1", "Jeypore", "JEYPORE GARAGE", 4, "VINEET MISHRA", "9078065387", 12, "192.168.1.34", 80, "admin", "admin@123", NvrBrand.CpPlus, "2402013063029880", "", "KVMS Software");
        AddNvr("svll-nvr-35", "Tendua_11", "Tendua", "SECURITY TENDUA", 4, "SURAJ SEN", "7000681359", 30, "192.168.10.28", 80, "admin", "admin@123", NvrBrand.CpPlus, "", "");
        AddNvr("svll-nvr-36", "Tendua_12", "Tendua", "EICHER DEPO", 9, "TILAK SAHU", "7000681425", 30, "192.168.1.230", 80, "itexe@shreevasulogistics.com", "Squad!321", NvrBrand.Hikvision, "12345678", "");
        AddNvr("svll-nvr-37", "Raipur Garage_1", "Raipur Garage", "PART LOADING", 3, "RAJU YADAV", "7000681307", 30, "192.168.10.37", 80, "admin", "admin@123", NvrBrand.CpPlus, "", "", "KVMS");
        AddNvr("svll-nvr-38", "Korba_1", "Korba", "KORBA GARAGE", 6, "UMESH TONDON", "7828864495", 90, "192.168.11.200", 80, "utandan428@gmail.com", "admin@12345", NvrBrand.Hikvision, "CBVMKF", "");
        AddNvr("svll-nvr-39", "RAIPUR CAMPUS_1", "TENDUA CAMPUS", "JYOTHY DEPO", 3, "BHUPENDRA", "7000671467", 30, "172.16.3.239", 80, "admin", "admin@12345", NvrBrand.Hikvision, "", "http://172.16.3.239/doc/page/login.asp?_1785735350881&page=preview");
        AddNvr("svll-nvr-40", "OFF_BUILD_1ST_FLR", "TENDUA CAMPUS", "RAIPUR", 10, "LEENA SINHASAR", "7000681419", 90, "192.168.8.6", 80, "admin", "netzen@12", NvrBrand.Hikvision, "", "");
        AddNvr("svll-nvr-41", "BRGR_VIJAYPURA_1", "KARNATAKA", "VIJAYPURA", 14, "MR. PAVVAN KONGARI", "9980022427", 30, "192.168.1.41", 80, "9481671763", "Kartik@2026", NvrBrand.Hikvision, "cctv123", "", "Hik-Connect");
        AddNvr("svll-nvr-42", "PARLE_AGRO_NGP", "MAHARASTRA", "NAGPUR", 10, "ISHWAR LILHARE", "7000681503", 22, "192.168.1.42", 80, "admin", "Appson1@", NvrBrand.CpPlus, "", "", "KVMS Lite");
        AddNvr("svll-nvr-43", "LINK_LOCK_NGP", "MAHARASTRA", "NAGPUR", 14, "ASHUTOSH", "86690 13052", 26, "192.168.1.43", 80, "admin", "Appson1@", NvrBrand.CpPlus, "0EFE4K0GBY3T3LP5", "", "KVMS Lite");
        AddNvr("svll-nvr-44", "CROPNXT_BAYER", "TENDUA CAMPUS", "RAIPUR", 8, "VIRENDRA SAHU", "8964848326", 30, "172.16.1.44", 80, "7000681395", "Shree@321", NvrBrand.Hikvision, "Bayer1234", "", "Hik-Connect");
        AddNvr("svll-nvr-45", "MARICO_RPR", "TENDUA CAMPUS", "RAIPUR", 14, "SHAILENDRA VERMA", "7000681306", 30, "172.16.1.45", 80, "7000681395", "Shree@321", NvrBrand.Hikvision, "Marico123", "", "Hik-Connect");
        AddNvr("svll-nvr-46", "cuttack", "ODISHA", "ODISHA", 4, "ODISHA TEAM", "NA", 23, "192.168.1.46", 80, "admin", "admin123", NvrBrand.CpPlus, "", "", "KVMS Lite");
    }

    public void ExportDevices(string targetPath)
    {
        lock (_lock)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(_devices, options);
            File.WriteAllText(targetPath, json);
        }
    }

    public bool ImportDevices(string sourcePath)
    {
        try
        {
            if (!File.Exists(sourcePath)) return false;
            string json = File.ReadAllText(sourcePath);
            var list = JsonSerializer.Deserialize<List<NvrDevice>>(json);
            if (list == null || list.Count == 0) return false;

            lock (_lock)
            {
                foreach (var d in list)
                {
                    int idx = _devices.FindIndex(x => x.Id == d.Id);
                    if (idx >= 0) _devices[idx] = d;
                    else _devices.Add(d);
                }
                Persist();
            }
            DevicesUpdated?.Invoke();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string EncryptString(string plain)
    {
        try
        {
            byte[] plainBytes = Encoding.UTF8.GetBytes(plain);
            byte[] encrypted = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
            return "DPAPI:" + Convert.ToBase64String(encrypted);
        }
        catch
        {
            return "B64:" + Convert.ToBase64String(Encoding.UTF8.GetBytes(plain));
        }
    }

    private static string DecryptString(string encrypted)
    {
        try
        {
            if (encrypted.StartsWith("DPAPI:"))
            {
                byte[] encBytes = Convert.FromBase64String(encrypted.Substring(6));
                byte[] plainBytes = ProtectedData.Unprotect(encBytes, null, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(plainBytes);
            }
            else if (encrypted.StartsWith("B64:"))
            {
                byte[] bytes = Convert.FromBase64String(encrypted.Substring(4));
                return Encoding.UTF8.GetString(bytes);
            }
            return encrypted;
        }
        catch
        {
            return encrypted;
        }
    }
}
