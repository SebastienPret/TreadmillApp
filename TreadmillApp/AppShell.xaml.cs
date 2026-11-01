using TreadmillApp.Services;

namespace TreadmillApp
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            ThemeService.ApplyShellColors(this);
        }
    }
}
