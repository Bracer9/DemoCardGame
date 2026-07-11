#!/usr/bin/env python3
"""Safely optimize PNG assets without changing their filenames or dimensions.

The default mode is a dry run. Use --apply to replace an image only when the
temporary PNG is valid and smaller than the original.
"""

from __future__ import annotations

import argparse
import hashlib
import os
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable

from PIL import Image, ImageFile


ImageFile.LOAD_TRUNCATED_IMAGES = False


@dataclass
class Result:
    path: Path
    status: str
    before: int = 0
    after: int = 0
    old_size: tuple[int, int] | None = None
    new_size: tuple[int, int] | None = None
    colors: int | None = None
    reason: str = ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description="Losslessly optimize PNGs and optionally downscale oversized game art."
    )
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        help="Files or directories to scan. Defaults to assets.",
    )
    parser.add_argument(
        "--apply",
        action="store_true",
        help="Replace originals when the generated PNG is valid and smaller.",
    )
    parser.add_argument(
        "--lossless",
        action="store_true",
        help="Disable palette quantization and only recompress identical pixels.",
    )
    parser.add_argument(
        "--keep-metadata",
        action="store_true",
        help="Keep common color/profile metadata. By default non-visual metadata is removed for smaller files.",
    )
    parser.add_argument(
        "--target-savings",
        type=float,
        default=50.0,
        metavar="PERCENT",
        help="Required file-size reduction for adaptive compression (default: 50).",
    )
    return parser.parse_args()


def iter_pngs(paths: Iterable[Path]) -> list[Path]:
    files: set[Path] = set()
    for path in paths:
        if path.is_file() and path.suffix.lower() == ".png":
            files.add(path)
        elif path.is_dir():
            files.update(item for item in path.rglob("*.png") if item.is_file())
    return sorted(files, key=lambda item: str(item).lower())


def pixel_fingerprint(image: Image.Image) -> str:
    """Fingerprint pixels plus palette-related data to verify lossless output."""
    digest = hashlib.sha256()
    digest.update(image.mode.encode("ascii", errors="replace"))
    digest.update(f"{image.width}x{image.height}".encode("ascii"))
    digest.update(image.tobytes())
    if image.mode == "P":
        digest.update(bytes(image.getpalette() or []))
    transparency = image.info.get("transparency")
    if transparency is not None:
        digest.update(repr(transparency).encode("utf-8"))
    return digest.hexdigest()


def save_kwargs(image: Image.Image, keep_metadata: bool) -> dict:
    if not keep_metadata:
        # PNG metadata such as XMP, EXIF and DPI is not needed by the game and
        # can be surprisingly large. Pixel data and transparency are retained.
        kwargs = {}
        if "transparency" in image.info:
            kwargs["transparency"] = image.info["transparency"]
        return kwargs

    kwargs = {}
    for key in ("icc_profile", "exif", "dpi", "transparency", "gamma"):
        value = image.info.get(key)
        if value is not None:
            kwargs[key] = value
    return kwargs


def make_candidate(
    source: Path,
    destination: Path,
    new_size: tuple[int, int],
    keep_metadata: bool,
    colors: int | None,
) -> tuple[tuple[int, int], str]:
    with Image.open(source) as image:
        image.load()
        original_size = image.size
        original_fingerprint = pixel_fingerprint(image)
        if new_size != original_size:
            raise ValueError("dimension changes are forbidden")

        working = image
        if colors is not None:
            if image.mode in {"RGBA", "LA"}:
                working = image.convert("RGBA").quantize(
                    colors=colors,
                    method=Image.Quantize.FASTOCTREE,
                    dither=Image.Dither.FLOYDSTEINBERG,
                )
            elif image.mode != "P":
                working = image.convert("RGB").quantize(
                    colors=colors,
                    method=Image.Quantize.MEDIANCUT,
                    dither=Image.Dither.FLOYDSTEINBERG,
                )

        working.save(
            destination,
            format="PNG",
            optimize=True,
            # Level 6 keeps PNG output lossless while avoiding the very slow
            # exhaustive compression pass on a large batch of illustrations.
            compress_level=6,
            **save_kwargs(working, keep_metadata),
        )

    with Image.open(destination) as check:
        check.load()
        if check.size != new_size:
            raise ValueError(f"unexpected dimensions {check.size}, expected {new_size}")
        if colors is None and pixel_fingerprint(check) != original_fingerprint:
            raise ValueError("pixel fingerprint changed during lossless compression")

    return new_size, original_fingerprint


