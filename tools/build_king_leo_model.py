#!/usr/bin/env python3
"""Build a bright, low-poly King Leo OBJ and a small presentation render.

The OBJ is deliberately made from simple, editable meshes so it imports cleanly
into Unity and remains easy to revise without a DCC tool.  Run this file after
changing the construction values below.
"""

from __future__ import annotations

import math
from pathlib import Path

import numpy as np
from PIL import Image, ImageDraw


ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / "Assets/JellyGarden/Models/KingLeoLowPoly"
OBJ = OUT / "KingLeoLowPoly.obj"
MTL = OUT / "KingLeoLowPoly.mtl"
PREVIEW = OUT / "KingLeoLowPoly_Preview.png"

MATERIALS = {
    "skin": (0.96, 0.56, 0.34),
    "hair": (0.25, 0.10, 0.045),
    "beard": (0.18, 0.075, 0.032),
    "blue": (0.075, 0.25, 0.78),
    "blueLight": (0.13, 0.38, 0.95),
    "red": (0.72, 0.075, 0.11),
    "redDark": (0.39, 0.025, 0.055),
    "gold": (1.0, 0.63, 0.055),
    "goldLight": (1.0, 0.82, 0.22),
    "brown": (0.26, 0.11, 0.045),
    "brownLight": (0.43, 0.20, 0.075),
    "pants": (0.10, 0.15, 0.27),
    "boot": (0.14, 0.06, 0.025),
    "white": (0.96, 0.95, 0.86),
    "black": (0.025, 0.02, 0.035),
    "ruby": (0.92, 0.035, 0.10),
    "platform": (0.16, 0.075, 0.23),
}


class Model:
    def __init__(self) -> None:
        self.vertices: list[np.ndarray] = []
        self.faces: list[tuple[str, tuple[int, ...]]] = []

    def vertex(self, point: tuple[float, float, float] | np.ndarray) -> int:
        self.vertices.append(np.asarray(point, dtype=float))
        return len(self.vertices)

    def face(self, material: str, *indices: int) -> None:
        self.faces.append((material, indices))

    def sphere(self, center, radius, material, scale=(1.0, 1.0, 1.0), rings=8, sides=12):
        """Faceted UV sphere, with the fewest rings needed for a friendly style."""
        start = len(self.vertices) + 1
        cx, cy, cz = center
        sx, sy, sz = scale
        for ring in range(rings + 1):
            phi = math.pi * ring / rings
            for side in range(sides):
                theta = math.tau * side / sides
                self.vertex((cx + radius * sx * math.sin(phi) * math.cos(theta),
                             cy + radius * sy * math.cos(phi),
                             cz + radius * sz * math.sin(phi) * math.sin(theta)))
        for ring in range(rings):
            for side in range(sides):
                a = start + ring * sides + side
                b = start + ring * sides + (side + 1) % sides
                c = start + (ring + 1) * sides + (side + 1) % sides
                d = start + (ring + 1) * sides + side
                self.face(material, a, b, c, d)

    def frustum(self, center, height, bottom_radius, top_radius, material, sides=12, caps=True):
        start = len(self.vertices) + 1
        cx, cy, cz = center
        y0, y1 = cy - height / 2, cy + height / 2
        for y, radius in ((y0, bottom_radius), (y1, top_radius)):
            for side in range(sides):
                angle = math.tau * side / sides
                self.vertex((cx + radius * math.cos(angle), y, cz + radius * math.sin(angle)))
        for side in range(sides):
            a = start + side
            b = start + (side + 1) % sides
            self.face(material, a, b, b + sides, a + sides)
        if caps:
            bottom = self.vertex((cx, y0, cz))
            top = self.vertex((cx, y1, cz))
            for side in range(sides):
                a = start + side
                b = start + (side + 1) % sides
                self.face(material, bottom, b, a)
                self.face(material, top, a + sides, b + sides)

    def capsule(self, a, b, radius, material, sides=10):
        """A cylinder between two points with spherical, low-poly ends."""
        a, b = np.asarray(a, dtype=float), np.asarray(b, dtype=float)
        axis = b - a
        length = np.linalg.norm(axis)
        w = axis / length
        guide = np.array((0.0, 1.0, 0.0)) if abs(w[1]) < .9 else np.array((1.0, 0.0, 0.0))
        u = np.cross(w, guide); u /= np.linalg.norm(u)
        v = np.cross(w, u)
        start = len(self.vertices) + 1
        for point in (a, b):
            for side in range(sides):
                angle = math.tau * side / sides
                self.vertex(point + radius * (u * math.cos(angle) + v * math.sin(angle)))
        for side in range(sides):
            n = (side + 1) % sides
            self.face(material, start + side, start + n, start + n + sides, start + side + sides)
        self.sphere(a, radius, material, scale=(1, 1, 1), rings=5, sides=sides)
        self.sphere(b, radius, material, scale=(1, 1, 1), rings=5, sides=sides)

    def prism(self, points, depth, material):
        """Extrude an x/y silhouette through z; useful for the dramatic cape."""
        start = len(self.vertices) + 1
        for z in (-depth / 2, depth / 2):
            for x, y, z_offset in points:
                self.vertex((x, y, z + z_offset))
        count = len(points)
        self.face(material, *range(start, start + count))
        self.face(material, *range(start + count, start + 2 * count))
        for i in range(count):
            a, b = start + i, start + (i + 1) % count
            self.face(material, a, b, b + count, a + count)

    def obj(self) -> str:
        lines = ["# King Leo low-poly character, generated by tools/build_king_leo_model.py", "mtllib KingLeoLowPoly.mtl", "o KingLeoLowPoly"]
        lines += ["v %.6f %.6f %.6f" % tuple(point) for point in self.vertices]
        active = None
        for material, face in self.faces:
            if material != active:
                lines.append("usemtl " + material)
                active = material
            lines.append("f " + " ".join(str(index) for index in face))
        return "\n".join(lines) + "\n"


