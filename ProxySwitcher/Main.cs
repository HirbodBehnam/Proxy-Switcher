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
        /// <summary>
        /// True when the gui must not be shown when starting the app
        /// </summary>
        public bool NoGuiStart;
        /// <summary>
        /// The proxy of system when the application has started
        /// </summary>
        private readonly string _defaultProxyHost;
        /// <summary>
        /// The bypass rules when the application has started
        /// </summary>
        private readonly string _defaultProxyOverride;
        /// <summary>
        /// Is the proxy on when the application has started?
        /// </summary>
        private readonly bool _defaultProxyIsOn;
        public Main()
        {
            InitializeComponent();
            // Get default proxy settings
            _defaultProxyHost = (string)Registry.GetValue(KeyName, "ProxyServer", "");
            _defaultProxyOverride = (string)Registry.GetValue(KeyName, "ProxyOverride", "");
            _defaultProxyIsOn = (int)Registry.GetValue(KeyName, "ProxyEnable", 0) == 1;
            // Add default buttons
            notifyIconContextMenu.Items.Insert(0, new ToolStripMenuItem("Default Off", null, (sender, args)
                => SetProxyEvent((ToolStripMenuItem)sender, _defaultProxyHost, false, _defaultProxyOverride)));
            notifyIconContextMenu.Items.Insert(1,new ToolStripMenuItem("Default On", null, (sender, args) 
                => SetProxyEvent((ToolStripMenuItem)sender ,_defaultProxyHost, true, _defaultProxyOverride)));
            // Disable proxy on startup
            if (Properties.Settings.Default.DisableProxyOnStartup)
            {
                SetProxy(_defaultProxyHost, false, _defaultProxyOverride);
                DisableProxyAtStartupCheckbox.Checked = true;
            }
            // Check the first or second button
            ((ToolStripMenuItem)notifyIconContextMenu.Items[!_defaultProxyIsOn || Properties.Settings.Default.DisableProxyOnStartup ? 0 : 1])
                .Checked = true;
            notifyIcon.Text = $"Proxy Switcher ({(!_defaultProxyIsOn || Properties.Settings.Default.DisableProxyOnStartup ? "Off" : "Default")})";
            notifyIconContextMenu.Items.Insert(2, new ToolStripSeparator());
            // Load settings
            var list = Properties.Settings.Default.ProxyList;
            foreach (string proxy in list)
            {
                proxyList.Items.Add(proxy);
                notifyIconContextMenu.Items.Insert(notifyIconContextMenu.Items.Count - 1, new ToolStripMenuItem(proxy, null,
                    (sender, args) => SetProxyEvent((ToolStripMenuItem)sender, proxy, true, BypassRules)));
            }
        }
        // https://stackoverflow.com/a/4210040/4213397
        protected override void SetVisibleCore(bool value) => base.SetVisibleCore(!NoGuiStart ? value : !NoGuiStart);
        private void exitToolStripMenuItem_Click(object sender, EventArgs e) =>
            Application.Exit();

        private void addButton_Click(object sender, EventArgs e)
        {
            var dialog = new AddProxy();
            dialog.ShowDialog(this);
            if (dialog.Proxy != "")
            {
                proxyList.Items.Add(dialog.Proxy);
                notifyIconContextMenu.Items.Insert(notifyIconContextMenu.Items.Count - 1, new ToolStripMenuItem(
                    dialog.Proxy, null, (s, args) => SetProxyEvent((ToolStripMenuItem)s, dialog.Proxy, true, BypassRules)));
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
                    int index = proxyList.SelectedIndex;
                    proxyList.Items.RemoveAt(index);
                    notifyIconContextMenu.Items.RemoveAt(index + 3); // +3 is because of default values at top
                    Properties.Settings.Default.ProxyList.RemoveAt(index);
                    Properties.Settings.Default.Save();
                }
            }
        }

        private void Main_FormClosed(object sender, FormClosedEventArgs e) =>
            SetProxy(_defaultProxyHost, _defaultProxyIsOn, _defaultProxyOverride);

        private void notifyIcon_DoubleClick(object sender, EventArgs e)
        {
            NoGuiStart = false;
            Show();
            WindowState = FormWindowState.Normal;
        } 

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

        /// <summary>
        /// An event to handle the menu clicks and set the proxy and checkbox
        /// </summary>
        /// <param name="sender">The toolbar item</param>
        /// <param name="host">Proxy host and port</param>
        /// <param name="enabled">Should the proxy be enabled?</param>
        /// <param name="rules">Bypass rules</param>
        private void SetProxyEvent(ToolStripMenuItem sender, string host, bool enabled, string rules)
        {
            foreach (var item in notifyIconContextMenu.Items)
                if(item is ToolStripMenuItem menuItem)
                    menuItem.Checked = false;
            sender.Checked = true;
            notifyIcon.Text = $"Proxy Switcher ({(enabled ? host : "Off")})";
            SetProxy(host, enabled, rules);
        }

        private void DisableProxyAtStartupCheckbox_CheckedChanged(object sender, EventArgs e)
        {
            Properties.Settings.Default.DisableProxyOnStartup = DisableProxyAtStartupCheckbox.Checked;
            Properties.Settings.Default.Save();
        }
    }
}
