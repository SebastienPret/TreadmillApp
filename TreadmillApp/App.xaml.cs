using Microsoft.Extensions.DependencyInjection;
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
            return new Window(new AppShell());
        }
    }
}