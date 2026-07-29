using log4net;
using MissionPlanner.Controls;
using MissionPlanner.Controls.BackstageView;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Radio;
using MissionPlanner.Utilities;
using System;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Drawing;

namespace MissionPlanner.GCSViews
{
    [ComVisible(true)]
    public partial class FirmwareSetup : MyUserControl, IActivate
    {
        internal static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string lastpagename = "";

        public FirmwareSetup()
        {
            InitializeComponent();
        }

        public bool isConnected
        {
            get { return MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isDisConnected
        {
            get { return MainV2.comPort.BaseStream == null || !MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isTracker
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == MissionPlanner.ArduPilot.Firmwares.ArduTracker; }
        }

        public bool isCopter
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == MissionPlanner.ArduPilot.Firmwares.ArduCopter2; }
        }

        public bool isCopter35plus
        {
            get { return MainV2.comPort.MAV.cs.version >= Version.Parse("3.5"); }
        }

        public bool isHeli
        {
            get { return isConnected && MainV2.comPort.MAV.aptype == MAVLink.MAV_TYPE.HELICOPTER; }
        }

        public bool isQuadPlane
        {
            get
            {
                return isConnected && isPlane &&
                       MainV2.comPort.MAV.param.ContainsKey("Q_ENABLE") &&
                       (MainV2.comPort.MAV.param["Q_ENABLE"].Value == 1.0);
            }
        }

        public bool isPlane
        {
            get
            {
                return isConnected &&
                       (MainV2.comPort.MAV.cs.firmware == MissionPlanner.ArduPilot.Firmwares.ArduPlane ||
                        MainV2.comPort.MAV.cs.firmware == MissionPlanner.ArduPilot.Firmwares.Ateryx);
            }
        }

