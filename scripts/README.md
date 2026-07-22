# scripts

这里保存 PcGuardianLite 使用的 PowerShell 报告和诊断脚本源码。

普通用户不需要手动运行这些脚本。安装包会把它们复制到：

```text
%LOCALAPPDATA%\PcGuardianLite\tools\scripts
```

程序按钮会自动调用对应脚本，报告默认输出到：

```text
%LOCALAPPDATA%\PcGuardianLite\ScriptReports
```

源码开发时，这些脚本仍保留在 `scripts` 目录，方便调试和继续迭代。
