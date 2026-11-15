# Phiếu: App AR Lab v0.2 — giao diện mới, AR theo yêu cầu, thấu kính 2 cụm

- Ngày: 2026-11-15
- Từ: Claude Opus 5.5 (Cowork) → Tới: Claude Code
- Trạng thái: moi

## 1. Việc cần làm

Làm lại app `unity/ARLab` theo mockup v0.2: mở app vào **thư viện**, xem **3D không bật camera**, chỉ khởi động AR khi người dùng bấm "Đặt vào không gian (AR)" và tắt hẳn AR khi rời màn AR. Thêm thí nghiệm thứ hai là thấu kính `thau-kinh-f200`, mô hình gồm 2 cụm tách rời được (đế kẹp và thấu kính có que).

## 2. Đầu vào

- Mockup (5 màn hình 390 × 844): canvas Design "AR Lab — Mockup giao diện app" — https://claude.ai/artifact/8MMkECk9Nq1sPscBpkFwBg (thầy mở cho xem nếu agent không đọc được). Tóm tắt từng màn ở mục 7.
- Mã hiện có: `unity/ARLab/Assets/Scripts/ARLabPlacer.cs` (đặt, xoay, chụm, tỉ lệ 1:1, ẩn/hiện mặt phẳng), `unity/ARLab/Assets/Editor/ARLabSetup.cs` (thiết lập Player, XR, URP, dựng cảnh, build APK; điểm vào `ARLabSetup.SetupAndBuild`).
- Gói thí nghiệm:
  - `experiments/den-soi-dot/` — `experiment.json`, `model/scene.glb` (1 cụm), `media/thumb.png`.
  - `experiments/thau-kinh-f200/` — `experiment.json`, `model/scene.glb`, `media/thumb.png`.
- Cấu trúc GLB thấu kính (đã kiểm tra trong file):
  - Nút gốc `DeKep`: đế kẹp sơn xám, gốc ở tâm đáy.
  - Nút gốc `ThauKinh`: que thép + kẹp chữ V + đĩa + kính; gốc ở **đáy que**. Nút con `TamQuang` ở quang tâm (cao 11,8 cm khi Dd = 0,12 m). `extras` của `ThauKinh`: `f_m = 0.2`, `n = 1.52`, `R_m = 0.208`, `ban_kinh_kinh_m = 0.0204`.
  - Que nhô trên mặt đế 1,2 cm; đáy que cách mặt bàn 0,36 cm.
  - Vật liệu kính `ThuyTinh`: `alphaMode = BLEND`, alpha 0,10.
- Định dạng gói: `docs/05-DINH-DANG-GOI-THI-NGHIEM.md`. Quy ước: `AGENTS.md`, `docs/02-QUY-UOC-DA-AGENT.md`. Cách build: `unity/README.md`.

## 3. Đầu ra mong muốn

- `unity/ARLab` build ra `work/build/ARLab.apk` bằng lệnh batch hiện có (`ARLabSetup.SetupAndBuild`), đã cập nhật để dựng cảnh mới.
- Giao diện bằng **UI Toolkit** (UXML + USS) trong `unity/ARLab/Assets/UI/`, bỏ IMGUI.
- Nạp gói theo **danh sách gói** (đọc `experiment.json` + `media/thumb.png` + `model/scene.glb` bằng glTFast lúc chạy). Tạm thời đóng gói 2 thí nghiệm vào `StreamingAssets/experiments/` trong lúc chưa có Firebase. Viết lớp nạp gói sao cho sau này chỉ cần đổi nguồn sang `catalog.json` trên mạng.
- Ghi kết quả vào mục 6 của phiếu này; thêm mục nhật ký vào `TIEN_TRINH.md`; chạy `python tools/sync_vault.py`; commit và push.

## 4. Tiêu chí đạt

- [ ] Build batch thành công, 0 lỗi; APK cài được trên Android 8.0 trở lên (minSdk 26), ARCore vẫn `required`, không có `com.google.ar.core.depth` bắt buộc.
- [ ] Mở app: **không** khởi động XR, không mở camera, không xin quyền camera. Quyền camera chỉ xin khi bấm "Đặt vào không gian (AR)" lần đầu.
- [ ] XR nạp theo yêu cầu: tắt "Initialize XR on Startup"; vào màn AR thì `XRGeneralSettings.Instance.Manager.InitializeLoader` rồi `StartSubsystems`; rời màn AR (nút X, nút Back của Android, app vào nền) thì `StopSubsystems` rồi `DeinitializeLoader`.
- [ ] Ở màn AR mà **2 phút** không chạm, AR tự dừng và hiện thông báo "Đã tắt camera để tiết kiệm pin" kèm nút bật lại.
- [ ] Màn chi tiết xem 3D bằng camera thường: kéo để xoay quanh mô hình, chụm để phóng to, có giới hạn góc và khoảng cách. Không dùng camera của máy.
- [ ] Thư viện hiện 2 thẻ (đèn sợi đốt, thấu kính) lấy tiêu đề và ảnh từ gói; thẻ "Rơi tự do" ở trạng thái "Sắp có".
- [ ] Màn AR có thanh công cụ nổi: Tỉ lệ 1:1 · Chú thích · Tách rời · Chụp · Thông tin.
  - "Chụp" lưu ảnh màn hình vào thư viện ảnh của máy.
  - "Chú thích" hiện nhãn bám theo bộ phận. Lấy danh sách bộ phận theo tên nút trong GLB; tên hiển thị tạm đặt trong một bảng tra ở phía app, **không** thêm trường vào `experiment.json`.
