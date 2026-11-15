"""Thư viện chất liệu dùng chung cho các script dựng mô hình bằng Blender.

Chất liệu được sinh bằng thuật toán (node nhiễu, dải màu, mặt nạ theo độ cao…),
rồi "nướng" (bake) ra 3 ảnh texture để xuất GLB — vì GLB không chứa được node:
  - <ten>_mau.png  : màu nền (sRGB)
  - <ten>_orm.png  : R = 1, G = độ nhám, B = độ kim loại (Non-Color, chuẩn glTF)
  - <ten>_nor.png  : bản đồ pháp tuyến (lồi lõm), không gian tiếp tuyến

Cách dùng trong script dựng hình:
    import chat_lieu as cl
    cl.bake(obj, "TruDan", cl.kim_loai_gi_set, size=1024, outdir=...)
Hàm chất liệu nhận một G (bộ dựng node), trả về dict:
    color (socket màu), rough (socket/số), metal (socket/số), height (socket hoặc None),
    bump (độ mạnh lồi lõm, mặc định 0.4)
Toạ độ dùng toạ độ đối tượng (mét), nên vân liền mạch, không đứt ở đường nối UV.
"""
import math
import os

import bpy


# ---------------------------------------------------------------- bộ dựng node
class G:
    def __init__(self, mat):
        self.nt = mat.node_tree
        self.n = self.nt.nodes
        self.l = self.nt.links

    def node(self, t, **props):
        nd = self.n.new(t)
        for k, v in props.items():
            setattr(nd, k, v)
        return nd

    def feed(self, val, sock):
        if val is None:
            return
        if isinstance(val, (int, float)):
            sock.default_value = val
        elif isinstance(val, tuple):
            sock.default_value = val if len(val) == 4 else (*val, 1.0)
        else:
            self.l.new(val, sock)

    def coord(self, scale=(1, 1, 1), offset=(0, 0, 0)):
        tc = self.node("ShaderNodeTexCoord")
        mp = self.node("ShaderNodeMapping")
        mp.inputs["Scale"].default_value = scale
        mp.inputs["Location"].default_value = offset
        self.l.new(tc.outputs["Object"], mp.inputs["Vector"])
        return mp.outputs["Vector"]

    def axis(self, name="Z"):
        tc = self.node("ShaderNodeTexCoord")
        sx = self.node("ShaderNodeSeparateXYZ")
        self.l.new(tc.outputs["Object"], sx.inputs["Vector"])
        return sx.outputs[name]

    def noise(self, scale_xyz=(1, 1, 1), scale=1.0, detail=4.0, rough=0.5, distortion=0.0, offset=(0, 0, 0)):
        nz = self.node("ShaderNodeTexNoise")
        nz.inputs["Scale"].default_value = scale
        nz.inputs["Detail"].default_value = detail
        nz.inputs["Roughness"].default_value = rough
        nz.inputs["Distortion"].default_value = distortion
        self.l.new(self.coord(scale_xyz, offset), nz.inputs["Vector"])
        return nz.outputs["Fac"]

    def ramp(self, fac, stops):
        r = self.node("ShaderNodeValToRGB")
        els = r.color_ramp.elements
        while len(els) > 1:
            els.remove(els[-1])
        p0, c0 = stops[0]
        els[0].position = p0
        els[0].color = c0 if len(c0) == 4 else (*c0, 1.0)
        for p, c in stops[1:]:
            e = els.new(p)
            e.color = c if len(c) == 4 else (*c, 1.0)
        self.feed(fac, r.inputs["Fac"])
        return r.outputs["Color"]

    def math(self, op, a, b=0.0, clamp=False):
        m = self.node("ShaderNodeMath", operation=op, use_clamp=clamp)
        self.feed(a, m.inputs[0])
        self.feed(b, m.inputs[1])
        return m.outputs[0]

    def mrange(self, v, fmin, fmax, tmin=0.0, tmax=1.0):
        m = self.node("ShaderNodeMapRange")
        self.feed(v, m.inputs["Value"])
        m.inputs["From Min"].default_value = fmin
        m.inputs["From Max"].default_value = fmax
        m.inputs["To Min"].default_value = tmin
        m.inputs["To Max"].default_value = tmax
        return m.outputs["Result"]

    def mix(self, fac, a, b):
        m = self.node("ShaderNodeMix", data_type="RGBA", blend_type="MIX")
        self.feed(fac, m.inputs[0])
        ins = [s for s in m.inputs if s.type == "RGBA"]
        self.feed(a, ins[0])
        self.feed(b, ins[1])
        return [s for s in m.outputs if s.type == "RGBA"][0]

    def pointiness(self):
        geo = self.node("ShaderNodeNewGeometry")
        return geo.outputs["Pointiness"]


