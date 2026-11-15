# AR Experiment — Cấu trúc thư mục và dữ liệu

Cập nhật: 15/11/2026

Dự án trải trên ba nơi. Lý do tách: Unity không build được Android từ đường dẫn có dấu, file lớn không nên vào git, và vault chỉ là nơi tra cứu. Xem [Quy ước đa agent](02-QUY-UOC-DA-AGENT.md) mục 1.

## 1. Repo — `D:\Dev\AR-Experiment`

GitHub: https://github.com/ArisuTsuberuku/AR-Experiment (nhánh `main`).

```
AR-Experiment/
├── AGENTS.md                 Quy tắc bắt buộc cho mọi agent (đọc đầu mỗi phiên)
├── CLAUDE.md                 Nạp AGENTS.md + ghi chú riêng cho Claude Code
├── README.md                 Giới thiệu ngắn, cách bắt đầu
├── TIEN_TRINH.md             Nhật ký tiến trình: Đang làm / Tiếp / Nhật ký
├── config.example.json       Mẫu đường dẫn theo máy → chép thành config.local.json
├── docs/
│   ├── 00-TONG-QUAN-HE-THONG.md
│   ├── 01-LO-TRINH.md
│   ├── 02-QUY-UOC-DA-AGENT.md
│   ├── 03-CAU-TRUC-THU-MUC-VA-DU-LIEU.md
│   ├── 04-CONG-CU-CONNECTOR-PLUGIN.md
│   ├── 05-DINH-DANG-GOI-THI-NGHIEM.md
│   ├── specs/                Mỗi thí nghiệm / tính năng một spec
│   ├── handoff/              Phiếu bàn giao giữa các agent
│   ├── reports/              Báo cáo kiểm thử, soát hình, sai số
│   └── archive/              Tài liệu giai đoạn khảo sát (10/11/2026)
├── pipeline/                 Python: từ ảnh và video ra gói thí nghiệm
│   ├── 0_nhap_du_lieu/       Chép phiên quay từ kho OneDrive vào work/
│   ├── 1_hieu_chuan/         Hiệu chuẩn máy quay
│   ├── 2_dong_bo/            Đồng bộ thời gian nhiều máy
│   ├── 3_theo_doi/           Theo dõi vật trong video
│   ├── 4_dung_3d/            Ảnh → mô hình 3D, hậu kỳ Blender
│   ├── 5_khop_mo_hinh/       Quỹ đạo → thông số vật lí + sai số
│   ├── 6_dong_goi/           Xuất gói theo định dạng chung
│   └── 7_kiem_chung/         Chiếu ngược, so với video gốc
├── experiments/              Gói thí nghiệm (đầu ra của pipeline, vào git)
│   ├── den-soi-dot/         Mô hình cấu tạo đèn sợi đốt (mô hình tĩnh)
│   └── roi-tu-do/
│       ├── experiment.json
│       ├── model/  tracks/  media/
├── unity/ARLab/              Dự án Unity 6.6 (URP + AR Foundation), xem unity/README.md
├── firebase/                 Cấu hình hosting, catalog.json
├── tools/                    sync_vault.py, kiem_tra_may.py
└── work/                     Vùng làm việc tạm — KHÔNG vào git
```

Vào git: mã, tài liệu, gói thí nghiệm (GLB dưới 10 MB, CSV, JSON). Không vào git: ảnh và video gốc, `work/`, thư mục sinh ra của Unity (`Library`, `Temp`, `Logs`, `Build`…), `config.local.json`, khoá và keystore.

## 2. Kho dữ liệu gốc — OneDrive

`D:\OneDrive\Công việc\04_Kho_Tai_Nguyen_Chung\05_Du_An_Code_&_EdTech\AR_experiment_website`

