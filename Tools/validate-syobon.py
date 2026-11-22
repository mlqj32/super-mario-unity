"""Compare the initially imported Cat Mario data with the pinned source.

This is an import/reference audit, not a restriction on later user map edits.
"""
from pathlib import Path
import re,json
from PIL import Image

root=Path(__file__).resolve().parent.parent
reference=root/'ThirdParty/SyobonActionMono/SyobonAction.Game'
text=(reference/'Game1.cs').read_text(encoding='utf-8-sig')
maps={};stage=area=0
pattern=r'if \(this.sta == 1 && this.stb == (\d+) && this.stc == (\d+)\)|stagedatex\[(\d+)\] = new byte\[\]\s*\{([^}]+)\};'
for m in re.finditer(pattern,text):
    if m.group(1):stage,area=map(int,m.group(1,2));continue
    maps.setdefault((stage,area),{})[int(m.group(3))]=[int(x) for x in re.findall(r'\d+',m.group(4))]
errors=[];cells=0
for (stage,area),rows in maps.items():
    lines=(root/f'Assets/Resources/Syobon/Stages/{stage}-{area}.csv').read_text().splitlines()
    for y in range(17):
        expected=rows.get(y,[]);cells+=len(expected)
        actual=[int(x) for x in lines[y].split(',')] if y<len(lines) and lines[y] else []
        if expected!=actual:errors.append(f'map {stage}-{area} row {y} differs from reference')
images=0
for original in (reference/'Content/res').glob('*.PNG'):
    source=Image.open(original).convert('RGBA');actual=Image.open(root/'Assets/Resources/Syobon/res'/original.name).convert('RGBA')
    expected=[(r,g,b,0 if (r,g,b)==(153,255,255) else a) for r,g,b,a in source.get_flattened_data()]
    if source.size!=actual.size or list(actual.get_flattened_data())!=expected:errors.append('atlas mismatch '+original.name)
    images+=1
result=dict(passed=not errors,stages=4,areas=len(maps),cells=cells,atlases=images,errors=errors)
(root/'Validation/syobon-reference.json').write_text(json.dumps(result,indent=2),encoding='utf-8')
print(('SYOBON_REFERENCE_PASS ' if not errors else 'SYOBON_REFERENCE_FAIL ')+json.dumps(result))
raise SystemExit(bool(errors))
