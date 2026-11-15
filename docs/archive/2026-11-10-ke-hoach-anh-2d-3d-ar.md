> Bản chuyển sang markdown của `Ke-hoach-quy-trinh-Anh-2D-Mo-hinh-3D-AR.docx` (10/11/2026). File Word gốc nằm trong kho dữ liệu OneDrive. Tài liệu giai đoạn khảo sát; hồ sơ hiện hành là `docs/00-TONG-QUAN-HE-THONG.md`.

# Kế hoạch quy trình: Ảnh 2D → Mô hình 3D bằng AI → Hiển thị AR

## Tóm tắt

Quy trình gồm 4 giai đoạn, làm được với 1 điện thoại và 1 máy tính. Kết
quả AR đầu tiên có trong 1–2 ngày bằng WebAR (4A); app Unity (4B) dựng
sau khi mô hình 3D đã đạt.

quy trình · 3 bước chung, 2 nhánh hiển thị AR

| Giai đoạn    | Việc chính                                  | Công cụ                                       | Đầu ra                                    |
|--------------|---------------------------------------------|-----------------------------------------------|-------------------------------------------|
| 1\. Chụp ảnh | 4 ảnh: trước, trái, sau, phải, nền trơn     | Điện thoại, bàn xoay, 2 đèn                   | 4 file JPG/PNG đạt kiểm tra               |
| 2\. Tạo 3D   | Multi-view → mô hình có texture             | Tripo hoặc Meshy                              | model.glb (và model.usdz)                 |
| 3\. Hậu kỳ   | Đặt tỉ lệ thật, gốc toạ độ ở đáy, giảm lưới | Blender hoặc công cụ có sẵn trong Tripo/Meshy | GLB dưới 10 MB, đúng kích thước mét       |
| 4A. WebAR    | Trang web hiển thị 3D + nút AR              | model-viewer, hosting HTTPS                   | Link + mã QR, chạy trên Android và iPhone |
| 4B. Unity    | App AR riêng, tuỳ biến đầy đủ               | Unity 6, AR Foundation, ARCore/ARKit          | File APK (Android) hoặc bản build iOS     |

Hai yêu cầu hiển thị được đáp ứng như sau:

- **Di chuyển điện thoại thấy góc nhìn tương ứng:** ARCore/ARKit theo
  dõi vị trí và hướng máy (6 bậc tự do) và neo mô hình vào một mặt phẳng
  thật. Đi vòng quanh là thấy mặt bên, mặt sau; không phải lập trình
  thêm.

- **Chạm để đổi góc nhìn:** kéo 1 ngón để xoay, chụm 2 ngón để phóng
  to/thu nhỏ, xoắn 2 ngón để xoay quanh trục đứng.

Mã nguồn bàn giao kèm tài liệu này (gói ar-pipeline.zip): script kiểm
tra ảnh, script gọi API Meshy, trang WebAR, và 2 script C# cho Unity.

Lưu ý về bản chất công nghệ: Tripo và Meshy là AI tạo sinh, tự suy đoán
phần bề mặt không có trong ảnh. Hình dáng và màu sắc giống thật, nhưng
kích thước và chi tiết nhỏ (vạch chia, chữ số) không chính xác tuyệt
đối. Phương án thay thế cho dụng cụ cần độ chính xác nằm ở mục Rủi ro.

## Giai đoạn 1 — Chụp ảnh 2D đa góc

Cần đúng 4 ảnh vuông góc nhau: trước, trái, sau, phải. Cả Tripo lẫn
Meshy chỉ nhận tối đa 4 ảnh ở chế độ đa góc, nên chất lượng từng ảnh
quan trọng hơn số lượng.