```
AR_experiment_website/
├── 00_DOC_TRUOC.md                       Giải thích kho này, trỏ về repo
├── 01_Anh_Da_Goc/                        ẢNH CHỤP NHIỀU GÓC — chép ảnh vào đây
│   ├── _MAU_ghi-chu-phien-chup.md
│   └── <mã-thí-nghiệm>/
│       └── <YYYY-MM-DD>_<lần>/
│           ├── 4-goc/                    front, left, back, right (cho AI tạo 3D)
│           ├── quet-vong/                30–80 ảnh chồng lấn (cho quét ảnh)
│           └── ghi-chu.md                Thiết bị, ánh sáng, số đo thật của vật
├── 02_Video_Goc/                         VIDEO GỐC THÍ NGHIỆM — chép video vào đây
│   ├── _MAU_ghi-chu-phien-quay.md
│   └── <mã-thí-nghiệm>/
│       └── <YYYY-MM-DD>_<lần>/
│           ├── cam1.mp4 [cam2.mp4 …]     Giữ nguyên file máy quay xuất ra
│           ├── hieu-chuan/               Ảnh / video bảng hiệu chuẩn của phiên này
│           └── ghi-chu.md                fps, tốc độ màn trập, khoảng cách, số đo thật
├── Ke-hoach-quy-trinh-Anh-2D-Mo-hinh-3D-AR.docx   (tài liệu khảo sát, giữ nguyên)
└── tien_trinh.md                                   (tài liệu khảo sát, giữ nguyên)
```

Quy tắc của kho:

- **Chỉ thêm, không sửa.** File gốc không cắt, không đổi tên, không nén lại. Xử lý làm trên bản chép trong `work/` của repo.
- Mỗi lần quay hoặc chụp là một **phiên**, thư mục tên `<YYYY-MM-DD>_<lần>`, ví dụ `2026-11-20_1`.
- Phiên nào cũng có `ghi-chu.md` (chép từ file mẫu). Thiếu phiếu ghi thì pipeline không có số đo thật để đặt tỉ lệ.
- Mã thí nghiệm viết thường, không dấu, nối bằng `-`, dùng chung ở mọi nơi: `roi-tu-do`, `con-lac-don`, `nem-ngang`.
- Video có mặt học sinh để ở kho này, không đưa lên repo công khai.
- Ngoại lệ đã có: 26 ảnh mô hình đèn sợi đốt nằm thẳng trong `01_Anh_Da_Goc/Den-soi-dot/` (phiên 15/11/2026, không có thư mục phiên). Giữ nguyên; các phiên sau theo đúng cấu trúc trên.

## 3. Vault Obsidian

`D:\OneDrive\Documents\Obsidian Vault\30-projects\13-AR-Experiment\`

| Ghi chú trong vault | Sinh từ file repo |
|---|---|
| `AR-Experiment-00-Tong-Quan-He-Thong` | `docs/00-TONG-QUAN-HE-THONG.md` |
| `AR-Experiment-01-Lo-Trinh` | `docs/01-LO-TRINH.md` |
| `AR-Experiment-02-Quy-Uoc-Da-Agent` | `docs/02-QUY-UOC-DA-AGENT.md` |
| `AR-Experiment-03-Cau-Truc-Thu-Muc-Va-Du-Lieu` | `docs/03-CAU-TRUC-THU-MUC-VA-DU-LIEU.md` |
| `AR-Experiment-04-Cong-Cu-Connector-Plugin` | `docs/04-CONG-CU-CONNECTOR-PLUGIN.md` |
| `AR-Experiment-05-Dinh-Dang-Goi-Thi-Nghiem` | `docs/05-DINH-DANG-GOI-THI-NGHIEM.md` |
| `AR-Experiment-Nhat-Ky-Tien-Trinh` | `TIEN_TRINH.md` |

`python tools/sync_vault.py` thêm frontmatter chuẩn của vault (`title`, `tags`, `type`, `created`, `updated`, `source_paths`, `related`), đổi liên kết giữa các tài liệu thành `[[wikilink]]`, và chỉ ghi lại khi nội dung đổi. Thêm tài liệu mới vào vault: khai báo trong `tools/vault_map.json`. Spec, phiếu, báo cáo không sao sang vault; khi một báo cáo đáng lưu làm tri thức thì khai báo thêm.

Ghi chú cũ `AR-Experiment-Website-Tien-Trinh` (trong `11-Cac-Du-An-RnD-Khac`) giữ nguyên làm tài liệu giai đoạn khảo sát.

## 4. Thiết lập trên một máy mới

1. `git clone https://github.com/ArisuTsuberuku/AR-Experiment.git D:\Dev\AR-Experiment` (đường dẫn không dấu, ngoài OneDrive).
2. `python tools/kiem_tra_may.py` — tạo `config.local.json` và `work/`, báo công cụ nào còn thiếu.
3. Sửa `config.local.json` nếu đường dẫn OneDrive, vault hoặc Blender của máy khác mặc định.
4. Mở dự án Unity từ `D:\Dev\AR-Experiment\unity\ARLab` (sau GĐ 1).

Máy MSI chưa thiết lập; làm 4 bước trên khi cần dùng.
