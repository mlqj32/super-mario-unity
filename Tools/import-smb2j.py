"""Offline original SMB2J area-parser extraction. No emulation runs in Unity.

Assembler outputs are produced by Tools/assemble-smb2j-reference.py from the pinned
doppelganger/threecreepio disassembly. The resulting editable nodes and tile
images are committed resources; running the game requires neither Python nor ROM.
"""
from pathlib import Path
import re,json,hashlib
from functools import lru_cache
from PIL import Image
from py65.devices.mpu6502 import MPU
ROOT=Path(__file__).resolve().parent.parent
BUILD=ROOT/'_zcode_tmp/smb2j'
symbols={}
for line in (BUILD/'extract.dbg').read_text().splitlines():
    if not line.startswith('sym\t'):continue
    fields=dict(re.findall(r'(\w+)=("[^"]*"|[^,]+)',line.split('\t')[1]))
    if 'val' in fields:
        symbols[int(fields['scope']),fields['name'].strip('"')]=int(fields['val'],0)
S={name:v for (scope,name),v in symbols.items() if scope==1}
binary=(BUILD/'extract.bin').read_bytes()

def machine(world):
    cpu=MPU();cpu.memory[0x6000:0xe000]=binary[:32768]
    if world>=5:cpu.memory[0xc470:0xc470+3631]=binary[32768:32768+3631]
    return cpu

def call(cpu,name,limit=300000):
    cpu.sp=0xff;cpu.stPushWord(0x4fff);cpu.pc=S[name]
    for _ in range(limit):
        cpu.step()
        if cpu.pc==0x5000:return
    raise RuntimeError(f'{name} timed out at {cpu.pc:04x}')

def objects(mem,addr,enemy=False):
    page=0;selected=False;result=[];i=0
    while i<256:
        a=mem[addr+i]
        if a==(0xff if enemy else 0xfd):return result
        b=mem[addr+i+1];row=a&15
        if b&128 and not selected:page+=1;selected=True
        if row==(15 if enemy else 13) and (enemy or not b&64) and not selected:
            page=b&(63 if enemy else 31);selected=True;i+=2;continue
        extra=mem[addr+i+2] if enemy and row==14 else None
        result.append(dict(x=page*256+(a&240),row=row,code=b&127,extra=extra,offset=i))
        i+=3 if extra is not None else 2;selected=False
    raise ValueError('unterminated object stream')