        public bool isRover
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == MissionPlanner.ArduPilot.Firmwares.ArduRover; }
        }

        public bool gotAllParams
        {
            get
            {
                log.InfoFormat("TotalReceived {0} TotalReported {1}", MainV2.comPort.MAV.param.TotalReceived,
                    MainV2.comPort.MAV.param.TotalReported);
                if (MainV2.comPort.MAV.param.TotalReceived < MainV2.comPort.MAV.param.TotalReported)
                {
                    return false;
                }

                return true;
            }
        }

        public BackstageViewPage AddBackstageViewPage(Type userControl, string headerText, bool enabled = true,
            BackstageViewPage Parent = null, bool advanced = false)
        {
            try
            {
                if (enabled)
                    return backstageView.AddPage(userControl, headerText, Parent, advanced);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return null;
            }

            return null;
        }

        public void Activate()
        {
            FirmwareSetup_Load(null, null);
        }

        public void Deactivate()
        {
            // Nothing to do
        }

        private void FirmwareSetup_Load(object sender, EventArgs e)
        {
            backstageView.Clear();
            ResourceManager rm = new ResourceManager(typeof(InitialSetup));

            // 1. Install Firmware (runs custom HTML manager)
            AddBackstageViewPage(typeof(ConfigFirmwareInstall), "Install Firmware");

            // 2. Optional Hardware Menu Tab
            var opt = AddBackstageViewPage(typeof(ConfigOptional), rm.GetString("backstageViewPageopt.Text"));
            if (MainV2.DisplayConfiguration.displayRTKInject)
            {
                var rtcmStr = rm.GetString("backstageViewPageSerialInjectGPS.Text");
                if (rtcmStr == null)
                {
                    rtcmStr = "RTK/GPS Inject";
                }
                AddBackstageViewPage(typeof(ConfigSerialInjectGPS), rtcmStr, true, opt);
            }

            AddBackstageViewPage(typeof(ConfigCubeID), "CubeID Update", isConnected, opt);

            if (MainV2.DisplayConfiguration.displaySikRadio)
            {
                AddBackstageViewPage(typeof(Sikradio), rm.GetString("backstageViewPageSikradio.Text"), true, opt);
            }

            if (MainV2.DisplayConfiguration.displayGPSOrder)
                AddBackstageViewPage(typeof(ConfigGPSOrder), "CAN GPS Order", isConnected && gotAllParams, opt);

            if (MainV2.DisplayConfiguration.displayBattMonitor)
            {
                AddBackstageViewPage(typeof(ConfigBatteryMonitoring), rm.GetString("backstageViewPagebatmon.Text"), isConnected && gotAllParams, opt);
                AddBackstageViewPage(typeof(ConfigBatteryMonitoring2), rm.GetString("backstageViewPageBatt2.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayCAN)
            {
                AddBackstageViewPage(typeof(ConfigDroneCAN), "DroneCAN/UAVCAN", true, opt);
            }
            if (MainV2.DisplayConfiguration.displayJoystick)
            {
                AddBackstageViewPage(typeof(MissionPlanner.Joystick.JoystickSetup), "Joystick", true, opt);
            }

            if (MainV2.DisplayConfiguration.displayCompassMotorCalib)
            {
                AddBackstageViewPage(typeof(ConfigCompassMot), rm.GetString("backstageViewPagecompassmot.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayRangeFinder)
            {
                AddBackstageViewPage(typeof(ConfigHWRangeFinder), rm.GetString("backstageViewPagesonar.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayAirSpeed)
            {
                AddBackstageViewPage(typeof(ConfigHWAirspeed), rm.GetString("backstageViewPageairspeed.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayPx4Flow)
            {
                AddBackstageViewPage(typeof(ConfigHWPX4Flow), rm.GetString("backstageViewPagePX4Flow.Text"), true, opt);
            }
            if (MainV2.DisplayConfiguration.displayOpticalFlow)
            {
                AddBackstageViewPage(typeof(ConfigHWOptFlow), rm.GetString("backstageViewPageoptflow.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayOsd)
            {
                AddBackstageViewPage(typeof(ConfigHWOSD), rm.GetString("backstageViewPageosd.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayCameraGimbal)
            {
                AddBackstageViewPage(typeof(ConfigMount), rm.GetString("backstageViewPagegimbal.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayAntennaTracker)
            {
                AddBackstageViewPage(typeof(ConfigAntennaTracker), rm.GetString("backstageViewPageAntTrack.Text"), isTracker, opt);
            }
            if (MainV2.DisplayConfiguration.displayMotorTest)
            {
                AddBackstageViewPage(typeof(ConfigMotorTest), rm.GetString("backstageViewPageMotorTest.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayBluetooth)
            {
                AddBackstageViewPage(typeof(ConfigHWBT), rm.GetString("backstageViewPagehwbt.Text"), true, opt);
            }
            if (MainV2.DisplayConfiguration.displayParachute)
            {
                AddBackstageViewPage(typeof(ConfigHWParachute), rm.GetString("backstageViewPageParachute.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayEsp)
            {
                AddBackstageViewPage(typeof(ConfigHWESP8266), rm.GetString("backstageViewPageESP.Text"), isConnected && gotAllParams, opt);
            }
            if (MainV2.DisplayConfiguration.displayAntennaTracker)
            {
                AddBackstageViewPage(typeof(MissionPlanner.Antenna.TrackerUI), "Antenna Tracker", true, opt);
            }
            if (MainV2.DisplayConfiguration.displayFFTSetup)
            {
                AddBackstageViewPage(typeof(ConfigFFT), "FFT Setup", isConnected && gotAllParams, opt);
            }

            // 3. Advanced Options Menu Tab
            if (MainV2.DisplayConfiguration.isAdvancedMode)
            {
                var adv = AddBackstageViewPage(typeof(ConfigAdvanced), "Advanced");

                if (MainV2.DisplayConfiguration.displayTerminal)
                {
                    AddBackstageViewPage(typeof(ConfigTerminal), "Terminal", true, adv);
                }

                if (MainV2.DisplayConfiguration.displayREPL)
                {
                    AddBackstageViewPage(typeof(ConfigREPL), "Script REPL", isConnected, adv);
                }
            }
        }

        private void FirmwareSetup_FormClosing(object sender, FormClosingEventArgs e)
        {
            // Clean up
        }
    }

    [ComVisible(true)]
    public class ConfigFirmwareInstall : UserControl, IActivate
    {
        private static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private WebBrowser webBrowser;

        public ConfigFirmwareInstall()
        {
            this.Size = new Size(800, 600);
            this.BackColor = MainV2.OdinTheme.Background;
            this.ForeColor = MainV2.OdinTheme.White;
        }

        public void Activate()
        {
            SetBrowserFeatureControl();

            if (webBrowser == null)
            {
                webBrowser = new WebBrowser();
                webBrowser.Dock = DockStyle.Fill;
                webBrowser.ScriptErrorsSuppressed = true;
                webBrowser.ObjectForScripting = this;
                webBrowser.DocumentCompleted += WebBrowser_DocumentCompleted;
                this.Controls.Add(webBrowser);

                string htmlPath = Path.Combine(Settings.GetRunningDirectory(), "firmware_manager.html");
                if (!File.Exists(htmlPath))
                {
                    string sourcePath = Path.Combine(Application.StartupPath, "..", "..", "firmware_manager.html");
                    if (File.Exists(sourcePath))
                    {
                        try
                        {
                            File.Copy(sourcePath, htmlPath, true);
                        }
                        catch (Exception ex)
                        {
                            log.Error("Failed to copy firmware_manager.html", ex);
                        }
                    }
                }

                webBrowser.Navigate(htmlPath);
            }
            else
            {
                TriggerReload();
            }
        }

        public void Deactivate()
        {
            // Nothing to do
        }

        private void WebBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            TriggerReload();
        }

        private void TriggerReload()
        {
            if (webBrowser != null && webBrowser.ReadyState == WebBrowserReadyState.Complete)
            {
                webBrowser.Document.InvokeScript("onStatusLoaded", new object[] { GetSystemStatus() });
            }
        }

        // --- FIRMWARE MANAGER BRIDGE METHODS ---
        public string GetSystemStatus()
        {
            try
            {
                bool connected = MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen;
                string firmware = connected ? MainV2.comPort.MAV.cs.firmware.ToString() : "OFFLINE";
                string frame = connected ? MainV2.comPort.MAV.aptype.ToString() : "";
                string port = connected ? MainV2.comPort.BaseStream.PortName : "";
                string baud = connected ? MainV2.comPort.BaseStream.BaudRate.ToString() : "";

                var status = new
                {
                    connected = connected,
                    firmware = firmware,
                    frame = frame,
                    port = port,
                    baud = baud
                };
                return JsonConvert.SerializeObject(status);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return "{\"connected\":false,\"firmware\":\"OFFLINE\",\"frame\":\"\",\"port\":\"\",\"baud\":\"\"}";
            }
        }

        public string DetectConnectedBoard()
        {
            try
            {
                if (MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen)
                {
                    var activeInfo = new
                    {
                        connected = true,
                        detected = true,
                        port = MainV2.comPort.BaseStream.PortName,
                        boardType = MainV2.comPort.MAV.aptype.ToString(),
                        firmware = MainV2.comPort.MAV.cs.firmware.ToString(),
                        version = MainV2.comPort.MAV.cs.version.ToString(),
                        chip = "",
                        flashSize = 0,
                        chipDesc = ""
                    };
                    return JsonConvert.SerializeObject(activeInfo);
                }

                string[] ports = SerialPort.GetPortNames();
                foreach (var port in ports)
                {
                    try
                    {
                        using (var up = new px4uploader.Uploader(port, 115200))
                        {
                            up.identify();
                            var boardInfo = new
                            {
                                connected = false,
                                detected = true,
                                port = port,
                                boardType = GetFriendlyBoardName(up.board_type),
                                boardId = up.board_type,
                                chip = up.chip.ToString("X"),
                                flashSize = up.fw_maxsize,
                                chipDesc = up.chip_desc
                            };
                            return JsonConvert.SerializeObject(boardInfo);
                        }
                    }
                    catch { }
                }

                return "{\"detected\":false,\"connected\":false}";
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return "{\"detected\":false,\"connected\":false}";
            }
        }

        private string GetFriendlyBoardName(int boardType)
        {
            switch (boardType)
            {
                case 9: return "Pixhawk / FMUv3";
                case 50: return "Cube Orange";
                case 20: return "Pixhawk 4";
                case 54: return "Pixhawk 5X";
                case 55: return "Pixhawk 6X";
                default: return "Board Type " + boardType;
            }
        }

        public string GetFirmwareListJson(bool includeBeta)
        {
            try
            {
                string url = includeBeta
                    ? "https://github.com/ArduPilot/binary/raw/master/dev/firmware2.xml;https://firmware.ardupilot.org/Tools/MissionPlanner/dev/firmware2.xml"
                    : "https://github.com/ArduPilot/binary/raw/master/Firmware/firmware2.xml;https://firmware.ardupilot.org/Tools/MissionPlanner/Firmware/firmware2.xml";

                var fwLoader = new Firmware();
                var list = fwLoader.getFWList(url);

                var simpleList = new List<object>();
                foreach (var soft in list)
                {
                    string vehicle = "other";
                    string nameLower = soft.name.ToLower();
                    string descLower = soft.desc.ToLower();
                    string fmuLower = soft.urlfmuv3.ToLower();

                    if (nameLower.Contains("copter") || descLower.Contains("copter") || fmuLower.Contains("copter"))
                    {
                        vehicle = "copter";
                    }
                    else if (nameLower.Contains("plane") || descLower.Contains("plane") || fmuLower.Contains("plane"))
                    {
                        vehicle = "plane";
                    }
                    else if (nameLower.Contains("rover") || descLower.Contains("rover") || fmuLower.Contains("rover"))
                    {
                        vehicle = "rover";
                    }
                    else if (nameLower.Contains("sub") || descLower.Contains("sub") || fmuLower.Contains("sub"))
                    {
                        vehicle = "sub";
                    }
                    else if (nameLower.Contains("tracker") || descLower.Contains("tracker") || fmuLower.Contains("tracker"))
                    {
                        vehicle = "tracker";
                    }

                    simpleList.Add(new
                    {
                        name = soft.name,
                        desc = soft.desc,
                        vehicleType = vehicle,
                        releaseType = includeBeta ? "beta" : "stable",
                        urlfmuv2 = soft.urlfmuv2,
                        urlfmuv3 = soft.urlfmuv3,
                        urlfmuv5 = soft.urlfmuv5,
                        urlpx4v2 = soft.urlpx4v2,
                        urlpx4v3 = soft.urlpx4v3,
                        rawSoftware = JsonConvert.SerializeObject(soft)
                    });
                }
                return JsonConvert.SerializeObject(simpleList);
            }
            catch (Exception ex)
            {
                log.Error(ex);
                return "[]";
            }
        }

        public void FlashFirmware(string rawSoftwareJson)
        {
            try
            {
                var soft = JsonConvert.DeserializeObject<Firmware.software>(rawSoftwareJson);
                if (soft == null) return;

                try
                {
                    if (MainV2.comPort.BaseStream != null && MainV2.comPort.BaseStream.IsOpen)
                    {
                        MainV2.comPort.BaseStream.Close();
                    }
                }
                catch { }

                Task.Run(() =>
                {
                    try
                    {
                        var fwFlashing = new Firmware();
                        fwFlashing.Progress += (percent, status) =>
                        {
                            this.BeginInvokeIfRequired(() =>
                            {
                                if (webBrowser != null && webBrowser.ReadyState == WebBrowserReadyState.Complete)
                                {
                                    webBrowser.Document.InvokeScript("onFlashProgress", new object[] { percent, status });
                                }
                            });
                        };

                        var ports = Win32DeviceMgmt.GetAllCOMPorts();
                        ports.AddRange(Linux.GetAllCOMPorts());

                        bool success = fwFlashing.updateLegacy(MainV2.comPortName, soft, "", ports);

                        this.BeginInvokeIfRequired(() =>
                        {
                            if (webBrowser != null && webBrowser.ReadyState == WebBrowserReadyState.Complete)
                            {
                                webBrowser.Document.InvokeScript("onFlashCompleted", new object[] { success, success ? "Firmware flashed successfully! Please reboot your flight controller." : "Failed to flash firmware." });
                            }
                        });
                    }
                    catch (Exception ex)
                    {
                        log.Error(ex);
                        this.BeginInvokeIfRequired(() =>
                        {
                            if (webBrowser != null && webBrowser.ReadyState == WebBrowserReadyState.Complete)
                            {
                                webBrowser.Document.InvokeScript("onFlashCompleted", new object[] { false, "Flash error: " + ex.Message });
                            }
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                log.Error(ex);
                CustomMessageBox.Show("Flash initialization error: " + ex.Message, "Error");
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
                log.Error("Failed to set FEATURE_BROWSER_EMULATION in registry", ex);
            }
        }
    }
}
