using Ionic.Zip;
using log4net;
using MissionPlanner.ArduPilot;
using MissionPlanner.ArduPilot.Mavlink;
using MissionPlanner.Utilities;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace MissionPlanner.Controls
{
    [ComVisible(true)]
    public partial class MavFTPUI : UserControl
    {
        private static readonly ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private MAVLinkInterface _mav;
        private MAVFtp _mavftp;
        private WebBrowser webBrowser;

        public MavFTPUI() : this(MainV2.comPort)
        {
        }

        public MavFTPUI(MAVLinkInterface mav)
        {
            _mav = mav;
            _mavftp = new MAVFtp(_mav, (byte)_mav.sysidcurrent, (byte)mav.compidcurrent);
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

        private void MavFTPUI_Load(object sender, EventArgs e)
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

                // Register progress listener
                _mavftp.Progress += (message, percent) =>
                {
                    try
                    {
                        if (this.IsDisposed || webBrowser == null) return;
                        this.BeginInvokeIfRequired(() =>
                        {
                            try
                            {
                                webBrowser.Document?.InvokeScript("onTransferProgress", new object[] { message, percent });
                            }
                            catch { }
                        });
                    }
                    catch { }
                };

                string htmlPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "mavftp_explorer.html");
                webBrowser.Navigate(htmlPath);
            }
        }

        // --- JavaScript Bridge APIs ---

        public string GetSystemStatus()
        {
            var status = new
            {
                connected = _mav.BaseStream != null && _mav.BaseStream.IsOpen,
                sysid = _mav.sysidcurrent,
                compid = _mav.compidcurrent
            };
            return JsonConvert.SerializeObject(status);
        }

        public string GetDirectoryContent(string path)
        {
            try
            {
                if (_mav.BaseStream == null || !_mav.BaseStream.IsOpen)
                    return "[]";

                if (string.IsNullOrEmpty(path))
                    path = "/";

                List<MAVFtp.FtpFileInfo> cacheList;
                lock (_mavftp)
                {
                    cacheList = _mavftp.kCmdListDirectory(path, new CancellationTokenSource());
                }

                if (cacheList == null)
                    return "[]";

                var items = cacheList
                    .Where(a => a.Name != "." && a.Name != "..")
                    .Select(a => new
                    {
                        name = a.Name,
                        isDirectory = a.isDirectory,
                        size = a.Size,
                        fullName = a.FullName
                    }).ToList();

                return JsonConvert.SerializeObject(items);
            }
            catch (Exception ex)
            {
                log.Error("GetDirectoryContent error for: " + path, ex);
                return "[]";
            }
        }

        public bool DownloadFile(string remotePath, string filename)
        {
            try
            {
                using (var sfd = new SaveFileDialog())
                {
                    sfd.FileName = filename;
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        using (var cancel = new CancellationTokenSource())
                        {
                            System.IO.MemoryStream ms;
                            lock (_mavftp)
                            {
                                ms = _mavftp.GetFile(remotePath, cancel, false);
                            }
                            File.WriteAllBytes(sfd.FileName, ms.ToArray());
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error("DownloadFile error", ex);
                CustomMessageBox.Show("Download failed: " + ex.Message);
                return false;
            }
        }

        public bool UploadFile(string remoteDir)
        {
            try
            {
                using (var ofd = new OpenFileDialog())
                {
                    ofd.Multiselect = false;
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string remotePath = (remoteDir.EndsWith("/") ? remoteDir : remoteDir + "/") + Path.GetFileName(ofd.FileName);
                        using (var cancel = new CancellationTokenSource())
                        {
                            lock (_mavftp)
                            {
                                _mavftp.UploadFile(remotePath, ofd.FileName, cancel);
                            }
                            
                            // Validate CRC32
                            uint crc = 0;
                            lock (_mavftp)
                            {
                                _mavftp.kCmdCalcFileCRC32(remotePath, ref crc, cancel);
                            }
                            var crcLocal = MAVFtp.crc_crc32(0, File.ReadAllBytes(ofd.FileName));
                            if (crcLocal != crc)
                            {
                                CustomMessageBox.Show("CRC validation failed for file upload!");
                                return false;
                            }
                            return true;
                        }
                    }
                }
                return false;
            }
            catch (Exception ex)
            {
                log.Error("UploadFile error", ex);
                CustomMessageBox.Show("Upload failed: " + ex.Message);
                return false;
            }
        }

        public bool DeleteFile(string remotePath)
        {
            try
            {
                using (var cancel = new CancellationTokenSource())
                {
                    lock (_mavftp)
                    {
                        return _mavftp.kCmdRemoveFile(remotePath, cancel);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("DeleteFile error", ex);
                return false;
            }
        }

        public bool CreateDirectory(string remotePath)
        {
            try
            {
                using (var cancel = new CancellationTokenSource())
                {
                    lock (_mavftp)
                    {
                        return _mavftp.kCmdCreateDirectory(remotePath, cancel);
                    }
                }
            }
            catch (Exception ex)
            {
                log.Error("CreateDirectory error", ex);
                return false;
            }
        }

        public bool RenameFile(string oldPath, string newPath)
        {
            try
            {
                using (var cancel = new CancellationTokenSource())
                {
                    lock (_mavftp)
                    {
                        _mavftp.kCmdRename(oldPath, newPath, cancel);
                    }
                    return true;
                }
            }
            catch (Exception ex)
            {
                log.Error("RenameFile error", ex);
                return false;
            }
        }

        // --- Legacy Designer Stubs to ensure compilation ---

        private void ListView1_DragDrop(object sender, DragEventArgs e) { }
        private void ListView1_ColumnClick(object sender, ColumnClickEventArgs e) { }
        private void DownloadToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void UploadToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void DeleteToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void RenameToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void ListView1_AfterLabelEdit(object sender, LabelEditEventArgs e) { }
        private void NewFolderToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void GetCRC32ToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void ListView1_MouseDown(object sender, MouseEventArgs e) { }
        private void ListView1_DragEnter(object sender, DragEventArgs e) { }
        private void ListView1_MouseDoubleClick(object sender, MouseEventArgs e) { }
        private void DownloadBurstToolStripMenuItem_Click(object sender, EventArgs e) { }
        private void BtnMountFuse_Click(object sender, EventArgs e) { }
        private void TreeView1_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e) { }
    }
}