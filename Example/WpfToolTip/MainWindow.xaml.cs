using System;
using System.Collections.Generic;
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

namespace WpfToolTip
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public ToolTip tt = new ToolTip();
        public MainWindow()
        {
            InitializeComponent();
            tt.Content = "TTTTTTTTTol Tiiiiiiiiiip";
            tt.Content = string.Empty;
            this.btn1.ToolTip = tt;
        }
    }
}
