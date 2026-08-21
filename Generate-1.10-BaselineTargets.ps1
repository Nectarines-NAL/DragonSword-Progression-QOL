[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [string]$OfficialGameDataRoot,

    [string]$OutputPath = (
        Join-Path $PSScriptRoot 'Baselines\BaselineTargets.json'
    )
)

$ErrorActionPreference = 'Stop'
$utf8WithoutBom = [Text.UTF8Encoding]::new($false)
$supportedBuildID = '24831799'

function Read-Table {
    param([Parameter(Mandatory)][string]$Name)

    $path = Join-Path $OfficialGameDataRoot "$Name.table"
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) {
        throw "Official game table was not found: $path"
    }
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
}

function Get-ActivityCategory {
    param([long]$MapID)

    if ($MapID -ge 10101 -and $MapID -le 11803) { return 'Normal' }
    if ($MapID -in 11901, 11902) { return 'Underwater' }
    if ($MapID -ge 20001 -and $MapID -le 20043) { return 'Currency' }
    if ($MapID -ge 400001 -and $MapID -le 400084) { return 'Trait' }
    if ($MapID -ge 100001 -and $MapID -le 100055) { return 'Hunt' }
    if ($MapID -ge 200001 -and $MapID -le 200006) { return 'Raid' }
    return $null
}

function Add-PositiveIDs {
    param(
        [Parameter(Mandatory)]
        [AllowEmptyCollection()]
        [Collections.Generic.HashSet[long]]$Set,
        $Values
    )

    foreach ($value in @($Values)) {
        if ([long]$value -gt 0) { [void]$Set.Add([long]$value) }
    }
}

function Get-RandomIDsForReward {
    param(
        [Parameter(Mandatory)]$RewardData,
        [Parameter(Mandatory)][long]$RewardID
    )

    $property = $RewardData.PSObject.Properties["$RewardID"]
    if ($null -eq $property) {
        throw "RewardData is missing activity reward $RewardID."
    }

    $result = [Collections.Generic.HashSet[long]]::new()
    foreach ($row in @($property.Value.RewardDatas)) {
        Add-PositiveIDs -Set $result -Values $row.RandomID
    }
    return $result
}

function New-RowIdentity {
    param(
        [long]$GroupID,
        [int]$RowIndex,
        $Row,
        [string]$Bucket = '',
        [string]$Category = '',
        [string]$ItemType = ''
    )

    $result = [ordered]@{
        group_id = $GroupID
        row_index = $RowIndex
        item_id = [long]$Row.ItemID
    }
    if (-not [string]::IsNullOrWhiteSpace($Bucket)) {
        $result.bucket = $Bucket
    }
    if (-not [string]::IsNullOrWhiteSpace($Category)) {
        $result.activity_category = $Category
    }
    if (-not [string]::IsNullOrWhiteSpace($ItemType)) {
        $result.item_type = $ItemType
    }
    return $result
}

$rewardRandomTable = Read-Table 'RewardRandomData'
$rewardTable = Read-Table 'RewardData'
$itemTable = Read-Table 'GameItemData'
$mapTable = Read-Table 'MapDungeonData'
$treasureTable = Read-Table 'PropTreasureBoxData'

$rewardRandom = $rewardRandomTable.Data
$rewardData = $rewardTable.Data
$items = $itemTable.Data
$maps = $mapTable.Data
$treasureBoxes = $treasureTable.Data

$allowedTypes = @{
    Normal = @('PAY_ADV_EXP', 'COMMON', 'EQUIPMENT', 'CHANGE_EQUIPMENT_SUB_STAT')
    Currency = @('PAY_ADV_EXP', 'COMMON', 'PAY_GOLD', 'CHARACTER_EXP', 'EQUIPMENT_EXP', 'KARMA_EXP')
    Trait = @('PAY_ADV_EXP', 'COMMON')
    Hunt = @('PAY_ADV_EXP', 'COMMON')
    Raid = @('PAY_ADV_EXP', 'COMMON', 'PAY_GOLD', 'GEM_SOCKET_MATERIAL', 'CHANGE_EQUIPMENT_SUB_STAT', 'GEM', 'EQUIPMENT')
    Underwater = @('PAY_GOLD', 'COMMON')
    Sudden = @(
        'PAY_ADV_EXP', 'PAY_GOLD', 'COMMON', 'GEM_SOCKET_MATERIAL',
        'CHANGE_EQUIPMENT_SUB_STAT'
    )
}

