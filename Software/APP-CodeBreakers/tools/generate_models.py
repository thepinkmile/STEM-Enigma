#!/usr/bin/env python3
"""Generate original, deterministic low-poly Enigma teaching meshes (OBJ)."""

from __future__ import annotations

import math
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
MODELS = ROOT / "src" / "CodeBreakers" / "Assets" / "Models"
SIDES = 48
TEETH = 26


class Mesh:
    def __init__(self) -> None:
        self.vertices: list[tuple[float, float, float]] = []
        self.triangles: list[tuple[int, int, int]] = []

    def vertex(self, point: tuple[float, float, float]) -> int:
        self.vertices.append(tuple(float(v) for v in point))
        return len(self.vertices) - 1

    def triangle(self, a: int, b: int, c: int) -> None:
        self.triangles.append((a, b, c))

    def quad(self, a: int, b: int, c: int, d: int) -> None:
        self.triangle(a, b, c)
        self.triangle(a, c, d)

    def orient_outward(self) -> None:
        volume6 = 0.0
        for a, b, c in self.triangles:
            p, q, r = (self.vertices[i] for i in (a, b, c))
            volume6 += (
                p[0] * (q[1] * r[2] - q[2] * r[1])
                + p[1] * (q[2] * r[0] - q[0] * r[2])
                + p[2] * (q[0] * r[1] - q[1] * r[0])
            )
        if volume6 < 0:
            self.triangles = [(a, c, b) for a, b, c in self.triangles]


class Model:
    def __init__(self) -> None:
        self.vertices: list[tuple[float, float, float]] = []
        self.triangles: list[tuple[int, int, int]] = []
        self.groups: list[tuple[str, int, int]] = []

    def add(self, name: str, mesh: Mesh) -> None:
        mesh.orient_outward()
        offset = len(self.vertices)
        first = len(self.triangles)
        self.vertices.extend(mesh.vertices)
        self.triangles.extend(tuple(i + offset for i in face) for face in mesh.triangles)
        self.groups.append((name, first, len(self.triangles)))


def _ring(mesh: Mesh, axis: str, along: float, radius: float, count: int) -> list[int]:
    result = []
    for i in range(count):
        angle = 2.0 * math.pi * i / count
        c, s = math.cos(angle), math.sin(angle)
        if axis == "x":
            result.append(mesh.vertex((along, radius * c, radius * s)))
        else:
            result.append(mesh.vertex((radius * c, along, radius * s)))
    return result


def cylinder(axis: str, length: float, radius: float, count: int = SIDES) -> Mesh:
    mesh = Mesh()
    half = length / 2.0
    low, high = _ring(mesh, axis, -half, radius, count), _ring(mesh, axis, half, radius, count)
    if axis == "x":
        low_center = mesh.vertex((-half, 0, 0))
        high_center = mesh.vertex((half, 0, 0))
    else:
        low_center = mesh.vertex((0, -half, 0))
        high_center = mesh.vertex((0, half, 0))
    for i in range(count):
        j = (i + 1) % count
        mesh.quad(low[i], low[j], high[j], high[i])
        mesh.triangle(low_center, low[j], low[i])
        mesh.triangle(high_center, high[i], high[j])
    return mesh


def annular_prism(
    axis: str,
    outer: list[tuple[float, float]],
    inner_radius: float,
    low: float,
    high: float,
) -> Mesh:
    mesh = Mesh()
    count = len(outer)
    outer_low, outer_high = [], []
    inner_low, inner_high = [], []
    for along, target in ((low, outer_low), (high, outer_high)):
        for a, r in outer:
            angle = a
            c, s = math.cos(angle), math.sin(angle)
            point = (along, r * c, r * s) if axis == "x" else (r * c, along, r * s)
            target.append(mesh.vertex(point))
    for along, target in ((low, inner_low), (high, inner_high)):
        target.extend(_ring(mesh, axis, along, inner_radius, count))
    for i in range(count):
        j = (i + 1) % count
        mesh.quad(outer_low[i], outer_low[j], outer_high[j], outer_high[i])
        mesh.quad(inner_low[i], inner_high[i], inner_high[j], inner_low[j])
        mesh.quad(outer_high[i], outer_high[j], inner_high[j], inner_high[i])
        mesh.quad(outer_low[i], inner_low[i], inner_low[j], outer_low[j])
    return mesh


