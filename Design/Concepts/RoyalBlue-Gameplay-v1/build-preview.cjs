const fs=require('node:fs'),path=require('node:path');
const assets={};for(const file of fs.readdirSync(path.join(__dirname,'assets')).filter(f=>f.endsWith('.webp')))assets[path.basename(file,'.webp')]='data:image/webp;base64,'+fs.readFileSync(path.join(__dirname,'assets',file)).toString('base64');
const font=fs.readFileSync(path.join(__dirname,'fonts/BorisBlackBloxx.ttf')).toString('base64');
const fragment=fs.readFileSync(path.join(__dirname,'prototype.template.html'),'utf8').replace('/*__FONT__*/',`@font-face{font-family:RoyalThroneDisplay;src:url(data:font/ttf;base64,${font}) format('truetype');font-display:swap}`).replace('/*__ASSETS__*/','const RT_ASSETS='+JSON.stringify(assets)+';').replace('/*__ENGINE__*/',fs.readFileSync(path.join(__dirname,'engine.js'),'utf8')).replace('/*__VIEW__*/',fs.readFileSync(path.join(__dirname,'view.js'),'utf8'));
const inline='C:/Users/chakm/.codex/visualizations/2026/09/08/01a07f2d-f2b5-7123-a1e6-e3dba88553a7/royal-throne-gameplay.html';
fs.writeFileSync(inline,fragment);
fs.writeFileSync(path.join(__dirname,'prototype.html'),'<!doctype html><html lang="en"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width, initial-scale=1"><title>Royal Throne — Gameplay Prototype</title><style>body{margin:0;padding:24px 12px;background:#eef2f7;}@media(max-width:450px){body{padding:12px 0}}</style></head><body>'+fragment+'</body></html>');
console.log(JSON.stringify({inline,fragmentBytes:Buffer.byteLength(fragment),under1MB:Buffer.byteLength(fragment)<1000000}));
