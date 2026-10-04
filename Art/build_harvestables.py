import bpy
import bmesh
import math
import os
import random
import sys
from mathutils import Vector

ROOT = os.path.dirname(os.path.abspath(__file__))
PROJECT = os.path.dirname(ROOT)
EXPORT_DIR = os.path.join(PROJECT, "Assets", "Factory", "Models")
PREVIEW_PATH = os.path.join(ROOT, "harvestables_preview.png")
BLEND_PATH = os.path.join(ROOT, "harvestables.blend")

COLORS = {
    "Bark": (0.33, 0.21, 0.12),
    "WoodCut": (0.78, 0.60, 0.38),
    "Needles": (0.13, 0.38, 0.17),
    "Leaves": (0.28, 0.55, 0.18),
    "Rock": (0.45, 0.44, 0.42),
    "RockDark": (0.32, 0.31, 0.30),
    "Moss": (0.30, 0.45, 0.20),
    "Ore": (0.80, 0.42, 0.18),
    "Oil": (0.03, 0.03, 0.04),
    "MachineBody": (0.90, 0.52, 0.12),
    "MachineDark": (0.16, 0.17, 0.19),
    "MachineMetal": (0.62, 0.64, 0.66),
    "PortIn": (0.20, 0.75, 0.30),
    "PortOut": (0.20, 0.50, 0.90),
    "Belt": (0.09, 0.09, 0.10),
    "BeltMark": (0.95, 0.80, 0.15),
}


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def get_material(name):
    mat = bpy.data.materials.get(name)
    if mat is None:
        mat = bpy.data.materials.new(name)
        mat.use_nodes = True
        bsdf = next(n for n in mat.node_tree.nodes if n.type == "BSDF_PRINCIPLED")
        r, g, b = COLORS[name]
        bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
        bsdf.inputs["Roughness"].default_value = 0.9
        mat.diffuse_color = (r, g, b, 1.0)
    return mat


def new_object(name, bm):
    mesh = bpy.data.meshes.new(name)
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.scene.collection.objects.link(obj)
    return obj


def material_index(obj, name):
    mats = obj.data.materials
    for i, m in enumerate(mats):
        if m.name == name:
            return i
    mats.append(get_material(name))
    return len(mats) - 1


def add_cone(bm, segments, r_bottom, r_top, z_bottom, z_top, mat_idx, jitter, rng, twist=0.0):
    ring_b = []
    ring_t = []
    for i in range(segments):
        a = twist + i * 2.0 * math.pi / segments
        jb = 1.0 + rng.uniform(-jitter, jitter)
        jt = 1.0 + rng.uniform(-jitter, jitter)
        ring_b.append(bm.verts.new((math.cos(a) * r_bottom * jb, math.sin(a) * r_bottom * jb, z_bottom + rng.uniform(-jitter, jitter) * 0.3)))
        if r_top > 0.0:
            ring_t.append(bm.verts.new((math.cos(a) * r_top * jt, math.sin(a) * r_top * jt, z_top)))
    faces = []
    if r_top > 0.0:
        for i in range(segments):
            j = (i + 1) % segments
            faces.append(bm.faces.new((ring_b[i], ring_b[j], ring_t[j], ring_t[i])))
        faces.append(bm.faces.new(list(reversed(ring_t))))
        faces[-1].normal_flip()
        faces[-1].normal_flip()
    else:
        tip = bm.verts.new((rng.uniform(-jitter, jitter) * 0.3, rng.uniform(-jitter, jitter) * 0.3, z_top))
        for i in range(segments):
            j = (i + 1) % segments
            faces.append(bm.faces.new((ring_b[i], ring_b[j], tip)))
    bottom = bm.faces.new(list(reversed(ring_b)))
    faces.append(bottom)
    for f in faces:
        f.material_index = mat_idx
    return faces


def add_blob(bm, center, radius, scale, subdiv, mat_idx, jitter, rng):
    result = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=radius)
    verts = result["verts"]
    for v in verts:
        n = v.co.normalized()
        d = 1.0 + rng.uniform(-jitter, jitter)
        v.co = Vector((n.x * radius * d * scale[0], n.y * radius * d * scale[1], n.z * radius * d * scale[2])) + Vector(center)
    faces = {f for v in verts for f in v.link_faces}
    for f in faces:
        f.material_index = mat_idx
    return faces


