"""Dựng mô hình 3D "thấu kính hội tụ f = 200 mm trên giá" bằng Blender, xuất GLB.

Dựng theo tỉ lệ đo trên 25 ảnh trong kho 01_Anh_Da_Goc/Thau-kinh/ (15/11/2026).
Mọi kích thước tính theo Dd = đường kính đĩa đen giữ kính (mét). Đo Dd thật rồi chạy lại.

Phần kính dựng theo vật lí: thấu kính hai mặt lồi đối xứng, chiết suất n,
bán kính mặt cong R = 2(n − 1)f (công thức thợ kính, thấu kính mỏng).

Hai cụm tách rời, như dụng cụ thật (xuất thành 2 nút gốc trong GLB):
  "DeKep"    : đế kẹp — khối sơn xám có lỗ vuông xuyên suốt và núm vặn để kẹp que.
               Dùng chung cho các dụng cụ quang khác (màn chắn, nguồn sáng...).
  "ThauKinh" : que thép + kẹp chữ V + đĩa + kính. Gốc nút ở đáy que; nút con
               "TamQuang" đặt ở quang tâm. Que cắm vào đế, nhô lên --que-nho (m).

Các bộ phận và chất liệu (ưu tiên kim loại):
  đế trụ thuôn + núm vặn  : kim loại sơn xám mờ, mép mòn lộ kim loại, bụi
  que (chân thấu kính)    : thép tròn gỉ nâu
  kẹp chữ V, đĩa, vành kính: nhựa / sơn đen mờ, loang bụi, xước
  đinh tán                : đồng
  nhãn "+200 mm", giấy dán: phẳng, màu trơn
  kính                    : trong suốt (alpha), n = 1,52

Chạy (từ gốc repo):
  D:/Blender/blender.exe -b --factory-startup -P pipeline/4_dung_3d/thau_kinh_blender.py -- \
      --Dd 0.12 --f 0.2 --out experiments/thau-kinh-f200/model/scene.glb --preview work/thau-kinh
Hệ trục: Z lên; mặt trước của đĩa nhìn về -Y; gốc toạ độ ở tâm đáy đế.
"""
import argparse
import math
import os
import sys

import bpy
import bmesh

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import chat_lieu as cl  # noqa: E402

argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
ap = argparse.ArgumentParser()
ap.add_argument("--Dd", type=float, default=0.12, help="đường kính đĩa đen (m)")
ap.add_argument("--f", type=float, default=0.20, help="tiêu cự (m)")
ap.add_argument("--n", type=float, default=1.52, help="chiết suất thuỷ tinh")
ap.add_argument("--out", default="scene.glb")
ap.add_argument("--preview", default="")
ap.add_argument("--tex", default="work/thau-kinh/tex")
ap.add_argument("--tex-size", type=int, default=1024)
ap.add_argument("--que-nho", type=float, default=None,
                help="đoạn que nhô trên mặt đế (m); mặc định 0,10 Dd theo ảnh")
ap.add_argument("--tach", type=float, default=0.0,
                help="chỉ cho ảnh xem trước: nhấc cụm thấu kính lên (m) để thấy 2 phần")
args = ap.parse_args(argv)
Dd = args.Dd

