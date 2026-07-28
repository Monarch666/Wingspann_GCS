using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System;
using System.Diagnostics;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [ComVisible(true)]
    public partial class ConfigAC_Fence : MyUserControl, IActivate
    {
        private WebBrowser webBrowser;

        public ConfigAC_Fence()
        {
            InitializeComponent();
        }

        public void Activate()
        {
            SetBrowserFeatureControl();

            if (webBrowser == null)
            {
                this.Controls.Clear();
                webBrowser = new WebBrowser();
                webBrowser.Dock = DockStyle.Fill;
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.ObjectForScripting = this;
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                this.Controls.Add(webBrowser);

                string htmlPath = Path.Combine(Settings.GetRunningDirectory(), "geofence_manager.html");
                if (!File.Exists(htmlPath))
                {
                    string sourcePath = Path.Combine(Application.StartupPath, "..", "..", "geofence_manager.html");
                    if (File.Exists(sourcePath))
                    {
                        try
                        {
                            File.Copy(sourcePath, htmlPath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to copy geofence_manager.html: " + ex.Message);
                        }
                    }
                }

                webBrowser.Navigate(htmlPath);
            }
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            TriggerReload();
        }

        private void TriggerReload()
        {
            if (webBrowser != null && webBrowser.ReadyState == WebBrowserReadyState.Complete)
            {
                webBrowser.Document.InvokeScript("onParamsLoaded", new object[] { GetFenceParams() });
            }
        }

        // --- GEOFENCE BRIDGE METHODS ---

        public string GetSystemStatus()
        {
            try
            {
                bool connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen;
                string firmware = connected ? MainV2.comPort.MAV.cs.firmware.ToString() : "OFFLINE";
                string port = connected ? MainV2.comPort.BaseStream.PortName : "";
                string baud = connected ? MainV2.comPort.BaseStream.BaudRate.ToString() : "";

                var status = new
                {
                    connected = connected,
                    firmware = firmware,
                    port = port,
                    baud = baud
                };
                return JsonConvert.SerializeObject(status);
            }
            catch (Exception ex)
            {
                return "{\"connected\":false,\"firmware\":\"OFFLINE\",\"port\":\"\",\"baud\":\"\"}";
            }
        }

        public string GetFenceParams()
        {
            try
            {
                bool connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen;
                var paramCache = MainV2.comPort.MAV.param;

                double fenceEnable = paramCache.ContainsKey("FENCE_ENABLE") ? (double)paramCache["FENCE_ENABLE"].Value : 0;
                double fenceType = paramCache.ContainsKey("FENCE_TYPE") ? (double)paramCache["FENCE_TYPE"].Value : 0;
                double fenceAction = paramCache.ContainsKey("FENCE_ACTION") ? (double)paramCache["FENCE_ACTION"].Value : 0;
                double fenceAltMax = paramCache.ContainsKey("FENCE_ALT_MAX") ? (double)paramCache["FENCE_ALT_MAX"].Value : 100;
                double fenceAltMin = paramCache.ContainsKey("FENCE_ALT_MIN") ? (double)paramCache["FENCE_ALT_MIN"].Value : -10;
                double fenceRadius = paramCache.ContainsKey("FENCE_RADIUS") ? (double)paramCache["FENCE_RADIUS"].Value : 300;

                string rtlAltName = paramCache.ContainsKey("RTL_ALT_M") ? "RTL_ALT_M" : "RTL_ALT";
                double rtlAlt = paramCache.ContainsKey(rtlAltName) ? (double)paramCache[rtlAltName].Value : 15;

                // Load Type Options
                var typeOptions = new System.Collections.Generic.List<object>();
                try
                {
                    var opts = ParameterMetaDataRepository.GetParameterOptionsInt("FENCE_TYPE", MainV2.comPort.MAV.cs.firmware.ToString());
                    if (opts != null)
                    {
                        foreach (var opt in opts)
                        {
                            typeOptions.Add(new { key = opt.Key, value = opt.Value });
                        }
                    }
                }
                catch { }

                if (typeOptions.Count == 0)
                {
                    // Fallback defaults
                    typeOptions.Add(new { key = 1, value = "Altitude Only" });
                    typeOptions.Add(new { key = 2, value = "Circle Only" });
                    typeOptions.Add(new { key = 3, value = "Circle & Altitude" });
                    typeOptions.Add(new { key = 4, value = "Polygon Only" });
                    typeOptions.Add(new { key = 7, value = "Circle, Polygon & Altitude" });
                }

                // Load Action Options
                var actionOptions = new System.Collections.Generic.List<object>();
                try
                {
                    var opts = ParameterMetaDataRepository.GetParameterOptionsInt("FENCE_ACTION", MainV2.comPort.MAV.cs.firmware.ToString());
                    if (opts != null)
                    {
                        foreach (var opt in opts)
                        {
                            actionOptions.Add(new { key = opt.Key, value = opt.Value });
                        }
                    }
                }
                catch { }

                if (actionOptions.Count == 0)
                {
                    // Fallback defaults
                    actionOptions.Add(new { key = 0, value = "Report Only" });
                    actionOptions.Add(new { key = 1, value = "RTL or Land" });
                    actionOptions.Add(new { key = 2, value = "Land Only" });
                    actionOptions.Add(new { key = 3, value = "Brake" });
                    actionOptions.Add(new { key = 4, value = "Smart RTL" });
                }

                var data = new
                {
                    connected = connected,
                    FENCE_ENABLE = fenceEnable,
                    FENCE_TYPE = fenceType,
                    FENCE_ACTION = fenceAction,
                    FENCE_ALT_MAX = fenceAltMax,
                    FENCE_ALT_MIN = fenceAltMin,
                    FENCE_RADIUS = fenceRadius,
                    RTL_ALT = rtlAlt,
                    RTL_ALT_NAME = rtlAltName,
                    typeOptions = typeOptions,
                    actionOptions = actionOptions
                };

                return JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                return "{}";
            }
        }

        public bool SaveFenceParams(string paramsJson)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<System.Collections.Generic.Dictionary<string, double>>(paramsJson);
                if (dict == null) return false;

                foreach (var kvp in dict)
                {
                    string name = kvp.Key;
                    float value = (float)kvp.Value;

                    // Write to Flight Controller
                    MainV2.comPort.setParam(name, value);
                }

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error saving geofence parameters: " + ex.Message);
                return false;
            }
        }

        private void SetBrowserFeatureControl()
        {
            try
            {
                string fileName = Path.GetFileName(Process.GetCurrentProcess().MainModule.FileName);
                using (var key = Microsoft.Win32.Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION"))
                {
                    if (key != null)
                    {
                        key.SetValue(fileName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to set FEATURE_BROWSER_EMULATION in registry: " + ex.Message);
            }
        }
    }
}