using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

/// <summary>
/// Màn AR: tìm mặt phẳng (vòng ngắm) → "Đặt ở đây" → mô hình trên mặt bàn, có thanh công cụ
/// (Tỉ lệ 1:1 · Chú thích · Tách rời · Chụp · Thông tin), ước lượng ánh sáng và kính khúc xạ.
/// Thao tác: kéo 1 ngón xoay; chụm 2 ngón phóng to; chạm mặt phẳng dời chỗ; kéo dọc trên thấu kính nâng / hạ que.
/// Tự tắt AR sau <see cref="TuTatSauGiay"/> giây không chạm, hoặc khi app vào nền.
/// Cấu trúc: MoHinhNeo (ARAnchor) → XoayTiLe (xoay, tỉ lệ) → mô hình.
/// </summary>
public class ManHinhAR : MonoBehaviour
{
    public const float TuTatSauGiay = 120f;
    const float BanKinhVong = 0.06f;      // m
    const float NangTachRoi = 0.04f;      // m, nút "Tách rời"
    const float NangToiDa = 0.06f;        // m, kéo dọc que
    const float ThoiGianTach = 0.4f;      // s

    public PhienAR phien;
    public Material vatLieuVong;
    public Light den;

    public event Action DaDong;
    public bool DangMo => man != null && !man.ClassListContains("an");

    static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    VisualElement goc, man, vungCham, lopNhan, theHuongDan, lopTamDung, thanhCongCu, bangDuoi;
    Label chip, huongDan, tieuDeBang, noiDungBang;
    Button nutDat, nutTachRoi, nutChuThich;

    GoiThiNghiem goi;
    Transform moHinh, gocCu, pivot, cumThauKinh, tamQuang;
    Vector3 viTriGocThauKinh;
    float chieuCao, tiLe = 1f, doNang;
    GameObject neo;
    LineRenderer vong;
    ARRaycastManager raycast;
    ARPlaneManager matPhang;
    ARCameraManager camAR;
    Pose? choDat;
    float lanChamCuoi;
    Coroutine hoatAnhTach;

    readonly List<(Transform nut, Label nhan)> nhanBoPhan = new List<(Transform, Label)>();
    readonly Dictionary<int, Vector2> ngon = new Dictionary<int, Vector2>();
    Vector2 diemBatDau;
    float tBatDau;
    bool daKeo, keoQue;

    // ánh sáng của màn xem 3D, trả lại khi rời AR
    Quaternion denXoay;
    Color denMau;
    float denCuongDo;
    AmbientMode moiTruongCheDo;
    SphericalHarmonicsL2 moiTruongGoc;
    bool daLuuAnhSang;

    // ------------------------------------------------------------------ Gắn giao diện
    public void Gan(VisualElement gocUI)
    {
        goc = gocUI;
        man = goc.Q("manAR");
        vungCham = goc.Q("vungChamAR");
        lopNhan = goc.Q("lopNhan");
        theHuongDan = goc.Q("theHuongDan");
        lopTamDung = goc.Q("lopTamDung");
        thanhCongCu = goc.Q("thanhCongCu");
        bangDuoi = goc.Q("bangDuoi");
        chip = goc.Q<Label>("chipAR");
        huongDan = goc.Q<Label>("huongDanAR");
        tieuDeBang = goc.Q<Label>("tieuDeBang");
        noiDungBang = goc.Q<Label>("noiDungBang");
        nutDat = goc.Q<Button>("nutDatAR");
        nutTachRoi = goc.Q<Button>("nutTachRoi");
        nutChuThich = goc.Q<Button>("nutChuThich");

        nutDat.clicked += () => { if (choDat.HasValue) Dat(choDat.Value); };
        goc.Q<Button>("nutDongAR").clicked += Dong;
        goc.Q<Button>("nutBatLai").clicked += () => StartCoroutine(BatAR());
        goc.Q<Button>("nutTiLe").clicked += () => DatTiLe(1f);
        nutChuThich.clicked += BatTatChuThich;
        nutTachRoi.clicked += TachRoi;
        goc.Q<Button>("nutChup").clicked += () => StartCoroutine(Chup());
        goc.Q<Button>("nutThongTin").clicked += HienThongTin;
        goc.Q<Button>("nutDongBang").clicked += () => bangDuoi.AddToClassList("an");

        // mọi lần chạm vào màn hình đều tính là "có thao tác"
        man.RegisterCallback<PointerDownEvent>(_ => lanChamCuoi = Time.unscaledTime, TrickleDown.TrickleDown);
        vungCham.RegisterCallback<PointerDownEvent>(KhiNhan);
        vungCham.RegisterCallback<PointerMoveEvent>(KhiKeo);
        vungCham.RegisterCallback<PointerUpEvent>(KhiTha);
        vungCham.RegisterCallback<PointerCancelEvent>(e => Tha(e.pointerId));
    }

