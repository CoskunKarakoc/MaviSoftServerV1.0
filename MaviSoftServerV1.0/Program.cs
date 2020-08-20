using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Windows.Forms;

namespace MaviSoftServerV1._0
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            #region Shell-Startup-Giris
            string Code = "";
            Code = ReadActivationCode();
            if (CheckActivationCode(Code) == true)
            {
                if (ReadSettings("Host") != null && ReadSettings("Host") != ""
                      && ReadSettings("UserID") != null && ReadSettings("UserID") != ""
                      && ReadSettings("Password") != null && ReadSettings("Password") != "")
                {
                    if (ConnectionStatus() == true)
                    {
                        Application.Run(new FrmMain());
                    }
                    else
                    {
                        Application.Run(new FrmGiris());
                    }
                }
                else
                {
                    Application.Run(new FrmGiris());
                }
            }
            else
            {
                Application.Run(new FrmActivation());
            }
            #endregion

            #region DefaultRun
            //Application.Run(new FrmActivation());
            #endregion
        }

        public static string ReadSettings(string key)
        {
            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var appSettings = (AppSettingsSection)configuration.GetSection("appSettings");
            return appSettings.Settings[key].Value;
        }

        public static bool ConnectionStatus()
        {
            var SqlAdress = "data source = " + ReadSettings("Host").Trim() + "\\" + ReadSettings("SQLServer").Trim() + "; initial catalog = MW301_DB25_WEB; User Id=" + ReadSettings("UserID").Trim() + "; Password=" + ReadSettings("Password").Trim() + "; MultipleActiveResultSets = True;";
            SqlServerAdress.SetAdres(SqlAdress);
            try
            {
                using (var connection = new SqlConnection(SqlAdress))
                {
                    connection.Open();
                    if (connection.State == System.Data.ConnectionState.Open)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
            catch (Exception)
            {
                return false;
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

        private static bool CheckActivationCode(string TCounterCode)
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

        public static string GetDriveSerialNumber(string drive)
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

    }
}
