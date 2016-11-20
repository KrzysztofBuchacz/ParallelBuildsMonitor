namespace ParallelBuildsMonitor
{
  using System.Diagnostics.CodeAnalysis;
  using System.Windows;
  using System.Windows.Controls;

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
  }
}