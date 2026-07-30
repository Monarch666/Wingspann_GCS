using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using MissionPlanner;
using MissionPlanner.Controls;
using ZedGraph;

namespace MissionPlanner.Log
{
    public partial class LogBrowse
    {
        private Panel pnlOdinTopToolbar;
        private Panel pnlOdinSidebar;
        private FlowLayoutPanel pnlOdinTelemetryList;
        private TextBox txtOdinSearch;
        private Panel pnlOdinSummaryCard;

        private void BuildOdinUI()
        {
            // 1. Hide Legacy Controls
            this.BUT_loadlog.Visible = false;
            this.BUT_Graphit.Visible = false;
            this.BUT_cleargraph.Visible = false;
            this.BUT_removeitem.Visible = false;
            this.CMB_preselect.Visible = false;
            
            this.chk_events.Visible = false;
            this.chk_params.Visible = false;
            this.chk_errors.Visible = false;
            this.chk_datagrid.Visible = false;
            this.chk_msg.Visible = false;
            this.chk_mode.Visible = false;
            this.chk_time.Visible = false;
            this.CHK_map.Visible = false;
            
            // Hide the old tree view by collapsing Panel2 where it lives!
            this.splitContainerAllTree.Panel2Collapsed = true;
            this.treeView1.Visible = false; 
            this.splitContainerAllTree.BackColor = Color.FromArgb(13, 17, 23); // Charcoal
            
            // Hide the button panel and datagrid initially
            this.splitContainerButGrid.Panel1Collapsed = true;
            this.splitContainerZgGrid.Panel2Collapsed = true; 

            // 2. Setup Top Toolbar
            pnlOdinTopToolbar = new Panel();
            pnlOdinTopToolbar.Height = 50;
            pnlOdinTopToolbar.Dock = DockStyle.Top;
            pnlOdinTopToolbar.BackColor = Color.FromArgb(20, 26, 32); // Dark Card
            pnlOdinTopToolbar.Padding = new Padding(10);
            this.Controls.Add(pnlOdinTopToolbar);
            this.Controls.SetChildIndex(pnlOdinTopToolbar, 0);

            var btnUpload = CreateOdinButton("Upload Log", Color.FromArgb(118, 255, 3), Color.Black);
            btnUpload.Location = new Point(15, 10);
            btnUpload.Click += (s, e) => { BUT_loadlog_Click(null, null); UpdateOdinSummaryCard(); };
            pnlOdinTopToolbar.Controls.Add(btnUpload);

            var btnDownload = CreateOdinButton("Download via MAVLink", Color.FromArgb(39, 49, 59), Color.White);
            btnDownload.Size = new Size(160, 30);
            btnDownload.Location = new Point(130, 10);
            btnDownload.Click += (s, e) => {
                var downloadForm = new MissionPlanner.Log.LogDownloadMavLink();
                downloadForm.ShowDialog(this);
            };
            pnlOdinTopToolbar.Controls.Add(btnDownload);

            var btnClear = CreateOdinButton("Clear Graph", Color.FromArgb(39, 49, 59), Color.White);
            btnClear.Location = new Point(295, 10);
            btnClear.Click += (s, e) => BUT_cleargraph_Click(null, null);
            pnlOdinTopToolbar.Controls.Add(btnClear);

            var btnMap = CreateOdinButton("Toggle Map", Color.FromArgb(39, 49, 59), Color.White);
            btnMap.Location = new Point(410, 10);
            btnMap.Click += (s, e) => { splitContainerZgMap.Panel2Collapsed = !splitContainerZgMap.Panel2Collapsed; };
            pnlOdinTopToolbar.Controls.Add(btnMap);
            
            var btnGrid = CreateOdinButton("Data Table", Color.FromArgb(39, 49, 59), Color.White);
            btnGrid.Location = new Point(525, 10);
            btnGrid.Click += (s, e) => { splitContainerZgGrid.Panel2Collapsed = !splitContainerZgGrid.Panel2Collapsed; };
            pnlOdinTopToolbar.Controls.Add(btnGrid);

            // 3. Setup Left Sidebar (Telemetry Browser)
            pnlOdinSidebar = new Panel();
            pnlOdinSidebar.Dock = DockStyle.Left;
            pnlOdinSidebar.Width = 300;
            pnlOdinSidebar.BackColor = Color.FromArgb(20, 26, 32);
            this.Controls.Add(pnlOdinSidebar);
            pnlOdinSidebar.BringToFront(); // Ensure it sits correctly alongside TopToolbar

            System.Windows.Forms.Label lblTeleTitle = new System.Windows.Forms.Label { Text = "Telemetry Browser", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 15) };
            pnlOdinSidebar.Controls.Add(lblTeleTitle);

