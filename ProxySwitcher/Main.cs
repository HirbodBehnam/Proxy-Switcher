using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.Win32;

namespace ProxySwitcher
{
    public partial class Main : Form
    {
        [DllImport("wininet.dll")]
        public static extern bool InternetSetOption(IntPtr hInternet, int dwOption, IntPtr lpBuffer, int dwBufferLength);
        public const int INTERNET_OPTION_SETTINGS_CHANGED = 39;
        public const int INTERNET_OPTION_REFRESH = 37;
        /// <summary>
        /// Some local addresses must not be passed through proxy.
        /// List from shadowsocks
        /// </summary>
        public const string BypassRules = "localhost;127.*;10.*;172.16.*;172.17.*;172.18.*;172.19.*;172.20.*;172.21.*;172.22.*;172.23.*;172.24.*;172.25.*;172.26.*;172.27.*;172.28.*;172.29.*;172.30.*;172.31.*;192.168.*;<local>";

        private const string KeyName =
            "HKEY_CURRENT_USER\\Software\\Microsoft\\Windows\\CurrentVersion\\Internet Settings";
        /// <summary>
        /// Sets the global proxy for the computer
        /// </summary>
        /// <remarks>
        /// https://stackoverflow.com/a/26273084/4213397
        /// </remarks>
        /// <param name="proxyHost">The proxy host and port</param>
        /// <param name="proxyEnabled">Enable the proxy?</param>
        /// <param name="overrideSettings"></param>
        private static void SetProxy(string proxyHost, bool proxyEnabled, string overrideSettings)
        {
            Registry.SetValue(KeyName, "ProxyServer", proxyHost);
            Registry.SetValue(KeyName, "ProxyEnable", proxyEnabled ? 1 : 0);
            Registry.SetValue(KeyName, "ProxyOverride", overrideSettings);

            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_SETTINGS_CHANGED, IntPtr.Zero, 0);
            InternetSetOption(IntPtr.Zero, INTERNET_OPTION_REFRESH, IntPtr.Zero, 0);
        }

        private readonly string _defaultProxyHost;
        private readonly string _defaultProxyOverride;
        public Main()
        {
            InitializeComponent();
            // Get default proxy settings
            _defaultProxyHost = (string)Registry.GetValue(KeyName, "ProxyServer", "");
            _defaultProxyOverride = (string)Registry.GetValue(KeyName, "ProxyOverride", "");
            notifyIconContextMenu.Items.Insert(0,new ToolStripMenuItem("Default", null, (sender, args) 
                => SetProxy(_defaultProxyHost, false, _defaultProxyOverride)));
            // Load settings
            var list = Properties.Settings.Default.ProxyList;
            foreach (string proxy in list)
            {
                proxyList.Items.Add(proxy);
                notifyIconContextMenu.Items.Insert(notifyIconContextMenu.Items.Count - 1, new ToolStripMenuItem(proxy, null, (sender, args) => 
                    SetProxy(proxy, true, BypassRules)));
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e) =>
            Application.Exit();

        private void addButton_Click(object sender, EventArgs e)
        {
            var dialog = new AddProxy();
            dialog.ShowDialog(this);
            if (dialog.Proxy != "")
            {
                proxyList.Items.Add(dialog.Proxy);
                notifyIconContextMenu.Items.Insert(notifyIconContextMenu.Items.Count - 2, new ToolStripMenuItem(dialog.Proxy, null, (s, args) => 
                    SetProxy(dialog.Proxy, true, BypassRules)));
                Properties.Settings.Default.ProxyList.Add(dialog.Proxy);
                Properties.Settings.Default.Save();
            }
        }

        private void deleteButton_Click(object sender, EventArgs e)
        {
            if (proxyList.SelectedItem != null)
            {
                var res = MessageBox.Show($"Are you sure you want to delete {proxyList.SelectedItem}?", "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res == DialogResult.Yes)
                {
                    proxyList.Items.RemoveAt(proxyList.SelectedIndex);
                    notifyIconContextMenu.Items.RemoveAt(proxyList.SelectedIndex + 1); // +1 is because of default value at top
                    Properties.Settings.Default.ProxyList.RemoveAt(proxyList.SelectedIndex);
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e) =>
            SetProxy(_defaultProxyHost, false, _defaultProxyOverride);

        private void notifyIcon_DoubleClick(object sender, EventArgs e) => Show();

        private void Main_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                if (Properties.Settings.Default.FirstMinimize)
                {
                    notifyIcon.BalloonTipTitle = "Proxy Switcher is in taskbar!";
                    notifyIcon.BalloonTipText = "Double click on icon to open the form again.";
                    notifyIcon.BalloonTipIcon = ToolTipIcon.Info;
                    notifyIcon.ShowBalloonTip(500);
                    Properties.Settings.Default.FirstMinimize = false;
                    Properties.Settings.Default.Save();
                }
                Hide();
            }
        }
    }
}
