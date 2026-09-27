const fs=require('fs'),path=require('path'),crypto=require('crypto');
const root=path.resolve(__dirname,'../..'),manifest=JSON.parse(fs.readFileSync(path.join(__dirname,'manifest.json')));
const stamp=new Date().toISOString().replace(/[-:]/g,'').replace(/\..*/,'').replace('T','-');
const backup=path.join(root,'Design/Backup','Coin-Club-'+stamp);
const hash=p=>crypto.createHash('sha256').update(fs.readFileSync(p)).digest('hex');
const before=[];
for(const item of manifest){for(const suffix of ['', '.meta']){const p=path.join(root,item.file+suffix),dst=path.join(backup,item.file+suffix);fs.mkdirSync(path.dirname(dst),{recursive:true});fs.copyFileSync(p,dst);}before.push({...item,originalHash:hash(path.join(root,item.file)),metaHash:hash(path.join(root,item.file+'.meta'))});}
for(const item of manifest)fs.copyFileSync(path.join(__dirname,'Prepared',item.file),path.join(root,item.file));
for(const item of before){if(hash(path.join(root,item.file+'.meta'))!==item.metaHash)throw Error('Metadata changed: '+item.file);item.installedHash=hash(path.join(root,item.file));}
fs.writeFileSync(path.join(__dirname,'installation.json'),JSON.stringify({backup,files:before},null,2));
console.log('Replaced '+before.length+' coin textures. All metadata byte-identical. Backup: '+backup);
