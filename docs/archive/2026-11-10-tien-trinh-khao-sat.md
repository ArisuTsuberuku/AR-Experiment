# Tiến trình dự án: Chuyển đổi thí nghiệm sang AR

Cập nhật: 10/11/2026

## 1. Mục tiêu

Tái hiện một thí nghiệm vật lý chuẩn trong AR sao cho người xem cầm điện thoại quan sát được như khi đứng trước thí nghiệm thật:

- Đi lại tự do quanh bàn, xem liền mạch 360°, không bị khoá ở một góc video.
- Nhiều người cùng xem một lúc, mỗi người một góc nhìn khác nhau.
- Chuẩn về mặt vật lý: chuyển động hiển thị là số liệu đo thật.
- Thí nghiệm mẫu đầu tiên: rơi tự do.
- Nguồn dữ liệu: 4 video quay từ 4 góc.

## 2. Đã làm

| Sản phẩm | Nội dung | Tình trạng |
| --- | --- | --- |
| Tài liệu kế hoạch (Claude Docs) | Quy trình 4 giai đoạn: chụp ảnh, tạo 3D bằng AI, hậu kỳ, hiển thị AR (WebAR và Unity); mục iPhone LiDAR; lịch 5 ngày, chi phí, rủi ro, tiêu chí nghiệm thu | Xong |
| `Ke-hoach-quy-trinh-Anh-2D-Mo-hinh-3D-AR.docx` | Bản Word của tài liệu trên | Xong |
| `ar-pipeline.zip` | Bộ mã, chi tiết bên dưới | Xong, kiểm thử một phần |

Nội dung `ar-pipeline.zip`:

| File | Việc | Kiểm thử |
| --- | --- | --- |
| `1_chup_anh/check_photos.py` | Kiểm tra và chuẩn hoá 4 ảnh front/left/back/right | Đã chạy trên ảnh giả lập |
| `2_tao_3d/meshy_multiview.py` | Gọi Meshy API tạo GLB + USDZ | Chưa chạy thật (cần API key) |
| `4a_webar/index.html` | Trang WebAR dùng model-viewer, kèm mô hình mẫu | Đã hiển thị 3D trên Chrome; chưa thử AR trên điện thoại |
| `4b_unity/Scripts/ARPlaceOnPlane.cs` | Đặt mô hình lên mặt phẳng, neo bằng ARAnchor | Chưa biên dịch trong Unity |
| `4b_unity/Scripts/ARObjectManipulator.cs` | Xoay, nghiêng, phóng to bằng chạm | Chưa biên dịch trong Unity |

## 3. Nội dung đã thảo luận

### 3.1 Quy trình ảnh 2D → 3D → AR
- Tripo và Meshy nhận tối đa 4 ảnh đa góc (trước, trái, sau, phải) và xuất GLB.
- Đây là AI tạo sinh: hình dáng giống thật nhưng không bảo đảm kích thước và chi tiết nhỏ; xử lý kém vật trong suốt.
- Hai cách hiển thị: WebAR (mở bằng link, làm trước) và app Unity với AR Foundation (tương tác đầy đủ, làm sau).

### 3.2 iPhone có LiDAR
- Quét vật thật bằng Object Capture trong app Reality Composer (iPhone 12 Pro trở lên), xuất USDZ.
- Trong app Unity: che khuất (occlusion) và lưới môi trường (meshing) trên máy có LiDAR.
- Giới hạn: LiDAR điện thoại thô với vật nhỏ; thuỷ tinh và bề mặt bóng vẫn khó; cần chuyển USDZ sang GLB.

### 3.3 Unity trên web
- Unity build được ra web, nhưng Unity không hỗ trợ trực tiếp XR trên nền tảng Web, nên AR Foundation không chạy trong bản web.
- Cần chạy bằng link thì dùng WebAR; cần tương tác đầy đủ thì dùng app.

### 3.4 Ghép 4 video đồng bộ
- Làm được trong app Unity: ghép 4 video thành một file lưới 2×2 để đồng bộ tuyệt đối, app chọn góc theo vị trí người xem.
- Giới hạn: chỉ có 4 góc rời rạc, hình nhảy mỗi 90°, video là mặt phẳng. Không đạt mục tiêu ở mục 1.

### 3.5 Nội suy góc khuyết từ 4 video
- Nội suy thẳng giữa các video cách nhau 90° không cho góc nhìn đúng.
- Tái dựng 4D bằng AI (4D Gaussian Splatting) mới ở mức nghiên cứu; bài 4C4D (CVPR 2026) dùng 4 máy quay nhưng chỉ phủ cung 100–120° phía trước.
- AI tạo sinh góc mới có thể sai vật lý, không dùng cho thí nghiệm chuẩn.

