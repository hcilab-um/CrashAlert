using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Threading;
using System.Windows.Threading;
using System.ComponentModel;

namespace KinectWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        NuiSensor _sensor;
        BackgroundWorker _worker = new BackgroundWorker();

        public MainWindow()
        {
            InitializeComponent();

            CompositionTarget.Rendering += new EventHandler(CompositionTarget_Rendering);
            Closing += new System.ComponentModel.CancelEventHandler(MainWindow_Closing);

            _sensor = new NuiSensor("SamplesConfig.xml");

            _worker.DoWork += new DoWorkEventHandler(Worker_DoWork);
        }

        void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            Dispatcher.BeginInvoke((Action)delegate
            {
                imgRaw.Source = _sensor.RawImageSource;
                imgDepth.Source = _sensor.DepthImageSource;
            });
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            if (!_worker.IsBusy)
            {
                _worker.RunWorkerAsync();
            }
        }

        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _sensor.Dispose();
        }

        private void BtnToggleVisibility_Click(object sender, RoutedEventArgs e)
        {
            if (imgDepth.IsVisible)
            {
                imgDepth.Visibility = System.Windows.Visibility.Hidden;
                btnToggleVisibility.Content = "Show depth image";
            }
            else
            {
                imgDepth.Visibility = System.Windows.Visibility.Visible;
                btnToggleVisibility.Content = "Show raw image";
            }
        }
    }
}
