using System.Diagnostics;
using System.Windows;

namespace wowsCheaterViewer
{
    /// <summary>
    /// ReadMeWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ReadMeWindow : Window
    {
        public ReadMeWindow()
        {
            InitializeComponent();
        }

        private void Hyperlink_Click(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            var psi = new ProcessStartInfo
            {
                FileName = e.Uri.AbsoluteUri,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
    }
}
