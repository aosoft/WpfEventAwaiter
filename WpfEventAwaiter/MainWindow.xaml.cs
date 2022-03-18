using System.Windows;
using System.Windows.Media.Animation;

namespace WpfEventAwaiter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
#if true
            if (TryFindResource("Storyboard") is Storyboard sb)
            {
                Button.Content = "Running";
                await sb.BeginTypeCAsync();
                Button.Content = null;
            }
#else
            await new SleepAwaitable(2000);
#endif
        }
    }
}