    public void Mo(GoiThiNghiem g, Transform moHinhCanDat, float chieuCaoM)
    {
        goi = g;
        moHinh = moHinhCanDat;
        gocCu = moHinh.parent;
        chieuCao = chieuCaoM;
        cumThauKinh = TimNut(moHinh, "ThauKinh");
        tamQuang = TimNut(moHinh, "TamQuang");
        if (cumThauKinh != null) viTriGocThauKinh = cumThauKinh.localPosition;
        doNang = 0f;
        nutTachRoi.SetEnabled(cumThauKinh != null);   // đèn sợi đốt chỉ có 1 cụm
        man.RemoveFromClassList("an");
        StartCoroutine(BatAR());
    }

    public void Dong()
    {
        TatAR();
        man.AddToClassList("an");
        DaDong?.Invoke();
    }

    static Transform TimNut(Transform goc, string ten)
    {
        foreach (var t in goc.GetComponentsInChildren<Transform>(true)) if (t.name == ten) return t;
        return null;
    }

    // ------------------------------------------------------------------ Bật / tắt AR
    IEnumerator BatAR()
    {
        lopTamDung.AddToClassList("an");
        thanhCongCu.AddToClassList("an");
        bangDuoi.AddToClassList("an");
        theHuongDan.RemoveFromClassList("an");
        nutDat.SetEnabled(false);
        chip.text = "Đang khởi động AR…";
        huongDan.text = "Cầm máy hướng về mặt bàn.";

        yield return phien.Bat();
        if (!phien.DangChay)
        {
            chip.text = "Không bật được AR";
            huongDan.text = phien.Loi;
            yield break;
        }

        raycast = phien.Khung.GetComponentInChildren<ARRaycastManager>();
        matPhang = phien.Khung.GetComponentInChildren<ARPlaneManager>();
        camAR = phien.Khung.GetComponentInChildren<ARCameraManager>();
        LuuAnhSangGoc();
        // chế độ HDR của ARCore: hướng, màu và cường độ đèn chính + ánh sáng môi trường
        camAR.requestedLightEstimation = LightEstimation.MainLightDirection | LightEstimation.MainLightIntensity |
                                         LightEstimation.AmbientSphericalHarmonics;
        camAR.frameReceived += KhiCoKhungHinh;
        vong = TaoVong();
        moHinh.gameObject.SetActive(false);   // chỉ hiện khi đã đặt lên mặt bàn
        lanChamCuoi = Time.unscaledTime;
    }

    void TamDung()
    {
        TatAR();
        theHuongDan.AddToClassList("an");
        thanhCongCu.AddToClassList("an");
        bangDuoi.AddToClassList("an");
        lopTamDung.RemoveFromClassList("an");
        chip.text = "Camera đã tắt";
    }

    // Trả mô hình về màn xem 3D (đúng vị trí, tỉ lệ, que về chỗ) rồi tắt hẳn AR.
    void TatAR()
    {
        StopAllCoroutines();
        hoatAnhTach = null;
        XoaChuThich();
        if (moHinh != null)
        {
            moHinh.SetParent(gocCu, false);
            moHinh.localPosition = Vector3.zero;
            moHinh.localRotation = Quaternion.identity;
            moHinh.localScale = Vector3.one;
            moHinh.gameObject.SetActive(true);
        }
        doNang = 0f;
        ApDungNang();
        if (camAR != null) camAR.frameReceived -= KhiCoKhungHinh;
        camAR = null;
        if (neo != null) Destroy(neo);
        if (pivot != null) Destroy(pivot.gameObject);
        if (vong != null) Destroy(vong.gameObject);
        neo = null;
        pivot = null;
        vong = null;
        tiLe = 1f;
        ngon.Clear();
        TraAnhSangGoc();
        ThauKinhKhucXa.KhoangCachNen = ThauKinhKhucXa.NenMacDinh;
        phien.Tat();
    }

