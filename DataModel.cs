using System;
using System.Collections.Generic;
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
        #region Members And Properties

        /// <summary>
        /// Holds point in time when entire build started (Solution).
        /// </summary>
        public DateTime StartTime { get; private set; }
        public Dictionary<string, DateTime> CurrentBuilds { get; private set; } = new Dictionary<string, DateTime>(); //TODO: Check if collection can be changed even if private
        public List<BuildInfo> FinishedBuilds { get; private set; } = new List<BuildInfo>(); //TODO: Check if collection can be changed even if private

        public List<Tuple<long, float>> CpuUsage { get; private set; } = new List<Tuple<long, float>>();
        public List<Tuple<long, float>> HddUsage { get; private set; } = new List<Tuple<long, float>>();
        public int MaxParallelBuilds { get; private set; } = 0;
        public int AllProjectsCount { get; private set; } = 0;

        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter hddCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        private Timer timer = new Timer();

        #endregion Members And Properties

        #region Creator and Constructors
        private DataModel()
        {
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
            CollectPerformanceData(true);
            MaxParallelBuilds = 0;
            AllProjectsCount = 0;
            CurrentBuilds.Clear();
            FinishedBuilds.Clear();
            CpuUsage.Clear();
            HddUsage.Clear();
        }
        #endregion

        #region Manipulation

        /// <summary>
        /// Call this method when starting colleting statistics for solution (.sln)
        /// </summary>
        public void BuildBegin(int allProjectsCount)
        {
            Reset();
            StartTime = DateTime.Now;
            this.AllProjectsCount = allProjectsCount;

            timer.Interval = 1000;
            timer.Elapsed += new ElapsedEventHandler(timer_Tick);
            timer.Enabled = true;
        }

        public void BuildDone()
        {
            timer.Enabled = false;
        }

        /// <summary>
        /// Call this method when new project (inside solution) starts building.
        /// </summary>
        /// <param name="projectKey"></param>
        public void AddCurrentBuild(string projectKey)
        {
            CurrentBuilds[projectKey] = DateTime.Now;
            if (CurrentBuilds.Count > MaxParallelBuilds)
            {
                MaxParallelBuilds = CurrentBuilds.Count;
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
            CurrentBuilds.Remove(projectKey);
            DateTime end = new DateTime(DateTime.Now.Ticks - StartTime.Ticks);
            FinishedBuilds.Add(new BuildInfo(projectKey, start.Ticks, end.Ticks, wasBuildSucceessful));
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

        #region CPU+HDDPerformance

        void timer_Tick(object sender, ElapsedEventArgs e)
        {
            CollectPerformanceData(false);
        }

        //TODO: This makes CPU Average is wrong
        long SleepTime(int count)
        {
            long sleep = 10000000; // 1 second
            if (count > 60)
                sleep *= 10; // 10 second
            else if (count > 1800)
                sleep *= 60; // 1 minute
            return sleep;
        }

        public void CollectPerformanceData(bool forceAdd)
        {
            long sleep = SleepTime(CpuUsage.Count);
            long ticks = DateTime.Now.Ticks;
            if (forceAdd || CpuUsage.Count == 0 || ticks > CpuUsage[CpuUsage.Count - 1].Item1 + sleep)
                CpuUsage.Add(new Tuple<long, float>(ticks, cpuCounter.NextValue()));
            sleep = SleepTime(HddUsage.Count);
            ticks = DateTime.Now.Ticks;
            if (forceAdd || HddUsage.Count == 0 || ticks > HddUsage[HddUsage.Count - 1].Item1 + sleep)
                HddUsage.Add(new Tuple<long, float>(ticks, hddCounter.NextValue()));
        }

        #endregion CPU+HDDPerformance
    }
}
