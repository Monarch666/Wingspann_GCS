using log4net;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [ComVisible(true)]
    public partial class ConfigFriendlyParams : MyUserControl, IActivate
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private Dictionary<string, string> _params = new Dictionary<string, string>();
        private WebBrowser webBrowser;

        public string ParameterMode { get; set; } = ParameterMetaDataConstants.Standard;

        public ConfigFriendlyParams()
        {
            InitializeComponent();
            ParameterMode = ParameterMetaDataConstants.Standard;
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.S))
            {
                // Ctrl+S shortcut handler if needed
                return true;
            }
            return false;
        }

        private void SetBrowserFeatureControl()
        {
            try
            {
                var registryKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Internet Explorer\Main\FeatureControl\FEATURE_BROWSER_EMULATION", true);
                if (registryKey != null)
                {
                    string exeName = System.IO.Path.GetFileName(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName);
                    registryKey.SetValue(exeName, 11001, Microsoft.Win32.RegistryValueKind.DWord);
                    registryKey.Close();
                }
            }
            catch { }
        }

        public void Activate()
        {
            SetBrowserFeatureControl();

            if (webBrowser == null)
            {
                webBrowser = new WebBrowser
                {
                    Dock = DockStyle.Fill,
                    IsWebBrowserContextMenuEnabled = false,
                    WebBrowserShortcutsEnabled = false,
                    ObjectForScripting = this
                };

                this.Controls.Clear();
                this.Controls.Add(webBrowser);

                string htmlPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "user_params.html");
                webBrowser.Navigate(htmlPath);
            }
            else
            {
                try
                {
                    webBrowser.Document?.InvokeScript("refreshDirectory");
                }
                catch { }
            }
        }

        // --- JavaScript Bridge APIs ---

        public string GetSystemStatus()
        {
            var status = new
            {
                connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen,
                sysid = MainV2.comPort.sysidcurrent,
                compid = MainV2.comPort.compidcurrent
            };
            return JsonConvert.SerializeObject(status);
        }

        private void FilterParamList()
        {
            _params.Clear();
            var locker = new object();
            string firmware = MainV2.comPort.MAV.cs.firmware.ToString();
            
            Parallel.ForEach(MainV2.comPort.MAV.param.Keys, x =>
            {
                string keyStr = x.ToString();
                var displayName = ParameterMetaDataRepository.GetParameterMetaData(keyStr,
                    ParameterMetaDataConstants.DisplayName, firmware);
                var parameterMode = ParameterMetaDataRepository.GetParameterMetaData(keyStr,
                    ParameterMetaDataConstants.User, firmware);

                if (!string.IsNullOrEmpty(displayName) &&
                    ((!string.IsNullOrEmpty(parameterMode) && parameterMode == ParameterMode) ||
                     string.IsNullOrEmpty(parameterMode) && ParameterMode == ParameterMetaDataConstants.Advanced))
                {
                    lock (locker)
                        _params[keyStr] = displayName;
                }
            });
        }

        public string GetUserParams()
        {
            try
            {
                if (MainV2.comPort.BaseStream == null || !MainV2.comPort.BaseStream.IsOpen)
                    return "[]";

                FilterParamList();

                var paramList = new List<object>();
                string firmware = MainV2.comPort.MAV.cs.firmware.ToString();

                foreach (var x in _params)
                {
                    string paramName = x.Key;
                    string displayName = x.Value;

                    if (!MainV2.comPort.MAV.param.ContainsKey(paramName))
                        continue;

                    float currentValue = (float)MainV2.comPort.MAV.param[paramName].Value;
                    string description = ParameterMetaDataRepository.GetParameterMetaData(paramName, ParameterMetaDataConstants.Description, firmware);
                    string units = ParameterMetaDataRepository.GetParameterMetaData(paramName, ParameterMetaDataConstants.Units, firmware);
                    string range = ParameterMetaDataRepository.GetParameterMetaData(paramName, ParameterMetaDataConstants.Range, firmware);
                    string increment = ParameterMetaDataRepository.GetParameterMetaData(paramName, ParameterMetaDataConstants.Increment, firmware);
                    string valuesRaw = ParameterMetaDataRepository.GetParameterMetaData(paramName, ParameterMetaDataConstants.Values, firmware);

                    var options = new List<object>();
                    if (!string.IsNullOrEmpty(valuesRaw))
                    {
                        var parts = valuesRaw.Split(',');
                        foreach (var part in parts)
                        {
                            var subparts = part.Split(':');
                            if (subparts.Length >= 2)
                            {
                                options.Add(new { value = subparts[0].Trim(), label = subparts[1].Trim() });
                            }
                            else if (subparts.Length == 1)
                            {
                                options.Add(new { value = subparts[0].Trim(), label = subparts[0].Trim() });
                            }
                        }
                    }

                    paramList.Add(new
                    {
                        name = paramName,
                        displayName = displayName,
                        value = currentValue,
                        description = description,
                        units = units,
                        range = range,
                        increment = increment,
                        options = options
                    });
                }

                return JsonConvert.SerializeObject(paramList);
            }
            catch (Exception ex)
            {
                log.Error("GetUserParams error", ex);
                return "[]";
            }
        }

        public bool WriteChanges(string json)
        {
            try
            {
                var changes = JsonConvert.DeserializeObject<Dictionary<string, float>>(json);
                bool errorThrown = false;
                foreach (var entry in changes)
                {
                    try
                    {
                        MainV2.comPort.setParam(entry.Key, entry.Value);
                    }
                    catch (Exception ex)
                    {
                        log.Error("Failed to set param " + entry.Key, ex);
                        errorThrown = true;
                    }
                }
                return !errorThrown;
            }
            catch (Exception ex)
            {
                log.Error("WriteChanges error", ex);
                return false;
            }
        }

        public bool RefreshParams()
        {
            try
            {
                if (MainV2.comPort.BaseStream == null || !MainV2.comPort.BaseStream.IsOpen)
                    return false;

                MainV2.comPort.getParamList();
                return true;
            }
            catch (Exception ex)
            {
                log.Error("RefreshParams error", ex);
                return false;
            }
        }

        // --- Legacy Designer Stubs to ensure compilation ---

        private void BUT_rerequestparams_Click(object sender, EventArgs e) { }
        private void BUT_writePIDS_Click(object sender, EventArgs e) { }
        private void this_Resize(object sender, EventArgs e) { }
        private void BUT_Find_Click(object sender, EventArgs e) { }
    }
}