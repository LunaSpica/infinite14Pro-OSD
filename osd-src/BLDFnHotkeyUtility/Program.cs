using System;
using System.Linq;
using System.Runtime.InteropServices;
using BLD.CAPSNUM;
using BLD.OSD;
using BLD.WMIOperation;
using BLD.Power;

namespace BLDFnHotkeyUtility;

internal class Program
{
	[Serializable]
	public struct MSG
	{
		public IntPtr hwnd;

		public IntPtr lParam;

		public int message;

		public int pt_x;

		public int pt_y;

		public int time;

		public IntPtr wParam;
	}

	public static FloatingOSDWindow _osd = new FloatingOSDWindow();

	private const int WMI_BASE = 1280;

	private const int WMIRGBKBLEVEL0 = 1281;

	private const int WMIRGBKBLEVEL1 = 1282;

	private const int WMIRGBKBLEVEL2 = 1283;

	private const int WMIRGBKBLEVEL3 = 1284;

	private const int BALANCE_MODE = 1285;

	private const int PERFERMANCE_MODE = 1286;

	private const int QUIET_MODE = 1287;

	private const int TOUCHPAD_STATE = 1288;

	private const int FN_STATE = 1289;

	public static byte[] StringToByteArray(string hex)
	{
		return (from x in Enumerable.Range(0, hex.Length)
			where x % 2 == 0
			select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray();
	}

	private static void Main1(string[] args)
	{
		byte[] array = new byte[32];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = 0;
		}
		if (args.Length == 0)
		{
			Console.WriteLine("Usage:\n Below is set the TouchPad ON \n BLDFnHotkeyUtility.exe 0x00 0xFA 0x00 0x0C 0x01 0x00");
			return;
		}
		for (int j = 0; j < args.Length; j++)
		{
			array[j] = Convert.ToByte(args[j], 16);
		}
		Tuple<bool, byte[]> tuple = WMIMethodServices.ExcMethod(array);
		if (!tuple.Item1)
		{
			Console.WriteLine("Error");
			return;
		}
		Console.WriteLine("Success");
		for (int k = 0; k < tuple.Item2.Length; k++)
		{
			Console.Write("0x{0:x} ", tuple.Item2[k]);
		}
		Console.WriteLine("\n===========================");
	}

	[DllImport("user32.dll")]
	public static extern bool GetMessage(out MSG lpMsg, IntPtr hWnd, uint wMsgFilterMin, uint wMsgFilterMax);

	[DllImport("user32.dll")]
	public static extern bool TranslateMessage([In] ref MSG lpMsg);

	[DllImport("user32.dll")]
	public static extern IntPtr DispatchMessage([In] ref MSG lpmsg);

	[DllImport("user32.dll")]
	public static extern bool PostMessage([Optional][In] IntPtr hWnd, [In] uint msg, [In] IntPtr wParam, [In] IntPtr lParam);

	private static void Main(string[] args)
	{
		Console.WriteLine("hello word");
		_osd.CreateWindowOnly();
		if (new WMIEventService().InitAndStart(wMIEventArrived))
		{
			Console.WriteLine("WMI EVENT Listening...");
		}
		new CAPSNUMEventService().Init_Keyboard_Event(CAPSNUMEventArrivedd);
		try
		{
			byte currentPerMode = WMIMethodServices.GetValue<byte>(WMIMethodName.SystemPerMode);
			if (currentPerMode != byte.MaxValue)
			{
				PowerModeSync.Apply((WMISystemPerMode)currentPerMode);
			}
		}
		catch
		{
		}
		MSG lpMsg;
		while (GetMessage(out lpMsg, IntPtr.Zero, 0u, 0u))
		{
			TranslateMessage(ref lpMsg);
			DispatchMessage(ref lpMsg);
		}
	}

	private static void CAPSNUMEventArrivedd(KeyBoardValue keyBoardValue, bool isOnOff)
	{
		switch (keyBoardValue)
		{
		case KeyBoardValue.Caps:
			if (isOnOff)
			{
				_osd.ShowOSD(ResourceOSD.CapsLK_ON);
			}
			else
			{
				_osd.ShowOSD(ResourceOSD.CapsLK_OFF);
			}
			break;
		case KeyBoardValue.Num:
			if (isOnOff)
			{
				_osd.ShowOSD(ResourceOSD.NumLK_ON);
			}
			else
			{
				_osd.ShowOSD(ResourceOSD.NumLK_OFF);
			}
			break;
		}
	}

	private static void wMIEventArrived(WMIEventType wMIEventType, WMIEventName wMIEventName, object eVENTvalue)
	{
		Console.WriteLine("WMIEventType: " + wMIEventType.ToString() + " wMIEventName: " + wMIEventName.ToString() + " eVENTvalue: " + eVENTvalue);
		IntPtr wParam = IntPtr.Zero;
		IntPtr zero = IntPtr.Zero;
		if (wMIEventType == WMIEventType.HotKey)
		{
			switch (wMIEventName)
			{
			case WMIEventName.RGBKeyboardBrightness:
				switch ((WMIRGBKeyboardBrightnessLevel)eVENTvalue)
				{
				case WMIRGBKeyboardBrightnessLevel.Level_0:
					PostMessage(_osd.Handle, 1281u, wParam, zero);
					break;
				case WMIRGBKeyboardBrightnessLevel.Level_1:
					PostMessage(_osd.Handle, 1282u, wParam, zero);
					break;
				case WMIRGBKeyboardBrightnessLevel.Level_2:
					PostMessage(_osd.Handle, 1283u, wParam, zero);
					break;
				case WMIRGBKeyboardBrightnessLevel.Level_3:
					PostMessage(_osd.Handle, 1284u, wParam, zero);
					break;
				}
				break;
			case WMIEventName.TouchPadState:
				switch ((WMIResultState)eVENTvalue)
				{
				case WMIResultState.OFF:
					wParam = IntPtr.Zero;
					zero = IntPtr.Zero;
					break;
				case WMIResultState.ON:
					wParam = (IntPtr)1;
					zero = IntPtr.Zero;
					break;
				}
				PostMessage(_osd.Handle, 1288u, wParam, zero);
				break;
			case WMIEventName.AmbientlightState:
				_ = (WMIResultState)eVENTvalue;
				break;
			case WMIEventName.SystemPerMode:
			{
				WMISystemPerMode wMISystemPerMode = (WMISystemPerMode)eVENTvalue;
				switch (wMISystemPerMode)
				{
				case WMISystemPerMode.BalanceMode:
					PostMessage(_osd.Handle, 1285u, wParam, zero);
					break;
				case WMISystemPerMode.PerformanceMode:
					PostMessage(_osd.Handle, 1286u, wParam, zero);
					break;
				case WMISystemPerMode.QuietMode:
					PostMessage(_osd.Handle, 1287u, wParam, zero);
					break;
				default:
					Console.WriteLine("Not support this WMISystemPerMode: " + wMISystemPerMode);
					break;
				}
				PowerModeSync.Apply(wMISystemPerMode);
				break;
			}
			case WMIEventName.FnState:
				switch ((WMIResultState)eVENTvalue)
				{
				case WMIResultState.OFF:
					wParam = IntPtr.Zero;
					zero = IntPtr.Zero;
					break;
				case WMIResultState.ON:
					wParam = (IntPtr)1;
					zero = IntPtr.Zero;
					break;
				}
				PostMessage(_osd.Handle, 1289u, wParam, zero);
				break;
			}
		}
		else
		{
			Console.WriteLine("Not support this WMIEventType: " + wMIEventType);
		}
	}
}
