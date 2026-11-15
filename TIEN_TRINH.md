# AR Experiment — Nhật ký tiến trình

Mọi agent cập nhật file này khi mở và đóng phiên (xem `AGENTS.md`). Chỉ thêm, không xoá mục cũ. Mục mới nhất của nhật ký nằm trên cùng.

## Đang làm

| Agent | Việc | Thư mục đang ghi | Từ |
|---|---|---|---|
| — | — | — | — |

## Tiếp

1. **Thầy:** cài lại `work/build/ARLab.apk` (15/11 22:22, 40,6 MB); xem chất liệu đế và cột dẫn dưới ánh sáng thật; báo lại chỗ còn "giả".
2. **Thầy:** đo đèn (đường kính đáy đế, chiều cao tới đỉnh dây đỡ) và thấu kính (đường kính đĩa đen, đường kính phần kính, chiều cao đế).
3. **Thầy:** góp ý mockup app v0.2 trên canvas Design "AR Lab — Mockup giao diện app" (5 màn hình; màn 5 kéo được thanh trượt).
4. **Thầy:** thử APK v0.2 bước 3 trên điện thoại (thanh công cụ AR, tách rời thấu kính, nhìn qua kính, Chụp, bóng đổ); trả lời 2 câu hỏi ở mục 6 phiếu app v0.2. Cowork đóng phiếu sau khi thầy thử.
5. **Thầy quyết định ⏸:** thêm thông số quang học (f, n) và lớp phủ tia sáng vào định dạng gói (đổi schema).
6. **Cowork / Claude Code:** chạy lại 2 script Blender với số đo thật; build lại APK.
7. **Thầy + Cowork:** quay thử rơi tự do bằng 1 điện thoại, chép vào `02_Video_Goc/roi-tu-do/<ngày>_1/` kèm `ghi-chu.md`; Cowork viết `docs/specs/roi-tu-do.md`.
8. **Claude Code:** viết `pipeline/` cho 1 máy quay theo spec (GĐ 2).
9. **Thầy:** cài ffmpeg và `pip install scipy pandas` (GĐ 2).
10. **Thầy quyết định ⏸:** dự án Firebase tạo mới hay dùng chung; repo công khai hay riêng tư.

## Nhật ký

### 2026-11-16 (03:00) — App v0.2 bước 3/3 xong: thanh công cụ AR, thấu kính 2 cụm, kính khúc xạ, ánh sáng + bóng (Claude Code)

Thầy đổi phạm vi: làm thêm **hiệu ứng quang học khi nhìn qua thấu kính** cùng bước 3 (phiếu ghi hoãn). Lớp phủ tia sáng (màn 5) vẫn để sau.

Đã làm:

- `ManHinhAR`: kéo xoay, chụm phóng, chạm mặt phẳng dời chỗ; thanh công cụ Tỉ lệ 1:1 · Chú thích (nhãn bám bộ phận) · Tách rời (nâng `ThauKinh` 4 cm / 0,4 s; kéo dọc 0–6 cm) · Chụp (vào Thư viện ảnh) · Thông tin; ước lượng ánh sáng ARCore HDR.
- Kính `ARLab/KinhKhucXa`: nhìn qua kính thấy nền phóng / lật theo thấu kính mỏng, k = 1 − D·L / (f·(L + D)); f, n đọc từ `extras` của nút `ThauKinh` trong GLB.
- Bóng `ARLab/BongDo`: mặt phẳng trong suốt dưới mô hình. URP: bật Opaque Texture, bóng 3 m, bóng mềm, **nhận bóng cho vật trong suốt** (renderer Mobile đang tắt — nguyên nhân bóng không hiện lúc đầu).
- Lệnh `ARLabSetup.ChupGiaoDien` chụp giao diện thành PNG + tự kiểm chiều ảnh qua kính bằng tấm hình mẫu (d = 0,1 m cùng chiều; d = 0,6 m ngược chiều).
- Mục 6 phiếu `docs/handoff/2026-11-15-app-v02-giao-dien-va-thau-kinh.md` đã ghi; `unity/README.md` cập nhật.

Kiểm tra: `ARLAB_KIEMTRA OK` (27), `ARLAB_KIEMTRA_AR OK` (33), `ARLAB_CHUP xong 0` + khúc xạ `DUNG` 2 trường hợp (32), `ARLAB_BUILD Succeeded` 0 lỗi, APK 35,6 MB, ARCore `required`, không có `depth` (34). **Chưa thử bản này trên điện thoại** (adb báo unauthorized, chờ thầy bấm cho phép).

