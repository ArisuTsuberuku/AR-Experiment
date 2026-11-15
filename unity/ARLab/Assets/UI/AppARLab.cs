using System;
using System.Collections.Generic;
using System.Linq;
using GLTFast;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

/// <summary>
/// Điều khiển giao diện app (UI Toolkit): màn Thư viện và màn Chi tiết 3D.
/// Mở app không bật XR, không mở camera của máy.
/// </summary>
public class AppARLab : MonoBehaviour
{
    public UIDocument giaoDien;
    public XemMoHinh3D xem3D;
    public Transform gocMoHinh;
    public ManHinhAR manAR;
    public Shader shaderKinh;        // ARLab/KinhKhucXa
    public Material vatLieuBong;     // ARLab/BongDo

    static readonly (string ma, string ten)[] s_ChuDe =
        { (null, "Tất cả"), ("dien-hoc", "Điện"), ("quang-hoc", "Quang"), ("co-hoc", "Cơ") };
    static readonly string[] s_MauCham = { "cham--1", "cham--2", "cham--3", "cham--4", "cham--5" };

    VisualElement manThuVien, manChiTiet, danhSach, noiDungTab;
    Label trangThai, tieuDeChiTiet, phuDeChiTiet, dangNap;
    TextField oTim;
    readonly List<Button> chips = new List<Button>();
    readonly List<Button> tabs = new List<Button>();
    string chuDe;

    List<GoiThiNghiem> goi = new List<GoiThiNghiem>();
    GoiThiNghiem dangXem;
    GltfImport moHinh;
    Transform hopMoHinh;
    Button nutAR;
    float chieuCao;
    int luotNap;

    async void Start()
    {
        var r = giaoDien.rootVisualElement;
        manThuVien = r.Q("manThuVien");
        manChiTiet = r.Q("manChiTiet");
        danhSach = r.Q("danhSach");
        noiDungTab = r.Q("noiDungTab");
        trangThai = r.Q<Label>("trangThai");
        tieuDeChiTiet = r.Q<Label>("tieuDeChiTiet");
        phuDeChiTiet = r.Q<Label>("phuDeChiTiet");
        dangNap = r.Q<Label>("dangNap");
        oTim = r.Q<TextField>("oTim");

        oTim.RegisterValueChangedCallback(_ => VeThuVien());
        var dongChip = r.Q("dongChip");
        foreach (var (ma, ten) in s_ChuDe)
        {
            var b = new Button(() => { chuDe = ma; VeThuVien(); }) { text = ten };
            b.AddToClassList("chip");
            b.userData = ma;
            dongChip.Add(b);
            chips.Add(b);
        }
        tabs.AddRange(r.Query<Button>(className: "tab").ToList());
        for (int i = 0; i < tabs.Count; i++)
        {
            int k = i;
            tabs[i].clicked += () => ChonTab(k);
        }
        r.Q<Button>("nutQuayLai").clicked += VeManThuVien;

        nutAR = r.Q<Button>("nutAR");
        nutAR.SetEnabled(false);
        nutAR.clicked += VaoAR;
        manAR.Gan(r);
        manAR.DaDong += () =>
        {
            manChiTiet.RemoveFromClassList("an");
            xem3D.Hien(true);
        };

        xem3D.GanKhung(r.Q("khung3D"));
        VeManThuVien();

        try
        {
            goi = await KhoGoi.NapDanhMuc();
            foreach (var g in goi) await KhoGoi.NapAnh(g);
            trangThai.AddToClassList("an");
        }
        catch (Exception e)
        {
            trangThai.text = "Không nạp được danh sách thí nghiệm. " + e.Message;
            Debug.LogError("ARLAB danh muc: " + e);
        }
        VeThuVien();
    }

    void Update()
    {
        // Nút Back của Android = phím Escape trong Input System
        if (Keyboard.current == null || !Keyboard.current.escapeKey.wasPressedThisFrame) return;
        if (manAR.DangMo) manAR.Dong();
        else if (dangXem != null) VeManThuVien();
        else Application.Quit();
    }

