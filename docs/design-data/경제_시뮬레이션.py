import json,random,math,statistics,time
from pathlib import Path
BASE=Path(__file__).resolve().parent
D=json.loads((BASE/'경제_설계_기준값.json').read_text(encoding='utf-8'))
PROFILES={'casual':[(480,20),(1200,20)],'standard':[(480,20),(780,20),(1260,20)],'active':[(480,30),(720,30),(1020,30),(1320,30)]}
DECK=['H01','H02','H03','H04','mine','tower','fence','fire'];POOL=DECK+['H05','H06','crossbow','magicTower','rally','mining'];B=[1,4,9,16,26];RANK=['N','R','SR','SSR','UR']
def run(seed,profile,strategy,days=180,match=4,pool_mode='expanding',stage_strategy='mixed'):
 rng=random.Random(seed);owned=[True]*8+[False]*6;copies=[0]*14;levels=[1]*14;ranks=[0]*14;stone=0;race=0;hero=False;shards=0;boxes=[];active=None;wins=0;attempts=0;claimed=0;discarded=0;seen={};snap={};upgrades=0
 def record(key,t):
  if key not in seen:seen[key]=(t-480)/1440
 def grow(t):
  nonlocal stone,race,upgrades
  while True:
   order=[0] if strategy=='focus' else sorted(range(8),key=lambda i:(levels[i],ranks[i],i))
   changed=False
   for i in order:
    cap=D['caps'][POOL[i]];rank=ranks[i];level=levels[i]
    if level>=cap['maxLevel']:continue
    if rank<4 and level==B[rank+1]:
     cost=D['evolutionCosts'][rank]
     if race<cost:continue
     race-=cost;ranks[i]+=1;changed=True
    else:
     k=level-B[rank];cc=D['cardCosts'][rank][k];sc=D['stoneCosts'][rank][k]
     if copies[i]<cc or stone<sc:continue
     copies[i]-=cc;stone-=sc;levels[i]+=1;upgrades+=1;record('first_upgrade',t);changed=True
    if changed:
     for rank,name in [(1,'R'),(2,'SR'),(3,'SSR'),(4,'UR')]:
      if ranks[0]>=rank:record('focus_'+name,t)
      if all(r>=rank for r in ranks[:8]):record('deck_'+name,t)
     if levels[0]>=36:record('focus_cap',t)
     break
   if not changed:break
 def grant(box,t):
  nonlocal stone,race,hero,shards,claimed
  idx,size=box;ch=D['chests'][idx];claimed+=1;stone+=ch['stones'];ids=rng.sample(range(size),ch['bundles']);qty=ch['copies']//ch['bundles']
  for i in ids:
   received=qty
   if not owned[i]:owned[i]=True;received-=1
   if levels[i]>=D['caps'][POOL[i]]['maxLevel']:stone+=received*D['maxedCardStoneConversion']
   else:copies[i]+=received
  if rng.random()<ch['raceChance']:race+=rng.randint(ch['raceMin'],ch['raceMax'])
  full=rng.random()<ch['heroChance']
  got_shards=rng.randint(ch['shardMin'],ch['shardMax']) if rng.random()<ch['shardChance'] else 0
  if full:
   if hero:got_shards+=D['heroDuplicateShards']
   else:hero=True;stone+=shards*D['ownedHeroShardStoneConversion'];shards=0;record('hero',t)
  if hero:stone+=got_shards*D['ownedHeroShardStoneConversion']
  else:
   shards+=got_shards
   if shards>=D['heroUnlockShards']:hero=True;stone+=(shards-D['heroUnlockShards'])*D['ownedHeroShardStoneConversion'];shards=0;record('hero',t)
  grow(t)
 def service(t):
  nonlocal active
  if active is not None and active<=t:
   grant(boxes.pop(0),t);active=None
  if active is None and boxes:active=t+D['chests'][boxes[0][0]]['minutes']
 for day in range(days):
  for start,length in PROFILES[profile]:
   t=day*1440+start;end=t+length;service(t)
   while t+match<=end:
    eligible=len(boxes)<5
    kind='normal' if wins<5 else 'elite' if wins==5 else ('elite' if attempts%3==2 else 'normal')
    if stage_strategy=='elite_only' and wins>=5:kind='elite'
    t+=match;attempts+=1;service(t)
    if rng.random()<.8:
     wins+=1
     if eligible:
      idx=rng.choices(range(5),D['rarityWeights'][kind])[0];size=14 if pool_mode=='wide' or (pool_mode=='expanding' and wins>12) else 8;boxes.append((idx,size));service(t)
     else:discarded+=1
  if day+1 in [7,30,90,180]:snap[str(day+1)]={'focus_level':levels[0],'deck_mean_level':sum(levels[:8])/8,'claimed':claimed,'no_chest_wins':discarded,'stones_balance':stone,'race_balance':race}
 assert len(boxes)<=5 and stone>=0 and race>=0 and all(v>=0 for v in copies)
 assert all(levels[i]<=D['caps'][POOL[i]]['maxLevel'] for i in range(14))
 return {'seen':seen,'snap':snap}
def aggregate(runs):
 keys=['first_upgrade','focus_R','focus_SR','focus_SSR','focus_UR','focus_cap','deck_R','deck_SR','hero'];out={}
 for k in keys:
  vals=sorted(r['seen'].get(k,math.inf) for r in runs)
  out[k]={'reached_percent':100*sum(math.isfinite(v) for v in vals)/len(vals),'days_p10_p50_p90':[round(vals[math.ceil(p*len(vals))-1],2) if math.isfinite(vals[math.ceil(p*len(vals))-1]) else None for p in [.1,.5,.9]]}
 out['snapshots']={day:{key:round(statistics.median(r['snap'][day][key] for r in runs),2) for key in runs[0]['snap'][day]} for day in runs[0]['snap']}
 return out
def main():
 start=time.time();result={'samples_per_group':200,'days':180,'seed_base':853,'model':'scheduled sessions, FIFO manual service at login/battle end; no background autostart; 80% fixed wins; four-minute battles; first elite on sixth win then two normal to one elite attempt mix; pool8 to14 after12wins (first twelve rewards retain pool8); zero initial growth currency; no pity; one race and one eligible hero; no difficulty feedback','groups':{}}
 for profile in PROFILES:
  for strat in ['focus','balanced']:
   result['groups'][profile+'_'+strat]=aggregate([run(853+i,profile,strat) for i in range(200)])
 # sensitivity is a separate scenario, not a replacement for the base model
 result['sensitivity_standard_8minute_balanced']=aggregate([run(2853+i,'standard','balanced',match=8) for i in range(200)])
 result['sensitivity_standard_elite_only_balanced']=aggregate([run(4853+i,'standard','balanced',stage_strategy='elite_only') for i in range(200)])
 result['sensitivity_standard_chapter1_balanced']=aggregate([run(6853+i,'standard','balanced',pool_mode='chapter1') for i in range(200)])
 (BASE/'경제_시뮬레이션_결과.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
 for name,g in result['groups'].items():print(name,{k:g[k] for k in ['first_upgrade','focus_R','focus_SR','focus_UR','deck_R','hero']},g['snapshots']['30'])
 print('seconds',round(time.time()-start,1))

if __name__=="__main__":main()
