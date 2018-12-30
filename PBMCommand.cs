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
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PBMCommand
    {
        #region Members

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
        private readonly Microsoft.VisualStudio.Shell.Package package;
        public EnvDTE.SolutionEvents solutionEvents;
        public EnvDTE.BuildEvents buildEvents;
        public static string addinName = "VSBuildMonitor";  //is it used anywhere?
        public static string commandToggle = "ToggleCPPH";  //is it used anywhere?
        public static string commandFixIncludes = "FixIncludes";   //is it used anywhere?
        public static string commandFindReplaceGUIDsInSelection = "FindReplaceGUIDsInSelection";   //is it used anywhere?

        #endregion Members

        #region Properties

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PBMCommand Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        public IServiceProvider ServiceProvider { get { return this.package; } }

        /// <summary>
        /// Convinient accessor to data.
        /// </summary>
        private static DataModel DataModel { get { return DataModel.Instance; } }

        #endregion Properties

        #region Initialization
        /// <summary>
        /// Initializes a new instance of the <see cref="PBMCommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PBMCommand(Microsoft.VisualStudio.Shell.Package package)
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
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            Instance = new PBMCommand(package);
        }
        #endregion Initialization

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
            ToolWindowPane window = this.package.FindToolWindow(typeof(PBMWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        #region IdeEvents

        void BuildEvents_OnBuildProjConfigBegin(string Project, string ProjectConfig, string Platform, string SolutionConfig)
        {
            DataModel.AddCurrentBuild(MakeKey(Project, ProjectConfig, Platform));
        }

        void BuildEvents_OnBuildProjConfigDone(string Project, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            string projectKey = MakeKey(Project, ProjectConfig, Platform);
            if (DataModel.FinishCurrentBuild(projectKey, Success))
            {
                GraphControl.Instance.InvalidateVisual(); //TODO. When GraphControl observe DataModel this is redundant
            }
        }

        void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
            DTE2 dte = (DTE2)(package as IServiceProvider).GetService(typeof(SDTE));
            int allProjectsCount = 0;
            foreach (Project project in dte.Solution.Projects)
                allProjectsCount += GetProjectsCount(project);
            DataModel.BuildBegin(System.IO.Path.GetFileName(dte.Solution.FileName), allProjectsCount);
            GraphControl.Instance.BuildBegin();
        }

        void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            DataModel.BuildDone();
            GraphControl.Instance.BuildDone();
        }

        void solutionEvents_AfterClosing()
        {
            DataModel.Reset();
            GraphControl.Instance.InvalidateVisual();
        }

        #endregion IdeEvents

        #region HelperMethods

        string MakeKey(string Project, string ProjectConfig, string Platform)
        {
            FileInfo fi = new FileInfo(Project);
            string key = fi.Name;// + "|" + ProjectConfig + "|" + Platform;
            return key;
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

        #endregion HelperMethods


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
    }
}
