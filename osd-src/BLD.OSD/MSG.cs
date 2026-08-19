using System;

namespace BLD.OSD;

internal struct MSG
{
	public IntPtr hwnd;

	public int message;

	public IntPtr wParam;

	public IntPtr lParam;

	public int time;

	public int pt_x;

	public int pt_y;
}