@lru_cache(None)
def extract(pointer,world):
    cpu=machine(world);m=cpu.memory
    m[S['AreaPointer']]=pointer;m[S['WorldNumber']]=world-1
    m[S['AreaObjectLength']:S['AreaObjectLength']+3]=[255]*3
    m[S['Hidden1UpFlag']]=1
    call(cpu,'GetAreaDataAddrs')
    addr=m[S['AreaDataLow']]+256*m[S['AreaDataHigh']]
    enemyaddr=m[S['EnemyDataLow']]+256*m[S['EnemyDataHigh']]
    obj=objects(m,addr);enemies=objects(m,enemyaddr,True)
    cols=((max([o['x'] for o in obj]+[0])//256)+2)*16
    grid=[];spawned=[];winds=[]
    for col in range(cols):
        m[S['ScreenLeft_PageLoc']]=col//16;m[S['ScreenLeft_X_Pos']]=col%16*16
        call(cpu,'AreaParserCore')
        grid.append(m[S['MetatileBuffer']:S['MetatileBuffer']+13])
        winds.append(m[S['WindFlag']])
        for slot in range(6):
            if m[S['Enemy_Flag']+slot]:
                spawned.append(dict(id=m[S['Enemy_ID']+slot],x=m[S['Enemy_PageLoc']+slot]*256+m[S['Enemy_X_Position']+slot],screenY=m[S['Enemy_Y_Position']+slot],col=col))
                m[S['Enemy_Flag']+slot]=0
        call(cpu,'IncrementColumnPos')
    return dict(pointer=pointer,world=world,header=m[addr-2:addr],objects=obj,enemies=enemies,spawned=spawned,tiles=grid,winds=winds)

# Standard 2C02 RGB palette; point-sampled without scaling/filtering.
NES='666666 002a88 1412a7 3b00a4 5c007e 6e0040 6c0600 561d00 333500 0b4800 005200 004f08 00404d 000000 000000 000000 adadad 155fd9 4240ff 7527fe a01acc b71e7b b53120 994e00 6b6d00 388700 0c9300 008f32 007c8d 000000 000000 000000 fffeff 64b0ff 9290ff c676ff f36aff fe6ecc fe8170 ea9e22 bcbe00 88d800 5ce430 45e082 48cdde 4f4f4f 000000 000000 fffeff c0dfff d3d2ff e8c8ff fbc2ff fec4ea feccc5 f7d8a5 e4e594 cfef96 bdf4ab b3f3cc b5ebf2 b8b8b8 000000 000000'.split()
COLORS=[tuple(bytes.fromhex(c))+(255,) for c in NES]
CHR=(ROOT/'ThirdParty/SMB2JReference/SM2CHAR1.chr').read_bytes()
MAIN=machine(1).memory
def bake_tile(tile,style,phase=None):
    folder=ROOT/f'Assets/Resources/JapanTiles/{style}';folder.mkdir(parents=True,exist_ok=True)
    palette_addr=S[['WaterPaletteData','GroundPaletteData','UndergroundPaletteData','CastlePaletteData'][style]]+3+(tile>>6)*4
    palette=MAIN[palette_addr:palette_addr+4]
    if phase is not None:palette[1]=[0x27,0x27,0x27,0x17,0x07,0x17][phase]
    addr=S[f'Palette{tile>>6}_MTiles']+(tile&63)*4
    image=Image.new('RGBA',(16,16))
    for index,num in enumerate(MAIN[addr:addr+4]):
        data=CHR[(256+num)*16:(257+num)*16]
        for y in range(8):
            for x in range(8):
                v=((data[y]>>(7-x))&1)|(((data[y+8]>>(7-x))&1)<<1)
                if v:image.putpixel((index//2*8+x,index%2*8+y),COLORS[palette[v]])
    image.save(folder/(f'{tile:02x}'+('' if phase is None else '-'+str(phase))+'.png'))

def bake_poison(style):
    palette_addr=S[['WaterPaletteData','GroundPaletteData','UndergroundPaletteData','CastlePaletteData'][style]]+3+28
    palette=MAIN[palette_addr:palette_addr+4];image=Image.new('RGBA',(16,16))
    for i,t in enumerate([0xd8,0xda,0xdb,0xff]):
        data=CHR[t*16:(t+1)*16]
        for y in range(8):
            for x in range(8):
                v=((data[y]>>(7-x))&1)|(((data[y+8]>>(7-x))&1)<<1)
                if v:image.putpixel((i%2*8+x,i//2*8+y),COLORS[palette[v]])
    image.save(ROOT/f'Assets/Resources/JapanTiles/{style}/poison.png')

def node(kind,x,y,group='Solid',w=16,h=16,**kw):
    return dict(type=kind,group=group,x=x,y=y,width=w,height=h,entrance=-1,targetLocation=-1,**kw)

def native_area(raw):
    style=(raw['pointer']>>5)&3
    setting=['Underwater','Overworld','Underworld','Castle'][style]
    if style==1 and raw['header'][0]&7>=4:setting+=' Night'
    nodes=[];tiles=raw['tiles'];ports=[];tiletypes=set()
    for col,rows in enumerate(tiles):
        for row,t in enumerate(rows):
            if t==0 or t in (0x20,0x23,0x44,0x68,0x69):continue
            x=col*16;y=176-row*16
            kind='JTile';group='Scenery';kw={}
            if t>=[0x10,0x4f,0x88,0xc0][t>>6]:kind='Stone';group='Solid'
            if t in (0x21,0x22,0x6f):group='Scenery';kind='JTile'
            if 0x4f<=t<=0x5d:
                kind='Brick'
                if t>=0x52:kw['contents']=['Mushroom','PoisonMushroom','Vine','Star','Coin','Mushroom1Up'][(t-0x52)%6]
            if 0x5e<=t<=0x61:
                kind='Block';kw.update(hidden=True,contents=['Coin','Mushroom1Up','PoisonMushroom','Mushroom'][t-0x5e])
            if t in (0xc0,0xc1,0xc2):kind='Block';kw['contents']=['Coin','Mushroom','PoisonMushroom'][t-0xc0]
            if t in (0xc3,0xc4):
                nodes.append(node('Coin',x+3,y-1,'Character',10,14));continue
            if t==0xc6:nodes.append(node('CastleAxe',x,y));continue
            if t==0x65:kind='Cannon'
            if t==0x89:kind='CastleBridge'
            if t in (0x16,0x17,0x18,0x8a,0x8b,0x8c,0x64):kind='TreeTop'
            if t==0x10 and col+1<len(tiles) and tiles[col+1][row]==0x11:
                ports.append(node('PipeEntry',x,y,'Trigger',32,16))
            if t in (0x19,0x6d):ports.append(node('PipeSideEntry',x-4,y,'Trigger',8,32))
            kw['spriteOverride']=f'JapanTiles/{style}/{t:02x}';tiletypes.add(t)
            nodes.append(node(kind,x,y,group,**kw))
    for t in tiletypes|{0xc5}:bake_tile(t,style)
    for t in (0xc0,0xc1,0xc2):
        for phase in range(6):bake_tile(t,style,phase)
    bake_poison(style)
    # Area objects create plants, springs and the goal flag in addition to tiles.
    for obj in raw['objects']:
        row=obj['row'];code=obj['code'];x=obj['x']
        if row<12 and code>>4==7:
            nodes.append(node('Piranha',x+8,176-row*16+24,'Character',16,24,red=raw['world']>=4))
        if row==15 and code>>4 in (6,7):
            start=1 if code>>4==6 else 4;bottom=176-(start+(code&7))*16
            nodes.append(node('Piranha',x+8,bottom,'Character',16,24,red=True,action='upside-down'))
        if row==13 and code==0x41:
            nodes.append(node('FlagPole',x+7,176,'Scenery',1,160))
            nodes.append(node('Flag',x-8,160,'Scenery',16,16))
        if row<12 and code==15:
            nodes.append(node('Springboard',x,176-row*16,'Solid',16,32))
        if row==13 and code in (0x48,0x49,0x4a):
            kind={0x48:'CheepsStart',0x49:'BulletBillsStart',0x4a:'CheepsStop'}[code]
            nodes.append(node(kind,x,176,'Trigger',16,208))
    enemytypes={0:'Koopa',1:'Koopa',2:'Beetle',3:'Koopa',4:'Piranha',5:'HammerBro',6:'Goomba',7:'Blooper',8:'BulletBill',10:'CheepCheep',11:'CheepCheep',12:'Podoboo',13:'Piranha',14:'Koopa',15:'Koopa',16:'Koopa',17:'Lakitu',18:'Spiny',45:'Bowser'}
    unknown=[];balance=[]
    for e in raw['enemies']:
        if e['row'] in (14,15):continue
        if e['code']&64 and not (raw['world']>4 or raw['world']==4 and style==3):continue
        t=e['code']&63;x=e['x'];y=208-e['row']*16-8
        if 0x37<=t<=0x3e:
            v=t-0x37
            for j in range(3 if v&1 else 2):nodes.append(node('Koopa' if v>=4 else 'Goomba',x+j*24,96 if v&2 else 32,'Character',16,24 if v>=4 else 16))
        elif t in enemytypes:
            kind=enemytypes[t];h={'Koopa':24,'Piranha':24,'Bowser':32,'Blooper':24}.get(kind,16)
            kw={}
            if t in (3,15):kw.update(smart=True,red=True)
            if t==14:kw['jumping']=True
            if t==15:kw.update(floating=True,begin=y-64,end=y+64)
            if t==16:kw.update(action='horizontal-flight',begin=x-48,end=x+48)
            if t==11:kw['red']=True
            if t==45:kw['throwing']=raw['world']>=6
            nodes.append(node(kind,x,y,'Character',32 if t==45 else 16,h,**kw))
        elif 0x1b<=t<=0x1f:
            nodes.append(node('CastleBlock',x,208-e['row']*16,'Solid',16,16,fireballs=12 if t==31 else 6,speed=(-1 if t in (29,30) else 1)*(1.4 if t in (28,30) else 1)))
        elif 0x24<=t<=0x2c:
            kw={};w=48
            if t==0x24:kw.update(inScale=True,link='balance-'+str(len(balance)//2));balance.append(x)
            if t==0x25:kw.update(floating=True,begin=y-48,end=y+48)
            if t in (0x26,0x27,0x2b,0x2c):kw.update(movement='movePlatformSpawn',vy=.75 if t in (0x26,0x2b) else -.75);w=24 if t>=0x2b else 48
            if t in (0x28,0x2a):kw.update(sliding=t==0x28,ride=t==0x2a,begin=x-64,end=x+96,speed=.75)
            if t==0x29:kw['falling']=True
            nodes.append(node('Platform',x,208-e['row']*16,'Solid',w,8,**kw))
        elif t in (0x14,0x15,0x17,0x18):
            nodes.append(node({0x14:'CheepsStart',0x15:'BowserFireStart',0x17:'BulletBillsStart',0x18:'CheepsStop'}[t],x,176,'Trigger',16,208))
        elif t not in (0x34,0x35):unknown.append(t)
    for start,value in enumerate(raw['winds']):
        if value and (start==0 or not raw['winds'][start-1]):
            end=start+1
            while end<len(raw['winds']) and raw['winds'][end]:end+=1
            nodes.append(node('Wind',start*16,192,'Trigger',(end-start)*16,256))
    loops=[o for o in raw['objects'] if o['row']==13 and o['code']==0x4b]
    entries=[i for i in range(12) if MAIN[S['LoopCmdWorldNumber']+i]==raw['world']-1]
    for o,i in zip(loops,entries):
        # Retain the original required foot height and multi-part gate count.
        nodes.append(node('LoopGate',o['x'],208,'Trigger',16,256,begin=192-MAIN[S['LoopCmdYPosition']+i],end=max(0,o['x']-1024),section=MAIN[S['MultiLoopCount']+i]))
    return dict(setting=setting,exit=-1,underwater=style==0,width=len(tiles)*16,nodes=nodes,sections=[]),ports,sorted(set(unknown))

WORLDS=[[0x20,0x40,0x21,0x60],[0x22,0x23,0x24,0x61],[0x25,0,0x26,0x62],[0x27,0x28,0x2a,0x63],[0x2b,0x43,0x2c,0x64],[0x2d,1,0x2e,0x65],[0x2f,0x30,0x31,0x66],[0x32,0x35,0x36,0x67]]

def convert_course(world,stage,pointer):
    raws={};areas=[];area_ids={};allports={};unknown={}
    def add_area(p):
        if p in area_ids:return area_ids[p]
        r=extract(p,world);raws[p]=r
        a,ports,unhandled=native_area(r);area_ids[p]=len(areas);areas.append(a);allports[p]=ports
        if unhandled:unknown[hex(p)]=unhandled
        for e in r['enemies']:
            if e['row']==14 and e['extra']>>5==world-1:add_area(e['code']&0x7f)
        return area_ids[p]
    add_area(pointer)
    locations=[];loc_ids={}
    def location(p,page=0,initial=False):
        key=(p,page)
        if key in loc_ids:return loc_ids[key]
        index=len(locations);loc_ids[key]=index
        raw=raws[p];entry=(raw['header'][0]>>3)&7
        y=208-[0,32,176,80,0,0,176,176][entry]-16
        x=page*256+40
        if not initial and page:
            # Return pipes are often decorative (12/13), not enterable (10/11).
            # Using only enterable caps can spawn Mario inside the return shaft.
            caps=[(c*16,176-row*16) for c,rows in enumerate(raw['tiles']) if page*16<=c<(page+1)*16 for row,t in enumerate(rows) if t in (0x10,0x12)]
            if caps:x=caps[0][0]+10;y=caps[0][1]
        if initial and (p>>5)&3==1:y=0
        locations.append(dict(area=area_ids[p],entry='Positioned',x=x,y=y,positioned=True))
        return index
    location(pointer,initial=True)
    for p,r in raws.items():
        routes=[e for e in r['enemies'] if e['row']==14 and e['extra']>>5==world-1]
        a=areas[area_ids[p]]
        warpobj=next((o for o in r['objects'] if o['row']==13 and o['code']==0x45),None)
        for port in allports[p]:
            routes_here=[e for e in routes if e['x']<=port['x']+48]
            if warpobj and port['x']>=warpobj['x']-256:
                dest=3 if world==1 and p>>5==1 else 2 if world==1 and p==0x40 else 4 if world==1 else 1 if world==3 else 6 if world==5 and p==0x2b else 8 if world==5 and p>>5==1 else 7 if world==5 else 5
                port['targetMap']=f'J-{dest}-1'
            elif routes_here:
                route=routes_here[-1];port['targetLocation']=location(route['code']&127,route['extra']&31)
            else:continue
            a['nodes'].append(port)
        for n in a['nodes']:
            if n.get('contents')=='Vine':
                candidates=[e for e in routes if e['x']<=n['x']+48]
                if candidates:
                    route=candidates[-1];n['targetLocation']=location(route['code']&127,route['extra']&31)
        if r['header'][1]>>6==3 and routes:
            a['exit']=location(routes[-1]['code']&127,routes[-1]['extra']&31)
    limit={1:400,2:300,3:200}.get(raws[pointer]['header'][0]>>6,400)
    return dict(id=f'J-{world}-{stage}',timeLimit=limit,locations=locations,areas=areas),unknown

if __name__=='__main__':
    report=[]
    for world,pointers in enumerate(WORLDS,1):
        for stage,pointer in enumerate(pointers,1):
            level,unknown=convert_course(world,stage,pointer)
            target=ROOT/f'Assets/Resources/LevelData/{level["id"]}.json';target.write_text(json.dumps(level,ensure_ascii=False,separators=(',',':')),encoding='utf-8')
            report.append(dict(id=level['id'],pointer=hex(pointer),areas=len(level['areas']),nodes=sum(len(a['nodes']) for a in level['areas']),unknown=unknown,sha256=hashlib.sha256(target.read_bytes()).hexdigest()))
            print(report[-1],flush=True)
    (ROOT/'Validation/japanese-import.json').write_text(json.dumps(report,indent=2))
    print('SMB2J_NATIVE_IMPORT courses='+str(len(report)))
