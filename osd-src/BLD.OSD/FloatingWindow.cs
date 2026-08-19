using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using BLD.WMIOperation;

namespace BLD.OSD;

public class FloatingWindow : NativeWindow, IDisposable
{
	public enum AnimateMode
	{
		Blend,
		SlideRightToLeft,
		SlideLeftToRight,
		SlideTopToBottom,
		SlideBottmToTop,
		RollRightToLeft,
		RollLeftToRight,
		RollTopToBottom,
		RollBottmToTop,
		ExpandCollapse
	}

	private bool _disposed;

	private byte _alpha = 250;

	private Size _size = new Size(400, 400);

	private Point _location = new Point(50, 50);

	private bool _isVisible;

	private Timer _refreshTimer;

	private Timer _healthTimer;

	private DateTime _lastHealthCheckUtc;

	private int _refreshAttempts;

	private bool _recreateOnRefresh;

	private string _refreshReason;

	private const int MaxRefreshAttempts = 5;

	private const int HealthCheckInterval = 1000;

	private const int MessageLoopGapThreshold = 5000;

	private const int WM_DISPLAYCHANGE = 126;

	private const int WM_SETTINGCHANGE = 26;

	private const int WM_DPICHANGED = 736;

	private const int WM_POWERBROADCAST = 536;

	private const int PBT_APMRESUMEAUTOMATIC = 18;

	private const int PBT_APMRESUMESUSPEND = 7;

	private const int PBT_APMRESUMECRITICAL = 6;

	public virtual Point Location
	{
		get
		{
			return _location;
		}
		set
		{
			if (base.Handle != IntPtr.Zero)
			{
				SetBoundsCore(value.X, value.Y, _size.Width, _size.Height);
				RECT rect = default(RECT);
				User32.GetWindowRect(base.Handle, ref rect);
				_location = new Point(rect.left, rect.top);
				UpdateLayeredWindow();
			}
			else
			{
				_location = value;
			}
		}
	}

	public virtual Size Size
	{
		get
		{
			return _size;
		}
		set
		{
			if (base.Handle != IntPtr.Zero)
			{
				SetBoundsCore(_location.X, _location.Y, value.Width, value.Height);
				RECT rect = default(RECT);
				User32.GetWindowRect(base.Handle, ref rect);
				_size = new Size(rect.right - rect.left, rect.bottom - rect.top);
				UpdateLayeredWindow();
			}
			else
			{
				_size = value;
			}
		}
	}

	public int Height
	{
		get
		{
			return _size.Height;
		}
		set
		{
			_size = new Size(_size.Width, value);
		}
	}

	public int Width
	{
		get
		{
			return _size.Width;
		}
		set
		{
			_size = new Size(value, _size.Height);
		}
	}

	public int X
	{
		get
		{
			return _location.X;
		}
		set
		{
			Location = new Point(value, Location.Y);
		}
	}

	public int Y
	{
		get
		{
			return _location.Y;
		}
		set
		{
			Location = new Point(Location.X, value);
		}
	}

	public Rectangle Bound => new Rectangle(new Point(0, 0), _size);

	public byte Alpha
	{
		get
		{
			return _alpha;
		}
		set
		{
			if (_alpha != value)
			{
				_alpha = value;
				UpdateLayeredWindow();
			}
		}
	}

	protected virtual void PerformPaint(PaintEventArgs e)
	{
		using (LinearGradientBrush brush = new LinearGradientBrush(Bound, Color.LightBlue, Color.DarkGoldenrod, 45f))
		{
			e.Graphics.FillRectangle(brush, Bound);
		}
		e.Graphics.DrawString("Overide this PerformPaint method...", new Font(FontFamily.GenericSansSerif, 12f, FontStyle.Regular), new SolidBrush(Color.FromArgb(170, Color.Red)), new PointF(0f, 10f));
	}

	protected internal void Invalidate()
	{
		UpdateLayeredWindow();
	}

