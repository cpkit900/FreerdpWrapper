using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using FreeRdpWrapper.Models;

namespace FreeRdpWrapper.Services
{
    public class NetworkAdapterInfo
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string SubnetMask { get; set; } = "255.255.255.0";
        public int PrefixLength { get; set; } = 24;
        public string GatewayIp { get; set; } = string.Empty;
        public string StartIp { get; set; } = string.Empty;
        public string EndIp { get; set; } = string.Empty;
        public bool HasGateway => !string.IsNullOrEmpty(GatewayIp);

        public string DisplayText => $"{Name} ({IpAddress}/{PrefixLength})";
    }

    public class DiscoveredDevice
    {
        public string InterfaceName { get; set; } = string.Empty;
        public string IpAddress { get; set; } = string.Empty;
        public string HostName { get; set; } = string.Empty;
        public List<int> OpenPorts { get; set; } = new();
        public long LatencyMs { get; set; }
        public ConnectionProtocol DetectedProtocol { get; set; } = ConnectionProtocol.RDP;
        public bool IsSelected { get; set; } = true;

        public string ServiceSummary
        {
            get
            {
                var list = new List<string>();
                if (OpenPorts.Contains(3389)) list.Add("RDP");
                if (OpenPorts.Contains(22)) list.Add("SSH");
                if (OpenPorts.Contains(5900)) list.Add("VNC");
                if (OpenPorts.Contains(80) || OpenPorts.Contains(443)) list.Add("WEB");
                if (OpenPorts.Contains(23)) list.Add("TELNET");
                foreach (var p in OpenPorts)
                {
                    if (p != 3389 && p != 22 && p != 5900 && p != 80 && p != 443 && p != 23)
                        list.Add($"P{p}");
                }
                return list.Count > 0 ? string.Join(", ", list) : "PING";
            }
        }

        public string PortSummary => OpenPorts.Count > 0 ? string.Join(", ", OpenPorts) : "-";
    }

    public class ScanOptions
    {
        public int PingTimeoutMs { get; set; } = 500;
        public int ServiceTimeoutMs { get; set; } = 500;
        public bool UseArpCache { get; set; } = true;
        public bool ResolveHostnames { get; set; } = true;
        public bool OnlyResponsive { get; set; } = true;
        public List<int> TargetPorts { get; set; } = new() { 3389, 22 };
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
        /// Retrieves all active, non-loopback, non-APIPA (169.254.x) network adapters, prioritizing the adapter with an active gateway.
        /// </summary>
        public static List<NetworkAdapterInfo> GetActiveAdapters()
        {
            var list = new List<NetworkAdapterInfo>();
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
                            string ipStr = addr.Address.ToString();
                            // Filter out 169.254.x.x link-local APIPA addresses
                            if (ipStr.StartsWith("169.254.", StringComparison.OrdinalIgnoreCase))
                                continue;

                            byte[] ipBytes = addr.Address.GetAddressBytes();
                            byte[] maskBytes = addr.IPv4Mask?.GetAddressBytes() ?? new byte[] { 255, 255, 255, 0 };

                            int prefix = 0;
                            for (int i = 0; i < 4; i++)
                            {
                                for (int b = 7; b >= 0; b--)
                                {
                                    if ((maskBytes[i] & (1 << b)) != 0) prefix++;
                                }
                            }

                            uint ipVal = BitConverter.ToUInt32(ipBytes.Reverse().ToArray(), 0);
                            uint maskVal = BitConverter.ToUInt32(maskBytes.Reverse().ToArray(), 0);
                            uint network = ipVal & maskVal;
                            uint broadcast = network | ~maskVal;

                            string startIp = new IPAddress(BitConverter.GetBytes(network + 1).Reverse().ToArray()).ToString();
                            string endIp = new IPAddress(BitConverter.GetBytes(broadcast - 1).Reverse().ToArray()).ToString();

                            string gw = ipProps.GatewayAddresses
                                .FirstOrDefault(g => g.Address.AddressFamily == AddressFamily.InterNetwork && !g.Address.ToString().StartsWith("0.0.0.0"))
                                ?.Address.ToString() ?? "";

                            list.Add(new NetworkAdapterInfo
                            {
                                Name = ni.Name,
                                Description = ni.Description,
                                IpAddress = ipStr,
                                SubnetMask = new IPAddress(maskBytes).ToString(),
                                PrefixLength = prefix > 0 ? prefix : 24,
                                GatewayIp = gw,
                                StartIp = startIp,
                                EndIp = endIp
                            });
                        }
                    }
                }
            }
            catch { }

            // Prioritize active internet/gateway adapter (e.g. Wi-Fi with default gateway)
            return list.OrderByDescending(a => a.HasGateway)
                       .ThenByDescending(a => a.IpAddress.StartsWith("192.168.0.") || a.IpAddress.StartsWith("192.168.1."))
                       .ThenBy(a => a.Name)
                       .ToList();
        }

        /// <summary>
        /// Reads local ARP cache table to discover already known active LAN hosts instantly.
        /// </summary>
        public static HashSet<string> GetArpTableIps()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                var psi = new ProcessStartInfo("arp", "-a")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(1000);
                    var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var line in lines)
                    {
                        var parts = line.Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2 && IPAddress.TryParse(parts[0], out var ip))
                        {
                            string ipStr = ip.ToString();
                            if (!ipStr.StartsWith("224.") && !ipStr.StartsWith("239.") && !ipStr.EndsWith(".255") && !ipStr.StartsWith("169.254."))
                            {
                                set.Add(ipStr);
                            }
                        }
                    }
                }
            }
            catch { }
            return set;
        }

        /// <summary>
        /// Generates a list of IP addresses from Start IP and End IP.
        /// </summary>
        public static List<string> GenerateIpRange(string startIpStr, string endIpStr)
        {
            var result = new List<string>();
            if (!IPAddress.TryParse(startIpStr, out var startIp) || !IPAddress.TryParse(endIpStr, out var endIp))
                return result;

            uint start = BitConverter.ToUInt32(startIp.GetAddressBytes().Reverse().ToArray(), 0);
            uint end = BitConverter.ToUInt32(endIp.GetAddressBytes().Reverse().ToArray(), 0);

            if (end < start)
            {
                var tmp = start;
                start = end;
                end = tmp;
            }

            // Cap at 1024 addresses per single batch for safety
            for (uint i = start; i <= end && result.Count < 1024; i++)
            {
                byte[] bytes = BitConverter.GetBytes(i).Reverse().ToArray();
                result.Add(new IPAddress(bytes).ToString());
            }

            return result;
        }

        /// <summary>
        /// Scans a range of IP addresses concurrently.
        /// </summary>
        public static async Task ScanNetworkAsync(
            List<string> ipList,
            ScanOptions options,
            string interfaceName,
            IProgress<ScanProgressReport> progress,
            CancellationToken ct)
        {
            if (ipList == null || ipList.Count == 0) return;

            var arpSet = options.UseArpCache ? GetArpTableIps() : new HashSet<string>();

            int total = ipList.Count;
            int scanned = 0;
            using var semaphore = new SemaphoreSlim(48); // 48 concurrent socket/ping workers

            var tasks = ipList.Select(async ip => {
                await semaphore.WaitAsync(ct);
                try
                {
                    if (ct.IsCancellationRequested) return;

                    bool isInArp = arpSet.Contains(ip);
                    var device = await ProbeHostAsync(ip, options, interfaceName, isInArp, ct);

                    int count = Interlocked.Increment(ref scanned);
                    progress?.Report(new ScanProgressReport
                    {
                        ScannedCount = count,
                        TotalCount = total,
                        CurrentIp = ip,
                        DiscoveredDevice = device
                    });
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
        }

        private static async Task<DiscoveredDevice?> ProbeHostAsync(
            string ip,
            ScanOptions options,
            string interfaceName,
            bool isInArp,
            CancellationToken ct)
        {
            long latency = 0;
            bool isAlive = isInArp;

            // 1. Quick ICMP Ping if not in ARP table
            if (!isAlive)
            {
                try
                {
                    using var ping = new Ping();
                    var reply = await ping.SendPingAsync(ip, options.PingTimeoutMs);
                    if (reply.Status == IPStatus.Success)
                    {
                        isAlive = true;
                        latency = reply.RoundtripTime;
                    }
                }
                catch { }
            }

            // 2. Probe Target Ports
            var openPorts = new List<int>();
            if (options.TargetPorts != null && options.TargetPorts.Count > 0)
            {
                var portTasks = options.TargetPorts.Select(async port => {
                    var sw = Stopwatch.StartNew();
                    try
                    {
                        using var tcp = new TcpClient();
                        using var reg = ct.Register(() => tcp.Close());
                        var connectTask = tcp.ConnectAsync(ip, port);
                        var delayTask = Task.Delay(options.ServiceTimeoutMs, ct);

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
                        isAlive = true;
                        if (latency == 0 || r.Latency < latency)
                            latency = r.Latency;
                    }
                }
            }

            if (!isAlive) return null;
            if (options.OnlyResponsive && openPorts.Count == 0 && !isInArp && latency == 0) return null;

            // 3. Resolve Hostname
            string hostName = string.Empty;
            if (options.ResolveHostnames)
            {
                try
                {
                    var dnsTask = Dns.GetHostEntryAsync(ip);
                    var timeoutTask = Task.Delay(600, ct);
                    var comp = await Task.WhenAny(dnsTask, timeoutTask);
                    if (comp == dnsTask && dnsTask.IsCompletedSuccessfully)
                    {
                        hostName = dnsTask.Result.HostName;
                    }
                }
                catch { }
            }

            if (string.IsNullOrWhiteSpace(hostName))
                hostName = ip;

            ConnectionProtocol proto = ConnectionProtocol.RDP;
            if (openPorts.Contains(3389)) proto = ConnectionProtocol.RDP;
            else if (openPorts.Contains(22)) proto = ConnectionProtocol.SSH;

            return new DiscoveredDevice
            {
                InterfaceName = interfaceName,
                IpAddress = ip,
                HostName = hostName,
                OpenPorts = openPorts,
                LatencyMs = latency > 0 ? latency : 5,
                DetectedProtocol = proto,
                IsSelected = true
            };
        }
    }
}
