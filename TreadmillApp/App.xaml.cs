using TreadmillApp.Services;

namespace TreadmillApp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            ThemeService.Initialize();   // recharge le dernier style choisi

        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Created += (s, e) => ThemeService.Apply(ThemeService.Current, save: false);
            return window;
        }
    }
}