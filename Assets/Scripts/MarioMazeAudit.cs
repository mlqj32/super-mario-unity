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
        void PlaceMazeRunner(float x,float y)
        {
            player.X=x;player.Y=y;player.vx4096=player.vy256=0;player.grounded=true;
            standing=null;cameraX=x-112;ClearJumpAssist();
        }
        bool WalkMazeTail(Actor decider,int section,float origin,bool gapJump=false)
        {
            bool sawPass=false,pressed=false;
            for(int i=0;i<500&&!decider.triggered&&phase==GamePhase.Playing;i++)
            {
                float x=player.X-origin;
                bool jump=section==0?(gapJump&&x>=716&&x<844)||x>=1064&&player.Y<48:x>=1000;
                Step(new Controls{horizontal=1,run=true,jump=jump,jumpPressed=jump&&(!pressed||player.grounded)});
                pressed=jump;sawPass|=sectionPassed;
            }
            return sawPass;
        }
        public void RunMazeAudit(Action<string,bool,string> record)
        {
            bool saved=responsiveControls;
            var savedTrace=trace;
            try
            {
                foreach(bool responsive in new[]{true,false})
                foreach(int size in new[]{1,2,3})
                foreach(int section in new[]{0,1})
                foreach(int approach in new[]{0,1,2})
                {
                    responsiveControls=responsive;NewGame("4-4");power=size;invulnerable=10000;
                    // Expand using the real entry, then place the player before
                    // the route detector. Never inject the maze verdict.
                    Step(default);
                    if(section==1)
                    {
                        LoadLocation(0);actors.First(a=>a.data.type=="Section").triggered=true;
                        ExpandSection(new Actor{x=256,data=new NodeData{type="Section"}},1);
                    }
                    var decider=actors.First(a=>a.data.type=="SectionDecider");
                    // Actual terrain, deliberately independent of detector coordinates.
                    // Walk / late jump / wrong lane; the upper route ends at x=976.
                    PlaceMazeRunner(section==0?900:944,section==0?(approach==2?0:112):approach*48+(approach==2?16:0));
                    var messages=new List<string>();trace=messages.Add;
                    if(section==1&&approach!=0)
                    {
                        // Traverse the actual middle / upper detector strips.
                        // No teleport to the decider and no verdict injection.
                        for(int i=0;i<160&&player.X<1080;i++)Step(new Controls{horizontal=1,run=true});
                        record($"Maze: second section rejects lane={approach} size={size} responsive={responsive}",
                            player.X>=1080&&!sectionPassed&&!decider.triggered,$"x={player.X} y={player.Y} passed={sectionPassed}");
                        continue;
                    }
                    bool sawPass=WalkMazeTail(decider,section,256,approach==1);
                    string detail=$"x={player.X} y={player.Y} passed={sawPass} phase={phase} trace={string.Join(";",messages)}";
                    int expected=section==0&&approach==2?0:section+1;
                    record($"Maze: 4-4 section={section} approach={approach} size={size} responsive={responsive}",
                        sawPass==(expected!=0)&&messages.Any(m=>m.StartsWith("Section "+expected+" at")),detail);
                    trace=null;
                }
                foreach(bool responsive in new[]{true,false})
                foreach(int size in new[]{1,2,3})
                {
                    responsiveControls=responsive;NewGame("4-4");power=size;invulnerable=10000;Step(default);
                    float origin=256;var messages=new List<string>();trace=messages.Add;
                    // Wrong lower lane -> retry upper lane -> second lower lane.
                    // Only reposition to each segment's approach; retain actual
                    // decisions/actors, so a stale verdict cannot be hidden.
                    for(int leg=0;leg<3;leg++)
                    {
                        int section=leg==2?1:0,expected=leg;
                        PlaceMazeRunner(origin+(section==0?644:688),leg==1?112:0);
                        var decider=actors.Last(a=>a.data.type=="SectionDecider");messages.Clear();
                        WalkMazeTail(decider,section,origin);
                        bool correct=decider.triggered&&messages.Contains("Section "+expected+" at "+decider.x);
                        if(leg==2)correct&=actors.Any(a=>a.data.type=="CastleAxe"&&a.x>decider.x);
                        record($"Maze: retry chain leg={leg} size={size} responsive={responsive}",correct,
                            $"x={player.X} y={player.Y} trace={string.Join(";",messages)}");
                        if(!correct)break;origin=decider.x;
                    }
                }
            }
            finally{trace=savedTrace;responsiveControls=saved;NewGame("1-1");}
        }
        IEnumerator MazeSmoke(string output)
        {
            int before=smokeErrors,count=0;var checks=new List<string>();
            RunMazeAudit((n,p,d)=>{count++;checks.Add((p?"PASS ":"FAIL ")+n+" "+d);SmokeCheck(p,n+" "+d);});
            string result=$"MARIO_MAZE_{(smokeErrors==before?"PASS":"FAIL")} checks={count} errors={smokeErrors-before}";
            File.WriteAllLines(Path.Combine(output,"maze-checks.txt"),checks);
            File.WriteAllText(Path.Combine(output,"maze-result.txt"),result);Debug.Log(result);
            yield return null;
        }
    }
}
