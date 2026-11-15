# AR Experiment — Nhật ký tiến trình

Mọi agent cập nhật file này khi mở và đóng phiên (xem `AGENTS.md`). Chỉ thêm, không xoá mục cũ. Mục mới nhất của nhật ký nằm trên cùng.

## Đang làm

| Agent | Việc | Thư mục đang ghi | Từ |
|---|---|---|---|
| — | — | — | — |

## Tiếp

1. **Thầy:** cài Unity Hub + Unity 6.3 LTS kèm Android Build Support (ổ D); kiểm tra điện thoại thử có hỗ trợ ARCore; cài plugin Unity cho Claude Code trên PC.
2. **Claude Code:** tạo `unity/ARLab` từ mẫu AR Mobile, thêm glTFast, build APK đặt một mô hình GLB lên mặt bàn (GĐ 1).
3. **Thầy + Cowork:** quay thử rơi tự do bằng 1 điện thoại, chép vào `02_Video_Goc/roi-tu-do/<ngày>_1/` kèm `ghi-chu.md`; Cowork viết `docs/specs/roi-tu-do.md`.
4. **Claude Code:** viết `pipeline/` cho 1 máy quay theo spec (GĐ 2). Việc này không cần Unity, làm song song được.
5. **Thầy:** cài ffmpeg và `pip install scipy pandas` (cho GĐ 2); cài RealityScan 2.2 trở lên (chuẩn bị GĐ 3).
6. **Thầy quyết định ⏸:** dự án Firebase tạo mới hay dùng chung; repo để công khai hay riêng tư; tìm lại `ar-pipeline.zip` hay viết lại.

## Nhật ký

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
