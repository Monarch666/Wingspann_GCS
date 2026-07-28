using log4net;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Reflection;
using System.Windows.Forms;
using System.Xml;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [ComVisible(true)]
    public partial class ConfigSimplePids : MyUserControl, IActivate
    {
        internal static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private WebBrowser webBrowser;

        public ConfigSimplePids()
        {
            InitializeComponent();
        }

        private void ConfigSimplePids_Load(object sender, EventArgs e)
        {
            // Handled by Activate() call from host framework
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

                string htmlPath = Path.Combine(Settings.GetRunningDirectory(), "tuning_manager.html");
                if (!File.Exists(htmlPath))
                {
                    string sourcePath = Path.Combine(Application.StartupPath, "..", "..", "tuning_manager.html");
                    if (File.Exists(sourcePath))
                    {
                        try
                        {
                            File.Copy(sourcePath, htmlPath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to copy tuning_manager.html: " + ex.Message);
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
                webBrowser.Document.InvokeScript("onParamsLoaded", new object[] { GetTuningParams() });
            }
        }

        // --- BASIC TUNING BRIDGE METHODS ---

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

        public string GetTuningParams()
        {
            try
            {
                bool connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen;
                var paramCache = MainV2.comPort.MAV.param;

                double atcInputTc = paramCache.ContainsKey("ATC_INPUT_TC") ? (double)paramCache["ATC_INPUT_TC"].Value : 0.15;
                double atcRatRllP = paramCache.ContainsKey("ATC_RAT_RLL_P") ? (double)paramCache["ATC_RAT_RLL_P"].Value : 0.135;
                double atcRatPitP = paramCache.ContainsKey("ATC_RAT_PIT_P") ? (double)paramCache["ATC_RAT_PIT_P"].Value : 0.135;
                double atcRatYawP = paramCache.ContainsKey("ATC_RAT_YAW_P") ? (double)paramCache["ATC_RAT_YAW_P"].Value : 0.20;

                var data = new
                {
                    connected = connected,
                    ATC_INPUT_TC = atcInputTc,
                    ATC_RAT_RLL_P = atcRatRllP,
                    ATC_RAT_PIT_P = atcRatPitP,
                    ATC_RAT_YAW_P = atcRatYawP,
                    vehicle = MainV2.comPort.MAV.cs.firmware.ToString()
                };

                return JsonConvert.SerializeObject(data);
            }
            catch (Exception ex)
            {
                return "{}";
            }
        }

        public bool SaveTuningParams(string paramsJson)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, double>>(paramsJson);
                if (dict == null) return false;

                foreach (var kvp in dict)
                {
                    string name = kvp.Key;
                    float value = (float)kvp.Value;

                    MainV2.comPort.setParam(name, value);
                }

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public bool BackupParams(string paramsJson)
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.Filter = "JSON Profile (*.json)|*.json";
                    sfd.FileName = "tuning_profile_backup.json";
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllText(sfd.FileName, paramsJson);
                        return true;
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public string ImportProfile()
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Filter = "JSON Profile (*.json)|*.json";
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        return File.ReadAllText(ofd.FileName);
                    }
                }
                return "";
            }
            catch (Exception ex)
            {
                return "";
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