def lathe(axis: str, profile: list[tuple[float, float]], count: int = SIDES) -> Mesh:
    """Revolve (axis-position, radius) boundary points around an axis."""
    mesh = Mesh()
    rings: list[list[int]] = []
    for along, radius in profile:
        if radius <= 1e-10:
            point = (along, 0.0, 0.0) if axis == "x" else (0.0, along, 0.0)
            rings.append([mesh.vertex(point)])
        else:
            rings.append(_ring(mesh, axis, along, radius, count))
    for index, ring_a in enumerate(rings):
        ring_b = rings[(index + 1) % len(rings)]
        if len(ring_a) == 1 and len(ring_b) == 1:
            continue
        for i in range(count):
            j = (i + 1) % count
            if len(ring_a) == 1:
                mesh.triangle(ring_a[0], ring_b[j], ring_b[i])
            elif len(ring_b) == 1:
                mesh.triangle(ring_a[i], ring_a[j], ring_b[0])
            else:
                mesh.quad(ring_a[i], ring_a[j], ring_b[j], ring_b[i])
    return mesh


def beveled_box() -> Mesh:
    """Unit chamfered box with bevels around its top and bottom perimeter."""
    mesh = Mesh()
    bevel_corner = 0.055
    levels = [(-0.5, 0.42), (-0.42, 0.5), (0.42, 0.5), (0.5, 0.42)]
    rings: list[list[int]] = []
    for z, half in levels:
        cut = min(bevel_corner, half * 0.35)
        outline = [
            (-half + cut, -half), (half - cut, -half),
            (half, -half + cut), (half, half - cut),
            (half - cut, half), (-half + cut, half),
            (-half, half - cut), (-half, -half + cut),
        ]
        rings.append([mesh.vertex((x, y, z)) for x, y in outline])
    count = len(rings[0])
    for lower, upper in zip(rings, rings[1:]):
        for i in range(count):
            j = (i + 1) % count
            mesh.quad(lower[i], lower[j], upper[j], upper[i])
    bottom = mesh.vertex((0, 0, levels[0][0]))
    top = mesh.vertex((0, 0, levels[-1][0]))
    for i in range(count):
        j = (i + 1) % count
        mesh.triangle(bottom, rings[0][j], rings[0][i])
        mesh.triangle(top, rings[-1][i], rings[-1][j])
    return mesh


def rotor_mesh() -> Mesh:
    model = Model()
    model.add("rotor_body", lathe("x", [
        (-0.15, 0.25), (-0.15, 0.92), (-0.13, 1.0),
        (0.13, 1.0), (0.15, 0.92), (0.15, 0.25),
        (0.13, 0.22), (-0.13, 0.22),
    ]))
    for i in range(TEETH):
        angle = 2 * math.pi * i / TEETH
        y, z = 0.72 * math.cos(angle), 0.72 * math.sin(angle)
        nub = cylinder("x", 0.06, 0.045, 12)
        nub.vertices = [(x + 0.12, yy + y, zz + z) for x, yy, zz in nub.vertices]
        model.add(f"rotor_contact_{i + 1:02d}", nub)
    return model


def gear_mesh() -> Mesh:
    points: list[tuple[float, float]] = []
    pitch = 2.0 * math.pi / TEETH
    pattern = [(-0.42, 0.84), (-0.26, 0.84), (-0.18, 1.0),
               (0.18, 1.0), (0.26, 0.84), (0.42, 0.84)]
    for tooth in range(TEETH):
        center = tooth * pitch
        points.extend((center + offset * pitch, radius) for offset, radius in pattern)
    return annular_prism("x", points, 0.18, -0.06, 0.06)


