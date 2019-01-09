using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace DiskPerformance
{
    public partial class MainWindow : Window
    {
        DispatcherTimer Timer99 = new DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
            Timer99.Tick += Timer99_Tick; // don't freeze the ui
            Timer99.Interval = new TimeSpan(0, 0, 0, 0, 1024);
            Timer99.IsEnabled = true;
        }
        public PerformanceCounter myCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        public PerformanceCounter rwCounter = new PerformanceCounter("PhysicalDisk", "Disk Write Bytes/sec", "_Total");
        public PerformanceCounter avgCounter = new PerformanceCounter("PhysicalDisk", "Avg. Disk sec/Read", "_Total");

        public void Timer99_Tick(System.Object sender, System.EventArgs e)

        {
            //Console.Clear();
            Int32 j = Convert.ToInt32(myCounter.NextValue());
            float k = Convert.ToSingle(rwCounter.NextValue());
            float l = Convert.ToSingle(avgCounter.NextValue());
            //Console.WriteLine(j);

            textblock1.Text = "\"PhysicalDisk\", \"% Disk Time\", \"_Total\": " + j.ToString() 
                + "\n\"PhysicalDisk\", \"Disk Write Bytes / sec\", \"_Total\": " + k.ToString()
                + "\n\"PhysicalDisk\", \"Avg. Disk sec/Read\", \"_Total\": " + l.ToString();
        }
    }
}