    // ------------------------------------------------------------------ Thư viện
    void VeManThuVien()
    {
        dangXem = null;
        luotNap++;
        BoMoHinh();
        manChiTiet.AddToClassList("an");
        manThuVien.RemoveFromClassList("an");
        xem3D.Hien(false);
    }

    void VeThuVien()
    {
        foreach (var c in chips) c.EnableInClassList("chip--chon", (string)c.userData == chuDe);

        string tim = oTim.value?.Trim() ?? "";
        var ds = goi.Where(g => (chuDe == null || g.subject?.topic == chuDe) &&
                                (tim.Length == 0 || g.title.IndexOf(tim, StringComparison.CurrentCultureIgnoreCase) >= 0))
                    .ToList();
        danhSach.Clear();
        if (ds.Count == 0 && goi.Count > 0)
        {
            danhSach.Add(TaoNhan("Không có thí nghiệm phù hợp.", "trang-thai"));
            return;
        }

        VisualElement luoi = null;
        for (int i = 0; i < ds.Count; i++)
        {
            bool lon = i == 0;
            var the = TaoThe(ds[i], lon);
            if (lon) { danhSach.Add(the); continue; }
            if (luoi == null || luoi.childCount == 2)
            {
                luoi = new VisualElement();
                luoi.AddToClassList("luoi");
                danhSach.Add(luoi);
            }
            luoi.Add(the);
        }
        if (luoi != null && luoi.childCount == 1)
        {
            var dem = new VisualElement();   // giữ thẻ lẻ cuối cùng ở nửa trái
            dem.AddToClassList("the--dem");
            luoi.Add(dem);
        }
    }

    VisualElement TaoThe(GoiThiNghiem g, bool lon)
    {
        var the = new Button(() => MoChiTiet(g));
        the.AddToClassList("the");
        the.AddToClassList(lon ? "the--lon" : "the--nho");

        var anh = new VisualElement();
        anh.AddToClassList("anh-the");
        if (g.anh != null) anh.style.backgroundImage = new StyleBackground(g.anh);   // ảnh lấy từ gói lúc chạy
        the.Add(anh);

        the.Add(TaoNhan(g.title, "ten-the"));
        the.Add(TaoNhan(MoTaNgan(g), "phu-the"));

        if (!g.CoMoHinh)
        {
            the.SetEnabled(false);
            the.AddToClassList("the--sap-co");
            anh.Add(TaoNhan("Sắp có", "nhan-sap-co"));
        }
        else if (g.status == "draft")
            anh.Add(TaoNhan("Bản nháp", "nhan-nhap"));
        return the;
    }

    static string MoTaNgan(GoiThiNghiem g)
    {
        string lop = g.subject?.grades != null && g.subject.grades.Length > 0 ? "Lớp " + string.Join(", ", g.subject.grades) : "";
        string cd = s_ChuDe.FirstOrDefault(c => c.ma != null && c.ma == g.subject?.topic).ten;
        return string.Join(" · ", new[] { lop, cd }.Where(s => !string.IsNullOrEmpty(s)));
    }

    // ------------------------------------------------------------------ Chi tiết 3D
    async void MoChiTiet(GoiThiNghiem g)
    {
        dangXem = g;
        tieuDeChiTiet.text = g.title;
        phuDeChiTiet.text = MoTaNgan(g);
        manThuVien.AddToClassList("an");
        manChiTiet.RemoveFromClassList("an");
        dangNap.text = "Đang nạp mô hình…";
        dangNap.RemoveFromClassList("an");
        noiDungTab.Clear();
        BoMoHinh();
        nutAR.SetEnabled(false);
        xem3D.Hien(true);

        int luot = ++luotNap;
        var hop = new GameObject(g.id).transform;
        hop.SetParent(gocMoHinh, false);
        try
        {
            var imp = await KhoGoi.NapMoHinh(g, hop);
            if (luot != luotNap)   // người dùng đã rời màn này trong lúc nạp
            {
                Destroy(hop.gameObject);
                imp.Dispose();
                return;
            }
            moHinh = imp;
            hopMoHinh = hop;
            nutAR.SetEnabled(true);
            var b = KhoGoi.HopBao(hop);
            chieuCao = b.size.y;
            ThauKinhKhucXa.Gan(hop, shaderKinh, g.thauKinh);
            TaoMatBong(hop, b);
            xem3D.NhinVao(b);
            dangNap.AddToClassList("an");
            ChonTab(0);
        }
        catch (Exception e)
        {
            if (luot == luotNap) dangNap.text = "Không nạp được mô hình. " + e.Message;
            Debug.LogError("ARLAB mo hinh " + g.id + ": " + e);
        }
    }

