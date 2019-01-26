namespace ParallelBuildsMonitor
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.VisualStudio.Shell;

    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// IMPORTANT NOTE:
    ///     "Parallel Builds Monitor" frame header might be visible (e.g.: when tabbed tougheder with "Output" frame),
    ///     but it does NOT contain PBMWindow object unless it contents is not visible on screen. It means that
    ///     PBMWindow() constructor, PreProcessMessage() and OnCreate() are never called.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("55ff594a-8ecc-4b14-9c9a-45f869673d62")]
    public class PBMWindow : ToolWindowPane
    {
        public const string Title = "Parallel Builds Monitor";

        /// <summary>
        /// Initializes a new instance of the <see cref="PBMWindow"/> class.
        /// </summary>
        public PBMWindow() : base(null)
        {
        }

        protected override void OnCreate()
        {
            this.Caption = Title;

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            this.Content = new PBMControl();
        }

        ///// <summary>
        ///// This method is called only when Visual Studio is closed, NOT when "Parallel Builds Monitor" pane/frame is closed!
        ///// </summary>
        //protected override void OnClose()
        //{
        //}
    }
}