	private bool UpdateLayeredWindow()
	{
		if (base.Handle == IntPtr.Zero || Size.Width <= 0 || Size.Height <= 0)
		{
			return false;
		}
		using Bitmap bitmap = new Bitmap(Size.Width, Size.Height, PixelFormat.Format32bppArgb);
		using Graphics graphics = Graphics.FromImage(bitmap);
		Rectangle clipRect = new Rectangle(0, 0, Size.Width, Size.Height);
		PerformPaint(new PaintEventArgs(graphics, clipRect));
		IntPtr dC = User32.GetDC(IntPtr.Zero);
		if (dC == IntPtr.Zero)
		{
			return false;
		}
		IntPtr intPtr = Gdi32.CreateCompatibleDC(dC);
		if (intPtr == IntPtr.Zero)
		{
			User32.ReleaseDC(IntPtr.Zero, dC);
			return false;
		}
		IntPtr intPtr2 = IntPtr.Zero;
		IntPtr intPtr3 = IntPtr.Zero;
		try
		{
			intPtr2 = bitmap.GetHbitmap(Color.FromArgb(0));
			intPtr3 = Gdi32.SelectObject(intPtr, intPtr2);
			SIZE psize = default(SIZE);
			psize.cx = Size.Width;
			psize.cy = Size.Height;
			POINT pptDst = default(POINT);
			pptDst.x = Location.X;
			pptDst.y = Location.Y;
			POINT pprSrc = default(POINT);
			BLENDFUNCTION pblend = default(BLENDFUNCTION);
			pblend.BlendOp = 0;
			pblend.BlendFlags = 0;
			pblend.SourceConstantAlpha = _alpha;
			pblend.AlphaFormat = 1;
			bool flag = User32.UpdateLayeredWindow(base.Handle, dC, ref pptDst, ref psize, intPtr, ref pprSrc, 0, ref pblend, 2);
			if (!flag)
			{
				int lastWin32Error = Marshal.GetLastWin32Error();
				LogWindowEvent("UpdateLayeredWindow failed for HWND 0x" + base.Handle.ToInt64().ToString("X") + ", error " + lastWin32Error + ".", EventLogEntryType.Warning);
			}
			return flag;
		}
		finally
		{
			if (intPtr3 != IntPtr.Zero)
			{
				Gdi32.SelectObject(intPtr, intPtr3);
			}
			if (intPtr2 != IntPtr.Zero)
			{
				Gdi32.DeleteObject(intPtr2);
			}
			Gdi32.DeleteDC(intPtr);
			User32.ReleaseDC(IntPtr.Zero, dC);
		}
	}

	public virtual void Show()
	{
		if (base.Handle == IntPtr.Zero)
		{
			CreateWindowOnly();
		}
		if (!UpdateLayeredWindow())
		{
			RecreateLayeredWindow("show refresh failed");
		}
		if (base.Handle != IntPtr.Zero)
		{
			User32.ShowWindow(base.Handle, 4);
			_isVisible = true;
		}
	}

	public virtual void Show(int x, int y)
	{
		_location.X = x;
		_location.Y = y;
		Show();
	}

	public virtual void ShowAnimate(AnimateMode mode, uint time)
	{
		uint num = 0u;
		switch (mode)
		{
		case AnimateMode.Blend:
			num = 524288u;
			break;
		case AnimateMode.ExpandCollapse:
			num = 16u;
			break;
		case AnimateMode.SlideLeftToRight:
			num = 262145u;
			break;
		case AnimateMode.SlideRightToLeft:
			num = 262146u;
			break;
		case AnimateMode.SlideTopToBottom:
			num = 262148u;
			break;
		case AnimateMode.SlideBottmToTop:
			num = 262152u;
			break;
		case AnimateMode.RollLeftToRight:
			num = 1u;
			break;
		case AnimateMode.RollRightToLeft:
			num = 2u;
			break;
		case AnimateMode.RollBottmToTop:
			num = 8u;
			break;
		case AnimateMode.RollTopToBottom:
			num = 4u;
			break;
		}
		if (base.Handle == IntPtr.Zero)
		{
			CreateWindowOnly();
		}
		if ((num & 0x80000u) != 0)
		{
			AnimateWithBlend(show: true, time);
		}
		else
		{
			User32.AnimateWindow(base.Handle, time, num);
		}
		_isVisible = true;
	}

	public virtual void ShowAnimate(int x, int y, AnimateMode mode, uint time)
	{
		_location.X = x;
		_location.Y = y;
		ShowAnimate(mode, time);
	}

	public virtual void Hide()
	{
		if (!(base.Handle == IntPtr.Zero))
		{
			User32.ShowWindow(base.Handle, 0);
			_isVisible = false;
		}
	}

	public virtual void HideAnimate(AnimateMode mode, uint time)
	{
		if (!(base.Handle == IntPtr.Zero))
		{
			uint num = 0u;
			switch (mode)
			{
			case AnimateMode.Blend:
				num = 524288u;
				break;
			case AnimateMode.ExpandCollapse:
				num = 16u;
				break;
			case AnimateMode.SlideLeftToRight:
				num = 262145u;
				break;
			case AnimateMode.SlideRightToLeft:
				num = 262146u;
				break;
			case AnimateMode.SlideTopToBottom:
				num = 262148u;
				break;
			case AnimateMode.SlideBottmToTop:
				num = 262152u;
				break;
			case AnimateMode.RollLeftToRight:
				num = 1u;
				break;
			case AnimateMode.RollRightToLeft:
				num = 2u;
				break;
			case AnimateMode.RollBottmToTop:
				num = 8u;
				break;
			case AnimateMode.RollTopToBottom:
				num = 4u;
				break;
			}
			num |= 0x10000u;
			if ((num & 0x80000u) != 0)
			{
				AnimateWithBlend(show: false, time);
			}
			else
			{
				User32.AnimateWindow(base.Handle, time, num);
			}
			Hide();
		}
	}

