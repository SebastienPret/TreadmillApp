using Microsoft.UI.Xaml;

namespace TreadmillApp.WinUI
{
    public partial class App : MauiWinUIApplication
    {
        public App()
        {
            InitializeComponent();
        }

        // C'est cette méthode exacte qui est exigée par MauiWinUIApplication :
        protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
    }
}