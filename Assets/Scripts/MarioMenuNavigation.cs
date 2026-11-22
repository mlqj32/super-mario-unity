using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        enum MenuPage { None,Title,Pause,Courses,Settings,Restart,End,Death,Exit }
        sealed class MenuItem
        {
            public string id,label;public Rect rect;public Action activate;public Action<int> adjust;
            public bool enabled=true,selected;
        }
        readonly Dictionary<MenuPage,string> menuFocusMemory=new Dictionary<MenuPage,string>();
        MenuPage focusPage;string menuFocus;
        bool menuExit,exitToDesktop,menuPointer,menuNeutralRequired;
        Vector2 previousMouse;int heldMenuDirection;float nextMenuRepeat;
        MenuPage CurrentMenu => menuExit?MenuPage.Exit:menuRestart?MenuPage.Restart:menuSettings?MenuPage.Settings:
            phase==GamePhase.Title?MenuPage.Title:phase==GamePhase.Paused?(menuLevels?MenuPage.Courses:MenuPage.Pause):
            phase==GamePhase.GameOver||phase==GamePhase.Victory?MenuPage.End:phase==GamePhase.Dying?MenuPage.Death:MenuPage.None;
        List<MenuItem> MenuItems()
        {
            var items=new List<MenuItem>();
            void Add(string id,Rect rect,string label,Action action,bool selected=false,bool enabled=true,Action<int> adjust=null)
                =>items.Add(new MenuItem{id=id,rect=rect,label=label,activate=action,selected=selected,enabled=enabled,adjust=adjust});
            void Campaign(float y)
            {
                Add("campaign0",new Rect(16,y,68,18),"初代",()=>menuCampaign=0,menuCampaign==0);
                Add("campaign1",new Rect(94,y,68,18),"日版二代",()=>menuCampaign=1,menuCampaign==1);
                Add("campaign2",new Rect(172,y,68,18),"猫里奥",()=>menuCampaign=2,menuCampaign==2);
            }
            switch(CurrentMenu)
            {
                case MenuPage.Title:
                    Campaign(112);
                    Add("start",new Rect(64,134,128,18),"开始游戏",StartCampaign);
                    Add("courses",new Rect(64,156,128,18),"选择关卡",()=>PauseGame(true));
                    Add("settings",new Rect(64,178,128,18),"设置",OpenSettings);
                    Add("exit",new Rect(64,200,128,18),"退出游戏",()=>RequestExit(true));break;
                case MenuPage.Pause:
                    Add("resume",new Rect(56,63,144,22),"继续游戏",ResumeGame);
                    Add("courses",new Rect(56,91,144,22),"选择关卡",()=>PauseGame(true));
                    Add("restart",new Rect(56,119,144,22),"重玩本关",RequestRestart);
                    Add("settings",new Rect(56,147,144,22),"设置",OpenSettings);
                    Add("title",new Rect(56,175,144,22),"返回标题",()=>RequestExit(false));break;
                case MenuPage.Courses:
                    Campaign(58);
                    if(menuCampaign==2)
                    {
                        for(int i=1;i<=4;i++)
                        {
                            int stage=i;
                            Add("cat"+i,new Rect(44+(i-1)%2*88,92+(i-1)/2*34,80,24),"第"+i+"关",()=>StartSyobon(stage),SyobonActive&&syobon.Stage==i);
                        }
                        Add("back",new Rect(64,205,128,18),"返回",CloseCourses);break;
                    }
                    for(int w=0;w<8;w++)for(int s=0;s<4;s++)
                    {
                        string course=(w+1)+"-"+(s+1),id=(menuCampaign==1?"J-":"")+course;
                        Add("course"+course,new Rect(16+w%4*60+s%2*28,84+w/4*54+s/2*22,26,18),course,()=>NewGame(id),level.id==id);
                    }
                    Add("back",new Rect(64,205,128,18),"返回",CloseCourses);break;
                case MenuPage.Settings:
                    Add("volume",new Rect(24,80,208,23),"",null,adjust:d=>SetVolume(masterVolume+d*.05f));
                    Add("mute",new Rect(24,108,96,18),muted?"已静音":"声音开启",ToggleMute,muted);
                    Add("display",new Rect(136,108,96,18),desiredFullscreen?"全屏":"窗口",()=>SetDisplayMode(!desiredFullscreen));
                    Add("aspect",new Rect(88,132,144,20),fillScreen?"铺满":"原始比例",()=>{fillScreen=!fillScreen;SaveSettings();});
                    Add("autoRun",new Rect(24,156,208,18),"自动加速 "+(autoRun?"开启":"关闭"),()=>SetAutoRun(!autoRun),autoRun,enabled:!SyobonActive,adjust:d=>SetAutoRun(d>0));
                    for(int i=0;i<3;i++)
                    {
                        int preset=i;
                        Add("size"+i,new Rect(24+i*72,180,64,18),i==0?"小窗":i==1?"标准":"大窗",()=>{windowPreset=preset;SaveSettings();ApplyDisplayMode();},windowPreset==i,!desiredFullscreen);
                    }
                    Add("back",new Rect(64,207,128,18),"返回",CloseSettings);break;
                case MenuPage.Restart:
                    Add("cancel",new Rect(24,147,96,22),"取消",CancelRestart);
                    Add("confirm",new Rect(136,147,96,22),"确认重开",ConfirmRestart);break;
                case MenuPage.Exit:
                    Add("cancel",new Rect(24,147,96,22),"取消",CancelExit);
                    Add("confirm",new Rect(136,147,96,22),"确认",ConfirmExit);break;
                case MenuPage.End:
                    Add("again",new Rect(64,136,128,20),"再玩一次",()=>StartCampaign(SyobonActive?2:level.Japanese?1:0,1));
                    Add("courses",new Rect(64,162,128,20),"选择关卡",()=>PauseGame(true));
                    Add("title",new Rect(64,188,128,20),"返回标题",ReturnToTitle);break;
                case MenuPage.Death:
                    Add("revive",new Rect(56,183,144,20),SyobonActive||lives>1?"立即复活":"跳过动画",CompleteDeath);break;
            }
            return items;
        }
        void EnsureMenuFocus(List<MenuItem> items)
        {
            MenuPage page=CurrentMenu;
            if(page!=focusPage)
            {
                if(focusPage!=MenuPage.None&&menuFocus!=null)menuFocusMemory[focusPage]=menuFocus;
                focusPage=page;menuNeutralRequired=heldMenuDirection!=0;heldMenuDirection=0;menuPointer=false;
                if(page==MenuPage.Restart||page==MenuPage.Exit)menuFocus="cancel";
                else if(!menuFocusMemory.TryGetValue(page,out menuFocus))
                    menuFocus=page==MenuPage.Title?"start":page==MenuPage.Courses?(menuCampaign==2?"cat"+(SyobonActive?syobon.Stage:1):"course"+level.Course):null;
            }
            if(!items.Exists(i=>i.enabled&&i.id==menuFocus))
            {
                string preferred=page==MenuPage.Courses?(menuCampaign==2?"cat"+(SyobonActive?syobon.Stage:1):"course"+level.Course):null;
                menuFocus=items.Find(i=>i.enabled&&i.id==preferred)?.id??items.Find(i=>i.enabled)?.id;
            }
            if(page==MenuPage.Restart)restartConfirmSelected=menuFocus=="confirm";
        }
        void FocusMenuItem(string id)
        {menuFocus=id;if(CurrentMenu==MenuPage.Restart)restartConfirmSelected=id=="confirm";}
        void MoveMenuFocus(List<MenuItem> items,int direction)
        {
            var current=items.Find(i=>i.id==menuFocus);if(current==null)return;
            Vector2 axis=direction==1?Vector2.right:direction==-1?Vector2.left:direction==2?Vector2.up:Vector2.down;
            MenuItem best=null;float bestScore=float.MaxValue;
            foreach(var item in items)
            {
                if(!item.enabled||item==current)continue;
                Vector2 delta=item.rect.center-current.rect.center;
                float along=Vector2.Dot(delta,axis),across=Mathf.Abs(axis.x!=0?delta.y:delta.x);
                if(along<=1)continue;
                float score=along+across*4;
                if(score<bestScore){best=item;bestScore=score;}
            }
            if(best!=null)FocusMenuItem(best.id);
        }
        int MenuDirection(Vector2 stick,float now)
        {
            int direction=0;
            float largest=Mathf.Max(Mathf.Abs(stick.x),Mathf.Abs(stick.y));
            if(menuNeutralRequired){if(largest>=.35f)return 0;menuNeutralRequired=false;}
            if(largest>=(heldMenuDirection==0?.55f:.35f))
                direction=Mathf.Abs(stick.x)>Mathf.Abs(stick.y)?(stick.x>0?1:-1):(stick.y>0?-2:2);
            if(direction==0){heldMenuDirection=0;return 0;}
            if(direction!=heldMenuDirection){heldMenuDirection=direction;nextMenuRepeat=now+.35f;return direction;}
            if(now<nextMenuRepeat)return 0;
            nextMenuRepeat=now+.11f;return direction;
        }
        bool HandleMenuInput(MarioInputFrame input,float now)
        {
            var page=CurrentMenu;
            var items=MenuItems();EnsureMenuFocus(items);
            if(page==MenuPage.None)
            {
                if(SyobonActive&&syobon.Waiting&&input.accept)return false;
                if(input.courses&&phase==GamePhase.Playing){PauseGame(true);return true;}
                if(input.pause&&SimulationActive){PauseGame();return true;}
                return false;
            }
            if(page==MenuPage.Death)
            {if(input.skip||input.accept||input.pause){CompleteDeath();return true;}return false;}
            if(input.cancel){BackFromMenu();return true;}
            if(input.pause&&!input.accept)
            {
                if(page==MenuPage.Title){ActivateMenuItem(items);return true;}
                if(page!=MenuPage.Restart&&page!=MenuPage.Exit){BackFromMenu();return true;}
            }
            if(input.courses&&(page==MenuPage.Title||page==MenuPage.Pause||page==MenuPage.End||page==MenuPage.Courses))
            {if(page==MenuPage.Courses)CloseCourses();else PauseGame(true);return true;}
            if(input.campaign!=0&&(page==MenuPage.Title||page==MenuPage.Courses))menuCampaign=(menuCampaign+(input.campaign>0?1:2))%3;
            int direction=MenuDirection(input.direction,now);
            if(input.tab)
            {
                int index=items.FindIndex(i=>i.id==menuFocus),step=input.reverseTab?-1:1;
                do{index=(index+step+items.Count)%items.Count;}while(!items[index].enabled);
                FocusMenuItem(items[index].id);menuPointer=false;
            }
            else if(direction!=0)
            {
                var item=items.Find(i=>i.id==menuFocus);
                if(item?.adjust!=null&&Mathf.Abs(direction)==1)item.adjust(direction);
                else MoveMenuFocus(items,direction);
                menuPointer=false;
            }
            // Rebuild after campaign changes so confirmation uses the new course ID.
            if(input.accept)ActivateMenuItem(MenuItems());
            return true;
        }
        void ActivateMenuItem(List<MenuItem> items)
        {items.Find(i=>i.id==menuFocus&&i.enabled)?.activate?.Invoke();ClearRestartInput();}
        void BackFromMenu()
        {
            switch(CurrentMenu)
            {
                case MenuPage.Settings:CloseSettings();break;
                case MenuPage.Courses:CloseCourses();break;
                case MenuPage.Restart:CancelRestart();break;
                case MenuPage.Exit:CancelExit();break;
                case MenuPage.Pause:ResumeGame();break;
                case MenuPage.End:ReturnToTitle();break;
                case MenuPage.Title:RequestExit(true);break;
            }
        }
        void CloseCourses()
        {menuLevels=false;if(phaseBeforePause!=GamePhase.Playing&&phaseBeforePause!=GamePhase.Dying)ResumeGame();else ClearRestartInput();}
        void RequestExit(bool desktop){menuExit=true;exitToDesktop=desktop;ClearRestartInput();}
        void CancelExit(){menuExit=false;ClearRestartInput();}
        void ConfirmExit(){if(!menuExit)return;if(exitToDesktop)Application.Quit();else ReturnToTitle();}
        void ReturnToTitle()
        {StopSyobon();menuExit=menuRestart=menuSettings=menuLevels=false;phase=GamePhase.Title;StopCourseAudio();ClearRestartInput();}
    }
}
