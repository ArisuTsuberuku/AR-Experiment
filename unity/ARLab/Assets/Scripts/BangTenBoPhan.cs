using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Tên hiển thị của bộ phận theo tên nút trong GLB.
/// shortcut: bảng đặt trong app vì định dạng gói chưa có trường này (đổi schema chờ thầy quyết ⏸);
/// khi có trường tên bộ phận trong experiment.json thì đọc từ gói và bỏ bảng này.
/// </summary>
public static class BangTenBoPhan
{
    static readonly Dictionary<string, string> s_Ten = new Dictionary<string, string>
    {
        // den-soi-dot
        { "DeNhua", "Đế nhựa bakelit" },
        { "Gioang", "Gioăng cao su" },
        { "TruDan", "Trụ dẫn điện" },
        { "CocNgan", "Cốc ngăn" },
        { "SoiToc", "Sợi tóc" },
        { "Moc", "Móc giữ sợi tóc" },
        { "DayDo", "Dây đỡ" },
        { "Chot", "Chốt" },
        // thau-kinh-f200
        { "DeKep", "Đế kẹp" },
        { "ThauKinh", "Thấu kính trên que" },
        { "Que", "Que thép" },
        { "KepNhua", "Kẹp chữ V" },
        { "DiaDen", "Đĩa đen" },
        { "Kinh", "Mặt kính hội tụ" },
        { "NhanNen", "Nhãn ghi tiêu cự" },
    };

    // "SoiToc_0", "Chot_0.091", "DinhTan.001" → "SoiToc", "Chot", "DinhTan"
    static string Goc(string tenNut) => Regex.Replace(tenNut, @"([_.]\d+)+$", "");

    public static string Ten(string tenNut) => s_Ten.TryGetValue(Goc(tenNut), out var t) ? t : null;

    /// <summary>Các bộ phận có tên trong bảng, mỗi tên một lần, theo thứ tự nút trong mô hình.</summary>
    public static List<(string ten, Transform nut)> LietKe(Transform goc) =>
        goc.GetComponentsInChildren<Transform>()
           .Where(t => t != goc)
           .Select(t => (ten: Ten(t.name), nut: t))
           .Where(x => x.ten != null)
           .GroupBy(x => x.ten).Select(nh => nh.First())
           .ToList();
}
