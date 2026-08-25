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
    private readonly LocalizationService _text;
    private readonly string _uiFontFamily;
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
    private readonly ComboBox _languageChoice = new();
    private BuildResult? _latestBuild;
    private string _currentProfileName = "Default";
    private bool _configurationDirty;
    private bool _applyingProfile;

    public MainForm(bool startMaximized = false, string? localeOverride = null, bool scanOnStart = true)
    {
        _text = new LocalizationService(AppContext.BaseDirectory, localeOverride);
        _uiFontFamily = _text.CurrentLanguage.FontFamily;
        Text = _text.Text("app.windowTitle", "DragonSword Progression QOL — 1.0.10 Release Candidate");
        MinimumSize = new Size(1000, 700);
        var working = Screen.PrimaryScreen?.WorkingArea ?? new Rectangle(0, 0, 1600, 1000);
        Size = new Size(Math.Min(1480, working.Width - 96), Math.Min(1000, working.Height - 72));
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Ink;
        ForeColor = TextMain;
        Font = UiFont(10f);
        AutoScaleDimensions = new SizeF(96f, 96f);
        AutoScaleMode = AutoScaleMode.Dpi;
        if (startMaximized) WindowState = FormWindowState.Maximized;
        _engine = new BuildEngine(AppContext.BaseDirectory);
        _profiles = new ProfileStore(AppContext.BaseDirectory);
        BuildLayout();
        SetDefaults();
        MigrateLegacyProfiles();
        if (scanOnStart) ScanConflicts();
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
        _tabs.TabPages.Add(BuildLanguageTab());
        _tabs.TabPages.Add(BuildAuditTab());
        shell.Controls.Add(_tabs, 0, 1);

        var footer = new Panel { Dock = DockStyle.Fill, BackColor = Ink };
        _status.Text = _text.Text("status.ready", "Ready — builds are local, offline, and reversible.");
        _status.ForeColor = TextMuted;
        _status.AutoSize = true;
        _status.Location = new Point(4, 20);
        footer.Controls.Add(_status);

        _build.Text = _text.Text("footer.build", "BUILD + INSTALL");
        StyleButton(_build, Ember, TextMain);
        _build.Size = new Size(Math.Max(180, TextRenderer.MeasureText(_build.Text, _build.Font).Width + 38), 42);
        _build.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _build.Location = new Point(footer.Width - 170, 8);
        _build.Click += async (_, _) => await BuildAsync();
        footer.Controls.Add(_build);

        _profileButton.Text = _text.Text("footer.profiles", "PROFILES");
        StyleButton(_profileButton, Ink, Teal);
        _profileButton.FlatAppearance.BorderSize = 0;
        _profileButton.BorderColor = Teal;
        _profileButton.Size = new Size(Math.Max(126, TextRenderer.MeasureText(_profileButton.Text, _profileButton.Font).Width + 38), 42);
        _profileButton.Top = 8;
        _profileButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _profileButton.Click += (_, _) => ShowProfileMenu();
        footer.Controls.Add(_profileButton);

        _profileStatus.Text = _text.Text("settings.default", "Settings: Default");
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
        var banner = new BannerPanel(_text, _uiFontFamily) { Dock = DockStyle.Fill, BackColor = PanelColor, BackgroundImageLayout = ImageLayout.Zoom };
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
        var tab = NewTab(_text.Text("tab.rewards", "Rewards"));
        tab.AutoScroll = true;
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, MinimumSize = new Size(900, 680), ColumnCount = 3, RowCount = 1, Padding = new Padding(8), BackColor = PanelColor };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40f));
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30f));
        layout.Controls.Add(BuildSection(
            _text.Text("section.world.title", "WORLD"),
            _text.Text("section.world.description", "Exploration rewards. x1 is vanilla; x5 gives five times the amount."),
            RewardGlyph.World,
            [
                AddMultiplier(_text.Text("reward.gathering.title", "World gathering"), "gathering", 1, 20, _text.Text("reward.gathering.description", "Plants, ore, and cooking-ingredient nodes."), RewardGlyph.Gathering),
                AddMultiplier(_text.Text("reward.enemy.title", "Enemy materials"), "enemy", 1, 20, _text.Text("reward.enemy.description", "Material drops from ordinary and field enemies."), RewardGlyph.Enemy),
                AddMultiplier(_text.Text("reward.chests.title", "Safe chest stacks"), "chests", 1, 10, _text.Text("reward.chests.description", "Stackable loot from one-time world exploration chests. Dungeon chests, unique items, and progression items stay unchanged."), RewardGlyph.Chest)
            ]), 0, 0);
        layout.Controls.Add(BuildSection(
            _text.Text("section.activities.title", "ACTIVITIES"),
            _text.Text("section.activities.description", "Dungeons, hunts, raids, and Sudden Missions. Gold and Crafting Materials include the two underwater dungeons."),
            RewardGlyph.Activities,
            [
                AddMultiplier(_text.Text("reward.equipment.title", "Equipment"), "equipment", 1, 10, _text.Text("reward.equipment.description", "Activity gear. x10 max. Inventory: 500 items."), RewardGlyph.Equipment),
                AddMultiplier(_text.Text("reward.materials.title", "Crafting materials"), "activityMaterials", 1, 20, _text.Text("reward.materials.description", "Trait stones, boss parts, Raid runes, upgrades, and underwater loot."), RewardGlyph.Material),
                AddMultiplier(_text.Text("reward.emblems.title", "Emblems"), "emblems", 1, 20, _text.Text("reward.emblems.description", "Exchange Shop currency; Sudden Missions included."), RewardGlyph.Emblem),
                AddMultiplier(_text.Text("reward.gold.title", "Gold"), "gold", 1, 20, _text.Text("reward.gold.description", "Gold from activity completions."), RewardGlyph.Gold),
                AddMultiplier(_text.Text("reward.experience.title", "Experience"), "rankXp", 1, 20, _text.Text("reward.experience.description", "Mercenary Corps Rank EXP."), RewardGlyph.Experience),
                AddMultiplier(_text.Text("reward.currencyXp.title", "Currency EXP items"), "currencyXpItems", 1, 20, _text.Text("reward.currencyXp.description", "Character, Equipment, and Karma EXP items from Currency Dungeons only."), RewardGlyph.Experience)
            ], 2), 1, 0);

        _spread.Checked = false;
        _rarity.Checked = false;
        var rarityChance = AddPercent(_text.Text("loot.chance.title", "Better-tier chance"), "rarityChance", 90, _text.Text("loot.chance.description", "When enabled, about 9 of 10 supported rolls select the better tier."), RewardGlyph.Rarity);
        _rarity.CheckedChanged += (_, _) => rarityChance.Enabled = _rarity.Checked;
        _rarity.CheckedChanged += (_, _) => MarkConfigurationEdited();
        _spread.CheckedChanged += (_, _) => MarkConfigurationEdited();
        rarityChance.Enabled = false;
        layout.Controls.Add(BuildSection(
            _text.Text("section.lootShape.title", "LOOT SHAPE"),
            _text.Text("section.lootShape.description", "Changes equipment rolls; neither option adds rewards by itself."),
            RewardGlyph.LootShape,
            [
                AddToggle(_spread, _text.Text("loot.spread.title", "Spread equipment rolls"), _text.Text("loot.spread.description", "Off (vanilla): multiplied gear stays stacked. On: the same total is split into independent equipment selections for more variety. Equipment only; Raid runes and item stats are unchanged."), RewardGlyph.Spread),
                AddToggle(_rarity, _text.Text("loot.rarity.title", "Favor better rarity"), _text.Text("loot.rarity.description", "Off (vanilla): original rarity odds. On: supported mixed-tier equipment and Raid rune pools favor their better listed tier. Quantity and item stats are unchanged."), RewardGlyph.Rarity),
                rarityChance
            ]), 2, 0);
        tab.Controls.Add(layout);
        return tab;
    }

    private TabPage BuildPathsTab()
    {
        var tab = NewTab(_text.Text("tab.build", "Build & Install"));
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelColor, Padding = new Padding(22), ColumnCount = 1, RowCount = 5 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 92));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 178));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        layout.Controls.Add(BuildPageHeader(), 0, 0);
        layout.Controls.Add(BuildPathCard(_text.Text("build.gamePath.title", "DragonSword installation"), _text.Text("build.gamePath.description", "Select the game folder—or DS, Paks, ~mods, Win64, or the game EXE. It is normalized automatically; installation goes to DS\\Content\\Paks\\~mods."), _gamePath, BrowseGame, RewardGlyph.GameFolder), 0, 1);
        layout.Controls.Add(BuildPathCard(_text.Text("build.outputPath.title", "Build output folder"), _text.Text("build.outputPath.description", "Defaults to this Windows user's Documents folder. Verified PAKs and reports are created here; any writable folder can be selected."), _outputPath, BrowseOutput, RewardGlyph.OutputFolder), 0, 2);
        layout.Controls.Add(BuildScanCard(), 0, 3);
        tab.Controls.Add(layout);
        return tab;
    }

    private Control BuildPageHeader()
    {
        var host = new Panel { Dock = DockStyle.Fill, BackColor = PanelColor, Padding = new Padding(6) };
        var icon = new RewardIcon(RewardGlyph.Build) { Location = new Point(8, 12), Size = new Size(42, 42), BackColor = PanelColor };
        var title = new Label { Text = _text.Text("build.header.title", "BUILD & INSTALL"), UseMnemonic = false, ForeColor = Gold, Font = UiFont(13f, FontStyle.Bold), AutoSize = true, Location = new Point(62, 8) };
        var detail = new Label
        {
            Text = _text.Text("build.header.description", "Choose settings, then use Build + Install. The verified PAK replaces only this app's previous PAK after confirmation; other mods are never changed."),
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

    private Control BuildPathCard(string titleText, string description, TextBox box, Action browse, RewardGlyph glyph)
    {
        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, Margin = new Padding(4, 5, 4, 5), Padding = new Padding(12), ColumnCount = 2, RowCount = 1 };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var icon = new RewardIcon(glyph) { Dock = DockStyle.Top, Size = new Size(40, 40), Margin = new Padding(2, 8, 8, 0) };
        var content = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, ColumnCount = 1, RowCount = 3 };
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        content.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
        var title = new Label { Text = titleText, ForeColor = Gold, Font = UiFont(10f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var detail = new Label { Text = description, ForeColor = TextMuted, Dock = DockStyle.Fill, UseCompatibleTextRendering = true };
        var pathRow = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, BackColor = PanelRaised };
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        pathRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
        box.Dock = DockStyle.Fill;
        box.BackColor = Ink;
        box.ForeColor = TextMain;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = UiFont(9.5f);
        box.Margin = new Padding(0, 2, 10, 2);
        var button = new Button { Text = _text.Text("build.browse", "BROWSE"), Dock = DockStyle.Fill, Margin = new Padding(0) };
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
        var title = new Label { Text = _text.Text("scan.title", "Installed mod check"), ForeColor = Gold, Font = UiFont(10f, FontStyle.Bold), Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft };
        var detail = new Label { Text = _text.Text("scan.description", "Scans mod PAKs in DS\\Content\\Paks and ~mods, then verifies their internal table paths. It never disables, deletes, or moves another mod."), ForeColor = TextMuted, Dock = DockStyle.Fill, UseCompatibleTextRendering = true };
        var scan = new Button { Text = _text.Text("scan.button", "SCAN INSTALLED MODS"), Dock = DockStyle.Fill, Margin = new Padding(0, 6, 14, 6) };
        StyleButton(scan, Ink, Teal);
        scan.Click += (_, _) => ScanConflicts();
        _conflictSummary.Text = _text.Text("scan.notScanned", "Not scanned yet.");
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

    private TabPage BuildLanguageTab()
    {
        var tab = NewTab(_text.Text("tab.language", "Language"));
        var page = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            BackColor = PanelColor,
            Padding = new Padding(28),
            ColumnCount = 1,
            RowCount = 4
        };
        page.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 190));
        page.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
        page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var header = new Panel { Dock = DockStyle.Fill, BackColor = PanelColor };
        var icon = new RewardIcon(RewardGlyph.World) { Location = new Point(6, 11), Size = new Size(42, 42), BackColor = PanelColor };
        var title = new Label { Text = _text.Text("language.header.title", "INTERFACE LANGUAGE"), ForeColor = Gold, Font = UiFont(13f, FontStyle.Bold), AutoSize = true, Location = new Point(62, 8) };
        var description = new Label
        {
            Text = _text.Text("language.header.description", "Choose the language used by the configurator. Language files are plain UTF-8 JSON and can be edited or shared without recompiling the application."),
            ForeColor = TextMuted,
            Location = new Point(62, 38),
            Height = 54,
            AutoSize = false,
            UseCompatibleTextRendering = true
        };
        header.Controls.Add(icon); header.Controls.Add(title); header.Controls.Add(description);
        header.Resize += (_, _) => description.Width = Math.Max(300, header.ClientSize.Width - description.Left - 12);

        var card = new TableLayoutPanel { Dock = DockStyle.Fill, BackColor = PanelRaised, Margin = new Padding(4, 6, 4, 6), Padding = new Padding(20), ColumnCount = 2, RowCount = 3 };
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        card.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        card.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        var currentTitle = new Label { Text = _text.Text("language.current.title", "Language for next launch"), ForeColor = Gold, Font = UiFont(10f, FontStyle.Bold), Dock = DockStyle.Fill };
        var currentDescription = new Label { Text = _text.Text("language.current.description", "English is the fallback for missing text. Initial translations are community-editable and may be improved over time."), ForeColor = TextMuted, Dock = DockStyle.Fill, UseCompatibleTextRendering = true };
        _languageChoice.DropDownStyle = ComboBoxStyle.DropDownList;
        _languageChoice.BackColor = Ink;
        _languageChoice.ForeColor = TextMain;
        _languageChoice.Font = UiFont(10f);
        _languageChoice.Dock = DockStyle.Fill;
        _languageChoice.Margin = new Padding(0, 6, 12, 8);
        foreach (var language in _text.AvailableLanguages) _languageChoice.Items.Add(language);
        _languageChoice.SelectedItem = _languageChoice.Items.Cast<LanguageOption>().First(x => x.Locale.Equals(_text.CurrentLocale, StringComparison.OrdinalIgnoreCase));
        var save = new Button { Text = _text.Text("language.save", "SAVE LANGUAGE"), Dock = DockStyle.Fill, Margin = new Padding(0, 6, 0, 8) };
        StyleButton(save, Ember, TextMain);
        save.Click += (_, _) => SaveLanguageSelection();
        card.Controls.Add(currentTitle, 0, 0); card.SetColumnSpan(currentTitle, 2);
        card.Controls.Add(currentDescription, 0, 1); card.SetColumnSpan(currentDescription, 2);
        card.Controls.Add(_languageChoice, 0, 2);
        card.Controls.Add(save, 1, 2);

        var openFiles = new Button { Text = _text.Text("language.openFolder", "OPEN LANGUAGE FILES"), Height = 42, Anchor = AnchorStyles.Left | AnchorStyles.Top, Margin = new Padding(4, 10, 0, 0) };
        StyleButton(openFiles, Ink, Teal);
        openFiles.Width = Math.Max(220, TextRenderer.MeasureText(openFiles.Text, openFiles.Font).Width + 42);
        openFiles.Click += (_, _) => OpenLanguagesFolder();
        var note = new Label { Text = _text.Text("language.fileNote", "To improve a translation, edit its JSON file in the Languages folder using a UTF-8 text editor. Keep the keys unchanged; only edit values inside strings."), ForeColor = TextMuted, Dock = DockStyle.Fill, Padding = new Padding(4, 4, 4, 4), UseCompatibleTextRendering = true };

        page.Controls.Add(header, 0, 0);
        page.Controls.Add(card, 0, 1);
        page.Controls.Add(openFiles, 0, 2);
        page.Controls.Add(note, 0, 3);
        tab.Controls.Add(page);
        return tab;
    }

    private void SaveLanguageSelection()
    {
        if (_languageChoice.SelectedItem is not LanguageOption language) return;
        _text.SaveLocale(language.Locale);
        MessageBox.Show(this,
            _text.Format("language.savedMessage", "{language} will be used the next time Progression QOL starts. Close and reopen the application to apply it.", ("language", language.NativeName)),
            _text.Text("language.savedTitle", "Language saved"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private void OpenLanguagesFolder()
    {
        try
        {
            Directory.CreateDirectory(_text.LanguagesDirectory);
            var start = new ProcessStartInfo { FileName = "explorer.exe", UseShellExecute = true };
            start.ArgumentList.Add(_text.LanguagesDirectory);
            Process.Start(start);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, _text.Text("language.folderErrorTitle", "Language folder could not be opened"), MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private TabPage BuildAuditTab()
    {
        var tab = NewTab(_text.Text("tab.audit", "Audit Log"));
        _log.Dock = DockStyle.Fill;
        _log.ReadOnly = true;
        _log.BorderStyle = BorderStyle.None;
        _log.BackColor = Color.FromArgb(16, 20, 23);
        _log.ForeColor = Color.FromArgb(188, 202, 204);
        _log.Font = new Font("Cascadia Mono", 9.25f);
        _log.Text = _text.Text("audit.heading", "Progression QOL audit log\n─────────────────────────\n");
        tab.Controls.Add(_log);
        return tab;
    }

    private Control AddMultiplier(string title, string key, int selectedValue, int maximum, string description, RewardGlyph glyph)
    {
        var options = new[] { 1, 2, 3, 5, 7, 10, 15, 20 }.Where(x => x <= maximum)
            .Select(x => new Choice(x, x == 1 ? _text.Text("choice.default", "x1 — Default") : $"x{x}" )).ToArray();
        return AddChoice(title, key, selectedValue, description, glyph, options, 230);
    }

    private Control AddPercent(string title, string key, int selectedValue, string description, RewardGlyph glyph)
    {
        var options = new[] { 25, 50, 75, 90, 95, 99 }
            .Select(x => new Choice(x, x == 90 ? _text.Text("choice.recommended", "90% — Recommended") : $"{x}%" )).ToArray();
        return AddChoice(title, key, selectedValue, description, glyph, options, 240);
    }

    private Control AddChoice(string title, string key, int selectedValue, string description, RewardGlyph glyph, IReadOnlyList<Choice> options, int buttonWidth)
    {
        var host = new Panel { Height = 159, MinimumSize = new Size(190, 159), BackColor = PanelRaised, Margin = new Padding(3) };
        var icon = new RewardIcon(glyph) { Location = new Point(8, 13), Size = new Size(36, 36) };
        var label = new Label { Text = title, ForeColor = TextMain, AutoSize = false, Font = UiFont(9.5f, FontStyle.Bold), Location = new Point(54, 3), Height = 41, UseCompatibleTextRendering = true };
        var help = new Label { Text = description, ForeColor = TextMuted, AutoEllipsis = false, AutoSize = false, Location = new Point(54, 81), Height = 76, UseCompatibleTextRendering = true };
        var combo = new MultiplierDropDown(options, selectedValue, _uiFontFamily) { Width = buttonWidth, Height = 32, Location = new Point(54, 45) };
        combo.ValueChanged += (_, _) => MarkConfigurationEdited();
        _multipliers[key] = combo;
        _toolTips.SetToolTip(host, description);
        _toolTips.SetToolTip(combo, description);
        host.Controls.Add(icon); host.Controls.Add(label); host.Controls.Add(help); host.Controls.Add(combo);
        host.Resize += (_, _) =>
        {
            combo.Left = label.Left;
            combo.Top = 45;
            combo.Width = Math.Max(120, Math.Min(buttonWidth, host.ClientSize.Width - combo.Left - 8));
            label.Width = Math.Max(120, host.ClientSize.Width - label.Left - 8);
            help.Width = Math.Max(120, host.ClientSize.Width - help.Left - 8);
        };
        return host;
    }

    private Control AddToggle(CheckBox checkBox, string title, string description, RewardGlyph glyph)
    {
        var host = new Panel { Height = 165, MinimumSize = new Size(230, 165), BackColor = PanelRaised };
        var icon = new RewardIcon(glyph) { Location = new Point(8, 12), Size = new Size(36, 36) };
        checkBox.Text = title;
        checkBox.AutoSize = false;
        checkBox.Height = 48;
        checkBox.Location = new Point(54, 8);
        checkBox.ForeColor = TextMain;
        checkBox.BackColor = PanelRaised;
        checkBox.Font = UiFont(9.5f, FontStyle.Bold);
        var help = new Label { Text = description, ForeColor = TextMuted, AutoEllipsis = false, AutoSize = false, Location = new Point(58, 55), Height = 106, UseCompatibleTextRendering = true };
        host.Controls.Add(icon); host.Controls.Add(checkBox); host.Controls.Add(help);
        host.Resize += (_, _) => { checkBox.Width = Math.Max(120, host.ClientSize.Width - 62); help.Width = Math.Max(120, host.ClientSize.Width - 66); };
        _toolTips.SetToolTip(host, description);
        _toolTips.SetToolTip(checkBox, description);
        return host;
    }

    private Control BuildSection(string title, string description, RewardGlyph glyph, IEnumerable<Control> controls, int columns = 1)
    {
        var items = controls.ToList();
        var host = new Panel { Dock = DockStyle.Fill, BackColor = PanelRaised, Padding = new Padding(10), Margin = new Padding(6) };
        var section = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, BackColor = PanelRaised };
        section.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        section.RowStyles.Add(new RowStyle(SizeType.Absolute, 90));
        section.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        var header = new Panel { Dock = DockStyle.Fill, BackColor = PanelRaised };
        var headerIcon = new RewardIcon(glyph) { Location = new Point(3, 8), Size = new Size(38, 38) };
        var label = new Label { Text = title, ForeColor = Gold, Font = UiFont(11f, FontStyle.Bold), Location = new Point(50, 4), Height = 27, AutoSize = true };
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
    private TabPage NewTab(string text) => new(text) { BackColor = PanelColor, ForeColor = TextMain, Padding = new Padding(5) };

    private Control PathRow(string labelText, TextBox box, int top, Action browse)
    {
        var host = new Panel { Left = 24, Top = top, Height = 76, Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right, Width = 940, BackColor = PanelRaised };
        var label = new Label { Text = labelText, ForeColor = Gold, AutoSize = true, Location = new Point(12, 8) };
        box.Location = new Point(12, 36); box.Width = 810; box.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right; box.BackColor = Ink; box.ForeColor = TextMain; box.BorderStyle = BorderStyle.FixedSingle;
        var button = new Button { Text = _text.Text("build.browse", "BROWSE"), Width = 90, Height = 28, Top = 34, Anchor = AnchorStyles.Top | AnchorStyles.Right };
        button.Left = host.Width - button.Width - 12; StyleButton(button, Ink, Teal); button.Click += (_, _) => browse();
        host.Controls.Add(label); host.Controls.Add(box); host.Controls.Add(button);
        return host;
    }

    private void SetDefaults()
    {
        _gamePath.Text = BuildEngine.DetectGameRoot() ?? _text.Text("build.gamePath.placeholder", "Select your DragonSword installation");
        _outputPath.Text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "DragonSword Progression QOL Builds");
        Append($"Baseline: verified unmodified game {BuildEngine.SupportedGameVersion} tables (Steam build {BuildEngine.SupportedSteamBuildId}).", Gold);
        Append("Network, telemetry, auto-update, DLL injection, and elevation: none.", Teal);
        foreach (var warning in _text.LoadWarnings)
            Append("LANGUAGE WARNING: " + warning, Gold);
    }

    private BuildConfig ReadConfig() => new(
        SelectedValue("gathering"), SelectedValue("enemy"), SelectedValue("equipment"),
        SelectedValue("activityMaterials"), SelectedValue("currencyXpItems"), SelectedValue("emblems"), SelectedValue("gold"), SelectedValue("rankXp"),
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
            _multipliers["currencyXpItems"].SetValue(config.CurrencyExperienceItemMultiplier);
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
            ? _text.Text("settings.custom", "Settings: Custom (unsaved)")
            : _text.Format("settings.named", "Settings: {name}", ("name", _currentProfileName == "Default" ? _text.Text("profile.defaultName", "Default") : _currentProfileName))
              + (_configurationDirty ? _text.Text("settings.editedSuffix", " (edited)") : "");
        _profileStatus.Left = Math.Max(4, _profileButton.Left - _profileStatus.Width - 16);
    }

    private void ShowProfileMenu()
    {
        _profileMenu.Items.Clear();
        _profileMenu.BackColor = PanelRaised;
        _profileMenu.ForeColor = TextMain;

        var save = NewProfileMenuItem(_text.Text("profile.menu.save", "SAVE CURRENT SETTINGS AS…"), (_, _) => SaveProfile());
        var load = NewProfileMenuItem(_text.Text("profile.menu.load", "LOAD PROFILE"));
        var delete = NewProfileMenuItem(_text.Text("profile.menu.delete", "DELETE PROFILE"));
        var profiles = _profiles.List();
        if (profiles.Count == 0)
        {
            load.DropDownItems.Add(new ToolStripMenuItem(_text.Text("profile.menu.none", "No saved profiles")) { Enabled = false });
            delete.DropDownItems.Add(new ToolStripMenuItem(_text.Text("profile.menu.none", "No saved profiles")) { Enabled = false });
        }
        else
        {
            foreach (var profile in profiles)
            {
                load.DropDownItems.Add(NewProfileMenuItem(profile.Name, (_, _) => LoadProfile(profile.Name)));
                delete.DropDownItems.Add(NewProfileMenuItem(profile.Name, (_, _) => DeleteProfile(profile.Name)));
            }
        }
        var open = NewProfileMenuItem(_text.Text("profile.menu.open", "OPEN PROFILES FOLDER"), (_, _) => OpenProfilesFolder());
        var import = NewProfileMenuItem(_text.Text("profile.menu.import", "IMPORT FROM OLDER VERSION…"), (_, _) => ImportProfilesFromOlderVersion());
        var reset = NewProfileMenuItem(_text.Text("profile.menu.reset", "RESET TO VANILLA DEFAULTS"), (_, _) => ResetToDefaults());
        _profileMenu.Items.Add(save);
        _profileMenu.Items.Add(load);
        _profileMenu.Items.Add(delete);
        _profileMenu.Items.Add(new ToolStripSeparator());
        _profileMenu.Items.Add(reset);
        _profileMenu.Items.Add(import);
        _profileMenu.Items.Add(open);
        _profileMenu.Show(_profileButton, new Point(0, -_profileMenu.PreferredSize.Height));
    }

    private ToolStripMenuItem NewProfileMenuItem(string text, EventHandler? click = null)
    {
        var item = new ToolStripMenuItem(text) { BackColor = PanelRaised, ForeColor = TextMain };
        if (click is not null) item.Click += click;
        return item;
    }

    private void SaveProfile()
    {
        var suggested = _currentProfileName == "Default" ? _text.Text("profile.suggestedName", "My Settings") : _currentProfileName;
        var name = PromptForProfileName(suggested);
        if (name is null) return;
        try
        {
            var path = _profiles.GetManagedPath(name);
            if (File.Exists(path) && MessageBox.Show(this,
                    _text.Format("profile.replacePrompt", "Replace the saved profile ‘{name}’?", ("name", name)),
                    _text.Text("profile.replaceTitle", "Replace profile"), MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
            path = _profiles.Save(name, ReadConfig());
            _currentProfileName = name.Trim();
            _configurationDirty = false;
            UpdateProfileStatus();
            Append($"Profile saved: {_currentProfileName} ({path})", Teal);
        }
        catch (Exception ex) { ShowProfileError(_text.Text("profile.error.save", "Profile could not be saved"), ex); }
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
        catch (Exception ex) { ShowProfileError(_text.Text("profile.error.load", "Profile could not be loaded"), ex); }
    }

    private void DeleteProfile(string name)
    {
        if (MessageBox.Show(this,
                _text.Format("profile.deletePrompt", "Delete the saved profile ‘{name}’?\n\nThis deletes only its JSON settings file. It does not change any PAK.", ("name", name)),
                _text.Text("profile.deleteTitle", "Delete profile"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
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
        catch (Exception ex) { ShowProfileError(_text.Text("profile.error.delete", "Profile could not be deleted"), ex); }
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
        catch (Exception ex) { ShowProfileError(_text.Text("profile.error.open", "Profiles folder could not be opened"), ex); }
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
            Description = _text.Text("profile.importDescription", "Select an older Progression QOL application folder or its Profiles folder"),
            UseDescriptionForTitle = true
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            var result = _profiles.ImportFromDirectory(dialog.SelectedPath);
            Append($"Profile import: {result.Imported} imported, {result.Skipped} already present, {result.Invalid} invalid. Source files were preserved.", result.Invalid > 0 ? Gold : Teal);
            MessageBox.Show(this,
                _text.Format("profile.importComplete", "Imported: {imported}\nAlready present: {skipped}\nInvalid files skipped: {invalid}\n\nOlder profile files were not deleted.",
                    ("imported", result.Imported), ("skipped", result.Skipped), ("invalid", result.Invalid)),
                _text.Text("profile.importCompleteTitle", "Profile import complete"),
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex) { ShowProfileError(_text.Text("profile.error.import", "Profiles could not be imported"), ex); }
    }

    private void ResetToDefaults()
    {
        ApplyConfig(new BuildConfig(1, 1, 1, 1, 1, 1, 1, 1, 1, false, false, 90m));
        _currentProfileName = "Default";
        _configurationDirty = false;
        UpdateProfileStatus();
        Append("Reward settings reset to vanilla defaults. Use Build + Install to disable this app's installed PAK and restore vanilla behavior.", Teal);
    }

    private string? PromptForProfileName(string suggested)
    {
        using var dialog = new Form
        {
            Text = _text.Text("profile.saveDialogTitle", "Save configuration profile"),
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
        var label = new Label { Text = _text.Text("profile.name", "Profile name"), AutoSize = true, ForeColor = Gold, Location = new Point(18, 16) };
        var box = new TextBox { Text = suggested, Location = new Point(18, 43), Width = 434, BackColor = Ink, ForeColor = TextMain, BorderStyle = BorderStyle.FixedSingle };
        var save = new Button { Text = _text.Text("profile.saveButton", "SAVE PROFILE"), DialogResult = DialogResult.OK, Size = new Size(132, 38), Location = new Point(320, 92) };
        var cancel = new Button { Text = _text.Text("common.cancel", "CANCEL"), DialogResult = DialogResult.Cancel, Size = new Size(105, 38), Location = new Point(205, 92) };
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
            _build.Enabled = false; _status.Text = _text.Text("build.status.building", "Building and verifying...");
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
            _status.Text = _text.Text("build.status.verified", "Build verified — not installed yet.");
            Append("BUILD VERIFIED — NOT INSTALLED YET.", Gold);
            InstallLatest();
        }
        catch (Exception ex)
        {
            Append("ERROR: " + ex.Message, Ember); _status.Text = _text.Text("build.status.failed", "Build failed safely; no game files were changed.");
            MessageBox.Show(ex.Message, _text.Text("build.failedTitle", "Build failed"), MessageBoxButtons.OK, MessageBoxIcon.Error);
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
            _status.Text = _text.Text("vanilla.status.noPak", "Vanilla defaults selected — no Progression QOL PAK is installed.");
            Append("Vanilla defaults confirmed. No installed Progression QOL PAK was found, so the game was not changed.", Teal);
            MessageBox.Show(this, _text.Text("vanilla.noPakMessage", "All settings are already at vanilla defaults, and no Progression QOL PAK is installed.\n\nNothing needs to be removed."), _text.Text("vanilla.noPakTitle", "Already using vanilla defaults"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var answer = MessageBox.Show(this,
            _text.Text("vanilla.restorePrompt", "Every reward setting is at its vanilla default.\n\nDisable the installed Progression QOL PAK and return this mod's rewards to vanilla behavior?\n\nThe PAK will be preserved as a disabled backup. Other mods will not be changed."),
            _text.Text("vanilla.restoreTitle", "Restore vanilla rewards?"),
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);
        if (answer != DialogResult.Yes)
        {
            _status.Text = _text.Text("vanilla.status.cancelled", "Vanilla restore cancelled — the installed PAK was not changed.");
            Append("Vanilla restore cancelled. The installed PAK was not changed.", Gold);
            return;
        }

        var backup = _engine.DisableInstalledPakForVanilla(gameRoot);
        if (backup is null) throw new IOException("The installed Progression QOL PAK disappeared before it could be disabled.");
        _latestBuild = null;
        _status.Text = _text.Text("vanilla.status.restored", "Progression QOL disabled — vanilla rewards restored for this mod.");
        Append($"Vanilla restored: disabled the installed Progression QOL PAK and preserved it at {backup}", Teal);
        MessageBox.Show(this,
            _text.Text("vanilla.restoredMessage", "Progression QOL has been disabled and its PAK was preserved as a non-loadable backup.\n\nThis restores vanilla reward behavior for this mod. Any other installed reward mod may still affect the game."),
            _text.Text("vanilla.restoredTitle", "Vanilla rewards restored"),
            MessageBoxButtons.OK,
            MessageBoxIcon.Information);
    }

    private static bool IsVanillaConfig(BuildConfig config) =>
        config.WorldGatheringMultiplier == 1 &&
        config.EnemyMaterialMultiplier == 1 &&
        config.EquipmentMultiplier == 1 &&
        config.ActivityMaterialMultiplier == 1 &&
        config.CurrencyExperienceItemMultiplier == 1 &&
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
            MessageBox.Show(_text.Text("build.noPak", "Build a PAK before installing."), _text.Text("build.noPakTitle", "No verified build"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }
        try
        {
            var gameRoot = BuildEngine.ResolveGameRoot(_gamePath.Text) ?? throw new DirectoryNotFoundException("DragonSword could not be found from the selected path.");
            _gamePath.Text = gameRoot;
            var conflicts = FindKnownConflicts(gameRoot);
            var basePrompt = _text.Text("install.prompt", "The new PAK is built and verified, but it is not installed yet.\n\nInstall it now? The existing Progression QOL PAK will be disabled and backed up.");
            var compatibility = BuildEngine.CheckGameBuild(gameRoot);
            if (compatibility.Kind == GameBuildCompatibilityKind.Older)
                throw new InvalidOperationException(_text.Format("install.olderBuildBlocked", "DragonSword build {detected} is older than this configurator's validated build {required}. Update the game before installing.", ("detected", compatibility.DetectedBuildId ?? "unknown"), ("required", BuildEngine.SupportedSteamBuildId)));
            var compatibilityWarning = compatibility.Kind switch
            {
                GameBuildCompatibilityKind.Newer => _text.Format("install.newerBuildWarning", "Newer DragonSword build {detected} detected. This configurator was validated on build {required}. You can continue, but it may be out of date and rewards may not work as intended. Restore vanilla if anything looks wrong and watch the mod page for an update.", ("detected", compatibility.DetectedBuildId ?? "unknown"), ("required", BuildEngine.SupportedSteamBuildId)),
                GameBuildCompatibilityKind.Unknown => _text.Format("install.unknownBuildWarning", "The Steam build could not be verified. You can continue, but this configurator may be out of date and rewards may not work as intended. Restore vanilla if anything looks wrong and watch the mod page for an update.", ("required", BuildEngine.SupportedSteamBuildId)),
                _ => ""
            };
            var prompt = basePrompt;
            if (!string.IsNullOrWhiteSpace(compatibilityWarning)) prompt += "\n\n" + compatibilityWarning;
            if (conflicts.Count > 0)
                prompt = _text.Format("install.promptWithConflicts", "{base}\n\nConflicting reward/material PAKs detected:\n{conflicts}\n\nThe installer will NOT remove them. Disable those PAKs before launching the game.", ("base", prompt), ("conflicts", string.Join("\n", conflicts.Select(Path.GetFileName))));
            if (MessageBox.Show(prompt, _text.Text("install.promptTitle", "Install verified build"), MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
            {
                Append("Installation skipped. The installed PAK was not changed.", Gold);
                _status.Text = _text.Text("install.status.skipped", "Build verified — installation was skipped.");
                return;
            }
            var destination = _engine.Install(_latestBuild, gameRoot, compatibility.Kind is GameBuildCompatibilityKind.Newer or GameBuildCompatibilityKind.Unknown);
            Append("Installed: " + destination, Teal);
            Append("Installed SHA256 verified: " + _latestBuild.Sha256, Teal);
            _status.Text = _text.Text("install.status.success", "Installed and hash-verified. Resolve any logged conflicts before launch.");
        }
        catch (Exception ex) { Append("INSTALL ERROR: " + ex.Message, Ember); MessageBox.Show(ex.Message, _text.Text("install.failedTitle", "Install failed"), MessageBoxButtons.OK, MessageBoxIcon.Error); }
    }

    private void ScanConflicts()
    {
        var gameRoot = BuildEngine.ResolveGameRoot(_gamePath.Text);
        if (gameRoot is null)
        {
            _conflictSummary.Text = _text.Text("scan.gameNotFound", "DragonSword installation not found from the selected path.");
            _conflictSummary.ForeColor = Ember;
            Append("Conflict scan stopped: the selected path could not be resolved to DragonSword.", Ember);
            return;
        }
        _gamePath.Text = gameRoot;
        var compatibility = BuildEngine.CheckGameBuild(gameRoot);
        var buildSummary = compatibility.Kind switch
        {
            GameBuildCompatibilityKind.Exact => "",
            GameBuildCompatibilityKind.Newer => _text.Format("scan.newerBuild", "Newer game build {detected} detected. Validated build: {required}. Installation is available with a compatibility warning.", ("detected", compatibility.DetectedBuildId ?? "unknown"), ("required", BuildEngine.SupportedSteamBuildId)),
            GameBuildCompatibilityKind.Older => _text.Format("scan.olderBuild", "Older game build {detected} detected. Update the game before installation.", ("detected", compatibility.DetectedBuildId ?? "unknown")),
            _ => _text.Text("scan.buildUnknown", "Steam build could not be verified. Installation is available with a compatibility warning.")
        };
        if (!string.IsNullOrWhiteSpace(buildSummary))
            Append(buildSummary, compatibility.Kind == GameBuildCompatibilityKind.Older ? Ember : Gold);
        var conflicts = FindKnownConflicts(gameRoot);
        if (conflicts.Count == 0)
        {
            _conflictSummary.Text = string.IsNullOrWhiteSpace(buildSummary) ? _text.Text("scan.clear", "No known reward or material conflicts found.") : buildSummary;
            _conflictSummary.ForeColor = compatibility.Kind == GameBuildCompatibilityKind.Exact ? Teal : compatibility.Kind == GameBuildCompatibilityKind.Older ? Ember : Gold;
            Append("Conflict scan: no installed PAK contains a managed reward/material table.", Teal);
        }
        else
        {
            _conflictSummary.Text = _text.Format("scan.conflicts", "{count} possible reward/material conflict(s) found. Review the Audit Log.", ("count", conflicts.Count));
            if (!string.IsNullOrWhiteSpace(buildSummary)) _conflictSummary.Text += " " + buildSummary;
            _conflictSummary.ForeColor = Ember;
            Append($"Conflict scan: {conflicts.Count} reward/material PAK(s) require manual disabling:", Ember);
            foreach (var file in conflicts) Append("  " + file, Ember);
        }
        var paksRoot = Path.Combine(gameRoot, "DS", "Content", "Paks");
        var respawnPaks = (Directory.Exists(paksRoot)
                ? Directory.EnumerateFiles(paksRoot, "DS_TreasureRespawn*.pak", SearchOption.TopDirectoryOnly)
                : [])
            .Concat(Directory.Exists(Path.Combine(paksRoot, "~mods"))
                ? Directory.EnumerateFiles(Path.Combine(paksRoot, "~mods"), "DS_TreasureRespawn*.pak", SearchOption.TopDirectoryOnly)
                : [])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (respawnPaks.Count > 0)
        {
            var respawnConflict = respawnPaks.Any(x => conflicts.Contains(x, StringComparer.OrdinalIgnoreCase));
            if (respawnConflict)
                Append("A Treasure Respawn-named PAK contains a managed reward/material table and must be reviewed as a conflict.", Ember);
            else
            {
                if (conflicts.Count == 0) _conflictSummary.Text += _text.Text("scan.respawnCompatible", " Treasure Respawn is compatible.");
                Append("Treasure Respawn detected and inspected: no managed reward/material table overlap.", Teal);
            }
        }
    }

    internal void PrepareScreenshot(int index, bool showcase)
    {
        _tabs.SelectedIndex = Math.Clamp(index, 0, _tabs.TabCount - 1);
        _gamePath.Text = @"C:\Games\DragonSword Awakening";
        _outputPath.Text = @"C:\Users\Player\Documents\DragonSword Progression QOL Builds";
        _conflictSummary.Text = _text.Text("scan.clear", "No known reward or material conflicts found.");
        _conflictSummary.ForeColor = Teal;
        if (!showcase) return;

        SetScreenshotValue("gathering", 5);
        SetScreenshotValue("enemy", 3);
        SetScreenshotValue("chests", 5);
        SetScreenshotValue("equipment", 5);
        SetScreenshotValue("activityMaterials", 10);
        SetScreenshotValue("currencyXpItems", 2);
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
            .Where(x => !Path.GetFileName(x).StartsWith("pakchunk", StringComparison.OrdinalIgnoreCase));
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
                else
                    Append($"Conflict scan could not inspect {Path.GetFileName(pak)}; no conflict was assumed.", Gold);
            }
        }
        return conflicts.Order(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static bool RegexLike(string value, params string[] terms) => terms.Any(x => value.Contains(x, StringComparison.OrdinalIgnoreCase));

    private void BrowseGame()
    {
        using var dialog = new FolderBrowserDialog { Description = _text.Text("browse.gameDescription", "Select DragonSword Awakening, DS, Paks, ~mods, or Win64"), UseDescriptionForTitle = true, SelectedPath = Directory.Exists(_gamePath.Text) ? _gamePath.Text : "" };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _gamePath.Text = BuildEngine.ResolveGameRoot(dialog.SelectedPath) ?? dialog.SelectedPath;
            ScanConflicts();
        }
    }

    private void BrowseOutput()
    {
        using var dialog = new FolderBrowserDialog { Description = _text.Text("browse.outputDescription", "Select where verified builds will be created"), UseDescriptionForTitle = true, SelectedPath = Directory.Exists(_outputPath.Text) ? _outputPath.Text : "" };
        if (dialog.ShowDialog(this) == DialogResult.OK) _outputPath.Text = dialog.SelectedPath;
    }

    private void Append(string text, Color color)
    {
        _log.SelectionStart = _log.TextLength; _log.SelectionColor = color; _log.AppendText($"[{DateTime.Now:HH:mm:ss}] {text}\n"); _log.SelectionColor = _log.ForeColor; _log.ScrollToCaret();
    }

    private Font UiFont(float size, FontStyle style = FontStyle.Regular) => new(_uiFontFamily, size, style);

    private void StyleButton(Button button, Color back, Color fore)
    {
        button.FlatStyle = FlatStyle.Flat; button.FlatAppearance.BorderColor = fore; button.FlatAppearance.BorderSize = 1; button.BackColor = back; button.ForeColor = fore; button.Font = UiFont(9f, FontStyle.Bold); button.Cursor = Cursors.Hand;
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

        public MultiplierDropDown(IReadOnlyList<Choice> choices, int selectedValue, string fontFamily)
        {
            _choices = choices;
            AccessibleRole = AccessibleRole.ComboBox;
            FlatStyle = FlatStyle.Flat;
            FlatAppearance.BorderColor = Color.FromArgb(92, 111, 119);
            FlatAppearance.MouseOverBackColor = Color.FromArgb(44, 55, 61);
            FlatAppearance.MouseDownBackColor = Color.FromArgb(54, 67, 73);
            BackColor = Ink;
            ForeColor = TextMain;
            Font = new Font(fontFamily, 9f, FontStyle.Bold);
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
        private readonly LocalizationService _text;
        private readonly string _fontFamily;

        public BannerPanel(LocalizationService text, string fontFamily)
        {
            _text = text;
            _fontFamily = fontFamily;
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
            using var titleFont = new Font(_fontFamily, 27f, FontStyle.Bold);
            using var creditFont = new Font(_fontFamily, 10f, FontStyle.Bold);
            using var buildFont = new Font(_fontFamily, 9.5f, FontStyle.Regular);
            using var titleBrush = new SolidBrush(TextMain);
            using var creditBrush = new SolidBrush(Gold);

            const float titleY = 5f;
            var creditY = titleY + titleFont.GetHeight(e.Graphics) + 4f;
            var buildY = creditY + creditFont.GetHeight(e.Graphics) + 3f;
            e.Graphics.DrawString(_text.Text("banner.title", "PROGRESSION QOL"), titleFont, titleBrush, 34f, titleY);
            e.Graphics.DrawString(_text.Text("banner.credit", "DRAGONSWORD REWARD CONFIGURATOR  •  CREATED BY NECTARINES"), creditFont, creditBrush, 38f, creditY);
            e.Graphics.DrawString(_text.Text("banner.build", "VERSION 0.9.5  •  BUILT FOR GAME 1.0.10  •  ONE CUSTOM PAK  •  OFFLINE"), buildFont, titleBrush, 39f, buildY);
        }
    }
}