    void OnApplicationPause(bool vaoNen)
    {
        if (vaoNen && phien.DangChay) TamDung();
    }

    // ------------------------------------------------------------------ Mỗi khung hình
    void Update()
    {
        if (!phien.DangChay) return;
        if (Time.unscaledTime - lanChamCuoi > TuTatSauGiay)
        {
            TamDung();
            return;
        }

        if (neo == null)
        {
            choDat = null;
            var giua = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
            if (ARSession.state >= ARSessionState.SessionTracking &&
                raycast.Raycast(giua, s_Hits, TrackableType.PlaneWithinPolygon))
                choDat = s_Hits[0].pose;

            vong.enabled = choDat.HasValue;
            if (choDat.HasValue)
                vong.transform.SetPositionAndRotation(choDat.Value.position + Vector3.up * 0.002f, choDat.Value.rotation);
            nutDat.SetEnabled(choDat.HasValue);
            chip.text = TrangThai();
            huongDan.text = choDat.HasValue
                ? "Vòng xanh nằm trên mặt bàn. Bấm \"Đặt ở đây\"."
                : "Di chuyển máy chậm quanh mặt bàn để tìm mặt phẳng.";
            return;
        }

        // đã đặt: ẩn lưới mặt phẳng (vẫn dùng để chạm dời chỗ), đo khoảng cách nền cho kính
        foreach (var p in matPhang.trackables)
            if (p.TryGetComponent<MeshRenderer>(out var mr) && mr.enabled) mr.enabled = false;
        if (tamQuang != null) DoKhoangCachNen();
    }

    void LateUpdate()
    {
        if (nhanBoPhan.Count == 0 || Camera.main == null || goc.panel == null) return;
        var cam = Camera.main;
        foreach (var (nut, nhan) in nhanBoPhan)
        {
            var b = KhoGoi.HopBao(nut);
            Vector3 diem = b.center + Vector3.up * b.extents.y;
            bool truocMat = cam.WorldToViewportPoint(diem).z > 0f;
            nhan.EnableInClassList("an", !truocMat);
            if (!truocMat) continue;
            // camera AR phủ cả màn hình: viewport (gốc dưới-trái) → panel (gốc trên-trái), không phụ thuộc Screen
            Vector3 v = cam.WorldToViewportPoint(diem);
            var khung = goc.panel.visualTree.worldBound;
            var p = new Vector2(v.x * khung.width, (1f - v.y) * khung.height);
            float w = float.IsNaN(nhan.resolvedStyle.width) ? 0f : nhan.resolvedStyle.width;
            float h = float.IsNaN(nhan.resolvedStyle.height) ? 0f : nhan.resolvedStyle.height;
            nhan.style.left = p.x - w * 0.5f;   // vị trí đổi theo khung hình: phải đặt trực tiếp
            nhan.style.top = p.y - h - 6f;
        }
    }

    // Khoảng cách từ quang tâm tới nền nhìn thấy qua kính: tia từ mắt qua quang tâm, gặp mặt phẳng AR phía sau kính.
    void DoKhoangCachNen()
    {
        var cam = Camera.main.transform;
        Vector3 huong = tamQuang.position - cam.position;
        float L = huong.magnitude;
        float d = ThauKinhKhucXa.NenMacDinh;
        if (L > 1e-3f && raycast.Raycast(new Ray(cam.position, huong / L), s_Hits, TrackableType.PlaneWithinPolygon))
            foreach (var h in s_Hits)
                if (h.distance > L + 0.01f) { d = h.distance - L; break; }
        ThauKinhKhucXa.KhoangCachNen = d;
    }

