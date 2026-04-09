using MergeNow.ViewModels;
using MergeNow.Views;
using Microsoft.TeamFoundation.Controls;
using System;
using System.ComponentModel;

namespace MergeNow
{
    [TeamExplorerSection(MergeNowSectionId, TeamExplorerPageIds.PendingChanges, MergeNowSectionSortOrder)]
    public class MergeNowSection : ITeamExplorerSection
    {
        public const string MergeNowSectionId = "0210c7cf-7c17-494b-a30b-836432a1bcfd";
        public const int MergeNowSectionSortOrder = 100;

        private readonly MergeNowSectionViewModel _mainViewModel;
        private readonly MergeNowSectionControl _mainView;
        private readonly MergeNowSectionMemento _memento;

        public MergeNowSection()
        {
            try
            {
                _mainViewModel = MergeNowComposition.Resolve<MergeNowSectionViewModel>()
                    ?? throw new InvalidOperationException("Failed to resolve Merge Now view model.");

                _mainView = new MergeNowSectionControl { DataContext = _mainViewModel };
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to initialize Merge Now.", ex);
            }

            try
            {
                _memento = MergeNowComposition.Resolve<MergeNowSectionMemento>() ?? new MergeNowSectionMemento();
            }
            catch (Exception ex)
            {
                _memento = new MergeNowSectionMemento();
                Logger.Error("Failed to load Merge Now memento.", ex);
            }

            IsExpanded = _memento.IsExpanded;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title => "Merge Now";

        public object SectionContent => _mainView
            ?? (object)"Failed to initialize Merge Now. Open Visual Studio Activity Log for more details.";

        private bool _isVisible = true;
        public bool IsVisible
        {
            get => _isVisible;
            set
            {
                _isVisible = value;
                OnPropertyChanged(nameof(IsVisible));
            }
        }

        private bool _isExpanded;
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
                _memento.IsExpanded = value;
            }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set
            {
                _isBusy = value;
                OnPropertyChanged(nameof(IsBusy));
            }
        }

        public void Initialize(object sender, SectionInitializeEventArgs e)
        {
        }

        public void Loaded(object sender, SectionLoadedEventArgs e)
        {
            _mainViewModel?.Reconnect();
        }

        public void SaveContext(object sender, SectionSaveContextEventArgs e)
        {
        }

        public void Refresh()
        {
            _mainViewModel?.Reconnect();
        }

        public void Cancel()
        {
        }

        public object GetExtensibilityService(Type serviceType)
        {
            return null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