# ---------------------------------------------------------------- tỉ lệ (theo Dd), đo trên ảnh
BASE_RB, BASE_RT, BASE_H = 0.26 * Dd, 0.235 * Dd, 0.27 * Dd   # thân đế thấp, bè (ảnh 2, 7)
BOSS_R, BOSS_H = 0.175 * Dd, 0.025 * Dd                        # gờ nổi trên mặt đế (ảnh 19, 25)
TOP = BASE_H + BOSS_H                                          # mặt trên đế kẹp
STEM_R = 0.033 * Dd                  # que thép Ø ~ 0,066 Dd (ảnh 25: ≈ 1/7 đường kính đế)
QUE_NHO = args.que_nho if args.que_nho is not None else 0.10 * Dd
QUE_Z0 = 0.03 * Dd                   # đáy que (que xuyên gần hết lỗ, thấy được từ đáy đế)
K_Z0 = TOP + QUE_NHO                 # đáy kẹp: đầu que
K_ZM = K_Z0 + 0.05 * Dd              # hết khối kẹp đứng, bắt đầu loe chữ V
K_Z1 = K_ZM + 0.07 * Dd              # đỉnh kẹp
K_T = 0.06 * Dd                      # bề dày kẹp
DISK_R = 0.5 * Dd
DISK_T = 0.04 * Dd
DISK_ZC = K_Z1 - 0.03 * Dd + DISK_R  # tâm đĩa (đĩa ngậm vào kẹp 0,03 Dd)
RECESS_R, RECESS_D = 0.36 * Dd, 0.012 * Dd
LENS_R = 0.17 * Dd                   # bán kính phần kính
COLLAR_R = 0.205 * Dd
R_CURV = 2 * (args.n - 1) * args.f   # bán kính mặt cong
EDGE_T = 0.012 * Dd                  # bề dày mép kính

bpy.ops.wm.read_factory_settings(use_empty=True)
scene = bpy.context.scene


def active():
    return bpy.context.active_object


def apply_bool(target, cutter, op="DIFFERENCE"):
    m = target.modifiers.new("b", "BOOLEAN")
    m.operation = op
    m.solver = "EXACT"
    m.object = cutter
    bpy.context.view_layer.objects.active = target
    bpy.ops.object.modifier_apply(modifier=m.name)
    bpy.data.objects.remove(cutter, do_unlink=True)


def cyl(r, h, loc, rot=(0, 0, 0), verts=64, name="c"):
    bpy.ops.mesh.primitive_cylinder_add(vertices=verts, radius=r, depth=h, location=loc, rotation=rot)
    o = active()
    o.name = name
    return o


def box(size, loc, rot=(0, 0, 0), name="b"):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc, rotation=rot)
    o = active()
    o.name = name
    o.scale = size
    bpy.ops.object.transform_apply(scale=True)
    return o


def smooth(o, angle=35):
    for p in o.data.polygons:
        p.use_smooth = True
    try:
        o.data.set_sharp_from_angle(angle=math.radians(angle))
    except Exception:
        pass


def bevel(o, w, seg=3, angle=30):
    m = o.modifiers.new("bv", "BEVEL")
    m.limit_method = "ANGLE"
    m.angle_limit = math.radians(angle)
    m.width = w
    m.segments = seg
    bpy.context.view_layer.objects.active = o
    bpy.ops.object.modifier_apply(modifier=m.name)


# ---------------------------------------------------------------- 1. đế + núm vặn (kim loại sơn xám)
bpy.ops.mesh.primitive_cone_add(vertices=72, radius1=BASE_RB, radius2=BASE_RT, depth=BASE_H,
                                location=(0, 0, BASE_H / 2))
de = active()
de.name = "De"
bevel(de, 0.012 * Dd, seg=3, angle=40)
go = cyl(BOSS_R, BOSS_H * 2, (0, 0, BASE_H), verts=72, name="Go")
bevel(go, 0.005 * Dd, seg=2, angle=40)
apply_bool(de, go, op="UNION")                                                         # gờ nổi trên mặt
apply_bool(de, box((0.15 * Dd, 0.09 * Dd, 0.8 * Dd), (0, 0, TOP)))                     # lỗ chữ nhật xuyên suốt
for x, y in [(-0.10 * Dd, -0.07 * Dd), (0.0, 0.10 * Dd)]:
    apply_bool(de, cyl(0.007 * Dd, 0.1 * Dd, (x, y, TOP), verts=16))                  # 2 lỗ nhỏ trên gờ
apply_bool(de, box((0.08 * Dd, 0.8 * Dd, 0.09 * Dd), (0, 0, 0)))                      # rãnh dưới đáy
smooth(de)

