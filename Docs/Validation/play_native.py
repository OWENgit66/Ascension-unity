"""Standalone validation: same T07 strategy, local UDP inputs, no GameFactory Python dependency.
Adapted from Ascension Tools/play_expansion.py; only transport and artifact paths changed.
Socket send success is not gameplay success: inspect resulting native events.
"""
import argparse,json,math,time,socket
from pathlib import Path
from collections import Counter
r=Path(__file__).resolve().parents[2]
cfg=json.loads((r/'UnityProject/Assets/Ascension/Mechanic/Resources/Balance.json').read_text(encoding='utf-8'));R=cfg['arenaRadius'];speed=cfg['moveSpeed'];wind_speed=R*cfg['final']['windDistanceR']/cfg['final']['windDuration']
p=argparse.ArgumentParser();p.add_argument('strategy',choices=list('ABCDE'));p.add_argument('--label',default='v1');p.add_argument('--radius',type=float,default=8);p.add_argument('--final-behaviour',choices=['observe','stand','shield','dash','circle','bloomdash'],default='observe');p.add_argument("--hazard-camera",action="store_true",help="Turn the ordinary camera toward Final hazards for visual review; compensate local movement");a=p.parse_args()
# Validation-only UDP client for the vendored Runtime wire protocol. Standard library only.
# Does not patch HP, time, resources, hazards or executable code.
class NativeInput:
 def __init__(self):
  self.runtime=self;self.sessions=self;self.sock=socket.socket(socket.AF_INET,socket.SOCK_DGRAM)
  self.fields={'world_id':'ascension_whitebox','participant_id':'independence','controller_id':'independence-controller','entity_id':'independence-input'}
 def send(self,message_type,**values):
  self.sock.sendto(json.dumps(dict(self.fields,type=message_type,**values)).encode(),('127.0.0.1',30131))
 def apply_input(self,controller=None,**values):
  self.send('input_state',**values);return {'ok':True}
 def leave(self,**values):
  self.send('participant_offline');self.sock.close()
c=NativeInput();c.send('sync_session',kind='human',mode='player',priority=0)
controller='independence-controller';diag=Path((r/'Docs/Evidence/latest-runtime-path.txt').read_text(encoding='utf8'))

offset=diag.stat().st_size if diag.exists() else 0;seq=0;rows=[];snap=None;start=time.monotonic();lastdir=(1,0);started=False
stem=f't065-{a.label}-{a.strategy}-{time.time_ns()}';trace=(r/'Docs/Evidence'/f'{stem}-inputs.jsonl').open('w',encoding='utf-8')
def norm(x,z):
 l=max(1,math.hypot(x,z));return x/l,z/l
def clamp(x,z):
 l=max(1,math.hypot(x,z)/R);return x/l,z/l
