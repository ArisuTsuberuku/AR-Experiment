"""Dựng mô hình 3D "mô hình cấu tạo đèn sợi đốt" bằng Blender, xuất GLB.

Dựng theo tỉ lệ đo trên 26 ảnh trong kho
  01_Anh_Da_Goc/Den-soi-dot/  (phiên chụp 15/11/2026).
Mọi kích thước tính theo D = đường kính đáy đế (mét). Đo lại D bằng thước
rồi chạy lại với --D để có tỉ lệ 1:1.

Chạy (từ gốc repo):
  D:/Blender/blender.exe -b --factory-startup -P pipeline/4_dung_3d/den_soi_dot_blender.py -- \
      --D 0.12 --out experiments/den-soi-dot/model/scene.glb --preview work/den-soi-dot

Bản 0.2: đế 8 mặt có gờ bo, chất liệu nướng ra texture (bakelit vân sọc, cột thép
oxit than chì có đốm gỉ, đầu cột dập dẹt) — xem chat_lieu.py.

Hệ trục trong Blender: Z lên, mặt trước của mô hình nhìn về -Y.
Gốc toạ độ: tâm đáy đế, nằm trên mặt bàn (z = 0).
"""
import argparse
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chat_lieu as cl  # noqa: E402

# ---------------------------------------------------------------- tham số
argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ap = argparse.ArgumentParser()
ap.add_argument("--D", type=float, default=0.12, help="đường kính đáy đế (m)")
ap.add_argument("--out", default="scene.glb")
ap.add_argument("--preview", default="", help="thư mục lưu ảnh render để so hình")
ap.add_argument("--tex", default="work/den-soi-dot/tex", help="thư mục lưu texture đã nướng")
ap.add_argument("--tex-size", type=int, default=1024)
args = ap.parse_args(argv)
D = args.D

# Tỉ lệ (theo D), đo trên ảnh chụp ngang và ảnh chụp từ trên xuống
BASE_R_BOTTOM = 0.50 * D
BASE_R_TOP = 0.355 * D
BASE_H = 0.40 * D
WALL = 0.035 * D
NOTCH_N = 8
NOTCH_W = 0.12 * D
NOTCH_H = 0.11 * D
NECK_R = 0.315 * D
NECK_H = 0.03 * D
ORING_R = 0.322 * D
ORING_T = 0.038 * D
DISK_R = 0.318 * D
DISK_H = 0.05 * D
Z_DISK_TOP = BASE_H + NECK_H + DISK_H

ROD_R = 0.024 * D
ROD_H = 0.54 * D
RODS = {"trai": (-0.21 * D, 0.02 * D), "phai": (0.21 * D, 0.02 * D), "giua": (0.0, -0.12 * D)}

COIL_R = 0.055 * D
COIL_WIRE = 0.0035 * D
COIL_TURNS = 19
Z_COIL = Z_DISK_TOP + 0.65 * D
COILS = [((-0.255 * D, 0.045 * D), (-0.018 * D, -0.10 * D)),
         ((0.018 * D, -0.10 * D), (0.255 * D, 0.045 * D))]

SUP_WIRE = 0.0065 * D
Z_LOOP = Z_DISK_TOP + 0.74 * D

# ---------------------------------------------------------------- tiện ích
bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene
scene.unit_settings.system = "METRIC"