$firstClearRewardIDs = [Collections.Generic.HashSet[long]]::new()
$targetRewardIDs = [Collections.Generic.HashSet[long]]::new()
$groupCategories = @{}
$groupRarityMetadata = @{}
$supportedMapCounts = @{}

foreach ($map in $maps.PSObject.Properties.Value) {
    Add-PositiveIDs -Set $firstClearRewardIDs -Values $map.First_Reward_ID
    $category = Get-ActivityCategory -MapID ([long]$map.ID)
    if ($null -eq $category -or ([bool]$map.IsHide -and $category -ne 'Underwater')) { continue }
    if ([long]$map.Reward_ID -le 0) {
        throw "$category map $($map.ID) has no direct clear reward in game 1.0.10."
    }

    if (-not $supportedMapCounts.ContainsKey($category)) { $supportedMapCounts[$category] = 0 }
    $supportedMapCounts[$category]++
    [void]$targetRewardIDs.Add([long]$map.Reward_ID)

    foreach ($groupID in (Get-RandomIDsForReward -RewardData $rewardData -RewardID ([long]$map.Reward_ID))) {
        if ($groupCategories.ContainsKey($groupID) -and $groupCategories[$groupID] -ne $category) {
            throw "Random group $groupID belongs to both $($groupCategories[$groupID]) and $category."
        }
        $groupCategories[$groupID] = $category

        if ($category -in @('Normal', 'Raid')) {
            $metadata = [pscustomobject]@{
                Category = $category
                Difficulty = [int]$map.ContentsDifficulty
            }
            if (
                $groupRarityMetadata.ContainsKey($groupID) -and
                (
                    $groupRarityMetadata[$groupID].Category -ne $metadata.Category -or
                    [int]$groupRarityMetadata[$groupID].Difficulty -ne $metadata.Difficulty
                )
            ) {
                throw "Random group $groupID has conflicting rarity metadata."
            }
            $groupRarityMetadata[$groupID] = $metadata
        }
    }
}

# Sudden Missions use seven RewardData entries which converge on these eleven
# repeat-reward groups. These identities come from the independently authored
# Dungeon QOL v1.3 work in this repository, and are revalidated below against
# the unmodified 1.0.10 tables before inclusion.
$suddenMissionGroups = 21000000..21000010
$suddenRewardIDs = 2100000..2100006
foreach ($rewardID in $suddenRewardIDs) { [void]$targetRewardIDs.Add([long]$rewardID) }
foreach ($groupID in $suddenMissionGroups) {
    if ($null -eq $rewardRandom.PSObject.Properties["$groupID"]) {
        throw "Game 1.0.10 is missing known Sudden Mission group $groupID."
    }
    $groupCategories[[long]$groupID] = 'Sudden'
}

$firstClearRandomIDs = [Collections.Generic.HashSet[long]]::new()
foreach ($rewardID in $firstClearRewardIDs) {
    $property = $rewardData.PSObject.Properties["$rewardID"]
    if ($null -eq $property) { continue }
    foreach ($row in @($property.Value.RewardDatas)) {
        Add-PositiveIDs -Set $firstClearRandomIDs -Values $row.RandomID
    }
}
foreach ($groupID in $firstClearRandomIDs) {
    if ($groupCategories.ContainsKey($groupID)) {
        throw "Repeat activity target $groupID overlaps a first-clear reward."
    }
}

$rewardReferences = @{}
foreach ($reward in $rewardData.PSObject.Properties.Value) {
    foreach ($row in @($reward.RewardDatas)) {
        foreach ($groupIDValue in @($row.RandomID)) {
            $groupID = [long]$groupIDValue
            if ($groupID -le 0) { continue }
            if (-not $rewardReferences.ContainsKey($groupID)) {
                $rewardReferences[$groupID] = [Collections.Generic.HashSet[long]]::new()
            }
            [void]$rewardReferences[$groupID].Add([long]$reward.ID)
        }
    }
}
foreach ($groupID in $groupCategories.Keys) {
    $outsideReferences = @(
        $rewardReferences[[long]$groupID] |
            Where-Object { -not $targetRewardIDs.Contains([long]$_) }
    )
    if ($outsideReferences.Count -gt 0) {
        throw (
            "Activity group $groupID is also used outside supported repeat rewards: " +
            ($outsideReferences -join ', ')
        )
    }
}

