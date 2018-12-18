using System;
using System.ComponentModel.Design;
using System.Globalization;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Timers;
using EnvDTE;
using EnvDTE80;
using System.Diagnostics;

namespace ParallelBuildsMonitor
{
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
  /// Command handler
  /// </summary>
  internal sealed class ParallelBuildsMonitorWindowCommand
  {
    /// <summary>
    /// Command ID.
    /// </summary>
    public const int CommandId = 0x0100;

    /// <summary>
    /// Command menu group (command set GUID).
    /// </summary>
    public static readonly Guid CommandSet = new Guid("0617d7cf-8a0f-436f-8b05-4be366046686");

    /// <summary>
    /// VS Package that provides this command, not null.
    /// </summary>
    private readonly Package package;

    public DateTime buildTime;
    public EnvDTE.BuildEvents buildEvents;
    public EnvDTE.SolutionEvents solutionEvents;
    public Dictionary<string, DateTime> currentBuilds = new Dictionary<string, DateTime>();
    public List<BuildInfo> finishedBuilds = new List<BuildInfo>();
    public List<Tuple<DateTime, float, int>> cpuUsage = new List<Tuple<DateTime, float, int>>();
    public List<Tuple<DateTime, float, int>> hddUsage = new List<Tuple<DateTime, float, int>>();
    public static string addinName = "VSBuildMonitor";
    public static string commandToggle = "ToggleCPPH";
    public static string commandFixIncludes = "FixIncludes";
    public static string commandFindReplaceGUIDsInSelection = "FindReplaceGUIDsInSelection";
    public int maxParallelBuilds = 0;
    public int allProjectsCount = 0;
    public int outputCounter = 0;
    PerformanceCounter cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
    PerformanceCounter hddCounter = new PerformanceCounter("PhysicalDisk", "% Disk Time", "_Total");

    /// <summary>
    /// Initializes a new instance of the <see cref="ParallelBuildsMonitorWindowCommand"/> class.
    /// Adds our command handlers for menu (commands must exist in the command table file)
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    private ParallelBuildsMonitorWindowCommand(Package package)
    {
      if (package == null)
      {
        throw new ArgumentNullException("package");
      }

      this.package = package;

      OleMenuCommandService commandService = this.ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
      if (commandService != null)
      {
        var menuCommandID = new CommandID(CommandSet, CommandId);
        var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandID);
        commandService.AddCommand(menuItem);
      }
      DTE2 dte = (DTE2)(package as IServiceProvider).GetService(typeof(SDTE));
      solutionEvents = dte.Events.SolutionEvents;
      solutionEvents.AfterClosing += new _dispSolutionEvents_AfterClosingEventHandler(solutionEvents_AfterClosing);
      buildEvents = dte.Events.BuildEvents;
      buildEvents.OnBuildBegin += new _dispBuildEvents_OnBuildBeginEventHandler(BuildEvents_OnBuildBegin);
      buildEvents.OnBuildDone += new _dispBuildEvents_OnBuildDoneEventHandler(BuildEvents_OnBuildDone);
      buildEvents.OnBuildProjConfigBegin += new _dispBuildEvents_OnBuildProjConfigBeginEventHandler(BuildEvents_OnBuildProjConfigBegin);
      buildEvents.OnBuildProjConfigDone += new _dispBuildEvents_OnBuildProjConfigDoneEventHandler(BuildEvents_OnBuildProjConfigDone);
    }

    /// <summary>
    /// Gets the instance of the command.
    /// </summary>
    public static ParallelBuildsMonitorWindowCommand Instance
    {
      get;
      private set;
    }

    /// <summary>
    /// Gets the service provider from the owner package.
    /// </summary>
    public IServiceProvider ServiceProvider
    {
      get
      {
        return this.package;
      }
    }

    /// <summary>
    /// Initializes the singleton instance of the command.
    /// </summary>
    /// <param name="package">Owner package, not null.</param>
    public static void Initialize(Package package)
    {
      Instance = new ParallelBuildsMonitorWindowCommand(package);
    }

    /// <summary>
    /// Shows the tool window when the menu item is clicked.
    /// </summary>
    /// <param name="sender">The event sender.</param>
    /// <param name="e">The event args.</param>
    private void ShowToolWindow(object sender, EventArgs e)
    {
      // Get the instance number 0 of this tool window. This window is single instance so this instance
      // is actually the only one.
      // The last flag is set to true so that if the tool window does not exists it will be created.
      ToolWindowPane window = this.package.FindToolWindow(typeof(ParallelBuildsMonitorWindow), 0, true);
      if ((null == window) || (null == window.Frame))
      {
        throw new NotSupportedException("Cannot create tool window");
      }

      IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
      Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
    }

    void solutionEvents_AfterClosing()
    {
      currentBuilds.Clear();
      finishedBuilds.Clear();
      cpuUsage.Clear();
      hddUsage.Clear();
      GraphControl.Instance.InvalidateVisual();
    }

