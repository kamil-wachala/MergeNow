using MergeNow.Services;
using MergeNow.Settings;
using MergeNow.ViewModels;
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
        protected async override Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
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
                var viewModel = MergeNowComposition.Resolve<MergeNowSectionViewModel>();
                await MergeNowCommand.InitializeAsync(this, viewModel);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Merge Now Changeset History context menu item.", ex);
            }

            await base.InitializeAsync(cancellationToken, progress);
        }

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