### 3.6 Hướng được đề xuất: dựng cảnh 3D có thời gian
4 máy quay làm dụng cụ đo, không phát video cho người xem. Người xem thấy mô hình 3D chuyển động theo số liệu đo.

1. Hiệu chuẩn 4 máy quay bằng bảng hiệu chuẩn đặt trên bàn, đưa về một hệ toạ độ chung.
2. Đồng bộ thời gian bằng đèn LED chớp trong khung của cả 4 máy.
3. Tìm tâm viên bi ở từng khung hình, giao các tia ngắm để ra vị trí 3D (x, y, z) theo thời gian.
4. Dựng mô hình 3D phần đứng yên (giá đỡ, thước, bàn) bằng một lượt chụp 30–80 ảnh quanh bộ thí nghiệm.
5. Đóng gói: mô hình 3D + file quỹ đạo + giá trị g kèm sai số + 4 video gốc kèm vị trí máy quay.
6. Phát lại trong AR ở tỉ lệ 1:1; mỗi điện thoại vẽ cảnh từ vị trí của mình.

Nhiều người cùng xem: dùng một tấm ảnh đánh dấu dán trên bàn để chung vị trí, và một máy chủ bấm phát để chung thời điểm.

Kiểm chứng: chiếu ngược cảnh 3D về vị trí 4 máy quay và chồng lên video gốc; độ lệch còn lại là sai số công bố.

### 3.7 Thiết bị quay 4 góc
Tiêu chí chọn: quay 240 fps tốc độ cố định, đặt tay được tốc độ màn trập từ 1/2000 s, 4 máy cùng loại, có cách đồng bộ.

| Phương án | Thiết bị | Giá tham khảo (11/2026) | Ưu điểm | Hạn chế |
| --- | --- | --- | --- | --- |
| A. Action cam | 4 × GoPro HERO13 Black (2.7K ở 240 fps) | Khoảng 9–11,5 triệu đồng/máy | Dễ dùng, tự ghi vào thẻ, có firmware GoPro Labs hỗ trợ timecode | Màn trập cuộn; đồng bộ timecode chỉ chính xác 1–2 khung hình |
| A'. Action cam | 4 × DJI Osmo Action 5 Pro (1080p ở 240 fps) | Khoảng 8,4 triệu đồng/máy | Rẻ hơn | Độ phân giải ở 240 fps thấp hơn; màn trập cuộn |
| B. Camera công nghiệp | 4 × Hikrobot MV-CS016-10UC (1440×1080, 249 fps) | Khoảng 200–320 USD/máy, chưa gồm ống kính | Màn trập toàn cục, trigger cứng nên 4 máy chụp cùng một thời điểm | Cần máy tính để ghi, đấu dây trigger, người rành kỹ thuật |

Lưu ý khi dùng action cam:
- Gắn máy xoay dọc để hướng quét của cảm biến vuông góc với hướng rơi, tránh sai lệch thời gian do màn trập cuộn, đồng thời được nhiều điểm ảnh hơn dọc quãng rơi.
- Vẫn cần đèn LED chớp làm mốc đồng bộ.
- Đèn chiếu sáng phải là loại không nhấp nháy.
- Nên mua hoặc thuê thử 1 máy để kiểm tra trước khi trang bị đủ 4. Máy quay chỉ dùng lúc sản xuất nên thuê là lựa chọn đáng cân nhắc.

Chưa chốt phương án.

## 4. Thông số đề xuất cho thí nghiệm rơi tự do

Ví dụ thả từ độ cao 1 m: thời gian rơi 0,45 s, vận tốc chạm đáy 4,43 m/s.

| Hạng mục | Đề xuất | Lý do |
| --- | --- | --- |
| Số máy quay | 4, hai cặp đối diện, cách nhau 90° | 2 máy đã đủ vị trí 3D; 4 máy cho kiểm tra chéo và 4 phép đo g độc lập |
| Tốc độ khung hình | 240 fps, tốc độ cố định | 108 khung hình cho quãng rơi; ở 30 fps chỉ có 14 |
| Tốc độ màn trập | Từ 1/2000 s | Bi nhoè khoảng 2 mm; cần đèn mạnh |
| Đồng bộ | Đèn LED chớp chung | Lệch 1 khung ở 240 fps gây sai 18 mm cuối quãng rơi |
| Thước chuẩn | Đặt trong mặt phẳng rơi | Thước lệch 5 cm khi máy cách 2 m gây sai tỉ lệ 2,5% |
| Vị trí máy | Ngang giữa quãng rơi, đặt xa và zoom | Giảm méo phối cảnh |
| Vật rơi | Bi thép nhỏ | Sức cản không khí ước tính dưới 0,5% với bi 2 cm rơi 1 m |

