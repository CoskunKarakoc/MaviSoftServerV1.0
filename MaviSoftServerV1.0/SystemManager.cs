using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MaviSoftServerV1._0
{
    public class SystemManager
    {
        public ushort mPanelIdleInterval { get; set; }

        public bool mInTime { get; set; }

        public FrmMain mParentForm { get; set; }

        public Label lblMesaj;

        public Thread PanelOutherThread { get; set; }

        public CommandConstants mPanelProc { get; set; }

        public DateTime mMailStartTime { get; set; }

        public DateTime mMailEndTime { get; set; }

        public DateTime mMailSendTime { get; set; }

        public SqlConnection mDBConn { get; set; }

        public string mDBSQLStr { get; set; }

        public SqlDataReader mDBReader { get; set; }

        public SqlCommand mDBCmd { get; set; }

        int mMailRetryCount = 0;

        int mTimeOut = 3;

        public SystemManager()
        {

        }

        public bool StartPanelOuther()
        {
            try
            {
                mPanelProc = CommandConstants.CMD_PORT_INIT;
                mPanelIdleInterval = 0;
                mInTime = true;

                mDBConn = new SqlConnection();
                mDBConn.ConnectionString = SqlServerAdress.GetAdress();
                mDBConn.Open();

                mPanelProc = CommandConstants.CMD_TASK_LIST;
                PanelOutherThread = new Thread(OutherThreadProccess);
                PanelOutherThread.Priority = ThreadPriority.Normal;
                PanelOutherThread.IsBackground = true;
                PanelOutherThread.Start();
                mMailSendTime = ReceiveceMailTime();
                mMailStartTime = DateTime.Now;
                mMailEndTime = mMailStartTime.AddHours(mTimeOut);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public void OutherThreadProccess()
        {
            while (true)
            {
                Thread.Sleep(5);

                switch (mPanelProc)
                {
                    case CommandConstants.CMD_TASK_LIST:
                        {
                            mMailStartTime = DateTime.Now;
                            if (mMailStartTime.ToShortTimeString() == mMailEndTime.ToShortTimeString())
                            {
                                if (mMailStartTime.ToShortTimeString() == mMailSendTime.ToShortTimeString() && mMailStartTime.Second == 0)
                                {
                                    mPanelProc = CommandConstants.CMD_SND_MAIL;
                                    break;
                                }
                                mMailSendTime = ReceiveceMailTime();
                                mMailEndTime = mMailStartTime.AddHours(mTimeOut);
                            }
                            else if (mMailStartTime.ToShortTimeString() == mMailSendTime.ToShortTimeString() && mMailStartTime.Second == 0)
                            {
                                mPanelProc = CommandConstants.CMD_SND_MAIL;
                                break;
                            }
                        }
                        break;
                    case CommandConstants.CMD_SND_MAIL:
                        {
                            if (CheckMailSend() == true)
                            {
                                SendMail("Fora Teknoloji", null, true);
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                        }
                        break;
                }

            }
        }
        public MailSettings ReceiveMailSettings()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            MailSettings mailSettings = new MailSettings();
            lock (TLockObj)
            {
                tDBSQLStr = "SELECT * FROM EMailSettings";
                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                tDBReader = tDBCmd.ExecuteReader();
                if (tDBReader.Read())
                {
                    mailSettings = new MailSettings
                    {
                        EMail_Adres = tDBReader[1].ToString(),
                        Kullanici_Adi = tDBReader[2].ToString(),
                        Password = tDBReader[3].ToString(),
                        MailHost = tDBReader[4].ToString(),
                        MailPort = tDBReader[5] as int? ?? default(int),
                        SSL = tDBReader[6] as bool? ?? default(bool),
                        Authentication = tDBReader[7] as int? ?? default(int),
                        Gonderme_Saati = tDBReader[8] as DateTime? ?? default(DateTime),
                        Gelmeyenler_Raporu = tDBReader[9] as bool? ?? default(bool),
                        Alici_1_EmailAdress = tDBReader[10].ToString(),
                        Alici_1_EmailGonder = tDBReader[11] as bool? ?? default(bool),
                        Alici_2_EmailAdress = tDBReader[12].ToString(),
                        Alici_2_EmailGonder = tDBReader[13] as bool? ?? default(bool),
                        Alici_3_EmailAdress = tDBReader[14].ToString(),
                        Alici_3_EmailGonder = tDBReader[15] as bool? ?? default(bool),
                    };
                }
                return mailSettings;
            }
        }

        public bool SendMail(string subject, string body = null, bool isHtml = true)
        {
            if (body == null)
                body = GelmeyenlerReport();

            bool result = false;
            MailSettings mailSettings = ReceiveMailSettings();
            try
            {
                var message = new MailMessage();
                if (mailSettings.EMail_Adres != null)
                {
                    message.From = new MailAddress(mailSettings.EMail_Adres.Trim(), (mailSettings.Kullanici_Adi + " Geçiş Kontrol Sistemi "));
                    if (mailSettings.Alici_1_EmailGonder == true && mailSettings.Alici_1_EmailAdress != null)
                    {
                        message.To.Add(new MailAddress(mailSettings.Alici_1_EmailAdress.Trim()));
                    }
                    if (mailSettings.Alici_2_EmailGonder == true && mailSettings.Alici_2_EmailAdress != null)
                    {
                        message.To.Add(new MailAddress(mailSettings.Alici_2_EmailAdress.Trim()));
                    }
                    if (mailSettings.Alici_3_EmailGonder == true && mailSettings.Alici_3_EmailAdress != null)
                    {
                        message.To.Add(new MailAddress(mailSettings.Alici_3_EmailAdress.Trim()));
                    }
                    message.Subject = subject;
                    message.Body = body;
                    message.IsBodyHtml = isHtml;
                    using (var smtp = new SmtpClient(mailSettings.MailHost.Trim(), mailSettings.MailPort))
                    {
                        smtp.EnableSsl = mailSettings.SSL;
                        smtp.Credentials = new NetworkCredential(mailSettings.EMail_Adres.Trim(), mailSettings.Password.Trim());
                        smtp.Send(message);
                        result = true;
                    }
                }
            }
            catch (Exception e)
            {
                result = false;
            }

            return result;
        }


        public DateTime ReceiveceMailTime()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            DateTime time = new DateTime();
            lock (TLockObj)
            {
                tDBSQLStr = "SELECT TOP 1 [Gonderme Saati] FROM EMailSettings";
                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                tDBReader = tDBCmd.ExecuteReader();
                if (tDBReader.Read())
                {
                    time = tDBReader[0] as DateTime? ?? default(DateTime);
                    return time;
                }
                else
                {
                    return DateTime.Now;
                }
            }
        }

        public bool CheckMailSend()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            bool Alici1 = false;
            bool Alici2 = false;
            bool Alici3 = false;
            lock (TLockObj)
            {
                tDBSQLStr = "SELECT [Alici 1 E-Mail Gonder],[Alici 2 E-Mail Gonder],[Alici 3 E-Mail Gonder] FROM EMailSettings";
                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                tDBReader = tDBCmd.ExecuteReader();
                while (tDBReader.Read())
                {
                    Alici1 = tDBReader[0] as bool? ?? default(bool);
                    Alici2 = tDBReader[1] as bool? ?? default(bool);
                    Alici3 = tDBReader[2] as bool? ?? default(bool);
                    if (Alici1 == true || Alici2 == true || Alici3 == true)
                        return true;
                    else
                        return false;

                }

                return false;


            }


        }

        public string GelmeyenlerReport()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            DateTime Baslangic_Tarihi = DateTime.Now;
            object TLockObj = new object();
            StringBuilder builder = new StringBuilder();
            lock (TLockObj)
            {
                tDBSQLStr = @"SELECT Users.ID, Users.[Kart ID], Users.Adi, 
                                         Users.Soyadi,Users.TCKimlik, Sirketler.Adi AS Şirket,
                                         Departmanlar.Adi AS Departman,AltDepartman.Adi AS [Alt Departman],Bolum.Adi AS [Bolum Adi],Unvan.Adi AS [Unvan Adi], Users.Plaka, Bloklar.Adi AS Blok, 
                                         Users.Daire,GroupsMaster.[Grup Adi] AS [Geçiş Grubu],
                                         Users.Resim FROM ((((((Users
                                         LEFT JOIN Departmanlar ON Users.[Departman No] = Departmanlar.[Departman No])
                                         LEFT JOIN AltDepartman ON Users.[Alt Departman No] = AltDepartman.[Alt Departman No])
                                         LEFT JOIN Bolum ON Users.[Bolum No] = Bolum.[Bolum No])
                                         LEFT JOIN Unvan ON Users.[Unvan No] = Unvan.[Unvan No])
                                         LEFT JOIN GroupsMaster ON Users.[Grup No] = GroupsMaster.[Grup No])
                                         LEFT JOIN Bloklar ON Users.[Blok No] = Bloklar.[Blok No])
                                         LEFT JOIN Sirketler ON Users.[Sirket No] = Sirketler.[Sirket No] WHERE Users.ID > 0 AND Sirketler.[Sirket No] IN(1000,1) AND Departmanlar.[Departman No] IN(1000,1)AND Users.[Kart ID] <> ALL (SELECT DISTINCT AccessDatas.[Kart ID] 
                FROM AccessDatas 
                WHERE AccessDatas.[Kullanici Tipi] = 0 
                AND AccessDatas.Kod = 1
                AND AccessDatas.Tarih >= CONVERT(SMALLDATETIME,'" + Baslangic_Tarihi.Date.AddSeconds(1).ToString("dd/MM/yyyy HH:mm:ss") + "',103)" +
               " AND AccessDatas.Tarih <= CONVERT(SMALLDATETIME,'" + Baslangic_Tarihi.Date.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("dd/MM/yyyy HH:mm:ss") + "',103)" +
               " AND AccessDatas.[Gecis Tipi] = 0" +
               ")";
                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                tDBReader = tDBCmd.ExecuteReader();

                builder.Append("<!DOCTYPE html><html><head><style>.base-table { border: solid 1px #DDEEEE;border-collapse: collapse;border-spacing: 0;font: normal 13px Arial, sans-serif;}.base-table thead th {background-color: #DDEFEF;border: solid 1px #DDEEEE;color: #336B6B;padding: 10px;text-align: left;text-shadow: 1px 1px 1px #fff;}.base-table tbody td {border: solid 1px #DDEEEE;color: #333;padding: 10px;text-shadow: 1px 1px 1px #fff;}</style><title>Gelmeyenler Raporu</title></head><body><h4>Gelmeyenler Raporu-Geçiş Kontrol Sistemleri</h4></br><h6>" + DateTime.Now.ToLongDateString() + "</h6>");
                builder.Append("<table class='base-table'><thead><tr><th>ID</th><th>Kart ID</th><th>Adı</th><th>Soyadı</th><th>TC Kimlik</th><th>Şirket</th><th>Departman</th><th>Alt Departman</th><th>Bölüm Adı</th><th>Ünvan</th><th>Plaka</th><th>Blok</th><th>Daire</th><th>Geçiş Grubu</th></tr></thead><tbody>");
                bool result = false;
                while (tDBReader.Read())
                {
                    result = true;
                    builder.Append("<tr><td>" + (tDBReader[0] as int? ?? default(int)) + "</td><td>" + (tDBReader[1].ToString()) + "</td><td>" + (tDBReader[2].ToString()) + "</td><td>" + (tDBReader[3].ToString()) + "</td><td>" + (tDBReader[4].ToString()) + "</td><td>" + (tDBReader[5].ToString()) + "</td><td>" + (tDBReader[6].ToString()) + "</td><td>" + (tDBReader[7].ToString()) + "</td><td>" + (tDBReader[8].ToString()) + "</td><td>" + (tDBReader[9].ToString()) + "</td><td>" + (tDBReader[10].ToString()) + "</td><td>" + (tDBReader[11].ToString()) + "</td><td>" + (tDBReader[12] as int? ?? default(int)) + "</td><td>" + (tDBReader[13].ToString()) + "</td></tr>");
                }
                builder.Append("</tbody></table></body></html>");
                if (result == true)
                {
                    return builder.ToString();
                }
                else
                {
                    return "<!DOCTYPE html><html><head><title>Page Title</title></head><body><h1>Gelmeyenler Raporu-Geçiş Kontrol Sistemleri</h1><p>Kritere Uygun Kayıt Bulunamadı.</p></body></html>";
                }


            }
        }



    }
}
