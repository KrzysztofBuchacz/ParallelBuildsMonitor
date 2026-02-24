using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Drawing;
using System.Security.Cryptography;
using ParallelBuildsMonitorTests;

namespace ParallelBuildsMonitor.Tests
{
    public static class ImageComparer
    {
        /// <summary>
        /// Use this method to check if images are equal.
        /// </summary>
        /// <remarks>
        /// Do NOT use to compare if images are diferent!
        /// 
        /// Is below page contain code that visualize .png difference?
        /// https://blogs.msdn.microsoft.com/gautamg/2010/04/08/how-to-do-image-comparison-in-coded-ui-test/
        /// </remarks>
        /// <param name="pathToExpectedPng"></param>
        /// <param name="pathToActualPng"></param>
        /// <returns>true when images are equal, false when images are different or on error</returns>
        public static bool AreImagesEqual(string pathToExpectedPng, string pathToActualPng)
        {
            if (String.IsNullOrEmpty(pathToExpectedPng) || String.IsNullOrEmpty(pathToActualPng))
                return false;

            if (pathToExpectedPng == pathToActualPng) // Buggy implementation. If one path is relative, second is absolute it gives wrong result
                return false;  //Yes, In tests, comparing the same file with itself should fail!

            Bitmap bitmapExpected = new Bitmap(pathToExpectedPng, true);
            Bitmap bitmapActual = new Bitmap(pathToActualPng, true);

            if ((bitmapExpected == null) || (bitmapActual == null))
                return false;

            return AreEqual(bitmapExpected, bitmapActual);
        }

        // Code base on code from page: https://www.c-sharpcorner.com/UploadFile/jawedmd/an-extension-of-asser-areequal-for-images-in-mstest/
        public static bool AreEqual(Bitmap expected, Bitmap actual)
        {
            if (expected.Size != actual.Size)
                return false;

            //Convert each image to a byte array
            ImageConverter ic = new ImageConverter();
            byte[] btImageExpected = new byte[1];
            btImageExpected = (byte[])ic.ConvertTo(expected, btImageExpected.GetType());
            byte[] btImageActual = new byte[1];
            btImageActual = (byte[])ic.ConvertTo(actual, btImageActual.GetType());

            //Compute a hash for each image
            var shaM = new SHA256Managed();
            byte[] hash1 = shaM.ComputeHash(btImageExpected);
            byte[] hash2 = shaM.ComputeHash(btImageActual);

            //Compare the hash values
            for (int i = 0; i < hash1.Length && i < hash2.Length; i++)
            {
                if (hash1[i] != hash2[i])
                    return false;
            }

            return true;
        }
    }

    [TestClass()]
    public class PngTestHelperMethodsTests
    {
        [TestMethod()]
        public void AreImagesEqual()
        {
            // null checks
            Assert.IsFalse(ImageComparer.AreImagesEqual(null, null));
            Assert.IsFalse(ImageComparer.AreImagesEqual(null, ""));
            Assert.IsFalse(ImageComparer.AreImagesEqual("", null));
            Assert.IsFalse(ImageComparer.AreImagesEqual("", ""));

            // miscleanius checks
            Assert.IsFalse(ImageComparer.AreImagesEqual("a.png", "a.png")); //Yes, In tests, comparing the same file with the same file should fail

            // real images checks
            Assert.IsTrue(ImageComparer.AreImagesEqual(TestUtils.GetTestFile("UnitTestImage.png"), TestUtils.GetTestFile("UnitTestImageCopy.png")));
            Assert.IsFalse(ImageComparer.AreImagesEqual(TestUtils.GetTestFile("UnitTestImage.png"), TestUtils.GetTestFile("UnitTestImageChanged.png")));
        }

    }

    [TestClass()]
    public class SavePngTests
    {
        [TestMethod()]
        public void SavePng_BeforeBuildStart()
        {
            PBMControl pBMcontrol = new PBMControl();

            Window window = new Window
            {
                Width = 800,
                Height = 600,
                Content = pBMcontrol
            };

            window.Show(); //this will draw PBMControl
            //window.ShowDialog(); //for debug

            string expected = TestUtils.GetTestFile("BeforeBuildStart.png");
            // ResultsDirectory   <= this is where results should be saved. How to get value of this xaml tag?
            string tmpFileName = Path.GetTempFileName(); //this method return path to non-existing file in temp directory
            tmpFileName += ".png";

            bool res = pBMcontrol.SaveGraph(tmpFileName /*pathToPngFile*/);
            Assert.IsTrue(res);
            Assert.IsTrue(ImageComparer.AreImagesEqual(expected, tmpFileName));
        }

