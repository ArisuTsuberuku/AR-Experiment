# AR Experiment — Nhật ký tiến trình

Mọi agent cập nhật file này khi mở và đóng phiên (xem `AGENTS.md`). Chỉ thêm, không xoá mục cũ. Mục mới nhất của nhật ký nằm trên cùng.

## Đang làm

| Agent | Việc | Thư mục đang ghi | Từ |
|---|---|---|---|
| — | — | — | — |

## Tiếp

1. **Thầy:** trong Unity Hub, thêm module *Android Build Support* (kèm OpenJDK, Android SDK & NDK) cho bản **6000.6.9f1** đã có ở `D:\Unity`; kiểm tra điện thoại thử có hỗ trợ ARCore, bật Tuỳ chọn nhà phát triển + Gỡ lỗi USB; cài plugin Unity cho Claude Code trên PC. ⏸ Chờ thầy chốt dùng 6.6 thay cho 6.3 LTS ghi trong tài liệu (đề xuất: dùng 6.6, lên 6.7 LTS khi ra).
2. **Claude Code:** tạo `unity/ARLab` từ mẫu AR Mobile, thêm glTFast, build APK đặt một mô hình GLB lên mặt bàn (GĐ 1).
3. **Thầy:** chụp ảnh bộ dụng cụ rơi tự do vào `01_Anh_Da_Goc/roi-tu-do/2026-11-15_1/` (`4-goc/` và `quet-vong/`), điền `ghi-chu.md` kèm số đo thật. Đổi tên thư mục theo ngày chụp nếu chụp hôm khác.
4. **Thầy + Cowork:** quay thử rơi tự do bằng 1 điện thoại, chép vào `02_Video_Goc/roi-tu-do/<ngày>_1/` kèm `ghi-chu.md`; Cowork viết `docs/specs/roi-tu-do.md`.
5. **Claude Code:** viết `pipeline/` cho 1 máy quay theo spec (GĐ 2). Việc này không cần Unity, làm song song được.
6. **Thầy:** cài ffmpeg và `pip install scipy pandas` (cho GĐ 2); cài RealityScan 2.2 trở lên (chuẩn bị GĐ 3).
7. **Thầy quyết định ⏸:** dự án Firebase tạo mới hay dùng chung; repo để công khai hay riêng tư; tìm lại `ar-pipeline.zip` hay viết lại.

## Nhật ký

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