| Hạng mục     | Yêu cầu                                                                                              |
|--------------|------------------------------------------------------------------------------------------------------|
| Máy ảnh      | Camera chính của điện thoại, zoom 1×–2×; không dùng ống góc rộng, không bật chế độ chân dung hay HDR |
| Giữ máy      | Gắn giá đỡ, ống kính ngang tầm tâm vật; khoá lấy nét và phơi sáng (AE/AF lock), tắt flash            |
| Nền          | Phông trơn một màu, tương phản với vật (xám nhạt hoặc trắng mờ); không có đồ vật khác trong khung    |
| Ánh sáng     | 2 nguồn sáng khuếch tán đặt chéo 45° hai bên; không có bóng đổ gắt, không có điểm chói               |
| Khung hình   | Thấy trọn vật, vật chiếm khoảng 40–70% chiều cao khung, không chạm mép ảnh                           |
| Độ phân giải | Cạnh ngắn từ 1.024 px trở lên                                                                        |

Cách chụp: giữ máy cố định, đặt vật lên bàn xoay và xoay vật 90° mỗi
lần.

1.  front: mặt chính diện của vật (0°).

2.  left: mặt bên trái của chính vật (90°), không phải bên trái của
    người chụp.

3.  back: mặt sau (180°).

4.  right: mặt bên phải của vật (270°).

Đặt tên file đúng 4 chữ trên, rồi chạy python check_photos.py anh_goc/
--prep anh_sach/. Script báo ảnh mờ, thiếu sáng, nền không đều, vật bị
cắt mép hoặc lệch cỡ giữa các góc, và xuất ảnh vuông 2.048 px. Ảnh đạt
khi kết quả là 0 lỗi.

Vật khó chụp cần xử lý trước:

- **Thuỷ tinh trong suốt (cốc, bình, ống nghiệm):** AI không nhìn thấy
  bề mặt trong suốt. Đổ nước pha màu vào bên trong, hoặc phủ tạm một lớp
  phấn mờ, rồi trả lại độ trong suốt ở giai đoạn 3.

- **Kim loại bóng, gương:** dùng ánh sáng khuếch tán hoặc phủ mờ tạm
  thời để mất phản chiếu.

- **Chi tiết mảnh (dây dẫn, que, kim):** thường bị mất hoặc dính liền;
  tách ra chụp riêng hoặc dựng tay.

## Giai đoạn 2 — Tạo mô hình 3D bằng AI

Chạy cùng bộ 4 ảnh qua cả Tripo và Meshy ở lần đầu, rồi chọn công cụ cho
kết quả tốt hơn với loại vật của dự án. Hai công cụ tương đương về cách
dùng; khác biệt chính là định dạng xuất và điều kiện gói miễn phí.

