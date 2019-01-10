using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Container to hold information about build for one project
    /// </summary>
    public struct BuildInfo
    {
        public BuildInfo(string projectUniqueName, string projectName, uint projectBuildOrderNumber, long b, long e, bool s)
        {
            ProjectUniqueName = projectUniqueName;
            ProjectName = projectName;
            ProjectBuildOrderNumber = projectBuildOrderNumber;
            begin = b;
            end = e;
            success = s;
        }

        /// <summary>
        /// Project stored in <c>Project.UniqueName</c> format.
        /// </summary>
        public string ProjectUniqueName { get; set; } // Probably should be renamed to UniqueName and BuildInfo into ProjectBuildInfo

        /// <summary>
        /// Project name in human readable format
        /// </summary>
        public string ProjectName { get; set; } // Probably should be renamed to Name and BuildInfo into ProjectBuildInfo

        /// <summary>
        /// Project build order number, e.g. "1>..." that shows in the Output Build Pane/Window during build.
        /// </summary>
        public uint ProjectBuildOrderNumber { get; set; } // Probably should be renamed to BuildOrderNumber and BuildInfo into ProjectBuildInfo

        /// <summary>
        /// Start project building time in <c>DateTime.Ticks</c> units
        /// </summary>
        /// <remarks>
        /// This is relative time counted since <c>DataModel.StartTime</c>
        /// </remarks>
        public long begin;

        /// <summary>
        /// End project building time in <c>DateTime.Ticks</c> units
        /// </summary>
        /// <remarks>
        /// This is relative time counted since <c>DataModel.StartTime</c>
        /// </remarks>
        public long end;

        /// <summary>
        /// Was build successful?
        /// </summary>
        public bool success;

        /// <summary>
        /// Return how long took project build in <c>DateTime.Ticks</c> units
        /// </summary>
        public long ElapsedTime  //TODO: Should it be renamed to BuildTime?
        {
            get
            {
                return end - begin;
            }
        }
    }

    #region Sorting

    public class ShorterElapsedTimeFirstInTheListComparer : IComparer<BuildInfo>
    {
        public int Compare(BuildInfo x, BuildInfo y)
        {
            long xEl = x.ElapsedTime;
            long yEl = y.ElapsedTime;

            if (xEl > yEl)
                return 1;

            if (xEl < yEl)
                return -1;

            return 0;
        }
    }

    // Let's keep this code in comment since:
    //      - it was tested and it is OK
    //      - but it never return 0, so sorting twice when there is the same value give different results!
    //public class FirstStartedFirstInTheListComparer : IComparer<BuildInfo>
    //{
    //    public int Compare(BuildInfo x, BuildInfo y)
    //    {
    //        //1[s] = 10000000[ticks] (1e7)
    //        if (Math.Abs(x.begin - y.begin) < 10000) // the same statring time
    //            return (x.end < y.end) ? -1 : 1; // shorter first on list
    //        return (x.begin < y.begin) ? -1 : 1;
    //    }
    //}

    #endregion Sorting
}
