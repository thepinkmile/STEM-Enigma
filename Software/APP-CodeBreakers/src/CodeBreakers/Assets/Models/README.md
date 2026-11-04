# Enigma visualization meshes

These are original, deterministic low-poly meshes generated locally by
`Software/APP-CodeBreakers/tools/generate_models.py`. The generator uses only
Python's standard library; it downloads or embeds no external assets. The
models are stylized educational visualization parts, not manufacturing designs
or historically exact Enigma replicas.

## Coordinates and intended scale

Every mesh is centered at its local origin. Positive **Y** is up. OBJ files
contain triangulated faces and per-triangle outward normals (`vn`); they are
intended to be imported as reusable meshes, assigned materials by the caller,
and instanced as needed. This directory defines only individual parts: assembly
layout and animation belong to the application.

Dimensions are in arbitrary unit-ish scene units; scale them to suit the
visualization. Bounds reported below are exact generator targets (to rounding
shown), in X × Y × Z order:

| File | Bounds | Features / axis |
| --- | ---: | --- |
| `case-base.obj` | 1 × 1 × 1 | Chamfered unit box |
| `case-wall.obj` | 1 × 1 × 1 | Chamfered unit box |
| `lid.obj` | 1 × 1 × 1 | Chamfered unit box |
| `keyboard-plate.obj` | 1 × 1 × 1 | Chamfered unit box |
| `lamp-panel.obj` | 1 × 1 × 1 | Chamfered unit box |
| `plugboard.obj` | 1 × 1 × 1 | Chamfered unit box |
| `rotor.obj` | 0.3 × 2 × 2 | Disc axis X, radius 1, 0.3 wide; bore and 26 separately grouped contact nubs |
| `gear.obj` | 0.12 × 1.998108 × 1.994023 | Axis X, outer radius 1, 26 distinct teeth, central bore |
| `reflector.obj` | 0.5 × 1.6 × 1.6 | Axis X, hollow beveled metal housing; faces and pins instanced separately |
| `shaft.obj` | 1 × 0.2 × 0.2 | Axis X, radius 0.1 |
| `key.obj` | 1 × 0.25 × 1 | Axis Y, flat round cap with beveled rim, radius 0.5 |
| `lamp.obj` | 1 × 0.25 × 1 | Axis Y, round lens, radius 0.5 |
| `contact.obj` | 1 × 0.15 × 1 | Axis Y, round contact, radius 0.5 |

OBJ normals are intentionally faceted; a renderer may apply its preferred
material and lighting treatment. `rotor.obj` includes a separate named object
for each of its 26 contact nubs, while the other parts are each reusable as a
single mesh.

## Regeneration and validation

From any working directory, run:

```text
python Software/APP-CodeBreakers/tools/generate_models.py
```

The script deterministically rewrites these OBJ files and prints each relative
path, exact bounds, vertex count, and triangle count. Before completing, it
checks finite vertex/normal values, one supplied normal per triangulated face,
valid OBJ index ranges, nondegenerate triangles, closed manifold edges, and
consistent outward winding for every named component.