def make_king() -> Model:
    m = Model()
    # The plinth makes the character immediately readable as a collectible model.
    m.frustum((0, .13, 0), .26, 1.64, 1.43, "platform", 16)
    m.frustum((0, .31, 0), .14, 1.40, 1.32, "gold", 16)
    m.frustum((0, .41, 0), .10, 1.24, 1.17, "goldLight", 16)

    # Boots and trousers.
    m.sphere((-.52, .80, .10), .42, "boot", scale=(1.10, .66, 1.44), rings=6)
    m.sphere((.52, .80, .10), .42, "boot", scale=(1.10, .66, 1.44), rings=6)
    m.frustum((-.42, 1.32, 0), .80, .32, .37, "pants", 10)
    m.frustum((.42, 1.32, 0), .80, .32, .37, "pants", 10)
    m.frustum((-.42, 1.66, .02), .16, .38, .38, "gold", 10)
    m.frustum((.42, 1.66, .02), .16, .38, .38, "gold", 10)

    # A broad tunic and a royal cloak that reads from front and three-quarter views.
    m.frustum((0, 2.82, 0), 2.35, 1.08, .84, "red", 12)
    m.frustum((0, 3.00, .66), .18, 1.11, 1.10, "gold", 12)
    m.frustum((0, 3.00, .69), .10, .97, .97, "brown", 12)
    cape = [(-1.28, 4.22, -.34), (-.76, 4.57, -.40), (.76, 4.57, -.40), (1.28, 4.22, -.34),
            (1.45, .74, -.24), (.78, .53, -.25), (0, .78, -.35), (-.78, .53, -.25), (-1.45, .74, -.24)]
    m.prism(cape, .18, "blue")
    lining = [(-1.06, 4.10, -.20), (-.62, 4.38, -.23), (.62, 4.38, -.23), (1.06, 4.10, -.20),
              (.96, .94, -.16), (0, 1.14, -.25), (-.96, .94, -.16)]
    m.prism(lining, .08, "blueLight")
    m.sphere((-.80, 4.25, .22), .18, "gold", scale=(1, 1, .55), rings=5)
    m.sphere((.80, 4.25, .22), .18, "gold", scale=(1, 1, .55), rings=5)
    m.capsule((-.75, 4.20, .38), (.75, 4.20, .38), .065, "gold", 8)

    # Arms; the left hand holds a scepter, giving the silhouette a clear story beat.
    m.capsule((-1.00, 3.85, .16), (-1.22, 2.65, .45), .25, "blue", 10)
    m.sphere((-1.25, 2.46, .50), .27, "skin", scale=(.9, 1.0, .8), rings=6)
    m.capsule((1.00, 3.85, .16), (1.22, 2.88, .45), .25, "blue", 10)
    m.sphere((1.24, 2.72, .50), .27, "skin", scale=(.9, 1.0, .8), rings=6)
    m.capsule((-1.34, .70, .42), (-1.28, 4.55, .42), .065, "brownLight", 8)
    m.sphere((-1.28, 4.70, .42), .22, "gold", rings=6)
    m.sphere((-1.28, 4.70, .42), .12, "ruby", rings=6)

    # Neck, face, hair, and a beard built from a faceted cone.
    m.frustum((0, 4.56, .08), .30, .34, .34, "skin", 10)
    m.sphere((0, 5.20, .09), .78, "hair", scale=(1.03, 1.08, .90), rings=8)
    m.sphere((0, 5.19, .48), .72, "skin", scale=(.92, 1.08, .64), rings=8)
    m.frustum((0, 4.77, .92), .62, .52, .16, "beard", 9)
    m.sphere((-.65, 5.19, .38), .20, "skin", scale=(.72, 1.1, .6), rings=5)
    m.sphere((.65, 5.19, .38), .20, "skin", scale=(.72, 1.1, .6), rings=5)
    # Eyes, brows and nose sit ahead of the facial surface.
    for x in (-.25, .25):
        m.sphere((x, 5.35, 1.00), .105, "white", scale=(1, 1, .52), rings=5)
        m.sphere((x + (.025 if x < 0 else -.025), 5.34, 1.075), .052, "black", scale=(1, 1, .32), rings=5)
        m.capsule((x - .12, 5.57, 1.03), (x + .12, 5.60, 1.03), .035, "hair", 6)
    m.sphere((0, 5.11, 1.12), .13, "skin", scale=(.80, 1.25, .68), rings=5)

    # Crown band and five alternating spires; a central ruby gives it a game-ready focal point.
    m.frustum((0, 5.84, .10), .19, .83, .78, "gold", 12)
    for x, height in ((-.55, .37), (-.28, .56), (0, .72), (.28, .56), (.55, .37)):
        m.frustum((x, 5.96 + height / 2, .10), height, .13, .015, "gold", 6)
    m.sphere((0, 5.86, .84), .135, "ruby", scale=(.75, 1, .38), rings=6)
    return m


