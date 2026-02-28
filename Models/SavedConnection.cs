using System;

namespace FreeRdpWrapper.Models
{
    public class SavedConnection : RdpConfig
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Group { get; set; } = "Default";
    }
}
