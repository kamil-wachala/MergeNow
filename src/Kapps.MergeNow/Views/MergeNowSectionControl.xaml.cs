using System.Windows.Controls;

namespace MergeNow.Views
{
    public partial class MergeNowSectionControl : UserControl
    {
        public MergeNowSectionControl()
        {
            InitializeComponent();
        }

        private void TargetBranchPopup_OnOpened(object sender, System.EventArgs e)
        {
            TargetBranchSearchTb.Focus();
            TargetBranchSearchTb.SelectAll();
        }
    }
}
