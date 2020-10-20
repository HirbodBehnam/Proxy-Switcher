using System;
using System.Windows.Forms;

namespace ProxySwitcher
{
    public partial class AddProxy : Form
    {
        /// <summary>
        /// If this value is not empty then parent should get the proxy 
        /// </summary>
        public string Proxy = "";
        public AddProxy()
        {
            InitializeComponent();
        }

        private void addButton_Click(object sender, EventArgs e) => Submit();
        private void AddProxy_KeyDown(object sender, KeyEventArgs e)
        {
            if(e.KeyCode == Keys.Enter)
                Submit();
        }
        private void Submit()
        {
            if (!ushort.TryParse(proxyPort.Text, out ushort _))
            {
                MessageBox.Show("Cannot parse port number.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Proxy = proxyHost.Text + ":" + proxyPort.Text;
            Close();
        }

    }
}
