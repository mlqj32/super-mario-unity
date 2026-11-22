using UnityEngine;
using UnityEngine.InputSystem;

namespace Mario
{
    // One snapshot feeds menus and gameplay; modal actions consume it before
    // simulation. Gamepad controls are semantic (south/east), not device indices.
    public struct MarioInputFrame
    {
        public Vector2 direction;
        public bool accept,cancel,pause,courses,skip,tab,reverseTab,fullscreen,mute;
        public bool jump,jumpPressed,fire,firePressed,padActivity,padConnected;
        public int campaign;
    }
    public sealed partial class MarioGame
    {
        bool blockJumpUntilReleased,blockFireUntilReleased,usingGamepad,padWasConnected;
        bool PlayStationPad => Gamepad.current is UnityEngine.InputSystem.DualShock.DualShockGamepad;
        string MenuInputHint => usingGamepad?(PlayStationPad?"方向选择 叉确认 圈返回":"方向选择 A确认 B返回"):"方向/Tab选择 回车确认 ESC返回";
        string GameplayInputHint => autoRun
            ?usingGamepad?(PlayStationPad?"叉跳跃 自动加速 方/圈开火":"A跳跃 自动加速 X/B开火"):"Z/空格跳跃 自动加速 X/SHIFT开火"
            :usingGamepad?(PlayStationPad?"叉跳跃 方/圈加速 OPTIONS暂停":"A跳跃 X/B加速 START暂停"):"Z/空格跳跃 X/SHIFT加速";
        static MarioInputFrame ReadInputFrame()
        {
            var keyboard=Keyboard.current;
            bool Down(Key k)=>keyboard!=null&&keyboard[k].wasPressedThisFrame;
            bool Held(Key k)=>keyboard!=null&&keyboard[k].isPressed;
            var f=new MarioInputFrame();
            f.direction=new Vector2((Held(Key.RightArrow)||Held(Key.D)?1:0)-(Held(Key.LeftArrow)||Held(Key.A)?1:0),
                (Held(Key.UpArrow)||Held(Key.W)?1:0)-(Held(Key.DownArrow)||Held(Key.S)?1:0));
            f.jump=Held(Key.Z)||Held(Key.Space);f.jumpPressed=Down(Key.Z)||Down(Key.Space);
            f.fire=Held(Key.X)||Held(Key.LeftShift)||Held(Key.RightShift);
            f.firePressed=Down(Key.X)||Down(Key.LeftShift)||Down(Key.RightShift);
            f.accept=Down(Key.Enter)||Down(Key.NumpadEnter)||f.jumpPressed;
            f.cancel=Down(Key.Escape)||Down(Key.X);f.pause=Down(Key.Enter)||Down(Key.NumpadEnter)||Down(Key.Escape);
            f.courses=Down(Key.F1);f.skip=Down(Key.R)||f.accept;
            f.tab=Down(Key.Tab);f.reverseTab=Held(Key.LeftShift)||Held(Key.RightShift);
            f.campaign=(Down(Key.E)?1:0)-(Down(Key.Q)?1:0);
            f.fullscreen=Down(Key.F11)||(Down(Key.Enter)&&(Held(Key.LeftAlt)||Held(Key.RightAlt)));
            f.mute=Down(Key.M);
            var pad=Gamepad.current;
            if(pad!=null&&pad.added&&pad.enabled)
            {
                f.padConnected=true;
                Vector2 dpad=pad.dpad.ReadValue(),stick=pad.leftStick.ReadValue();
                Vector2 direction=dpad.sqrMagnitude>.1f?dpad:stick;
                if(f.direction.x==0)f.direction.x=direction.x;
                if(f.direction.y==0)f.direction.y=direction.y;
                bool jump=pad.buttonSouth.isPressed,fire=pad.buttonWest.isPressed||pad.buttonEast.isPressed;
                f.jump|=jump;f.jumpPressed|=pad.buttonSouth.wasPressedThisFrame;
                f.fire|=fire;f.firePressed|=pad.buttonWest.wasPressedThisFrame||pad.buttonEast.wasPressedThisFrame;
                f.accept|=pad.buttonSouth.wasPressedThisFrame;f.cancel|=pad.buttonEast.wasPressedThisFrame;
                f.pause|=pad.startButton.wasPressedThisFrame;f.courses|=pad.selectButton.wasPressedThisFrame;
                f.skip|=pad.buttonSouth.wasPressedThisFrame||pad.startButton.wasPressedThisFrame;
                f.campaign+=(pad.rightShoulder.wasPressedThisFrame?1:0)-(pad.leftShoulder.wasPressedThisFrame?1:0);
                f.padActivity=direction.sqrMagnitude>.25f||jump||fire||pad.startButton.isPressed||pad.selectButton.isPressed||pad.leftShoulder.isPressed||pad.rightShoulder.isPressed;
            }
            return f;
        }
        Controls GameplayControls(MarioInputFrame input)
        {
            if(!input.jump)blockJumpUntilReleased=false;
            if(!input.fire)blockFireUntilReleased=false;
            bool jump=input.jump&&!blockJumpUntilReleased,fire=input.fire&&!blockFireUntilReleased;
            var controls=new Controls{
                horizontal=input.direction.x>.5f?1:input.direction.x<-.5f?-1:0,
                up=input.direction.y>.5f,down=input.direction.y<-.5f,
                // Automatic running never synthesizes a fire-button press.
                jump=jump,run=autoRun||fire,
                jumpPressed=!blockJumpUntilReleased&&(input.jumpPressed||jump&&!previousJump),
                firePressed=!blockFireUntilReleased&&(input.firePressed||fire&&!previousFire)};
            previousJump=jump;previousFire=fire;return controls;
        }
        void TrackInputDevice(MarioInputFrame input)
        {
            if(input.padActivity)usingGamepad=true;
            else if(Keyboard.current!=null&&Keyboard.current.anyKey.wasPressedThisFrame)usingGamepad=false;
            if(padWasConnected&&!input.padConnected&&usingGamepad&&SimulationActive)PauseGame();
            padWasConnected=input.padConnected;
        }
    }
}