def finalize(obj):
    for p in obj.data.polygons:
        p.use_smooth = False
    bm = bmesh.new()
    bm.from_mesh(obj.data)
    bmesh.ops.recalc_face_normals(bm, faces=bm.faces[:])
    bm.to_mesh(obj.data)
    bm.free()
    obj.data.update()


def build_pine(name, seed, height, tiers, base_radius, trunk_radius):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    bark = material_index(obj, "Bark")
    needles = material_index(obj, "Needles")
    bm = bmesh.new()
    canopy_start = height * 0.22
    add_cone(bm, 7, trunk_radius, trunk_radius * 0.45, 0.0, height * 0.75, bark, 0.05, rng)
    tier_h = (height - canopy_start) / (tiers * 0.72)
    for t in range(tiers):
        frac = t / max(1, tiers - 1)
        z0 = canopy_start + t * tier_h * 0.65
        r = base_radius * (1.0 - 0.72 * frac)
        z1 = min(height, z0 + tier_h * (1.15 - 0.2 * frac))
        add_cone(bm, 8, r, 0.0, z0, z1, needles, 0.12, rng, twist=rng.uniform(0, math.pi))
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def build_leaf_tree(name, seed, trunk_height, blobs, trunk_radius):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    bark = material_index(obj, "Bark")
    leaves = material_index(obj, "Leaves")
    bm = bmesh.new()
    add_cone(bm, 7, trunk_radius, trunk_radius * 0.55, 0.0, trunk_height + 1.0, bark, 0.06, rng)
    for (cx, cy, cz, r, sx, sy, sz) in blobs:
        add_blob(bm, (cx, cy, cz), r, (sx, sy, sz), 2, leaves, 0.10, rng)
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def build_stump(name, seed):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    bark = material_index(obj, "Bark")
    cut = material_index(obj, "WoodCut")
    bm = bmesh.new()
    faces = add_cone(bm, 8, 0.62, 0.5, 0.0, 0.75, bark, 0.06, rng)
    top = max(faces, key=lambda f: f.calc_center_median().z)
    top.material_index = cut
    for i in range(3):
        a = i * 2.0 * math.pi / 3.0 + rng.uniform(-0.3, 0.3)
        base = Vector((math.cos(a) * 0.55, math.sin(a) * 0.55, 0.0))
        tip = Vector((math.cos(a) * 1.05, math.sin(a) * 1.05, 0.0))
        side = Vector((-math.sin(a), math.cos(a), 0.0)) * 0.18
        v1 = bm.verts.new(base + side)
        v2 = bm.verts.new(base - side)
        v3 = bm.verts.new(tip)
        v4 = bm.verts.new(base + Vector((0, 0, 0.35)))
        for f in (bm.faces.new((v1, v2, v4)), bm.faces.new((v2, v3, v4)), bm.faces.new((v3, v1, v4))):
            f.material_index = bark
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def build_rock(name, seed, size, subdiv, moss):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    rock = material_index(obj, "Rock")
    dark = material_index(obj, "RockDark")
    moss_idx = material_index(obj, "Moss") if moss else None
    bm = bmesh.new()
    result = bmesh.ops.create_icosphere(bm, subdivisions=subdiv, radius=1.0)
    for v in result["verts"]:
        n = v.co.normalized()
        d = 1.0 + rng.uniform(-0.28, 0.2)
        v.co = Vector((n.x * d * size[0] * 0.5, n.y * d * size[1] * 0.5, n.z * d * size[2] * 0.5 + size[2] * 0.35))
        if v.co.z < 0.0:
            v.co.z = 0.0
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.02)
    bm.normal_update()
    for f in bm.faces:
        n = f.normal
        f.material_index = rock
        if n.z < 0.15 and rng.random() < 0.45:
            f.material_index = dark
        if moss_idx is not None and n.z > 0.75 and rng.random() < 0.8:
            f.material_index = moss_idx
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def build_chunk(name, seed):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    rock = material_index(obj, "Rock")
    bm = bmesh.new()
    result = bmesh.ops.create_icosphere(bm, subdivisions=1, radius=0.28)
    for v in result["verts"]:
        v.co *= 1.0 + rng.uniform(-0.3, 0.2)
    for f in bm.faces:
        f.material_index = rock
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def add_crystal(bm, base, direction, radius, length, mat_idx, rng):
    rot = Vector((0.0, 0.0, 1.0)).rotation_difference(direction.normalized()).to_matrix()
    segments = 5
    lower = []
    upper = []
    for i in range(segments):
        a = i * 2.0 * math.pi / segments + rng.uniform(-0.2, 0.2)
        lower.append(bm.verts.new(base + rot @ Vector((math.cos(a) * radius, math.sin(a) * radius, 0.0))))
        upper.append(bm.verts.new(base + rot @ Vector((math.cos(a) * radius * 0.85, math.sin(a) * radius * 0.85, length * 0.7))))
    tip = bm.verts.new(base + rot @ Vector((0.0, 0.0, length)))
    faces = [bm.faces.new(list(reversed(lower)))]
    for i in range(segments):
        j = (i + 1) % segments
        faces.append(bm.faces.new((lower[i], lower[j], upper[j], upper[i])))
        faces.append(bm.faces.new((upper[i], upper[j], tip)))
    for f in faces:
        f.material_index = mat_idx


