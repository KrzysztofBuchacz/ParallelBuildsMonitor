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
        static public void SaveAsCsv(string outputPaneContent)
        {
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog
            {
                FileName = DataModel.Instance.GetSaveFileNamePrefix(),
                DefaultExt = ".csv",                      // Default file extension
                Filter = "Comma-separated values|*.csv"   // Filter files by extension
            };

            if (dlg.ShowDialog() != true)
                return;

            SaveCsv.SaveCriticalPathAsCsv(dlg.FileName, outputPaneContent);
        }

        static public bool SaveCriticalPathAsCsv(string FileName, string outputPaneContent)
        {
            bool result = true;
            ReadOnlyCollection<BuildInfo> criticalPath = DataModel.Instance.CriticalPath;
            if (criticalPath.Count < 1)
            {
                Debug.Assert(false, "SaveCriticalPathAsCsv() failed! No data in DataModel.CriticalPath. Fix data or disable command.");
                return false;
            }


            Dictionary<int, List<int?>> detailedReport = null;
            List<string> detailedReportCoulmnNames = null;
            { // Prepare dara for "Build Timing" detailed report
                HashSet<int> only = new HashSet<int>();
                foreach (BuildInfo bi in criticalPath)
                {
                    only.Add((int)(bi.ProjectBuildOrderNumber));
                }

                result &= SaveCsv.GetBuildTimingsFromOutput(outputPaneContent, only, out Dictionary<int, Dictionary<string, int>> all, out Dictionary<string, int> sums);
                Dictionary<string, int> columnIndex = GetColumIndexesAndNamesFromSums(sums, out detailedReportCoulmnNames);
                result &= SaveCsv.SortBuildTimings(all, columnIndex, out detailedReport);

                if (detailedReportCoulmnNames != null)
                {
                    for (int idx = 0; idx < detailedReportCoulmnNames.Count; idx++)
                    {
                        if (detailedReportCoulmnNames[idx] != null)
                            detailedReportCoulmnNames[idx] += " [ms]";
                    }
                }

                if ((detailedReport == null) || (detailedReportCoulmnNames == null))
                {
                    if ((detailedReport != null) || (detailedReportCoulmnNames != null))
                        Debug.Assert(false, "Problem while saving Critical Path .csv report. Bug in SaveCriticalPathAsCsv() method. Something wrong while processing \"Build Timing\" detailed report data.");

                    detailedReport = null;
                    detailedReportCoulmnNames = null;
                }
            }

            bool wasSolutionBuildSuccessful = !(DataModel.Instance.FinishedBuilds.Any(bi => bi.success != true));

            // Sort by Build time descending
            List<BuildInfo> criticalPathSorted = new List<BuildInfo>(criticalPath);
            criticalPathSorted.Sort(new ShorterElapsedTimeFirstInTheListComparer());
            criticalPathSorted.Reverse();

            // SummaryDetailedColumnSeparator
            string sdColumnSeparator = null;

            try
            {
                using (StreamWriter file = new StreamWriter(FileName, false /*false means overwrite existing file*/))
                {
                    string solutionNameWithMachineInfo = "CRICTICAL PATH for " + DataModel.Instance.GetSolutionNameWithMachineInfo("  |  ", true /*WithBuildStartedStr*/);

                    file.WriteLine(EscapeRow(new List<object>() { solutionNameWithMachineInfo }));

                    if (!wasSolutionBuildSuccessful)
                    {
                        file.WriteLine(EscapeRow(null));
                        file.WriteLine(EscapeRow(new List<object> { "WARNING! One or more projects build failed! Report does not reflect real build times!" }));
                        file.WriteLine(EscapeRow(null));
                    }

                    List<object> summaryDetailedPreHeader = new List<object> { "Summary Report:" };
                    if (detailedReport != null)
                    {
                        summaryDetailedPreHeader.AddRange(new List<object> { null, null, null, null }); // rest columns for "Summary Report:" 
                        summaryDetailedPreHeader.Add(sdColumnSeparator);
                        summaryDetailedPreHeader.Add("Detailed Report: (Columns Sorted Descending according to sum for column)");
                    }

                    List<object> summaryDetailedHeader = new List<object> { "Critical Path Order", "Project Name", "Build Time in [s] " /*+ Environment.NewLine this should be here but it does NOT work in Excel. It works correctly on Google Doc */ + "(Sorted Descending)", "Start Time [s]", "End Time [s]" };
                    if (detailedReportCoulmnNames != null)
                    {
                        summaryDetailedHeader.Add(sdColumnSeparator);
                        summaryDetailedHeader.AddRange(detailedReportCoulmnNames);
                    }

                    file.WriteLine(EscapeRow(summaryDetailedPreHeader));
                    file.WriteLine(EscapeRow(summaryDetailedHeader));

                    foreach (BuildInfo bi in criticalPathSorted)
                    {
                        List<object> data = new List<object>
                        {
                            criticalPath.IndexOf(bi) + 1, bi.ProjectName, Utils.TicksToSeconds(bi.ElapsedTime), Utils.TicksToSeconds(bi.begin), Utils.TicksToSeconds(bi.end)
                        };
                        if (detailedReport != null)
                        {
                            data.Add(sdColumnSeparator);
                            int projectBuildOrderNumber = (int)(bi.ProjectBuildOrderNumber);
                            if (detailedReport.ContainsKey(projectBuildOrderNumber))
                            { // C# project doesn't contain detailed reports. Skip them.
                                foreach (int? value in detailedReport[projectBuildOrderNumber]) 
                                    data.Add(value);
                            }
                        }

                        file.WriteLine(EscapeRow(data));
                    }
                }
            }
            catch
            {
                Debug.Assert(false, "Problem while saving Critical Path .csv report. Bug in SaveCriticalPathAsCsv() method. Problem with algorithm that create .csv table.");
                return false;
            }

            return result;
        }

        static public string EscapeCell(object CellContent)
        {
            if (CellContent == null)
                return String.Empty;

            return string.Format("\"{0}\"", CellContent);
        }

        static public string EscapeRow(IEnumerable<object> RowContent)
        {
            if (RowContent == null)
                return String.Empty;

            List<string> cells = new List<string>();
            foreach (object item in RowContent)
                cells.Add(EscapeCell(item));

            return string.Join(", ", cells);
        }

        #region Parsing Output Build Pane/Window

        /// <summary>
        /// Parse Visual Studio Output Build and return "Build Timing" informations for all projects, when "Build Timing" 
        /// is turned on (VS -> Tools -> Options -> Projects and Solutions -> VC++ Project Settings -> Build Timing)
        /// </summary>
        /// <param name="outputStr">String possessed from VS "Output" window "Build" pane.</param>
        /// <param name="only">When empty, collect "Build Timing" for all projects.
        ///                    When contain project build order number collect only "Build Timing" for mentioned projects.</param>
        /// <param name="all">Output dictionary, where outer dictioanry key (<c>int</c>) is project build order number.
        ///                   Inner dictionary contain parameters, as <c>string</c> keys
        ///                   and their corresponding values values, as <c>int</c> values in [ms] units.</param>
        /// <param name="sums">Output dictionary, where key (<c>string</c>) is parameter name, which is also column name, 
        ///                    value (<c>int</c>) which is sum of this parameter across all projects.</param>
        /// <returns>false when there were problems with parsing (e.g. could not cast to int)</returns>
        public static bool GetBuildTimingsFromOutput(string outputStr, HashSet<int> only, out Dictionary<int, Dictionary<string, int>> all, out Dictionary<string, int> sums)
        {
            bool wasAllParseOK = true;

            if (outputStr == null)
            {
                all = null;
                sums = null;
                return true;
            }

            all = new Dictionary<int, Dictionary<string, int>>(); // <c>int</c> is project order e.g. 2, <c>string</c> is key e.g. "ClCompile", <c>int</c> is value e.g. "3960"
            sums = new Dictionary<string, int>();

            string[] output = outputStr.Split(new string[] { Environment.NewLine }, StringSplitOptions.None);

            int sectionID = 0;
            Dictionary<string, int> section = null; // <c>string</c> is key e.g. "ClCompile", <c>int</c> is value e.g. "3960"
            bool collect = false;
            foreach (string oo in output)
            {
                if (oo.Contains("Task Performance Summary"))
                {
                    string sectionIdStr = oo.Substring(0, oo.IndexOf('>'));
                    if (!int.TryParse(sectionIdStr, out sectionID))
                    {
                        wasAllParseOK = false;
                        continue;
                    }

                    if ((only != null) && (only.Count > 0) && (!only.Contains(sectionID)))
                        continue;

                    section = new Dictionary<string, int>(); // <c>string</c> is key e.g. "ClCompile", <c>int</c> is value e.g. "3960"
                    collect = true;
                    continue;
                }

                if (!oo.Contains(" ms "))
                {
                    if (collect)
                    { // End of section, time to store data
                        all[sectionID] = section;

                        sectionID = 0;
                        section = null;
                        collect = false;
                    }
                    continue;
                }

                if (collect)
                {
                    List<string> items = new List<string>(oo.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
                    if (items.Count < 4)
                    {
                        wasAllParseOK = false;
                        continue;
                    }

                    if (!int.TryParse(items[1], out int value))
                    {
                        wasAllParseOK = false;
                        continue;
                    }

                    section[items[3]] = value;

                    if (sums.ContainsKey(items[3]))
                        sums[items[3]] += value;
                    else
                        sums[items[3]] = value;
                }
            }

            if (collect)
                wasAllParseOK = false;

            if ((all.Count == 0) != (sums.Count == 0))
                Debug.Assert(false, "Detailed Report will be inacurate or missing! Failure when processing Build from Output Pane/Window.");

            if (all.Count == 0)
                all = null;

            if (sums.Count == 0)
                sums = null;

            return wasAllParseOK;
        }

        /// <summary>
        /// Transforms <c>sums</c> dictionary into <c>columIndexes</c> dictionary.
        /// e.g.: 
        ///     <c>sums</c> = { "Link", 628 }, { "AssignCulture", 0 }, { "CL", 3956 }
        /// will be transformed to:
        ///     <c>columIndexes</c> = { "CL", 0 }, { "Link", 1 }, { "AssignCulture", 2 }
        /// </summary>
        /// <param name="sums">Input dictionary, where key (<c>string</c>) is parameter name, which is also column name, 
        ///                    value (<c>int</c>) which is sum of this parameter across all projects.</param>
        /// <param name="columnNames">List of column names ordered acoording the same rules as <c>sums</c>.</param>
        /// <returns>Dictionary where key is column name and value is column index starting from 0</returns>
        public static Dictionary<string, int> GetColumIndexesAndNamesFromSums(Dictionary<string, int> sums, out List<string> columnNames)
        {
            if (sums == null)
            {
                columnNames = null;
                return null;
            }

            Dictionary<string, int> sortedDict = new Dictionary<string, int>();
            columnNames = new List<string>();

            List<KeyValuePair<string, int>> sorted = (from kv in sums orderby kv.Value descending, kv.Key select kv).ToList();
            for (int i = 0; i < sorted.Count; i++)
            {
                sortedDict.Add(sorted[i].Key, i);
                columnNames.Add(sorted[i].Key);
            }

            return sortedDict;
        }

        /// <summary>
        /// Transform <c>all</c> data into <c>sorted</c> data, which ones are in table format.
        /// <c>sums</c> is used to have columns in <c>sorted</c> in descending order.
        /// When data is missing in <c>all</c>, it will be reflected as <c>null</c> in <c>sorted</c> dictionary.
        /// </summary>
        /// <param name="all">Input dictionary, where outer dictioanry key (<c>int</c>) is project build order number.
        ///                   Inner dictionary contain parameters, as <c>string</c> keys
        ///                   and their corresponding values values, as <c>int</c> values.</param>
        /// <param name="columnIndex">Input dictionary, where key (<c>string</c>) is parameter name, which is also column name 
        ///                    and value (<c>int</c>) is column index for this parameter.</param>
        /// <param name="throwAssert">Internal use only. When <c>true</c>, <c>Assert()</c> is thrown, for some specific situation.
        ///                           When <c>false</c>, <c>Assert()</c> is never thrown</param>
        /// <returns><c>true</c> when sorting was fully successfull. <c>false</c> when something went wrong.
        ///          Usually it means that provided <c>sums</c> collection is incomplete.</returns>
        public static bool SortBuildTimings(Dictionary<int, Dictionary<string, int>> all, IDictionary<string, int> columnIndex, out Dictionary<int, List<int?>> sorted, bool throwAssert = true)
        {
            bool res = true;
            if (all == null)
            {
                sorted = null;
                return true;
            }

            if (all.Count == 0)
            {
                sorted = new Dictionary<int, List<int?>>();
                return true;
            }

            if (columnIndex == null)
            {
                Debug.Assert(!throwAssert, "Missing Build Timing key in sums! Some values will be missing in .csv report!");
                sorted = null;
                return false; //without indexes we can't continue. Number of parameters is changing from project to project. Without <c> columnIndex </c> [ms] value land in wrong column
            }

            sorted = new Dictionary<int, List<int?>>();
            foreach (KeyValuePair<int, Dictionary<string, int>> proj in all)
            { //sort proj.Value

                int?[] sortedValues = new int?[columnIndex.Count];
                for (int i = 0; i < sortedValues.Length; i++)
                    sortedValues[i] = null;

                foreach (KeyValuePair<string, int> value in proj.Value)
                {
                    if (!columnIndex.ContainsKey(value.Key))
                    {
                        Debug.Assert(!throwAssert, "Missing Build Timing key in sums! Some values will be missing in .csv report!");
                        res = false;
                        continue;
                    }

                    sortedValues[columnIndex[value.Key]] = value.Value;
                }

                sorted[proj.Key] = sortedValues.ToList();
            }

            return res;
        }

        #endregion Parsing Output Build Pane/Window
    }
}