| Tiêu chí              | Tripo                                                                                                                                                                                        | Meshy                                                                                                                                                         |
|-----------------------|----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------|---------------------------------------------------------------------------------------------------------------------------------------------------------------|
| Ảnh đầu vào đa góc    | front bắt buộc, thêm ít nhất 1 trong left/back/right; tối đa 4 ([tài liệu API](https://developers.tripo3d.com/en/docs/generation-multiview-to-model/standard))                               | 1–4 ảnh JPG/PNG, ảnh đầu tiên là mặt trước ([tài liệu API](https://docs.meshy.ai/api/multi-image-to-3d))                                                      |
| Định dạng xuất        | GLB, FBX, OBJ, USD, STL                                                                                                                                                                      | GLB, FBX, OBJ, STL, USDZ, 3MF ([Meshy docs](https://docs.meshy.ai/en))                                                                                        |
| File cho iPhone       | Không xuất USDZ trực tiếp; trang WebAR tự chuyển từ GLB                                                                                                                                      | Xuất thẳng USDZ, texture nhúng sẵn                                                                                                                            |
| Gói miễn phí          | Khoảng 300 credit/tháng, mô hình công khai theo CC BY 4.0; đa góc nằm ở gói trả phí ([Costbench](https://www.costbench.com/software/ai-3d-generation/tripo-ai/free-plan/), nguồn bên thứ ba) | 100 credit/tháng; mô hình thuộc Meshy, cấp lại theo CC BY 4.0 ([3Dnatives](https://www.3dnatives.com/en/3dnatives-lab-meshy-ai-3d-model-generator-23092026/)) |
| Gói trả phí thấp nhất | Khoảng 20 USD/tháng                                                                                                                                                                          | Khoảng 20 USD/tháng                                                                                                                                           |
| Tự động hoá           | Có API                                                                                                                                                                                       | Có API; script meshy_multiview.py kèm theo                                                                                                                    |

Giá và hạn mức gói thay đổi thường xuyên và các trang tổng hợp đưa số
khác nhau; xác nhận trên trang giá chính thức trước khi mua.

Thao tác trên web (giống nhau ở cả hai công cụ):

1.  Chọn Image to 3D, chế độ đa ảnh (multi-view / multi-image).

2.  Tải front.png vào ô mặt trước, rồi left, back, right vào đúng ô.

3.  Bật tạo texture; đặt số mặt lưới khoảng 30.000 tam giác.

4.  Tạo mô hình, xoay đủ 360° trong khung xem trước để so với vật thật.

5.  Tải về GLB; với Meshy tải thêm USDZ.

Mô hình đạt khi: đường bao đúng ở cả 4 hướng, không thủng lưới, không có
khối thừa, texture mặt sau khớp ảnh back. Không đạt thì chụp lại góc sai
thay vì tạo lại nhiều lần với cùng ảnh.

Khi có nhiều vật, dùng API thay cho thao tác tay: python
meshy_multiview.py anh_sach/ --out models --name ten_vat. Script gửi
ảnh, chờ xử lý và tải về cả GLB lẫn USDZ; mỗi lần chạy tốn credit của
tài khoản.

## Giai đoạn 3 — Hậu kỳ và tối ưu mô hình

Mô hình AI xuất ra chưa có kích thước thật và thường nặng hơn mức điện
thoại cần. Chỉnh 4 thứ sau trong Blender (miễn phí) trước khi đưa vào
AR; các ngưỡng là mức khuyến nghị của kế hoạch này.

| Hạng mục        | Mục tiêu                                      | Cách làm trong Blender                                         |
|-----------------|-----------------------------------------------|----------------------------------------------------------------|
| Kích thước thật | Đúng số đo của vật, đơn vị mét                | Đo chiều cao vật thật, nhập vào Dimensions, rồi Apply Scale    |
| Gốc toạ độ      | Tâm đáy vật nằm tại (0, 0, 0), vật đứng thẳng | Dời vật sao cho đáy chạm mặt lưới, đặt Origin về 3D Cursor     |
| Số tam giác     | Từ 30.000 đến 50.000                          | Decimate modifier, giảm đến khi đường bao bắt đầu gãy thì dừng |
| Dung lượng      | GLB dưới 10 MB, texture 2.048 px              | Thu nhỏ texture; xuất glTF Binary (.glb)                       |

Với dụng cụ thuỷ tinh, trả lại độ trong suốt ở bước này: trong vật liệu
Principled BSDF, giảm Alpha về khoảng 0,3 và đặt Blend Mode sang Alpha
Blend. glTF lưu được thiết lập này và cả WebAR lẫn Unity đều hiển thị.

Nếu chưa dùng Blender, hai việc vẫn làm được ở chỗ khác: giảm lưới bằng
tuỳ chọn số mặt ngay khi tạo mô hình, và đặt kích thước thật bằng ô
Target Height Meters trong script Unity. Trang WebAR hiển thị kích thước
mô hình (cm) ở góc trên để đối chiếu nhanh với số đo thật.

## Giai đoạn 4A — WebAR (không cài app)

WebAR là đường nhanh nhất để thấy mô hình trên nền camera thật: một
trang web, mở bằng link hoặc mã QR, chạy trên cả Android và iPhone.
Trang dùng thư viện mã nguồn mở model-viewer của Google, đã kèm sẵn
trong thư mục 4a_webar.

| Thiết bị                              | Chế độ AR được dùng                                             | File mô hình                                                  |
|---------------------------------------|-----------------------------------------------------------------|---------------------------------------------------------------|
| Android có ARCore, trình duyệt Chrome | WebXR ngay trong trình duyệt; dự phòng là ứng dụng Scene Viewer | ten.glb                                                       |
| iPhone, iPad (Safari)                 | AR Quick Look                                                   | ten.usdz nếu có; không có thì tự chuyển từ GLB khi bấm nút AR |

Các bước triển khai:

1.  Chép ten.glb (và ten.usdz) vào 4a_webar/models/.

2.  Thử trên máy tính: python -m http.server 8000 trong thư mục
    4a_webar, mở http://localhost:8000/?m=ten.

3.  Đưa cả thư mục lên hosting có HTTPS (GitHub Pages, Netlify hoặc
    Cloudflare Pages đều có gói miễn phí).

4.  Mở https://\<địa chỉ\>/?m=ten&t=Tên hiển thị trên điện thoại, bấm
    “Xem trong không gian thật”, hướng camera xuống mặt bàn.

5.  Tạo mã QR từ link để dán cạnh vật thật hoặc đưa vào tài liệu.

AR trên web bắt buộc HTTPS
([model-viewer](https://modelviewer.dev/examples/augmentedreality)); mở
file trực tiếp từ máy chỉ xem được 3D.

Thao tác người dùng nhận được:

- **Trên trang (chưa bật AR):** kéo 1 ngón xoay tự do mọi hướng, chụm 2
  ngón phóng to.

- **Trong AR:** đi quanh vật để đổi góc nhìn; kéo 1 ngón dời vật, xoắn 2
  ngón xoay quanh trục đứng, chụm để đổi cỡ.

Giới hạn của 4A: trong AR chỉ xoay được quanh trục đứng, không nghiêng
được vật; không thêm được nút bấm, chú thích hay các bước thí nghiệm
riêng. Cần những thứ đó thì làm 4B.

Tình trạng kiểm thử: trang đã tải và hiển thị đúng mô hình mẫu ở chế độ
3D trên Chrome. Chế độ AR chưa được thử trên điện thoại thật; đây là
việc đầu tiên cần làm sau khi đưa lên hosting.

## Giai đoạn 4B — App Unity với AR Foundation

App Unity cho đủ cả hai yêu cầu ở mức cao nhất: mô hình neo vào mặt bàn
thật, và xoay, nghiêng, phóng to bằng chạm ngay trong AR. Hai script C#
trong 4b_unity/Scripts làm toàn bộ phần này; không cần viết thêm mã cho
bản đầu.

Phiên bản: Unity 6 (6000.x) với AR Foundation 6.x. Gói ARCore (Android)
hoặc ARKit (iOS) phải cùng số phiên bản với AR Foundation
([Unity](https://docs.unity3d.com/6/Documentation/Manual/xr-support-packages.html)).

Các bước dựng:

1.  Cài Unity 6 LTS qua Unity Hub, tích Android Build Support (và iOS
    Build Support nếu có máy Mac).

2.  Tạo dự án từ template AR Mobile; template cài sẵn AR Foundation,
    ARCore, ARKit và Input System.

3.  Trong Package Manager, chọn Add package by name, nhập
    com.unity.cloud.gltfast để Unity đọc được file .glb ([Unity
    glTFast](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.9/manual/installation.html)).

4.  Project Settings → XR Plug-in Management: bật ARCore ở tab Android,
    ARKit ở tab iOS.

5.  Kéo model.glb vào Assets; chép 2 file .cs vào Assets/Scripts.

6.  Trên đối tượng XR Origin, thêm AR Plane Manager (gán Plane Prefab),
    AR Raycast Manager, AR Anchor Manager và AR Place On Plane.

7.  Kéo mô hình vào ô Model Prefab; nhập chiều cao thật của vật (mét)
    vào Target Height Meters.

8.  Build Settings → Android → Build And Run với điện thoại đã bật USB
    debugging.

Hai script chia việc như sau:

| Script                 | Gắn vào                | Thao tác                        | Kết quả                                                        |
|------------------------|------------------------|---------------------------------|----------------------------------------------------------------|
| ARPlaceOnPlane.cs      | XR Origin              | Chạm nhanh 1 ngón lên mặt phẳng | Đặt mô hình, mặt trước quay về người xem; chạm chỗ khác để dời |
| ARObjectManipulator.cs | Tự gắn khi đặt mô hình | Kéo 1 ngón ngang                | Xoay quanh trục đứng                                           |
|                        |                        | Kéo 1 ngón dọc                  | Nghiêng vật để xem mặt trên, mặt đáy                           |
|                        |                        | Chụm hoặc mở 2 ngón             | Thu nhỏ, phóng to (0,2× đến 5×)                                |
|                        |                        | Xoắn 2 ngón                     | Xoay quanh trục đứng                                           |

Mô hình được gắn ARAnchor nên đứng yên trong không gian thật; người dùng
đi vòng quanh là thấy mặt bên và mặt sau đúng phối cảnh. Phóng to diễn
ra quanh tâm đáy nên vật luôn chạm mặt bàn; xoay diễn ra quanh tâm khối.

Hai điều kiện phần cứng: điện thoại Android phải nằm trong danh sách hỗ
trợ ARCore, và bản iOS chỉ build được trên máy Mac có Xcode.

Unity chạy được trên web, nhưng phần AR thì không: Unity 6 build ra nền
tảng Web và mở được trên Safari iOS 15+ và Chrome Android
([Unity](https://docs.unity3d.com/Manual/webgl-browsercompatibility.html)),
song Unity không hỗ trợ trực tiếp XR trên nền tảng Web
([Unity](https://docs.unity3d.com/6/Documentation/Manual/xr-support-packages.html)),
nên AR Foundation và hai script ở trên không chạy trong bản web. Muốn AR
từ Unity trên trình duyệt phải dùng công cụ bên thứ ba như WebXR Export
hoặc Needle Engine; các công cụ này dựa trên WebXR nên chủ yếu chạy trên
Chrome Android, và Unity không bảo đảm tương thích. Vì vậy kế hoạch tách
đôi: cần chạy bằng link thì dùng 4A, cần tương tác đầy đủ thì dùng app
4B.

Tình trạng kiểm thử: hai script được viết theo API của AR Foundation 6
và đã rà soát bằng mắt, nhưng chưa được biên dịch hay chạy trong Unity.
Nếu Console báo lỗi, gửi nguyên văn dòng lỗi để sửa.

## Phương án bổ sung — iPhone có cảm biến LiDAR

iPhone có LiDAR (dòng Pro, từ iPhone 12 Pro) thêm được hai thứ: quét ra
hình học của chính vật thật thay cho AI suy đoán, và hiển thị AR hiểu
được chiều sâu của không gian thật.

| Ứng dụng                        | Công cụ                                                                                                                                                     | Được gì so với kế hoạch gốc                                                                                     |
|---------------------------------|-------------------------------------------------------------------------------------------------------------------------------------------------------------|-----------------------------------------------------------------------------------------------------------------|
| Quét vật, thay giai đoạn 1–2    | Object Capture trong app [Reality Composer](https://apps.apple.com/app/id1462358802) của Apple (miễn phí): đi quanh vật theo hướng dẫn, xuất USDZ           | Lưới dựng từ ảnh và độ sâu thật nên mặt sau, mặt trên không bị bịa; USDZ mở thẳng bằng AR Quick Look            |
| Quét bằng app bên thứ ba        | Polycam, Scaniverse, KIRI Engine                                                                                                                            | Có cả chế độ LiDAR (hợp với không gian, vật lớn) và chế độ ảnh (hợp với vật nhỏ); nhiều lựa chọn định dạng xuất |
| Che khuất trong app Unity       | AR Occlusion Manager                                                                                                                                        | Bàn tay và đồ vật thật che được mô hình ảo, mô hình trông như nằm thật trên bàn                                 |
| Lưới môi trường trong app Unity | AR Mesh Manager, chỉ chạy trên máy có LiDAR ([Unity](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/samples/features/meshing.html)) | Đặt mô hình lên bề mặt bất kỳ, không chỉ mặt phẳng; dò bề mặt nhanh hơn, cả ở mặt bàn trơn                      |

Ba giới hạn cần biết trước:

- **Vật nhỏ:** độ phân giải của LiDAR trên điện thoại thô, hợp với căn
  phòng và đồ nội thất. Với dụng cụ cỡ vài căng-ti-mét, chi tiết đến từ
  ảnh chụp; LiDAR chủ yếu giúp giữ đúng tỉ lệ và định hướng.

- **Thuỷ tinh và bề mặt bóng:** vẫn khó như với AI tạo sinh; vẫn phải
  phủ mờ hoặc dựng tay.

- **Định dạng và dung lượng:** Object Capture xuất USDZ, cần chuyển sang
  GLB (Blender) cho Android và Unity; lưới quét thường nặng, phải giảm ở
  giai đoạn 3.

Cách kết hợp đề xuất: dùng Tripo hoặc Meshy cho vật đơn giản cần làm
nhanh, dùng quét bằng iPhone cho vật cần đúng hình dáng và kích thước.
Giai đoạn 3, 4A và 4B giữ nguyên cho cả hai nguồn mô hình.

## Lịch triển khai và chi phí

Bản chạy thử với 1 vật mất khoảng 5 ngày làm việc, trong đó 2 ngày đầu
đã cho kết quả AR xem được trên điện thoại. Số ngày là ước lượng cho
người chưa dùng Unity.

| Ngày | Việc                                                                             | Điểm kiểm tra cuối ngày                              |
|------|----------------------------------------------------------------------------------|------------------------------------------------------|
| 1    | Dựng góc chụp, chụp 4 ảnh, chạy check_photos.py; tạo mô hình trên Tripo và Meshy | Có model.glb đạt tiêu chí giai đoạn 2                |
| 2    | Hậu kỳ trong Blender; đưa trang WebAR lên hosting                                | Xem được mô hình trong AR trên 1 Android và 1 iPhone |
| 3    | Cài Unity 6, tạo dự án AR Mobile, build scene mẫu của template lên điện thoại    | App mẫu dò được mặt phẳng trên máy thật              |
| 4    | Thêm mô hình và 2 script; sửa lỗi biên dịch nếu có                               | Đặt, xoay, phóng to mô hình trong app                |
| 5    | Nghiệm thu theo danh sách ở mục cuối; ghi lại thông số chụp cho các vật sau      | Biên bản nghiệm thu bản chạy thử                     |

Sau bản chạy thử, mỗi vật mới chỉ lặp lại giai đoạn 1–3 và thêm 1 file
vào thư mục models.

| Hạng mục            | Chi phí                                        | Ghi chú                                                        |
|---------------------|------------------------------------------------|----------------------------------------------------------------|
| Tripo hoặc Meshy    | 0 USD để thử; khoảng 20 USD/tháng khi làm thật | Gói miễn phí: mô hình công khai, giấy phép CC BY 4.0           |
| Blender             | 0                                              | Mã nguồn mở                                                    |
| model-viewer        | 0                                              | Mã nguồn mở (Apache-2.0)                                       |
| Hosting trang WebAR | 0                                              | Gói miễn phí của GitHub Pages, Netlify hoặc Cloudflare Pages   |
| Unity               | 0 với gói Personal                             | Đơn vị cần tự đối chiếu điều kiện giấy phép Personal của Unity |
| Phát hành iOS       | Tài khoản Apple Developer trả phí              | Chỉ cần khi phát app cho nhiều iPhone; WebAR không cần         |
| Dụng cụ chụp        | Tự trang bị                                    | Giá đỡ điện thoại, phông nền, bàn xoay, 2 đèn                  |

## Rủi ro và tiêu chí nghiệm thu

Rủi ro lớn nhất nằm ở giai đoạn 2: AI tạo sinh cho hình dáng giống thật
nhưng không bảo đảm đúng kích thước và chi tiết nhỏ, và xử lý kém vật
trong suốt.

| Rủi ro                                 | Dấu hiệu                                 | Cách xử lý                                                                                                                 |
|----------------------------------------|------------------------------------------|----------------------------------------------------------------------------------------------------------------------------|
| Dụng cụ thuỷ tinh, vật bóng            | Lưới méo, thủng, mất thành bình          | Đổ nước màu hoặc phủ mờ khi chụp; với vật tròn xoay (cốc, bình, ống nghiệm), dựng tay trong Blender nhanh và chính xác hơn |
| Cần đúng kích thước, vạch chia, chữ số | Vạch chia nhoè, sai số, tỉ lệ lệch       | Chuyển sang quét photogrammetry (chụp 30–80 ảnh chồng lấn quanh vật) hoặc dựng tay và dán ảnh vạch chia thật               |
| Mặt sau hoặc đáy sai                   | Texture bịa, khối thừa ở mặt khuất       | Gửi đủ 4 góc; chụp lại góc bị sai                                                                                          |
| Mô hình nặng                           | AR giật, trang web tải lâu               | Giảm về 30.000 tam giác, texture 1.024 px                                                                                  |
| AR trôi, không dò được mặt bàn         | Mô hình trượt, không hiện lưới mặt phẳng | Dùng mặt bàn có vân hoặc trải tờ giấy có hoa văn, đủ sáng, lia máy chậm; tránh mặt kính và mặt trắng bóng                  |
| Điện thoại không hỗ trợ                | Nút AR không hiện                        | Kiểm tra máy trong danh sách ARCore; iPhone dùng Safari                                                                    |
| Giấy phép gói miễn phí                 | Mô hình công khai, phải ghi nguồn        | Nâng gói trả phí trước khi tạo mô hình dùng chính thức                                                                     |
| Script Unity lỗi biên dịch             | Console báo đỏ sau khi chép file         | Đối chiếu phiên bản AR Foundation (cần 6.0 trở lên); gửi dòng lỗi để sửa                                                   |

Tiêu chí nghiệm thu bản chạy thử:

- [ ] 4 ảnh qua check_photos.py với 0 lỗi

- [ ] model.glb dưới 10 MB, kích thước hiển thị lệch không quá 5% so với
  số đo thật

- [ ] WebAR chạy trên ít nhất 1 máy Android và 1 iPhone

- [ ] Đặt mô hình lên bàn, đi trọn 1 vòng 360°: mô hình đứng yên, thấy
  đúng mặt trước, bên, sau

- [ ] Trong app Unity: kéo 1 ngón xoay và nghiêng, chụm 2 ngón đổi cỡ,
  chạm nhanh dời vị trí

- [ ] App Unity chạy mượt, không giật khi xoay mô hình

## Nguồn tham khảo

- [Meshy — Multi-Image to 3D
  API](https://docs.meshy.ai/api/multi-image-to-3d): số ảnh đầu vào,
  tham số, định dạng xuất

- [Meshy — tài liệu tổng quan](https://docs.meshy.ai/en): định dạng
  xuất, gói miễn phí 100 credit

- [Tripo — Multiview to 3D Model
  API](https://developers.tripo3d.com/en/docs/generation-multiview-to-model/standard):
  4 góc front, left, back, right

- [Costbench — gói miễn phí
  Tripo](https://www.costbench.com/software/ai-3d-generation/tripo-ai/free-plan/):
  nguồn bên thứ ba về giá

- [3Dnatives — thử nghiệm
  Meshy](https://www.3dnatives.com/en/3dnatives-lab-meshy-ai-3d-model-generator-23092026/):
  giấy phép gói miễn phí

- [model-viewer — Augmented
  Reality](https://modelviewer.dev/examples/augmentedreality): các chế
  độ AR, tự tạo USDZ, yêu cầu HTTPS

- [Unity — Install AR Foundation
  6.3](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/project-setup/install-arfoundation.html):
  phiên bản Editor và gói ARCore/ARKit

- [Unity — XR
  packages](https://docs.unity3d.com/6/Documentation/Manual/xr-support-packages.html):
  các gói phải cùng phiên bản

- [Unity glTFast —
  Installation](https://docs.unity3d.com/Packages/com.unity.cloud.gltfast@6.9/manual/installation.html):
  import file GLB

- [Apple — Reality Composer](https://apps.apple.com/app/id1462358802):
  Object Capture trên iPhone có LiDAR, xuất USDZ

- [Unity — Meshing
  samples](https://docs.unity3d.com/Packages/com.unity.xr.arfoundation@6.3/manual/samples/features/meshing.html):
  meshing cần thiết bị có LiDAR

- [Polycam — LiDAR vs.
  Photogrammetry](https://poly.cam/blog/lidar-vs-photogrammetry-which-3d-scanning-method-is-right-for-you):
  chọn phương pháp quét theo không gian hay vật thể
