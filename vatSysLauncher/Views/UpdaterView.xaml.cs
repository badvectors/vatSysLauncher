using System.Windows.Controls;
using vatSysLauncher.Controllers;

namespace vatSysLauncher.Views
{
    public partial class UpdaterView : UserControl
    {
        public UpdaterView()
        {
            InitializeComponent();
            DataContext = Launcher.MainViewModel;
        }

        private void UpdaterLog_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is not TextBox) return;
            var textBox = (TextBox)e.Source;
            textBox.CaretIndex = textBox.Text.Length;
            textBox.ScrollToEnd();
        }
    }
}
