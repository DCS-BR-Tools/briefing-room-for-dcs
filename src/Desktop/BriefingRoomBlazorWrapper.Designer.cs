using System.Drawing;

namespace BriefingRoom4DCS.GUI.Desktop
{
    partial class BriefingRoomBlazorWrapper
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1800, 1080);
            this.Text = "BriefingRoom";
            this.Icon = new Icon("Resources/icon.ico");
        }

        #endregion
    }
}

