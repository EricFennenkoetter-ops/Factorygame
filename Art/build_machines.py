import os
import sys
import random
import math

import bmesh
import bpy

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import build_harvestables as h

PREVIEW_PATH = os.path.join(h.ROOT, "machines_preview.png")
BLEND_PATH = os.path.join(h.ROOT, "machines.blend")

BELT_Z = (0.84, 0.92)
BELT_HALF_WIDTH = 0.7
RAIL_OUTER = 0.85


def add_box(bm, lo, hi, mat_idx):
    x0, y0, z0 = lo
    x1, y1, z1 = hi
    v = [bm.verts.new(p) for p in (
        (x0, y0, z0), (x1, y0, z0), (x1, y1, z0), (x0, y1, z0),
        (x0, y0, z1), (x1, y0, z1), (x1, y1, z1), (x0, y1, z1))]
    for idx in ((0, 3, 2, 1), (4, 5, 6, 7), (0, 1, 5, 4), (1, 2, 6, 5), (2, 3, 7, 6), (3, 0, 4, 7)):
        bm.faces.new([v[i] for i in idx]).material_index = mat_idx


def add_prism(bm, points, z0, z1, mat_idx):
    lower = [bm.verts.new((x, y, z0)) for x, y in points]
    upper = [bm.verts.new((x, y, z1)) for x, y in points]
    bm.faces.new(list(reversed(lower))).material_index = mat_idx
    bm.faces.new(upper).material_index = mat_idx
    n = len(points)
    for i in range(n):
        j = (i + 1) % n
        bm.faces.new((lower[i], lower[j], upper[j], upper[i])).material_index = mat_idx


def add_belt_section(bm, y0, y1, belt, metal, dark, base_z):
    add_box(bm, (-BELT_HALF_WIDTH, y0, BELT_Z[0]), (BELT_HALF_WIDTH, y1, BELT_Z[1]), belt)
    add_box(bm, (-RAIL_OUTER, y0, 0.75), (-BELT_HALF_WIDTH, y1, 1.0), metal)
    add_box(bm, (BELT_HALF_WIDTH, y0, 0.75), (RAIL_OUTER, y1, 1.0), metal)
    add_box(bm, (-BELT_HALF_WIDTH, y0, base_z), (BELT_HALF_WIDTH, y1, BELT_Z[0]), dark)


def add_chevron(bm, y, mat_idx):
    add_prism(bm, [(0.0, y + 0.25), (-0.35, y - 0.1), (0.35, y - 0.1)], BELT_Z[1], BELT_Z[1] + 0.015, mat_idx)


def add_port(bm, face_y, outward, frame, opening):
    inner = face_y + outward * 0.04
    outer = face_y + outward * 0.15
    y0, y1 = sorted((face_y, outer))
    add_box(bm, (-1.15, y0, 0.6), (-0.95, y1, 2.2), frame)
    add_box(bm, (0.95, y0, 0.6), (1.15, y1, 2.2), frame)
    add_box(bm, (-1.15, y0, 2.0), (1.15, y1, 2.2), frame)
    add_box(bm, (-1.15, y0, 0.6), (1.15, y1, 0.75), frame)
    p0, p1 = sorted((face_y, inner))
    add_box(bm, (-0.95, p0, 0.75), (0.95, p1, 2.0), opening)


