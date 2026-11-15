#!/usr/bin/env python3
"""Sao tài liệu của repo sang vault Obsidian.

Repo là nguồn sự thật; vault chỉ là bản sao để tra cứu. Script này:
  - đọc danh sách tài liệu trong tools/vault_map.json,
  - thêm frontmatter theo chuẩn của vault (title, tags, type, created, updated,
    source_paths, related),
  - đổi liên kết markdown giữa các tài liệu thành [[wikilink]],
  - chỉ ghi lại ghi chú khi nội dung thay đổi,
  - không xoá, không đổi tên, không đụng tới ghi chú nào khác trong vault.

Cách dùng (chạy từ gốc repo):
    python tools/sync_vault.py            # đồng bộ
    python tools/sync_vault.py --check    # chỉ báo cáo, không ghi; mã thoát 1 nếu lệch
    python tools/sync_vault.py --vault "D:/duong/dan/vault"

Đường dẫn vault lấy theo thứ tự: tham số --vault, biến môi trường AR_VAULT_DIR,
config.local.json, config.example.json.
"""
from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import re
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
MAP_FILE = REPO / "tools" / "vault_map.json"
LINK_RE = re.compile(r"(?<!\!)\[([^\]]+)\]\(([^)\s]+)\)")


def read_text(path: Path) -> str:
    return path.read_text(encoding="utf-8-sig").replace("\r\n", "\n")


def find_vault(cli_value: str | None) -> Path:
    if cli_value:
        return Path(cli_value)
    if os.environ.get("AR_VAULT_DIR"):
        return Path(os.environ["AR_VAULT_DIR"])
    for name in ("config.local.json", "config.example.json"):
        cfg = REPO / name
        if cfg.exists():
            value = json.loads(read_text(cfg)).get("vault_dir")
            if value:
                return Path(value)
    raise SystemExit("Không tìm thấy đường dẫn vault (vault_dir).")


def convert_links(text: str, src: Path, notes_by_src: dict[str, str]) -> str:
    """Đổi liên kết tương đối tới tài liệu khác thành wikilink của vault."""

    def repl_factory(in_table: bool):
        def repl(m: re.Match) -> str:
            label, target = m.group(1), m.group(2)
            if re.match(r"^(https?:|mailto:|#)", target):
                return m.group(0)
            rel_target = target.split("#", 1)[0]
            try:
                resolved = (src.parent / rel_target).resolve().relative_to(REPO)
            except ValueError:
                return m.group(0)
            key = resolved.as_posix()
            if key in notes_by_src:
                sep = "\\|" if in_table else "|"
                return f"[[{notes_by_src[key]}{sep}{label}]]"
            return f"{label} (`{key}` trong repo)"

        return repl

    out, in_fence = [], False
    for line in text.split("\n"):
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            out.append(line)
            continue
        if in_fence:
            out.append(line)
            continue
        out.append(LINK_RE.sub(repl_factory(line.lstrip().startswith("|")), line))
    return "\n".join(out)


def yaml_list(items: list[str], quote: bool) -> str:
    if quote:
        return "[" + ", ".join(json.dumps(i, ensure_ascii=False) for i in items) + "]"
    return "[" + ", ".join(items) + "]"


def build_note(entry: dict, cfg: dict, notes_by_src: dict[str, str],
               created: str, updated: str) -> str:
    src = REPO / entry["src"]
    body = convert_links(read_text(src).strip("\n"), src, notes_by_src)
    related = list(dict.fromkeys(entry.get("related", []) + cfg.get("common_related", [])))
    related = [r for r in related if r != entry["note"]]
    front = [
        "---",
        f"title: {json.dumps(entry['title'], ensure_ascii=False)}",
        f"tags: {yaml_list(entry['tags'], quote=False)}",
        f"type: {entry.get('type', 'project')}",
        f"created: {created}",
        f"updated: {updated}",
        f"source_paths: {yaml_list([str(src)], quote=True)}",
        f"related: {yaml_list([f'[[{r}]]' for r in related], quote=True)}",
        "---",
    ]
    notice = [
        "> [!NOTE] Bản sao từ repo AR-Experiment",
        f"> Sinh tự động từ `{entry['src']}` bằng `tools/sync_vault.py`. "
        "**Không sửa ở đây**; sửa trong repo rồi đồng bộ lại.",
    ]
    footer = ["---", "**Liên kết trong Bộ não (MOC & Dự án):**"] + [f"- [[{r}]]" for r in related]
    return "\n".join(front + notice + ["", body, ""] + footer) + "\n"


def without_updated(text: str) -> str:
    return re.sub(r"^updated: .*$", "updated: -", text, count=1, flags=re.M)


def existing_field(text: str, field: str) -> str | None:
    m = re.search(rf"^{field}: (\S+)\s*$", text, flags=re.M)
    return m.group(1) if m else None


def main() -> int:
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")
    ap = argparse.ArgumentParser(description="Sao tài liệu repo sang vault Obsidian.")
    ap.add_argument("--check", action="store_true", help="chỉ báo cáo, không ghi")
    ap.add_argument("--vault", help="đường dẫn gốc của vault")
    args = ap.parse_args()

    cfg = json.loads(read_text(MAP_FILE))
    vault = find_vault(args.vault)
    if not vault.is_dir() or not ((vault / ".obsidian").exists() or (vault / "AGENTS.md").exists()):
        print(f"DỪNG: '{vault}' không phải vault Obsidian (thiếu .obsidian và AGENTS.md).")
        return 2
    out_dir = vault / cfg["subdir"]
    notes_by_src = {n["src"]: n["note"] for n in cfg["notes"]}
    today = dt.date.today().isoformat()

    missing_src = [n["src"] for n in cfg["notes"] if not (REPO / n["src"]).exists()]
    if missing_src:
        print("DỪNG: thiếu file nguồn: " + ", ".join(missing_src))
        return 2

    changed = 0
    for entry in cfg["notes"]:
        target = out_dir / f"{entry['note']}.md"
        old = read_text(target) if target.exists() else None
        created = (existing_field(old, "created") if old else None) or today
        old_updated = (existing_field(old, "updated") if old else None) or today
        candidate = build_note(entry, cfg, notes_by_src, created, old_updated)
        if old is not None and without_updated(old) == without_updated(candidate):
            print(f"  khớp      {entry['note']}")
            continue
        changed += 1
        if args.check:
            print(f"  LỆCH      {entry['note']}  ({'chưa có' if old is None else 'nội dung khác'})")
            continue
        out_dir.mkdir(parents=True, exist_ok=True)
        new_text = build_note(entry, cfg, notes_by_src, created, today)
        target.write_text(new_text, encoding="utf-8", newline="\n")
        print(f"  {'tạo mới ' if old is None else 'cập nhật'}  {entry['note']}")

    total = len(cfg["notes"])
    if args.check:
        print(f"Kiểm tra: {total - changed}/{total} ghi chú khớp với repo.")
        return 1 if changed else 0
    print(f"Xong: {changed} ghi chú được ghi, {total - changed} giữ nguyên → {out_dir}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
