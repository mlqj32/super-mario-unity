"""Rebuild the original rising-coin sprites (CHR $60-$63, lower half flipped)."""
from pathlib import Path
import json
from PIL import Image
root=Path(__file__).resolve().parent.parent
chrdata=(root/'ThirdParty/SMB1Reference/smb1.chr').read_bytes()
palette=[(0,0,0,0),(216,40,0,255),(255,255,255,255),(252,152,56,255)]
for frame in range(4):
    tile=chrdata[(0x60+frame)*16:(0x61+frame)*16]
    image=Image.new('RGBA',(8,16))
    for y in range(8):
        for x in range(8):
            value=((tile[y]>>(7-x))&1)|(((tile[y+8]>>(7-x))&1)<<1)
            image.putpixel((x,y),palette[value]);image.putpixel((x,15-y),palette[value])
    image.save(root/f'Assets/Resources/Pixels/{40+frame:04}.png')
path=root/'Assets/Resources/SpriteCatalog.json'
catalog=json.loads(path.read_text(encoding='utf-8'))
for entry in catalog['entries']:
    if entry['key'].startswith('Character Coin anim '):
        entry['parts'][0]['width']=8;entry['parts'][0]['height']=16
path.write_text(json.dumps(catalog,separators=(',',':')),encoding='utf-8')
print('MARIO_COIN_SPRITES_PASS frames=4 size=8x16 source=SMB1_CHR_60_63')
