# RezTopPhoto Billboard — CS2 asset preparation

This folder contains the source artwork and the first Source 2 material setup for the RezTop.cc billboard.

## Files
- `reztop_billboard.obj` — editable billboard mesh source (OBJ).
- `reztop_billboard.mtl` — OBJ preview material definitions.
- `reztop_billboard_texture.png` — supplied RezTop.cc banner artwork.
- `reztop_billboard.vmat` — CS2 material source using the `csgo_complex.vfx` shader.
- `compile_material.bat` — starter script for the CS2 Resource Compiler.

## Important: material and texture path

In your CS2 addon content folder, create:

```
content/csgo_addons/reztopphoto/
└── materials/
    └── reztopphoto/
        ├── reztop_billboard.vmat
        └── reztop_billboard_texture.png
```

Copy `reztop_billboard.vmat` and `reztop_billboard_texture.png` into that folder. The VMAT references the texture as:

```
materials/reztopphoto/reztop_billboard_texture.png
```

The game uses compiled Source 2 resources. The source `.vmat` and PNG are not themselves the final runtime assets. Compile the material with the CS2 Workshop Tools Resource Compiler. The compiler should produce compiled material/texture resources (for example `.vmat_c` and `.vtex_c`). Verify the output in the addon's game directory and preview it in the Material Editor.

## Model stage

The OBJ is an interchange mesh, not a compiled CS2 model. To finish the model:
1. Import `reztop_billboard.obj` into Blender.
2. Check the face orientation, UVs, and the two material slots (`Photo_Surface`, `Frame_Black`).
3. Assign the compiled/created billboard material to the photo surface and a dark material to the frame.
4. Export the mesh as FBX.
5. In CS2 Workshop Tools, create a model in ModelDoc, import the FBX, assign the billboard material, configure collision as appropriate, and compile the `.vmdl` to `.vmdl_c`.

## Current status

- [x] Original banner PNG is present in this repository folder.
- [x] OBJ/MTL source is present.
- [x] CS2 VMAT source has been added.
- [ ] Material and texture compiled and tested in CS2 Workshop Tools.
- [ ] OBJ imported/exported and model compiled in ModelDoc.
- [ ] In-game placement and visibility tested on a workshop map.

## Notes

CounterStrikeSharp cannot turn an arbitrary PNG into a world-space model texture by assigning a file path. The model and its Source 2 material/texture assets must be compiled and available to clients through a supported addon/workshop distribution path.
