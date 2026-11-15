using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Lưu ảnh PNG vào Thư viện ảnh của máy (Android: thư mục Pictures/AR Lab), không cần plugin.
/// Android 10+ (API 29+): MediaStore, không cần quyền. Android 8–9: cần quyền ghi bộ nhớ, xin ở lần chụp đầu.
/// Editor: lưu vào Application.persistentDataPath.
/// </summary>
public static class ChupAnh
{
    const string ThuMuc = "AR Lab";

    /// <returns>Câu báo kết quả để hiện cho người dùng.</returns>
    public static string Luu(byte[] png, string ten)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using var phienBan = new AndroidJavaClass("android.os.Build$VERSION");
            return phienBan.GetStatic<int>("SDK_INT") >= 29 ? LuuMediaStore(png, ten) : LuuCu(png, ten);
        }
        catch (Exception e)
        {
            Debug.LogError("ARLAB chup: " + e);
            return "Không lưu được ảnh";
        }
#else
        string duongDan = Path.Combine(Application.persistentDataPath, ten);
        File.WriteAllBytes(duongDan, png);
        Debug.Log("ARLAB chup: " + duongDan);
        return "Đã lưu ảnh";
#endif
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    static string LuuMediaStore(byte[] png, string ten)
    {
        var hoatDong = UnityEngine.Android.AndroidApplication.currentActivity;   // đối tượng dùng chung, không giải phóng
        using var resolver = hoatDong.Call<AndroidJavaObject>("getContentResolver");
        using var giaTri = new AndroidJavaObject("android.content.ContentValues");
        giaTri.Call("put", "_display_name", ten);
        giaTri.Call("put", "mime_type", "image/png");
        giaTri.Call("put", "relative_path", "Pictures/" + ThuMuc);
        using var anh = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
        using var noiLuu = anh.GetStatic<AndroidJavaObject>("EXTERNAL_CONTENT_URI");
        using var uri = resolver.Call<AndroidJavaObject>("insert", noiLuu, giaTri);
        if (uri == null) return "Không lưu được ảnh";
        using var luong = resolver.Call<AndroidJavaObject>("openOutputStream", uri);
        luong.Call("write", (sbyte[])(Array)png);
        luong.Call("close");
        return "Đã lưu vào Thư viện ảnh (AR Lab)";
    }

    static string LuuCu(byte[] png, string ten)
    {
        const string quyen = UnityEngine.Android.Permission.ExternalStorageWrite;
        if (!UnityEngine.Android.Permission.HasUserAuthorizedPermission(quyen))
        {
            UnityEngine.Android.Permission.RequestUserPermission(quyen);
            return "Cho phép lưu ảnh rồi bấm Chụp lần nữa";
        }
        // ghi tạm vào bộ đệm của app rồi để MediaStore chép vào Thư viện ảnh
        string tam = Path.Combine(Application.temporaryCachePath, ten);
        File.WriteAllBytes(tam, png);
        var hoatDong = UnityEngine.Android.AndroidApplication.currentActivity;   // đối tượng dùng chung, không giải phóng
        using var resolver = hoatDong.Call<AndroidJavaObject>("getContentResolver");
        using var anh = new AndroidJavaClass("android.provider.MediaStore$Images$Media");
        string uri = anh.CallStatic<string>("insertImage", resolver, tam, ten, ThuMuc);
        File.Delete(tam);
        if (string.IsNullOrEmpty(uri)) return "Không lưu được ảnh";
        return "Đã lưu vào Thư viện ảnh (AR Lab)";
    }
#endif
}
