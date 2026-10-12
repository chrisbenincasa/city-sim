import sys, os, re
# Applies $VARY ("key=value ...") to the copy of rulesets/base/ in DIR. Called by tune.sh.
#
# usage: VARY="..." vary.py DIR
#
# Each key edits one authored value by pattern, so a change to base's layout can break a key;
# a key whose pattern no longer matches fails its assert. Keys and what they set:
#   dflour sflour ssund    dwelling flour, shopfront flour and shopfront sundries capacities
#   gwage mwage twage      grocer, mill and teaching wage_per_day
#   ggrant mgrant          grocer and mill opening_grant
#   pay                    every pay_period_days
#   bankrupt quit          every goes_bankrupt_after_days_in_arrears; add quits_after_days_unpaid
#   draw                   add pays_owner_above_pay_periods to every waged trade
#   fund                   school funding amount
#   btax itax upkeep       business tax threshold, income tax allowance, Street upkeep
#   trade_thr trade_cool   trade Zone Rule build threshold and cooldown, in Days
#   found=BAND,TICKS       append a [founding] section
#   lpd                    labour_per_day
#   millout milllab        mill Rule flour output and labour input
#   bakeout bakelab        bake Rule sundries output and labour input
#   grindlab grindrate     grind Rule labour input and rate; nogrind=1 deletes the Rule
#   buyflour               flour per buy_flour application
#   cons                   consume Rule rate
#   parkrate cars          floor tiles per parking space, car ownership percent
#   blevy hlevy            daily levy on Business or Household balances, in percent
#   rebate                 daily treasury payment to every Household
#   demandmill=THR,COOL    move mills to their own kind raised by flour demand
#   noflourimport flouredge=EDGE   remove flour from every gate, or from all but one edge
d=sys.argv[1]; kv=dict(a.split('=') for a in os.environ.get('VARY','').split() if a)
def sub(f, pat, rep):
    p=os.path.join(d,f); s=open(p).read(); n=re.subn(pat, rep, s, flags=re.S); 
    assert n[1]>=1, (f,pat); open(p,'w').write(n[0])
H='housing.toml'
if 'dflour' in kv: sub(H, r'(id = "dwelling".*?resource = "flour",\s*capacity = )\d+', r'\g<1>'+kv['dflour'])
if 'sflour' in kv: sub(H, r'(id = "shopfront".*?resource = "flour",\s*capacity = )\d+', r'\g<1>'+kv['sflour'])
if 'ssund' in kv: sub(H, r'(id = "shopfront".*?resource = "sundries", capacity = )\d+', r'\g<1>'+kv['ssund'])
if 'gwage' in kv: sub(H, r'(id = "grocer".*?wage_per_day\s*= )\d+', r'\g<1>'+kv['gwage'])
if 'mwage' in kv: sub(H, r'(id = "mill".*?wage_per_day\s*= )\d+', r'\g<1>'+kv['mwage'])
if 'ggrant' in kv: sub(H, r'(id = "grocer"\nopening_grant = )\d+', r'\g<1>'+kv['ggrant'])
if 'mgrant' in kv: sub(H, r'(id = "mill"\nopening_grant = )\d+', r'\g<1>'+kv['mgrant'])
if 'btax' in kv: sub('city.toml', r'(threshold_per_day  = )\d+', r'\g<1>'+kv['btax'])
if 'upkeep' in kv: sub('city.toml', r'(upkeep_per_segment_per_day\s*= )\d+', r'\g<1>'+kv['upkeep'])
if 'fund' in kv: sub('school.toml', r'(resource = "money", amount = )\d+', r'\g<1>'+kv['fund'])
if 'twage' in kv: sub('school.toml', r'(wage_per_day\s*= )\d+', r'\g<1>'+kv['twage'])
if 'grindlab' in kv: sub(H, r'(id      = "grind".*?resource = "labour", amount = )\d+', r'\g<1>'+kv['grindlab'])
if 'trade_thr' in kv: sub(H, r'(id            = "trade".*?build_threshold_days = )\d+', r'\g<1>'+kv['trade_thr'])
if 'trade_cool' in kv: sub(H, r'(cooldown_days\s*= )\d+', r'\g<1>'+kv['trade_cool'])
if 'bakeout' in kv: sub(H, r'(id      = "bake".*?outputs = \[ \{ scope = "local", resource = "sundries", amount = )\d+', r'\g<1>'+kv['bakeout'])
if 'buyflour' in kv: sub(H, r'(id      = "buy_flour".*?resource = "flour",  amount = )\d+(.*?resource = "flour", amount = )\d+', r'\g<1>'+kv['buyflour']+r'\g<2>'+kv['buyflour'])
if 'grindrate' in kv: sub(H, r'(id      = "grind"\norder   = 7\nkind    = "shopfront"\nrate    = )\d+', r'\g<1>'+kv['grindrate'])
if 'nogrind' in kv: sub(H, r'\[\[rule\]\]\nid      = "grind".*?outputs = \[ \{ scope = "local", resource = "flour", amount = 1 \} \]\n', '')
if 'milllab' in kv: sub(H, r'(id      = "mill"\n.*?resource = "labour", amount = )\d+', r'\g<1>'+kv['milllab'])
if 'millout' in kv: sub(H, r'(id      = "mill"\n.*?outputs = \[ \{ scope = "local", resource = "flour", amount = )\d+', r'\g<1>'+kv['millout'])
if 'bakelab' in kv: sub(H, r'(id      = "bake".*?resource = "labour", amount = )\d+', r'\g<1>'+kv['bakelab'])
if 'parkrate' in kv: sub('city.toml', r'(floor_tiles_per_parking_space = )\d+', r'\g<1>'+kv['parkrate'])
if 'cars' in kv: sub('city.toml', r'(car_ownership_percent = )\d+', r'\g<1>'+kv['cars'])
if 'cons' in kv: sub(H, r'(id      = "consume"\norder   = 2\nkind    = "dwelling"\nrate    = )\d+', r'\g<1>'+kv['cons'])
if 'itax' in kv: sub('city.toml', r'(allowance_per_day       = )\d+', r'\g<1>'+kv['itax'])
if 'demandmill' in kv:
    thr, cool = kv['demandmill'].split(',')
    sub(H, r'    \{ resource = "flour",    capacity = 256, owner = "business" \},\n    \{ resource = "money",    owner = "business" \},\n    \{ resource = "labour",   owner = "business" \},\n\]\nhouses   = true\npremises = true\nrent     = 400\nparked   = true\nbusiness = "mill"\n',
        ']\nhouses   = true\nrent     = 400\nparked   = true\n\n[[building]]\nid = "watermill"\nlabel = "Mill"\nbins = [\n    { resource = "flour",  capacity = 256, owner = "business" },\n    { resource = "money",  owner = "business" },\n    { resource = "labour", owner = "business" },\n]\npremises = true\nparked   = true\nbusiness = "mill"\n')
    sub(H, r'(id      = "mill"\norder   = 4\nkind    = )"dwelling"', r'\g<1>"watermill"')
    sub(H, r'(\[\[rule\]\]\nid      = "restock")', '[[zone_rule]]\nid            = "milling"\nkind          = "watermill"\nzone          = 1\ninterval      = 32\nrevisit_ticks = 2048\nbuild_threshold_days = '+thr+'\ncooldown_days        = '+cool+'\n\n\\g<1>')
