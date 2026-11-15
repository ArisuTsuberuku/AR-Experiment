using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using ETouch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TPhase = UnityEngine.InputSystem.TouchPhase;

/// <summary>
/// Đặt mô hình thí nghiệm lên mặt phẳng thật (mặt bàn) và cho phép xoay, thu phóng.
/// Mặc định tỉ lệ 1:1 với vật thật; nút "Tỉ lệ 1:1" đưa về đúng kích thước thật.
///
/// Thao tác:
///   chạm vào mặt phẳng   → đặt (hoặc dời) mô hình
///   kéo một ngón          → xoay quanh trục đứng
///   chụm / mở hai ngón    → thu nhỏ / phóng to
/// Cấu trúc: AnchorGO (ARAnchor, do AR cập nhật) → pivot (xoay, tỉ lệ) → mô hình.
/// </summary>
public class ARLabPlacer : MonoBehaviour
{
    public ARRaycastManager raycastManager;
    public ARPlaneManager planeManager;
    public GameObject modelPrefab;
    public string modelTitle = "Mô hình thí nghiệm";

    static readonly List<ARRaycastHit> s_Hits = new List<ARRaycastHit>();

    GameObject anchorGO;
    Transform pivot;
    GameObject model;
    float scale = 1f;
    float modelHeight;
    bool planesVisible = true;
    bool moved;
    float touchStart;

    Rect btnScale, btnReset, btnPlanes;
    float topBar, bottomBar;
    int fontSize;

    void OnEnable() => EnhancedTouchSupport.Enable();
    void OnDisable() => EnhancedTouchSupport.Disable();

    void Update()
    {
        Layout();

        var touches = ETouch.activeTouches;
        if (touches.Count == 1)
        {
            var t = touches[0];
            if (t.phase == TPhase.Began)
            {
                touchStart = Time.unscaledTime;
                moved = false;
            }
            else if (t.phase == TPhase.Moved)
            {
                if (t.delta.sqrMagnitude > 9f) moved = true;
                if (moved && pivot != null && !OverUi(t.startScreenPosition))
                    pivot.Rotate(0f, -t.delta.x * 0.25f, 0f, Space.Self);
            }
            else if (t.phase == TPhase.Ended && !moved && Time.unscaledTime - touchStart < 0.5f)
            {
                OnTap(t.screenPosition);
            }
        }
        else if (touches.Count == 2 && pivot != null)
        {
            moved = true;
            var a = touches[0];
            var b = touches[1];
            float now = Vector2.Distance(a.screenPosition, b.screenPosition);
            float before = Vector2.Distance(a.screenPosition - a.delta, b.screenPosition - b.delta);
            if (before > 1f) SetScale(scale * now / before);
        }

        if (!planesVisible)
            foreach (var p in planeManager.trackables)
                if (p.gameObject.activeSelf) p.gameObject.SetActive(false);
    }

    void OnTap(Vector2 screenPos)
    {
        var g = ToGui(screenPos);
        if (btnReset.Contains(g)) { SetScale(1f); return; }
        if (btnPlanes.Contains(g)) { TogglePlanes(); return; }
        if (OverUi(screenPos)) return;

        if (raycastManager.Raycast(screenPos, s_Hits, TrackableType.PlaneWithinPolygon))
            Place(s_Hits[0].pose);
    }

    void Place(Pose pose)
    {
        if (modelPrefab == null) return;

        // Hướng mặt trước của mô hình về phía người xem
        var cam = Camera.main != null ? Camera.main.transform : null;
        var rot = Quaternion.identity;
        if (cam != null)
        {
            Vector3 toCam = cam.position - pose.position;
            toCam.y = 0f;
            if (toCam.sqrMagnitude > 1e-6f) rot = Quaternion.LookRotation(toCam.normalized, Vector3.up);
        }

        if (pivot == null)
        {
            pivot = new GameObject("Pivot").transform;
            model = Instantiate(modelPrefab, pivot);
            model.transform.localPosition = Vector3.zero;
            modelHeight = MeasureHeight(model);
        }
        else
        {
            pivot.SetParent(null, false);
        }

        if (anchorGO != null) Destroy(anchorGO);
        anchorGO = new GameObject("MoHinhNeo");
        anchorGO.transform.SetPositionAndRotation(pose.position, rot);
        anchorGO.AddComponent<ARAnchor>();

        pivot.SetParent(anchorGO.transform, false);
        pivot.localPosition = Vector3.zero;
        pivot.localScale = Vector3.one * scale;
    }