def build_ore_node(name, seed, size, crystal_count):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    rock = material_index(obj, "Rock")
    dark = material_index(obj, "RockDark")
    ore = material_index(obj, "Ore")
    bm = bmesh.new()
    result = bmesh.ops.create_icosphere(bm, subdivisions=2, radius=1.0)
    for v in result["verts"]:
        n = v.co.normalized()
        d = 1.0 + rng.uniform(-0.2, 0.15)
        v.co = Vector((n.x * d * size[0] * 0.5, n.y * d * size[1] * 0.5, n.z * d * size[2] * 0.5 + size[2] * 0.35))
        if v.co.z < 0.0:
            v.co.z = 0.0
    bmesh.ops.remove_doubles(bm, verts=bm.verts[:], dist=0.02)
    bm.normal_update()
    for f in bm.faces:
        f.material_index = dark if (f.normal.z < 0.15 and rng.random() < 0.5) else rock
    center = Vector((0.0, 0.0, size[2] * 0.35))
    candidates = [v.co.copy() for v in bm.verts if v.co.z > size[2] * 0.45]
    rng.shuffle(candidates)
    for p in candidates[:crystal_count]:
        out = p - center
        out.z = max(out.z, 0.2)
        direction = (out.normalized() + Vector((0.0, 0.0, 0.6))).normalized()
        base = center + (p - center) * 0.85
        add_crystal(bm, base, direction, rng.uniform(0.12, 0.2) * size[2], rng.uniform(0.45, 0.8) * size[2], ore, rng)
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def build_oil_seep(name, seed):
    rng = random.Random(seed)
    obj = new_object(name, bmesh.new())
    dark = material_index(obj, "RockDark")
    oil = material_index(obj, "Oil")
    bm = bmesh.new()
    count = 7
    for i in range(count):
        a = i * 2.0 * math.pi / count + rng.uniform(-0.25, 0.25)
        r = rng.uniform(0.35, 0.55)
        add_blob(bm, (math.cos(a) * 1.35, math.sin(a) * 1.35, r * 0.4), r, (1.0, 1.0, 0.8), 1, dark, 0.2, rng)
    for v in bm.verts:
        if v.co.z < 0.0:
            v.co.z = 0.0
    add_cone(bm, 10, 1.2, 1.2, 0.0, 0.12, oil, 0.04, rng)
    bm.to_mesh(obj.data)
    bm.free()
    finalize(obj)
    return obj


def export_fbx(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    path = os.path.join(EXPORT_DIR, obj.name + ".fbx")
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        object_types={"MESH"},
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        bake_space_transform=True,
        mesh_smooth_type="FACE",
        use_mesh_modifiers=True,
        add_leaf_bones=False,
        bake_anim=False,
    )
    return path


