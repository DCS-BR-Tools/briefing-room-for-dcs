using System;
using System.Windows.Forms;

namespace BriefingRoom4DCS.GUI.Desktop
{
    internal static class ExceptionHandler
    {
        internal static void ShowException(Exception ex)
        {
            string nl = Environment.NewLine;
            string separator = new string('-', 50);
            
            string infos = $"BriefingRoom Crash Report{nl}" +
                          $"Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}{nl}" +
                          $"Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}{nl}" +
                          $"{separator}{nl}{nl}" +
                          $"ERROR: {ex.GetType().Name}{nl}" +
                          $"{ex.Message}{nl}{nl}" +
                          $"{separator}{nl}" +
                          $"SOURCE:{nl}{ex.Source}{nl}{nl}" +
                          $"{separator}{nl}" +
                          $"STACK TRACE:{nl}{ex.StackTrace}{nl}";

            if (ex.InnerException != null)
            {
                infos += $"{nl}{separator}{nl}" +
                        $"INNER EXCEPTION: {ex.InnerException.GetType().Name}{nl}" +
                        $"{ex.InnerException.Message}{nl}" +
                        $"{ex.InnerException.StackTrace}";
            }

            using var form = new Form
            {
                Text = "BriefingRoom - Application Error",
                Width = 700,
                Height = 500,
                StartPosition = FormStartPosition.CenterScreen,
                MinimizeBox = false,
                MaximizeBox = true,
                ShowInTaskbar = true
            };

            var headerLabel = new Label
            {
                Text = "An unexpected error occurred. Please copy this information and report it.",
                Dock = DockStyle.Top,
                Height = 30,
                Padding = new Padding(5),
                BackColor = System.Drawing.Color.FromArgb(220, 53, 69),
                ForeColor = System.Drawing.Color.White,
                Font = new System.Drawing.Font("Segoe UI", 10, System.Drawing.FontStyle.Bold)
            };

            var textBox = new TextBox
            {
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                Dock = DockStyle.Fill,
                Text = infos,
                Font = new System.Drawing.Font("Consolas", 9),
                BackColor = System.Drawing.Color.White,
                WordWrap = false
            };

            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 45,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(5),
                BackColor = System.Drawing.Color.FromArgb(240, 240, 240)
            };

            var closeButton = new Button 
            { 
                Text = "Close", 
                Width = 80, 
                Height = 30,
                Margin = new Padding(5)
            };
            closeButton.Click += (s, e) => form.Close();

            var copyButton = new Button 
            { 
                Text = "Copy to Clipboard", 
                Width = 130, 
                Height = 30,
                Margin = new Padding(5)
            };
            copyButton.Click += (s, e) =>
            {
                Clipboard.SetText(infos);
                copyButton.Text = "Copied!";
                copyButton.Enabled = false;
            };

            buttonPanel.Controls.Add(closeButton);
            buttonPanel.Controls.Add(copyButton);
            form.Controls.Add(textBox);
            form.Controls.Add(headerLabel);
            form.Controls.Add(buttonPanel);
            form.ShowDialog();
        }
    }
}
