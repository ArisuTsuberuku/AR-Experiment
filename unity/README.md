# Unity — app AR Lab

Chưa có dự án. Tạo ở GĐ 1, đặt tại `unity/ARLab`.

Điều kiện trước khi tạo:

- Unity Hub và Unity 6.3 LTS (6000.3.x) kèm Android Build Support (OpenJDK, Android SDK & NDK). Cài ở ổ D.
- Plugin Unity chính thức cho Claude Code đã cài trên máy.
- Điện thoại Android hỗ trợ ARCore, đã bật gỡ lỗi USB.

Định hướng dự án:

| Hạng mục | Chọn |
|---|---|
| Mẫu dự án | AR Mobile |
| Package | AR Foundation 6.3, Google ARCore XR Plugin (cùng phiên bản), glTFast (`com.unity.cloud.gltfast`), Input System; Addressables thêm ở GĐ 5 nếu cần tầng 2 |
| Nền tảng | Android; kiến trúc ARM64, IL2CPP |
| Đường dẫn | Chỉ mở từ `D:\Dev\AR-Experiment\unity\ARLab` (không dấu) |

Claude Code tạo dự án bằng skill `unity:new-unity-project` và quản lý package bằng `unity:unity-package-management`. Thư mục sinh ra của Unity (`Library`, `Temp`, `Logs`, `Build`…) đã được loại khỏi git ở `.gitignore` gốc.
