#!/usr/bin/env python3
"""Kiểm tra máy và chuẩn bị các file theo máy cho dự án AR Experiment.

Chạy từ gốc repo:  python tools/kiem_tra_may.py

Việc làm:
  - tạo config.local.json từ config.example.json nếu chưa có,
  - tạo thư mục work/ nếu chưa có,
  - báo công cụ nào đã có, công cụ nào còn thiếu.
Script không cài đặt, không xoá, không sửa gì ngoài hai thứ trên.
"""
from __future__ import annotations

import importlib.util
import json
import os
import platform
import shutil
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent


def line(ok: bool | None, name: str, detail: str = "") -> None:
    mark = {True: "[ có  ]", False: "[THIẾU]", None: "[  ?  ]"}[ok]
    print(f"{mark} {name:<34} {detail}")


def load_config() -> dict:
    local, example = REPO / "config.local.json", REPO / "config.example.json"
    if not local.exists():
        data = json.loads(example.read_text(encoding="utf-8"))
        data.pop("_huong_dan", None)
        local.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n",
                         encoding="utf-8", newline="\n")
        print(f"Đã tạo {local.name} từ {example.name}; sửa đường dẫn nếu máy này khác.\n")
    return json.loads(local.read_text(encoding="utf-8"))


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    cfg = load_config()
    (REPO / cfg.get("work_dir", "work")).mkdir(exist_ok=True)

    print(f"Máy: {platform.node()}   Repo: {REPO}\n")

    print("— Vị trí repo —")
    repo_str = str(REPO)
    line(repo_str.isascii(), "Đường dẫn không dấu", repo_str)
    line("onedrive" not in repo_str.lower(), "Nằm ngoài OneDrive")

    print("\n— Dữ liệu và vault —")
    cap = Path(cfg["captures_root"])
    line(cap.is_dir(), "Kho dữ liệu gốc", str(cap))
    for key in ("photos_dir", "videos_dir"):
        line((cap / cfg[key]).is_dir(), f"  {cfg[key]}")
    vault = Path(cfg["vault_dir"])
    line(vault.is_dir(), "Vault Obsidian", str(vault))

    print("\n— Công cụ dòng lệnh —")
    for exe, note in [("git", ""), ("gh", "GitHub CLI"), ("node", ""),
                      ("ffmpeg", "cần cho GĐ 2"), ("firebase", "cần cho GĐ 5"),
                      ("adb", "đi kèm Android Build Support của Unity")]:
        found = shutil.which(exe)
        line(bool(found), exe, found or note)
    blender = Path(cfg.get("blender_exe", ""))
    line(blender.is_file(), "Blender", str(blender))

    print("\n— Unity —")
    hub_candidates = [
        Path(os.environ.get("ProgramFiles", "C:/Program Files")) / "Unity Hub" / "Unity Hub.exe",
        Path("D:/Unity Hub/Unity Hub.exe"),
        Path("D:/Program Files/Unity Hub/Unity Hub.exe"),
    ]
    hub = next((p for p in hub_candidates if p.is_file()), None)
    line(bool(hub) or None, "Unity Hub", str(hub) if hub else "không thấy ở vị trí thường gặp")
    unity_cli = shutil.which("unity")
    line(bool(unity_cli) or None, "Unity CLI", unity_cli or "không thấy trong PATH")
    project = REPO / "unity" / "ARLab" / "ProjectSettings"
    line(project.is_dir(), "Dự án unity/ARLab", "" if project.is_dir() else "tạo ở GĐ 1")

    print("\n— Python —")
    line(sys.version_info >= (3, 10), "Python", platform.python_version())
    for mod in ("numpy", "cv2", "scipy", "pandas", "matplotlib"):
        line(importlib.util.find_spec(mod) is not None, f"  {mod}")

    print("\n[  ?  ] nghĩa là không tự xác định được; kiểm tra bằng tay.")
    return 0


if __name__ == "__main__":
    sys.exit(main())
