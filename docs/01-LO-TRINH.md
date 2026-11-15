# AR Experiment — Lộ trình

Cập nhật: 15/11/2026 (tối: chốt Unity 6.6, mô hình đèn sợi đốt)

Mỗi giai đoạn có một kết quả kiểm được và một agent chủ trì. Trạng thái dùng 4 giá trị: `xong`, `dang-lam`, `ke-tiep`, `cho`. Bối cảnh hệ thống: [Tổng quan](00-TONG-QUAN-HE-THONG.md). Vai trò các agent: [Quy ước đa agent](02-QUY-UOC-DA-AGENT.md).

## Bảng giai đoạn

| GĐ | Tên | Kết quả phải có | Tiêu chí nghiệm thu | Chủ trì | Trạng thái |
|---|---|---|---|---|---|
| 0 | Nền móng | Repo, kho dữ liệu, bộ tài liệu, quy ước agent, bản sao vault | Repo đẩy lên GitHub; `tools/sync_vault.py --check` báo khớp | Fable 5.2 | `xong` |
| 1 | Môi trường và app khung | Unity 6.6 + Android Build Support; dự án `unity/ARLab`; APK đặt được mô hình đèn sợi đốt (GLB) lên mặt bàn | Trên điện thoại thật: mô hình đứng yên khi đi một vòng 360°, kích thước lệch không quá 5 % so với số đo | Claude Code | `dang-lam` — thầy đã thử APK trên điện thoại: đặt được, hiện đúng chiều cao; chờ đo đối chiếu và thử đi vòng 360° |
| 2 | Đo từ video (1 máy quay) | `pipeline/` chạy trọn: nhập liệu → hiệu chuẩn → theo dõi → khớp mô hình; gói `roi-tu-do` có quỹ đạo và giá trị g | g đo được lệch không quá 2 % so với 9,8 m/s²; có sai số; chạy lại ra cùng kết quả | Claude Code (mã), Cowork (spec, kiểm số) | `cho` |
| 3 | Mô hình 3D từ ảnh đa góc | Mô hình tĩnh đúng tỉ lệ mét, GLB dưới 10 MB. Bản đầu: đèn sợi đốt (dựng bằng script Blender), sau đó bộ rơi tự do | Kích thước lệch không quá 2 % so với thước; soát hình đạt ở 4 hướng | Antigravity Argon (dựng, soát hình), Cowork (phiếu) | `dang-lam` — đèn sợi đốt 0.2.0 (chất liệu nướng texture), thấu kính f = 200 mm 0.1.0 (2 cụm: đế kẹp + thấu kính có que); chờ số đo thật |
| 4 | Trình phát thí nghiệm trong AR | App đọc gói, phát chuyển động: tốc độ thật, quay chậm, dừng từng khung; lớp phủ vệt, vectơ, đồ thị | Vị trí vật trong AR khớp file quỹ đạo ở mọi thời điểm kiểm tra; xem được 360° | Claude Code | `cho` |
| 5 | Phân phối nội dung | Firebase Hosting chứa `catalog.json` và gói; app tải, lưu đệm, báo bản mới | Thêm một gói mới lên máy chủ → app đã cài thấy và chạy được, không build lại | Claude Code (app), Cowork (quy trình phát hành) | `cho` |
| 6 | Kiểm chứng và công bố sai số | Ảnh chồng cảnh dựng lên video gốc; bảng sai số trong gói | Sai số chiếu ngược được ghi trong `provenance`; báo cáo trong `docs/reports/` | Cowork (số liệu), Argon (soát hình) | `cho` |
| 7 | Nhiều người cùng xem | Chung vị trí qua ảnh đánh dấu, chung thời điểm qua máy chủ | 2 điện thoại nhìn cùng một vật, lệch thời điểm dưới 1 khung hình màn hình | Claude Code | `cho` |
| 8 | Chuẩn hoá và nhân rộng | Định dạng gói v1.0; skill "thêm thí nghiệm mới"; thí nghiệm thứ 2 và 3 | Thí nghiệm mới đi hết dây chuyền mà không sửa mã app | Fable 5.2 (rà soát), các agent khác | `cho` |
| 9 | Mở rộng | Nhiều máy quay; nhóm B (mô hình tham số); thử nghiệm splat cho nhóm C; iOS | Theo từng nhánh | Tuỳ nhánh | `cho` |