- [ ] Thấu kính: "Tách rời" nâng cụm `ThauKinh` lên 4 cm (hoạt ảnh khoảng 0,4 giây) rồi hạ lại khi bấm lần nữa. Có thể kéo dọc để chỉnh độ cao que trong khoảng 0–6 cm trên mặt đế, giống nới núm vặn trên dụng cụ thật. Đèn sợi đốt có 1 cụm thì nút này mờ đi.
- [ ] Kính hiển thị trong suốt trong URP (không trắng đục, không bị mặt sau che); nếu glTFast nạp sai thì gán lại shader trong suốt cho vật liệu tên `ThuyTinh`.
- [ ] Ánh sáng: bật ước lượng ánh sáng của AR Foundation (cường độ chính, màu); mô hình có bóng đổ lên mặt phẳng (một mặt phẳng nhận bóng trong suốt dưới mô hình).
- [ ] Hiện chiều cao mô hình theo cm như bản 0.1 (vẫn đúng ở tỉ lệ 1:1).
- [ ] Thử được trong Editor, không cần điện thoại: thư viện và màn chi tiết chạy trong Play Mode; màn AR dùng XR Simulation nếu có, không thì ghi rõ chưa thử.

## 5. Ràng buộc

- Chỉ ghi vào: `unity/ARLab/**`, mục 6 của phiếu này, `TIEN_TRINH.md`, `unity/README.md`.
- **Không** sửa `experiments/*/experiment.json` và `docs/05-...`: đổi định dạng gói phải chờ thầy quyết (⏸). Thông số quang học tạm đọc từ `extras` của nút `ThauKinh` trong GLB, chỉ để hiển thị.
- **Không** làm lớp phủ tia sáng / hiệu ứng khúc xạ của thấu kính trong phiếu này (thầy hoãn). Màn 5 của mockup để phiếu sau.
- Không tạo dự án Firebase, không đổi quyền repo, không cài phần mềm lớn (⏸ hỏi thầy).
- Dự án Unity giữ ở đường dẫn ASCII `D:\Dev\AR-Experiment\unity\ARLab`, Unity **6000.6.9f1**, URP. Không đụng dữ liệu gốc trong OneDrive.
- Sau khi đổi cài đặt ARCore phải xoá `Library/Bee/Android/Prj/IL2CPP/Gradle` rồi build lại (gói ARCore chỉ thêm, không xoá dòng trong manifest).
- Lệnh chạy lâu (build khoảng 8 phút) chạy nền và đọc log trong `work/logs/`.

## 7. Mockup tóm tắt (để đọc khi không mở được canvas)

Phong cách: nền tối `#0E1116`, chữ `#F2F4F7`, chữ phụ `#9AA4B2`, màu nhấn hổ phách `#F5A524` (chữ trên nút `#1A1204`), màu phụ xanh lơ `#4CC3FF`, phông Be Vietnam Pro, bo góc 14–28 px, vùng chạm tối thiểu 44 px, khung kính mờ (`rgba(14,17,22,0.6–0.8)` + viền `rgba(255,255,255,0.12)`).

1. **Thư viện:** tiêu đề "Thí nghiệm Vật lí", ô tìm kiếm, chip lọc (Tất cả / Điện / Quang / Cơ), băng báo gói mới (nút "Tải"), thẻ lớn đèn sợi đốt, lưới 2 thẻ nhỏ (thấu kính, rơi tự do "Sắp có"), thanh điều hướng dưới (Thư viện / Đã tải / Bài học).
2. **Chi tiết 3D:** khung xem 3D cao khoảng 400 px, chip "Xem 3D · camera tắt", nút quay lại và chia sẻ, tab Bộ phận / Thông số / Nguồn, danh sách bộ phận có chấm màu, nút chính "Đặt vào không gian (AR)" kèm dòng "Camera chỉ bật khi bạn bấm nút này".
3. **AR tìm mặt phẳng:** nút X, chip trạng thái "Đã thấy mặt bàn", vòng ngắm xanh lơ nằm trên mặt bàn, thẻ hướng dẫn ở dưới cùng với nút "Đặt ở đây" và dòng "Tự tắt camera sau 2 phút không thao tác".
4. **AR đã đặt:** khung tên + "x1,00 · cao 15,1 cm", nút tạm dừng AR, nhãn chú thích bám bộ phận, thanh công cụ 5 nút, bảng trượt dưới cùng giải thích bộ phận đang chọn.
5. **AR thấu kính – tia sáng:** để phiếu sau.

## 6. Kết quả (agent nhận ghi)

- Đã làm:
- File sinh ra:
- Kiểm tra đã chạy và kết quả:
- Chưa làm được / cần hỏi:
