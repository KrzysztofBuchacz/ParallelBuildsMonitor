using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ParallelBuildsMonitor.Events
{
    internal class BuildEvents : IVsUpdateSolutionEvents2
    {
        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            PBMCommand.BuildEvents_OnBuildBegin();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            PBMCommand.BuildEvents_OnBuildDone();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }

        private string ProjectFullPath(IVsHierarchy pHierProj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            object saveName;
            pHierProj.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_SaveName, out saveName);
            object projectDir;
            pHierProj.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out projectDir);
            return projectDir.ToString() + saveName.ToString();
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            PBMCommand.BuildEvents_OnBuildProjConfigBegin(ProjectFullPath(pHierProj));
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            PBMCommand.BuildEvents_OnBuildProjConfigDone(ProjectFullPath(pHierProj), fSuccess != 0);
            return VSConstants.S_OK;
        }
    }
}
