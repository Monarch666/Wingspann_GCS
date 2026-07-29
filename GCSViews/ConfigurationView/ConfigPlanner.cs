using DirectShowLib;
using MissionPlanner.Controls;
using MissionPlanner.Joystick;
using MissionPlanner.Maps;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WebCamService;
using Newtonsoft.Json;
using log4net;

namespace MissionPlanner.GCSViews.ConfigurationView
{
    [ComVisible(true)]
    public partial class ConfigPlanner : MyUserControl, IActivate
    {
        private List<CultureInfo> _languages;
        private bool startup;
        static temp temp;
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private WebBrowser webBrowser;

        public ConfigPlanner()
        {
            InitializeComponent();
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

                string htmlPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "planner_settings.html");
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

        public string GetPlannerSettings()
        {
            try
            {
                var cultureCodes = new[]
                {
                    "en-US", "zh-Hans", "zh-TW", "ru-RU", "Fr", "Pl", "it-IT", "es-ES", "de-DE", "ja-JP", "id-ID", "ko-KR",
                    "ar", "pt", "tr", "ru-KZ", "uk"
                };
                var languages = cultureCodes
                    .Select(CultureInfoEx.GetCultureInfo)
                    .Where(c => c != null)
                    .Select(c => new { code = c.Name, name = c.DisplayName })
                    .ToList();

                var currentUiCulture = Thread.CurrentThread.CurrentUICulture.Name;

                // Enumerate video devices
                var videoDevices = new List<string>();
                try
                {
                    var devices = DsDevice.GetDevicesOfCat(FilterCategory.VideoInputDevice);
                    foreach (var d in devices) videoDevices.Add(d.Name);
                }
                catch { }

                var settings = new
                {
                    // General/Layout
                    layout = MainV2.DisplayConfiguration.displayName.ToString(),
                    language = currentUiCulture,
                    languages = languages,
                    theme = ThemeManager.thmColor.strThemeName,
                    themes = ThemeManager.ThemeNames,
                    gcsId = MAVLinkInterface.gcssysid,
                    severity = Settings.Instance.GetInt32("severity", 4),

                    // Video
                    videoDevice = Settings.Instance.GetInt32("video_device", 0),
                    videoDevices = videoDevices,
                    hudShow = FlightData.myhud.hudon,
                    hudColor = Settings.Instance["hudcolor"] ?? "White",
                    hudColors = Enum.GetNames(typeof(KnownColor)),

                    // Units
                    distUnits = Settings.Instance["distunits"]?.ToString() ?? "Meters",
                    distUnitsOptions = Enum.GetNames(typeof(distances)),
                    speedUnits = Settings.Instance["speedunits"]?.ToString() ?? "meters_per_second",
                    speedUnitsOptions = Enum.GetNames(typeof(speeds)),
                    altUnits = Settings.Instance["altunits"]?.ToString() ?? "Meters",
                    altUnitsOptions = Enum.GetNames(typeof(altitudes)),

                    // Telemetry Rates
                    rateAttitude = MainV2.comPort.MAV.cs.rateattitude,
                    ratePosition = MainV2.comPort.MAV.cs.rateposition,
                    rateRc = MainV2.comPort.MAV.cs.raterc,
                    rateStatus = MainV2.comPort.MAV.cs.ratestatus,
                    rateSensors = MainV2.comPort.MAV.cs.ratesensors,

                    // Speech Checkboxes
                    speechEnable = Settings.Instance.GetBoolean("speechenable"),
                    speechArmedOnly = Settings.Instance.GetBoolean("speech_armed_only"),
                    speechWaypoint = Settings.Instance.GetBoolean("speechwaypointenabled"),
                    speechMode = Settings.Instance.GetBoolean("speechmodeenabled"),
                    speechCustom = Settings.Instance.GetBoolean("speechcustomenabled"),
                    speechBattery = Settings.Instance.GetBoolean("speechbatteryenabled"),
                    speechAlt = Settings.Instance.GetBoolean("speechaltenabled"),
                    speechArm = Settings.Instance.GetBoolean("speecharmenabled"),
                    speechLowSpeed = Settings.Instance.GetBoolean("speechlowspeedenabled"),

                    // Aircraft Display
                    displayCog = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayCOG", true),
                    displayHeading = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayHeading", true),
                    displayNavBearing = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayNavBearing", true),
                    displayRadius = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayRadius", true),
                    displayTarget = Settings.Instance.GetBoolean("GMapMarkerBase_DisplayTarget", true),
                    displayTooltip = Settings.Instance.GetString("mapicondesc", "") != "",
                    tooltipFormat = Settings.Instance["mapicondesc"] ?? "",
                    lineLength = Settings.Instance.GetInt32("GMapMarkerBase_Length", 500),
                    inactiveStyle = Settings.Instance.GetString("GMapMarkerBase_InactiveDisplayStyle", Maps.GMapMarkerBase.InactiveDisplayStyleEnum.Normal.ToString()),
                    inactiveStyles = Enum.GetNames(typeof(Maps.GMapMarkerBase.InactiveDisplayStyleEnum)),

                    // Logging & Map Cache
                    logDir = Settings.Instance.LogDir,
                    mapCache = Settings.Instance["mapCache"] ?? GMap.NET.GMaps.Instance.Mode.ToString(),
                    mapCacheOptions = Enum.GetNames(typeof(GMap.NET.AccessMode)),
                    mapCacheDir = MyImageCache.Instance.CacheLocation,

                    // Checkbox Settings
                    betaUpdates = Settings.Instance.GetBoolean("beta_updates"),
                    norcReceiver = Settings.Instance.GetBoolean("norcreceiver"),
                    showAirports = Settings.Instance.GetBoolean("showairports"),
                    enableAdsb = Settings.Instance.GetBoolean("enableadsb"),
                    showTfr = Settings.Instance.GetBoolean("showtfr"),
                    passwordProtect = Settings.Instance.GetBoolean("password_protect"),
                    autoParamCommit = Settings.Instance.GetBoolean("autoParamCommit"),
                    showNoFly = Settings.Instance.GetBoolean("ShowNoFly"),
                    paramsBg = Settings.Instance.GetBoolean("Params_BG"),
                    slowMachine = Settings.Instance.GetBoolean("SlowMachine"),
                    analyticsOptOut = Settings.Instance.GetBoolean("analyticsoptout"),
                    gdiPlus = Settings.Instance.GetBoolean("CHK_GDIPlus"),
                    mapRotation = Settings.Instance.GetBoolean("CHK_maprotation"),
                    distToHomeFlightData = Settings.Instance.GetBoolean("CHK_disttohomeflightdata"),
                    resetapmonconnect = Settings.Instance.GetBoolean("CHK_resetapmonconnect"),
                    rtsresetesp32 = Settings.Instance.GetBoolean("CHK_rtsresetesp32")
                };

                return JsonConvert.SerializeObject(settings);
            }
            catch (Exception ex)
            {
                log.Error("GetPlannerSettings error", ex);
                return "{}";
            }
        }

