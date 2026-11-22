using System;
using System.Runtime.InteropServices;
using System.Text;

namespace Mario
{
    // Keep Windows caption controls and maximize/restore, but disallow manual
    // border dragging and the system menu's Size command. Never hook the editor.
    internal static class WindowsWindowPolicy
    {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        delegate IntPtr WndProc(IntPtr hwnd,uint message,IntPtr wParam,IntPtr lParam);
        delegate bool EnumWindowProc(IntPtr window,IntPtr parameter);
        static readonly WndProc callback=WindowProcedure;
        static IntPtr hwnd,previous;
        [DllImport("user32.dll")] static extern bool EnumWindows(EnumWindowProc callback,IntPtr parameter);
        [DllImport("user32.dll",CharSet=CharSet.Unicode)] static extern int GetClassNameW(IntPtr window,StringBuilder name,int capacity);
        [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr window,out uint process);
        [DllImport("kernel32.dll")] static extern uint GetCurrentProcessId();
        [DllImport("user32.dll",EntryPoint="SetWindowLongPtrW",SetLastError=true)] static extern IntPtr SetWindowLongPtr(IntPtr window,int index,IntPtr value);
        [DllImport("user32.dll")] static extern IntPtr CallWindowProcW(IntPtr proc,IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll")] static extern bool IsWindow(IntPtr window);
        [DllImport("user32.dll")] static extern bool IsZoomed(IntPtr window);
        [DllImport("user32.dll")] static extern bool ShowWindow(IntPtr window,int command);
        [DllImport("user32.dll")] internal static extern IntPtr SendMessageW(IntPtr window,uint message,IntPtr wParam,IntPtr lParam);
        [DllImport("user32.dll")] internal static extern bool GetWindowRect(IntPtr window,out WindowRect rect);
        [StructLayout(LayoutKind.Sequential)] internal struct WindowRect {public int left,top,right,bottom;}
        internal static IntPtr Handle=>hwnd;
        internal static bool Installed=>hwnd!=IntPtr.Zero&&previous!=IntPtr.Zero&&IsWindow(hwnd);
        internal static bool Maximized=>Installed&&IsZoomed(hwnd);
        internal static void EnsureInstalled()
        {
            if(Installed)return;
            // The render window need not be active on this thread (background
            // launch, controller backend, or display-mode transition). Only
            // locate this process's actual Unity player window, never helper
            // input windows or another application's foreground window.
            IntPtr window=IntPtr.Zero;uint ownProcess=GetCurrentProcessId();
            EnumWindows((candidate,parameter)=>
            {
                GetWindowThreadProcessId(candidate,out uint process);if(process!=ownProcess)return true;
                var name=new StringBuilder(128);GetClassNameW(candidate,name,name.Capacity);
                if(name.ToString()!="UnityWndClass")return true;
                window=candidate;return false;
            },IntPtr.Zero);
            if(window==IntPtr.Zero)return;
            var old=SetWindowLongPtr(window,-4,Marshal.GetFunctionPointerForDelegate(callback));
            if(old!=IntPtr.Zero){hwnd=window;previous=old;}
        }
        static IntPtr WindowProcedure(IntPtr window,uint message,IntPtr wParam,IntPtr lParam)
        {
            if(message==0x0112&&(wParam.ToInt64()&0xfff0)==0xf000)return IntPtr.Zero; // SC_SIZE
            var result=CallWindowProcW(previous,window,message,wParam,lParam);
            if(message==0x0084&&result.ToInt64()>=10&&result.ToInt64()<=17)return new IntPtr(18); // sizing edge -> border
            return result;
        }
        internal static void Restore(){if(Installed&&IsZoomed(hwnd))ShowWindow(hwnd,9);}
        internal static void Maximize(){if(Installed)ShowWindow(hwnd,3);}
        internal static void Shutdown()
        {if(Installed&&IsWindow(hwnd))SetWindowLongPtr(hwnd,-4,previous);hwnd=previous=IntPtr.Zero;}
#else
        internal static void EnsureInstalled(){}
        internal static void Restore(){}
        internal static void Shutdown(){}
#endif
    }
}
