"""Validate Unity YAML documents, local object references and external asset GUIDs."""
import json,re
from pathlib import Path
import yaml

root=Path(__file__).resolve().parent.parent
guids={}
errors=[]
for meta in (root/'Assets').rglob('*.meta'):
    match=re.search(r'^guid: ([0-9a-f]{32})$',meta.read_text(encoding='utf-8-sig'),re.M)
    if match:
        guid=match.group(1)
        if guid in guids:errors.append(f'Duplicate GUID: {meta}')
        guids[guid]=meta
files=blocks=local_refs=external_refs=0
def visit(value,ids,path):
    global local_refs,external_refs
    if isinstance(value,dict):
        if 'fileID' in value and value['fileID']:
            if value.get('guid'):
                guid=value['guid']
                if guid not in guids and not guid.startswith('0000000000000000'):
                    errors.append(f'{path}: missing GUID {guid}')
                external_refs+=1
            elif value['fileID'] not in ids:
                errors.append(f'{path}: missing local fileID {value["fileID"]}')
            else:local_refs+=1
        for item in value.values():visit(item,ids,path)
    elif isinstance(value,list):
        for item in value:visit(item,ids,path)
for path in (root/'Assets').rglob('*'):
    if path.suffix not in ('.unity','.prefab','.asset'):continue
    text=path.read_text(encoding='utf-8-sig')
    ids=re.findall(r'^--- !u!\d+ &(-?\d+)',text,re.M)
    if len(ids)!=len(set(ids)):errors.append(f'{path}: duplicate file IDs')
    clean=re.sub(r'^%.*\n','',text,flags=re.M)
    clean=re.sub(r'^--- !u!\d+ &-?\d+.*$', '---',clean,flags=re.M)
    documents=list(yaml.safe_load_all(clean))
    if len(documents)!=len(ids):errors.append(f'{path}: document count mismatch')
    for doc in documents:visit(doc,set(map(int,ids)),str(path.relative_to(root)))
    files+=1;blocks+=len(documents)
result=dict(pass_=not errors,files=files,blocks=blocks,assetGuids=len(guids),localReferences=local_refs,externalReferences=external_refs,errors=errors)
(root/'Validation/structure.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print(('UNITY_YAML_PASS' if not errors else 'UNITY_YAML_FAIL')+' '+json.dumps(result,ensure_ascii=False))
raise SystemExit(bool(errors))