### 2026-11-16 (02:35) — Thầy xác nhận app v0.2 (bước 1–2) chạy trên điện thoại (Claude Code)

Thầy mở bản cài lúc 02:30 trên Sony SOG06: giao diện hiện, chạy tốt. Còn chờ bước 3 của phiếu.

### 2026-11-16 (02:30) — Sửa lỗi app v0.2 mở ra màn đen sau logo Unity (Claude Code)

Thầy cài APK bước 2 lên điện thoại (Sony SOG06, Android 14): sau "Made by Unity" màn hình đen.

Tìm nguyên nhân:

- `work/logs/19-logcat-man-den.log` (adb logcat): không có lỗi hay ngoại lệ; app vẫn chạy nhưng không vẽ gì.
- Thêm `ARLabSetup.ChupGiaoDien`: chạy cảnh trong Play Mode, vẽ giao diện vào ảnh PNG và ghi trạng thái phần tử. Lần đầu chạy, `UIDocument.panelSettings` = null; file cảnh lưu `m_PanelSettings: {fileID: 0}`.
- **Nguyên nhân 1:** `BuildScene` nạp PanelSettings trước `EditorSceneManager.NewScene(... Single)`. Mở cảnh mới thì Unity gỡ các asset không được tham chiếu, nên cảnh lưu ra mà không có PanelSettings → UI Toolkit không vẽ. Sửa: nạp asset sau `NewScene`; `BuildScene` báo lỗi nếu cảnh lưu thiếu PanelSettings.
- **Nguyên nhân 2:** theme riêng `ARLab.tss` thiếu quy tắc kéo giãn gốc UIDocument (theme mặc định của Unity vẫn có), nên giao diện cao 0. Sửa: thêm `.unity-ui-document__root` (absolute, giãn 4 cạnh). Thêm căn giữa chữ cho nút, lề cho nội dung thư viện.

Kiểm tra:

- `work/logs/24-chup.log` + ảnh `work/logs/18-chup-thu-vien.png`, `18-chup-chi-tiet.png`: thư viện hiện đủ 3 thẻ có ảnh, phông tiếng Việt đúng; màn chi tiết hiện mô hình đèn, 8 bộ phận, nút AR.
- `work/logs/25-build-sua-man-den.log`: `ARLAB_BUILD Succeeded`, 0 lỗi. Đã cài lên điện thoại bằng `adb install -r` (02:30); chưa thấy thầy mở thử.

Bài học: kiểm tra phải gồm **nhìn thấy giao diện** (ảnh chụp), không chỉ log của lớp nạp gói và XR.

### 2026-11-16 (02:20) — App v0.2 bước 2/3 xong: kiểm tra và build sạch (Claude Code)

Thầy đã đăng nhập lại Unity Hub; Unity chạy nền được.

- Sửa `ARLabSetup.KiemTraAR`: dự án tắt Reload Domain khi vào Play, nên đăng ký trình theo dõi ngay khi gọi lệnh (lượt đầu bị treo trong Play Mode, `work/logs/15-kiemtra-ar.log`, đã dừng).
- `work/logs/16-kiemtra-ar.log`: `ARLAB_KIEMTRA_AR OK` — mở app XR chưa nạp, không có ARSession; vào màn AR: SimulationLoader nạp, `ARSession.state = SessionTracking`, camera xem 3D tắt; rời màn: XR gỡ hẳn, camera xem 3D bật lại; đúng cả 2 lượt.
- Xoá `Library/Bee/Android/Prj/IL2CPP/Gradle`, build sạch: `work/logs/17-build-v02-buoc2.log` — `ARLAB_BUILD Succeeded`, 0 lỗi, 1 phút 37 giây. APK 35,3 MB; `aapt`: 0.2.0 (mã 2), minSdk 26, ARCore `required`, không có `com.google.ar.core.depth`, `SkipPermissionsDialog` = true, không có tệp XR Simulation trong APK.

Chưa thử:

- Trên điện thoại: quyền camera chỉ hỏi lần đầu bấm "Đặt vào không gian (AR)", vòng ngắm, "Đặt ở đây", tự tắt sau 2 phút, app vào nền.
- Trong XR Simulation lượt kiểm tra không thấy mặt phẳng nào sau 5 giây (camera mô phỏng đứng yên), nên vòng ngắm và "Đặt ở đây" chưa được thử tự động. Thầy có thể thử trong Editor: Play → mở thí nghiệm → bấm AR, giữ chuột phải + W/A/S/D để đi lại trong phòng mô phỏng.

