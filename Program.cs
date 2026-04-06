using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Security.Credentials.UI;

namespace FreeRdpWrapper;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();

        var settings = FreeRdpWrapper.Services.SettingsStore.LoadSettings();
        
        bool isSetup = string.IsNullOrEmpty(settings.MasterPasswordHash);
        
        using (var dialog = new FreeRdpWrapper.UI.MasterPasswordDialog(isSetup, settings.MasterPasswordHash))
        {
            if (dialog.ShowDialog() != DialogResult.OK)
            {
                // User cancelled or failed authentication
                return;
            }
        }

        Application.Run(new Form1());
    }    
}