def build_machine(name):
    obj = h.new_object(name, bmesh.new())
    body = h.material_index(obj, "MachineBody")
    dark = h.material_index(obj, "MachineDark")
    metal = h.material_index(obj, "MachineMetal")
    port_in = h.material_index(obj, "PortIn")
    port_out = h.material_index(obj, "PortOut")
    belt = h.material_index(obj, "Belt")
    mark = h.material_index(obj, "BeltMark")
    bm = bmesh.new()

    add_box(bm, (-3.0, -3.0, 0.0), (3.0, 3.0, 0.4), dark)
    add_box(bm, (-2.3, -2.1, 0.4), (2.3, 2.1, 3.4), body)
    add_box(bm, (-2.5, -2.3, 3.4), (2.5, 2.3, 3.65), dark)
    add_box(bm, (-1.8, -1.2, 3.65), (-0.4, 0.6, 4.1), metal)
    rng = random.Random(7)
    chimney = h.add_cone(bm, 8, 0.35, 0.35, 3.65, 4.9, metal, 0.0, rng)
    chimney += h.add_cone(bm, 8, 0.42, 0.42, 4.7, 4.95, dark, 0.0, rng)
    for v in {v for f in chimney for v in f.verts}:
        v.co.x += 1.5
        v.co.y -= 1.2

    add_port(bm, -2.1, -1.0, port_in, belt)
    add_port(bm, 2.1, 1.0, port_out, belt)
    add_belt_section(bm, -3.0, -2.1, belt, metal, dark, 0.4)
    add_belt_section(bm, 2.1, 3.0, belt, metal, dark, 0.4)

    add_box(bm, (-0.25, -1.6, 3.65), (0.25, 0.4, 3.72), mark)
    add_prism(bm, [(0.0, 1.6), (-0.75, 0.4), (0.75, 0.4)], 3.65, 3.72, mark)

    add_box(bm, (2.3, -0.8, 1.2), (2.45, 0.3, 2.2), metal)
    add_box(bm, (2.45, -0.15, 1.85), (2.52, 0.15, 2.05), mark)
    for x in (-2.3, 2.1):
        for y in (-2.1, 1.9):
            add_box(bm, (x, y, 0.4), (x + 0.2, y + 0.2, 3.4), dark)

    bm.to_mesh(obj.data)
    bm.free()
    h.finalize(obj)
    return obj


def build_conveyor(name):
    obj = h.new_object(name, bmesh.new())
    belt = h.material_index(obj, "Belt")
    metal = h.material_index(obj, "MachineMetal")
    dark = h.material_index(obj, "MachineDark")
    mark = h.material_index(obj, "BeltMark")
    bm = bmesh.new()
    add_belt_section(bm, -1.0, 1.0, belt, metal, dark, 0.7)
    for y in (-0.75, 0.65):
        for x in (-0.65, 0.53):
            add_box(bm, (x, y, 0.0), (x + 0.12, y + 0.12, 0.7), dark)
    add_chevron(bm, -0.5, mark)
    add_chevron(bm, 0.4, mark)
    bm.to_mesh(obj.data)
    bm.free()
    h.finalize(obj)
    return obj


def render_with_belts(machine, conveyor):
    scene = bpy.context.scene
    extra = []
    for i, y in enumerate((-4.0, -6.0, 4.0, 6.0)):
        copy = conveyor.copy()
        copy.data = conveyor.data
        copy.location = (0.0, y, 0.0)
        scene.collection.objects.link(copy)
        extra.append(copy)
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except TypeError:
        pass
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_object_outline = True
    scene.display.shading.show_cavity = True
    ground = bpy.data.meshes.new("Ground")
    gbm = bmesh.new()
    bmesh.ops.create_grid(gbm, x_segments=1, y_segments=1, size=12)
    gbm.to_mesh(ground)
    gbm.free()
    gobj = bpy.data.objects.new("Ground", ground)
    scene.collection.objects.link(gobj)
    gmat = bpy.data.materials.new("GroundPreview")
    gmat.diffuse_color = (0.45, 0.62, 0.32, 1.0)
    ground.materials.append(gmat)
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = 17.0
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = (16.0, -12.0, 13.0)
    cam.rotation_euler = (math.radians(58), 0.0, math.radians(53))
    scene.render.resolution_x = 1400
    scene.render.resolution_y = 1000
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)
    for o in extra + [gobj, cam]:
        bpy.data.objects.remove(o)


def main():
    h.reset_scene()
    os.makedirs(h.EXPORT_DIR, exist_ok=True)
    machine = build_machine("Machine_Constructor")
    conveyor = build_conveyor("Conveyor_Belt")
    for obj in (machine, conveyor):
        path = h.export_fbx(obj)
        d = obj.dimensions
        print("EXPORTED %s  size=%.2f x %.2f x %.2f" % (os.path.basename(path), d.x, d.y, d.z))
    render_with_belts(machine, conveyor)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("PREVIEW", PREVIEW_PATH)


if __name__ == "__main__":
    main()
