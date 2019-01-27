using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Management;

namespace ParallelBuildsMonitor
{
    /// <summary>
    /// MachineInfo collect information about machine configuration and return as human readable string.
    /// </summary>
    /// <remarks>
    /// Informations are collected only once and then cached.
    /// </remarks>
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
        public ReadOnlyCollection<DetectSsd.DriveType> HddsTypes { get { return hddsTypes.AsReadOnly(); } }

        public List<UInt32> cpusSpeedInMHz = new List<UInt32>();
        public List<DetectSsd.DriveType> hddsTypes = new List<DetectSsd.DriveType>();
        private string separatorCached;

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
                foreach (var item in new ManagementObjectSearcher("Select NumberOfProcessors, TotalPhysicalMemory from Win32_ComputerSystem").Get())
                {
                    PhysicalProcessorsNumber = (UInt32)item["NumberOfProcessors"];
                    TotalPhysicalMemoryInGB = (Int32)Math.Round(Convert.ToDouble(item.Properties["TotalPhysicalMemory"].Value) / 1048576 / 1024, 0);
                }
            }
            catch
            {
                Debug.Assert(false, "Getting Physical Processors Number or Total Physical Memory failed! Exception thrown while trying to possess those values.");
            }

            try
            { // Not sure if try{} catch{} is needed here
                foreach (var item in new ManagementObjectSearcher("Select NumberOfCores, NumberOfLogicalProcessors, MaxClockSpeed from Win32_Processor").Get())
                {
                    PhysicalCoresNumber += (UInt32)item["NumberOfCores"];
                    LogicalCoresNumber += (UInt32)item["NumberOfLogicalProcessors"]; //Environment.ProcessorCount is the same as NumberOfLogicalProcessors
                    cpusSpeedInMHz.Add((UInt32)item["MaxClockSpeed"]);
                }
            }
            catch
            {
                Debug.Assert(false, "Getting Physical Cores Number, Logical Cores Number or CPU Speed failed! Exception thrown while trying to possess those values.");
            }


            if (PhysicalCoresNumber != LogicalCoresNumber)
                HyperThreadingEnabled = true;

            try
            { // Not sure if try{} catch{} is needed here
                foreach (var item in new ManagementObjectSearcher("SELECT DeviceID, TotalHeads FROM Win32_DiskDrive").Get())
                {
                    PhysicalHDDsNumber += 1;
                }
            }
            catch
            {
                Debug.Assert(false, "Getting Physical HDDs Number failed! Exception thrown while trying to possess those values.");
            }


            if (PhysicalHDDsNumber > 0)
            {
                for (int ii = 0; ii < PhysicalHDDsNumber; ii++)
                {
                        //DetectSsd.DriveType driveType = DetectSsd.IsSsdDrive(ii); // Temporary removing old way
                        DetectSsd.DriveType driveType = GetDriveType(ii);
                        hddsTypes.Add(driveType);
                }
            }
        }

        public static DetectSsd.DriveType GetDriveType(int physicalDriveNumber)
        {
            try
            { // Not sure if try{} catch{} is needed here
                using (ManagementObjectSearcher physicalDiskSearcher = new ManagementObjectSearcher(
                            @"\\localhost\ROOT\Microsoft\Windows\Storage",
                            $"SELECT MediaType FROM MSFT_PhysicalDisk WHERE DeviceID='{physicalDriveNumber}'"))
                {
                    ManagementBaseObject physicalDisk = physicalDiskSearcher.Get().Cast<ManagementBaseObject>().Single();
                    UInt16 mediaType = (UInt16)physicalDisk["MediaType"];
                    switch (mediaType)
                    { // According to: https://docs.microsoft.com/en-us/previous-versions/windows/desktop/stormgmt/msft-physicaldisk
                        case 0: return DetectSsd.DriveType.Unknown;
                        case 3: return DetectSsd.DriveType.Rotational;  // Is it correct?
                        case 4: return DetectSsd.DriveType.SSD;
                        case 5: return DetectSsd.DriveType.Unknown;     // Is it correct?
                    }

                    return DetectSsd.DriveType.Unknown;
                }
            }
            catch
            {
                Debug.Assert(false, "Getting Type of HDDs failed! Exception thrown while trying to to determine HDD type through ManagementObject.");
                return DetectSsd.DriveType.Unknown;
            }
        }


        public override string ToString()
        {
            return ToString(" | ");
        }

        public string ToString(string separator)
        {
            if (separatorCached == separator && info != null)
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
                bool AreAllValuesTheSame = !CpusSpeedInMHz.Any(oo => oo != CpusSpeedInMHz[0]);
                List<string> values = new List<string>();
                foreach (UInt32 value in CpusSpeedInMHz)
                {
                    values.Add(String.Format(System.Globalization.CultureInfo.InvariantCulture, "{0:0.0}GHz", ((double)value) / 1000)); //This also do the rounding
                    if (AreAllValuesTheSame)
                        break;
                }
                list.Add("CPU" + ((values.Count > 1) ? "s" : "") + " Speed: " + string.Join(", ", values));
            }
            if (PhysicalCoresNumber > 0)
                list.Add("Hyper Threading: " + (HyperThreadingEnabled ? "Enabled" : "Disabled"));
            if (TotalPhysicalMemoryInGB > 0)
                list.Add("RAM: " + TotalPhysicalMemoryInGB.ToString() + "GB");
            if (PhysicalHDDsNumber > 0)
            {
                List<DetectSsd.DriveType> values = new List<DetectSsd.DriveType>();
                bool AreAllValuesTheSame = !HddsTypes.Any(oo => oo != HddsTypes[0]);
                foreach (DetectSsd.DriveType value in HddsTypes)
                {
                    if (AreAllValuesTheSame)
                    { // Add only one value if all the same type
                        if (DetectSsd.DriveType.Unknown != value)
                            values.Add(value); //If all Unknown then do not add any info.
                        break;
                    }
                    values.Add(value);
                }
                list.Add("HDD: " + PhysicalHDDsNumber + ((values.Count > 1) ? ":" : "") + ((values.Count > 0) ? " " : "") + string.Join(", ", values));
            }


            separatorCached = separator;
            info = string.Join(separator, list);

            return info;
        }
    }
}
