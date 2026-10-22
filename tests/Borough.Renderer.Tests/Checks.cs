using System;
using System.Linq;
using Borough.Shell;
using Godot;

namespace Borough.Renderer.Tests;

public partial class Checks : Node
{
    public override void _Ready()
    {
        try
        {
            CheckFacades();
            CheckRoofs();
            CheckNearBand();
            CheckGate();
            CheckOwners();
            var layer = new InstanceLayer();
            AddChild(layer);
            InstanceBuffer b = layer.Multimesh;
            b.Mesh = new BoxMesh();
            b.UseColors = true;
            b.UseCustomData = true;
            for (int i = 0; i < 70000; i++) Write(b, i, (ulong)i + 1, new Vector3(i % 100, 0, (i / 100) * .25f));
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.VisibleInstanceCount == 70000 && b.InstanceCount >= 70000, "dense batch exceeds old cap");
            Require(b.BatchCount == 1, "one dense Chunk");
            long uploads = b.Uploads;
            for (int i = 0; i < 70000; i++) Write(b, i, (ulong)i + 1, new Vector3(i % 100, 0, (i / 100) * .25f));
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.Uploads == uploads, "unchanged pass uploads nothing");
            // Reverse order, preserving entity identity and GPU slots.
            for (int i = 0; i < 70000; i++)
            {
                int original = 69999 - i;
                Write(b, i, (ulong)original + 1, new Vector3(original % 100, 0, (original / 100) * .25f));
            }
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.Uploads == uploads, "enumeration order does not repack batches");
            Require(b.GetInstanceTransform(0).Origin == new Vector3(99, 0, 174.75f), "CPU identity follows order");
            if (DisplayServer.GetName() != "headless")
                Require(b.UploadedTransform(0).Origin.IsEqualApprox(new Vector3(99, 0, 174.75f)), "GPU identity follows order");
            // Same count, new identity, different Chunk, rotated geometry spanning its boundary.
            var transform = new Transform3D(Basis.FromEuler(new Vector3(.2f, .7f, .1f)) * Basis.FromScale(new Vector3(90, 20, 30)), new Vector3(1025, 10, -5));
            b.Identity(0, 999999);
            b.SetInstanceTransform(0, transform);
            b.SetInstanceColor(0, new Color(.2f, .4f, .6f, 1));
            b.SetInstanceCustomData(0, new Color(.1f, .3f, .5f, 2047f));
            b.VisibleInstanceCount = 1;
            b.Flush();
            Require(b.BatchCount == 1 && b.InstanceCount == 16, "empty batches released and capacity shrinks");
            Require(b.Batches.Single().Bounds.Encloses(transform * b.Mesh.GetAabb()), "bounds cover complete rotated geometry");
            if (DisplayServer.GetName() != "headless")
            {
                Require(b.UploadedTransform(0).IsEqualApprox(transform), "bulk transform layout");
                Require(b.UploadedColour(0).IsEqualApprox(new Color(.2f, .4f, .6f, 1)), "bulk colour layout");
                Require(b.Batches.Single().Node.Multimesh.GetInstanceCustomData(0).IsEqualApprox(new Color(.1f, .3f, .5f, 2047f)), "bulk custom layout");
            }
            // Multiple components per entity, then relocation and one component disappearing.
            Write(b, 0, 5, Vector3.Zero); Write(b, 1, 5, new Vector3(2048, 0, 0));
            b.VisibleInstanceCount = 2; b.Flush();
            uploads = b.Uploads;
            Write(b, 0, 5, Vector3.One); Write(b, 1, 5, new Vector3(2048, 0, 0));
            b.VisibleInstanceCount = 2; b.Flush();
            Require(b.Uploads == uploads + 1, "one changed Chunk uploads alone");
            Write(b, 0, 5, new Vector3(4096, 0, 0));
            b.VisibleInstanceCount = 1; b.Flush();
            Require(b.BatchCount == 1, "migration and component removal release old batches");
            b.Flush(new Vector3(50000, 0, 0), 2000);
            Require(b.VisibleInstanceCount == 1 && b.ResidentInstances == 0 && b.InstanceCount == 0, "distant detail retains records and releases GPU storage");
            b.Flush(new Vector3(4096, 0, 0), 2000);
            Require(b.ResidentInstances == 1 && b.InstanceCount > 0, "returning camera restores detail");
            uploads = b.Uploads;
            b.Flush(new Vector3(4100, 0, 0), 2000);
            Require(b.Uploads == uploads, "nearby camera motion uploads nothing");
            b.Replace(5, new[] { new InstanceValue(new Transform3D(Basis.Identity, new Vector3(4097, 0, 0)), Colors.Red) });
            b.Flush(new Vector3(4100, 0, 0), 2000);
            Require(b.Uploads == uploads + 1 && b.IdAt(0) == 5, "incremental entity edit preserves identity");
            Mesh near = new SphereMesh(), middle = new CapsuleMesh();
            layer.Details = [(100, near), (500, middle)];
            uploads = b.Uploads;
            b.Flush(new Vector3(4097, 50, 0), 2000);
            InstanceBuffer.Batch chunk = b.Batches.Single();
            Require(chunk.Detail == 0 && chunk.Node.Multimesh.Mesh == near, "a chunk near the eye draws the nearest band");
            b.Flush(new Vector3(4097, 110, 0), 2000);
            Require(chunk.Detail == 0, "a chunk keeps its band inside the hysteresis margin");
            b.Flush(new Vector3(4097, 300, 0), 2000);
            Require(chunk.Detail == 1 && chunk.Node.Multimesh.Mesh == middle, "a receding eye moves the chunk to the next band");
            b.Flush(new Vector3(4097, 1000, 0), 2000);
            Require(chunk.Detail == -1 && chunk.Node.Multimesh.Mesh == b.Mesh && b.Uploads == uploads, "beyond every band the chunk draws the buffer's mesh without an upload");
            layer.Details = [];
            b.Replace(5, Array.Empty<InstanceValue>());
            b.Flush();
            Require(b.VisibleInstanceCount == 0, "incremental entity removal");
            b.VisibleInstanceCount = 0; b.Flush();
            Require(b.InstanceCount == 0 && b.BatchCount == 0, "empty layer releases all buffers");
            Write(b, 0, 1, Vector3.Zero); Write(b, 1, 2, new Vector3(2048, 0, 0));
            b.VisibleInstanceCount = 2;
            b.Flush(maxBytes: 1280, allowOversize: false);
            Require(b.PendingBatches == 1 && b.ResidentInstances == 1, "upload allowance defers work without dropping records");
            b.Flush(maxBytes: 1280, allowOversize: false);
            Require(b.PendingBatches == 0 && b.ResidentInstances == 2, "deferred uploads finish on subsequent frames");
            b.VisibleInstanceCount = 0; b.Flush();
            GD.Print("Renderer checks passed: capacity, retention, identity, removal, migration, bounds, bulk buffers, near band, gate, owners.");
            GetTree().Quit();
        }
        catch (Exception error) { GD.PrintErr(error); GetTree().Quit(1); }
    }
    private static void Write(InstanceBuffer buffer, int index, ulong id, Vector3 position)
    {
        buffer.Identity(index, id);
        buffer.SetInstanceTransform(index, new Transform3D(Basis.Identity, position));
        buffer.SetInstanceColor(index, Colors.White);
        buffer.SetInstanceCustomData(index, Colors.Black);
    }

    private void CheckNearBand()
    {
        bool near = true;
        var bodies = new InstanceLayer();
        var boxes = new InstanceLayer();
        AddChild(bodies);
        AddChild(boxes);
        bodies.Multimesh.Mesh = boxes.Multimesh.Mesh = new BoxMesh();
        bodies.Multimesh.Near = boxes.Multimesh.Near = _ => near;
        bodies.Multimesh.NearOnly = true;
        var at = new Transform3D(Basis.Identity, new Vector3(10, 0, 10));
        bodies.Multimesh.Replace(7, [new InstanceValue(at, Colors.White)]);
        boxes.Multimesh.Replace(7, [new InstanceValue(at, Colors.White, default, Far: true)]);
        boxes.Multimesh.Replace(8, [new InstanceValue(at, Colors.White)]);
        bodies.Multimesh.Flush(Vector3.Zero);
        boxes.Multimesh.Flush(Vector3.Zero);
        Require(bodies.Multimesh.ResidentInstances == 1 && !boxes.Multimesh.IsResident(0) && boxes.Multimesh.IsResident(1),
            "a near chunk draws the body and hides its far box");
        if (DisplayServer.GetName() != "headless")
            Require(boxes.Multimesh.UploadedTransform(0).Basis.Scale == Vector3.Zero, "a hidden far box is collapsed");

        near = false;
        bodies.Multimesh.Repartition();
        boxes.Multimesh.Repartition();
        bodies.Multimesh.Flush(Vector3.Zero);
        boxes.Multimesh.Flush(Vector3.Zero);
        Require(bodies.Multimesh.ResidentInstances == 0 && boxes.Multimesh.IsResident(0) && boxes.Multimesh.IsResident(1),
            "a far chunk drops the body and draws its far box");
        bodies.QueueFree();
        boxes.QueueFree();
    }
    private void CheckGate()
    {
        var layer = new InstanceLayer { Gated = true, DetailDistance = 1000f };
        AddChild(layer);
        Require(GetViewport().GetCamera3D() is null, "the gate check starts without a camera");
        layer.BuildFrom();
        Require(!layer.Reaches(Vector3.Zero) && layer.Ground() is { } none && !none.HasArea() && layer.Stale(Vector3.Zero),
            "a gated build before any camera is empty and stale");

        var camera = new Camera3D { Position = new Vector3(0, 100, 0) };
        AddChild(camera);
        camera.MakeCurrent();
        layer.BuildFrom();
        Require(layer.Reaches(new Vector3(1000, 0, 0)) && !layer.Reaches(new Vector3(5000, 0, 0)),
            "a gated build reaches its detail distance and no farther");
        Require(layer.Ground() is { } ground && ground.HasPoint(new Vector2(1000, 0)) && !ground.HasPoint(new Vector2(5000, 0)),
            "the reached ground holds what reaches");
        Require(!layer.Stale(new Vector3(200, 100, 0)) && layer.Stale(new Vector3(300, 100, 0)),
            "a gated layer goes stale after a quarter of its detail distance");
        var box = new Transform3D(Basis.FromScale(new Vector3(4000, 1, 1)), new Vector3(3000, 0, 0));
        Require(layer.Reaches(box), "a long box reaches by its nearest end");

        camera.Position = new Vector3(0, 5000, 0);
        layer.BuildFrom();
        Require(layer.Ground() is { } high && !high.HasArea() && !layer.Reaches(Vector3.Zero),
            "a camera beyond reach builds nothing");
        layer.DetailDistance = 8000f;
        Require(layer.Stale(camera.Position), "a changed detail distance makes the layer stale");
        camera.QueueFree();
        layer.QueueFree();
    }

    private void CheckOwners()
    {
        var layer = new InstanceLayer();
        AddChild(layer);
        InstanceBuffer b = layer.Multimesh;
        b.Mesh = new BoxMesh();
        b.UseColors = true;
        static InstanceValue At(float east) => new(new Transform3D(Basis.Identity, new Vector3(east, 0, 0)), Colors.White);

        b.Write(2, [At(10), At(20)], [21, 22]);
        b.Write(1, [At(2048)], [11]);
        b.Flush();
        Require(b.VisibleInstanceCount == 3 && b.Canonical().Select(b.IdAt).SequenceEqual<ulong>([11, 21, 22]),
            "canonical order is owner key, then each owner's order");

        long uploads = b.Uploads;
        Require(!b.Write(2, [At(10), At(20)], [21, 22]), "an unchanged owner reports no geometry");
        b.Flush();
        Require(b.Uploads == uploads, "an unchanged owner uploads nothing");

        Require(b.Write(2, [At(15)], [23]), "a shrunk owner reports geometry");
        b.Flush();
        Require(b.Uploads == uploads + 1 && b.Canonical().Select(b.IdAt).SequenceEqual<ulong>([11, 23]),
            "a rewritten owner uploads only its own chunk and keeps its place");

        b.Remove(1);
        b.Flush();
        Require(b.VisibleInstanceCount == 1 && b.BatchCount == 1 && b.IdAt(0) == 23, "a removed owner releases its instances");

        Require(Throws(() => b.VisibleInstanceCount = 0), "a layer written by owner refuses a whole write");
        Require(Throws(() => b.Replace(9, [At(0)])), "a layer written by owner refuses an entity edit");
        b.Remove(2);
        b.Flush();
        Require(b.BatchCount == 0, "an emptied owner layer releases all buffers");
        layer.QueueFree();
    }

    private static bool Throws(Action action)
    {
        try { action(); return false; }
        catch (InvalidOperationException) { return true; }
    }

    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
