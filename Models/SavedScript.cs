using System;

namespace FreeRdpWrapper.Models
{
    public class SavedScript
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
    }
}
