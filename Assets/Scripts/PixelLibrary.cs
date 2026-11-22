using System;
using System.Collections.Generic;
using UnityEngine;

namespace Mario
{
    public static class PixelLibrary
    {
        [Serializable] public sealed class Catalog { public Entry[] entries; }
        [Serializable] public sealed class Entry { public string key, layout; public Part[] parts; }
        [Serializable] public sealed class Part { public string name, file; public int width, height; }
        static readonly Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>();
        static readonly Dictionary<string, Entry> cache = new Dictionary<string, Entry>();
        static Catalog catalog;
        static readonly Dictionary<char,Texture2D> glyphs=new Dictionary<char,Texture2D>();
        static readonly Dictionary<char,Sprite> glyphSprites=new Dictionary<char,Sprite>();
        public static Entry Find(string group, string type, string state = "")
        {
            if (catalog == null) catalog = JsonUtility.FromJson<Catalog>(Resources.Load<TextAsset>("SpriteCatalog").text);
            string query = group + " " + type + " " + state;
            if (cache.TryGetValue(query, out var cached)) return cached;
            var tokens = new HashSet<string>(query.Split(' '));
            Entry best = null; int bestScore = int.MinValue;
            foreach (var e in catalog.entries)
            {
                var words = e.key.Split(' ');
                if (words.Length < 2 || words[0] != group || words[1] != type) continue;
                int score = -words.Length; bool valid = true;
                for (int i = 2; i < words.Length; i++)
                {
                    if (words[i] == "normal") continue;
                    if (!tokens.Contains(words[i])) { valid = false; break; }
                    score += words[i] == "Underworld" || words[i] == "Castle" || words[i] == "Underwater" ? 300 : 100;
                }
                if (valid && score > bestScore) { bestScore = score; best = e; }
            }
            cache[query] = best; return best;
        }
        public static Sprite Sprite(Part part)
        {
            if (sprites.TryGetValue(part.file, out var result)) return result;
            var tex = Resources.Load<Texture2D>(part.file);
            if (tex == null) throw new InvalidOperationException("Missing pixel asset: " + part.file);
            tex.filterMode = FilterMode.Point; tex.wrapMode = TextureWrapMode.Clamp;
            result = UnityEngine.Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), Vector2.zero, 1, 0, SpriteMeshType.FullRect);
            result.name = part.file; sprites[part.file] = result; return result;
        }
        public static Texture2D Glyph(char c)
        {
            if(glyphs.TryGetValue(c,out var tex))return tex;
            tex=Resources.Load<Texture2D>("Font/"+(int)c);
            if(tex!=null)tex.filterMode=FilterMode.Point;
            glyphs[c]=tex;return tex;
        }
        public static Sprite TopSlice(Part part,int height)
        {
            var source=Sprite(part);height=Mathf.Clamp(height,1,source.texture.height);
            string key=part.file+"/top/"+height;
            if(sprites.TryGetValue(key,out var result))return result;
            result=UnityEngine.Sprite.Create(source.texture,new Rect(0,source.texture.height-height,source.texture.width,height),Vector2.zero,1,0,SpriteMeshType.FullRect);
            result.name=key;sprites[key]=result;return result;
        }
        public static Sprite GlyphSprite(char c)
        {
            if(glyphSprites.TryGetValue(c,out var sprite))return sprite;
            var tex=Glyph(c);if(tex==null)return null;
            sprite=UnityEngine.Sprite.Create(tex,new Rect(0,0,tex.width,tex.height),Vector2.zero,1,0,SpriteMeshType.FullRect);glyphSprites[c]=sprite;return sprite;
        }
    }
    public sealed class PixelVisual : MonoBehaviour
    {
        readonly List<SpriteRenderer> renderers = new List<SpriteRenderer>();
        string signature;
        public string CurrentSignature => signature;
        public void SetTile(string file,int order=0,float width=16,float height=16,bool forceTiled=false)
        {
            if(renderers.Count==0){var child=new GameObject("Reference tile");child.transform.SetParent(transform,false);renderers.Add(child.AddComponent<SpriteRenderer>());}
            for(int i=1;i<renderers.Count;i++)renderers[i].enabled=false;
            var r=renderers[0];r.enabled=true;r.sprite=PixelLibrary.Sprite(new PixelLibrary.Part{file=file,width=16,height=16});
            // Resizing ropes keep tiled mode even at exactly 16px; switching
            // modes there can retain the previous frame's renderer bounds.
            r.drawMode=!forceTiled&&width==16&&height==16?SpriteDrawMode.Simple:SpriteDrawMode.Tiled;r.size=new Vector2(width,height);
            r.flipX=r.flipY=false;r.color=Color.white;r.sortingOrder=order;r.transform.localPosition=Vector3.zero;transform.localScale=Vector3.one;signature=file;
        }
        public void SetText(string text,float top,int order=3)
        {
            if(string.IsNullOrEmpty(text)){signature=null;foreach(var r in renderers)r.enabled=false;return;}
            text=text.Replace("THANK YOU MARIO!","谢谢你 马里奥!").Replace("BUT OUR PRINCESS IS IN\nANOTHER CASTLE!","公主在另一座城堡!").Replace("WELCOME TO WARP WORLD!","欢迎来到跳关区!").Replace("PRESS BUTTON B\nTO SELECT A WORLD","按F1选择关卡").Replace("YOUR QUEST IS OVER.\nWE PRESENT YOU A NEW QUEST.","冒险完成!\n新的冒险等着你!");
            string key="text/"+text+"/"+top+"/"+order;if(signature==key)return;signature=key;transform.localScale=Vector3.one;
            while(renderers.Count<text.Length){var child=new GameObject("Glyph");child.transform.SetParent(transform,false);renderers.Add(child.AddComponent<SpriteRenderer>());}
            float x=0,y=top-8;int index=0;
            foreach(char raw in text.ToUpperInvariant())
            {if(raw=='\n'){x=0;y-=16;continue;}var r=renderers[index++];r.sprite=PixelLibrary.GlyphSprite(raw);r.drawMode=SpriteDrawMode.Simple;r.sortingOrder=order;r.enabled=r.sprite!=null;r.flipX=r.flipY=false;r.color=Color.white;r.transform.localScale=Vector3.one;r.transform.localPosition=new Vector3(x,y-(raw>127?4:0),0);x+=raw>127?12:8;}
            for(int i=index;i<renderers.Count;i++)renderers[i].enabled=false;
        }
        public void Set(string group, string type, string state, float width, float height, int order = 0, bool flip = false, Color? tint = null, bool flipVertical = false)
        {
            var e = PixelLibrary.Find(group, type, state);
            if (e == null) { foreach (var r in renderers) r.enabled = false; return; }
            string key = e.key + "/" + width + "/" + height+(flipVertical?"/upside-down":"");
            if (signature != key)
            {
                signature = key;
                while (renderers.Count < e.parts.Length)
                {
                    var child = new GameObject("Pixels"); child.transform.SetParent(transform, false);
                    renderers.Add(child.AddComponent<SpriteRenderer>());
                }
                // Place caps before the middle so the middle gets exactly the remaining span.
                float topH=0,bottomH=0,leftW=0,rightW=0;
                foreach(var p in e.parts) { if(p.name=="top")topH=p.height;if(p.name=="bottom")bottomH=p.height;if(p.name=="left")leftW=p.width;if(p.name=="right")rightW=p.width; }
                for (int i = 0; i < renderers.Count; i++)
                {
                    var r = renderers[i]; r.enabled = i < e.parts.Length;
                    if (!r.enabled) continue;
                    var p = e.parts[i]; r.sprite = PixelLibrary.Sprite(p);
                    // A character's artwork may extend beyond its collision
                    // box (e.g. crouching Mario: 22 painted rows, 16 hitbox rows).
                    // Only terrain and growing vines repeat/crop their texture.
                    bool nativeSprite=group=="Character"&&type!="Vine"&&e.layout=="single";
                    r.drawMode = nativeSprite?SpriteDrawMode.Simple:SpriteDrawMode.Tiled;
                    float w = width, h = height, x = 0, y = 0;
                    if(nativeSprite){w=p.width;h=p.height;x=Mathf.Floor((width-w)/2);}
                    else if (e.layout == "vertical")
                    {
                        if(p.name=="top")
                        {
                            h=Mathf.Min(topH,height);y=height-h;
                            // A short cannon keeps the muzzle (top of the art),
                            // not the skull at the bottom of the 32px source.
                            if(type=="Cannon"&&h<topH)r.sprite=PixelLibrary.TopSlice(p,Mathf.CeilToInt(h));
                        }
                        else if(p.name=="bottom"){h=Mathf.Min(bottomH,height);}
                        else {h=Mathf.Max(0,height-topH-bottomH);y=bottomH;}
                    }
                    else if(e.layout=="horizontal")
                    {
                        if(p.name=="left")w=Mathf.Min(leftW,width);
                        else if(p.name=="right"){w=Mathf.Min(rightW,width);x=width-w;}
                        else {w=Mathf.Max(0,width-leftW-rightW);x=leftW;}
                    }
                    r.flipY=flipVertical;
                    r.size = new Vector2(w, h); r.transform.localPosition = new Vector3(x,y+(flipVertical?h:0),0);
                    // The spring coil compresses between fixed caps instead of
                    // cropping/repeating the middle texture as its height changes.
                    bool springCoil=type=="Springboard"&&p.name=="middle";
                    r.transform.localScale=springCoil?new Vector3(w/p.width,h/p.height,1):Vector3.one;
                    if(springCoil)r.drawMode=SpriteDrawMode.Simple;
                }
            }
            // Flip around the visual's center while preserving its left/bottom anchor.
            transform.localScale = new Vector3(flip ? -1 : 1, 1, 1);
            for(int i=0;i<renderers.Count;i++) { var r=renderers[i]; r.enabled=i<e.parts.Length; r.sortingOrder = order; r.color = tint ?? Color.white; }
        }
        public void Hide(bool hidden)
        {
            bool shouldBeActive=!hidden;
            if(gameObject.activeSelf!=shouldBeActive)gameObject.SetActive(shouldBeActive);
        }
    }
}