ang = math.radians(-60)                       # núm vặn ở phía trước bên phải
ca, sa = math.cos(ang), math.sin(ang)
zk = 0.10 * Dd
r_at = BASE_RB - (BASE_RB - BASE_RT) * (zk / BASE_H)
shaft = cyl(0.016 * Dd, 0.09 * Dd, (ca * (r_at + 0.035 * Dd), sa * (r_at + 0.035 * Dd), zk),
            rot=(0, math.pi / 2, ang), verts=24, name="TrucNum")
collar = cyl(0.03 * Dd, 0.02 * Dd, (ca * (r_at + 0.01 * Dd), sa * (r_at + 0.01 * Dd), zk),
             rot=(0, math.pi / 2, ang), verts=32, name="DemNum")
wing = box((0.035 * Dd, 0.11 * Dd, 0.075 * Dd), (ca * (r_at + 0.09 * Dd), sa * (r_at + 0.09 * Dd), zk),
           rot=(0, 0, ang), name="TaiNum")
bevel(wing, 0.012 * Dd, seg=3)
for o in (shaft, collar, wing):
    smooth(o)
de_son = cl.join([de, shaft, collar, wing], "DeSonXam")

# ---------------------------------------------------------------- 2. que thép gỉ (chân thấu kính)
q_top = K_Z0 + 0.03 * Dd             # đầu que ngậm vào khối kẹp
can = cyl(STEM_R, q_top - QUE_Z0, (0, 0, (QUE_Z0 + q_top) / 2), verts=32, name="Que")
bevel(can, 0.004 * Dd, seg=2, angle=40)
smooth(can)

# ---------------------------------------------------------------- 3. kẹp chữ V (nhựa đen)
bm = bmesh.new()
wb, wt = 0.08 * Dd, 0.26 * Dd
pts = [(-wb / 2, K_ZM), (wb / 2, K_ZM), (wt / 2, K_Z1), (-wt / 2, K_Z1)]
vf = [bm.verts.new((x, -K_T / 2, z)) for x, z in pts]
vb = [bm.verts.new((x, K_T / 2, z)) for x, z in pts]
bm.faces.new(vf[::-1])
bm.faces.new(vb)
for i in range(4):
    j = (i + 1) % 4
    bm.faces.new((vf[i], vf[j], vb[j], vb[i]))
me = bpy.data.meshes.new("Kep")
bm.to_mesh(me)
bm.free()
kep = bpy.data.objects.new("Kep", me)
scene.collection.objects.link(kep)
bevel(kep, 0.008 * Dd, seg=2)
khoi = box((wb, K_T * 1.1, K_ZM - K_Z0 + 0.01 * Dd), (0, 0, (K_Z0 + K_ZM) / 2 + 0.005 * Dd), name="KhoiKep")
bevel(khoi, 0.006 * Dd, seg=2)
kep_nhua = cl.join([kep, khoi], "KepNhua")

# ---------------------------------------------------------------- 4. đĩa đen + vành kính
rotY = (math.pi / 2, 0, 0)        # trục trụ nằm theo Y
dia = cyl(DISK_R, DISK_T, (0, 0, DISK_ZC), rot=rotY, verts=128, name="Dia")
bevel(dia, 0.006 * Dd, seg=2, angle=40)
apply_bool(dia, cyl(RECESS_R, RECESS_D * 2, (0, -DISK_T / 2, DISK_ZC), rot=rotY, verts=96))   # mặt trước lõm bậc
apply_bool(dia, cyl(LENS_R, DISK_T * 4, (0, 0, DISK_ZC), rot=rotY, verts=96))                 # lỗ lắp kính
vanh = cyl(COLLAR_R, DISK_T + 0.035 * Dd, (0, -0.004 * Dd, DISK_ZC), rot=rotY, verts=96, name="VanhKinh")
apply_bool(vanh, cyl(LENS_R - 0.006 * Dd, DISK_T * 4, (0, 0, DISK_ZC), rot=rotY, verts=96))
bevel(vanh, 0.004 * Dd, seg=2, angle=40)
smooth(dia)
smooth(vanh)
dia_den = cl.join([dia, vanh], "DiaDen")

