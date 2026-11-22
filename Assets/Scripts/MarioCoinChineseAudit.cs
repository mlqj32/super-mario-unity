using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        public void RunCoinChineseAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Coin/CN/Japan: "+n,p,d);
            AuditArena();var block=Spawn(new NodeData{type="Block",group="Solid",x=160,y=64,contents="Coin"});
            player.X=162;player.Y=31.5f;player.vy256=768;player.grounded=false;Step(default);
            var effect=actors.FirstOrDefault(a=>a.data.type=="CoinPop");
            Check("real head collision creates visual coin",effect!=null&&block.used&&coins==1&&score==200);
            if(effect!=null)
            {
                float firstY=effect.y;player.X=40;player.Y=0;player.vy256=0;
                var cells=new HashSet<string>();
                for(int f=0;f<24;f++){Step(default);RefreshVisuals();cells.Add(effect.visual.CurrentSignature);}
                Check("coin rises and cycles four sprites",effect.y>firstY&&cells.Count==4,"cells="+cells.Count);
                Check("coin effect never awards twice or hurts",score==200&&coins==1&&phase==GamePhase.Playing);
                for(int f=0;f<12;f++)Step(default);
                Check("coin becomes score popup",effect.data.type=="ScorePop"&&effect.data.text=="200"&&score==200);
                Bump(block);Check("used block cannot create second effect",coins==1&&actors.Count(a=>a.alive&&a.data.type=="CoinPop")==0);
                for(int f=0;f<30;f++)Step(default);Check("score effect expires",!effect.alive&&score==200);
            }
            AuditArena();var brick=Spawn(new NodeData{type="Brick",group="Solid",x=160,y=64,contents="Coin"});
            Bump(brick);for(int f=0;f<20;f++)Step(default);Bump(brick);
            Check("multi-coin brick creates separate effects",coins==2&&actors.Count(a=>a.alive&&a.data.type=="CoinPop")==2);
            coins=99;int life=lives;Bump(brick);Check("100 coins grant exactly one life",coins==0&&lives==life+1);
            var glyphs=Resources.Load<TextAsset>("ChineseGlyphs");
            Check("Chinese coverage manifest exists",glyphs!=null);
            if(glyphs!=null)foreach(char c in glyphs.text.Trim())Check("Chinese glyph "+c,PixelLibrary.Glyph(c)!=null);
            Check("HUD coin texture",PixelLibrary.Sprite(PixelLibrary.Find("Character","Coin","Overworld normal").parts[0]).texture!=null);
            Check("separate 32-course campaigns",Levels.Count(l=>l.Japanese)==32&&Levels.Count(l=>!l.Japanese)==32);
            foreach(var l in Levels.Where(l=>l.Japanese))
            {
                Check(l.id+" original tile data",l.areas[0].nodes.Count(n=>!string.IsNullOrEmpty(n.spriteOverride))>100);
                Check(l.id+" distinct from first game",l.areas[0].nodes.Length!=Levels.First(a=>a.id==l.Course).areas[0].nodes.Length);
            }
            AuditArena();power=2;var poison=AuditActor("PoisonMushroom",40);Step(default);
            Check("poison damages without powerup points",!poison.alive&&power==1&&score==0);
            AuditArena();var pipe=Spawn(new NodeData{type="PipeEntry",group="Trigger",x=32,y=0,width=32,height=16,targetLocation=1});
            player.X=42;player.Y=0;Step(new Controls{down=true});Check("native Japanese pipe enters",inTransport);
            for(int i=0;i<40;i++)Step(default);Check("native Japanese pipe arrives",location==1);
            NewGame("1-1");
        }
        IEnumerator CoinChineseSmoke(string output)
        {
            int before=smokeErrors,checks=0;
            RunCoinChineseAudit((n,p,d)=>{checks++;SmokeCheck(p,n+" "+d);});
            NewGame("1-1");foreach(var enemy in actors.Where(a=>a.IsEnemy))enemy.alive=false;
            var block=actors.First(a=>a.data.type=="Block"&&(string.IsNullOrEmpty(a.data.contents)||a.data.contents=="Coin"));
            player.X=block.x+2;player.Y=block.y-PlayerHeight-.5f;player.vy256=768;player.grounded=false;cameraX=Mathf.Max(0,player.X-112);
            Step(default);var coin=actors.FirstOrDefault(a=>a.data.type=="CoinPop");SmokeCheck(coin!=null,"coin pop screenshot fixture");
            for(int f=0;f<66;f++)
            {
                if(f<8&&f%2==0||f%8==0||f==65){RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"coin-pop-"+f.ToString("D2"));}
                Step(default);
            }
            foreach(string id in new[]{"J-1-1","J-1-2","J-3-2","J-5-1","J-8-4"})
            {NewGame(id);for(int f=0;f<20;f++)Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,id);}
            phase=GamePhase.Paused;menuLevels=true;menuCampaign=1;yield return null;yield return new WaitForEndOfFrame();SaveScreen(output,"japan-course-select");
            string result="MARIO_COIN_CHINESE_JAPAN_"+(smokeErrors==before?"PASS":"FAIL")+" checks="+checks+" errors="+(smokeErrors-before);
            File.WriteAllText(Path.Combine(output,"coin-chinese-japan-result.txt"),result);Debug.Log(result);
        }
    }
}
