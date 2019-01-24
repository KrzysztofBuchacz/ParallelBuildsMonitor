using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.Win32;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideMenuResource("Menus.ctmenu", 1)]
    // Not sure if VsDockStyle.Linked is the correctly selected value
    [ProvideToolWindow(typeof(PBMWindow), Style = VsDockStyle.Linked, DockedHeight = 200, Window = "DocumentWell", Orientation = ToolWindowOrientation.Bottom)]
    [Guid(Package.PackageGuidString)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.None)] // When "PackageAutoLoadFlags.BackgroundLoad" is used "ToolWindowPane window = package.FindToolWindow(typeof(PBMWindow), 0, true);" falls into infinite loop, not sure why because before "await JoinableTaskFactory.SwitchToMainThreadAsync();" is called and debugger shows that "ToolWindowPane window = package.FindToolWindow(typeof(PBMWindow), 0, true);" is executed in MainThread...
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class Package : Microsoft.VisualStudio.Shell.AsyncPackage
    {
        /// <summary>
        /// ParallelBuildsMonitorWindowPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "3fe81f94-00df-4a3d-bff1-ae20f305aedb";


        public Package()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }


        // Commented out below methods are proper initialization in Microsoft.VisualStudio.Shell.15 (VS 2017),
        // however PBM is suportting .14 (VS2015), so we can't use them, becasue they are missing in .14 (VS2015) interface.
        // VS 2017 SDK example is here: https://github.com/Microsoft/VSSDK-Extensibility-Samples/tree/master/AsyncToolWindow
        //public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
        //{
        //    return toolWindowType.Equals(Guid.Parse(typeof(PBMWindow).GUID)) ? this : null;
        //}
        //
        //protected override string GetToolWindowTitle(Type toolWindowType, int id)
        //{
        //    return toolWindowType == typeof(PBMWindow) ? PBMWindow.Title : base.GetToolWindowTitle(toolWindowType, id);
        //}
        //
        //protected override async Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        //{
        //    // Perform as much work as possible in this method which is being run on a background thread.
        //    // The object returned from this method is passed into the constructor of the PBMWindow 
        //    var dte = await GetServiceAsync(typeof(EnvDTE.DTE)) as EnvDTE80.DTE2;
        //
        //    return new PBMWindowState
        //    {
        //        DTE = dte
        //    };
        //}

        /// <summary>
        /// Asynchronic Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <remarks>
        /// This method is called in Background Thread!
        /// <para>
        /// This method is called:
        ///     - when user click on "VS -> Menu -> View -> Other Windows -> Parallel Builds Monitor" menu item (menu item is added to VS menu by other means)
        ///     - if attribute [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, PackageAutoLoadFlags.None)] is applied to AsyncPackage class
        /// </para>
        /// <para>
        /// This method is NOT called:
        ///     - "Parallel Builds Monitor" pane is available but not active (e.g.: tabbed together with "Output" pange and "Output" pane active).
        ///         Such state is achieved e.g.: when previous session of Visual Studio is left with with "Output" pane active and "Parallel Builds Monitor"
        ///         is tabbed with it. That occur even when [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExistsAndFullyLoaded_string, ...] is applied.
        ///         This method will be called when solution (SolutionExistsAndFullyLoaded_string) is loaded to VS.
        /// </para>
        /// </remarks>
        protected override async System.Threading.Tasks.Task InitializeAsync(System.Threading.CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync();
            await ShowToolWindow.InitializeAsync(this);
        }

        ///// <summary>
        ///// This method is called only when Visual Studio is closed, NOT when "Parallel Builds Monitor" pane/frame is closed!
        ///// </summary>
        //protected override int QueryClose(out bool canClose)
        //{
        //    return base.QueryClose(out canClose);
        //}
    }


    /// <summary>
    /// Two goals:
    ///     1) Adding "Parallel Builds Monitor" menu item into "VS -> Menu -> View -> Other Windows" menu
    ///     2) When "VS -> Menu -> View -> Other Windows -> Parallel Builds Monitor" is clicked,
    ///         showing "Parallel Builds Monitor" tool window.
    /// </summary>
    internal sealed class ShowToolWindow
    {
        [Guid("0617d7cf-8a0f-436f-8b05-4be366046686")]
        public enum MainMenuCommandSet
        {
            ShowToolWindow = 0x0100
        }

        public static async System.Threading.Tasks.Task InitializeAsync(AsyncPackage package)
        {
            { // Adding callback method to "Parallel Builds Monitor" menu item into "VS -> Menu -> View -> Other Windows" menu
                IMenuCommandService commandService = (IMenuCommandService)await package.GetServiceAsync(typeof(IMenuCommandService));
                CommandID menuCommandID = new CommandID(typeof(MainMenuCommandSet).GUID, (int)MainMenuCommandSet.ShowToolWindow);
                MenuCommand menuItem = new MenuCommand((s, e) => Execute(package), menuCommandID);
                commandService.AddCommand(menuItem);
            }

            { // Start listening VS Events...
                // Should we always collect data even when "Parallel Builds Monitor" pane is closed?
                // What if user open PBM pane in the the middle of build? Should we show Gantt chart or draw notice like "Restart build to see results"?
                // If we decide not to collect data when PBM pane is closed, then user must manually activate PBM before build in order to have Gantt.
                // This is because "Output" pane is left as active after each build, so "Output" pane will be active pane after VS restart.
                PBMCommand.Initialize(package);
            }

            { // Show and Activate "Parallel Builds Monitor" pane
                // Do we really want to activate "Parallel Builds Monitor" pane after each solution load?
                // Or maybe we want to do that only once after installation?
                // Will it work when PBM plugin is installed whn solution is already opened?
                //Execute(package);
            }
        }

        // This is proper initialization since VS2017.
        //    For details see comment to GetAsyncToolWindowFactory() method.
        //private static void Execute(AsyncPackage package)
        //{
        //    package.JoinableTaskFactory.RunAsync(async () =>
        //    {
        //        ToolWindowPane window = await package.ShowToolWindowAsync(              // MISSING METHOD IN Microsoft.VisualStudio.Shell.14 in AsyncPackage class. Available since .15
        //            typeof(PBMWindow),
        //            0,
        //            create: true,
        //            cancellationToken: package.DisposalToken);
        //    });
        //}


        /// <summary>
        /// Shows "Parallel Builds Monitor" tool window when "VS -> Menu -> View -> Other Windows -> Parallel Builds Monitor" is clicked.
        /// </summary>
        private static void Execute(AsyncPackage package)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            ToolWindowPane window = package.FindToolWindow(typeof(PBMWindow), 0, true);
            if ((null == window) || (null == window.Frame))
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            IVsWindowFrame windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }

    //// see InitializeToolWindowAsync() method for details about this
    //public class PBMWindowState
    //{
    //    public EnvDTE80.DTE2 DTE { get; set; }
    //}
}
