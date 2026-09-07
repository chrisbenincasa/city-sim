const {spawn}=require('child_process');const fs=require('fs');const path=require('path');
const root=__dirname;
const run=async()=>{
 const chrome=spawn('/Applications/Google Chrome.app/Contents/MacOS/Google Chrome',['--headless','--disable-gpu','--no-first-run','--disable-background-networking','--remote-debugging-port=9337','--user-data-dir=/private/tmp/architecture3-chrome-qa','about:blank'],{stdio:'ignore'});
 let ws;try{
  let tab;for(let i=0;i<50;i++){try{const a=await(await fetch('http://127.0.0.1:9337/json')).json();tab=a.find(x=>x.type==='page');if(tab)break;}catch{}await new Promise(r=>setTimeout(r,100));}
  ws=new WebSocket(tab.webSocketDebuggerUrl);await new Promise(r=>ws.addEventListener('open',r,{once:true}));let id=0;const pending=new Map();ws.onmessage=e=>{const m=JSON.parse(e.data);if(m.id){pending.get(m.id)?.(m);pending.delete(m.id);}};
  const cmd=(method,params={})=>new Promise((resolve,reject)=>{let n=++id;pending.set(n,m=>m.error?reject(m.error):resolve(m.result));ws.send(JSON.stringify({id:n,method,params}));});
  await cmd('Page.enable');
  const captures=[...['sacramento','south-bend','amersfoort','tingbjerg','valencia','fictional','fictional-materials'].map(x=>['drawings/'+x+'.svg',1160,1130]),['drawings/workplace.svg',1160,980],['drawings/construction.svg',1160,900]];
  const results=[];
  for(const [file,width,height,scroll] of captures){
   await cmd('Emulation.setDeviceMetricsOverride',{width,height,deviceScaleFactor:1,mobile:false});await cmd('Page.navigate',{url:'file://'+root+'/'+file});await new Promise(r=>setTimeout(r,450));
   if(scroll)await cmd('Runtime.evaluate',{expression:scroll==='bottom'?'window.scrollTo(0,document.body.scrollHeight)':`document.querySelector(${JSON.stringify(scroll)})?.scrollIntoView()`});
   const evald=await cmd('Runtime.evaluate',{expression:`JSON.stringify({title:document.title,width:innerWidth,scrollWidth:document.documentElement.scrollWidth,brokenImages:[...document.images].filter(i=>!i.complete||i.naturalWidth===0).map(i=>i.src),svgOverflow:[...document.querySelectorAll('svg text')].filter(e=>{let b=e.getBBox(),v=e.ownerSVGElement.viewBox.baseVal;return b.x<v.x-1||b.y<v.y-1||b.x+b.width>v.x+v.width+1||b.y+b.height>v.y+v.height+1}).map(e=>e.textContent)})`,returnByValue:true});
   const result=JSON.parse(evald.result.value);result.file=file;result.scroll=scroll||'top';results.push(result);
   const shot=await cmd('Page.captureScreenshot',{format:'png',captureBeyondViewport:false});const out='/private/tmp/architecture3-qa-'+file.replaceAll('/','-').replace(/\.(svg|html)$/,'')+(scroll?'-'+scroll.replace('#',''):'')+(width<500?'-mobile':'')+'.png';fs.writeFileSync(out,Buffer.from(shot.data,'base64'));
  }
  fs.writeFileSync(root+'/evidence/render-checks-corrections.json',JSON.stringify(results,null,2));console.log(JSON.stringify(results,null,2));
 }finally{if(ws)ws.close();chrome.kill('SIGTERM');}
};run().catch(e=>{console.error(e);process.exitCode=1;});