            txtOdinSearch = new TextBox();
            txtOdinSearch.Location = new Point(15, 45);
            txtOdinSearch.Size = new Size(270, 30);
            txtOdinSearch.Font = new Font("Segoe UI", 10);
            txtOdinSearch.BackColor = Color.FromArgb(39, 49, 59);
            txtOdinSearch.ForeColor = Color.White;
            txtOdinSearch.BorderStyle = BorderStyle.FixedSingle;
            txtOdinSearch.Text = "Search signals...";
            txtOdinSearch.GotFocus += (s, e) => { if (txtOdinSearch.Text == "Search signals...") txtOdinSearch.Text = ""; };
            txtOdinSearch.TextChanged += TxtOdinSearch_TextChanged;
            pnlOdinSidebar.Controls.Add(txtOdinSearch);

            pnlOdinTelemetryList = new FlowLayoutPanel();
            pnlOdinTelemetryList.Location = new Point(15, 80);
            pnlOdinTelemetryList.Size = new Size(270, this.Height - 140);
            pnlOdinTelemetryList.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            pnlOdinTelemetryList.AutoScroll = true;
            pnlOdinTelemetryList.FlowDirection = FlowDirection.TopDown;
            pnlOdinTelemetryList.WrapContents = false;
            pnlOdinSidebar.Controls.Add(pnlOdinTelemetryList);

            // 4. Setup Flight Summary Card (Overlay on Graph)
            pnlOdinSummaryCard = new Panel();
            pnlOdinSummaryCard.Size = new Size(250, 200);
            pnlOdinSummaryCard.BackColor = Color.FromArgb(200, 20, 26, 32); // Semi-transparent
            pnlOdinSummaryCard.Location = new Point(this.splitContainerZgMap.Panel1.Width - 270, 20);
            pnlOdinSummaryCard.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            pnlOdinSummaryCard.Visible = false; // Hidden until log loads
            this.splitContainerZgMap.Panel1.Controls.Add(pnlOdinSummaryCard);
            this.splitContainerZgMap.Panel1.Controls.SetChildIndex(pnlOdinSummaryCard, 0);

            // 5. Restyle ZedGraph
            zg1.GraphPane.Fill = new Fill(Color.FromArgb(13, 17, 23));
            zg1.GraphPane.Chart.Fill = new Fill(Color.FromArgb(13, 17, 23));
            zg1.GraphPane.Chart.Border.IsVisible = false;
            zg1.GraphPane.XAxis.MajorGrid.IsVisible = true;
            zg1.GraphPane.YAxis.MajorGrid.IsVisible = true;
            zg1.GraphPane.XAxis.MajorGrid.Color = Color.FromArgb(39, 49, 59);
            zg1.GraphPane.YAxis.MajorGrid.Color = Color.FromArgb(39, 49, 59);
            zg1.GraphPane.XAxis.Color = Color.White;
            zg1.GraphPane.YAxis.Color = Color.White;
            zg1.GraphPane.XAxis.Title.FontSpec.FontColor = Color.White;
            zg1.GraphPane.YAxis.Title.FontSpec.FontColor = Color.White;
            zg1.GraphPane.Title.FontSpec.FontColor = Color.FromArgb(118, 255, 3);
            zg1.GraphPane.Legend.Fill = new Fill(Color.FromArgb(20, 26, 32));
            zg1.GraphPane.Legend.FontSpec.FontColor = Color.White;
            zg1.GraphPane.Legend.Border.Color = Color.FromArgb(39, 49, 59);
            zg1.Invalidate();
        }

        private void TxtOdinSearch_TextChanged(object sender, EventArgs e)
        {
            string query = txtOdinSearch.Text.ToLower();
            if (query == "search signals...") return;

            foreach (Control c in pnlOdinTelemetryList.Controls)
            {
                if (c is Button btn)
                {
                    btn.Visible = string.IsNullOrEmpty(query) || btn.Text.ToLower().Contains(query);
                }
            }
        }