Phía người xem: cần chế độ quay chậm và dừng từng khung (0,45 s chỉ là 27 khung hình trên màn 60 Hz); đứng cách bàn 1,5–2 m để thấy trọn quãng rơi ở tỉ lệ 1:1.

## 5. Hướng phát triển

| Bước | Việc | Kết quả cần có |
| --- | --- | --- |
| 1 | Thử trang WebAR trên điện thoại thật; biên dịch 2 script Unity | Xác nhận phần hiển thị nền tảng chạy được |
| 2 | Viết script hiệu chuẩn 4 máy quay và dựng quỹ đạo 3D từ 4 video | File quỹ đạo (t, x, y, z), giá trị g của từng máy, sai số |
| 3 | Quay thử thí nghiệm rơi tự do theo thông số mục 4 | Bộ 4 video đồng bộ + ảnh chụp quanh bộ dụng cụ |
| 4 | Viết script Unity phát lại quỹ đạo: tốc độ thật, quay chậm, dừng từng khung | Viên bi 3D rơi đúng số liệu, xem 360° |
| 5 | Thêm lớp phủ: thước, vệt vị trí, đồ thị y(t) và v(t), giá trị g | Bản dùng cho giảng dạy |
| 6 | Đồng bộ nhiều điện thoại bằng ảnh đánh dấu và đồng hồ chung | Nhiều người xem cùng lúc, khác góc |
| 7 | Chồng cảnh dựng lên 4 video gốc để kiểm chứng | Số liệu sai số công bố |
| 8 | Chuẩn hoá thành khuôn mẫu cho thí nghiệm khác | Mỗi thí nghiệm = 1 gói dữ liệu theo cùng định dạng |

## 6. Điểm còn mở

- Hướng ở mục 3.6 là đề xuất, chưa được chốt.
- Chọn thiết bị quay ở mục 3.7 (action cam hay camera công nghiệp; mua hay thuê).
- Danh sách thí nghiệm sau rơi tự do. Thí nghiệm có chất lỏng hoặc hiện tượng không có hình dạng cố định sẽ không dùng được cách theo dõi vật rắn.
- Nền tảng điện thoại mục tiêu (Android, iPhone hay cả hai). Bản iOS cần máy Mac để build.
- Điện thoại dùng để quay có ghi được 240 fps tốc độ cố định hay không.
- Giá và hạn mức gói miễn phí của Tripo lấy từ nguồn bên thứ ba, cần xác nhận trên trang chính thức.

## 7. Nguồn đã dùng

- Meshy, Multi-Image to 3D API: https://docs.meshy.ai/api/multi-image-to-3d
- Tripo, Multiview to 3D Model API: https://developers.tripo3d.com/en/docs/generation-multiview-to-model/standard
- model-viewer, Augmented Reality: https://modelviewer.dev/examples/augmentedreality
- Unity, XR packages: https://docs.unity3d.com/6/Documentation/Manual/xr-support-packages.html
- Unity, Web browser compatibility: https://docs.unity3d.com/Manual/webgl-browsercompatibility.html
- Unity, Meshing samples: https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/samples/features/meshing.html
- Unity, Video transparency support: https://docs.unity3d.com/Manual//VideoTransparency.html
- Apple, Reality Composer: https://apps.apple.com/app/id1462358802
- 4C4D: 4 Camera 4D Gaussian Splatting (CVPR 2026): https://arxiv.org/html/2604.04063v1
- Giá GoPro HERO13 Black (CellphoneS): https://cellphones.com.vn/camera-hanh-trinh-gopro-hero-13-black.html
- Giá DJI Osmo Action 5 Pro (CellphoneS): https://cellphones.com.vn/camera-hanh-dong-dji-action-5-pro-standard.html
- Hikrobot MV-CS016-10UC: https://light186.com/product/hikrobot-1-6mp-249-1fps-usb-3-0-camera-with-an-imx273-sensor-2/
- GoPro Labs, đồng bộ thời gian: https://gopro.github.io/labs/control/gpssync
