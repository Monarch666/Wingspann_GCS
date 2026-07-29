using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Utilities;
using System;
using System.Collections;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [ComVisible(true)]
    public partial class ConfigArducopter : MyUserControl, IActivate
    {
        private WebBrowser webBrowser;

        public ConfigArducopter()
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

                string htmlPath = Path.Combine(Settings.GetRunningDirectory(), "extended_tuning.html");
                if (!File.Exists(htmlPath))
                {
                    string sourcePath = Path.Combine(Application.StartupPath, "..", "..", "extended_tuning.html");
                    if (File.Exists(sourcePath))
                    {
                        try
                        {
                            File.Copy(sourcePath, htmlPath, true);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Failed to copy extended_tuning.html: " + ex.Message);
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
                webBrowser.Document.InvokeScript("onParamsLoaded", new object[] { GetExtendedParams() });
            }
        }

        // --- EXTENDED TUNING FALLBACK BRIDGES ---

        private static readonly Dictionary<string, string[]> FallbackMap = new Dictionary<string, string[]>()
        {
            { "STB_RLL_P", new[] {"STB_RLL_P", "ATC_ANG_RLL_P", "Q_A_ANG_RLL_P"} },
            { "STB_PIT_P", new[] {"STB_PIT_P", "ATC_ANG_PIT_P", "Q_A_ANG_PIT_P"} },
            { "STB_YAW_P", new[] {"STB_YAW_P", "ATC_ANG_YAW_P", "Q_A_ANG_YAW_P"} },
            { "ATC_ACCEL_R_MAX", new[] {"ATC_ACCEL_R_MAX", "Q_A_ACCEL_R_MAX", "ATC_ACC_R_MAX", "Q_A_ACC_R_MAX"} },
            { "ATC_ACCEL_P_MAX", new[] {"ATC_ACCEL_P_MAX", "Q_A_ACCEL_P_MAX", "ATC_ACC_P_MAX", "Q_A_ACC_P_MAX"} },
            { "ATC_ACCEL_Y_MAX", new[] {"ATC_ACCEL_Y_MAX", "Q_A_ACCEL_Y_MAX", "ATC_ACC_Y_MAX", "Q_A_ACC_Y_MAX"} },
            { "ATC_INPUT_TC", new[] {"ATC_INPUT_TC", "Q_A_INPUT_TC"} },

            { "RATE_RLL_P", new[] { "RATE_RLL_P", "ATC_RAT_RLL_P", "Q_A_RAT_RLL_P" } },
            { "RATE_RLL_I", new[] { "RATE_RLL_I", "ATC_RAT_RLL_I", "Q_A_RAT_RLL_I" } },
            { "RATE_RLL_D", new[] {"RATE_RLL_D", "ATC_RAT_RLL_D", "Q_A_RAT_RLL_D"} },
            { "RATE_RLL_IMAX", new[] {"ATC_RAT_RLL_IMAX", "Q_A_RAT_RLL_IMAX", "RATE_RLL_IMAX"} },
            { "RATE_RLL_FILT", new[] {"RATE_RLL_FILT", "ATC_RAT_RLL_FILT", "ATC_RAT_RLL_FLTE", "Q_A_RAT_RLL_FLTE"} },
            { "ATC_RAT_RLL_FLTD", new[] { "ATC_RAT_RLL_FLTD", "Q_A_RAT_RLL_FLTD" } },
            { "ATC_RAT_RLL_FLTT", new[] { "ATC_RAT_RLL_FLTT", "Q_A_RAT_RLL_FLTT" } },

            { "RATE_PIT_P", new[] { "RATE_PIT_P", "ATC_RAT_PIT_P", "Q_A_RAT_PIT_P" } },
            { "RATE_PIT_I", new[] { "RATE_PIT_I", "ATC_RAT_PIT_I", "Q_A_RAT_PIT_I" } },
            { "RATE_PIT_D", new[] {"RATE_PIT_D", "ATC_RAT_PIT_D", "Q_A_RAT_PIT_D"} },
            { "RATE_PIT_IMAX", new[] {"ATC_RAT_PIT_IMAX", "Q_A_RAT_PIT_IMAX", "RATE_PIT_IMAX"} },
            { "RATE_PIT_FILT", new[] {"RATE_PIT_FILT", "ATC_RAT_PIT_FILT", "ATC_RAT_PIT_FLTE", "Q_A_RAT_PIT_FLTE"} },
            { "ATC_RAT_PIT_FLTD", new[] { "ATC_RAT_PIT_FLTD", "Q_A_RAT_PIT_FLTD" } },
            { "ATC_RAT_PIT_FLTT", new[] { "ATC_RAT_PIT_FLTT", "Q_A_RAT_PIT_FLTT" } },

            { "RATE_YAW_P", new[] { "RATE_YAW_P", "ATC_RAT_YAW_P", "Q_A_RAT_YAW_P" } },
            { "RATE_YAW_I", new[] { "RATE_YAW_I", "ATC_RAT_YAW_I", "Q_A_RAT_YAW_I" } },
            { "RATE_YAW_D", new[] {"RATE_YAW_D", "ATC_RAT_YAW_D", "Q_A_RAT_YAW_D"} },
            { "RATE_YAW_IMAX", new[] {"ATC_RAT_YAW_IMAX", "Q_A_RAT_YAW_IMAX", "RATE_YAW_IMAX"} },
            { "RATE_YAW_FILT", new[] {"RATE_YAW_FILT", "ATC_RAT_YAW_FILT", "ATC_RAT_YAW_FLTE", "Q_A_RAT_YAW_FLTE"} },
            { "ATC_RAT_YAW_FLTD", new[] { "ATC_RAT_YAW_FLTD", "Q_A_RAT_YAW_FLTD" } },
            { "ATC_RAT_YAW_FLTT", new[] { "ATC_RAT_YAW_FLTT", "Q_A_RAT_YAW_FLTT" } },

            { "PSC_VELXY_P", new[] {"VEL_XY_P", "PSC_VELXY_P", "Q_P_VELXY_P", "PSC_NE_VEL_P", "Q_P_NE_VEL_P"} },
            { "PSC_VELXY_I", new[] {"VEL_XY_I", "PSC_VELXY_I", "Q_P_VELXY_I", "PSC_NE_VEL_I", "Q_P_NE_VEL_I"} },
            { "PSC_VELXY_D", new[] {"LOITER_LAT_D", "PSC_VELXY_D", "Q_P_VELXY_D", "PSC_NE_VEL_D", "Q_P_NE_VEL_D"} },
            { "PSC_VELXY_IMAX", new[] {"VEL_XY_IMAX", "PSC_VELXY_IMAX", "Q_P_VELXY_IMAX", "PSC_NE_VEL_IMAX", "Q_P_NE_VEL_IMAX"} },
            { "PSC_POSXY_P", new[] {"HLD_LAT_P", "POS_XY_P", "PSC_POSXY_P", "Q_P_POSXY_P"} },

            { "PSC_ACCZ_P", new[] { "THR_ACCEL_P", "ACCEL_Z_P", "PSC_ACCZ_P", "Q_P_ACCZ_P" } },
            { "PSC_ACCZ_I", new[] { "THR_ACCEL_I", "ACCEL_Z_I", "PSC_ACCZ_I", "Q_P_ACCZ_I" } },
            { "PSC_ACCZ_D", new[] {"THR_ACCEL_D", "ACCEL_Z_D", "PSC_ACCZ_D", "Q_P_ACCZ_D"}},
            { "PSC_ACCZ_IMAX", new[] {"THR_ACCEL_IMAX", "ACCEL_Z_IMAX", "PSC_ACCZ_IMAX", "Q_P_ACCZ_IMAX"}},
            { "PSC_VELZ_P", new[] {"THR_RATE_P", "VEL_Z_P", "PSC_VELZ_P", "Q_P_VELZ_P"}},
            { "PSC_POSZ_P", new[] {"THR_ALT_P", "POS_Z_P", "PSC_POSZ_P", "Q_P_POSZ_P"}},

            { "WPNAV_SPEED", new[] {"WPNAV_SPEED", "Q_WP_SPEED", "WP_SPD"}},
            { "WPNAV_RADIUS", new[] {"WPNAV_RADIUS", "Q_WP_RADIUS", "WP_RADIUS_M"}},
            { "WPNAV_SPEED_UP", new[] {"WPNAV_SPEED_UP", "Q_WP_SPEED_UP", "WP_SPD_UP"}},
            { "WPNAV_SPEED_DN", new[] {"WPNAV_SPEED_DN", "Q_WP_SPEED_DN", "WP_SPD_DN"}},
            { "WPNAV_LOIT_SPEED", new[] {"WPNAV_LOIT_SPEED", "LOIT_SPEED", "Q_LOIT_SPEED", "LOIT_SPEED_MS"}},

            { "INS_GYRO_FILTER", new[] { "INS_GYRO_FILTER" } },
            { "INS_ACCEL_FILTER", new[] { "INS_ACCEL_FILTER" } },
            { "INS_LOG_BAT_MASK", new[] { "INS_LOG_BAT_MASK" } },
            { "INS_LOG_BAT_OPT", new[] { "INS_LOG_BAT_OPT" } },

            { "INS_NOTCH_ENABLE", new[] { "INS_NOTCH_ENABLE" } },
            { "INS_NOTCH_FREQ", new[] { "INS_NOTCH_FREQ" } },
            { "INS_NOTCH_BW", new[] { "INS_NOTCH_BW" } },
            { "INS_NOTCH_ATT", new[] { "INS_NOTCH_ATT" } },

            { "INS_HNTCH_ENABLE", new[] { "INS_HNTCH_ENABLE" } },
            { "INS_HNTCH_MODE", new[] { "INS_HNTCH_MODE" } },
            { "INS_HNTCH_REF", new[] { "INS_HNTCH_REF" } },
            { "INS_HNTCH_FREQ", new[] { "INS_HNTCH_FREQ" } },
            { "INS_HNTCH_ATT", new[] { "INS_HNTCH_ATT" } },
            { "INS_HNTCH_BW", new[] { "INS_HNTCH_BW" } },
            { "INS_HNTCH_OPTS", new[] { "INS_HNTCH_OPTS" } },
            { "INS_HNTCH_HMNCS", new[] { "INS_HNTCH_HMNCS" } }
        };

        private double GetParamValue(string[] names, double def = 0.0)
        {
            foreach (var name in names)
            {
                if (MainV2.comPort.MAV.param.ContainsKey(name))
                {
                    return (double)MainV2.comPort.MAV.param[name].Value;
                }
            }
            return def;
        }

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

        public string GetExtendedParams()
        {
            try
            {
                bool connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen;
                var result = new Dictionary<string, double>();
                result["connected"] = connected ? 1.0 : 0.0;

                foreach (var pair in FallbackMap)
                {
                    result[pair.Key] = GetParamValue(pair.Value);
                }

                return JsonConvert.SerializeObject(result);
            }
            catch (Exception ex)
            {
                return "{}";
            }
        }

        public bool SaveExtendedParams(string paramsJson)
        {
            try
            {
                var dict = JsonConvert.DeserializeObject<Dictionary<string, double>>(paramsJson);
                if (dict == null) return false;

                foreach (var kvp in dict)
                {
                    string genericName = kvp.Key;
                    float value = (float)kvp.Value;

                    if (FallbackMap.ContainsKey(genericName))
                    {
                        var fallbacks = FallbackMap[genericName];
                        bool written = false;
                        foreach (var name in fallbacks)
                        {
                            if (MainV2.comPort.MAV.param.ContainsKey(name))
                            {
                                MainV2.comPort.setParam(name, value);
                                written = true;
                                break;
                            }
                        }
                        if (!written && fallbacks.Length > 0)
                        {
                            MainV2.comPort.setParam(fallbacks[0], value);
                        }
                    }
                    else
                    {
                        MainV2.comPort.setParam(genericName, value);
                    }
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
                    sfd.FileName = "extended_tuning_profile.json";
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

        public void RerequestParams()
        {
            try
            {
                if (MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen)
                {
                    MainV2.comPort.getParamList();
                    TriggerReload();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error refreshing parameters list: " + ex.Message);
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

        // --- DESIGNER STUBS TO PREVENT COMPILER ERRORS ---
        public void EEPROM_View_float_TextChanged(object sender, EventArgs e) {}
        public void BUT_writePIDS_Click(object sender, EventArgs e) {}
        public void BUT_rerequestparams_Click(object sender, EventArgs e) {}
        public void BUT_refreshpart_Click(object sender, EventArgs e) {}
        public void numeric_ValueUpdated(object sender, EventArgs e) {}
        public void OnEnter_NumUpDown(object sender, EventArgs e) {}
    }
}