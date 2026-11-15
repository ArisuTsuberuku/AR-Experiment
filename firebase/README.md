# Firebase — nơi đặt gói thí nghiệm

Chưa có dự án Firebase. Tạo dự án là điểm dừng ⏸ chờ thầy quyết (tạo mới hay dùng chung dự án có sẵn).

Dự kiến dùng Firebase Hosting làm máy chủ file tĩnh:

```
/catalog.json                 Danh mục thí nghiệm (docs/05, mục 5)
/<mã>/<phiên bản>/            Một phiên bản gói, không ghi đè sau khi phát hành
    experiment.json
    model/scene.glb
    tracks/*.csv
    media/thumb.jpg
```

Quy trình phát hành một gói (viết thành script ở GĐ 5): kiểm tra gói → chép vào thư mục hosting theo `<mã>/<phiên bản>/` → cập nhật `catalog.json` → triển khai → mở app kiểm tra tải được.

Không đưa khoá dịch vụ, token vào repo.
