using System;
using System.Drawing;
using System.Windows.Forms;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;
using System.IO;
using System.Threading.Tasks;

namespace MissionPlanner.GCSViews
{
    public class WeatherDashboard : UserControl
    {
        private WebView2 webView;
        private Label lblLoading;
        private bool isWebViewInitialized = false;
        private Timer locationPoller;

        public WeatherDashboard()
        {
            InitializeComponent();
            if (!DesignMode)
            {
                _ = InitializeWebViewAsync();
            }
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.BackColor = Color.FromArgb(13, 17, 23); // Dark theme

            lblLoading = new Label();
            lblLoading.Text = "Loading Zoom Earth Engine...";
            lblLoading.ForeColor = Color.White;
            lblLoading.Font = new Font("Segoe UI", 14, FontStyle.Bold);
            lblLoading.AutoSize = false;
            lblLoading.Dock = DockStyle.Fill;
            lblLoading.TextAlign = ContentAlignment.MiddleCenter;
            this.Controls.Add(lblLoading);

            webView = new WebView2();
            webView.Dock = DockStyle.Fill;
            webView.Visible = false;
            
            this.Controls.Add(webView);

            this.Dock = DockStyle.Fill;
            this.ResumeLayout(false);

            locationPoller = new Timer();
            locationPoller.Interval = 2000;
            locationPoller.Tick += (s, e) =>
            {
                if (isWebViewInitialized && webView.CoreWebView2 != null && webView.Visible)
                {
                    if (MainV2.comPort.MAV.cs.lat != 0 && MainV2.comPort.MAV.cs.lng != 0)
                    {
                        string js = $"if (window.updateLocationFromGCS) window.updateLocationFromGCS({MainV2.comPort.MAV.cs.lat}, {MainV2.comPort.MAV.cs.lng});";
                        webView.ExecuteScriptAsync(js);
                    }
                }
            };
            locationPoller.Start();
        }

        private async Task InitializeWebViewAsync()
        {
            try
            {
                string userDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MissionPlanner", "WebView2Cache");
                var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
                
                await webView.EnsureCoreWebView2Async(env);
                
                string webAppFolder = @"E:\Projects\Weather App";
                
                if (Directory.Exists(webAppFolder))
                {
                    webView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                        "weatherapp.local", 
                        webAppFolder, 
                        CoreWebView2HostResourceAccessKind.Allow);

                    webView.Source = new Uri("http://weatherapp.local/index.html");
                }
                else
                {
                    lblLoading.Text = $"Error: Web app folder not found at {webAppFolder}";
                }

                webView.NavigationCompleted += (sender, args) =>
                {
                    if (args.IsSuccess)
                    {
                        lblLoading.Visible = false;
                        webView.Visible = true;
                        
                        // Pass current UAV coordinates if available
                        if (MainV2.comPort.MAV.cs.lat != 0 && MainV2.comPort.MAV.cs.lng != 0)
                        {
                            string js = $"if (window.updateLocationFromGCS) window.updateLocationFromGCS({MainV2.comPort.MAV.cs.lat}, {MainV2.comPort.MAV.cs.lng});";
                            webView.ExecuteScriptAsync(js);
                        }
                    }
                    else
                    {
                        lblLoading.Text = "Failed to load Zoom Earth Engine.";
                    }
                };
                
                isWebViewInitialized = true;
            }
            catch (Exception ex)
            {
                lblLoading.Text = "WebView2 Runtime is not installed.\nPlease install the Microsoft Edge WebView2 Runtime.";
            }
        }
    }
}