    void BuildEvents_OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
    {
      string key = MakeKey(Project, ProjectConfig, Platform);
      if (currentBuilds.ContainsKey(key))
      {
        outputCounter++;
        DateTime start = new DateTime(currentBuilds[key].Ticks - buildTime.Ticks);
        currentBuilds.Remove(key);
        DateTime end = new DateTime(DateTime.Now.Ticks - buildTime.Ticks);
        finishedBuilds.Add(new BuildInfo(key, start.Ticks, end.Ticks, Success));
        TimeSpan s = end - start;
        DateTime t = new DateTime(s.Ticks);
        StringBuilder b = new StringBuilder(outputCounter.ToString("D3"));
        b.Append(" ");
        b.Append(key);
        int space = 50 - key.Length;
        if (space > 0)
        {
          b.Append(' ', space);
        }
        b.Append(" \t");
        b.Append(SecondsToString(start.Ticks));
        b.Append("\t");
        b.Append(SecondsToString(t.Ticks));
        b.Append("\n");
        GraphControl.Instance.InvalidateVisual();
      }
    }

    string MakeKey(string Project, string ProjectConfig, string Platform)
    {
      FileInfo fi = new FileInfo(Project);
            string key = fi.Name;// + "|" + ProjectConfig + "|" + Platform;
      return key;
    }

    void BuildEvents_OnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
    {
      string key = MakeKey(Project, ProjectConfig, Platform);
      currentBuilds[key] = DateTime.Now;
      if (currentBuilds.Count > maxParallelBuilds)
      {
        maxParallelBuilds = currentBuilds.Count;
      }
    }

    void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
    {
      GraphControl.Instance.timer.Enabled = false;
      GraphControl.Instance.isBuilding = false;
      TimeSpan s = DateTime.Now - buildTime;
      DateTime t = new DateTime(s.Ticks);
      string msg = "Build Total Time: " + SecondsToString(t.Ticks) + ", max. number of parallel builds: " + maxParallelBuilds.ToString() + "\n";
      CollectPerformanceData();
      GraphControl.Instance.InvalidateVisual();
    }

    int GetProjectsCount(Project project)
    {
      int count = 0;
      if (project != null)
      {
        if (project.FullName.ToLower().EndsWith(".vcxproj") ||
            project.FullName.ToLower().EndsWith(".csproj") ||
            project.FullName.ToLower().EndsWith(".vbproj"))
          count = 1;
        if (project.ProjectItems != null)
        {
          foreach (ProjectItem projectItem in project.ProjectItems)
          {
            count += GetProjectsCount(projectItem.SubProject);
          }
        }
      }
      return count;
    }

    void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
    {
      buildTime = DateTime.Now;
      maxParallelBuilds = 0;
      allProjectsCount = 0;
      DTE2 dte = (DTE2)(package as IServiceProvider).GetService(typeof(SDTE));
      foreach (Project project in dte.Solution.Projects)
        allProjectsCount += GetProjectsCount(project);
      outputCounter = 0;
      GraphControl.Instance.timer.Enabled = true;
      GraphControl.Instance.timer.Interval = 1000;
      GraphControl.Instance.timer.Elapsed += new ElapsedEventHandler(timer_Tick);
      currentBuilds.Clear();
      finishedBuilds.Clear();
      cpuUsage.Clear();
      hddUsage.Clear();
      CollectPerformanceData();
      GraphControl.Instance.scrollLast = true;
      GraphControl.Instance.isBuilding = true;
      GraphControl.Instance.InvalidateVisual();
    }

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

    public static string SecondsToString(long ticks)
    {
      long seconds = ticks / 10000000;
      string ret;
      if (seconds > 9)
      {
        ret = (seconds % 60).ToString() + "s";
      }
      else if (seconds > 0)
      {
        long dsecs = ticks / 1000000;
        ret = (seconds % 60).ToString() + "." + (dsecs % 10).ToString() + "s";
      }
      else
      {
        long csecs = ticks / 100000;
        ret = (seconds % 60).ToString() + "." + ((csecs % 100) < 10 ? "0" : "") + (csecs % 100).ToString() + "s";
      }
      long minutes = seconds / 60;
      if (minutes > 0)
      {
        ret = (minutes % 60).ToString() + "m" + ret;
        long hours = minutes / 60;
        if (hours > 0)
        {
          ret = hours.ToString() + "h" + ret;
        }
      }
      return ret;
    }

    void CollectPerformanceData()
    {
      float cpuUsageInPercent = cpuCounter.NextValue();
      cpuUsage.Add(new Tuple<DateTime, float, int>(DateTime.Now, cpuUsageInPercent, 0));
      float hddUsageInPercent = hddCounter.NextValue();
      hddUsage.Add(new Tuple<DateTime, float, int>(DateTime.Now, hddUsageInPercent, 0));
    }

    void timer_Tick(object sender, ElapsedEventArgs e)
    {
      GraphControl.Instance.Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background,
           new System.Action(() =>
           {
             CollectPerformanceData();
             GraphControl.Instance.InvalidateVisual();
           }));
    }
  }
}
