"""Independent checks of native root tile placement and portal reachability."""
from pathlib import Path
import json,runpy,hashlib
root=Path(__file__).resolve().parent.parent
reference=runpy.run_path(str(root/'Tools/import-smb2j.py'))
report=[];errors=[];total=0
for row in json.loads((root/'Validation/japanese-import.json').read_text()):
    path=root/f'Assets/Resources/LevelData/{row["id"]}.json'
    document=json.loads(path.read_text(encoding='utf-8'))
    raw=reference['extract'](int(row['pointer'],16),int(row['id'][2]))
    expected={(col*16,176-y*16):t for col,values in enumerate(raw['tiles']) for y,t in enumerate(values) if t not in (0,0x20,0x23,0x44,0x68,0x69,0xc3,0xc4,0xc6)}
    actual={(n['x'],n['y']):int(n['spriteOverride'].split('/')[-1],16) for n in document['areas'][0]['nodes'] if n.get('spriteOverride')}
    if actual!=expected:errors.append(row['id']+' root metatile mismatch')
    if hashlib.sha256(path.read_bytes()).hexdigest()!=row['sha256']:errors.append(row['id']+' manifest hash mismatch')
    seen={0};todo=[0]
    while todo:
        loc=todo.pop();a=document['areas'][document['locations'][loc]['area']]
        exits=[n.get('targetLocation',-1) for n in a['nodes']]+[a.get('exit',-1)]
        for target in exits:
            if target>=0 and target not in seen:seen.add(target);todo.append(target)
    reachable={document['locations'][l]['area'] for l in seen}
    if not any(n['type'] in ('FlagPole','CastleAxe') for i in reachable for n in document['areas'][i]['nodes']):errors.append(row['id']+' no connected goal')
    if row['unknown']:errors.append(row['id']+' unsupported enemy stream IDs')
    total+=len(expected)
    report.append(dict(course=row['id'],rootTiles=len(expected),reachableLocations=len(seen),sourcePointer=row['pointer']))
result=dict(passed=not errors,courses=len(report),matchedRootTiles=total,errors=errors,details=report)
(root/'Validation/japanese-reference-check.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(('SMB2J_REFERENCE_PASS' if not errors else 'SMB2J_REFERENCE_FAIL')+f' courses={len(report)} matchedRootTiles={total} errors={errors}')
raise SystemExit(bool(errors))
