using System;

namespace FreeRdpWrapper.Models
{
    public enum AppTheme
    {
        Dark,
        Light,
        SolarizedDark,
        Dracula,
        HighContrast
    }

    public static class ThemeSettings
    {
        public static AppTheme CurrentTheme { get; set; } = AppTheme.Dark;
    }
}
