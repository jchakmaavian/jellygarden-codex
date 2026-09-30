"""Decode a bounded reference atlas with the existing Unity ASTC decoder DLL."""
from pathlib import Path
import ctypes as C
import csv
import re
import struct
from PIL import Image

BASE = Path(__file__).resolve().parent
FILES = BASE / "ReferencePackage/datapack.unity3d_data"
blob = (FILES / "resources.assets").read_bytes()
rows = list(csv.DictReader((BASE / "reference-asset-names.csv").open(encoding="utf-8")))

lib = C.CDLL(r"C:\Program Files\Unity\Hub\Editor\6000.5.10f1\Editor\Data\Tools\astcenc-sse2.dll")
lib.astcenc_config_init.argtypes = [C.c_int, C.c_uint, C.c_uint, C.c_uint, C.c_float, C.c_uint, C.c_void_p]
lib.astcenc_context_alloc.argtypes = [C.c_void_p, C.c_uint, C.POINTER(C.c_void_p)]
lib.astcenc_context_free.argtypes = [C.c_void_p]
lib.astcenc_decompress_image.argtypes = [C.c_void_p, C.c_void_p, C.c_size_t, C.c_void_p, C.c_void_p, C.c_uint]
class AstcImage(C.Structure):
    _fields_ = [("dim_x", C.c_uint), ("dim_y", C.c_uint), ("dim_z", C.c_uint),
                ("data_type", C.c_int), ("data", C.POINTER(C.c_void_p))]

def u32(pos): return struct.unpack_from("<I", blob, pos)[0]
out_dir = BASE / "ReferenceArt"
out_dir.mkdir(exist_ok=True)
manifest=[]
for row in rows:
    if int(row["class_id"]) != 28 or row["name"] not in ("GameplayAtlas", "HomeUiAtlas", "UiAtlas", "LevelWinAtlas", "GameplayExtensionAtlas"):
        continue
    p=int(row["byte_offset"])
    q=(p+4+u32(p)+3)&~3
    width,height,size=u32(q+8),u32(q+12),u32(q+16)
    fmt=u32(q+24)
    if not (48 <= fmt <= 53) and fmt != 4: continue
    block=[4,5,6,8,10,12][fmt-48] if fmt != 4 else 1
    # This player stores streamed Texture2D payloads after fixed metadata.
    stream_offset=struct.unpack_from("<Q",blob,q+88)[0]
    stream_size=u32(q+96)
    path_size=u32(q+100)
    stream_name=blob[q+104:q+104+path_size].rstrip(b"\0").decode("utf-8")
    expected_size = width*height*4 if fmt == 4 else ((width+block-1)//block)*((height+block-1)//block)*16
    if stream_name != "resources.assets.resS" or stream_size != expected_size:
        continue
    with (FILES/stream_name).open("rb") as f:
        f.seek(stream_offset); compressed=f.read(stream_size)
    assert len(compressed)==stream_size
    if fmt == 4:
        decoded = compressed
    else:
        config=C.create_string_buffer(4096)
        error=lib.astcenc_config_init(1,block,block,1,0.0,16,config)
        assert error==0, ("config",error)
        context=C.c_void_p()
        error=lib.astcenc_context_alloc(config,1,C.byref(context))
        assert error==0, ("context",error)
        dest=C.create_string_buffer(width*height*4)
        slices=(C.c_void_p*1)(C.cast(dest,C.c_void_p))
        image=AstcImage(width,height,1,0,slices)
        swizzle=(C.c_int*4)(0,1,2,3)
        compressed_buf=C.create_string_buffer(compressed)
        error=lib.astcenc_decompress_image(context,compressed_buf,len(compressed),C.byref(image),swizzle,0)
        lib.astcenc_context_free(context)
        assert error==0, ("decode",error)
        decoded = dest.raw
    filename=re.sub(r"[^\w.-]+","_",row["name"])+".png"
    Image.frombytes("RGBA",(width,height),decoded).transpose(Image.Transpose.FLIP_TOP_BOTTOM).save(out_dir/filename)
    manifest.append(dict(name=row["name"],width=width,height=height,format=fmt,block=block,
                         stream_offset=stream_offset,stream_size=stream_size,file=filename))
    print(filename,width,height)
    if len(manifest)>=8: break
import json
(out_dir/"atlas-manifest.json").write_text(json.dumps(manifest,indent=2))
