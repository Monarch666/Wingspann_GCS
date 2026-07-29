using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.Controls;
using MissionPlanner.Controls.BackstageView;
using MissionPlanner.GCSViews.ConfigurationView;
using MissionPlanner.Radio;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using System.Windows.Forms;

namespace MissionPlanner.GCSViews
{
    public partial class InitialSetup : MyUserControl, IActivate
    {
        internal static readonly ILog log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static string lastpagename = "";

        [Flags]
        public enum pageOptions
        {
            none = 0,
            isConnected = 1,
            isDisConnected = 2,
            isTracker = 4,
            isCopter = 8,
            isCopter35plus = 16,
            isHeli = 32,
            isQuadPlane = 64,
            isPlane = 128,
            isRover = 256,
            gotAllParams = 512
        }

        public class pluginPage
        {
            public Type page;
            public string headerText;
            public pageOptions options;

            public pluginPage(Type page, string headerText, pageOptions options)
            {
                this.page = page;
                this.headerText = headerText;
                this.options = options;
            }
        }


        private static List<pluginPage> pluginViewPages = new List<pluginPage>();
        public static void AddPluginViewPage(Type page, string headerText, pageOptions options = pageOptions.none)
        {
            pluginViewPages.Add(new pluginPage(page, headerText, options));
        }


        public InitialSetup()
        {
            InitializeComponent();
        }

        public bool isConnected
        {
            get { return MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isDisConnected
        {
            get { return !MainV2.comPort.BaseStream.IsOpen; }
        }

        public bool isTracker
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduTracker; }
        }