# ---------------------------------------------------------------- chất liệu
def lin(c):
    """sRGB (0–1) → tuyến tính, để nhập màu đo từ ảnh."""
    return tuple(((x + 0.055) / 1.055) ** 2.4 if x > 0.04045 else x / 12.92 for x in c)


def bakelit_nau(g, z_top=None, matte=0.0):
    """Nhựa bakelit nâu: sọc dọc đậm nhạt, bóng loang, xước nhỏ, bụi ở mép trên."""
    streak = g.noise((40, 40, 2.2), scale=1.0, detail=5, rough=0.55, distortion=0.3)
    streak2 = g.noise((160, 160, 6), scale=1.0, detail=3, rough=0.5)
    s = g.math("ADD", g.math("MULTIPLY", streak, 0.8), g.math("MULTIPLY", streak2, 0.2))
    color = g.ramp(s, [(0.34, lin((0.36, 0.16, 0.10))), (0.46, lin((0.47, 0.23, 0.14))),
                       (0.56, lin((0.56, 0.29, 0.19))), (0.66, lin((0.47, 0.22, 0.14)))])
    scratch = g.mrange(g.noise((1, 1, 1), scale=700, detail=1, rough=0.3), 0.75, 0.79)
    dust_n = g.mrange(g.noise((1, 1, 1), scale=220, detail=5, rough=0.7), 0.45, 0.75)
    if z_top is not None:   # dải bụi ngay dưới mép trên của đế
        near_top = g.math("MULTIPLY", g.mrange(g.axis("Z"), z_top - 0.012, z_top),
                          g.mrange(g.axis("Z"), z_top + 0.006, z_top))
        dust = g.math("MULTIPLY", near_top, dust_n)
    else:
        dust = g.math("MULTIPLY", dust_n, 0.35)
    dust = g.math("MAXIMUM", g.math("MULTIPLY", dust, 0.7), g.math("MULTIPLY", scratch, 0.2))
    color = g.mix(dust, color, lin((0.72, 0.62, 0.55)))
    rough = g.math("ADD", g.mrange(s, 0.3, 0.7, 0.12 + matte, 0.26 + matte), g.math("MULTIPLY", dust, 0.45), clamp=True)
    height = g.math("ADD", g.math("MULTIPLY", streak, 0.3), g.math("MULTIPLY", scratch, -0.6))
    return dict(color=color, rough=rough, metal=0.0, height=height, bump=0.08)


def kim_loai_gi_than_chi(g, z_floor=0.0, z_patch=0.02):
    """Thép phủ oxit màu than chì ngả xanh, sần; đốm gỉ trắng bạc; mảng trắng loang ở chân."""
    base_n = g.noise((1, 1, 1), scale=180, detail=8, rough=0.65)
    color = g.ramp(base_n, [(0.30, lin((0.15, 0.17, 0.16))), (0.55, lin((0.21, 0.24, 0.22))),
                            (0.75, lin((0.28, 0.29, 0.27)))])
    speck = g.mrange(g.noise((1, 1, 1), scale=1400, detail=2, rough=0.4), 0.70, 0.73)
    foot = g.mrange(g.axis("Z"), z_floor + z_patch, z_floor)                 # 1 ở chân, 0 ở trên
    patch_n = g.mrange(g.noise((1, 1, 0.5), scale=120, detail=6, rough=0.6), 0.35, 0.65)
    patch = g.math("MULTIPLY", foot, patch_n)
    white = g.math("MAXIMUM", speck, patch)
    color = g.mix(white, color, lin((0.70, 0.71, 0.68)))
    rough = g.math("ADD", g.mrange(base_n, 0.3, 0.8, 0.55, 0.85), g.math("MULTIPLY", white, 0.15), clamp=True)
    metal = g.mrange(white, 0.0, 1.0, 0.35, 0.0)
    height = g.math("ADD", base_n, g.math("MULTIPLY", speck, 0.5))
    return dict(color=color, rough=rough, metal=metal, height=height, bump=0.5)


