using System;

namespace Mario
{
    [Serializable] public struct Controls
    {
        public int horizontal; public bool jump, jumpPressed, run, firePressed, down, up;
    }
    /// <summary>
    /// Fixed-step, integer subpixel movement. Horizontal velocity uses 1/4096 px/frame;
    /// vertical velocity uses 1/256 px/frame. Physics tables follow the SMB1 NTSC listing.
    /// Optional responsive horizontal control; jump intent assistance lives in MarioGame.
    /// </summary>
    [Serializable] public sealed class ClassicMotor
    {
        public const double Frequency = 60.0988138974405;
        public const int WalkSpeed = 24 * 256, RunSpeed = 40 * 256;
        public int x4096, y256, vx4096, vy256;
        public int facing = 1, movingDirection = 1, runningSpeed, runTimer, jumpGravity = 32, fallGravity = 112;
        public bool grounded, swimming, crouched, jumpCut;
        public bool responsive;
        public int jumpOrigin, minimumJump = 1;
        static readonly int[] Rising={32,32,30,40,40}, Falling={112,112,96,144,144};
        public float X { get => x4096 / 4096f; set => x4096 = (int)Math.Round(value * 4096); }
        public float Y { get => y256 / 256f; set => y256 = (int)Math.Round(value * 256); }
        public float VX => (vx4096 >> 8) / 16f;
        public float VY => vy256 / 256f;
        public void LaunchSpring(bool boosted)
        {
            // Red spring reference forces: 7px normal, 12px boosted, gravity 0x70.
            // Reset the previous fall's cut/origin so it cannot weaken this launch.
            vy256=(boosted?12:7)*256;jumpGravity=fallGravity=112;
            jumpOrigin=y256;jumpCut=false;grounded=false;
        }
        public bool TryJump(bool allowCoyote=false)
        {
            if(!grounded&&!swimming&&!allowCoyote)return false;
            int abs=Math.Abs(vx4096>>8);
            int index=abs<=8?0:abs<16?1:abs<=24?2:abs<28?3:4;
            jumpGravity=swimming?13:Rising[index];fallGravity=swimming?10:Falling[index];
            vy256=swimming?384:index<3?1024:1280;
            jumpOrigin=y256;grounded=false;jumpCut=false;return true;
        }
        void ResponsiveHorizontal(Controls input)
        {
            if(input.horizontal!=0)facing=input.horizontal;
            int direction=crouched&&grounded?0:input.horizontal;
            int target=direction*(input.run?RunSpeed:WalkSpeed);
            bool reversing=direction!=0&&vx4096!=0&&Math.Sign(vx4096)!=direction;
            // Brake independently of acceleration: run speed is useful for gaps,
            // but must not impose a long slide when correcting a landing.
            int ticks;
            bool braking=reversing||direction==0||Math.Abs(vx4096)>Math.Abs(target);
            if(reversing){target=0;ticks=grounded?4:6;}
            else if(braking)ticks=grounded?6:9;
            else ticks=grounded?18:24;
            int rate=braking?RunSpeed:Math.Abs(target);
            int step=(rate+ticks-1)/ticks;
            vx4096=target>vx4096?Math.Min(target,vx4096+step):Math.Max(target,vx4096-step);
        }
        public void Step(Controls input)
        {
            int moving = movingDirection;
            int abs = Math.Abs(vx4096 >> 8);
            if (runTimer > 0) runTimer--;
            bool same = input.horizontal == moving;
            if(input.jumpPressed)TryJump();
            if(responsive&&!swimming)ResponsiveHorizontal(input);
            else
            {
            if (grounded && input.run && same && !swimming) runTimer = 10;
            bool running = !swimming && (grounded ? same && runTimer > 0 : abs > 24);
            int cap = swimming && grounded ? 16 : running ? 40 : 24;
            int friction = running ? 0xE4 : runningSpeed!=0 || abs > 0x20 ? 0xD0 : 0x98;
            // Facing is held during a land jump; braking therefore keeps the original air inertia.
            if (facing != moving) friction *= 2;
            if (grounded || swimming)
            {
                if(abs>=28)runningSpeed=abs;
                else if(input.horizontal!=0||input.run||input.down||input.up)
                {
                    if(same)runningSpeed=0;
                    else if(abs<11){movingDirection=facing;vx4096=0;}
                }
                if (input.horizontal != 0) facing = input.horizontal;
            }
            int dir = crouched && grounded ? 0 : input.horizontal;
            if (dir != 0)
            {
                vx4096 += dir * friction;
                int speedByte=vx4096>>8;
                if(dir>0&&speedByte>=cap)vx4096=(cap<<8)|(vx4096&255);
                if(dir<0&&speedByte < -cap)vx4096=(-cap<<8)|(vx4096&255);
            }
            else if (grounded)
            {
                int speedByte=vx4096>>8,oldSpeed= speedByte;
                if(speedByte!=0)vx4096-=Math.Sign(speedByte)*friction;
                speedByte=vx4096>>8;
                if(oldSpeed<0&&speedByte>=cap)vx4096=(cap<<8)|(vx4096&255);
                if(oldSpeed>0&&speedByte < -cap)vx4096=(-cap<<8)|(vx4096&255);
            }
            }
            // The NES position accumulator sees the signed velocity byte, not the
            // low byte accumulating friction. Preserve that 1/16-pixel quantization.
            x4096 += (vx4096 >> 8) * 256;
            if((vx4096>>8)!=0)movingDirection=vx4096>0?1:-1;
            if (!grounded)
            {
                y256 += vy256;
                bool released = !input.jump && y256 - jumpOrigin >= minimumJump * 256;
                if(released || vy256<=0)jumpCut=true;
                int gravity = !jumpCut ? jumpGravity : fallGravity;
                vy256 -= gravity;
                // The original terminal-speed check clamps only after the fractional
                // byte reaches 0x80. Its descent alternates between 4 and 4.x px/frame.
                int down=-vy256;
                if((down>>8)>=4&&(down&255)>=128)vy256=-1024;
            }
        }
    }
}