### 2026-11-16 (02:05) — App v0.2 bước 2/3: XR nạp theo yêu cầu, tự tắt sau 2 phút — đã viết mã, CHƯA kiểm tra (Claude Code)

Đã viết (chưa biên dịch được, xem phần Kẹt):

- `Assets/Scripts/PhienAR.cs`: vào màn AR thì `InitializeLoader` → `StartSubsystems` → tạo khung AR (AR Session + XR Origin) từ prefab `Assets/Prefabs/KhungAR.prefab`; rời màn AR thì huỷ khung → `StopSubsystems` → `DeinitializeLoader`. Camera xem 3D tắt khi AR chạy.
- `Assets/UI/ManHinhAR.cs` + màn `manAR` trong `ARLab.uxml`: nút ×, chip trạng thái ("Đang tìm mặt bàn…", "Đã thấy mặt bàn"), vòng ngắm xanh lơ ở giữa màn hình, thẻ hướng dẫn với nút "Đặt ở đây" và dòng "Tự tắt camera sau 2 phút không thao tác". Sau 120 giây không chạm, hoặc khi app vào nền: tắt hẳn AR, hiện "Đã tắt camera để tiết kiệm pin" + nút "Bật lại camera". Nút Back của Android rời màn AR.
- `ARLabSetup`: Android dùng ARCore, Standalone dùng XR Simulation (chỉ để thử trong Editor), cả hai `InitManagerOnStart = false`; dựng prefab khung AR, vật liệu vòng ngắm; thêm `ARLabSetup.KiemTraAR` (Play Mode + XR Simulation, bật/tắt AR 2 lượt, kiểm tra XR được gỡ hẳn).
- Bỏ `ARLabPlacer.cs` (IMGUI của bản 0.1); xoay, chụm, tỉ lệ 1:1 sẽ viết lại trong `ManHinhAR` ở bước 3.

Kẹt: Unity chạy nền thoát mã 198 — "No valid Unity Editor license found" ("Access token is unavailable"), 2 lần (`work/logs/14-kiemtra-ar.log`). Phiên đăng nhập Unity Hub hết hạn; tối 15/11 vẫn chạy được.

Tiếp: thầy mở Unity Hub, đăng nhập lại, kiểm tra giấy phép Personal còn hiệu lực → Claude Code chạy `ARLabSetup.KiemTraAR`, sửa lỗi nếu có, xoá `Library/Bee/Android/Prj/IL2CPP/Gradle`, build sạch, kiểm tra APK bằng `aapt`.

### 2026-11-15 (23:00) — App v0.2 bước 1/3: nạp gói, thư viện, xem 3D không bật camera (Claude Code)

Làm theo phiếu `docs/handoff/2026-11-15-app-v02-giao-dien-va-thau-kinh.md`; thầy đã duyệt kế hoạch 3 bước.

Đã làm:

- `Assets/Scripts/GoiThiNghiem.cs`: lớp nạp gói `KhoGoi` đọc `catalog.json` (theo docs/05 mục 5) → `experiment.json` → `media/thumb.png` → `model/scene.glb` (glTFast lúc chạy). Mọi file tải qua `UnityWebRequest` từ `KhoGoi.Goc`; GĐ 5 chỉ đổi `Goc` sang URL máy chủ.
- Giao diện UI Toolkit `Assets/UI/` (bỏ IMGUI ở màn mới): màn Thư viện (tìm kiếm, chip Tất cả / Điện / Quang / Cơ, thẻ lớn + lưới thẻ nhỏ, "Rơi tự do" hiện "Sắp có" vì gói chưa có mô hình) và màn Chi tiết 3D (chip "Xem 3D · camera tắt", tab Bộ phận / Thông số / Nguồn, chiều cao mô hình theo cm). Phông Be Vietnam Pro (OFL) trong `Assets/UI/Fonts/`.
- `XemMoHinh3D.cs`: camera thường, kéo xoay, chụm / lăn chuột phóng to, giới hạn góc −5…80° và khoảng cách 1,2–6 lần bán kính mô hình. Nút Back của Android quay về thư viện.
- `BangTenBoPhan.cs`: tên bộ phận theo tên nút GLB, đặt tạm trong app (không đụng `experiment.json`).
- `ARLabSetup`: chép gói từ `experiments/` vào `StreamingAssets`, giữ biến thể shader glTFast bằng vật liệu mẫu trong `Resources/BienTheShader`, PanelSettings 390 × 844, dựng cảnh mới không có XR, tắt "Initialize XR on Startup", phiên bản 0.2.0 (mã 2). Thêm `ARLabSetup.KiemTra` để tự kiểm tra lớp nạp gói ở chế độ nền.
- `unity/ARLab/.gitignore`: các thư mục chép sinh ra không vào git; bỏ `Assets/Models/den-soi-dot` khỏi git.

