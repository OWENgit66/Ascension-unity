"""Audit real standalone native runs and unchanged game sources. Standard library only.
Run from any directory: python audit_independence.py --original <original Ascension folder>.
The original folder is needed only for migration comparison, never for Unity or the game.
"""
from pathlib import Path
import argparse, collections, hashlib, json

r = Path(__file__).resolve().parents[2]
e = r / 'Docs/Evidence'
p = argparse.ArgumentParser()
p.add_argument('--original', type=Path, required=True)
a = p.parse_args()
sha = lambda f: hashlib.sha256(f.read_bytes()).hexdigest()
baseline = json.loads((e/'original-baseline.json').read_text(encoding='utf8'))
original_changed = [s for s, h in baseline.items() if not (a.original/s).is_file() or sha(a.original/s) != h]
manifest = json.loads((e/'dependency-copy-manifest.json').read_text(encoding='utf8'))
allowed = {'Packages/manifest.json', 'Packages/packages-lock.json'}
copied_changed = [s for s in manifest['copied'] if s not in allowed and
                  (not (r/'UnityProject'/s).is_file() or sha(r/'UnityProject'/s) != sha(a.original/'UnityProject'/s))]
new_files = [f.relative_to(r/'UnityProject').as_posix() for folder in ['Assets','ProjectSettings']
             for f in (r/'UnityProject'/folder).rglob('*') if f.is_file()
             and f.relative_to(r/'UnityProject').as_posix() not in manifest['copied']]
unexpected = [s for s in new_files if s not in ['Assets/Ascension/Editor/StandaloneBuild.cs',
                                             'Assets/Ascension/Editor/StandaloneBuild.cs.meta',
                                             'ProjectSettings/PackageManagerSettings.asset',
                                             'ProjectSettings/URPProjectSettings.asset']]
packages = json.loads((r/'UnityProject/Packages/packages-lock.json').read_text(encoding='utf8'))['dependencies']
paths = sorted(e.glob('t065-independence-final-*-result.json'), key=lambda f:f.stat().st_mtime)
if not paths: raise SystemExit('Final native run is not complete')
result = paths[-1]
run = json.loads(result.read_text(encoding='utf8'))
rows = [json.loads(s) for s in result.with_name(result.name.replace('-result.json','-native.jsonl')).read_text(encoding='utf8').splitlines()]
allrows = [json.loads(s) for s in Path(run['source']).read_text(encoding='utf8').splitlines()]
events = collections.Counter(q['eventKey'] for q in rows)
cfg = json.loads((r/'UnityProject/Assets/Ascension/Mechanic/Resources/Balance.json').read_text(encoding='utf8'))
t = next(t for t in cfg['techniques'] if t['id']==run['technique'])
economy=[]
for prev, q in zip(rows, rows[1:]):
    key=q['eventKey']
    if key in ('dash','shield'): economy.append(abs(prev['qi']-q['qi']-t[key+'Cost'])<.001)
    if key=='qi_collected': economy.append(abs(q['qi']-min(cfg['maxQi'],prev['qi']+t['qiValue']))<.001)
    if key=='shield': economy.append(q['shieldAmount']==t['shieldCapacity'] and abs(q['shieldRemaining']-t['shieldDuration'])<.001)
starts=[q for q in allrows if q['eventKey']=='started']
pauses=[]; paused=None
for q in allrows:
    if q['eventKey']=='paused': paused=q
    if q['eventKey']=='resumed' and paused:
        pauses.append(all(q[k]==paused[k] for k in ['elapsed','hp','qi','position','strikes','elements','dashCooldown','shieldCooldown','shieldRemaining']))
        paused=None
log=(e/'player-final.log').read_text(encoding='utf8',errors='replace')
build=json.loads((e/'build.json').read_text(encoding='utf8'))
checks={
    'originalSourceAndReleaseUntouched':not original_changed,
    'copiedGameplayArtAudioSceneSettingsByteIdentical':not copied_changed,
    'onlyNewEditorBuildEntrypointAndUnityEditorMetadata':not unexpected,
    'buildSucceeded':build['success'] and build['errors']==0,
    'formal360Win':run['profile']=='normal' and run['status']=='Won' and 360<=run['elapsed']<360.1,
    'movement':len({(q['position']['x'],q['position']['z']) for q in rows})>100,
    'qiDashShield':all(events[k]>0 for k in ['qi_collected','dash','shield','shield_absorbed']),
    'allHazards':all(events[k]>0 for k in ['warning_normal','warning_chain','warning_tracking','warning_predictive','warning_blocking','active_wind','active_fire','active_earth','active_bloomcenter','active_bloomray']),
    'stagesAndFinal':events['stage_changed']==3 and run['finalWaves']==9,
    'economy':bool(economy) and all(economy),
    'lose':any(q['eventKey']=='lost' and q['hp']==0 for q in allrows),
    'restart':len(starts)>=2 and all(q['hp']==100 and q['qi']==60 and q['elapsed']==0 and not q['strikes'] and not q['elements'] for q in starts),
    'pause':bool(pauses) and all(pauses),
    'noRuntimeError':not any(s in log for s in ['Exception:','Shader error','Missing audio:','Failed to bind UDP']),
    'removedUnusedPackages':not any(s in packages for s in ['com.unity.cloud.gltfast','com.unity.collections','com.unity.test-framework','com.unity.ext.nunit']),
    'burstMatchesOriginal':packages['com.unity.burst']['version']=='1.8.24',
    'noLocalPackageLinks':not any(str(v['version']).startswith(('file:','../','..\\')) for v in packages.values()),
}
report={'scope':'Actual independent Release, native normal input and Computer Use. Not human blind testing.',
        'checks':checks,'allPassed':all(checks.values()),'run':run,'economyChecks':len(economy),
        'originalFilesChecked':len(baseline),'originalChanged':original_changed,'copiedChanged':copied_changed,
        'newFiles':new_files,'pauseChecks':pauses,'build':build}
(e/'independence-acceptance.json').write_text(json.dumps(report,ensure_ascii=False,indent=2),encoding='utf8')
files=[f for folder in ['UnityProject/Assets','UnityProject/Packages','UnityProject/ProjectSettings','Builds/Windows']
       for f in (r/folder).rglob('*') if f.is_file() and not any('DoNotShip' in part for part in f.parts)]
(e/'independent-final-fingerprint.json').write_text(json.dumps({f.relative_to(r).as_posix():sha(f) for f in files},indent=2),encoding='utf8')
print(json.dumps({k:report[k] for k in ['allPassed','checks','economyChecks','copiedChanged','originalChanged','newFiles']},ensure_ascii=True,indent=2))
if not report['allPassed']: raise SystemExit(1)