    string TrangThai()
    {
        switch (ARSession.state)
        {
            case ARSessionState.Unsupported: return "Máy này không hỗ trợ ARCore";
            case ARSessionState.NeedsInstall:
            case ARSessionState.Installing: return "Cần cài Google Play Services for AR";
            case ARSessionState.SessionTracking:
                return choDat.HasValue ? "Đã thấy mặt bàn" : matPhang.trackables.count > 0 ? "Hướng vòng ngắm vào mặt bàn" : "Đang tìm mặt bàn…";
            default: return "Đang khởi động AR…";
        }
    }

    // ------------------------------------------------------------------ Đặt, xoay, tỉ lệ
    void Dat(Pose pose)
    {
        var rot = Quaternion.identity;
        var cam = Camera.main;
        if (cam != null)
        {
            Vector3 veCam = cam.transform.position - pose.position;   // mặt trước mô hình hướng về người xem
            veCam.y = 0f;
            if (veCam.sqrMagnitude > 1e-6f) rot = Quaternion.LookRotation(veCam.normalized, Vector3.up);
        }
        var neoMoi = new GameObject("MoHinhNeo");
        neoMoi.transform.SetPositionAndRotation(pose.position, rot);
        neoMoi.AddComponent<ARAnchor>();

        if (pivot == null)
        {
            pivot = new GameObject("XoayTiLe").transform;
            moHinh.SetParent(pivot, false);
            moHinh.localPosition = Vector3.zero;
            moHinh.localRotation = Quaternion.identity;
            moHinh.gameObject.SetActive(true);
        }
        pivot.SetParent(neoMoi.transform, false);   // dời chỗ: giữ góc xoay người dùng đã chọn
        pivot.localPosition = Vector3.zero;
        pivot.localScale = Vector3.one * tiLe;
        if (neo != null) Destroy(neo);
        neo = neoMoi;

        vong.enabled = false;
        theHuongDan.AddToClassList("an");
        thanhCongCu.RemoveFromClassList("an");
        CapNhatChip();
    }

    /// <summary>Chỉ để tự kiểm tra trong Editor (XR Simulation không có sẵn mặt phẳng): đặt mô hình trước camera.</summary>
    public void DatDeKiemTra(float xaM, float thapM)
    {
        var cam = Camera.main.transform;
        Vector3 truoc = cam.forward;
        truoc.y = 0f;
        Dat(new Pose(cam.position + truoc.normalized * xaM - Vector3.up * thapM, Quaternion.identity));
    }

    void DatTiLe(float s)
    {
        tiLe = Mathf.Clamp(s, 0.2f, 5f);
        if (Mathf.Abs(tiLe - 1f) < 0.03f) tiLe = 1f;   // hít về 1:1
        if (pivot != null) pivot.localScale = Vector3.one * tiLe;
        CapNhatChip();
    }

    void CapNhatChip()
    {
        if (neo == null) return;
        chip.text = $"x{So(tiLe, "0.00")} · cao {So(chieuCao * tiLe * 100f, "0.0")} cm";
    }

    static string So(float x, string dang) => x.ToString(dang, CultureInfo.InvariantCulture).Replace('.', ',');

    // ------------------------------------------------------------------ Cử chỉ (UI Toolkit, nhiều ngón)
    void KhiNhan(PointerDownEvent e)
    {
        ngon[e.pointerId] = e.position;
        vungCham.CapturePointer(e.pointerId);
        if (ngon.Count != 1) return;
        diemBatDau = e.position;
        tBatDau = Time.unscaledTime;
        daKeo = false;
        keoQue = cumThauKinh != null && pivot != null && TrungCumThauKinh(e.position);
    }

    void KhiKeo(PointerMoveEvent e)
    {
        if (!ngon.TryGetValue(e.pointerId, out var cu)) return;
        Vector2 moi = e.position;
        if (ngon.Count == 1)
        {
            if (!daKeo && (moi - diemBatDau).magnitude > 8f) daKeo = true;
            if (daKeo && pivot != null)
            {
                if (keoQue) DoiDoNang(-(moi.y - cu.y) * MetMoiDiem());
                else pivot.Rotate(0f, -(moi.x - cu.x) * 0.4f, 0f, Space.Self);
            }
        }
        else if (ngon.Count == 2 && pivot != null)
        {
            daKeo = true;
            Vector2 khac = default;
            foreach (var kv in ngon) if (kv.Key != e.pointerId) khac = kv.Value;
            float truoc = Vector2.Distance(cu, khac), sau = Vector2.Distance(moi, khac);
            if (truoc > 1f) DatTiLe(tiLe * sau / truoc);
        }
        ngon[e.pointerId] = moi;
    }

