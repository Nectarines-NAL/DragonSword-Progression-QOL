using System.Globalization;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace DragonSword.ProgressionQoL;

internal sealed record LanguageOption(
    string Locale,
    string NativeName,
    string EnglishName,
    string ReviewStatus,
    string FontFamily)
{
    public override string ToString() => $"{NativeName} — {EnglishName}";
}

internal sealed class LocalizationService
{
    private const int SchemaVersion = 1;
    private const long MaximumLanguageFileBytes = 512 * 1024;
    private const string EmbeddedEnglishName = "DragonSword.ProgressionQoL.Languages.en-US.json";
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private readonly string _languagesDirectory;
    private readonly string _settingsPath;
    private readonly Dictionary<string, LoadedLanguage> _languages = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _english;

    public LocalizationService(string baseDirectory, string? localeOverride = null)
    {
        _languagesDirectory = Path.Combine(Path.GetFullPath(baseDirectory), "Languages");
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _settingsPath = Path.Combine(appData, "nectarines", "DragonSword Progression QOL", "ui-settings.json");

        var embeddedEnglish = LoadEmbeddedEnglish();
        _english = new Dictionary<string, string>(embeddedEnglish.Strings, StringComparer.Ordinal);
        _languages[embeddedEnglish.Locale] = new LoadedLanguage(embeddedEnglish, true);
        LoadExternalLanguages();

        var requested = localeOverride ?? ReadSavedLocale() ?? MapCulture(CultureInfo.CurrentUICulture);
        CurrentLocale = ResolveAvailableLocale(requested) ?? "en-US";
    }

    public string CurrentLocale { get; }
    public string LanguagesDirectory => _languagesDirectory;
    public IReadOnlyList<string> LoadWarnings { get; private set; } = [];

    public LanguageOption CurrentLanguage => _languages[CurrentLocale].Option;

