using System;
using System.Windows.Forms;
using LAD08PackagingV1.Properties;
using MetroFramework.Forms;

namespace LAD08PackagingV1
{
    public partial class Login : MetroForm
    {
        public Login()
        {
            InitializeComponent();
        }
        public DialogResult Result { get; protected set; }
        private void Login_Load(object sender, EventArgs e)
        {
            txtPassword.Clear();
            Result = DialogResult.No;
        }

        private void CheckPassword(string data)
        {
            data = data.Trim('\r', '\n');
            var setting = new Settings();
            var jj = data == setting.AdminPassword;
            Result = jj ? DialogResult.Yes : DialogResult.No;
            Close();
        }
        private void Login_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyValue == 13)
            {
                CheckPassword(txtPassword.Text);
            }
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            CheckPassword(txtPassword.Text);
        }
    }
}