    void KhiTha(PointerUpEvent e)
    {
        bool cham = ngon.Count == 1 && !daKeo && Time.unscaledTime - tBatDau < 0.5f;
        Tha(e.pointerId);
        if (cham && neo != null && raycast.Raycast(RaManHinh(e.position), s_Hits, TrackableType.PlaneWithinPolygon))
            Dat(s_Hits[0].pose);   // chạm mặt phẳng: dời mô hình tới đó
    }

    void Tha(int id)
    {
        ngon.Remove(id);
        if (vungCham.HasPointerCapture(id)) vungCham.ReleasePointer(id);
    }

    // toạ độ panel (gốc trên-trái) → toạ độ màn hình (gốc dưới-trái)
    Vector2 RaManHinh(Vector2 p)
    {
        var g = goc.panel.visualTree.worldBound;
        return new Vector2(p.x / g.width * Screen.width, Screen.height - p.y / g.height * Screen.height);
    }

    bool TrungCumThauKinh(Vector2 p)
    {
        var b = KhoGoi.HopBao(cumThauKinh);
        b.Expand(0.02f * tiLe);   // nới hộp cho dễ chạm vào que mảnh
        return b.IntersectRay(Camera.main.ScreenPointToRay(RaManHinh(p)));
    }

    // Một điểm UI ứng với bao nhiêu mét (đơn vị mô hình) ở khoảng cách của thấu kính.
    float MetMoiDiem()
    {
        var cam = Camera.main;
        float L = Vector3.Distance(cam.transform.position, cumThauKinh.position);
        float caoKhung = 2f * L * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
        return caoKhung / goc.panel.visualTree.worldBound.height / tiLe;
    }

    // ------------------------------------------------------------------ Thấu kính: tách rời, nâng que
    void TachRoi()
    {
        if (cumThauKinh == null) return;
        if (hoatAnhTach != null) StopCoroutine(hoatAnhTach);
        hoatAnhTach = StartCoroutine(ChuyenDoNang(doNang > 0.001f ? 0f : NangTachRoi));
    }

    IEnumerator ChuyenDoNang(float dich)
    {
        float tu = doNang;
        for (float t = 0f; t < ThoiGianTach; t += Time.deltaTime)
        {
            doNang = Mathf.Lerp(tu, dich, Mathf.SmoothStep(0f, 1f, t / ThoiGianTach));
            ApDungNang();
            yield return null;
        }
        doNang = dich;
        ApDungNang();
        hoatAnhTach = null;
    }

    void DoiDoNang(float d)
    {
        if (hoatAnhTach != null) { StopCoroutine(hoatAnhTach); hoatAnhTach = null; }
        doNang = Mathf.Clamp(doNang + d, 0f, NangToiDa);
        ApDungNang();
    }

    void ApDungNang()
    {
        if (cumThauKinh == null) return;
        cumThauKinh.localPosition = viTriGocThauKinh + Vector3.up * doNang;
        nutTachRoi.text = doNang > 0.001f ? "Ghép lại" : "Tách rời";
    }

    // ------------------------------------------------------------------ Chú thích, thông tin
    void BatTatChuThich()
    {
        bool bat = nhanBoPhan.Count == 0;
        XoaChuThich();
        nutChuThich.EnableInClassList("nut-cong-cu--bat", bat);
        if (!bat) return;
        foreach (var (ten, nut) in BangTenBoPhan.LietKe(moHinh))
        {
            var l = new Label(ten);
            l.AddToClassList("nhan-bo-phan");
            l.RegisterCallback<ClickEvent>(_ => HienBang(ten, $"Bộ phận của mô hình \"{goi.title}\". Phần giải thích bộ phận chưa có trong gói thí nghiệm."));
            lopNhan.Add(l);
            nhanBoPhan.Add((nut, l));
        }
    }

