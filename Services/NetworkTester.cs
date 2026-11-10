using System;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace FreeRdpWrapper.Services
{
    public class NetworkProbeResult
    {
        public bool IsPingSuccessful { get; set; }
        public long PingRoundtripMs { get; set; }
        public bool IsPortOpen { get; set; }
        public int Port { get; set; }
        public string ProtocolName { get; set; } = "RDP";
        public long PortHandshakeMs { get; set; }
        public string ErrorMessage { get; set; } = string.Empty;

        public string SummaryText
        {
            get
            {
                string protoLabel = !string.IsNullOrEmpty(ProtocolName) ? $" ({ProtocolName})" : "";
                if (IsPortOpen)
                    return $"Port {Port}{protoLabel} OPEN ({PortHandshakeMs} ms)" + (IsPingSuccessful ? $", Ping {PingRoundtripMs} ms" : "");
                if (IsPingSuccessful)
                    return $"Host reachable (Ping {PingRoundtripMs} ms), but Port {Port}{protoLabel} CLOSED";
                return string.IsNullOrEmpty(ErrorMessage) ? $"Unreachable (Port {Port}{protoLabel} closed)" : $"Failed: {ErrorMessage}";
            }
        }
    }

    public static class NetworkTester
    {
        public static async Task<NetworkProbeResult> ProbeConnectionAsync(FreeRdpWrapper.Models.SavedConnection connection, int timeoutMs = 2000)
        {
            int defaultPort = connection.Protocol == FreeRdpWrapper.Models.ConnectionProtocol.SSH ? 22 : 3389;
            var res = await ProbeHostAsync(connection.Host, defaultPort, timeoutMs);
            res.ProtocolName = connection.Protocol == FreeRdpWrapper.Models.ConnectionProtocol.SSH ? "SSH" : "RDP";
            return res;
        }

        public static async Task<NetworkProbeResult> ProbeHostAsync(string hostWithPort, int defaultPort = 3389, int timeoutMs = 2000)
        {
            string protoName = defaultPort == 22 ? "SSH" : (defaultPort == 3389 ? "RDP" : $"Port {defaultPort}");
            var result = new NetworkProbeResult { Port = defaultPort, ProtocolName = protoName };

            string host = hostWithPort.Trim();
            if (host.Contains(':'))
            {
                var parts = host.Split(':');
                host = parts[0];
                if (int.TryParse(parts[1], out int p))
                    result.Port = p;
            }

            if (string.IsNullOrWhiteSpace(host))
            {
                result.ErrorMessage = "Empty host specified.";
                return result;
            }

            // 1. TCP Port Handshake Check (Most reliable for RDP/SSH even when ICMP is blocked by firewalls)
            var sw = System.Diagnostics.Stopwatch.StartNew();
            try
            {
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(host, result.Port);
                var timeoutTask = Task.Delay(timeoutMs);

                var completed = await Task.WhenAny(connectTask, timeoutTask);
                sw.Stop();

                if (completed == connectTask && tcpClient.Connected)
                {
                    result.IsPortOpen = true;
                    result.PortHandshakeMs = sw.ElapsedMilliseconds;
                }
                else
                {
                    result.IsPortOpen = false;
                    result.ErrorMessage = "Connection timed out (Port closed or firewalled).";
                }
            }
            catch (Exception ex)
            {
                sw.Stop();
                result.IsPortOpen = false;
                result.ErrorMessage = ex.Message;
            }

            // 2. ICMP Ping Check in background
            try
            {
                using var ping = new Ping();
                var reply = await ping.SendPingAsync(host, timeoutMs);
                if (reply.Status == IPStatus.Success)
                {
                    result.IsPingSuccessful = true;
                    result.PingRoundtripMs = reply.RoundtripTime;
                }
            }
            catch
            {
                result.IsPingSuccessful = false;
            }

            return result;
        }
    }
}

