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
            var layer = new InstanceLayer();
            AddChild(layer);
            InstanceBuffer b = layer.Multimesh;
            b.Mesh = new BoxMesh();
            b.UseColors = true;
            b.UseCustomData = true;
            for (int i = 0; i < 70000; i++) Write(b, i, (ulong)i + 1, new Vector3(i % 100, 0, i / 100));
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.VisibleInstanceCount == 70000 && b.InstanceCount >= 70000, "dense batch exceeds old cap");
            Require(b.BatchCount == 1, "one dense Chunk");
            long uploads = b.Uploads;
            for (int i = 0; i < 70000; i++) Write(b, i, (ulong)i + 1, new Vector3(i % 100, 0, i / 100));
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.Uploads == uploads, "unchanged pass uploads nothing");
            // Reverse order, preserving entity identity and GPU slots.
            for (int i = 0; i < 70000; i++)
            {
                int original = 69999 - i;
                Write(b, i, (ulong)original + 1, new Vector3(original % 100, 0, original / 100));
            }
            b.VisibleInstanceCount = 70000;
            b.Flush();
            Require(b.Uploads == uploads, "enumeration order does not repack batches");
            Require(b.GetInstanceTransform(0).Origin == new Vector3(99, 0, 699), "CPU identity follows order");
            if (DisplayServer.GetName() != "headless")
                Require(b.UploadedTransform(0).Origin.IsEqualApprox(new Vector3(99, 0, 699)), "GPU identity follows order");
            // Same count, new identity, different Chunk, rotated geometry spanning its boundary.
            var transform = new Transform3D(Basis.FromEuler(new Vector3(.2f, .7f, .1f)) * Basis.FromScale(new Vector3(90, 20, 30)), new Vector3(1025, 10, -5));
            b.Identity(0, 999999);
            b.SetInstanceTransform(0, transform);
            b.SetInstanceColor(0, new Color(.2f, .4f, .6f, 1));
            b.SetInstanceCustomData(0, new Color(.1f, .3f, .5f, .7f));
            b.VisibleInstanceCount = 1;
            b.Flush();
            Require(b.BatchCount == 1 && b.InstanceCount == 16, "empty batches released and capacity shrinks");
            Require(b.Batches.Single().Bounds.Encloses(transform * b.Mesh.GetAabb()), "bounds cover complete rotated geometry");
            if (DisplayServer.GetName() != "headless")
            {
                Require(b.UploadedTransform(0).IsEqualApprox(transform), "bulk transform layout");
                Require(b.UploadedColour(0).IsEqualApprox(new Color(.2f, .4f, .6f, 1)), "bulk colour layout");
                Require(b.Batches.Single().Node.Multimesh.GetInstanceCustomData(0).IsEqualApprox(new Color(.1f, .3f, .5f, .7f)), "bulk custom layout");
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
            GD.Print("Renderer checks passed: capacity, retention, identity, removal, migration, bounds, bulk buffers.");
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
    private static void Require(bool condition, string name)
    {
        if (!condition) throw new InvalidOperationException(name);
    }
}
