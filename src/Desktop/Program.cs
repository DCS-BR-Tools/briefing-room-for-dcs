using System;
using System.Globalization;
using System.Windows.Forms;
using BriefingRoom4DCS.Generator;

namespace BriefingRoom4DCS.GUI.Desktop
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                Application.SetHighDpiMode(HighDpiMode.SystemAware);
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;
                CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InstalledUICulture;
                // Browser initializes lazily on first image generation
                Application.Run(new BriefingRoomBlazorWrapper());
            }
            catch (Exception ex)
            {
                ExceptionHandler.ShowException(ex);
            }
            finally
            {
                Imagery.ShutdownAsync().GetAwaiter().GetResult();
            }
        }
    }
}

