namespace ParallelBuildsMonitor
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;


    /// <summary>
    /// Interaction logic for ParallelBuildsMonitorWindowControl.
    /// </summary>
    public partial class ParallelBuildsMonitorWindowControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelBuildsMonitorWindowControl"/> class.
        /// </summary>
        public ParallelBuildsMonitorWindowControl()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel.Instance;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            ScrollViewer scrollViewer = sender as ScrollViewer;
            if (scrollViewer != null)
            {
                if (e.ExtentHeightChange == 0)
                {
                    // user action
                    if (scrollViewer.VerticalOffset == scrollViewer.ScrollableHeight)
                    {
                        GraphControl.Instance.scrollLast = true;
                    }
                    else
                    {
                        GraphControl.Instance.scrollLast = false;
                    }
                }
                if (e.ExtentHeightChange != 0)
                {
                    if (GraphControl.Instance.scrollLast)
                    {
                        scrollViewer.ScrollToBottom();
                    }
                }
            }
        }

        /// <summary>
        /// Store visual representation of any control from screen to .png file.
        /// </summary>
        /// <remarks>
        /// This method save even invisible part of control.
        /// </remarks>
        /// <param name="target">Any control like Image, Button, Calendar etc.</param>
        /// <param name="background">Providing background will override control transparent background. This is optional, and can be null.</param>
        /// <param name="fileName">Filename with path, where .png will be saved.</param>
        public static void SaveToPng(Visual target, Brush background, string fileName) //Original name was CreateBitmapFromVisual()
        {
            if (target == null || string.IsNullOrEmpty(fileName))
            {
                Debug.Assert(false, "Wrong SaveToPng() method call. Fix caller!");
                return;
            }

            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            if (bounds.IsEmpty)
            {
                Debug.Assert(false, "How it happen that save was not dissabled? Disable Save button to avoid wrong call.");
                return;
            }

            RenderTargetBitmap renderTarget = new RenderTargetBitmap((Int32)bounds.Width, (Int32)bounds.Height, 96, 96, PixelFormats.Pbgra32);
            DrawingVisual visual = new DrawingVisual();
            using (DrawingContext context = visual.RenderOpen())
            {
                if (background != null)
                    context.DrawRectangle(background, null, new Rect(new Point(), bounds.Size)); // GraphControl has transparent background. This draw background

                VisualBrush visualBrush = new VisualBrush(target);
                context.DrawRectangle(visualBrush, null, new Rect(new Point(), bounds.Size));
            }

            renderTarget.Render(visual);
            PngBitmapEncoder bitmapEncoder = new PngBitmapEncoder();
            bitmapEncoder.Frames.Add(BitmapFrame.Create(renderTarget));
            using (Stream stm = File.Create(fileName))
            {
                bitmapEncoder.Save(stm);
            }
        }

        private void MenuItem_ClickSaveGraph(object sender, RoutedEventArgs e)
        {
            string date = DataModel.Instance.StartTime.ToString("yyyy-MM-dd HH.mm.ss"); //Format "2018-05-08 01.09.07" to preserve correct sorting

            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "PBM " + DataModel.Instance.SolutionName + " " + date;
            dlg.DefaultExt = ".png";                      // Default file extension
            dlg.Filter = "Portable Images (.png)|*.png";  // Filter files by extension

            if (dlg.ShowDialog() != true)
                return;

            SaveToPng(this.graph, this.Background, dlg.FileName);
        }
    }
}
