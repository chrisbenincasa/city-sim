extends SceneTree

# Whether Main.CheckBodyKitMaterial still catches a kit material body-kit.gdshader cannot carry.
# It needs Godot, so it is not an xunit test. Build the shell first, then run it from the
# repository root:
#
#   dotnet build src/Borough.Godot
#   godot --headless --path src/Borough.Godot --script ../../tests/Borough.Tests/Appearance/BodyKitGuard.gd
#
# It prints one line and exits 0 when it passes. ⚠ A failure quits with 1 rather than asserting:
# an assertion leaves the SceneTree running, and a hung run reads as a slow one.

var failures: Array[String] = []

func _initialize():
    var shell = load("res://Main.cs").new()
    if shell.has_method("CheckBodyKitMaterial"):
        _check(shell)
    else:
        failures.append("Main.cs does not offer CheckBodyKitMaterial to the scripting API.")
    shell.free()

    if failures.is_empty():
        print("Body kit drift guard passed; four unsupported properties warned once each.")
        quit()
        return

    for failure in failures:
        printerr("body kit drift guard: " + failure)
    quit(1)

func _check(shell):
    # A material the rebuild reproduces in full, double-sided as the shader is.
    var blank = StandardMaterial3D.new()
    blank.resource_name = "supported-kit-probe"
    blank.cull_mode = BaseMaterial3D.CULL_DISABLED
    var carried = shell.call("CheckBodyKitMaterial", blank)
    if carried != 0:
        failures.append("a fully carried material warned %d times." % carried)

    # One drifting material per property, each warning once and then never again.
    for property in ["albedo_texture", "normal_enabled", "transparency", "emission_enabled"]:
        var material = StandardMaterial3D.new()
        material.resource_name = "kit-drift-" + property
        material.cull_mode = BaseMaterial3D.CULL_DISABLED
        match property:
            "albedo_texture":
                material.albedo_texture = ImageTexture.create_from_image(Image.create(1, 1, false, Image.FORMAT_RGBA8))
            "normal_enabled":
                material.normal_enabled = true
            "transparency":
                material.transparency = BaseMaterial3D.TRANSPARENCY_ALPHA
            "emission_enabled":
                material.emission_enabled = true
        var first = shell.call("CheckBodyKitMaterial", material)
        var again = shell.call("CheckBodyKitMaterial", material)
        if first != 1:
            failures.append("drifting %s warned %d times instead of once." % [property, first])
        if again != 0:
            failures.append("drifting %s warned again on a second look." % property)
