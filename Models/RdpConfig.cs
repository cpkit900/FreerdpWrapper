namespace FreeRdpWrapper.Models
{
    public enum CertSecurity
    {
        Ignore,
        Tofu,
        Deny
    }

    public class RdpConfig
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        
        // Advanced Display & Exec
        public bool UseSdl { get; set; } = false; // wfreerdp is standard on Windows, sdlfreerdp is optional
        public CertSecurity CertConfig { get; set; } = CertSecurity.Ignore;
        public bool DynamicResolution { get; set; } = true;
        public bool UseCustomResolution { get; set; } = false;
        public int ResolutionWidth { get; set; } = 1920;
        public int ResolutionHeight { get; set; } = 1080;
        public bool GamingMode { get; set; } = false;
        public bool EnableDpiScale { get; set; } = true;
        public string RemoteApp { get; set; } = string.Empty; // Alias, e.g. "||winword"
        public string SshKeyPath { get; set; } = string.Empty;
        public bool AdminSession { get; set; } = false;
        public bool AutoNetworkProfile { get; set; } = true;
        public bool EnableUsbRedirection { get; set; } = false;
        
        // Security
        public bool EnableCredGuard { get; set; } = false;
        // RD Gateway
        public bool EnableGateway { get; set; } = false;
        public string GatewayHost { get; set; } = string.Empty;
        public string GatewayDomain { get; set; } = string.Empty;
        public string GatewayUser { get; set; } = string.Empty;
        public string GatewayPass { get; set; } = string.Empty;

        // Local Resources (Hardware Redirection)
        public bool EnableClipboard { get; set; } = true;
        public bool EnableSound { get; set; } = false;
        public bool EnableMicrophone { get; set; } = false;
        public bool EnableCamera { get; set; } = false;
        public bool MapDrive { get; set; } = false;
        public bool MapPrinter { get; set; } = false;
        public bool MapSmartcard { get; set; } = false;

        // Session Reliability & Multi-Monitor
        public bool MultiMonitor { get; set; } = false;
        public bool AutoReconnect { get; set; } = true;

        public string AdditionalFlags { get; set; } = string.Empty;
    }
}
