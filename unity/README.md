# Unity — app AR Lab

Dự án: `unity/ARLab`. Chỉ mở từ `D:\Dev\AR-Experiment\unity\ARLab` (đường dẫn không dấu, ngoài OneDrive).

| Hạng mục | Giá trị |
|---|---|
| Unity | 6000.6.9f1 (Unity 6.6), cài ở `D:\Unity`; nâng lên 6.7 LTS khi phát hành |
| Render pipeline | URP (mẫu `urp-blank`); renderer `Assets/Settings/Mobile_Renderer.asset` và `PC_Renderer.asset` đã có `ARBackgroundRendererFeature`. `ARLabSetup` bật Opaque Texture (kính khúc xạ), khoảng cách bóng 3 m, bóng mềm, nhận bóng cho vật trong suốt |
| Gói | AR Foundation 6.6.2, Google ARCore XR Plugin 6.6.2, glTFast 6.21.0, Input System 1.20.1 |
| Android | `vn.arexperiment.arlab`, API tối thiểu 26, IL2CPP ARM64, OpenGL ES 3, màn hình dọc |
| Cảnh | `Assets/Scenes/ARLab.unity` (do `ARLabSetup` dựng lại): đèn, Camera Xem3D, MoHinh, Giao dien (UIDocument). Mở app **không** nạp XR |

## Mã nguồn

| File | Việc |
|---|---|
| `Assets/Scripts/GoiThiNghiem.cs` | Lớp nạp gói `KhoGoi`: đọc `catalog.json` → `experiment.json` → `media/thumb.png` → `model/scene.glb` (glTFast, lúc chạy). Mọi file tải qua `UnityWebRequest` từ địa chỉ gốc `KhoGoi.Goc`; GĐ 5 chỉ cần đổi `Goc` sang URL máy chủ |
| `Assets/Scripts/XemMoHinh3D.cs` | Xem 3D bằng camera thường: kéo để xoay, chụm hoặc lăn chuột để phóng to; có giới hạn góc và khoảng cách |
| `Assets/Scripts/BangTenBoPhan.cs` | Tên hiển thị của bộ phận theo tên nút GLB (tạm đặt trong app, định dạng gói chưa có trường này) |
| `Assets/UI/` | Giao diện UI Toolkit: `ARLab.uxml`, `ARLab.uss`, theme `ARLab.tss`, `AppARLab.cs`; phông Be Vietnam Pro (OFL) trong `Fonts/` |
| `Assets/Scripts/PhienAR.cs` | Nạp XR khi vào màn AR, gỡ hẳn khi rời |
| `Assets/UI/ManHinhAR.cs` | Màn AR: vòng ngắm, đặt / dời / xoay / chụm, thanh công cụ (Tỉ lệ 1:1 · Chú thích · Tách rời · Chụp · Thông tin), kéo dọc trên thấu kính để nâng que 0–6 cm, ước lượng ánh sáng ARCore, tự tắt sau 2 phút |
| `Assets/Scripts/ThauKinhKhucXa.cs` + `Assets/Shaders/KinhKhucXa.shader` | Kính thấu kính mỏng: nhìn qua kính thấy nền phóng / lật theo f (đọc từ `extras` của nút `ThauKinh` trong GLB) |
| `Assets/Shaders/BongDo.shader` | Mặt phẳng trong suốt dưới mô hình chỉ hiện bóng đổ |
| `Assets/Scripts/ChupAnh.cs` | Lưu ảnh chụp vào Thư viện ảnh (Android 10+: MediaStore; 8–9: xin quyền ghi bộ nhớ) |
| `Assets/Editor/ARLabSetup.cs` | Thiết lập Player, gán ARCore, gắn nền camera AR vào URP, chép gói thí nghiệm, giữ biến thể shader glTFast, dựng cảnh, build APK, tự kiểm tra. Có menu **AR Lab** khi mở Editor |

Thư mục sinh ra, không vào git (xem `unity/ARLab/.gitignore`): `Assets/StreamingAssets/experiments/` (bản chép các gói + `catalog.json`), `Assets/Models/<mã>/scene.glb` và `Assets/Resources/BienTheShader/` (vật liệu mẫu để bản build giữ shader của glTFast).

