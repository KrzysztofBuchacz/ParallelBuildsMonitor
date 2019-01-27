using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Timers;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Container to holds all build statistics data
    /// </summary>
    public class DataModel : IDisposable
    {
        #region Properties

        public string SolutionName { get; private set; } 
        /// <summary>
        /// Holds point in time when entire build started (Solution).
        /// </summary>
        public DateTime StartTime { get; private set; }
        public ReadOnlyDictionary<string, Tuple<uint, long>> CurrentBuilds { get { return new ReadOnlyDictionary<string, Tuple<uint, long>>(currentBuilds); } }
        public ReadOnlyCollection<BuildInfo> FinishedBuilds { get { return finishedBuilds.AsReadOnly(); } }
        public ReadOnlyDictionary<string, List<string>> ProjectDependenies { get { return new ReadOnlyDictionary<string, List<string>>(projectDependenies); } }
        public ReadOnlyCollection<BuildInfo> CriticalPath { get { return criticalPath.AsReadOnly(); } }

        public ReadOnlyCollection<Tuple<long, float>> CpuUsage { get { return cpuUsage.AsReadOnly(); } }
        public ReadOnlyCollection<Tuple<long, float>> HddUsage { get { return hddUsage.AsReadOnly(); } }
        public int MaxParallelBuilds { get; private set; } = 0;

        #endregion Properties

        #region Members

        private Dictionary<string, Tuple<uint, long>> currentBuilds = new Dictionary<string, Tuple<uint, long>>(); //<c>string</c> is ProjectUniqueName, <c>uint</c> is project build order number, <c>long</c> is project Start time, relative, counted since <c>DataModel.StartTime</c> in <c>DateTime.Ticks</c> units.
        private List<BuildInfo> finishedBuilds = new List<BuildInfo>();
        private Dictionary<string, List<string>> projectDependenies = new Dictionary<string, List<string>>(); //<c>string</c> is ProjectUniqueName, <c>List<string></c> is list of projects that <c>Key</c> project depends on
        private List<BuildInfo> criticalPath = new List<BuildInfo>();

        private List<Tuple<long, float>> cpuUsage = new List<Tuple<long, float>>();
        private List<Tuple<long, float>> hddUsage = new List<Tuple<long, float>>();

        private PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
        private PerformanceCounter hddCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");
        private static readonly double performanceTimerInterval = 1000; // 1000 means collect data every 1s.
        private System.Timers.Timer performanceTimer = new System.Timers.Timer(performanceTimerInterval);

        static uint projectBuildOrderNumber = 1;

        #endregion Members

        #region For Testing Only

        // Better option is [assembly: InternalsVisibleTo("ParallelBuildsMonitorTests")]...

        public void SetCriticalPath(List<BuildInfo> CriticalPath)
        {
            criticalPath = CriticalPath;
        }

        #endregion For Testing Only

        #region Creator, Constructors

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
            currentBuilds.Clear();
            projectBuildOrderNumber = 0;
            finishedBuilds.Clear();
            criticalPath.Clear();
            cpuUsage.Clear();
            hddUsage.Clear();
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
                cpuCounter.Dispose();
                hddCounter.Dispose();
                performanceTimer.Dispose();
            }

            disposed = true;
        }

        #endregion Dispose

        #region Manipulation

        /// <summary>
        /// Call this method when starting colleting statistics for solution (.sln)
        /// </summary>
        public void BuildBegin(string solutionName)
        {
            Reset();
            SolutionName = solutionName;
            StartTime = DateTime.Now;
            performanceTimer.Start();
            CollectPerformanceData(); // Collect 1st sample. Second will be taken after performanceTimerInterval
        }

        public void BuildDone(Dictionary<string, List<string>> projectDependenies, bool findAndSetCriticalPath)
        {
            this.projectDependenies = projectDependenies;
            performanceTimer.Stop();

            if (findAndSetCriticalPath)
                FindAndSetCriticalPath();
        }

        /// <summary>
        /// Call this method when new project (inside solution) starts building.
        /// </summary>
        /// <param name="projectKey"></param>
        public void AddCurrentBuild(string projectKey)
        {
            // It is assumed that OnBuildProjConfigBegin event come in the same order as numbering (1>...) shows in the Output Build Pane/Window
            currentBuilds[projectKey] = new Tuple<uint, long>(++projectBuildOrderNumber, DateTime.Now.Ticks - StartTime.Ticks);
            if (currentBuilds.Count > MaxParallelBuilds)
            {
                MaxParallelBuilds = currentBuilds.Count;
            }
        }

        /// <summary>
        /// This method move project from CurrentBuilds to FinishedBuilds array
        /// </summary>
        /// <param name="ProjectUniqueName">ProjectUniqueName in <c>Project.UniqueName</c> format</param>
        /// <returns>true on success (when project was successfully moved from CurrentBuilds to FinishedBuilds array</returns>
        public bool FinishCurrentBuild(string ProjectUniqueName, bool wasBuildSucceessful)
        {
            if (!CurrentBuilds.ContainsKey(ProjectUniqueName))
                return false;

            uint projectBuildOrderNumber = CurrentBuilds[ProjectUniqueName].Item1;
            long start = CurrentBuilds[ProjectUniqueName].Item2;
            long end = DateTime.Now.Ticks - StartTime.Ticks;
            currentBuilds.Remove(ProjectUniqueName);
            finishedBuilds.Add(new BuildInfo(ProjectUniqueName, GetHumanReadableProjectName(ProjectUniqueName), projectBuildOrderNumber, start, end, wasBuildSucceessful));

            return true;
        }

        #endregion Manipulation

        #region HelperMethods

        /// <summary>
        /// Return common string with solution name, build start time and machine info.
        /// </summary>
        /// <returns></returns>
        public string GetSolutionNameWithMachineInfo(string Separator, bool WithBuildStartedStr)
        {
            return SolutionName + Separator + ((WithBuildStartedStr) ? "Build Started: " : "") + StartTime.ToString("yyyy-MM-dd HH:mm:ss") + Separator + MachineInfo.Instance.ToString(Separator); // Data format "2018-05-08 01.09.07" to preserve correct sorting
        }

        /// <summary>
        /// Return common part for PBM file names, so they are unambiguous and can be sorted on HDD in easy way.
        /// </summary>
        /// <returns></returns>
        public string GetSaveFileNamePrefix()
        {
            string date = StartTime.ToString("yyyy-MM-dd HH.mm.ss"); //Format "2018-05-08 01.09.07" to preserve correct sorting
            return "PBM " + SolutionName + " " + date;
        }

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
                long firstTick = long.MaxValue;
                long lastTick = 0;
                long totTicks = 0;
                foreach (BuildInfo info in FinishedBuilds)
                {
                    totTicks += info.end - info.begin;
                    if (info.end > lastTick)
                        lastTick = info.end;
                    if (info.begin < firstTick)
                        firstTick = info.begin;
                }
                foreach (Tuple<uint, long> start in CurrentBuilds.Values)
                {
                    lastTick = nowTicks - StartTime.Ticks;
                    totTicks += nowTicks - (start.Item2 + StartTime.Ticks);
                }
                totTicks /= MaxParallelBuilds;
                if (lastTick > firstTick)
                {
                    percentage = totTicks * 100 / (lastTick - firstTick);
                }
            }
            return percentage;
        }

        private BuildInfo? GetFinishedProject(string ProjectUniqueName)
        {
            foreach (BuildInfo proj in finishedBuilds)
            {
                if (proj.ProjectUniqueName == ProjectUniqueName)
                    return proj;
            }

            return null;
        }

        /// <summary>
        /// Find critical path and store it in <c>criticalPath</c> list.
        /// </summary>
        /// <remarks>
        /// <c>ProjectDependenies</c> need to be initialized before calling this method.
        /// </remarks>
        /// <returns><c>true</c> when successfully critical path found and stored in <c>CriticalPath</c> property.</returns>
        private bool FindAndSetCriticalPath()
        {
            criticalPath.Clear();

            if ((finishedBuilds.Count < 1) || (ProjectDependenies.Count < 1))
                return false;

            BuildInfo lastProject = finishedBuilds[finishedBuilds.Count-1];
            criticalPath.Add(lastProject);
            if (finishedBuilds.Count == 1)
                return true; // Case when Build/Rebuild only 1 project

            while (true)
            {
                List<string> precedentProjects = ProjectDependenies[lastProject.ProjectUniqueName];
                if (precedentProjects.Count < 1)
                {
                    criticalPath.Reverse();
                    return true; // No precendent project means that this is first project.
                }

                double minDiff = double.MaxValue;
                BuildInfo minProject = new BuildInfo();
                foreach (string precendentProjectUN in precedentProjects)
                {
                    BuildInfo? precendentProject = GetFinishedProject(precendentProjectUN);
                    if (precendentProject == null)
                    {
                        Debug.Assert(false, "Missing project in finishedBuilds collection. Critical path will be drawn incorrectly!");
                        continue;
                    }

                    long diff = lastProject.begin - precendentProject.Value.end;
                    if (diff < minDiff)
                    {
                        minDiff = diff;
                        minProject = precendentProject.Value;
                    }
                }

                criticalPath.Add(minProject);
                lastProject = minProject;
            }
        }

        /// <summary>
        /// Convert <c>Project.UniqueName</c> into short human readable string
        /// </summary>
        /// <param name="ProjectUniqueName">Project name in <c>Project.UniqueName</c> format</param>
        /// <returns>Human readable project name</returns>
        static public string GetHumanReadableProjectName(string ProjectUniqueName)
        {
            FileInfo fi = new FileInfo(ProjectUniqueName);
            string key = fi.Name;
            return key;
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
