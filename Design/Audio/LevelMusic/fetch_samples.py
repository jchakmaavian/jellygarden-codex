"""Downloads the orchestral samples the epic level music uses from VSCO 2 Community Edition
(Versilian Studios, https://github.com/sgossner/VSCO-2-CE, CC0 1.0 public domain).

Usage: python fetch_samples.py <destDir>
Only the listed folders/files are fetched (about 500 MB); existing files are skipped.
"""
import json, os, sys, urllib.parse, urllib.request
from concurrent.futures import ThreadPoolExecutor

REPO = "sgossner/VSCO-2-CE"
FOLDERS = [
    "Strings/Cello Section/spic", "Strings/Cello Section/susvib",
    "Strings/Viola Section/spic", "Strings/Violin Section/Spic", "Strings/Violin Section/susVib",
    "Strings/Solo Contrabass/Spic", "Strings/Harp",
    "Brass/F Horn/sus", "Brass/F Horn/stac", "Brass/Tenor Trombone/sus",
    "Percussion/Timpani", "Percussion/Timpani/Rolls",
    "VSCO 1 Percussion/drums/bass", "VSCO 1 Percussion/drums/tenor/tenor_lower",
    "VSCO 1 Percussion/drums/other/ethnic/giant",
]
FILES = [
    "VSCO 1 Percussion/varMetal/Cymbals/clash/crash_hit_ff_loose.wav",
    "VSCO 1 Percussion/varMetal/Cymbals/clash/crash_hit_fff_loose.wav",
    "VSCO 1 Percussion/varMetal/Cymbals/susp/susp_hit_softmall_roll2_cresc.wav",
    "VSCO 1 Percussion/varMetal/Cymbals/susp/susp_hit_softmall_ff.wav",
]

def main(dest):
    tree = json.load(urllib.request.urlopen(f"https://api.github.com/repos/{REPO}/git/trees/master?recursive=1"))["tree"]
    want = [e["path"] for e in tree if e["type"] == "blob" and e["path"].lower().endswith(".wav")
            and (e["path"] in FILES or any(e["path"].startswith(f + "/") and "/" not in e["path"][len(f) + 1:] or
                                            (f.endswith("giant") and e["path"].startswith(f + "/")) for f in FOLDERS))]
    def get(path):
        out = os.path.join(dest, *path.split("/"))
        if os.path.exists(out) and os.path.getsize(out) > 0: return 0
        os.makedirs(os.path.dirname(out), exist_ok=True)
        url = f"https://raw.githubusercontent.com/{REPO}/master/" + urllib.parse.quote(path)
        data = urllib.request.urlopen(url).read()
        with open(out, "wb") as f: f.write(data)
        return len(data)
    with ThreadPoolExecutor(12) as ex: total = sum(ex.map(get, want))
    print(f"{len(want)} samples, {total / 1e6:.0f} MB downloaded into {dest}")

if __name__ == "__main__":
    main(sys.argv[1] if len(sys.argv) > 1 else "vsco")
