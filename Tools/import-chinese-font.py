"""Verify Fusion Pixel cmap, then bake crisp 12px UI glyphs. Existing NES ASCII stays 8px."""
from pathlib import Path
import json
from PIL import Image,ImageDraw,ImageFont
from fontTools.ttLib import TTFont
root=Path(__file__).resolve().parent.parent
font_path=root/'ThirdParty/FusionPixel/fusion-pixel-12px-monospaced-zh_hans.otf'
text=''.join(p.read_text(encoding='utf-8-sig') for p in (root/'Assets/Scripts').rglob('*.cs'))
chars=sorted(set(c for c in text if '\u4e00'<=c<='\u9fff'))
cmap=TTFont(font_path).getBestCmap()
missing=[c for c in chars if ord(c) not in cmap]
assert not missing,missing
font=ImageFont.truetype(str(font_path),12)
for c in chars:
    mask=Image.new('L',(12,12));d=ImageDraw.Draw(mask)
    d.text((0,0),c,font=font,fill=255,anchor='lt')
    mask=mask.point(lambda x:255 if x>=128 else 0)
    assert mask.getbbox(),c
    image=Image.new('RGBA',(12,12),'white');image.putalpha(mask)
    image.save(root/f'Assets/Resources/Font/{ord(c)}.png')
result=dict(font='Fusion Pixel 12px zh_hans',version='2026.10.16',originalChineseGlyphs=0,checked=len(chars),missing=missing,characters=''.join(chars))
(root/'Assets/Resources/ChineseGlyphs.txt').write_text(''.join(chars),encoding='utf-8')
(root/'Validation/chinese-font.json').write_text(json.dumps(result,ensure_ascii=False,indent=2),encoding='utf-8')
print('MARIO_CHINESE_FONT_PASS '+json.dumps(result,ensure_ascii=False))
