using Microsoft.Win32.SafeHandles;
using System;
using System.Runtime.InteropServices;
using System.Text;


//
//
//   NOTE:
//      This is working solution however NOT used right now!
//      Now, Windows provide new way of SSD check -> MSFT_PhysicalDisk
//
//


namespace ParallelBuildsMonitor
{
    // Code from page: https://emoacht.wordpress.com/2012/11/06/csharp-ssd/

    /// <summary>
    /// Entry method is IsSsdDrive(). Check its description for details.
    /// </summary>
    public static class DetectSsd
    {
        internal static class NativeMethods
        {
            // For CreateFile to get handle to drive
            private const uint GENERIC_READ = 0x80000000;
            private const uint GENERIC_WRITE = 0x40000000;
            private const uint FILE_SHARE_READ = 0x00000001;
            private const uint FILE_SHARE_WRITE = 0x00000002;
            private const uint OPEN_EXISTING = 3;
            private const uint FILE_ATTRIBUTE_NORMAL = 0x00000080;

            // CreateFile to get handle to drive
            [DllImport("kernel32.dll", SetLastError = true)]
            private static extern SafeFileHandle CreateFileW(
                [MarshalAs(UnmanagedType.LPWStr)]
                string lpFileName,
                uint dwDesiredAccess,
                uint dwShareMode,
                IntPtr lpSecurityAttributes,
                uint dwCreationDisposition,
                uint dwFlagsAndAttributes,
                IntPtr hTemplateFile);

            // For control codes
            private const uint FILE_DEVICE_MASS_STORAGE = 0x0000002d;
            private const uint IOCTL_STORAGE_BASE = FILE_DEVICE_MASS_STORAGE;
            private const uint FILE_DEVICE_CONTROLLER = 0x00000004;
            private const uint IOCTL_SCSI_BASE = FILE_DEVICE_CONTROLLER;
            private const uint METHOD_BUFFERED = 0;
            private const uint FILE_ANY_ACCESS = 0;
            private const uint FILE_READ_ACCESS = 0x00000001;
            private const uint FILE_WRITE_ACCESS = 0x00000002;

            private static uint CTL_CODE(uint DeviceType, uint Function,
                                         uint Method, uint Access)
            {
                return ((DeviceType << 16) | (Access << 14) |
                        (Function << 2) | Method);
            }

            // For DeviceIoControl to check no seek penalty
            private const uint StorageDeviceSeekPenaltyProperty = 7;
            private const uint PropertyStandardQuery = 0;

            [StructLayout(LayoutKind.Sequential)]
            private struct STORAGE_PROPERTY_QUERY
            {
                public uint PropertyId;
                public uint QueryType;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
                public byte[] AdditionalParameters;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct DEVICE_SEEK_PENALTY_DESCRIPTOR
            {
                public uint Version;
                public uint Size;
                [MarshalAs(UnmanagedType.U1)]
                public bool IncursSeekPenalty;
            }

