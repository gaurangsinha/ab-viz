using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;

namespace ab_viz {

    /// <summary>
    /// Main Form
    /// </summary>
    public partial class frmMain : Form {

        private static readonly string SEPARATOR_LINE = Environment.NewLine + "--------------------------------------------------------------------------------" + Environment.NewLine;
        private ApacheBench _ApacheBench = null;
        private string[] _Summaries = null;
        private int _RepeatIndex = 0;
        private bool _IsCanceled = false;

        /// <summary>
        /// Initializes a new instance of the <see cref="frmMain" /> class.
        /// </summary>
        public frmMain() {
            InitializeComponent();
            chkSeries.ItemCheck += chkSeriesTypes_ItemCheck; //chkSeries_ItemCheck;
            chkSeriesTypes.ItemCheck += chkSeriesTypes_ItemCheck;
        }

        /// <summary>
        /// Handles the Load event of the frmMain control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void frmMain_Load(object sender, EventArgs e) {
            //Check ApacheBench is present            
            if (ApacheBench.IsApacheBenchPresent()) {
                string version = ApacheBench.GetVersion();
                lblToolStripStatus.Text = "ApacheBench Found " + version;
            }
            else {
                MessageBox.Show("Cannot find ApacheBench. Please place ApacheBench 'ab.exe' in the same folder as this application", "Not Found", MessageBoxButtons.OK, MessageBoxIcon.Stop);
                lblToolStripStatus.Text = "ApacheBench NOT FOUND!";
            }            

            //Enumerate all network interfaces
            var interfaces = Program.NetWorkInterfaces();
            foreach (var nic in interfaces) {
                cboAddress.Items.Add(nic);
            }
        }

        /// <summary>
        /// Gets the arguments.
        /// </summary>
        /// <returns></returns>
        private List<KeyValuePair<string, string>> GetArguments() {
            var arguments = new List<KeyValuePair<string, string>>();

            //-n requests     Number of requests to perform
            AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_REQUESTS, ((int)numRequests.Value).ToString());
            
