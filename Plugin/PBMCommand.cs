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
        private static DTE2 Dte { get; set; }
        private static EnvDTE.SolutionEvents SolutionEvents { get; set; }  //DTE2.Events.SolutionEvents must be called only once. Calling several times makes events stop working. That is why it is kept as member (call like "DTE2.Events.SolutionEvents.AfterClosing += solutionEvents_AfterClosing;" just NOT work)
        private static EnvDTE.BuildEvents BuildEvents { get; set; }        //DTE2.Events.BuildEvents    must be called only once. Calling several times makes events stop working. That is why it is kept as member (call like "DTE2.Events.BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;" just NOT work)
        private static DataModel DataModel { get { return DataModel.Instance; } } // Convinient accessor to data.

        #endregion Members

        #region Initialize-Uninitialize

        /// <summary>
        /// 
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static bool Initialize(Microsoft.VisualStudio.Shell.Package package)
        {
            if (package == null)
                return false;

            if (Package != null)
                return false; // Protection against double initialization (double subscription to events, double menus etc)

            Package = package;
            Dte = (DTE2)ServiceProvider.GetService(typeof(SDTE));

            bool res = SubscribeUnsubscibeApiEvents(true /*subscibe*/);
            res &= CreateDestroyMenu(true /*create*/);

            return res;
        }

        public static bool Uninitialize()
        {
            bool res = CreateDestroyMenu(false /*create*/);
            res &= SubscribeUnsubscibeApiEvents(false /*subscibe*/);

            Dte = null;
            Package = null;

            return res;
        }


        private static bool SubscribeUnsubscibeApiEvents(bool subscibe)
        {
            if (Dte == null)
                return false;

            // Solution Events Callbacks
            SolutionEvents = Dte.Events.SolutionEvents;
            if (subscibe)
            {
                SolutionEvents.AfterClosing += solutionEvents_AfterClosing;
            }
            else
            {
                SolutionEvents.AfterClosing -= solutionEvents_AfterClosing;
            }

            // Project Events Callbacks
            BuildEvents = Dte.Events.BuildEvents;
            if (subscibe)
            {
                BuildEvents.OnBuildBegin += BuildEvents_OnBuildBegin;
                BuildEvents.OnBuildDone += BuildEvents_OnBuildDone;
                BuildEvents.OnBuildProjConfigBegin += BuildEvents_OnBuildProjConfigBegin;
                BuildEvents.OnBuildProjConfigDone += BuildEvents_OnBuildProjConfigDone;
            }
            else
            {
                BuildEvents.OnBuildBegin -= BuildEvents_OnBuildBegin;
                BuildEvents.OnBuildDone -= BuildEvents_OnBuildDone;
                BuildEvents.OnBuildProjConfigBegin -= BuildEvents_OnBuildProjConfigBegin;
                BuildEvents.OnBuildProjConfigDone -= BuildEvents_OnBuildProjConfigDone;
            }

            return true;
        }

        #endregion Initialize-Uninitialize

        #region Menu

        [Guid("048AF9A5-402D-4441-B221-5EEC9ACD93DB")]
        public enum ContextMenuCommandSet
        {
            idContextMenu = 0x1000,
            SaveAsPng = 0x0101,
            SaveAsCsv = 0x0102
        }

        //TODO: Is it correct implementation of destroy?
        private static bool CreateDestroyMenu(bool create)
        {
            OleMenuCommandService commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService == null)
                return false;

            // Save As .png
            var SaveAsPngCommandID = new CommandID(typeof(ContextMenuCommandSet).GUID, (int)ContextMenuCommandSet.SaveAsPng);
            var menuItemSaveAsPng = new OleMenuCommand(SaveAsPng, SaveAsPngCommandID);
            menuItemSaveAsPng.BeforeQueryStatus += MenuItemSaveAsPng_BeforeQueryStatus;
            if (create)
                commandService.AddCommand(menuItemSaveAsPng);
            else
                commandService.RemoveCommand(menuItemSaveAsPng);

            // Save As .csv
            var SaveAsCsvCommandID = new CommandID(typeof(ContextMenuCommandSet).GUID, (int)ContextMenuCommandSet.SaveAsCsv);
            var menuItemSaveAsCsv = new OleMenuCommand(SaveAsCsv, SaveAsCsvCommandID);
            menuItemSaveAsCsv.BeforeQueryStatus += MenuItemSaveAsCsv_BeforeQueryStatus;
            if (create)
                commandService.AddCommand(menuItemSaveAsCsv);
            else
                commandService.RemoveCommand(menuItemSaveAsCsv);

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
        static void BuildEvents_OnBuildProjConfigBegin(string ProjectUniqueName, string ProjectConfig, string Platform, string SolutionConfig)
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
        static void BuildEvents_OnBuildProjConfigDone(string ProjectUniqueName, string ProjectConfig, string Platform, string SolutionConfig, bool Success)
        {
            DataModel.FinishCurrentBuild(ProjectUniqueName, Success);
        }

        static void BuildEvents_OnBuildBegin(vsBuildScope Scope, vsBuildAction Action)
        {
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
        static void BuildEvents_OnBuildDone(vsBuildScope Scope, vsBuildAction Action)
        {
            bool findAndSetCriticalPath = ((Action == vsBuildAction.vsBuildActionBuild) || (Action == vsBuildAction.vsBuildActionRebuildAll)); // There is no Critical Path for vsBuildAction.vsBuildActionClean. Clean is done sequentially.
            DataModel.BuildDone(GetProjectDependenies(), findAndSetCriticalPath);
            GraphControl.Instance?.BuildDone();
        }

        /// <summary>
        /// Event called on Closing Solution without closing VS ("VS -> File -> Close Solution").
        /// </summary>
        /// <remarks>
        /// Clear data and graph from prevously executed build.
        /// </remarks>
        static void solutionEvents_AfterClosing()
        {
            DataModel.Reset();
            GraphControl.Instance?.InvalidateVisual();
        }

        #endregion IdeEvents

        #region HelperMethods

        public static EnvDTE.OutputWindowPane GetOutputBuildPane()
        {
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
