namespace DragonSword.ProgressionQoL;

internal sealed record BuildConfig(
    int WorldGatheringMultiplier,
    int EnemyMaterialMultiplier,
    int EquipmentMultiplier,
    int ActivityMaterialMultiplier,
    int CurrencyExperienceItemMultiplier,
    int AdventurerEmblemMultiplier,
    int GoldMultiplier,
    int RankExperienceMultiplier,
    int WorldChestMultiplier,
    bool SpreadRolls,
    bool EnhancedRarity,
    decimal HighGradeChance);

internal sealed record BuildResult(
    string PakPath,
    string Sha256,
    int FilesPacked,
    int GatheringRows,
    int EnemyMaterialRows,
    int ActivityRows,
    int ChestRows,
    int SpreadRewardRows,
    int GeneratedGroups,
    IReadOnlyList<string> PackedFiles);