    // Camera của máy chỉ bật từ đây (PhienAR nạp XR khi vào màn AR).
    void VaoAR()
    {
        if (hopMoHinh == null) return;
        manChiTiet.AddToClassList("an");
        manAR.Mo(dangXem, hopMoHinh, chieuCao);
    }

    // Mặt phẳng trong suốt dưới đáy mô hình chỉ để nhận bóng đổ (đi theo mô hình sang màn AR).
    void TaoMatBong(Transform hop, Bounds b)
    {
        var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
        Destroy(q.GetComponent<Collider>());
        q.name = "BongDo";
        q.transform.SetParent(hop, true);
        q.transform.SetPositionAndRotation(new Vector3(b.center.x, b.min.y + 0.0005f, b.center.z), Quaternion.Euler(90f, 0f, 0f));
        float c = Mathf.Max(b.size.x, b.size.z) * 3f;
        q.transform.localScale = new Vector3(c, c, 1f);
        var r = q.GetComponent<MeshRenderer>();
        r.sharedMaterial = vatLieuBong;
        r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    void BoMoHinh()
    {
        foreach (Transform c in gocMoHinh) Destroy(c.gameObject);
        moHinh?.Dispose();
        moHinh = null;
        hopMoHinh = null;
    }

    void ChonTab(int k)
    {
        for (int i = 0; i < tabs.Count; i++) tabs[i].EnableInClassList("tab--chon", i == k);
        noiDungTab.Clear();
        var g = dangXem;
        if (g == null || moHinh == null) return;

        if (k == 0)
        {
            var bp = BangTenBoPhan.LietKe(gocMoHinh);
            for (int i = 0; i < bp.Count; i++)
            {
                var dong = new VisualElement();
                dong.AddToClassList("dong-bo-phan");
                var cham = new VisualElement();
                cham.AddToClassList("cham");
                cham.AddToClassList(s_MauCham[i % s_MauCham.Length]);
                dong.Add(cham);
                dong.Add(TaoNhan(bp[i].ten, "chu-dong"));
                noiDungTab.Add(dong);
            }
        }
        else if (k == 1)
        {
            ThemDong("Chiều cao mô hình", SoViet(chieuCao * 100f) + " cm");
            if (g.thauKinh != null)
            {
                ThemDong("Tiêu cự f", SoViet(g.thauKinh.f_m * 1000f).Replace(",0", "") + " mm (nhãn trên giá; ghi trong mô hình)");
                ThemDong("Chiết suất kính n", g.thauKinh.n.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',') + " (giả định thuỷ tinh crown, chưa đo)");
            }
            ThemDong("Kích thước", g.provenance?.scaleSource);
            ThemDong("Phiên bản gói", g.version);
            ThemDong("Trạng thái", g.status == "draft" ? "Bản nháp — số đo chưa kiểm" : g.status);
        }
        else
        {
            ThemDong("Phiên chụp", g.provenance?.session);
            ThemDong("Cách dựng", g.provenance?.method);
            ThemDong("Ngày tạo", g.provenance?.createdAt);
        }
    }

    void ThemDong(string ten, string giaTri)
    {
        if (string.IsNullOrEmpty(giaTri)) return;
        var dong = new VisualElement();
        dong.AddToClassList("dong-thong-so");
        dong.Add(TaoNhan(ten, "ten-thong-so"));
        dong.Add(TaoNhan(giaTri, "gia-tri-thong-so"));
        noiDungTab.Add(dong);
    }

    static string SoViet(float x) => x.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture).Replace('.', ',');

    static Label TaoNhan(string chu, string lop)
    {
        var l = new Label(chu);
        l.AddToClassList(lop);
        return l;
    }
}
