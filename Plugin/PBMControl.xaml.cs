using System;
using System.ComponentModel.Design;
using System.Windows;
using System.Windows.Controls;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Interaction logic for PBMControl.
    /// </summary>
    public partial class PBMControl : UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParallelBuildsMonitorWindowControl"/> class.
        /// </summary>
        public PBMControl()
        {
            this.InitializeComponent();
            this.DataContext = ViewModel.Instance;
        }

        private void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (GraphControl.Instance == null)
                return;

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

        public bool SaveGraph(string pathToPngFile)
        {
            if (String.IsNullOrEmpty(pathToPngFile))
            {
                Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
                {
                    FileName = DataModel.Instance.GetSaveFileNamePrefix(),
                    DefaultExt = ".png",               // Default file extension
                    Filter = "Portable Images|*.png"   // Filter files by extension
                };

                if (dlg.ShowDialog() != true)
                    return false;

                pathToPngFile = dlg.FileName;
            }

            return SavePng.SaveToPng(this.graph, this.Background, pathToPngFile);
        }

        private void MyToolWindow_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            OleMenuCommandService commandService = PBMCommand.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            CommandID menuID = new CommandID(typeof(PBMCommand.ContextMenuCommandSet).GUID, (int)PBMCommand.ContextMenuCommandSet.idContextMenu);
            Point p = this.PointToScreen(e.GetPosition(this));
            commandService?.ShowContextMenu(menuID, (int)p.X, (int)p.Y);
        }
    }
}
