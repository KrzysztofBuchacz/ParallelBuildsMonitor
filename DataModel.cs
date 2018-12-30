using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Timers;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Container to hold information about build for one project
    /// </summary>
    public struct BuildInfo
    {
        public BuildInfo(string n, long b, long e, bool s)
        {
            name = n;
            begin = b;
            end = e;
            success = s;
        }
        public long begin;
        public long end;
        public string name;
        public bool success;
    }

    /// <summary>
    /// Container to holds all build statistics data
    /// </summary>
    class DataModel
    {
        #region Properties

        public string SolutionName { get; private set; } 
        /// <summary>
        /// Holds point in time when entire build started (Solution).
        /// </summary>
        public DateTime StartTime { get; private set; }
        public ReadOnlyDictionary<string, DateTime> CurrentBuilds { get { return new ReadOnlyDictionary<string, DateTime>(currentBuilds); } }
        public ReadOnlyCollection<BuildInfo> FinishedBuilds { get { return finishedBuilds.AsReadOnly(); } }

        public ReadOnlyCollection<Tuple<long, float>> CpuUsage { get { return cpuUsage.AsReadOnly(); } }
        public ReadOnlyCollection<Tuple<long, float>> HddUsage { get { return hddUsage.AsReadOnly(); } }
        public int MaxParallelBuilds { get; private set; } = 0;
        public int AllProjectsCount { get; private set; } = 0;

        #endregion Properties

        #region Members

        private Dictionary<string, DateTime> currentBuilds = new Dictionary<string, DateTime>();
        private List<BuildInfo> finishedBuilds = new List<BuildInfo>();

        private List<Tuple<long, float>> cpuUsage = new List<Tuple<long, float>>();
        private List<Tuple<long, float>> hddUsage = new List<Tuple<long, float>>();

        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter hddCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        private static double performanceTimerInterval = 1000; // 1000 means collect data every 1s.
        private System.Timers.Timer performanceTimer = new System.Timers.Timer(performanceTimerInterval);

        #endregion Members

        #region Creator and Constructors

        private DataModel()
        {
            performanceTimer.Elapsed += new ElapsedEventHandler(PerformanceTimerEventTick);
        }

        private static DataModel instance = null;
        // Singleton
        public static DataModel Instance
        {
            get
            {
                if (instance == null)
                    instance = new DataModel();
                return instance;
            }
        }

        public void Reset()
        {
            //instance = new DataModel(); // It doesn't work! So do it manually...

            StartTime = DateTime.Now;
            MaxParallelBuilds = 0;
            AllProjectsCount = 0;
            currentBuilds.Clear();
            finishedBuilds.Clear();
            cpuUsage.Clear();
            hddUsage.Clear();
        }
        #endregion

        #region Manipulation

        /// <summary>
        /// Call this method when starting colleting statistics for solution (.sln)
        /// </summary>
        public void BuildBegin(string solutionName, int allProjectsCount)
        {
            Reset();
            SolutionName = solutionName;
            StartTime = DateTime.Now;
            this.AllProjectsCount = allProjectsCount;
            performanceTimer.Start();
            CollectPerformanceData(); // Collect 1st sample. Second will be taken after performanceTimerInterval
        }

        public void BuildDone()
        {
            performanceTimer.Stop();
        }

        /// <summary>
        /// Call this method when new project (inside solution) starts building.
        /// </summary>
        /// <param name="projectKey"></param>
        public void AddCurrentBuild(string projectKey)
        {
            currentBuilds[projectKey] = DateTime.Now;
            if (currentBuilds.Count > MaxParallelBuilds)
            {
                MaxParallelBuilds = currentBuilds.Count;
            }
        }

        /// <summary>
        /// This method move project from CurrentBuilds to FinishedBuilds array
        /// </summary>
        /// <param name="projectKey"></param>
        /// <returns>true on success (when project was successfully moved from CurrentBuilds to FinishedBuilds array</returns>
        public bool FinishCurrentBuild(string projectKey, bool wasBuildSucceessful)
        {
            if (!CurrentBuilds.ContainsKey(projectKey))
                return false;

            DateTime start = new DateTime(CurrentBuilds[projectKey].Ticks - StartTime.Ticks);
            currentBuilds.Remove(projectKey);
            DateTime end = new DateTime(DateTime.Now.Ticks - StartTime.Ticks);
            finishedBuilds.Add(new BuildInfo(projectKey, start.Ticks, end.Ticks, wasBuildSucceessful));
            TimeSpan s = end - start;
            DateTime t = new DateTime(s.Ticks);

            return true;
        }

        #endregion Manipulation

        #region HelperMethods

        /// <summary>
        /// Number of time (in %) for how long max parallel builds were run during solution buils.
        /// </summary>
        /// <returns></returns>
        public long PercentageProcessorUse()
        {
            long percentage = 0;
            if (MaxParallelBuilds > 0)
            {
                long nowTicks = DateTime.Now.Ticks;
                long maxTick = 0;
                long totTicks = 0;
                foreach (BuildInfo info in FinishedBuilds)
                {
                    totTicks += info.end - info.begin;
                    if (info.end > maxTick)
                    {
                        maxTick = info.end;
                    }
                }
                foreach (DateTime start in CurrentBuilds.Values)
                {
                    maxTick = nowTicks - StartTime.Ticks;
                    totTicks += nowTicks - start.Ticks;
                }
                totTicks /= MaxParallelBuilds;
                if (maxTick > 0)
                {
                    percentage = totTicks * 100 / maxTick;
                }
            }
            return percentage;
        }

        #endregion HelperMethods

        #region CPU, HDD Performance

        private void PerformanceTimerEventTick(object sender, ElapsedEventArgs e)
        {
            CollectPerformanceData();
        }

        private void CollectPerformanceData()
        {
            long ticks = DateTime.Now.Ticks;
            cpuUsage.Add(new Tuple<long, float>(ticks, cpuCounter.NextValue()));
            hddUsage.Add(new Tuple<long, float>(ticks, hddCounter.NextValue()));
        }

        #endregion CPU, HDD Performance
    }
}