def kim_loai_bac_xin(g):
    """Kim loại xám bạc đã xỉn, có mảng sẫm."""
    n = g.noise((1, 1, 1), scale=300, detail=6, rough=0.6)
    color = g.ramp(n, [(0.35, lin((0.38, 0.38, 0.37))), (0.6, lin((0.58, 0.58, 0.56))), (0.8, lin((0.66, 0.66, 0.63)))])
    rough = g.mrange(n, 0.3, 0.8, 0.35, 0.6)
    return dict(color=color, rough=rough, metal=0.75, height=n, bump=0.3)


def thep_gi_set(g):
    """Thép tròn gỉ nâu: nền sắt sẫm, loang gỉ cam nâu, sần."""
    n = g.noise((1, 1, 0.6), scale=160, detail=8, rough=0.65)
    rust_m = g.mrange(g.noise((1, 1, 0.4), scale=90, detail=6, rough=0.6, offset=(3, 1, 7)), 0.42, 0.62)
    iron = g.ramp(n, [(0.3, lin((0.10, 0.09, 0.085))), (0.7, lin((0.22, 0.20, 0.18)))])
    rust = g.ramp(n, [(0.3, lin((0.30, 0.13, 0.06))), (0.7, lin((0.48, 0.24, 0.10)))])
    color = g.mix(rust_m, iron, rust)
    rough = g.mrange(rust_m, 0.0, 1.0, 0.45, 0.9)
    metal = g.mrange(rust_m, 0.0, 1.0, 0.75, 0.05)
    height = g.math("ADD", n, g.math("MULTIPLY", rust_m, 0.6))
    return dict(color=color, rough=rough, metal=metal, height=height, bump=0.45)


def son_xam(g):
    """Kim loại sơn xám mờ: sơn hơi lốm đốm, bụi, mép bị mòn lộ kim loại."""
    n = g.noise((1, 1, 1), scale=70, detail=6, rough=0.55)
    color = g.ramp(n, [(0.3, lin((0.60, 0.62, 0.62))), (0.7, lin((0.70, 0.72, 0.72)))])
    edge = g.mrange(g.pointiness(), 0.53, 0.62)
    wear = g.math("MULTIPLY", edge, g.mrange(g.noise((1, 1, 1), scale=400, detail=3), 0.4, 0.6))
    grime = g.mrange(g.noise((1, 1, 1), scale=40, detail=4, rough=0.7, offset=(5, 2, 1)), 0.55, 0.8)
    color = g.mix(g.math("MULTIPLY", grime, 0.35), color, lin((0.45, 0.45, 0.43)))
    color = g.mix(wear, color, lin((0.78, 0.78, 0.76)))
    rough = g.math("ADD", g.mrange(n, 0.3, 0.7, 0.55, 0.7), g.math("MULTIPLY", wear, -0.3), clamp=True)
    metal = g.mrange(wear, 0.0, 1.0, 0.0, 0.9)
    height = g.math("ADD", g.math("MULTIPLY", n, 0.3), g.math("MULTIPLY", wear, -0.5))
    return dict(color=color, rough=rough, metal=metal, height=height, bump=0.15)


def nhua_den_bui(g):
    """Nhựa / sơn đen mờ, loang bụi xám, xước nhẹ."""
    n = g.noise((1, 1, 1), scale=35, detail=6, rough=0.65)
    color = g.ramp(n, [(0.35, lin((0.06, 0.06, 0.06))), (0.62, lin((0.11, 0.11, 0.11))), (0.8, lin((0.20, 0.20, 0.19)))])
    scratch = g.mrange(g.noise((1, 1, 1), scale=1000, detail=1), 0.69, 0.75)
    color = g.mix(g.math("MULTIPLY", scratch, 0.6), color, lin((0.30, 0.30, 0.29)))
    rough = g.mrange(n, 0.3, 0.8, 0.55, 0.8)
    height = g.math("ADD", g.math("MULTIPLY", n, 0.3), g.math("MULTIPLY", scratch, -0.4))
    return dict(color=color, rough=rough, metal=0.0, height=height, bump=0.12)


