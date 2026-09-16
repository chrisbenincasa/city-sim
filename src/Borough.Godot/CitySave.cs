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

internal sealed record SavedCity(
    World World, SaveHeader Header, RulesetCapture Capture, RulesetNames Names, ulong Seed);

// The package carries content; Core still owns every byte of the world dump.
internal static class CitySave
{
    /// <summary>The envelope this build writes. A v1 save holds one file under no envelope.</summary>
    private const int Envelope = 2;

    private sealed record Metadata(int Version, ulong Seed);
    private sealed class Sink(Stream stream) : ISaveSink
    {
        public void Write(ReadOnlySpan<byte> bytes) => stream.Write(bytes);
    }
    private sealed class Source(Stream stream) : ISaveSource
    {
        public void Read(Span<byte> bytes) => stream.ReadExactly(bytes);
    }

    public static void Write(string path, World world, RulesetCapture capture, ulong seed)
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
                        JsonSerializer.Serialize(entry, new Metadata(Envelope, seed));
                    foreach (KeyValuePair<string, byte[]> held in RulesetBundle.Write(capture))
                        using (var entry = zip.CreateEntry(held.Key).Open()) entry.Write(held.Value);
                    using (var entry = zip.CreateEntry("world.save", CompressionLevel.Fastest).Open())
                        SaveFile.Write(world, capture.ContentHash, new Sink(entry));
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
        if (metadata.Version is not (1 or Envelope))
            throw new InvalidDataException("Unsupported city save version.");
        RulesetCapture capture = Held(zip, metadata.Version, Entry);
        RulesetSourceResult resolved = RulesetSource.Resolve(capture);
        if (resolved.Ruleset is null) throw new InvalidDataException(resolved.Describe());
        using var state = Entry("world.save");
        // Check the content identity before constructing a world under these rules.
        byte[] bytes = new byte[SaveHeader.Bytes];
        state.ReadExactly(bytes);
        SaveHeader header = SaveHeader.Read(bytes);
        if (header.RulesetInForce != capture.ContentHash)
            throw new InvalidDataException("The saved Ruleset does not match the city.");
        if (header.Key != WorldKey.FromSeed(metadata.Seed))
            throw new InvalidDataException("The saved seed does not match the city.");
        using var body = Entry("world.save");
        World world = SaveFile.Read(new Source(body), resolved.Ruleset, out header);
        if (body.ReadByte() != -1) throw new InvalidDataException("Unexpected data after the saved city.");
        return new SavedCity(world, header, capture, resolved.Names, metadata.Seed);
    }

    /// <summary>The Ruleset the save holds, recaptured so a package reloads without its directory.</summary>
    private static RulesetCapture Held(ZipArchive zip, int version, Func<string, Stream> entry)
    {
        if (version == 1)
        {
            using Stream held = entry(RulesetBundle.LegacyEntry);

            return Captured(RulesetCapture.FromEntries(RulesetBundle.LegacyEntry, Bytes(held), []));
        }

        var entries = new List<KeyValuePair<string, byte[]>>();

        foreach (ZipArchiveEntry held in zip.Entries)
        {
            if (held.FullName is "city.json" or "world.save") continue;
            using Stream content = held.Open();
            entries.Add(new KeyValuePair<string, byte[]>(held.FullName, Bytes(content)));
        }

        return Captured(RulesetBundle.Read(entries));
    }

    private static byte[] Bytes(Stream stream)
    {
        using var content = new MemoryStream();
        stream.CopyTo(content);
        return content.ToArray();
    }

    private static RulesetCapture Captured(RulesetCaptureResult captured) =>
        captured.Capture ?? throw new InvalidDataException(
            string.Join(Environment.NewLine, captured.Diagnostics));
}
