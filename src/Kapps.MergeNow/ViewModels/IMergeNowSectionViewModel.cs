using MergeNow.Core.Mvvm.Commands;
using MergeNow.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace MergeNow.ViewModels
{
    public interface IMergeNowSectionViewModel
    {
        IBaseCommand BrowseCommand { get; }
        IBaseCommand FindCommand { get; }
        IBaseCommand OpenChangesetCommand { get; }
        IBaseCommand MergeCommand { get; }
        IBaseCommand ClearPageCommand { get; }
        IBaseCommand ClearMergeNowCommand { get; }
        ObservableCollection<string> TargetBranches { get; }
        ICollectionView FilteredTargetBranches { get; }

        bool IsSectionEnabled { get; set; }
        string ChangesetNumber { get; set; }
        string ChangesetName { get; set; }
        string SelectedTargetBranch { get; set; }
        string TargetBranchSearchText { get; set; }
        string TargetBranchPickerDisplayText { get; }
        bool IsTargetBranchPickerOpen { get; set; }
        bool AnyTargetBranches { get; }
        bool AnyFilteredTargetBranches { get; }
        bool CombinedMerge { get; set; }
        bool IsAdvancedExpanded { get; set; }
        bool HasMergeStatus { get; }
        string MergeStatusSummary { get; }
        string MergeStatusDetails { get; }
        MergeResultType MergeStatusKind { get; }
    }
}