        public bool SavePlannerSettings(string json)
        {
            try
            {
                var s = JsonConvert.DeserializeObject<Dictionary<string, string>>(json);

                Action<string, string> saveBool = (key, val) => {
                    Settings.Instance[key] = (val == "true" || val == "True" || val == "1").ToString();
                };

                // General/Layout
                if (s.ContainsKey("layout"))
                {
                    string layout = s["layout"];
                    if (layout == DisplayNames.Advanced.ToString()) MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Advanced();
                    else if (layout == DisplayNames.Basic.ToString()) MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Basic();
                    else if (layout == DisplayNames.Custom.ToString()) MainV2.DisplayConfiguration = MainV2.DisplayConfiguration.Custom();
                    Settings.Instance["displayview"] = MainV2.DisplayConfiguration.ConvertToString();
                }

                if (s.ContainsKey("language"))
                {
                    string lang = s["language"];
                    try {
                        var ci = CultureInfoEx.GetCultureInfo(lang);
                        if (ci != null) MainV2.instance.changelanguage(ci);
                    } catch {}
                }

                if (s.ContainsKey("theme"))
                {
                    string theme = s["theme"];
                    try {
                        ThemeManager.LoadTheme(theme);
                        ThemeManager.ApplyThemeTo(MainV2.instance);
                    } catch {}
                }

                if (s.ContainsKey("gcsId")) Settings.Instance["gcsid"] = s["gcsId"];
                if (s.ContainsKey("severity")) Settings.Instance["severity"] = s["severity"];

                // Video
                if (s.ContainsKey("videoDevice")) Settings.Instance["video_device"] = s["videoDevice"];
                if (s.ContainsKey("videoOptions")) Settings.Instance["video_options"] = s["videoOptions"];
                if (s.ContainsKey("hudShow")) {
                    bool val = s["hudShow"] == "true";
                    FlightData.myhud.hudon = val;
                    Settings.Instance["CHK_hudshow"] = val.ToString();
                }
                if (s.ContainsKey("hudColor")) Settings.Instance["hudcolor"] = s["hudColor"];

                // Units
                bool unitsChanged = false;
                if (s.ContainsKey("distUnits")) {
                    string val = s["distUnits"];
                    if (Settings.Instance["distunits"]?.ToString() != val) {
                        Settings.Instance["distunits"] = val;
                        unitsChanged = true;
                    }
                }
                if (s.ContainsKey("speedUnits")) {
                    string val = s["speedUnits"];
                    if (Settings.Instance["speedunits"]?.ToString() != val) {
                        Settings.Instance["speedunits"] = val;
                        unitsChanged = true;
                    }
                }
                if (s.ContainsKey("altUnits")) {
                    string val = s["altUnits"];
                    if (Settings.Instance["altunits"]?.ToString() != val) {
                        Settings.Instance["altunits"] = val;
                        unitsChanged = true;
                    }
                }
                if (unitsChanged) {
                    MainV2.instance.ChangeUnits();
                }

                // Telemetry Rates
                if (s.ContainsKey("rateAttitude") && int.TryParse(s["rateAttitude"], out int rAtt)) {
                    MainV2.comPort.MAV.cs.rateattitude = rAtt;
                    CurrentState.rateattitudebackup = MainV2.comPort.MAV.cs.rateattitude;
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA1, MainV2.comPort.MAV.cs.rateattitude);
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA2, MainV2.comPort.MAV.cs.rateattitude);
                }
                if (s.ContainsKey("ratePosition") && int.TryParse(s["ratePosition"], out int rPos)) {
                    MainV2.comPort.MAV.cs.rateposition = rPos;
                    CurrentState.ratepositionbackup = MainV2.comPort.MAV.cs.rateposition;
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.POSITION, MainV2.comPort.MAV.cs.rateposition);
                }
                if (s.ContainsKey("rateRc") && int.TryParse(s["rateRc"], out int rRc)) {
                    MainV2.comPort.MAV.cs.raterc = rRc;
                    CurrentState.ratercbackup = MainV2.comPort.MAV.cs.raterc;
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RC_CHANNELS, MainV2.comPort.MAV.cs.raterc);
                }
                if (s.ContainsKey("rateStatus") && int.TryParse(s["rateStatus"], out int rStat)) {
                    MainV2.comPort.MAV.cs.ratestatus = rStat;
                    CurrentState.ratestatusbackup = MainV2.comPort.MAV.cs.ratestatus;
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTENDED_STATUS, MainV2.comPort.MAV.cs.ratestatus);
                }
                if (s.ContainsKey("rateSensors") && int.TryParse(s["rateSensors"], out int rSens)) {
                    MainV2.comPort.MAV.cs.ratesensors = rSens;
                    CurrentState.ratesensorsbackup = MainV2.comPort.MAV.cs.ratesensors;
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.EXTRA3, MainV2.comPort.MAV.cs.ratesensors);
                    MainV2.comPort.requestDatastream(MAVLink.MAV_DATA_STREAM.RAW_SENSORS, MainV2.comPort.MAV.cs.ratesensors);
                }

                // Speech Checkboxes
                if (s.ContainsKey("speechEnable")) {
                    bool val = s["speechEnable"] == "true";
                    MainV2.speechEnable = val;
                    Settings.Instance["speechenable"] = val.ToString();
                    if (MainV2.speechEngine != null) MainV2.speechEngine.SpeakAsyncCancelAll();
                }
                if (s.ContainsKey("speechArmedOnly")) {
                    MainV2.speech_armed_only = s["speechArmedOnly"] == "true";
                    Settings.Instance["speech_armed_only"] = s["speechArmedOnly"];
                }
                if (s.ContainsKey("speechWaypoint")) saveBool("speechwaypointenabled", s["speechWaypoint"]);
                if (s.ContainsKey("speechMode")) saveBool("speechmodeenabled", s["speechMode"]);
                if (s.ContainsKey("speechCustom")) saveBool("speechcustomenabled", s["speechCustom"]);
                if (s.ContainsKey("speechBattery")) saveBool("speechbatteryenabled", s["speechBattery"]);
                if (s.ContainsKey("speechAlt")) saveBool("speechaltenabled", s["speechAlt"]);
                if (s.ContainsKey("speechArm")) saveBool("speecharmenabled", s["speechArm"]);
                if (s.ContainsKey("speechLowSpeed")) saveBool("speechlowspeedenabled", s["speechLowSpeed"]);

                // Aircraft Display
                if (s.ContainsKey("displayCog")) {
                    saveBool("GMapMarkerBase_DisplayCOG", s["displayCog"]);
                    Maps.GMapMarkerBase.DisplayCOGSetting = s["displayCog"] == "true";
                }
                if (s.ContainsKey("displayHeading")) {
                    saveBool("GMapMarkerBase_DisplayHeading", s["displayHeading"]);
                    Maps.GMapMarkerBase.DisplayHeadingSetting = s["displayHeading"] == "true";
                }
                if (s.ContainsKey("displayNavBearing")) {
                    saveBool("GMapMarkerBase_DisplayNavBearing", s["displayNavBearing"]);
                    Maps.GMapMarkerBase.DisplayNavBearingSetting = s["displayNavBearing"] == "true";
                }
                if (s.ContainsKey("displayRadius")) {
                    saveBool("GMapMarkerBase_DisplayRadius", s["displayRadius"]);
                    Maps.GMapMarkerBase.DisplayRadiusSetting = s["displayRadius"] == "true";
                }
                if (s.ContainsKey("displayTarget")) {
                    saveBool("GMapMarkerBase_DisplayTarget", s["displayTarget"]);
                    Maps.GMapMarkerBase.DisplayTargetSetting = s["displayTarget"] == "true";
                }
                if (s.ContainsKey("tooltipFormat")) {
                    Settings.Instance["mapicondesc"] = s["tooltipFormat"];
                }
                if (s.ContainsKey("lineLength")) {
                    Settings.Instance["GMapMarkerBase_length"] = s["lineLength"];
                    Maps.GMapMarkerBase.length = int.Parse(s["lineLength"]);
                }
                if (s.ContainsKey("inactiveStyle")) {
                    if (Enum.TryParse(s["inactiveStyle"], out Maps.GMapMarkerBase.InactiveDisplayStyleEnum result)) {
                        Settings.Instance["GMapMarkerBase_InactiveDisplayStyle"] = s["inactiveStyle"];
                        Maps.GMapMarkerBase.InactiveDisplayStyle = result;
                    }
                }

                // Logging & Map Cache
                if (s.ContainsKey("logDir")) {
                    string path = s["logDir"];
                    if (!string.IsNullOrEmpty(path) && Directory.Exists(path)) {
                        Settings.Instance.LogDir = path;
                    }
                }
                if (s.ContainsKey("mapCache")) {
                    Settings.Instance["mapCache"] = s["mapCache"];
                    GMap.NET.GMaps.Instance.Mode = (GMap.NET.AccessMode)Enum.Parse(typeof(GMap.NET.AccessMode), s["mapCache"]);
                }

                // Standard checkbox settings
                if (s.ContainsKey("betaUpdates")) saveBool("beta_updates", s["betaUpdates"]);
                if (s.ContainsKey("norcReceiver")) saveBool("norcreceiver", s["norcReceiver"]);
                if (s.ContainsKey("showAirports")) {
                    saveBool("showairports", s["showAirports"]);
                    MainV2.ShowAirports = s["showAirports"] == "true";
                }
                if (s.ContainsKey("enableAdsb")) {
                    saveBool("enableadsb", s["enableAdsb"]);
                    MainV2.instance.EnableADSB = s["enableAdsb"] == "true";
                }
                if (s.ContainsKey("showTfr")) {
                    saveBool("showtfr", s["showTfr"]);
                    MainV2.ShowTFR = s["showTfr"] == "true";
                }
                if (s.ContainsKey("passwordProtect")) saveBool("password_protect", s["passwordProtect"]);
                if (s.ContainsKey("autoParamCommit")) saveBool("autoParamCommit", s["autoParamCommit"]);
                bool newShowNoFly = false;
                bool showNoFlyPresent = s.ContainsKey("showNoFly");
                if (showNoFlyPresent) {
                    newShowNoFly = (s["showNoFly"] == "true" || s["showNoFly"] == "True");
                }

                bool newMapRotation = false;
                bool mapRotationPresent = s.ContainsKey("mapRotation");
                if (mapRotationPresent) {
                    newMapRotation = (s["mapRotation"] == "true" || s["mapRotation"] == "True");
                }

                if (showNoFlyPresent || mapRotationPresent) {
                    // Mutual exclusion logic
                    if (newMapRotation) {
                        newShowNoFly = false;
                    } else if (newShowNoFly) {
                        newMapRotation = false;
                    }

                    if (showNoFlyPresent) {
                        Settings.Instance["ShowNoFly"] = newShowNoFly.ToString();
                    }
                    if (mapRotationPresent) {
                        Settings.Instance["CHK_maprotation"] = newMapRotation.ToString();
                        if (newMapRotation && FlightData.instance != null && FlightData.instance.gMapControl1 != null) {
                            FlightData.instance.gMapControl1.Bearing = 0;
                        }
                    }
                }

                if (s.ContainsKey("paramsBg")) saveBool("Params_BG", s["paramsBg"]);
                if (s.ContainsKey("slowMachine")) saveBool("SlowMachine", s["slowMachine"]);
                if (s.ContainsKey("analyticsOptOut")) {
                    saveBool("analyticsoptout", s["analyticsOptOut"]);
                    Tracking.OptOut = s["analyticsOptOut"] == "true";
                }
                if (s.ContainsKey("gdiPlus")) saveBool("CHK_GDIPlus", s["gdiPlus"]);
                if (s.ContainsKey("distToHomeFlightData")) saveBool("CHK_disttohomeflightdata", s["distToHomeFlightData"]);
                if (s.ContainsKey("resetapmonconnect")) saveBool("CHK_resetapmonconnect", s["resetapmonconnect"]);
                if (s.ContainsKey("rtsresetesp32")) saveBool("CHK_rtsresetesp32", s["rtsresetesp32"]);

                return true;
            }
            catch (Exception ex)
            {
                log.Error("SavePlannerSettings error", ex);
                return false;
            }
        }

        public bool StartVideoDevice()
        {
            try
            {
                // Simple mockup video trigger
                return true;
            }
            catch (Exception ex)
            {
                log.Error("StartVideoDevice error", ex);
                return false;
            }
        }

        public bool StopVideoDevice()
        {
            try
            {
                return true;
            }
            catch (Exception ex)
            {
                log.Error("StopVideoDevice error", ex);
                return false;
            }
        }

        public void ShowJoystickSetup()
        {
            try
            {
                new JoystickSetup().ShowUserControl();
            }
            catch (Exception ex)
            {
                log.Error("ShowJoystickSetup error", ex);
            }
        }

        public string BrowseLogDir()
        {
            try
            {
                using (var ofd = new FolderBrowserDialog())
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        return ofd.SelectedPath;
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("BrowseLogDir error", ex);
            }
            return "";
        }

        public void OpenMapCacheDir()
        {
            try
            {
                string folderPath = MyImageCache.Instance.CacheLocation;
                if (Directory.Exists(folderPath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        Arguments = folderPath,
                        FileName = "explorer.exe"
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error("OpenMapCacheDir error", ex);
            }
        }

        public void ToggleVario()
        {
            try
            {
                if (Vario.run) Vario.Stop();
                else Vario.Start();
            }
            catch (Exception ex)
            {
                log.Error("ToggleVario error", ex);
            }
        }

        public class GCSBitmapInfo
        {
            public GCSBitmapInfo(int width, int height, long fps, string standard, AMMediaType media)
            {
                Width = width;
                Height = height;
                Fps = fps;
                Standard = standard;
                Media = media;
            }

            public int Width { get; set; }
            public int Height { get; set; }
            public long Fps { get; set; }
            public string Standard { get; set; }
            public AMMediaType Media { get; set; }

            public override string ToString()
            {
                return Width + " x " + Height + string.Format(" {0:0.00} fps ", 10000000.0 / Fps) + Standard;
            }
        }

        // --- Legacy Designer Stubs to ensure compilation ---

        private void BUT_videostart_Click(object sender, EventArgs e) { }
        private void BUT_videostop_Click(object sender, EventArgs e) { }
        private void CMB_videosources_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CHK_hudshow_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_enablespeech_CheckedChanged(object sender, EventArgs e) { }
        private void CMB_severity_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_language_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_osdcolor_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CHK_speechwaypoint_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_speechmode_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_speechcustom_CheckedChanged(object sender, EventArgs e) { }
        private void BUT_rerequestparams_Click(object sender, EventArgs e) { }
        private void CHK_speechbattery_CheckedChanged(object sender, EventArgs e) { }
        private void BUT_Joystick_Click(object sender, EventArgs e) { }
        private void CMB_distunits_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_speedunits_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_rateattitude_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_rateposition_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_ratestatus_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_raterc_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_ratesensors_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CHK_mavdebug_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_resetapmonconnect_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_rtsresetesp32_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_speechaltwarning_CheckedChanged(object sender, EventArgs e) { }
        private void NUM_tracklength_ValueChanged(object sender, EventArgs e) { }
        private void CHK_loadwponconnect_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_GDIPlus_CheckedChanged(object sender, EventArgs e) { }
        private void ConfigPlanner_Load(object sender, EventArgs e) { }
        private void CMB_osdcolor_DrawItem(object sender, DrawItemEventArgs e) { }
        private void CMB_videosources_Click(object sender, EventArgs e) { }
        private void CHK_maprotation_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_disttohomeflightdata_CheckedChanged(object sender, EventArgs e) { }
        private void BUT_logdirbrowse_Click(object sender, EventArgs e) { }
        private void CMB_theme_SelectedIndexChanged(object sender, EventArgs e) { }
        private void BUT_themecustom_Click(object sender, EventArgs e) { }
        private void CHK_speecharmdisarm_CheckedChanged(object sender, EventArgs e) { }
        private void BUT_Vario_Click(object sender, EventArgs e) { }
        private void chk_analytics_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_beta_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_Password_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_speechlowspeed_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_showairports_CheckedChanged(object sender, EventArgs e) { }
        private void chk_ADSB_CheckedChanged(object sender, EventArgs e) { }
        private void chk_tfr_CheckedChanged(object sender, EventArgs e) { }
        private void chk_temp_CheckedChanged(object sender, EventArgs e) { }
        private void chk_norcreceiver_CheckedChanged(object sender, EventArgs e) { }
        private void CMB_Layout_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CHK_AutoParamCommit_CheckedChanged(object sender, EventArgs e) { }
        private void chk_shownofly_CheckedChanged(object sender, EventArgs e) { }
        private void CMB_altunits_SelectedIndexChanged(object sender, EventArgs e) { }
        private void num_gcsid_ValueChanged(object sender, EventArgs e) { }
        private void CHK_params_bg_CheckedChanged(object sender, EventArgs e) { }
        private void chk_slowMachine_CheckedChanged(object sender, EventArgs e) { }
        private void CHK_speechArmedOnly_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displaycog_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displayheading_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displaynavbearing_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displayradius_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displaytarget_CheckedChanged(object sender, EventArgs e) { }
        private void chk_displaytooltip_CheckedChanged(object sender, EventArgs e) { }
        private void num_linelength_ValueChanged(object sender, EventArgs e) { }
        private void cmb_secondarydisplaystyle_SelectedIndexChanged(object sender, EventArgs e) { }
        private void CMB_mapCache_SelectedIndexChanged(object sender, EventArgs e) { }
        private void BUT_mapCacheDir_Click(object sender, EventArgs e) { }
    }
}