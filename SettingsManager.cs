using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Media;

namespace AutoClicker
{
    /// <summary>Сериализация настроек и пользовательских тем.</summary>
    public static class SettingsManager
    {
        private const string SettingsFileName = "autoclicker_settings.json";
        private const string ThemeFolderName = "theme_configs";

        private static readonly JsonSerializerSettings JsonSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
            Converters = { new ColorJsonConverter() },
            ObjectCreationHandling = ObjectCreationHandling.Replace
        };

        private static string _dataDirectory;

        public static string AppDirectory
        {
            get
            {
                try
                {
                    string exe = Assembly.GetExecutingAssembly().Location;
                    if (!string.IsNullOrEmpty(exe)) return Path.GetDirectoryName(exe);
                }
                catch { }
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        /// <summary>
        /// Папка для данных: рядом с exe (portable), а если она недоступна
        /// для записи — %APPDATA%\AutoClickerPro.
        /// </summary>
        public static string DataDirectory
        {
            get
            {
                if (_dataDirectory != null) return _dataDirectory;
                _dataDirectory = ResolveDataDirectory();
                return _dataDirectory;
            }
        }

        private static string ResolveDataDirectory()
        {
            try
            {
                string appDir = AppDirectory;
                Directory.CreateDirectory(appDir);
                string probe = Path.Combine(appDir, ".write_probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return appDir;
            }
            catch
            {
                try
                {
                    string fallback = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AutoClickerPro");
                    Directory.CreateDirectory(fallback);
                    return fallback;
                }
                catch
                {
                    return AppDirectory;
                }
            }
        }

        public static string SettingsPath => Path.Combine(DataDirectory, SettingsFileName);
        public static string ThemeDirectory => Path.Combine(DataDirectory, ThemeFolderName);

        // === Основные настройки ===

        public static AppSettings Load()
        {
            try
            {
                MigrateOldFiles();
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    var settings = JsonConvert.DeserializeObject<AppSettings>(json, JsonSettings);
                    if (settings != null) return settings;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Load error: {ex.Message}");
            }
            return new AppSettings();
        }

        public static void Save(AppSettings settings)
        {
            if (settings == null) return;
            try
            {
                Directory.CreateDirectory(AppDirectory);
                File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings, JsonSettings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Save error: {ex.Message}");
            }
        }

        public static bool Export(AppSettings settings, string path)
        {
            try
            {
                File.WriteAllText(path, JsonConvert.SerializeObject(settings, JsonSettings));
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Export error: {ex.Message}");
                return false;
            }
        }

        public static AppSettings Import(string path)
        {
            try
            {
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<AppSettings>(File.ReadAllText(path), JsonSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Settings] Import error: {ex.Message}");
                return null;
            }
        }

        /// <summary>Старые версии писали файлы рядом с рабочей папкой — переносим их.</summary>
        private static void MigrateOldFiles()
        {
            try
            {
                var sources = new[] { Environment.CurrentDirectory, AppDirectory }
                    .Where(dir => !string.IsNullOrWhiteSpace(dir))
                    .Distinct(StringComparer.OrdinalIgnoreCase);

                foreach (var dir in sources)
                {
                    string oldSettings = Path.Combine(dir, SettingsFileName);
                    if (!File.Exists(SettingsPath) && File.Exists(oldSettings) && !PathsEqual(oldSettings, SettingsPath))
                        File.Copy(oldSettings, SettingsPath, false);

                    string oldThemes = Path.Combine(dir, ThemeFolderName);
                    if (Directory.Exists(oldThemes) && !PathsEqual(oldThemes, ThemeDirectory))
                    {
                        Directory.CreateDirectory(ThemeDirectory);
                        foreach (var file in Directory.GetFiles(oldThemes, "*.json"))
                        {
                            string target = Path.Combine(ThemeDirectory, Path.GetFileName(file));
                            if (!File.Exists(target)) File.Copy(file, target, false);
                        }
                    }
                }
            }
            catch { /* миграция не критична */ }
        }

        private static bool PathsEqual(string a, string b)
            => string.Equals(Path.GetFullPath(a).TrimEnd('\\'), Path.GetFullPath(b).TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);

        // === Пользовательские темы ===

        public static void SaveThemeConfig(ThemeConfig config)
        {
            try
            {
                if (config == null || string.IsNullOrWhiteSpace(config.Name)) return;
                Directory.CreateDirectory(ThemeDirectory);
                File.WriteAllText(ThemePath(config.Name), JsonConvert.SerializeObject(config, JsonSettings));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] Save error: {ex.Message}");
            }
        }

        public static ThemeConfig LoadThemeConfig(string name)
        {
            try
            {
                string path = ThemePath(name);
                if (!File.Exists(path)) return null;
                return JsonConvert.DeserializeObject<ThemeConfig>(File.ReadAllText(path), JsonSettings);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] Load error: {ex.Message}");
                return null;
            }
        }

        public static void DeleteThemeConfig(string name)
        {
            try
            {
                string path = ThemePath(name);
                if (File.Exists(path)) File.Delete(path);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Theme] Delete error: {ex.Message}");
            }
        }

        public static List<string> GetSavedThemeConfigs()
        {
            try
            {
                if (!Directory.Exists(ThemeDirectory)) return new List<string>();
                return Directory.GetFiles(ThemeDirectory, "*.json")
                    .Select(Path.GetFileNameWithoutExtension)
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .OrderBy(n => n, StringComparer.CurrentCultureIgnoreCase)
                    .ToList();
            }
            catch
            {
                return new List<string>();
            }
        }

        private static string ThemePath(string name)
        {
            string safe = new string((name ?? "My theme").Where(c => !Path.GetInvalidFileNameChars().Contains(c)).ToArray()).Trim();
            if (string.IsNullOrWhiteSpace(safe)) safe = "My theme";
            return Path.Combine(ThemeDirectory, safe + ".json");
        }
    }

    /// <summary>Аккуратно пишет Color как "#AARRGGBB" и читает обратно.</summary>
    public class ColorJsonConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType) => objectType == typeof(Color);

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            var color = (Color)value;
            writer.WriteValue($"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}");
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null) return Colors.Transparent;

            if (reader.TokenType == JsonToken.StartObject)
            {
                // Старый формат системного сериализатора: {"A":255,"R":..}
                var obj = JObject.Load(reader);
                return Color.FromArgb(GetByte(obj, "A"), GetByte(obj, "R"), GetByte(obj, "G"), GetByte(obj, "B"));
            }

            string text = reader.Value?.ToString();
            if (string.IsNullOrWhiteSpace(text)) return Colors.Transparent;

            try
            {
                return (Color)ColorConverter.ConvertFromString(text);
            }
            catch
            {
                return Colors.Transparent;
            }
        }

        private static byte GetByte(JObject obj, string name)
        {
            var token = obj[name];
            if (token == null) return (byte)0;
            try { return token.Value<byte>(); } catch { return (byte)0; }
        }
    }
}
