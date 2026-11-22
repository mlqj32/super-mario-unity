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
        Actor EntryAuditDoor(Actor pole)=>actors.Where(a=>a.alive&&a.x>pole.x&&a.y<=pole.y&&
            (a.data.type=="CastleDoor"||(a.data.spriteOverride??"").EndsWith("/4b")))
            .OrderBy(a=>a.x).FirstOrDefault();

        void EntryAuditStart(Actor pole,int size)
        {
            power=size;timeLeft=202;invulnerable=0;player.X=pole.x-8;player.Y=pole.y+64;
            player.grounded=false;player.vx4096=player.vy256=0;player.facing=-1;
            cameraX=Mathf.Max(0,pole.x-120);Step(default);
        }
        public void RunCastleEntryAudit(Action<string,bool,string> record)
        {
            void Check(string n,bool p,string d="")=>record("Castle entry: "+n,p,d);
            foreach(var course in Levels)
            for(int ai=0;ai<course.areas.Length;ai++)
            {
                if(!course.areas[ai].nodes.Any(n=>n.type=="FlagPole"))continue;
                int loc=Array.FindIndex(course.locations,l=>l.area==ai);if(loc<0)continue;
                foreach(int size in new[]{1,2,3})
                {
                    NewGame(course.id);LoadLocation(loc);
                    var pole=actors.First(a=>a.data.type=="FlagPole");var door=EntryAuditDoor(pole);
                    string label=course.id+" area="+ai+" power="+size;
                    Check(label+" usable ground-level castle door",door!=null);if(door==null)continue;
                    EntryAuditStart(pole,size);Check(label+" flag triggers",phase==GamePhase.Flag);
                    float destination=door.x+door.w/2-6;int groundedWalk=0,badPose=0;bool entered=false,overshot=false;
                    var cells=new HashSet<string>();
                    for(int f=0;f<600&&level.id==course.id&&(phase==GamePhase.Flag||phase==GamePhase.Clear);f++)
                    {
                        Step(default);RefreshVisuals();
                        if(phase!=GamePhase.Clear)continue;
                        if(player.X>destination+1)overshot=true;
                        if(player.X>pole.x+20&&player.X<destination-4&&Mathf.Abs(player.Y-door.y)<.1f)
                        {
                            groundedWalk++;
                            if(!player.grounded||player.VX<=0||player.facing!=1||!PlayerVisualSignature.Contains("running"))badPose++;
                            cells.Add(PlayerVisualSignature);
                        }
                        if(!playerVisual.gameObject.activeSelf)
                        {
                            entered=Mathf.Abs(player.X-destination)<.1f&&Mathf.Abs(player.Y-door.y)<.1f&&player.VX==0;
                            float x=player.X;int beforeFrame=frame;PauseGame();Step(default);RefreshVisuals();
                            Check(label+" pause preserves hidden entrance",!playerVisual.gameObject.activeSelf&&frame==beforeFrame&&player.X==x);
                            ResumeGame();
                            for(int i=0;i<8&&phase==GamePhase.Clear;i++){Step(default);RefreshVisuals();}
                            Check(label+" remains inside while settlement waits",phase==GamePhase.Clear&&!playerVisual.gameObject.activeSelf&&player.X==x);
                            break;
                        }
                    }
                    Check(label+" landed walk uses running poses",groundedWalk>=6&&badPose==0&&cells.Count==3,
                        $"walking={groundedWalk} bad={badPose} poses={cells.Count}");
                    Check(label+" stops and disappears at actual doorway",entered&&!overshot,$"entered={entered} overshot={overshot} x={player.X} door={destination}");
                    for(int f=0;f<1200&&level.id==course.id;f++)Step(default);
                    Check(label+" advances after entrance and music",level.id!=course.id);
                    RefreshVisuals();Check(label+" next course restores player",playerVisual.gameObject.activeSelf);
                }
            }
            // An edited castle farther away must not be skipped by the old
            // fixed walk duration, even when all music and score timers expire.
            NewGame("1-1");var farPole=actors.First(a=>a.data.type=="FlagPole");
            var farDoor=EntryAuditDoor(farPole);farDoor.x+=1000;
            Spawn(new NodeData{type="Floor",group="Solid",x=farPole.x+140,y=0,width=1400,height=32});
            EntryAuditStart(farPole,2);
            for(int i=0;i<1000&&phase!=GamePhase.Clear;i++)Step(default);
            for(int i=0;i<Mathf.Max(211,clearAudioFrames+1);i++)Step(default);
            Check("distant edited door prevents premature transition",level.id=="1-1"&&phase==GamePhase.Clear&&!finishEntered);
            for(int i=0;i<1500&&level.id=="1-1";i++)Step(default);
            Check("distant edited door eventually completes",level.id=="1-2");

            NewGame("1-1");var missingPole=actors.First(a=>a.data.type=="FlagPole");
            foreach(var actor in actors.Where(a=>a.data.type=="CastleDoor"))actor.alive=false;
            EntryAuditStart(missingPole,1);
            for(int i=0;i<1200&&level.id=="1-1";i++)Step(default);
            Check("custom map without door does not deadlock",level.id=="1-2");
        }
        IEnumerator CastleEntrySmoke(string output)
        {
            int before=smokeErrors,count=0;var lines=new List<string>();
            RunCastleEntryAudit((n,p,d)=>{count++;lines.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            foreach(string id in new[]{"1-1","1-3","J-1-1","J-8-3"})
            {
                NewGame(id);var pole=actors.First(a=>a.data.type=="FlagPole");var door=EntryAuditDoor(pole);
                EntryAuditStart(pole,2);bool savedWalk=false,savedEntry=false;
                for(int f=0;f<260&&level.id==id;f++)
                {
                    Step(default);RefreshVisuals();yield return null;yield return new WaitForEndOfFrame();
                    if(phase!=GamePhase.Clear)continue;
                    if(!savedWalk&&player.grounded&&player.X>pole.x+30&&playerVisual.gameObject.activeSelf)
                    {SaveScreen(output,"castle-entry-"+id+"-walk");savedWalk=true;}
                    if(!savedEntry&&!playerVisual.gameObject.activeSelf)
                    {SaveScreen(output,"castle-entry-"+id+"-inside");savedEntry=true;break;}
                }
                count++;SmokeCheck(savedWalk&&savedEntry,"visible walk and hidden door screenshots "+id);
            }
            string result=$"MARIO_CASTLE_ENTRY_{(before==smokeErrors?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            lines.Add(result);File.WriteAllLines(Path.Combine(output,"castle-entry-checks.txt"),lines);Debug.Log(result);
        }
    }
}
