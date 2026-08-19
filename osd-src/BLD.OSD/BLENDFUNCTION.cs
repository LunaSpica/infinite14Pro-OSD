using System.Runtime.InteropServices;

namespace BLD.OSD;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
internal struct BLENDFUNCTION
{
	public byte BlendOp;

	public byte BlendFlags;

	public byte SourceConstantAlpha;

	public byte AlphaFormat;
}
