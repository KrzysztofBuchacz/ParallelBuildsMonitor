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
using System.Diagnostics;

namespace ParallelBuildsMonitor
{
    public class GraphControl : ContentControl, IDisposable
    {
        #region Internal classes

        private class Spacings
        {
            /// <summary>
            /// Distance from left window border to project order number text.
            /// </summary>
            static readonly public double lOrder = 2.0 + GraphControl.penThickness;

            /// <summary>
            /// Distance from left window border to project name text. Set dynamically depending on project order number length text (number of projects).
            /// </summary>
            public double lProjName = 20.0;

            /// <summary>
            /// Distance from left window border to Gantt chart. Set dynamically depending on project name length text.
            /// </summary>
            public double lGanttC = 70.0;

            /// <summary>
            /// Distance from Gantt chart to right window border.
            /// </summary>
            static readonly public double rGanttC = 2.0 + GraphControl.penThickness;
        }

        #endregion Internal classes

        #region Members

        private static readonly double refreshTimerInterval = 1000; // 1000 means collect data every 1s.
        private System.Timers.Timer refreshTimer = new System.Timers.Timer(refreshTimerInterval);
        private long nowTickForTest = 0; // This value is used only when greater from 0 and only for testing.   Rationale: GraphControl refresh itself, but for test constant data is required.

        #endregion Members

        #region Members Visual

        public static readonly double minGanttWidth = 4.0; // don't draw Gantt and Graphs when there is less space than minGanttWidth

        public static readonly double penThickness = 1.0;
        // put any colors here, set final theme dependent colors in OnForegroundChanged
        static Pen blackPen = new Pen(new SolidColorBrush(Colors.Black), penThickness);
        static Pen grid = new Pen(new SolidColorBrush(Colors.LightGray), penThickness);
        static Pen cpuPen = new Pen(new SolidColorBrush(Colors.Black), penThickness);
        static Pen hddPen = new Pen(new SolidColorBrush(Colors.Black), penThickness);
        static Pen cpuSoftPen = new Pen(new SolidColorBrush(Colors.Black), penThickness);
        static Pen hddSoftPen = new Pen(new SolidColorBrush(Colors.Black), penThickness);
        static Brush blueSolidBrush = new SolidColorBrush(Colors.DarkBlue);
        static Brush blackBrush = new SolidColorBrush(Colors.Black);
        static Brush greenSolidBrush = new SolidColorBrush(Colors.DarkGreen);
        static Brush redSolidBrush = new SolidColorBrush(Colors.DarkRed);
        static Brush whiteBrush = new SolidColorBrush(Colors.White);
        static Brush greenGradientBrush = new LinearGradientBrush(Colors.MediumSeaGreen, Colors.DarkGreen, new Point(0, 0), new Point(0, 1));
        static Brush blueGradientBrush = new LinearGradientBrush(Colors.LightBlue, Colors.DarkBlue, new Point(0, 0), new Point(0, 1));
        static Brush redGradientBrush = new LinearGradientBrush(Colors.IndianRed, Colors.DarkRed, new Point(0, 0), new Point(0, 1));
        static Brush criticalPathGradientBrush = new LinearGradientBrush(Colors.LightYellow, Colors.Yellow, new Point(0, 0), new Point(0, 1));

        readonly Typeface fontFace = null;      // it is set in constructor
        readonly double rowHeight = 0;          // it is set in constructor
        static int rowNbrNoProjectsFiller = 6;  // Number of rows that are reserved to display message for user when there is no Gantt chart
        static string emptyGanttMsg = "Run Build or Rebuild to see Gantt chart here";

        public bool scrollLast = true;

        #endregion Members Visual

        #region Properties

        /// <summary>
        /// Convinient accessor to data.
        /// </summary>
        static DataModel DataModel { get { return DataModel.Instance; } }

        #endregion Properties

        #region Creator, Constructors

        public GraphControl()
        {
            Instance = this;
            fontFace = new Typeface(FontFamily.Source);
            rowHeight = (new FormattedText("A0", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush)).Height + penThickness;
            OnForegroundChanged();
            refreshTimer.Elapsed += new ElapsedEventHandler(RefreshTimerEventTick); // Are we sure there is only one instance of GraphControl? If not operator += will multiply calls...

            if (DataModel.IsBuilding)
            { // GraphControl can be constructed when build is already in progress
              // (Open VS, Start Build when PBM window is hidden behind Output tab, Switch to PBM window),
              // then GraphControl never got BuildBegin() event, so we need start it manually.
                BuildBegin();
            }
        }

        public static GraphControl Instance
        {
            get;
            private set;
        }