# ---------------------------------------------------------------- vật liệu đơn giản (không nướng)
def simple(name, rgb, metallic=0.0, roughness=0.5, alpha=1.0, ior=1.5):
    m = bpy.data.materials.new(name)
    m.use_nodes = True
    b = m.node_tree.nodes.get("Principled BSDF")
    b.inputs["Base Color"].default_value = (*rgb, 1.0)
    b.inputs["Metallic"].default_value = metallic
    b.inputs["Roughness"].default_value = roughness
    if alpha < 1.0:
        b.inputs["Alpha"].default_value = alpha
        b.inputs["IOR"].default_value = ior
        try:
            m.surface_render_method = "BLENDED"
        except Exception:
            pass
    return m


# ---------------------------------------------------------------- nướng
def _select_only(obj):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj


def unwrap(obj, margin=0.01):
    _select_only(obj)
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.uv.smart_project(angle_limit=math.radians(66), island_margin=margin)
    bpy.ops.object.mode_set(mode="OBJECT")


def bake(obj, name, recipe, size=1024, outdir=".", **kw):
    """Nướng chất liệu `recipe` lên `obj` (đã join), gán vật liệu dùng texture."""
    scene = bpy.context.scene
    scene.render.engine = "CYCLES"
    scene.cycles.device = "CPU"
    scene.cycles.samples = 1
    scene.render.bake.margin = 6
    unwrap(obj)

    mat = bpy.data.materials.new(name)
    mat.use_nodes = True
    mat.node_tree.nodes.clear()
    g = G(mat)
    spec = recipe(g, **kw)
    out = g.node("ShaderNodeOutputMaterial")
    emit = g.node("ShaderNodeEmission")
    emit.inputs["Strength"].default_value = 1.0
    g.l.new(emit.outputs[0], out.inputs["Surface"])
    target = g.node("ShaderNodeTexImage")
    g.n.active = target
    for nd in g.n:
        nd.select = False
    target.select = True

    obj.data.materials.clear()
    obj.data.materials.append(mat)
    _select_only(obj)
    os.makedirs(outdir, exist_ok=True)

    def new_img(suffix, noncolor):
        img = bpy.data.images.new(f"{name}_{suffix}", size, size, alpha=False)
        if noncolor:
            img.colorspace_settings.name = "Non-Color"
        return img

    # 1) màu
    img_c = new_img("mau", False)
    target.image = img_c
    g.feed(spec["color"], emit.inputs["Color"])
    bpy.ops.object.bake(type="EMIT")
    # 2) ORM
    img_o = new_img("orm", True)
    target.image = img_o
    comb = g.node("ShaderNodeCombineColor")
    comb.inputs["Red"].default_value = 1.0
    g.feed(spec["rough"], comb.inputs["Green"])
    g.feed(spec["metal"], comb.inputs["Blue"])
    g.l.new(comb.outputs[0], emit.inputs["Color"])
    bpy.ops.object.bake(type="EMIT")
    # 3) pháp tuyến
    img_n = new_img("nor", True)
    target.image = img_n
    bsdf = g.node("ShaderNodeBsdfPrincipled")
    if spec.get("height") is not None:
        bump = g.node("ShaderNodeBump")
        bump.inputs["Strength"].default_value = spec.get("bump", 0.4)
        bump.inputs["Distance"].default_value = 0.0005
        g.feed(spec["height"], bump.inputs["Height"])
        g.l.new(bump.outputs["Normal"], bsdf.inputs["Normal"])
    g.l.new(bsdf.outputs[0], out.inputs["Surface"])
    bpy.ops.object.bake(type="NORMAL", normal_space="TANGENT")

    for img in (img_c, img_o, img_n):
        img.filepath_raw = os.path.join(outdir, img.name + ".png")
        img.file_format = "PNG"
        img.save()

    # vật liệu cuối: chỉ còn texture (glTF đọc được)
    mat.node_tree.nodes.clear()
    out = g.node("ShaderNodeOutputMaterial")
    bsdf = g.node("ShaderNodeBsdfPrincipled")
    g.l.new(bsdf.outputs[0], out.inputs["Surface"])
    tc = g.node("ShaderNodeTexImage")
    tc.image = img_c
    g.l.new(tc.outputs["Color"], bsdf.inputs["Base Color"])
    to = g.node("ShaderNodeTexImage")
    to.image = img_o
    sep = g.node("ShaderNodeSeparateColor")
    g.l.new(to.outputs["Color"], sep.inputs[0])
    g.l.new(sep.outputs["Green"], bsdf.inputs["Roughness"])
    g.l.new(sep.outputs["Blue"], bsdf.inputs["Metallic"])
    tn = g.node("ShaderNodeTexImage")
    tn.image = img_n
    nm = g.node("ShaderNodeNormalMap")
    g.l.new(tn.outputs["Color"], nm.inputs["Color"])
    g.l.new(nm.outputs["Normal"], bsdf.inputs["Normal"])
    print(f"NUONG {name}: {size}px -> {outdir}")
    return mat


