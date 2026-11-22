"""Build only the offline parser reference. Requires ca65/ld65 and no FDS BIOS.

Usage: python Tools/assemble-smb2j-reference.py --cc65 _zcode_tmp/cc65/bin
Then:  python Tools/import-smb2j.py
"""
from pathlib import Path
import argparse,subprocess
parser=argparse.ArgumentParser();parser.add_argument('--cc65',required=True,type=Path)
args=parser.parse_args();root=Path(__file__).resolve().parent.parent
tools=args.cc65.resolve();dest=root/'_zcode_tmp/smb2j';dest.mkdir(parents=True,exist_ok=True)
source=''
for name,origin in zip(['main','data2','data3','data4'],[0x6000,0xc470,0xc5d0,0xc2b4]):
    (dest/f'sm2{name}.asm').write_bytes((root/f'ThirdParty/SMB2JReference/sm2{name}.asm').read_bytes())
    source+=f'.scope FILE_SM2{name.upper()}\n.org {origin}\n.include "sm2{name}.asm"\n.reloc\n.endscope\n'
(dest/'extract.asm').write_text(source)
(dest/'layout').write_text('MEMORY { CODE: start=$0, size=$FFDC, fill=yes, fillval=$00; }\nSEGMENTS { CODE: load=CODE, type=ro; }\n')
subprocess.run([str(tools/'ca65.exe'),'-g','extract.asm','-o','extract.o'],cwd=dest,check=True)
subprocess.run([str(tools/'ld65.exe'),'--dbgfile','extract.dbg','-C','layout','extract.o','-o','extract.bin'],cwd=dest,check=True)
print('SMB2J_ASSEMBLE_PASS (offline parser only)')
