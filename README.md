# T140 OSD

T140 的 Fn 热键和屏幕显示（OSD）工具包。程序通过硬件 WMI 事件和低级键盘钩子监听热键、Caps Lock、Num Lock 等状态，并在屏幕中央显示对应图标。

## 文件说明

| 文件 | 作用 |
| --- | --- |
| `BLDFnHotkeyUtility.exe` | 用户会话中的 OSD 窗口、WMI 热键监听和 Caps/Num Lock 监听程序 |
| `BLDHotKeyService.exe` | Windows 服务，负责启动和监控 OSD，并在睡眠恢复或会话解锁后重启 OSD |
| `BLDPowerModeHelper.exe` | x64 助手，为 OSD 调用 `PowerSetActiveOverlayScheme` 使 Windows 电源模式立即生效 |
| `InstallService.bat` | 以管理员权限安装或更新 `BLDHotKeyService` |
| `UninstallService.bat` | 停止并卸载 `BLDHotKeyService` |
| `InstallUtil.exe` | .NET Framework 服务安装工具 |
| `BLDFnHotkeyUtility.exe.manifest` | UAC、Windows 兼容性和 Per-Monitor DPI 配置 |

## 安装和更新

将整个目录作为一个部署目录使用，不要只复制单个服务文件。首次安装或更换部署目录后，在目标目录中右键以管理员身份运行 `InstallService.bat`。脚本会：

1. 请求管理员权限并切换到脚本所在目录。
2. 停止并卸载已注册的 `BLDHotKeyService`（如果存在）。
3. 使用 `InstallUtil.exe` 注册当前目录中的 `BLDHotKeyService.exe`。
4. 注册为自动启动服务并立即启动。

服务启动的是同一部署目录中的 `BLDFnHotkeyUtility.exe`。只要部署目录和服务注册路径没有变化，替换任一 EXE 后都不需要重新安装服务，但必须重启 `BLDHotKeyService` 才会加载新文件；如果服务注册路径指向旧目录，则需要从新目录重新运行 `InstallService.bat`。

卸载时运行 `UninstallService.bat`。该操作会停止服务并终止由服务启动的 OSD 进程。

## 检查服务路径

```powershell
sc.exe qc BLDHotKeyService
Get-CimInstance Win32_Service -Filter "Name='BLDHotKeyService'" |
    Select-Object Name, State, StartMode, PathName, StartName
```

`PathName` 应指向当前部署目录中的 `BLDHotKeyService.exe`，服务状态应为 `Running`。如果返回错误 1060，说明服务尚未安装。

## OSD 恢复修复

长时间运行或睡眠/唤醒后，原程序可能出现“进程仍在运行但状态切换不再显示”的问题，原因包括：

- 分层窗口每次重绘创建的 `Bitmap` 没有及时释放，长期运行会积累图形资源。
- 睡眠后原有分层窗口可能仍存在，但对应的合成表面已经失效。
- 该机器不会稳定地向 OSD 窗口发送 `WM_POWERBROADCAST` 恢复消息，因此仅在 OSD 内处理恢复消息并不可靠。
- OSD 定时器在线程池线程中直接关闭窗口，旧定时器消息可能覆盖新的状态切换。

当前版本采用“OSD 自检 + 服务事件恢复”的双层方式：

- 释放分层窗口和 WM_PRINTCLIENT 绘制使用的位图及 GDI 对象。
- 为 OSD 显示定时器增加代次校验，只处理当前状态的隐藏消息，并将窗口操作交回窗口消息线程。
- OSD 的 UI 线程每秒记录一次时间；如果消息循环停顿超过 5 秒，则在恢复运行后自动重建分层窗口，不依赖 `WM_POWERBROADCAST`。
- `BLDHotKeyService` 通过 Windows Service Control Manager 接收系统恢复事件和会话解锁事件。
- 收到 `ResumeAutomatic`、`ResumeCritical`、`ResumeSuspend` 或 `SessionUnlock` 后，服务等待 500 ms，再重启当前活动会话中的 OSD。
- 500 ms 内连续到达的恢复和解锁通知会合并，避免重复重启。
- 两个 EXE 均保留版本 `1.0.6.1`；OSD 目标平台为 x86/.NET Framework 4.x。

## 性能档位 → Windows 电源模式同步

OSD 的性能档位（WMI 事件 `SystemPerMode`，共 3 档）与 Windows 电源模式（电源滑块 overlay，AC/DC 两套记忆）自动同步：

| OSD 档位 | Windows 电源模式 | Overlay GUID |
| --- | --- | --- |
| PerformanceMode（性能） | 最佳性能 | `ded574b5-45a0-4f42-8737-46345c09c238` |
| BalanceMode（均衡） | 平衡 | `00000000-0000-0000-0000-000000000000` |
| QuietMode（安静） | 最佳能效 | `961cc777-2547-4f9d-8174-7d86181b8a7a` |

实现方式：OSD（x86，LocalSystem）收到事件或启动时写入 `ActiveOverlayAcPowerScheme` 与 `ActiveOverlayDcPowerScheme`（SYSTEM-only 键，OSD 具备权限），再调用同目录的 x64 助手 `BLDPowerModeHelper.exe` 调用 `PowerSetActiveOverlayScheme` 让当前供电状态立即生效。注意该 API 的 32 位实现在本机会导致访问违规（0xC0000005），因此必须经 x64 助手调用。所有同步动作会写入 `OSDEvents` 事件日志（前缀 `PowerModeSync:`）。

反编译源码（含本功能的 `BLD.Power/PowerModeSync.cs`）位于仓库 `osd-src/` 目录；用 .NET Framework 4 的 Roslyn csc 以 x86 编译 OSD、x64 编译助手。

## 验证

可以通过连续发送热键或锁定键状态进行显示测试。睡眠/唤醒后，服务收到恢复事件时会重启 OSD；即使系统未发送该事件，OSD 也会在检测到超过 5 秒的消息循环停顿后重建窗口。随后切换状态应正常显示 OSD。当前文件的 SHA-256 为：

```text
BLDHotKeyService.exe:   66C9D65F3FDA313DDC7298F3DC1B0548A44DBAFF64F1D5D33D5D80F9CBB66FC3
BLDFnHotkeyUtility.exe: CA2A1577AD492BC8EE66FE21391C12A69BEAA6A622972FD31333F7C9D4433B5D
BLDPowerModeHelper.exe:  F02615E87E2B0143D5F3197CBE9677B821A8D0B77E2279878540AA9C810F377A
```

仓库同时包含 `osd-src/` 目录中的反编译源码（含后续修复与电源模式同步功能），发布仍以更新后的可执行文件形式进行。
