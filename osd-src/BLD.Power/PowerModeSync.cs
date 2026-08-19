using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Microsoft.Win32;
using BLD.WMIOperation;

namespace BLD.Power;

// OSD 性能档位 -> Windows 电源模式同步。
// OSD 由 LocalSystem 服务启动，可直接写 SYSTEM-only 的电源模式注册表值；
// 立即生效通过同目录下的 x64 助手 BLDPowerModeHelper.exe 调用 PowerSetActiveOverlayScheme
// (该 API 的 32 位实现在本机会导致访问违规，OSD 为 x86，因此必须经 x64 进程调用)。
public static class PowerModeSync
{
	private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
	private const string AcValueName = "ActiveOverlayAcPowerScheme";
	private const string DcValueName = "ActiveOverlayDcPowerScheme";
	private const string ActiveSchemeValueName = "ActivePowerScheme";
	private const string HelperFileName = "BLDPowerModeHelper.exe";

	private static readonly Guid BestEfficiency = new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a");
	private static readonly Guid Balanced = Guid.Empty;
	private static readonly Guid BestPerformance = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");

	public static void Apply(WMISystemPerMode mode)
	{
		Guid overlay;
		string name;
		switch (mode)
		{
		case WMISystemPerMode.PerformanceMode:
			overlay = BestPerformance;
			name = "最佳性能";
			break;
		case WMISystemPerMode.BalanceMode:
			overlay = Balanced;
			name = "平衡";
			break;
		case WMISystemPerMode.QuietMode:
			overlay = BestEfficiency;
			name = "最佳能效";
			break;
		default:
			Log("忽略未知的 SystemPerMode: " + mode, EventLogEntryType.Warning);
			return;
		}
		try
		{
			// 同时写入 AC/DC 两套记忆
			WriteGuidValue(AcValueName, overlay);
			WriteGuidValue(DcValueName, overlay);
			Guid scheme = ReadActiveSchemeGuid();
			uint ret = RunHelper(overlay, scheme);
			Log("SystemPerMode=" + mode + " -> Windows 电源模式 '" + name + "' (overlay " + overlay.ToString("D") + ", scheme " + scheme.ToString("D") + ", helper ret " + ret + ")", EventLogEntryType.Information);
		}
		catch (Exception ex)
		{
			Log("同步 Windows 电源模式失败 (SystemPerMode=" + mode + "): " + ex.Message, EventLogEntryType.Warning);
		}
	}

	private static uint RunHelper(Guid overlay, Guid scheme)
	{
		string dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
		string helper = Path.Combine(dir ?? ".", HelperFileName);
		if (!File.Exists(helper))
		{
			Log("助手程序不存在: " + helper, EventLogEntryType.Warning);
			return uint.MaxValue;
		}
		using (Process p = new Process())
		{
			p.StartInfo = new ProcessStartInfo(helper, overlay.ToString("D") + " " + scheme.ToString("D"));
			p.StartInfo.UseShellExecute = false;
			p.StartInfo.CreateNoWindow = true;
			if (!p.Start())
			{
				return uint.MaxValue;
			}
			if (!p.WaitForExit(5000))
			{
				p.Kill();
				return uint.MaxValue;
			}
			return (uint)p.ExitCode;
		}
	}

	private static void WriteGuidValue(string valueName, Guid value)
	{
		using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: true))
		{
			if (key == null)
			{
				throw new InvalidOperationException("无法打开注册表键 " + KeyPath);
			}
			key.SetValue(valueName, value.ToString("D"), RegistryValueKind.String);
		}
	}

	private static Guid ReadActiveSchemeGuid()
	{
		using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyPath, writable: false))
		{
			string raw = key?.GetValue(ActiveSchemeValueName) as string;
			Guid guid;
			if (raw != null && Guid.TryParse(raw, out guid))
			{
				return guid;
			}
		}
		return new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
	}

	private static void Log(string message, EventLogEntryType entryType)
	{
		try
		{
			EventLogHelper._EventLog.WriteEntry("PowerModeSync: " + message, entryType);
		}
		catch
		{
		}
	}
}
