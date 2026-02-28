namespace FreeRdpWrapper.Models
{
    public class RdpConfig
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public string Domain { get; set; } = string.Empty;
        
        // Advanced Display & Exec
        public bool UseSdl { get; set; } = false; // wfreerdp is standard on Windows, sdlfreerdp is optional
        public bool IgnoreCert { get; set; } = true;
        public bool DynamicResolution { get; set; } = true;
        public bool EnableDpiScale { get; set; } = true;
        public string RemoteApp { get; set; } = string.Empty; // Alias, e.g. "||winword"
        
        // Security
        public bool EnableCredGuard { get; set; } = false;
        
        // RD Gateway
        public string GatewayHost { get; set; } = string.Empty;
        public string GatewayUser { get; set; } = string.Empty;
        public string GatewayPass { get; set; } = string.Empty;

        // Local Resources (Hardware Redirection)
        public bool EnableClipboard { get; set; } = true;
        public bool EnableSound { get; set; } = false;
        public bool EnableMicrophone { get; set; } = false;
        public bool MapDrive { get; set; } = false;
        public bool MapPrinter { get; set; } = false;
        public bool MapSmartcard { get; set; } = false;

        public string AdditionalFlags { get; set; } = string.Empty;
    }
}