$activityRows = [Collections.Generic.List[object]]::new()
$activityKeys = [Collections.Generic.HashSet[string]]::new()
foreach ($entry in ($groupCategories.GetEnumerator() | Sort-Object Key)) {
    $groupID = [long]$entry.Key
    $category = [string]$entry.Value
    $property = $rewardRandom.PSObject.Properties["$groupID"]
    if ($null -eq $property) { throw "RewardRandomData is missing activity group $groupID." }
    $rows = @($property.Value.RewardRandomDataArray)
    for ($index = 0; $index -lt $rows.Count; $index++) {
        $row = $rows[$index]
        $item = $items.PSObject.Properties["$($row.ItemID)"].Value
        if ($null -eq $item) { throw "Activity group $groupID references unknown item $($row.ItemID)." }
        $itemType = [string]$item.ItemType
        if ($itemType -notin $allowedTypes[$category]) {
            throw "$category group $groupID contains unsupported item type '$itemType' ($($row.ItemID))."
        }
        $bucket = switch ($itemType) {
            'EQUIPMENT' { 'equipment'; break }
            'PAY_GOLD' { 'gold'; break }
            'PAY_ADV_EXP' { 'rank_experience'; break }
            default { 'materials' }
        }
        $activityRows.Add((New-RowIdentity -GroupID $groupID -RowIndex $index -Row $row -Bucket $bucket -Category $category -ItemType $itemType))
        [void]$activityKeys.Add("$groupID`:$index")
    }
}

$gradeRank = @{ NORMAL = 1; SUPERIOR = 2; RARE = 3; EPIC = 4; LEGENDARY = 5 }
$rarityGroups = [Collections.Generic.List[object]]::new()
foreach ($entry in ($groupRarityMetadata.GetEnumerator() | Sort-Object Key)) {
    $groupID = [long]$entry.Key
    $metadata = $entry.Value
    $eligibleType = if ($metadata.Category -eq 'Normal') { 'EQUIPMENT' } else { 'GEM' }
    $rows = @($rewardRandom.PSObject.Properties["$groupID"].Value.RewardRandomDataArray)
    $eligible = @(
        for ($index = 0; $index -lt $rows.Count; $index++) {
            $row = $rows[$index]
            $item = $items.PSObject.Properties["$($row.ItemID)"].Value
            if ([string]$item.ItemType -eq $eligibleType) {
                [pscustomobject]@{ Index = $index; Row = $row; Grade = [string]$item.Grade }
            }
        }
    )
    $grades = @($eligible.Grade | Sort-Object -Unique)
    if ($grades.Count -lt 2) { continue }
    if ($grades.Count -ne 2) { throw "Rarity group $groupID contains more than two eligible grades." }
    foreach ($grade in $grades) {
        if (-not $gradeRank.ContainsKey($grade)) { throw "Rarity group $groupID has unknown grade '$grade'." }
    }
    $orderedGrades = @($grades | Sort-Object { $gradeRank[$_] })
    $rarityGroups.Add([ordered]@{
        group_id = $groupID
        policy = "$($metadata.Category.ToLowerInvariant())_difficulty_$($metadata.Difficulty)"
        lower_grade = $orderedGrades[0]
        higher_grade = $orderedGrades[1]
        rows = @(
            foreach ($candidate in $eligible) {
                [ordered]@{
                    row_index = [int]$candidate.Index
                    item_id = [long]$candidate.Row.ItemID
                    tier = if ($candidate.Grade -eq $orderedGrades[0]) { 'lower' } else { 'higher' }
                }
            }
        )
    })
}

$chestRewardIDs = [Collections.Generic.HashSet[long]]::new()
foreach ($treasure in $treasureBoxes.PSObject.Properties.Value) {
    Add-PositiveIDs -Set $chestRewardIDs -Values $treasure.ItemRewardID
}
$chestRandomIDs = [Collections.Generic.HashSet[long]]::new()
foreach ($rewardID in $chestRewardIDs) {
    foreach ($groupID in (Get-RandomIDsForReward -RewardData $rewardData -RewardID $rewardID)) {
        [void]$chestRandomIDs.Add($groupID)
    }
}

