import csv,json,urllib.request,urllib.error,concurrent.futures,time
from pathlib import Path
O=Path(__file__).resolve().parent
rows=[r for r in csv.DictReader((O/'sources.csv').open()) if r['url'].startswith('https:')]
def check(r):
 try:
  req=urllib.request.Request(r['url'],method='HEAD',headers={'User-Agent':'Mozilla/5.0 (architecture research link verification)'})
  with urllib.request.urlopen(req,timeout=25) as res:return dict(source_id=r['source_id'],url=r['url'],method='HEAD',status=res.status,final_url=res.url,content_type=res.headers.get('Content-Type',''),note='Availability check only; not fresh content inspection.')
 except Exception as e:return dict(source_id=r['source_id'],url=r['url'],method='HEAD',status=getattr(e,'code',None),error=str(e),note='HEAD failure does not invalidate separately downloaded/inspected content; see source record.')
with concurrent.futures.ThreadPoolExecutor(max_workers=4) as ex:results=list(ex.map(check,rows))
(O/'evidence/external-links.json').write_text(json.dumps(results,indent=2)+'\n')
print(json.dumps([{'source':r['source_id'],'status':r['status']} for r in results],indent=2))
