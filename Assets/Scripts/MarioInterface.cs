using UnityEngine;

namespace Mario
{
    public sealed partial class MarioGame
    {
        Texture2D white;
        int menuCampaign;
        void Fill(Rect r,Color color){if(white==null)white=Texture2D.whiteTexture;var old=GUI.color;GUI.color=color;GUI.DrawTexture(r,white);GUI.color=old;}
        static float TextWidth(string text){float w=0;foreach(char c in text)w+=c>127?12:8;return w;}
        void Text(string text,float x,float y,float scale=1,Color? color=null)
        {
            float start=x;var old=GUI.color;GUI.color=color??Color.white;
            foreach(char raw in text.ToUpperInvariant())
            {
                if(raw=='\n'){y+=16*scale;x=start;continue;}
                var tex=raw==' '?null:PixelLibrary.Glyph(raw);
                if(tex!=null)GUI.DrawTexture(new Rect(x,y,tex.width*scale,tex.height*scale),tex,ScaleMode.StretchToFill);
                x+=(raw>127?12:8)*scale;
            }
            GUI.color=old;
        }
        bool Button(Rect r,string text,bool selected=false)
        {
            bool hover=r.Contains(Event.current.mousePosition);Fill(r,selected?new Color32(180,55,22,255):hover?new Color32(66,76,112,255):new Color32(28,34,54,255));
            Fill(new Rect(r.x,r.y,r.width,1),selected?new Color32(255,176,93,255):new Color32(91,105,141,255));
            Text(text,r.x+(r.width-TextWidth(text))/2,r.y+(r.height-(ContainsChinese(text)?12:8))/2);
            return GUI.Button(r,GUIContent.none,GUIStyle.none);
        }
        static bool ContainsChinese(string text){foreach(char c in text)if(c>127)return true;return false;}
        void DrawMenuItems()
        {
            var items=MenuItems();EnsureMenuFocus(items);
            // IMGUI must not also activate its last mouse-focused control from
            // the same Return/Space already handled by our common input layer.
            if(Event.current.type==EventType.KeyDown)Event.current.Use();
            foreach(var item in items)
            {
                bool hover=item.rect.Contains(Event.current.mousePosition);
                if(item.enabled&&hover&&(menuPointer||Event.current.type==EventType.MouseDown))FocusMenuItem(item.id);
                bool enabled=GUI.enabled;GUI.enabled=item.enabled;
                if(item.id=="volume")
                {
                    if(Button(new Rect(24,83,24,18),"-"))SetVolume(masterVolume-.05f);
                    float volume=GUI.HorizontalSlider(new Rect(56,88,144,16),masterVolume,0,1);
                    if(!Mathf.Approximately(volume,masterVolume))SetVolume(volume);
                    if(Button(new Rect(208,83,24,18),"+"))SetVolume(masterVolume+.05f);
                }
                else if(Button(item.rect,item.label,item.selected))
                {FocusMenuItem(item.id);item.activate?.Invoke();ClearRestartInput();break;}
                GUI.enabled=enabled;
                if(!item.enabled)Fill(item.rect,new Color32(12,16,30,170));
                if(item.id==menuFocus&&item.enabled)
                {
                    Rect r=item.rect;var gold=new Color32(255,222,100,255);
                    Fill(new Rect(r.x-2,r.y-2,r.width+4,2),gold);Fill(new Rect(r.x-2,r.yMax,r.width+4,2),gold);
                    Fill(new Rect(r.x-2,r.y,2,r.height),gold);Fill(new Rect(r.xMax,r.y,2,r.height),gold);
                }
            }
        }
        void OnGUI()
        {
            if(Error!=null){GUI.Label(new Rect(20,20,800,100),Error);return;}if(target==null)return;
            Rect viewport=SyobonActive?SyobonPresentationRect(Screen.width,Screen.height,fillScreen):PresentationRect(Screen.width,Screen.height,fillScreen);
            Fill(new Rect(0,0,Screen.width,Screen.height),new Color32(10,13,23,255));
            GUI.DrawTexture(viewport,SyobonActive?syobon.Target:target,ScaleMode.StretchToFill,false);
            Matrix4x4 old=GUI.matrix;GUI.matrix=Matrix4x4.TRS(new Vector3(viewport.x,viewport.y,0),Quaternion.identity,new Vector3(viewport.width/256,viewport.height/240,1));
            if(SyobonActive)
            {
                Fill(new Rect(0,0,256,18),new Color32(12,16,30,210));
                Text("猫里奥",8,3);Text("关卡 "+syobon.Stage+"/4",94,3);Text("x"+syobon.Remaining,202,5);
                if(syobon.Waiting)Text(usingGamepad?"按A继续":"按跳跃键继续",84,205);
            }
            else
            {
            Text("马里奥",16,4);Text(score.ToString("D6"),16,18);Text(level.Japanese?"二代":"初代",96,4);
            var coin=PixelLibrary.Sprite(PixelLibrary.Find("Character","Coin","Overworld normal").parts[0]).texture;
            GUI.DrawTexture(new Rect(88,15,coin.width,coin.height),coin,ScaleMode.StretchToFill);
            Text("x"+coins.ToString("D2"),100,18);
            Text("关卡",148,4);Text(level.Course,148,18);Text("时间",208,4);Text(timeLeft.ToString("D3"),208,18);
            }
            if(menuExit)
            {
                Fill(new Rect(12,65,232,134),new Color32(12,16,30,255));
                string title=exitToDesktop?"退出游戏":"返回标题";
                Text(title,(256-TextWidth(title))/2,82);
                Text("本次关卡进度将不保留",24,111);
            }
            else if(menuRestart)DrawRestartConfirmation();
            else if(menuSettings)DrawSettings();
            else if(phase==GamePhase.Title)
            {
                Fill(new Rect(24,45,208,50),new Color32(180,55,22,255));Fill(new Rect(24,45,208,2),new Color32(255,188,128,255));
                Text("超级马里奥",68,60,2);
                Text("三部作品 / 68关",52,98);
            }
            else if(phase==GamePhase.Paused)
            {
                Fill(new Rect(8,34,240,200),new Color32(12,16,30,247));
                if(menuLevels)
                {
                    Text("选择关卡",104,40);
                    Text(usingGamepad?(PlayStationPad?"L1/R1 切换作品":"LB/RB 切换作品"):"Q/E 切换作品",64,187,.75f);
                }
                else
                {
                    Text("已暂停",110,45);
                    Text(SyobonActive?"方向移动 跳跃确认 暂停可重玩":GameplayInputHint,12,207,.75f);
                }
            }
            else if(phase==GamePhase.GameOver||phase==GamePhase.Victory)
            {
                Fill(new Rect(16,72,224,145),Color.black);
                Text(phase==GamePhase.Victory?(SyobonActive?"猫里奥通关!":"谢谢你 马里奥!"):"游戏结束",phase==GamePhase.Victory?54:104,87);
                Text(phase==GamePhase.Victory?"冒险完成!":"选择下方操作",phase==GamePhase.Victory?100:56,112);
            }
            else if(phase==GamePhase.Clear&&Stage==4)
            {Text("谢谢你 马里奥!",54,80);if(World!=8)Text("公主在另一座城堡!",36,104);}
            if(phase==GamePhase.Dying)
            {
                string remaining="剩余生命 "+(SyobonActive?syobon.Remaining-1:Mathf.Max(0,lives-1));
                Fill(new Rect(72,58,112,26),new Color32(12,16,30,240));
                Text(remaining,(256-TextWidth(remaining))/2,65);
                Fill(new Rect(24,178,208,51),new Color32(12,16,30,240));
                Text(usingGamepad?(PlayStationPad?"叉 / OPTIONS":"A / START"):"回车 / 空格 / Z / R",34,211);
            }
            if(CurrentMenu!=MenuPage.None)
            {
                DrawMenuItems();
                if(CurrentMenu!=MenuPage.Death)
                {
                    Fill(new Rect(0,227,256,13),new Color32(12,16,30,250));
                    Text(MenuInputHint,5,230,.65f);
                    if(CurrentMenu==MenuPage.Settings&&menuFocus=="volume")Text("左右调整",88,65,.75f);
                }
            }
            GUI.matrix=old;
        }
    }
}
