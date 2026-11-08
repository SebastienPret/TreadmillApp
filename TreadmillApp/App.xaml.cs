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
            window.Created += async (s, e) =>
            {
                ThemeService.Apply(ThemeService.Current, save: false);

                // try catch update on start
                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(3));   // laisse l'interface s'afficher d'abord
                    await UpdateService.CheckAsync();

                }
                catch (Exception ex)
                {
                    // Log the exception or handle it as needed
                    Console.WriteLine($"Error checking for updates: {ex.Message}");
                }
            };

            return window;
            
        }
    }
}