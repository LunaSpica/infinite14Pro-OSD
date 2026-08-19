using System;
using System.Drawing;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace PowerModeTester
{
    internal static class Program
    {
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    internal enum PowerModeLevel
    {
        BestEfficiency,
        Balanced,
        BestPerformance
    }

    internal static class SysImpersonation
    {
        private const uint PROCESS_QUERY_INFORMATION = 0x0400;
        private const uint TOKEN_DUPLICATE = 0x0002;
        private const uint TOKEN_QUERY = 0x0008;
        private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
        private const uint MAXIMUM_ALLOWED = 0x02000000;
        private const uint SE_PRIVILEGE_ENABLED = 0x0002;
        private const int SECURITY_IMPERSONATION = 2;
        private const int TOKEN_IMPERSONATION = 2;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr OpenProcess(uint access, bool inherit, uint pid);

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetCurrentProcess();

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool OpenProcessToken(IntPtr processHandle, uint desiredAccess, out IntPtr tokenHandle);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool LookupPrivilegeValue(string systemName, string name, out long luid);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool AdjustTokenPrivileges(IntPtr tokenHandle, bool disableAll, ref TOKEN_PRIVILEGES newState, uint bufferLength, IntPtr previousState, IntPtr returnLength);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool DuplicateTokenEx(IntPtr existingToken, uint desiredAccess, IntPtr tokenAttributes, int impersonationLevel, int tokenType, out IntPtr newToken);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool ImpersonateLoggedOnUser(IntPtr token);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern bool RevertToSelf();

        [StructLayout(LayoutKind.Sequential)]
        private struct LUID { public uint LowPart; public int HighPart; }

        [StructLayout(LayoutKind.Sequential)]
        private struct TOKEN_PRIVILEGES { public uint PrivilegeCount; public LUID Luid; public uint Attributes; }

        public static bool EnableSeDebugPrivilege()
        {
            IntPtr tok;
            if (!OpenProcessToken(GetCurrentProcess(), TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tok))
            {
                return false;
            }
            long luid;
            if (!LookupPrivilegeValue(null, "SeDebugPrivilege", out luid))
            {
                return false;
            }
            TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
            tp.PrivilegeCount = 1;
            tp.Luid.LowPart = (uint)(luid & 0xffffffff);
            tp.Luid.HighPart = (int)(luid >> 32);
            tp.Attributes = SE_PRIVILEGE_ENABLED;
            return AdjustTokenPrivileges(tok, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero) && Marshal.GetLastWin32Error() != 1300;
        }

        public static string RunAsSystem(Action action)
        {
            string[] candidates = { "winlogon", "services", "lsass", "spoolsv", "SearchIndexer" };
            string err = "未找到可用的 SYSTEM 进程";
            foreach (string cn in candidates)
            {
                try
                {
                    Process[] ps = Process.GetProcessesByName(cn);
                    if (ps.Length == 0)
                    {
                        continue;
                    }
                    IntPtr h = OpenProcess(PROCESS_QUERY_INFORMATION, false, (uint)ps[0].Id);
                    if (h == IntPtr.Zero)
                    {
                        err = "OpenProcess " + cn + " 失败 " + Marshal.GetLastWin32Error();
                        continue;
                    }
                    IntPtr srcTok;
                    if (!OpenProcessToken(h, TOKEN_DUPLICATE | TOKEN_QUERY, out srcTok))
                    {
                        err = "OpenProcessToken " + cn + " 失败 " + Marshal.GetLastWin32Error();
                        continue;
                    }
                    IntPtr imp;
                    if (!DuplicateTokenEx(srcTok, MAXIMUM_ALLOWED, IntPtr.Zero, SECURITY_IMPERSONATION, TOKEN_IMPERSONATION, out imp))
                    {
                        err = "DuplicateTokenEx " + cn + " 失败 " + Marshal.GetLastWin32Error();
                        continue;
                    }
                    if (!ImpersonateLoggedOnUser(imp))
                    {
                        err = "ImpersonateLoggedOnUser " + cn + " 失败 " + Marshal.GetLastWin32Error();
                        continue;
                    }
                    err = null;
                    break;
                }
                catch { }
            }
            if (err != null)
            {
                return err;
            }
            try
            {
                action();
                return null;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            finally
            {
                RevertToSelf();
            }
        }
    }

    internal static class PowerModeApi
    {
        private const string KeyPath = @"SYSTEM\CurrentControlSet\Control\Power\User\PowerSchemes";
        private const string AcValueName = "ActiveOverlayAcPowerScheme";
        private const string DcValueName = "ActiveOverlayDcPowerScheme";
        private const string ActiveSchemeValueName = "ActivePowerScheme";

        private static readonly Guid BestEfficiency = new Guid("961cc777-2547-4f9d-8174-7d86181b8a7a");
        private static readonly Guid BestPerformance = new Guid("ded574b5-45a0-4f42-8737-46345c09c238");

        [DllImport("powrprof.dll")]
        private static extern uint PowerSetActiveOverlayScheme(Guid overlaySchemeGuid, Guid schemeGuid);

        [DllImport("powrprof.dll")]
        private static extern uint PowerGetEffectiveOverlayScheme(out Guid overlaySchemeGuid);

        public static Guid ToOverlayGuid(PowerModeLevel level)
        {
            switch (level)
            {
                case PowerModeLevel.BestEfficiency: return BestEfficiency;
                case PowerModeLevel.BestPerformance: return BestPerformance;
                default: return Guid.Empty;
            }
        }

        public static string ToDisplayName(PowerModeLevel level)
        {
            switch (level)
            {
                case PowerModeLevel.BestEfficiency: return "最佳能效";
                case PowerModeLevel.BestPerformance: return "最佳性能";
                default: return "平衡";
            }
        }

        private static Guid ReadGuidValue(string valueName)
        {
            using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyPath, false))
            {
                if (key != null)
                {
                    string raw = key.GetValue(valueName) as string;
                    Guid guid;
                    if (raw != null && Guid.TryParse(raw, out guid))
                    {
                        return guid;
                    }
                }
            }
            return Guid.Empty;
        }

        public static Guid ActiveSchemeGuid
        {
            get
            {
                Guid g = ReadGuidValue(ActiveSchemeValueName);
                if (g == Guid.Empty)
                {
                    g = new Guid("381b4222-f694-41f0-9685-ff5bb260df2e");
                }
                return g;
            }
        }

        public static PowerModeLevel GetAcMode()
        {
            return ToLevel(ReadGuidValue(AcValueName));
        }

        public static PowerModeLevel GetDcMode()
        {
            return ToLevel(ReadGuidValue(DcValueName));
        }

        public static PowerModeLevel GetEffectiveMode()
        {
            Guid g;
            uint ret = PowerGetEffectiveOverlayScheme(out g);
            if (ret == 0)
            {
                return ToLevel(g);
            }
            return IsOnAc() ? GetAcMode() : GetDcMode();
        }

        public static PowerModeLevel ToLevel(Guid overlay)
        {
            if (overlay == BestEfficiency) return PowerModeLevel.BestEfficiency;
            if (overlay == BestPerformance) return PowerModeLevel.BestPerformance;
            return PowerModeLevel.Balanced;
        }

        public static uint ApplyToCurrentSource(PowerModeLevel level)
        {
            return PowerSetActiveOverlayScheme(ToOverlayGuid(level), ActiveSchemeGuid);
        }

        public static bool IsOnAc()
        {
            return SystemInformation.PowerStatus.PowerLineStatus == PowerLineStatus.Online;
        }

        public static string WriteOtherSourceValue(bool forAc, PowerModeLevel level)
        {
            string valueName = forAc ? AcValueName : DcValueName;
            Guid overlay = ToOverlayGuid(level);
            string fail = SysImpersonation.RunAsSystem(delegate()
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey(KeyPath, true))
                {
                    if (key == null)
                    {
                        throw new InvalidOperationException("无法打开注册表键");
                    }
                    key.SetValue(valueName, overlay.ToString("D"), RegistryValueKind.String);
                }
            });
            if (fail != null)
            {
                throw new InvalidOperationException(fail);
            }
            Guid read = ReadGuidValue(valueName);
            return "已保存 " + valueName + "=" + overlay.ToString("D") + (read == overlay ? " (确认成功)" : " (未确认，请点刷新)");
        }
    }

    internal class MainForm : Form
    {
        private static readonly Guid GuidAcDcPowerSource = new Guid("5d3e9a59-e9d5-4b00-a6bd-ff34ff516548");
        private IntPtr powerNotify;

        private Label sourceLabel;
        private Label effectiveLabel;
        private Button[] acButtons;
        private Button[] dcButtons;
        private TextBox logBox;

        [DllImport("user32.dll")]
        private static extern IntPtr RegisterPowerSettingNotification(IntPtr hRecipient, ref Guid powerSettingGuid, int flags);

        [DllImport("user32.dll")]
        private static extern bool UnregisterPowerSettingNotification(IntPtr handle);

        public MainForm()
        {
            Text = "Windows 电源模式切换验证 (Power Mode Tester)";
            Font = new Font("Microsoft YaHei UI", 9F);
            ClientSize = new Size(560, 530);
            MinimumSize = new Size(560, 530);
            StartPosition = FormStartPosition.CenterScreen;
            BuildUi();
            RefreshAll();
            if (!SysImpersonation.EnableSeDebugPrivilege())
            {
                AppendLog("警告: 启用 SeDebugPrivilege 失败，设置另一供电状态可能无法写入");
            }
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Guid g = GuidAcDcPowerSource;
            powerNotify = RegisterPowerSettingNotification(Handle, ref g, 0);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (powerNotify != IntPtr.Zero)
            {
                UnregisterPowerSettingNotification(powerNotify);
            }
            base.OnFormClosed(e);
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_POWERBROADCAST = 0x218;
            const int PBT_POWERSETTINGCHANGE = 0x8013;
            if (m.Msg == WM_POWERBROADCAST && m.WParam == (IntPtr)PBT_POWERSETTINGCHANGE)
            {
                AppendLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] 检测到电源状态变化");
                RefreshAll();
                PowerModeLevel stored = PowerModeApi.IsOnAc() ? PowerModeApi.GetAcMode() : PowerModeApi.GetDcMode();
                PowerModeLevel effective = PowerModeApi.GetEffectiveMode();
                if (stored != effective)
                {
                    uint ret = PowerModeApi.ApplyToCurrentSource(stored);
                    AppendLog("自动套用该供电状态的模式: " + PowerModeApi.ToDisplayName(stored) + " (ret=" + ret + ")");
                    RefreshAll();
                }
            }
            base.WndProc(ref m);
        }

        private void BuildUi()
        {
            int y = 10;

            sourceLabel = new Label { Location = new Point(12, y), AutoSize = true };
            y += 26;
            effectiveLabel = new Label { Location = new Point(12, y), AutoSize = true, Font = new Font("Microsoft YaHei UI", 10F, FontStyle.Bold) };
            y += 30;

            GroupBox acGroup = new GroupBox { Text = "已接通电源 (AC)", Location = new Point(12, y), Size = new Size(530, 70) };
            GroupBox dcGroup = new GroupBox { Text = "使用电池 (DC)", Location = new Point(12, y + 80), Size = new Size(530, 70) };

            acButtons = CreateButtons(acGroup, true);
            dcButtons = CreateButtons(dcGroup, false);

            y += 160;
            Button refreshButton = new Button { Text = "刷新状态", Location = new Point(12, y), Size = new Size(100, 28) };
            refreshButton.Click += delegate { RefreshAll(); };

            y += 40;
            Label hintLabel = new Label
            {
                Location = new Point(12, y),
                Size = new Size(530, 30),
                ForeColor = Color.DimGray,
                Text = "两组按钮随时可点: 点击即保存，与 Windows 设置页一致；当前供电状态立即生效，另一状态在切换到该供电方式时自动运行。"
            };
            y += 34;
            Label logLabel = new Label { Text = "操作日志:", Location = new Point(12, y), AutoSize = true };
            y += 20;
            logBox = new TextBox { Location = new Point(12, y), Size = new Size(530, 175), Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, BackColor = Color.White };

            Controls.Add(sourceLabel);
            Controls.Add(effectiveLabel);
            Controls.Add(acGroup);
            Controls.Add(dcGroup);
            Controls.Add(refreshButton);
            Controls.Add(hintLabel);
            Controls.Add(logLabel);
            Controls.Add(logBox);
        }

        private Button[] CreateButtons(GroupBox group, bool forAc)
        {
            string[] names = { "最佳能效", "平衡", "最佳性能" };
            Button[] buttons = new Button[3];
            for (int i = 0; i < 3; i++)
            {
                Button b = new Button { Text = names[i], Size = new Size(160, 30), Location = new Point(12 + i * 172, 28) };
                PowerModeLevel level = (PowerModeLevel)i;
                bool ac = forAc;
                b.Click += delegate
                {
                    try
                    {
                        if (ac == PowerModeApi.IsOnAc())
                        {
                            uint ret = PowerModeApi.ApplyToCurrentSource(level);
                            string ok = ret == 0 ? "成功" : "失败";
                            AppendLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] 设置 " + (ac ? "AC" : "DC") + " -> " + PowerModeApi.ToDisplayName(level) + " | PowerSetActiveOverlayScheme ret=" + ret + " (" + ok + "，立即生效)");
                            if (ret != 0)
                            {
                                MessageBox.Show(this, "切换失败，返回码: " + ret, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            }
                        }
                        else
                        {
                            string msg = PowerModeApi.WriteOtherSourceValue(ac, level);
                            AppendLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] 设置 " + (ac ? "AC" : "DC") + " -> " + PowerModeApi.ToDisplayName(level) + " | " + msg + " (已保存并显示，与设置页一致)");
                        }
                    }
                    catch (Exception ex)
                    {
                        AppendLog("[" + DateTime.Now.ToString("HH:mm:ss") + "] 失败: " + ex.Message);
                        MessageBox.Show(this, "设置失败: " + ex.Message, "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    RefreshAll();
                };
                group.Controls.Add(b);
                buttons[i] = b;
            }
            return buttons;
        }

        private void RefreshAll()
        {
            bool onAc = PowerModeApi.IsOnAc();
            sourceLabel.Text = "当前供电状态: " + (onAc ? "已接通电源 (AC)" : "使用电池 (DC)");
            PowerModeLevel acMode = PowerModeApi.GetAcMode();
            PowerModeLevel dcMode = PowerModeApi.GetDcMode();
            effectiveLabel.Text = "当前生效电源模式: " + PowerModeApi.ToDisplayName(PowerModeApi.GetEffectiveMode()) + "    [AC=" + PowerModeApi.ToDisplayName(acMode) + ", DC=" + PowerModeApi.ToDisplayName(dcMode) + "]";
            UpdateButtonColors(acButtons, acMode);
            UpdateButtonColors(dcButtons, dcMode);
        }

        private void UpdateButtonColors(Button[] buttons, PowerModeLevel current)
        {
            Color active = Color.FromArgb(176, 224, 176);
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].BackColor = ((PowerModeLevel)i == current) ? active : SystemColors.Control;
            }
        }

        private void AppendLog(string text)
        {
            logBox.AppendText(text + Environment.NewLine);
        }
    }
}
