using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;


namespace ParallelBuildsMonitor
{
    /// <summary>
    /// This call collect information about machine and return human readable string.
    /// </summary>
    public class MachineInfo
    {
        public string MachineName { get; private set; }
        public UInt32 PhysicalProcessorsNumber { get; private set; } = 0;
        public UInt32 PhysicalCoresNumber { get; private set; } = 0;
        public UInt32 LogicalCoresNumber { get; private set; } = 0;
        public ReadOnlyCollection<UInt32> CpusSpeedInMHz { get { return cpusSpeedInMHz.AsReadOnly(); } }
        public bool HyperThreadingEnabled { get; private set; } = false;
        public int TotalPhysicalMemoryInGB { get; private set; } = 0;
        public int PhysicalHDDsNumber { get; private set; } = 0;
        //public ReadOnlyCollection<UInt32> HddsSpeed { get { return hddsSpeed.AsReadOnly(); } } //TODO: Missing implementation.

        public List<UInt32> cpusSpeedInMHz = new List<UInt32>();

        static private MachineInfo instance;
        static public MachineInfo Instance
        {
            get
            {
                if (instance == null)
                    instance = new MachineInfo();
                return instance;
            }
        }
        private string info = null;

        private MachineInfo()
        {
            MachineName = Environment.MachineName;

            try
            { // Not sure if try{} catch{} is needed here
                foreach (var item in new System.Management.ManagementObjectSearcher("Select NumberOfProcessors, TotalPhysicalMemory from Win32_ComputerSystem").Get())
                {
                    PhysicalProcessorsNumber = (UInt32)item["NumberOfProcessors"];
                    TotalPhysicalMemoryInGB = (Int32)Math.Round(Convert.ToDouble(item.Properties["TotalPhysicalMemory"].Value) / 1048576 / 1024, 0);
                }
            }
            catch { }

            try
            { // Not sure if try{} catch{} is needed here
                foreach (var item in new System.Management.ManagementObjectSearcher("Select NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed from Win32_Processor").Get())
                {
                    PhysicalCoresNumber += (UInt32)item["NumberOfCores"];
                    LogicalCoresNumber += (UInt32)item["NumberOfLogicalProcessors"]; //Environment.ProcessorCount is the same as NumberOfLogicalProcessors
                    cpusSpeedInMHz.Add((UInt32)item["MaxClockSpeed"]);
                }
            }
            catch { }

            if (PhysicalCoresNumber != LogicalCoresNumber)
                HyperThreadingEnabled = true;

            foreach (System.IO.DriveInfo info in System.IO.DriveInfo.GetDrives())
            {
                if (info.DriveType == System.IO.DriveType.Fixed)
                    PhysicalHDDsNumber += 1;
            }
        }

        public override string ToString()
        {
            return ToString(" | ");
        }

        public string ToString(string separator)
        {
            if (info != null)
                return info;

            List<string> list = new List<string>();
            //if (MachineName.Length > 0)  // For now do not add machine name
            //    list.Add("Machine: " + MachineName);
            if (PhysicalProcessorsNumber > 0)
                list.Add("Processors: " + PhysicalProcessorsNumber.ToString());
            if (PhysicalCoresNumber > 0)
                list.Add("Cores: " + PhysicalCoresNumber.ToString());
            if (CpusSpeedInMHz.Count > 0)
            {
                bool AreAllValuesTheSame = CpusSpeedInMHz.Any(o => o != CpusSpeedInMHz[0]);
                List<string> values = new List<string>();
                foreach (UInt32 value in CpusSpeedInMHz)
                {
                    values.Add(String.Format("{0:0.0}GHz", ((double)value) / 1000, 1)); //This also do the rounding
                    if (AreAllValuesTheSame)
                        break;
                }
                list.Add("CPU Speed: " + string.Join(", ", values));
            }
            if (PhysicalCoresNumber > 0)
                list.Add("Hyper Threading: " + (HyperThreadingEnabled ? "Enabled" : "Disabled"));
            if (TotalPhysicalMemoryInGB > 0)
                list.Add("RAM: " + TotalPhysicalMemoryInGB.ToString() + "GB");
            if (PhysicalHDDsNumber > 0)
                list.Add("HDDs: " + PhysicalHDDsNumber);

            info = string.Join(separator, list);

            return info;
        }
    }
}
