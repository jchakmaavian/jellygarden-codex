// Built-in image_gen batch orchestration; invoked in functions.exec, not Node.
async function(jobs){
 const inv=load("royalInventory"), directions=load("royalDirections");
 const root="/Users/avian/Desktop/Project/match3/king-leo/art-review/areas-002-025";
 const progress=load("areaRoyalProgress")||{};
 const results=await Promise.allSettled(jobs.map(async job=>{
  const area=inv.areas.find(a=>a.id===job.area),dir=directions.areas[String(job.area)];
  const prev=job.redo?{state:job.state-1,path:root+"/v1/area-"+String(job.area).padStart(3,"0")+"/state-"+String(job.state-1).padStart(2,"0")+".png"}:progress[String(job.area)];
  if(job.state>0&&(!prev||prev.state!==job.state-1))throw new Error("Previous state not ready: "+job.area);
  const label=job.state===0?"Before repairs":area.tasks[job.state-1].label;
  let prompt=job.state===0
   ? `Use case: stylized-concept. Asset: KING LEO area ${job.area}, ${area.name}, STATE 0 before repairs. Generate one portrait 9:16 environment still. The input is STYLE REFERENCE ONLY: match the approved gate's warm royal 3D animation quality, but create this DIFFERENT location.\nSTYLE: ${directions.style}\nSCENE AND ALL INITIAL DAMAGE: ${dir.scene}\nCOMPOSITION: fixed slightly elevated three-quarter camera, fit the entire small room or courtyard into one portrait composition, broad readable shapes, keep every listed repair subject visible and spatially separated. No collage or cutaway panels. Main room occupies central 75%, a quiet foreground floor or path occupies lower 20%, architecture visible above. Clear 3D depth, soft shadows and ambient occlusion; materials are smooth but detailed, not plastic toys. The space needs repair but is still cheerful, sunlit, colourful and inviting.\nLATER repairs, NONE done yet, in this exact order: ${area.tasks.map(t=>t.index+". "+t.label).join("; ")}.\nDo not add living characters unless explicitly required. No UI, labels, readable words, logos, watermark, floating icons, particles, construction workers or tools being operated. One standalone portrait image with no border.`
   : `Use case: precise-object-edit. KING LEO area ${job.area} ${area.name}, STATE ${job.state}: ${label}. The input is the EXACT PREVIOUS STATE of this room, the edit target. Preserve image dimensions, camera, crop, perspective, architecture, background, object identity and positions, material style, overall warm daylight and every previously completed repair.\nCHANGE ONLY THIS REPAIR: ${dir.repairs[job.state-1]}\nCUMULATIVE CONTINUITY: ${job.state===1?"No earlier repairs.":dir.repairs.slice(0,job.state-1).map((s,i)=>"Step "+(i+1)+" ALREADY DONE AND MUST REMAIN: "+s).join(" ")}\nSTILL UNFINISHED; keep these subjects in the same damaged/unfixed condition as the input: ${area.tasks.slice(job.state).map(t=>t.label).join("; ")||"None; all room repairs are complete."}\nDo not redesign the room, move other objects, relight the whole scene, change the floor pattern outside the repair, invent decorations, restore future tasks or remove previous repairs. Exact same scene with one local completed change. Keep the approved warm royal polished 3D animation aesthetic. No panels, text, HUD, captions, particles, workers or watermark. Single finished-stage portrait image, not an action illustration.`;
  const reference=job.state===0?load("areas2to25Reference"):prev.path;
  const references=[reference];
  if((job.area===5&&job.state===4)||(job.area===21&&job.state===4)||(job.area===25&&job.state===8)){
   const portraits="/Users/avian/Desktop/Project/match3/king-leo/Assets/JellyGarden/Resources/Story/Art/";
   references.push(portraits+"portrait_leo.png",portraits+"portrait_freya.png");
   prompt+="\nADDITIONAL CHARACTER REFERENCES: image 2 is King Leo, image 3 is Queen Freya. Match those identities only in the repaired portrait paintings or the small rescue figures, as requested for this step. Leo is the warm round-faced red-bearded king with a large curled moustache, gold gem crown and crimson fur-trimmed robe. Freya is the adult queen with long copper-red hair, green eyes, gold leaf circlet and emerald-gold gown. The FIRST image remains the exact room edit target; preserve the room composition. Do not insert the portrait cutouts or enlarge the characters.";
  }
  const result=await tools.image_gen__imagegen({prompt,referenced_image_paths:references});
  text("Area "+job.area+" · "+area.name+" · Stage "+job.state+" · "+label);
  generatedImage(result);
  const source=result.output_hint?.match(/as (\/\S+\.png) by default/)?.[1];
  if(!source)throw new Error("No generated output path: "+job.area+"/"+job.state);
  const relative="v1/area-"+String(job.area).padStart(3,"0")+"/state-"+String(job.state).padStart(2,"0");
  const path=root+"/"+relative+".png";
  const record={area:job.area,state:job.state,label,generator:"Built-in image_gen",prompt,source,path,reference,references};
  const oldRecord=load("royalRecord_"+job.area+"_"+job.state);
  if(job.redo&&oldRecord)record.supersedesSource=oldRecord.source;
  store("royalRecord_"+job.area+"_"+job.state,record);
  const replacement=JSON.stringify(record,null,2).split("\n").map(l=>"+"+l).join("\n");
  const patch=job.redo&&oldRecord
   ? "*** Begin Patch\n*** Update File: "+root+"/"+relative+".json\n@@\n"+JSON.stringify(oldRecord,null,2).split("\n").map(l=>"-"+l).join("\n")+"\n"+replacement+"\n*** End Patch"
   : "*** Begin Patch\n*** Add File: "+root+"/"+relative+".json\n"+replacement+"\n*** End Patch";
  await tools.apply_patch(patch);
  const q=s=>"'"+s.replace(/'/g,"'\\''")+"'";
  const saved=await tools.exec_command({cmd:"cp "+q(source)+" "+q(path),max_output_tokens:500});
  if(saved.exit_code!==0)throw new Error("Copy failed for "+path);
  return {area:job.area,state:job.state,path,label};
 }));
 const savedProgress=load("areaRoyalProgress")||{};
 for(let i=0;i<results.length;i++){
  const r=results[i];
  if(r.status==="fulfilled")savedProgress[String(r.value.area)]={state:r.value.state,path:r.value.path};
  text(r.status==="fulfilled"?r.value:{job:jobs[i],error:String(r.reason)});
 }
 store("areaRoyalProgress",savedProgress);
 return results.every(r=>r.status==="fulfilled");
}
