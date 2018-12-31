using System;
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvDTE;
using EnvDTE80;
using System.Collections.ObjectModel;

namespace ParallelBuildsMonitor
{
    public class GraphControl : ContentControl
    {
        #region Members

        public bool scrollLast = true;
        public string intFormat = "D3";
        public bool isBuilding = false;

        // put any colors here, set final theme dependent colors in OnForegroundChanged
        Brush blueSolidBrush = new SolidColorBrush(Colors.DarkBlue);
        Pen blackPen = new Pen(new SolidColorBrush(Colors.Black), 1.0);
        Brush blackBrush = new SolidColorBrush(Colors.Black);
        Brush greenSolidBrush = new SolidColorBrush(Colors.DarkGreen);
        Brush redSolidBrush = new SolidColorBrush(Colors.DarkRed);
        Brush whiteBrush = new SolidColorBrush(Colors.White);
        Pen grid = new Pen(new SolidColorBrush(Colors.LightGray), 1.0);
        Pen cpuPen = new Pen(new SolidColorBrush(Colors.Black), 1.0);
        Pen hddPen = new Pen(new SolidColorBrush(Colors.Black), 1.0);
        private static double refreshTimerInterval = 1000; // 1000 means collect data every 1s.
        private System.Timers.Timer refreshTimer = new System.Timers.Timer(refreshTimerInterval);

        #endregion Members

        #region Properties

        /// <summary>
        /// Convinient accessor to data.
        /// </summary>
        static DataModel DataModel { get { return DataModel.Instance; } }

        #endregion Properties

        public GraphControl()
        {
            Instance = this;
            OnForegroundChanged();
            refreshTimer.Elapsed += new ElapsedEventHandler(RefreshTimerEventTick); // Are we sure there is only one instance of GraphControl? If not operator += will multiply calls...
        }

        void OnForegroundChanged()
        {
            bool isDark = false;
            SolidColorBrush foreground = Foreground as SolidColorBrush;
            if (foreground != null)
            {
                isDark = foreground.Color.R > 0x80;
            }

            blackBrush = new SolidColorBrush(isDark ? Colors.White : Colors.Black);
            Background = Brushes.Transparent;
            greenSolidBrush = new SolidColorBrush(isDark ? Colors.LightGreen : Colors.DarkGreen);
            redSolidBrush = new SolidColorBrush(isDark ? Color.FromRgb(249, 33, 33) : Colors.DarkRed);
            blueSolidBrush = new SolidColorBrush(isDark ? Color.FromRgb(81, 156, 245) : Colors.DarkBlue);
            blackPen = new Pen(new SolidColorBrush(isDark ? Colors.White : Colors.Black), 1.0);
            grid = new Pen(new SolidColorBrush(isDark ? Color.FromRgb(66, 66, 66) : Colors.LightGray), 1.0);
            cpuPen = new Pen(new SolidColorBrush(isDark ? Colors.LightSeaGreen : Colors.DarkTurquoise), 1.0);
            hddPen = new Pen(new SolidColorBrush(isDark ? Colors.LightSkyBlue : Colors.DarkViolet), 1.0);
        }

        public static GraphControl Instance
        {
            get;
            private set;
        }

        public void BuildBegin()
        {
            scrollLast = true;
            refreshTimer.Start();
            isBuilding = true;
        }

        public void BuildDone()
        {
            refreshTimer.Stop();
            isBuilding = false;
            InvalidateVisual(); // When solution build finished, refresh graph manually, since refreshTimer has stopped.
        }

        private void RefreshTimerEventTick(object sender, ElapsedEventArgs e)
        {
            GraphControl.Instance.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                 new System.Action(() =>
                 {
                     GraphControl.Instance.InvalidateVisual();
                 }));
        }