            //-c concurrency  Number of multiple requests to make
            AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_CONCURRENCY, ((int)numConcurrency.Value).ToString());

            //-t timelimit    Seconds to max. wait for responses
            if (0 != numTimelimit.Value) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_TIMELIMIT, ((int)numTimelimit.Value).ToString());
            }

            //-b windowsize   Size of TCP send/receive buffer, in bytes
            if (0 != numWindowSize.Value) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_WINDOWSIZE, ((int)numWindowSize.Value).ToString());
            }

            //-B address      Address to bind to when making outgoing connections
            if (null != cboAddress.SelectedItem) {
                string addr = ((KeyValuePair<string, string>)cboAddress.SelectedItem).Key;
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_ADDRESS, addr);
            }

            //-p postfile     File containing data to POST. Remember also to set -T
            if (!string.IsNullOrEmpty(txtPostfile.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_POSTFILE, "\"" + txtPostfile.Text + "\"");
            }

            //-u putfile      File containing data to PUT. Remember also to set -T
            if (!string.IsNullOrEmpty(txtPutFile.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_PUTFILE, "\"" + txtPutFile.Text + "\"");
            }

            //-T content-type Content-type header for POSTing
            if (!string.IsNullOrEmpty(txtContentType.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_CONTENT_TYPE, txtContentType.Text);
            }

            //-C attribute    Add cookie, eg. 'Apache=1234'. (repeatable)
            if (!string.IsNullOrEmpty(txtCookies.Text)) {
                string[] parts = txtCookies.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) {
                    AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_COOKIE, p);
                }
            }

            //-H attribute    Add Arbitrary header line
            if (!string.IsNullOrEmpty(txtHeaders.Text)) {
                string[] parts = txtHeaders.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts) {
                    AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_HEADER, p);
                }
            }

            //-A attribute    Add Basic WWW Authentication, the attributes are a colon separated username and password.
            if (!string.IsNullOrEmpty(txtAuthenticate.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_AUTHENTICATE, txtAuthenticate.Text);
            }

            //-X
            if (!string.IsNullOrEmpty(txtProxyServer.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_PROXY_SERVER, txtProxyServer.Text);
            }

            //-P
            if (!string.IsNullOrEmpty(txtProxyAuthenticate.Text)) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_PROXY_AUTHENTICATE, txtProxyAuthenticate.Text);
            }

            //-k
            if (chkKeepAlive.Checked) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_KEEP_ALIVE, null);
            }

            //-i
            if (chkUseHEAD.Checked) {
                AddArgument(arguments, ApacheBench.APACHE_BENCH_FLAG_USE_HEAD, null);
            }

            return arguments;
        }

        /// <summary>
        /// Adds the argument.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        private void AddArgument(List<KeyValuePair<string, string>> args, string key, string value) {
            args.Add(new KeyValuePair<string, string>(key, value));
        }

        /// <summary>
        /// Handles the Click event of the btnStart control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnStart_Click(object sender, EventArgs e) {
            txtSummary.Text = string.Empty;
            _Summaries = new string[(int)numRepeat.Value];
            
            _IsCanceled = false;
            
            chkSeries.Items.Clear();
            chartRequests.Series.Clear();
            chartRequestDistribution.Series.Clear();
            chartPercentageSummary.Series.Clear();
            for (int i = 0; i < numRepeat.Value; i++) {
                string run = "Run" + (i + 1).ToString();
                chartPercentageSummary.Series.Add(new Series() {
                    Name = run,
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                    BorderWidth = 2,
                    Legend = "Legend1"
                });

                chartRequests.Series.Add(new Series() {
                    Name = run,
                    ChartType = SeriesChartType.Point,
                    BorderWidth = 1,
                    Legend = "Legend1"
                });

                chartRequestDistribution.Series.Add(new Series() {
                    Name = run + "_ctime",
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                    BorderWidth = 2,
                    Legend = "Legend1"
                });

                chartRequestDistribution.Series.Add(new Series() {
                    Name = run + "_dtime",
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                    BorderWidth = 2,
                    Legend = "Legend1"
                });

                chartRequestDistribution.Series.Add(new Series() {
                    Name = run + "_ttime",
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                    BorderWidth = 2,
                    Legend = "Legend1"
                });

                chartRequestDistribution.Series.Add(new Series() {
                    Name = run + "_wait",
                    ChartType = System.Windows.Forms.DataVisualization.Charting.SeriesChartType.Line,
                    BorderWidth = 2,
                    Legend = "Legend1"
                });

                chkSeries.Items.Add(run, true);
            }
            //Initialize chkSeriesType to checked.
            for (int i = 0; i < chkSeriesTypes.Items.Count; i++) {
                chkSeriesTypes.SetItemChecked(i, true);
            }

            _RepeatIndex = 0;            

            ToggleControls();
            StartBenchmark();
        }

        private void StartBenchmark() {
            if (!string.IsNullOrEmpty(txtUrl.Text) && Uri.IsWellFormedUriString(txtUrl.Text, UriKind.Absolute)) {
                var arguments = GetArguments();

                if (null != arguments) {                    
                    _ApacheBench = new ApacheBench(txtUrl.Text, arguments);
                    _ApacheBench.InProgress += ApacheBench_InProgress;
                    _ApacheBench.DataReceived += ApacheBench_DataReceived;
                    _ApacheBench.Completed += ApacheBench_Completed;

                    lblToolStripStatus.Text = string.Format("Started Run {0} ...", _RepeatIndex + 1);
                    pbToolStripProgressBar.Value = 0;
                    btnCancel.Visible = true;
                    if (!_ApacheBench.Start()) {
                        MessageBox.Show("Error starting 'ab.exe'", "Error Starting", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            else {
                txtUrl.Focus();
                txtUrl.SelectionStart = 0;
                txtUrl.SelectionLength = txtUrl.Text.Length;
                //toolTip1.ToolTipTitle = "Please enter valid URL";
                toolTip1.Show("Please enter valid URL", txtUrl, 3000);
                ToggleControls();
            }
        }

        /// <summary>
        /// Handles DataReceived event
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void ApacheBench_DataReceived(object sender, string e) {
            this.Invoke(new MethodInvoker(() => {
                _Summaries[_RepeatIndex] = _ApacheBench.StandardOutput;
                txtSummary.Text = string.Join(SEPARATOR_LINE, _Summaries);
            }));
        }

        /// <summary>
        /// Handles InProgress event for ApacheBench
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The data.</param>
        private void ApacheBench_InProgress(object sender, int e) {
            this.Invoke(new MethodInvoker(() => {
                pbToolStripProgressBar.Value = (e > 0 && e < 100) ? e : pbToolStripProgressBar.Value;
                lblToolStripStatus.Text = string.Format("Run [{0}] {1}", _RepeatIndex + 1, GetLastLine(_ApacheBench.StandardError));
            }));
        }

        /// <summary>
        /// Handles the Completed event
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void ApacheBench_Completed(object sender, EventArgs e) {
            this.Invoke(new MethodInvoker(() => {                
                _Summaries[_RepeatIndex] = _ApacheBench.StandardOutput;
                txtSummary.Text = string.Join(SEPARATOR_LINE, _Summaries);
                ProcessSummaryData("Run" + numRequests.ToString());
                _RepeatIndex++;
                if (_RepeatIndex < numRepeat.Value && !_IsCanceled) {
                    StartBenchmark();
                }
                else {
                    ToggleControls();
                    lblToolStripStatus.Text = (!_IsCanceled) ? "Completed Benchmark" : "Benchmark Cancelled";
                    btnCancel.Visible = false;
                }
            }));
        }

        /// <summary>
        /// Processes the summary data.
        /// </summary>
        private void ProcessSummaryData(string seriesName) {
            if (File.Exists(ApacheBench.DEFAULT_PERCENTAGE_FILE)) {
                string[] fileData = File.ReadAllLines(ApacheBench.DEFAULT_PERCENTAGE_FILE);
                string last = string.Empty;
                for (int i = 2; i < 101; i++) {
                    string[] parts = fileData[i].Split(',');
                    chartPercentageSummary.Series[_RepeatIndex].Points.AddXY(parts[0], parts[1]);
                    last = parts[1];
                }
                chartPercentageSummary.Series[_RepeatIndex].Points.AddXY("100", last);
            }

            if (File.Exists(ApacheBench.DEFAULT_GNUPLOT_FILE)) {
                int req = 1;
                using (var sr = new StreamReader(ApacheBench.DEFAULT_GNUPLOT_FILE)) {
                    sr.ReadLine();  //Skip header line
                    var run = "Run" + (_RepeatIndex + 1).ToString();
                    while (!sr.EndOfStream) {
                        var line = sr.ReadLine();
                        var parts = line.Split('\t');

                        chartRequests.Series[run].Points.AddXY(long.Parse(parts[1].Trim()), long.Parse(parts[4].Trim()));

                        chartRequestDistribution.Series[run + "_ctime"].Points.AddXY(req, parts[2]);
                        chartRequestDistribution.Series[run + "_dtime"].Points.AddXY(req, parts[3]);
                        chartRequestDistribution.Series[run + "_ttime"].Points.AddXY(req, parts[4]);
                        chartRequestDistribution.Series[run + "_wait"].Points.AddXY(req, parts[5]);

                        req++;
                    }
                    chartRequests.Series[run].Sort(PointSortOrder.Ascending, "X");
                }
            }
        }

        /// <summary>
        /// Toggles the controls.
        /// </summary>
        private void ToggleControls() {
            grpOptions.Enabled = !grpOptions.Enabled;
            pbToolStripProgressBar.Visible = !pbToolStripProgressBar.Visible;
        }

        /// <summary>
        /// Handles the Click event of the toolStripStatusLabel1 control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void lblApacheBenchDocLink_Click(object sender, EventArgs e) {
            Process.Start((string)lblApacheBenchDocLink.Tag);
        }

        /// <summary>
        /// Gets the last line.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        private string GetLastLine(string text) {
            if (!string.IsNullOrEmpty(text)) {
                var lines = text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                return lines.Length > 0 ? lines[lines.Length - 1] : text;
            }
            return text;
        }

        /// <summary>
        /// Handles the MouseMove event of the chartPercentageSummary control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        void chartPercentageSummary_MouseMove(object sender, System.Windows.Forms.MouseEventArgs e) {
            // Call HitTest
            HitTestResult result = chartPercentageSummary.HitTest(e.X, e.Y);

            // Reset Data Point Attributes
            foreach (DataPoint point in chartPercentageSummary.Series[0].Points) {
                point.BackSecondaryColor = Color.Black;
                point.BackHatchStyle = ChartHatchStyle.None;
                point.BorderWidth = 1;
            }

            // If the mouse if over a data point
            if (result.ChartElementType == ChartElementType.DataPoint) {
                // Find selected data point
                DataPoint point = chartPercentageSummary.Series[0].Points[result.PointIndex];

                // Change the appearance of the data point
                point.BackSecondaryColor = Color.White;
                point.BackHatchStyle = ChartHatchStyle.Percent25;
                point.BorderWidth = 4;
            }
            else {
                // Set default cursor
                this.Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSelectPostFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnSelectPostFile_Click(object sender, EventArgs e) {
            var result = openFileDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(openFileDialog1.FileName)) {
                txtPostfile.Text = openFileDialog1.FileName;
            }
        }

        /// <summary>
        /// Handles the Click event of the btnSelectPutFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnSelectPutFile_Click(object sender, EventArgs e) {
            var result = openFileDialog1.ShowDialog();
            if (!string.IsNullOrEmpty(openFileDialog1.FileName)) {
                txtPutFile.Text = openFileDialog1.FileName;
            }
        }

        /// <summary>
        /// Handles the ItemCheck event of the chkSeries control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemCheckEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void chkSeries_ItemCheck(object sender, ItemCheckEventArgs e) {
            //Get list of series to show
            bool[] enabled = Enumerable.Repeat<bool>(false, chkSeries.Items.Count).ToArray();
            for (int i = 0; i < chkSeries.CheckedIndices.Count; i++) {
                enabled[chkSeries.CheckedIndices[i]] = true;
            }
            enabled[e.Index] = CheckState.Checked == e.NewValue;

            //Set series visibility
            txtSummary.Text = string.Empty;
            for (int i = 0; i < chkSeries.Items.Count; i++) {
                var run = "Run" + (i + 1).ToString();
                chartPercentageSummary.Series[run].Enabled = enabled[i];
                foreach (var ser in chartRequestDistribution.Series.Where(s => s.Name.Contains(run))) {
                    ser.Enabled = enabled[i];
                }
                if(enabled[i]) {
                    txtSummary.Text += _Summaries[i] + SEPARATOR_LINE;
                }
            }

            for (int i = 0; i < chkSeriesTypes.Items.Count; i++) {
                if (!chkSeriesTypes.GetItemChecked(i)) {
                    chkSeriesTypes.SetItemCheckState(i, CheckState.Indeterminate);
                }
            }
        }

        /// <summary>
        /// Handles the ItemCheck event of the chkSeriesTypes control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ItemCheckEventArgs" /> instance containing the event data.</param>
        /// <exception cref="System.NotImplementedException"></exception>
        private void chkSeriesTypes_ItemCheck(object sender, ItemCheckEventArgs e) {
            bool[] series = Enumerable.Repeat<bool>(false, chkSeries.Items.Count).ToArray();
            for (int i = 0; i < chkSeries.CheckedIndices.Count; i++) {
                series[chkSeries.CheckedIndices[i]] = true;
            }
            List<string> types = chkSeriesTypes.CheckedItems.Cast<string>().ToList();

            if (((CheckedListBox)sender).Name == chkSeries.Name) {
                series[e.Index] = CheckState.Checked == e.NewValue;
            }
            else if (((CheckedListBox)sender).Name == chkSeriesTypes.Name) {
                if (CheckState.Checked == e.NewValue) {
                    types.Add((string)chkSeriesTypes.Items[e.Index]);
                }
                else if (CheckState.Unchecked == e.NewValue) {
                    types.Remove((string)chkSeriesTypes.Items[e.Index]);
                }
            }
            
            RefreshSummaryData(series, types);
        }

        /// <summary>
        /// Refreshes the summary data.
        /// </summary>
        /// <param name="series">The series.</param>
        /// <param name="types">The types.</param>
        private void RefreshSummaryData(bool[] series, List<string> types) {
            //Set series visibility
            txtSummary.Text = string.Empty;
            for (int i = 0; i < chkSeries.Items.Count; i++) {
                string runStr = "Run" + (i + 1).ToString();
                chartPercentageSummary.Series[i].Enabled = series[i];
                foreach (var ser in chartRequestDistribution.Series.Where(s => s.Name.Contains(runStr))) {
                    ser.Enabled = series[i] & types.Contains(ser.Name.Replace(runStr + "_", string.Empty));
                }
                if (series[i]) {
                    txtSummary.Text += string.Format("{0}{1}{0}{2}", SEPARATOR_LINE, runStr, _Summaries[i]);
                }
            }
        }

        /// <summary>
        /// Handles the ButtonClick event of the btnCancel control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void btnCancel_ButtonClick(object sender, EventArgs e) {
            _IsCanceled = true;
            _ApacheBench.Cancel();
        }
    }
}
