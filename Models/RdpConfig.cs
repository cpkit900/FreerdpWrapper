namespace FreeRdpWrapper.Models
{
    public class RdpConfig
    {
        public string Host { get; set; } = string.Empty;
        public string User { get; set; } = string.Empty;
        public string Pass { get; set; } = string.Empty;
        public bool UseSdl { get; set; } = false; // wfreerdp is standard on Windows, sdlfreerdp is optional
        public bool IgnoreCert { get; set; } = true;
        public bool DynamicResolution { get; set; } = true;
        public string AdditionalFlags { get; set; } = string.Empty;
    }
}