Kiểm tra:

- `work/logs/12-kiemtra-goi.log`: `ARLAB_KIEMTRA OK` — đọc 3 gói; đèn cao 15,1 cm (12 renderer, 8 bộ phận); thấu kính cao 17,8 cm (10 renderer, có `DeKep`, `ThauKinh`, `TamQuang`); ảnh thumb 512 × 512.
- `work/logs/13-build-v02-buoc1.log`: `ARLAB_BUILD Succeeded`, 0 lỗi, 2 phút 9 giây. APK 35,3 MB; `aapt`: versionName 0.2.0, minSdk 26, ARCore `required`, không có `com.google.ar.core.depth`, `SkipPermissionsDialog` = true.
- **Chưa** chạy Play Mode và chưa thử trên điện thoại: giao diện cần thầy xem (menu AR Lab → 1, mở cảnh `ARLab`, Play).

Ghi chú: nút "Đặt vào không gian (AR)" đang mờ, bật ở bước 2. Băng báo gói mới (nút "Tải") và nút chia sẻ của mockup chưa làm vì chưa có nguồn gói trên mạng.

### 2026-11-15 (22:45) — Thấu kính tách 2 cụm, phiếu giao app v0.2 (Claude Opus 5.5)

Thầy góp ý: thấu kính bị tụt vào đế; thật ra đế là **đế kẹp**, chân thấu kính là **que kim loại** cắm vào và được núm vặn giữ.

Đã làm:

- `thau_kinh_blender.py`: thêm gờ nổi và lỗ chữ nhật xuyên suốt cho đế; que thép gỉ to hơn (Ø 0,066 Dd), nhô 0,10 Dd (tham số `--que-nho`); khối kẹp đứng trên đầu que rồi mới loe chữ V. Xuất **2 nút gốc** `DeKep` và `ThauKinh` (gốc ở đáy que, nút con `TamQuang` ở quang tâm, `extras` ghi f, n, R). Thêm `--tach` để render ảnh tách rời.
- GLB mới: 801 kB, cao 0,178 m, quang tâm cao 11,8 cm. Báo cáo: mục 3b trong `docs/reports/2026-11-15-chat-lieu-thau-kinh-soat-hinh.md`.
- Mockup: thay ảnh thấu kính mới; số d′ không còn bị xuống dòng.
- Phiếu giao Claude Code: `docs/handoff/2026-11-15-app-v02-giao-dien-va-thau-kinh.md`.

Ghi chú: hiệu ứng ánh sáng qua thấu kính (khúc xạ nền, tia sáng, vệt hội tụ) thầy hoãn; hướng giải quyết ghi trong câu trả lời phiên này, chưa đưa vào phiếu.

### 2026-11-15 (khuya) — Chất liệu kim loại, mô hình thấu kính, mockup app v0.2 (Claude Opus 5.5)

Thầy đã thử APK trên điện thoại: đặt được mô hình, hiện "x1,00 · cao 15,1 cm". Thầy góp ý: giao diện đơn điệu; vào app bật camera ngay thì tốn pin; đế đèn trông "nhựa nhựa"; màu cột dẫn chưa đúng. Thầy chụp thêm 25 ảnh thấu kính hội tụ f = 200 mm vào `01_Anh_Da_Goc/Thau-kinh/`.

Đã làm:

