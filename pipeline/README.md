# Pipeline — từ ảnh và video ra gói thí nghiệm

Chưa có mã. Thư mục con chia theo bước; mỗi bước là một script đọc kết quả bước trước trong `work/<mã>/<phiên>/` và ghi kết quả của mình vào đó. Bước cuối ghi gói vào `experiments/<mã>/`.

| Bước | Thư mục | Vào | Ra |
|---|---|---|---|
| 0 | `0_nhap_du_lieu/` | Phiên quay trong kho OneDrive (`config.local.json` → `captures_root`) | Bản chép trong `work/`, thông tin video (fps, độ phân giải, số khung) |
| 1 | `1_hieu_chuan/` | Ảnh / video bảng hiệu chuẩn | Thông số trong và vị trí từng máy quay |
| 2 | `2_dong_bo/` | Video nhiều máy | Độ lệch thời gian giữa các máy (bỏ qua khi 1 máy) |
| 3 | `3_theo_doi/` | Video đã chép | Toạ độ điểm ảnh của vật theo từng khung |
| 4 | `4_dung_3d/` | Ảnh đa góc | `model/scene.glb` đúng tỉ lệ mét (RealityScan hoặc AI tạo 3D, hậu kỳ Blender) |
| 5 | `5_khop_mo_hinh/` | Toạ độ điểm ảnh + hiệu chuẩn | Quỹ đạo 3D, thông số vật lí, sai số |
| 6 | `6_dong_goi/` | Kết quả các bước | `experiments/<mã>/` theo `docs/05-DINH-DANG-GOI-THI-NGHIEM.md` |
| 7 | `7_kiem_chung/` | Gói + video gốc | Ảnh chồng cảnh dựng lên video, sai số chiếu ngược |

Quy ước cho người viết mã:

- Không ghi vào kho OneDrive. Không sửa file gốc.
- Mỗi script chạy lại được nhiều lần cho cùng kết quả.
- Đọc ảnh ở đường dẫn có dấu bằng `np.fromfile` + `cv2.imdecode`.
- In tiếng Việt: đặt `sys.stdout.reconfigure(encoding="utf-8")` ở đầu script.
- Đại lượng đo luôn đi kèm sai số và cách tính.
