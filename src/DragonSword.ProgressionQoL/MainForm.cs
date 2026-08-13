using System.Diagnostics;
using System.Drawing.Drawing2D;

namespace DragonSword.ProgressionQoL;

internal sealed class MainForm : Form
{
    private static readonly Color Ink = Color.FromArgb(13, 17, 20);
    private static readonly Color PanelColor = Color.FromArgb(24, 29, 33);
    private static readonly Color PanelRaised = Color.FromArgb(31, 37, 42);
    private static readonly Color Ember = Color.FromArgb(205, 70, 52);
    private static readonly Color Gold = Color.FromArgb(196, 163, 99);
    private static readonly Color Teal = Color.FromArgb(83, 157, 162);
    private static readonly Color TextMain = Color.FromArgb(232, 231, 226);
    private static readonly Color TextMuted = Color.FromArgb(161, 169, 173);

    private readonly BuildEngine _engine;
    private readonly ProfileStore _profiles;
    private readonly Dictionary<string, MultiplierDropDown> _multipliers = [];
    private readonly ToolTip _toolTips = new() { AutoPopDelay = 12000, InitialDelay = 350, ReshowDelay = 100 };
    private readonly CheckBox _spread = new();
    private readonly CheckBox _rarity = new();
    private readonly TextBox _gamePath = new();
    private readonly TextBox _outputPath = new();
    private readonly RichTextBox _log = new();
    private readonly Label _status = new();
    private readonly Label _conflictSummary = new();
    private readonly TabControl _tabs = new();
    private readonly Button _build = new();
    private readonly CrispBorderButton _profileButton = new();
    private readonly Label _profileStatus = new();
    private readonly ContextMenuStrip _profileMenu = new();
    private BuildResult? _latestBuild;
    private string _currentProfileName = "Default";
    private bool _configurationDirty;
    private bool _applyingProfile;