        [TestMethod()]
        public void SavePng_BuildInProgress()
        {
            { // MachineInfo is in the picture, so make MachineInfo as machine indepenent string for unit test puroposes
                string machineIndepenentInfo = "Processors: 1 | Cores: 2 | CPU Speed: 2.7GHz | Hyper Threading: Enabled | RAM: 8GB | HDD: 1 SSD";
                MachineInfo mi = MachineInfo.Instance;
                var machineInfo = new PrivateObject(mi); // Use PrivateObject class to change private member of MachineInfo object.
                machineInfo.SetField("info", machineIndepenentInfo);

                Assert.AreEqual(MachineInfo.Instance.ToString(), machineIndepenentInfo); // Verify if internal data were updated
            }

            { // Feed DataModel with sample data
                DataModel dm = DataModel.Instance;
                var dataModel = new PrivateObject(dm); // Use PrivateObject class to change private member of MachineInfo object.
                dataModel.SetProperty("SolutionName", "Example.sln");
                dataModel.SetProperty("StartTime", new System.DateTime(636848298660196710L));
                dataModel.SetProperty("MaxParallelBuilds", 4);

                //<c>string</c> is ProjectUniqueName, <c>uint</c> is project build order number, <c>long</c> is project Start time, relative, counted since <c>DataModel.StartTime</c> in <c>DateTime.Ticks</c> units.
                Dictionary<string, Tuple<uint, long>> currentBuilds = new Dictionary<string, Tuple<uint, long>>
                {
                    { "WpfToolTip\\WpfToolTip.csproj", new Tuple<uint, long>(2, 77409902) },
                    { "constexpr_templates\\constexpr_templates.vcxproj", new Tuple<uint, long>(3, 79745101) },
                    { "DiskPerformance\\DiskPerformance.csproj", new Tuple<uint, long>(4, 81644812) },
                    { "junk-vector-const\\junk-vector-const.vcxproj", new Tuple<uint, long>(5, 83574518) },
                };
                dataModel.SetField("currentBuilds", currentBuilds);

                List<BuildInfo> finishedBuilds = new List<BuildInfo>()
                {
                    new BuildInfo("junk-finddir\\junk-finddir.vcxproj", "junk-finddir.vcxproj", 1, 359937, 77299946, true)
                };
                dataModel.SetField("finishedBuilds", finishedBuilds);

                Dictionary<string, List<string>> projectDependenies = new Dictionary<string, List<string>>(); //<c>string</c> is ProjectUniqueName, <c>List<string></c> is list of projects that <c>Key</c> project depends on
                dataModel.SetField("projectDependenies", projectDependenies);

                List<BuildInfo> criticalPath = new List<BuildInfo>();
                dataModel.SetField("criticalPath", criticalPath);

                List<Tuple<long, float>> cpuUsage = new List<Tuple<long, float>>()
                {
                    new Tuple<long, float>(636848298660196710, 0.0f),
                    new Tuple<long, float>(636848298670348211, 77.42183f),
                    new Tuple<long, float>(636848298681079693, 58.4654f),
                    new Tuple<long, float>(636848298691089400, 78.53645f),
                    new Tuple<long, float>(636848298701193247, 43.16831f),
                    new Tuple<long, float>(636848298711194104, 40.23949f),
                    new Tuple<long, float>(636848298721322554, 33.27891f),
                    new Tuple<long, float>(636848298731336449, 37.58672f),
                    new Tuple<long, float>(636848298741491580, 43.4553f),
                    new Tuple<long, float>(636848298752339901, 94.59882f)
                };
                dataModel.SetField("cpuUsage", cpuUsage);

                List<Tuple<long, float>> hddUsage = new List<Tuple<long, float>>()
                {
                    new Tuple<long, float>(636848298660196710, 0.0f),
                    new Tuple<long, float>(636848298670348211, 3.894294f),
                    new Tuple<long, float>(636848298681079693, 10.60813f),
                    new Tuple<long, float>(636848298691089400, 37.33977f),
                    new Tuple<long, float>(636848298701193247, 3.864434f),
                    new Tuple<long, float>(636848298711194104, 4.997182f),
                    new Tuple<long, float>(636848298721322554, 3.919567f),
                    new Tuple<long, float>(636848298731336449, 9.422446f),
                    new Tuple<long, float>(636848298741491580, 5.652133f),
                    new Tuple<long, float>(636848298752339901, 0.8484202f)
                };
                dataModel.SetField("hddUsage", hddUsage);

                { // private static need to be handled through PrivateType
                    PrivateType pt = new PrivateType(typeof(DataModel));
                    pt.SetStaticFieldOrProperty("projectBuildOrderNumber", 5u);
                }

                Assert.AreEqual(DataModel.Instance.SolutionName, "Example.sln"); // Verify if internal data were updated
            }

            PBMControl pBMcontrol = new PBMControl();

            var graphControl = new PrivateObject(GraphControl.Instance); // Use PrivateObject class to change private member of GraphControl object.
            graphControl.SetField("nowTickForTest", 636848298760000000L);

            Window window = new Window
            {
                Width = 800,
                Height = 600,
                Content = pBMcontrol
            };

            window.Show(); //this will draw PBMControl
            //window.ShowDialog(); //for debug

            string expected = TestUtils.GetTestFile("BuildInProgress.png");
            // ResultsDirectory   <= this is where results should be saved. How to get value of this xaml tag?
            string tmpFileName = Path.GetTempFileName(); //this method return path to non-existing file in temp directory
            tmpFileName += ".png";

            bool res = pBMcontrol.SaveGraph(tmpFileName /*pathToPngFile*/);
            Assert.IsTrue(res);
            Assert.IsTrue(ImageComparer.AreImagesEqual(expected, tmpFileName));
        }

