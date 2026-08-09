namespace DragonSword.ProgressionQoL;

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length is 2 or 3 && args[0].Equals("--build-test", StringComparison.OrdinalIgnoreCase))
        {
            RunBuildTest(args[1], args.Length == 3 ? args[2] : null);
            return;
        }
        if (args.Length == 3 && args[0].Equals("--build-test-current", StringComparison.OrdinalIgnoreCase))
        {
            RunBuildTest(args[1], args[2], BaselineSource.CurrentGame);
            return;
        }
        if (args.Length == 2 && args[0].Equals("--ui-snapshot", StringComparison.OrdinalIgnoreCase))
        {
            RunUiSnapshot(args[1], false);
            return;
        }
        if (args.Length == 2 && args[0].Equals("--ui-snapshot-wide", StringComparison.OrdinalIgnoreCase))
        {
            RunUiSnapshot(args[1], true, 0);
            return;
        }
        if (args.Length == 2 && args[0].Equals("--ui-snapshot-build", StringComparison.OrdinalIgnoreCase))
        {
            RunUiSnapshot(args[1], true, 1);
            return;
        }
        if (args.Length == 2 && args[0].Equals("--ui-snapshot-showcase", StringComparison.OrdinalIgnoreCase))
        {
            RunUiSnapshot(args[1], true, 0, true);
            return;
        }
        if (args.Length == 2 && args[0].Equals("--ui-snapshot-audit", StringComparison.OrdinalIgnoreCase))
        {
            RunUiSnapshot(args[1], true, 2);
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        Application.ThreadException += (_, e) =>
            MessageBox.Show(e.Exception.Message, "Progression QOL", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Application.Run(new MainForm());
    }

    private static void RunBuildTest(string outputDirectory, string? gameRoot, BaselineSource baselineSource = BaselineSource.BundledStatic)
    {
        try
        {
            var engine = new BuildEngine(AppContext.BaseDirectory);
            var config = new BuildConfig(2, 2, 5, 5, 5, 10, 10, 2, true, true, 90m);
            var progress = new Progress<string>(Console.WriteLine);
            var result = engine.BuildAsync(config, outputDirectory, progress, gameRoot, baselineSource).GetAwaiter().GetResult();
            Console.WriteLine($"TEST_BUILD_OK|{result.PakPath}|{result.Sha256}|{result.FilesPacked}");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine("TEST_BUILD_FAILED|" + ex);
            Environment.ExitCode = 1;
        }
    }

    private static void RunUiSnapshot(string outputPath, bool wide, int tabIndex = 0, bool showcase = false)
    {
        ApplicationConfiguration.Initialize();
        using var form = new MainForm(false) { StartPosition = FormStartPosition.Manual, Location = new Point(-32000, -32000) };
        if (wide) form.Size = new Size(1920, 1120);
        form.Show();
        form.PrepareScreenshot(tabIndex, showcase);
        Application.DoEvents();
        using var bitmap = new Bitmap(form.Width, form.Height);
        form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
        bitmap.Save(Path.GetFullPath(outputPath), System.Drawing.Imaging.ImageFormat.Png);
        form.Hide();
        Console.WriteLine("UI_SNAPSHOT_OK|" + Path.GetFullPath(outputPath));
    }
}
