using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace KuFi.Engine.Services
{
    /// <summary>
    /// Service untuk membaca CPU dan RAM secara real-time.
    /// Menggunakan P/Invoke ke Windows API native (kernel32.dll) agar jauh lebih ringan 
    /// daripada menggunakan System.Diagnostics.PerformanceCounter.
    /// Sangat cocok untuk spesifikasi RAM 4GB.
    /// </summary>
    public class SystemMonitorService
    {
        public delegate void ResourceUpdateHandler(double cpuUsage, double availableRamGb);
        public event ResourceUpdateHandler? OnUpdate;

        private CancellationTokenSource? _cts;

        // P/Invoke untuk membaca RAM sistem
        [StructLayout(LayoutKind.Sequential)]
        private struct MEMORYSTATUSEX
        {
            public uint dwLength;
            public uint dwMemoryLoad;
            public ulong ullTotalPhys;
            public ulong ullAvailPhys;
            public ulong ullTotalPageFile;
            public ulong ullAvailPageFile;
            public ulong ullTotalVirtual;
            public ulong ullAvailVirtual;
            public ulong ullAvailExtendedVirtual;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool GlobalMemoryStatusEx(ref MEMORYSTATUSEX lpBuffer);

        // P/Invoke untuk membaca CPU usage
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetSystemTimes(out FILETIME lpIdleTime, out FILETIME lpKernelTime, out FILETIME lpUserTime);

        [StructLayout(LayoutKind.Sequential)]
        private struct FILETIME
        {
            public uint dwLowDateTime;
            public uint dwHighDateTime;

            public ulong ToULong()
            {
                return ((ulong)dwHighDateTime << 32) | dwLowDateTime;
            }
        }

        public void StartMonitoring()
        {
            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            // Memastikan monitor berjalan di thread terpisah agar tidak membekukan UI
            Task.Run(async () =>
            {
                try
                {
                    FILETIME prevIdleTime = default;
                    FILETIME prevKernelTime = default;
                    FILETIME prevUserTime = default;
                    
                    GetSystemTimes(out prevIdleTime, out prevKernelTime, out prevUserTime);

                    while (!token.IsCancellationRequested)
                    {
                        // 1. Ambil Available RAM
                        MEMORYSTATUSEX memStatus = new MEMORYSTATUSEX();
                        memStatus.dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
                        
                        double availRamGb = 0;
                        if (GlobalMemoryStatusEx(ref memStatus))
                        {
                            availRamGb = memStatus.ullAvailPhys / (1024.0 * 1024.0 * 1024.0);
                        }

                        // 2. Ambil CPU usage dengan mengalkulasi perbedaan tick dalam 1 detik
                        await Task.Delay(1000, token); 
                        
                        GetSystemTimes(out FILETIME idleTime, out FILETIME kernelTime, out FILETIME userTime);
                        
                        ulong sysIdle = idleTime.ToULong() - prevIdleTime.ToULong();
                        ulong sysKernel = kernelTime.ToULong() - prevKernelTime.ToULong();
                        ulong sysUser = userTime.ToULong() - prevUserTime.ToULong();
                        
                        ulong sysTotal = sysKernel + sysUser;
                        
                        double cpuUsage = 0;
                        if (sysTotal > 0)
                        {
                            cpuUsage = ((sysTotal - sysIdle) * 100.0) / sysTotal;
                        }

                        prevIdleTime = idleTime;
                        prevKernelTime = kernelTime;
                        prevUserTime = userTime;

                        // Lempar hasil ke event
                        OnUpdate?.Invoke(Math.Round(cpuUsage, 1), Math.Round(availRamGb, 1));
                    }
                }
                catch (Exception)
                {
                    // Catch diam agar tidak ada crash aplikasi jika API gagal dipanggil
                }
            }, token);
        }

        public void StopMonitoring()
        {
            _cts?.Cancel();
        }
    }
}
