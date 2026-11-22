// Offline conversion of the credited FullScreenMario map and pixel data.
// Usage: node Tools/import-reference.cjs <FullScreenMario source directory>
const fs=require('fs'),path=require('path'),vm=require('vm'),zlib=require('zlib');
const root=path.resolve(__dirname,'..'),src=process.argv[2]||path.join(root,'ThirdParty/FullScreenMario/Source');
const out=path.join(root,'Assets/Resources');fs.mkdirSync(out,{recursive:true});
function Base(){};
Base.prototype.proliferate=function(a,b,no){for(const k in b)if(!no||!(k in a))a[k]=b[k];return a};
const ctx=vm.createContext({GameStartr:{GameStartr:Base},console,Uint8ClampedArray,Uint8Array,Math});
function read(p){vm.runInContext(fs.readFileSync(path.join(src,p),'utf8'),ctx,{filename:p})}
read('FullScreenMario.js');for(const s of ['objects','sprites','maps'])read('settings/'+s+'.js');
const FSM=ctx.FullScreenMario.FullScreenMario, settings=FSM.settings,defaults={};
function inherit(tree,parent={}){for(const [name,children]of Object.entries(tree)){let p=settings.objects.properties[name]||{};if(Array.isArray(p))p={width:p[0],height:p[1]};defaults[name]={...parent,...p};inherit(children,defaults[name]);}}
inherit(settings.objects.inheritance);
const scope=Object.create(FSM.prototype);Object.assign(scope,{settings,unitsize:4,MapScreener:{height:120*4},ObjectMaker:{getFullPropertiesOf:n=>defaults[n],getFullProperties:()=>defaults}});
const fnNames=new Map(Object.entries(FSM.prototype).filter(([k,v])=>typeof v==='function').map(([k,v])=>[v,k]));
const special=new Set(['Section','SectionPass','SectionFail','SectionDecider','CheepsStart','CheepsStop','BulletBillsStart','BulletBillsStop','LakituStop']);
function expand(records,area,map){const result=[];for(const r of records||[]){if(r.macro){if(special.has(r.macro)){result.push({...r,thing:r.macro,macro:undefined});continue;}const f=settings.maps.macros[r.macro];if(!f)throw Error('Unknown macro '+r.macro);const expanded=f.call(scope,r,[],area,map,scope);result.push(...expand(Array.isArray(expanded)?expanded:[expanded],area,map));}else result.push(r);}return result;}
function node(r){const d=defaults[r.thing]||{}, n={type:r.thing,group:r.groupType||d.groupType||'Trigger',x:(r.x||0)*2,y:(r.y||0)*2,width:(r.width||d.width||8)*2,height:(r.height||d.height||8)*2};
 if(!Number.isFinite(n.height)||r.height==='Infinity')n.height=Math.max(32,n.y+32);
 if(!Number.isFinite(n.width))n.width=256;
 for(const k of ['hidden','jumping','floating','sliding','falling','smart','hard','throwing','inScale','onPipe','red','small','flying'])n[k]=!!r[k];n.ride=r.transport===true;
 for(const k of ['begin','end'])n[k]=(r[k]||0)*2;
 n.speed=r.speed===undefined?.75:r.speed/2;n.vx=(r.xvel||0)/2;n.vy=-(r.yvel||0)/2;n.fireballs=r.fireballs||0;
 n.movement=typeof r.movement==='function'?fnNames.get(r.movement)||'':r.movement||'';
 n.action=typeof r.activate==='function'?fnNames.get(r.activate)||'':r.activate||'';
 n.link=r.collectionName||'';n.linkPart=r.collectionKey||'';
 n.section=r.section||0;n.pass=r.pass||0;n.fail=r.fail||0;
 n.entrance=Number.isInteger(r.entrance)?r.entrance:-1;
 let c=r.contents===undefined?d.contents:r.contents;n.contents=Array.isArray(c)?c[0]:c||'';
 let t=Array.isArray(c)?c[1]?.transport:r.transport;
 n.targetLocation=Number.isInteger(t)?t:(t&&Number.isInteger(t.location)?t.location:-1);n.targetMap=t&&typeof t==='object'?t.map||'':'';
 n.text=(r.texts||[]).map(t=>t.text||'').join('\n');
 return n;
}
fs.mkdirSync(path.join(out,'LevelData'),{recursive:true});
let nodeCount=0,areaCount=0,types=new Set();
for(let w=1;w<=8;w++)for(let l=1;l<=4;l++){
 const id=`${w}-${l}`;read('settings/maps/'+id+'.js');const m=settings.maps.library[id];
 const areas=m.areas.map(a=>{const nodes=expand(a.creation,a,m).map(node).filter(n=>!['DecorativeBack','DecorativeDot'].includes(n.type)&&!(id==='1-1'&&n.type==='CustomText'&&n.x<230));
 const sections=(a.sections||[]).map(s=>({parts:['before','stretch','after'].map(k=>({kind:k,width:(s[k]?.width||0)*2,nodes:expand(s[k]?.creation||[],a,m).map(node)}))}));
 // The browser reference checks 4-4 beyond an 80px gap under a low ceiling.
 // Its jump/collision model differs from our native motor. Sample the chosen
 // lane on the last 32px of solid ledge so falling cannot erase the upper route.
 if(id==='4-4')for(const n of sections[0].parts[0].nodes)
  if(n.type==='SectionPass'||n.type==='SectionFail'){n.x=688;n.width=32;}
 for(const n of [...nodes,...sections.flatMap(s=>s.parts.flatMap(p=>p.nodes))]){types.add(n.type);nodeCount++;}
 areaCount++;return{setting:a.setting||'Overworld',exit:Number.isInteger(a.exit)?a.exit:-1,underwater:!!a.underwater||a.setting.includes('Underwater'),width:Math.max(256,...nodes.filter(n=>n.group!=='Scenery').map(n=>n.x+n.width)),nodes,sections};});
 const data={id,timeLimit:m.time||400,locations:m.locations.map(l=>({area:l.area||0,entry:l.entry||'Normal',x:(l.xloc||0)*2})),areas};
 fs.writeFileSync(path.join(out,'LevelData',id+'.json'),JSON.stringify(data,null,2));
}
// Expand palettes/aliases to editable lossless PNGs; no resampling or AI reconstruction.
const lib=settings.sprites.library,palette=settings.sprites.paletteDefault,filters=settings.sprites.filters;
function lookup(keys){let v=lib;for(const k of keys)v=v[k];return v;}
function resolve(v,filter=[],depth=0){if(depth>25)throw Error('Sprite alias cycle');if(typeof v==='string')return{raw:v,filter};if(Array.isArray(v)){if(v[0]==='same')return resolve(lookup(v[1]),filter,depth+1);if(v[0]==='filter')return resolve(lookup(v[1]),[...filter,filters[v[2]][1]],depth+1);if(v[0]==='multiple'){let parts={};for(const[k,p]of Object.entries(v[2]))if(typeof p==='string')parts[k]={raw:p,filter};return{layout:v[1],parts};}throw Error('Sprite command '+v[0]);}let o={};for(const[k,p]of Object.entries(v))o[k]=resolve(p,filter,depth+1);return o;}
function decode(raw,ff){let p=palette.map((_,i)=>i),digits=2,i=0,a=[];while(i<raw.length){let ch=raw[i++];if(ch==='p'){if(raw[i]==='['){let end=raw.indexOf(']',i);p=raw.slice(i+1,end).split(',').map(Number);digits=String(p.length-1).length;i=end+1;}else{p=palette.map((_,i)=>i);digits=2;}}else if(ch==='x'){let idx=Number(raw.slice(i,i+digits));i+=digits;let end=raw.indexOf(',',i),count=Number(raw.slice(i,end));i=end+1;for(let j=0;j<count;j++)a.push(p[idx]);}else{let idx=Number(ch+raw.slice(i,i+digits-1));i+=digits-1;a.push(p[idx]);}}
 return a.map(v=>{for(const f of ff){const k=String(v).padStart(2,'0');if(k in f)v=Number(f[k]);}if(!palette[v])throw Error('Bad color '+v);return palette[v];});}
