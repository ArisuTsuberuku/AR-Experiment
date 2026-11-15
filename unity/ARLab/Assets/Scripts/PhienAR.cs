using System.Collections;
using UnityEngine;
using UnityEngine.XR.Management;

/// <summary>
/// Bật / tắt AR theo yêu cầu: chỉ nạp XR (và mở camera của máy) khi vào màn AR, tắt hẳn khi rời.
/// Bật:  InitializeLoader → StartSubsystems → tạo khung AR (AR Session + XR Origin) từ prefab.
/// Tắt:  huỷ khung AR → StopSubsystems → DeinitializeLoader.
/// Khung AR được tạo mới mỗi lần bật để các thành phần AR Foundation lấy subsystem mới.
/// </summary>
public class PhienAR : MonoBehaviour
{
    public GameObject mauKhungAR;   // prefab do ARLabSetup dựng
    public GameObject cameraXem;    // camera xem 3D thường, tắt khi AR chạy

    public GameObject Khung { get; private set; }
    public bool DangChay => Khung != null;
    public string Loi { get; private set; }

    static XRManagerSettings QuanLy =>
        XRGeneralSettings.Instance != null ? XRGeneralSettings.Instance.Manager : null;

    public IEnumerator Bat()
    {
        Loi = null;
        if (DangChay) yield break;
        var m = QuanLy;
        if (m == null) { Loi = "App chưa cấu hình XR cho nền tảng này."; yield break; }
        if (m.activeLoader == null) yield return m.InitializeLoader();
        if (m.activeLoader == null) { Loi = "Không khởi động được AR trên máy này."; yield break; }

        m.StartSubsystems();
        cameraXem.SetActive(false);
        Khung = Instantiate(mauKhungAR);
        Debug.Log("ARLAB ar: bat (" + m.activeLoader.name + ")");
    }

    public void Tat()
    {
        if (Khung != null)
        {
            Khung.SetActive(false);   // các thành phần AR dừng ngay, trước khi huỷ subsystem
            Destroy(Khung);
            Khung = null;
        }
        if (cameraXem != null) cameraXem.SetActive(true);

        var m = QuanLy;
        if (m != null && m.activeLoader != null)
        {
            m.StopSubsystems();
            m.DeinitializeLoader();
            Debug.Log("ARLAB ar: tat");
        }
    }

    void OnDestroy() => Tat();
}