def render(model: Model) -> None:
    """Tiny painter's-algorithm renderer for an honest preview of the OBJ geometry."""
    size, scale = 900, 1.24
    background = Image.new("RGB", (size, size), (29, 19, 47))
    draw = ImageDraw.Draw(background)
    # Soft royal-violet studio backdrop and floor shadow.
    for y in range(size):
        shade = int(20 + 40 * (y / size))
        draw.line((0, y, size, y), fill=(28 + shade // 4, 15 + shade // 9, 48 + shade // 3))
    draw.ellipse((200, 748, 700, 840), fill=(14, 8, 23))
    camera = np.array((8.2, 6.9, 13.0))
    target = np.array((0.0, 3.35, 0.0))
    forward = target - camera; forward /= np.linalg.norm(forward)
    right = np.cross(forward, np.array((0.0, 1.0, 0.0))); right /= np.linalg.norm(right)
    up = np.cross(right, forward)
    light = np.array((-.45, .82, .60)); light /= np.linalg.norm(light)
    projected = []
    for material, face in model.faces:
        vertices = np.array([model.vertices[i - 1] for i in face])
        normal = np.cross(vertices[1] - vertices[0], vertices[2] - vertices[0])
        length = np.linalg.norm(normal)
        if length < 1e-7:
            continue
        normal /= length
        center = vertices.mean(axis=0)
        depth = float(np.dot(center - camera, forward))
        if depth <= .2:
            continue
        coords = []
        for vertex in vertices:
            delta = vertex - camera
            z = np.dot(delta, forward)
            coords.append((size / 2 + scale * size * np.dot(delta, right) / z,
                           size * .57 - scale * size * np.dot(delta, up) / z))
        diffuse = max(.22, float(np.dot(normal, light)) * .72 + .32)
        rgb = tuple(min(255, int(channel * 255 * diffuse)) for channel in MATERIALS[material])
        projected.append((depth, coords, rgb))
    for _, coords, rgb in sorted(projected, reverse=True):
        draw.polygon(coords, fill=rgb)
    background.resize((700, 700), Image.Resampling.LANCZOS).save(PREVIEW)


def write_materials() -> None:
    chunks = ["# Materials for King Leo low-poly model"]
    for name, color in MATERIALS.items():
        chunks.extend((f"newmtl {name}", "Kd %.4f %.4f %.4f" % color, "Ka %.4f %.4f %.4f" % tuple(c * .25 for c in color), "Ks 0.1200 0.1200 0.1200", "Ns 32.0000", ""))
    MTL.write_text("\n".join(chunks), encoding="utf-8")


def main() -> None:
    OUT.mkdir(parents=True, exist_ok=True)
    model = make_king()
    OBJ.write_text(model.obj(), encoding="utf-8")
    write_materials()
    render(model)
    print(f"wrote {OBJ.relative_to(ROOT)} ({len(model.vertices)} vertices, {len(model.faces)} faces)")
    print(f"wrote {PREVIEW.relative_to(ROOT)}")


if __name__ == "__main__":
    main()
