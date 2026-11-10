using System;

namespace FreeRdpWrapper.Models
{
    public class SavedConnection : RdpConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = "Default";
        public ConnectionProtocol Protocol { get; set; } = ConnectionProtocol.RDP;
        public SshClientEngine SshEngine { get; set; } = SshClientEngine.IntegratedTab;
    }

    public enum ConnectionProtocol
    {
        RDP,
        SSH
    }

    public enum SshClientEngine
    {
        IntegratedTab,
        PuTTY,
        OpenSshConsole
    }
}
