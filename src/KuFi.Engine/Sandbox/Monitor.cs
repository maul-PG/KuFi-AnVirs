using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace KuFi.Engine.Sandbox
{
    /// <summary>
    /// Modul pemantauan sumber daya RAM dan CPU khusus untuk proses di dalam Sandbox.
    /// Dibuat sangat ringan dan tidak akan membuat UI lag (hang).
    /// </summary>
    public class Monitor
    {
        public delegate void ResourceUsageUpdateHandler(double cpuUsage, double ramUsageMb);
        public event ResourceUsageUpdateHandler? OnUpdate;

        private CancellationTokenSource? _cancellationTokenSource;

        /// <summary>
        /// Memulai loop monitoring di thread background.
        /// </summary>
        public void StartMonitoring(int processId)
        {
            _cancellationTokenSource = new CancellationTokenSource();
            CancellationToken token = _cancellationTokenSource.Token;

            // Membungkusnya di dalam Task.Run agar berjalan asinkron di Background Thread,
            // sehingga aplikasi utama KuFi dan UI tidak mengalami freeze/hang.
            Task.Run(async () =>
            {
                try
                {
                    using (Process targetProcess = Process.GetProcessById(processId))
                    {
                        DateTime lastTime = DateTime.Now;
                        TimeSpan lastTotalProcessorTime = targetProcess.TotalProcessorTime;

                        // Loop berjalan selama proses masih hidup dan tidak dibatalkan
                        while (!token.IsCancellationRequested && !targetProcess.HasExited)
                        {
                            // Refresh data metrik sistem operasi untuk proses ini
                            targetProcess.Refresh();

                            // 1. Ekstraksi penggunaan RAM (WorkingSet64 mengembalikan byte)
                            double ramUsageMb = targetProcess.WorkingSet64 / (1024.0 * 1024.0);

                            // 2. Ekstraksi penggunaan CPU secara ringan (Perhitungan Manual Deltas)
                            // Ini jauh lebih ringan bagi sistem dibanding menggunakan PerformanceCounter.
                            DateTime currentTime = DateTime.Now;
                            TimeSpan currentTotalProcessorTime = targetProcess.TotalProcessorTime;
                            
                            double cpuUsage = 0;
                            double timePassedMs = (currentTime - lastTime).TotalMilliseconds;
                            
                            if (timePassedMs > 0)
                            {
                                double cpuTimePassedMs = (currentTotalProcessorTime - lastTotalProcessorTime).TotalMilliseconds;
                                // Menghitung persentase berdasar ketersediaan core
                                cpuUsage = (cpuTimePassedMs / (System.Environment.ProcessorCount * timePassedMs)) * 100.0;
                            }

                            // Update penanda waktu untuk siklus berikutnya
                            lastTime = currentTime;
                            lastTotalProcessorTime = currentTotalProcessorTime;

                            // Pancarkan event update ke UI
                            OnUpdate?.Invoke(cpuUsage, ramUsageMb);

                            // PENTING: Jeda 2 detik (2000ms).
                            // Hindari loop yang terlalu kencang (seperti Sleep 100ms) agar laptop RAM terbatas tidak hang.
                            await Task.Delay(2000, token);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Monitor Warning] Pemantauan sandbox dihentikan: {ex.Message}");
                }
            }, token);
        }

        /// <summary>
        /// Mematikan loop monitoring secara halus
        /// </summary>
        public void StopMonitoring()
        {
            if (_cancellationTokenSource != null)
            {
                _cancellationTokenSource.Cancel();
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }
    }
}
