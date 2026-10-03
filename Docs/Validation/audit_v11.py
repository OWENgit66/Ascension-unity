"""Audit real player evidence; does not control or modify the game. Python stdlib only."""
import json, math
from pathlib import Path
from collections import Counter

root = Path(__file__).resolve().parents[2]
cfg = json.loads((root/'UnityProject/Assets/Ascension/Mechanic/Resources/Balance.json').read_text(encoding='utf-8'))
results=[]
for path in sorted((root/'Docs/Evidence').glob('v11-*-result.json')):
    native=path.with_name(path.name.replace('-result.json','-native.jsonl'))
    rows=[json.loads(line) for line in native.read_text(encoding='utf-8').splitlines()]
    previous={}; maxima=Counter(); wind_pairs=0; slowed=0; shield_slow=False; dash_slow=False; left_water=False; was_slow=False
    for row in rows:
        counts=Counter(e['kind'] for e in row['elements'])
        for kind, cap in [(1,5),(5,4),(6,3)]:
            assert counts[kind]<=cap, (path.name,'cap',kind,counts[kind])
            maxima[kind]=max(maxima[kind],counts[kind])
        for e in row['elements']:
            if e['kind']!=6: continue
            pos=e['position']; p=(pos['x'],pos['z'])
            assert math.hypot(*p)<=cfg['arenaRadius']-e['radius']+.002, 'ball out of bounds'
            # Gameplay events can be emitted midway through a Tick, before every ball advances.
            # Periodic snapshots are taken after the complete Tick; compare those for velocity.
            if row['eventKey']=='snapshot':
                if e['id'] in previous:
                    t, old=previous[e['id']]; dt=row['elapsed']-t
                    assert math.dist(old,p)<=cfg['moveSpeed']*cfg['final']['ballSpeed']*dt+.025, 'ball jump'
                previous[e['id']]=(row['elapsed'],p)
        if row['eventKey']=='final_cue_windlightning':
            winds=[e for e in row['elements'] if e['kind']==0]
            partners=[s for s in row['strikes'] if s['locked'] and s['age']<.01 and 1.1<=s['warning']<=1.3]
            assert winds and partners, 'wind without visible locked partner'
            assert all(e['warning']>=partners[-1]['warning'] for e in winds), 'push before lightning'
            wind_pairs+=1
        slow=row.get('movementMultiplier',1)<1
        if slow:
            assert abs(row['movementMultiplier']-.7)<.001, 'water slow stacked'
            slowed+=1; shield_slow |= row['shieldRemaining']>0; dash_slow |= row['eventKey']=='dash'
        if was_slow and not slow: left_water=True
        was_slow=slow
    result=json.loads(path.read_text(encoding='utf-8'))
    results.append(dict(file=path.name,status=result['status'],seconds=round(result['elapsed'],3),hp=result['hp'],qi=result['qi'],minQi=result['minQi'],dash=result['events'].get('dash',0),shield=result['events'].get('shield',0),pickup=result['events'].get('qi_collected',0),waves=result['finalWaves'],maxFire=maxima[1],maxWater=maxima[5],maxBall=maxima[6],windPairs=wind_pairs,slowedRows=slowed,shieldWhileSlowed=shield_slow,dashWhileSlowed=dash_slow,waterExit=left_water))
    if path.name.startswith('v11-r1-'):
        born={}; impacted=set(); locked={}; delays=[]; randoms=[]; stage_counts=Counter(); random_max=0; last_random=None
        for row in rows:
            strikes=row['strikes']; random_now=[s for s in strikes if s['kind']==6]
            random_max=max(random_max,len(random_now))
            assert len(random_now)<=cfg['randomLimits'][row['stage']-1], 'random warning cap'
            for i,s in enumerate(random_now):
                p=(s['position']['x'],s['position']['z'])
                assert math.hypot(*p)+s['radius']<=cfg['arenaRadius']-.049, 'random circle outside arena'
                for other in random_now[i+1:]:
                    q=(other['position']['x'],other['position']['z'])
                    assert math.dist(p,q)>=s['radius']+other['radius']+.49, 'random circles overlap'
            for s in strikes:
                p=(s['position']['x'],s['position']['z'])
                if s['locked']:
                    if s['id'] in locked:assert math.dist(locked[s['id']],p)<.001, 'strike moved after lock'
                    locked[s['id']]=p
                if row['eventKey'].startswith('warning_') and s['id'] not in born:
                    born[s['id']]=row['elapsed']
                    if s['kind']==6:
                        if last_random is not None:assert math.dist(last_random,p)>=cfg['randomRadius']*2-.001, 'repeated random location'
                        last_random=p;randoms.append(p);stage_counts[row['stage']]+=1
                if row['eventKey']=='lightning' and s['impacted'] and s['id'] not in impacted:
                    impacted.add(s['id']);assert s['id'] in born, 'impact without warning'
                    delay=row['elapsed']-born[s['id']]
                    assert delay>=s['warning']-.002, 'damage VFX before warning finishes'
                    delays.append(delay)
        results[-1].update(impactTimingChecks=len(delays),warningToImpactSeconds=[round(min(delays),4),round(max(delays),4)],randomWarnings=len(randoms),randomByStage=dict(stage_counts),maxRandomWarnings=random_max,randomQuadrants=len({(x>=0,z>=0) for x,z in randoms}),pressureDeferrals=result['events'].get('random_pressure_deferred',0))
print(json.dumps({'passed':True,'runs':results},ensure_ascii=False,indent=2))
