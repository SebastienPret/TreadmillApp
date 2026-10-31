using TreadmillApp.Services;

namespace TreadmillApp.Views
{
    public partial class DashboardPage : ContentPage
    {
        public static TreadmillService Treadmill { get; } = new();

        public DashboardPage()
        {
            InitializeComponent();
        }
    }
}