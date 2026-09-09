'use strict';
const $ = s => document.querySelector(s);
const families = [['Connections','↗'],['Zoning','▦'],['Services','⌂'],['Government','≡'],['Demolish','⌫']];
const dataViews = [['Maps','M3 3h7v7H3zM14 3h7v7h-7zM3 14h7v7H3zM14 14h7v7h-7z'],['City','M4 20V12h4v8M10 20V4h4v16M16 20V8h4v12'],['Evidence','M15 15l6 6M17 10a7 7 0 1 1-14 0 7 7 0 0 1 14 0'],['Pins','M9 3h6l-1 6 4 4v2H6v-2l4-4zM12 15v7']];
// These entries exercise catalogue size and navigation, not the simulation's availability rules.
const catalogue = [];
function add(family, group, names, status='Sketch', detail='Future interaction placeholder') {
  names.split('|').forEach(name => catalogue.push({id:catalogue.length,name,family,group,status,detail}));
}
add('Connections','Roads','Street','','Lay a connection on the map');
add('Connections','Roads','Arterial|Junction pieces');
add('Connections','Transit','Transit line|Stop / station|Shared right-of-way|Reserved right-of-way|Separated right-of-way');
add('Zoning','Permissions','Residential|Commercial|Office|Industry — Extraction|Industry — Processing','','Paint permissions; existing Buildings remain');
add('Zoning','Remove permissions','Erase zoning','','Remove permissions; existing Buildings remain');
add('Services','Education','School','','Place an education service');
add('Services','Education','Further education');
add('Services','Health','Clinic','','Place a health service');
add('Services','Response','Fire service|Waste service');
add('Services','Utilities','Power facility|Water facility|Sewage facility','Unresolved','Utility transport and facility catalogue need scoping');
add('Government','Finance','Taxes|Service funding|Transfers|Borrowing','Sketch','Inspect payers, recipients, and scope before applying');
add('Government','Local government','Draw Ward|Rename Ward|Redraw Ward|Ward overrides','Unresolved','Ward purpose is settled; shape and overlap rules are open');
add('Government','Programmes','Constraints|Clearance programme');
add('Government','Transit operation','Line operation','Sketch','Vehicle provision and its operating consequences');
add('Government','Land','Remediation / unsealing|Terraforming','Unresolved','Placement in this catalogue is provisional; no terrain gesture is promised');
add('Demolish','Removal','Abandoned Building','','Clear abandoned stock');
add('Demolish','Removal','Remove Street|Occupied Building','Sketch','Show removal costs and affected people before commitment');
add('Layers','Environment','Pollution|Land value|Sealing|Health','','Show a schematic layer; keep the tool');
add('Layers','Environment','Water pollution|Noise');
add('Layers','Movement','Traffic volume|Commute time');
add('Layers','Access','Utility coverage|Service access');
add('Layers','People','Affordability|Household composition');
add('Following','Evidence','Supply diagnosis','','Follow a sample shortage to its cause');
add('Following','People and places','Pinned Household|Pinned place|Pinned metric|Recent Trips|Notifications','Sketch','Follow named subjects and retain inspection history');
add('Zoning','Permissions','Mixed use','','Provisional sixth permission in this study');
let state;
const instructions = {
  zone:'Choose a permission from the compact zoning chooser; it folds away when selected. Paint, change selection size, and erase. Open Maps from the bottom console while the tool remains held. Try the Land value link from zoning too.',
  supply:'Inspect the shop’s supply route. Choose Repair connection, close the catalogue if needed, and click the highlighted missing link. Advance the sample to see the staged result. The inspector should remain available throughout.',
  transit:'Open Line operation from Connections → Transit line, or Government. Change vehicle provision and apply. Inspect a stop, then use Back to return to the line. Pin the line and find it with the Pins button on the bottom console.'
};
function initial(scenario) {
  return {scenario,family:scenario==='zone'?'Zoning':scenario==='transit'?'Connections':null,dataView:null,lastChooser:'tools',mapGroup:'Environment',query:'',filter:'All',tool:null,
    selected:scenario==='supply'?'shop':scenario==='transit'?'line':null,history:[],parcels:{},parcel:22,
    brush:'Parcels',density:'Low',layer:null,favourites:[],pinned:[],done:new Set(),route:false,repaired:false,delivered:false,
    vehicles:4,draftVehicles:4,zoom:1,angle:0,paused:true,speed:1,minute:0};
}
function reset() {
  state=initial($('#scenario').value); $('#camera').hidden=true; $('#camera-toggle').setAttribute('aria-expanded','false');
  $('#task').textContent=instructions[state.scenario]; render(); tell('Choose a tool or inspect a parcel. Every change here belongs to the study.');
}
function button(text, attrs='') {return `<button ${attrs}>${text}</button>`;}
function badge(t) {return t.status?`<span class="badge ${t.status==='Unresolved'?'unresolved':''}">${t.status}</span>`:'';}
function tell(text) {$('#feedback').textContent=text;}
function completed(id) {state.done.add(id);$('#progress').textContent='Tried: '+[...state.done].join(' · ');}
function openFamily(name) {state.family=state.family===name?null:name;state.lastChooser='tools';state.query='';state.filter='All';renderRail();renderCatalogue();}
function closeTools(){const family=state.family;state.family=null;renderRail();renderCatalogue();if(family)$(`[data-family="${family}"]`)?.focus({preventScroll:true});}
function renderRail() {
  const focused=document.activeElement?.dataset.family;
  $('#rail').innerHTML=families.map(([n,s],i)=>`${i===5?'<hr>':''}<button data-family="${n}" aria-pressed="${state.family===n}"><span class="symbol" aria-hidden="true">${s}</span>${n}</button>`).join('');
  if(focused)$(`[data-family="${focused}"]`)?.focus({preventScroll:true});
}
function renderData(){
  const focused=document.activeElement?.dataset.data;
  $('#data-launchers').innerHTML=dataViews.map(([name,path])=>button(`<svg viewBox="0 0 24 24" aria-hidden="true"><path d="${path}"/></svg><span>${name}</span>`,`data-data="${name}" title="${name==='Maps'?'Data maps':name==='Pins'?'Pinned people, places and metrics':name}" aria-expanded="${state.dataView===name}" class="${state.dataView===name?'selected':''}"`)).join('');
  if(focused)$(`[data-data="${focused}"]`)?.focus({preventScroll:true});
  const p=$('#data-panel');p.hidden=!state.dataView;if(!state.dataView)return;
  let content='';
  if(state.dataView==='Maps'){
    const layers=catalogue.filter(t=>t.family==='Layers'),groups=[...new Set(layers.map(t=>t.group))];
    content=`<div class="map-groups">${groups.map(g=>button(g,`data-map-group="${g}" aria-pressed="${state.mapGroup===g}"`)).join('')}</div><div class="data-choices">${layers.filter(t=>t.group===state.mapGroup).map(t=>button(`${t.name}${badge(t)}`,`data-tool="${t.id}" aria-pressed="${state.layer===t.name}"`)).join('')}</div>`;
  }else if(state.dataView==='City'){
    content=`<p class="subtle">The city in this study</p><div class="fact"><span>Parcels with permissions</span><strong>${Object.keys(state.parcels).length} / 48</strong></div>${button('Food supply →','class="link-button" data-data-inspect="shop"')}${button('Transit service →','class="link-button" data-data-inspect="line"')}<p class="subtle">Future summaries: Households, employment, Goods, finances and service access. Each summary should lead to its constituents.</p>`;
  }else if(state.dataView==='Evidence'){
    content=`<p class="subtle">Follow a condition to its cause.</p>${button(`${state.delivered?'Food delivery arrived':'Shop waiting for Food'} →`,'class="link-button" data-data-inspect="shop"')}${button('North Bank line →','class="link-button" data-data-inspect="line"')}<p class="subtle">Two illustrative cases. Future entries connect sustained problems to named people, places and available interventions.</p>`;
  }else{
    content=state.pinned.length?state.pinned.map(id=>button(`${subjectName(id)} →`,`class="link-button" data-data-inspect="${id}"`)).join(''):'<p class="subtle">Pin a subject from its inspector to keep it here.</p>';
    content+='<p class="subtle">Follow people, places and metrics you choose.</p>';
  }
  p.innerHTML=`<div class="heading"><h2>${state.dataView==='Maps'?'Data maps':state.dataView}</h2>${button('×','class="close" data-close="data" aria-label="Close data view"')}</div><div class="data-body">${content}</div>`;
  requestAnimationFrame(fit);
}
function closeData(){const name=state.dataView;state.dataView=null;renderData();if(name)$(`[data-data="${name}"]`)?.focus({preventScroll:true});}
function renderCatalogue() {
  const panel=$('#catalog');panel.hidden=!state.family;if(!state.family)return;
  const compact=state.family==='Zoning'||state.family==='Demolish'||state.family==='Connections';panel.classList.toggle('compact',compact);panel.classList.toggle('zoning',state.family==='Zoning');
  if(compact){
    const items=catalogue.filter(t=>t.family===state.family);let last='';
    const names={'Industry — Extraction':'Extraction','Industry — Processing':'Processing'};
    panel.innerHTML=`<div class="heading"><h2>${state.family}</h2>${button('×','class="close" data-close="catalog" aria-label="Close tool chooser"')}</div><div class="compact-options">${items.filter(t=>t.name!=='Erase zoning').map(t=>{
      const heading=state.family==='Zoning'||last===t.group?'':`<div class="group-title">${t.group}</div>`;last=t.group;
      return heading+button(`<span class="zone-chip" data-zone-name="${t.name}" aria-hidden="true"></span>${names[t.name]||t.name}${badge(t)}`,`data-tool="${t.id}" title="${t.name}: ${t.detail}" aria-pressed="${state.tool?.id===t.id}"`);
    }).join('')}</div>${state.family==='Zoning'?`<div class="zone-extras">${button('⌫ Erase permissions',`data-tool="${catalogue.find(t=>t.name==='Erase zoning').id}"`)}${button('View land value ↗','class="context-data" data-context-map="Land value"')}</div>`:''}`;
    requestAnimationFrame(fit);return;
  }
  panel.innerHTML=`<div class="heading"><div><h2>${state.family}</h2><p>Explore · choose · keep your place</p></div>${button('×','class="close" data-close="catalog" aria-label="Close catalogue; keep tool"')}</div><input id="search" class="catalog-search" aria-label="Search all tools" placeholder="Search every family…"><div class="filters">${['All','Favourites'].map(x=>button(x,`data-filter="${x}" aria-pressed="${state.filter===x}"`)).join('')}</div><div class="catalog-list" id="catalog-list"></div>`;
  $('#search').value=state.query;renderList();
}
function renderList() {
  if(!$('#catalog-list'))return;
  const focused=document.activeElement?.dataset.tool;
  const q=state.query.toLowerCase();let items=catalogue.filter(t=>t.family!=='Zoning'&&families.some(([name])=>name===t.family)&&(q?`${t.name} ${t.family} ${t.group}`.toLowerCase().includes(q):state.filter==='Favourites'||t.family===state.family));
  if(state.filter==='Favourites')items=state.favourites.map(id=>catalogue[id]).filter(t=>items.includes(t));
  let last='';$('#catalog-list').innerHTML=items.map(t=>{
    const group=state.query||state.filter==='Favourites'?`${t.family} / ${t.group}`:t.group;
    const title=last!==group?`<div class="group-title">${group}</div>`:'';last=group;
    const selected=state.tool?.id===t.id||(t.family==='Layers'&&state.layer===t.name);
    return title+`<div class="tool-card">${button(`${t.name}${badge(t)}<small>${t.detail}</small>`,`class="choose ${selected?'selected':''}" data-tool="${t.id}" title="${t.detail}" aria-pressed="${selected}"`)}${button(state.favourites.includes(t.id)?'★':'☆',`class="star" data-star="${t.id}" aria-label="${state.favourites.includes(t.id)?'Unfavourite':'Favourite'} ${t.name}" aria-pressed="${state.favourites.includes(t.id)}"`)}</div>`;
  }).join('')||'<p class="subtle">No matching tools. Try another search or star a tool to keep it here.</p>';
  if(state.family==='Following'&&!q&&state.filter==='All')$('#catalog-list').insertAdjacentHTML('afterbegin',`<div class="group-title">Your Pins</div>${state.pinned.length?state.pinned.map(x=>button(`${subjectName(x)} →`,`class="link-button" data-inspect="${x}"`)).join(''):'<p class="subtle">Pin a subject from its inspector.</p>'}`);
  if(focused!==undefined)$(`[data-tool="${focused}"]`)?.focus({preventScroll:true});
}
function choose(t) {
  if(t.family==='Layers'){state.layer=t.name;state.dataView=null;renderMap();renderLegend();renderData();renderList();$('[data-data="Maps"]').focus({preventScroll:true});completed('layer selected');tell(`${t.name} layer is on. Your held tool is unchanged.`);return;}
  if(t.name==='Supply diagnosis'){inspect('shop');return;}
  if(t.name==='Transit line'||t.name==='Line operation'){inspect('line');closeTools();return;}
  if(t.family==='Zoning'||t.name==='Street'||t.name==='Abandoned Building'||t.name==='School'||t.name==='Clinic') {
    state.tool=t;renderConsole();closeTools();tell(`${t.name} selected. Map clicks act; Cancel returns to inspection.`);return;
  }
  inspect('sketch:'+t.id);tell(`${t.name}: interaction placeholder. Read its intended scope in the inspector.`);
}
function renderConsole() {
  const t=state.tool;$('#active').innerHTML=`<span class="tool-mark" aria-hidden="true">${t?.family==='Zoning'?'▦':t?'↗':'⌖'}</span><div><b>${t?.name||'Inspect'}</b><small>${t?'Tool held · map clicks act':'Click the city to understand it'}</small></div>`;
  $('#options').innerHTML=t?.family==='Zoning'?`<label>Selection <select id="brush"><option>Parcels</option><option>Whole blocks</option></select></label>${t.name!=='Erase zoning'?'<label>Ceiling <select id="density"><option>Low</option><option>Medium</option><option>High</option></select></label>':''}`:'';
  if(t)$('#options').insertAdjacentHTML('beforeend',button('Cancel','id="cancel-tool"'));
  if($('#brush'))$('#brush').value=state.brush;if($('#density'))$('#density').value=state.density;
  $('#pause').textContent=state.paused?'▶':'Ⅱ';$('#pause').setAttribute('aria-label',state.paused?'Resume preview clock':'Pause preview clock');$('#speed').textContent=state.speed+'×';
  requestAnimationFrame(fit);
}
function fit(){ $('#city').style.setProperty('--console-clear',($('#console').offsetHeight+32)+'px');$('#city').style.setProperty('--legend-clear',state?.layer?($('#legend').offsetHeight+12)+'px':'0px');$('#city').classList.toggle('has-inspector',!!state?.selected); }
new ResizeObserver(fit).observe($('#console'));
new ResizeObserver(fit).observe($('#legend'));
function subjectName(id){return id==='shop'?'North Bank shop':id==='route'?'Shop supply route':id==='line'?'North Bank line':id==='stop'?'Market stop':id==='parcel'?`Parcel ${state.parcel+1}`:id.startsWith('sketch:')?catalogue[+id.split(':')[1]].name:'Selection';}
function inspect(id) {if(state.selected!==id&&state.selected)state.history.push(state.selected);state.selected=id;renderInspector();renderMap();fit();}
function renderInspector() {
  const id=state.selected,panel=$('#inspector');panel.hidden=!id;if(!id)return;
  let content='';
  if(id==='parcel')content=`<span class="eyebrow">LAND · PARCEL ${state.parcel+1}</span><h3>${state.parcels[state.parcel]?.name||'Open permissions'}</h3><p class="subtle">A place to build a hypothesis.</p><div class="fact"><span>Permission</span><strong>${state.parcels[state.parcel]?.name||'None'}</strong></div><div class="fact"><span>Density ceiling</span><strong>${state.parcels[state.parcel]?.density||'—'}</strong></div><p class="section-title">Existing Buildings</p><p class="subtle">Changing or erasing permissions leaves the drawn Buildings in place.</p>${button('Choose zoning →','class="link-button" data-open="Zoning"')}`;
  else if(id==='shop')content=`<span class="eyebrow">BUILDING · COMMERCIAL</span><h3>North Bank shop</h3><p class="subtle">Food for the neighbourhood</p><div class="fact"><span>Food in stock</span><strong>${state.delivered?'18':'0'} units</strong></div><div class="callout"><strong>${state.delivered?'Sample delivery arrived':state.repaired?'Connection restored; delivery pending':'Waiting for Food'}</strong><p>${state.delivered?'This staged outcome completes the interaction. It is not a simulation forecast.':state.repaired?'Keep watching the original problem after the intervention.':'A delivery cannot reach the shop. Follow its route to see the interruption.'}</p></div>${button('Inspect supply route →','class="link-button" data-action="route"')}${state.repaired&&!state.delivered?button('Advance sample delivery','class="primary" data-action="deliver"'):''}`;
  else if(id==='route')content=`<span class="eyebrow">EVIDENCE · SUPPLY</span><h3>Shop supply route</h3><p class="subtle">Outside Connection → North Bank shop</p><div class="callout"><strong>${state.repaired?'Missing link restored':'A Street connection is missing'}</strong><p>${state.repaired?'The connection changed. Stock remains the quantity to watch.':'The dashed amber link on the map interrupts this sample route.'}</p></div><div class="fact"><span>Affected destination</span><strong>1 shop</strong></div>${state.repaired?button('Return to shop →','class="link-button" data-inspect="shop"'):button('Repair connection →','class="primary link-button" data-action="repair"')}<p class="subtle">Illustrative cause and intervention; this study does not run routing.</p>`;
  else if(id==='line')content=`<span class="eyebrow">TRANSIT · OPERATION <span class="badge">Sketch</span></span><h3>North Bank line</h3><p class="subtle">Shared right-of-way · 3 stops</p><div class="fact"><span>Vehicles in service</span><strong>${state.vehicles}</strong></div><div class="fact"><span>Sample round trip</span><strong>24 min</strong></div><div class="fact"><span>Derived interval</span><strong>${(24/state.vehicles).toFixed(1)} min</strong></div><p class="section-title">Vehicle provision</p><label for="vehicles">Choose vehicles: <b id="vehicle-value">${state.draftVehicles}</b></label><input id="vehicles" type="range" min="1" max="12" value="${state.draftVehicles}"><p class="subtle">Preview: <span id="interval-preview">${(24/state.draftVehicles).toFixed(1)}</span> min between departures at the fixed sample journey time.</p>${button('Apply vehicle provision',`class="primary" data-action="apply-vehicles" ${state.draftVehicles===state.vehicles?'disabled':''}`)}${button('Inspect Market stop →','class="link-button" data-inspect="stop"')}<p class="subtle">Sample arithmetic, not a service forecast. In Borough, journey time comes from the city.</p>`;
  else if(id==='stop')content=`<span class="eyebrow">TRANSIT · ACCESS <span class="badge">Sketch</span></span><h3>Market stop</h3><div class="fact"><span>Serving line</span><strong>North Bank</strong></div><div class="fact"><span>Sample waiting count</span><strong>12 people</strong></div>${button('Open line operation →','class="link-button" data-inspect="line"')}<p class="subtle">Stops and their inspection are future placeholders.</p>`;
  else {
    const t=catalogue[+id.split(':')[1]];content=`<span class="eyebrow">${t.family} / ${t.group}</span><h3>${t.name}${badge(t)}</h3><p>${t.detail}.</p><p class="subtle">This entry tests discoverability and the space its future controls will need. No map action or simulated consequence is attached.</p>`;
  }
  panel.innerHTML=`<div class="heading"><div>${button('← Back',`class="back" data-action="back" ${!state.history.length?'disabled':''}`)}</div><div class="row">${button(state.pinned.includes(id)?'★ Pinned':'☆ Pin',`data-action="pin" aria-pressed="${state.pinned.includes(id)}"`)}${button('×','class="close" data-close="inspector" aria-label="Close inspector"')}</div></div><div class="inspector-body">${content}</div>`;
}
function renderLegend(){
  $('#legend').hidden=!state.layer;if(!state.layer){fit();return;}
  $('#legend').innerHTML=`${button('×','class="close" data-action="layer-off" aria-label="Turn layer off"')}<strong>${state.layer}</strong><div class="ramp"></div><div class="ends"><span>Lower</span><span>Higher</span></div><p class="subtle" style="margin:7px 0 0">Illustrative colour distribution · no measured values</p>`;
  requestAnimationFrame(fit);
}
const NS='http://www.w3.org/2000/svg';
function renderMap(){
  const focused=document.activeElement?.dataset.parcel;
  let svg='<defs><pattern id="grain" width="29" height="31" patternUnits="userSpaceOnUse"><circle cx="6" cy="9" r="1" fill="#607959" opacity=".13"/></pattern></defs><rect width="1440" height="900" fill="#a6b89c"/><path d="M1050 -50 C890 150 1230 330 1130 500 S950 790 1180 950" stroke="#7dabb0" stroke-width="200" fill="none"/><path d="M1050 -50 C890 150 1230 330 1130 500 S950 790 1180 950" stroke="#a6c9c7" stroke-width="170" fill="none"/><rect width="1440" height="900" fill="url(#grain)"/>';
  svg+=`<g id="map-content" transform="translate(720 430) scale(${state.zoom}) rotate(${state.angle}) translate(-720 -430)">`;
  svg+='<rect x="160" y="150" width="960" height="590" rx="20" fill="#c2c3af"/>';
  for(let i=0;i<48;i++){
    const col=i%8,row=Math.floor(i/8),x=178+col*114+(col>=4?18:0),y=170+row*90;
    const zone=state.parcels[i];svg+=`<g class="parcel ${zone?'painted':''} ${state.selected==='parcel'&&state.parcel===i?'selected':''}" data-parcel="${i}" role="button" tabindex="0" aria-label="Parcel ${i+1}, ${zone?.name||'unzoned'}"><rect class="lot" x="${x}" y="${y}" width="99" height="76" rx="3"/>`;
    if(i%5!==0)for(let b=0;b<3;b++)svg+=`<rect class="building" x="${x+9+b*27}" y="${y+14}" width="20" height="38"/><path class="roof" d="M${x+9+b*27} ${y+14}h20l-3 -6h-14z"/>`;
    else svg+=`<circle class="tree" cx="${x+32}" cy="${y+30}" r="14"/><circle class="tree" cx="${x+62}" cy="${y+45}" r="11"/>`;
    if(state.layer)svg+=`<rect class="overlay-wash" x="${x}" y="${y}" width="99" height="76" fill="${['#d4dfc5','#c7b66d','#965b4c'][(i*7+state.layer.length)%3]}"/>`;
    svg+='</g>';
  }
  if(state.scenario==='transit'||state.selected==='line'||state.selected==='stop')svg+='<path class="line-path" d="M420 430 H1060 V610 H760"/><circle class="line-stop" cx="520" cy="430" r="10" data-stop="1" role="button" tabindex="0" aria-label="Inspect west stop"/><circle class="line-stop" cx="760" cy="430" r="10" data-stop="1" role="button" tabindex="0" aria-label="Inspect Market stop"/><circle class="line-stop" cx="1060" cy="610" r="10" data-stop="1" role="button" tabindex="0" aria-label="Inspect east stop"/><text class="map-caption" x="770" y="411">Market stop</text>';
  if(state.route)svg+=`<path d="M100 430 H624 M760 430 H1040" stroke="#cc944b" stroke-width="7" fill="none"/><g data-link="1" role="button" tabindex="0" aria-label="${state.repaired?'Restored connection':'Repair highlighted missing connection'}"><rect x="635" y="407" width="121" height="45" rx="7" fill="${state.repaired?'#c0cbb2':'#ffe3a3'}" stroke="#9b6a2f" stroke-width="3" ${state.repaired?'':'stroke-dasharray="8 5"'}/><text class="map-caption" x="640" y="389">${state.repaired?'Restored':'Missing connection'}</text></g>`;
  svg+='</g>';$('#map').innerHTML=svg;
  if(focused!==undefined)$(`[data-parcel="${focused}"]`)?.focus({preventScroll:true});
}
function render(){renderRail();renderCatalogue();renderInspector();renderConsole();renderLegend();renderMap();renderData();$('#progress').textContent='';}
function cancelTool(){state.tool=null;renderConsole();if(state.family)renderList();tell('Tool cancelled. Map clicks inspect again.');completed('cancel tool');}
let stroke=null;
function parcelSet(i){if(state.brush==='Parcels')return[i];const x=Math.floor((i%8)/2)*2,y=Math.floor(Math.floor(i/8)/2)*2;return[y*8+x,y*8+x+1,(y+1)*8+x,(y+1)*8+x+1];}
function preview(i){for(const id of parcelSet(i))stroke.add(id);document.querySelectorAll('[data-parcel]').forEach(p=>p.classList.toggle('preview',stroke.has(+p.dataset.parcel)));tell(`${stroke.size} parcels · ${state.tool.name} · release to apply`);}
function commitStroke(){if(!stroke)return;const count=stroke.size;for(const i of stroke){if(state.tool.name==='Erase zoning')delete state.parcels[i];else state.parcels[i]={name:state.tool.name,density:state.density};}stroke=null;renderMap();renderInspector();completed(state.tool.name==='Erase zoning'?'erase permissions':'paint '+state.brush.toLowerCase());tell(`${count} parcels ${state.tool.name==='Erase zoning'?'erased':'zoned'}. Existing Buildings remain.`);}
function actParcel(i){
  if(state.tool?.family==='Zoning'){stroke=new Set();preview(i);commitStroke();return;}
  if(state.tool){tell(state.tool.name==='Street'?'For this workflow, click the highlighted missing connection.':'Placement is outside these three workflows. This tool is shown to test selection and cancellation.');return;}
  state.parcel=i;inspect('parcel');tell(`Parcel ${i+1} selected.`);
}
function repairLink(){if(state.tool?.name!=='Street'){tell('Inspect the route and choose Repair connection first.');return;}state.repaired=true;completed('repair connection');inspect('shop');state.tool=null;renderConsole();tell('Sample connection restored. The shop remains selected; advance the sample to inspect the outcome.');}
$('#map').addEventListener('pointerdown',e=>{const p=e.target.closest('[data-parcel]');if(e.button!==0||!p||state.tool?.family!=='Zoning')return;e.preventDefault();stroke=new Set();preview(+p.dataset.parcel);});
$('#map').addEventListener('pointerover',e=>{const p=e.target.closest('[data-parcel]');if(!p)return;if(stroke)preview(+p.dataset.parcel);else tell(state.tool?`${state.tool.name} · parcel ${+p.dataset.parcel+1} · ${state.tool.family==='Zoning'?'click or drag to apply':'click to preview action'}`:`Parcel ${+p.dataset.parcel+1} · click to inspect`);});
window.addEventListener('pointerup',()=>{if(stroke){commitStroke();suppressMapClick=true;}if(suppressMapClick)setTimeout(()=>suppressMapClick=false,0);});
let suppressMapClick=false;
window.addEventListener('pointercancel',()=>{stroke=null;renderMap();tell('Stroke cancelled.');});
$('#map').addEventListener('click',e=>{if(suppressMapClick)return;const p=e.target.closest('[data-parcel]');if(p)actParcel(+p.dataset.parcel);if(e.target.closest('[data-link]'))repairLink();if(e.target.closest('[data-stop]'))inspect('stop');});
$('#map').addEventListener('keydown',e=>{if(!['Enter',' '].includes(e.key))return;e.preventDefault();const p=e.target.closest('[data-parcel]');if(p)actParcel(+p.dataset.parcel);if(e.target.closest('[data-link]'))repairLink();if(e.target.closest('[data-stop]'))inspect('stop');});
document.addEventListener('keydown',e=>{if(e.key!=='Escape')return;if(stroke){stroke=null;suppressMapClick=true;renderMap();tell('Stroke cancelled before applying.');}else if(!$('#camera').hidden){$('#camera').hidden=true;$('#camera-toggle').setAttribute('aria-expanded','false');$('#camera-toggle').focus();}else if(state.dataView&&(state.lastChooser==='data'||!state.family)){closeData();}else if(state.family){closeTools();tell('Tool chooser closed; held tool and layer preserved.');}else if(state.tool)cancelTool();});
document.addEventListener('click',e=>{
 const b=e.target.closest('button');if(!b||b.disabled)return;
 if(b.dataset.family)openFamily(b.dataset.family);
 if(b.dataset.open){state.family=b.dataset.open;state.lastChooser='tools';state.query='';state.filter='All';renderRail();renderCatalogue();}
 if(b.dataset.data){state.dataView=state.dataView===b.dataset.data?null:b.dataset.data;state.lastChooser='data';renderData();}
 if(b.dataset.mapGroup){state.mapGroup=b.dataset.mapGroup;renderData();$(`[data-map-group="${state.mapGroup}"]`).focus({preventScroll:true});}
 if(b.dataset.contextMap){choose(catalogue.find(t=>t.family==='Layers'&&t.name===b.dataset.contextMap));completed('contextual data link');}
 if(b.dataset.dataInspect){inspect(b.dataset.dataInspect);closeData();}
 if(b.dataset.close==='data')closeData();
 if(b.dataset.close==='catalog'){closeTools();completed('close chooser, keep tool');}
 if(b.dataset.close==='inspector'){state.selected=null;$('#inspector').hidden=true;fit();}
 if(b.dataset.filter){state.filter=b.dataset.filter;renderCatalogue();}
 if(b.dataset.tool!==undefined)choose(catalogue[+b.dataset.tool]);
 if(b.dataset.star!==undefined){const id=+b.dataset.star;state.favourites=state.favourites.includes(id)?state.favourites.filter(x=>x!==id):[...state.favourites,id];renderList();completed('favourite');}
 if(b.dataset.inspect)inspect(b.dataset.inspect);
 switch(b.dataset.action){
 case 'back':state.selected=state.history.pop();renderInspector();renderMap();fit();completed('inspection history');break;
 case 'pin':state.pinned=state.pinned.includes(state.selected)?state.pinned.filter(x=>x!==state.selected):[...state.pinned,state.selected];renderInspector();if(state.family)renderList();renderData();completed('Pin');break;
 case 'route':state.route=true;inspect('route');completed('follow Evidence');break;
 case 'repair':state.tool=catalogue.find(t=>t.name==='Street');closeTools();renderConsole();tell('Street held. Click the dashed missing connection on the map.');break;
 case 'deliver':state.delivered=true;renderInspector();completed('watch sample outcome');tell('Sample delivery advanced: Food stock changed from 0 to 18 units.');break;
 case 'apply-vehicles':state.vehicles=state.draftVehicles;renderInspector();completed('change vehicle provision');tell('Sample provision applied. The interval is derived from vehicle count and the fixed sample journey time.');break;
 case 'layer-off':state.layer=null;renderLegend();renderMap();if(state.family)renderList();renderData();tell('Layer off. Tool and selection preserved.');break;
 }
 if(b.id==='cancel-tool')cancelTool();
 if(b.dataset.camera){const c=b.dataset.camera;if(c==='in')state.zoom=Math.min(1.6,state.zoom+.1);if(c==='out')state.zoom=Math.max(.7,state.zoom-.1);if(c==='left')state.angle-=15;if(c==='right')state.angle+=15;if(c==='reset'){state.zoom=1;state.angle=0;}renderMap();}
});
document.addEventListener('input',e=>{
 if(e.target.id==='search'){state.query=e.target.value;renderList();}
 if(e.target.id==='vehicles'){state.draftVehicles=+e.target.value;$('#vehicle-value').textContent=e.target.value;$('#interval-preview').textContent=(24/state.draftVehicles).toFixed(1);$('[data-action="apply-vehicles"]').disabled=state.draftVehicles===state.vehicles;}
});
document.addEventListener('change',e=>{if(e.target.id==='brush')state.brush=e.target.value;if(e.target.id==='density')state.density=e.target.value;});
$('#scenario').onchange=reset;$('#reset').onclick=reset;
$('#theme').onclick=e=>{document.body.classList.toggle('dark');e.target.textContent=document.body.classList.contains('dark')?'Light theme':'Dark theme';};
$('#text-size').onchange=e=>{document.documentElement.style.setProperty('--scale',e.target.value);requestAnimationFrame(fit);};
$('#camera-toggle').onclick=()=>{$('#camera').hidden=!$('#camera').hidden;$('#camera-toggle').setAttribute('aria-expanded',!$('#camera').hidden);};
$('#pause').onclick=()=>{state.paused=!state.paused;renderConsole();tell(state.paused?'Preview clock paused.':'Preview clock running; scenario outcomes advance only through their explicit controls.');};
$('#speed').onclick=()=>{state.speed=state.speed===4?1:state.speed*2;renderConsole();};
setInterval(()=>{if(state.paused)return;state.minute+=state.speed;$('#phase').textContent=['Morning','Midday','Evening','Night'][Math.floor(state.minute/20)%4];},1000);
reset();
