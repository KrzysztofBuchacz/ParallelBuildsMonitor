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
        #region Members
        //DO NOT SUBMIT!!  Restrict access to get; set; to some members
        public DateTime buildTime;
        public Dictionary<string, DateTime> currentBuilds = new Dictionary<string, DateTime>();
        public List<BuildInfo> finishedBuilds = new List<BuildInfo>();
        public List<Tuple<long, float>> cpuUsage = new List<Tuple<long, float>>();
        public List<Tuple<long, float>> hddUsage = new List<Tuple<long, float>>();
        public int maxParallelBuilds = 0;
        public int allProjectsCount = 0;
        public int outputCounter = 0;

        PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        PerformanceCounter hddCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        public Timer timer = new Timer();

        #endregion Members

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

            buildTime = DateTime.Now;
            CollectPerformanceData(true);
            maxParallelBuilds = 0;
            allProjectsCount = 0;
            outputCounter = 0;
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
        public void BuildBegin(int allProjectsCount)
        {
            Reset();
            buildTime = DateTime.Now;
            this.allProjectsCount = allProjectsCount;

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
            currentBuilds[projectKey] = DateTime.Now;
            if (currentBuilds.Count > maxParallelBuilds)
            {
                maxParallelBuilds = currentBuilds.Count;
            }
        }

        /// <summary>
        /// This method move project from currentBuilds to finishedBuilds array
        /// </summary>
        /// <param name="projectKey"></param>
        /// <returns>true on success (when project was successfully moved from currentBuilds to finishedBuilds array</returns>
        public bool FinishCurrentBuild(string projectKey, bool wasBuildSucceessful)
        {
            if (!currentBuilds.ContainsKey(projectKey))
                return false;

            outputCounter++;
            DateTime start = new DateTime(currentBuilds[projectKey].Ticks - buildTime.Ticks);
            currentBuilds.Remove(projectKey);
            DateTime end = new DateTime(DateTime.Now.Ticks - buildTime.Ticks);
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
            if (maxParallelBuilds > 0)
            {
                long nowTicks = DateTime.Now.Ticks;
                long maxTick = 0;
                long totTicks = 0;
                foreach (BuildInfo info in finishedBuilds)
                {
                    totTicks += info.end - info.begin;
                    if (info.end > maxTick)
                    {
                        maxTick = info.end;
                    }
                }
                foreach (DateTime start in currentBuilds.Values)
                {
                    maxTick = nowTicks - buildTime.Ticks;
                    totTicks += nowTicks - start.Ticks;
                }
                totTicks /= maxParallelBuilds;
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
            long sleep = SleepTime(cpuUsage.Count);
            long ticks = DateTime.Now.Ticks;
            if (forceAdd || cpuUsage.Count == 0 || ticks > cpuUsage[cpuUsage.Count - 1].Item1 + sleep)
                cpuUsage.Add(new Tuple<long, float>(ticks, cpuCounter.NextValue()));
            sleep = SleepTime(hddUsage.Count);
            ticks = DateTime.Now.Ticks;
            if (forceAdd || hddUsage.Count == 0 || ticks > hddUsage[hddUsage.Count - 1].Item1 + sleep)
                hddUsage.Add(new Tuple<long, float>(ticks, hddCounter.NextValue()));
        }

        #endregion CPU+HDDPerformance
    }
}
