using Microsoft.VisualStudio.TestTools.UnitTesting;
using ParallelBuildsMonitor;
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
        [TestMethod()]
        public void ToStringTest()
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
    }
}