	public virtual void Close()
	{
		Hide();
		Dispose();
	}

	private void AnimateWithBlend(bool show, uint time)
	{
		byte alpha = _alpha;
		byte b = (byte)(alpha / (time / 10));
		if (b == 0)
		{
			b++;
		}
		if (show)
		{
			_alpha = 0;
			UpdateLayeredWindow();
			User32.ShowWindow(base.Handle, 4);
		}
		byte b2 = (byte)((!show) ? alpha : 0);
		while (show ? (b2 <= alpha) : (b2 >= 0))
		{
			_alpha = b2;
			UpdateLayeredWindow();
			if ((show && b2 > alpha - b) || (!show && b2 < b))
			{
				_alpha = 0;
				UpdateLayeredWindow();
				break;
			}
			b2 += (byte)(b * (show ? 1 : (-1)));
		}
		_alpha = alpha;
		if (show)
		{
			UpdateLayeredWindow();
		}
	}

	public void CreateWindowOnly()
	{
		if (!(base.Handle != IntPtr.Zero))
		{
			_disposed = false;
			CreateParams createParams = new CreateParams();
			createParams.Caption = "FloatingNativeWindow";
			int num = _location.X;
			int num2 = _location.Y;
			Screen screen = Screen.FromPoint(_location);
			if (num + _size.Width > screen.Bounds.Width)
			{
				num = screen.Bounds.Width - _size.Width;
			}
			if (num2 + _size.Height > screen.Bounds.Height)
			{
				num2 = screen.Bounds.Height - _size.Height;
			}
			_location = new Point(num, num2);
			Size size = _size;
			_ = _location;
			createParams.X = num;
			createParams.Y = num2;
			createParams.Height = size.Height;
			createParams.Width = size.Width;
			createParams.Parent = IntPtr.Zero;
			uint style = 2147483648u;
			createParams.Style = (int)style;
			createParams.ExStyle = 134742184;
			CreateHandle(createParams);
			UpdateLayeredWindow();
			StartHealthMonitor();
		}
	}

	private void PerformWmPaint_WmPrintClient(ref Message m, bool isPaintMessage)
	{
		PAINTSTRUCT ps = default(PAINTSTRUCT);
		IntPtr hdc = (isPaintMessage ? User32.BeginPaint(m.HWnd, ref ps) : m.WParam);
		RECT rect = default(RECT);
		User32.GetWindowRect(base.Handle, ref rect);
		Rectangle clipRect = new Rectangle(0, 0, rect.right - rect.left, rect.bottom - rect.top);
		using (Graphics graphics2 = Graphics.FromHdc(hdc))
		{
			using Bitmap image = new Bitmap(clipRect.Width, clipRect.Height);
			using (Graphics graphics = Graphics.FromImage(image))
			{
				PerformPaint(new PaintEventArgs(graphics, clipRect));
			}
			graphics2.DrawImageUnscaled(image, 0, 0);
		}
		if (isPaintMessage)
		{
			User32.EndPaint(m.HWnd, ref ps);
		}
	}

	protected override void WndProc(ref Message m)
	{
		if (m.Msg == 126 || m.Msg == 26 || m.Msg == 736)
		{
			base.WndProc(ref m);
			ScheduleLayeredWindowRefresh(recreateHandle: true, "display configuration change", 250);
		}
		else if (m.Msg == 536)
		{
			base.WndProc(ref m);
			int num = m.WParam.ToInt32();
			if (num == 18 || num == 7 || num == 6)
			{
				ScheduleLayeredWindowRefresh(recreateHandle: true, "power resume " + num, 500);
			}
		}
		else if (m.Msg == 15)
		{
			PerformWmPaint_WmPrintClient(ref m, isPaintMessage: true);
			Console.WriteLine("WM_PAINT");
		}
		else if (m.Msg == 792)
		{
			PerformWmPaint_WmPrintClient(ref m, isPaintMessage: false);
			Console.WriteLine("unknow message: 0x318");
		}
		else
		{
			Console.WriteLine("{0}", m.ToString());
			base.WndProc(ref m);
		}
	}

	private void ScheduleLayeredWindowRefresh(bool recreateHandle, string reason, int delay)
	{
		_refreshAttempts = 0;
		_recreateOnRefresh = recreateHandle;
		_refreshReason = reason;
		ArmRefreshTimer(delay);
	}