def join(objs, name):
    for o in bpy.context.scene.objects:
        o.select_set(False)
    for o in objs:
        o.select_set(True)
    bpy.context.view_layer.objects.active = objs[0]
    if len(objs) > 1:
        bpy.ops.object.join()
    o = bpy.context.active_object
    o.name = name
    # đưa gốc toạ độ về gốc thế giới: toạ độ đối tượng = toạ độ thật (m),
    # để các mặt nạ theo độ cao (chân cột, mép đế) đặt đúng chỗ
    bpy.ops.object.transform_apply(location=True, rotation=True, scale=True)
    return o


# ---------------------------------------------------------------- render so hình
def preview(scene, outdir, target, D, views=None, res=(600, 800), samples=64):
    from mathutils import Vector
    os.makedirs(outdir, exist_ok=True)
    world = bpy.data.worlds.new("W")
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs[0].default_value = (0.18, 0.17, 0.16, 1)
    world.node_tree.nodes["Background"].inputs[1].default_value = 0.35
    scene.world = world
    bpy.ops.mesh.primitive_plane_add(size=6 * D, location=(0, 0, 0))
    ban = bpy.context.active_object
    ban.data.materials.append(simple("Ban", (0.04, 0.037, 0.034), 0.0, 0.8))
    for loc, energy in [((0.6, -0.8, 1.0), 6.0), ((-0.8, -0.4, 0.8), 3.0), ((0, 0.8, 1.2), 2.0)]:
        L = bpy.data.lights.new("L", "AREA")
        L.energy = energy * (D / 0.12) ** 2 * 8
        L.size = 0.5
        lo = bpy.data.objects.new("L", L)
        lo.location = Vector(loc) * (D / 0.12)
        lo.rotation_euler = (Vector((0, 0, 0.1)) - lo.location).to_track_quat("-Z", "Y").to_euler()
        scene.collection.objects.link(lo)
    cam = bpy.data.objects.new("Cam", bpy.data.cameras.new("Cam"))
    cam.data.lens = 50
    scene.collection.objects.link(cam)
    scene.camera = cam
    scene.render.engine = "CYCLES"
    scene.cycles.samples = samples
    scene.cycles.device = "CPU"
    scene.render.resolution_x, scene.render.resolution_y = res
    target = Vector(target)
    views = views or {"truoc": (0.0, -1.0, 0.18), "xien": (0.7, -0.7, 0.25), "sau": (0.0, 1.0, 0.18),
                      "tren": (0.0, -0.25, 1.0)}
    for name, d in views.items():
        cam.location = target + Vector(d).normalized() * 3.8 * D
        cam.rotation_euler = (target - cam.location).to_track_quat("-Z", "Y").to_euler()
        scene.render.filepath = os.path.join(outdir, f"{name}.png")
        bpy.ops.render.render(write_still=True)
        print("ANH:", scene.render.filepath)
