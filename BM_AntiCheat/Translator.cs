using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace BM_AntiCheat;
public static class Translator
{
    public static Dictionary<string, Dictionary<int, string>> Translations = new();

    public static void Initialize()
    {
        LoadLanguageFiles();
    }
    private static void LoadLanguageFiles()
    {
        var assembly = Assembly.GetExecutingAssembly();

        var resourceNames = assembly.GetManifestResourceNames()
                                    .Where(n => n.EndsWith(".json") && n.Contains(".Language."));

        foreach (var resourceName in resourceNames)
        {
            try
            {
                using (var stream = assembly.GetManifestResourceStream(resourceName))
                using (var reader = new StreamReader(stream))
                {
                    var json = reader.ReadToEnd();
                    var dict = JsonSerializer.Deserialize<Dictionary<string, string>>(json);

                    if (!dict.TryGetValue("LanguageID", out string langIdStr) || !int.TryParse(langIdStr, out int langId))
                    {
                        main.Logger.LogWarning($"[Translator] LanguageID mancante o invalido nel file {resourceName}");
                        continue;
                    }

                    dict.Remove("LanguageID");

                    foreach (var kvp in dict)
                    {
                        if (!Translations.ContainsKey(kvp.Key))
                            Translations[kvp.Key] = new Dictionary<int, string>();

                        Translations[kvp.Key][langId] = kvp.Value.Replace("\\n", "\n");
                    }
                }
            }
            catch (Exception ex)
            {
                main.Logger.LogWarning($"[Translator] Errore caricando risorsa {resourceName}: {ex}");
            }
        }
    }

    public static string Get(string key, int languageId)
    {
        if (Translations.TryGetValue(key, out var langs))
        {
            if (langs.TryGetValue(languageId, out var translation) && !string.IsNullOrWhiteSpace(translation))
            {
                return translation;
            }
            else
            {
                main.Logger.LogWarning($"[Translator] Traduzione mancante per chiave '{key}' e lingua ID '{languageId}'.");
            }
        }
        else
        {
            main.Logger.LogWarning($"[Translator] Chiave '{key}' non trovata nel dizionario delle traduzioni.");
        }

        return $"*{key}"; // Fallback
    }

    public static SupportedLangs GetUserTrueLang()
    {
        try
        {
            var name = CultureInfo.CurrentUICulture.Name.ToLower();

            if (name.StartsWith("en")) return SupportedLangs.English;
            if (name.StartsWith("pt-br")) return SupportedLangs.Brazilian;
            if (name.StartsWith("pt")) return SupportedLangs.Portuguese;
            if (name.StartsWith("ko")) return SupportedLangs.Korean;
            if (name.StartsWith("ru")) return SupportedLangs.Russian;
            if (name.StartsWith("nl")) return SupportedLangs.Dutch;
            if (name.StartsWith("fil")) return SupportedLangs.Filipino;
            if (name.StartsWith("fr")) return SupportedLangs.French;
            if (name.StartsWith("de")) return SupportedLangs.German;
            if (name.StartsWith("it")) return SupportedLangs.Italian;
            if (name.StartsWith("ja")) return SupportedLangs.Japanese;
            if (name.StartsWith("es")) return SupportedLangs.Spanish;
            if (name.StartsWith("zh-chs")) return SupportedLangs.SChinese;
            if (name.StartsWith("zh-cht")) return SupportedLangs.TChinese;
            if (name.StartsWith("ga")) return SupportedLangs.Irish;

            return SupportedLangs.English;
        }
        catch
        {
            return SupportedLangs.English;
        }
    }

    public static string GetAuto(string key)
    {
        int langId = (int)GetUserTrueLang();
        return Get(key, langId);
    }

    public static string GetAuto(string key, params object[] args)
    {
        string raw = GetAuto(key);
        return string.Format(raw, args);
    }

}
