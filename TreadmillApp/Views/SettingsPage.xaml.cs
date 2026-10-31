using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsPage()
        {
            InitializeComponent();
            _viewModel = BindingContext as SettingsViewModel;
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel?.LoadSettings();
        }
    }
}