    static float MeasureHeight(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return 0f;
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b.size.y;
    }

    void SetScale(float s)
    {
        scale = Mathf.Clamp(s, 0.2f, 5f);
        if (Mathf.Abs(scale - 1f) < 0.03f) scale = 1f;   // hít về 1:1
        if (pivot != null) pivot.localScale = Vector3.one * scale;
    }

    void TogglePlanes()
    {
        planesVisible = !planesVisible;
        foreach (var p in planeManager.trackables) p.gameObject.SetActive(planesVisible);
    }

    // ------------------------------------------------------------ giao diện (vẽ bằng IMGUI, bấm do Update xử lý)
    void Layout()
    {
        fontSize = Mathf.Max(18, (int)(Screen.height * 0.022f));
        float pad = fontSize * 0.6f;
        topBar = fontSize * 4.6f;
        bottomBar = fontSize * 2.8f + pad;
        float y = Screen.height - fontSize * 2.8f - pad;
        float w = Screen.width - 4 * pad;
        btnScale = new Rect(pad, y, w * 0.40f, fontSize * 2.8f);
        btnReset = new Rect(2 * pad + w * 0.40f, y, w * 0.28f, fontSize * 2.8f);
        btnPlanes = new Rect(3 * pad + w * 0.68f, y, w * 0.32f, fontSize * 2.8f);
    }

    static Vector2 ToGui(Vector2 screenPos) => new Vector2(screenPos.x, Screen.height - screenPos.y);

    bool OverUi(Vector2 screenPos)
    {
        var g = ToGui(screenPos);
        return g.y < topBar || g.y > Screen.height - bottomBar;
    }

    string StatusText()
    {
        if (ARSession.state == ARSessionState.Unsupported) return "Máy này không hỗ trợ ARCore.";
        if (ARSession.state == ARSessionState.NeedsInstall || ARSession.state == ARSessionState.Installing)
            return "Cần cài Google Play Services for AR.";
        if (ARSession.state != ARSessionState.SessionTracking)
            return "Đang khởi động AR… cầm máy và di chuyển chậm.";
        if (model == null)
            return planeManager.trackables.count == 0
                ? "Quét chậm quanh mặt bàn để tìm mặt phẳng."
                : "Chạm vào mặt phẳng để đặt mô hình.";
        return "Kéo 1 ngón: xoay · Chụm 2 ngón: phóng to/thu nhỏ · Chạm mặt phẳng: dời chỗ";
    }

    void OnGUI()
    {
        var lab = new GUIStyle(GUI.skin.label) { fontSize = fontSize, wordWrap = true, richText = true };
        lab.normal.textColor = Color.white;
        var box = new GUIStyle(GUI.skin.box) { fontSize = fontSize, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        box.normal.textColor = Color.white;
        float pad = fontSize * 0.6f;

        GUI.Box(new Rect(0, 0, Screen.width, topBar), GUIContent.none);
        GUI.Label(new Rect(pad, pad * 0.6f, Screen.width - 2 * pad, fontSize * 1.6f), "<b>" + modelTitle + "</b>", lab);
        GUI.Label(new Rect(pad, fontSize * 2.0f, Screen.width - 2 * pad, fontSize * 2.6f), StatusText(), lab);

        string info = model == null
            ? "Chưa đặt mô hình"
            : $"Tỉ lệ x{scale:0.00}\ncao {modelHeight * scale * 100f:0.0} cm";
        GUI.Box(btnScale, info, box);
        GUI.Box(btnReset, "Tỉ lệ 1:1", box);
        GUI.Box(btnPlanes, planesVisible ? "Ẩn mặt phẳng" : "Hiện mặt phẳng", box);
    }
}