def material(name, rgb, metallic=0.0, roughness=0.5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    bsdf = m.node_tree.nodes.get("Principled BSDF")
    bsdf.inputs["Base Color"].default_value = (*rgb, 1.0)
    bsdf.inputs["Metallic"].default_value = metallic
    bsdf.inputs["Roughness"].default_value = roughness
    return m


M_NHUA = material("NhuaNau_tam", (0.24, 0.065, 0.032), 0.0, 0.30)      # tạm, sẽ nướng lại
M_CAOSU = cl.simple("CaoSuDen", (0.018, 0.018, 0.018), 0.0, 0.62)
M_TRU = material("TruKimLoai_tam", (0.035, 0.038, 0.038), 0.3, 0.7)   # tạm, sẽ nướng lại
M_DAY = cl.simple("DayDo_XamBe", cl.lin((0.70, 0.66, 0.60)), 0.55, 0.55)   # dây đỡ: xám be mờ
M_SOI = cl.simple("SoiToc_Bac", cl.lin((0.80, 0.80, 0.78)), 1.0, 0.3)     # sợi tóc: bạc sáng


def assign(obj, mat):
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    return obj


def shade_smooth(obj, angle_deg=40):
    for p in obj.data.polygons:
        p.use_smooth = True
    try:
        obj.data.set_sharp_from_angle(angle=math.radians(angle_deg))
    except Exception:
        pass


def apply_boolean(target, cutter, op="DIFFERENCE"):
    mod = target.modifiers.new("bool", "BOOLEAN")
    mod.operation = op
    mod.solver = "EXACT"
    mod.object = cutter
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def cylinder(name, r, h, loc, verts=48, rot=(0, 0, 0)):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    o = bpy.context.active_object
    o.name = name
    return o


def tube_from_points(name, pts, radius, mat, closed=False, res=6):
    cu = bpy.data.curves.new(name, "CURVE")
    cu.dimensions = "3D"
    cu.bevel_depth = radius
    cu.bevel_resolution = 2
    cu.use_fill_caps = True
    sp = cu.splines.new("POLY")
    sp.points.add(len(pts) - 1)
    for p, v in zip(sp.points, pts):
        p.co = (v[0], v[1], v[2], 1.0)
    sp.use_cyclic_u = closed
    obj = bpy.data.objects.new(name, cu)
    scene.collection.objects.link(obj)
    bpy.context.view_layer.objects.active = obj
    for o in bpy.context.selected_objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.ops.object.convert(target="MESH")
    obj = bpy.context.active_object
    assign(obj, mat)
    shade_smooth(obj, 80)
    return obj


def catmull_rom(ctrl, per_seg=12, closed=False):
    pts = [Vector(c) for c in ctrl]
    n = len(pts)
    out = []
    segs = n if closed else n - 1
    for i in range(segs):
        p0 = pts[(i - 1) % n] if (closed or i > 0) else pts[0]
        p1 = pts[i]
        p2 = pts[(i + 1) % n]
        p3 = pts[(i + 2) % n] if (closed or i + 2 < n) else pts[-1]
        for k in range(per_seg):
            t = k / per_seg
            t2, t3 = t * t, t * t * t
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t3))
    if not closed:
        out.append(pts[-1])
    return [tuple(v) for v in out]


# ---------------------------------------------------------------- 1. đế
# 8 mặt phẳng, đỉnh lệch 22,5° để mỗi khe vòm nằm giữa một mặt (như vật thật)
bpy.ops.mesh.primitive_cone_add(vertices=8, radius1=BASE_R_BOTTOM / math.cos(math.pi / 8),
                                radius2=BASE_R_TOP / math.cos(math.pi / 8), depth=BASE_H,
                                end_fill_type="NOTHING", location=(0, 0, BASE_H / 2),
                                rotation=(0, 0, math.pi / 8))
de = bpy.context.active_object
de.name = "De"
bev = de.modifiers.new("bo_canh", "BEVEL")
bev.limit_method = "ANGLE"
bev.angle_limit = math.radians(30)
bev.width = 0.045 * D
bev.segments = 4
bpy.ops.object.modifier_apply(modifier=bev.name)
sol = de.modifiers.new("day", "SOLIDIFY")
sol.thickness = WALL
sol.offset = -1.0
bpy.ops.object.modifier_apply(modifier=sol.name)

for i in range(NOTCH_N):
    a = 2 * math.pi * i / NOTCH_N
    c, s = math.cos(a), math.sin(a)
    # khe vòm = trụ nằm ngang (nửa trên) + hộp (nửa dưới)
    r_mid = (BASE_R_BOTTOM + BASE_R_TOP) / 2
    arch = cylinder("khe", NOTCH_W / 2, 0.4 * D, (c * r_mid, s * r_mid, NOTCH_H - NOTCH_W / 2),
                    verts=32, rot=(0, math.pi / 2, a))
    bpy.ops.mesh.primitive_cube_add(size=1, location=(c * r_mid, s * r_mid, (NOTCH_H - NOTCH_W / 2) / 2 - 0.01 * D))
    box = bpy.context.active_object
    box.scale = (0.4 * D, NOTCH_W, NOTCH_H - NOTCH_W / 2 + 0.02 * D)
    box.rotation_euler = (0, 0, a)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    apply_boolean(de, arch)
    apply_boolean(de, box)

