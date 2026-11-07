using TreadmillApp.Services;
using TreadmillApp.Views;

namespace TreadmillApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            ThemeService.ApplyShellColors(this);

            Routing.RegisterRoute(nameof(ScanDevicesPage), typeof(ScanDevicesPage));
        }
    }
}
