using TreadmillApp.Services;
using TreadmillApp.ViewModels;

namespace TreadmillApp.Views
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel? _viewModel;

        public SettingsPage()
        {
            try
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
            catch (Exception ex)
            {
                // TEMPORAIRE : affiche l'erreur à l'écran pour la lire en Release
                Content = new ScrollView
                {
                    Content = new Label
                    {
                        Text = ex.ToString(),
                        Margin = 20,
                        FontSize = 12
                    }
                };
            }
        }

        protected override void OnAppearing()
        {
            base.OnAppearing();
            _viewModel?.LoadSettings();
        }
    }
}