    public MainForm(bool startMaximized = false)
    {
        Text = "DragonSword Progression QOL — 1.09 Release Candidate";
        MinimumSize = new Size(1000, 700);
        var working = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1600, 1000);
        Size = new Size(Math.Min(1480, working.Width - 96), Math.Min(1000, working.Height - 72));
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ink;
        ForeColor = TextMain;
        Font = new Font("Segoe UI", 10f);
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        if (startMaximized) WindowState = FormWindowState.Maximized;
        _engine = new BuildEngine(AppContext.BaseDirectory);
        _profiles = new ProfileStore(AppContext.BaseDirectory);
        BuildLayout();
        SetDefaults();
        MigrateLegacyProfiles();
        ScanConflicts();
    }

    private void BuildLayout()
    {
        var shell = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 3, ColumnCount = 1, BackColor = Ink, Padding = new Padding(12) };
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 140));
        shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        shell.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        Controls.Add(shell);
        shell.Controls.Add(BuildBanner(), 0, 0);

        _tabs.Dock = DockStyle.Fill;
        _tabs.Appearance = TabAppearance.Normal;
        _tabs.Padding = new Point(18, 8);
        _tabs.TabPages.Add(BuildRewardsTab());
        _tabs.TabPages.Add(BuildPathsTab());
        _tabs.TabPages.Add(BuildAuditTab());
        shell.Controls.Add(_tabs, 0, 1);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        _status.Text = "Ready — builds are local, offline, and reversible.";
        _status.ForeColor = TextMuted;
        _status.AutoSize = true;
        _status.Location = new Point(4, 20);
        footer.Controls.Add(_status);

        _build.Text = "BUILD + INSTALL";
        StyleButton(_build, Ember, TextMain);
        _build.Size = new Size(180, 42);
        _build.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _build.Location = new Point(footer.Width - 170, 8);
        _build.Click += async (_, _) => await BuildAsync();
        footer.Controls.Add(_build);

        _profileButton.Text = "PROFILES";
        StyleButton(_profileButton, Ink, Teal);
        _profileButton.FlatAppearance.BorderSize = 0;
        _profileButton.BorderColor = Teal;
        _profileButton.Size = new Size(126, 42);
        _profileButton.Top = 8;
        _profileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _profileButton.Click += (_, _) => ShowProfileMenu();
        footer.Controls.Add(_profileButton);

        _profileStatus.Text = "Settings: Default";
        _profileStatus.ForeColor = Teal;
        _profileStatus.AutoSize = true;
        _profileStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        footer.Controls.Add(_profileStatus);

        footer.Resize += (_, _) =>
        {
            _build.Left = footer.ClientSize.Width - _build.Width;
            _profileButton.Left = _build.Left - _profileButton.Width - 10;
            _profileButton.Top = _build.Top;
            _profileStatus.Left = _profileButton.Left - _profileStatus.Width - 16;
            _profileStatus.Top = 20;
        };
        shell.Controls.Add(footer, 0, 2);
    }

    private Control BuildBanner()
    {
        var banner = new BannerPanel { Dock = DockStyle.Fill, BackColor = PanelColor, BackgroundImageLayout = ImageLayout.Zoom };
        var imagePath = Path.Combine(AppContext.BaseDirectory, "Assets", "progression-qol-original-key-art-v1.png");
        if (File.Exists(imagePath))
        {
            using var source = Image.FromFile(imagePath);
            banner.BackgroundImage = new Bitmap(source);
        }
        return banner;
    }

    private TabPage BuildRewardsTab()
    {
        var tab = NewTab("Rewards");
        tab.AutoScroll = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, MinimumSize = new Size(900, 680), ColumnCount = 3, RowCount = 1, Padding = new Padding(8), BackColor = PanelColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
        layout.Controls.Add(BuildSection(
            "WORLD",
            "Exploration rewards. x1 is vanilla; x5 gives five times the amount.",
            RewardGlyph.World,
            [
                AddMultiplier("World gathering", "gathering", 1, 20, "Plants, ore, and cooking-ingredient nodes.", RewardGlyph.Gathering),
                AddMultiplier("Enemy materials", "enemy", 1, 20, "Material drops from ordinary and field enemies.", RewardGlyph.Enemy),
                AddMultiplier("Safe chest stacks", "chests", 1, 10, "Stackable loot from one-time world exploration chests. Dungeon chests, unique items, and progression items stay unchanged.", RewardGlyph.Chest)
            ]), 0, 0);
        layout.Controls.Add(BuildSection(
            "ACTIVITIES",
            "Dungeons, hunts, raids, and Sudden Missions. Each setting multiplies its original 1.09 reward.",
            RewardGlyph.Activities,
            [
                AddMultiplier("Equipment", "equipment", 1, 10, "Activity gear. x10 max. Inventory: 500 items.", RewardGlyph.Equipment),
                AddMultiplier("Crafting materials", "activityMaterials", 1, 20, "Trait stones, boss parts, Raid runes, upgrades, and character, equipment, or Karma XP items.", RewardGlyph.Material),
                AddMultiplier("Emblems", "emblems", 1, 20, "Exchange Shop currency; Sudden Missions included.", RewardGlyph.Emblem),
                AddMultiplier("Gold", "gold", 1, 20, "Gold from activity completions.", RewardGlyph.Gold),
                AddMultiplier("Experience", "rankXp", 1, 20, "Mercenary Corps Rank EXP.", RewardGlyph.Experience)
            ], 2), 1, 0);

        _spread.Checked = false;
        _rarity.Checked = false;
        var rarityChance = AddPercent("Better-tier chance", "rarityChance", 90, "When enabled, about 9 of 10 supported rolls select the better tier.", RewardGlyph.Rarity);
        _rarity.CheckedChanged += (_, _) => rarityChance.Enabled = _rarity.Checked;
        _rarity.CheckedChanged += (_, _) => MarkConfigurationEdited();
        _spread.CheckedChanged += (_, _) => MarkConfigurationEdited();
        rarityChance.Enabled = false;
        layout.Controls.Add(BuildSection(
            "LOOT SHAPE",
            "Changes equipment rolls; neither option adds rewards by itself.",
            RewardGlyph.LootShape,
            [
                AddToggle(_spread, "Spread equipment rolls", "Off (vanilla): multiplied gear stays stacked. On: the same total is split into independent equipment selections for more variety. Equipment only; Raid runes and item stats are unchanged.", RewardGlyph.Spread),
                AddToggle(_rarity, "Favor better rarity", "Off (vanilla): original rarity odds. On: supported mixed-tier equipment and Raid rune pools favor their better listed tier. Quantity and item stats are unchanged.", RewardGlyph.Rarity),
                rarityChance
            ]), 2, 0);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildPathsTab()
    {
        var tab = NewTab("Build & Install");
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelColor, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(BuildPageHeader(), 0, 0);
        layout.Controls.Add(BuildPathCard("DragonSword installation", "Select the game folder—or DS, Paks, ~mods, Win64, or the game EXE. It is normalized automatically; installation goes to DS\\Content\\Paks.", _gamePath, BrowseGame, RewardGlyph.GameFolder), 0, 1);
        layout.Controls.Add(BuildPathCard("Build output folder", "Defaults to this Windows user's Documents folder. Verified PAKs and reports are created here; any writable folder can be selected.", _outputPath, BrowseOutput, RewardGlyph.OutputFolder), 0, 2);
        layout.Controls.Add(BuildScanCard(), 0, 3);
        tab.Controls.Add(layout);
        return tab;
    }

    private static Control BuildPageHeader()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = PanelColor, Padding = new Padding(6) };
        var icon = new RewardIcon(RewardGlyph.Build) { Location = new Point(8, 12), Size = new Size(42, 42), BackColor = PanelColor };
        var title = new Label { Text = "BUILD & INSTALL", UseMnemonic = false, ForeColor = Gold, Font = new Font("Segoe UI Semibold", 13f), AutoSize = true, Location = new Point(62, 8) };
        var detail = new Label
        {
            Text = "Choose settings, then use Build + Install. The verified PAK replaces only this app's previous PAK after confirmation; other mods are never changed.",
            ForeColor = TextMuted,
            AutoSize = false,
            Location = new Point(62, 37),
            Height = 50,
            UseCompatibleTextRendering = true
        };
        host.Controls.Add(icon); host.Controls.Add(title); host.Controls.Add(detail);
        host.Resize += (_, _) => detail.Width = Math.Max(300, host.ClientSize.Width - detail.Left - 8);
        return host;
    }

    private static Control BuildPathCard(string titleText, string description, TextBox box, Action browse, RewardGlyph glyph)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, Margin = new Padding(4, 5, 4, 5), Padding = new Padding(12), ColumnCount = 2, RowCount = 1 };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var icon = new RewardIcon(glyph) { Dock = DockStyle.Top, Size = new Size(40, 40), Margin = new Padding(2, 8, 8, 0) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, ColumnCount = 1, RowCount = 3 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var title = new Label { Text = titleText, ForeColor = Gold, Font = new Font("Segoe UI Semibold", 10f), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var detail = new Label { Text = description, ForeColor = TextMuted, Dock = DockStyle.Fill, UseCompatibleTextRendering = true };
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = PanelRaised };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        box.Dock = DockStyle.Fill;
        box.BackColor = Ink;
        box.ForeColor = TextMain;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = new Font("Segoe UI", 9.5f);
        box.Margin = new Padding(0, 2, 10, 2);
        var button = new Button { Text = "BROWSE", Dock = DockStyle.Fill, Margin = new Padding(0) };
        StyleButton(button, Ink, Teal);
        button.Click += (_, _) => browse();
        pathRow.Controls.Add(box, 0, 0);
        pathRow.Controls.Add(button, 1, 0);
        content.Controls.Add(title, 0, 0);
        content.Controls.Add(detail, 0, 1);
        content.Controls.Add(pathRow, 0, 2);
        card.Controls.Add(icon, 0, 0);
        card.Controls.Add(content, 1, 0);
        return card;
    }

    private Control BuildScanCard()
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, Margin = new Padding(4, 5, 4, 5), Padding = new Padding(12), ColumnCount = 2, RowCount = 1 };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var icon = new RewardIcon(RewardGlyph.Scan) { Dock = DockStyle.Top, Size = new Size(40, 40), Margin = new Padding(2, 8, 8, 0) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, ColumnCount = 2, RowCount = 3 };
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 210));
        content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        var title = new Label { Text = "Installed mod check", ForeColor = Gold, Font = new Font("Segoe UI Semibold", 10f), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var detail = new Label { Text = "Scans likely mod PAKs in DS\\Content\\Paks and ~mods, then verifies their internal table paths. It never disables, deletes, or moves another mod.", ForeColor = TextMuted, Dock = DockStyle.Fill, UseCompatibleTextRendering = true };
        var scan = new Button { Text = "SCAN INSTALLED MODS", Dock = DockStyle.Fill, Margin = new Padding(0, 6, 14, 6) };
        StyleButton(scan, Ink, Teal);
        scan.Click += (_, _) => ScanConflicts();
        _conflictSummary.Text = "Not scanned yet.";
        _conflictSummary.ForeColor = TextMuted;
        _conflictSummary.Dock = DockStyle.Fill;
        _conflictSummary.TextAlign = ContentAlignment.MiddleLeft;
        _conflictSummary.AutoEllipsis = false;
        content.Controls.Add(title, 0, 0);
        content.SetColumnSpan(title, 2);
        content.Controls.Add(detail, 0, 1);
        content.SetColumnSpan(detail, 2);
        content.Controls.Add(scan, 0, 2);
        content.Controls.Add(_conflictSummary, 1, 2);
        card.Controls.Add(icon, 0, 0);
        card.Controls.Add(content, 1, 0);
        return card;
    }

    private TabPage BuildAuditTab()
    {
        var tab = NewTab("Audit Log");
        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Color.FromArgb(16, 20, 23);
        _log.ForeColor = Color.FromArgb(188, 202, 204);
        _log.Font = new Font("Cascadia Mono", 9.25f);
        _log.Text = "Progression QOL audit log\n─────────────────────────\n";
        tab.Controls.Add(_log);
        return tab;
    }

    private Control AddMultiplier(string title, string key, int selectedValue, int maximum, string description, RewardGlyph glyph)
    {
        var options = new[] { 1, 2, 3, 5, 7, 10, 15, 20 }.Where(x => x <= maximum)
            .Select(x => new Choice(x, x == 1 ? "x1 — Default" : $"x{x}" )).ToArray();
        return AddChoice(title, key, selectedValue, description, glyph, options, 170);
    }

    private Control AddPercent(string title, string key, int selectedValue, string description, RewardGlyph glyph)
    {
        var options = new[] { 25, 50, 75, 90, 95, 99 }
            .Select(x => new Choice(x, x == 90 ? "90% — Recommended" : $"{x}%" )).ToArray();
        return AddChoice(title, key, selectedValue, description, glyph, options, 240);
    }

    private Control AddChoice(string title, string key, int selectedValue, string description, RewardGlyph glyph, IReadOnlyList<Choice> options, int buttonWidth)
    {
        var host = new Panel { Height = 141, MinimumSize = new Size(190, 141), BackColor = PanelRaised, Margin = new Padding(3) };
        var icon = new RewardIcon(glyph) { Location = new Point(8, 13), Size = new Size(36, 36) };
        var label = new Label { Text = title, ForeColor = TextMain, AutoSize = false, Font = new Font("Segoe UI Semibold", 9.5f), Location = new Point(54, 3), Height = 23 };
        var help = new Label { Text = description, ForeColor = TextMuted, AutoEllipsis = false, AutoSize = false, Location = new Point(54, 63), Height = 76, UseCompatibleTextRendering = true };
        var combo = new MultiplierDropDown(options, selectedValue) { Width = buttonWidth, Height = 32, Location = new Point(54, 27) };
        combo.ValueChanged += (_, _) => MarkConfigurationEdited();
        _multipliers[key] = combo;
        _toolTips.SetToolTip(host, description);
        _toolTips.SetToolTip(combo, description);
        host.Controls.Add(icon); host.Controls.Add(label); host.Controls.Add(help); host.Controls.Add(combo);
        host.Resize += (_, _) =>
        {
            combo.Left = label.Left;
            combo.Top = 27;
            label.Width = Math.Max(120, host.ClientSize.Width - label.Left - 8);
            help.Width = Math.Max(120, host.ClientSize.Width - help.Left - 8);
        };
        return host;
    }

    private Control AddToggle(CheckBox checkBox, string title, string description, RewardGlyph glyph)
    {
        var host = new Panel { Height = 145, MinimumSize = new Size(230, 145), BackColor = PanelRaised };
        var icon = new RewardIcon(glyph) { Location = new Point(8, 12), Size = new Size(36, 36) };
        checkBox.Text = title;
        checkBox.AutoSize = false;
        checkBox.Height = 30;
        checkBox.Location = new Point(54, 8);
        checkBox.ForeColor = TextMain;
        checkBox.BackColor = PanelRaised;
        checkBox.Font = new Font("Segoe UI Semibold", 9.5f);
        var help = new Label { Text = description, ForeColor = TextMuted, AutoEllipsis = false, AutoSize = false, Location = new Point(58, 35), Height = 106, UseCompatibleTextRendering = true };
        host.Controls.Add(icon); host.Controls.Add(checkBox); host.Controls.Add(help);
        host.Resize += (_, _) => { checkBox.Width = Math.Max(120, host.ClientSize.Width - 62); help.Width = Math.Max(120, host.ClientSize.Width - 66); };
        _toolTips.SetToolTip(host, description);
        _toolTips.SetToolTip(checkBox, description);
        return host;
    }

    private static Control BuildSection(string title, string description, RewardGlyph glyph, IEnumerable<Control> controls, int columns = 1)
    {
        var items = controls.ToList();
        var host = new Panel { Dock = DockStyle.Fill, BackColor = PanelRaised, Padding = new Padding(10), Margin = new Padding(6) };
        var section = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PanelRaised };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new Panel { Dock = DockStyle.Fill, BackColor = PanelRaised };
        var headerIcon = new RewardIcon(glyph) { Location = new Point(3, 8), Size = new Size(38, 38) };
        var label = new Label { Text = title, ForeColor = Gold, Font = new Font("Segoe UI Semibold", 11f), Location = new Point(50, 4), Height = 27, AutoSize = true };
        var detail = new Label { Text = description, ForeColor = TextMuted, Location = new Point(50, 31), AutoSize = false, Height = 55, UseCompatibleTextRendering = true };
        header.Controls.Add(headerIcon); header.Controls.Add(label); header.Controls.Add(detail);
        header.Resize += (_, _) => detail.Width = Math.Max(120, header.ClientSize.Width - detail.Left - 4);
        var rows = (int)Math.Ceiling(items.Count / (double)columns);
        var stack = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = columns,
            RowCount = rows,
            AutoScroll = false,
            Padding = new Padding(0, 2, 0, 0),
            BackColor = PanelRaised,
            GrowStyle = TableLayoutPanelGrowStyle.FixedSize
        };
        for (var column = 0; column < columns; column++)
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f / columns));
        var rowHeights = new float[rows];
        for (var index = 0; index < items.Count; index++)
        {
            var control = items[index];
            var rowHeight = Math.Max(88f, control.Height + 16f);
            rowHeights[index / columns] = Math.Max(rowHeights[index / columns], rowHeight);
            control.Dock = DockStyle.Fill;
            control.Margin = new Padding(4, 5, 4, 5);
            stack.Controls.Add(control, index % columns, index / columns);
        }
        foreach (var rowHeight in rowHeights)
            stack.RowStyles.Add(new RowStyle(SizeType.Absolute, rowHeight));
        section.Controls.Add(header, 0, 0);
        section.Controls.Add(stack, 0, 1);
        host.Controls.Add(section);
        return host;
    }

    private static Label Info(string text) => new() { Text = text, ForeColor = TextMuted, BackColor = PanelRaised, AutoEllipsis = false, Height = 68, Padding = new Padding(5, 7, 5, 5), UseCompatibleTextRendering = true };
    private static TabPage NewTab(string text) => new(text) { BackColor = PanelColor, ForeColor = TextMain, Padding = new Padding(5) };

    private Control PathRow(string labelText, TextBox box, int top, Action browse)
    {
        var host = new Panel { Left = 24, Top = top, Height = 76, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Width = 940, BackColor = PanelRaised };
        var label = new Label { Text = labelText, ForeColor = Gold, AutoSize = true, Location = new Point(12, 8) };
        box.Location = new Point(12, 36); box.Width = 810; box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; box.BackColor = Ink; box.ForeColor = TextMain; box.BorderStyle = BorderStyle.FixedSingle;
        var button = new Button { Text = "BROWSE", Width = 90, Height = 28, Top = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        button.Left = host.Width - button.Width - 12; StyleButton(button, Ink, Teal); button.Click += (_, _) => browse();
        host.Controls.Add(label); host.Controls.Add(box); host.Controls.Add(button);
        return host;
    }

    private void SetDefaults()
    {
        _gamePath.Text = BuildEngine.DetectGameRoot() ?? "Select your DragonSword installation";
        _outputPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DragonSword Progression QOL Builds");
        Append($"Baseline: verified unmodified game {BuildEngine.SupportedGameVersion} tables (Steam build {BuildEngine.SupportedSteamBuildId}).", Gold);
        Append("Network, telemetry, auto-update, DLL injection, and elevation: none.", Teal);
    }

    private BuildConfig ReadConfig() => new(
        SelectedValue("gathering"), SelectedValue("enemy"), SelectedValue("equipment"),
        SelectedValue("activityMaterials"), SelectedValue("emblems"), SelectedValue("gold"), SelectedValue("rankXp"),
        SelectedValue("chests"), _spread.Checked, _rarity.Checked, SelectedValue("rarityChance"));

    private int SelectedValue(string key) => _multipliers[key].Value;

    private void ApplyConfig(BuildConfig config)
    {
        _applyingProfile = true;
        try
        {
            _multipliers["gathering"].SetValue(config.WorldGatheringMultiplier);
            _multipliers["enemy"].SetValue(config.EnemyMaterialMultiplier);
            _multipliers["equipment"].SetValue(config.EquipmentMultiplier);
            _multipliers["activityMaterials"].SetValue(config.ActivityMaterialMultiplier);
            _multipliers["emblems"].SetValue(config.AdventurerEmblemMultiplier);
            _multipliers["gold"].SetValue(config.GoldMultiplier);
            _multipliers["rankXp"].SetValue(config.RankExperienceMultiplier);
            _multipliers["chests"].SetValue(config.WorldChestMultiplier);
            _multipliers["rarityChance"].SetValue((int)config.HighGradeChance);
            _spread.Checked = config.SpreadRolls;
            _rarity.Checked = config.EnhancedRarity;
        }
        finally { _applyingProfile = false; }
    }

    private void MarkConfigurationEdited()
    {
        if (_applyingProfile) return;
        _configurationDirty = true;
        UpdateProfileStatus();
    }

    private void UpdateProfileStatus()
    {
        _profileStatus.Text = _currentProfileName == "Default" && _configurationDirty
            ? "Settings: Custom (unsaved)"
            : $"Settings: {_currentProfileName}{(_configurationDirty ? " (edited)" : "")}";
        _profileStatus.Left = Math.Max(4, _profileButton.Left - _profileStatus.Width - 16);
    }

    private void ShowProfileMenu()
    {
        _profileMenu.Items.Clear();
        _profileMenu.BackColor = PanelRaised;
        _profileMenu.ForeColor = TextMain;

        var save = NewProfileMenuItem("SAVE CURRENT SETTINGS AS…", (_, _) => SaveProfile());
        var load = NewProfileMenuItem("LOAD PROFILE");
        var delete = NewProfileMenuItem("DELETE PROFILE");
        var profiles = _profiles.List();
        if (profiles.Count == 0)
        {
            load.DropDownItems.Add(new ToolStripMenuItem("No saved profiles") { Enabled = false });
            delete.DropDownItems.Add(new ToolStripMenuItem("No saved profiles") { Enabled = false });
        }
        else
        {
            foreach (var profile in profiles)
            {
                load.DropDownItems.Add(NewProfileMenuItem(profile.Name, (_, _) => LoadProfile(profile.Name)));
                delete.DropDownItems.Add(NewProfileMenuItem(profile.Name, (_, _) => DeleteProfile(profile.Name)));
            }
        }
        var open = NewProfileMenuItem("OPEN PROFILES FOLDER", (_, _) => OpenProfilesFolder());
        var import = NewProfileMenuItem("IMPORT FROM OLDER VERSION…", (_, _) => ImportProfilesFromOlderVersion());
        var reset = NewProfileMenuItem("RESET TO VANILLA DEFAULTS", (_, _) => ResetToDefaults());
        _profileMenu.Items.Add(save);
        _profileMenu.Items.Add(load);
        _profileMenu.Items.Add(delete);
        _profileMenu.Items.Add(new ToolStripSeparator());
        _profileMenu.Items.Add(reset);
        _profileMenu.Items.Add(import);
        _profileMenu.Items.Add(open);
        _profileMenu.Show(_profileButton, new Point(0, -_profileMenu.PreferredSize.Height));
    }

    private static ToolStripMenuItem NewProfileMenuItem(string text, EventHandler? click = null)
    {
        var item = new ToolStripMenuItem(text) { BackColor = PanelRaised, ForeColor = TextMain };
        if (click is not null) item.Click += click;
        return item;
    }

    private void SaveProfile()
    {
        var suggested = _currentProfileName == "Default" ? "My Settings" : _currentProfileName;
        var name = PromptForProfileName(suggested);
        if (name is null) return;
        try
        {
            var path = _profiles.GetManagedPath(name);
            if (File.Exists(path) && MessageBox.Show(this, $"Replace the saved profile ‘{name}’?", "Replace profile", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            path = _profiles.Save(name, ReadConfig());
            _currentProfileName = name.Trim();
            _configurationDirty = false;
            UpdateProfileStatus();
            Append($"Profile saved: {_currentProfileName} ({path})", Teal);
        }
        catch (Exception ex) { ShowProfileError("Profile could not be saved", ex); }
    }

    private void LoadProfile(string name)
    {
        try
        {
            var document = _profiles.Load(_profiles.GetManagedPath(name));
            ApplyConfig(document.Settings);
            _currentProfileName = document.Name;
            _configurationDirty = false;
            UpdateProfileStatus();
            Append($"Profile loaded: {_currentProfileName}. No PAK was built or installed.", Teal);
        }
        catch (Exception ex) { ShowProfileError("Profile could not be loaded", ex); }
    }

    private void DeleteProfile(string name)
    {
        if (MessageBox.Show(this, $"Delete the saved profile ‘{name}’?\n\nThis deletes only its JSON settings file. It does not change any PAK.", "Delete profile", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        try
        {
            _profiles.Delete(name);
            if (_currentProfileName.Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                _currentProfileName = "Default";
                _configurationDirty = true;
                UpdateProfileStatus();
            }
            Append($"Profile deleted: {name}. Current settings and installed PAK were not changed.", Gold);
        }
        catch (Exception ex) { ShowProfileError("Profile could not be deleted", ex); }
    }

    private void OpenProfilesFolder()
    {
        try
        {
            _profiles.EnsureDirectory();
            var start = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true };
            start.ArgumentList.Add(_profiles.ProfilesDirectory);
            Process.Start(start);
        }
        catch (Exception ex) { ShowProfileError("Profiles folder could not be opened", ex); }
    }

    private void MigrateLegacyProfiles()
    {
        try
        {
            var result = _profiles.MigrateLegacyProfiles();
            if (result.Imported > 0)
                Append($"Migrated {result.Imported} saved profile(s) from the older application folder into persistent Windows user storage. Old files were preserved.", Teal);
            if (result.Invalid > 0)
                Append($"Legacy profile migration skipped {result.Invalid} invalid JSON file(s).", Gold);
        }
        catch (Exception ex)
        {
            Append($"PROFILE MIGRATION WARNING: {ex.Message}", Gold);
        }
    }

    private void ImportProfilesFromOlderVersion()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Select an older Progression QOL application folder or its Profiles folder",
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var result = _profiles.ImportFromDirectory(dialog.SelectedPath);
            Append($"Profile import: {result.Imported} imported, {result.Skipped} already present, {result.Invalid} invalid. Source files were preserved.", result.Invalid > 0 ? Gold : Teal);
            MessageBox.Show(this,
                $"Imported: {result.Imported}\nAlready present: {result.Skipped}\nInvalid files skipped: {result.Invalid}\n\nOlder profile files were not deleted.",
                "Profile import complete",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowProfileError("Profiles could not be imported", ex); }
    }

    private void ResetToDefaults()
    {
        ApplyConfig(new BuildConfig(1, 1, 1, 1, 1, 1, 1, 1, false, false, 90m));
        _currentProfileName = "Default";
        _configurationDirty = false;
        UpdateProfileStatus();
        Append("Reward settings reset to vanilla defaults. Use Build + Install to disable this app's installed PAK and restore vanilla behavior.", Teal);
    }

    private string? PromptForProfileName(string suggested)
    {
        using var dialog = new Form
        {
            Text = "Save configuration profile",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MinimizeBox = false,
            MaximizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(470, 150),
            BackColor = PanelColor,
            ForeColor = TextMain,
            Font = Font,
            AutoScaleMode = AutoScaleMode.Dpi
        };
        var label = new Label { Text = "Profile name", AutoSize = true, ForeColor = Gold, Location = new Point(18, 16) };
        var box = new TextBox { Text = suggested, Location = new Point(18, 43), Width = 434, BackColor = Ink, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle };
        var save = new Button { Text = "SAVE PROFILE", DialogResult = DialogResult.OK, Size = new Size(132, 38), Location = new Point(320, 92) };
        var cancel = new Button { Text = "CANCEL", DialogResult = DialogResult.Cancel, Size = new Size(105, 38), Location = new Point(205, 92) };
        StyleButton(save, Ember, TextMain);
        StyleButton(cancel, Ink, TextMuted);
        dialog.Controls.Add(label); dialog.Controls.Add(box); dialog.Controls.Add(save); dialog.Controls.Add(cancel);
        dialog.AcceptButton = save;
        dialog.CancelButton = cancel;
        dialog.Shown += (_, _) => { box.SelectAll(); box.Focus(); };
        return dialog.ShowDialog(this) == DialogResult.OK ? box.Text : null;
    }

    private void ShowProfileError(string title, Exception ex)
    {
        Append($"PROFILE ERROR: {ex.Message}", Ember);
        MessageBox.Show(this, ex.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private async Task BuildAsync()
    {
        try
        {
            _build.Enabled = false; _status.Text = "Building and verifying...";
            var config = ReadConfig();
            if (IsVanillaConfig(config))
            {
                RestoreVanillaSettings();
                return;
            }
            var progress = new Progress<string>(message => { _status.Text = message; Append(message, TextMuted); });
            _latestBuild = await _engine.BuildAsync(config, _outputPath.Text, progress);
            Append($"PAK: {_latestBuild.PakPath}", TextMain);
            Append($"SHA256: {_latestBuild.Sha256}", Teal);
            Append($"Rows — gathering {_latestBuild.GatheringRows}, enemies {_latestBuild.EnemyMaterialRows}, activities {_latestBuild.ActivityRows}, chests {_latestBuild.ChestRows}, spread rewards {_latestBuild.SpreadRewardRows}.", Gold);
            _status.Text = "Build verified — not installed yet.";
            Append("BUILD VERIFIED — NOT INSTALLED YET.", Gold);
            InstallLatest();
        }
        catch (Exception ex)
        {
            Append("ERROR: " + ex.Message, Ember); _status.Text = "Build failed safely; no game files were changed.";
            MessageBox.Show(ex.Message, "Build failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally { _build.Enabled = true; }
    }

    private void RestoreVanillaSettings()
    {
        var gameRoot = BuildEngine.ResolveGameRoot(_gamePath.Text) ?? throw new DirectoryNotFoundException("DragonSword could not be found from the selected path.");
        _gamePath.Text = gameRoot;
        var installedPak = BuildEngine.FindInstalledPak(gameRoot);
        if (installedPak is null)
        {
            _status.Text = "Vanilla defaults selected — no Progression QOL PAK is installed.";
            Append("Vanilla defaults confirmed. No installed Progression QOL PAK was found, so the game was not changed.", Teal);
            MessageBox.Show(this, "All settings are already at vanilla defaults, and no Progression QOL PAK is installed.\n\nNothing needs to be removed.", "Already using vanilla defaults", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            "Every reward setting is at its vanilla default.\n\nDisable the installed Progression QOL PAK and return this mod's rewards to vanilla behavior?\n\nThe PAK will be preserved as a disabled backup. Other mods will not be changed.",
            "Restore vanilla rewards?",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
        {
            _status.Text = "Vanilla restore cancelled — the installed PAK was not changed.";
            Append("Vanilla restore cancelled. The installed PAK was not changed.", Gold);
            return;
        }

        var backup = _engine.DisableInstalledPakForVanilla(gameRoot);
        if (backup is null) throw new IOException("The installed Progression QOL PAK disappeared before it could be disabled.");
        _latestBuild = null;
        _status.Text = "Progression QOL disabled — vanilla rewards restored for this mod.";
        Append($"Vanilla restored: disabled the installed Progression QOL PAK and preserved it at {backup}", Teal);
        MessageBox.Show(this,
            "Progression QOL has been disabled and its PAK was preserved as a non-loadable backup.\n\nThis restores vanilla reward behavior for this mod. Any other installed reward mod may still affect the game.",
            "Vanilla rewards restored",
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static bool IsVanillaConfig(BuildConfig config) =>
        config.WorldGatheringMultiplier == 1 &&
        config.EnemyMaterialMultiplier == 1 &&
        config.EquipmentMultiplier == 1 &&
        config.ActivityMaterialMultiplier == 1 &&
        config.AdventurerEmblemMultiplier == 1 &&
        config.GoldMultiplier == 1 &&
        config.RankExperienceMultiplier == 1 &&
        config.WorldChestMultiplier == 1 &&
        !config.SpreadRolls &&
        !config.EnhancedRarity;

    private void InstallLatest()
    {
        if (_latestBuild is null)
        {
            MessageBox.Show("Build a PAK before installing.", "No verified build", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var gameRoot = BuildEngine.ResolveGameRoot(_gamePath.Text) ?? throw new DirectoryNotFoundException("DragonSword could not be found from the selected path.");
            _gamePath.Text = gameRoot;
            var conflicts = FindKnownConflicts(gameRoot);
            var warning = conflicts.Count == 0 ? "" : "\n\nConflicting reward/material PAKs detected:\n" + string.Join("\n", conflicts.Select(Path.GetFileName)) + "\n\nThe installer will NOT remove them. Disable those PAKs before launching the game.";
            if (MessageBox.Show("The new PAK is built and verified, but it is not installed yet.\n\nInstall it now? The existing Progression QOL PAK will be disabled and backed up." + warning, "Install verified build", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                Append("Installation skipped. The installed PAK was not changed.", Gold);
                _status.Text = "Build verified — installation was skipped.";
                return;
            }
            var destination = _engine.Install(_latestBuild, gameRoot);
            Append("Installed: " + destination, Teal);
            Append("Installed SHA256 verified: " + _latestBuild.Sha256, Teal);
            _status.Text = "Installed and hash-verified. Resolve any logged conflicts before launch.";
        }
        catch (Exception ex) { Append("INSTALL ERROR: " + ex.Message, Ember); MessageBox.Show(ex.Message, "Install failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ScanConflicts()
    {
        var gameRoot = BuildEngine.ResolveGameRoot(_gamePath.Text);
        if (gameRoot is null)
        {
            _conflictSummary.Text = "DragonSword installation not found from the selected path.";
            _conflictSummary.ForeColor = Ember;
            Append("Conflict scan stopped: the selected path could not be resolved to DragonSword.", Ember);
            return;
        }
        _gamePath.Text = gameRoot;
        var buildId = BuildEngine.ReadInstalledSteamBuildId(gameRoot);
        if (buildId is null)
        {
            _conflictSummary.Text = "Steam build could not be verified; automatic installation will stay blocked.";
            _conflictSummary.ForeColor = Ember;
            Append("Game detected, but Steam build ID could not be verified.", Ember);
            return;
        }
        else if (!buildId.Equals(BuildEngine.SupportedSteamBuildId, StringComparison.Ordinal))
        {
            _conflictSummary.Text = $"Game build {buildId} is unsupported. Required: {BuildEngine.SupportedSteamBuildId}.";
            _conflictSummary.ForeColor = Ember;
            Append($"Unsupported game build {buildId}; automatic installation requires {BuildEngine.SupportedSteamBuildId}.", Ember);
            return;
        }
        var conflicts = FindKnownConflicts(gameRoot);
        if (conflicts.Count == 0)
        {
            _conflictSummary.Text = "No known reward or material conflicts found.";
            _conflictSummary.ForeColor = Teal;
            Append("Conflict scan: no installed PAK contains a managed reward/material table.", Teal);
        }
        else
        {
            _conflictSummary.Text = $"{conflicts.Count} possible reward/material conflict(s) found. Review the Audit Log.";
            _conflictSummary.ForeColor = Ember;
            Append($"Conflict scan: {conflicts.Count} reward/material PAK(s) require manual disabling:", Ember);
            foreach (var file in conflicts) Append("  " + file, Ember);
        }
        var paksRoot = Path.Combine(gameRoot, "DS", "Content", "Paks");
        var respawn = Directory.EnumerateFiles(paksRoot, "DS_TreasureRespawn*.pak", SearchOption.TopDirectoryOnly)
            .Concat(Directory.Exists(Path.Combine(paksRoot, "~mods")) ? Directory.EnumerateFiles(Path.Combine(paksRoot, "~mods"), "DS_TreasureRespawn*.pak", SearchOption.TopDirectoryOnly) : [])
            .FirstOrDefault();
        if (respawn is not null)
        {
            if (conflicts.Count == 0) _conflictSummary.Text += " Treasure Respawn is compatible.";
            Append("Treasure Respawn detected: compatible file topology (SectionTreasureBoxData only).", Teal);
        }
    }

    internal void PrepareScreenshot(int index, bool showcase)
    {
        _tabs.SelectedIndex = Math.Clamp(index, 0, _tabs.TabCount - 1);
        _gamePath.Text = @"C:\Games\DragonSword Awakening";
        _outputPath.Text = @"C:\Users\Player\Documents\DragonSword Progression QOL Builds";
        _conflictSummary.Text = "No known reward or material conflicts found.";
        _conflictSummary.ForeColor = Teal;
        if (!showcase) return;

        SetScreenshotValue("gathering", 5);
        SetScreenshotValue("enemy", 3);
        SetScreenshotValue("chests", 5);
        SetScreenshotValue("equipment", 5);
        SetScreenshotValue("activityMaterials", 10);
        SetScreenshotValue("emblems", 5);
        SetScreenshotValue("gold", 10);
        SetScreenshotValue("rankXp", 5);
        _spread.Checked = true;
        _rarity.Checked = true;
    }

    private void SetScreenshotValue(string key, int value)
    {
        if (_multipliers.TryGetValue(key, out var choice)) choice.SetValue(value);
    }

    private List<string> FindKnownConflicts(string gameRoot)
    {
        var paksRoot = Path.Combine(gameRoot, "DS", "Content", "Paks");
        if (!Directory.Exists(paksRoot)) return [];
        var owned = "DS_ZZZ_ProgressionQoL_Configured_P.pak";
        var directories = new[] { paksRoot, Path.Combine(paksRoot, "~mods") }.Where(Directory.Exists);
        var candidates = directories.SelectMany(x => Directory.EnumerateFiles(x, "*.pak", SearchOption.TopDirectoryOnly))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Where(x => !Path.GetFileName(x).Equals(owned, StringComparison.OrdinalIgnoreCase))
            .Where(x => !Path.GetFileName(x).StartsWith("pakchunk", StringComparison.OrdinalIgnoreCase))
            .Where(x => !Path.GetFileName(x).StartsWith("DS_TreasureRespawn", StringComparison.OrdinalIgnoreCase))
            .Where(x => RegexLike(Path.GetFileName(x), "Dungeon", "Reward", "Material", "Combined", "AllInOne", "ProgressionQoL", "Loot", "Drop"));
        var conflicts = new List<string>();
        foreach (var pak in candidates)
        {
            try
            {
                if (_engine.PakTouchesManagedTables(pak)) conflicts.Add(pak);
            }
            catch
            {
                if (RegexLike(Path.GetFileName(pak), "Dungeon", "Reward", "Material", "AllInOne", "Combined", "ProgressionQoL"))
                    conflicts.Add(pak);
            }
        }
        return conflicts.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool RegexLike(string value, params string[] terms) => terms.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));

    private void BrowseGame()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select DragonSword Awakening, DS, Paks, ~mods, or Win64", UseDescriptionForTitle = true, SelectedPath = Directory.Exists(_gamePath.Text) ? _gamePath.Text : "" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gamePath.Text = BuildEngine.ResolveGameRoot(dialog.SelectedPath) ?? dialog.SelectedPath;
            ScanConflicts();
        }
    }

    private void BrowseOutput()
    {
        using var dialog = new FolderBrowserDialog { Description = "Select where verified builds will be created", UseDescriptionForTitle = true, SelectedPath = Directory.Exists(_outputPath.Text) ? _outputPath.Text : "" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _outputPath.Text = dialog.SelectedPath;
    }

    private void Append(string text, Color color)
    {
        _log.SelectionStart = _log.TextLength; _log.SelectionColor = color; _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n"); _log.SelectionColor = _log.ForeColor; _log.ScrollToCaret();
    }

    private static void StyleButton(Button button, Color back, Color fore)
    {
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = fore; button.FlatAppearance.BorderSize = 1; button.BackColor = back; button.ForeColor = fore; button.Font = new Font("Segoe UI Semibold", 9f); button.Cursor = Cursors.Hand;
    }

    private sealed record Choice(int Value, string Label)
    {
        public override string ToString() => Label;
    }

    private sealed class CrispBorderButton : Button
    {
        public Color BorderColor { get; set; } = Teal;

        public CrispBorderButton()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaint(PaintEventArgs paintEvent)
        {
            base.OnPaint(paintEvent);
            if (ClientSize.Width < 2 || ClientSize.Height < 2) return;
            using var border = new Pen(BorderColor, 1f);
            paintEvent.Graphics.DrawRectangle(border, 0, 0, ClientSize.Width - 1, ClientSize.Height - 1);
        }
    }

    private sealed class MultiplierDropDown : Button
    {
        private readonly ContextMenuStrip _menu = new();
        private readonly IReadOnlyList<Choice> _choices;

        public int Value { get; private set; }
        public event EventHandler? ValueChanged;

        public MultiplierDropDown(IReadOnlyList<Choice> choices, int selectedValue)
        {
            _choices = choices;
            AccessibleRole = AccessibleRole.ComboBox;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(92, 111, 119);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 55, 61);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(54, 67, 73);
            BackColor = Ink;
            ForeColor = TextMain;
            Font = new Font("Segoe UI Semibold", 9f);
            TextAlign = ContentAlignment.MiddleLeft;
            Padding = new Padding(8, 0, 24, 0);
            UseVisualStyleBackColor = false;

            _menu.BackColor = PanelRaised;
            _menu.ForeColor = TextMain;
            _menu.ShowImageMargin = true;
            _menu.Font = Font;
            foreach (var choice in choices)
            {
                var item = new ToolStripMenuItem(choice.Label) { ForeColor = TextMain, BackColor = PanelRaised, Tag = choice };
                item.Click += (_, _) => SelectChoice((Choice)item.Tag!);
                _menu.Items.Add(item);
            }
            SelectChoice(choices.First(x => x.Value == selectedValue));
            Click += (_, _) => _menu.Show(this, new Point(0, Height));
        }

        private void SelectChoice(Choice choice)
        {
            var changed = Value != choice.Value;
            Value = choice.Value;
            Text = choice.Label + "   ▼";
            AccessibleName = choice.Label;
            foreach (ToolStripMenuItem item in _menu.Items)
                item.Checked = ((Choice)item.Tag!).Value == Value;
            if (changed) ValueChanged?.Invoke(this, EventArgs.Empty);
        }

        public void SetValue(int value) => SelectChoice(_choices.First(x => x.Value == value));

        protected override void Dispose(bool disposing)
        {
            if (disposing) _menu.Dispose();
            base.Dispose(disposing);
        }
    }

    private enum RewardGlyph
    {
        World,
        Activities,
        LootShape,
        Gathering,
        Enemy,
        Chest,
        Equipment,
        Material,
        Emblem,
        Gold,
        Experience,
        Spread,
        Rarity,
        Build,
        GameFolder,
        OutputFolder,
        Scan
    }

    private sealed class RewardIcon : Control
    {
        private readonly RewardGlyph _glyph;

        public RewardIcon(RewardGlyph glyph)
        {
            _glyph = glyph;
            DoubleBuffered = true;
            BackColor = PanelRaised;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            e.Graphics.ScaleTransform(Width / 36f, Height / 36f);
            using var background = new SolidBrush(Color.FromArgb(22, 28, 32));
            using var border = new Pen(Color.FromArgb(115, Gold), 1.2f);
            using var pen = new Pen(_glyph is RewardGlyph.World or RewardGlyph.Activities or RewardGlyph.LootShape ? Gold : Teal, 2.1f)
            {
                StartCap = LineCap.Round,
                EndCap = LineCap.Round,
                LineJoin = LineJoin.Round
            };
            e.Graphics.FillEllipse(background, 1.5f, 1.5f, 33f, 33f);
            e.Graphics.DrawEllipse(border, 1.5f, 1.5f, 33f, 33f);

            switch (_glyph)
            {
                case RewardGlyph.World:
                    e.Graphics.DrawEllipse(pen, 9, 9, 18, 18);
                    e.Graphics.DrawArc(pen, 13, 9, 10, 18, 90, 180);
                    e.Graphics.DrawArc(pen, 13, 9, 10, 18, 270, 180);
                    e.Graphics.DrawLine(pen, 9, 18, 27, 18);
                    break;
                case RewardGlyph.Activities:
                    e.Graphics.DrawLine(pen, 11, 9, 25, 27);
                    e.Graphics.DrawLine(pen, 25, 9, 11, 27);
                    e.Graphics.DrawLine(pen, 8, 23, 14, 29);
                    e.Graphics.DrawLine(pen, 28, 23, 22, 29);
                    break;
                case RewardGlyph.LootShape:
                case RewardGlyph.Spread:
                    e.Graphics.DrawLine(pen, 18, 11, 11, 24);
                    e.Graphics.DrawLine(pen, 18, 11, 18, 25);
                    e.Graphics.DrawLine(pen, 18, 11, 25, 24);
                    e.Graphics.FillEllipse(pen.Brush, 15, 8, 6, 6);
                    e.Graphics.FillEllipse(pen.Brush, 8, 22, 6, 6);
                    e.Graphics.FillEllipse(pen.Brush, 15, 22, 6, 6);
                    e.Graphics.FillEllipse(pen.Brush, 22, 22, 6, 6);
                    break;
                case RewardGlyph.Gathering:
                    using (var path = new GraphicsPath())
                    {
                        path.AddBezier(10, 24, 8, 11, 23, 8, 27, 10);
                        path.AddBezier(27, 10, 27, 23, 18, 28, 10, 24);
                        e.Graphics.DrawPath(pen, path);
                        e.Graphics.DrawLine(pen, 11, 25, 24, 12);
                    }
                    break;
                case RewardGlyph.Enemy:
                    e.Graphics.DrawEllipse(pen, 10, 7, 16, 16);
                    e.Graphics.FillEllipse(pen.Brush, 13, 13, 4, 4);
                    e.Graphics.FillEllipse(pen.Brush, 20, 13, 4, 4);
                    e.Graphics.DrawLine(pen, 18, 17, 16, 21);
                    e.Graphics.DrawLine(pen, 16, 21, 20, 21);
                    e.Graphics.DrawLine(pen, 12, 22, 12, 27);
                    e.Graphics.DrawLine(pen, 24, 22, 24, 27);
                    e.Graphics.DrawLine(pen, 12, 27, 24, 27);
                    e.Graphics.DrawLine(pen, 16, 23, 16, 27);
                    e.Graphics.DrawLine(pen, 20, 23, 20, 27);
                    break;
                case RewardGlyph.Chest:
                    e.Graphics.DrawRectangle(pen, 8, 15, 20, 13);
                    e.Graphics.DrawArc(pen, 8, 9, 20, 12, 180, 180);
                    e.Graphics.DrawLine(pen, 8, 20, 28, 20);
                    e.Graphics.FillEllipse(pen.Brush, 16, 22, 4, 4);
                    break;
                case RewardGlyph.Equipment:
                    e.Graphics.DrawArc(pen, 8, 7, 20, 21, 180, 180);
                    e.Graphics.DrawLine(pen, 8, 17, 28, 17);
                    e.Graphics.DrawRectangle(pen, 11, 18, 14, 6);
                    e.Graphics.DrawLine(pen, 11, 24, 11, 28);
                    e.Graphics.DrawLine(pen, 25, 24, 25, 28);
                    e.Graphics.DrawLine(pen, 16, 18, 16, 24);
                    e.Graphics.DrawLine(pen, 18, 7, 22, 4);
                    break;
                case RewardGlyph.Material:
                    e.Graphics.DrawPolygon(pen, [new PointF(18, 7), new PointF(27, 15), new PointF(23, 28), new PointF(13, 28), new PointF(9, 15)]);
                    e.Graphics.DrawLine(pen, 9, 15, 27, 15);
                    e.Graphics.DrawLine(pen, 18, 7, 13, 28);
                    e.Graphics.DrawLine(pen, 18, 7, 23, 28);
                    break;
                case RewardGlyph.Emblem:
                    e.Graphics.DrawEllipse(pen, 9, 7, 18, 22);
                    e.Graphics.DrawEllipse(pen, 13, 11, 10, 10);
                    e.Graphics.DrawLine(pen, 18, 13, 18, 19);
                    e.Graphics.DrawLine(pen, 15, 16, 21, 16);
                    e.Graphics.DrawLine(pen, 12, 25, 9, 30);
                    e.Graphics.DrawLine(pen, 24, 25, 27, 30);
                    break;
                case RewardGlyph.Gold:
                    e.Graphics.DrawEllipse(pen, 9, 9, 18, 7);
                    e.Graphics.DrawArc(pen, 9, 13, 18, 8, 0, 180);
                    e.Graphics.DrawArc(pen, 9, 18, 18, 8, 0, 180);
                    e.Graphics.DrawLine(pen, 9, 12, 9, 23);
                    e.Graphics.DrawLine(pen, 27, 12, 27, 23);
                    break;
                case RewardGlyph.Experience:
                    e.Graphics.DrawRectangle(pen, 11, 9, 14, 18);
                    e.Graphics.DrawArc(pen, 8, 7, 7, 6, 90, 180);
                    e.Graphics.DrawArc(pen, 21, 23, 7, 6, 270, 180);
                    e.Graphics.DrawLine(pen, 14, 15, 22, 15);
                    e.Graphics.DrawLine(pen, 14, 19, 22, 19);
                    break;
                case RewardGlyph.Rarity:
                    var points = Enumerable.Range(0, 10).Select(i =>
                    {
                        var radius = i % 2 == 0 ? 11f : 5f;
                        var angle = -Math.PI / 2 + i * Math.PI / 5;
                        return new PointF(18 + radius * (float)Math.Cos(angle), 18 + radius * (float)Math.Sin(angle));
                    }).ToArray();
                    e.Graphics.DrawPolygon(pen, points);
                    break;
                case RewardGlyph.Build:
                    e.Graphics.DrawRectangle(pen, 9, 9, 18, 18);
                    e.Graphics.DrawLine(pen, 13, 14, 23, 14);
                    e.Graphics.DrawLine(pen, 13, 18, 23, 18);
                    e.Graphics.DrawLine(pen, 18, 22, 18, 29);
                    e.Graphics.DrawLine(pen, 14, 25, 18, 29);
                    e.Graphics.DrawLine(pen, 22, 25, 18, 29);
                    break;
                case RewardGlyph.GameFolder:
                case RewardGlyph.OutputFolder:
                    e.Graphics.DrawLine(pen, 8, 13, 15, 13);
                    e.Graphics.DrawLine(pen, 15, 13, 18, 16);
                    e.Graphics.DrawLine(pen, 18, 16, 28, 16);
                    e.Graphics.DrawRectangle(pen, 8, 16, 20, 12);
                    if (_glyph == RewardGlyph.OutputFolder)
                    {
                        e.Graphics.DrawLine(pen, 18, 9, 18, 22);
                        e.Graphics.DrawLine(pen, 14, 18, 18, 22);
                        e.Graphics.DrawLine(pen, 22, 18, 18, 22);
                    }
                    break;
                case RewardGlyph.Scan:
                    e.Graphics.DrawEllipse(pen, 8, 8, 15, 15);
                    e.Graphics.DrawLine(pen, 21, 21, 29, 29);
                    e.Graphics.DrawLine(pen, 12, 15, 19, 15);
                    e.Graphics.DrawLine(pen, 15.5f, 11.5f, 15.5f, 18.5f);
                    break;
            }
        }
    }

    private sealed class BannerPanel : Panel
    {
        public BannerPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (BackgroundImage is null) { base.OnPaintBackground(e); return; }
            e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            var scale = Math.Max((float)ClientSize.Width / BackgroundImage.Width, (float)ClientSize.Height / BackgroundImage.Height);
            var size = new SizeF(BackgroundImage.Width * scale, BackgroundImage.Height * scale);
            e.Graphics.DrawImage(BackgroundImage, new RectangleF(0, (ClientSize.Height - size.Height) * 0.50f, size.Width, size.Height));
            using var overlay = new LinearGradientBrush(ClientRectangle, Color.FromArgb(170, 8, 13, 18), Color.FromArgb(35, 8, 13, 16), LinearGradientMode.Horizontal);
            e.Graphics.FillRectangle(overlay, ClientRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
            using var titleFont = new Font("Segoe UI", 27f, FontStyle.Bold);
            using var creditFont = new Font("Segoe UI", 10f, FontStyle.Bold);
            using var buildFont = new Font("Segoe UI", 9.5f, FontStyle.Regular);
            using var titleBrush = new SolidBrush(TextMain);
            using var creditBrush = new SolidBrush(Gold);

            const float titleY = 5f;
            var creditY = titleY + titleFont.GetHeight(e.Graphics) + 4f;
            var buildY = creditY + creditFont.GetHeight(e.Graphics) + 3f;
            e.Graphics.DrawString("PROGRESSION QOL", titleFont, titleBrush, 34f, titleY);
            e.Graphics.DrawString("DRAGONSWORD REWARD CONFIGURATOR  •  CREATED BY NECTARINES", creditFont, creditBrush, 38f, creditY);
            e.Graphics.DrawString("VERSION 0.9.2 RC3  •  BUILT FOR GAME 1.0.9  •  ONE CUSTOM PAK  •  OFFLINE", buildFont, titleBrush, 39f, buildY);
        }
    }
}
