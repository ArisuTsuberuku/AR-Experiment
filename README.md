# AR Experiment

Biến thí nghiệm vật lí thật thành thí nghiệm AR trên điện thoại.

- **Ảnh chụp nhiều góc** → mô hình 3D của bộ dụng cụ.
- **Video quay thí nghiệm** → thông số thật của hệ (quỹ đạo, vận tốc, gia tốc… kèm sai số).
- **App AR (Unity)** phát lại thí nghiệm ở tỉ lệ 1:1, xem được từ mọi góc.
- **Cài app một lần**; thí nghiệm mới tải về như dữ liệu.

Trạng thái: giai đoạn 0 (nền móng) xong ngày 15/11/2026. Chưa có mã chạy được.

## Bắt đầu từ đâu

| Bạn là | Đọc |
|---|---|
| Người mới xem dự án | [docs/00-TONG-QUAN-HE-THONG.md](docs/00-TONG-QUAN-HE-THONG.md) |
| Agent AI mở phiên làm việc | [AGENTS.md](AGENTS.md), rồi [TIEN_TRINH.md](TIEN_TRINH.md) |
| Muốn biết việc kế tiếp | [docs/01-LO-TRINH.md](docs/01-LO-TRINH.md) |
| Thiết lập máy mới | [docs/03-CAU-TRUC-THU-MUC-VA-DU-LIEU.md](docs/03-CAU-TRUC-THU-MUC-VA-DU-LIEU.md) mục 4 |

## Tài liệu

| File | Nội dung |
|---|---|
| [00 — Tổng quan hệ thống](docs/00-TONG-QUAN-HE-THONG.md) | Kiến trúc, nguyên tắc, quyết định đã chốt, điểm còn mở |
| [01 — Lộ trình](docs/01-LO-TRINH.md) | Các giai đoạn, tiêu chí nghiệm thu, việc kế tiếp |
| [02 — Quy ước đa agent](docs/02-QUY-UOC-DA-AGENT.md) | Vai trò và cách phối hợp Claude, Claude Code, Antigravity |
| [03 — Cấu trúc thư mục và dữ liệu](docs/03-CAU-TRUC-THU-MUC-VA-DU-LIEU.md) | Repo, kho ảnh / video gốc, vault Obsidian |
| [04 — Công cụ, connector, plugin](docs/04-CONG-CU-CONNECTOR-PLUGIN.md) | Đã có gì, còn thiếu gì |
| [05 — Định dạng gói thí nghiệm](docs/05-DINH-DANG-GOI-THI-NGHIEM.md) | Hợp đồng dữ liệu giữa pipeline và app |

## Thiết lập nhanh

```
git clone https://github.com/ArisuTsuberuku/AR-Experiment.git D:\Dev\AR-Experiment
cd D:\Dev\AR-Experiment
python tools/kiem_tra_may.py
```

Đặt repo ở đường dẫn không dấu, ngoài OneDrive.
