using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;


namespace ParallelBuildsMonitorTests
{
    /// <summary>
    /// Compare <c>Dictionary</c>ies by values, not by reference as C# default is.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    public class DictionaryComparer<TKey, TValue> : IEqualityComparer<Dictionary<TKey, TValue>>
    {
        private IEqualityComparer<TValue> valueComparer;
        public DictionaryComparer(IEqualityComparer<TValue> valueComparer = null)
        {
            this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }

        public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
        {
            if ((x == null) && (y == null))
                return true;

            if ((x == null) || (y == null))
                return false;

            if (x.Count != y.Count)
                return false;

            if (x.Keys.Except(y.Keys).Any())
                return false;

            if (y.Keys.Except(x.Keys).Any())
                return false;

            foreach (KeyValuePair<TKey, TValue> pair in x)
            {
                if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                    return false;
            }
            return true;
        }

        public int GetHashCode(Dictionary<TKey, TValue> obj)
        {
            if (obj == null)
                return 0;
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Compare <c>List</c>s by values, not by reference as C# default is.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ListComparer<T> : IEqualityComparer<List<T>>
    {
        private IEqualityComparer<T> valueComparer;
        public ListComparer(IEqualityComparer<T> valueComparer = null)
        {
            this.valueComparer = valueComparer ?? EqualityComparer<T>.Default;
        }

        public bool Equals(List<T> x, List<T> y)
        {
            if ((x == null) && (y == null))
                return true;

            if ((x == null) || (y == null))
                return false;

            if (x.Count != y.Count)
                return false;

            for (int ii = 0; ii < x.Count; ii++)
            {
                if (x[ii].ToString() != y[ii].ToString())
                    return false;
            }

            return true;
        }

        public int GetHashCode(List<T> obj)
        {
            if (obj == null)
                return 0;
            return obj.GetHashCode();
        }
    }

    /// <summary>
    /// Provide static methods that compare nested <c>Dictionary</c>ies and <c>List</c>s.
    /// </summary>
    public static class ComparisonMethods
    {
        /// <summary>
        /// Compare Dictionary with nested List, by values in those collections (not by reference as default implementation)
        /// </summary>
        /// <remarks>
        /// Comparision:
        ///     - Dictionary keys can be in arbitrary order
        ///     - List elements must be in the same sequence
        ///     - strings are compare as case sensitive
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="W"></typeparam>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <returns></returns>
        public static bool CollectionAreEquals<T, W>(Dictionary<T, List<W>> dict1, Dictionary<T, List<W>> dict2)
        {
            return (new DictionaryComparer<T, List<W>>(new ListComparer<W>())).Equals(dict1, dict2);
        }

        /// <summary>
        /// Compare Dictionary with nested Dictionary, by values in those collections (not by reference as default implementation)
        /// </summary>
        /// <remarks>
        /// Comparision:
        ///     - Dictionary keys can be in arbitrary order
        ///     - strings are compare as case sensitive
        ///     - comparison nested Dictionary have the same rules as outside Dictionary
        /// </remarks>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="W"></typeparam>
        /// <param name="dict1"></param>
        /// <param name="dict2"></param>
        /// <returns></returns>
        public static bool CollectionAreEquals<T, U, W>(Dictionary<T, Dictionary<U, W>> dict1, Dictionary<T, Dictionary<U, W>> dict2)
        {
            return (new DictionaryComparer<T, Dictionary<U, W>>(new DictionaryComparer<U, W>())).Equals(dict1, dict2);
        }
    }


    /// <summary>
    /// Other test helper methods.
    /// </summary>
    public static class TestUtils
    {
        static string CurrentFileName([System.Runtime.CompilerServices.CallerFilePath] string fileName = "")
        {
            return fileName;
        }

        /// <summary>
        /// Return absolute path to file file with test data.
        /// </summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public static string GetTestFile(string fileName)
        {
            string testDirectory = Path.GetDirectoryName(CurrentFileName());

            return Path.Combine(testDirectory, fileName ?? "");
        }

        /// <summary>
        /// Ensures the hardware-dependent MachineInfo line contains all required fields and that each field value is correctly formatted.
        /// </summary>
        /// Validates:
        ///     - line contains every required field,
        ///     - no missing values,
        ///     - value is in appropriate format (number or string)
        ///     - value itself is NOT compared
        public static bool ValidateMachineInfoLine(string line)
        {
            if (string.IsNullOrEmpty(line))
                return false;

            // Check for required fields with flexible value matching
            var requiredPatterns = new[]
            {
                @"CRICTICAL PATH for",
                @"Build Started:\s*\d{4}-\d{2}-\d{2}\s+\d{2}:\d{2}:\d{2}",
                @"Processors:\s*\d+",
                @"Cores:\s*\d+",
                @"CPU Speed:\s*[\d.]+\s*GHz",
                @"Hyper Threading:\s*(Enabled|Disabled)",
                @"RAM:\s*\d+\s*(GB|TB)",
                @"HDD:\s*\d+\s*SSD"
            };

            var regex = new System.Text.RegularExpressions.Regex(@"[\r\n]");
            foreach (var pattern in requiredPatterns)
            {
                if (!System.Text.RegularExpressions.Regex.IsMatch(line, pattern))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Compares CSV content where the first line contains hardware-specific values.
        /// </summary>
        /// The first line which contains MachineInfo that is hardware-specific is smartly compared by ValidateMachineInfoLine().
        /// All subsequent lines must match exactly.
        /// <param name="current">The actual CSV output</param>
        /// <param name="expected">The expected CSV output</param>
        /// <returns>True if comparison succeeds, false otherwise</returns>
        public static bool CompareCsvWithMachineInfo(string current, string expected)
        {
            var currentLines = current.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            var expectedLines = expected.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            // Both must have at least the hardware info line
            if (currentLines.Length == 0 || expectedLines.Length == 0)
                return false;

            // Validate first line has required hardware fields (but don't compare exact values)
            if (!ValidateMachineInfoLine(currentLines[0]))
                return false;

            // Both must have the same number of lines
            if (currentLines.Length != expectedLines.Length)
                return false;

            // Compare all remaining lines (line 1 onwards) exactly
            for (int i = 1; i < currentLines.Length; i++)
            {
                if (currentLines[i] != expectedLines[i])
                    return false;
            }

            return true;
        }
    }
}
