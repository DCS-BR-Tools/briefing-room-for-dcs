using System;
using System.Globalization;
using System.Windows.Forms;

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
                IronPdf.Installation.Initialize();
                Application.Run(new BriefingRoomBlazorWrapper());
            }
            catch (Exception ex)
            {
                ExceptionHandler.ShowException(ex);
            }
        }
    }
}

