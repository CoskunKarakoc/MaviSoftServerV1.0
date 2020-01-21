using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Management;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MaviSoftServerV1._0
{
    public partial class FrmActivation : Form
    {
        private string ActivationCode = "";
        public FrmActivation()
        {
            InitializeComponent();
        }

        private void FrmActivation_Load(object sender, EventArgs e)
        {
            var UserSuccess = ReadSettings("Activation");
            if (UserSuccess != null || UserSuccess != "")
            {
                FrmGiris frmGiris = new FrmGiris();
                frmGiris.Show();
                this.Hide();
            }
            var result = ProduceActivationKey();

        }




        private string ProduceActivationKey()
        {
            string HDD_ID;
            string TLocalCode;
            HDD_ID = GetHardDiskSerialNo();
            byte[] array = new byte[2];
            var result1 = HDD_ID.Substring(0, 2);
            var result2 = HDD_ID.Substring(2, 2);
            var result3 = HDD_ID.Substring(4, 2);
            var result4 = HDD_ID.Substring(6, 2);
            var convert1 = Convert.ToInt32(result1);
            var convert2 = Convert.ToInt32(result2);
            var convert4 = Convert.ToInt32(result4);
            return HDD_ID;
        }

        public string GetHardDiskSerialNo()
        {
            ManagementClass mangnmt = new ManagementClass("Win32_LogicalDisk");
            ManagementObjectCollection mcol = mangnmt.GetInstances();
            string result = "";
            foreach (ManagementObject strt in mcol)
            {
                result += Convert.ToString(strt["VolumeSerialNumber"]);
            }
            return result;
        }

        private void btnKapat_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }


        private void btnKaydet_Click(object sender, EventArgs e)
        {
            AddUpdateAppSettings("Activation", ActivationCode);
            MessageBox.Show("Aktivasyon Kaydedildi!");
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

    }
}
