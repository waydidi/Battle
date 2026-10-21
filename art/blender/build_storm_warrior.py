"""
Builds "Storm", the stylized male Sunspire Warrior, entirely from code:
modelled from primitives, rigged with a Unity-Humanoid-friendly skeleton,
animated (Idle, Walk, Run, Attack, Dodge, Sit, Sleep) and exported for Unity (FBX) and the web (GLB).

Original placeholder art for Kingdoms of the Eight Banners - no third-party assets.

Run (Blender 4.x, no GUI needed):
    blender -b --factory-startup -P art/blender/build_storm_warrior.py -- --out <output dir> [--render]

Outputs in <output dir>:
    Storm.blend   editable source
    Storm.fbx     for Unity  (client-unity/Assets/Art/Characters/Storm/)
    Storm.glb     for the web prototype / glTF tools
    renders/*.png turnaround + pose renders (with --render)

Conventions: metres, character faces -Y (Blender "front"), +Z up, T-pose rest.
The character's left side is +X.
"""

import math
import os
import random
import sys

import bmesh
import bpy
from mathutils import Euler, Matrix, Quaternion, Vector

# --------------------------------------------------------------------------- args

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
OUT_DIR = os.path.abspath(argv[argv.index("--out") + 1]) if "--out" in argv else os.path.abspath("storm_out")
DO_RENDER = "--render" in argv
os.makedirs(OUT_DIR, exist_ok=True)

random.seed(8)

# --------------------------------------------------------------------------- scene reset

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.render.fps = 30
bpy.context.preferences.filepaths.save_version = 0  # no .blend1 backups

# --------------------------------------------------------------------------- materials

def material(name, color, metallic=0.0, roughness=0.6, emission=None, strength=0.0):
    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes["Principled BSDF"]
    bsdf.inputs["Base Color"].default_value = (*color, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    if emission:
        bsdf.inputs["Emission Color"].default_value = (*emission, 1.0)
        bsdf.inputs["Emission Strength"].default_value = strength
    mat.diffuse_color = (*color, 1.0)
    return mat


def srgb(hex_color):
    """'#rrggbb' -> linear RGB tuple (Blender colour inputs are linear)."""
    h = hex_color.lstrip("#")
    def lin(c):
        c = int(c, 16) / 255.0
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return tuple(lin(h[i:i + 2]) for i in (0, 2, 4))


M = {
    "skin": material("Skin", srgb("#e0a982"), roughness=0.55),
    "hair": material("Hair", srgb("#1d1714"), roughness=0.45),
    "eye": material("Eye", srgb("#2a1a12"), roughness=0.2),
    "eye_white": material("EyeWhite", srgb("#efe9e2"), roughness=0.3),
    "leather": material("Leather", srgb("#3a2a22"), roughness=0.7),
    "cloth": material("ClothDark", srgb("#2b2526"), roughness=0.85),
    "tunic": material("TunicRed", srgb("#6e1c1a"), roughness=0.8),
    "scarf": material("ScarfRed", srgb("#b3221d"), roughness=0.75),
    "bronze": material("Bronze", srgb("#8a5a2b"), metallic=0.9, roughness=0.35),
    "gold": material("Gold", srgb("#d9a441"), metallic=1.0, roughness=0.25),
    "steel": material("Steel", srgb("#c9ccd2"), metallic=1.0, roughness=0.18),
    "sunfire": material("SunfireEdge", srgb("#ff8a2a"), roughness=0.4, emission=srgb("#ff7a1a"), strength=6.0),
    "boots": material("Boots", srgb("#4a3226"), roughness=0.65),
}

# --------------------------------------------------------------------------- mesh helpers

PARTS = []  # (object, bone name or callable that assigns weights)


def finish(obj, mat, bone, smooth=True, subdiv=0):
    if subdiv:
        mod = obj.modifiers.new("Subdivision", "SUBSURF")
        mod.levels = subdiv
        mod.render_levels = subdiv
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.modifier_apply(modifier=mod.name)
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    for poly in obj.data.polygons:
        poly.use_smooth = smooth
    PARTS.append((obj, bone))
    return obj


def apply_transform(obj):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)