        void DrawGraph(string title, System.Windows.Media.DrawingContext drawingContext, ReadOnlyCollection<Tuple<long, float>> data, Pen pen, ref int i, Size RenderSize, double rowHeight, double maxStringLength, long maxTick, long nowTick, Typeface fontFace, bool showAverage)
        {
            // Status separator
            drawingContext.DrawLine(grid, new Point(0, i * rowHeight), new Point(RenderSize.Width, i * rowHeight));

            // Draw graph
            FormattedText itext = new FormattedText(title, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
            drawingContext.DrawText(itext, new Point(1, i * rowHeight));

            if (data.Count > 0)
            {
                double pixelsRange = RenderSize.Width - maxStringLength;

                double sumValues = 0;
                long sumTicks = 0;
                long previousTick = DataModel.StartTime.Ticks;
                float previousValue = 0.0f;

                int step = 1;
                if (data.Count > 10) // Do rarefy only when more than 10 samples
                {
                    step = (int)(data.Count / (pixelsRange / 10)); // Draw no frequent than 10 pixels
                    if (step < 1)
                        step = 1;
                }

                for (int nbr = 0; nbr < data.Count; nbr += step)
                {
                    long spanL = previousTick - DataModel.StartTime.Ticks;
                    long spanR = data[nbr].Item1 - DataModel.StartTime.Ticks;
                    if (isBuilding && nbr >= data.Count - step)
                        spanR = nowTick - DataModel.StartTime.Ticks;
                    double shiftL = (int)(spanL * (long)(RenderSize.Width - maxStringLength) / maxTick);
                    double shiftR = (int)(spanR * (long)(RenderSize.Width - maxStringLength) / maxTick);
                    Point p1 = new Point(maxStringLength + shiftL, (i + 1) * rowHeight - Math.Min(previousValue, 100.0) * (rowHeight - 2) / 100 - 1);
                    Point p2 = new Point(maxStringLength + shiftR, (i + 1) * rowHeight - Math.Min(data[nbr].Item2, 100.0) * (rowHeight - 2) / 100 - 1);

                    StreamGeometry streamGeometry = new StreamGeometry();
                    using (StreamGeometryContext geometryContext = streamGeometry.Open())
                    {
                        geometryContext.BeginFigure(p1, true, true);
                        Point p3 = new Point(p2.X, (i + 1) * rowHeight);
                        Point p4 = new Point(p1.X, (i + 1) * rowHeight);
                        PointCollection points = new PointCollection { p2, p3, p4 };
                        geometryContext.PolyLineTo(points, true, true);
                    }

                    drawingContext.DrawGeometry(pen.Brush, pen, streamGeometry);
                    //drawingContext.DrawLine(pen, p1, p2);

                    if (showAverage)
                    {
                        sumValues += (previousValue + data[nbr].Item2) * (data[nbr].Item1 - previousTick);
                        sumTicks += data[nbr].Item1 - previousTick;
                    }

                    previousTick = data[nbr].Item1;
                    previousValue = data[nbr].Item2;
                }

                if (showAverage)
                {
                    long average = (long)(sumValues / 2 / sumTicks);
                    FormattedText avg = new FormattedText("Avg. " + average.ToString() + "%", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                    double m = avg.Width;
                    drawingContext.DrawText(avg, new Point(RenderSize.Width - m, i * rowHeight));
                }
            }
            i++;
        }

        /// <summary>
        /// This class ensure that IsGraphDrawn is always correctly set according to current visual.
        /// </summary>
        private class IsGraphDrawnScope : IDisposable
        {
            public bool IsDrawn { get; set; } = false;

            public void Dispose()
            {
                ViewModel.Instance.IsGraphDrawn = IsDrawn;
            }
        }

        protected override void OnRender(System.Windows.Media.DrawingContext drawingContext)
        {
            try
            {
                using (IsGraphDrawnScope isGraphDrawnScope = new IsGraphDrawnScope())
                {
                    if (RenderSize.Width < 10.0 || RenderSize.Height < 10.0)
                    {
                        return;
                    }

                    if (PBMCommand.Instance == null)
                        return;

                    DTE2 dte = (DTE2)PBMCommand.Instance.ServiceProvider.GetService(typeof(DTE));

                    if (dte == null)
                        return;

                    if (DataModel.Instance.AllProjectsCount == 0)
                        return;

                    Typeface fontFace = new Typeface(FontFamily.Source);

                    FormattedText dummyText = new FormattedText("A0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);

                    double rowHeight = dummyText.Height + 1;
                    int linesCount = DataModel.CurrentBuilds.Count + DataModel.FinishedBuilds.Count + 1 + 1 + 1 + 1; // 1 for header, 1 for status, 1 for CPU, 1 for HDD
                    double totalHeight = rowHeight * linesCount;

                    Height = totalHeight;

                    double maxStringLength = 0.0;
                    long tickStep = 100000000;
                    long maxTick = tickStep;
                    long nowTick = DateTime.Now.Ticks;
                    long t = nowTick - DataModel.StartTime.Ticks;
                    if (DataModel.CurrentBuilds.Count > 0)
                    {
                        if (t > maxTick)
                        {
                            maxTick = t;
                        }
                    }
                    int i;
                    bool atLeastOneError = false;
                    for (i = 0; i < DataModel.FinishedBuilds.Count; i++)
                    {
                        FormattedText iname = new FormattedText(DataModel.FinishedBuilds[i].name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        double l = iname.Width;
                        t = DataModel.FinishedBuilds[i].end;
                        atLeastOneError = atLeastOneError || !DataModel.FinishedBuilds[i].success;
                        if (t > maxTick)
                        {
                            maxTick = t;
                        }
                        if (l > maxStringLength)
                        {
                            maxStringLength = l;
                        }
                    }
                    foreach (KeyValuePair<string, DateTime> item in DataModel.CurrentBuilds)
                    {
                        FormattedText iname = new FormattedText(item.Key, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        double l = iname.Width;
                        if (l > maxStringLength)
                        {
                            maxStringLength = l;
                        }
                    }
                    if (isBuilding)
                    {
                        maxTick = (maxTick / tickStep + 1) * tickStep;
                    }
                    FormattedText iint = new FormattedText(i.ToString(intFormat) + " ", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                    maxStringLength += 5 + iint.Width;

                    i = 0;
                    { // Draw header
                        string headerSeparator = "  |  ";
                        string headerText = DataModel.SolutionName + headerSeparator + DataModel.StartTime.ToString("yyyy-MM-dd HH:mm:ss") + headerSeparator + MachineInfo.Instance.ToString(headerSeparator); // Data format "2018-05-08 01.09.07" to preserve correct sorting

                        // Draw text
                        FormattedText itext = new FormattedText(headerText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        {
                            // Cut text when it is too long for window. Probably correct solution is to add horizontal scrollbar to window.
                            // Set a maximum width and height. If the text overflows these values, an ellipsis "..." appears.
                            itext.MaxTextWidth = RenderSize.Width;
                            itext.MaxTextHeight = rowHeight;
                        }
                        drawingContext.DrawText(itext, new Point(1, i * rowHeight));

                        // Draw separator
                        drawingContext.DrawLine(grid, new Point(0, i * rowHeight), new Point(RenderSize.Width, i * rowHeight));
                        i++;
                    }

                    Brush greenGradientBrush = new LinearGradientBrush(Colors.MediumSeaGreen, Colors.DarkGreen, new Point(0, 0), new Point(0, 1));
                    Brush redGradientBrush = new LinearGradientBrush(Colors.IndianRed, Colors.DarkRed, new Point(0, 0), new Point(0, 1));
                    foreach (BuildInfo item in DataModel.FinishedBuilds)
                    {
                        Brush solidBrush = item.success ? greenSolidBrush : redSolidBrush;
                        Brush gradientBrush = item.success ? greenGradientBrush : redGradientBrush;
                        DateTime span = new DateTime(item.end - item.begin);
                        string time = PBMCommand.SecondsToString(span.Ticks);
                        FormattedText itext = new FormattedText((i).ToString(intFormat) + " " + item.name, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, solidBrush);
                        drawingContext.DrawText(itext, new Point(1, i * rowHeight));
                        Rect r = new Rect();
                        r.X = maxStringLength + (int)((item.begin) * (long)(RenderSize.Width - maxStringLength) / maxTick);
                        r.Width = maxStringLength + (int)((item.end) * (long)(RenderSize.Width - maxStringLength) / maxTick) - r.X;
                        if (r.Width == 0)
                        {
                            r.Width = 1;
                        }
                        r.Y = i * rowHeight + 1;
                        r.Height = rowHeight - 1;
                        drawingContext.DrawRectangle(gradientBrush, null, r);
                        FormattedText itime = new FormattedText(time, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, whiteBrush);
                        double timeLen = itime.Width;
                        if (r.Width > timeLen)
                        {
                            drawingContext.DrawText(itime, new Point(r.Right - timeLen, i * rowHeight));
                        }
                        drawingContext.DrawLine(grid, new Point(0, i * rowHeight), new Point(RenderSize.Width, i * rowHeight));
                        i++;
                    }

                    Brush blueGradientBrush = new LinearGradientBrush(Colors.LightBlue, Colors.DarkBlue, new Point(0, 0), new Point(0, 1));
                    foreach (KeyValuePair<string, DateTime> item in DataModel.CurrentBuilds)
                    {
                        FormattedText itext = new FormattedText((i).ToString(intFormat) + " " + item.Key, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blueSolidBrush);
                        drawingContext.DrawText(itext, new Point(1, i * rowHeight));
                        Rect r = new Rect();
                        r.X = maxStringLength + (int)((item.Value.Ticks - DataModel.StartTime.Ticks) * (long)(RenderSize.Width - maxStringLength) / maxTick);
                        r.Width = maxStringLength + (int)((nowTick - DataModel.StartTime.Ticks) * (long)(RenderSize.Width - maxStringLength) / maxTick) - r.X;
                        if (r.Width == 0)
                        {
                            r.Width = 1;
                        }
                        r.Y = i * rowHeight + 1;
                        r.Height = rowHeight - 1;
                        drawingContext.DrawRectangle(blueGradientBrush, null, r);
                        drawingContext.DrawLine(grid, new Point(0, i * rowHeight), new Point(RenderSize.Width, i * rowHeight));
                        i++;
                    }

                    DrawGraph("CPU usage", drawingContext, DataModel.CpuUsage, cpuPen, ref i, RenderSize, rowHeight, maxStringLength, maxTick, nowTick, fontFace, true /*showAverage*/);
                    DrawGraph("HDD usage", drawingContext, DataModel.HddUsage, hddPen, ref i, RenderSize, rowHeight, maxStringLength, maxTick, nowTick, fontFace, false /*showAverage - Probably there is no max value for HDD that is why we can't cound average*/);

                    if (DataModel.CurrentBuilds.Count > 0 || DataModel.FinishedBuilds.Count > 0)
                    {
                        string line = "";
                        if (isBuilding)
                        {
                            line = "Building...";
                        }
                        else
                        {
                            line = "Done";
                        }
                        if (DataModel.MaxParallelBuilds > 0)
                        {
                            line += " (" + DataModel.PercentageProcessorUse().ToString() + "% of " + DataModel.MaxParallelBuilds.ToString() + " CPUs)";
                        }
                        FormattedText itext = new FormattedText(line, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        drawingContext.DrawText(itext, new Point(1, i * rowHeight));
                    }

                    // Draw vertical line that separate project names from Gantt chart
                    drawingContext.DrawLine(grid, new Point(maxStringLength, 1 * rowHeight), new Point(maxStringLength, i * rowHeight));

                    drawingContext.DrawLine(new Pen(atLeastOneError ? redSolidBrush : greenSolidBrush, 1), new Point(0, i * rowHeight), new Point(RenderSize.Width, i * rowHeight));

                    DateTime dt = new DateTime(maxTick);
                    string s = PBMCommand.SecondsToString(dt.Ticks);
                    FormattedText maxTime = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                    double m = maxTime.Width;
                    drawingContext.DrawText(maxTime, new Point(RenderSize.Width - m, i * rowHeight));

                    isGraphDrawnScope.IsDrawn = true;
                } //End of IsGraphDrawnScope
            }
            catch (Exception)
            {   // Keep this try{} catch{}!
                // Actually code in try{} bail from time to time on windows resize.
                // I guess because there is division by (x - y), while x equals y, but it might be not the only case.
            }
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Foreground")
            {
                OnForegroundChanged();
            }
            base.OnPropertyChanged(e);
        }

    }
}
