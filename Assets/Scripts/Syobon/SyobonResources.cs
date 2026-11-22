// Adapted 2026-10-17 from SageChaozu/SyobonActionMono, commit 8a5b5f512fa94d3af1bfdac5aab804db5a7f7ace.
// Original game: Chiku. GPL-3.0; full source/license in ThirdParty/SyobonActionMono.
// Unity rendering, input, audio and editable map adapters replace XNA dependencies.
namespace Mario.Syobon
{

    using Texture2D = UnityEngine.Texture2D;
    using AudioClip = UnityEngine.AudioClip;
    public class Resources
    {
        protected SpriteFont font;

        protected GraphicDerivitation[][] grap;

        protected Texture2D[] mGrap;

        protected UnityEngine.AudioSource music,sfx;
        public bool AudioEnabled=true;
        public Resources(UnityEngine.GameObject owner){music=owner.AddComponent<UnityEngine.AudioSource>();sfx=owner.AddComponent<UnityEngine.AudioSource>();music.playOnAwake=sfx.playOnAwake=false;music.loop=true;music.volume=.4f;sfx.volume=.65f;}

        protected AudioClip[] sounds;

        public void ChangeMusic(int cue)
        {music.Stop();music.clip=sounds[cue];}

        public void Initialize()
        {
            int x1, t, i, j;

            this.font = new SpriteFont();

            this.sounds = new AudioClip[22];

            this.sounds[0] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/jump");
            this.sounds[1] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/brockbreak");
            this.sounds[2] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/coin");
            this.sounds[3] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/humi");
            this.sounds[4] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/koura");
            this.sounds[5] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/dokan");
            this.sounds[6] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/brockkinoko");
            this.sounds[7] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/powerup");
            this.sounds[8] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/kirra");
            this.sounds[9] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/goal");
            this.sounds[10] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/death");
            this.sounds[11] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/Pswitch");
            this.sounds[12] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/jumpBlock");
            this.sounds[13] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/hintBlock");
            this.sounds[14] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/4-clear");
            this.sounds[15] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/allclear");
            this.sounds[16] = UnityEngine.Resources.Load<AudioClip>("Syobon/SE/tekifire");

            this.sounds[17] = UnityEngine.Resources.Load<AudioClip>("Syobon/BGM/titerman");
            this.sounds[18] = UnityEngine.Resources.Load<AudioClip>("Syobon/BGM/spelunk");
            this.sounds[19] = UnityEngine.Resources.Load<AudioClip>("Syobon/BGM/star4");
            this.sounds[20] = UnityEngine.Resources.Load<AudioClip>("Syobon/BGM/makaimura");
            this.sounds[21] = UnityEngine.Resources.Load<AudioClip>("Syobon/BGM/puyo");

            this.mGrap = new Texture2D[51];
            this.mGrap[0] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/player");
            this.mGrap[1] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/brock");
            this.mGrap[2] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/item");
            this.mGrap[3] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/teki");
            this.mGrap[4] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/haikei");
            this.mGrap[5] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/brock2");
            this.mGrap[6] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/omake");
            this.mGrap[7] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/omake2");
            this.mGrap[30] = UnityEngine.Resources.Load<Texture2D>("Syobon/res/syobon3");

            this.grap = new GraphicDerivitation[161][];
            for (i = 0; i < 161; i++)
            {
                this.grap[i] = new GraphicDerivitation[8];
                for (j = 0; j < 8; j++)
                {
                    this.grap[i][j] = new GraphicDerivitation();
                }
            }

            this.grap[40][0].setValues(0, 0, 30, 36, 0);
            this.grap[0][0].setValues(31 * 4, 0, 30, 36, 0);
            this.grap[1][0].setValues(31 * 1, 0, 30, 36, 0);
            this.grap[2][0].setValues(31 * 2, 0, 30, 36, 0);
            this.grap[3][0].setValues(31 * 3, 0, 30, 36, 0);
            this.grap[41][0].setValues(50, 0, 51, 73, 6);

            x1 = 1;
            for (t = 0; t <= 6; t++)
            {
                this.grap[t][x1].setValues(33 * t, 0, 30, 30, x1);
                this.grap[t + 30][x1].setValues(33 * t, 33, 30, 30, x1);
                this.grap[t + 60][x1].setValues(33 * t, 66, 30, 30, x1);
            }

            this.grap[8][x1].setValues(33 * 7, 0, 30, 30, x1);
            this.grap[16][x1].setValues(33 * 6, 0, 24, 27, 2);

            x1 = 5;
            for (t = 0; t <= 6; t++)
            {
                this.grap[t][x1].setValues(33 * t, 0, 30, 30, x1);
            }

            this.grap[10][5].setValues(33 * 1, 33, 30, 30, x1);
            this.grap[11][5].setValues(33 * 2, 33, 30, 30, x1);
            this.grap[12][5].setValues(33 * 0, 66, 30, 30, x1);
            this.grap[13][5].setValues(33 * 1, 66, 30, 30, x1);
            this.grap[14][5].setValues(33 * 2, 66, 30, 30, x1);

            x1 = 2;
            for (t = 0; t <= 5; t++)
            {
                this.grap[t][x1].setValues(33 * t, 0, 30, 30, x1);
            }

            x1 = 3;
            this.grap[0][x1].setValues(33 * 0, 0, 30, 30, x1);
            this.grap[1][x1].setValues(33 * 1, 0, 30, 43, x1);
            this.grap[2][x1].setValues(33 * 2, 0, 30, 30, x1);
            this.grap[3][x1].setValues(33 * 3, 0, 30, 44, x1);
            this.grap[4][x1].setValues(33 * 4, 0, 33, 35, x1);
            this.grap[5][x1].setValues(0, 0, 37, 55, 7);
            this.grap[6][x1].setValues(38 * 2, 0, 36, 50, 7);
            this.grap[150][x1].setValues(38 * 2 + 37 * 2, 0, 36, 50, 7);
            this.grap[7][x1].setValues(33 * 6 + 1, 0, 32, 32, x1);
            this.grap[8][x1].setValues(38 * 2 + 37 * 3, 0, 37, 47, 7);
            this.grap[151][x1].setValues(38 * 3 + 37 * 3, 0, 37, 47, 7);
            this.grap[9][x1].setValues(33 * 7 + 1, 0, 26, 30, x1);
            this.grap[10][x1].setValues(214, 0, 46, 16, 6);

            this.grap[30][x1].setValues(0, 56, 30, 36, 7);
            this.grap[155][x1].setValues(31 * 3, 56, 30, 36, 7);
            this.grap[31][x1].setValues(50, 74, 49, 79, 6);

            this.grap[80][x1].setValues(151, 31, 70, 40, 4);
            this.grap[81][x1].setValues(151, 72, 70, 40, 4);
            this.grap[130][x1].setValues(151 + 71, 72, 70, 40, 4);
            this.grap[82][x1].setValues(33 * 1, 0, 30, 30, 5);
            this.grap[83][x1].setValues(0, 0, 49, 48, 6);
            this.grap[84][x1].setValues(33 * 5 + 1, 0, 30, 30, x1);
            this.grap[86][x1].setValues(102, 66, 49, 59, 6);
            this.grap[152][x1].setValues(152, 66, 49, 59, 6);

            this.grap[90][x1].setValues(102, 0, 64, 63, 6);

            this.grap[100][x1].setValues(33 * 1, 0, 30, 30, 2);
            this.grap[101][x1].setValues(33 * 7, 0, 30, 30, 2);
            this.grap[102][x1].setValues(33 * 3, 0, 30, 30, 2);

            this.grap[105][x1].setValues(33 * 5, 0, 30, 30, 2);
            this.grap[110][x1].setValues(33 * 4, 0, 30, 30, 2);

            x1 = 4;
            this.grap[0][x1].setValues(0, 0, 150, 90, x1);
            this.grap[1][x1].setValues(151, 0, 65, 29, x1);
            this.grap[2][x1].setValues(151, 31, 70, 40, x1);
            this.grap[3][x1].setValues(0, 91, 100, 90, x1);
            this.grap[4][x1].setValues(151, 113, 51, 29, x1);
            this.grap[5][x1].setValues(222, 0, 28, 60, x1);
            this.grap[6][x1].setValues(151, 143, 90, 40, x1);

            this.grap[20][x1].setValues(0, 182, 40, 60, x1);

            x1 = 5;
            this.grap[0][x1].setValues(167, 0, 45, 45, 6);
        }

