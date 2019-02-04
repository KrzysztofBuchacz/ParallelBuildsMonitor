using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelBuildsMonitor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

using CM = ParallelBuildsMonitor.Tests.ComparisonMethods;
using System.Windows.Controls;
using System.Drawing;
using System.Security.Cryptography;

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
        public void SavePng_Empty()
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

            string expected = TestUtils.GetTestFile("Empty.png");
            // ResultsDirectory   <= this is where results should be saved. How to get value of this xaml tag?
            string tmpFileName = Path.GetTempFileName(); //this method return path to non-existing file in temp directory
            tmpFileName += ".png";

            bool res = pBMcontrol.SaveGraph(tmpFileName /*pathToPngFile*/);
            Assert.IsTrue(res);
            Assert.IsTrue(ImageComparer.AreImagesEqual(expected, tmpFileName));
        }

        [TestMethod()]
        public void SavePng_InProgress()
        {
            { // MachineInfo is in the picture, so make MachineInfo as machine indepenent string for unit test puroposes
                string machineIndepenentInfo = "Processors: 1 | Cores: 2 | CPU Speed: 2.2GHz | Hyper Threading: Enabled | RAM: 8GB | HDD: 1 SSD";
                MachineInfo mi = MachineInfo.Instance;
                var machineInfo = new PrivateObject(mi); // Use PrivateObject class to change private member of MachineInfo object.
                machineInfo.SetField("info", machineIndepenentInfo);

                Assert.AreEqual(MachineInfo.Instance.ToString(), machineIndepenentInfo);
            }

            // Missing further part of test...
        }
    }
}
