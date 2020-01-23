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
using System.Globalization;
using System.IO;

namespace MaviSoftServerV1._0
{
    public partial class FrmActivation : Form
    {

        public FrmActivation()
        {
            InitializeComponent();

            string Code = "";

            Code = ReadActivationCode();

            if (CheckActivationCode(Code) == true)
            {
                Application.Run(new FrmGiris());
            }
        }

        private void FrmActivation_Load(object sender, EventArgs e)
        {

            string TStrAK = "";
            TStrAK = ProduceActivationKey();
            txtActivationKey.Text = TStrAK.Substring(0, 4) + "" + TStrAK.Substring(4, 4) + "" + TStrAK.Substring(8, 4) + "" + TStrAK.Substring(12, 4);
            txtActivationKey.TextAlign = HorizontalAlignment.Center;
            txtActivationKey.ForeColor = Color.Red;
            Font font = new Font(txtActivationKey.Font.FontFamily, 12.0F);
            txtActivationKey.Font = font;

        }


        private string ProduceActivationKey()
        {
            string HDD_ID;
            string TLocalCode;
            try
            {
                HDD_ID = GetDriveSerialNumber("C:\\");
                TLocalCode = (int.Parse("FF", NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(0, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse("FF", NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(2, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse("FF", NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(4, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse("FF", NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(6, 2), NumberStyles.HexNumber)).ToString("X2");

                TLocalCode = TLocalCode.Substring(2, 1) + TLocalCode.Substring(7, 1) +
                             TLocalCode.Substring(0, 1) + TLocalCode.Substring(4, 1) +
                             TLocalCode.Substring(6, 1) + TLocalCode.Substring(1, 1) +
                             TLocalCode.Substring(3, 1) + TLocalCode.Substring(5, 1);

                TLocalCode = TLocalCode.Substring(0, 2) + (int.Parse(TLocalCode.Substring(0, 2), NumberStyles.HexNumber) ^ int.Parse("FF", NumberStyles.HexNumber)).ToString("X2") + "" +
                             TLocalCode.Substring(2, 2) + (int.Parse(TLocalCode.Substring(0, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(2, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             TLocalCode.Substring(4, 2) + (int.Parse(TLocalCode.Substring(0, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(2, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(4, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             TLocalCode.Substring(6, 2) + (int.Parse(TLocalCode.Substring(0, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(2, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(4, 2), NumberStyles.HexNumber) ^ int.Parse(TLocalCode.Substring(6, 2), NumberStyles.HexNumber)).ToString("X2");

                return TLocalCode;
            }
            catch (Exception)
            {
                return string.Empty;
            }



        }

        public string GetDriveSerialNumber(string drive)
        {
            try
            {
                var driveSerialnumber = string.Empty;
                var pathRoot = Path.GetPathRoot(drive);
                if (pathRoot == null)
                {
                    return driveSerialnumber;
                }
                var driveFixed = pathRoot.Replace("\\", "");
                if (driveFixed.Length == 1)
                {
                    driveFixed = driveFixed + ":";
                }
                var wmiQuery = string.Format("SELECT VolumeSerialNumber FROM Win32_LogicalDisk Where Name = '{0}'", driveFixed);
                using (var driveSearcher = new ManagementObjectSearcher(wmiQuery))
                {
                    using (var driveCollection = driveSearcher.Get())
                    {
                        foreach (var moItem in driveCollection.Cast<ManagementObject>())
                        {
                            driveSerialnumber = Convert.ToString(moItem["VolumeSerialNumber"]);
                        }
                    }
                }
                return driveSerialnumber;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        private void btnKapat_Click(object sender, EventArgs e)
        {

            if (CheckActivationCode(txtActivationCode.Text) == true)
            {
                FrmGiris frmGiris = new FrmGiris();
                frmGiris.Show();
                this.Hide();
            }
            else
            {
                Application.Exit();
            }
        }

        private void btnKaydet_Click(object sender, EventArgs e)
        {
            string TACStr;
            TACStr = txtActivationCode.Text;
            if (CheckActivationCode(TACStr) == true)
            {
                lblActivationStatus.Text = "Aktivasyon Tamamlandı";
                lblActivationStatus.ForeColor = Color.Green;
                // AddUpdateAppSettings("ActivationCode", TACStr);
                AddActivationCode(TACStr);
            }
            else
            {
                lblActivationStatus.Text = "Aktivasyon Hatası";
                lblActivationStatus.ForeColor = Color.Red;
            }

            MessageBox.Show("Aktivasyon Kaydedildi!");
        }

        private void chkBoxActivation_CheckedChanged(object sender, EventArgs e)
        {
            if (chkBoxActivation.Checked == true)
            {
                txtActivationCode.Enabled = true;
            }
            else
            {
                txtActivationCode.Enabled = false;
            }
        }

        private void btnYenile_Click(object sender, EventArgs e)
        {
            string TStrAK = "";
            TStrAK = ProduceActivationKey();
            txtActivationKey.Text = "";
            txtActivationKey.Text = txtActivationKey.Text = TStrAK.Substring(0, 4) + "" + TStrAK.Substring(4, 4) + "" + TStrAK.Substring(8, 4) + "" + TStrAK.Substring(12, 4);
        }

        private void btnKontrol_Click(object sender, EventArgs e)
        {
            string TACStr;
            TACStr = txtActivationCode.Text.Trim();
            if (TACStr == "" || TACStr == null || TACStr == "Demo")
            {
                lblActivationStatus.Text = "Aktivasyon Hatası";
                lblActivationStatus.ForeColor = Color.Red;
            }
            else if (CheckActivationCode(TACStr) == true)
            {
                lblActivationStatus.Text = "Aktivasyon Tamamlandı";
                lblActivationStatus.ForeColor = Color.Green;
            }
            else
            {
                lblActivationStatus.Text = "Aktivasyon Hatası";
                lblActivationStatus.ForeColor = Color.Red;
            }
            chkBoxActivation.Checked = false;
            txtActivationCode.Enabled = false;
        }

        private bool CheckActivationCode(string TCounterCode)
        {
            string HDD_ID;
            string TCCStr;
            // HDD_ID = GetHardDiskSerialNo();
            HDD_ID = GetDriveSerialNumber("C:\\");
            if (TCounterCode == "" || TCounterCode == null)
            {
                return false;
            }
            else
            {
                try
                {
                    TCCStr = TCounterCode.Substring(13, 1) + TCounterCode.Substring(7, 1) +
                    TCounterCode.Substring(11, 1) + TCounterCode.Substring(1, 1) +
                    TCounterCode.Substring(15, 1) + TCounterCode.Substring(6, 1) +
                    TCounterCode.Substring(10, 1) + TCounterCode.Substring(14, 1) +
                    TCounterCode.Substring(4, 1) + TCounterCode.Substring(0, 1) +
                    TCounterCode.Substring(5, 1) + TCounterCode.Substring(8, 1) +
                    TCounterCode.Substring(2, 1) + TCounterCode.Substring(3, 1) +
                    TCounterCode.Substring(9, 1) + TCounterCode.Substring(12, 1);

                    TCCStr = (int.Parse(TCCStr.Substring(0, 4), NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(0, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse(TCCStr.Substring(4, 4), NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(2, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse(TCCStr.Substring(8, 4), NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(4, 2), NumberStyles.HexNumber)).ToString("X2") + "" +
                             (int.Parse(TCCStr.Substring(12, 4), NumberStyles.HexNumber) - int.Parse(HDD_ID.Substring(6, 2), NumberStyles.HexNumber)).ToString("X2");

                    if (TCCStr == "A492B592")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                catch (Exception)
                {
                    return false;
                }
            }

        }

        private static void AddActivationCode(string code)
        {
            string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\Activation";
            StreamWriter writer = new StreamWriter(path);
            try
            {

                writer.WriteLine(code);
                MessageBox.Show("Kayıt Başarılı");
            }
            catch (Exception)
            {
                MessageBox.Show("Kayıt işleminde bir hata oluştu!");
            }
            finally
            {
                writer.Flush();
                writer.Close();
            }
        }

        private static string ReadActivationCode()
        {
            string path = Path.GetDirectoryName(Application.ExecutablePath) + "\\Activation";
            FileStream fileStream = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Read);
            string Code = "";
            try
            {
                StreamReader reader = new StreamReader(fileStream);

                if (fileStream.CanRead)
                {

                    Code = reader.ReadToEnd();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("Bir hata oluştu!");
            }
            finally
            {
                fileStream.Close();
            }
            return Code;
        }


        private void FrmActivation_FormClosing(object sender, FormClosingEventArgs e)
        {
            string Code = "";

            Code = ReadActivationCode();

            if (CheckActivationCode(Code) == true)
            {
                FrmGiris frmGiris = new FrmGiris();
                frmGiris.Show();
                this.Hide();
            }
        }
    }
}