san = cylinder("SanDe", BASE_R_TOP - WALL * 0.5, 0.012 * D, (0, 0, BASE_H - 0.006 * D), verts=96)
assign(de, M_NHUA)
assign(san, M_NHUA)
shade_smooth(de)

# ---------------------------------------------------------------- 2. cổ, gioăng, đĩa
co = assign(cylinder("Co", NECK_R, NECK_H, (0, 0, BASE_H + NECK_H / 2), verts=96), M_NHUA)
bpy.ops.mesh.primitive_torus_add(major_radius=ORING_R, minor_radius=ORING_T / 2,
                                 major_segments=96, minor_segments=12,
                                 location=(0, 0, BASE_H + NECK_H + ORING_T * 0.3))
gioang = assign(bpy.context.active_object, M_CAOSU)
gioang.name = "Gioang"
shade_smooth(gioang, 80)

dia = cylinder("Dia", DISK_R, DISK_H, (0, 0, BASE_H + NECK_H + DISK_H / 2), verts=96)
for (hx, hy, hr) in [(0.0, 0.10 * D, 0.045 * D), (0.07 * D, -0.02 * D, 0.025 * D),
                     (-0.12 * D, -0.10 * D, 0.012 * D), (0.15 * D, 0.13 * D, 0.012 * D)]:
    apply_boolean(dia, cylinder("lo", hr, DISK_H * 3, (hx, hy, BASE_H + NECK_H + DISK_H / 2), verts=32))
assign(dia, M_NHUA)
shade_smooth(dia)
for key, (x, y) in RODS.items():
    assign(cylinder(f"Gu_{key}", 0.052 * D, 0.022 * D, (x, y, Z_DISK_TOP + 0.011 * D), verts=48), M_NHUA)

# ---------------------------------------------------------------- 3. trụ
TIP_H = 0.06 * D
rod_parts = []
for key, (x, y) in RODS.items():
    body_h = ROD_H - TIP_H * 0.8
    t = assign(cylinder(f"Tru_{key}", ROD_R, body_h, (x, y, Z_DISK_TOP + body_h / 2), verts=24), M_TRU)
    shade_smooth(t)
    tip = cylinder(f"DauTru_{key}", ROD_R, TIP_H, (x, y, Z_DISK_TOP + ROD_H - TIP_H / 2), verts=24)
    tip.scale = (1.22, 0.62, 1.0)      # đầu cột bị dập dẹt
    bpy.ops.object.transform_apply(scale=True)
    assign(tip, M_TRU)
    rod_parts += [t, tip]
bpy.ops.mesh.primitive_cube_add(size=1, location=(0.13 * D, -0.07 * D, Z_DISK_TOP + 0.075 * D))
coc = bpy.context.active_object
coc.name = "CocNgan"
coc.scale = (0.035 * D, 0.035 * D, 0.15 * D)
bpy.ops.object.transform_apply(scale=True)
assign(coc, M_TRU)

# ---------------------------------------------------------------- 4. sợi tóc (2 ống lò xo)
for idx, (a2, b2) in enumerate(COILS):
    a = Vector((a2[0], a2[1], Z_COIL))
    b = Vector((b2[0], b2[1], Z_COIL))
    ax = (b - a).normalized()
    u = ax.cross(Vector((0, 0, 1))).normalized()
    v = ax.cross(u).normalized()
    steps = COIL_TURNS * 20
    pts = []
    for k in range(steps + 1):
        t = k / steps
        ang = 2 * math.pi * COIL_TURNS * t
        p = a + (b - a) * t + COIL_R * (math.cos(ang) * u + math.sin(ang) * v)
        pts.append(tuple(p))
    tube_from_points(f"SoiToc_{idx}", pts, COIL_WIRE, M_SOI)

