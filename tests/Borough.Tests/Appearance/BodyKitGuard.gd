extends SceneTree

# Run after building the shell with Godot's --headless --script option.
func _initialize():
    var shell = load("res://Main.cs").new()
    assert(shell.has_method("CheckBodyKitMaterial"))
    var blank = StandardMaterial3D.new()
    blank.resource_name = "supported-kit-probe"
    blank.cull_mode = BaseMaterial3D.CULL_DISABLED
    assert(shell.call("CheckBodyKitMaterial", blank) == 0)
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
        assert(shell.call("CheckBodyKitMaterial", material) == 1)
        assert(shell.call("CheckBodyKitMaterial", material) == 0)
    shell.free()
    print("Body kit drift guard passed; four unsupported properties warned once each.")
    quit()
