using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Mario.Syobon
{
    // Small drawing vocabulary used by the preserved C# gameplay source.
    // No MonoGame assemblies, subprocess, browser or emulator are used.
    public struct Color
    {
        public byte r,g,b,a;
        public Color(int r,int g,int b,int a=255){this.r=(byte)r;this.g=(byte)g;this.b=(byte)b;this.a=(byte)a;}
        public static Color White=>new Color(255,255,255);
        public static Color Black=>new Color(0,0,0);
        public static implicit operator UnityEngine.Color(Color c)=>new Color32(c.r,c.g,c.b,c.a);
    }
    public struct Vector2
    {
        public float X,Y;
        public Vector2(float x,float y){X=x;Y=y;}
        public static Vector2 Zero=>new Vector2();
        public static Vector2 One=>new Vector2(1,1);
    }
    public struct Rectangle
    {
        public int X,Y,Width,Height;
        public Rectangle(int x,int y,int w,int h){X=x;Y=y;Width=w;Height=h;}
    }
    public enum SpriteEffects {None}
    public sealed class SpriteFont{}
    public enum Keys {Left,Right,Up,Down,Z,O,F1,Enter,Space,D0,D1,D2,D3,D4}
    public struct KeyState
    {
        public int horizontal;public bool jump,down,accept;
        public bool IsKeyDown(Keys key)=>key==Keys.Left?horizontal<0:key==Keys.Right?horizontal>0:
            key==Keys.Up||key==Keys.Z?jump:key==Keys.Down?down:key==Keys.Enter?accept:false;
    }
    public sealed class GameTime
    {
        public TimeSpan TotalGameTime,ElapsedGameTime;
    }
    public static class Maps
    {
        static readonly Dictionary<string,byte[][]> cache=new Dictionary<string,byte[][]>();
        public static byte[] Row(int stage,int area,int row)
        {
            string name=stage+"-"+area;
            if(!cache.TryGetValue(name,out var rows))
            {
                var resource=UnityEngine.Resources.Load<TextAsset>("Syobon/Stages/"+name);
                if(resource==null)throw new InvalidOperationException("Missing Syobon map "+name);
                var lines=resource.text.Replace("\r","").Split('\n');rows=new byte[17][];
                for(int y=0;y<17;y++)
                {
                    string line=y<lines.Length?lines[y]:"";
                    string[] cells=line.Length==0?Array.Empty<string>():line.Split(',');
                    if(cells.Length>1001)throw new InvalidOperationException("Syobon map row exceeds engine bounds: "+name);
                    rows[y]=new byte[cells.Length];
                    for(int x=0;x<cells.Length;x++)rows[y][x]=byte.Parse(cells[x],CultureInfo.InvariantCulture);
                }
                cache[name]=rows;
            }
            return rows[row];
        }
        public static void Reload()=>cache.Clear();
    }
    public sealed class SpriteBatch : IDisposable
    {
        public struct Quad
        {
            public Texture2D texture;public UnityEngine.Vector2 a,b,c,d;
            public Rect uv;public UnityEngine.Color color;
        }
        readonly List<Quad> draws=new List<Quad>(2048);
        readonly Func<bool> mirrored;
        Material material;
        public RenderTexture Target {get;private set;}
        public UnityEngine.Color Background {get;private set;}
        public int DrawCount=>draws.Count;
        public SpriteBatch(Func<bool> mirrored){this.mirrored=mirrored;}
        public void Begin()=>draws.Clear();
        public void End(){}
        public void Clear(Color color)=>Background=color;
        public void Draw(Texture2D texture,Vector2 position,Color color)=>Draw(texture,position,null,color);
        public void Draw(Texture2D texture,Vector2 position,Rectangle? source,Color color)
            =>Draw(texture,position,source,color,0,Vector2.Zero,Vector2.One,SpriteEffects.None,0);
        public void Draw(Texture2D texture,Vector2 position,Rectangle? source,Color color,float rotation,Vector2 origin,float scale,SpriteEffects effects,float depth)
            =>Draw(texture,position,source,color,rotation,origin,new Vector2(scale,scale),effects,depth);
        public void Draw(Texture2D texture,Vector2 position,Rectangle? source,Color color,float rotation,Vector2 origin,Vector2 scale,SpriteEffects effects,float depth)
        {
            if(texture==null)throw new InvalidOperationException("Missing Syobon texture");
            var s=source??new Rectangle(0,0,texture.width,texture.height);
            if(s.Width<=0||s.Height<=0)return;
            if(s.X<0||s.Y<0||s.X+s.Width>texture.width||s.Y+s.Height>texture.height)
                throw new InvalidOperationException("Syobon source rectangle exceeds "+texture.name+": "+s.X+","+s.Y+","+s.Width+","+s.Height);
            float co=Mathf.Cos(rotation),si=Mathf.Sin(rotation);
            UnityEngine.Vector2 Point(float x,float y)
            {x=(x-origin.X)*scale.X;y=(y-origin.Y)*scale.Y;return new UnityEngine.Vector2(position.X+x*co-y*si,position.Y+x*si+y*co);}
            float u=s.X/(float)texture.width,w=s.Width/(float)texture.width;
            if(mirrored()){u+=w;w=-w;}
            draws.Add(new Quad{texture=texture,a=Point(0,0),b=Point(s.Width,0),c=Point(s.Width,s.Height),d=Point(0,s.Height),
                uv=new Rect(u,1-s.Y/(float)texture.height,w,-s.Height/(float)texture.height),color=color});
        }
        public void Box(float x,float y,float w,float h,Color color,bool fill)
        {
            if(w<=0||h<=0)return;
            if(!fill){Box(x,y,w,1,color,true);Box(x,y+h-1,w,1,color,true);Box(x,y,1,h,color,true);Box(x+w-1,y,1,h,color,true);return;}
            draws.Add(new Quad{texture=Texture2D.whiteTexture,a=new UnityEngine.Vector2(x,y),b=new UnityEngine.Vector2(x+w,y),
                c=new UnityEngine.Vector2(x+w,y+h),d=new UnityEngine.Vector2(x,y+h),uv=new Rect(0,0,1,1),color=color});
        }
        public void Line(float x1,float y1,float x2,float y2,Color color)
        {
            var delta=new UnityEngine.Vector2(x2-x1,y2-y1);var normal=new UnityEngine.Vector2(-delta.y,delta.x).normalized*.5f;
            var p=new UnityEngine.Vector2(x1,y1);var q=new UnityEngine.Vector2(x2,y2);
            draws.Add(new Quad{texture=Texture2D.whiteTexture,a=p-normal,b=q-normal,c=q+normal,d=p+normal,uv=new Rect(0,0,1,1),color=color});
        }
        public void Ellipse(int x,int y,int rx,int ry,Color color,bool fill)
        {
            if(rx<=0||ry<=0)return;
            for(int row=-ry;row<=ry;row++)
            {
                int half=Mathf.RoundToInt(rx*Mathf.Sqrt(Mathf.Max(0,1-row*row/(float)(ry*ry))));
                if(fill)Box(x-half,y+row,half*2+1,1,color,true);
                else{Box(x-half,y+row,1,1,color,true);Box(x+half,y+row,1,1,color,true);}
            }
        }
        public void DrawString(SpriteFont font,string text,Vector2 position,Color color)
        {
            text=SyobonText.Translate(text);float x=position.X,y=position.Y;
            foreach(char c in text.ToUpperInvariant())
            {
                if(c=='\n'){x=position.X;y+=16;continue;}
                var glyph=PixelLibrary.Glyph(c);
                if(glyph!=null)
                {
                    float w=glyph.width,h=glyph.height;
                    draws.Add(new Quad{texture=glyph,a=new UnityEngine.Vector2(x,y),b=new UnityEngine.Vector2(x+w,y),
                        c=new UnityEngine.Vector2(x+w,y+h),d=new UnityEngine.Vector2(x,y+h),uv=new Rect(0,1,1,-1),color=color});
                }
                x+=c>127?12:8;
            }
        }
        public void Render()
        {
            if(Target==null){Target=new RenderTexture(480,420,0,RenderTextureFormat.ARGB32){filterMode=FilterMode.Point};Target.Create();}
            if(material==null)material=new Material(Shader.Find("Sprites/Default")){hideFlags=HideFlags.HideAndDontSave};
            var previous=RenderTexture.active;RenderTexture.active=Target;
            GL.PushMatrix();
            try
            {
                GL.LoadPixelMatrix(0,480,420,0);GL.Clear(true,true,Background);
                foreach(var q in draws)
                {
                    material.mainTexture=q.texture;material.SetPass(0);GL.Begin(GL.QUADS);GL.Color(q.color);
                    GL.TexCoord2(q.uv.x,q.uv.y);GL.Vertex3(q.a.x,q.a.y,0);
                    GL.TexCoord2(q.uv.xMax,q.uv.y);GL.Vertex3(q.b.x,q.b.y,0);
                    GL.TexCoord2(q.uv.xMax,q.uv.yMax);GL.Vertex3(q.c.x,q.c.y,0);
                    GL.TexCoord2(q.uv.x,q.uv.yMax);GL.Vertex3(q.d.x,q.d.y,0);GL.End();
                }
            }
            finally{GL.PopMatrix();RenderTexture.active=previous;}
        }
        public void Dispose()
        {if(Target!=null){Target.Release();UnityEngine.Object.DestroyImmediate(Target);Target=null;}if(material!=null)UnityEngine.Object.DestroyImmediate(material);}
    }
    public partial class Engine : IDisposable
    {
        KeyState keys;int ticks;
        public int Stage=>stb;
        public int Area=>stc;
        public int Remaining=>nokori;
        public int Checkpoint=>tyuukan;
        public bool Dead=>main==1&&(mtype==200||mhp<=0);
        public bool Won=>ending==1||main==2;
        public bool Waiting=>paused;
        public int TickCount=>ticks;
        public int MusicSamples=>resources.MusicSamples;
        public bool AudioPlaying=>resources.Playing;
        public float X=>(ma+screenX)/100f;
        public float Y=>mb/100f;
        // Cat Mario has no separate walk/run button.  Holding a direction
        // always drives the original acceleration curve; expose the current
        // velocity so the cross-campaign input audit can verify that behavior.
        public float HorizontalVelocity=>mc/100f;
        public bool UsesAutomaticAcceleration=>true;
        public RenderTexture Target=>spriteBatch.Target;
        public void SetAudio(bool enabled)=>resources.AudioEnabled=enabled;
        public void Start(int stage,int lives=10)
        {
            if(stage<1||stage>4)throw new ArgumentOutOfRangeException(nameof(stage));
            resources.Stop();sta=1;stb=stage;stc=0;main=1;zxon=0;nokori=lives;trap=tyuukan=over=ending=maintm=0;
            tmsgtype=tmsgy=tmsgtm=keytm=mkeytm=mmsgtm=mmutekitm=mjumptm=blacktm=0;
            paused=false;keys=default;Step(default);
        }
        public void Step(KeyState input)
        {
            keys=input;ticks++;
            Update(new GameTime{TotalGameTime=TimeSpan.FromSeconds(ticks/30d),ElapsedGameTime=TimeSpan.FromSeconds(1/30d)});
        }
        public void Retry(bool checkpoint)
        {
            int remaining=nokori-(Dead?1:0),saved=checkpoint?tyuukan:0,stage=stb,area=checkpoint?stc:0;
            resources.Stop();stb=stage;stc=area;main=1;zxon=0;maintm=mtype=tmsgtype=tmsgy=tmsgtm=0;
            keytm=mkeytm=mmsgtm=mmutekitm=mjumptm=blacktm=0;
            trap=0;tyuukan=saved;nokori=remaining;paused=false;keys=default;Step(default);
        }
        public void Pause()=>resources.Pause();
        public void Resume()=>resources.Resume();
        public void Render()=>spriteBatch.Render();
        public void Dispose(){resources.Dispose();spriteBatch.Dispose();}
    }
}
