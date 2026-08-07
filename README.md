# T140 OSD

T140 的 Fn 热键和屏幕显示（OSD）工具包。程序通过硬件 WMI 事件和低级键盘钩子监听热键、Caps Lock、Num Lock 等状态，并在屏幕中央显示对应图标。

## 文件说明

| 文件 | 作用 |
| --- | --- |
| `BLDFnHotkeyUtility.exe` | 用户会话中的 OSD 窗口、WMI 热键监听和 Caps/Num Lock 监听程序 |
| `BLDHotKeyService.exe` | Windows 服务，负责在用户登录、解锁或 OSD 进程退出后启动 OSD |
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

服务启动的是同一部署目录中的 `BLDFnHotkeyUtility.exe`。只替换 OSD EXE 后通常不需要重新安装服务，但必须重启当前 OSD 进程；如果服务注册路径指向旧目录，则需要从新目录重新运行 `InstallService.bat`。

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
- 睡眠恢复、显示配置变化和 DPI 变化后没有重新提交分层窗口内容。
- OSD 定时器在线程池线程中直接关闭窗口，旧定时器消息可能覆盖新的状态切换。

当前 `BLDFnHotkeyUtility.exe` 已修复这些路径：

- 释放分层窗口和 WM_PRINTCLIENT 绘制使用的位图及 GDI 对象。
- 处理 `WM_POWERBROADCAST` 的恢复事件，以及显示/DPI 配置变化，并执行重绘和延迟重试。
- 为 OSD 显示定时器增加代次校验，只处理当前状态的隐藏消息，并将窗口操作交回窗口消息线程。
- 保留 EXE 版本 `1.0.6.1`，目标平台为 x86/.NET Framework 4.x。

## 验证

可以通过连续发送热键或锁定键状态进行显示测试；也应在睡眠/唤醒后再次切换状态确认 OSD 恢复。当前修复版的 SHA-256 为：

```text
99EC951A25563934801C38D11671713E9ADCDF2F1AF3BC7975D1CF8C6321CE8B
```

仓库只包含发布包和可执行文件，没有原始 C# 工程源码；因此 OSD 修复以更新后的 `BLDFnHotkeyUtility.exe` 形式发布。