        #endregion Creator, Constructors

        #region Dispose

        private bool disposed = false; // Flag: Has Dispose already been called?

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                refreshTimer.Dispose();
            }

            disposed = true;
        }

        #endregion Dispose

        #region Starting, Stopping

        public void BuildBegin()
        {
            scrollLast = true;
            refreshTimer.Start();
        }

        public void BuildDone()
        {
            refreshTimer.Stop();
            InvalidateVisual(); // When solution build finished, refresh graph manually, since refreshTimer has stopped.
        }

        #endregion Starting, Stopping

        #region Helpers - Draw Methods

        void DrawGraph(string title, DrawingContext drawingContext, ReadOnlyCollection<Tuple<long, float>> data, Pen pen, Pen softPen, int rowNbr, Size RenderSize, double rowHeight, Spacings spacings, long maxTick, long nowTick, Typeface fontFace)
        {
            if (data.Count < 1)
                return;

            double pixelRange = RenderSize.Width - spacings.lGanttC - Spacings.rGanttC;
            if (pixelRange < minGanttWidth)
            {
                DrawText(drawingContext, title, rowNbr, Spacings.lOrder, blackBrush);
                return;
            }

            drawingContext.PushClip(new RectangleGeometry(new Rect(0, rowNbr * rowHeight, RenderSize.Width, rowHeight)));

            double sumValues = 0;
            long sumTicks = 0;
            long previousTick = DataModel.StartTime.Ticks;
            float previousValue = 0.0f;

            int step = 1;
            if (data.Count > 10) // Do rarefy only when more than 10 samples
            {
                step = (int)(data.Count / (pixelRange / 10)); // Draw no frequent than 10 pixels
                if (step < 1)
                    step = 1;
            }

            var allPoints = new List<Point>();
            for (int nbr = 0; nbr < data.Count; nbr += step)
            {
                long spanL = previousTick - DataModel.StartTime.Ticks;
                long spanR = data[nbr].Item1 - DataModel.StartTime.Ticks;
                if (DataModel.IsBuilding && nbr >= data.Count - step)
                    spanR = nowTick - DataModel.StartTime.Ticks;

                // Why (int) and (long) is needed here? Let's try to simplify
                //double shiftL = (int)(spanL * (long)pixelRange / maxTick);
                //double shiftR = (int)(spanR * (long)pixelRange / maxTick);
                double shiftL = spanL * pixelRange / maxTick;
                double shiftR = spanR * pixelRange / maxTick;
                Point p1 = new Point(spacings.lGanttC + shiftL, (rowNbr + 1) * rowHeight - previousValue * (rowHeight - 2) / 100 - 1);
                Point p2 = new Point(spacings.lGanttC + shiftR, (rowNbr + 1) * rowHeight - data[nbr].Item2 * (rowHeight - 2) / 100 - 1);

                StreamGeometry streamGeometry = new StreamGeometry();
                using (StreamGeometryContext geometryContext = streamGeometry.Open())
                {
                    geometryContext.BeginFigure(p1, true, true);
                    Point p3 = new Point(p2.X, (rowNbr + 1) * rowHeight);
                    Point p4 = new Point(p1.X, (rowNbr + 1) * rowHeight);
                    PointCollection points = new PointCollection { p2, p3, p4 };
                    geometryContext.PolyLineTo(points, true, true);
                }

                drawingContext.DrawGeometry(softPen.Brush, softPen, streamGeometry);

                if (nbr == 0)
                    allPoints.Add(p1);
                allPoints.Add(p2);

                if (nbr > 0)
                {
                    sumValues += (previousValue + data[nbr].Item2) * (data[nbr].Item1 - previousTick);
                    sumTicks += data[nbr].Item1 - previousTick;
                }

                previousTick = data[nbr].Item1;
                previousValue = data[nbr].Item2;
            }

            for (int nbr = 1; nbr < allPoints.Count; ++nbr)
                drawingContext.DrawLine(pen, allPoints[nbr-1], allPoints[nbr]);

            if (sumTicks > 0)
            {
                long average = (long)(sumValues / 2 / sumTicks);
                FormattedText avg = new FormattedText(" (Avg. " + average.ToString() + "%)", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                title += avg.Text;
            }

            DrawText(drawingContext, title, rowNbr, Spacings.lOrder, blackBrush);
            drawingContext.Pop();
        }

        private void DrawText(DrawingContext drawingContext, string caption, int rowNbr, double xPos, Brush textColor)
        {
            FormattedText captionFT = new FormattedText(caption, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, textColor);
            drawingContext.DrawText(captionFT, new Point(xPos, rowNbr * rowHeight));
        }

        private void DrawOrderAndProjectNameText(DrawingContext drawingContext, int rowNbr, uint projectBuildOrderNumber, string projectName, Spacings margins, Brush textColor)
        {
            // project build Order
            DrawText(drawingContext, projectBuildOrderNumber.ToString() + ">", rowNbr, Spacings.lOrder, textColor);

            // project Name
            DrawText(drawingContext, projectName, rowNbr, margins.lProjName, textColor);
        }

        /// <summary>
        /// Draw separator at the bottom of rowNbr.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="rowNbr"></param>
        private void DrawSeparator(DrawingContext drawingContext, int rowNbr)
        {
            drawingContext.DrawLine(grid, new Point(0, (rowNbr + 1) * rowHeight), new Point(RenderSize.Width, (rowNbr + 1) * rowHeight));
        }

        /// <summary>
        /// Draw vertical separator for Gantt chart - it omit first row.
        /// </summary>
        /// <param name="drawingContext"></param>
        /// <param name="x"></param>
        /// <param name="totalRowNbr"></param>
        private void DrawVerticalSeparator(DrawingContext drawingContext, double x, int totalRowNbr, bool wholeSize)
        {
            drawingContext.DrawLine(grid, new Point(x, (wholeSize ? 0 : 1) * rowHeight), new Point(x, (wholeSize ? totalRowNbr + 1 : totalRowNbr) * rowHeight));
        }

        private void DrawMachineInfo(DrawingContext drawingContext, int rowNbr)
        {
            string headerText = DataModel.GetSolutionNameWithMachineInfo("  |  ", false /*WithBuildStartedStr*/);

            FormattedText itext = new FormattedText(headerText, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
            // Cut text when it is too long for window - Set a maximum width and height. If the text overflows those values, an ellipsis "..." appears.
            itext.MaxTextWidth = RenderSize.Width - Spacings.lOrder - Spacings.rGanttC;
            itext.MaxTextHeight = rowHeight;

            drawingContext.DrawText(itext, new Point(Spacings.lOrder, rowNbr * rowHeight));
        }



        private void DrawBar(DrawingContext drawingContext, int rowNbr, long startTime, long endTime, long maxTick, Spacings spacings, Brush color, bool markAsCriticalPath)
        {
            double pixelRange = RenderSize.Width - spacings.lGanttC - Spacings.rGanttC;
            if (pixelRange < minGanttWidth)
                return;

            Rect r = new Rect();
            // Why (int) and (long) is needed here? Let's try to simplify
            //r.X = spacings.lGanttC + (int)(startTime * (long)(pixelRange) / maxTick);
            //r.Width = spacings.lGanttC + (int)(endTime * (long)(pixelRange) / maxTick) - r.X;
            r.X = spacings.lGanttC + (startTime * pixelRange / maxTick);
            r.Width = spacings.lGanttC + (endTime * pixelRange / maxTick) - r.X;
            if (r.Width == 0)
                r.Width = 1;

            r.Y = rowNbr * rowHeight + 1;
            r.Height = rowHeight - 1;
            drawingContext.DrawRectangle(color, null, r); //Draw Gantt graph

            if (markAsCriticalPath)
            {
                Rect cPR = new Rect(r.Location, r.Size);
                double cPRLHeight = r.Height / 8;
                cPR.Height = cPRLHeight;

                drawingContext.DrawRectangle(criticalPathGradientBrush, null, cPR); //Draw top yellow line in Gantt graph

                cPR.Y += (rowHeight - cPRLHeight - penThickness);
                drawingContext.DrawRectangle(criticalPathGradientBrush, null, cPR); //Draw bottom yellow line in Gantt graph
            }

            string time = Utils.SecondsToString(endTime - startTime);
            FormattedText itime = new FormattedText(time, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, whiteBrush);
            double timeLen = itime.Width;
            if (r.Width > timeLen)
            {
                drawingContext.DrawText(itime, new Point(r.Right - timeLen, rowNbr * rowHeight)); //Write elapsed time
            }
        }

        #endregion Helpers - Draw Methods

        #region Helpers - Utilities

        static protected bool IsEmptyBuilds()
        {
            return ((DataModel.CurrentBuilds.Count == 0) && (DataModel.FinishedBuilds.Count == 0));
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

        #endregion Helpers - Utilities

        #region Main Draw Method

        protected override void OnRender(DrawingContext drawingContext)
        {
            try
            {
                using (IsGraphDrawnScope isGraphDrawnScope = new IsGraphDrawnScope())
                {
                    if (RenderSize.Width < 10.0 || RenderSize.Height < 10.0)
                        return;

                    if (IsEmptyBuilds())
                    { // Case when no single build was started yet  -  display some info to ensure user that everything is OK
                        FormattedText captionFT = new FormattedText(emptyGanttMsg, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        drawingContext.DrawText(captionFT, new Point(50.0, rowNbrNoProjectsFiller / 2 * rowHeight));

                        return;
                    }

                    int linesCount = DataModel.CurrentBuilds.Count + DataModel.FinishedBuilds.Count + 1 + 1 + 1 + 1; // 1 for header, 1 for status, 1 for CPU, 1 for HDD
                    double totalHeight = rowHeight * linesCount;

                    Height = totalHeight + penThickness;

                    long tickStep = 100000000;
                    long maxTick = tickStep;
                    long nowTick = ((nowTickForTest > 0) ? nowTickForTest : DateTime.Now.Ticks);
                    long t = nowTick - DataModel.StartTime.Ticks;
                    if (DataModel.CurrentBuilds.Count > 0)
                    {
                        if (t > maxTick)
                        {
                            maxTick = t;
                        }
                    }
                    int ii;
                    bool atLeastOneError = false;
                    uint maxBuildOrderNbr = 1;
                    double projectNameMaxLen = 10;
                    for (ii = 0; ii < DataModel.FinishedBuilds.Count; ii++)
                    {
                        FormattedText iname = new FormattedText(DataModel.FinishedBuilds[ii].ProjectName, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        double ll = iname.Width;
                        t = DataModel.FinishedBuilds[ii].end;
                        atLeastOneError = atLeastOneError || !DataModel.FinishedBuilds[ii].success;
                        if (t > maxTick)
                            maxTick = t;

                        if (ll > projectNameMaxLen)
                            projectNameMaxLen = ll;

                        if (DataModel.FinishedBuilds[ii].ProjectBuildOrderNumber > maxBuildOrderNbr)
                            maxBuildOrderNbr = DataModel.FinishedBuilds[ii].ProjectBuildOrderNumber;
                    }
                    foreach (KeyValuePair<string, Tuple<uint, long>> item in DataModel.CurrentBuilds)
                    {
                        FormattedText iname = new FormattedText(DataModel.GetHumanReadableProjectName(item.Key), CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                        double ll = iname.Width;
                        if (ll > projectNameMaxLen)
                            projectNameMaxLen = ll;

                        if (item.Value.Item1 > maxBuildOrderNbr)
                            maxBuildOrderNbr = item.Value.Item1;
                    }
                    if (DataModel.IsBuilding)
                    {
                        maxTick = (maxTick / tickStep + 1) * tickStep;
                    }

                    Spacings spacings = new Spacings();
                    { //setting spacings
                        string pattern = "> ";
                        int len = maxBuildOrderNbr.ToString().Length + pattern.Length;
                        pattern = pattern.PadLeft(len, '8'); // let's assume that 8 is the widest char
                        FormattedText bn = new FormattedText(pattern, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);

                        spacings.lProjName = Spacings.lOrder + bn.Width + 3; // let's add 3 pix just in case 8 is not the widest
                        spacings.lGanttC = spacings.lProjName + projectNameMaxLen + penThickness + 3; // let's add 3 pix just in case
                    }

                    // check if usage text is longer than the longest project name, and yes, values greater than 1000% are possible
                    double usageTextLen = new FormattedText("HDD Usage (Avg. 1000%)", CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush).Width;
                    spacings.lGanttC = Math.Max(spacings.lGanttC, Spacings.lOrder + usageTextLen + penThickness);

                    int rowNbr = 0; //first row has number 0

                    { // Draw header
                        DrawSeparator(drawingContext, rowNbr); // draw backgroud things first due to anty-aliasing
                        DrawMachineInfo(drawingContext, rowNbr);

                        rowNbr++;
                    }

                    foreach (BuildInfo item in DataModel.FinishedBuilds)
                    {
                        DrawSeparator(drawingContext, rowNbr); // draw backgroud things first due to anty-aliasing
                        DrawOrderAndProjectNameText(drawingContext, rowNbr, item.ProjectBuildOrderNumber, item.ProjectName, spacings, (item.success ? greenSolidBrush : redSolidBrush));
                        DrawBar(drawingContext, rowNbr, item.begin, item.end, maxTick, spacings, (item.success ? greenGradientBrush : redGradientBrush), DataModel.CriticalPath.Contains(item));

                        rowNbr++;
                    }

                    foreach (KeyValuePair<string, Tuple<uint, long>> item in DataModel.CurrentBuilds)
                    {
                        DrawSeparator(drawingContext, rowNbr); // draw backgroud things first due to anty-aliasing
                        DrawOrderAndProjectNameText(drawingContext, rowNbr, item.Value.Item1, DataModel.GetHumanReadableProjectName(item.Key), spacings, blueSolidBrush);
                        DrawBar(drawingContext, rowNbr, item.Value.Item2, (nowTick - DataModel.StartTime.Ticks), maxTick, spacings, blueGradientBrush, false /*markAsCriticalPath*/);

                        rowNbr++;
                    }

                    DrawSeparator(drawingContext, rowNbr); // draw backgroud things first due to anty-aliasing
                    DrawGraph("CPU usage", drawingContext, DataModel.CpuUsage, cpuPen, cpuSoftPen, rowNbr, RenderSize, rowHeight, spacings, maxTick, nowTick, fontFace);
                    rowNbr++;

                    DrawSeparator(drawingContext, rowNbr); // draw backgroud things first due to anty-aliasing
                    DrawGraph("HDD usage", drawingContext, DataModel.HddUsage, hddPen, hddSoftPen, rowNbr, RenderSize, rowHeight, spacings, maxTick, nowTick, fontFace);
                    rowNbr++;

                    if (DataModel.CurrentBuilds.Count > 0 || DataModel.FinishedBuilds.Count > 0)
                    {
                        string status = (DataModel.IsBuilding ? "Building..." : "Done.");
                        if (DataModel.MaxParallelBuilds > 0)
                            status += " Max. no. of parallel projects: " + DataModel.MaxParallelBuilds.ToString() + " (Avg. " + 
                                Math.Round(DataModel.MaxParallelBuilds/100.0* DataModel.PercentageProcessorUse(), 1).ToString(format: "0.0") + ")";

                        DrawText(drawingContext, status, rowNbr, Spacings.lOrder, blackBrush);
                    }

                    DrawVerticalSeparator(drawingContext, spacings.lGanttC - penThickness, rowNbr, false /*wholeSize*/);

                    drawingContext.DrawLine(new Pen(atLeastOneError ? redSolidBrush : greenSolidBrush, 1), new Point(0, rowNbr * rowHeight), new Point(RenderSize.Width, rowNbr * rowHeight));

                    DateTime dt = new DateTime(maxTick);
                    string s = Utils.SecondsToString(dt.Ticks);
                    FormattedText maxTime = new FormattedText(s, CultureInfo.CurrentCulture, FlowDirection.LeftToRight, fontFace, FontSize, blackBrush);
                    double m = maxTime.Width;
                    drawingContext.DrawText(maxTime, new Point(RenderSize.Width - m - Spacings.rGanttC, rowNbr * rowHeight));

                    { // Draw frame around - it looks better in window and definitelly in saved .png. Draw frame as last to be clearly visible
                        DrawSeparator(drawingContext, -1);                                                                   // Top
                        DrawSeparator(drawingContext, rowNbr);                                                               // Bottom
                        DrawVerticalSeparator(drawingContext, 0, rowNbr, true /*wholeSize*/);                                // Left
                        DrawVerticalSeparator(drawingContext, RenderSize.Width - penThickness, rowNbr, true /*wholeSize*/);  // Right
                    }

                    isGraphDrawnScope.IsDrawn = true;
                } //End of IsGraphDrawnScope
            }
            catch (Exception)
            {
                Debug.Assert(false, "Gantt chart not refreshed! Exception thrown while drawing Gantt chart.");
            }
        }

        #endregion Main Draw Method

        #region Events Handling

        private void RefreshTimerEventTick(object sender, ElapsedEventArgs e)
        {
            GraphControl.Instance.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
                 new System.Action(() =>
                 {
                     GraphControl.Instance.InvalidateVisual();
                 }));
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property.Name == "Foreground")
            {
                OnForegroundChanged();
            }
            base.OnPropertyChanged(e);
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
            cpuPen = new Pen(new SolidColorBrush(isDark ? Color.FromRgb(200, 135, 200) : Color.FromRgb(90, 40, 90)), 1.0);
            cpuSoftPen = new Pen(new SolidColorBrush(isDark ? Color.FromRgb(90, 40, 90) : Color.FromRgb(200, 135, 200)), 1.0);
            hddPen = new Pen(new SolidColorBrush(isDark ? Color.FromRgb(130, 196, 255) : Color.FromRgb(0, 56, 106)), 1.0);
            hddSoftPen = new Pen(new SolidColorBrush(isDark ? Color.FromRgb(0, 56, 106) : Color.FromRgb(130, 196, 255)), 1.0);
        }

        #endregion Events Handling
    }
}