def percent_saved(before: int, after: int) -> float:
    return (before - after) / before * 100 if before else 0.0


def process(path: Path, args: argparse.Namespace) -> Result:
    before = path.stat().st_size
    temp_path: Path | None = None
    try:
        with Image.open(path) as image:
            image.load()
            old_size = image.size
        # This tool is intentionally lossless in dimensions as well as pixels.
        new_size = old_size

        with tempfile.NamedTemporaryFile(
            prefix=f".{path.stem}.", suffix=".png", dir=path.parent, delete=False
        ) as temporary:
            temp_path = Path(temporary.name)

        palette_sizes = [None] if args.lossless else [256, 192, 128, 96, 64]
        chosen_colors: int | None = None
        after = before
        for colors in palette_sizes:
            make_candidate(path, temp_path, new_size, args.keep_metadata, colors)
            candidate_size = temp_path.stat().st_size
            after = candidate_size
            chosen_colors = colors
            if args.lossless or percent_saved(before, candidate_size) >= args.target_savings:
                break

        savings = percent_saved(before, after)
        if after >= before:
            return Result(path, "skipped", before, after, old_size, new_size, chosen_colors, "candidate is not smaller")
        if not args.lossless and savings < args.target_savings:
            return Result(
                path,
                "skipped",
                before,
                after,
                old_size,
                new_size,
                chosen_colors,
                f"savings {savings:.2f}% below --target-savings",
            )

        if args.apply:
            os.replace(temp_path, path)
            temp_path = None
            status = "updated"
        else:
            status = "preview"
        return Result(path, status, before, after, old_size, new_size, chosen_colors)
    except Exception as error:  # Report one bad asset and continue with the batch.
        return Result(path, "error", before, 0, reason=str(error))
    finally:
        if temp_path is not None:
            temp_path.unlink(missing_ok=True)


def format_bytes(value: int) -> str:
    units = ("B", "KB", "MB", "GB")
    amount = float(value)
    for unit in units:
        if amount < 1024 or unit == units[-1]:
            return f"{amount:.2f} {unit}"
        amount /= 1024
    return f"{value} B"


def main() -> int:
    args = parse_args()
    if not 0 <= args.target_savings < 100:
        raise SystemExit("--target-savings must be between 0 and 100")

    roots = args.paths or [Path("assets")]
    files = iter_pngs(roots)
    if not files:
        print("No PNG files found.")
        return 0

    mode = "APPLY" if args.apply else "DRY RUN"
    compression = "lossless" if args.lossless else f"adaptive target={args.target_savings:g}%"
    print(f"PNG compression: {mode} | {compression} | dimensions=preserved | files={len(files)}")
    results = [process(path, args) for path in files]

    changed = [result for result in results if result.status in {"updated", "preview"}]
    errors = [result for result in results if result.status == "error"]
    for result in results:
        if result.status in {"updated", "preview"}:
            size_note = ""
            if result.old_size != result.new_size:
                size_note = f" | {result.old_size[0]}x{result.old_size[1]} -> {result.new_size[0]}x{result.new_size[1]}"
            palette_note = f" | palette={result.colors}" if result.colors is not None else ""
            print(
                f"{result.status.upper():7} {result.path} | "
                f"{format_bytes(result.before)} -> {format_bytes(result.after)} "
                f"(-{percent_saved(result.before, result.after):.1f}%){palette_note}{size_note}"
            )
        elif result.status == "error":
            print(f"ERROR   {result.path} | {result.reason}")

    before_total = sum(result.before for result in changed)
    after_total = sum(result.after for result in changed)
    print(
        f"Summary: {len(changed)} candidate(s), {len(errors)} error(s), "
        f"{format_bytes(before_total)} -> {format_bytes(after_total)} "
        f"(-{percent_saved(before_total, after_total):.1f}%)."
    )
    if not args.apply and changed:
        print("Dry run only. Add --apply to replace the original files; filenames are preserved.")
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
