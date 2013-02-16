using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ab_viz {
    static class Program {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main() {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new frmMain());
        }

        /// <summary>
        /// Gets the Network interfaces.
        /// </summary>
        /// <returns></returns>
        public static KeyValuePair<string, string>[] NetWorkInterfaces() {
            var interfaces = new List<KeyValuePair<string, string>>();
            foreach (NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()) {
                if (null != nic.GetIPProperties().UnicastAddresses && nic.GetIPProperties().UnicastAddresses.Count > 0) {
                    string ip = nic.GetIPProperties().UnicastAddresses[0].Address.ToString();
                    interfaces.Add(new KeyValuePair<string, string>(ip, nic.Name));
                }
            }
            return interfaces.ToArray();
        }
    }
}
