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

        private string ProjectUniqueName(IVsHierarchy pHierProj)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            pHierProj.GetProperty(VSConstants.VSITEMID_ROOT, (int)__VSHPROPID.VSHPROPID_ExtObject, out object project);
            return (project as EnvDTE.Project).UniqueName;
        }

        public int UpdateProjectCfg_Begin(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, ref int pfCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            PBMCommand.BuildEvents_OnBuildProjConfigBegin(ProjectUniqueName(pHierProj));
            return VSConstants.S_OK;
        }

        public int UpdateProjectCfg_Done(IVsHierarchy pHierProj, IVsCfg pCfgProj, IVsCfg pCfgSln, uint dwAction, int fSuccess, int fCancel)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            dwLastAction = dwAction;
            PBMCommand.BuildEvents_OnBuildProjConfigDone(ProjectUniqueName(pHierProj), fSuccess != 0);
            return VSConstants.S_OK;
        }
    }
}
