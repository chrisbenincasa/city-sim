using System.Collections.Generic;
using Godot;

namespace Borough.Shell;

/// <summary>
/// The city scene's material textures, decoded on Godot's worker threads while the city is
/// prepared. A path missing from <see cref="City"/> still loads, on the calling thread.
/// </summary>
public static class Textures
{
    private static readonly string[] City =
    [
        "res://assets/city/brick-wall-diffuse.jpg",
        "res://assets/city/brick-wall-normal.jpg",
        "res://assets/city/material-study/painted-plaster-albedo.jpg",
        "res://assets/city/material-study/painted-plaster-normal.jpg",
        "res://assets/city/material-study/painted-plaster-roughness.jpg",
        "res://assets/city/material-study/damaged-plaster-height.jpg",
        "res://assets/city/material-study/damaged-plaster-normal.jpg",
        "res://assets/city/roofing/asphalt-shingles-albedo.png",
        "res://assets/city/roofing/asphalt-shingles-normal.png",
        "res://assets/city/roofing/asphalt-shingles-roughness.png",
    ];

    private static readonly HashSet<string> Requested = [];

    public static void Prefetch()
    {
        foreach (string path in City)
        {
            if (ResourceLoader.LoadThreadedRequest(path) == Error.Ok) Requested.Add(path);
        }
    }

    public static Texture2D Load(string path) =>
        Requested.Remove(path) ? (Texture2D)ResourceLoader.LoadThreadedGet(path) : GD.Load<Texture2D>(path);
}
