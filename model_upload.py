#!/usr/bin/env python3
"""
把一個 GLB 壓成 KTX2，產生 AR APP 用的產品資料夾結構，並自動上傳到 PBR_3D repo。

用法:
    python model_upload.py --name SOFA --model path/to/model.glb --size 19 20 36
    (--size 對應  深 寬 高  公分)

預設行為 = 自動上傳:
    clone/更新 PBR_3D → 建立 SOFA/ → git add/commit/push

產出結構:
    SOFA/
      3d_model/<model>.glb     # 壓縮後(KTX2/UASTC)
      PICS/.gitkeep            # 空資料夾(放產品圖)
      Info.json

選項:
    --no-push        只 commit，不 push
    --out DIR        只在本機 DIR 產生資料夾，不做 git(離線模式)
    --no-compress    不壓縮，直接複製 GLB
"""
import argparse
import json
import os
import shutil
import subprocess
import sys
from pathlib import Path

DEFAULT_REPO = "https://github.com/TWAnnoyingKid/PBR_3D.git"


def _gt_cmd(extra):
    """呼叫 gltf-transform(Windows 走 cmd /c 才吃得到 .cmd)。"""
    base = ["cmd", "/c", "gltf-transform"] if os.name == "nt" else ["gltf-transform"]
    return base + extra


def has_gltf_transform():
    try:
        return subprocess.run(_gt_cmd(["--version"]), capture_output=True).returncode == 0
    except FileNotFoundError:
        return False


def has_ktx():
    """gltf-transform uastc 底層需要 Khronos 的 ktx CLI。"""
    try:
        return subprocess.run(["ktx", "--version"], capture_output=True).returncode == 0
    except FileNotFoundError:
        return False


def git(workdir, *args, check=True):
    r = subprocess.run(["git", "-C", str(workdir), *args])
    if check and r.returncode != 0:
        sys.exit(f"[X] git {' '.join(args)} 失敗")
    return r


def ensure_repo(repo_url, workdir):
    workdir = Path(workdir)
    if (workdir / ".git").is_dir():
        print(f"[git] 更新既有 clone: {workdir}")
        git(workdir, "pull", "--ff-only", check=False)
    else:
        print(f"[git] clone {repo_url} → {workdir}")
        if subprocess.run(["git", "clone", repo_url, str(workdir)]).returncode != 0:
            sys.exit("[X] git clone 失敗(檢查網路/權限)")
    return workdir


def fmt_num(v):
    return str(int(v)) if float(v).is_integer() else str(v)


def build_product(root, model, name, size, price, url, description, no_compress):
    model_dir = root / "3d_model"
    pics_dir = root / "PICS"
    if root.exists():
        print(f"[!] {root} 已存在，將覆寫其中的檔案")
    model_dir.mkdir(parents=True, exist_ok=True)
    pics_dir.mkdir(parents=True, exist_ok=True)

    out_glb = model_dir / model.name
    if no_compress:
        shutil.copy2(model, out_glb)
        print(f"[copy] 未壓縮 → {out_glb}")
    else:
        if not has_gltf_transform():
            sys.exit("[X] 找不到 gltf-transform。安裝: npm i -g @gltf-transform/cli (或加 --no-compress)")
        if not has_ktx():
            sys.exit("[X] gltf-transform uastc 需要 Khronos 的 ktx CLI，但找不到。\n"
                     "    Linux: 到 https://github.com/KhronosGroup/KTX-Software/releases 下載 .deb 後\n"
                     "           sudo apt install ./KTX-Software-*.deb\n"
                     "    或先加 --no-compress 跳過壓縮。")
        print("[ktx2] 壓縮中 (UASTC)...")
        if subprocess.run(_gt_cmd(["uastc", str(model), str(out_glb)])).returncode != 0 or not out_glb.exists():
            sys.exit("[X] gltf-transform 壓縮失敗")
        print(f"[ktx2] 完成 → {out_glb}")

    (pics_dir / ".gitkeep").write_text("", encoding="utf-8")  # git 不追蹤空資料夾

    if size:
        depth, width, height = size
        size_str = f"{fmt_num(depth)}深 x {fmt_num(width)}寬 x {fmt_num(height)}高 公分"
    else:
        size_str = ""   # 沒給尺寸 → 留空，之後可在 Info.json 補
    info = {
        "productName": name,
        "price": price,
        "url": url,
        "description": description or f"Local SVBRDF {name} model.",
        "sizeOptions": size_str,
    }
    (root / "Info.json").write_text(json.dumps(info, ensure_ascii=False, indent=2), encoding="utf-8")
    print(f"[info] {root / 'Info.json'}  ->  {info['sizeOptions']}")


def main():
    ap = argparse.ArgumentParser(description="壓縮 GLB、產生 AR 產品資料夾並上傳 PBR_3D")
    ap.add_argument("--name", required=True, help="產品名(= 資料夾名)")
    ap.add_argument("--model", required=True, help="來源 .glb 路徑")
    ap.add_argument("--size", nargs=3, type=float, default=None,
                    metavar=("DEPTH", "WIDTH", "HEIGHT"), help="深 寬 高 (公分)，選填")
    ap.add_argument("--repo", default=DEFAULT_REPO, help="PBR_3D git URL")
    ap.add_argument("--workdir", default=str(Path(__file__).parent / "pbr3d_repo"),
                    help="本機 clone 位置(會重複使用)")
    ap.add_argument("--out", default=None, help="離線模式：只在此目錄產生資料夾，不做 git")
    ap.add_argument("--no-push", action="store_true", help="只 commit 不 push")
    ap.add_argument("--no-compress", action="store_true", help="不壓縮，直接複製 GLB")
    ap.add_argument("--price", type=float, default=0)
    ap.add_argument("--url", default="")
    ap.add_argument("--description", default=None)
    args = ap.parse_args()

    model = Path(args.model)
    if not model.is_file() or model.suffix.lower() != ".glb":
        sys.exit(f"[X] 找不到或不是 .glb: {model}")

    # 離線模式：只產生資料夾
    if args.out:
        root = Path(args.out) / args.name
        build_product(root, model, args.name, args.size, args.price, args.url, args.description, args.no_compress)
        print(f"\n[done] {root}  (離線模式，未上傳)")
        return

    # 上傳模式
    workdir = ensure_repo(args.repo, args.workdir)
    root = workdir / args.name
    build_product(root, model, args.name, args.size, args.price, args.url, args.description, args.no_compress)

    git(workdir, "add", args.name)
    committed = git(workdir, "commit", "-m", f"Add/update {args.name}", check=False).returncode == 0
    if not committed:
        print("[git] 沒有變更可 commit")
    if args.no_push:
        print("[git] --no-push：已 commit，未推送")
    else:
        git(workdir, "push", "-u", "origin", "HEAD")  # -u origin HEAD：空 repo 第一次也能推
        print("[git] 已 push 到 PBR_3D")

    print(f"\n[done] {args.name} 上傳完成。記得補產品圖到 PICS/ 後再上傳一次。")


if __name__ == "__main__":
    main()
