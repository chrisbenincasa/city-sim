using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using Borough.Core;
using Borough.Core.Determinism;
using Borough.Core.Entities;
using Borough.Core.Persistence;
using Borough.Formats;

namespace Borough.Shell;

internal sealed record SavedCity(World World, SaveHeader Header, string Toml, RulesetNames Names, ulong Seed);

// The package carries content; Core still owns every byte of the world dump.
internal static class CitySave
{
    private sealed record Metadata(int Version, ulong Seed);
    private sealed class Sink(Stream stream) : ISaveSink
    {
        public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes);
    }
    private sealed class Source(Stream stream) : ISaveSource
    {
        public void Read(Span<byte> bytes) => stream.ReadExactly(bytes);
    }

    public static void Write(string path, World world, string toml, ulong seed)
    {
        path = Path.GetFullPath(path);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write))
            {
                using (var zip = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true))
                {
                    using (var entry = zip.CreateEntry("city.json").Open())
                        JsonSerializer.Serialize(entry, new Metadata(1, seed));
                    byte[] content = Encoding.UTF8.GetBytes(toml);
                    using (var entry = zip.CreateEntry("ruleset.toml").Open()) entry.Write(content);
                    using (var entry = zip.CreateEntry("world.save", CompressionLevel.Fastest).Open())
                        SaveFile.Write(world, RulesetFile.HashOfContent(content), new Sink(entry));
                }
                file.Flush(flushToDisk: true);
            }
            File.Move(temporary, path, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }

    public static SavedCity Read(string path)
    {
        using var zip = ZipFile.OpenRead(path);
        Stream Entry(string name) => (zip.GetEntry(name)
            ?? throw new InvalidDataException($"Missing {name} in city save.")).Open();
        Metadata metadata;
        using (var entry = Entry("city.json"))
            metadata = JsonSerializer.Deserialize<Metadata>(entry)
                ?? throw new InvalidDataException("Missing city metadata.");
        if (metadata.Version != 1) throw new InvalidDataException("Unsupported city save version.");
        string toml;
        using (var reader = new StreamReader(Entry("ruleset.toml"), Encoding.UTF8)) toml = reader.ReadToEnd();
        RulesetLoadResult parsed = RulesetLoader.Parse(toml, "saved ruleset.toml");
        if (parsed.Ruleset is null) throw new InvalidDataException(parsed.Describe());
        using var state = Entry("world.save");
        // Check the content identity before constructing a world under these rules.
        byte[] bytes = new byte[SaveHeader.Bytes];
        state.ReadExactly(bytes);
        SaveHeader header = SaveHeader.Read(bytes);
        if (header.RulesetInForce != RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(toml)))
            throw new InvalidDataException("The saved Ruleset does not match the city.");
        if (header.Key != WorldKey.FromSeed(metadata.Seed))
            throw new InvalidDataException("The saved seed does not match the city.");
        using var body = Entry("world.save");
        World world = SaveFile.Read(new Source(body), parsed.Ruleset, out header);
        if (body.ReadByte() != -1) throw new InvalidDataException("Unexpected data after the saved city.");
        return new SavedCity(world, header, toml, parsed.Names, metadata.Seed);
    }
}
