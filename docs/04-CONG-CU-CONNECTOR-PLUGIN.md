# AR Experiment — Công cụ, connector, plugin

Khảo sát ngày 15/11/2026 trên máy HOANGLINH-PC và tài khoản claude.ai của thầy. Trạng thái thay đổi theo thời gian; khi cài thêm thì cập nhật bảng và ghi ngày.

## 1. Đã có và dùng được cho dự án

### Plugin trên tài khoản claude.ai

| Plugin | Dùng vào việc gì trong dự án | Ghi chú |
|---|---|---|
| **Unity (chính thức)** | Tạo dự án, quản lý package, điều khiển Unity Editor và build từ dòng lệnh; các skill `unity:new-unity-project`, `unity:unity-cli`, `unity:unity-package-management`, `unity:ui`, `unity:localization`, `unity:asset-transformer-toolkit` | Cần Unity Editor trên máy (chưa cài). Chưa có skill riêng cho AR Foundation; phần AR dựa vào tài liệu Unity. Claude Code trên PC phải cài plugin riêng. |
| **3D CAD Converter** | Chuyển định dạng lưới 3D (USD/USDZ, OBJ, FBX, STL → glTF/GLB), giảm số tam giác, đổi trục đứng, xem thông tin file 3D | Chạy trên PC; đang hoạt động |
| **Firebase** | Tạo và quản lý dự án Firebase, Hosting cho gói thí nghiệm | Đang hoạt động; chưa có dự án cho AR Lab |
| **Desktop Commander** | Chạy lệnh trên PC: git, Python, ffmpeg, Blender chế độ nền | Đang hoạt động |
| **agent-discussion** | Cho các agent (Claude Code, Antigravity…) tranh luận phương án qua một file chung | Dùng khi bất đồng thiết kế |
| **engineering**, **product-management**, **design** | Ghi quyết định kiến trúc, chiến lược kiểm thử, viết spec, cập nhật lộ trình, rà soát giao diện app | Hỗ trợ quy trình |
| **Canva** | Thiết kế ảnh đánh dấu, phiếu hướng dẫn quay, áp phích có mã QR | Connector đã kết nối |
| **Cloudinary** | Lưu và chuyển mã video trên mây | Chưa cần; để dành nếu phải phát video gốc trong app |

### Connector trên tài khoản claude.ai

| Connector | Trạng thái | Dùng trong dự án |
|---|---|---|
| Google Drive, Google Docs | Đã kết nối | Nhận ảnh / video chép từ điện thoại nếu thầy dùng Drive |
| Gmail, Google Calendar | Đã kết nối | Không cần cho dự án |
| Canva | Đã kết nối | Như trên |
| Remote Desktop Commander | Đã kết nối | Như Desktop Commander |
| Google Sheets, Google Slides | Chưa kết nối | Sheets hữu ích nếu thầy muốn bảng số liệu đo trên mây |
| Microsoft 365 | Kết nối dở | Không cần: OneDrive đã có sẵn trên máy |

Đã tìm trong danh mục connector với từ khoá Unity, Blender, mô hình 3D, AR, quét ảnh: không có connector phù hợp (kết quả tìm chỉ phủ một phần danh mục). Không cần connector cho Blender vì Blender chạy được bằng dòng lệnh.

### Phần mềm trên máy HOANGLINH-PC

| Phần mềm | Phiên bản | Dùng vào |
|---|---|---|
| Blender | 5.1.2 (`D:\Blender`) | Hậu kỳ mô hình: tỉ lệ, gốc toạ độ, giảm lưới, xuất GLB |
| Python | 3.14.4, có OpenCV 4.14.0, numpy, matplotlib | Pipeline |
| Node.js | 24.14.1 | Firebase CLI |
| Firebase CLI | có trong PATH | Triển khai Hosting |
| Git, GitHub CLI | 2.53.0, 2.102.0 (đã đăng nhập `ArisuTsuberuku`) | Repo |
| Antigravity (IDE + CLI `agy`) | theo hồ sơ máy trong vault | Gemini 4 Argon, Hydrogen |

## 2. Còn thiếu