- `pipeline/4_dung_3d/chat_lieu.py` (mới): thư viện chất liệu dùng chung, nướng ra 3 texture (màu, ORM, normal) để glTF chở được. Công thức: bakelit nâu, kim loại gỉ than chì, bạc xỉn, sơn xám mòn mép, thép gỉ, nhựa đen bụi.
- Đèn sợi đốt v0.3 (gói 0.2.0): đế 8 mặt có vát, vân bakelit + bụi mép; cột dẫn xám đen gỉ, chân bạc màu; dây đỡ be xám mờ. GLB 1 039 kB.
- `pipeline/4_dung_3d/thau_kinh_blender.py` (mới) và gói `experiments/thau-kinh-f200/` (bản nháp 0.1.0): đế sơn xám + núm vặn, cần thép gỉ, kẹp chữ V, đĩa đen lõm bậc, đinh tán đồng, nhãn "+200 mm"; kính hai mặt lồi R = 2(n − 1)f = 208 mm, tâm dày 3,4 mm. GLB 751 kB, kính `alphaMode = BLEND`.
- Soát hình: `docs/reports/2026-11-15-chat-lieu-thau-kinh-soat-hinh.md` — kết luận `can-sua` (đủ thử AR, chờ số đo).
- Build lại APK với đèn mới: `ARLAB_BUILD Succeeded`, 0 lỗi, 40,6 MB (tăng 9 MB do texture).
- Mockup app v0.2 trên canvas Design của Claude (riêng tư): 5 màn hình 390 × 844 — Thư viện, Chi tiết 3D (camera tắt), AR tìm mặt phẳng, AR đã đặt + chú thích + thanh công cụ, AR lớp phủ tia sáng thấu kính (thanh trượt d, tính d′ và k theo 1/f = 1/d + 1/d′).

Ghi chú:

- Bản render Cycles vẽ kính hơi trắng đục; GLB vẫn đặt kính trong suốt. Cần xem trên điện thoại.
- Thấu kính chưa có trong app (app hiện chỉ nạp 1 mô hình); để cho app v0.2.
- Định dạng gói chưa có chỗ ghi f, n; tạm ghi trong `provenance.method`. Thêm trường mới là đổi schema → chờ thầy quyết.
- Ảnh trong báo cáo chỉ có dụng cụ, không có học sinh (repo công khai).

### 2026-11-15 (tối) — Mô hình đèn sợi đốt và APK AR đầu tiên (Claude Opus 5.5)

Thầy chốt: **Unity 6.6** (6000.6.9f1) thay cho 6.3 LTS. Thầy chụp 26 ảnh mô hình cấu tạo đèn sợi đốt vào `01_Anh_Da_Goc/Den-soi-dot/`.

Đã làm:

- Đánh giá ảnh: đủ 360°, có ảnh trên và dưới, ánh sáng đều. Sợi tóc và dây đỡ mảnh 1–2 mm nên chọn dựng bằng script Blender thay cho quét ảnh / AI tạo 3D.
- `pipeline/4_dung_3d/den_soi_dot_blender.py`: dựng đế (8 khe vòm), gioăng, đĩa, 3 trụ, 2 ống sợi tóc, dây đỡ hình trái tim; xuất `experiments/den-soi-dot/model/scene.glb` (549 kB, ~21 000 tam giác); render 4 góc để so hình. Báo cáo: `docs/reports/2026-11-15-den-soi-dot-soat-hinh.md`.
- Gói `experiments/den-soi-dot/` (bản nháp 0.1.0, kích thước ước lượng D = 12 cm).
- Dự án `unity/ARLab` từ mẫu `urp-blank` (URP, vì Built-in bị khai tử từ 6.5); thêm AR Foundation 6.6.2, ARCore 6.6.2, glTFast 6.21.0.
- `Assets/Scripts/ARLabPlacer.cs`: chạm mặt phẳng để đặt, kéo xoay, chụm thu phóng, nút "Tỉ lệ 1:1", ẩn/hiện mặt phẳng, hiện chiều cao theo cm.
- `Assets/Editor/ARLabSetup.cs`: thiết lập Player, gán ARCore, gắn `ARBackgroundRendererFeature` vào URP, ARCore Depth = tuỳ chọn, dựng cảnh, build APK.
- Cập nhật `docs/00`, `01`, `04`, `AGENTS.md`, `unity/README.md` theo quyết định Unity 6.6.

Kiểm tra:

- Build lần 1: `ARLAB_BUILD Succeeded`, 0 lỗi, 7 phút 55 giây. APK 31,2 MB; `aapt` xác nhận gói `vn.arexperiment.arlab`, minSdk 26, targetSdk 36, arm64-v8a, quyền CAMERA, ARCore `required`.
- Phát hiện ARCore mặc định đặt Depth bắt buộc (máy không có Depth sẽ không cài được qua CH Play) → chuyển sang tuỳ chọn. Gói ARCore chỉ thêm, không xoá dòng này khỏi manifest, nên phải xoá dự án Gradle trung gian (`Library/Bee/Android/Prj/IL2CPP/Gradle`) rồi build lại. APK cuối (21:51): 31,2 MB, không còn `com.google.ar.core.depth`, ARCore vẫn `required`.
- **Chưa thử trên điện thoại thật** (chưa có máy cắm vào PC).

