# AR Experiment — Định dạng gói thí nghiệm

Bản nháp **v0.1** (15/11/2026). Chốt thành v1.0 sau khi GĐ 2 và GĐ 4 chạy được với thí nghiệm rơi tự do. Đổi định dạng là điểm dừng ⏸ chờ thầy duyệt.

Gói thí nghiệm là hợp đồng giữa pipeline (bên tạo) và app (bên phát). App không biết gì về một thí nghiệm ngoài những gì gói mô tả. Vì vậy thêm thí nghiệm mới chỉ là thêm một gói. Bối cảnh: [Tổng quan](00-TONG-QUAN-HE-THONG.md) mục 7.

## 1. Cấu trúc một gói

```
experiments/<mã>/
├── experiment.json        Mô tả gói (bắt buộc)
├── model/
│   └── scene.glb          Phần đứng yên: giá, thước, bàn (mét, đã đặt gốc toạ độ)
├── tracks/
│   └── <vật>.csv          Chuyển động của từng vật theo thời gian
└── media/
    ├── thumb.jpg          Ảnh đại diện trong danh sách của app
    └── …                  Tuỳ chọn: video gốc bản nhẹ để đối chiếu
```

## 2. Quy ước chung

| Hạng mục | Quy ước |
|---|---|
| Đơn vị | mét, giây, độ; khối lượng kg |
| Hệ trục trong gói | Theo glTF: thuận tay phải, `+Y` hướng lên. App tự đổi sang hệ trục của Unity khi nạp |
| Gốc toạ độ | Tâm ảnh đánh dấu đặt trên bàn; mặt ảnh là mặt phẳng `y = 0` |
| Mốc thời gian | `t = 0` là sự kiện ghi ở `timeline.t0` (ví dụ lúc thả vật) |
| Tỉ lệ | 1:1 với thí nghiệm thật |
| Mã hoá | UTF-8; số thập phân dùng dấu chấm |
| Phiên bản gói | `version` theo dạng `chính.phụ.sửa`; số liệu đổi thì tăng `phụ` |

## 3. `experiment.json`

```json
{
  "schemaVersion": "0.1",
  "id": "roi-tu-do",
  "title": "Rơi tự do",
  "subject": { "grades": [10], "topic": "co-hoc" },
  "version": "0.0.0",
  "status": "draft",
  "minAppVersion": "0.1.0",

  "world": { "handedness": "right", "up": "+Y", "unit": "m" },
  "anchor": { "type": "image-marker", "markerId": "arlab-01", "physicalWidthM": 0.15 },

  "scene": { "staticModel": "model/scene.glb" },
  "objects": [
    {
      "id": "bi",
      "label": "Viên bi thép",
      "shape": { "type": "sphere", "radiusM": 0.01 },
      "track": "tracks/bi.csv"
    }
  ],

  "timeline": { "t0": "tha-vat", "durationS": 0.45, "sampleRateHz": 240 },

  "overlays": [
    { "type": "trail", "object": "bi", "everyS": 0.05 },
    { "type": "vector", "object": "bi", "quantity": "velocity" },
    { "type": "graph", "object": "bi", "x": "t", "y": "y" },
    { "type": "ruler", "from": [0, 0, 0], "to": [0, 1, 0] }
  ],

  "results": [
    { "symbol": "g", "value": 9.79, "uncertainty": 0.05, "unit": "m/s^2",
      "method": "khớp y = y0 + v0·t − ½·g·t²" }
  ],

  "provenance": {
    "session": "02_Video_Goc/roi-tu-do/2026-11-20_1",
    "cameras": 1,
    "pipelineVersion": "0.1.0",
    "reprojectionErrorPx": 1.2,
    "createdAt": "2026-11-20"
  }
}
```

Các số trong ví dụ chỉ để minh hoạ cấu trúc, không phải số đo.

| Trường | Bắt buộc | Ý nghĩa |
|---|---|---|
| `schemaVersion` | Có | Phiên bản định dạng; app từ chối bản nó không hiểu |
| `id` | Có | Mã thí nghiệm, trùng tên thư mục |
| `title`, `subject` | Có | Hiển thị và lọc trong app |
| `version`, `status` | Có | `status`: `draft` (chưa có số liệu thật), `review` (chờ kiểm chứng), `published` |
| `minAppVersion` | Có | App cũ hơn thì báo cập nhật |
| `anchor` | Có | Cách neo cảnh vào thế giới thật; `physicalWidthM` là bề rộng ảnh đánh dấu sau khi in |
| `scene.staticModel` | Không | Thí nghiệm chỉ có vật chuyển động thì bỏ |
| `objects[]` | Có | Vật chuyển động: hình cơ bản (`sphere`, `box`, `cylinder`) hoặc `model` trỏ tới GLB riêng |
| `objects[].track` | Có | File quỹ đạo |
| `timeline` | Có | Độ dài và tần số lấy mẫu |
| `overlays[]` | Không | Lớp phủ app được phép hiển thị; loại app chưa biết thì bỏ qua |
| `results[]` | Không | Đại lượng đo được, luôn kèm `uncertainty` và `method` |
| `provenance` | Có khi `status` khác `draft` | Truy nguồn: phiên quay, số máy, phiên bản pipeline, sai số chiếu ngược |

## 4. File quỹ đạo `tracks/<vật>.csv`

```
t,x,y,z,qx,qy,qz,qw
0.000000,0.0000,1.0000,0.0000,0,0,0,1
0.004167,0.0000,0.9999,0.0000,0,0,0,1
```

- `t` tính bằng giây từ `t0`; `x,y,z` bằng mét trong hệ trục của gói.
- `qx,qy,qz,qw` là hướng của vật (quaternion); vật không quay thì để `0,0,0,1` hoặc bỏ 4 cột.
- Mẫu cách đều theo `sampleRateHz`. App nội suy giữa hai mẫu khi phát chậm.
- Đây là số liệu **đo**. Đường cong lí thuyết, nếu cần hiển thị, do app vẽ từ `results`, không trộn vào file này.

## 5. Danh mục trên máy chủ `catalog.json`

```json
{
  "catalogVersion": 1,
  "updatedAt": "2026-11-20",
  "experiments": [
    { "id": "roi-tu-do", "title": "Rơi tự do", "version": "1.0.0",
      "minAppVersion": "0.1.0", "path": "roi-tu-do/1.0.0/", "sizeBytes": 0 }
  ]
}
```

App tải `catalog.json` khi mở, so `version` với bản đang lưu, tải gói mới về bộ nhớ đệm. Mỗi phiên bản gói nằm trong thư mục riêng (`<mã>/<phiên bản>/`) và không bị ghi đè sau khi phát hành, để máy đang dùng bản cũ không hỏng giữa chừng.

## 6. Việc phải quyết khi chốt v1.0

- Quy tắc đổi hệ trục khi nạp vào Unity (kiểm bằng một gói thử có 3 điểm biết trước toạ độ).
- Cách mô tả thí nghiệm nhóm B (mô hình tham số): thêm khối `model` với công thức và thông số, hay loại `objects` mới.
- Nhiều lần đo trong một gói (để học sinh so sánh) hay mỗi lần đo một gói.
- Có ký số / mã băm cho gói để app kiểm tra toàn vẹn không.