if 'noflourimport' in kv: sub('people.toml', r'  \{ resource = "flour",    price = \d+ \},\n', '')
if 'flouredge' in kv:
    keep = kv['flouredge']
    p=os.path.join(d,'people.toml'); blocks=re.split(r'(?=\[\[hinterland\]\]\n)', open(p).read()); out=[]
    for b in blocks:
        m=re.match(r'\[\[hinterland\]\]\nid = "(\w+)"', b)
        if m and m.group(1)!=keep: b=re.sub(r'  \{ resource = "flour",    price = \d+ \},\n','',b)
        out.append(b)
    open(p,'w').write(''.join(out))
if 'lpd' in kv: sub('city.toml', r'(labour_per_day = )\d+', r'\g<1>'+kv['lpd'])
if 'found' in kv:
    band, ticks = kv['found'].split(',')
    p=os.path.join(d,'city.toml'); open(p,'a').write('\n[founding]\nfounding_band    = '+band+'\nreconsider_ticks = '+ticks+'\n')
if 'pay' in kv: sub(H, r'(pay_period_days = )\d+', r'\g<1>'+kv['pay'])
def policy(pid, order, sweeps, apply, transfer):
    p=os.path.join(d,'school.toml'); open(p,'a').write(f'\n[[policy]]\nid       = "{pid}"\norder    = {order}\nsweeps   = "{sweeps}"\ninterval = 2048\napply    = {apply}\ntransfer = {transfer}\n')
if 'blevy' in kv: policy('business_levy', 2, 'business', '{ derived = "balance", percent = '+kv['blevy']+' }', '{ from = "local", to = "global", resource = "money", amount = 1 }')
if 'hlevy' in kv: policy('household_levy', 3, 'household', '{ derived = "balance", percent = '+kv['hlevy']+' }', '{ from = "local", to = "global", resource = "money", amount = 1 }')
if 'rebate' in kv: policy('household_rebate', 4, 'household', '{ min = '+kv['rebate']+', max = '+kv['rebate']+' }', '{ from = "global", to = "local", resource = "money", amount = 1 }')
if 'bankrupt' in kv: sub(H, r'(goes_bankrupt_after_days_in_arrears = )\d+', r'\g<1>'+kv['bankrupt'])
if 'quit' in kv:
    for f in os.listdir(d):
        if f.endswith('.toml'):
            p=os.path.join(d,f); s=open(p).read()
            s=re.sub(r'(goes_bankrupt_after_days_in_arrears\s*=\s*\d+\n)', r'\g<1>quits_after_days_unpaid = '+kv['quit']+'\n', s)
            open(p,'w').write(s)
if 'draw' in kv:
    for f in os.listdir(d):
        if f.endswith('.toml'):
            p=os.path.join(d,f); s=open(p).read()
            s=re.sub(r'(goes_bankrupt_after_days_in_arrears\s*=\s*\d+\n)', r'\g<1>pays_owner_above_pay_periods = '+kv['draw']+'\n', s)
            open(p,'w').write(s)
