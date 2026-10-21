"""
Bundles the browser prototype into one self-contained HTML file:
Storm.glb (base64) and the HUD portrait (data URI) are inlined into index.src.html.

    python3 web-prototype/build.py [output.html]

Default output: web-prototype/dist/index.html (open it in any modern browser).
Rebuild the model first with art/blender/build_storm.sh if the character changed.
"""
import base64
import io
import pathlib
import sys

HERE = pathlib.Path(__file__).resolve().parent
SOURCE = HERE / "index.src.html"
GLB = HERE / "assets" / "Storm.glb"
PORTRAIT = HERE / "assets" / "portrait.png"


def portrait_data_uri() -> str:
    if not PORTRAIT.exists():
        return ""
    try:
        from PIL import Image  # optional: shrink to a small JPEG
    except ImportError:
        return "data:image/png;base64," + base64.b64encode(PORTRAIT.read_bytes()).decode()
    image = Image.open(PORTRAIT).convert("RGB")
    image.thumbnail((160, 160))
    buffer = io.BytesIO()
    image.save(buffer, "JPEG", quality=85)
    return "data:image/jpeg;base64," + base64.b64encode(buffer.getvalue()).decode()


def main() -> None:
    out = pathlib.Path(sys.argv[1]) if len(sys.argv) > 1 else HERE / "dist" / "index.html"
    html = SOURCE.read_text(encoding="utf-8")
    html = html.replace("__STORM_GLB_BASE64__", base64.b64encode(GLB.read_bytes()).decode())
    html = html.replace("__STORM_PORTRAIT__", portrait_data_uri())
    out.parent.mkdir(parents=True, exist_ok=True)
    out.write_text(html, encoding="utf-8")
    print(f"Wrote {out} ({out.stat().st_size / 1e6:.1f} MB)")


if __name__ == "__main__":
    main()
