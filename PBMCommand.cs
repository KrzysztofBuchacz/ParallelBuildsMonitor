using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System.IO;
using System.Text;
using System.Timers;
using EnvDTE;
using EnvDTE80;
using System.Collections.Generic;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class PBMCommand
    {
        #region Constants

        [Guid("0617d7cf-8a0f-436f-8b05-4be366046686")]
        public enum MainMenuCommandSet
        {
            ShowToolWindow = 0x0100
        }

        [Guid("048AF9A5-402D-4441-B221-5EEC9ACD93DB")]
        public enum ContextMenuCommandSet
        {
            idContextMenu = 0x1000,
            Save = 0x0101
        }

        #endregion Constants

        #region Members

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Microsoft.VisualStudio.Shell.Package package;
        public EnvDTE.SolutionEvents solutionEvents;
        public EnvDTE.BuildEvents buildEvents;

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
                var menuCommandID = new CommandID(typeof(MainMenuCommandSet).GUID, (int)MainMenuCommandSet.ShowToolWindow);
                var menuItem = new OleMenuCommand(this.ShowToolWindow, menuCommandID);
                commandService.AddCommand(menuItem);

                var contextCommandID = new CommandID(typeof(ContextMenuCommandSet).GUID, (int)ContextMenuCommandSet.Save);
                var menuItemSave = new OleMenuCommand(this.SaveGraph, contextCommandID);
                menuItemSave.BeforeQueryStatus += MenuItemSave_BeforeQueryStatus;
                commandService.AddCommand(menuItemSave);
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

        private void MenuItemSave_BeforeQueryStatus(object sender, EventArgs e)
        {
            var myCommand = sender as OleMenuCommand;
            if (null != myCommand)
            {
                myCommand.Enabled = ViewModel.Instance.IsGraphDrawn;
            }
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

        private void SaveGraph(object sender, EventArgs e)
        {
            PBMWindow window = this.package.FindToolWindow(typeof(PBMWindow), 0, true) as PBMWindow;
            PBMControl control = window.Content as PBMControl;
            control?.SaveGraph();
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
            ToolWindowPane window = this.package.FindToolWindow(typeof(PBMWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }

        #region IdeEvents

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ProjectUniqueName">The same as <c>Project.UniqueName</c> property</param>
        /// <param name="ProjectConfig"></param>
        /// <param name="Platform"></param>
        /// <param name="SolutionConfig"></param>
        void BuildEvents_OnBuildProjConfigBegin(string ProjectUniqueName, string ProjectConfig, string Platform, string SolutionConfig)
        {
            DataModel.AddCurrentBuild(ProjectUniqueName);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ProjectUniqueName">The same as <c>Project.UniqueName</c> property</param>
        /// <param name="ProjectConfig"></param>
        /// <param name="Platform"></param>
        /// <param name="SolutionConfig"></param>
        /// <param name="Success"></param>
        void BuildEvents_OnBuildProjConfigDone(string ProjectUniqueName, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            DataModel.FinishCurrentBuild(ProjectUniqueName, Success);
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

        /// <summary>
        /// Get build dependencies.
        /// </summary>
        /// <returns>Dictionary where key is <c>Project.UniqueName</c> 
        /// and value is list of projects that this projects depend on in <c>Project.UniqueName</c> form</returns>
        private Dictionary<string, List<string>> GetProjectDependenies()
        {
            Dictionary<string, List<string>> deps = new Dictionary<string, List<string>>();

            DTE2 dte = (DTE2)(package as IServiceProvider).GetService(typeof(SDTE));
            foreach (BuildDependency bd in dte.Solution.SolutionBuild.BuildDependencies)
            {
                string name = bd.Project.UniqueName;
                List<string> RequiredProjects = new List<string>();
                if (bd.RequiredProjects is Array dep)
                {
                    foreach (Project proj in dep)
                        RequiredProjects.Add(proj.UniqueName);
                }

                deps.Add(name, RequiredProjects);
            }

            return deps;
        }


        /// <summary>
        /// <c>BuildEvents_OnBuildDone</c> is called when solution build is finished.
        /// </summary>
        /// <param name="Scope"></param>
        /// <param name="Action"></param>
        void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            DataModel.SetProjectDependenies(GetProjectDependenies());
            DataModel.BuildDone();
            GraphControl.Instance.BuildDone();
        }

        /// <summary>
        /// Event called on Closing Solution without closing VS ("VS -> File -> Close Solution").
        /// </summary>
        /// <remarks>
        /// Clear data and graph from prevously executed build.
        /// </remarks>
        void solutionEvents_AfterClosing()
        {
            DataModel.Reset();
            GraphControl.Instance.InvalidateVisual();
        }

        #endregion IdeEvents

        #region HelperMethods

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
