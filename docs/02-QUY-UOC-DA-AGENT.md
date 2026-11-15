# AR Experiment — Quy ước dùng chung cho các agent

Cập nhật: 15/11/2026

Áp dụng cho: Claude (phiên kiến trúc, Fable 5.2), Claude Cowork (Opus 5.5), Claude Code, Antigravity với Gemini 4 Argon và Gemini 4 Hydrogen (mức suy luận High). Bản rút gọn bắt buộc đọc ở mỗi phiên là `AGENTS.md` ở gốc repo; tài liệu này giải thích đầy đủ.

## 1. Ba nơi lưu, mỗi nơi một vai

| Nơi | Vai | Ai sửa |
|---|---|---|
| Repo `D:\Dev\AR-Experiment` (GitHub) | **Nguồn sự thật** cho mã, spec, tiến trình, gói thí nghiệm | Mọi agent, theo quyền ghi ở mục 4 |
| Kho OneDrive `AR_experiment_website` | Ảnh và video **gốc**, chỉ thêm, không sửa | Thầy chép dữ liệu vào; agent chỉ đọc |
| Vault Obsidian `30-projects/13-AR-Experiment` | **Bản sao để tra cứu** và nối với bộ não thứ hai | Chỉ `tools/sync_vault.py` ghi; không sửa tay |

Tài liệu chỉ sửa ở repo. Sửa ở vault sẽ bị ghi đè ở lần đồng bộ sau.

## 2. Ai làm việc gì

| Agent | Vai | Việc tiêu biểu | Giao ra |
|---|---|---|---|
| **Claude — Fable 5.2** | Kiến trúc sư | Tổng quan, quyết định kiến trúc, rà soát ở các mốc, gỡ bế tắc giữa các agent | `docs/00`, `01`, `02`, `05`, `AGENTS.md` |
| **Claude Cowork — Opus 5.5** | Phân tích và điều phối | Spec thí nghiệm, so sánh phương án, kiểm số liệu, báo cáo, phiếu bàn giao, thao tác file trên PC, đồng bộ vault | `docs/specs/`, `docs/reports/`, `docs/handoff/`, `TIEN_TRINH.md` |
| **Claude Code** | Lập trình | C# cho Unity, Python cho pipeline, test, build, commit | `unity/`, `pipeline/`, `tools/`, `firebase/` |
| **Antigravity — Gemini 4 Argon** | Mắt nhìn và phản biện | Phân tích ảnh / video, dựng và soát hình mô hình 3D, script Blender, soát ảnh chụp màn hình AR, ý kiến thứ hai về thiết kế | `experiments/<mã>/model/`, mục "Kết quả" của phiếu, `docs/reports/` (soát hình) |
| **Antigravity — Gemini 4 Hydrogen (High)** | Thực thi và kiểm tra chéo | Chạy pipeline theo phiếu, chuyển định dạng, sửa lỗi nhỏ, viết test, kiểm tra lại kết quả của agent khác | Theo phiếu |
| **Thầy** | Chủ dự án | Chọn việc, quay và đo thí nghiệm thật, duyệt ở các điểm dừng ⏸ | Dữ liệu gốc, quyết định |

Chọn agent theo loại việc:

| Việc | Giao cho |
|---|---|
| Chưa rõ nên làm gì, hoặc đụng tới kiến trúc và định dạng gói | Fable 5.2 (hiếm, theo mốc ở Lộ trình) |
| Đọc tài liệu, viết spec, so sánh phương án, kiểm số | Cowork |
| Viết hoặc sửa mã khi đã có spec | Claude Code |
| Phải nhìn ảnh, video, mô hình 3D; việc trong Blender | Antigravity Argon |
| Việc lặp, đầu vào và đầu ra đã rõ | Antigravity Hydrogen High |
| Việc nhiều bước vừa cần lập kế hoạch vừa cần thực thi trong Antigravity | Orchestrator (Argon): Argon lập kế hoạch, Hydrogen thực thi |

Hai agent bất đồng về phương án: ghi hai phương án vào một phiếu trong `docs/handoff/`, để thầy hoặc phiên Fable quyết. Có thể dùng plugin `agent-discussion` để các agent tranh luận qua một file chung.

## 3. Nghi thức một phiên làm việc

**Mở phiên**

1. `git pull` trong `D:\Dev\AR-Experiment`.
2. Đọc `AGENTS.md`.
3. Đọc `TIEN_TRINH.md`: mục "Đang làm" và "Tiếp".
4. Đọc spec hoặc phiếu của việc sắp làm.
5. Ghi một dòng vào "Đang làm": agent, việc, thư mục sẽ ghi.

**Đóng phiên**

1. Chạy kiểm tra liên quan tới phần vừa làm.
2. Cập nhật `TIEN_TRINH.md`: đã làm gì, kết quả kiểm tra, việc "Tiếp".
3. Xoá dòng của mình khỏi "Đang làm".
4. `python tools/sync_vault.py` để cập nhật bản sao trong vault.
5. `git add`, `git commit`, `git push`.

Việc dở dang cũng phải đóng phiên đủ 5 bước, ghi rõ dở ở đâu.

## 4. Quyền ghi theo thư mục

