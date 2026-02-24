using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelBuildsMonitor;
using ParallelBuildsMonitorTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ParallelBuildsMonitor.Tests
{

    [TestClass()]
    public class MachineInfoTests
    {
        // Ensures that all MachineInfo's fields were initialized (in constructor).
        [TestMethod()]
        public void ValuesInitialized()
        {
            MachineInfo machineInfo = MachineInfo.Instance;
            Assert.IsTrue(machineInfo.MachineName.Length > 0);
            Assert.IsTrue(machineInfo.PhysicalProcessorsNumber > 0);
            Assert.IsTrue(machineInfo.PhysicalCoresNumber > 0);
            Assert.IsTrue(machineInfo.LogicalCoresNumber > 0);
            Assert.IsTrue(machineInfo.CpusSpeedInMHz.Count > 0);
            Assert.IsTrue(machineInfo.CpusSpeedInMHz[0] > 1000);
            Assert.IsTrue(machineInfo.TotalPhysicalMemoryInGB > 2);
            Assert.IsTrue(machineInfo.PhysicalHDDsNumber > 0);
            Assert.IsTrue(machineInfo.HddsTypes.Count > 0);
        }

        // Ensures that MachineInfo returns a string that contains all required fields in the correct format.
        [TestMethod()]
        public void ValidateMachineInfoString()
        {
            Assert.IsTrue(TestUtils.ValidateMachineInfoLine(MachineInfo.Instance.ToString())); // NOTE: The test is failing probably because debugger is calling ToString() method, before the constructor is finished, making "separatorCached" set and consequently returning empty "info"
        }
    }
}
