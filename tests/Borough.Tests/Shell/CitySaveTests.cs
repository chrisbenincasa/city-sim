using System.IO.Compression;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Input;
using Borough.Formats;
using Borough.Shell;

namespace Borough.Tests.Shell;

public sealed class CitySaveTests
{
    [Theory]
    [InlineData("minimal.toml")]
    [InlineData("care.toml")]
    public void Embedded_content_restores_names_and_continues_the_same_city(string ruleset)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", ruleset));
        var rules = RulesetLoader.Parse(toml, ruleset).Ruleset!;
        var key = WorldKey.FromSeed(7);
        var original = new Simulation(new World(32, rules, key), key);
        original.Step(new TickInput([new Command(CommandKind.Populate, default, default)], 0));
        for (int i = 0; i < 64; i++) original.Step(default);
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        string path = Path.Combine(folder, "My city.borough-city");
        try
        {
            ulong before = original.World.HashState();
            CitySave.Write(path, original.World, toml, 7);
            Assert.Equal(before, original.World.HashState());
            SavedCity loaded = CitySave.Read(path);
            Assert.Equal(toml, loaded.Toml);
            Assert.Equal(RulesetLoader.Parse(toml, ruleset).Names.Kind(1), loaded.Names.Kind(1));
            var resumed = new Simulation(loaded.World, loaded.Header.Key);
            Assert.Equal(before, resumed.World.HashState());
            for (int i = 0; i < 64; i++)
            {
                original.Step(default);
                resumed.Step(default);
                Assert.Equal(original.World.HashState(), resumed.World.HashState());
            }
            CitySave.Write(path, resumed.World, toml, 7);
            Assert.Equal(resumed.World.HashState(), CitySave.Read(path).World.HashState());
            Assert.Single(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }

    [Theory]
    [InlineData("ruleset.toml")]
    [InlineData("world.save")]
    [InlineData("city.json")]
    public void Damaged_package_is_refused_without_changing_the_source_city(string entryName)
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var key = WorldKey.FromSeed(0);
        var world = new World(0, RulesetLoader.Parse(toml, "minimal.toml").Ruleset!, key);
        string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".borough-city");
        try
        {
            CitySave.Write(path, world, toml, 0);
            ulong before = world.HashState();
            using (var zip = ZipFile.Open(path, ZipArchiveMode.Update))
            {
                zip.GetEntry(entryName)!.Delete();
                using var writer = new StreamWriter(zip.CreateEntry(entryName).Open());
                writer.Write(entryName == "ruleset.toml" ? toml + "\n# different content\n" : "broken");
            }
            Assert.ThrowsAny<Exception>(() => CitySave.Read(path));
            Assert.Equal(before, world.HashState());
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void Failed_replacement_cleans_temporary_file_and_preserves_destination()
    {
        string toml = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Rulesets", "minimal.toml"));
        var world = new World(0, RulesetLoader.Parse(toml, "minimal.toml").Ruleset!, WorldKey.FromSeed(0));
        string folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        string destination = Path.Combine(folder, "existing");
        Directory.CreateDirectory(destination);
        File.WriteAllText(Path.Combine(destination, "keep"), "original");
        try
        {
            Assert.ThrowsAny<IOException>(() => CitySave.Write(destination, world, toml, 0));
            Assert.Equal("original", File.ReadAllText(Path.Combine(destination, "keep")));
            Assert.Empty(Directory.GetFiles(folder));
        }
        finally { Directory.Delete(folder, true); }
    }
}
