using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public class DiscoveredDevice
    {
        public string IpAddress { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public List<int> OpenPorts { get; set; } = new();
        public long LatencyMs { get; set; }
        public ConnectionProtocol DetectedProtocol { get; set; } = ConnectionProtocol.RDP;
        public bool IsSelected { get; set; } = true;

        public string ProtocolSummary
        {
            get
            {
                var list = new List<string>();
                if (OpenPorts.Contains(3389)) list.Add("RDP (3389)");
                if (OpenPorts.Contains(22)) list.Add("SSH (22)");
                if (OpenPorts.Contains(5900)) list.Add("VNC (5900)");
                if (OpenPorts.Contains(80) || OpenPorts.Contains(443)) list.Add("Web (80/443)");
                foreach (var p in OpenPorts)
                {
                    if (p != 3389 && p != 22 && p != 5900 && p != 80 && p != 443)
                        list.Add($"Port {p}");
                }
                return list.Count > 0 ? string.Join(", ", list) : "Online";
            }
        }
    }

    public class ScanProgressReport
    {
        public int ScannedCount { get; set; }
        public int TotalCount { get; set; }
        public string CurrentIp { get; set; } = string.Empty;
        public DiscoveredDevice? DiscoveredDevice { get; set; }
    }

    public static class NetworkScannerService
    {
        /// <summary>
        /// Detects active local network IPv4 subnet in CIDR format (e.g. 192.168.1.0/24).
        /// </summary>
        public static string DetectLocalSubnet()
        {
            try
            {
                foreach (var ni in NetworkInterface.GetAllNetworkInterfaces())
                {
                    if (ni.OperationalStatus != OperationalStatus.Up ||
                        ni.NetworkInterfaceType == NetworkInterfaceType.Loopback)
                        continue;

                    var ipProps = ni.GetIPProperties();
                    foreach (var addr in ipProps.UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == AddressFamily.InterNetwork &&
                            !IPAddress.IsLoopback(addr.Address))
                        {
                            byte[] ipBytes = addr.Address.GetAddressBytes();
                            byte[] maskBytes = addr.IPv4Mask?.GetAddressBytes() ?? new byte[] { 255, 255, 255, 0 };

                            byte[] netBytes = new byte[4];
                            int prefixLength = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                netBytes[i] = (byte)(ipBytes[i] & maskBytes[i]);
                                for (int b = 7; b >= 0; b--)
                                {
                                    if ((maskBytes[i] & (1 << b)) != 0) prefixLength++;
                                }
                            }

                            return $"{new IPAddress(netBytes)}/{prefixLength}";
                        }
                    }
                }
            }
            catch { }

            return "192.168.1.0/24";
        }

        /// <summary>
        /// Generates a list of IP addresses from CIDR (e.g. 192.168.1.0/24) or range (e.g. 192.168.1.1-254).
        /// </summary>
        public static List<string> ParseIpRange(string input)
        {
            var result = new List<string>();
            if (string.IsNullOrWhiteSpace(input)) return result;

            input = input.Trim();

            // Format 1: CIDR e.g. 192.168.1.0/24
            if (input.Contains('/'))
            {
                var parts = input.Split('/');
                if (IPAddress.TryParse(parts[0], out var baseIp) && int.TryParse(parts[1], out int prefix))
                {
                    if (prefix >= 16 && prefix <= 30)
                    {
                        uint ipVal = BitConverter.ToUInt32(baseIp.GetAddressBytes().Reverse().ToArray(), 0);
                        uint mask = ~(uint.MaxValue >> prefix);
                        uint network = ipVal & mask;
                        uint broadcast = network | ~mask;

                        // Hosts range between network+1 and broadcast-1
                        for (uint i = network + 1; i < broadcast && result.Count < 1024; i++)
                        {
                            byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                            result.Add(new IPAddress(bytes).ToString());
                        }
                        return result;
                    }
                }
            }

            // Format 2: Range e.g. 192.168.1.1 - 192.168.1.50 or 192.168.1.1-50
            if (input.Contains('-'))
            {
                var parts = input.Split('-');
                string startStr = parts[0].Trim();
                string endStr = parts[1].Trim();

                if (IPAddress.TryParse(startStr, out var startIp))
                {
                    if (!endStr.Contains('.') && int.TryParse(endStr, out int endLastOctet))
                    {
                        // 192.168.1.1 - 50 format
                        byte[] sBytes = startIp.GetAddressBytes();
                        int startLastOctet = sBytes[3];
                        for (int o = startLastOctet; o <= endLastOctet && o <= 254 && result.Count < 512; o++)
                        {
                            result.Add($"{sBytes[0]}.{sBytes[1]}.{sBytes[2]}.{o}");
                        }
                        return result;
                    }
                    else if (IPAddress.TryParse(endStr, out var endIp))
                    {
                        uint startVal = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
                        uint endVal = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);
                        if (endVal >= startVal)
                        {
                            for (uint i = startVal; i <= endVal && result.Count < 512; i++)
                            {
                                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                                result.Add(new IPAddress(bytes).ToString());
                            }
                            return result;
                        }
                    }
                }
            }

            // Format 3: Single IP or comma-separated IPs
            var commaSeparated = input.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var item in commaSeparated)
            {
                string trimmed = item.Trim();
                if (IPAddress.TryParse(trimmed, out _))
                {
                    result.Add(trimmed);
                }
            }

            return result;
        }

        /// <summary>
        /// Asynchronously scans a list of IP addresses across specified ports using high concurrency.
        /// </summary>
        public static async Task ScanNetworkAsync(
            List<string> ipList,
            List<int> portsToCheck,
            int timeoutMs,
            IProgress<ScanProgressReport> progress,
            CancellationToken ct)
        {
            if (ipList == null || ipList.Count == 0 || portsToCheck == null || portsToCheck.Count == 0)
                return;

            int total = ipList.Count;
            int scanned = 0;
            using var semaphore = new SemaphoreSlim(40); // 40 parallel tasks

            var tasks = ipList.Select(async ip => {
                await semaphore.WaitAsync(ct);
                try
                {
                    if (ct.IsCancellationRequested) return;

                    var discovered = await ProbeDeviceAsync(ip, portsToCheck, timeoutMs, ct);

                    int count = Interlocked.Increment(ref scanned);
                    progress?.Report(new ScanProgressReport
                    {
                        ScannedCount = count,
                        TotalCount = total,
                        CurrentIp = ip,
                        DiscoveredDevice = discovered
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private static async Task<DiscoveredDevice?> ProbeDeviceAsync(
            string ip,
            List<int> ports,
            int timeoutMs,
            CancellationToken ct)
        {
            var openPorts = new List<int>();
            long bestLatency = 0;

            // 1. Probe all requested ports concurrently for this IP
            var portTasks = ports.Select(async port => {
                var sw = System.Diagnostics.Stopwatch.StartNew();
                try
                {
                    using var tcp = new TcpClient();
                    using var reg = ct.Register(() => tcp.Close());
                    var connectTask = tcp.ConnectAsync(ip, port);
                    var delayTask = Task.Delay(timeoutMs, ct);

                    var completed = await Task.WhenAny(connectTask, delayTask);
                    sw.Stop();

                    if (completed == connectTask && tcp.Connected)
                    {
                        return (Port: port, IsOpen: true, Latency: sw.ElapsedMilliseconds);
                    }
                }
                catch { }
                return (Port: port, IsOpen: false, Latency: 0L);
            });

            var portResults = await Task.WhenAll(portTasks);

            foreach (var r in portResults)
            {
                if (r.IsOpen)
                {
                    openPorts.Add(r.Port);
                    if (bestLatency == 0 || r.Latency < bestLatency)
                        bestLatency = r.Latency;
                }
            }

            // If no port open, do a quick ICMP ping to check if alive at all (optional)
            if (openPorts.Count == 0)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, Math.Min(timeoutMs, 400));
                    if (reply.Status == IPStatus.Success)
                    {
                        bestLatency = reply.RoundtripTime;
                        // Device is alive but ports closed
                    }
                    else
                    {
                        return null; // Host is dead
                    }
                }
                catch
                {
                    return null;
                }
            }

            // 2. Resolve Hostname via reverse DNS (with 800ms limit)
            string hostname = string.Empty;
            try
            {
                var dnsTask = Dns.GetHostEntryAsync(ip);
                var timeoutTask = Task.Delay(800, ct);
                var completed = await Task.WhenAny(dnsTask, timeoutTask);
                if (completed == dnsTask && dnsTask.IsCompletedSuccessfully)
                {
                    hostname = dnsTask.Result.HostName;
                }
            }
            catch { }

            if (string.IsNullOrWhiteSpace(hostname))
                hostname = ip;

            // Determine primary protocol
            ConnectionProtocol proto = ConnectionProtocol.RDP;
            if (openPorts.Contains(3389))
                proto = ConnectionProtocol.RDP;
            else if (openPorts.Contains(22))
                proto = ConnectionProtocol.SSH;

            return new DiscoveredDevice
            {
                IpAddress = ip,
                HostName = hostname,
                OpenPorts = openPorts,
                LatencyMs = bestLatency,
                DetectedProtocol = proto,
                IsSelected = true
            };
        }
    }
}
