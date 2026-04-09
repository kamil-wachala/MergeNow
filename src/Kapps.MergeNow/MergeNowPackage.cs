using MergeNow.Services;
using MergeNow.Settings;
using MergeNow.ViewModels;
using EnvDTE;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace MergeNow
{
    [ProvideOptionPage(typeof(MergeNowSettings), "Merge Now", "General", 0, 0, true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [ProvideAutoLoad(UIContextGuids80.NoSolution, PackageAutoLoadFlags.BackgroundLoad)]
    [InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Version)]
    [Guid(PackageGuids.guidMergeNowPackageString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class MergeNowPackage : AsyncPackage
    {
#if VS2022_PACKAGE
        private const int VisualStudio2022MajorVersion = 17;
#endif
#if VS2026_PACKAGE
        private const int VisualStudio2026MajorVersion = 18;
#endif

        protected async override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
#if VS2022_PACKAGE || VS2026_PACKAGE
            if (!await IsSupportedHostAsync(cancellationToken))
            {
                return;
            }
#endif

            try
            {
                var serviceCollection = new ServiceCollection();
                ConfigureServices(serviceCollection);
                MergeNowComposition.Initialize(serviceCollection.BuildServiceProvider());
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Merge Now service provider.", ex);
            }

            try
            {
                await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
                var viewModel = MergeNowComposition.Resolve<MergeNowSectionViewModel>();
                await MergeNowCommand.InitializeAsync(this, viewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Merge Now Changeset History context menu item.", ex);
            }

            await base.InitializeAsync(cancellationToken, progress);
        }

#if VS2022_PACKAGE || VS2026_PACKAGE
        private async System.Threading.Tasks.Task<bool> IsSupportedHostAsync(CancellationToken cancellationToken)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

            var dte = await GetServiceAsync(typeof(DTE)) as DTE;
            var versionText = dte?.Version;

            if (!Version.TryParse(versionText, out var version))
            {
                return true;
            }

#if VS2022_PACKAGE
            if (version.Major <= VisualStudio2022MajorVersion)
            {
                return true;
            }

            var message = $"MergeNow (VS 2022) supports Visual Studio 2022 (v{VisualStudio2022MajorVersion}) only. Detected Visual Studio v{version.Major}. Please install the MergeNow (VS 2026) extension instead.";
#else
            if (version.Major >= VisualStudio2026MajorVersion)
            {
                return true;
            }

            var message = $"MergeNow (VS 2026) supports Visual Studio 2026 (v{VisualStudio2026MajorVersion}) or newer only. Detected Visual Studio v{version.Major}. Please install the MergeNow (VS 2022) extension instead.";
#endif
            Logger.Error(message);
            VsShellUtilities.ShowMessageBox(this, message, "Merge Now",
                OLEMSGICON.OLEMSGICON_WARNING, OLEMSGBUTTON.OLEMSGBUTTON_OK, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
            return false;
        }
#endif

        private void ConfigureServices(IServiceCollection services)
        {
            services.AddSingleton<AsyncPackage>(_ => this);
            services.AddSingleton<IMergeNowSettings, MergeNowLazySettings>();
            services.AddSingleton<IMessageService, MessageService>();
            services.AddSingleton<IMergeNowService, MergeNowService>();
            services.AddSingleton<MergeNowSectionViewModel>();
            services.AddSingleton<MergeNowSectionMemento>();
        }
    }
}
