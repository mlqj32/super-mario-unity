using System;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Mario.Syobon
{
    public partial class Engine
    {
        public void RunAudit(Action<string,bool,string> record,string output)
        {
            void Check(string n,bool p,string d="")=>record("Syobon: "+n,p,d);
            resources.AudioEnabled=false;Directory.CreateDirectory(output);
            resources.ValidateAssets(record);
            foreach(var key in new[]{(1,0),(2,0),(2,1),(2,2),(3,0),(3,1),(3,5),(4,0)})
            {
                Start(key.Item1);stc=key.Item2;zxon=0;Step(default);
                for(int i=0;i<3;i++)Step(default);
                Check(key+" loads map and scripted actors",ta.Count(x=>x>-9000000)>20&&spriteBatch.DrawCount>10);
                Render();Save(Path.Combine(output,"area-"+key.Item1+"-"+key.Item2+".png"));
                int screens=Math.Max(1,scrollx/40000+1),visited=0;
                for(int screen=0;screen<screens;screen++)
                {
                    Start(key.Item1);stc=key.Item2;zxon=0;Step(default);
                    screenX=fzx=screen*40000;ma=18000;mb=20000;md=0;
                    for(int i=0;i<45;i++)Step(new KeyState{horizontal=1,jump=i%20<8,accept=true});
                    Render();visited++;
                }
                Check(key+" every screen simulates and renders",visited==screens,"screens="+screens);
                Start(key.Item1);stc=key.Item2;zxon=0;Step(default);
                int deaths=0;bool stable=true;
                for(int i=0;i<1800;i++)
                {
                    if(Dead){Retry(true);deaths++;}
                    Step(new KeyState{horizontal=i%180<150?1:-1,jump=i%49<18,down=i%113==0,accept=i%13==0});
                    stable&=Math.Abs((long)ma)<100000000&&Math.Abs((long)mb)<100000000;
                    if(Won)break;
                }
                Check(key+" mixed input stress",stable,"deaths="+deaths+" ticks="+ticks);
            }
            Start(1);for(int i=0;i<5;i++)Step(default);
            Check("player survives idle spawn",!Dead&&mhp==1);
            float groundY=Y;
            for(int i=0;i<8;i++)Step(new KeyState{jump=true});
            Check("jump raises player",Y<groundY-20,"ground="+groundY+" y="+Y);
            Start(1);for(int i=0;i<12;i++)Step(new KeyState{horizontal=1});
            Check("right input moves player",X>80,"x="+X);
            Start(1);float firstSpeed=0,lastSpeed=0;
            for(int i=0;i<12;i++){Step(new KeyState{horizontal=1});if(i==0)firstSpeed=Math.Abs(HorizontalVelocity);lastSpeed=Math.Abs(HorizontalVelocity);}
            Check("direction input uses original automatic acceleration",UsesAutomaticAcceleration&&lastSpeed>firstSpeed&&lastSpeed>0,"first="+firstSpeed+" last="+lastSpeed);
            var sheet=UnityEngine.Resources.Load<Texture2D>("Syobon/res/player");
            Check("colour key becomes alpha",sheet.GetPixels32().Any(p=>p.a==0)&&!sheet.GetPixels32().Any(p=>p.a>0&&p.r==153&&p.g==255&&p.b==255));
            Check("pixel texture retains native dimensions",sheet.width==169&&sheet.height==44,"size="+sheet.width+"x"+sheet.height);
            Start(1);int block=Array.FindIndex(ttype,x=>x==7);
            Check("hidden blocks present",block>=0);
            if(block>=0)
            {
                screenX=ta[block]-18000;ma=18000;mb=tb[block]+3200;md=-1000;mzimen=0;
                for(int i=0;i<8;i++)Step(default);
                Check("jump reveals hidden block",ttype[block]!=7,"type="+ttype[block]);
            }
            Start(1);int checkpoint=Array.FindIndex(stype,x=>x==500);
            Check("checkpoint exists",checkpoint>=0);
            if(checkpoint>=0)
            {
                int destination=enemyX[checkpoint];screenX=destination-18000;fzx=screenX;ma=18000;mb=enemyY[checkpoint];md=0;
                Step(default);Check("checkpoint activates",tyuukan>0);
                mhp=0;Step(default);Check("damage enters death",Dead);
                int remaining=nokori;Retry(true);
                Check("quick retry keeps checkpoint and spends one life",!Dead&&tyuukan>0&&nokori==remaining-1&&Math.Abs(X-destination/100f)<5,"x="+X+" target="+destination/100f);
                Retry(false);Check("confirmed restart returns to course start",tyuukan==0&&X<150&&!Dead);
            }
            Start(1,0);mhp=0;Step(default);Retry(true);
            Check("negative lives remain playable",nokori==-1&&!Dead);
            for(int stage=1;stage<=3;stage++)
            {
                Start(stage);mtype=300;mtm=249;
                Step(default);for(int i=0;i<32;i++)Step(default);
                Check(stage+" clear advances exactly once",stb==stage+1&&main==1,"stage="+stb);
            }
            Start(4);mtype=301;mtm=439;Step(default);
            Check("final ending reachable",Won,"mode="+mtype+" timer="+mtm);
            Start(2);stc=1;zxon=0;Step(default);mhp=0;Step(default);Retry(true);
            Check("death retry stays in underground area",stb==2&&stc==1&&!Dead);
            Start(1);mhp=0;Step(default);
            for(int i=0;i<105;i++)Step(default);
            Check("automatic death transition spends only one life",nokori==9&&!Dead,"lives="+nokori+" mode="+main);
            Start(1);stagedate[0][900]=123;stc=0;zxon=0;Step(default);
            Check("reload clears cells outside new map",stagedate[0][900]==0);
            AuditDialogs(record,output);
            Start(1);
        }
        // Hit the real stage-two sign from below, then let its opening animation run.
        public bool OpenStageTwoHintForAudit()
        {
            Start(2);
            int sign=Array.FindIndex(ttype,x=>x==300);
            if(sign<0)return false;
            screenX=fzx=0;ma=ta[sign];mb=tb[sign]+3200;md=-1000;mzimen=0;
            for(int i=0;i<40&&!Waiting;i++)Step(default);
            return Waiting&&tmsg==1;
        }
        Color32[] DialogPixels()
        {
            Render();var previous=RenderTexture.active;RenderTexture.active=Target;
            var image=new Texture2D(480,420,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,480,420),0,0);image.Apply();return image.GetPixels32();}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
        }
        void AuditDialogs(Action<string,bool,string> record,string output)
        {
            void Check(string n,bool p,string d="")=>record("Syobon dialog: "+n,p,d);
            Check("real stage-two sign opens and waits",OpenStageTwoHintForAudit());
            DialogPixels();Save(Path.Combine(output,"dialog-stage-two-sign.png"));
            var cases=new[]{(0,new[]{0}),(1,new[]{0,1,2,4,6}),(2,new[]{3,6}),
                (3,new[]{3,6}),(4,new[]{2,4}),(5,new[]{0,1,3,6}),(80,new[]{1,2,5}),(100,new[]{0,2,3,5,6})};
            int message=67;
            foreach(var hint in cases)
            {
                Start(2);tmsg=hint.Item1;tmsgtype=1;tmsgtm=15;tmsgy=300;
                for(int i=0;i<40&&!Waiting;i++)Step(default);
                Check(hint.Item1+" opening reaches confirmation",Waiting);
                var pixels=DialogPixels();Save(Path.Combine(output,"dialog-"+hint.Item1+".png"));
                foreach(int row in hint.Item2)
                {
                    int white=0;
                    for(int y=46+row*24;y<58+row*24;y++)for(int x=66;x<414;x++)
                    {var p=pixels[(419-y)*480+x];if(p.r>220&&p.g>220&&p.b>220)white++;}
                    Check("message "+message+" has visible text",white>20,"whitePixels="+white);
                    string text=SyobonText.Translate("Message "+message++);
                    Check("localized row fits and has Chinese glyphs",text.Sum(c=>c>127?12:8)<=348&&
                        text.Where(c=>c>127).All(c=>UnityEngine.Resources.Load<Texture2D>("Font/"+(int)c)!=null),text);
                }
                int xBefore=ma,yBefore=mb;
                for(int i=0;i<60;i++)Step(new KeyState{horizontal=1,jump=true});
                Check(hint.Item1+" waiting preserves body and freezes gameplay",Waiting&&ma==xBefore&&mb==yBefore&&pixels.SequenceEqual(DialogPixels()));
                Step(new KeyState{accept=true});for(int i=0;i<18;i++)Step(default);
                Check(hint.Item1+" confirmation closes without deadlock",!Waiting&&tmsgtype==0);
            }
            OpenStageTwoHintForAudit();Retry(false);
            Check("retry clears active dialog",!Waiting&&tmsgtype==0&&!Dead);
        }
        public void Save(string path)
        {
            var previous=RenderTexture.active;RenderTexture.active=Target;
            var image=new Texture2D(480,420,TextureFormat.RGB24,false);
            try{image.ReadPixels(new Rect(0,0,480,420),0,0);image.Apply();File.WriteAllBytes(path,image.EncodeToPNG());}
            finally{RenderTexture.active=previous;UnityEngine.Object.DestroyImmediate(image);}
        }
    }
}
