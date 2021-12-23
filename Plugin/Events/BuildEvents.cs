using Microsoft.VisualStudio;
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

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            PBMCommand.BuildEvents_OnBuildProjConfigBegin(pHierProj.GetCanonicalName());
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            PBMCommand.BuildEvents_OnBuildProjConfigDone(pHierProj.GetCanonicalName(), fSuccess != 0);
            return VSConstants.S_OK;
        }
    }
}
