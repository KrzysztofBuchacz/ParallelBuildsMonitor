using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelBuildsMonitorTests;
using System.Collections.Generic;
using System.IO;
using CM = ParallelBuildsMonitorTests.ComparisonMethods;

namespace ParallelBuildsMonitor.Tests
{
    [TestClass()]
    public class CsvTestHelperMethodsTests
    {
        [TestMethod()]
        public void CollectionAssertAreEqualTest_Simple()
        {
            // null checks
            CollectionAssert.AreEqual(null, null);
            CollectionAssert.AreNotEqual(null, new Dictionary<string, int>());
            CollectionAssert.AreNotEqual(null, new Dictionary<string, int> { { "A", 0 } });
            //CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { null, 0 } });             // generic collections does not allow string as null. "new Dictionary<string, int> { { null, 0 } }" is runtime exception. CORRECT
            CollectionAssert.AreNotEqual(new Dictionary<string, string> { { "A", "A" } }, new Dictionary<string, string> { { "A", null } });

            // Difference in length
            CollectionAssert.AreNotEqual(new Dictionary<string, int>(), new Dictionary<string, int> { { "A", 0 } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { "A", 0 }, { "B", 1 } });

            // Difference in key/value
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { "A", 0 } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { "A", 1 } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { "B", 0 } });