## Build APK từ dòng lệnh

`ARLabSetup` tự chép mọi gói trong `experiments/` vào app, không cần chép tay.

```powershell
& 'D:\Unity\6000.6.9f1\Editor\Unity.exe' -batchmode -projectPath D:\Dev\AR-Experiment\unity\ARLab `
  -buildTarget Android -executeMethod ARLabSetup.SetupAndBuild `
  -logFile D:\Dev\AR-Experiment\work\logs\build.log
```

Mã thoát 0: APK ở `work\build\ARLab.apk`. Tìm dòng `ARLAB_BUILD` trong nhật ký để xem kết quả. Lần đầu lâu (Gradle tải thư viện Android); các lần sau nhanh hơn. Không chạy khi Editor đang mở chính dự án này.

Tự kiểm tra lớp nạp gói (không build, khoảng 1 phút): thay `ARLabSetup.SetupAndBuild` bằng `ARLabSetup.KiemTra`, tìm dòng `ARLAB_KIEMTRA OK`.

Tự kiểm tra AR theo yêu cầu (Play Mode + XR Simulation, khoảng 3 phút): dùng `ARLabSetup.KiemTraAR`, tìm dòng `ARLAB_KIEMTRA_AR OK`. Lệnh kiểm tra: mở app chưa nạp XR; vào màn AR thì nạp; rời màn thì gỡ hẳn (2 lượt).

Chụp giao diện để xem tận mắt (Play Mode → PNG, khoảng 3 phút): dùng `ARLabSetup.ChupGiaoDien`. Lệnh lưu `work/logs/18-chup-*.png` (thư viện, chi tiết, thấu kính, AR trong XR Simulation, tách rời + chú thích) và tự kiểm tra chiều ảnh qua kính bằng tấm hình mẫu đặt sau kính (d = 0,1 m cùng chiều; d = 0,6 m ngược chiều); tìm dòng `ARLAB_CHUP xong 0`. **Đổi giao diện hay cách dựng cảnh thì chạy lệnh này và xem ảnh trước khi build.**

XR nạp theo yêu cầu (`Assets/Scripts/PhienAR.cs`): "Initialize XR on Startup" tắt cho cả Android (ARCore) lẫn Standalone (XR Simulation, chỉ để thử trong Editor). Đổi cài đặt ARCore thì xoá `Library/Bee/Android/Prj/IL2CPP/Gradle` rồi build lại.

## Thử trong Editor

Mở dự án, chạy menu **AR Lab → 1. Thiết lập dự án và dựng cảnh** (để chép gói vào `StreamingAssets`), mở `Assets/Scenes/ARLab.unity`, bấm Play. Đặt cửa sổ Game theo khổ dọc (ví dụ 390 × 844). Kéo chuột để xoay, lăn chuột để phóng to; phím Esc thay nút Back.

## Cài lên điện thoại

1. Điện thoại Android có hỗ trợ ARCore; bật *Tuỳ chọn nhà phát triển* → *Gỡ lỗi USB*; cài *Google Play Services for AR*.
2. Cắm cáp, chấp nhận "Cho phép gỡ lỗi USB".
3. `D:\Unity\6000.6.9f1\Editor\Data\PlaybackEngines\AndroidPlayer\SDK\platform-tools\adb.exe install -r work\build\ARLab.apk`

Không có cáp: chép `ARLab.apk` sang điện thoại (Zalo, Drive…) rồi mở để cài, cho phép "cài ứng dụng không rõ nguồn".

## Giới hạn của bản 0.2.0

- Gói thí nghiệm đóng sẵn trong APK (`StreamingAssets`). Tải gói từ Firebase (không cài lại app) là việc của GĐ 5.
- Kính khúc xạ dùng một khoảng cách nền cho cả mặt kính: đo bằng raycast tới mặt phẳng AR phía sau kính, không có thì giả định 1,5 m. Không làm nhoè, không tán sắc, chưa có lớp phủ tia sáng.
- Kích thước mô hình là ước lượng (đường kính đáy đế 12 cm), chưa đo.
- Thư mục sinh ra của Unity (`Library`, `Temp`, `Logs`, `UserSettings`…) và `*.csproj`, `*.slnx` không vào git.
