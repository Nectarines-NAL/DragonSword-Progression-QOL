using System.Diagnostics;
using System.Globalization;
using System.IO.Compression;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace DragonSword.ProgressionQoL;

internal sealed class BuildEngine
{
    private const string PakName = "DS_ZZZ_ProgressionQoL_Configured_P.pak";
    public const string SupportedGameVersion = "1.0.9";
    public const string SupportedSteamBuildId = "24693558";
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly Regex XmlId = new("\\b(?:\\w+:)?ID=\"(\\d+)\"", RegexOptions.Compiled);

    private readonly string _baseDirectory;
    private readonly string _baselineDirectory;
    private readonly string _repakPath;

    public BuildEngine(string baseDirectory)
    {
        _baseDirectory = baseDirectory;
        _baselineDirectory = Path.Combine(baseDirectory, "Baselines");
        _repakPath = Path.Combine(baseDirectory, "Tools", "repak.exe");
    }

    public async Task<BuildResult> BuildAsync(BuildConfig config, string outputRoot, IProgress<string>? progress = null)
    {
        Validate(config);
        RequireFile(_repakPath);
        foreach (var name in new[] { "RewardRandomData.table", "RewardRandomData.xml", "RewardData.table", "RewardData.xml", "PropCollectData.table", "PropCollectData.xml", "GameItemData.table", "BaselineTargets.json" })
            RequireFile(Path.Combine(_baselineDirectory, name));

        var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", Invariant);
        var buildRoot = Path.Combine(Path.GetFullPath(outputRoot), $"ProgressionQoL-{stamp}");
        if (Directory.Exists(buildRoot))
            throw new IOException($"Build directory already exists: {buildRoot}");

        var stagingContent = Path.Combine(buildRoot, "Staging", "DS", "Content");
        var clientDirectory = Path.Combine(stagingContent, "Design", "GameData");
        var serverDirectory = Path.Combine(stagingContent, "__GeneratedGameData__", "Server", "XML", "GameData");
        var packageDirectory = Path.Combine(buildRoot, "Package", "DS", "Content", "Paks");
        Directory.CreateDirectory(clientDirectory);
        Directory.CreateDirectory(serverDirectory);
        Directory.CreateDirectory(packageDirectory);

        progress?.Report("Loading verified baseline tables...");
        var vanillaRandom = LoadObject("RewardRandomData.table");
        var modifiedRandom = (JsonObject)vanillaRandom.DeepClone();
        var vanillaRewardData = LoadObject("RewardData.table");
        var modifiedRewardData = (JsonObject)vanillaRewardData.DeepClone();
        var vanillaCollect = LoadObject("PropCollectData.table");
        var modifiedCollect = (JsonObject)vanillaCollect.DeepClone();
        var gameItems = LoadObject("GameItemData.table");
        var targets = LoadObject("BaselineTargets.json");

        var itemTypes = BuildItemTypeMap(gameItems);
        var activityTargets = ReadRowTargets(targets["activity_reward_rows"]);
        var chestTargets = ReadRowTargets(targets["safe_world_chest_rows"]);
        var protectedRows = activityTargets.Concat(chestTargets).Select(RowKey).ToHashSet(StringComparer.Ordinal);
        var enemyTargets = DeriveEnemyMaterialTargets(vanillaRewardData, vanillaRandom, itemTypes)
            .Where(x => !protectedRows.Contains(RowKey(x))).ToList();
        var gatheringIds = DeriveGatheringTargets(vanillaCollect, itemTypes);

        progress?.Report($"Applying {activityTargets.Count:N0} activity, {enemyTargets.Count:N0} enemy-material, and {chestTargets.Count:N0} chest rules...");
        var activityChanged = 0;
        foreach (var target in activityTargets)
        {
            var multiplier = target.Bucket switch
            {
                "equipment" => config.SpreadRolls ? 1 : config.EquipmentMultiplier,
                "materials" when target.ItemId == 1450701 => config.AdventurerEmblemMultiplier,
                "materials" => config.ActivityMaterialMultiplier,
                "gold" => config.GoldMultiplier,
                "rank_experience" => config.RankExperienceMultiplier,
                _ => 1
            };
            if (multiplier != 1)
            {
                ScaleRewardRow(vanillaRandom, modifiedRandom, target, multiplier);
                activityChanged++;
            }
        }

        var enemyChanged = 0;
        if (config.EnemyMaterialMultiplier != 1)
        {
            foreach (var target in enemyTargets)
            {
                ScaleRewardRow(vanillaRandom, modifiedRandom, target, config.EnemyMaterialMultiplier);
                enemyChanged++;
            }
        }

        var chestChanged = 0;
        if (config.WorldChestMultiplier != 1)
        {
            foreach (var target in chestTargets)
            {
                ScaleRewardRow(vanillaRandom, modifiedRandom, target, config.WorldChestMultiplier);
                chestChanged++;
            }
        }

        var gatheringChanged = ScaleGathering(vanillaCollect, modifiedCollect, gatheringIds, config.WorldGatheringMultiplier);
        if (config.EnhancedRarity)
            ApplyRarity(modifiedRandom, targets["rarity_groups"] as JsonArray ?? [], config.HighGradeChance);

        var spread = config.SpreadRolls && config.EquipmentMultiplier > 1
            ? ApplySpreadRolls(modifiedRandom, modifiedRewardData, activityTargets, config.EquipmentMultiplier)
            : new SpreadResult(0, []);

        var rewardChanged = activityChanged > 0 || enemyChanged > 0 || chestChanged > 0 || config.EnhancedRarity || spread.GeneratedGroups.Count > 0;
        if (!rewardChanged && gatheringChanged == 0)
            throw new InvalidOperationException("Every option is still vanilla. Increase at least one value or enable a reward feature.");

        var packedFiles = new List<string>();
        if (rewardChanged)
        {
            WriteJson(Path.Combine(clientDirectory, "RewardRandomData.table"), modifiedRandom);
            WriteRewardRandomXml(Path.Combine(_baselineDirectory, "RewardRandomData.xml"), modifiedRandom, spread.GeneratedGroups, Path.Combine(serverDirectory, "RewardRandomData.xml"));
            packedFiles.Add("Design/GameData/RewardRandomData.table");
            packedFiles.Add("__GeneratedGameData__/Server/XML/GameData/RewardRandomData.xml");
        }
        if (gatheringChanged > 0)
        {
            WriteJson(Path.Combine(clientDirectory, "PropCollectData.table"), modifiedCollect);
            WritePropCollectXml(Path.Combine(_baselineDirectory, "PropCollectData.xml"), modifiedCollect, Path.Combine(serverDirectory, "PropCollectData.xml"));
            packedFiles.Add("Design/GameData/PropCollectData.table");
            packedFiles.Add("__GeneratedGameData__/Server/XML/GameData/PropCollectData.xml");
        }
        if (spread.TransformedRows > 0)
        {
            WriteJson(Path.Combine(clientDirectory, "RewardData.table"), modifiedRewardData);
            WriteRewardDataXml(Path.Combine(_baselineDirectory, "RewardData.xml"), modifiedRewardData, Path.Combine(serverDirectory, "RewardData.xml"));
            packedFiles.Add("Design/GameData/RewardData.table");
            packedFiles.Add("__GeneratedGameData__/Server/XML/GameData/RewardData.xml");
        }

        progress?.Report("Packing and verifying the Unreal PAK...");
        var pakPath = Path.Combine(packageDirectory, PakName);
        await RunAsync(_repakPath, ["pack", "--version", "V11", "--compression", "Zlib", "--mount-point", "../../../DS/Content/", "--path-hash-seed", "0", "-q", stagingContent, pakPath]);
        var listing = await RunAsync(_repakPath, ["list", pakPath]);
        foreach (var expected in packedFiles)
            if (!listing.Contains(expected, StringComparison.OrdinalIgnoreCase))
                throw new InvalidDataException($"PAK verification failed: {expected} is missing.");

        var verifyDirectory = Path.Combine(buildRoot, "VerifiedUnpack");
        Directory.CreateDirectory(verifyDirectory);
        await RunAsync(_repakPath, ["unpack", "-q", "-o", verifyDirectory, pakPath]);
        foreach (var expected in packedFiles)
        {
            var source = Path.Combine(stagingContent, expected.Replace('/', Path.DirectorySeparatorChar));
            var unpacked = Directory.EnumerateFiles(verifyDirectory, Path.GetFileName(expected), SearchOption.AllDirectories).SingleOrDefault()
                ?? throw new InvalidDataException($"Unpack verification failed for {expected}.");
            if (!CryptographicOperations.FixedTimeEquals(SHA256.HashData(File.ReadAllBytes(source)), SHA256.HashData(File.ReadAllBytes(unpacked))))
                throw new InvalidDataException($"Hash verification failed for {expected}.");
        }

        var sha = Convert.ToHexString(SHA256.HashData(await File.ReadAllBytesAsync(pakPath)));
        var report = new JsonObject
        {
            ["application_version"] = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "unknown",
            ["baseline"] = $"Verified unmodified game {SupportedGameVersion} tables (Steam build {SupportedSteamBuildId})",
            ["generated_at"] = DateTimeOffset.Now.ToString("O", Invariant),
            ["pak_sha256"] = sha,
            ["configuration"] = JsonSerializer.SerializeToNode(config),
            ["counts"] = new JsonObject
            {
                ["gathering_rows"] = gatheringChanged,
                ["enemy_material_rows"] = enemyChanged,
                ["activity_rows"] = activityChanged,
                ["safe_chest_rows"] = chestChanged,
                ["spread_reward_rows"] = spread.TransformedRows,
                ["generated_random_groups"] = spread.GeneratedGroups.Count
            },
            ["packed_files"] = new JsonArray(packedFiles.Select(x => JsonValue.Create(x)).ToArray())
        };
        WriteJson(Path.Combine(buildRoot, "BUILD-REPORT.json"), report);
        File.WriteAllText(Path.Combine(buildRoot, "INSTALL.txt"),
            $"Copy Package\\DS\\Content\\Paks\\{PakName} to your game's DS\\Content\\Paks folder.\r\n" +
            "Disable other mods that edit RewardRandomData, RewardData, or PropCollectData. DS_TreasureRespawn.pak does not overlap these paths.\r\n", new UTF8Encoding(false));

        try { Directory.Delete(Path.Combine(buildRoot, "Staging"), true); Directory.Delete(verifyDirectory, true); } catch { /* Build remains valid if cleanup is blocked. */ }
        progress?.Report("Build complete and hash-verified.");
        return new BuildResult(pakPath, sha, packedFiles.Count, gatheringChanged, enemyChanged, activityChanged, chestChanged, spread.TransformedRows, spread.GeneratedGroups.Count, packedFiles);
    }

