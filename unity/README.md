# Unity — app AR Lab

Dự án: `unity/ARLab`. Chỉ mở từ `D:\Dev\AR-Experiment\unity\ARLab` (đường dẫn không dấu, ngoài OneDrive).

| Hạng mục | Giá trị |
|---|---|
| Unity | 6000.6.9f1 (Unity 6.6), cài ở `D:\Unity`; nâng lên 6.7 LTS khi phát hành |
| Render pipeline | URP (mẫu `urp-blank`); renderer `Assets/Settings/Mobile_Renderer.asset` và `PC_Renderer.asset` đã có `ARBackgroundRendererFeature` |
| Gói | AR Foundation 6.6.2, Google ARCore XR Plugin 6.6.2, glTFast 6.21.0, Input System 1.20.1 |
| Android | `vn.arexperiment.arlab`, API tối thiểu 26, IL2CPP ARM64, OpenGL ES 3, màn hình dọc |
| Cảnh | `Assets/Scenes/ARLab.unity`: AR Session, XR Origin (dò mặt phẳng ngang, raycast, neo), AR Lab |

## Mã nguồn

| File | Việc |
|---|---|
| `Assets/Scripts/ARLabPlacer.cs` | Chạy trên điện thoại: chạm mặt phẳng để đặt mô hình, kéo 1 ngón để xoay, chụm 2 ngón để phóng to / thu nhỏ, nút "Tỉ lệ 1:1", ẩn / hiện mặt phẳng |
| `Assets/Editor/ARLabSetup.cs` | Thiết lập Player, gán ARCore, gắn nền camera AR vào URP, dựng lại cảnh, build APK. Có menu **AR Lab** khi mở Editor |
| `Assets/Models/den-soi-dot/scene.glb` | Bản chép của `experiments/den-soi-dot/model/scene.glb` (glTFast nhập khi build) |

## Build APK từ dòng lệnh

```powershell
Copy-Item experiments\den-soi-dot\model\scene.glb unity\ARLab\Assets\Models\den-soi-dot\scene.glb -Force
& 'D:\Unity\6000.6.9f1\Editor\Unity.exe' -batchmode -projectPath D:\Dev\AR-Experiment\unity\ARLab `
  -buildTarget Android -executeMethod ARLabSetup.SetupAndBuild `
  -logFile D:\Dev\AR-Experiment\work\logs\build.log
```

Mã thoát 0: APK ở `work\build\ARLab.apk`. Tìm dòng `ARLAB_BUILD` trong nhật ký để xem kết quả. Lần đầu lâu (Gradle tải thư viện Android); các lần sau nhanh hơn. Không chạy khi Editor đang mở chính dự án này.

## Cài lên điện thoại

1. Điện thoại Android có hỗ trợ ARCore; bật *Tuỳ chọn nhà phát triển* → *Gỡ lỗi USB*; cài *Google Play Services for AR*.
2. Cắm cáp, chấp nhận "Cho phép gỡ lỗi USB".
3. `D:\Unity\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe install -r work\build\ARLab.apk`

Không có cáp: chép `ARLab.apk` sang điện thoại (Zalo, Drive…) rồi mở để cài, cho phép "cài ứng dụng không rõ nguồn".

## Giới hạn của bản 0.1.0

- Mô hình được gắn sẵn trong APK. Tải gói thí nghiệm từ Firebase (không cài lại app) là việc của GĐ 5.
- Kích thước mô hình là ước lượng (đường kính đáy đế 12 cm), chưa đo.
- Thư mục sinh ra của Unity (`Library`, `Temp`, `Logs`, `UserSettings`…) và `*.csproj`, `*.slnx` không vào git.
