using System;

namespace FreeRdpWrapper.Models
{
    public enum AppTheme
    {
        HighContrast, // Matrix World (Default)
        Dark,
        Light,
        SolarizedDark,
        Dracula
    }

    public static class ThemeSettings
    {
        public static AppTheme CurrentTheme { get; set; } = AppTheme.HighContrast;
    }
}