def write_obj(name: str, model: Model) -> tuple[Path, tuple[float, float, float], int, int]:
    path = MODELS / f"{name}.obj"
    normals: list[tuple[float, float, float]] = []
    face_normals: list[int] = []
    for a, b, c in model.triangles:
        p, q, r = (model.vertices[i] for i in (a, b, c))
        ux, uy, uz = q[0] - p[0], q[1] - p[1], q[2] - p[2]
        vx, vy, vz = r[0] - p[0], r[1] - p[1], r[2] - p[2]
        nx, ny, nz = uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx
        magnitude = math.sqrt(nx * nx + ny * ny + nz * nz)
        if magnitude <= 1e-12:
            raise ValueError(f"{name}: degenerate triangle")
        normals.append((nx / magnitude, ny / magnitude, nz / magnitude))
        face_normals.append(len(normals))

    lines = ["# Original procedural educational mesh; generated by tools/generate_models.py"]
    lines.extend(f"v {x:.8f} {y:.8f} {z:.8f}" for x, y, z in model.vertices)
    lines.extend(f"vn {x:.8f} {y:.8f} {z:.8f}" for x, y, z in normals)
    group_by_triangle = {}
    for group_name, start, end in model.groups:
        for triangle_index in range(start, end):
            group_by_triangle[triangle_index] = group_name
    last_group = None
    for index, (a, b, c) in enumerate(model.triangles):
        group = group_by_triangle.get(index, name)
        if group != last_group:
            lines.append(f"o {group}")
            last_group = group
        normal = face_normals[index]
        lines.append(f"f {a + 1}//{normal} {b + 1}//{normal} {c + 1}//{normal}")
    path.write_text("\n".join(lines) + "\n", encoding="ascii", newline="\n")

    extents = tuple(
        max(point[axis] for point in model.vertices) - min(point[axis] for point in model.vertices)
        for axis in range(3)
    )
    validate_obj(path)
    return path, extents, len(model.vertices), len(model.triangles)


def validate_obj(path: Path) -> None:
    vertices: list[tuple[float, float, float]] = []
    normals: list[tuple[float, float, float]] = []
    faces: dict[str, list[tuple[int, int, int, int, int, int]]] = {}
    group = path.stem
    for line in path.read_text(encoding="ascii").splitlines():
        fields = line.split()
        if not fields:
            continue
        if fields[0] == "o":
            group = fields[1]
            faces.setdefault(group, [])
        elif fields[0] == "v":
            point = tuple(float(value) for value in fields[1:])
            if len(point) != 3 or not all(math.isfinite(value) for value in point):
                raise ValueError(f"{path.name}: invalid vertex")
            vertices.append(point)
        elif fields[0] == "vn":
            normal = tuple(float(value) for value in fields[1:])
            if len(normal) != 3 or not all(math.isfinite(value) for value in normal):
                raise ValueError(f"{path.name}: invalid normal")
            normals.append(normal)
        elif fields[0] == "f":
            if len(fields) != 4:
                raise ValueError(f"{path.name}: face is not a triangle")
            refs = []
            for entry in fields[1:]:
                vertex_ref, normal_ref = entry.split("//")
                v_idx, n_idx = int(vertex_ref), int(normal_ref)
                if not 1 <= v_idx <= len(vertices) or not 1 <= n_idx <= len(normals):
                    raise ValueError(f"{path.name}: out-of-range face reference")
                refs.extend((v_idx, n_idx))
            if len(set(refs[::2])) < 3:
                raise ValueError(f"{path.name}: repeated face vertex")
            p, q, r = (vertices[i - 1] for i in refs[::2])
            ux, uy, uz = q[0] - p[0], q[1] - p[1], q[2] - p[2]
            vx, vy, vz = r[0] - p[0], r[1] - p[1], r[2] - p[2]
            area2 = math.sqrt((uy * vz - uz * vy) ** 2 + (uz * vx - ux * vz) ** 2
                              + (ux * vy - uy * vx) ** 2)
            if not math.isfinite(area2) or area2 <= 1e-10:
                raise ValueError(f"{path.name}: degenerate face")
            normal = normals[refs[5] - 1]
            cross = (uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx)
            if sum(cross[i] * normal[i] for i in range(3)) <= 0:
                raise ValueError(f"{path.name}: face normal disagrees with winding")
            faces.setdefault(group, []).append(tuple(refs))
    if not vertices or not normals or not any(faces.values()):
        raise ValueError(f"{path.name}: empty mesh")
    if len(normals) != sum(len(group_faces) for group_faces in faces.values()):
        raise ValueError(f"{path.name}: expected one supplied normal per triangle")
    for group_name, group_faces in faces.items():
        edge_uses: dict[tuple[int, int], list[int]] = {}
        volume6 = 0.0
        for face in group_faces:
            a, b, c = (face[i] for i in (0, 2, 4))
            p, q, r = (vertices[i - 1] for i in (a, b, c))
            volume6 += (
                p[0] * (q[1] * r[2] - q[2] * r[1])
                + p[1] * (q[2] * r[0] - q[0] * r[2])
                + p[2] * (q[0] * r[1] - q[1] * r[0])
            )
            for start, end in ((a, b), (b, c), (c, a)):
                key = (min(start, end), max(start, end))
                edge_uses.setdefault(key, []).append(1 if start < end else -1)
        bad_edge = next(
            ((edge, uses) for edge, uses in edge_uses.items()
             if len(uses) != 2 or sum(uses) != 0),
            None,
        )
        if bad_edge:
            edge, uses = bad_edge
            raise ValueError(
                f"{path.name}: {group_name} has invalid edge {edge} with uses {uses}"
            )
        if not math.isfinite(volume6) or volume6 <= 1e-10:
            raise ValueError(f"{path.name}: {group_name} does not have outward winding")


