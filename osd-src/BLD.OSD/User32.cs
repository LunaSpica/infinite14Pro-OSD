using System;
using System.Runtime.InteropServices;

namespace BLD.OSD;

internal class User32
{
	public const uint WS_POPUP = 2147483648u;

	public const int WS_EX_TOPMOST = 8;

	public const int WS_EX_TOOLWINDOW = 128;

	public const int WS_EX_LAYERED = 524288;

	public const int WS_EX_TRANSPARENT = 32;

	public const int WS_EX_NOACTIVATE = 134217728;

	public const int SW_SHOWNOACTIVATE = 4;

	public const int SW_HIDE = 0;

	public const uint AW_HOR_POSITIVE = 1u;

	public const uint AW_HOR_NEGATIVE = 2u;

	public const uint AW_VER_POSITIVE = 4u;

	public const uint AW_VER_NEGATIVE = 8u;

	public const uint AW_CENTER = 16u;

	public const uint AW_HIDE = 65536u;

	public const uint AW_ACTIVATE = 131072u;

	public const uint AW_SLIDE = 262144u;

	public const uint AW_BLEND = 524288u;

	private User32()
	{
	}

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool AnimateWindow(IntPtr hWnd, uint dwTime, uint dwFlags);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr BeginPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool ClientToScreen(IntPtr hWnd, ref POINT pt);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool DispatchMessage(ref MSG msg);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool DrawFocusRect(IntPtr hWnd, ref RECT rect);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool EndPaint(IntPtr hWnd, ref PAINTSTRUCT ps);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr GetDC(IntPtr hWnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr GetFocus();

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern ushort GetKeyState(int virtKey);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool GetMessage(ref MSG msg, int hWnd, uint wFilterMin, uint wFilterMax);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr GetParent(IntPtr hWnd);

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern bool GetClientRect(IntPtr hWnd, [In][Out] ref RECT rect);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr GetWindow(IntPtr hWnd, int cmd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool GetWindowRect(IntPtr hWnd, ref RECT rect);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool HideCaret(IntPtr hWnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool InvalidateRect(IntPtr hWnd, ref RECT rect, bool erase);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr LoadCursor(IntPtr hInstance, uint cursor);

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern int MapWindowPoints(IntPtr hWndFrom, IntPtr hWndTo, [In][Out] ref RECT rect, int cPoints);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool MoveWindow(IntPtr hWnd, int x, int y, int width, int height, bool repaint);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool PeekMessage(ref MSG msg, int hWnd, uint wFilterMin, uint wFilterMax, uint wFlag);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool PostMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool ReleaseCapture();

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool ScreenToClient(IntPtr hWnd, ref POINT pt);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern uint SendMessage(IntPtr hWnd, int Msg, uint wParam, uint lParam);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr SetCursor(IntPtr hCursor);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr SetFocus(IntPtr hWnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int newLong);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern int SetWindowPos(IntPtr hWnd, IntPtr hWndAfter, int X, int Y, int Width, int Height, uint flags);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool redraw);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool ShowCaret(IntPtr hWnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool SetCapture(IntPtr hWnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern int ShowWindow(IntPtr hWnd, short cmdShow);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool SystemParametersInfo(uint uiAction, uint uiParam, ref int bRetValue, uint fWinINI);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool TrackMouseEvent(ref TRACKMOUSEEVENTS tme);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool TranslateMessage(ref MSG msg);

	[DllImport("User32.dll", CharSet = CharSet.Auto, SetLastError = true)]
	internal static extern bool UpdateLayeredWindow(IntPtr hwnd, IntPtr hdcDst, ref POINT pptDst, ref SIZE psize, IntPtr hdcSrc, ref POINT pprSrc, int crKey, ref BLENDFUNCTION pblend, int dwFlags);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool UpdateWindow(IntPtr hwnd);

	[DllImport("User32.dll", CharSet = CharSet.Auto)]
	internal static extern bool WaitMessage();

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern bool AdjustWindowRectEx(ref RECT lpRect, int dwStyle, bool bMenu, int dwExStyle);
}
