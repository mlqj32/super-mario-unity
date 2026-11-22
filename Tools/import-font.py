"""Extract the original 8x8 HUD alphabet without filtering or resampling."""
from pathlib import Path
from PIL import Image
root=Path(__file__).resolve().parent.parent
data=(root/'ThirdParty/SMB1Reference/smb1.chr').read_bytes()
dest=root/'Assets/Resources/Font'
dest.mkdir(parents=True,exist_ok=True)
for i,ch in enumerate('0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ'):
    tile=data[(256+i)*16:(257+i)*16]
    image=Image.new('RGBA',(8,8))
    for y in range(8):
        for x in range(8):
            if ((tile[y]|tile[y+8])>>(7-x))&1:
                image.putpixel((x,y),(255,255,255,255))
    image.save(dest/(str(ord(ch))+'.png'))
patterns={'-':['00000000','00000000','00000000','01111110','00000000','00000000','00000000','00000000'],'.':['00000000']*6+['00110000','00110000'],':':['00000000','00110000','00110000','00000000','00000000','00110000','00110000','00000000'],'/':['00000010','00000110','00001100','00011000','00110000','01100000','01000000','00000000'],'!':['00110000','00110000','00110000','00110000','00110000','00000000','00110000','00000000'],'+':['00000000','00011000','00011000','01111110','00011000','00011000','00000000','00000000']}
patterns['%']=['01100010','01100100','00001000','00010000','00100000','01000110','10000110','00000000']
for ch,rows in patterns.items():
    image=Image.new('RGBA',(8,8))
    for y,row in enumerate(rows):
        for x,v in enumerate(row):
            if v=='1':image.putpixel((x,y),(255,255,255,255))
    image.save(dest/(str(ord(ch))+'.png'))
print('FONT_IMPORT_PASS glyphs='+str(36+len(patterns)))