            // DeviceIoControl to check no seek penalty
            [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl",
                       SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                ref STORAGE_PROPERTY_QUERY lpInBuffer,
                uint nInBufferSize,
                ref DEVICE_SEEK_PENALTY_DESCRIPTOR lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

            // For DeviceIoControl to check nominal media rotation rate
            private const uint ATA_FLAGS_DATA_IN = 0x02;

            [StructLayout(LayoutKind.Sequential)]
            private struct ATA_PASS_THROUGH_EX
            {
                public ushort Length;
                public ushort AtaFlags;
                public byte PathId;
                public byte TargetId;
                public byte Lun;
                public byte ReservedAsUchar;
                public uint DataTransferLength;
                public uint TimeOutValue;
                public uint ReservedAsUlong;
                public IntPtr DataBufferOffset;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                public byte[] PreviousTaskFile;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
                public byte[] CurrentTaskFile;
            }

            [StructLayout(LayoutKind.Sequential)]
            private struct ATAIdentifyDeviceQuery
            {
                public ATA_PASS_THROUGH_EX header;
                [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
                public ushort[] data;
            }

            // DeviceIoControl to check nominal media rotation rate
            [DllImport("kernel32.dll", EntryPoint = "DeviceIoControl",
                       SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            private static extern bool DeviceIoControl(
                SafeFileHandle hDevice,
                uint dwIoControlCode,
                ref ATAIdentifyDeviceQuery lpInBuffer,
                uint nInBufferSize,
                ref ATAIdentifyDeviceQuery lpOutBuffer,
                uint nOutBufferSize,
                out uint lpBytesReturned,
                IntPtr lpOverlapped);

            // For error message
            private const uint FORMAT_MESSAGE_FROM_SYSTEM = 0x00001000;

            // From: https://referencesource.microsoft.com/#system/compmod/microsoft/win32/SafeNativeMethods.cs
            [DllImport("kernel32.dll", CharSet = System.Runtime.InteropServices.CharSet.Auto, SetLastError = true, BestFitMapping = true)]
            [System.Runtime.Versioning.ResourceExposure(System.Runtime.Versioning.ResourceScope.None)]
            static extern uint FormatMessage(
                uint dwFlags,
                IntPtr lpSource,
                uint dwMessageId,
                uint dwLanguageId,
                StringBuilder lpBuffer,
                uint nSize,
                IntPtr Arguments);


            /// <summary>
            /// Method for no seek penalty
            /// </summary>
            /// <param name="sDrive"></param>
            /// <returns></returns>
            public static DriveType HasNoSeekPenalty(string sDrive)
            {
                SafeFileHandle hDrive = CreateFileW(
                    sDrive,
                    0, // No access to drive
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hDrive == null || hDrive.IsInvalid)
                {
                    //string message = GetErrorMessage(Marshal.GetLastWin32Error());
                    //Console.WriteLine("CreateFile failed. " + message);
                    return DriveType.Unknown;
                }

                uint IOCTL_STORAGE_QUERY_PROPERTY = CTL_CODE(
                    IOCTL_STORAGE_BASE, 0x500,
                    METHOD_BUFFERED, FILE_ANY_ACCESS); // From winioctl.h

                STORAGE_PROPERTY_QUERY query_seek_penalty =
                    new STORAGE_PROPERTY_QUERY();
                query_seek_penalty.PropertyId = StorageDeviceSeekPenaltyProperty;
                query_seek_penalty.QueryType = PropertyStandardQuery;

                DEVICE_SEEK_PENALTY_DESCRIPTOR query_seek_penalty_desc =
                    new DEVICE_SEEK_PENALTY_DESCRIPTOR();

                uint returned_query_seek_penalty_size;

                bool query_seek_penalty_result = DeviceIoControl(
                    hDrive,
                    IOCTL_STORAGE_QUERY_PROPERTY,
                    ref query_seek_penalty,
                    (uint)Marshal.SizeOf(query_seek_penalty),
                    ref query_seek_penalty_desc,
                    (uint)Marshal.SizeOf(query_seek_penalty_desc),
                    out returned_query_seek_penalty_size,
                    IntPtr.Zero);

                if (query_seek_penalty_result == false)
                {
                    //string message = GetErrorMessage(Marshal.GetLastWin32Error());
                    //Console.WriteLine("DeviceIoControl failed. " + message);
                }

                hDrive.Close();

                if (query_seek_penalty_result == false)
                    return DriveType.Unknown;

                if (query_seek_penalty_desc.IncursSeekPenalty == false)
                {
                    //Console.WriteLine("This drive has NO SEEK penalty.");
                    return DriveType.SSD;
                }
                else
                {
                    //Console.WriteLine("This drive has SEEK penalty.");
                    return DriveType.Rotational;
                }
            }

            /// <summary>
            /// Method for nominal media rotation rate.
            /// </summary>
            /// <remarks>
            /// Administrative privilege is required!
            /// </remarks>
            /// <param name="sDrive"></param>
            /// <returns></returns>
            public static DriveType HasNominalMediaRotationRate(string sDrive)
            {
                SafeFileHandle hDrive = CreateFileW(
                    sDrive,
                    GENERIC_READ | GENERIC_WRITE, // Administrative privilege is required
                    FILE_SHARE_READ | FILE_SHARE_WRITE,
                    IntPtr.Zero,
                    OPEN_EXISTING,
                    FILE_ATTRIBUTE_NORMAL,
                    IntPtr.Zero);

                if (hDrive == null || hDrive.IsInvalid)
                {
                    //string message = GetErrorMessage(Marshal.GetLastWin32Error());
                    //Console.WriteLine("CreateFile failed. " + message);
                    return DriveType.Unknown;
                }

                uint IOCTL_ATA_PASS_THROUGH = CTL_CODE(
                    IOCTL_SCSI_BASE, 0x040b, METHOD_BUFFERED,
                    FILE_READ_ACCESS | FILE_WRITE_ACCESS); // From ntddscsi.h

                ATAIdentifyDeviceQuery id_query = new ATAIdentifyDeviceQuery();
                id_query.data = new ushort[256];

                id_query.header.Length = (ushort)Marshal.SizeOf(id_query.header);
                id_query.header.AtaFlags = (ushort)ATA_FLAGS_DATA_IN;
                id_query.header.DataTransferLength =
                    (uint)(id_query.data.Length * 2); // Size of "data" in bytes
                id_query.header.TimeOutValue = 3; // Sec
                id_query.header.DataBufferOffset = (IntPtr)Marshal.OffsetOf(
                    typeof(ATAIdentifyDeviceQuery), "data");
                id_query.header.PreviousTaskFile = new byte[8];
                id_query.header.CurrentTaskFile = new byte[8];
                id_query.header.CurrentTaskFile[6] = 0xec; // ATA IDENTIFY DEVICE

                uint retval_size;

                bool result = DeviceIoControl(
                    hDrive,
                    IOCTL_ATA_PASS_THROUGH,
                    ref id_query,
                    (uint)Marshal.SizeOf(id_query),
                    ref id_query,
                    (uint)Marshal.SizeOf(id_query),
                    out retval_size,
                    IntPtr.Zero);

                if (result == false)
                {
                    string message = GetErrorMessage(Marshal.GetLastWin32Error());
                    //Console.WriteLine("DeviceIoControl failed. " + message);
                }

                hDrive.Close();

                if (result == false)
                    return DriveType.Unknown;

                // Word index of nominal media rotation rate
                // (1 means non-rotate device)
                const int kNominalMediaRotRateWordIndex = 217;

                if (id_query.data[kNominalMediaRotRateWordIndex] == 1)
                {
                    //Console.WriteLine("This drive is NON-ROTATE device.");
                    return DriveType.SSD;
                }
                else
                {
                    //Console.WriteLine("This drive is ROTATE device.");
                    return DriveType.Rotational;
                }
            }

            // Method for error message
            private static string GetErrorMessage(int code)
            {
                StringBuilder message = new StringBuilder(255);

                FormatMessage(
                  FORMAT_MESSAGE_FROM_SYSTEM,
                  IntPtr.Zero,
                  (uint)code,
                  0,
                  message,
                  (uint)message.Capacity,
                  IntPtr.Zero);

                return message.ToString();
            }
        }

        public enum DriveType
        {
            Unknown,
            SSD,
            Rotational
        }


        /// <summary>
        /// This method return if questioned HDD is SSD or not.
        /// </summary>
        /// <remarks>
        /// <para>
        /// There is no perfect method to detect type of HDD drive.
        /// E.g.: Hybrid drives combine Rotational and SSD together, all hidden behind hardware.
        /// Another aspect is that methods in this class ask HDD Driver for relevant information.
        /// If that information is not provided by Driver, e.g. due to using generic HDD Driver
        /// or because dedicated Driver doesn't provide such information at all, those metods are 
        /// unable to determine HDD type.
        /// </para>
        /// Detection algorithm base on Microsoft algorithm:
        /// https://blogs.technet.microsoft.com/filecab/2009/11/25/windows-7-disk-defragmenter-user-interface-overview/
        /// which is:
        /// <code>
        /// if (no_seek_penalty)
        ///     return DriveType.SSD;
        /// if (is_ATA_nominal_disc_rate)
        ///     return DriveType.SSD;
        /// return DriveType.Rotational;
        /// </code>
        /// </remarks>
        /// <param name="physicalDriveNumber"></param>
        /// <returns></returns>
        public static DriveType IsSsdDrive(int physicalDriveNumber)
        {
            try
            { // Not sure if try{} catch{} is needed here

                string physicalDrive = "\\\\.\\PhysicalDrive" + physicalDriveNumber.ToString();
                DriveType driveType = DetectSsd.NativeMethods.HasNoSeekPenalty(physicalDrive);
                if (driveType != DriveType.Unknown)
                    return driveType;

                return DetectSsd.NativeMethods.HasNominalMediaRotationRate(physicalDrive);
            }
            catch
            {
                System.Diagnostics.Debug.Assert(false, "Getting Type of HDDs failed! Exception thrown while trying to to determine HDD type.");
                return DetectSsd.DriveType.Unknown;
            }
        }
    }
}
