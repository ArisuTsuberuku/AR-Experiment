using UnityEngine;

/// <summary>
/// Gắn vào mặt kính: thay vật liệu "ThuyTinh" bằng shader ARLab/KinhKhucXa và mỗi khung hình
/// cập nhật quang tâm, tiêu cự (theo tỉ lệ hiện tại của mô hình) và khoảng cách tới nền phía sau.
/// </summary>
public class ThauKinhKhucXa : MonoBehaviour
{
    // shortcut: một khoảng cách nền cho cả mặt kính; màn AR đo bằng raycast tới mặt phẳng, không có thì giả định 1,5 m.
    // Khi bật ARCore Depth thì lấy độ sâu thật từng điểm.
    public const float NenMacDinh = 1.5f;
    public static float KhoangCachNen = NenMacDinh;

    Transform tamQuang;
    Material vatLieu;
    float tieuCu;

    static readonly int s_Tam = Shader.PropertyToID("_TamQuangWS");
    static readonly int s_F = Shader.PropertyToID("_TieuCu");
    static readonly int s_D = Shader.PropertyToID("_KhoangCachNen");

    /// <summary>Tìm mặt kính (vật liệu tên ThuyTinh) trong mô hình và gắn hiệu ứng. Trả về null nếu mô hình không có thấu kính.</summary>
    public static ThauKinhKhucXa Gan(Transform moHinh, Shader shader, ThongSoThauKinh ts)
    {
        if (ts == null || shader == null) return null;
        Transform tam = null;
        foreach (var t in moHinh.GetComponentsInChildren<Transform>()) if (t.name == "TamQuang") tam = t;
        if (tam == null) return null;

        ThauKinhKhucXa kq = null;
        foreach (var r in moHinh.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            bool doi = false;
            for (int i = 0; i < mats.Length; i++)
            {
                if (mats[i] == null || !mats[i].name.StartsWith("ThuyTinh")) continue;
                doi = true;
                if (kq == null)
                {
                    kq = r.gameObject.AddComponent<ThauKinhKhucXa>();
                    kq.tamQuang = tam;
                    kq.tieuCu = ts.f_m;
                    kq.vatLieu = new Material(shader) { name = "KinhKhucXa" };
                }
                mats[i] = kq.vatLieu;
            }
            if (doi)
            {
                r.sharedMaterials = mats;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;   // kính trong gần như không đổ bóng
            }
        }
        return kq;
    }

    void LateUpdate()
    {
        vatLieu.SetVector(s_Tam, tamQuang.position);
        vatLieu.SetFloat(s_F, tieuCu * Mathf.Abs(tamQuang.lossyScale.x));   // phóng mô hình thì tiêu cự đổi cùng tỉ lệ
        vatLieu.SetFloat(s_D, KhoangCachNen);
    }

    void OnDestroy()
    {
        if (vatLieu != null) Destroy(vatLieu);
    }
}