function crc(buf){let c=0xffffffff;for(let b of buf){c^=b;for(let i=0;i<8;i++)c=(c>>>1)^((c&1)?0xedb88320:0);}return(c^0xffffffff)>>>0;}
function chunk(t,b){let data=Buffer.concat([Buffer.from(t),b]),h=Buffer.alloc(4),c=Buffer.alloc(4);h.writeUInt32BE(b.length);c.writeUInt32BE(crc(data));return Buffer.concat([h,data,c]);}
function png(w,h,p){let ih=Buffer.alloc(13);ih.writeUInt32BE(w);ih.writeUInt32BE(h,4);ih[8]=8;ih[9]=6;let rows=[];for(let y=0;y<h;y++)rows.push(Buffer.from([0]),Buffer.from(p.slice(y*w,(y+1)*w).flat()));return Buffer.concat([Buffer.from([137,80,78,71,13,10,26,10]),chunk('IHDR',ih),chunk('IDAT',zlib.deflateSync(Buffer.concat(rows))),chunk('IEND',Buffer.alloc(0))]);}
fs.mkdirSync(path.join(out,'Pixels'),{recursive:true});let entries=[],count=0;
function savePart(key,part,v,layout){const pixels=decode(v.raw,v.filter),type=key[1],d=defaults[type]||{};let w=Math.round((d.spritewidth||d.width||8)*2),h;if((type==='Player'||type==='Luigi')&&key.includes('dead'))w=15;
 if(layout==='horizontal'){h=Math.round((d.spriteheight||d.height||8)*2);w=pixels.length/h;}else h=pixels.length/w;
 if(type==='PipeHorizontal'&&key.includes('small')){w=16;h=pixels.length/w;}
 if(!Number.isInteger(w)||!Number.isInteger(h)){// Variable player dimensions retain the original 16-pixel width.
 w=type==='Player'?16:Math.round((d.width||8)*2);h=pixels.length/w;
 }
 if(pixels.length===1){w=1;h=1;}
 if(!Number.isInteger(w)||!Number.isInteger(h)||!w||!h)throw Error('Dimensions '+key.join('/')+' '+part+' '+pixels.length+' '+w+'x'+h);
 const file=String(count++).padStart(4,'0');fs.writeFileSync(path.join(out,'Pixels',file+'.png'),png(w,h,pixels));return{name:part,file:'Pixels/'+file,width:w,height:h};}
function walk(v,key=[]){if(v.raw){entries.push({key:key.join(' '),layout:'single',parts:[savePart(key,'middle',v,'single')]});}else if(v.layout){entries.push({key:key.join(' '),layout:v.layout,parts:Object.entries(v.parts).map(([k,p])=>savePart(key,k,p,v.layout))});}else for(const[k,p]of Object.entries(v))walk(p,[...key,k]);}
walk(resolve(lib));fs.writeFileSync(path.join(out,'SpriteCatalog.json'),JSON.stringify({entries},null,2));
// Preserve the original CHR coin animation rather than the reference's pink alias palette.
require('child_process').execFileSync('python',[path.join(root,'Tools','import-coin-frames.py')],{stdio:'inherit'});
fs.writeFileSync(path.join(root,'_zcode_tmp','import-report.json'),JSON.stringify({levels:32,areas:areaCount,nodes:nodeCount,sprites:count,types:[...types].sort()},null,2));
console.log(JSON.stringify({levels:32,areas:areaCount,nodes:nodeCount,sprites:count,types:[...types].sort()}));
