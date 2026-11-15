# AGENTS.md — AR Experiment

Quy tắc bắt buộc cho mọi agent làm việc trong repo này: Claude (Fable 5.2), Claude Cowork (Opus 5.5), Claude Code, Antigravity (Gemini 4 Argon, Gemini 4 Hydrogen). Bản đầy đủ: `docs/02-QUY-UOC-DA-AGENT.md`.

Chủ dự án: thầy Vũ Hoàng Linh, giáo viên Vật lí, biết code ở mức cơ bản. Trả lời và viết tài liệu bằng **tiếng Việt**, giải thích ngắn gọn.

## Dự án

Biến thí nghiệm vật lí thật thành thí nghiệm AR trên điện thoại: ảnh nhiều góc → mô hình 3D; video → thông số thật của hệ; app Unity phát lại ở mọi góc nhìn; thí nghiệm mới tải về như dữ liệu, không cài lại app.

## Đọc theo thứ tự này khi mở phiên

1. `AGENTS.md` (file này)
2. `TIEN_TRINH.md` — mục "Đang làm" và "Tiếp"
3. `docs/00-TONG-QUAN-HE-THONG.md` nếu chưa biết hệ thống
4. Spec trong `docs/specs/` hoặc phiếu trong `docs/handoff/` của việc sắp làm

## Luật

1. **Repo là nguồn sự thật.** Tài liệu chỉ sửa ở repo. Vault Obsidian là bản sao do `python tools/sync_vault.py` sinh ra; không sửa tay trong vault.
2. **Dữ liệu gốc chỉ đọc.** Ảnh và video trong kho OneDrive không sửa, không đổi tên, không xoá. Xử lý trên bản chép trong `work/`.
3. **Ghi đúng thư mục của mình** (bảng dưới). Mỗi lúc một agent ghi trong một thư mục; ghi tên vào "Đang làm" trước khi bắt đầu.
4. **Bàn giao bằng file:** phiếu trong `docs/handoff/` theo `_MAU-phieu-ban-giao.md`.
5. **Đóng phiên đủ 5 bước:** kiểm tra → cập nhật `TIEN_TRINH.md` → xoá tên khỏi "Đang làm" → `python tools/sync_vault.py` → commit và push.
6. **Dừng ở điểm ⏸** và hỏi thầy: đổi định dạng gói, mua thiết bị hoặc dịch vụ trả phí, tạo dự án Firebase, đổi quyền repo, cài phần mềm lớn, phát hành cho học sinh, xoá hay di chuyển gì đó trong kho OneDrive hoặc vault.
7. **Không bịa số liệu.** Đại lượng đo phải có phiên quay, phương pháp và sai số. Chưa đo thì để trống và `status: draft`.
8. **Không đưa bí mật vào repo hay vault:** khoá API, token, keystore. Repo đang công khai: không đưa ảnh, video có mặt học sinh.

## Quyền ghi

| Thư mục | Agent được ghi |
|---|---|
| `docs/00…05`, `AGENTS.md`, `CLAUDE.md` | Fable 5.2; Cowork khi thầy yêu cầu |
| `docs/specs/`, `docs/reports/`, `docs/handoff/` | Cowork; Argon và Hydrogen ghi mục "Kết quả" của phiếu mình nhận |
| `unity/`, `pipeline/`, `tools/`, `firebase/` | Claude Code; Hydrogen khi phiếu giao rõ file |
| `experiments/<mã>/` | Pipeline (Claude Code, Hydrogen); Argon ghi `model/` |
| `TIEN_TRINH.md` | Mọi agent, chỉ thêm |
| `work/` | Mọi agent (không vào git) |

## Môi trường

- Repo: `D:\Dev\AR-Experiment`. Đường dẫn này cố ý không dấu và nằm ngoài OneDrive; **không** chuyển dự án Unity vào OneDrive hay thư mục có dấu (build Android sẽ lỗi).
- Đường dẫn theo máy (kho OneDrive, vault, Blender) đọc từ `config.local.json`; chưa có thì chạy `python tools/kiem_tra_may.py`.
- Hai máy dùng chung OneDrive (`HOANGLINH-PC`, `MSI`): kiểm tra `$env:COMPUTERNAME` và hồ sơ máy trong vault (`40-devices/`) trước khi dùng đường dẫn tuyệt đối khác.
- Quy tắc an toàn OneDrive và vault: `AGENTS.md` ở gốc vault `D:\OneDrive\Documents\Obsidian Vault`.
- Nền tảng: Unity 6.3 LTS, AR Foundation 6.3, Android trước. Gói nội dung đặt trên Firebase.
- Commit: `<khu vực>: <việc>` với khu vực thuộc `docs | pipeline | unity | exp | tools | firebase`; một nhánh `main`; không `--force`.
