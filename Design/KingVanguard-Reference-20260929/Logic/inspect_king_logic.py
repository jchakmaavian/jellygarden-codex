"""Recover scoped, read-only King metadata evidence from the user-provided APK.

Metadata supplies names/signatures/default constants; it does not recover IL2CPP
method bodies or prove thresholds, transitions, selection probabilities, or calls.
"""
from pathlib import Path
import importlib.util
import hashlib
import json
import re
import struct
import zipfile

OUT = Path(__file__).resolve().parent
PROJECT = OUT.parents[2]
APK = PROJECT / "Design/Backup/apk/royal_thron_rfv387.apk"
BACKUP = PROJECT / "Design/Backup/Reference-UI-Characters/20260923-184326"

def main():
    spec = importlib.util.spec_from_file_location("reference_metadata", PROJECT / "Design/Inspection-Levels01-50/read_reference_metadata.py")
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    with zipfile.ZipFile(APK) as archive:
        entry = next(n for n in archive.namelist() if n.endswith("global-metadata.dat"))
        data = archive.read(entry)
    md = module.Metadata.__new__(module.Metadata)
    md.b = data
    md.strings, md.string_size = md.pair(24)
    md.types_offset, md.types_size = md.pair(160)
    md.methods_offset, _ = md.pair(48)
    md.parameters_offset, _ = md.pair(88)
    md.fields_offset, _ = md.pair(96)
    md.values_offset, _ = md.pair(72)
    md.field_defaults = md.defaults(64)
    md.parameter_defaults = md.defaults(56)
    types = []
    primitives = {}
    for index in range(md.types_size // 88):
        a = struct.unpack_from("<16I8H2I", data, md.types_offset + index * 88)
        name, namespace = md.string(a[0]), md.string(a[1])
        types.append((index, a, name, namespace))
        if namespace == "System" and name in ("Single", "Int32", "Boolean", "String"):
            primitives[a[2]] = name

    def default(defaults, index):
        if index not in defaults:
            return {}
        typ, relative = defaults[index]
        result = {"default_type_index": typ, "default_data_index": relative,
                  "default_raw_hex": data[md.values_offset + relative:md.values_offset + relative + 8].hex()}
        if relative == 0xffffffff:
            result["default_value"] = None
        elif primitives.get(typ) == "Int32":
            result["default_value"] = md.integer_default(relative)
        elif primitives.get(typ) == "Single":
            result["default_value"] = struct.unpack_from("<f", data, md.values_offset + relative)[0]
        elif primitives.get(typ) == "Boolean":
            result["default_value"] = bool(data[md.values_offset + relative])
        return result

    selected = []
    for index, a, name, namespace in types:
        if not namespace.startswith("Royal.") or not ("king" in name.lower() or ".king" in namespace.lower()):
            continue
        t = {"name": name, "namespace": namespace, "index": index, "fields": [], "methods": []}
        for fi in range(a[8], a[8] + a[18]):
            ni, typ, token = struct.unpack_from("<III", data, md.fields_offset + fi * 12)
            t["fields"].append({"name": md.string(ni), "type_index": typ, "field_index": fi, **default(md.field_defaults, fi)})
        for mi in range(a[9], a[9] + a[16]):
            method = struct.unpack_from("<7I4H", data, md.methods_offset + mi * 36)
            parameters = []
            for pi in range(method[4], method[4] + method[-1]):
                ni, token, typ = struct.unpack_from("<III", data, md.parameters_offset + pi * 12)
                parameters.append({"name": md.string(ni), "type_index": typ, **default(md.parameter_defaults, pi)})
            t["methods"].append({"name": md.string(method[0]), "method_index": mi, "parameters": parameters})
        selected.append(t)
    strings = [x.decode("utf-8", "replace") for x in data[md.strings:md.strings + md.string_size].split(b"\0")]
    hits = [{"index": i, "name": s, "context": strings[max(0, i - 4):i + 7]}
            for i, s in enumerate(strings)
            if re.search(r"KingAnimation|KingController|KingView|KingUi|KingState|King.*Happy|King.*Sad|King.*Idle|King.*Move|King.*Win|King.*Fail", s, re.I)]
    animation_source = OUT.parent / 'King.skel.spine38.json'
    decoded = json.loads(animation_source.read_text(encoding="utf8"))
    animations = [{"name": name, "duration_seconds": animation.get("duration"),
                   "evidence": "directly decoded packaged Spine animation data"}
                  for name, animation in decoded["animations"].items()]
    result = {"source_apk": str(APK.relative_to(PROJECT)), "source_metadata_entry": entry,
              "metadata_sha256": hashlib.sha256(data).hexdigest(), "metadata_version": struct.unpack_from("<I", data, 4)[0],
              "evidence_scope": "Names, signatures and metadata default constants only. No decompiled IL2CPP method bodies; no device-runtime behavior verification.",
              "primitive_type_indices": primitives, "types": selected, "string_context": hits,
              "animation_source": str(animation_source.relative_to(PROJECT)),
              "animations": animations}
    (OUT / "king-metadata-evidence.json").write_text(json.dumps(result, indent=2, ensure_ascii=False), encoding="utf8")
    print(json.dumps({"types": [{"name": t["namespace"] + "." + t["name"], "methods": len(t["methods"])} for t in selected], "animations": animations}, indent=2, ensure_ascii=True))

if __name__ == "__main__":
    main()