$safeChestTypes = @(
    'PAY_GOLD', 'PAY_ADV_EXP', 'CHARACTER_EXP', 'EQUIPMENT_EXP', 'KARMA_EXP',
    'GEM_SOCKET_MATERIAL', 'CHANGE_EQUIPMENT_SUB_STAT', 'COMMON'
)
$excludedChestItemIDs = @(1000501) # Fragment of Sacred Light
$chestRows = [Collections.Generic.List[object]]::new()
$missingChestGroups = [Collections.Generic.List[long]]::new()
foreach ($groupID in ($chestRandomIDs | Sort-Object)) {
    $property = $rewardRandom.PSObject.Properties["$groupID"]
    if ($null -eq $property) {
        $missingChestGroups.Add($groupID)
        continue
    }
    $rows = @($property.Value.RewardRandomDataArray)
    for ($index = 0; $index -lt $rows.Count; $index++) {
        $row = $rows[$index]
        $item = $items.PSObject.Properties["$($row.ItemID)"].Value
        if ($null -eq $item) { throw "World chest group $groupID references unknown item $($row.ItemID)." }
        if ([string]$item.ItemType -in $safeChestTypes -and [long]$row.ItemID -notin $excludedChestItemIDs) {
            if ($activityKeys.Contains("$groupID`:$index")) {
                throw "World chest row $groupID`:$index overlaps an activity target."
            }
            $chestRows.Add((New-RowIdentity -GroupID $groupID -RowIndex $index -Row $row))
        }
    }
}

$manifest = [ordered]@{
    schema_version = 4
    provenance = "Independently derived from unmodified DragonSword: Awakening 1.0.10 Steam build $supportedBuildID tables and nectarines' Dungeon QOL Sudden Mission research."
    game_version = '1.0.10'
    steam_build_id = $supportedBuildID
    baseline_sha256 = [ordered]@{
        reward_random_client = (Get-FileHash -LiteralPath (Join-Path $OfficialGameDataRoot 'RewardRandomData.table') -Algorithm SHA256).Hash
        reward_data_client = (Get-FileHash -LiteralPath (Join-Path $OfficialGameDataRoot 'RewardData.table') -Algorithm SHA256).Hash
        prop_collect_client = (Get-FileHash -LiteralPath (Join-Path $OfficialGameDataRoot 'PropCollectData.table') -Algorithm SHA256).Hash
        game_item_client = (Get-FileHash -LiteralPath (Join-Path $OfficialGameDataRoot 'GameItemData.table') -Algorithm SHA256).Hash
        map_dungeon_client = (Get-FileHash -LiteralPath (Join-Path $OfficialGameDataRoot 'MapDungeonData.table') -Algorithm SHA256).Hash
    }
    activity_reward_rows = @($activityRows)
    rarity_groups = @($rarityGroups)
    safe_world_chest_rows = @($chestRows)
    known_source_defects = @(
        foreach ($groupID in $missingChestGroups) {
            [ordered]@{
                id = "missing_reward_random_group_$groupID"
                effect = 'A world chest references an absent guaranteed reward group.'
                policy = 'Diagnose only; never invent an unknown reward.'
            }
        }
    )
    counts = [ordered]@{
        visible_normal_maps = [int]$supportedMapCounts.Normal
        visible_currency_maps = [int]$supportedMapCounts.Currency
        visible_trait_maps = [int]$supportedMapCounts.Trait
        visible_hunt_maps = [int]$supportedMapCounts.Hunt
        visible_raid_maps = [int]$supportedMapCounts.Raid
        supported_underwater_maps = [int]$supportedMapCounts.Underwater
        activity_random_groups = $groupCategories.Count
        activity_reward_rows = $activityRows.Count
        sudden_mission_groups = $suddenMissionGroups.Count
        rarity_groups = $rarityGroups.Count
        safe_world_chest_rows = $chestRows.Count
        missing_world_chest_groups = $missingChestGroups.Count
    }
}

$outputDirectory = Split-Path -Parent ([IO.Path]::GetFullPath($OutputPath))
if (-not (Test-Path -LiteralPath $outputDirectory -PathType Container)) {
    New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
}
[IO.File]::WriteAllText($OutputPath, ($manifest | ConvertTo-Json -Depth 40), $utf8WithoutBom)

Write-Output "BASELINE_TARGETS_110_COMPLETE $OutputPath"
$manifest.counts | Format-List