| Thứ cần | Để làm gì | Giai đoạn | Ghi chú |
|---|---|---|---|
| **Unity Hub + Unity 6.3 LTS** kèm Android Build Support | Dựng app | GĐ 1 | Cài ở ổ D (ổ C còn ít chỗ). Bản LTS 6.3 hiện là 6000.3.x |
| **AR Foundation 6.3 + Google ARCore XR Plugin** (cùng phiên bản), **glTFast** | AR, đọc GLB lúc chạy | GĐ 1 | Cài qua Package Manager sau khi có dự án |
| **Điện thoại Android có ARCore** | Thử app | GĐ 1 | Kiểm tra trong danh sách thiết bị hỗ trợ ARCore |
| **Plugin Unity cho Claude Code trên PC** | Claude Code điều khiển Unity | GĐ 1 | Bản trên claude.ai không tự có ở Claude Code máy trạm |
| **ffmpeg** | Tách khung hình, đọc thông tin video | GĐ 2 | Ví dụ `winget install Gyan.FFmpeg` |
| Thư viện Python: scipy, pandas | Khớp mô hình, xử lý bảng số liệu | GĐ 2 | `pip install scipy pandas`; `python tools/kiem_tra_may.py` báo gói nào thiếu |
| **Bảng hiệu chuẩn in sẵn, thước chuẩn, ảnh đánh dấu** | Hiệu chuẩn máy quay, đặt tỉ lệ, neo AR | GĐ 2 | In trên giấy cứng, đo lại kích thước sau khi in |
| **RealityScan** (bản 2.2 trở lên) | Quét ảnh ra mô hình 3D | GĐ 3 | Miễn phí cho thu nhập dưới 1 triệu USD/năm. Từ bản 2.2 hỗ trợ card AMD trên Windows; cần xác nhận RX 9060 XT trong danh sách |
| Tài khoản Meshy hoặc Tripo | Tạo 3D nhanh từ 4 ảnh | GĐ 3, tuỳ chọn | Gói miễn phí giới hạn và mô hình công khai |
| **Dự án Firebase cho AR Lab** | Đặt gói thí nghiệm | GĐ 5 | ⏸ thầy quyết định tạo mới hay dùng chung; kiểm tra hạn mức gói miễn phí hiện hành |
| Máy Mac + tài khoản Apple Developer | Bản iOS | GĐ 9 | Chưa cần |

Lưu ý phần cứng: máy dùng card AMD. Meshroom và bước dựng dày của COLMAP cần card NVIDIA (CUDA), nên không chọn hai công cụ này làm đường chính trên máy này.

## 3. Vấn đề phát hiện khi khảo sát

- Plugin `browser-use` trên PC không khởi động được (thiếu lệnh `uvx`). Không ảnh hưởng dự án.
- `ar-pipeline.zip` (bộ mã giai đoạn khảo sát: `check_photos.py`, `meshy_multiview.py`, trang WebAR, 2 script Unity) được nhắc trong `tien_trinh.md` nhưng không có trong thư mục dự án.

## 4. Thứ tự cài đề xuất

1. Unity Hub → Unity 6.3 LTS + Android Build Support (GĐ 1).
2. Plugin Unity cho Claude Code trên PC.
3. ffmpeg và hai thư viện Python còn thiếu (GĐ 2 — không phụ thuộc Unity).
4. RealityScan (GĐ 3).
5. Dự án Firebase (GĐ 5).

## Nguồn

- Unity, plugin chính thức cho Claude Code (9/9/2026): https://unity.com/blog/unity-plugin-for-claude-code
- Unity, vòng đời phiên bản: https://endoflife.date/unity
- Unity, AR Foundation cho 6000.3: https://docs.unity.com/en-us/engine/6000.3/manual/packages-list/packages-all/pack-safe/com-unity-xr-arfoundation
- RealityScan 2.2 hỗ trợ card AMD trên Windows: https://peterfalkingham.com/2026/06/25/realityscan-2-2-finally-brings-amd-gpu-support-to-windows/
- So sánh phần mềm quét ảnh miễn phí 2026: https://www.meshy.ai/blog/free-photogrammetry-software
