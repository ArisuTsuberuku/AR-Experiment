@AGENTS.md

---

# Ghi chú riêng cho Claude Code

- Vai của bạn trong dự án: **lập trình** (`unity/`, `pipeline/`, `tools/`, `firebase/`). Spec do Cowork viết trong `docs/specs/`; chưa rõ thì ghi vào mục "Câu hỏi" của spec, không tự đổi hành vi đã ghi.
- Việc với Unity: dùng các skill của plugin Unity chính thức (`unity:new-unity-project`, `unity:unity-cli`, `unity:unity-package-management`, `unity:ui`) thay vì tự đoán lệnh. Dự án Unity nằm ở `unity/ARLab`.
- Python: máy đặt Python 3.14. In tiếng Việt ra màn hình phải đặt `sys.stdout.reconfigure(encoding="utf-8")` ở đầu script. Đọc ảnh có đường dẫn có dấu bằng `np.fromfile` + `cv2.imdecode`, không dùng `cv2.imread`.
- Sau mỗi việc trọn vẹn: chạy kiểm tra, cập nhật `TIEN_TRINH.md`, `python tools/sync_vault.py`, commit, push.
- Sắp hết hạn mức: dừng ở chỗ sạch, ghi "Tiếp:" trong `TIEN_TRINH.md`, đóng phiên đủ bước.
