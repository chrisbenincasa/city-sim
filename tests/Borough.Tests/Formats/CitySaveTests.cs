using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Core.Persistence;
using Borough.Core.Rules;
using Borough.Formats;

namespace Borough.Tests.Formats;

public sealed class CitySaveTests
{
    private sealed class Sink(Stream stream) : ISaveSink
    {
        public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes);
    }

    private static RulesetCapture Single(string ruleset) =>
        RulesetCapture.Read(Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset)).Capture!;

    private static RulesetCapture Packaged() => RulesetCapture.Read(
        Path.Combine(AppContext.BaseDirectory, "Rulesets", "split", "ruleset.toml")).Capture!;

    private static Ruleset Rules(RulesetCapture capture) => RulesetSource.Resolve(capture).Ruleset!;

    [Theory]
    [InlineData("minimal.toml")]
    [InlineData("care.toml")]
    public void Embedded_content_restores_names_and_continues_the_same_city(string ruleset)
    {
        RulesetCapture capture = Single(ruleset);
        var key = WorldKey.FromSeed(7);
        var original = new Simulation(new World(32, Rules(capture), key), key);
        original.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));
        for (int i = 0; i < 64; i++) original.Step(default);
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "My city.borough-city");
        try
        {
            ulong before = original.World.HashState();
            CitySave.Write(path, original.World, capture, 7);
            Assert.Equal(before, original.World.HashState());
            SavedCity loaded = CitySave.Read(path);
            Assert.Equal(capture.ContentHash, loaded.Capture.ContentHash);
            Assert.Equal(RulesetSource.Resolve(capture).Names.Kind(1), loaded.Names.Kind(1));
            var resumed = new Simulation(loaded.World, loaded.Header.Key);
            Assert.Equal(before, resumed.World.HashState());
            for (int i = 0; i < 64; i++)
            {
                original.Step(default);
                resumed.Step(default);
                Assert.Equal(original.World.HashState(), resumed.World.HashState());
            }
            CitySave.Write(path, resumed.World, capture, 7);
            Assert.Equal(resumed.World.HashState(), CitySave.Read(path).World.HashState());
            Assert.Single(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Fact]
    public void A_package_city_reloads_without_its_source_directory()
    {
        RulesetCapture capture = RulesetCapture.Read(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "split", "ruleset.toml")).Capture!;
        var key = WorldKey.FromSeed(3);
        var original = new Simulation(new World(32, Rules(capture), key), key);
        original.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));
        for (int i = 0; i < 32; i++) original.Step(default);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            CitySave.Write(path, original.World, capture, 3);

            SavedCity loaded = CitySave.Read(path);

            Assert.Equal(RulesetSourceMode.Source, loaded.Capture.Mode);
            Assert.Equal(capture.ContentHash, loaded.Capture.ContentHash);
            Assert.Equal(
                capture.Members.Select(member => member.Path),
                loaded.Capture.Members.Select(member => member.Path));
            Assert.Equal(original.World.HashState(), loaded.World.HashState());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void A_resumed_city_names_its_Ruleset_rather_than_the_archive()
    {
        RulesetCapture capture = Single("minimal.toml");
        var key = WorldKey.FromSeed(11);
        var world = new World(16, Rules(capture), key);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            CitySave.Write(path, world, capture, 11);

            Assert.Equal("minimal.toml", CitySave.Read(path).Capture.EntryName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void A_package_city_resumes_under_its_manifest_name()
    {
        RulesetCapture capture = RulesetCapture.Read(
            Path.Combine(AppContext.BaseDirectory, "Rulesets", "split", "ruleset.toml")).Capture!;
        var key = WorldKey.FromSeed(13);
        var world = new World(16, Rules(capture), key);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            CitySave.Write(path, world, capture, 13);

            Assert.Equal("ruleset.toml", CitySave.Read(path).Capture.EntryName);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void A_version_1_save_still_loads_under_its_legacy_identity()
    {
        byte[] content = File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var key = WorldKey.FromSeed(5);
        var world = new World(16, RulesetLoader.Parse(Encoding.UTF8.GetString(content), "minimal.toml").Ruleset!, key);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            using (var file = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                using (var entry = zip.CreateEntry("city.json").Open())
                    JsonSerializer.Serialize(entry, new { Version = 1, Seed = 5UL });
                using (var entry = zip.CreateEntry("ruleset.toml").Open()) entry.Write(content);
                using (var entry = zip.CreateEntry("world.save").Open())
                    SaveFile.Write(world, RulesetFile.HashOfContent(content), new Sink(entry));
            }

            SavedCity loaded = CitySave.Read(path);

            Assert.Equal(RulesetSourceMode.Legacy, loaded.Capture.Mode);
            Assert.Equal(RulesetFile.HashOfContent(content), loaded.Capture.ContentHash);
            Assert.Equal(world.HashState(), loaded.World.HashState());
        }
        finally { File.Delete(path); }
    }

    [Theory]
    [InlineData("ruleset.toml")]
    [InlineData("world.save")]
    [InlineData("city.json")]
    public void Damaged_single_file_save_is_refused_without_changing_the_source_city(string entryName) =>
        AssertDamageIsRefused(Single("minimal.toml"), entryName);

    /// <summary>
    /// The package half, which the single-file theory above cannot reach: a v2 save carries the
    /// manifest and every member, and damaging one member is a damaged Ruleset.
    /// </summary>
    [Theory]
    [InlineData("source.toml")]
    [InlineData("members/goods.toml")]
    [InlineData("bundle.json")]
    [InlineData("world.save")]
    public void Damaged_package_save_is_refused_without_changing_the_source_city(string entryName) =>
        AssertDamageIsRefused(Packaged(), entryName);

    private static void AssertDamageIsRefused(RulesetCapture capture, string entryName)
    {
        var key = WorldKey.FromSeed(0);
        var world = new World(0, Rules(capture), key);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            CitySave.Write(path, world, capture, 0);
            ulong before = world.HashState();
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                zip.GetEntry(entryName)!.Delete();
                using var writer = new StreamWriter(zip.CreateEntry(entryName).Open());
                writer.Write(entryName.EndsWith(".toml", StringComparison.Ordinal)
                    ? Encoding.UTF8.GetString(capture.Entry.Span) + "\n# different content\n"
                    : "broken");
            }

            // Narrow on purpose: a damaged save is a refusal the shell reports, and a
            // NullReferenceException passing for one is how a read that crashes reads as a read
            // that refused.
            Assert.True(
                Record.Exception(() => CitySave.Read(path))
                    is InvalidDataException or InvalidOperationException,
                "a damaged save was not refused as one.");
            Assert.Equal(before, world.HashState());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Failed_replacement_cleans_temporary_file_and_preserves_destination()
    {
        RulesetCapture capture = Single("minimal.toml");
        var world = new World(0, Rules(capture), WorldKey.FromSeed(0));
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(folder, "existing");
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "keep"), "original");
        try
        {
            Assert.ThrowsAny<IOException>(() => CitySave.Write(destination, world, capture, 0));
            Assert.Equal("original", File.ReadAllText(Path.Combine(destination, "keep")));
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }
}
