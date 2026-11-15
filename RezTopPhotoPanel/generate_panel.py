# Run inside Blender's Scripting workspace.
# Creates a UV-mapped flat photo panel and exports it as FBX.
import bpy
import os

WIDTH = 1.6
HEIGHT = 0.9
OUTPUT_NAME = "photo_panel.fbx"

# Clear scene
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

# Create a plane in the XY plane; its front normal points +Z.
bpy.ops.mesh.primitive_plane_add(size=1.0, location=(0.0, 0.0, 0.0))
panel = bpy.context.object
panel.name = "RezTopPhotoPanel"
panel.dimensions = (WIDTH, HEIGHT, 0.0)
bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

# Ensure full 0..1 UV mapping.
if panel.data.uv_layers.active:
    for loop in panel.data.uv_layers.active.data:
        loop.uv.x = max(0.0, min(1.0, loop.uv.x))
        loop.uv.y = max(0.0, min(1.0, loop.uv.y))

# Give the mesh a material slot for the Source 2 material assignment step.
material = bpy.data.materials.new(name="M_PhotoSurface")
material.use_nodes = True
panel.data.materials.append(material)

# Select only the panel for export.
bpy.ops.object.select_all(action="DESELECT")
panel.select_set(True)
bpy.context.view_layer.objects.active = panel

output_path = os.path.join(os.path.dirname(bpy.data.filepath) or os.getcwd(), OUTPUT_NAME)
bpy.ops.export_scene.fbx(
    filepath=output_path,
    use_selection=True,
    apply_scale_options="FBX_SCALE_ALL",
    axis_forward="-Z",
    axis_up="Y",
    object_types={"MESH"},
    mesh_smooth_type="OFF"
)
print("Exported RezTop photo panel to:", output_path)
