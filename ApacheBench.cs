using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ab_viz {

    /// <summary>
    /// Apache Bench
    /// </summary>
    public class ApacheBench : IDisposable {

        public static readonly string APACHE_BENCH_EXE = "ab.exe";
        public static readonly string APACHE_BENCH_FLAG_VERSION = "-V";
        public static readonly string APACHE_BENCH_FLAG_REQUESTS = "-n";
        public static readonly string APACHE_BENCH_FLAG_CONCURRENCY = "-c";
        public static readonly string APACHE_BENCH_FLAG_TIMELIMIT = "-t";
        public static readonly string APACHE_BENCH_FLAG_WINDOWSIZE = "-b";
        public static readonly string APACHE_BENCH_FLAG_ADDRESS = "-B";
        public static readonly string APACHE_BENCH_FLAG_POSTFILE = "-p";
        public static readonly string APACHE_BENCH_FLAG_PUTFILE = "-u";
        public static readonly string APACHE_BENCH_FLAG_CONTENT_TYPE = "-T";
        public static readonly string APACHE_BENCH_FLAG_COOKIE = "-C";
        public static readonly string APACHE_BENCH_FLAG_HEADER = "-H";

        public static readonly string APACHE_BENCH_FLAG_GNUPLOT = "-g";
        public static readonly string DEFAULT_GNUPLOT_FILE = "graph.tsv";

        public static readonly string APACHE_BENCH_FLAG_PERCENTAGE_FILE = "-e";
        public static readonly string DEFAULT_PERCENTAGE_FILE = "summary.csv";

        #region Public Properties
        private string _URL = null;
        /// <summary>
        /// Gets the URL.
        /// </summary>
        /// <value>
        /// The URL.
        /// </value>
        public string URL {
            get { 
                return _URL; 
            }
        }


        private List<KeyValuePair<string, string>> _Arguments = null;
        /// <summary>
        /// Gets the arguments.
        /// </summary>
        /// <value>
        /// The arguments.
        /// </value>
        public List<KeyValuePair<string, string>> Arguments {
            get { 
                return _Arguments; 
            }
        }

        /// <summary>
        /// Process object
        /// </summary>
        private Process _Process;

        /// <summary>
        /// Process parameters object
        /// </summary>
        private ProcessStartInfo _ProcessStartInfo;

        private string _StandardOutput = string.Empty;
        /// <summary>
        /// Gets the standard output.
        /// </summary>
        /// <value>
        /// The standard output.
        /// </value>
        public string StandardOutput {
            get { 
                return _StandardOutput; 
            }
        }

        private string _StandardError = string.Empty;
        /// <summary>
        /// Gets the standard error.
        /// </summary>
        /// <value>
        /// The standard error.
        /// </value>
        public string StandardError {
            get { 
                return _StandardError; 
            }
        }

        private bool _ProcessExited = false;
        /// <summary>
        /// Gets a value indicating whether this instance has exited.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has exited; otherwise, <c>false</c>.
        /// </value>
        public bool HasExited {
            get {
                return (null != _Process) ? _Process.HasExited & _ProcessExited : false;
            }
        }

        private int _CompletedPercentage = 0;
        /// <summary>
        /// Gets the completed percentage.
        /// </summary>
        /// <value>
        /// The completed percentage.
        /// </value>
        public int CompletedPercentage {
            get {
                return _CompletedPercentage;
            }
        }
        #endregion

        #region Public Events

        /// <summary>
        /// Send the progress percentage
        /// </summary>
        public event EventHandler<int> InProgress;

        /// <summary>
        /// Sends the new data that has been received on standard output
        /// </summary>
        public event EventHandler<string> DataReceived;

        /// <summary>
        /// Invoked when process has completed execution
        /// </summary>
        public event EventHandler Completed;

        #endregion

        /// <summary>
        /// Monitors Standard Output stream
        /// </summary>
        private Task _UpdateStandardOutput = null;

        /// <summary>
        /// Monitors Standard Error stream
        /// </summary>
        private Task _UpdateStandardError = null;

        /// <summary>
        /// Cancellation token for monitoring tasks
        /// </summary>
        private CancellationTokenSource _CancellationToken = null;

        /// <summary>
        /// Gets the num of requests.
        /// </summary>
        /// <value>
        /// The num of requests.
        /// </value>
        public int NumOfRequests {
            get {
                int req = 0;
                var arg = GetValueFromArguments(_Arguments, APACHE_BENCH_FLAG_REQUESTS);
                if (null != arg) {
                    int.TryParse(arg, out req);
                }
                return req;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ApacheBench" /> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="arguments">The arguments.</param>
        public ApacheBench(string url, params KeyValuePair<string, string>[] arguments) 
            : this (url, new List<KeyValuePair<string,string>>(arguments)) { }
        
        /// <summary>
        /// Initializes a new instance of the <see cref="ApacheBench" /> class.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="arguments">The arguments.</param>
        public ApacheBench(string url, List<KeyValuePair<string, string>> arguments) {
            this._URL = null != url ? FormatURL(url) : url;
            this._Arguments = arguments ?? new List<KeyValuePair<string,string>>();
        }

        /// <summary>
        /// Creates the process start info object.
        /// </summary>
        private void CreateProcessStartInfo() {
            if (!string.IsNullOrEmpty(this._URL)) {
                //Add gnuplot flag
                _Arguments.Add(new KeyValuePair<string, string>(APACHE_BENCH_FLAG_GNUPLOT, DEFAULT_GNUPLOT_FILE));
                //Add percentage csv flag
                _Arguments.Add(new KeyValuePair<string, string>(APACHE_BENCH_FLAG_PERCENTAGE_FILE, DEFAULT_PERCENTAGE_FILE));
            }

            this._ProcessStartInfo = new ProcessStartInfo(APACHE_BENCH_EXE) {                
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                Arguments = string.Format(" {0} {1} ",
                    string.Join(" ", this._Arguments.ConvertAll<string>(kv => kv.Key + (!string.IsNullOrEmpty(kv.Value) ? " " + kv.Value : string.Empty))),
                    this._URL)
            };
        }

        /// <summary>
        /// Starts this instance.
        /// </summary>
        public bool Start() {
            this.CleanUp();
            this.CreateProcessStartInfo();
            this._Process = new Process();
            this._Process.EnableRaisingEvents = true;
            this._Process.Exited += _Process_Exited;
            //this._Process.OutputDataReceived += this._Process_OutputDataReceived;
            //this._Process.ErrorDataReceived += this._Process_ErrorDataReceived;            
            this._Process.StartInfo = this._ProcessStartInfo;
            bool status = this._Process.Start();
            this._UpdateStandardOutput = Task.Factory.StartNew(UpdateStandardOutput);
            this._CancellationToken = new CancellationTokenSource();
            this._UpdateStandardError = Task.Factory.StartNew(UpdateStandardError, _CancellationToken.Token);
            return status;
        }

        /// <summary>
        /// Handles the Exited event of the _Process control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs" /> instance containing the event data.</param>
        private void _Process_Exited(object sender, EventArgs e) {
            if(null != this._UpdateStandardOutput && TaskStatus.Running == this._UpdateStandardOutput.Status) {
                this._UpdateStandardOutput.Wait(1000);
            }
            if (null != this._Process && !this._Process.StandardOutput.EndOfStream) {
                this._StandardOutput += this._Process.StandardOutput.ReadToEnd();
            }

            if (null != this._UpdateStandardError && TaskStatus.Running == this._UpdateStandardError.Status) {
                this._UpdateStandardOutput.Wait(1000);
            }
            if (null != this._Process && !this._Process.StandardError.EndOfStream) {
                this._StandardError += this._Process.StandardError.ReadToEnd();
            }
        }

        /// <summary>
        /// Waits for exit.
        /// </summary>
        public void WaitForExit() {
            if (null != this._Process) {
                this._Process.WaitForExit();
            }
        }

        /// <summary>
        /// Updates the Standard Output
        /// </summary>
        private void UpdateStandardOutput() {
            while (!this._Process.HasExited) {
                string line = this._Process.StandardOutput.ReadLine();
                this._StandardOutput += line + Environment.NewLine;
                if (null != this.DataReceived) {
                    this.DataReceived(this, line);
                }
            }

            if (null != _CancellationToken) {
                _CancellationToken.Cancel();
            }
            
            if (null != _Process && null != _Process.StandardOutput && !_Process.StandardOutput.EndOfStream) {
                this._StandardOutput += this._Process.StandardOutput.ReadToEnd();
            }

            if (null != this.Completed) {
                this.Completed(this, new EventArgs());
            }

            this._ProcessExited = true;
        }

        /// <summary>
        /// Updates the standard error.
        /// </summary>
        private void UpdateStandardError() {
            while (!this._Process.HasExited) {
                try {
                    int percentage = 0;
                    string line = this._Process.StandardError.ReadLine();
                    this._StandardError += line + Environment.NewLine;
                    if (null != line) {
                        var number = Regex.Match(line, @"\d+").Value;
                        percentage = (int)((double.Parse(number) / (double)this.NumOfRequests) * (double)100);
                    }
                    if (null != this.InProgress) {
                        this.InProgress(this, percentage);
                    }
                }
                catch (TaskCanceledException) { }
                catch { }
            }
        }

        public void Cancel() {
            if (null != _Process && false == _Process.HasExited) {
                _Process.Kill();
            }
        }

        /// <summary>
        /// Runs the ApacheBench in sync.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <param name="arguments">The arguments.</param>
        /// <returns>The data on StandardOutput</returns>
        public static string RunSync(string url, params KeyValuePair<string, string>[] arguments) {
            string output = string.Empty;
            using (var ab = new ApacheBench(url, arguments)) {
                if (ab.Start()) {
                    while (!ab.HasExited) ;
                    output = ab.StandardOutput;
                }
            }
            return output;
        }

        /// <summary>
        /// Determines whether [is apache bench present].
        /// </summary>
        /// <returns>
        ///   <c>true</c> if [is apache bench present]; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsApacheBenchPresent() {
            return File.Exists(APACHE_BENCH_EXE);
        }

        /// <summary>
        /// Gets the version.
        /// </summary>
        /// <returns></returns>
        public static string GetVersion() {
            string version = null;
            if (IsApacheBenchPresent()) {
                try {
                    var output = ApacheBench.RunSync(null, new KeyValuePair<string, string>(APACHE_BENCH_FLAG_VERSION, null));
                    if (!string.IsNullOrEmpty(output)) {
                        var lines = output.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
                        version = lines[0].Split(',')[1].Trim();
                    }
                }
                catch { 
                    //Log error
                }
            }
            return version;
        }

        /// <summary>
        /// Formats the URL.
        /// </summary>
        /// <param name="url">The URL.</param>
        /// <returns></returns>
        public static string FormatURL(string url) {
            Uri address = new Uri(url, UriKind.Absolute);
            return address.AbsoluteUri;
        }

        /// <summary>
        /// Gets the value from arguments.
        /// </summary>
        /// <param name="args">The args.</param>
        /// <param name="key">The key.</param>
        /// <returns></returns>
        public static string GetValueFromArguments(List<KeyValuePair<string, string>> args, string key) {
            foreach (var kv in args) {
                if (kv.Key == key) {
                    return kv.Value;
                }
            }
            return null;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose() {
            CleanUp();
        }

        /// <summary>
        /// Frees resources allocated to this instance.
        /// </summary>
        private void CleanUp() {
            if (null != _UpdateStandardOutput) {
                _UpdateStandardOutput.Dispose();
                _UpdateStandardOutput = null;
            }

            if (null != _UpdateStandardError) {
                _UpdateStandardError.Dispose();
                _UpdateStandardError = null;
            }

            if (null != _Process) {
                _Process.Dispose();
                _Process = null;
            }
        }
    }
}
