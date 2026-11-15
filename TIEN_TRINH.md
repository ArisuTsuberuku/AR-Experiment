# AR Experiment — Nhật ký tiến trình

Mọi agent cập nhật file này khi mở và đóng phiên (xem `AGENTS.md`). Chỉ thêm, không xoá mục cũ. Mục mới nhất của nhật ký nằm trên cùng.

## Đang làm

| Agent | Việc | Thư mục đang ghi | Từ |
|---|---|---|---|
| — | — | — | — |

## Tiếp

1. **Thầy:** cài `work/build/ARLab.apk` lên điện thoại Android có ARCore (hướng dẫn: `unity/README.md`), thử đặt mô hình đèn sợi đốt lên bàn, đi một vòng quanh; báo lại: mô hình có đứng yên không, máy nào, chỗ nào lệch.
2. **Thầy:** đo **đường kính đáy đế** mô hình đèn thật (cm) và chiều cao từ mặt bàn tới đỉnh dây đỡ.
3. **Cowork / Claude Code:** chạy lại `pipeline/4_dung_3d/den_soi_dot_blender.py --D <số đo>`; sửa đoạn dây đỡ đi lên từ trụ giữa (xem `docs/reports/2026-11-15-den-soi-dot-soat-hinh.md`); build lại APK.
4. **Thầy:** cài plugin Unity cho Claude Code trên PC để các phiên sau điều khiển được Unity Editor đang mở.
5. **Thầy + Cowork:** quay thử rơi tự do bằng 1 điện thoại, chép vào `02_Video_Goc/roi-tu-do/<ngày>_1/` kèm `ghi-chu.md`; Cowork viết `docs/specs/roi-tu-do.md`.
6. **Claude Code:** viết `pipeline/` cho 1 máy quay theo spec (GĐ 2).
7. **Thầy:** cài ffmpeg và `pip install scipy pandas` (GĐ 2); cài RealityScan 2.2 trở lên nếu muốn quét ảnh phần đế.
8. **Thầy quyết định ⏸:** dự án Firebase tạo mới hay dùng chung; repo công khai hay riêng tư.

## Nhật ký

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
