#!/usr/bin/env bash
# Rebuilds Storm from art/blender/build_storm_warrior.py and copies every output where it is used.
#   ./art/blender/build_storm.sh            model + animations only
#   ./art/blender/build_storm.sh --render   also re-render the preview images (slow on CPU)
set -euo pipefail
ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
OUT="$(mktemp -d)"
trap 'rm -rf "$OUT"' EXIT

blender -b --factory-startup -P "$ROOT/art/blender/build_storm_warrior.py" -- --out "$OUT" "$@"

mkdir -p "$ROOT/client-unity/Assets/Art/Characters/Storm" "$ROOT/web-prototype/assets"
cp "$OUT/Storm.fbx" "$ROOT/client-unity/Assets/Art/Characters/Storm/Storm.fbx"   # Unity
cp "$OUT/Storm.glb" "$ROOT/web-prototype/assets/Storm.glb"                      # web prototype
cp "$OUT/Storm.blend" "$ROOT/art/blender/Storm.blend"                           # editable source
if [ -d "$OUT/renders" ]; then
  mkdir -p "$ROOT/docs/images/storm"
  for png in "$OUT"/renders/*.png; do
    name="$(basename "$png" .png)"
    if command -v convert >/dev/null; then
      convert "$png" -quality 85 "$ROOT/docs/images/storm/$name.jpg"   # keep the repo small
    else
      cp "$png" "$ROOT/docs/images/storm/"
    fi
  done
  cp "$OUT/renders/portrait.png" "$ROOT/web-prototype/assets/portrait.png"
fi
echo "Storm rebuilt."