	private void StartHealthMonitor()
	{
		if (_healthTimer != null)
		{
			return;
		}
		_lastHealthCheckUtc = DateTime.UtcNow;
		_healthTimer = new Timer();
		_healthTimer.Interval = 1000;
		_healthTimer.Tick += delegate
		{
			DateTime utcNow = DateTime.UtcNow;
			TimeSpan timeSpan = utcNow - _lastHealthCheckUtc;
			_lastHealthCheckUtc = utcNow;
			if (timeSpan.TotalMilliseconds >= 5000.0)
			{
				ScheduleLayeredWindowRefresh(recreateHandle: true, "message loop resumed after " + Convert.ToInt32(timeSpan.TotalSeconds) + " seconds", 50);
			}
		};
		_healthTimer.Start();
	}

	private void ArmRefreshTimer(int delay)
	{
		if (_refreshTimer == null)
		{
			_refreshTimer = new Timer();
			_refreshTimer.Tick += delegate
			{
				_refreshTimer.Stop();
				RunScheduledLayeredWindowRefresh();
			};
		}
		_refreshTimer.Interval = delay;
		_refreshTimer.Stop();
		_refreshTimer.Start();
	}

	private void RunScheduledLayeredWindowRefresh()
	{
		if (_disposed)
		{
			return;
		}
		bool recreateOnRefresh = _recreateOnRefresh;
		string refreshReason = _refreshReason;
		_recreateOnRefresh = false;
		if (recreateOnRefresh ? RecreateLayeredWindow(refreshReason) : RefreshLayeredWindow())
		{
			_refreshAttempts = 0;
			return;
		}
		_refreshAttempts++;
		if (_refreshAttempts < 5)
		{
			_recreateOnRefresh = _refreshAttempts >= 2;
			_refreshReason = refreshReason + " retry " + _refreshAttempts;
			ArmRefreshTimer(500);
			return;
		}
		LogWindowEvent("Layered window recovery gave up after " + _refreshAttempts + " attempts (" + refreshReason + ").", EventLogEntryType.Error);
	}

	private bool RefreshLayeredWindow()
	{
		if (base.Handle == IntPtr.Zero)
		{
			CreateWindowOnly();
		}
		bool result = UpdateLayeredWindow();
		if (_isVisible && base.Handle != IntPtr.Zero)
		{
			User32.ShowWindow(base.Handle, 4);
		}
		return result;
	}

	private bool RecreateLayeredWindow(string reason)
	{
		bool isVisible = _isVisible;
		IntPtr intPtr = base.Handle;
		if (intPtr != IntPtr.Zero)
		{
			User32.ShowWindow(intPtr, 0);
			DestroyHandle();
		}
		CreateWindowOnly();
		bool flag = UpdateLayeredWindow();
		if (isVisible && base.Handle != IntPtr.Zero)
		{
			User32.ShowWindow(base.Handle, 4);
		}
		_isVisible = isVisible;
		LogWindowEvent("Recreated layered window for " + reason + ": HWND 0x" + intPtr.ToInt64().ToString("X") + " -> 0x" + base.Handle.ToInt64().ToString("X") + ", refreshed=" + flag + ".", flag ? EventLogEntryType.Information : EventLogEntryType.Warning);
		return flag;
	}

	private static void LogWindowEvent(string message, EventLogEntryType entryType)
	{
		try
		{
			EventLogHelper._EventLog.WriteEntry("OSD window: " + message, entryType);
		}
		catch
		{
		}
	}

	protected virtual void SetBoundsCore(int x, int y, int width, int height)
	{
		if (X == x && Y == y && Width == width && Height == height)
		{
			return;
		}
		if (base.Handle != IntPtr.Zero)
		{
			int num = 20;
			if (X == x && Y == y)
			{
				num |= 2;
			}
			if (Width == width && Height == height)
			{
				num |= 1;
			}
			User32.SetWindowPos(base.Handle, IntPtr.Zero, x, y, width, height, (uint)num);
		}
		else
		{
			Location = new Point(x, y);
			Size = new Size(width, height);
		}
	}

	public void Dispose()
	{
		if (_healthTimer != null)
		{
			_healthTimer.Stop();
			_healthTimer.Dispose();
			_healthTimer = null;
		}
		if (_refreshTimer != null)
		{
			_refreshTimer.Stop();
			_refreshTimer.Dispose();
			_refreshTimer = null;
		}
		if (base.Handle != IntPtr.Zero)
		{
			DestroyHandle();
		}
		_disposed = true;
	}

	private void Dispose(bool disposing)
	{
		if (!_disposed)
		{
			DestroyHandle();
			_disposed = true;
		}
	}
}