        private void PopulateOdinTelemetryBrowser()
        {
            if (pnlOdinTelemetryList == null) return;
            pnlOdinTelemetryList.Controls.Clear();

            // We read the nodes that were just populated in treeView1 in the background!
            foreach (TreeNode parentNode in treeView1.Nodes)
            {
                // Create a Category Header
                System.Windows.Forms.Label lblCategory = new System.Windows.Forms.Label();
                lblCategory.Text = "▼ " + parentNode.Text;
                lblCategory.Font = new Font("Segoe UI", 10, FontStyle.Bold);
                lblCategory.ForeColor = Color.FromArgb(165, 175, 184); // Secondary text
                lblCategory.AutoSize = true;
                lblCategory.Margin = new Padding(0, 10, 0, 5);
                pnlOdinTelemetryList.Controls.Add(lblCategory);

                foreach (TreeNode childNode in parentNode.Nodes)
                {
                    if (childNode.Nodes.Count > 0)
                    {
                        // Instances (e.g. BAT[0], BAT[1])
                        foreach (TreeNode subChild in childNode.Nodes)
                        {
                            pnlOdinTelemetryList.Controls.Add(CreateTelemetryChip(subChild, $"{parentNode.Text}[{childNode.Text}].{subChild.Text}"));
                        }
                    }
                    else
                    {
                        // Direct fields (e.g. GPS.Alt)
                        pnlOdinTelemetryList.Controls.Add(CreateTelemetryChip(childNode, $"{parentNode.Text}.{childNode.Text}"));
                    }
                }
            }
        }

        private Button CreateTelemetryChip(TreeNode linkedNode, string displayName)
        {
            Button btn = new Button();
            btn.Text = "  □  " + displayName;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Regular);
            btn.ForeColor = Color.White;
            btn.BackColor = Color.FromArgb(20, 26, 32);
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Size = new Size(245, 28);
            btn.TextAlign = ContentAlignment.MiddleLeft;
            btn.Margin = new Padding(5, 1, 5, 1);
            
            btn.Click += (s, e) =>
            {
                // Toggle the hidden treeview node programmatically!
                linkedNode.Checked = !linkedNode.Checked;
                btn.Text = linkedNode.Checked ? "  ✔  " + displayName : "  □  " + displayName;
                btn.ForeColor = linkedNode.Checked ? Color.FromArgb(118, 255, 3) : Color.White;
                
                // Manually fire the TreeView AfterCheck event so LogBrowse backend plots it!
                treeView1_AfterCheck(treeView1, new TreeViewEventArgs(linkedNode, TreeViewAction.ByMouse));
                
                // Force graph redraw styling
                foreach(var curve in zg1.GraphPane.CurveList) {
                    if (curve is LineItem li) {
                        li.Line.Width = 2.0f;
                        li.Symbol.Size = 4.0f;
                    }
                }
            };
            
            return btn;
        }

        private void UpdateOdinSummaryCard()
        {
            if (pnlOdinSummaryCard == null) return;
            pnlOdinSummaryCard.Visible = true;
            pnlOdinSummaryCard.Controls.Clear();

            System.Windows.Forms.Label title = new System.Windows.Forms.Label { Text = "Flight Summary", Font = new Font("Segoe UI", 12, FontStyle.Bold), ForeColor = Color.FromArgb(118, 255, 3), AutoSize = true, Location = new Point(15, 15) };
            pnlOdinSummaryCard.Controls.Add(title);

            // We can extract rough metrics by looking at log format or data bounds if we want to get fancy,
            // but for now we'll just show the Log file path and a placeholder or basic stats.
            System.Windows.Forms.Label lblFile = new System.Windows.Forms.Label { Text = "File: " + System.IO.Path.GetFileName(this.Text), Font = new Font("Segoe UI", 9), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 45), MaximumSize = new Size(220, 0) };
            pnlOdinSummaryCard.Controls.Add(lblFile);

            System.Windows.Forms.Label lblLines = new System.Windows.Forms.Label { Text = "Data Rows: " + dataGridView1.RowCount, Font = new Font("Segoe UI", 9), ForeColor = Color.White, AutoSize = true, Location = new Point(15, 80) };
            pnlOdinSummaryCard.Controls.Add(lblLines);
        }

        private Button CreateOdinButton(string text, Color bg, Color fg)
        {
            Button btn = new Button();
            btn.Text = text;
            btn.BackColor = bg;
            btn.ForeColor = fg;
            btn.FlatStyle = FlatStyle.Flat;
            btn.FlatAppearance.BorderSize = 0;
            btn.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            btn.Size = new Size(110, 30);
            btn.Cursor = Cursors.Hand;
            return btn;
        }
    }
}
