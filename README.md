# PcGuardianLite

PcGuardianLite 是一款轻量 Windows 桌面电脑状态悬浮工具。它提供常驻悬浮球、系统状态面板、CPU / 内存 / 磁盘 / 上传下载速度监控、健康评分、进程占用排行、网络测速、报告生成和保守安全清理。

## 普通用户下载

请到 GitHub 仓库的 Releases 页面下载：

```text
PcGuardianLiteSetup.exe
```

下载后双击安装即可。安装器会把主程序和必要脚本放到：

```text
%LOCALAPPDATA%\PcGuardianLite
```

安装时可以勾选“在桌面创建快捷方式”。不建议普通用户下载 GitHub 的 `Source code.zip`，那是源码包，不是可直接使用的安装包。

更详细的步骤见 [下载使用.md](./下载使用.md)。

## 主要功能

- 常驻桌面的悬浮球，右键可切换显示模式、透明度、隐藏到托盘或退出。
- 展开面板查看 CPU、内存、磁盘、上传、下载和温度状态。
- 网络页提供实时流量说明和 Speed Test 下载 / 上传测速。
- 清理页先扫描、列出可清理内容，再由用户勾选清理。
- 进程页只展示占用排行，不结束进程。
- 报告页可生成电脑体检报告、网络报告、文件夹占用报告，并打开报告目录。
- 单实例运行，重复启动不会出现多个悬浮球。
- 安装器会注册 Windows 卸载入口，并提供本地卸载脚本。

## 项目结构

```text
src/                       应用、安装器和核心逻辑
scripts/                   报告与诊断 PowerShell 脚本源码
tests/                     轻量回归测试
build_installer.bat        生成安装包
build_release.bat          生成可分发 release 文件夹
下载使用.md                给普通用户看的下载、安装和卸载说明
```

安装包内部会把脚本放到 `tools\scripts`，用户日常只需要启动 `PcGuardianLite.exe` 或桌面快捷方式，不需要手动运行脚本。

## 开发者构建

运行测试：

```powershell
dotnet run --project .\tests\PcGuardianLite.Tests\PcGuardianLite.Tests.csproj
```

构建解决方案：

```powershell
dotnet build .\PcGuardianLite.sln
```

生成安装包：

```powershell
.\build_installer.ps1
```

或者双击：

```text
build_installer.bat
```

生成适合发送给别人的发布文件夹：

```powershell
.\build_release.ps1
```

或者双击：

```text
build_release.bat
```

输出位置：

```text
release\PcGuardianLiteSetup.exe
```

## 卸载

安装后可从 Windows 设置卸载：

```text
Settings > Apps > Installed apps > PcGuardianLite > Uninstall
```

安装目录也会包含：

```text
%LOCALAPPDATA%\PcGuardianLite\Uninstall PcGuardianLite.bat
```

卸载会移除程序文件、桌面快捷方式和当前用户的卸载注册项。`ScriptReports` 等生成报告目录会保留，避免误删用户报告。

## 安全清理策略

清理功能采用保守白名单：

- 用户临时文件：只清理 24 小时前的旧文件。
- Windows 临时文件：只清理 7 天前的旧文件。
- 回收站：可选，并会二次确认。
- 不清理浏览器缓存、下载目录、桌面、注册表、Prefetch、Windows Update 缓存或报告目录。
- 遇到锁定文件、权限不足或系统保护位置时跳过，不强制删除。