def render_preview(objects):
    scene = bpy.context.scene
    try:
        scene.render.engine = "BLENDER_WORKBENCH"
    except TypeError:
        pass
    scene.display.shading.light = "STUDIO"
    scene.display.shading.color_type = "MATERIAL"
    scene.display.shading.show_object_outline = True
    scene.display.shading.show_cavity = True
    world = bpy.data.worlds.new("World")
    scene.world = world
    x = 0.0
    for obj in objects:
        width = max(obj.dimensions.x, obj.dimensions.y)
        obj.location.x = x + width * 0.5
        x += width + 1.2
    ground = bpy.data.meshes.new("Ground")
    bm = bmesh.new()
    bmesh.ops.create_grid(bm, x_segments=1, y_segments=1, size=x * 0.6 + 8)
    bm.to_mesh(ground)
    bm.free()
    gobj = bpy.data.objects.new("Ground", ground)
    gobj.location = (x * 0.5, 0, 0)
    scene.collection.objects.link(gobj)
    gmat = bpy.data.materials.new("GroundPreview")
    gmat.diffuse_color = (0.45, 0.62, 0.32, 1.0)
    ground.materials.append(gmat)
    cam_data = bpy.data.cameras.new("PreviewCam")
    cam_data.type = "ORTHO"
    cam_data.ortho_scale = x + 4.0
    cam = bpy.data.objects.new("PreviewCam", cam_data)
    scene.collection.objects.link(cam)
    scene.camera = cam
    cam.location = (x * 0.5, -40.0, 18.0)
    cam.rotation_euler = (math.radians(72), 0.0, 0.0)
    scene.render.resolution_x = 1800
    scene.render.resolution_y = 700
    scene.render.filepath = PREVIEW_PATH
    bpy.ops.render.render(write_still=True)
    bpy.data.objects.remove(gobj)
    bpy.data.objects.remove(cam)
    for obj in objects:
        obj.location.x = 0.0


def main():
    reset_scene()
    os.makedirs(EXPORT_DIR, exist_ok=True)
    models = [
        build_pine("Tree_Pine_A", 11, 11.0, 4, 3.0, 0.42),
        build_pine("Tree_Pine_B", 23, 13.0, 5, 2.6, 0.38),
        build_pine("Tree_Pine_C", 37, 9.0, 3, 3.4, 0.46),
        build_leaf_tree("Tree_Leaf_A", 41, 4.2, [
            (0.0, 0.0, 6.4, 2.6, 1.0, 1.0, 0.85),
            (1.4, 0.6, 5.6, 1.9, 1.0, 1.0, 0.9),
            (-1.2, -0.8, 5.8, 1.8, 1.0, 1.0, 0.9),
        ], 0.4),
        build_leaf_tree("Tree_Leaf_B", 53, 5.0, [
            (0.0, 0.0, 7.6, 2.2, 1.0, 1.0, 1.25),
            (0.6, -0.9, 6.0, 1.7, 1.0, 1.0, 1.0),
        ], 0.36),
        build_stump("Tree_Stump", 61),
        build_rock("Rock_A", 71, (2.4, 2.0, 1.6), 2, True),
        build_rock("Rock_B", 83, (1.7, 1.5, 2.1), 2, False),
        build_rock("Rock_C", 97, (3.0, 2.3, 1.3), 2, True),
        build_chunk("Rock_Chunk", 101),
        build_ore_node("Ore_Node_A", 113, (2.6, 2.3, 1.7), 6),
        build_ore_node("Ore_Node_B", 127, (2.1, 2.4, 2.0), 8),
        build_oil_seep("Oil_Seep", 139),
    ]
    for obj in models:
        path = export_fbx(obj)
        dims = obj.dimensions
        print("EXPORTED %s  size=%.2f x %.2f x %.2f  tris=%d" % (
            os.path.basename(path), dims.x, dims.y, dims.z,
            sum(len(p.vertices) - 2 for p in obj.data.polygons)))
    render_preview(models)
    bpy.ops.wm.save_as_mainfile(filepath=BLEND_PATH)
    print("PREVIEW", PREVIEW_PATH)


if __name__ == "__main__":
    main()
