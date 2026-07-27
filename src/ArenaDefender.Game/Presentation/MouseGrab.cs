using System;
using System.Runtime.InteropServices;

namespace ArenaDefender.Presentation;

/// <summary>Confines the mouse pointer to a window using SDL, which MonoGame does not expose.</summary>
internal static class MouseGrab
{
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void SetGrab(IntPtr window, int grabbed);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int SetRect(IntPtr window, ref SdlRect rect);

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate int ClearRect(IntPtr window, IntPtr rect);

    [StructLayout(LayoutKind.Sequential)]
    private struct SdlRect
    {
        public int X;
        public int Y;
        public int W;
        public int H;
    }

    private static SetGrab? _setGrab;
    private static SetRect? _setRect;
    private static ClearRect? _clearRect;
    private static bool _resolved;
    private static bool _confined;

    public static void Confine(IntPtr window, int width, int height)
    {
        if (window == IntPtr.Zero || _confined || width <= 0 || height <= 0)
        {
            return;
        }

        Resolve();
        _confined = true;

        _setGrab?.Invoke(window, 1);

        var rect = new SdlRect { W = width, H = height };
        _setRect?.Invoke(window, ref rect);
    }

    public static void Release(IntPtr window)
    {
        if (window == IntPtr.Zero || !_confined)
        {
            return;
        }

        _confined = false;
        _clearRect?.Invoke(window, IntPtr.Zero);
        _setGrab?.Invoke(window, 0);
    }

    private static void Resolve()
    {
        if (_resolved)
        {
            return;
        }

        _resolved = true;

        foreach (string name in new[] { "SDL2", "libSDL2-2.0.so.0", "SDL2.dll", "libSDL2.dylib" })
        {
            if (!NativeLibrary.TryLoad(name, out IntPtr library))
            {
                continue;
            }

            // SetWindowMouseGrab avoids the keyboard grab the older call also asks for, which some
            // window managers refuse, taking the mouse confinement down with it.
            if (NativeLibrary.TryGetExport(library, "SDL_SetWindowMouseGrab", out IntPtr grab)
                || NativeLibrary.TryGetExport(library, "SDL_SetWindowGrab", out grab))
            {
                _setGrab = Marshal.GetDelegateForFunctionPointer<SetGrab>(grab);
            }

            if (NativeLibrary.TryGetExport(library, "SDL_SetWindowMouseRect", out IntPtr rect))
            {
                _setRect = Marshal.GetDelegateForFunctionPointer<SetRect>(rect);
                _clearRect = Marshal.GetDelegateForFunctionPointer<ClearRect>(rect);
            }

            if (_setGrab is not null || _setRect is not null)
            {
                return;
            }
        }
    }
}