# móc treo: từ đầu trụ lên tới đáy ống lò xo
hooks = [(RODS["trai"], COILS[0][0]), (RODS["giua"], COILS[0][1]), (RODS["phai"], COILS[1][1])]
for i, ((rx, ry), (cx, cy)) in enumerate(hooks):
    z0 = Z_DISK_TOP + ROD_H
    tube_from_points(f"Moc_{i}", [(rx, ry, z0 - 0.01 * D), (rx, ry, z0 + 0.03 * D),
                                  (cx, cy, Z_COIL - COIL_R)], COIL_WIRE * 1.4, M_SOI)

# ---------------------------------------------------------------- 5. dây đỡ (một dây liền)
gx, gy = RODS["giua"]
zc1, zc2 = Z_DISK_TOP + 0.28 * D, Z_DISK_TOP + 0.37 * D
off = ROD_R + SUP_WIRE * 1.2
ctrl = [
    (gx - off, gy - 0.005 * D, zc1),            # kẹp dưới, bên trái trụ giữa
    (gx - off, gy - 0.005 * D, Z_DISK_TOP + 0.50 * D),
    (-0.29 * D, 0.00 * D, Z_LOOP),               # đỉnh thuỳ trái
    (-0.25 * D, 0.14 * D, Z_LOOP),
    (-0.12 * D, 0.16 * D, Z_LOOP),
    (0.00 * D, 0.07 * D, Z_LOOP),                # điểm lõm giữa (phía sau)
    (0.12 * D, 0.16 * D, Z_LOOP),
    (0.25 * D, 0.14 * D, Z_LOOP),
    (0.29 * D, 0.00 * D, Z_LOOP),                # đỉnh thuỳ phải
    (gx + off, gy - 0.005 * D, Z_DISK_TOP + 0.52 * D),
    (gx + off, gy - 0.005 * D, zc2),            # kẹp trên, bên phải trụ giữa
]
tube_from_points("DayDo", catmull_rom(ctrl, per_seg=14), SUP_WIRE, M_DAY)
for z, sx in [(zc1, -1), (zc2, 1)]:   # 2 chốt ngang kẹp dây vào trụ giữa
    tube_from_points(f"Chot_{z:.3f}", [(gx, gy, z), (gx + sx * (off + SUP_WIRE * 2), gy, z)],
                     SUP_WIRE * 0.9, M_DAY)

# ---------------------------------------------------------------- nướng chất liệu
texdir = os.path.abspath(args.tex)
nhua = [o for o in scene.objects if o.type == "MESH" and (o.name in ("De", "SanDe", "Co", "Dia") or o.name.startswith("Gu_"))]
de_nhua = cl.join(nhua, "DeNhua")
cl.bake(de_nhua, "DeBakelit", cl.bakelit_nau, size=args.tex_size, outdir=texdir, z_top=BASE_H)
tru = cl.join(rod_parts, "TruDan")
cl.bake(tru, "TruThanChi", cl.kim_loai_gi_than_chi, size=args.tex_size, outdir=texdir,
        z_floor=Z_DISK_TOP, z_patch=0.15 * D)
cl.bake(coc, "CocBac", cl.kim_loai_bac_xin, size=256, outdir=texdir)

# ---------------------------------------------------------------- xuất GLB
for o in scene.objects:
    o.select_set(o.type == "MESH")
out = os.path.abspath(args.out)
os.makedirs(os.path.dirname(out), exist_ok=True)
bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True,
                          export_yup=True, export_apply=True,
                          export_image_format="JPEG", export_jpeg_quality=90)
tris = sum(len(p.vertices) - 2 for o in scene.objects if o.type == "MESH" for p in o.data.polygons)
print(f"XONG GLB: {out}  ({os.path.getsize(out)/1024:.0f} kB, ~{tris} tam giác, D = {D} m, "
      f"cao {Z_LOOP + SUP_WIRE:.3f} m)")

# ---------------------------------------------------------------- ảnh so hình
if args.preview:
    cl.preview(scene, os.path.abspath(args.preview), (0, 0, 0.62 * D), D)