| Thư mục | Ghi | Chỉ đọc |
|---|---|---|
| `docs/00…05`, `AGENTS.md`, `CLAUDE.md` | Fable 5.2; Cowork khi thầy yêu cầu | Còn lại |
| `docs/specs/`, `docs/reports/`, `docs/handoff/` | Cowork; Argon và Hydrogen ghi mục "Kết quả" của phiếu được giao | Claude Code |
| `unity/`, `pipeline/`, `tools/`, `firebase/` | Claude Code; Hydrogen khi phiếu giao rõ file | Còn lại |
| `experiments/<mã>/` | Pipeline sinh ra (Claude Code, Hydrogen); Argon ghi `model/` | Còn lại |
| `TIEN_TRINH.md` | Mọi agent, chỉ thêm, không xoá mục cũ | — |
| `work/` | Mọi agent (vùng nháp, không vào git) | — |
| Kho OneDrive, vault | Không agent nào ghi trực tiếp | Mọi agent |

Mỗi lúc chỉ một agent ghi trong một thư mục. Thấy tên agent khác trong "Đang làm" ở thư mục mình cần thì dừng, báo thầy.

## 5. Bàn giao bằng file

Agent không gọi trực tiếp nhau. Việc chuyển từ agent này sang agent khác đi qua một **phiếu** trong `docs/handoff/` theo mẫu `_MAU-phieu-ban-giao.md`: việc, đầu vào, đầu ra mong muốn, tiêu chí đạt, và mục "Kết quả" để agent nhận ghi lại. Thầy là người mở agent kia và đưa câu lệnh.

Câu lệnh mẫu (dán nguyên văn):

| Tình huống | Câu lệnh |
|---|---|
| Tiếp tục công việc | `Đọc AGENTS.md và TIEN_TRINH.md, rồi làm tiếp mục "Tiếp".` |
| Viết spec (Cowork) | `Viết spec cho thí nghiệm <mã> theo docs/specs/_MAU-spec.md.` |
| Lập trình (Claude Code) | `Làm theo docs/specs/<mã>.md. Chạy kiểm tra, cập nhật TIEN_TRINH.md, đồng bộ vault, commit và push.` |
| Giao việc cho Antigravity | `Làm phiếu docs/handoff/<ngày>-<việc>.md. Ghi kết quả vào mục "Kết quả" của phiếu.` |
| Kiểm tra chéo (Hydrogen) | `Kiểm tra lại kết quả của phiếu docs/handoff/<ngày>-<việc>.md theo tiêu chí đạt; ghi nhận xét vào phiếu.` |
| Rà soát mốc (Fable 5.2) | `Rà soát mốc GĐ <n> theo docs/01-LO-TRINH.md; cập nhật tổng quan và lộ trình.` |

## 6. Commit

- Một nhánh `main`. Commit nhỏ, đẩy ngay sau mỗi việc trọn vẹn. Không `--force`.
- Dòng đầu: `<khu vực>: <việc>`, khu vực là một trong `docs`, `pipeline`, `unity`, `exp`, `tools`, `firebase`. Ví dụ: `pipeline: theo doi vien bi bang nguong mau`.
- Dòng cuối ghi agent thực hiện.
- Không commit: ảnh và video gốc, `work/`, `config.local.json`, khoá API, file keystore.

## 7. Điểm dừng chờ thầy duyệt ⏸

Agent chuẩn bị xong rồi dừng, không tự làm:

- Chốt hoặc đổi định dạng gói thí nghiệm (đổi `schemaVersion`).
- Mua hoặc thuê thiết bị; đăng ký dịch vụ trả phí.
- Tạo dự án Firebase, đổi quyền riêng tư của repo, cài phần mềm lớn lên máy.
- Phát hành APK hoặc gói cho học sinh.
- Xoá, di chuyển, đổi tên bất cứ thứ gì trong kho OneDrive hoặc vault.

## 8. An toàn

- Tuân thủ `AGENTS.md` ở gốc vault (vùng cấm OneDrive, quy tắc theo máy, không ghi bí mật vào vault).
- Kiểm tra `$env:COMPUTERNAME` trước khi dùng đường dẫn tuyệt đối; đường dẫn của máy đọc từ `config.local.json`.
- Dữ liệu gốc chỉ đọc. Mọi xử lý làm trên bản chép trong `work/`.
- Repo đang công khai: không đưa vào ảnh, video có mặt học sinh; không đưa thông tin cá nhân.
- Dự án Unity chỉ mở ở đường dẫn không dấu. Không đặt dự án Unity vào OneDrive.

## 9. Tiết kiệm hạn mức

- Fable 5.2 chỉ dùng ở các mốc rà soát.
- Việc nặng về hình ảnh chuyển cho Antigravity; Claude đọc số liệu thay vì chụp màn hình.
- Kết quả kiểm tra ghi thành dữ liệu (JSON, CSV) trong `docs/reports/` để agent sau đọc lại được mà không phải chạy lại.
- Sắp hết hạn mức giữa chừng: dừng ở chỗ sạch, đóng phiên đủ 5 bước.

## Nguồn

- Antigravity, chọn mô hình và mức suy luận: https://antigravity.google/docs/models/
- Google, giới thiệu Gemini 4 Argon: https://blog.google/innovation-and-ai/models-and-research/gemini-models/introducing-gemini-4-argon/
- Unity, plugin chính thức cho Claude Code: https://unity.com/blog/unity-plugin-for-claude-code
