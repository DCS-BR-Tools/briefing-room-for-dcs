using System.Windows.Forms;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.WebView.WindowsForms;
using Microsoft.Extensions.DependencyInjection;

namespace BriefingRoom4DCS.GUI.Desktop
{
    public partial class BriefingRoomBlazorWrapper : Form
    {
        public BriefingRoomBlazorWrapper()
        {
            InitializeComponent();

            Text = $"BriefingRoom {BriefingRoom.VERSION} for DCS World";

            var serviceCollection = new ServiceCollection();
            serviceCollection.AddBlazorWebView();
            serviceCollection.AddWindowsFormsBlazorWebView();
            serviceCollection.AddBlazoredLocalStorage();
            serviceCollection.AddScoped<BriefingRoom>();
            var blazor = new BlazorWebView()
            {
                Dock = DockStyle.Fill,
                HostPage = "wwwroot/index.html",
                Services = serviceCollection.BuildServiceProvider()
            };
            blazor.RootComponents.Add<App>("#app");
            Controls.Add(blazor);
        }

    }
}