    void XoaChuThich()
    {
        foreach (var (_, l) in nhanBoPhan) l.RemoveFromHierarchy();
        nhanBoPhan.Clear();
        nutChuThich?.RemoveFromClassList("nut-cong-cu--bat");
    }

    void HienThongTin()
    {
        var dong = new List<string> { $"Cao {So(chieuCao * tiLe * 100f, "0.0")} cm ở tỉ lệ x{So(tiLe, "0.00")} (x1,00 = kích thước thật)." };
        var tk = goi.thauKinh;
        if (tk != null)
            dong.Add($"Tiêu cự f = {So(tk.f_m * 1000f, "0")} mm, chiết suất n = {So(tk.n, "0.00")} (ghi trong mô hình, chưa đo). " +
                     "Nhìn qua kính: vật ở xa hơn tiêu cự cho ảnh ngược chiều; vật đặt gần kính hơn tiêu cự cho ảnh cùng chiều, to hơn.");
        if (!string.IsNullOrEmpty(goi.provenance?.scaleSource)) dong.Add("Kích thước: " + goi.provenance.scaleSource);
        if (goi.status == "draft") dong.Add("Gói bản nháp: số đo chưa kiểm.");
        HienBang(goi.title, string.Join("\n", dong));
    }

    void HienBang(string tieuDe, string noiDung)
    {
        tieuDeBang.text = tieuDe;
        noiDungBang.text = noiDung;
        bangDuoi.RemoveFromClassList("an");
    }

    // ------------------------------------------------------------------ Chụp ảnh
    IEnumerator Chup()
    {
        man.AddToClassList("an-khi-chup");   // ảnh chỉ có mô hình trên nền camera, không có nút
        yield return null;
        yield return new WaitForEndOfFrame();
        var tex = ScreenCapture.CaptureScreenshotAsTexture();
        man.RemoveFromClassList("an-khi-chup");
        string ten = "ARLab_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
        chip.text = ChupAnh.Luu(tex.EncodeToPNG(), ten);
        Destroy(tex);
    }

    // ------------------------------------------------------------------ Ước lượng ánh sáng (AR Foundation)
    void LuuAnhSangGoc()
    {
        denXoay = den.transform.rotation;
        denMau = den.color;
        denCuongDo = den.intensity;
        moiTruongCheDo = RenderSettings.ambientMode;
        moiTruongGoc = RenderSettings.ambientProbe;
        daLuuAnhSang = true;
    }

    void TraAnhSangGoc()
    {
        if (!daLuuAnhSang) return;
        daLuuAnhSang = false;
        den.transform.rotation = denXoay;
        den.color = denMau;
        den.intensity = denCuongDo;
        RenderSettings.ambientMode = moiTruongCheDo;
        RenderSettings.ambientProbe = moiTruongGoc;
    }

    void KhiCoKhungHinh(ARCameraFrameEventArgs a)
    {
        var le = a.lightEstimation;
        if (le.mainLightDirection.HasValue) den.transform.rotation = Quaternion.LookRotation(le.mainLightDirection.Value);
        if (le.mainLightColor.HasValue) den.color = le.mainLightColor.Value;
        else if (le.colorCorrection.HasValue) den.color = le.colorCorrection.Value;
        if (le.averageMainLightBrightness.HasValue) den.intensity = le.averageMainLightBrightness.Value;
        else if (le.averageBrightness.HasValue) den.intensity = le.averageBrightness.Value * 2f;
        if (le.ambientSphericalHarmonics.HasValue)
        {
            RenderSettings.ambientMode = AmbientMode.Skybox;
            RenderSettings.ambientProbe = le.ambientSphericalHarmonics.Value;
        }
    }

    LineRenderer TaoVong()
    {
        var lr = new GameObject("VongNgam").AddComponent<LineRenderer>();
        lr.sharedMaterial = vatLieuVong;
        lr.useWorldSpace = false;
        lr.loop = true;
        lr.widthMultiplier = 0.004f;
        lr.shadowCastingMode = ShadowCastingMode.Off;
        const int n = 64;
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float g = i * Mathf.PI * 2f / n;
            lr.SetPosition(i, new Vector3(Mathf.Cos(g), 0f, Mathf.Sin(g)) * BanKinhVong);
        }
        lr.enabled = false;
        return lr;
    }
}