        [TestMethod()]
        public void SavePng_BuildFinished()
        {
            { // MachineInfo is in the picture, so make MachineInfo as machine indepenent string for unit test puroposes
                string machineIndepenentInfo = "Processors: 1 | Cores: 2 | CPU Speed: 2.7GHz | Hyper Threading: Enabled | RAM: 8GB | HDD: 1 SSD";
                MachineInfo mi = MachineInfo.Instance;
                var machineInfo = new PrivateObject(mi); // Use PrivateObject class to change private member of MachineInfo object.
                machineInfo.SetField("info", machineIndepenentInfo);
                machineInfo.SetField("separatorCached", " | ");  // Set separator to avoid generating machine dependent info and use just set ones

                Assert.AreEqual(MachineInfo.Instance.ToString(), machineIndepenentInfo); // Verify if internal data were updated
            }

            { // Feed DataModel with sample data
                DataModel dm = DataModel.Instance;
                var dataModel = new PrivateObject(dm); // Use PrivateObject class to change private member of MachineInfo object.
                dataModel.SetProperty("SolutionName", "Example.sln");
                dataModel.SetProperty("StartTime", new System.DateTime(636849135031953262L));
                dataModel.SetProperty("MaxParallelBuilds", 4);

                //<c>string</c> is ProjectUniqueName, <c>uint</c> is project build order number, <c>long</c> is project Start time, relative, counted since <c>DataModel.StartTime</c> in <c>DateTime.Ticks</c> units.
                Dictionary<string, Tuple<uint, long>> currentBuilds = new Dictionary<string, Tuple<uint, long>> { };
                dataModel.SetField("currentBuilds", currentBuilds);

                List<BuildInfo> finishedBuilds = new List<BuildInfo>()
                {
                    new BuildInfo("junk-finddir\\junk-finddir.vcxproj", "junk-finddir.vcxproj", 1, 399931, 54684041, true),
                    new BuildInfo("DiskPerformance\\DiskPerformance.csproj", "DiskPerformance.csproj", 4, 59892361, 97806434, false),
                    new BuildInfo("junk-vector-const\\junk-vector-const.vcxproj", "junk-vector-const.vcxproj", 5, 61022178, 121272755, true),
                    new BuildInfo("WpfToolTip\\WpfToolTip.csproj", "WpfToolTip.csproj", 2, 56140367, 144319141, true),
                    new BuildInfo("constexpr_templates\\constexpr_templates.vcxproj", "constexpr_templates.vcxproj", 3, 58330492, 151578262, true),
                    new BuildInfo("variadic-macros-v1\\variadic-macros-v1.vcxproj", "variadic-macros-v1.vcxproj", 6, 144449121, 172642436, true)
                };
                dataModel.SetField("finishedBuilds", finishedBuilds);

                Dictionary<string, List<string>> projectDependenies = new Dictionary<string, List<string>> //<c>string</c> is ProjectUniqueName, <c>List<string></c> is list of projects that <c>Key</c> project depends on
                {
                    {"constexpr_templates\\constexpr_templates.vcxproj", new List<string>{"junk-finddir\\junk-finddir.vcxproj"}},
                    {"DiskPerformance\\DiskPerformance.csproj", new List<string>{"junk-finddir\\junk-finddir.vcxproj"}},
                    {"junk-finddir\\junk-finddir.vcxproj", new List<string>{}},
                    {"junk-vector-const\\junk-vector-const.vcxproj", new List<string>{"junk-finddir\\junk-finddir.vcxproj"}},
                    {"variadic-macros-v1\\variadic-macros-v1.vcxproj", new List<string>{"junk-finddir\\junk-finddir.vcxproj", "WpfToolTip\\WpfToolTip.csproj"}},
                    {"WpfToolTip\\WpfToolTip.csproj", new List<string>{"junk-finddir\\junk-finddir.vcxproj"}}
                };
                dataModel.SetField("projectDependenies", projectDependenies);

                List<BuildInfo> criticalPath = new List<BuildInfo>
                {
                    new BuildInfo("junk-finddir\\junk-finddir.vcxproj", "junk-finddir.vcxproj", 1, 399931, 54684041, true),
                    new BuildInfo("WpfToolTip\\WpfToolTip.csproj", "WpfToolTip.csproj", 2, 56140367, 144319141, true),
                    new BuildInfo("variadic-macros-v1\\variadic-macros-v1.vcxproj", "variadic-macros-v1.vcxproj", 6, 144449121, 172642436, true)
                };
                dataModel.SetField("criticalPath", criticalPath);

                List<Tuple<long, float>> cpuUsage = new List<Tuple<long, float>>()
                {
                    new Tuple<long, float>(636849135031963260, 0.0f),
                    new Tuple<long, float>(636849135042025422, 47.36607f),
                    new Tuple<long, float>(636849135052097336, 43.76379f),
                    new Tuple<long, float>(636849135062946043, 36.62839f),
                    new Tuple<long, float>(636849135073055216, 45.90309f),
                    new Tuple<long, float>(636849135083364284, 48.84662f),
                    new Tuple<long, float>(636849135094055281, 48.48177f),
                    new Tuple<long, float>(636849135108063077, 98.88455f),
                    new Tuple<long, float>(636849135118301475, 100f),
                    new Tuple<long, float>(636849135130849521, 100f),
                    new Tuple<long, float>(636849135142927622, 87.70002f),
                    new Tuple<long, float>(636849135152926055, 91.4049f),
                    new Tuple<long, float>(636849135163764355, 76.97614f),
                    new Tuple<long, float>(636849135174642665, 89.21751f),
                    new Tuple<long, float>(636849135188218652, 79.268f),
                    new Tuple<long, float>(636849135199266903, 58.98672f)
                };
                dataModel.SetField("cpuUsage", cpuUsage);

                List<Tuple<long, float>> hddUsage = new List<Tuple<long, float>>()
                {
                    new Tuple<long, float>(636849135031963260, 0.0f),
                    new Tuple<long, float>(636849135042025422, 64.68444f),
                    new Tuple<long, float>(636849135052097336, 10.2781f),
                    new Tuple<long, float>(636849135062946043, 2.541418f),
                    new Tuple<long, float>(636849135073055216, 5.857255f),
                    new Tuple<long, float>(636849135083364284, 8.998146f),
                    new Tuple<long, float>(636849135094055281, 3.024021f),
                    new Tuple<long, float>(636849135108063077, 18.93993f),
                    new Tuple<long, float>(636849135118301475, 25.54949f),
                    new Tuple<long, float>(636849135130849521, 1.439461f),
                    new Tuple<long, float>(636849135142927622, 2.991249f),
                    new Tuple<long, float>(636849135152926055, 1.817592f),
                    new Tuple<long, float>(636849135163764355, 4.232076f),
                    new Tuple<long, float>(636849135174642665, 6.579974f),
                    new Tuple<long, float>(636849135188218652, 8.768057f),
                    new Tuple<long, float>(636849135199266903, 0.652989f),
                };
                dataModel.SetField("hddUsage", hddUsage);

                { // private static need to be handled through PrivateType
                    PrivateType pt = new PrivateType(typeof(DataModel));
                    pt.SetStaticFieldOrProperty("projectBuildOrderNumber", 6u);
                }

                Assert.AreEqual(DataModel.Instance.SolutionName, "Example.sln"); // Verify if internal data were updated
            }

            PBMControl pBMcontrol = new PBMControl();

            var graphControl = new PrivateObject(GraphControl.Instance); // Use PrivateObject class to change private member of GraphControl object.
            graphControl.SetField("nowTickForTest", 636849135031953262L + 172642436 + 1000); //1000 is just assumed delay for finishing (counting dependencies etc.)

            Window window = new Window
            {
                Width = 800,
                Height = 600,
                Content = pBMcontrol
            };

            window.Show(); //this will draw PBMControl
            //window.ShowDialog(); //for debug

            string expected = TestUtils.GetTestFile("BuildFinished.png");
            // ResultsDirectory   <= this is where results should be saved. How to get value of this xaml tag?
            string tmpFileName = Path.GetTempFileName(); //this method return path to non-existing file in temp directory
            tmpFileName += ".png";

            bool res = pBMcontrol.SaveGraph(tmpFileName /*pathToPngFile*/);
            Assert.IsTrue(res);
            Assert.IsTrue(ImageComparer.AreImagesEqual(expected, tmpFileName));
        }
    }
}
