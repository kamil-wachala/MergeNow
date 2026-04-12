using MergeNow.Core.Mvvm.Commands;
using MergeNow.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Data;

namespace MergeNow.ViewModels
{
    public class MergeNowSectionDesignViewModel : IMergeNowSectionViewModel
    {
        public IBaseCommand BrowseCommand { get; } = new EmptyCommand();
        public IBaseCommand FindCommand { get; } = new EmptyCommand();
        public IBaseCommand OpenChangesetCommand { get; } = new EmptyCommand();
        public IBaseCommand MergeCommand { get; } = new EmptyCommand();
        public IBaseCommand ClearPageCommand { get; } = new EmptyCommand();
        public IBaseCommand ClearMergeNowCommand { get; } = new EmptyCommand();

        public ObservableCollection<string> TargetBranches { get; } = new ObservableCollection<string>
        {
            "$/releases/r01",
            "$/releases/r02"
        };

        public ICollectionView FilteredTargetBranches { get; }

        public bool IsSectionEnabled { get; set; } = true;
        public string ChangesetNumber { get; set; } = "123456";
        public string ChangesetName { get; set; } = "My changeset name";
        public string SelectedTargetBranch { get; set; } = "$/releases/r01";
        public string TargetBranchSearchText { get; set; } = string.Empty;
        public string TargetBranchPickerDisplayText => SelectedTargetBranch ?? "Select a target branch";
        public bool IsTargetBranchPickerOpen { get; set; }
        public bool AnyTargetBranches => true;
        public bool AnyFilteredTargetBranches => true;
        public bool CombinedMerge { get; set; } = true;
        public bool IsAdvancedExpanded { get; set; } = true;
        public bool HasMergeStatus => true;
        public string MergeStatusSummary { get; } = "Merge completed.";
        public string MergeStatusDetails { get; } = "Merge statistics:\nFiles: 12\nUpdates: 12\nPlease review the changes and check-in manually.";
        public MergeResultType MergeStatusKind => MergeResultType.Success;

        public MergeNowSectionDesignViewModel()
        {
            FilteredTargetBranches = CollectionViewSource.GetDefaultView(TargetBranches);
        }
    }
}