    public IReadOnlyList<LanguageOption> AvailableLanguages => _languages.Values
        .Select(x => x.Option)
        .OrderBy(x => x.Locale.Equals("en-US", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
        .ThenBy(x => x.EnglishName, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public string Text(string key, string? fallback = null)
    {
        if (_languages.TryGetValue(CurrentLocale, out var selected) && selected.Strings.TryGetValue(key, out var localized) && !string.IsNullOrWhiteSpace(localized))
            return localized;
        if (_english.TryGetValue(key, out var english) && !string.IsNullOrWhiteSpace(english))
            return english;
        return fallback ?? key;
    }

    public string Format(string key, string fallback, params (string Name, object? Value)[] values)
    {
        var result = Text(key, fallback);
        foreach (var (name, value) in values)
            result = result.Replace("{" + name + "}", Convert.ToString(value, CultureInfo.CurrentCulture) ?? "", StringComparison.Ordinal);
        return result;
    }

    public void SaveLocale(string locale)
    {
        var resolved = ResolveAvailableLocale(locale) ?? throw new ArgumentException("The selected language pack is unavailable.", nameof(locale));
        var directory = Path.GetDirectoryName(_settingsPath)!;
        Directory.CreateDirectory(directory);
        var json = JsonSerializer.Serialize(new UiSettings { Language = resolved }, JsonOptions);
        var temporary = _settingsPath + ".tmp";
        File.WriteAllText(temporary, json, new UTF8Encoding(false));
        File.Move(temporary, _settingsPath, true);
    }

    public IReadOnlyList<string> ValidateLanguageFiles()
    {
        var issues = new List<string>();
        foreach (var language in _languages.Values.OrderBy(x => x.Option.Locale, StringComparer.OrdinalIgnoreCase))
        {
            var missing = _english.Keys.Where(x => !language.Strings.ContainsKey(x)).Order(StringComparer.Ordinal).ToArray();
            var unknown = language.Strings.Keys.Where(x => !_english.ContainsKey(x)).Order(StringComparer.Ordinal).ToArray();
            if (missing.Length > 0)
                issues.Add($"{language.Option.Locale}: {missing.Length} missing key(s): {string.Join(", ", missing)}");
            if (unknown.Length > 0)
                issues.Add($"{language.Option.Locale}: {unknown.Length} unknown key(s): {string.Join(", ", unknown)}");
        }
        issues.AddRange(LoadWarnings);
        return issues;
    }

    private void LoadExternalLanguages()
    {
        var warnings = new List<string>();
        if (!Directory.Exists(_languagesDirectory))
        {
            LoadWarnings = [$"Languages folder not found: {_languagesDirectory}. Embedded English fallback is active."];
            return;
        }

        foreach (var path in Directory.EnumerateFiles(_languagesDirectory, "*.json", SearchOption.TopDirectoryOnly).Order(StringComparer.OrdinalIgnoreCase))
        {
            try
            {
                var file = new FileInfo(path);
                if (file.Length > MaximumLanguageFileBytes)
                    throw new InvalidDataException($"file exceeds {MaximumLanguageFileBytes:N0} bytes");
                var document = Deserialize(File.ReadAllText(path, Encoding.UTF8), Path.GetFileName(path));
                var external = new LoadedLanguage(document, false);
                if (document.Locale.Equals("en-US", StringComparison.OrdinalIgnoreCase))
                {
                    foreach (var pair in external.Strings) _english[pair.Key] = pair.Value;
                    _languages["en-US"] = external;
                }
                else if (!_languages.TryAdd(document.Locale, external))
                {
                    warnings.Add($"Duplicate locale {document.Locale} ignored: {Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                warnings.Add($"Language file ignored ({Path.GetFileName(path)}): {ex.Message}");
            }
        }
        LoadWarnings = warnings;
    }

    private static LanguageDocument LoadEmbeddedEnglish()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(EmbeddedEnglishName)
            ?? throw new InvalidOperationException("The embedded English language source is missing.");
        using var reader = new StreamReader(stream, Encoding.UTF8, true);
        return Deserialize(reader.ReadToEnd(), "embedded en-US.json");
    }

    private static LanguageDocument Deserialize(string json, string source)
    {
        var document = JsonSerializer.Deserialize<LanguageDocument>(json, JsonOptions)
            ?? throw new InvalidDataException($"{source} is empty or invalid JSON");
        if (document.SchemaVersion != SchemaVersion)
            throw new InvalidDataException($"unsupported schemaVersion {document.SchemaVersion}");
        if (string.IsNullOrWhiteSpace(document.Locale) || document.Locale.Length > 32)
            throw new InvalidDataException("locale is missing or invalid");
        if (string.IsNullOrWhiteSpace(document.NativeName) || string.IsNullOrWhiteSpace(document.EnglishName))
            throw new InvalidDataException("language names are required");
        if (document.Strings is null || document.Strings.Count == 0)
            throw new InvalidDataException("strings is empty");
        if (document.Strings.Any(x => string.IsNullOrWhiteSpace(x.Key) || x.Key.Length > 120 || x.Value is null || x.Value.Length > 8000))
            throw new InvalidDataException("one or more string entries are invalid");
        return document;
    }

    private string? ReadSavedLocale()
    {
        try
        {
            if (!File.Exists(_settingsPath)) return null;
            var settings = JsonSerializer.Deserialize<UiSettings>(File.ReadAllText(_settingsPath, Encoding.UTF8), JsonOptions);
            return settings?.Language;
        }
        catch
        {
            return null;
        }
    }

    private string? ResolveAvailableLocale(string? locale)
    {
        if (string.IsNullOrWhiteSpace(locale)) return null;
        if (_languages.ContainsKey(locale)) return _languages.Keys.First(x => x.Equals(locale, StringComparison.OrdinalIgnoreCase));
        var mapped = MapCultureName(locale);
        return mapped is not null && _languages.ContainsKey(mapped) ? mapped : null;
    }

    private static string? MapCulture(CultureInfo culture) => MapCultureName(culture.Name);

    private static string? MapCultureName(string cultureName)
    {
        var value = cultureName.Replace('_', '-').ToLowerInvariant();
        if (value is "zh-hant" or "zh-tw" or "zh-hk" or "zh-mo" || value.StartsWith("zh-hant-", StringComparison.Ordinal)) return "zh-TW";
        if (value.StartsWith("zh", StringComparison.Ordinal)) return "zh-CN";
        if (value.StartsWith("ja", StringComparison.Ordinal)) return "ja-JP";
        if (value.StartsWith("ko", StringComparison.Ordinal)) return "ko-KR";
        if (value.StartsWith("fr", StringComparison.Ordinal)) return "fr-FR";
        if (value.StartsWith("de", StringComparison.Ordinal)) return "de-DE";
        if (value.StartsWith("es", StringComparison.Ordinal)) return "es-ES";
        if (value.StartsWith("ru", StringComparison.Ordinal)) return "ru-RU";
        if (value.StartsWith("th", StringComparison.Ordinal)) return "th-TH";
        if (value.StartsWith("pt", StringComparison.Ordinal)) return "pt-BR";
        if (value.StartsWith("en", StringComparison.Ordinal)) return "en-US";
        return null;
    }

    private sealed class LoadedLanguage
    {
        public LoadedLanguage(LanguageDocument document, bool embedded)
        {
            Strings = new Dictionary<string, string>(document.Strings, StringComparer.Ordinal);
            Option = new LanguageOption(
                document.Locale,
                document.NativeName,
                document.EnglishName,
                document.ReviewStatus ?? (embedded ? "source" : "unreviewed"),
                string.IsNullOrWhiteSpace(document.FontFamily) ? "Segoe UI" : document.FontFamily);
        }

        public Dictionary<string, string> Strings { get; }
        public LanguageOption Option { get; }
    }

    private sealed class LanguageDocument
    {
        public int SchemaVersion { get; set; }
        public string Locale { get; set; } = "";
        public string NativeName { get; set; } = "";
        public string EnglishName { get; set; } = "";
        public string? ReviewStatus { get; set; }
        public string? TranslatorCredit { get; set; }
        public string? FontFamily { get; set; }
        public Dictionary<string, string> Strings { get; set; } = [];
    }

    private sealed class UiSettings
    {
        public string Language { get; set; } = "en-US";
    }
}
