using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelBuildsMonitor
{
    public abstract class SaveCsv
    {
        static public void SaveAsCsv()
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = DataModel.Instance.GetSaveFileNamePrefix(),
                DefaultExt = ".csv",                      // Default file extension
                Filter = "Comma-separated values|*.csv"   // Filter files by extension
            };

            if (dlg.ShowDialog() != true)
                return;

            SaveCsv.SaveCriticalPathAsCsv(dlg.FileName);
        }

        static public bool SaveCriticalPathAsCsv(string FileName)
        {
            ReadOnlyCollection<BuildInfo> criticalPath = DataModel.Instance.CriticalPath;
            if (criticalPath.Count < 1)
            {
                Debug.Assert(false, "SaveCriticalPathAsCsv() failed! No data in DataModel.CriticalPath. Fix data or disable command.");
                return false;
            }

            // Sort by Build time descending
            List<BuildInfo> criticalPathSorted = new List<BuildInfo>(criticalPath);
            criticalPathSorted.Sort(new ShorterElapsedTimeFirstInTheListComparer());
            criticalPathSorted.Reverse();

            try
            {
                using (StreamWriter file = new StreamWriter(FileName, false /*false means overwrite existing file*/))
                {
                    string solutionNameWithMachineInfo = "CRICTICAL PATH for " + DataModel.Instance.GetSolutionNameWithMachineInfo("  |  ", true /*WithBuildStartedStr*/);

                    file.WriteLine(EscapeRow(new List<object>() { solutionNameWithMachineInfo }));
                    file.WriteLine(EscapeRow(new List<object>() { "Build Order", "Project Name", "Build Time in [s] "+ Environment.NewLine + "(Sorted Descending)", "Start Time [s]", "End Time [s]" }));

                    foreach (BuildInfo bi in criticalPathSorted)
                    {
                        file.WriteLine(EscapeRow(new List<object>() { criticalPath.IndexOf(bi) + 1, bi.ProjectName, Utils.TicksToSeconds(bi.ElapsedTime), Utils.TicksToSeconds(bi.begin), Utils.TicksToSeconds(bi.end) })); // DO NOT SUBMIT!!!  Is 1[s] enough resolution?
                    }
                }
            }
            catch
            {
                return false;
            }

            return true;
        }

        static public string EscapeCell(object CellContent)
        {
            return string.Format("\"{0}\"", CellContent);
        }

        static public string EscapeRow(IEnumerable<object> RowContent)
        {
            List<string> cells = new List<string>();
            foreach (object item in RowContent)
            {
                cells.Add(EscapeCell(item));
            }

            return string.Join(", ", cells);
        }

    }
}
