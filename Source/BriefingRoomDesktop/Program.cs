using System;
using System.Windows.Forms;

namespace BriefingRoomDesktop
{
    static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.SetHighDpiMode(HighDpiMode.SystemAware);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                Application.Run(new BriefingRoomBlazorWrapper());
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        internal static void HandleException(Exception ex)
        {
            string LF = Environment.NewLine + Environment.NewLine;
            string title = $"FUBAR... I got a crash at {DateTime.Now}";            
            string infos = $"Please take a screenshot of this message\n\r\n\r" +
                        $"Message : {LF}{ex.Message}{LF}" +
                        $"Source : {LF}{ex.Source}{LF}" +
                        $"Stack : {LF}{ex.StackTrace}{LF}" +
                        $"InnerException : {ex.InnerException}";

            MessageBox.Show(infos, title, MessageBoxButtons.OK, MessageBoxIcon.Error); // Do logging of exception details
        }
    }

}
