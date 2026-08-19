using System;
using System.Runtime.InteropServices;

namespace BLD.OSD;

internal class Gdi32
{
	private Gdi32()
	{
	}

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern int CombineRgn(IntPtr dest, IntPtr src1, IntPtr src2, int flags);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr CreateBrushIndirect(ref LOGBRUSH brush);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr CreateCompatibleDC(IntPtr hDC);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr CreateRectRgnIndirect(ref RECT rect);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern bool DeleteDC(IntPtr hDC);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr DeleteObject(IntPtr hObject);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern int GetClipBox(IntPtr hDC, ref RECT rectBox);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern bool PatBlt(IntPtr hDC, int x, int y, int width, int height, uint flags);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern int SelectClipRgn(IntPtr hDC, IntPtr hRgn);

	[DllImport("gdi32.dll", CharSet = CharSet.Auto)]
	internal static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);
}
