using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace DragonSword.ProgressionQoL;

internal sealed record ProfileDocument(
    int SchemaVersion,
    string Name,
    string SavedAtUtc,
    string ApplicationVersion,
    BuildConfig Settings);

internal sealed record ProfileImportResult(string SourceDirectory, int Imported, int Skipped, int Invalid);

internal sealed class ProfileStore
{
    private const int CurrentSchemaVersion = 1;
    private static readonly int[] GeneralMultipliers = [1, 2, 3, 5, 7, 10, 15, 20];
    private static readonly int[] LimitedMultipliers = [1, 2, 3, 5, 7, 10];
    private static readonly decimal[] RarityChances = [25m, 50m, 75m, 90m, 95m, 99m];
    private static readonly HashSet<string> ReservedNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    private readonly string _legacyProfilesDirectory;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = false,
        WriteIndented = true
    };

    public ProfileStore(string applicationDirectory, string? profilesDirectory = null)
    {
        _legacyProfilesDirectory = Path.Combine(Path.GetFullPath(applicationDirectory), "Profiles");
        var localApplicationData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (string.IsNullOrWhiteSpace(localApplicationData) && profilesDirectory is null)
            throw new DirectoryNotFoundException("Windows did not provide a Local AppData folder for persistent profiles.");
        ProfilesDirectory = Path.GetFullPath(profilesDirectory ?? Path.Combine(localApplicationData, "nectarines", "DragonSword Progression QOL", "Profiles"));
    }

    public string ProfilesDirectory { get; }

    public IReadOnlyList<ProfileDocument> List()
    {
        if (!Directory.Exists(ProfilesDirectory)) return [];

        var profiles = new List<ProfileDocument>();
        foreach (var path in Directory.EnumerateFiles(ProfilesDirectory, "*.json", SearchOption.TopDirectoryOnly))
        {
            try { profiles.Add(Load(path)); }
            catch { /* Invalid files stay visible on disk but are not offered as loadable profiles. */ }
        }
        return profiles.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public string Save(string name, BuildConfig settings)
    {
        name = ValidateName(name);
        ValidateSettings(settings);
        EnsureDirectory();

        var document = new ProfileDocument(
            CurrentSchemaVersion,
            name,
            DateTimeOffset.UtcNow.ToString("O"),
            CurrentApplicationVersion(),
            settings);
        var path = GetManagedPath(name);
        WriteDocument(path, document);
        return path;
    }

    public ProfileDocument Load(string path)
    {
        var document = JsonSerializer.Deserialize<ProfileDocument>(File.ReadAllText(path, Encoding.UTF8), _jsonOptions)
            ?? throw new InvalidDataException("The selected profile is empty or invalid.");
        if (document.SchemaVersion != CurrentSchemaVersion)
            throw new InvalidDataException($"Profile schema {document.SchemaVersion} is unsupported. Expected {CurrentSchemaVersion}.");
        ValidateName(document.Name);
        ValidateSettings(document.Settings);
        return document;
    }

    public ProfileImportResult MigrateLegacyProfiles()
    {
        var sources = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { _legacyProfilesDirectory };
        var applicationDirectory = Path.GetDirectoryName(_legacyProfilesDirectory)!;
        var parent = Directory.GetParent(applicationDirectory);
        if (parent is not null)
        {
            try
            {
                foreach (var sibling in Directory.EnumerateDirectories(parent.FullName, "DragonSword-Progression-QOL-*", SearchOption.TopDirectoryOnly))
                    sources.Add(Path.Combine(sibling, "Profiles"));
            }
            catch { /* Current-folder migration and manual import remain available. */ }
        }

        var imported = 0;
        var skipped = 0;
        var invalid = 0;
        var checkedSources = new List<string>();
        foreach (var source in sources.Where(Directory.Exists).Where(x => !PathsEqual(x, ProfilesDirectory)))
        {
            var result = MigrateSourceOnce(source);
            imported += result.Imported;
            skipped += result.Skipped;
            invalid += result.Invalid;
            checkedSources.Add(result.SourceDirectory);
        }
        return new ProfileImportResult(string.Join("; ", checkedSources), imported, skipped, invalid);
    }

    public ProfileImportResult ImportFromDirectory(string selectedDirectory)
    {
        if (string.IsNullOrWhiteSpace(selectedDirectory)) throw new DirectoryNotFoundException("Select an older application folder or its Profiles folder.");
        var source = Path.GetFullPath(selectedDirectory);
        var nestedProfiles = Path.Combine(source, "Profiles");
        if (Directory.Exists(nestedProfiles)) source = nestedProfiles;
        if (!Directory.Exists(source)) throw new DirectoryNotFoundException("The selected folder does not exist.");
        if (PathsEqual(source, ProfilesDirectory)) return new ProfileImportResult(source, 0, Directory.EnumerateFiles(source, "*.json", SearchOption.TopDirectoryOnly).Count(), 0);

        EnsureDirectory();
        var imported = 0;
        var skipped = 0;
        var invalid = 0;
        foreach (var path in Directory.EnumerateFiles(source, "*.json", SearchOption.TopDirectoryOnly))
        {
            ProfileDocument document;
            try { document = Load(path); }
            catch
            {
                invalid++;
                continue;
            }

            var destination = GetManagedPath(document.Name);
            if (File.Exists(destination))
            {
                skipped++;
                continue;
            }
            WriteDocument(destination, document);
            imported++;
        }
        return new ProfileImportResult(source, imported, skipped, invalid);
    }

    public string GetManagedPath(string name)
    {
        name = ValidateName(name);
        var root = Path.GetFullPath(ProfilesDirectory).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        var path = Path.GetFullPath(Path.Combine(root, name + ".json"));
        if (!path.StartsWith(root, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The profile path escaped the Profiles folder.");
        return path;
    }

    public void Delete(string name)
    {
        var path = GetManagedPath(name);
        if (!File.Exists(path)) throw new FileNotFoundException("The selected profile no longer exists.", path);
        File.Delete(path);
    }

    public void EnsureDirectory() => Directory.CreateDirectory(ProfilesDirectory);

    private ProfileImportResult MigrateSourceOnce(string source)
    {
        var migrationRoot = Path.Combine(Path.GetDirectoryName(ProfilesDirectory)!, "MigrationMarkers");
        Directory.CreateDirectory(migrationRoot);
        var sourceKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Path.GetFullPath(source).ToUpperInvariant())));
        var marker = Path.Combine(migrationRoot, sourceKey + ".txt");
        if (File.Exists(marker)) return new ProfileImportResult(source, 0, 0, 0);

        var result = ImportFromDirectory(source);
        File.WriteAllText(marker,
            $"Migrated from: {result.SourceDirectory}{Environment.NewLine}Imported: {result.Imported}{Environment.NewLine}Skipped: {result.Skipped}{Environment.NewLine}Invalid: {result.Invalid}{Environment.NewLine}",
            new UTF8Encoding(false));
        return result;
    }

    private void WriteDocument(string path, ProfileDocument document)
    {
        var json = JsonSerializer.Serialize(document, _jsonOptions) + Environment.NewLine;
        var temporaryPath = path + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json, new UTF8Encoding(false));
            File.Move(temporaryPath, path, true);
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }

    private static string CurrentApplicationVersion() =>
        Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown";

    private static bool PathsEqual(string left, string right) =>
        Path.GetFullPath(left).TrimEnd(Path.DirectorySeparatorChar).Equals(Path.GetFullPath(right).TrimEnd(Path.DirectorySeparatorChar), StringComparison.OrdinalIgnoreCase);

    private static string ValidateName(string value)
    {
        var name = value.Trim();
        if (name.Length is < 1 or > 64) throw new InvalidDataException("Profile names must contain 1–64 characters.");
        if (name.Equals("Default", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Default is the built-in vanilla profile. Choose a different name for saved settings.");
        if (name.EndsWith('.') || name.EndsWith(' ')) throw new InvalidDataException("Profile names cannot end with a period or space.");
        if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) throw new InvalidDataException("The profile name contains a character Windows cannot use in a filename.");
        if (ReservedNames.Contains(name)) throw new InvalidDataException("That profile name is reserved by Windows.");
        return name;
    }

    private static void ValidateSettings(BuildConfig settings)
    {
        if (settings is null) throw new InvalidDataException("The profile has no settings block.");
        RequireChoice(nameof(settings.WorldGatheringMultiplier), settings.WorldGatheringMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.EnemyMaterialMultiplier), settings.EnemyMaterialMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.EquipmentMultiplier), settings.EquipmentMultiplier, LimitedMultipliers);
        RequireChoice(nameof(settings.ActivityMaterialMultiplier), settings.ActivityMaterialMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.AdventurerEmblemMultiplier), settings.AdventurerEmblemMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.GoldMultiplier), settings.GoldMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.RankExperienceMultiplier), settings.RankExperienceMultiplier, GeneralMultipliers);
        RequireChoice(nameof(settings.WorldChestMultiplier), settings.WorldChestMultiplier, LimitedMultipliers);
        if (!RarityChances.Contains(settings.HighGradeChance))
            throw new InvalidDataException($"HighGradeChance must be one of: {string.Join(", ", RarityChances)}.");
    }

    private static void RequireChoice(string field, int value, IReadOnlyCollection<int> allowed)
    {
        if (!allowed.Contains(value))
            throw new InvalidDataException($"{field} has unsupported value {value}. Allowed values: {string.Join(", ", allowed)}.");
    }
}