def main() -> None:
    MODELS.mkdir(parents=True, exist_ok=True)
    models: dict[str, Model] = {}
    for name in ("case-base", "case-wall", "lid", "keyboard-plate", "lamp-panel", "plugboard"):
        model = Model()
        model.add(name.replace("-", "_"), beveled_box())
        models[name] = model

    models["rotor"] = rotor_mesh()
    models["gear"] = Model()
    models["gear"].add("gear", gear_mesh())
    models["reflector"] = Model()
    models["reflector"].add("reflector", lathe("x", [
        (-0.25, 0.73), (-0.25, 0.78), (-0.23, 0.8),
        (0.23, 0.8), (0.25, 0.78), (0.25, 0.73),
    ]))
    models["shaft"] = Model()
    models["shaft"].add("shaft", cylinder("x", 1.0, 0.1))
    models["key"] = Model()
    models["key"].add("key", lathe("y", [
        (-0.125, 0.0), (-0.125, 0.46), (-0.08, 0.5),
        (0.08, 0.5), (0.125, 0.46), (0.125, 0.0),
    ]))
    models["lamp"] = Model()
    models["lamp"].add("lamp", lathe("y", [
        (-0.125, 0.0), (-0.125, 0.43), (-0.09, 0.5), (0.005, 0.5),
        (0.08, 0.42), (0.125, 0.28), (0.125, 0.0),
    ]))
    models["contact"] = Model()
    models["contact"].add("contact", lathe("y", [
        (-0.075, 0.0), (-0.075, 0.42), (-0.06, 0.5),
        (0.06, 0.5), (0.075, 0.42), (0.075, 0.0),
    ]))

    reports = []
    for name, model in models.items():
        reports.append(write_obj(name, model))
    for path, extents, vertices, triangles in reports:
        relative = path.relative_to(ROOT.parent.parent)
        print(f"{relative}: dimensions=({extents[0]:.6f}, {extents[1]:.6f}, {extents[2]:.6f}) "
              f"vertices={vertices} triangles={triangles}")
    print(f"Validated {len(reports)} OBJ files: finite values, indexed normals, triangle winding data, "
          "in-range indices, and nondegenerate faces.")


if __name__ == "__main__":
    main()
