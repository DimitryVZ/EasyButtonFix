using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;

namespace EasyButtonFix.Core
{
    /// <summary>Einfache INI-Datei unter %APPDATA%\EasyButtonFix.</summary>
    public static class Settings
    {
        static string Dir => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EasyButtonFix");
        static string FilePath => Path.Combine(Dir, "settings.ini");

        public static bool TrayHintShown;
        /// <summary>"auto", "de" oder "en"</summary>
        public static string Language = "auto";

        public static void Load(MouseFilter f)
        {
            try
            {
                if (!File.Exists(FilePath)) return;
                var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var line in File.ReadAllLines(FilePath))
                {
                    int i = line.IndexOf('=');
                    if (i > 0) map[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
                }
                if (map.TryGetValue("Active", out var a)) f.Active = a == "1";
                if (map.TryGetValue("TrayHintShown", out var t)) TrayHintShown = t == "1";
                if (map.TryGetValue("Language", out var lang) && (lang == "de" || lang == "en" || lang == "auto")) Language = lang;
                foreach (var b in f.All)
                {
                    if (map.TryGetValue(b.Key + ".Enabled", out var v)) b.Enabled = v == "1";
                    if (map.TryGetValue(b.Key + ".Ms", out v) && int.TryParse(v, out var ms)) b.ThresholdMs = ms;
                    if (map.TryGetValue(b.Key + ".Blocked", out v) && int.TryParse(v, out var n)) b.blocked = n;
                }
            }
            catch { /* kaputte Datei -> Standardwerte */ }
        }

        public static void Save(MouseFilter f)
        {
            try
            {
                Directory.CreateDirectory(Dir);
                var lines = new List<string>
                {
                    "Active=" + (f.Active ? "1" : "0"),
                    "TrayHintShown=" + (TrayHintShown ? "1" : "0"),
                    "Language=" + Language
                };
                foreach (var b in f.All)
                {
                    lines.Add(b.Key + ".Enabled=" + (b.Enabled ? "1" : "0"));
                    lines.Add(b.Key + ".Ms=" + b.ThresholdMs);
                    lines.Add(b.Key + ".Blocked=" + b.BlockedCount);
                }
                File.WriteAllLines(FilePath, lines);
            }
            catch { }
        }

        // ---------------- Autostart ----------------
        const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        const string RunName = "EasyButtonFix";

        public static bool Autostart
        {
            get
            {
                try { using (var k = Registry.CurrentUser.OpenSubKey(RunKey)) return k?.GetValue(RunName) != null; }
                catch { return false; }
            }
            set
            {
                using (var k = Registry.CurrentUser.OpenSubKey(RunKey, true))
                {
                    if (k == null) return;
                    if (value) k.SetValue(RunName, "\"" + System.Reflection.Assembly.GetEntryAssembly().Location + "\" --tray");
                    else k.DeleteValue(RunName, false);
                }
            }
        }
    }
}