# ---------------------------------------------------------------- 5. kính hai mặt lồi (theo công thức)
def sag(r):
    return R_CURV - math.sqrt(R_CURV * R_CURV - r * r)


nr, ns = 24, 96
center_half = EDGE_T / 2 + sag(LENS_R)
bm = bmesh.new()
rings = {}
for side in (-1, 1):
    ring_list = []
    tip = bm.verts.new((0, side * center_half, DISK_ZC))
    for i in range(1, nr + 1):
        r = LENS_R * i / nr
        y = side * (EDGE_T / 2 + sag(LENS_R) - sag(r))
        ring_list.append([bm.verts.new((r * math.cos(2 * math.pi * j / ns), y,
                                        DISK_ZC + r * math.sin(2 * math.pi * j / ns))) for j in range(ns)])
    rings[side] = (tip, ring_list)
    for j in range(ns):
        a, b = ring_list[0][j], ring_list[0][(j + 1) % ns]
        bm.faces.new((tip, a, b) if side > 0 else (tip, b, a))
    for i in range(nr - 1):
        for j in range(ns):
            a, b = ring_list[i][j], ring_list[i][(j + 1) % ns]
            c, d = ring_list[i + 1][(j + 1) % ns], ring_list[i + 1][j]
            bm.faces.new((a, d, c, b) if side > 0 else (a, b, c, d))
outer_f, outer_b = rings[-1][1][-1], rings[1][1][-1]
for j in range(ns):
    a, b = outer_f[j], outer_f[(j + 1) % ns]
    c, d = outer_b[(j + 1) % ns], outer_b[j]
    bm.faces.new((a, b, c, d))
bmesh.ops.recalc_face_normals(bm, faces=bm.faces)
me = bpy.data.meshes.new("Kinh")
bm.to_mesh(me)
bm.free()
kinh = bpy.data.objects.new("Kinh", me)
scene.collection.objects.link(kinh)
smooth(kinh, 80)
kinh.data.materials.append(cl.simple("ThuyTinh", (0.62, 0.72, 0.70), 0.0, 0.02, alpha=0.10, ior=args.n))

# ---------------------------------------------------------------- 6. chi tiết mặt sau: đinh tán, nhãn, giấy dán
M_DONG = cl.simple("DinhTanDong", cl.lin((0.72, 0.42, 0.28)), 1.0, 0.38)
y_back = DISK_T / 2
for x, dz in [(-0.05 * Dd, 0.42 * Dd), (-0.30 * Dd, -0.34 * Dd)]:
    bpy.ops.mesh.primitive_uv_sphere_add(radius=0.012 * Dd, location=(x, y_back, DISK_ZC + dz), segments=16, ring_count=8)
    dt = active()
    dt.scale = (1, 0.45, 1)
    bpy.ops.object.transform_apply(scale=True)
    dt.name = "DinhTan"
    smooth(dt, 80)
    dt.data.materials.append(M_DONG)

nhan = box((0.095 * Dd, 0.003 * Dd, 0.09 * Dd), (-0.395 * Dd, y_back + 0.0015 * Dd, DISK_ZC + 0.05 * Dd), name="NhanNen")
nhan.data.materials.append(cl.simple("NhanDen", (0.01, 0.01, 0.01), 0.0, 0.5))
cu = bpy.data.curves.new("ChuNhan", "FONT")
cu.body = f"+{round(args.f * 1000)}\nmm"
cu.align_x = "CENTER"
cu.align_y = "CENTER"
cu.size = 0.032 * Dd
chu = bpy.data.objects.new("ChuNhan", cu)
scene.collection.objects.link(chu)
chu.location = (-0.395 * Dd, y_back + 0.0032 * Dd, DISK_ZC + 0.05 * Dd)
chu.rotation_euler = (math.pi / 2, 0, math.pi)        # quay ra mặt sau (+Y), đọc xuôi
bpy.context.view_layer.objects.active = chu
for o in scene.objects:
    o.select_set(o == chu)