GĐ 2 và GĐ 3 độc lập với nhau, có thể làm song song sau GĐ 1. GĐ 2 không cần Unity, nên có thể bắt đầu ngay trong lúc chờ cài Unity.

## Việc kế tiếp (theo thứ tự)

1. **Thầy:** cài lại `work/build/ARLab.apk` (bản 15/11 22:22, mô hình đèn có chất liệu mới), xem chất liệu đế và cột dẫn dưới ánh sáng thật; đo đèn (đường kính đáy đế, chiều cao) và thấu kính (đường kính đĩa đen, phần kính, chiều cao đế). → nghiệm thu GĐ 1, GĐ 3.
2. **Thầy:** góp ý mockup giao diện app v0.2 (5 màn hình, canvas Design "AR Lab — Mockup giao diện app").
3. **Claude Code:** làm phiếu `docs/handoff/2026-11-15-app-v02-giao-dien-va-thau-kinh.md` — app v0.2 theo mockup: thư viện + xem 3D không bật camera, AR chỉ chạy khi bấm "Đặt vào không gian", tự dừng sau 2 phút; UI Toolkit; ước lượng ánh sáng, bóng đổ; tải gói từ `catalog.json`.
4. **Cowork / Claude Code:** chạy lại 2 script Blender với số đo thật; build lại APK.
5. **Thầy quyết định ⏸:** thêm trường thông số quang học (f, n) và lớp phủ tia sáng vào định dạng gói (đổi schema).
6. **Thầy + Cowork:** quay thử rơi tự do bằng 1 điện thoại theo phiếu ghi trong `02_Video_Goc/_MAU_ghi-chu-phien-quay.md`; Cowork viết `docs/specs/roi-tu-do.md`.
7. **Claude Code:** viết `pipeline/` cho trường hợp 1 máy quay theo spec. → GĐ 2.
8. **Thầy quyết định:** dự án Firebase (mới hay dùng chung), quyền riêng tư của repo.

## Hướng phát triển dài hạn

- **Thư viện thí nghiệm theo chương trình:** mỗi bài trong SGK có thí nghiệm thật quay được là một gói. Ưu tiên các bài mà quan sát bằng mắt khó (diễn ra quá nhanh, cần nhiều góc nhìn).
- **Liên thông với Web Tương Tác:** cùng một bộ số liệu đo thật có thể làm "đáp án thực nghiệm" để đối chiếu với thí nghiệm ảo trên web.
- **Học sinh tự làm gói:** quy trình đủ đơn giản để nhóm học sinh quay, đo và đóng gói một thí nghiệm — phù hợp đề tài STEM / nghiên cứu khoa học kĩ thuật.
- **Bộ dụng cụ quay chuẩn:** bảng hiệu chuẩn, ảnh đánh dấu, thước chuẩn, đèn LED đồng bộ in sẵn và hướng dẫn một trang.
- **Tự động hoá dây chuyền:** khi pipeline ổn định, một lệnh duy nhất đi từ phiên quay tới gói đã tải lên máy chủ.
- **iOS** khi có máy Mac; tận dụng LiDAR để che khuất vật ảo bằng vật thật.

## Mốc rà soát bằng Fable 5.2

Gọi phiên kiến trúc ở ba thời điểm: (1) sau GĐ 2, trước khi chốt định dạng gói; (2) sau GĐ 5, trước khi phát hành cho học sinh; (3) khi bắt đầu nhóm thí nghiệm mới (B hoặc C). Ngoài ra chỉ gọi khi hai agent khác không thống nhất được phương án.
