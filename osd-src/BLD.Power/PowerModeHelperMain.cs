using System;
using System.Runtime.InteropServices;

// x64 助手: 为 x86 的 OSD 调用 PowerSetActiveOverlayScheme 使电源模式立即生效。
// 参数: <overlayGuid> <schemeGuid>; 退出码即 API 返回码。
internal static class PowerModeHelperMain
{
	[DllImport("powrprof.dll")]
	private static extern uint PowerSetActiveOverlayScheme(Guid overlaySchemeGuid, Guid schemeGuid);

	private static int Main(string[] args)
	{
		try
		{
			if (args == null || args.Length < 2)
			{
				return 100;
			}
			Guid overlay;
			Guid scheme;
			if (!Guid.TryParse(args[0], out overlay) || !Guid.TryParse(args[1], out scheme))
			{
				return 101;
			}
			return (int)PowerSetActiveOverlayScheme(overlay, scheme);
		}
		catch
		{
			return 200;
		}
	}
}