bpy.ops.object.convert(target="MESH")
active().data.materials.append(cl.simple("ChuTrang", (0.85, 0.85, 0.85), 0.0, 0.6))

giay = box((0.11 * Dd, 0.002 * Dd, 0.30 * Dd), (0.36 * Dd, y_back + 0.001 * Dd, DISK_ZC - 0.01 * Dd), name="GiayDan")
giay.rotation_euler = (0, math.radians(4), 0)
giay.data.materials.append(cl.simple("GiayDan", cl.lin((0.80, 0.77, 0.68)), 0.0, 0.9))

# ---------------------------------------------------------------- 7. nướng chất liệu
texdir = os.path.abspath(args.tex)
cl.bake(de_son, "DeSonXam", cl.son_xam, size=args.tex_size, outdir=texdir)
cl.bake(can, "CanThepGi", cl.thep_gi_set, size=512, outdir=texdir)
cl.bake(kep_nhua, "KepNhuaDen", cl.nhua_den_bui, size=512, outdir=texdir)
cl.bake(dia_den, "DiaNhuaDen", cl.nhua_den_bui, size=args.tex_size, outdir=texdir)

# ---------------------------------------------------------------- 2 cụm: DeKep, ThauKinh
def empty(name, loc):
    e = bpy.data.objects.new(name, None)
    e.empty_display_size = 0.02
    e.location = loc
    scene.collection.objects.link(e)
    return e


bpy.context.view_layer.update()
n_de = empty("DeKep", (0, 0, 0))
n_tk = empty("ThauKinh", (0, 0, QUE_Z0))
n_tq = empty("TamQuang", (0, 0, DISK_ZC))
bpy.context.view_layer.update()
n_tq.parent = n_tk
n_tq.matrix_parent_inverse = n_tk.matrix_world.inverted()
n_tk["f_m"] = args.f
n_tk["n"] = args.n
n_tk["R_m"] = round(R_CURV, 5)
n_tk["ban_kinh_kinh_m"] = round(LENS_R, 5)
for o in list(scene.objects):
    if o.type != "MESH":
        continue
    parent = n_de if o == de_son else n_tk
    o.parent = parent
    o.matrix_parent_inverse = parent.matrix_world.inverted()

# ---------------------------------------------------------------- xuất GLB
for o in scene.objects:
    o.select_set(o.type in ("MESH", "EMPTY"))
out = os.path.abspath(args.out)
os.makedirs(os.path.dirname(out), exist_ok=True)
bpy.ops.export_scene.gltf(filepath=out, export_format="GLB", use_selection=True, export_yup=True,
                          export_apply=True, export_extras=True, export_image_format="JPEG", export_jpeg_quality=90)
tris = sum(len(p.vertices) - 2 for o in scene.objects if o.type == "MESH" for p in o.data.polygons)
top = DISK_ZC + DISK_R
print(f"XONG GLB: {out} ({os.path.getsize(out)/1024:.0f} kB, ~{tris} tam giác, Dd = {Dd} m, "
      f"cao {top:.3f} m, R mặt cong = {R_CURV*1000:.0f} mm, dày tâm kính = {2*center_half*1000:.1f} mm, "
      f"quang tâm cao {DISK_ZC*100:.1f} cm, que nhô {QUE_NHO*100:.1f} cm)")

if args.preview:
    if args.tach:
        n_tk.location.z += args.tach
    views = {"truoc": (0.0, -1.0, 0.15), "xien": (0.7, -0.7, 0.25), "sau": (-0.3, 1.0, 0.15),
             "ben": (1.0, -0.05, 0.12)}
    cl.preview(scene, os.path.abspath(args.preview), (0, 0, 0.78 * Dd + args.tach / 2), Dd * 1.3 + args.tach, views=views)