        public void PlaySound(int cue)
        {if(AudioEnabled&&sounds[cue]!=null){if(cue==10)sfx.Stop();sfx.PlayOneShot(sounds[cue]);}}

        public SpriteFont returnFont()
        {
            return this.font;
        }

        public Rectangle returnFrame(int x, int y)
        {
            return this.grap[x][y].rect;
        }

        public Texture2D returnTexture(int x, int y)
        {
            return this.mGrap[this.grap[x][y].textureNumber];
        }

        public Texture2D returnTitle()
        {
            return this.mGrap[30];
        }

        public void StartMusic(){if(AudioEnabled&&music.clip!=null)music.Play();}

        public void Pause(){music.Pause();sfx.Pause();}
        public void Resume(){music.UnPause();sfx.UnPause();}
        public void Stop(){music.Stop();sfx.Stop();}
        public void Dispose(){Stop();UnityEngine.Object.DestroyImmediate(music);UnityEngine.Object.DestroyImmediate(sfx);}
        public bool Playing=>music.isPlaying||sfx.isPlaying;
        public int MusicSamples=>music.timeSamples;
        public void ValidateAssets(System.Action<string,bool,string> record)
        {
            for(int i=0;i<sounds.Length;i++)record("Syobon audio "+i,sounds[i]!=null&&sounds[i].length>0,sounds[i]==null?"missing":sounds[i].name);
            foreach(int i in new[]{0,1,2,3,4,5,6,7,30})record("Syobon atlas "+i,mGrap[i]!=null,mGrap[i]==null?"missing":mGrap[i].name);
            int invalid=0,count=0;
            foreach(var row in grap)foreach(var frame in row)
            {
                var r=frame.rect;if(r.Width==0||r.Height==0)continue;count++;
                var texture=mGrap[frame.textureNumber];
                if(texture==null||r.X<0||r.Y<0||r.X+r.Width>texture.width||r.Y+r.Height>texture.height)invalid++;
            }
            record("Syobon source sprite rectangles",invalid==0,"frames="+count+" invalid="+invalid);
        }
        public void StopMusic()
        {
            this.music.Stop();
        }
    }

    public class GraphicDerivitation
    {
        public Rectangle rect;

        public int textureNumber;

        public void setValues(int x, int y, int width, int height, int TextureNumber)
        {
            this.rect = new Rectangle(x, y, width, height);
            this.textureNumber = TextureNumber;
        }
    }
}
