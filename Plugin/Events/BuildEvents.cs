using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ParallelBuildsMonitor.Events
{
    internal class BuildEvents : IVsUpdateSolutionEvents2
    {
        private uint dwLastAction;

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

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            dwLastAction = 0;
            PBMCommand.BuildEvents_OnBuildBegin();
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            PBMCommand.BuildEvents_OnBuildDone(dwLastAction);
            return VSConstants.S_OK;
        }

        private string ProjectFullPath(IVsHierarchy pHierProj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            pHierProj.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_SaveName, out object saveName);
            pHierProj.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ProjectDir, out object projectDir);
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
            dwLastAction = dwAction;
            PBMCommand.BuildEvents_OnBuildProjConfigDone(ProjectFullPath(pHierProj), fSuccess != 0);
            return VSConstants.S_OK;
        }
    }
}
