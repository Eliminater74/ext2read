using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace Ext2Read.WinForms
{
    public class AppSettings
    {
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ext2Read", "settings.json");
        private static AppSettings _instance;

        public static AppSettings Instance
        {
            get
            {
                if (_instance == null)
                    _instance = Load();
                return _instance;
            }
        }

        public bool AutoScanOnStartup { get; set; } = true;
        public Dictionary<string, string> Preferences { get; set; } = new Dictionary<string, string>();

        public static string Get(string key, string defaultValue)
        {
            if (Instance.Preferences.TryGetValue(key, out var val)) return val;
            return defaultValue;
        }

        public static void Set(string key, string value)
        {
            Instance.Preferences[key] = value;
            Instance.Save();
        }

        public static AppSettings Load()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        public void Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
