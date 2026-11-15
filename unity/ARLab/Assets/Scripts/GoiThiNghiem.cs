using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using GLTFast;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>Một gói thí nghiệm (đọc từ experiment.json, xem docs/05). Chỉ lấy các trường app cần.</summary>
[Serializable]
public class GoiThiNghiem
{
    [Serializable] public class MonHoc { public int[] grades; public string topic; }
    [Serializable] public class Canh { public string staticModel; }
    [Serializable] public class NguonGoc { public string session, method, scaleSource, createdAt; }

    public string id, title, version, status;
    public MonHoc subject;
    public Canh scene;
    public NguonGoc provenance;

    [NonSerialized] public string url;       // thư mục gói, kết thúc bằng "/"
    [NonSerialized] public Texture2D anh;    // media/thumb.png (null nếu chưa có)
    [NonSerialized] public ThongSoThauKinh thauKinh;   // extras của nút ThauKinh trong GLB (null nếu không có)

    public bool CoMoHinh => !string.IsNullOrEmpty(scene?.staticModel);
}

/// <summary>Thông số quang học ghi trong extras của nút ThauKinh (pipeline Blender). Chỉ để hiển thị và vẽ kính; chưa đo.</summary>
[Serializable]
public class ThongSoThauKinh { public float f_m, n, R_m, ban_kinh_kinh_m; }

/// <summary>
/// Nạp gói thí nghiệm theo danh mục catalog.json (định dạng docs/05 mục 5).
/// Mọi file tải qua UnityWebRequest từ địa chỉ gốc <see cref="Goc"/>,
/// nên sau này đổi sang Firebase chỉ cần đổi Goc thành URL thư mục chứa catalog.json.
/// </summary>
public static class KhoGoi
{
    [Serializable] class DanhMuc { public MucDanhMuc[] experiments; }
    [Serializable] class MucDanhMuc { public string id, path; }
    [Serializable] class GltfJson { public GltfNut[] nodes; }
    [Serializable] class GltfNut { public string name; public ThongSoThauKinh extras; }

    // shortcut: gói đóng sẵn trong APK (StreamingAssets); GĐ 5 đổi sang URL máy chủ + bộ nhớ đệm.
    public static string Goc = DuongDanThanhUrl(Path.Combine(Application.streamingAssetsPath, "experiments"));

    static string DuongDanThanhUrl(string p) => p.Contains("://") ? p.Replace('\\', '/') : new Uri(p).AbsoluteUri;

    static async Task<byte[]> Tai(string url)
    {
        using var req = UnityWebRequest.Get(url);
        await req.SendWebRequest();
        if (req.result != UnityWebRequest.Result.Success)
            throw new IOException($"Không tải được {url}: {req.error}");
        return req.downloadHandler.data;
    }

    static string Chu(byte[] b) => System.Text.Encoding.UTF8.GetString(b);

    /// <summary>Đọc danh mục rồi từng experiment.json; gói có mô hình xếp trước.</summary>
    public static async Task<List<GoiThiNghiem>> NapDanhMuc()
    {
        var dm = JsonUtility.FromJson<DanhMuc>(Chu(await Tai(Goc + "/catalog.json")));
        var ds = new List<GoiThiNghiem>();
        foreach (var m in dm.experiments)
        {
            string thuMuc = Goc + "/" + m.path.TrimEnd('/') + "/";
            try
            {
                var g = JsonUtility.FromJson<GoiThiNghiem>(Chu(await Tai(thuMuc + "experiment.json")));
                g.url = thuMuc;
                ds.Add(g);
            }
            catch (Exception e) { Debug.LogWarning($"ARLAB goi {m.id}: bo qua ({e.Message})"); }
        }
        return ds.OrderBy(g => g.CoMoHinh ? 0 : 1).ToList();
    }

    public static async Task<Texture2D> NapAnh(GoiThiNghiem g)
    {
        if (g.anh != null) return g.anh;
        try
        {
            var tex = new Texture2D(2, 2);
            if (tex.LoadImage(await Tai(g.url + "media/thumb.png"))) g.anh = tex;
        }
        catch (IOException) { }   // gói chưa có ảnh: thẻ hiện nền trơn
        return g.anh;
    }

    /// <summary>Nạp scene.glb bằng glTFast, gắn các nút gốc vào <paramref name="cha"/>. Trả về GltfImport để giải phóng khi bỏ mô hình.</summary>
    /// <param name="hoan">Bộ hoãn của glTFast; để null khi chạy app, truyền UninterruptedDeferAgent khi kiểm tra ngoài Play Mode.</param>
    public static async Task<GltfImport> NapMoHinh(GoiThiNghiem g, Transform cha, IDeferAgent hoan = null)
    {
        if (!g.CoMoHinh) throw new InvalidOperationException($"Gói {g.id} chưa có mô hình");
        string url = g.url + g.scene.staticModel;
        var bytes = await Tai(url);
        g.thauKinh = DocThauKinh(bytes);
        var gltf = new GltfImport(deferAgent: hoan);
        if (!await gltf.Load(bytes, new Uri(url)) || !await gltf.InstantiateMainSceneAsync(cha))
        {
            gltf.Dispose();
            throw new IOException($"glTFast không nạp được {url}");
        }
        return gltf;
    }

    // GLB: 12 byte đầu tệp, rồi khối JSON (4 byte độ dài, 4 byte loại, nội dung). glTFast không đưa extras ra.
    static ThongSoThauKinh DocThauKinh(byte[] glb)
    {
        if (glb.Length < 20 || BitConverter.ToUInt32(glb, 0) != 0x46546C67) return null;   // "glTF"
        int n = (int)BitConverter.ToUInt32(glb, 12);
        var json = JsonUtility.FromJson<GltfJson>(System.Text.Encoding.UTF8.GetString(glb, 20, n));
        var nut = json.nodes?.FirstOrDefault(x => x.name == "ThauKinh");
        return nut?.extras != null && nut.extras.f_m > 0f ? nut.extras : null;
    }

    /// <summary>Hộp bao (toạ độ thế giới) của mọi Renderer bên dưới.</summary>
    public static Bounds HopBao(Transform t)
    {
        var rs = t.GetComponentsInChildren<Renderer>();
        if (rs.Length == 0) return new Bounds(t.position, Vector3.zero);
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }
}
