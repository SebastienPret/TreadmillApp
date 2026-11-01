using TreadmillApp.Services;
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

            StylePicker.ItemsSource = Enum.GetNames<AppStyle>();
            StylePicker.SelectedItem = ThemeService.Current.ToString();
            StylePicker.SelectedIndexChanged += (s, e) =>
            {
                if (StylePicker.SelectedItem is string name && Enum.TryParse(name, out AppStyle style))
                    ThemeService.Apply(style);
            };
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel?.LoadSettings();
        }
    }
}
