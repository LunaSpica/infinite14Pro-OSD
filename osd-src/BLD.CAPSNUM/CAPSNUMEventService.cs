#define DEBUG
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using BLD.OSD;
using BLDFnHotkeyUtility;

namespace BLD.CAPSNUM;

public class CAPSNUMEventService
{
	public delegate void CAPSNUMEventArrivedHandler(KeyBoardValue keyBoardValue, bool isOnOff);

	public delegate int keyboardHookProc(int code, IntPtr wParam, IntPtr lParam);

	public enum HookType
	{
		WH_JOURNALRECORD,
		WH_JOURNALPLAYBACK,
		WH_KEYBOARD,
		WH_GETMESSAGE,
		WH_CALLWNDPROC,
		WH_CBT,
		WH_SYSMSGFILTER,
		WH_MOUSE,
		WH_HARDWARE,
		WH_DEBUG,
		WH_SHELL,
		WH_FOREGROUNDIDLE,
		WH_CALLWNDPROCRET,
		WH_KEYBOARD_LL,
		WH_MOUSE_LL
	}

	public delegate int HookProc(int nCode, IntPtr wParam, IntPtr lParam);

	private static IntPtr hHook;

	private static FloatingOSDWindow __osd = Program._osd;

	private const int WM_KEYDOWN = 256;

	private const int WM_KEYUP = 257;

	private const int VK_CAPITAL = 20;

	private const int VK_NUMLOCK = 144;

	private const int WM_USER = 1024;

	private const int WM_CAPSLOCK = 1025;

	private const int WM_NUMLOCK = 1026;

	private static keyboardHookProc callbackDelegate;

	private readonly object _stateDebounceLock = new object();

	private System.Threading.Timer _capsLockDebounceTimer;

	private System.Threading.Timer _numLockDebounceTimer;

	private int _capsLockDebounceGeneration;

	private int _numLockDebounceGeneration;

	public event CAPSNUMEventArrivedHandler _CAPSNUMEventArrived;

	[DllImport("user32.dll")]
	private static extern IntPtr SetWindowsHookEx(HookType hookType, keyboardHookProc callback, IntPtr hInstance, uint threadId);

	[DllImport("user32.dll")]
	private static extern int CallNextHookEx(IntPtr hHook, int nCode, IntPtr wParam, IntPtr lParam);

	[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
	public static extern short GetKeyState(int keyCode);

	[DllImport("user32.dll")]
	private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

	[DllImport("user32.dll")]
	public static extern bool PostMessage([Optional][In] IntPtr hWnd, [In] uint msg, [In] IntPtr wParam, [In] IntPtr lParam);

	private void ScheduleCapsLockUpdate()
	{
		bool expectedState = !CapsLockStatus();
		lock (_stateDebounceLock)
		{
			int generation = ++_capsLockDebounceGeneration;
			_capsLockDebounceTimer?.Dispose();
			_capsLockDebounceTimer = new System.Threading.Timer(delegate
			{
				CommitCapsLockUpdate(expectedState, generation);
			}, null, 25, -1);
		}
	}

	private void ScheduleNumLockUpdate()
	{
		bool expectedState = !NumLockStatus();
		lock (_stateDebounceLock)
		{
			int generation = ++_numLockDebounceGeneration;
			_numLockDebounceTimer?.Dispose();
			_numLockDebounceTimer = new System.Threading.Timer(delegate
			{
				CommitNumLockUpdate(expectedState, generation);
			}, null, 25, -1);
		}
	}

	private void CommitCapsLockUpdate(bool expectedState, int generation)
	{
		lock (_stateDebounceLock)
		{
			if (generation != _capsLockDebounceGeneration)
			{
				return;
			}
			_capsLockDebounceTimer?.Dispose();
			_capsLockDebounceTimer = null;
		}
		if (CapsLockStatus() == expectedState)
		{
			PostMessage(__osd.Handle, 1025u, expectedState ? ((IntPtr)1) : IntPtr.Zero, IntPtr.Zero);
		}
	}

	private void CommitNumLockUpdate(bool expectedState, int generation)
	{
		lock (_stateDebounceLock)
		{
			if (generation != _numLockDebounceGeneration)
			{
				return;
			}
			_numLockDebounceTimer?.Dispose();
			_numLockDebounceTimer = null;
		}
		if (NumLockStatus() == expectedState)
		{
			PostMessage(__osd.Handle, 1026u, expectedState ? ((IntPtr)1) : IntPtr.Zero, IntPtr.Zero);
		}
	}

	public int LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam)
	{
		if (nCode >= 0 && wParam == (IntPtr)256)
		{
			int num = Marshal.ReadInt32(lParam);
			Debug.WriteLine("my key is: " + num);
			if (num == 20)
			{
				ScheduleCapsLockUpdate();
			}
			if (num == 144)
			{
				ScheduleNumLockUpdate();
			}
		}
		return CallNextHookEx(hHook, nCode, wParam, lParam);
	}

	public static bool SetCapsLock()
	{
		keybd_event(20, 69, 1u, (UIntPtr)0uL);
		keybd_event(20, 69, 3u, (UIntPtr)0uL);
		return true;
	}

	public static bool SetNumLock()
	{
		keybd_event(144, 69, 1u, (UIntPtr)0uL);
		keybd_event(144, 69, 3u, (UIntPtr)0uL);
		return true;
	}

	public static bool CapsLockStatus()
	{
		return (GetKeyState(20) & 0xFFFF) != 0;
	}

	public static bool NumLockStatus()
	{
		return (GetKeyState(144) & 0xFFFF) != 0;
	}

	public void Init_Keyboard_Event(CAPSNUMEventArrivedHandler cAPSNUMEventArrived)
	{
		_CAPSNUMEventArrived += cAPSNUMEventArrived;
		uint num = 0u;
		callbackDelegate = LowLevelKeyboardProc;
		num = 0u;
		while (true)
		{
			switch (num)
			{
			default:
				return;
			case 0u:
			case 1u:
			case 2u:
			case 3u:
			case 4u:
			case 5u:
			case 6u:
			case 7u:
			case 8u:
			case 9u:
				hHook = SetWindowsHookEx(HookType.WH_KEYBOARD_LL, callbackDelegate, IntPtr.Zero, 0u);
				if (!(hHook == IntPtr.Zero))
				{
					return;
				}
				break;
			case 10u:
				MessageBox.Show("Keyboard hook not set!");
				return;
			}
			Thread.Sleep(1000);
			num++;
		}
	}
}
