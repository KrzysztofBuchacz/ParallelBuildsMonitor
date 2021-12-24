using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using EnvDTE;
using System.Collections.Generic;
using System.Diagnostics;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Goal of this class is:
    ///     - subscrible/unsubscribe to Visual Studio Events
    ///     - provide access to IServiceProvider interface for other classes
    ///     - create menu
    ///     - provide commands and query statuses for menu items
    /// </summary>
    internal static class PBMCommand
    {
        #region Members

        private static Microsoft.VisualStudio.Shell.Package Package { get; set; }
        public static IServiceProvider ServiceProvider { get { return Package; } }
        private static EnvDTE80.DTE2 Dte { get; set; }
        private static Events.SolutionEvents SolutionEvents { get; set; }
        private static Events.BuildEvents BuildEvents { get; set; }
        private static DataModel DataModel { get { return DataModel.Instance; } } // Convinient accessor to data.

        #endregion Members

        #region Initialize

        /// <summary>
        /// 
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (package == null)
                return;

            if (Package != null)
                return; // Protection against double initialization (double subscription to events, double menus etc)

            Package = package;
            Dte = ServiceProvider.GetService(typeof(DTE)) as EnvDTE80.DTE2;
            Assumes.Present(Dte);

            var svc = ServiceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(svc);

            SolutionEvents = new ParallelBuildsMonitor.Events.SolutionEvents();

            svc.AdviseSolutionEvents(SolutionEvents, out _);

            var svb = ServiceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager;
            Assumes.Present(svb);

            BuildEvents = new ParallelBuildsMonitor.Events.BuildEvents();
            svb.AdviseUpdateSolutionEvents(BuildEvents, out _);

            CreateMenu();
        }

        #endregion Initialize

        #region Menu

        [Guid("048AF9A5-402D-4441-B221-5EEC9ACD93DB")]
        public enum ContextMenuCommandSet
        {
            idContextMenu = 0x1000,
            SaveAsPng = 0x0101,
            SaveAsCsv = 0x0102
        }

        private static bool CreateMenu()
        {
            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return false;

            // Save As .png
            var SaveAsPngCommandID = new CommandID(typeof(ContextMenuCommandSet).GUID, (int)ContextMenuCommandSet.SaveAsPng);
            var menuItemSaveAsPng = new OleMenuCommand(SaveAsPng, SaveAsPngCommandID);
            menuItemSaveAsPng.BeforeQueryStatus += MenuItemSaveAsPng_BeforeQueryStatus;
            commandService.AddCommand(menuItemSaveAsPng);

            // Save As .csv
            var SaveAsCsvCommandID = new CommandID(typeof(ContextMenuCommandSet).GUID, (int)ContextMenuCommandSet.SaveAsCsv);
            var menuItemSaveAsCsv = new OleMenuCommand(SaveAsCsv, SaveAsCsvCommandID);
            menuItemSaveAsCsv.BeforeQueryStatus += MenuItemSaveAsCsv_BeforeQueryStatus;
            commandService.AddCommand(menuItemSaveAsCsv);

            return true;
        }

        private static void MenuItemSaveAsPng_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand myCommand)
                myCommand.Enabled = ViewModel.Instance.IsGraphDrawn;
        }

        private static void SaveAsPng(object sender, EventArgs e)
        {
            try
            {
                PBMWindow window = Package.FindToolWindow(typeof(PBMWindow), 0, true) as PBMWindow;
                PBMControl control = window?.Content as PBMControl;
                control?.SaveGraph(null /*pathToPngFile*/);
            }
            catch
            {
                Debug.Assert(false, "Saving Gantt chart as .png failed! Exception thrown while trying save .png file.");
            }
        }

        private static void MenuItemSaveAsCsv_BeforeQueryStatus(object sender, EventArgs e)
        {
            if (sender is OleMenuCommand myCommand)
                myCommand.Enabled = (DataModel.CriticalPath.Count > 0);
        }

        private static void SaveAsCsv(object sender, EventArgs e)
        {
            try
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                string outputPaneContent = GetAllTextFromPane(GetOutputBuildPane()); // Output Build Pane/Window can be cleared even during build, so this is not perfect solution...
                SaveCsv.SaveAsCsv(outputPaneContent);
            }
            catch
            {
                Debug.Assert(false, "Saving .csv failure! Exception thrown while trying save .csv file.");
            }
        }

        #endregion Menu

        #region IdeEvents

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ProjectUniqueName">The same as <c>Project.UniqueName</c> property</param>
        /// <param name="ProjectConfig"></param>
        /// <param name="Platform"></param>
        /// <param name="SolutionConfig"></param>
        public static void BuildEvents_OnBuildProjConfigBegin(string ProjectUniqueName)
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
        public static void BuildEvents_OnBuildProjConfigDone(string ProjectUniqueName, bool Success)
        {
            DataModel.FinishCurrentBuild(ProjectUniqueName, Success);
        }

        public static void BuildEvents_OnBuildBegin()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            DataModel.BuildBegin(System.IO.Path.GetFileName(Dte.Solution.FileName));
            GraphControl.Instance?.BuildBegin();
        }

        /// <summary>
        /// Get build dependencies.
        /// </summary>
        /// <returns>Dictionary where key is <c>Project.UniqueName</c> 
        /// and value is list of projects that this projects depend on in <c>Project.UniqueName</c> form</returns>
        static private Dictionary<string, List<string>> GetProjectDependenies()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Dictionary<string, List<string>> deps = new Dictionary<string, List<string>>();

            foreach (BuildDependency bd in Dte.Solution.SolutionBuild.BuildDependencies)
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
        public static void BuildEvents_OnBuildDone(uint dwAction)
        {
            // Find critical path only for Build action not for Clean or any other action
            bool findAndSetCriticalPath = (dwAction & (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD) != 0;
            DataModel.BuildDone(GetProjectDependenies(), findAndSetCriticalPath);
            GraphControl.Instance?.BuildDone();
        }

        /// <summary>
        /// Event called on Closing Solution without closing VS ("VS -> File -> Close Solution").
        /// </summary>
        /// <remarks>
        /// Clear data and graph from prevously executed build.
        /// </remarks>
        public static void AfterSolutionClosing()
        {
            DataModel.Reset();
            GraphControl.Instance?.InvalidateVisual();
        }

        #endregion IdeEvents

        #region HelperMethods

        public static EnvDTE.OutputWindowPane GetOutputBuildPane()
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            EnvDTE.OutputWindowPanes panes = Dte.ToolWindows.OutputWindow.OutputWindowPanes;
            foreach (EnvDTE.OutputWindowPane pane in panes)
            {
                if (pane.Name.Contains("Build"))
                    return pane;
            }

            return null;
        }

        public static string GetAllTextFromPane(EnvDTE.OutputWindowPane Pane)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            if (Pane == null)
                return null;

            TextDocument doc = Pane.TextDocument;
            TextSelection sel = doc.Selection;
            sel.StartOfDocument(false);
            sel.EndOfDocument(true);

            string content = sel.Text;

            return content;
        }

        #endregion HelperMethods
    }
}
