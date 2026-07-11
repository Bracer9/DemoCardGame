# PNG Asset Compression

Use the project-local script to reduce PNG download and load size while keeping the existing paths and filenames.

## Preview the recommended pass

From the project root:

```powershell
python tools/compress-png-assets.py
```

The default adaptive mode targets at least 50% file-size reduction by converting full-color PNG data to an optimized palette. It tries 256 colors first and only uses a smaller palette when needed to reach the target. This is suitable for relics and other art displayed at small UI sizes.

The script never changes width or height, including for relics, statues, portraits, or future asset groups. The default is a dry run.

## Apply the pass

```powershell
python tools/compress-png-assets.py --apply
```

The script writes beside each source first, validates the PNG, then replaces the source in place. It never renames or moves an asset. If a lossless candidate has changed pixels, it is rejected.

Useful variants:

```powershell
# Compress a selected group by at least 50% without changing dimensions.
python tools/compress-png-assets.py assets/ui/relics --apply

# Process only selected asset groups.
python tools/compress-png-assets.py assets/ui/relics assets/New_Portraits --apply

# Strict pixel-identical recompression (usually saves much less space).
python tools/compress-png-assets.py --lossless --apply

# Keep common color/profile metadata if the source needs it.
python tools/compress-png-assets.py --keep-metadata --apply
```

The script needs Python 3 and Pillow. No project runtime or asset reference needs to change because filenames, relative paths, width, and height remain the same.