def sphere(loc, radius, scale=(1, 1, 1), mat=None, bone="Chest", segments=24, rings=16, rot=(0, 0, 0), smooth=True):
    bpy.ops.mesh.primitive_uv_sphere_add(segments=segments, ring_count=rings, radius=radius, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.scale = scale
    apply_transform(obj)
    return finish(obj, mat, bone, smooth)


def box(loc, size, mat, bone, rot=(0, 0, 0), subdiv=0, smooth=True, bevel=0.0, bevel_segments=2):
    bpy.ops.mesh.primitive_cube_add(size=1.0, location=loc, rotation=rot)
    obj = bpy.context.active_object
    obj.scale = size
    apply_transform(obj)
    if bevel:
        mod = obj.modifiers.new("Bevel", "BEVEL")
        mod.width = bevel
        mod.segments = bevel_segments
        bpy.ops.object.modifier_apply(modifier=mod.name)
    return finish(obj, mat, bone, smooth, subdiv)


def cylinder_between(p0, p1, radius, mat, bone, verts=20, radius_end=None, smooth=True):
    p0, p1 = Vector(p0), Vector(p1)
    direction = p1 - p0
    bpy.ops.mesh.primitive_cone_add(vertices=verts, radius1=radius, radius2=radius if radius_end is None else radius_end,
                                    depth=direction.length, location=(p0 + p1) / 2)
    obj = bpy.context.active_object
    obj.rotation_mode = "QUATERNION"
    obj.rotation_quaternion = Vector((0, 0, 1)).rotation_difference(direction.normalized())
    apply_transform(obj)
    return finish(obj, mat, bone, smooth)


def torus(loc, major, minor, mat, bone, rot=(0, 0, 0), scale=(1, 1, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, location=loc, rotation=rot,
                                     major_segments=32, minor_segments=10)
    obj = bpy.context.active_object
    obj.scale = scale
    apply_transform(obj)
    return finish(obj, mat, bone)


def squash_x(obj, factor, center_x):
    """Flattens a part along X around its own centre (obj.scale would scale around the world origin)."""
    for v in obj.data.vertices:
        v.co.x = center_x + (v.co.x - center_x) * factor


def cone_towards(base, direction, radius, length, mat, bone, verts=10):
    direction = Vector(direction).normalized()
    tip = Vector(base) + direction * length
    return cylinder_between(base, tip, radius, mat, bone, verts=verts, radius_end=0.0)


# --------------------------------------------------------------------------- the body

SHOULDER_Z = 1.45
HAND_X = 0.76

def side_parts(sign, suffix):
    """Arms and legs for one side. sign=+1 is the character's left (+X)."""
    s = sign
    # Arm (T-pose along X).
    cylinder_between((s * 0.19, 0, SHOULDER_Z), (s * 0.47, 0, SHOULDER_Z), 0.05, M["tunic"], "UpperArm" + suffix,
                     radius_end=0.043)
    sphere((s * 0.47, 0, SHOULDER_Z), 0.043, mat=M["skin"], bone="LowerArm" + suffix)
    cylinder_between((s * 0.47, 0, SHOULDER_Z), (s * 0.70, 0, SHOULDER_Z), 0.041, M["skin"], "LowerArm" + suffix,
                     radius_end=0.034)
    cylinder_between((s * 0.55, 0, SHOULDER_Z), (s * 0.70, 0, SHOULDER_Z), 0.048, M["bronze"], "LowerArm" + suffix,
                     radius_end=0.042)
    torus((s * 0.555, 0, SHOULDER_Z), 0.048, 0.008, M["gold"], "LowerArm" + suffix, rot=(0, math.pi / 2, 0))
    box((s * HAND_X, -0.005, SHOULDER_Z), (0.075, 0.06, 0.07), M["leather"], "Hand" + suffix, subdiv=2)
    box((s * (HAND_X - 0.01), -0.045, SHOULDER_Z + 0.005), (0.03, 0.035, 0.025), M["leather"], "Hand" + suffix, subdiv=1)

    # Leg.
    x = s * 0.1
    cylinder_between((x, 0, 0.95), (x, 0, 0.50), 0.078, M["cloth"], "UpperLeg" + suffix, radius_end=0.06)
    sphere((x, -0.035, 0.50), 0.058, scale=(1, 0.75, 1.0), mat=M["bronze"], bone="LowerLeg" + suffix)
    cylinder_between((x, 0, 0.50), (x, 0, 0.30), 0.057, M["cloth"], "LowerLeg" + suffix, radius_end=0.05)
    cylinder_between((x, 0, 0.36), (x, 0, 0.07), 0.065, M["boots"], "LowerLeg" + suffix, radius_end=0.058)
    torus((x, 0, 0.36), 0.066, 0.013, M["bronze"], "LowerLeg" + suffix)
    box((x, -0.045, 0.045), (0.1, 0.22, 0.09), M["boots"], "Foot" + suffix, subdiv=2)
    box((x, -0.16, 0.035), (0.09, 0.07, 0.07), M["boots"], "Toes" + suffix, subdiv=2)

    # Shoulder armour: the left pauldron is bigger and layered (like the reference).
    if s > 0:
        sphere((0.23, 0.0, SHOULDER_Z + 0.03), 0.12, scale=(1.0, 1.05, 0.72), mat=M["bronze"], bone="UpperArm" + suffix)
        sphere((0.26, 0.0, SHOULDER_Z - 0.03), 0.105, scale=(1.0, 1.0, 0.55), mat=M["leather"], bone="UpperArm" + suffix)
        torus((0.23, 0.0, SHOULDER_Z + 0.0), 0.115, 0.012, M["gold"], "UpperArm" + suffix, rot=(0, math.radians(-20), 0),
              scale=(1, 1.05, 1))
    else:
        sphere((-0.215, 0.0, SHOULDER_Z + 0.02), 0.09, scale=(1.0, 1.0, 0.7), mat=M["leather"], bone="UpperArm" + suffix)


def build_body():
    # Torso.
    box((0, 0.005, 1.31), (0.38, 0.22, 0.34), M["tunic"], "Chest", bevel=0.08, bevel_segments=4)
    box((0, -0.03, 1.33), (0.35, 0.19, 0.28), M["bronze"], "Chest", bevel=0.07, bevel_segments=4)   # breastplate
    box((0, -0.125, 1.36), (0.05, 0.012, 0.05), M["gold"], "Chest", rot=(0, math.radians(45), 0), bevel=0.004)  # emblem
    for sx in (-1, 1):
        box((sx * 0.07, -0.122, 1.28), (0.13, 0.012, 0.012), M["gold"], "Chest", rot=(0, 0, 0))     # trim lines
    box((0, 0.005, 1.13), (0.3, 0.19, 0.26), M["tunic"], "Spine", bevel=0.07, bevel_segments=4)
    box((0, 0, 0.96), (0.32, 0.21, 0.2), M["cloth"], "Hips", bevel=0.07, bevel_segments=4)

    # Belt, buckle and tassets.
    cylinder_between((0, 0, 0.985), (0, 0, 1.045), 0.165, M["leather"], "Hips", verts=32)
    for obj, _ in PARTS[-1:]:
        obj.scale = (1, 0.72, 1)
        apply_transform(obj)
    box((0, -0.122, 1.015), (0.07, 0.02, 0.055), M["gold"], "Hips", bevel=0.005)
    for sx in (-1, 1):
        box((sx * 0.085, -0.105, 0.9), (0.1, 0.014, 0.15), M["bronze"], "Hips", rot=(math.radians(8), 0, sx * math.radians(-10)), bevel=0.006)
        box((sx * 0.16, 0.0, 0.9), (0.014, 0.12, 0.15), M["bronze"], "Hips", rot=(0, sx * math.radians(-8), 0), bevel=0.006)
    box((0, -0.11, 0.86), (0.08, 0.012, 0.2), M["tunic"], "Hips", rot=(math.radians(6), 0, 0))         # front tabard

    # Neck and head.
    cylinder_between((0, 0, 1.47), (0, 0, 1.6), 0.052, M["skin"], "Neck")
    sphere((0, 0.0, 1.68), 0.112, scale=(0.95, 1.0, 1.12), mat=M["skin"], bone="Head")
    sphere((0, -0.02, 1.625), 0.085, scale=(0.95, 1.0, 0.85), mat=M["skin"], bone="Head")             # jaw
    for sx in (-1, 1):
        sphere((sx * 0.039, -0.098, 1.69), 0.017, scale=(1.3, 0.6, 0.9), mat=M["eye_white"], bone="Head")
        sphere((sx * 0.039, -0.106, 1.69), 0.0095, mat=M["eye"], bone="Head")
        box((sx * 0.042, -0.106, 1.718), (0.04, 0.008, 0.009), M["hair"], "Head", rot=(0, sx * math.radians(-12), 0))
        sphere((sx * 0.108, 0.0, 1.675), 0.026, scale=(0.4, 0.7, 1.0), mat=M["skin"], bone="Head")    # ears
    cone_towards((0, -0.103, 1.675), (0, -1, -0.35), 0.014, 0.03, M["skin"], "Head")                   # nose
    box((0, -0.096, 1.632), (0.035, 0.006, 0.005), M["eye"], "Head")                                   # mouth

    # Hair: a cap plus spikes, swept back with a few bangs over the forehead.
    sphere((0, 0.018, 1.725), 0.122, scale=(1.0, 1.05, 0.82), mat=M["hair"], bone="Head")
    spikes = [
        # (direction, base offset, radius, length)
        ((0.0, 0.6, 1.0), (0, 0.02, 1.80), 0.05, 0.14),
        ((0.5, 0.5, 0.8), (0.05, 0.03, 1.79), 0.045, 0.13),
        ((-0.5, 0.5, 0.8), (-0.05, 0.03, 1.79), 0.045, 0.13),
        ((0.0, 1.0, 0.3), (0, 0.08, 1.76), 0.05, 0.15),
        ((0.6, 1.0, 0.1), (0.06, 0.07, 1.74), 0.045, 0.13),
        ((-0.6, 1.0, 0.1), (-0.06, 0.07, 1.74), 0.045, 0.13),
        ((0.0, 1.0, -0.4), (0, 0.1, 1.68), 0.045, 0.12),
        ((0.9, 0.4, 0.2), (0.09, 0.02, 1.74), 0.04, 0.1),
        ((-0.9, 0.4, 0.2), (-0.09, 0.02, 1.74), 0.04, 0.1),
        ((0.3, -1.0, -0.9), (0.03, -0.08, 1.77), 0.035, 0.1),      # bangs
        ((-0.4, -1.0, -0.8), (-0.035, -0.08, 1.775), 0.035, 0.11),
        ((0.0, -1.0, -0.6), (0.0, -0.07, 1.785), 0.03, 0.09),
        ((0.9, -0.3, -0.7), (0.085, -0.05, 1.73), 0.03, 0.09),      # sideburns
        ((-0.9, -0.3, -0.7), (-0.085, -0.05, 1.73), 0.03, 0.09),
    ]
    for direction, base, radius, length in spikes:
        cone_towards(base, direction, radius, length, M["hair"], "Head")

    # Scarf around the neck.
    torus((0, 0.0, 1.49), 0.085, 0.035, M["scarf"], "Chest", scale=(1.0, 0.9, 1.0))
    box((0.05, -0.08, 1.42), (0.06, 0.02, 0.12), M["scarf"], "Chest", rot=(math.radians(10), 0, math.radians(15)), subdiv=1)

    side_parts(+1, ".L")
    side_parts(-1, ".R")
    build_cape()
    build_sword()


def build_cape():
    """A tattered red cape hanging from the shoulders; weights blend from Chest (top) to Cape (bottom)."""
    cols, rows = 12, 16
    top_z, bottom_z = 1.47, 0.42
    mesh = bpy.data.meshes.new("Cape")
    bm = bmesh.new()
    grid = []
    for r in range(rows + 1):
        t = r / rows
        z = top_z + (bottom_z - top_z) * t
        half_width = 0.19 + 0.17 * t
        row = []
        for c in range(cols + 1):
            u = c / cols * 2 - 1
            x = u * half_width
            y = 0.125 + 0.07 * t + 0.07 * (u * u) * (1 - t * 0.5) - 0.05 * (1 - u * u) * (1 - t)
            zz = z
            if r == rows:
                zz += random.uniform(-0.06, 0.05)   # ragged hem
            row.append(bm.verts.new((x, y, zz)))
        grid.append(row)
    for r in range(rows):
        for c in range(cols):
            bm.faces.new((grid[r][c], grid[r][c + 1], grid[r + 1][c + 1], grid[r + 1][c]))
    bm.to_mesh(mesh)
    bm.free()
    obj = bpy.data.objects.new("Cape", mesh)
    bpy.context.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    solid = obj.modifiers.new("Solidify", "SOLIDIFY")
    solid.thickness = 0.012
    bpy.ops.object.modifier_apply(modifier=solid.name)
    obj.data.materials.append(M["scarf"])
    for poly in obj.data.polygons:
        poly.use_smooth = True

    chest = obj.vertex_groups.new(name="Chest")
    cape = obj.vertex_groups.new(name="Cape")
    for v in obj.data.vertices:
        t = max(0.0, min(1.0, (top_z - v.co.z) / (top_z - bottom_z)))
        w = min(1.0, t * 1.4)
        if w < 1.0:
            chest.add([v.index], 1.0 - w, "REPLACE")
        if w > 0.0:
            cape.add([v.index], w, "REPLACE")
    PARTS.append((obj, None))


def build_sword():
    """Sword held in the right hand. In rest pose the blade points forward (-Y) out of the fist."""
    hx, hz = -HAND_X, SHOULDER_Z
    bone = "Hand.R"
    cylinder_between((hx, 0.07, hz), (hx, -0.06, hz), 0.017, M["leather"], bone, verts=12)          # grip
    sphere((hx, 0.08, hz), 0.026, mat=M["gold"], bone=bone)                                           # pommel
    box((hx, -0.075, hz), (0.03, 0.03, 0.2), M["gold"], bone, bevel=0.008)                            # cross-guard
    sphere((hx, -0.075, hz), 0.026, scale=(1, 0.8, 1), mat=M["scarf"], bone=bone)                     # guard gem
    # Blade: a flat, slightly tapered bar with a pointed tip and a glowing "sunfire" fuller.
    blade_len = 0.82
    cylinder_between((hx, -0.09, hz), (hx, -0.09 - blade_len, hz), 0.05, M["steel"], bone, verts=4, radius_end=0.042)
    blade = PARTS[-1][0]
    squash_x(blade, 0.28, hx)
    for poly in blade.data.polygons:
        poly.use_smooth = False
    tip = cone_towards((hx, -0.09 - blade_len, hz), (0, -1, 0), 0.042, 0.12, M["steel"], bone, verts=4)
    squash_x(tip, 0.28, hx)
    for poly in tip.data.polygons:
        poly.use_smooth = False
    box((hx, -0.1 - blade_len * 0.45, hz), (0.03, blade_len * 0.75, 0.014), M["sunfire"], bone)


# --------------------------------------------------------------------------- armature

BONES = [
    # name, head, tail, parent
    ("Hips", (0, 0, 0.98), (0, 0, 1.06), None),
    ("Spine", (0, 0, 1.06), (0, 0, 1.22), "Hips"),
    ("Chest", (0, 0, 1.22), (0, 0, 1.45), "Spine"),
    ("Neck", (0, 0, 1.48), (0, 0, 1.57), "Chest"),
    ("Head", (0, 0, 1.57), (0, 0, 1.82), "Neck"),
    ("Cape", (0, 0.15, 1.45), (0, 0.22, 0.6), "Chest"),
]
for suffix, s in ((".L", 1), (".R", -1)):
    BONES += [
        ("Shoulder" + suffix, (s * 0.05, 0, SHOULDER_Z), (s * 0.18, 0, SHOULDER_Z), "Chest"),
        ("UpperArm" + suffix, (s * 0.19, 0, SHOULDER_Z), (s * 0.47, 0, SHOULDER_Z), "Shoulder" + suffix),
        ("LowerArm" + suffix, (s * 0.47, 0, SHOULDER_Z), (s * 0.71, 0, SHOULDER_Z), "UpperArm" + suffix),
        ("Hand" + suffix, (s * 0.71, 0, SHOULDER_Z), (s * 0.81, 0, SHOULDER_Z), "LowerArm" + suffix),
        ("UpperLeg" + suffix, (s * 0.1, 0, 0.94), (s * 0.1, 0, 0.5), "Hips"),
        ("LowerLeg" + suffix, (s * 0.1, 0, 0.5), (s * 0.1, 0, 0.09), "UpperLeg" + suffix),
        ("Foot" + suffix, (s * 0.1, 0, 0.09), (s * 0.1, -0.12, 0.03), "LowerLeg" + suffix),
        ("Toes" + suffix, (s * 0.1, -0.12, 0.03), (s * 0.1, -0.2, 0.03), "Foot" + suffix),
    ]


def build_armature():
    arm_data = bpy.data.armatures.new("StormRig")
    rig = bpy.data.objects.new("Storm", arm_data)
    bpy.context.collection.objects.link(rig)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.object.mode_set(mode="EDIT")
    for name, head, tail, parent in BONES:
        bone = arm_data.edit_bones.new(name)
        bone.head = head
        bone.tail = tail
        bone.roll = 0.0
        if parent:
            bone.parent = arm_data.edit_bones[parent]
    bpy.ops.object.mode_set(mode="OBJECT")
    return rig


def skin(rig):
    for obj, bone in PARTS:
        if bone is not None:
            group = obj.vertex_groups.new(name=bone)
            group.add([v.index for v in obj.data.vertices], 1.0, "REPLACE")
    bpy.ops.object.select_all(action="DESELECT")
    for obj, _ in PARTS:
        obj.select_set(True)
    body = PARTS[0][0]
    bpy.context.view_layer.objects.active = body
    bpy.ops.object.join()
    body.name = "StormBody"
    body.data.name = "StormBody"
    body.parent = rig
    mod = body.modifiers.new("Armature", "ARMATURE")
    mod.object = rig
    return body


# --------------------------------------------------------------------------- animation

def world_rot(rig, bone_name, *rotations):
    """
    Quaternion for a pose bone from rotations given in armature (rest) axes.
    rotations: (axis, degrees) pairs applied in order. Character axes: forward -Y, left +X, up +Z.
    """
    q = Quaternion()
    for axis, degrees in rotations:
        q = Quaternion(Vector(axis), math.radians(degrees)) @ q
    rest = rig.data.bones[bone_name].matrix_local.to_quaternion()
    return rest.inverted() @ q @ rest


X, Y, Z = (1, 0, 0), (0, 1, 0), (0, 0, 1)


def arms_down_pose(lower=72, elbow=18):
    """Relaxed arms (rotations shared by several clips). Left arm points +X, right arm -X in rest."""
    return {
        "UpperArm.L": [(Y, lower), (X, -6)],
        "UpperArm.R": [(Y, -lower), (X, -6)],
        "LowerArm.L": [(Z, -elbow)],
        "LowerArm.R": [(Z, elbow)],
        "Hand.R": [(X, 30)],
    }


def key_pose(rig, frame, pose, hips_offset=(0, 0, 0)):
    for pb in rig.pose.bones:
        pb.rotation_mode = "QUATERNION"
        rotations = pose.get(pb.name, [])
        pb.rotation_quaternion = world_rot(rig, pb.name, *rotations) if rotations else Quaternion()
        pb.keyframe_insert("rotation_quaternion", frame=frame)
    hips = rig.pose.bones["Hips"]
    # Hips bone points +Z, so its local Y is world up and local Z is world -Y... use the rest matrix to convert.
    rest = rig.data.bones["Hips"].matrix_local.to_3x3()
    hips.location = rest.inverted() @ Vector(hips_offset)
    hips.keyframe_insert("location", frame=frame)


def merged(*poses):
    out = {}
    for pose in poses:
        for bone, rotations in pose.items():
            out[bone] = out.get(bone, []) + rotations
    return out


def make_action(rig, name, keys, cyclic):
    action = bpy.data.actions.new(name)
    rig.animation_data_create()
    rig.animation_data.action = action
    for frame, pose, hips in keys:
        key_pose(rig, frame, pose, hips)
    for fcurve in action.fcurves:
        for point in fcurve.keyframe_points:
            point.interpolation = "BEZIER"
        if cyclic:
            fcurve.modifiers.new("CYCLES")
    action.use_fake_user = True
    track = rig.animation_data.nla_tracks.new()
    track.name = name
    track.strips.new(name, int(keys[0][0]), action)
    rig.animation_data.action = None
    return action


def build_animations(rig):
    relaxed = arms_down_pose()

    # Idle: 2 s breathing loop.
    idle = []
    for frame, breath in ((1, 0.0), (31, 1.0), (61, 0.0)):
        idle.append((frame, merged(relaxed, {
            "Spine": [(X, 2 + 1.5 * breath)],
            "Chest": [(X, -1.5 * breath)],
            "Head": [(X, -1 * breath)],
            "UpperArm.L": [(Y, -2 * breath)],
            "UpperArm.R": [(Y, 2 * breath)],
            "Cape": [(X, 3 * breath)],
        }), (0, 0, -0.008 * breath)))
    make_action(rig, "Idle", idle, cyclic=True)

    # Run: 20-frame loop (0.67 s). Legs and arms swing in opposition.
    run = []
    for frame, phase in ((1, 0.0), (6, 0.25), (11, 0.5), (16, 0.75), (21, 1.0)):
        s = math.sin(phase * 2 * math.pi)
        c = math.cos(phase * 2 * math.pi)
        bob = 0.03 * abs(c)
        run.append((frame, merged(arms_down_pose(lower=70, elbow=70), {
            "Hips": [(Z, 8 * s)],
            "Spine": [(X, 12), (Z, -6 * s)],
            "Head": [(X, -8)],
            "UpperLeg.L": [(X, -40 * s)],
            "UpperLeg.R": [(X, 40 * s)],
            # Knee bends most while that leg swings back.
            "LowerLeg.L": [(X, 15 + 60 * max(0.0, -s))],
            "LowerLeg.R": [(X, 15 + 60 * max(0.0, s))],
            "Foot.L": [(X, -10 * s)],
            "Foot.R": [(X, 10 * s)],
            "UpperArm.L": [(X, 35 * s)],
            "UpperArm.R": [(X, -35 * s)],
            "Cape": [(X, 28 + 6 * abs(c))],
        }), (0, 0, bob - 0.04)))
    make_action(rig, "Run", run, cyclic=True)

    # Walk: 32-frame loop (~1.07 s), relaxed swing.
    walk = []
    for frame, phase in ((1, 0.0), (9, 0.25), (17, 0.5), (25, 0.75), (33, 1.0)):
        s = math.sin(phase * 2 * math.pi)
        c = math.cos(phase * 2 * math.pi)
        walk.append((frame, merged(arms_down_pose(lower=74, elbow=22), {
            "Hips": [(Z, 5 * s)],
            "Spine": [(X, 3), (Z, -3 * s)],
            "UpperLeg.L": [(X, -24 * s)],
            "UpperLeg.R": [(X, 24 * s)],
            "LowerLeg.L": [(X, 8 + 30 * max(0.0, -s))],
            "LowerLeg.R": [(X, 8 + 30 * max(0.0, s))],
            "Foot.L": [(X, -6 * s)],
            "Foot.R": [(X, 6 * s)],
            "UpperArm.L": [(X, 18 * s)],
            "UpperArm.R": [(X, -18 * s)],
            "Cape": [(X, 8 + 3 * abs(c))],
        }), (0, 0, 0.012 * abs(c) - 0.012)))
    make_action(rig, "Walk", walk, cyclic=True)

    # Sit: sitting on the ground, knees up, forearms resting on the knees; slow breathing loop.
    def sit_pose(breath):
        return merged(arms_down_pose(lower=70, elbow=55), {
            "Spine": [(X, 12 + 2 * breath)],
            "Head": [(X, -6 - 2 * breath)],
            "UpperLeg.L": [(X, -75), (Y, 12)],
            "UpperLeg.R": [(X, -75), (Y, -12)],
            "LowerLeg.L": [(X, 105)],
            "LowerLeg.R": [(X, 105)],
            "Foot.L": [(X, -25)],
            "Foot.R": [(X, -25)],
            "UpperArm.L": [(X, -38)],
            "UpperArm.R": [(X, -38)],
            "Hand.R": [(X, 45)],          # rest the blade towards the ground
            "Cape": [(X, 75)],            # drape the cape behind on the ground
        })
    make_action(rig, "Sit", [
        (1, sit_pose(0.0), (0, 0.05, -0.8)),
        (46, sit_pose(1.0), (0, 0.05, -0.8)),
        (91, sit_pose(0.0), (0, 0.05, -0.8)),
    ], cyclic=True)

    # Sleep: lying on the back, arms by the sides, chest rising and falling.
    def sleep_pose(breath):
        return merged(arms_down_pose(lower=80, elbow=8), {
            "Hips": [(X, -90)],
            "Spine": [(X, -1.5 * breath)],
            "Chest": [(X, -1.5 * breath)],
            "Head": [(X, -12), (Y, 10)],
            "UpperLeg.L": [(X, -4), (Y, 4)],
            "UpperLeg.R": [(X, -12), (Y, -4)],
            "LowerLeg.R": [(X, 14)],
            "Foot.L": [(X, 35)],
            "Foot.R": [(X, 35)],
            "Hand.R": [(X, 60)],          # blade lies along the body instead of pointing up
            "Cape": [(X, 8)],
        })
    make_action(rig, "Sleep", [
        (1, sleep_pose(0.0), (0, 0, -0.84)),
        (61, sleep_pose(1.0), (0, 0, -0.84)),
        (121, sleep_pose(0.0), (0, 0, -0.84)),
    ], cyclic=True)

    # Attack: Desert Slash - wind up, diagonal slash, recover (0.6 s).
    windup = merged(relaxed, {
        "Spine": [(Z, 25)],
        "Chest": [(Z, 10)],
        "UpperArm.R": [(Y, 60), (Z, -40)],
        "LowerArm.R": [(Z, 60)],
        "Hand.R": [(X, -40)],
        "UpperLeg.L": [(X, -20)],
        "LowerLeg.L": [(X, 20)],
        "UpperLeg.R": [(X, 15)],
    })
    strike = merged(relaxed, {
        "Spine": [(Z, -35), (X, 15)],
        "Chest": [(Z, -10)],
        "UpperArm.R": [(Y, -20), (Z, 75)],
        "LowerArm.R": [(Z, 10)],
        "Hand.R": [(X, 10)],
        "UpperArm.L": [(Y, -10)],
        "UpperLeg.L": [(X, -35)],
        "LowerLeg.L": [(X, 35)],
        "UpperLeg.R": [(X, 25)],
        "LowerLeg.R": [(X, 15)],
        "Cape": [(X, 15), (Z, 15)],
    })
    make_action(rig, "Attack", [
        (1, relaxed, (0, 0, 0)),
        (6, windup, (0, 0, -0.03)),
        (10, strike, (0, -0.08, -0.07)),
        (14, strike, (0, -0.09, -0.07)),
        (19, relaxed, (0, 0, 0)),
    ], cyclic=False)

    # Dodge: low dash crouch (0.35 s).
    crouch = merged(arms_down_pose(lower=50, elbow=60), {
        "Spine": [(X, 35)],
        "Head": [(X, -20)],
        "UpperLeg.L": [(X, -60)],
        "LowerLeg.L": [(X, 80)],
        "UpperLeg.R": [(X, 20)],
        "LowerLeg.R": [(X, 70)],
        "Cape": [(X, 45)],
    })
    make_action(rig, "Dodge", [
        (1, relaxed, (0, 0, 0)),
        (5, crouch, (0, 0, -0.25)),
        (9, crouch, (0, 0, -0.25)),
        (12, relaxed, (0, 0, 0)),
    ], cyclic=False)


# --------------------------------------------------------------------------- export

def export(rig, body):
    bpy.ops.object.select_all(action="DESELECT")
    rig.select_set(True)
    body.select_set(True)
    bpy.context.view_layer.objects.active = rig
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(OUT_DIR, "Storm.blend"))

    bpy.ops.export_scene.gltf(
        filepath=os.path.join(OUT_DIR, "Storm.glb"),
        export_format="GLB",
        use_selection=True,
        export_animations=True,
        export_animation_mode="ACTIONS",
        export_force_sampling=True,
        export_apply=True,
    )

    bpy.ops.export_scene.fbx(
        filepath=os.path.join(OUT_DIR, "Storm.fbx"),
        use_selection=True,
        object_types={"ARMATURE", "MESH"},
        apply_scale_options="FBX_SCALE_ALL",
        axis_forward="-Z",
        axis_up="Y",
        add_leaf_bones=False,
        primary_bone_axis="Y",
        secondary_bone_axis="X",
        bake_anim=True,
        bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False,
        bake_anim_force_startend_keying=True,
        path_mode="COPY",
    )


# --------------------------------------------------------------------------- renders

def render_setup():
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 96
    # Some distro builds of Blender ship without OpenImageDenoise; fall back to more samples.
    scene.cycles.use_denoising = getattr(bpy.app.build_options, "openimagedenoise", False)
    scene.render.resolution_x = 720
    scene.render.resolution_y = 960
    scene.render.film_transparent = False
    scene.view_settings.view_transform = "AgX" if "AgX" in [v.identifier for v in
        scene.view_settings.bl_rna.properties["view_transform"].enum_items] else "Filmic"
    scene.view_settings.look = "None"

    world = bpy.data.worlds.new("Sky")
    scene.world = world
    world.use_nodes = True
    bg = world.node_tree.nodes["Background"]
    bg.inputs["Color"].default_value = (*srgb("#e9c48f"), 1.0)
    bg.inputs["Strength"].default_value = 0.9

    bpy.ops.object.light_add(type="SUN", rotation=(math.radians(50), math.radians(10), math.radians(-35)))
    sun = bpy.context.active_object
    sun.data.energy = 4.0
    sun.data.color = (1.0, 0.86, 0.68)
    sun.data.angle = math.radians(4)

    bpy.ops.object.light_add(type="AREA", location=(1.2, 2.0, 2.2))
    rim = bpy.context.active_object
    rim.data.energy = 250
    rim.data.size = 2
    rim.data.color = (1.0, 0.75, 0.5)
    rim.rotation_euler = (Vector((0, 0, 1.2)) - rim.location).to_track_quat("-Z", "Y").to_euler()

    bpy.ops.mesh.primitive_plane_add(size=40, location=(0, 0, 0))
    ground = bpy.context.active_object
    ground.data.materials.append(material("Sand", srgb("#d8b27a"), roughness=0.95))

    bpy.ops.object.camera_add()
    cam = bpy.context.active_object
    scene.camera = cam
    cam.data.lens = 60
    return cam


def aim(cam, location, target):
    cam.location = location
    cam.rotation_euler = (Vector(target) - Vector(location)).to_track_quat("-Z", "Y").to_euler()


def render_to(path):
    scene.render.filepath = path
    bpy.ops.render.render(write_still=True)


def set_pose(rig, action_name, frame):
    rig.animation_data.action = bpy.data.actions[action_name]
    for track in rig.animation_data.nla_tracks:
        track.mute = True
    scene.frame_set(frame)


def render_all(rig):
    renders = os.path.join(OUT_DIR, "renders")
    os.makedirs(renders, exist_ok=True)
    cam = render_setup()

    # Turnaround in the relaxed idle pose.
    set_pose(rig, "Idle", 1)
    target = (0, 0, 0.95)
    for name, angle in (("front", 0), ("three_quarter", 35), ("side", 90), ("back", 180)):
        a = math.radians(angle)
        aim(cam, (math.sin(a) * 4.6, -math.cos(a) * 4.6, 1.15), target)
        render_to(os.path.join(renders, "turnaround_%s.png" % name))

    # One render per animation.
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720
    shots = [
        ("walk", "Walk", 9, (2.6, -3.6, 1.4), (0, 0, 0.9)),
        ("run", "Run", 6, (2.6, -3.6, 1.4), (0, 0, 0.9)),
        ("attack", "Attack", 10, (-2.4, -3.8, 1.5), (0, -0.2, 1.0)),
        ("dodge", "Dodge", 6, (2.8, -3.2, 1.3), (0, 0, 0.6)),
        ("sit", "Sit", 1, (2.2, -3.0, 1.2), (0, 0, 0.45)),
        ("sleep", "Sleep", 1, (2.6, -1.8, 2.0), (0, 0.6, 0.15)),
    ]
    for name, action, frame, location, target in shots:
        set_pose(rig, action, frame)
        aim(cam, location, target)
        render_to(os.path.join(renders, "pose_%s.png" % name))
    # HUD portrait (square close-up of the face).
    scene.render.resolution_x = 384
    scene.render.resolution_y = 384
    set_pose(rig, "Idle", 1)
    cam.data.lens = 85
    aim(cam, (0.55, -1.75, 1.74), (0, 0, 1.69))
    render_to(os.path.join(renders, "portrait.png"))
    cam.data.lens = 60
    scene.render.resolution_x = 960
    scene.render.resolution_y = 720

    # Gameplay-style over-the-shoulder view.
    set_pose(rig, "Idle", 1)
    aim(cam, (0.9, 3.4, 2.3), (0.2, -2.0, 1.2))
    render_to(os.path.join(renders, "gameplay_camera.png"))


# --------------------------------------------------------------------------- main

build_body()
rig = build_armature()
body = skin(rig)
build_animations(rig)
export(rig, body)
if DO_RENDER:
    render_all(rig)
print("STORM_DONE", OUT_DIR)
