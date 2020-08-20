using System;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;

namespace MaviSoftServerV1._0
{
    public partial class FrmGiris : Form
    {
        public string SqlAdress = "";

        SqlConnection connection = null;

        public FrmGiris()
        {
            InitializeComponent();
          
        }

        private void btnKapat_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void btnTamam_Click(object sender, EventArgs e)
        {
            if (chkBoxDefault.Checked == true)
            {
                string UserName = txtUserName.Text;
                string Password = txtSifre.Text;
                string HostPC = txtHostPC.Text;
                if (UserName == null || Password == null || HostPC == null || UserName == "" || Password == "" || HostPC == "")
                {
                    lblMessage.Text = "Yanlış yada eksik karakter girdiniz!";
                    lblMessage.Visible = true;
                    txtServer.Clear();
                    txtUserName.Clear();
                    txtSifre.Clear();
                    txtHostPC.Clear();
                }
                else
                {

                    //SqlAdress = "data source = " + HostPC.Trim() + "; initial catalog = MW301_DB25; User Id=" + UserName.Trim() + "; Password=" + Password.Trim() + "; MultipleActiveResultSets = True;";
                    SqlAdress = "data source = " + HostPC.Trim() + "; initial catalog = MW301_DB25_WEB; User Id=" + UserName.Trim() + "; Password=" + Password.Trim() + "; MultipleActiveResultSets = True;";
                    SqlServerAdress.SetAdres(SqlAdress);
                    using (connection = new SqlConnection(SqlAdress))
                    {
                        connection.Open();
                        try
                        {
                            if (connection.State == ConnectionState.Open)
                            {
                                AddUpdateAppSettings("Host", HostPC.Trim());
                                AddUpdateAppSettings("SQLServer", "");
                                AddUpdateAppSettings("UserID", UserName.Trim());
                                AddUpdateAppSettings("Password", Password.Trim());
                                FrmMain form1 = new FrmMain();
                                form1.Show();
                                this.Hide();
                            }

                        }
                        catch (Exception)
                        {
                            lblMessage.Text = "Yanlış yada eksik karakter girdiniz!";
                            lblMessage.Visible = true;
                            txtUserName.Clear();
                            txtSifre.Clear();
                            txtHostPC.Clear();
                            connection.Dispose();
                            connection.Close();
                        }
                    }
                }
            }
            else
            {

                string UserName = txtUserName.Text;
                string Password = txtSifre.Text;
                string HostPC = txtHostPC.Text;
                string SQLServer = txtServer.Text;
                if (UserName == null || Password == null || HostPC == null || SQLServer == null || UserName == "" || Password == "" || HostPC == "" || SQLServer == "")
                {
                    lblMessage.Text = "Yanlış yada eksik karakter girdiniz!";
                    lblMessage.Visible = true;
                    txtServer.Clear();
                    txtUserName.Clear();
                    txtSifre.Clear();
                    txtHostPC.Clear();
                }
                else
                {

                    //SqlAdress = "data source = " + HostPC.Trim() + "\\" + SQLServer.Trim() + "; initial catalog = MW301_DB25; User Id=" + UserName.Trim() + "; Password=" + Password.Trim() + "; MultipleActiveResultSets = True;";
                    SqlAdress = "data source = " + HostPC.Trim() + "\\" + SQLServer.Trim() + "; initial catalog = MW301_DB25_WEB; User Id=" + UserName.Trim() + "; Password=" + Password.Trim() + "; MultipleActiveResultSets = True;";
                    SqlServerAdress.SetAdres(SqlAdress);
                    using (connection = new SqlConnection(SqlAdress))
                    {
                        connection.Open();
                        try
                        {
                            if (connection.State == ConnectionState.Open)
                            {
                                AddUpdateAppSettings("Host", HostPC.Trim());
                                AddUpdateAppSettings("SQLServer", SQLServer.Trim());
                                AddUpdateAppSettings("UserID", UserName.Trim());
                                AddUpdateAppSettings("Password", Password.Trim());
                                FrmMain form1 = new FrmMain();
                                form1.Show();
                                this.Hide();
                            }

                        }
                        catch (Exception)
                        {
                            lblMessage.Text = "Yanlış yada eksik karakter girdiniz!";
                            lblMessage.Visible = true;
                            txtServer.Clear();
                            txtUserName.Clear();
                            txtSifre.Clear();
                            txtHostPC.Clear();
                            connection.Dispose();
                            connection.Close();
                        }
                    }
                }
            }
        }

        private void chckDegistir_CheckedChanged(object sender, EventArgs e)
        {
            if (chckDegistir.Checked == true && chkBoxDefault.Checked == false)
            {
                txtHostPC.Enabled = true;
                txtServer.Enabled = true;
            }
            else if (chckDegistir.Checked == true && chkBoxDefault.Checked == true)
            {
                txtHostPC.Enabled = true;
                txtServer.Enabled = false;
            }
            else
            {
                txtHostPC.Enabled = false;
                txtServer.Enabled = false;
            }
        }

        private void FrmGiris_Load(object sender, EventArgs e)
        {
            txtHostPC.Text = ReadSettings("Host");
            txtServer.Text = ReadSettings("SQLServer");
            txtUserName.Text = ReadSettings("UserID");
            txtSifre.Text = ReadSettings("Password");
        }

        public static void AddUpdateAppSettings(string key, string value)
        {
            try
            {
                var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                configuration.AppSettings.Settings[key].Value = value;
                configuration.Save();
                ConfigurationManager.RefreshSection("appSettings");
            }
            catch (Exception)
            {

                throw;
            }
        }

        public static string ReadSettings(string key)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = (AppSettingsSection)configuration.GetSection("appSettings");
            return appSettings.Settings[key].Value;
        }

        private void FrmGiris_FormClosed(object sender, FormClosedEventArgs e)
        {
            Environment.Exit(0);
        }

        private void chkBoxDefault_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxDefault.Checked == true)
            {
                txtServer.Clear();
                txtServer.Enabled = false;
            }
            else
            {
                txtServer.Clear();
                txtServer.Enabled = true;
            }
        }
    }
}
