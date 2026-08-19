using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Timers;
using System.Windows.Forms;
using BLDFnHotkeyUtility;

namespace BLD.OSD;

public class FloatingOSDWindow : FloatingWindow
{
	private SolidBrush _brush;

	private StringFormat _stringFormat;

	private Rectangle _rScreen;

	private System.Timers.Timer _viewClock;

	private Font _textFont;

	private string _text;

	private AnimateMode _mode;

	private uint _time;

	private GraphicsPath _gp;

	private Image _img;

	private int _showGeneration;

	private const int WM_USER = 1024;

	private const int WM_CAPSLOCK = 1025;

	private const int WM_NUMLOCK = 1026;

	private const int WM_BLENDTIMER = 1027;

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

	public void ShowOSD(Image image)
	{
		Screen primaryScreen = Screen.PrimaryScreen;
		Console.WriteLine("Width: " + primaryScreen.Bounds.Width + " Height: " + primaryScreen.Bounds.Height);
		Size size = new Size(120, 120);
		int x = primaryScreen.Bounds.Width / 2 - size.Width / 2;
		int y = Convert.ToInt32((double)(primaryScreen.Bounds.Height - size.Height) / 1.25);
		Show(new Point(x, y), byte.MaxValue, 1500, AnimateMode.ExpandCollapse, 250u, image, size);
	}

	public void HideOSD()
	{
		_showGeneration++;
		StopViewTimer();
		HideAnimate(AnimateMode.Blend, 300u);
	}

	private void StopViewTimer()
	{
		System.Timers.Timer viewClock = _viewClock;
		_viewClock = null;
		if (viewClock != null)
		{
			viewClock.Stop();
			viewClock.Dispose();
		}
	}

	private void Show(Point pt, byte alpha, Color textColor, Font textFont, int showTimeMSec, AnimateMode mode, uint time, string text, Image image)
	{
		StopViewTimer();
		int generation = ++_showGeneration;
		_brush = new SolidBrush(textColor);
		_textFont = textFont;
		_text = text;
		_img = image;
		_mode = mode;
		_time = time;
		_rScreen = Screen.PrimaryScreen.Bounds;
		if (_stringFormat == null)
		{
			_stringFormat = new StringFormat();
			_stringFormat.Alignment = StringAlignment.Near;
			_stringFormat.LineAlignment = StringAlignment.Near;
			_stringFormat.Trimming = StringTrimming.EllipsisWord;
		}
		base.Location = pt;
		base.Alpha = alpha;
		base.Size = new Size(300, 300);
		if (time != 0)
		{
			base.ShowAnimate(mode, time);
		}
		else
		{
			base.Show();
		}
		System.Timers.Timer viewClock = new System.Timers.Timer(showTimeMSec);
		viewClock.Elapsed += delegate
		{
			viewTimer(viewClock, generation, time);
		};
		viewClock.AutoReset = false;
		_viewClock = viewClock;
		viewClock.Enabled = true;
	}

	private void Show(Point pt, byte alpha, int showTimeMSec, AnimateMode mode, uint time, Image image, Size size)
	{
		StopViewTimer();
		int generation = ++_showGeneration;
		if (_img != null)
		{
			_img.Dispose();
		}
		_img = image;
		_mode = mode;
		_time = time;
		_rScreen = Screen.PrimaryScreen.Bounds;
		if (_stringFormat == null)
		{
			_stringFormat = new StringFormat();
			_stringFormat.Alignment = StringAlignment.Near;
			_stringFormat.LineAlignment = StringAlignment.Near;
			_stringFormat.Trimming = StringTrimming.EllipsisWord;
		}
		base.Location = pt;
		base.Alpha = alpha;
		base.Size = size;
		base.Show();
		System.Timers.Timer viewClock = new System.Timers.Timer(showTimeMSec);
		viewClock.Elapsed += delegate
		{
			viewTimer(viewClock, generation, time);
		};
		viewClock.AutoReset = false;
		_viewClock = viewClock;
		viewClock.Enabled = true;
	}

	protected override void PerformPaint(PaintEventArgs e)
	{
		if (base.Handle == IntPtr.Zero)
		{
			return;
		}
		Graphics graphics = e.Graphics;
		if (_gp != null)
		{
			_gp.Dispose();
		}
		graphics.SmoothingMode = SmoothingMode.HighQuality;
		try
		{
			if (_img != null)
			{
				graphics.DrawImage(_img, 0, 0, Size.Width, Size.Width);
			}
		}
		catch (Exception ex)
		{
			Console.WriteLine(ex.ToString());
		}
	}

	private void viewTimer(System.Timers.Timer viewClock, int generation, uint time)
	{
		viewClock.Stop();
		viewClock.Dispose();
		if (time != 0 && base.Handle != IntPtr.Zero)
		{
			Program.PostMessage(base.Handle, 1027u, (IntPtr)time, (IntPtr)generation);
		}
	}

	protected override void WndProc(ref Message m)
	{
		Console.WriteLine("{0}", m.ToString());
		switch (m.Msg)
		{
		case 1025:
			if (m.WParam.ToInt32() != 0)
			{
				ShowOSD(ResourceOSD.CapsLK_ON);
			}
			else
			{
				ShowOSD(ResourceOSD.CapsLK_OFF);
			}
			break;
		case 1026:
			if (m.WParam.ToInt32() != 0)
			{
				ShowOSD(ResourceOSD.NumLK_ON);
			}
			else
			{
				ShowOSD(ResourceOSD.NumLK_OFF);
			}
			break;
		case 1281:
			ShowOSD(ResourceOSD.RGBKeyboardBrightnessLevel_0);
			break;
		case 1282:
			ShowOSD(ResourceOSD.RGBKeyboardBrightnessLevel_1);
			break;
		case 1283:
			ShowOSD(ResourceOSD.RGBKeyboardBrightnessLevel_2);
			break;
		case 1284:
			ShowOSD(ResourceOSD.RGBKeyboardBrightnessLevel_3);
			break;
		case 1288:
			if (m.WParam.ToInt32() != 0)
			{
				ShowOSD(ResourceOSD.TouchPad_OFF);
			}
			else
			{
				ShowOSD(ResourceOSD.TouchPad_ON);
			}
			break;
		case 1285:
			ShowOSD(ResourceOSD.SystemPerfMode_1);
			break;
		case 1286:
			ShowOSD(ResourceOSD.SystemPerfMode_2);
			break;
		case 1287:
			ShowOSD(ResourceOSD.SystemPerfMode_0);
			break;
		case 1289:
			if (m.WParam.ToInt32() != 0)
			{
				ShowOSD(ResourceOSD.Fn_ON);
			}
			else
			{
				ShowOSD(ResourceOSD.Fn_OFF);
			}
			break;
		case 1027:
			if (m.LParam.ToInt32() == _showGeneration)
			{
				int time = m.WParam.ToInt32();
				HideAnimate(AnimateMode.Blend, (uint)time);
				if (_img != null)
				{
					_img.Dispose();
					_img = null;
				}
			}
			break;
		default:
			base.WndProc(ref m);
			break;
		}
	}
}