Ghi chú:

- `device_commit_files` có lúc chép bản cũ khi chép lại cùng đường dẫn tạm; đã kiểm tra MD5 sau mỗi lần chép.
- Ảnh đèn sợi đốt nằm thẳng trong `Den-soi-dot/`, không có thư mục phiên; giữ nguyên, không di chuyển.
- Thư mục `experiments/roi-tu-do` đã bị đổi tên thành `den-soi-dot` trước 21:30 (không phải do phiên này). Đã khôi phục `roi-tu-do` từ git; `den-soi-dot` giữ làm gói của đèn sợi đốt.
- Thêm `.utmp/` (thư mục build tạm của Android) vào `.gitignore`.

### 2026-11-15 (tối) — Chuẩn bị ảnh mẫu và môi trường build (Claude Opus 5.5)

- Phát hiện máy đã có **Unity 6000.6.9f1** tại `D:\Unity` (Unity Hub đặt thư mục cài là `D:\Unity`), nhưng chỉ có module Windows, **chưa có Android Build Support**.
- Unity 6.6 là bản *Supported* (được cập nhật như LTS cho tới khi 6.7 ra); 6.7 LTS dự kiến ra quý 4/2026. Đề xuất dùng 6.6 cho GĐ 1 thay vì cài thêm 6.3 LTS, rồi nâng lên 6.7 LTS. Chưa sửa `docs/00`, `docs/04` — chờ thầy chốt.
- Tạo thư mục phiên chụp đầu tiên `01_Anh_Da_Goc/roi-tu-do/2026-11-15_1/` gồm `4-goc/`, `quet-vong/`, `ghi-chu.md` (chép từ mẫu).
- Ổ trống: C 18 GB, D 129,8 GB → module Android phải cài vào ổ D.

### 2026-11-15 — Khởi tạo hệ thống (Claude Fable 5.2)

Đã làm:

- Khảo sát thư mục dự án cũ, vault Obsidian, connector và plugin, phần mềm trên máy HOANGLINH-PC.
- Chốt với thầy: repo ở `D:\Dev\AR-Experiment` (ngoài OneDrive, đường dẫn không dấu); Android trước; gói nội dung đặt trên Firebase.
- Clone repo trống `ArisuTsuberuku/AR-Experiment`, dựng cấu trúc thư mục chuẩn.
- Tạo kho dữ liệu gốc trong OneDrive: `01_Anh_Da_Goc/` (ảnh nhiều góc), `02_Video_Goc/` (video gốc), kèm phiếu ghi mẫu.
- Viết bộ tài liệu nền `docs/00…05`, `AGENTS.md`, `CLAUDE.md`, mẫu spec và phiếu bàn giao.
- Viết `tools/sync_vault.py` (sao tài liệu sang vault) và `tools/kiem_tra_may.py` (kiểm tra máy).
- Tạo `30-projects/13-AR-Experiment/` trong vault, nối vào `00-Home`, MOC dự án EdTech, bảng dự án trong `CLAUDE.md` của vault, từ điển thuật ngữ, hồ sơ máy.
- Lưu tài liệu giai đoạn khảo sát (10/11/2026) vào `docs/archive/`.

Kiểm tra:

- `python tools/kiem_tra_may.py` trên HOANGLINH-PC: repo ở đường dẫn không dấu, ngoài OneDrive; thấy kho dữ liệu gốc và vault; có git, gh, node, firebase, Blender, numpy, OpenCV, matplotlib; thiếu ffmpeg, adb, scipy, pandas; không thấy Unity Hub và Unity CLI.
- `python tools/sync_vault.py`: tạo 7 ghi chú trong vault; `--check` sau đó báo 7/7 khớp.

Phát hiện:

- Máy chưa cài Unity. `ar-pipeline.zip` của giai đoạn khảo sát không có trong thư mục dự án.
- Repo GitHub đang công khai.
- Máy dùng card AMD: chọn RealityScan (hỗ trợ AMD từ bản 2.2) thay cho Meshroom / COLMAP dựng dày.
