using System;

namespace BLD.OSD;

internal struct TRACKMOUSEEVENTS
{
	public uint cbSize;

	public uint dwFlags;

	public IntPtr hWnd;

	public uint dwHoverTime;
}
