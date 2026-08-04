using System.Windows;
using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class InitView : UserControl
    {
        public InitView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private void VatSysCloseButton_Click(object sender, RoutedEventArgs e)
        {
            VatSys.Close();
        }
    }
}
