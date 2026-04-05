using MergeNow.Core.Mvvm;
using MergeNow.Core.Mvvm.Commands;
using MergeNow.Core.Utils;
using MergeNow.Model;
using MergeNow.Services;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MergeNow.ViewModels
{
    public class MergeNowSectionViewModel : BaseViewModel, IMergeNowSectionViewModel
    {
        private readonly IMergeNowService _mergeNowService;
        private readonly IMessageService _messageService;

        private Changeset SelectedChangeset { get; set; }
        private MergeHistory MergeHistory { get; }

        public IBaseCommand BrowseCommand { get; }
        public IBaseCommand FindCommand { get; }
        public IBaseCommand OpenChangesetCommand { get; }
        public IBaseCommand MergeCommand { get; }
        public IBaseCommand ClearPageCommand { get; }
        public IBaseCommand ClearMergeNowCommand { get; }

        public ObservableCollection<string> TargetBranches { get; }

        private bool _isOnline;
        public bool IsSectionEnabled
        {
            get => _isOnline;
            set => SetValue(ref _isOnline, value);
        }

        private string _changesetNumber;
        public string ChangesetNumber
        {
            get => _changesetNumber;
            set
            {
                SetValue(ref _changesetNumber, value);
                ResetView();
            }
        }

        private string _changesetName;
        public string ChangesetName
        {
            get => _changesetName;
            set => SetValue(ref _changesetName, value);
        }

        private string _selectedTargetBranch;
        public string SelectedTargetBranch
        {
            get => _selectedTargetBranch;
            set => SetValue(ref _selectedTargetBranch, value);
        }

        public bool AnyTargetBranches => TargetBranches.Any();

        private bool _combinedMerge;
        public bool CombinedMerge
        {
            get => _combinedMerge;
            set => SetValue(ref _combinedMerge, value);
        }

        private bool _isAdvancedExpanded;
        public bool IsAdvancedExpanded
        {
            get => _isAdvancedExpanded;
            set => SetValue(ref _isAdvancedExpanded, value);
        }

        public MergeNowSectionViewModel(IMergeNowService mergeNowService, IMessageService messageService)
        {
            _mergeNowService = mergeNowService;
            _messageService = messageService;

            FindCommand = new AsyncCommand(_messageService.ShowError, FindChangesetAsync, CanFindChangeset);
            LinkToViewModel(FindCommand);

            BrowseCommand = new AsyncCommand(_messageService.ShowError, BrowseChangesetAsync);
            LinkToViewModel(BrowseCommand);

            MergeCommand = new AsyncCommand(_messageService.ShowError, MergeChangesetAsync, CanMergeChangeset);
            LinkToViewModel(MergeCommand);

            OpenChangesetCommand = new AsyncCommand(_messageService.ShowError, OpenChangesetAsync, CanOpenChangeset);
            LinkToViewModel(OpenChangesetCommand);

            ClearPageCommand = new AsyncCommand(_messageService.ShowError, ClearPageCommandAsync);
            LinkToViewModel(ClearPageCommand);

            ClearMergeNowCommand = new AsyncCommand(_messageService.ShowError, ClearMergeNowCommandAsync);
            LinkToViewModel(ClearMergeNowCommand);

            TargetBranches = new ObservableCollection<string>();
            LinkToViewModel(TargetBranches);

            MergeHistory = new MergeHistory();
        }

        public void Reconnect()
        {
            ReconnectAsync().FireAsyncCatchErrors(_messageService.ShowError);
        }

        private async Task FindChangesetAsync()
        {
            var changeset = await _mergeNowService.FindChangesetAsync(ChangesetNumber);

            if (changeset == null)
            {
                _messageService.ShowWarning($"Changeset '{ChangesetNumber}' does not exist.");
                return;
            }

            await ApplyChangesetAsync(changeset);
        }

        private async Task BrowseChangesetAsync()
        {
            var changeset = await _mergeNowService.BrowseChangesetAsync();

            // Browse was canceled
            if (changeset == null)
            {
                return;
            }

            await ApplyChangesetAsync(changeset);
        }

        private Task OpenChangesetAsync()
        {
            return _mergeNowService.ViewChangesetDetailsAsync(SelectedChangeset);
        }

        private async Task MergeChangesetAsync()
        {
            if (!CombinedMerge)
            {
                MergeHistory.Clear();
            }

            var isSectionEnabled = IsSectionEnabled;

            try
            {
                IsSectionEnabled = false;
                await Task.Run(() => _mergeNowService.MergeAsync(SelectedChangeset, SelectedTargetBranch, MergeHistory));
            }
            finally
            {
                IsSectionEnabled = isSectionEnabled;
            }
        }

        private async Task ReconnectAsync()
        {
            IsSectionEnabled = await _mergeNowService.IsOnlineAsync();
        }

        private Task ClearPageCommandAsync()
        {
            return _mergeNowService.ClearPendingChangesPageAsync();
        }

        private Task ClearMergeNowCommandAsync()
        {
            ChangesetNumber = null;
            return Task.CompletedTask;
        }

        private bool CanFindChangeset()
        {
            return !string.IsNullOrWhiteSpace(ChangesetNumber) &&
                int.TryParse(ChangesetNumber, out var number) &&
                number > 0;
        }

        private bool CanMergeChangeset()
        {
            return !string.IsNullOrWhiteSpace(SelectedTargetBranch);
        }

        private bool CanOpenChangeset()
        {
            return !string.IsNullOrWhiteSpace(ChangesetName);
        }

        private async Task ApplyChangesetAsync(Changeset changeset)
        {
            ResetView();

            if (changeset == null)
            {
                return;
            }

            ChangesetNumber = changeset.ChangesetId.ToString();
            ChangesetName = changeset.Comment;
            SelectedChangeset = changeset;

            var branches = await _mergeNowService.GetTargetBranchesAsync(SelectedChangeset);
            branches?.ToList().ForEach(branch => TargetBranches.Add(branch));
        }

        private void ResetView()
        {
            SelectedChangeset = null;
            ChangesetName = string.Empty;

            SelectedTargetBranch = string.Empty;
            TargetBranches.Clear();

            MergeHistory.Clear();
        }

        public void ShowForSelectedHistoryViewChangeset()
        {
            ShowForSelectedHistoryViewChangesetAsync().FireAsyncCatchErrors(_messageService.ShowError);
        }

        private async Task ShowForSelectedHistoryViewChangesetAsync()
        {
            var changesets = await _mergeNowService.GetHistoryViewSelectedChangesetsAsync();

            if (changesets == null || changesets.Length == 0)
            {
                return;
            }

            await _mergeNowService.NavigateToPendingChangePageAsync();
            await ApplyChangesetAsync(changesets.First());
        }
    }
}
