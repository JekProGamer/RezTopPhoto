import bpy
from pathlib import Path

# Run this script from Blender. Keep this script, OBJ, MTL and PNG in one folder.
ROOT = Path(__file__).resolve().parent
OBJ = ROOT / "reztop_billboard.obj"
TEXTURE = ROOT / "reztop_billboard_texture.png"
FBX = ROOT / "reztop_billboard.fbx"

if not OBJ.exists():
    raise FileNotFoundError(f"OBJ not found: {OBJ}")
if not TEXTURE.exists():
    raise FileNotFoundError(f"Texture not found: {TEXTURE}")

# Clear the scene.
bpy.ops.object.select_all(action="SELECT")
bpy.ops.object.delete(use_global=False)

# Import the prepared billboard mesh.
if hasattr(bpy.ops.wm, "obj_import"):
    bpy.ops.wm.obj_import(filepath=str(OBJ))
else:
    bpy.ops.import_scene.obj(filepath=str(OBJ), use_split_objects=True, use_split_groups=True)

meshes = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
if not meshes:
    raise RuntimeError("The OBJ imported, but no mesh objects were created.")

# Create the photo material and connect the supplied banner texture.
photo = bpy.data.materials.get("Photo_Surface") or bpy.data.materials.new("Photo_Surface")
photo.use_nodes = True
nodes = photo.node_tree.nodes
links = photo.node_tree.links
bsdf = next((n for n in nodes if n.type == "BSDF_PRINCIPLED"), None)
if bsdf is None:
    bsdf = nodes.new("ShaderNodeBsdfPrincipled")
output = next((n for n in nodes if n.type == "OUTPUT_MATERIAL"), None)
if output is None:
    output = nodes.new("ShaderNodeOutputMaterial")
if not any(link.from_node == bsdf and link.to_node == output for link in links):
    links.new(bsdf.outputs["BSDF"], output.inputs["Surface"])

image = bpy.data.images.load(str(TEXTURE), check_existing=True)
tex = nodes.new("ShaderNodeTexImage")
tex.image = image
links.new(tex.outputs["Color"], bsdf.inputs["Base Color"])
if "Roughness" in bsdf.inputs:
    bsdf.inputs["Roughness"].default_value = 0.8

# Create a dark, non-metallic frame material.
frame = bpy.data.materials.get("Frame_Black") or bpy.data.materials.new("Frame_Black")
frame.use_nodes = True
frame_bsdf = next((n for n in frame.node_tree.nodes if n.type == "BSDF_PRINCIPLED"), None)
if frame_bsdf:
    frame_bsdf.inputs["Base Color"].default_value = (0.012, 0.014, 0.02, 1.0)
    frame_bsdf.inputs["Metallic"].default_value = 0.15
    frame_bsdf.inputs["Roughness"].default_value = 0.42

# Preserve OBJ material assignments by name; fill any missing slots.
for obj in meshes:
    old_slots = list(obj.data.materials)
    old_names = [m.name if m else "" for m in old_slots]
    obj.data.materials.clear()
    obj.data.materials.append(photo)
    obj.data.materials.append(frame)
    for poly in obj.data.polygons:
        old_index = poly.material_index
        old_name = old_names[old_index] if old_index < len(old_names) else ""
        poly.material_index = 1 if "frame" in old_name.lower() else 0
    obj.name = "RezTopPhoto_Billboard"
    obj.data.name = "RezTopPhoto_Billboard_Mesh"

# Select only the billboard and export an FBX for CS2 ModelDoc.
bpy.ops.object.select_all(action="DESELECT")
for obj in meshes:
    obj.select_set(True)
bpy.context.view_layer.objects.active = meshes[0]

bpy.ops.export_scene.fbx(
    filepath=str(FBX),
    use_selection=True,
    apply_unit_scale=True,
    apply_scale_options="FBX_SCALE_ALL",
    bake_space_transform=False,
    object_types={"MESH"},
    use_mesh_modifiers=True,
    mesh_smooth_type="FACE",
    add_leaf_bones=False,
    path_mode="COPY",
    embed_textures=False,
)

print(f"SUCCESS: FBX exported to {FBX}")
print("Next: import reztop_billboard.fbx in CS2 Workshop Tools / ModelDoc and assign the compiled RezTopPhoto material.")
