using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Xem mô hình 3D bằng camera thường (không dùng camera của máy):
/// kéo 1 ngón để xoay quanh mô hình, chụm 2 ngón (hoặc lăn chuột) để phóng to/thu nhỏ.
/// Nhận thao tác từ khung UI Toolkit và chỉ vẽ vào vùng của khung đó.
/// </summary>
[RequireComponent(typeof(Camera))]
public class XemMoHinh3D : MonoBehaviour
{
    const float GocNguocMin = -5f, GocNguocMax = 80f;   // giới hạn góc nhìn dọc (độ)
    const float DoXoay = 0.4f;                          // độ / điểm ảnh UI

    Camera cam;
    VisualElement khung;
    readonly Dictionary<int, Vector2> ngon = new Dictionary<int, Vector2>();
    Vector3 tam;
    float yaw = 210f, pitch = 18f, khoangCach = 0.5f, kcMin = 0.1f, kcMax = 2f;
    int matNaGoc;

    void Awake()
    {
        cam = GetComponent<Camera>();
        matNaGoc = cam.cullingMask;
    }

    public void Hien(bool bat) => cam.cullingMask = bat ? matNaGoc : 0;

    public void GanKhung(VisualElement k)
    {
        khung = k;
        k.RegisterCallback<GeometryChangedEvent>(_ => CapNhatVungVe());
        k.RegisterCallback<PointerDownEvent>(e =>
        {
            ngon[e.pointerId] = e.position;
            k.CapturePointer(e.pointerId);
        });
        k.RegisterCallback<PointerMoveEvent>(KhiKeo);
        k.RegisterCallback<PointerUpEvent>(e => Tha(e.pointerId));
        k.RegisterCallback<PointerCancelEvent>(e => Tha(e.pointerId));
        k.RegisterCallback<WheelEvent>(e => { khoangCach *= 1f + e.delta.y * 0.05f; DatCamera(); });
    }

    void Tha(int id)
    {
        ngon.Remove(id);
        if (khung.HasPointerCapture(id)) khung.ReleasePointer(id);
    }

    void KhiKeo(PointerMoveEvent e)
    {
        if (!ngon.TryGetValue(e.pointerId, out var cu)) return;
        Vector2 moi = e.position;
        if (ngon.Count == 1)
        {
            yaw += (moi.x - cu.x) * DoXoay;
            pitch += (moi.y - cu.y) * DoXoay;
        }
        else if (ngon.Count == 2)
        {
            Vector2 khac = default;
            foreach (var kv in ngon) if (kv.Key != e.pointerId) khac = kv.Value;
            float truoc = Vector2.Distance(cu, khac), sau = Vector2.Distance(moi, khac);
            if (sau > 1f) khoangCach *= truoc / sau;
        }
        ngon[e.pointerId] = moi;
        DatCamera();
    }

    /// <summary>Đặt tâm quay vào giữa mô hình và chọn khoảng cách vừa khung.</summary>
    public void NhinVao(Bounds b)
    {
        float r = Mathf.Max(b.extents.magnitude, 0.01f);
        tam = b.center;
        kcMin = r * 1.2f;
        kcMax = r * 6f;
        khoangCach = r * 2.8f;
        yaw = 210f;
        pitch = 18f;
        cam.nearClipPlane = r * 0.05f;
        cam.farClipPlane = r * 40f;
        DatCamera();
    }

    void DatCamera()
    {
        pitch = Mathf.Clamp(pitch, GocNguocMin, GocNguocMax);
        khoangCach = Mathf.Clamp(khoangCach, kcMin, kcMax);
        var q = Quaternion.Euler(pitch, yaw, 0f);
        transform.SetPositionAndRotation(tam - q * Vector3.forward * khoangCach, q);
    }

    // Camera chỉ vẽ phần màn hình nằm dưới khung 3D (toạ độ panel → toạ độ viewport, gốc ở dưới).
    void CapNhatVungVe()
    {
        var man = khung.panel?.visualTree.worldBound ?? default;
        var k = khung.worldBound;
        if (man.width < 1f || man.height < 1f || k.height < 1f) return;
        cam.rect = new Rect(k.x / man.width, 1f - k.yMax / man.height, k.width / man.width, k.height / man.height);
    }
}
