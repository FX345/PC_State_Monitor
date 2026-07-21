using System.Windows;
using PcGuardianLite.Core;

namespace PcGuardianLite.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = @"Local\PcGuardianLite.SingleInstance";
    private SingleInstanceGuard? _singleInstanceGuard;

    protected override void OnStartup(StartupEventArgs e)
    {
        if (UninstallRunner.IsUninstallRequest(e.Args))
        {
            ShutdownMode = ShutdownMode.OnExplicitShutdown;
            UninstallRunner.RunInteractive();
            Shutdown();
            return;
        }

        _singleInstanceGuard = SingleInstanceGuard.TryAcquire(SingleInstanceMutexName);
        if (!_singleInstanceGuard.HasOwnership)
        {
            Shutdown();
            return;
        }

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _singleInstanceGuard?.Dispose();
        base.OnExit(e);
    }
}