print('Ready: click Start/Restart. '+stem,flush=True)
try:
 while time.monotonic()-start<520:
  if diag.exists():
   with diag.open(encoding='utf-8') as f:
    f.seek(offset)
    for line in f:
     try:
      row=json.loads(line);snap=row
      if row['eventKey']=='started': rows=[];started=True
      if started:rows.append(row)
     except json.JSONDecodeError:pass
    offset=f.tell()
  mx=mz=0;dash=shield=False
  if started and snap:
   if snap['status'] in ('Won','Lost'):break
   if snap['status']=='Playing':
    t=snap['elapsed'];x=snap['position']['x'];z=snap['position']['z'];qi=snap['qi'];rad=max(.01,math.hypot(x,z))
    mx,mz=norm(-z/rad+x/rad*(a.radius-rad)*.7,x/rad+z/rad*(a.radius-rad)*.7) if rad>2 else (1,0)
    if a.strategy in 'AE':
     hazards=snap.get('strikes',[]);elements=snap.get('elements',[]);orbs=snap.get('orbs',[])
     target=min(orbs,key=lambda q:math.hypot(q['position']['x']-x,q['position']['z']-z))['position'] if orbs and qi<75 else None
     def predicted(v,tm):
      px=x+v[0]*speed*tm;pz=z+v[1]*speed*tm
      for e in elements:
       if e['kind']==0:
        duration=max(0,min(tm,e['warning']+e['duration']-e['age'])-max(0,e['warning']-e['age']))
        px+=e['direction']['x']*wind_speed*duration;pz+=e['direction']['z']*wind_speed*duration
      return clamp(px,pz)
     def distance(e,px,pz):
      ax=e['position']['x'];az=e['position']['z']
      if e['kind'] in (2,4):
       dx=e['end']['x']-ax;dz=e['end']['z']-az;l=dx*dx+dz*dz
       u=max(0,min(1,((px-ax)*dx+(pz-az)*dz)/l)) if l else 0
       ax+=u*dx;az+=u*dz
      return math.hypot(px-ax,pz-az)
     def score(v):
      score=0
      for h in hazards:
       if not h['locked']:continue
       impact=max(0,h['warning']-h['age']);end=max(impact,h['warning']+h.get('lingering',0)-h['age'])
       for tm in (impact,(impact+end)/2,end):
        if tm>1.8:continue
        px,pz=predicted(v,tm)
        gap=math.hypot(px-h['position']['x'],pz-h['position']['z'])-h['radius']-.65
        if gap<0:score+=120+(-gap)*35
        elif gap<1:score+=4*(1-gap)
      for e in elements:
       if e['kind']==0:continue
       impact=max(0,e['warning']-e['age']);end=min(2.2,e['warning']+e['duration']-e['age'])
       tm=impact
       while tm<=end+.001:
        px,pz=predicted(v,tm);gap=distance(e,px,pz)-e['radius']-.65
        if gap<0:score+=120-gap*35
        tm+=.15
      px,pz=predicted(v,.5)
      score+=max(0,math.hypot(px,pz)-13)*3
      score+=.3*(1-v[0]*lastdir[0]-v[1]*lastdir[1])
      if target:score+=math.hypot(px-target['x'],pz-target['z'])*.8
      else:score+=abs(math.hypot(px,pz)-7)*.12
      return score
     candidates=[(math.cos(i*math.pi/12),math.sin(i*math.pi/12)) for i in range(24)]+[(0,0)]
     mx,mz=min(candidates,key=score)
     # Skill use based only on visible locked telegraphs. Shield protects a risky pickup;
     # dash is reserved for an imminent strike with no cheap walking exit.
     imminent=any(h['locked'] and h['warning']-h['age']<.9 and math.hypot(x-h['position']['x'],z-h['position']['z'])<h['radius']+.7 for h in hazards)
     pickup_risk=target and math.hypot(x-target['x'],z-target['z'])<4 and imminent
     if a.strategy=='E':
      shield=bool((pickup_risk or sum(h['locked'] and math.hypot(x-h['position']['x'],z-h['position']['z'])<h['radius']+1 for h in hazards)>1) and imminent and snap['shieldCooldown']<=0 and qi>=snap.get('shieldCost',30))
      dash=bool(imminent and not shield and snap['dashCooldown']<=0 and qi>=snap.get('dashCost',18))
    if a.strategy=='E' and snap.get('shieldRemaining',0)>.25:
     # Spend shield to keep a pickup route instead of also dodging its protected hit.
     mx,mz=norm(target['x']-x,target['z']-z) if target else (0,0)
    if a.strategy=='C':dash=int(t*4)%2==0
    if a.strategy=='D':shield=int(t*4)%2==0
    if snap['stage']==4 and a.final_behaviour!='observe':
     planned=(mx,mz,dash,shield)
     mx=mz=0;dash=shield=False
     es=snap.get('elements',[])
     if a.final_behaviour in ('stand','shield'):
      threats=[e for e in es if e['kind'] in (1,2,3)]
      if threats:
       e=threats[0];tx=e['position']['x'];tz=e['position']['z']
       if e['kind']==2:tx=(tx+e['end']['x'])/2;tz=(tz+e['end']['z'])/2
       mx,mz=norm(tx-x,tz-z) if math.hypot(tx-x,tz-z)>.15 else (0,0)
      shield=a.final_behaviour=='shield' and any(e['kind']!=0 and -.3<=e['age']-e['warning']<=.12 for e in es) and int(t*4)%2==0
     if a.final_behaviour=='bloomdash':
      mx,mz,dash,shield=planned
      bloom=next((e for e in es if e['kind']==3),None)
      if bloom:
       mx,mz=norm(bloom['position']['x']-x,bloom['position']['z']-z)
       if bloom['age']>=.65:mx,mz=math.cos(math.pi/6),math.sin(math.pi/6)
       dash=bloom['age']>=.85;shield=False
     if a.final_behaviour=='circle':mx,mz=norm(-z/rad+x/rad*(a.radius-rad)*.7,x/rad+z/rad*(a.radius-rad)*.7) if rad>2 else (1,0)
     if a.final_behaviour=='dash':
      earth=next((e for e in es if e['kind']==2),None)
      if earth:
       mx,mz=norm((earth['position']['x']+earth['end']['x'])/2-x,(earth['position']['z']+earth['end']['z'])/2-z)
       shield=earth['age']>=earth['warning']-.3 and int(t*4)%2==0
      wind=next((e for e in es if e['kind']==0),None)
      if wind:mx=-wind['direction']['x'];mz=-wind['direction']['z'];dash=wind['age']>=wind['warning'] and int(t*4)%2==0
      bloom=next((e for e in es if e['kind']==3),None)
      if bloom:
       # Approach centre, then commit to a safe wedge with ordinary movement + Dash.
       if bloom['age']<bloom['warning']-.45:mx,mz=norm(bloom['position']['x']-x,bloom['position']['z']-z)
       else:mx,mz=math.cos(math.pi/6),math.sin(math.pi/6);dash=bloom['age']>=bloom['warning']-.28 and int(t*10)%2==0
    lastdir=(mx,mz)
  camera_yaw=0
  if a.hazard_camera and snap and snap.get('elements'):
   focus=next((e for e in snap['elements'] if e['kind'] in (1,2,3)),None)
   if focus:
    dx=focus['position']['x']-snap['position']['x'];dz=focus['position']['z']-snap['position']['z']
    camera_yaw=math.degrees(math.atan2(dx,dz))
  angle=math.radians(camera_yaw);local_x=math.cos(angle)*mx-math.sin(angle)*mz;local_z=math.sin(angle)*mx+math.cos(angle)*mz
  result=c.runtime.sessions.apply_input(controller,move_x=local_x,move_y=local_z,run=dash,jump=shield,yaw=camera_yaw,pitch=55,seq=seq)
  assert result.get('ok'),result
  trace.write(json.dumps({'elapsed':snap.get('elapsed') if snap else None,'move':[mx,mz],'cameraYaw':camera_yaw,'dash':dash,'shield':shield})+'\n');trace.flush();seq+=1;time.sleep(.06)
finally:
 c.runtime.sessions.apply_input(controller,seq=seq);c.runtime.sessions.leave(controller_id=controller);trace.close()
if not rows:raise RuntimeError('No run observed')
counts=Counter(q['eventKey'] for q in rows)
result={'technique':snap.get('technique'),'strategy':a.strategy,'finalBehaviour':a.final_behaviour,'label':a.label,'radius':a.radius,'source':str(diag),'status':snap['status'],'elapsed':snap['elapsed'],'hp':snap['hp'],'qi':snap['qi'],'stage':snap['stage'],'profile':snap.get('profile'),'seed':snap.get('seed'),'events':dict(counts),'minQi':min(q['qi'] for q in rows),'lowQiSnapshots':sum(q['eventKey']=='snapshot' and q['qi']<q.get('dashCost',18) for q in rows),'finalWaves':snap['finalWaves']}
(r/'Docs/Evidence'/f'{stem}-result.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
(r/'Docs/Evidence'/f'{stem}-native.jsonl').write_text('\n'.join(json.dumps(row) for row in rows),encoding='utf-8')
print(json.dumps(result),flush=True)