            // Different order - actually Dictionary has order
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "B", 1 } }, new Dictionary<string, int> { { "A", 0 }, { "B", 1 } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "B", 1 }, { "A", 0 } }, new Dictionary<string, int> { { "A", 0 }, { "B", 1 } });
            CollectionAssert.AreEquivalent(new Dictionary<string, int> { { "B", 1 }, { "A", 0 } }, new Dictionary<string, int> { { "A", 0 }, { "B", 1 } });

            // Different types
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, long> { { "A", 0 } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, string> { { "A", null } });
            CollectionAssert.AreNotEqual(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, string> { { "A", "0" } });
        }


        [TestMethod()]
        public void CollectionAreEqualsTest_NestedCollection()
        {
            // null checks
            Assert.IsTrue(CM.CollectionAreEquals((Dictionary<string, List<int>>)null, (Dictionary<string, List<int>>)null));
            Assert.IsFalse(CM.CollectionAreEquals(null, new Dictionary<string, List<int>>()));
            Assert.IsFalse(CM.CollectionAreEquals(null, new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }));
            //Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, int> { { "A", 0 } }, new Dictionary<string, int> { { null, 0 } }));             // generic collections does not allow string as null. "new Dictionary<string, int> { { null, 0 } }" is runtime exception. CORRECT
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int>() } }, new Dictionary<string, List<int>> { { "A", null } }));

            // null checks in nested collection
            Assert.IsTrue(CM.CollectionAreEquals(new Dictionary<int, List<int?>> { { 0, new List<int?> { null } } }, new Dictionary<int, List<int?>> { { 0, new List<int?> { null } } }));
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<int, List<int?>> { { 0, new List<int?>() } }, new Dictionary<int, List<int?>> { { 0, new List<int?> { null } } }));
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<int, List<int?>> { { 0, new List<int?> { null, 0 } } }, new Dictionary<int, List<int?>> { { 0, new List<int?> { null, null } } }));

            //// Difference in length
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int>() } }, new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }));
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }, new Dictionary<string, List<int>> { { "A", new List<int> { 0, 0 } } }));

            // Difference in key/value
            Assert.IsTrue(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 2, 5 } } }, new Dictionary<string, List<int>> { { "A", new List<int> { 2, 5 } } }));
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 2 } } }, new Dictionary<string, List<int>> { { "A", new List<int> { 3 } } }));
            Assert.IsFalse(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }, new Dictionary<string, List<int>> { { "B", new List<int> { 0 } } }));

            // Different order - actually Dictionary has order, but CM.CollectionAreEquals() ignore it - correct
            Assert.IsTrue(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "B", new List<int> { 1 } }, { "A", new List<int> { 0 } } }, new Dictionary<string, List<int>> { { "A", new List<int> { 0 } }, { "B", new List<int> { 1 } } }));

            // Different types
            //Assert.IsTrue(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }, new Dictionary<string, List<long>> { { "A", new List<long> { 0 } } }));        // does not compile. CORRECT
            //Assert.IsTrue(CM.CollectionAreEquals(new Dictionary<string, List<int>> { { "A", new List<int> { 0 } } }, new Dictionary<string, List<string>> { { "A", new List<string> { null } } })); // does not compile. CORRECT
        }
    }

    [TestClass()]
    public class SaveCsvTests
    {
        [TestMethod()]
        public void GetBuildTimingsFromOutputTest_NullAndEmpty()
        {
            string outputStr1 = null;
            HashSet<int> only1 = null;
            bool res1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(res1);
            Assert.IsNull(all1);
            Assert.IsNull(sums1);

            string outputStr2 = "";
            HashSet<int> only2 = null;
            bool res2 = SaveCsv.GetBuildTimingsFromOutput(outputStr2, only2, out Dictionary<int, Dictionary<string, int>> all2, out Dictionary<string, int> sums2);
            Assert.IsTrue(res2);
            Assert.IsNull(all2);
            Assert.IsNull(sums2);

            string outputStr3 = "";
            HashSet<int> only3 = new HashSet<int>();
            bool res3 = SaveCsv.GetBuildTimingsFromOutput(outputStr3, only3, out Dictionary<int, Dictionary<string, int>> all3, out Dictionary<string, int> sums3);
            Assert.IsTrue(res3);
            Assert.IsNull(all3);
            Assert.IsNull(sums3);

            string outputStr4 = null;
            HashSet<int> only4 = new HashSet<int>();
            bool res4 = SaveCsv.GetBuildTimingsFromOutput(outputStr4, only4, out Dictionary<int, Dictionary<string, int>> all4, out Dictionary<string, int> sums4);
            Assert.IsTrue(res4);
            Assert.IsNull(all4);
            Assert.IsNull(sums4);

            string outputStr5 = ""
                //+ "1>Task Performance Summary:\r\n"
                + "1>        0 ms  AssignCulture                              1 calls\r\n"
                + "1>      628 ms  Link                                       1 calls\r\n"
                + "1>     3956 ms  CL                                         2 calls\r\n";
            HashSet<int> only5 = new HashSet<int>();
            bool res5 = SaveCsv.GetBuildTimingsFromOutput(outputStr5, only5, out Dictionary<int, Dictionary<string, int>> all5, out Dictionary<string, int> sums5);
            Assert.IsTrue(res5);
            Assert.IsNull(all5);
            Assert.IsNull(sums5);
        }

        [TestMethod()]
        public void GetBuildTimingsFromOutputTest_OneProject()
        {
            string outputStr1 = ""
                + "1>Task Performance Summary:\r\n"
                + "1>        0 ms  AssignCulture                              1 calls\r\n"
                + "1>      628 ms  Link                                       1 calls\r\n"
                + "1>     3956 ms  CL                                         2 calls\r\n";
            HashSet<int> only1 = null;
            bool res1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(res1);
            Assert.IsTrue(CM.CollectionAreEquals(all1, new Dictionary<int, Dictionary<string, int>> { { 1, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 3956 } } } }));
            CollectionAssert.AreEqual(sums1, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 3956 } });
        }

        [TestMethod()]
        public void GetBuildTimingsFromOutputTest_TwoProjects()
        {
            string outputStr1 = ""
                + "1>Task Performance Summary:\r\n"
                + "1>        0 ms  AssignCulture                              1 calls\r\n"
                + "1>      628 ms  Link                                       1 calls\r\n"
                + "1>     3956 ms  CL                                         2 calls\r\n"
                + "\r\n"
                + "2>Task Performance Summary:\r\n"
                + "2>        0 ms  AssignCulture                              1 calls\r\n"
                + "2>       17 ms  CppClean                                   1 calls\r\n"
                + "2>     3956 ms  CL                                         2 calls\r\n";
            HashSet<int> only1 = null;
            bool res1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(res1);
            Assert.IsTrue(CM.CollectionAreEquals(all1, new Dictionary<int, Dictionary<string, int>> {
                { 1, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 3956 } } },
                { 2, new Dictionary<string, int> { { "AssignCulture", 0 }, { "CppClean", 17 }, { "CL", 3956 } } }
            }));
            CollectionAssert.AreEqual(sums1, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 2 * 3956 }, { "CppClean", 17 } });


            string outputStr2 = ""
                + "1>Task Performance Summary:\r\n"
                + "1>        0 ms  AssignCulture                              1 calls\r\n"
                + "1>      628 ms  Link                                       1 calls\r\n"
                + "1>     3956 ms  CL                                         2 calls\r\n"
                + "\r\n"
                + "2>Task Performance Summary:\r\n"
                + "2>        0 ms  AssignCulture                              1 calls\r\n"
                + "2>       17 ms  CppClean                                   1 calls\r\n"
                + "2>     3956 ms  CL                                         2 calls\r\n";
            HashSet<int> only2 = new HashSet<int> { 1 };
            bool res2 = SaveCsv.GetBuildTimingsFromOutput(outputStr2, only2, out Dictionary<int, Dictionary<string, int>> all2, out Dictionary<string, int> sums2);
            Assert.IsTrue(res2);
            Assert.IsTrue(CM.CollectionAreEquals(all2, new Dictionary<int, Dictionary<string, int>> {
                { 1, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 3956 } } }
            }));
            CollectionAssert.AreEqual(sums2, new Dictionary<string, int> { { "AssignCulture", 0 }, { "Link", 628 }, { "CL", 3956 } });


            string outputStr3 = ""
                + "1>Task Performance Summary:\r\n"
                + "1>        0 ms  AssignCulture                              1 calls\r\n"
                + "1>      628 ms  Link                                       1 calls\r\n"
                + "1>     3956 ms  CL                                         2 calls\r\n"
                + "\r\n"
                + "2>Task Performance Summary:\r\n"
                + "2>        0 ms  AssignCulture                              1 calls\r\n"
                + "2>       17 ms  CppClean                                   1 calls\r\n"
                + "2>     3956 ms  CL                                         2 calls\r\n";
            HashSet<int> only3 = new HashSet<int> { 2 };
            bool res3 = SaveCsv.GetBuildTimingsFromOutput(outputStr3, only3, out Dictionary<int, Dictionary<string, int>> all3, out Dictionary<string, int> sums3);
            Assert.IsTrue(res3);
            Assert.IsTrue(CM.CollectionAreEquals(all3, new Dictionary<int, Dictionary<string, int>> {
                { 2, new Dictionary<string, int> { { "AssignCulture", 0 }, { "CppClean", 17 }, { "CL", 3956 } } }
            }));
            CollectionAssert.AreEqual(sums3, new Dictionary<string, int> { { "AssignCulture", 0 }, { "CppClean", 17 }, { "CL", 3956 } });
        }

        [TestMethod()]
        public void GetBuildTimingsFromOutputTest_RealOutput()
        {
            string outputStr1 = File.ReadAllText(TestUtils.GetTestFile("TestOutputWithBuildTiming.txt"));
            HashSet<int> only1 = null;
            bool res1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(res1);

            Dictionary<int, Dictionary<string, int>> expectedAll1 = new Dictionary<int, Dictionary<string, int>>
            {
                { 1, new Dictionary<string, int> {
                    {"MSBuild", 0},
                    {"FindUnderPath", 1},
                    {"RemoveDuplicates", 1},
                    {"AssignProjectConfiguration", 1},
                    {"MakeDir", 1},
                    {"AssignCulture", 1},
                    {"ReadLinesFromFile", 2},
                    {"AssignTargetPath", 2},
                    {"ResolvePackageFileConflicts", 2},
                    {"Message", 2},
                    {"Delete", 2},
                    {"Touch", 4},
                    {"WriteLinesToFile", 4},
                    {"SetEnv", 4},
                    {"GetOutOfDateItems", 8},
                    {"CppClean", 25},
                    {"Link", 700},
                    {"CL", 3098}
                } },
                { 5, new Dictionary<string, int> {
                    {"AssignTargetPath", 0},
                    {"MSBuild", 0},
                    {"AssignCulture", 0},
                    {"Message", 3},
                    {"CppClean", 3},
                    {"ResolvePackageFileConflicts", 3},
                    {"AssignProjectConfiguration", 5},
                    {"GetOutOfDateItems", 7},
                    {"Touch", 7},
                    {"RemoveDuplicates", 9},
                    {"SetEnv", 15},
                    {"WriteLinesToFile", 15},
                    {"ReadLinesFromFile", 17},
                    {"Delete", 23},
                    {"MakeDir", 24},
                    {"FindUnderPath", 30},
                    {"Link", 725},
                    {"CL", 2240}
                } },
                { 3, new Dictionary<string, int> {
                    {"MSBuild", 0},
                    {"RemoveDuplicates", 0},
                    {"CppClean", 0},
                    {"AssignCulture", 1},
                    {"AssignProjectConfiguration", 1},
                    {"AssignTargetPath", 2},
                    {"FindUnderPath", 2},
                    {"MakeDir", 4},
                    {"Delete", 8},
                    {"GetOutOfDateItems", 8},
                    {"ReadLinesFromFile", 10},
                    {"Message", 10},
                    {"Touch", 11},
                    {"SetEnv", 16},
                    {"ResolvePackageFileConflicts", 20},
                    {"WriteLinesToFile", 25},
                    {"Exec", 132},
                    {"Link", 627},
                    {"CL", 4255}
                } },
                { 6, new Dictionary<string, int> {
                    {"Message", 0},
                    {"SetEnv", 0},
                    {"GetOutOfDateItems", 0},
                    {"FindUnderPath", 0},
                    {"RemoveDuplicates", 0},
                    {"AssignCulture", 0},
                    {"Delete", 1},
                    {"MSBuild", 1},
                    {"ResolvePackageFileConflicts", 1},
                    {"AssignProjectConfiguration", 1},
                    {"Touch", 1},
                    {"AssignTargetPath", 1},
                    {"ReadLinesFromFile", 1},
                    {"WriteLinesToFile", 2},
                    {"MakeDir", 4},
                    {"CppClean", 20},
                    {"Link", 580},
                    {"CL", 1500}
                } }
            };

            Dictionary<string, int> expectedSums1 = new Dictionary<string, int> {
                {"MSBuild", 1},
                {"FindUnderPath", 33},
                {"RemoveDuplicates", 10},
                {"AssignProjectConfiguration", 8},
                {"MakeDir", 33},
                {"AssignCulture", 2},
                {"ReadLinesFromFile", 30},
                {"AssignTargetPath", 5},
                {"ResolvePackageFileConflicts", 26},
                {"Message", 15},
                {"Delete", 34},
                {"Touch", 23},
                {"WriteLinesToFile", 46},
                {"SetEnv", 35},
                {"GetOutOfDateItems", 23},
                {"CppClean", 48},
                {"Link", 2632},
                {"CL", 11093},
                {"Exec", 132}
            };

            Assert.IsTrue(CM.CollectionAreEquals(all1, expectedAll1));
            CollectionAssert.AreEqual(sums1, expectedSums1);
        }

        [TestMethod()]
        public void GetColumIndexesAndNamesFromSumsTest()
        {
            List<string> columnNames = null;

            // null and empty checks
            CollectionAssert.AreEqual(null, SaveCsv.GetColumIndexesAndNamesFromSums(null, out columnNames));
            CollectionAssert.AreNotEqual(null, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 0 } }, out columnNames));
            CollectionAssert.AreNotEqual(null, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int>(), out columnNames));
            CollectionAssert.AreEqual(new SortedDictionary<string, int>(), SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int>(), out columnNames));

            // 1 value checks
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 1 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 0 } }, out columnNames));

            // value order checks
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "B", 1 }, { "C", 2 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 333 }, { "B", 222 }, { "C", 111 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "C", 0 }, { "B", 1 }, { "A", 2 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 111 }, { "B", 222 }, { "C", 333 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "B", 0 }, { "C", 1 }, { "A", 2 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 111 }, { "B", 333 }, { "C", 222 } }, out columnNames));

            // value duplicated order checks, second sort is alphabeticaly
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "B", 1 }, { "C", 2 }, { "D", 3 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 333 }, { "B", 222 }, { "C", 222 }, { "D", 111 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "B", 1 }, { "C", 2 }, { "D", 3 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 333 }, { "C", 222 }, { "B", 222 }, { "D", 111 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "D", 1 }, { "B", 2 }, { "C", 3 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 333 }, { "B", 0 }, { "C", 0 }, { "D", 111 } }, out columnNames));
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "A", 0 }, { "D", 1 }, { "B", 2 }, { "C", 3 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 333 }, { "C", 0 }, { "B", 0 }, { "D", 111 } }, out columnNames));

            // value duplicated order checks, second sort is alphabeticaly
            CollectionAssert.AreEqual(new Dictionary<string, int> { { "B", 0 }, { "C", 1 }, { "D", 2 }, { "A", 3 } }, SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 50 }, { "C", 222 }, { "B", 222 }, { "D", 111 } }, out columnNames));

            // columnNames check
            Dictionary<string, int> columnIndexes = SaveCsv.GetColumIndexesAndNamesFromSums(new Dictionary<string, int> { { "A", 222 }, { "C", 333 }, { "B", 333 }, { "D", 111 } }, out columnNames);
            Assert.IsTrue(columnIndexes.Count == columnNames.Count);
            List<string> columnNamesExpected = new List<string> { "B", "C", "A", "D" };
            CollectionAssert.AreEqual(columnNames, columnNamesExpected);
        }

        [TestMethod()]
        public void SortBuildTimingsTest_NullEmpty()
        {
            { // <c>all</c> null checks
                bool res1 = SaveCsv.SortBuildTimings(null, null, out Dictionary<int, List<int?>> sorted1);
                Assert.IsTrue(res1);
                Assert.IsNull(sorted1);

                bool res2 = SaveCsv.SortBuildTimings(null, new Dictionary<string, int>(), out Dictionary<int, List<int?>> sorted2);
                Assert.IsTrue(res2);
                Assert.IsNull(sorted2);

                bool res3 = SaveCsv.SortBuildTimings(null, new Dictionary<string, int>() { { "A", 0 }, { "B", 1 } }, out Dictionary<int, List<int?>> sorted3);
                Assert.IsTrue(res3);
                Assert.IsNull(sorted3);
            }

            { // <c>sums</c> null checks
                bool res1 = SaveCsv.SortBuildTimings(null, null, out Dictionary<int, List<int?>> sorted1); // already checked, but easier to read and copy for next test cases
                Assert.IsTrue(res1);
                Assert.IsNull(sorted1);

                bool res2 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>(), null, out Dictionary<int, List<int?>> sorted2);
                Assert.IsTrue(res2);
                Assert.IsTrue(CM.CollectionAreEquals(sorted2, new Dictionary<int, List<int?>>()));

                bool res3 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>() { { 2, new Dictionary<string, int>() { { "A", 0 } } } }, null, out Dictionary<int, List<int?>> sorted3, false /*throwAssert*/);
                Assert.IsFalse(res3);
                Assert.IsNull(sorted3);
            }

            { // <c>all</c> empty checks
                bool res1 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>(), null, out Dictionary<int, List<int?>> sorted1);
                Assert.IsTrue(res1);
                Assert.IsTrue(CM.CollectionAreEquals(sorted1, new Dictionary<int, List<int?>>()));

                bool res2 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>(), new Dictionary<string, int>(), out Dictionary<int, List<int?>> sorted2);
                Assert.IsTrue(res2);
                Assert.IsTrue(CM.CollectionAreEquals(sorted2, new Dictionary<int, List<int?>>()));

                bool res3 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>(), new Dictionary<string, int>() { { "A", 0 }, { "B", 1 } }, out Dictionary<int, List<int?>> sorted3);
                Assert.IsTrue(res3);
                Assert.IsTrue(CM.CollectionAreEquals(sorted3, new Dictionary<int, List<int?>>()));
            }

            { // <c>sums</c> empty checks
                bool res1 = SaveCsv.SortBuildTimings(null, new Dictionary<string, int>(), out Dictionary<int, List<int?>> sorted1);
                Assert.IsTrue(res1);
                Assert.IsNull(sorted1);

                bool res2 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>(), new Dictionary<string, int>(), out Dictionary<int, List<int?>> sorted2);
                Assert.IsTrue(res2);
                Assert.IsTrue(CM.CollectionAreEquals(sorted2, new Dictionary<int, List<int?>>()));

                bool res3 = SaveCsv.SortBuildTimings(new Dictionary<int, Dictionary<string, int>>() { { 2, new Dictionary<string, int>() { { "A", 0 } } } }, new Dictionary<string, int>(), out Dictionary<int, List<int?>> sorted3, false /*throwAssert*/);
                Assert.IsFalse(res3);
                Assert.IsTrue(CM.CollectionAreEquals(sorted3, new Dictionary<int, List<int?>>() { { 2, new List<int?>() } }));
            }
        }

        [TestMethod()]
        public void SortBuildTimingsTest_Values()
        {
            Dictionary<int, Dictionary<string, int>> all = new Dictionary<int, Dictionary<string, int>>();
            //Dictionary<string, int> sums = new Dictionary<string, int>() { { "A", 222 }, { "B", 333 }, { "C", 111 }, { "D", 333 } };
            Dictionary<string, int> columnIndex = new Dictionary<string, int>() { { "A", 2 }, { "B", 0 }, { "C", 3 }, { "D", 1 } };
            Dictionary<int, List<int?>> sorted = new Dictionary<int, List<int?>>();

            // missing all values
            bool res1 = SaveCsv.SortBuildTimings(all, columnIndex, out Dictionary<int, List<int?>> sorted1);
            Assert.IsTrue(res1);
            Assert.IsTrue(CM.CollectionAreEquals(sorted1, sorted));

            // missing 3 values
            all.Add(2, new Dictionary<string, int>() { { "A", 2 } });
            sorted.Add(2, new List<int?>() { null, null, 2, null });
            bool res2 = SaveCsv.SortBuildTimings(all, columnIndex, out Dictionary<int, List<int?>> sorted2);
            Assert.IsTrue(res2);
            Assert.IsTrue(CM.CollectionAreEquals(sorted2, sorted));

            // missing 2 values
            all.Add(4, new Dictionary<string, int>() { { "C", 1 }, { "D", 3 } });
            sorted.Add(4, new List<int?>() { null, 3, null, 1 });
            bool res3 = SaveCsv.SortBuildTimings(all, columnIndex, out Dictionary<int, List<int?>> sorted3);
            Assert.IsTrue(res3);
            Assert.IsTrue(CM.CollectionAreEquals(sorted3, sorted));

            // all values
            all.Add(5, new Dictionary<string, int>() { { "C", 1 }, { "D", 6 }, { "A", 2 }, { "B", 3 } });
            sorted.Add(5, new List<int?>() { 3, 6, 2, 1 });
            bool res4 = SaveCsv.SortBuildTimings(all, columnIndex, out Dictionary<int, List<int?>> sorted4);
            Assert.IsTrue(res4);
            Assert.IsTrue(CM.CollectionAreEquals(sorted4, sorted));
        }

        [TestMethod()]
        public void WholeWorkflow_RealOutput()
        {
            //
            // Step 1: Parse string from Output pane
            //
            string outputStr1 = File.ReadAllText(TestUtils.GetTestFile("TestOutputWithBuildTiming.txt"));
            HashSet<int> only1 = null;
            bool resG1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(resG1);
            Assert.IsTrue(all1.Count == 4);   //small intermediate check
            Assert.IsTrue(sums1.Count == 19); //small intermediate check

            //
            // Step 2: Setup colum indexes
            //
            Dictionary<string, int> columnIndex1 = SaveCsv.GetColumIndexesAndNamesFromSums(sums1, out List<string> columnNames1);
            Assert.IsTrue(columnIndex1.Count == 19); //small intermediate check
            List<string> columnNamesExpected1 = new List<string> { "CL", "Link", "Exec", "CppClean", "WriteLinesToFile", "SetEnv", "Delete", "FindUnderPath", "MakeDir", "ReadLinesFromFile", "ResolvePackageFileConflicts", "GetOutOfDateItems", "Touch", "Message", "RemoveDuplicates", "AssignProjectConfiguration", "AssignTargetPath", "AssignCulture", "MSBuild" };
            CollectionAssert.AreEqual(columnNames1, columnNamesExpected1);

            //
            // Step 3: Sort results and fill gaps
            //
            bool resS1 = SaveCsv.SortBuildTimings(all1, columnIndex1, out Dictionary<int, List<int?>> sorted1);
            Assert.IsTrue(resS1);
            Dictionary<int, List<int?>> sortedExpected = new Dictionary<int, List<int?>>
            {
                { 1, new List<int?>{ 3098, 700, null, 25, 4, 4, 2, 1, 1, 2, 2, 8, 4, 2, 1, 1, 2, 1, 0 } },
                { 5, new List<int?>{ 2240, 725, null, 3, 15, 15, 23, 30, 24, 17, 3, 7, 7, 3, 9, 5, 0, 0, 0 } },
                { 3, new List<int?>{ 4255, 627, 132, 0, 25, 16, 8, 2, 4, 10, 20, 8, 11, 10, 0, 1, 2, 1, 0 } },
                { 6, new List<int?>{ 1500, 580, null, 20, 2, 0, 1, 0, 4, 1, 1, 0, 1, 0, 0, 1, 1, 0, 1 } }
            };
            Assert.IsTrue(CM.CollectionAreEquals(sorted1, sortedExpected));
        }

        [TestMethod()]
        public void WholeWorkflowOnly_RealOutput()
        {
            //
            // Step 1: Parse string from Output pane
            //
            string outputStr1 = File.ReadAllText(TestUtils.GetTestFile("TestOutputWithBuildTiming.txt"));
            HashSet<int> only1 = new HashSet<int> { 5, 3 };
            bool resG1 = SaveCsv.GetBuildTimingsFromOutput(outputStr1, only1, out Dictionary<int, Dictionary<string, int>> all1, out Dictionary<string, int> sums1);
            Assert.IsTrue(resG1);
            Assert.IsTrue(all1.Count == 2);   //small intermediate check
            Assert.IsTrue(sums1.Count == 19); //small intermediate check

            //
            // Step 2: Setup colum indexes
            //
            Dictionary<string, int> columnIndex1 = SaveCsv.GetColumIndexesAndNamesFromSums(sums1, out List<string> columnNames1);
            Assert.IsTrue(columnIndex1.Count == 19); //small intermediate check
            //List<string> columnNamesExpected1 = new List<string> { "CL", "Link", "Exec", "CppClean", "WriteLinesToFile", "SetEnv", "Delete", "FindUnderPath", "MakeDir", "ReadLinesFromFile", "ResolvePackageFileConflicts", "GetOutOfDateItems", "Touch", "Message", "RemoveDuplicates", "AssignProjectConfiguration", "AssignTargetPath", "AssignCulture", "MSBuild" };
            // column order is different because sums are different
            List<string> columnNamesExpected1 = new List<string> { "CL", "Link", "Exec", "WriteLinesToFile", "FindUnderPath", "Delete", "SetEnv", "MakeDir", "ReadLinesFromFile", "ResolvePackageFileConflicts", "Touch", "GetOutOfDateItems", "Message", "RemoveDuplicates", "AssignProjectConfiguration", "CppClean", "AssignTargetPath", "AssignCulture", "MSBuild" };
            CollectionAssert.AreEqual(columnNames1, columnNamesExpected1);

            //
            // Step 3: Sort results and fill gaps
            //
            bool resS1 = SaveCsv.SortBuildTimings(all1, columnIndex1, out Dictionary<int, List<int?>> sorted1);
            Assert.IsTrue(resS1);
            Dictionary<int, List<int?>> sortedExpected = new Dictionary<int, List<int?>>
            {
                //{ 5, new List<int?>{ 2240, 725, null, 3, 15, 15, 23, 30, 24, 17, 3, 7, 7, 3, 9, 5, 0, 0, 0 } },
                // values are the same, but sorting is different because sums are different due to some project elimination. It should be like that
                { 5, new List<int?>{ 2240, 725, null, 15, 30, 23, 15, 24, 17, 3, 7, 7, 3, 9, 5, 3, 0, 0, 0 } },

                //{ 3, new List<int?>{ 4255, 627, 132, 0, 25, 16, 8, 2, 4, 10, 20, 8, 11, 10, 0, 1, 2, 1, 0 } }
                // values are the same, but sorting is different because sums are different due to some project elimination. It should be like that
                { 3, new List<int?>{ 4255, 627, 132, 25, 2, 8, 16, 4, 10, 20, 11, 8, 10, 0, 1, 0, 2, 1, 0 } }
            };
            Assert.IsTrue(CM.CollectionAreEquals(sorted1, sortedExpected));
        }

        [TestMethod()]
        public void EscapeRowTest()
        {
            Assert.AreEqual("", SaveCsv.EscapeRow(null));
            Assert.AreEqual("", SaveCsv.EscapeRow(new List<object>()));
            Assert.AreEqual("\"A1 A2\"", SaveCsv.EscapeRow(new List<object> { "A1 A2" }));
            Assert.AreEqual("\"A1 A2\", \"B\"", SaveCsv.EscapeRow(new List<object> { "A1 A2", "B" }));

            // null should be represented as "no cell" (two comas ",,") rather than "empty cell" (empty string in double qoutes "\"\"").
            // When there is "no cell" value from previous cell is not truncated, so it is fully visible
            Assert.AreEqual("\"A\", ", SaveCsv.EscapeRow(new List<object> { "A", null }));
            Assert.AreEqual("\"A\", , \"B\"", SaveCsv.EscapeRow(new List<object> { "A", null, "B" }));
        }

        [TestMethod()]
        public void SaveCriticalPathAsCsvWithBuildTimingTest()
        {
            string outputStr1 = File.ReadAllText(TestUtils.GetTestFile("TestOutputWithBuildTiming.txt"));
            string tmpFileName = Path.GetTempFileName(); //this method Create file in temp directory

            List<BuildInfo> criticalPath = new List<BuildInfo>
            {
                new BuildInfo("junk-finddir\\junk-finddir.vcxproj", "junk-finddir.vcxproj", 1, 59992, 40783233, true),
                new BuildInfo("WpfToolTip\\WpfToolTip.csproj", "WpfToolTip.csproj", 2, 40808204, 102393597, true),
                new BuildInfo("variadic-macros-v1\\variadic-macros-v1.vcxproj", "variadic-macros-v1.vcxproj", 6, 102473587, 149216287, true),
            };
            var dataModel = new PrivateObject(DataModel.Instance); // Use PrivateObject class to change private member of DataModel object.
            dataModel.SetField("criticalPath", criticalPath);

            bool res1 = SaveCsv.SaveCriticalPathAsCsv(tmpFileName, outputStr1);
            Assert.IsTrue(res1);

            string current = File.ReadAllText(TestUtils.GetTestFile(tmpFileName));
            string expected = File.ReadAllText(TestUtils.GetTestFile("PBM Example.sln CP WithBuildTiming.csv"));
            Assert.IsTrue(TestUtils.CompareCsvWithMachineInfo(current, expected), "CSV content mismatch");

            File.Delete(tmpFileName);
        }

        [TestMethod()]
        public void SaveCriticalPathAsCsvWithoutBuildTimingTest()
        {
            string outputStr1 = File.ReadAllText(TestUtils.GetTestFile("TestOutputWithoutBuildTiming.txt"));
            string tmpFileName = Path.GetTempFileName(); //this method Create file in temp directory

            List<BuildInfo> criticalPath = new List<BuildInfo>
            {
                new BuildInfo("junk-finddir\\junk-finddir.vcxproj", "junk-finddir.vcxproj", 1, 59992, 40783233, true),
                new BuildInfo("WpfToolTip\\WpfToolTip.csproj", "WpfToolTip.csproj", 2, 40808204, 102393597, true),
                new BuildInfo("variadic-macros-v1\\variadic-macros-v1.vcxproj", "variadic-macros-v1.vcxproj", 6, 102473587, 149216287, true),
            };
            var dataModel = new PrivateObject(DataModel.Instance); // Use PrivateObject class to change private member of DataModel object.
            dataModel.SetField("criticalPath", criticalPath);

            bool res1 = SaveCsv.SaveCriticalPathAsCsv(tmpFileName, outputStr1);
            Assert.IsTrue(res1);

            string current = File.ReadAllText(TestUtils.GetTestFile(tmpFileName));
            string expected = File.ReadAllText(TestUtils.GetTestFile("PBM Example.sln CP WithoutBuildTiming.csv"));
            Assert.IsTrue(TestUtils.CompareCsvWithMachineInfo(current, expected), "CSV content mismatch");

            File.Delete(tmpFileName);
        }
    }
}
