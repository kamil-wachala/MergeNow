using MergeNow.ViewModels;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using Task = System.Threading.Tasks.Task;

namespace MergeNow
{
    internal sealed class MergeNowCommand
    {
        public const int CommandId = PackageIds.MergeNowCommandId;
        public static readonly Guid CommandSet = PackageGuids.guidMergeNowPackageCmd;

        private readonly MergeNowSectionViewModel _viewModel;

        public static MergeNowCommand Instance { get; private set; }

        private MergeNowCommand(MergeNowSectionViewModel viewModel, OleMenuCommandService commandService)
        {
            _viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);
            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage asyncPackage, MergeNowSectionViewModel viewModel)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(asyncPackage.DisposalToken);

            var commandService = await asyncPackage.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            Instance = new MergeNowCommand(viewModel, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            _viewModel.ShowForSelectedHistoryViewChangeset();
        }
    }
}