    public string Install(BuildResult result, string gameRoot)
    {
        var fullRoot = ResolveGameRoot(gameRoot) ?? throw new DirectoryNotFoundException("DragonSword could not be found from that selection. Select the game folder, DS, Paks, ~mods, Win64, or DSClient-Win64-Shipping.exe.");
        var executable = Path.Combine(fullRoot, "DS", "Binaries", "Win64", "DSClient-Win64-Shipping.exe");
        if (!File.Exists(executable)) throw new DirectoryNotFoundException("That folder does not contain DSClient-Win64-Shipping.exe.");
        if (Process.GetProcessesByName("DSClient-Win64-Shipping").Length > 0 || Process.GetProcessesByName("DSClient").Length > 0)
            throw new InvalidOperationException("Close DragonSword before installing the PAK.");
        var installedBuild = ReadInstalledSteamBuildId(fullRoot);
        if (installedBuild is null)
            throw new InvalidOperationException($"Steam build could not be verified. This release only installs automatically on DragonSword {SupportedGameVersion} (Steam build {SupportedSteamBuildId}).");
        if (!installedBuild.Equals(SupportedSteamBuildId, StringComparison.Ordinal))
            throw new InvalidOperationException($"Unsupported DragonSword build {installedBuild}. This release requires game {SupportedGameVersion} (Steam build {SupportedSteamBuildId}); update the game or use the matching configurator release.");

        var modDirectory = Path.Combine(fullRoot, "DS", "Content", "Paks");
        Directory.CreateDirectory(modDirectory);
        var destination = Path.Combine(modDirectory, PakName);
        var backupDirectory = Path.Combine(modDirectory, "ProgressionQoL-Backups");
        Directory.CreateDirectory(backupDirectory);

        // Backups must not retain a loadable .pak extension. This also safely
        // migrates backups produced by early prototypes of this configurator.
        foreach (var legacyBackup in Directory.EnumerateFiles(
                     backupDirectory,
                     $"{Path.GetFileNameWithoutExtension(PakName)}-*.pak",
                     SearchOption.TopDirectoryOnly))
        {
            File.Move(legacyBackup, UniqueDisabledBackupPath(legacyBackup));
        }

        if (File.Exists(destination))
        {
            var backupStem = Path.Combine(backupDirectory, $"{Path.GetFileNameWithoutExtension(PakName)}-{DateTime.Now:yyyyMMdd-HHmmss}.pak");
            var backup = UniqueDisabledBackupPath(backupStem);
            File.Move(destination, backup);
        }
        File.Copy(result.PakPath, destination, overwrite: false);
        var installedSha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(destination)));
        if (!installedSha.Equals(result.Sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("The installed PAK failed SHA-256 verification. Do not launch the game with this file.");
        return destination;
    }

    public string? DisableInstalledPakForVanilla(string gameRoot)
    {
        var fullRoot = ResolveGameRoot(gameRoot) ?? throw new DirectoryNotFoundException("DragonSword could not be found from that selection. Select the game folder, DS, Paks, ~mods, Win64, or DSClient-Win64-Shipping.exe.");
        var executable = Path.Combine(fullRoot, "DS", "Binaries", "Win64", "DSClient-Win64-Shipping.exe");
        if (!File.Exists(executable)) throw new DirectoryNotFoundException("That folder does not contain DSClient-Win64-Shipping.exe.");
        if (Process.GetProcessesByName("DSClient-Win64-Shipping").Length > 0 || Process.GetProcessesByName("DSClient").Length > 0)
            throw new InvalidOperationException("Close DragonSword before restoring vanilla rewards.");

        var pakDirectory = Path.Combine(fullRoot, "DS", "Content", "Paks");
        var installedPak = Path.Combine(pakDirectory, PakName);
        if (!File.Exists(installedPak)) return null;

        var backupDirectory = Path.Combine(pakDirectory, "ProgressionQoL-Backups");
        Directory.CreateDirectory(backupDirectory);
        var backupStem = Path.Combine(backupDirectory, $"{Path.GetFileNameWithoutExtension(PakName)}-{DateTime.Now:yyyyMMdd-HHmmss}-vanilla-restore.pak");
        var backup = UniqueDisabledBackupPath(backupStem);
        File.Move(installedPak, backup);
        return backup;
    }

    public static string? FindInstalledPak(string gameRoot)
    {
        var fullRoot = ResolveGameRoot(gameRoot);
        if (fullRoot is null) return null;
        var path = Path.Combine(fullRoot, "DS", "Content", "Paks", PakName);
        return File.Exists(path) ? path : null;
    }

    public static string? ReadInstalledSteamBuildId(string gameRoot)
    {
        var fullRoot = ResolveGameRoot(gameRoot);
        if (fullRoot is null) return null;
        var common = Directory.GetParent(fullRoot);
        var steamApps = common?.Parent;
        if (common is null || steamApps is null ||
            !common.Name.Equals("common", StringComparison.OrdinalIgnoreCase) ||
            !steamApps.Name.Equals("steamapps", StringComparison.OrdinalIgnoreCase)) return null;
        var manifest = Path.Combine(steamApps.FullName, "appmanifest_4570720.acf");
        if (!File.Exists(manifest)) return null;
        var match = Regex.Match(File.ReadAllText(manifest), "\\\"buildid\\\"\\s+\\\"(?<id>\\d+)\\\"", RegexOptions.IgnoreCase);
        return match.Success ? match.Groups["id"].Value : null;
    }

    private static string UniqueDisabledBackupPath(string pakPath)
    {
        var candidate = pakPath + ".disabled";
        for (var suffix = 2; File.Exists(candidate); suffix++)
            candidate = pakPath + $".{suffix}.disabled";
        return candidate;
    }

    public static string? ResolveGameRoot(string selectedPath)
    {
        if (string.IsNullOrWhiteSpace(selectedPath)) return null;
        string fullPath;
        try { fullPath = Path.GetFullPath(selectedPath.Trim().Trim('"')); }
        catch { return null; }
        if (File.Exists(fullPath)) fullPath = Path.GetDirectoryName(fullPath) ?? fullPath;

        var current = new DirectoryInfo(fullPath);
        for (var depth = 0; current is not null && depth < 8; depth++, current = current.Parent)
        {
            var executable = Path.Combine(current.FullName, "DS", "Binaries", "Win64", "DSClient-Win64-Shipping.exe");
            var paks = Path.Combine(current.FullName, "DS", "Content", "Paks");
            if (File.Exists(executable) || Directory.Exists(paks)) return current.FullName;
        }
        return null;
    }

    public static string? DetectGameRoot()
    {
        var candidates = new List<string>();
        foreach (var drive in DriveInfo.GetDrives().Where(x => x.IsReady))
        {
            candidates.Add(Path.Combine(drive.RootDirectory.FullName, "SteamLibrary", "steamapps", "common"));
            candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files (x86)", "Steam", "steamapps", "common"));
            candidates.Add(Path.Combine(drive.RootDirectory.FullName, "Program Files", "Steam", "steamapps", "common"));
        }
        foreach (var common in candidates.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            IEnumerable<string> directories;
            try { directories = Directory.EnumerateDirectories(common, "*Dragon*Sword*"); }
            catch { continue; }
            foreach (var directory in directories)
                if (ResolveGameRoot(directory) is { } root) return root;
        }
        return null;
    }

    public bool PakTouchesManagedTables(string pakPath)
    {
        RequireFile(_repakPath);
        var listing = RunAsync(_repakPath, ["list", pakPath]).GetAwaiter().GetResult();
        return listing.Contains("RewardRandomData.table", StringComparison.OrdinalIgnoreCase)
            || listing.Contains("RewardRandomData.xml", StringComparison.OrdinalIgnoreCase)
            || listing.Contains("RewardData.table", StringComparison.OrdinalIgnoreCase)
            || listing.Contains("RewardData.xml", StringComparison.OrdinalIgnoreCase)
            || listing.Contains("PropCollectData.table", StringComparison.OrdinalIgnoreCase)
            || listing.Contains("PropCollectData.xml", StringComparison.OrdinalIgnoreCase);
    }

    private JsonObject LoadObject(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(_baselineDirectory, name)))?.AsObject()
        ?? throw new InvalidDataException($"Invalid JSON baseline: {name}");

    private static Dictionary<long, string> BuildItemTypeMap(JsonObject table)
    {
        var map = new Dictionary<long, string>();
        foreach (var pair in table["Data"]!.AsObject())
            if (long.TryParse(pair.Key, out var id) && pair.Value is JsonObject item)
                map[id] = item["ItemType"]?.GetValue<string>() ?? "";
        return map;
    }

    private static List<RowTarget> DeriveEnemyMaterialTargets(JsonObject rewardData, JsonObject rewardRandom, Dictionary<long, string> itemTypes)
    {
        var fieldGroups = new HashSet<long>();
        foreach (var bucket in rewardData["Data"]!.AsObject().Select(x => x.Value!.AsObject()))
            foreach (var row in bucket["RewardDatas"]!.AsArray().Select(x => x!.AsObject()))
                if (row["IsFieldDrop"]?.GetValue<bool>() == true && row["DropType"]?.GetValue<string>() == "DROP")
                    foreach (var value in row["RandomID"]!.AsArray())
                        if (value?.GetValue<long>() is > 0 and var id) fieldGroups.Add(id);

        var result = new List<RowTarget>();
        var data = rewardRandom["Data"]!.AsObject();
        foreach (var groupId in fieldGroups.Order())
        {
            if (data[groupId.ToString(Invariant)] is not JsonObject bucket) continue;
            var rows = bucket["RewardRandomDataArray"]!.AsArray();
            for (var i = 0; i < rows.Count; i++)
            {
                var itemId = rows[i]!["ItemID"]!.GetValue<long>();
                if (itemTypes.TryGetValue(itemId, out var type) && type is "COMMON" or "COOKING_INGREDIENT")
                    result.Add(new RowTarget(groupId, i, itemId, "enemy_material"));
            }
        }
        return result;
    }

    private static List<long> DeriveGatheringTargets(JsonObject collect, Dictionary<long, string> itemTypes)
    {
        var result = new List<long>();
        foreach (var pair in collect["Data"]!.AsObject())
        {
            var row = pair.Value!.AsObject();
            var itemId = row["ItemID"]?.GetValue<long>() ?? 0;
            var count = row["ItemCount"]?.GetValue<long>() ?? 0;
            if (itemId > 0 && count > 0 && itemTypes.TryGetValue(itemId, out var type) && type is "COMMON" or "COOKING_INGREDIENT")
                result.Add(long.Parse(pair.Key, Invariant));
        }
        return result;
    }

    private static List<RowTarget> ReadRowTargets(JsonNode? node)
    {
        var result = new List<RowTarget>();
        foreach (var entry in node?.AsArray() ?? [])
        {
            var obj = entry!.AsObject();
            result.Add(new RowTarget(obj["group_id"]!.GetValue<long>(), obj["row_index"]!.GetValue<int>(), obj["item_id"]!.GetValue<long>(), obj["bucket"]?.GetValue<string>() ?? ""));
        }
        return result;
    }

    private static string RowKey(RowTarget row) => $"{row.GroupId}|{row.RowIndex}|{row.ItemId}";

    private static void ScaleRewardRow(JsonObject vanilla, JsonObject modified, RowTarget target, int multiplier)
    {
        var source = RewardRow(vanilla, target);
        var destination = RewardRow(modified, target);
        destination["Min_ItemCount"] = checked(source["Min_ItemCount"]!.GetValue<long>() * multiplier);
        destination["Max_ItemCount"] = checked(source["Max_ItemCount"]!.GetValue<long>() * multiplier);
    }

    private static JsonObject RewardRow(JsonObject table, RowTarget target)
    {
        var bucket = table["Data"]![target.GroupId.ToString(Invariant)]?.AsObject()
            ?? throw new InvalidDataException($"Missing reward group {target.GroupId}.");
        var rows = bucket["RewardRandomDataArray"]!.AsArray();
        var row = rows[target.RowIndex]?.AsObject() ?? throw new InvalidDataException($"Missing reward row {RowKey(target)}.");
        if (row["ItemID"]!.GetValue<long>() != target.ItemId) throw new InvalidDataException($"Reward identity mismatch at {RowKey(target)}.");
        return row;
    }

    private static int ScaleGathering(JsonObject vanilla, JsonObject modified, IEnumerable<long> ids, int multiplier)
    {
        if (multiplier == 1) return 0;
        var changed = 0;
        foreach (var id in ids)
        {
            var key = id.ToString(Invariant);
            var source = vanilla["Data"]![key]!.AsObject();
            modified["Data"]![key]!["ItemCount"] = checked(source["ItemCount"]!.GetValue<long>() * multiplier);
            changed++;
        }
        return changed;
    }

    private static void ApplyRarity(JsonObject random, JsonArray groups, decimal highPercent)
    {
        foreach (var entry in groups)
        {
            var group = entry!.AsObject();
            var groupId = group["group_id"]!.GetValue<long>();
            var rows = group["rows"]!.AsArray();
            var high = rows.Count(x => x!["tier"]!.GetValue<string>() == "higher");
            var low = rows.Count - high;
            if (high == 0 || low == 0) continue;
            foreach (var targetNode in rows)
            {
                var targetObject = targetNode!.AsObject();
                var target = new RowTarget(groupId, targetObject["row_index"]!.GetValue<int>(), targetObject["item_id"]!.GetValue<long>(), "rarity");
                var isHigh = targetObject["tier"]!.GetValue<string>() == "higher";
                RewardRow(random, target)["ItemWeight"] = isHigh ? highPercent / high : (100m - highPercent) / low;
            }
        }
    }

    private static SpreadResult ApplySpreadRolls(JsonObject modifiedRandom, JsonObject modifiedRewardData, List<RowTarget> activityTargets, int multiplier)
    {
        var equipmentGroups = activityTargets.Where(x => x.Bucket == "equipment").Select(x => x.GroupId).ToHashSet();
        var clones = new Dictionary<(long Group, long Quantity), long>();
        var generated = new List<CloneDefinition>();
        long nextId = 1_900_000_000;
        var transformed = 0;

        foreach (var bucket in modifiedRewardData["Data"]!.AsObject().Select(x => x.Value!.AsObject()))
        foreach (var row in bucket["RewardDatas"]!.AsArray().Select(x => x!.AsObject()))
        {
            var ids = row["RandomID"]!.AsArray().Select(x => x?.GetValue<long>() ?? 0).ToArray();
            var rates = row["RandomRate"]!.AsArray().Select(x => x?.GetValue<double>() ?? 0).ToArray();
            var hits = Enumerable.Range(0, ids.Length).Where(i => equipmentGroups.Contains(ids[i])).ToArray();
            if (hits.Length != 3 || hits.Select(i => ids[i]).Distinct().Count() != 1 || hits.Count(i => rates[i] == 1d) != 2 || hits.Count(i => rates[i] == .5d) != 1) continue;

            var fixedEntries = Enumerable.Range(0, ids.Length).Where(i => ids[i] != 0 && !hits.Contains(i)).Select(i => (ids[i], rates[i])).ToList();
            var rollCount = Math.Min(5, Math.Min(2 * multiplier, 9 - fixedEntries.Count - 1));
            if (rollCount < 2) continue;
            var total = 2 * multiplier;
            var quantities = Enumerable.Range(0, rollCount).Select(i => (long)(total / rollCount + (i < total % rollCount ? 1 : 0))).ToList();
            var sourceGroup = ids[hits[0]];
            var newIds = fixedEntries.Select(x => x.Item1).ToList();
            var newRates = fixedEntries.Select(x => x.Item2).ToList();
            foreach (var quantity in quantities)
            {
                newIds.Add(GetOrCreateClone(sourceGroup, quantity));
                newRates.Add(1d);
            }
            newIds.Add(GetOrCreateClone(sourceGroup, multiplier));
            newRates.Add(.5d);
            while (newIds.Count < 10) { newIds.Add(0); newRates.Add(0); }
            if (newIds.Count != 10) throw new InvalidDataException("Spread Rolls exceeded the ten-slot reward schema.");
            row["RandomID"] = new JsonArray(newIds.Select(x => JsonValue.Create(x)).ToArray());
            row["RandomRate"] = new JsonArray(newRates.Select(x => JsonValue.Create(x)).ToArray());
            transformed++;

            long GetOrCreateClone(long groupId, long quantity)
            {
                if (clones.TryGetValue((groupId, quantity), out var existing)) return existing;
                while (modifiedRandom["Data"]![nextId.ToString(Invariant)] is not null) nextId++;
                // Clone the already-modified group so Spread composes with
                // Favor Better Rarity instead of restoring vanilla weights.
                var source = modifiedRandom["Data"]![groupId.ToString(Invariant)]!.AsObject();
                var clone = (JsonObject)source.DeepClone();
                clone["ID"] = nextId;
                foreach (var cloneRow in clone["RewardRandomDataArray"]!.AsArray().Select(x => x!.AsObject()))
                {
                    cloneRow["ID"] = nextId;
                    cloneRow["Min_ItemCount"] = checked(cloneRow["Min_ItemCount"]!.GetValue<long>() * quantity);
                    cloneRow["Max_ItemCount"] = checked(cloneRow["Max_ItemCount"]!.GetValue<long>() * quantity);
                }
                modifiedRandom["Data"]![nextId.ToString(Invariant)] = clone;
                clones[(groupId, quantity)] = nextId;
                generated.Add(new CloneDefinition(groupId, nextId, quantity));
                return nextId++;
            }
        }
        return new SpreadResult(transformed, generated);
    }

    private static void WriteRewardRandomXml(string baselinePath, JsonObject modified, List<CloneDefinition> clones, string outputPath)
    {
        var lines = File.ReadAllLines(baselinePath).ToList();
        UpdateRewardLines(lines, modified);
        var lineMap = MapXmlLines(lines);
        var insertion = lines.FindLastIndex(x => x.TrimStart().StartsWith("</", StringComparison.Ordinal));
        foreach (var clone in clones)
        {
            if (!lineMap.TryGetValue(clone.SourceGroupId, out var sources)) throw new InvalidDataException($"Missing XML source group {clone.SourceGroupId}.");
            var cloneBucket = modified["Data"]![clone.CloneGroupId.ToString(Invariant)]!.AsObject();
            var cloneRows = cloneBucket["RewardRandomDataArray"]!.AsArray();
            for (var i = 0; i < sources.Count; i++)
            {
                var line = lines[sources[i]];
                line = SetAttribute(line, "ID", clone.CloneGroupId.ToString(Invariant));
                var row = cloneRows[i]!.AsObject();
                line = SetAttribute(line, "Min_ItemCount", Number(row["Min_ItemCount"]));
                line = SetAttribute(line, "Max_ItemCount", Number(row["Max_ItemCount"]));
                line = SetAttribute(line, "ItemWeight", Number(row["ItemWeight"]));
                lines.Insert(insertion++, line);
            }
        }
        WriteLines(outputPath, lines);
    }

    private static void UpdateRewardLines(List<string> lines, JsonObject modified)
    {
        var map = MapXmlLines(lines);
        foreach (var pair in modified["Data"]!.AsObject())
        {
            if (!long.TryParse(pair.Key, out var id) || !map.TryGetValue(id, out var indices)) continue;
            var rows = pair.Value!["RewardRandomDataArray"]!.AsArray();
            if (indices.Count != rows.Count) continue;
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i]!.AsObject();
                var line = lines[indices[i]];
                line = SetAttribute(line, "Min_ItemCount", Number(row["Min_ItemCount"]));
                line = SetAttribute(line, "Max_ItemCount", Number(row["Max_ItemCount"]));
                line = SetAttribute(line, "ItemWeight", Number(row["ItemWeight"]));
                lines[indices[i]] = line;
            }
        }
    }

    private static void WriteRewardDataXml(string baselinePath, JsonObject modified, string outputPath)
    {
        var lines = File.ReadAllLines(baselinePath).ToList();
        var map = MapXmlLines(lines);
        foreach (var pair in modified["Data"]!.AsObject())
        {
            if (!long.TryParse(pair.Key, out var id) || !map.TryGetValue(id, out var indices)) continue;
            var rows = pair.Value!["RewardDatas"]!.AsArray();
            if (rows.Count != indices.Count) throw new InvalidDataException($"RewardData XML topology mismatch for {id}.");
            for (var i = 0; i < rows.Count; i++)
            {
                var row = rows[i]!.AsObject();
                var line = lines[indices[i]];
                var ids = row["RandomID"]!.AsArray();
                var rates = row["RandomRate"]!.AsArray();
                for (var slot = 0; slot < 10; slot++)
                {
                    line = SetAttribute(line, $"RandomID{slot + 1}", (ids[slot]?.GetValue<long>() ?? 0) == 0 ? "" : Number(ids[slot]));
                    line = SetAttribute(line, $"RandomRate{slot + 1}", (rates[slot]?.GetValue<double>() ?? 0) == 0 ? "" : Number(rates[slot]));
                }
                lines[indices[i]] = line;
            }
        }
        WriteLines(outputPath, lines);
    }

    private static void WritePropCollectXml(string baselinePath, JsonObject modified, string outputPath)
    {
        var lines = File.ReadAllLines(baselinePath).ToList();
        var map = MapXmlLines(lines);
        foreach (var pair in modified["Data"]!.AsObject())
            if (long.TryParse(pair.Key, out var id) && map.TryGetValue(id, out var indices) && indices.Count == 1)
                lines[indices[0]] = SetAttribute(lines[indices[0]], "ItemCount", Number(pair.Value!["ItemCount"]));
        WriteLines(outputPath, lines);
    }

    private static Dictionary<long, List<int>> MapXmlLines(List<string> lines)
    {
        var result = new Dictionary<long, List<int>>();
        for (var i = 0; i < lines.Count; i++)
        {
            var match = XmlId.Match(lines[i]);
            if (!match.Success || !long.TryParse(match.Groups[1].Value, out var id)) continue;
            if (!result.TryGetValue(id, out var list)) result[id] = list = [];
            list.Add(i);
        }
        return result;
    }

    private static string SetAttribute(string line, string name, string value)
    {
        var pattern = "((?:\\w+:)?" + Regex.Escape(name) + "=\")[^\"]*(\")";
        if (!Regex.IsMatch(line, pattern)) throw new InvalidDataException($"XML attribute {name} was not found.");
        return new Regex(pattern).Replace(line, m => m.Groups[1].Value + value + m.Groups[2].Value, 1);
    }

    private static string Number(JsonNode? node) => node is null ? "" : node.ToJsonString().Trim('"');
    private static void WriteLines(string path, IEnumerable<string> lines) => File.WriteAllLines(path, lines, new UTF8Encoding(false));
    private static void WriteJson(string path, JsonNode node) => File.WriteAllText(path, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }), new UTF8Encoding(false));

    private static async Task<string> RunAsync(string fileName, IReadOnlyList<string> arguments)
    {
        using var process = new Process { StartInfo = new ProcessStartInfo { FileName = fileName, UseShellExecute = false, RedirectStandardOutput = true, RedirectStandardError = true, CreateNoWindow = true } };
        foreach (var argument in arguments) process.StartInfo.ArgumentList.Add(argument);
        process.Start();
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync().ConfigureAwait(false);
        var output = (await stdout.ConfigureAwait(false)) + (await stderr.ConfigureAwait(false));
        if (process.ExitCode != 0) throw new InvalidOperationException($"{Path.GetFileName(fileName)} failed with exit code {process.ExitCode}:\r\n{output.Trim()}");
        return output;
    }

    private static void Validate(BuildConfig c)
    {
        foreach (var value in new[] { c.WorldGatheringMultiplier, c.EnemyMaterialMultiplier, c.EquipmentMultiplier, c.ActivityMaterialMultiplier, c.AdventurerEmblemMultiplier, c.GoldMultiplier, c.RankExperienceMultiplier, c.WorldChestMultiplier })
            if (value is < 1 or > 100) throw new ArgumentOutOfRangeException(nameof(c), "Multipliers must be between 1 and 100.");
        if (c.EquipmentMultiplier > 10)
            throw new ArgumentOutOfRangeException(nameof(c), "Equipment is capped at x10 to protect the game's 500-slot equipment inventory.");
        if (c.HighGradeChance is < 0 or > 100) throw new ArgumentOutOfRangeException(nameof(c), "High-grade chance must be between 0 and 100 percent.");
    }

    private static void RequireFile(string path) { if (!File.Exists(path)) throw new FileNotFoundException("A required, auditable application file is missing. Antivirus quarantine may be responsible.", path); }

    private sealed record RowTarget(long GroupId, int RowIndex, long ItemId, string Bucket);
    private sealed record CloneDefinition(long SourceGroupId, long CloneGroupId, long Quantity);
    private sealed record SpreadResult(int TransformedRows, List<CloneDefinition> GeneratedGroups);
}
