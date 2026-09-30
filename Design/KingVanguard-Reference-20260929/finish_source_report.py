from inspect_king_sources import *
import shutil,csv
sys.path.insert(0,str(ROOT/'Design/ReferenceGameplayImport/Media/Tools/Python'))
import UnityPy

def main():
    original=OUT/'Original-rfv387'
    for ext in ['skel','atlas']:
        shutil.copy2(OUT/('EmbeddedKingBundles/Recovered/kingnightmarecommon_v3_data/nightmare_popup.'+ext),original/('nightmare_popup.'+ext))
    shutil.copy2(OUT/'EmbeddedKingBundles/Recovered/kingnightmarecommon_v3/Textures/nightmare_popup__465607759375844926.png',original/'nightmare_popup.png')
    materials=[]
    for file,wanted in [('sharedassets2.assets','King_Material'),('resources.assets','AreaCompleted_Material')]:
        env=UnityPy.load(str(ROOT/'Design/Inspection-rfv387/datapack.unity3d_data'/file))
        for o in env.objects:
            if o.type.name!='Material':continue
            t=o.read_typetree()
            if t['m_Name']!=wanted:continue
            (original/(wanted+'.json')).write_text(json.dumps(t,indent=2),encoding='utf8');materials.append(dict(source=file,path_id=o.path_id,name=wanted,properties=t['m_SavedProperties']))
    f=OUT/'EmbeddedKingBundles/Recovered/kingnightmarecommon_v3/Serialized/nightmare_popup_Material__-7520026923060983228.json'
    shutil.copy2(f,original/'nightmare_popup_Material.json');t=json.loads(f.read_text());materials.append(dict(source='kingnightmarecommon_v3',path_id=-7520026923060983228,name=t['m_Name'],properties=t['m_SavedProperties']))
    save('source-material-evidence.json',materials)
    sources=[]
    with zipfile.ZipFile(ROOT/'Design/Backup/apk/royal_thron_rfv387.apk') as z:
        for name in ['data.unity3d','datapack.unity3d']:
            b=z.read('assets/bin/Data/'+name);f=ROOT/'Design/Inspection-rfv387'/name;sources.append(dict(name=name,archive_sha256=digest(b),local_sha256=digest(f.read_bytes()),identical=b==f.read_bytes()))
    apk=ROOT/'Design/Backup/apk/royal_thron_rfv387.apk'
    provenance=dict(apk=str(apk.relative_to(ROOT)),bytes=apk.stat().st_size,sha256=digest(apk.read_bytes()),bundle_verification=sources,original_files=[dict(file=f.name,bytes=f.stat().st_size,sha256=digest(f.read_bytes())) for f in sorted(original.iterdir()) if f.is_file()])
    save('source-provenance.json',provenance)
    main=json.loads((OUT/'rfv387-spine-skeletons.json').read_text(encoding='utf8'));extra=json.loads((OUT/'embedded-king-bundle-skeletons.json').read_text(encoding='utf8'))
    selected=[x for x in main+extra if x['name'] in ['King.skel','AreaCompleted.skel','nightmare_popup.skel']]
    rows=[dict(rig=x['name'],animation=a['name'],duration_seconds=a['duration'],bone_timelines=a['bone_timelines'],slot_timelines=a['slot_timelines'],deform_timelines=a['deform_timelines'],draw_order_keys=a['draw_order_keys']) for x in selected for a in x['skeleton']['animations']]
    save('all-king-spine-animation-inventory.json',rows)
    with (OUT/'all-king-spine-animation-inventory.csv').open('w',encoding='utf-8-sig',newline='') as f:
        w=csv.DictWriter(f,fieldnames=list(rows[0]));w.writeheader();w.writerows(rows)
    portrait=['Frame Background3','sol frame','sag frame']
    area=json.loads((OUT/'AreaCompleted.skel.spine38.json').read_text(encoding='utf8'))
    exclusions=dict(portrait_frame_slots=portrait,portrait_optional_hand_mask_and_hands=['frame_clipping','sag','sol'],area_standalone_exclusions=[s['name'] for i,s in enumerate(area['slots']) if i in [0,1] or 57<=i<=75],note='Null excluded attachments after every animation apply; slot attachment timelines can otherwise restore them. Keep original binary and artwork unchanged in this folder.')
    save('standalone-slot-exclusions.json',exclusions)
    md=['# Royal Match King source verification — rfv387','',f"Source: `{provenance['apk']}` ({provenance['bytes']:,} bytes).",f"SHA-256: `{provenance['sha256']}`.",'','The two core Unity packs match the requested APK byte-for-byte. Both core packs and all 22 standalone `assets/android/` bundles were inspected. The four King-named bundles were also unpacked and their visual/native data preserved. The broader bundle audit found no additional King character Spine rig; the `table_game` bundle contributes King Workshop UI/area/gameplay atlases and a tutorial hand rig. This is an offline asset/metadata inventory, not a claim that every downloaded game episode is present or playable.','','## Recovered character structures','','| Structure | Source | Bones / slots | Tracks | Use |','|---|---|---:|---:|---|']
    uses={'King.skel':'Gameplay portrait with source expression/idle/reaction motion; no legs.','AreaCompleted.skel':'Upper-body King, articulated arms/fingers, banner/horns; no legs.','nightmare_popup.skel':'Bedtime King reading/sleeping, dog, book, blanket and bed; no leg rig.'}
    for r in selected:
        s=r['skeleton'];md.append(f"| `{r['name']}` ({s['version']}) | `{r['file']}` | {s['bones']} / {s['slots']} | {len(s['animations'])} | {uses[r['name']]} |")
    md+=['| Native KingDrill | `assets/android/kingdrill_v3` | Unity transform/sprite hierarchy | 6 King clips | Full-body King with independent left/right leg sprites, arms, head and expressions. |','','The three Spine binaries contain **48 named tracks, 47 with nonzero duration**; `_3` in the portrait is an empty zero-duration track. All binaries parsed to the exact end of their source payloads. The native KingDrill has six King clips plus the separate drill loop. These are separate source structures, not a single universal turnaround/locomotion rig.','','## Native KingDrill clips','','| Clip | Seconds |','|---|---:|']
    native=json.loads((OUT/'EmbeddedKingBundles/Recovered/kingdrill_v3/decoded-animation-clips.json').read_text())
    for a in native:
        if a['name'].startswith('King'):md.append(f"| `{a['name']}` | {a['duration']:.6f} |")
    md+=['','Original native controller/clip JSON, sprites, atlas and hierarchy are in `EmbeddedKingBundles/Recovered/kingdrill_v3`. The inventory preserves original data even where an intermediate decoded clip has unresolved hashed transform paths; reconstruction verification is reported separately by the implementation task.','','## Original deliverables and texture handling','','`Original-rfv387/` contains the three `.skel` binaries, their original `.atlas` text, decoded `.png` textures, and source material evidence. The binaries are directly readable by the installed Spine 3.8 runtime; `.skel.bytes` is a Unity import naming convention, not a conversion. The `*.spine38.json` files in this reference folder are inspection schemas, **not** standard Spine JSON imports.','','- Portrait `King.png`: decoded 672 × 1872, exactly matches atlas declared dimensions.','- `AreaCompleted.png`: APK texture is 2048 × 2048, original atlas declares 2048 × 1714. This is the source player texture scaling; runtime adaptation must retain the declared source UV layout or resample the decoded texture to the declared dimensions. The reference file remains the original decoded 2048-square payload.','- `nightmare_popup.png`: decoded 2048 × 2048 and atlas declares the same.','- Source material `_StraightAlphaInput` values are recorded in `source-material-evidence.json`; they are 0 for these source Spine materials, so source textures are intended for premultiplied-alpha rendering. Adapted straight-alpha paintings require a corresponding material setup.','','## Activity and logic evidence','','The APK IL2CPP metadata exposes `KingUi` and `KingAnimationStateManager` fields/method names, and serialized scene references recover animation-role assignments. Verified integer constants: `StartWorryMoveCount = 3` in both classes and `LBComboExciteRayThreshold = 8` in the state manager. Method names show move/goal updates, idle cycling, worry, happy, excited, sad, relief and combo/win checks. These observations establish data and interfaces; compiled IL2CPP method bodies are not editable original C# source, so do not describe a rewritten policy as a byte-identical recovered game algorithm. See `Logic/` for the focused binding evidence.','','## Framing and exclusions','','The portrait export bounds are x=-215.85194397, y=-0.16087499, width=431.40197754, height=406.26498413 in source units. The AreaCompleted export bounds include large decorative banner content and must not be used as the standalone character fit rectangle. Official runtime setup-pose measured bounds are separately recorded in `import-report.txt`; they can differ slightly from export metadata and must be measured after presentation exclusions.','','`standalone-slot-exclusions.json` lists exact frame/background/banner/horn slot names. Apply exclusions after animation application, because original attachment timelines can restore hidden decoration. Keep the original source binary unchanged.','','## All 48 Spine animation durations','','| Rig | Animation | Seconds |','|---|---|---:|']
    for r in rows:md.append(f"| `{r['rig']}` | `{r['animation']}` | {r['duration_seconds']:.6f} |")
    md+=['','## Scope and limitations','','No source project was launched, no game server was contacted, and no source APK/package files were modified by this inspection. This task inspected packaged assets and metadata; it did not verify a running Royal Match session. Prior extracted King data was used only for comparison, never as the new import source: all portrait animation timelines happen to match the older copy exactly, while the new source atlas and skin data differ. Current V1 Royal Throne art/rig was not used in this extraction. Costume redesign and integration QA are separate from this source report.','']
    (OUT/'SOURCE-VERIFICATION.md').write_text('\n'.join(md),encoding='utf8')
    print('REPORT',len(rows),'Spine tracks',provenance['sha256']);print('MATERIALS',[(m['name'],dict(m['properties']['m_Floats']).get('_StraightAlphaInput')) for m in materials])

if __name__=='__main__':main()