        public bool isCopter
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduCopter2; }
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
                       (MainV2.comPort.MAV.cs.firmware == Firmwares.ArduPlane ||
                        MainV2.comPort.MAV.cs.firmware == Firmwares.Ateryx);
            }
        }

        public bool isRover
        {
            get { return isConnected && MainV2.comPort.MAV.cs.firmware == Firmwares.ArduRover; }
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
            HardwareConfig_Load(null, null);
        }

        private void HardwareConfig_Load(object sender, EventArgs e)
        {
            backstageView.Clear();
            ResourceManager rm = new ResourceManager(this.GetType());

            // Add the Setup Overview Dashboard at the top
            AddBackstageViewPage(typeof(ConfigSetupOverview), "Overview");

            if (!gotAllParams)
            {
                if (MainV2.comPort.BaseStream.IsOpen)
                    AddBackstageViewPage(typeof(ConfigParamLoading), Strings.Loading);
            }




            var mand = AddBackstageViewPage(typeof(ConfigMandatory), rm.GetString("backstageViewPagemand.Text"), isConnected && gotAllParams);

            if (MainV2.DisplayConfiguration.displayFrameType)
            {
                //AddBackstageViewPage(typeof(ConfigTradHeli), rm.GetString("backstageViewPagetradheli.Text"), isHeli && gotAllParams, mand);
                AddBackstageViewPage(typeof(ConfigTradHeli4), rm.GetString("backstageViewPagetradheli.Text"), isHeli && gotAllParams, mand);
                AddBackstageViewPage(typeof(ConfigFrameType), rm.GetString("backstageViewPageframetype.Text"), isCopter && gotAllParams && !isCopter35plus, mand);
                AddBackstageViewPage(typeof(ConfigFrameClassType), rm.GetString("backstageViewPageframetype.Text"),
                    MainV2.comPort.MAV.param.ContainsKey("FRAME_CLASS") || isCopter && gotAllParams && isCopter35plus,
                    mand);
            }

            if (MainV2.DisplayConfiguration.displayAccelCalibration)
            {
                AddBackstageViewPage(typeof(ConfigAccelerometerCalibration), rm.GetString("backstageViewPageaccel.Text"), true, mand);
            }


            if (MainV2.DisplayConfiguration.displayCompassConfiguration)
            {
                // Always use ConfigHWCompass (modern Horizon UI) regardless of firmware version.
                // ConfigHWCompass2 was loaded when COMPASS_PRIO1_ID existed (newer firmware),
                // but this caused an inconsistent UI between connected and disconnected states.
                AddBackstageViewPage(typeof(ConfigHWCompass), rm.GetString("backstageViewPagecompass.Text"),
                    true, mand);
            }
            if (MainV2.DisplayConfiguration.displayRadioCalibration)
            {
                AddBackstageViewPage(typeof(ConfigRadioInput), rm.GetString("backstageViewPageradio.Text"), true, mand);
            }
            if (MainV2.DisplayConfiguration.displayServoOutput)
            {
                AddBackstageViewPage(typeof(ConfigRadioOutput), "Servo Output", isConnected && gotAllParams, mand);

            }
            if (MainV2.DisplayConfiguration.displaySerialPorts)
            {
                AddBackstageViewPage(typeof(ConfigSerial), rm.GetString("backstageViewPageSerial.Text"), isConnected && gotAllParams, mand);
            }
            if (MainV2.DisplayConfiguration.displayEscCalibration)
            {
                AddBackstageViewPage(typeof(ConfigESCCalibration), "ESC Calibration", isConnected && gotAllParams, mand);
            }
            if (MainV2.DisplayConfiguration.displayFlightModes)
            {
                AddBackstageViewPage(typeof(ConfigFlightModes), rm.GetString("backstageViewPageflmode.Text"), isConnected && gotAllParams, mand);
            }
            if (MainV2.DisplayConfiguration.displayFailSafe)
            {
                AddBackstageViewPage(typeof(ConfigFailSafe), rm.GetString("backstageViewPagefs.Text"), isConnected && gotAllParams, mand);
            }

            if ((isCopter || isQuadPlane) && MainV2.DisplayConfiguration.displayInitialParams)
            {
                AddBackstageViewPage(typeof(ConfigInitialParams), rm.GetString("backstageViewPageInitialParams.Text"), isConnected && gotAllParams, mand);
            }

            if (MainV2.DisplayConfiguration.displayHWIDs)
                AddBackstageViewPage(typeof(ConfigHWIDs), "HW ID", isConnected && gotAllParams, mand);

            if (MainV2.DisplayConfiguration.displayADSB)
                AddBackstageViewPage(typeof(ConfigADSB), "ADSB", isConnected && gotAllParams, mand);

            // Optional hardware and advanced options sections are removed from Calibration (InitialSetup) and moved to Hardware & Firmware (FirmwareSetup) view.


            foreach (var item in pluginViewPages)
            {

                // go through all options
                if (item.options.HasFlag(pageOptions.isConnected) && !isConnected)
                    continue;
                if (item.options.HasFlag(pageOptions.isDisConnected) && !isDisConnected)
                    continue;
                if (item.options.HasFlag(pageOptions.isTracker) && !isTracker)
                    continue;
                if (item.options.HasFlag(pageOptions.isCopter) && !isCopter)
                    continue;
                if (item.options.HasFlag(pageOptions.isCopter35plus) && !isCopter35plus)
                    continue;
                if (item.options.HasFlag(pageOptions.isHeli) && !isHeli)
                    continue;
                if (item.options.HasFlag(pageOptions.isQuadPlane) && !isQuadPlane)
                    continue;
                if (item.options.HasFlag(pageOptions.isPlane) && !isPlane)
                    continue;
                if (item.options.HasFlag(pageOptions.isRover) && !isRover)
                    continue;
                if (item.options.HasFlag(pageOptions.gotAllParams) && !gotAllParams)
                    continue;

                AddBackstageViewPage(item.page, item.headerText);
            }

            // remeber last page accessed
            bool activated = false;
            foreach (BackstageViewPage page in backstageView.Pages)
            {
                if (page.LinkText == lastpagename && page.Show)
                {
                    backstageView.ActivatePage(page);
                    activated = true;
                    break;
                }
            }
            if (!activated && backstageView.Pages.Count > 0)
            {
                foreach (BackstageViewPage page in backstageView.Pages)
                {
                    if (page.Show)
                    {
                        backstageView.ActivatePage(page);
                        break;
                    }
                }
            }

            ThemeManager.ApplyThemeTo(this);
        }

        private void HardwareConfig_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (backstageView.SelectedPage != null)
                lastpagename = backstageView.SelectedPage.LinkText;

            backstageView.Close();
        }
    }
}