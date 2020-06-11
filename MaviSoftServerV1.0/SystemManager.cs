using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace MaviSoftServerV1._0
{
    public class SystemManager
    {

        private const ushort NO_TASK = 0;

        private const ushort DB_TASK = 1;

        private const ushort IP_TASK = 2;

        private int mTaskNo;

        private int mTaskType;

        private int mTaskIntParam1;

        private int mTaskIntParam2;

        private int mTaskIntParam3;

        private int mTaskIntParam4;

        private int mTaskIntParam5;

        private string mTaskStrParam1;

        private string mTaskStrParam2;

        private string mTaskStrParam3;

        private string mTaskUserName;

        private bool mTaskUpdateTable;

        public ushort mPanelIdleInterval { get; set; }

        public bool mInTime { get; set; }

        public FrmMain mParentForm { get; set; }

        public Label lblMesaj;

        public Thread PanelOutherThread { get; set; }

        public CommandConstants mPanelProc { get; set; }

        public DateTime mMailStartTime { get; set; }

        public DateTime mMailEndTime { get; set; }

        public DateTime mMailSendTime { get; set; }

        public DateTime mYemekhaneMailStartTime { get; set; }

        public DateTime mYemekhaneMailEndTime { get; set; }

        public DateTime mYemekhaneMailSendTime { get; set; }

        private DateTime mTaskListStartTime { get; set; }

        private DateTime mTaskListEndTime { get; set; }

        private DateTime mVisitorDeleteStartTime { get; set; }

        private DateTime mVisitorDeleteEndTime { get; set; }

        public SqlConnection mDBConn { get; set; }

        public string mDBSQLStr { get; set; }

        public SqlDataReader mDBReader { get; set; }

        public SqlCommand mDBCmd { get; set; }

        public List<Panel> mPanelsList;

        public List<PanelLog> mLogPanelList;

        private DateTime mStartTime { get; set; }

        private DateTime mEndTime { get; set; }

        private DateTime mPeriodeAccessStartTime { get; set; }

        private DateTime mPeriodeAccessEndTime { get; set; }

        private DateTime mPeriodicAccessDataTime { get; set; }

        private DateTime mPeriodicPanelHourUpgradeStart { get; set; }

        private DateTime mPeriodicPanelHourUpgradeEnd { get; set; }

        private DateTime mPeriodicIcerdeDisardaSMSKontrolStart { get; set; }

        private DateTime mPeriodicIcerdeDisardaSMSKontrolEnd { get; set; }

        private DateTime mPeriodicGelmeyenSMSKontrolStart { get; set; }

        private DateTime mPeriodicGelmeyenSMSKontrolEnd { get; set; }

        int mTimeOut = 1;



        public SystemManager(List<Panel> panels, List<PanelLog> logPanels, FrmMain frmMain)
        {
            mPanelsList = panels;
            mLogPanelList = logPanels;
            mParentForm = frmMain;
            mTaskListStartTime = DateTime.Now;
            mVisitorDeleteStartTime = DateTime.Now;
            mVisitorDeleteEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 9, 20, 0, 0);
            mTaskListEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 3, 0, 0, 0);
        }



        public bool StartPanelOuther()
        {
            try
            {

                mPanelProc = CommandConstants.CMD_TASK_LIST;
                PanelOutherThread = new Thread(SystemManagerThread);
                PanelOutherThread.Priority = ThreadPriority.Normal;
                PanelOutherThread.IsBackground = true;
                PanelOutherThread.Start();
                mMailSendTime = ReceiveceMailTime();
                mMailStartTime = DateTime.Now;
                mMailEndTime = mMailStartTime.AddHours(mTimeOut);
                mYemekhaneMailStartTime = DateTime.Now;
                mYemekhaneMailEndTime = mYemekhaneMailStartTime.AddHours(mTimeOut);
                mYemekhaneMailSendTime = ReceiveceYemekhaneMailTime();
                mStartTime = DateTime.Now;
                mEndTime = mStartTime.AddMilliseconds(500);
                mPeriodeAccessStartTime = DateTime.Now;
                mPeriodeAccessEndTime = mPeriodeAccessStartTime.AddHours(mTimeOut);
                mPeriodicAccessDataTime = ReceivePeriodicAccessDataTime();
                mPeriodicPanelHourUpgradeStart = DateTime.Now;
                mPeriodicPanelHourUpgradeEnd = mPeriodicPanelHourUpgradeStart.AddHours(1);
                mPeriodicIcerdeDisardaSMSKontrolStart = DateTime.Now;
                mPeriodicIcerdeDisardaSMSKontrolEnd = mPeriodicIcerdeDisardaSMSKontrolStart.AddHours(mTimeOut);
                mPeriodicGelmeyenSMSKontrolStart = DateTime.Now;
                mPeriodicGelmeyenSMSKontrolEnd = mPeriodicGelmeyenSMSKontrolStart.AddHours(mTimeOut);
                return true;

            }
            catch (Exception)
            {
                return false;
            }
        }

        public void SystemManagerThread()
        {
            while (true)
            {
                Thread.Sleep(50);//CHANGE:100'DEN 50 YE DÜŞÜRÜLDÜ 09032020

                switch (mPanelProc)
                {
                    case CommandConstants.CMD_TASK_LIST:
                        {
                            SyncGetNewTask();

                            /*Gelmeyenler Raporu Mail Gönderme Kontrolü*/
                            mMailStartTime = DateTime.Now;
                            if (mMailStartTime.ToShortTimeString() == mMailEndTime.ToShortTimeString())
                            {
                                if (mMailStartTime.ToShortTimeString() == mMailSendTime.ToShortTimeString() && mMailStartTime.Second == 0)
                                {
                                    if (CheckMailSendForGelmeyenler() == true)
                                    {
                                        SendMail("Fora Teknoloji", GelmeyenlerReport(), true);
                                    }
                                }
                                mMailSendTime = ReceiveceMailTime();
                                mMailEndTime = mMailStartTime.AddHours(mTimeOut);
                            }
                            else if (mMailStartTime.ToShortTimeString() == mMailSendTime.ToShortTimeString() && mMailStartTime.Second == 0)
                            {
                                if (CheckMailSendForGelmeyenler() == true)
                                {
                                    SendMail("Fora Teknoloji", GelmeyenlerReport(), true);
                                }
                            }

                            /*İçerde-Dışarda Raporunda ki İçerdeki Kullanıcıların sahip olduğu numaraya mesaj atma kontrolü*/
                            mPeriodicIcerdeDisardaSMSKontrolStart = DateTime.Now;
                            if (mPeriodicIcerdeDisardaSMSKontrolStart.ToShortTimeString() == mPeriodicIcerdeDisardaSMSKontrolEnd.ToShortTimeString())
                            {
                                SmsSettings smsSetting = new SmsSettings();
                                if (mPeriodicIcerdeDisardaSMSKontrolStart.ToShortTimeString() == smsSetting.Icerde_Disarda_Saat.Value.ToShortTimeString() && mPeriodicIcerdeDisardaSMSKontrolStart.Second == 0)
                                {
                                    if (smsSetting.Icerde_Disarda_Gonder == true)
                                    {
                                        SendSms sendSms = new SendSms(smsSetting);
                                        sendSms.IcerdeDisardaRaporMesajıGonder();
                                    }
                                }
                                mPeriodicIcerdeDisardaSMSKontrolEnd = mPeriodicIcerdeDisardaSMSKontrolStart.AddHours(mTimeOut);
                            }
                            else
                            {
                                SmsSettings smsSetting = new SmsSettings();
                                if (mPeriodicIcerdeDisardaSMSKontrolStart.ToShortTimeString() == smsSetting.Icerde_Disarda_Saat.Value.ToShortTimeString() && mPeriodicIcerdeDisardaSMSKontrolStart.Second == 0)
                                {
                                    if (smsSetting.Icerde_Disarda_Gonder == true)
                                    {
                                        SendSms sendSms = new SendSms(smsSetting);
                                        sendSms.IcerdeDisardaRaporMesajıGonder();
                                    }
                                }
                            }

                            /*Gelmeyenler Raporu SMS Gönderme Kontrolü*/
                            mPeriodicGelmeyenSMSKontrolStart = DateTime.Now;
                            if (mPeriodicGelmeyenSMSKontrolStart.ToShortTimeString() == mPeriodicGelmeyenSMSKontrolEnd.ToShortTimeString())
                            {
                                SmsSettings smsSetting = new SmsSettings();
                                if (mPeriodicGelmeyenSMSKontrolStart.ToShortTimeString() == smsSetting.Gelmeyenler_Saat.Value.ToShortTimeString() && mPeriodicGelmeyenSMSKontrolStart.Second == 0)
                                {
                                    if (smsSetting.Gelmeyenler_Gonder == true)
                                    {
                                        SendSms sendSms = new SendSms(smsSetting);
                                        sendSms.GelmeyenMesajiGonder();
                                    }
                                }
                                mPeriodicGelmeyenSMSKontrolEnd = mPeriodicGelmeyenSMSKontrolStart.AddHours(mTimeOut);
                            }
                            else
                            {
                                SmsSettings smsSetting = new SmsSettings();
                                if (mPeriodicGelmeyenSMSKontrolStart.ToShortTimeString() == smsSetting.Gelmeyenler_Saat.Value.ToShortTimeString() && mPeriodicGelmeyenSMSKontrolStart.Second == 0)
                                {
                                    if (smsSetting.Gelmeyenler_Gonder == true)
                                    {
                                        SendSms sendSms = new SendSms(smsSetting);
                                        sendSms.GelmeyenMesajiGonder();
                                    }
                                }
                            }

                            /*Yemekhane Mail Gönderme Kontrolü*/
                            mYemekhaneMailStartTime = DateTime.Now;
                            if (mYemekhaneMailStartTime.ToShortTimeString() == mYemekhaneMailEndTime.ToShortTimeString())
                            {
                                if (mYemekhaneMailStartTime.ToShortTimeString() == mYemekhaneMailSendTime.ToShortTimeString() && mYemekhaneMailStartTime.Second == 0)
                                {
                                    if (CheckMailSendForYemekhane() == true)
                                    {
                                        SendMail("Fora Teknoloji", YemekhaneReport(), true);
                                    }
                                }
                                mYemekhaneMailSendTime = ReceiveceYemekhaneMailTime();
                                mYemekhaneMailEndTime = mYemekhaneMailStartTime.AddHours(mTimeOut);
                            }
                            else if (mYemekhaneMailStartTime.ToShortTimeString() == mYemekhaneMailSendTime.ToShortTimeString() && mYemekhaneMailStartTime.Second == 0)
                            {
                                if (CheckMailSendForYemekhane() == true)
                                {
                                    SendMail("Fora Teknoloji", YemekhaneReport(), true);
                                }
                            }


                            /*Her gün saat 03:00' da TaskList Table temizleme kontrolü*/
                            /*"AccessDatasTemps" veritabanından siliniyor.*/
                            mTaskListStartTime = DateTime.Now;
                            if (mTaskListStartTime.ToShortTimeString() == mTaskListEndTime.ToShortTimeString())
                            {
                                ClearTaskList();
                                DeleteAccessDatasTempsLog();
                                mTaskListEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 3, 0, 0, 0);
                            }

                            /*Her gün saat 00:00'da "Tüm Ziyaretçiler" panellerlerden siliniyor ve "AccessDatasTemp" veritabanından siliniyor.*/
                            mVisitorDeleteStartTime = DateTime.Now;
                            if (mVisitorDeleteStartTime.ToShortTimeString() == mVisitorDeleteEndTime.ToShortTimeString() && mVisitorDeleteStartTime.Second == 0)
                            {
                                DeleteAllVisitor();
                                mVisitorDeleteEndTime = new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 0, 0, 0, 0);
                            }


                            /*Tüm Geçiş Olay Verilerinin Alınması Kontrolü*/
                            mPeriodeAccessStartTime = DateTime.Now;
                            if (mPeriodeAccessStartTime.ToShortTimeString() == mPeriodeAccessEndTime.ToShortTimeString())
                            {

                                if (mPeriodeAccessStartTime.ToShortTimeString() == mPeriodicAccessDataTime.ToShortTimeString() && mPeriodeAccessStartTime.Second == 0)
                                {
                                    mPanelProc = CommandConstants.CMD_RCV_LOGS;
                                    break;
                                }
                                mPeriodicAccessDataTime = ReceivePeriodicAccessDataTime();
                                mPeriodeAccessEndTime = mPeriodeAccessStartTime.AddHours(mTimeOut);

                            }
                            else if (mPeriodeAccessStartTime.ToShortTimeString() == mPeriodicAccessDataTime.ToShortTimeString() && mPeriodeAccessStartTime.Second == 0)
                            {
                                mPanelProc = CommandConstants.CMD_RCV_LOGS;
                                break;
                            }

                            /*Periyodik olarak panel saatinin güncellenmesi*/
                            mPeriodicPanelHourUpgradeStart = DateTime.Now;
                            if (mPeriodicPanelHourUpgradeStart.ToShortTimeString() == mPeriodicPanelHourUpgradeEnd.ToShortTimeString())
                            {
                                mPeriodicPanelHourUpgradeEnd = mPeriodicPanelHourUpgradeStart.AddHours(mTimeOut);
                                mPanelProc = CommandConstants.CMD_SND_RTC;
                                break;
                            }


                        }
                        break;
                    case CommandConstants.CMD_RCV_LOGS:
                        {
                            if (CheckPeriodicAccessReceive() == true)
                            {
                                // CheckDeleteAfterReceiving();
                                StringBuilder TSndStr = new StringBuilder();
                                int TDataInt;
                                object TLockObj = new object();
                                string tDBSQLStr = "";
                                SqlCommand tDBCmd;
                                lock (TLockObj)
                                {
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
                                        foreach (var panel in mPanelsList)
                                        {
                                            tDBSQLStr += "INSERT INTO TaskList ([Gorev Kodu], [IntParam 1], [Panel No], [Durum Kodu], Tarih, [Kullanici Adi], [Tablo Guncelle])" +
                                            " VALUES(" +
                                            (int)CommandConstants.CMD_RCV_LOGS + "," + 1 + "," + panel.mPanelNo + "," + 1 + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + "'System'," + 0 + ") ";
                                        }
                                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                        TDataInt = tDBCmd.ExecuteNonQuery();
                                    }
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                                break;
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                                break;
                            }
                        }
                    case CommandConstants.CMD_SND_RTC:
                        {
                            StringBuilder TSndStr = new StringBuilder();
                            int TDataInt;
                            object TLockObj = new object();
                            string tDBSQLStr = "";
                            SqlCommand tDBCmd;
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();
                                    foreach (var panel in mPanelsList)
                                    {
                                        tDBSQLStr += "INSERT INTO TaskList ([Gorev Kodu], [IntParam 1], [Panel No], [Durum Kodu], Tarih, [Kullanici Adi], [Tablo Guncelle])" +
                                        " VALUES(" +
                                        (int)CommandConstants.CMD_SND_RTC + "," + 1 + "," + panel.mPanelNo + "," + 1 + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + "'System'," + 0 + ") ";
                                    }
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    TDataInt = tDBCmd.ExecuteNonQuery();
                                }
                            }
                            mPanelProc = CommandConstants.CMD_TASK_LIST;
                            break;
                        }
                }

            }
        }

        /// <summary>
        /// Web Arayüzde Oluşturulan Mail Ayarlarının Alındığı Yer
        /// </summary>
        /// <returns></returns>
        public MailSettings ReceiveMailSettings()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            MailSettings mailSettings = new MailSettings();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT * FROM EMailSettings";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        mailSettings = new MailSettings
                        {
                            EMail_Adres = tDBReader["E-Mail Adres"].ToString(),
                            Kullanici_Adi = tDBReader["Kullanici Adi"].ToString(),
                            Password = tDBReader["Sifre"].ToString(),
                            MailHost = tDBReader["SMPT Server"].ToString(),
                            MailPort = tDBReader["SMPT Server Port"] as int? ?? default(int),
                            SSL = tDBReader["SSL Kullan"] as bool? ?? default(bool),
                            Authentication = tDBReader["Authentication"] as int? ?? default(int),
                            Gonderme_Saati = tDBReader["Gonderme Saati"] as DateTime? ?? default(DateTime),
                            Gelmeyenler_Raporu = tDBReader["Gelmeyenler Raporu"] as bool? ?? default(bool),
                            Yemekhane_Raporu = tDBReader["Yemekhane Raporu"] as bool? ?? default(bool),
                            Kapi_Grup_No = tDBReader["Kapi Grup No"] as int? ?? default(int),
                            Kapi_Grup_Baslangic_Saati = tDBReader["Kapi Grup Baslangic Saati"] as DateTime? ?? default(DateTime),
                            Kapi_Grup_Bitis_Saati = tDBReader["Kapi Grup Bitis Saati"] as DateTime? ?? default(DateTime),
                            Kapi_Grup_Gonderme_Saati = tDBReader["Kapi Grup Gonderme Saati"] as DateTime? ?? default(DateTime),
                            Alici_1_EmailAdress = tDBReader["Alici 1 E-Mail Adres"].ToString(),
                            Alici_1_EmailGonder = tDBReader["Alici 1 E-Mail Gonder"] as bool? ?? default(bool),
                            Alici_2_EmailAdress = tDBReader["Alici 2 E-Mail Adres"].ToString(),
                            Alici_2_EmailGonder = tDBReader["Alici 2 E-Mail Gonder"] as bool? ?? default(bool),
                            Alici_3_EmailAdress = tDBReader["Alici 3 E-Mail Adres"].ToString(),
                            Alici_3_EmailGonder = tDBReader["Alici 3 E-Mail Gonder"] as bool? ?? default(bool),
                        };
                    }
                    return mailSettings;
                }
            }
        }

        /// <summary>
        /// Mail Gönderme Rutin
        /// Not:'Body boş bırakılırsa Gelmeyenler Raporu HTML Çıktısı Olarak Dolduruluyor.'
        /// </summary>
        /// <param name="subject"></param>
        /// <param name="body"></param>
        /// <param name="isHtml"></param>
        /// <returns></returns>
        public bool SendMail(string subject, string body = null, bool isHtml = true)
        {
            bool result = false;
            MailSettings mailSettings = ReceiveMailSettings();
            try
            {
                var message = new MailMessage();
                if (mailSettings.EMail_Adres != null)
                {
                    message.From = new MailAddress(mailSettings.EMail_Adres.Trim(), mailSettings.Kullanici_Adi);
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
                    message.Subject = "Kartlı Geçiş Kontrol Sistemi ";
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
            catch (Exception)
            {
                result = false;
            }

            return result;
        }

        /// <summary>
        /// Mail Gönderme Saatinin Alındığı Yer
        /// </summary>
        /// <returns></returns>
        public DateTime ReceiveceMailTime()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            DateTime time = new DateTime();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
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
        }

        /// <summary>
        /// Yemekhane Mail Gönderme Saatinin Alındığı Yer
        /// </summary>
        /// <returns></returns>
        public DateTime ReceiveceYemekhaneMailTime()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            DateTime time = new DateTime();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT TOP 1 [Kapi Grup Gonderme Saati] FROM EMailSettings";
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
        }

        /// <summary>
        /// Offline Geçiş Kayıtlarının Alınma Saatinin Kontrol Edildiği Yer.
        /// </summary>
        /// <returns></returns>
        public DateTime ReceivePeriodicAccessDataTime()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            DateTime time = new DateTime();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT TOP 1 [PeriodicAccessDataTime] FROM ProgInit";
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
        }

        /// <summary>
        /// Offline Geçiş Kayıtlarının Tüm Panellerden Alınıp Alınmayacağının Kontrolü
        /// </summary>
        /// <returns></returns>
        public bool CheckPeriodicAccessReceive()
        {
            object TLockObj = new object();
            string tDBSQLStr = "";
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            bool AllPanelAccess = false;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT TOP 1 [AllPanelPeriodicAccessReceive] FROM ProgInit";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        AllPanelAccess = tDBReader[0] as bool? ?? default(bool);
                    }
                }
            }
            return AllPanelAccess;
        }

        /// <summary>
        /// Mail'in Gönderilip-Gönderilmeyeceğinin Kontrolünün Yapıldığı Methot
        /// </summary>
        /// <returns></returns>
        public bool CheckMailSendForGelmeyenler()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            bool Alici1 = false;
            bool Alici2 = false;
            bool Alici3 = false;
            bool GelmeyenRapor = false;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT [Alici 1 E-Mail Gonder],[Alici 2 E-Mail Gonder],[Alici 3 E-Mail Gonder],[Gelmeyenler Raporu] FROM EMailSettings";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    while (tDBReader.Read())
                    {
                        Alici1 = tDBReader[0] as bool? ?? default(bool);
                        Alici2 = tDBReader[1] as bool? ?? default(bool);
                        Alici3 = tDBReader[2] as bool? ?? default(bool);
                        GelmeyenRapor = tDBReader[3] as bool? ?? default(bool);
                        if (GelmeyenRapor == true && (Alici1 == true || Alici2 == true || Alici3 == true))
                            return true;
                        else
                            return false;
                    }
                    return false;
                }
            }


        }

        /// <summary>
        /// Yemekhane Mail'inin Gönderilip-Gönderilmeyeceğinin Kontrolünün Yapıldığı Methot
        /// </summary>
        /// <returns></returns>
        public bool CheckMailSendForYemekhane()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            object TLockObj = new object();
            bool Alici1 = false;
            bool Alici2 = false;
            bool Alici3 = false;
            bool YemekhaneRaporGonder = false;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT [Alici 1 E-Mail Gonder],[Alici 2 E-Mail Gonder],[Alici 3 E-Mail Gonder],[Yemekhane Raporu] FROM EMailSettings";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    while (tDBReader.Read())
                    {
                        Alici1 = tDBReader[0] as bool? ?? default(bool);
                        Alici2 = tDBReader[1] as bool? ?? default(bool);
                        Alici3 = tDBReader[2] as bool? ?? default(bool);
                        YemekhaneRaporGonder = tDBReader[3] as bool? ?? default(bool);
                        if (YemekhaneRaporGonder == true && (Alici1 == true || Alici2 == true || Alici3 == true))
                            return true;
                        else
                            return false;

                    }

                    return false;

                }
            }
        }

        /// <summary>
        /// Mail İşlemi İçin Gelmeyenler Raporu
        /// </summary>
        /// <returns></returns>
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
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
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
                        return "<!DOCTYPE html><html><head><title>Gelmeyenler</title></head><body><h1>Gelmeyenler Raporu-Geçiş Kontrol Sistemleri</h1><p>Kritere Uygun Kayıt Bulunamadı.</p></body></html>";
                    }
                }

            }
        }

        /// <summary>
        /// Mail İşlemi İçin Yemekhane Raporu
        /// </summary>
        /// <returns></returns>
        public string YemekhaneReport()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            DateTime Baslangic_Tarihi = DateTime.Now;
            object TLockObj = new object();
            StringBuilder builder = new StringBuilder();
            MailSettings mailSettings = ReceiveMailSettings();
            List<int> PanelListesi = new List<int>();
            PanelListesi = DoorGroupsDetailList();
            if (mailSettings.Kapi_Grup_No != null)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
                        var BaslangicTarihSaat = DateTime.Now.ToShortDateString() + " " + mailSettings.Kapi_Grup_Baslangic_Saati.Value.ToLongTimeString();
                        var BitisTarihSaat = DateTime.Now.ToShortDateString() + " " + mailSettings.Kapi_Grup_Bitis_Saati.Value.ToLongTimeString();
                        tDBSQLStr = @"SELECT COUNT(*),PanelSettings.[Panel ID],
                        PanelSettings.[Panel Name],MIN(AccessDatas.Tarih) AS [İlk Kayıt],MAX(AccessDatas.Tarih) AS [Son Kayıt] FROM AccessDatas
				        LEFT JOIN PanelSettings ON AccessDatas.[Panel ID]=PanelSettings.[Panel ID]
				        WHERE AccessDatas.[Gecis Tipi] = 0 AND AccessDatas.[Kart ID]>0";
                        tDBSQLStr += " AND AccessDatas.Tarih >= CONVERT(SMALLDATETIME,'" + BaslangicTarihSaat + "',103) ";
                        tDBSQLStr += " AND AccessDatas.Tarih <= CONVERT(SMALLDATETIME,'" + BitisTarihSaat + "',103) AND AccessDatas.Kod=1";
                        if (PanelListesi.Count > 0)
                        {
                            var sayac = 0;
                            var count = PanelListesi.Count();
                            tDBSQLStr += " AND (";
                            foreach (var item in PanelListesi)
                            {
                                sayac++;
                                if (sayac == count)
                                {
                                    tDBSQLStr += " (AccessDatas.[Panel ID]= " + item + " AND AccessDatas.[Kapi ID] IN(SELECT DoorGroupsDetail.[Kapi ID] FROM DoorGroupsDetail WHERE DoorGroupsDetail.[Kapi Grup No]=" + mailSettings.Kapi_Grup_No + " AND DoorGroupsDetail.[Panel ID]=" + item + "))";
                                    break;
                                }
                                else
                                {
                                    tDBSQLStr += " (AccessDatas.[Panel ID]= " + item + " AND AccessDatas.[Kapi ID] IN(SELECT DoorGroupsDetail.[Kapi ID] FROM DoorGroupsDetail WHERE DoorGroupsDetail.[Kapi Grup No]=" + mailSettings.Kapi_Grup_No + " AND DoorGroupsDetail.[Panel ID]=" + item + "))";
                                    tDBSQLStr += " OR ";
                                }
                            }

                            tDBSQLStr += ")";
                        }
                        tDBSQLStr += " GROUP BY PanelSettings.[Panel ID],PanelSettings.[Panel Name]";
                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                        tDBReader = tDBCmd.ExecuteReader();
                        builder.Append("<!DOCTYPE html><html><head><style>.base-table { border: solid 1px #DDEEEE;border-collapse: collapse;border-spacing: 0;font: normal 13px Arial, sans-serif;}.base-table thead th {background-color: #DDEFEF;border: solid 1px #DDEEEE;color: #336B6B;padding: 10px;text-align: left;text-shadow: 1px 1px 1px #fff;}.base-table tbody td {border: solid 1px #DDEEEE;color: #333;padding: 10px;text-shadow: 1px 1px 1px #fff;}</style><title>Gelmeyenler Raporu</title></head><body><h4>Yemekhane Raporu-Geçiş Kontrol Sistemleri</h4></br><h6>" + DateTime.Now.ToLongDateString() + "</h6>");
                        builder.Append("<table class='base-table'><thead><tr><th>Panel ID</th><th>Panel Adı</th><th>İlk Kayıt</th><th>Son Kayıt</th><th>Onaylı Geçiş Sayısı</th></tr></thead><tbody>");
                        bool result = false;
                        int sum = 0;
                        while (tDBReader.Read())
                        {
                            result = true;
                            sum += (tDBReader[0] as int? ?? default(int));
                            builder.Append("<tr><td>" + (tDBReader[1] as int? ?? default(int)) + "</td><td>" + (tDBReader[2].ToString()) + "</td><td>" + (tDBReader[3].ToString()) + "</td><td>" + (tDBReader[4].ToString()) + "</td><td>" + (tDBReader[0] as int? ?? default(int)) + "</td></tr>");
                        }
                        builder.Append("</tbody><tfoot style='text - align:center'><tr><td></td><td></td><td></td><td><h3><b>Toplam:</b></h3></td><td>" + sum + "</td></tr></tfoot></table></body></html>");
                        if (result == true)
                        {
                            return builder.ToString();
                        }
                        else
                        {
                            return "<!DOCTYPE html><html><head><title>Yemekhane</title></head><body><h1>Yemekhane Raporu-Geçiş Kontrol Sistemleri</h1><p>Kritere Uygun Kayıt Bulunamadı.</p></body></html>";
                        }
                    }
                }
            }
            else
            {
                return "<!DOCTYPE html><html><head><title>Yemekhane</title></head><body><h1>Yemekhane Raporu-Geçiş Kontrol Sistemleri</h1><p>Kritere Uygun Kayıt Bulunamadı.</p></body></html>";
            }
        }

        /// <summary>
        /// Yemekhane Raporu İçin DoorGroupsDetail Panel Listesi
        /// </summary>
        /// <returns></returns>
        public List<int> DoorGroupsDetailList()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            DateTime Baslangic_Tarihi = DateTime.Now;
            object TLockObj = new object();
            StringBuilder builder = new StringBuilder();
            MailSettings mailSettings = ReceiveMailSettings();
            List<int> panelListesi = new List<int>();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT DISTINCT [Panel ID] FROM DoorGroupsDetail WHERE DoorGroupsDetail.[Kapi Grup No] = " + mailSettings.Kapi_Grup_No;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    while (tDBReader.Read())
                    {
                        panelListesi.Add(tDBReader[0] as int? ?? default(int));
                    }
                }
            }
            return panelListesi;
        }

        /// <summary>
        /// Görev Listesinden Durumu Yeni Olan Görev Alınıyor.
        /// </summary>
        public void SyncGetNewTask()
        {
            object TLockObj = new object();
            ushort TTaskOk = 0;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();

                    foreach (var panel in mPanelsList)
                    {   //Panel Göreve İçin Bekliyorsa
                        if (panel.mPanelProc == CommandConstants.CMD_TASK_LIST)
                        {
                            mDBSQLStr = "Select * from TaskList where [Panel No] = " + panel.mPanelNo + " And [Durum Kodu]=" + (int)CTaskStates.TASK_NEW + " Order By [Kayit No]";
                            mDBCmd = new SqlCommand(mDBSQLStr, mDBConn);
                            mDBReader = mDBCmd.ExecuteReader();

                            TTaskOk = 0;
                            while (mDBReader.Read())
                            {
                                if ((mDBReader["Kayit No"] as int? ?? default(int)) > 0 && (mDBReader["Gorev Kodu"] as int? ?? default(int)) > 0 && (mDBReader["IntParam 1"] as int? ?? default(int)) >= 0)
                                {
                                    TTaskOk = 1;
                                    break;
                                }
                            }

                            if (TTaskOk > 0)
                            {
                                mTaskNo = (int)mDBReader["Kayit No"];
                                mTaskType = (int)mDBReader["Gorev Kodu"];
                                mTaskIntParam1 = mDBReader["IntParam 1"] as int? ?? default(int);
                                mTaskIntParam2 = mDBReader["IntParam 2"] as int? ?? default(int);
                                mTaskIntParam3 = mDBReader["IntParam 3"] as int? ?? default(int);
                                mTaskIntParam4 = mDBReader["IntParam 4"] as int? ?? default(int);
                                mTaskIntParam5 = mDBReader["IntParam 5"] as int? ?? default(int);
                                mTaskStrParam1 = mDBReader["StrParam 1"].ToString();
                                mTaskStrParam2 = mDBReader["StrParam 2"].ToString();
                                mTaskStrParam3 = mDBReader["StrParam 3"].ToString();
                                mTaskUserName = mDBReader["Kullanici Adi"].ToString();
                                mTaskUpdateTable = (bool)mDBReader["Tablo Guncelle"];
                                //Panel Bağlantısı Kopmamışsa
                                if (panel.mPanelClient.Connected == true)
                                {
                                    panel.mTempTaskSource = DB_TASK;
                                    panel.mTempTaskNo = mTaskNo;
                                    panel.mTempTaskType = mTaskType;
                                    panel.mTempTaskIntParam1 = mTaskIntParam1;
                                    panel.mTempTaskIntParam2 = mTaskIntParam2;
                                    panel.mTempTaskIntParam3 = mTaskIntParam3;
                                    panel.mTempTaskIntParam4 = mTaskIntParam4;
                                    panel.mTempTaskIntParam5 = mTaskIntParam5;
                                    panel.mTempTaskStrParam1 = mTaskStrParam1;
                                    panel.mTempTaskUserName = mTaskUserName;
                                    panel.mTempTaskUpdateTable = mTaskUpdateTable;
                                    if (panel.mTempTaskType == (int)CommandConstants.CMD_SND_GENERALSETTINGS)
                                        Thread.Sleep(2000);
                                }
                                else
                                {
                                    //Panel Bağlantısı Kopmuşsa
                                    string tDBSQLStr;
                                    string tDBSQLStr2;
                                    SqlCommand tDBCmd;
                                    SqlCommand tDBCmd2;
                                    object TLockObjj = new object();
                                    int TRetInt = 0;
                                    lock (TLockObjj)
                                    {
                                        try
                                        {
                                            tDBSQLStr = "UPDATE TaskList SET [Durum Kodu]=" + (int)CTaskStates.TASK_NOCONNECTION + " WHERE [Kayit No]=" + mTaskNo;
                                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                            TRetInt = tDBCmd.ExecuteNonQuery();
                                            if (TRetInt < 0)
                                            {
                                                tDBSQLStr2 = "DELETE FROM TaskList WHERE [Kayit No]=" + mTaskNo;
                                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                                TRetInt = tDBCmd2.ExecuteNonQuery();
                                            }
                                        }
                                        catch (Exception)
                                        {
                                            throw;
                                        }
                                    }

                                }
                            }
                            else
                            {
                                panel.mTempTaskSource = NO_TASK;
                                panel.mTempTaskNo = 0;
                                panel.mTempTaskType = 0;
                            }
                            mDBReader.Close();
                        }
                        else if (panel.mPanelProc == CommandConstants.CMD_PORT_DISABLED || panel.mPanelProc == CommandConstants.CMD_PORT_CLOSE)//Panel Bağlanamıyorsa
                        {
                            int TRetInt;
                            string tDBSQLStr;
                            SqlCommand tDBCmd;
                            mDBSQLStr = "UPDATE TaskList SET [Durum Kodu] = " + (int)CTaskStates.TASK_ERROR + " WHERE [Panel No] = " + panel.mPanelNo + " AND [Durum Kodu]<>2";
                            mDBCmd = new SqlCommand(mDBSQLStr, mDBConn);
                            TRetInt = mDBCmd.ExecuteNonQuery();
                            if (TRetInt < 0)
                            {
                                tDBSQLStr = "DELETE TaskList WHERE [Panel No] = " + panel.mPanelNo;
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                tDBCmd.ExecuteNonQuery();
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Zaman Periyoduna Göre Önceki Görev Listesi Temizleniyor.
        /// </summary>
        /// <returns></returns>
        private bool ClearTaskList()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            object TLockObj = new object();
            int TRetInt = 0;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "DELETE FROM TaskList WHERE TaskList.Tarih <= '" + mTaskListEndTime.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    TRetInt = tDBCmd.ExecuteNonQuery();
                    if (TRetInt < 0)
                    {
                        return false;
                    }
                    else
                    {
                        return true;
                    }
                }
            }
        }
        /// <summary>
        /// Panel Bağlı Değilse Görev Zaman Aşımına Uğruyor
        /// </summary>
        /// <param name="taskNo">TaskList No</param>
        /// <returns></returns>
        private bool NotConnectedPanel(int taskNo)
        {
            string tDBSQLStr;
            string tDBSQLStr2;
            SqlCommand tDBCmd;
            SqlCommand tDBCmd2;
            object TLockObj = new object();
            int TRetInt = 0;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "UPDATE TaskList SET [Durum Kodu]=" + 4 + " WHERE [Kayit No]=" + taskNo;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    TRetInt = mDBCmd.ExecuteNonQuery();
                    if (TRetInt > 0)
                    {
                        return true;
                    }
                    else
                    {
                        tDBSQLStr2 = "DELETE FROM TaskList WHERE [Kayit No]=" + taskNo;
                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                        TRetInt = tDBCmd2.ExecuteNonQuery();
                        if (TRetInt > 0)
                        {
                            return true;
                        }
                        else
                        {
                            return false;
                        }
                    }
                }
            }
        }

        private void DeleteAllVisitor()
        {
            int TRetInt;
            string tDBSQLStr;
            object TLockObj = new object();
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<int> visitorID = new List<int>();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        mDBConn.Open();
                        tDBSQLStr = "SELECT ID FROM Users WHERE [Kullanici Tipi]=1";
                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            visitorID.Add((tDBReader[0] as int? ?? default(int)));
                        }
                        foreach (var id in visitorID)
                        {
                            foreach (var panel in mPanelsList)
                            {
                                tDBSQLStr += "INSERT INTO TaskList ([Gorev Kodu], [IntParam 1], [Panel No], [Durum Kodu], Tarih, [Kullanici Adi], [Tablo Guncelle],[Deneme Sayisi])" +
                                                                       " VALUES(" +
                                                                       (int)CommandConstants.CMD_ERS_USER + "," + id + "," + panel.mPanelNo + "," + 1 + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + "'System'," + 1 + "," + 1 + ") ";
                            }

                        }
                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                        TRetInt = tDBCmd.ExecuteNonQuery();
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }


        private void DeleteAccessDatasTempsLog()
        {
            int TRetInt;
            string tDBSQLStr;
            string tDBSQLStr2 = "";
            object TLockObj = new object();
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<int> kayitNo = new List<int>();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    try
                    {
                        mDBConn.Open();
                        tDBSQLStr = "SELECT * FROM AccessDatasTemps";
                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            if ((tDBReader["Kod"] as int? ?? default(int)) >= 20 && (tDBReader["Kod"] as int? ?? default(int)) <= 27)
                            {
                                if ((tDBReader["Kontrol"] as int? ?? default(int)) == 1)
                                {
                                    kayitNo.Add(tDBReader["Kayit No"] as int? ?? default(int));
                                }
                            }
                            else
                            {
                                kayitNo.Add(tDBReader["Kayit No"] as int? ?? default(int));
                            }

                        }

                        if (kayitNo.Count > 0 && kayitNo != null)
                        {
                            tDBSQLStr2 = "";
                            foreach (var item in kayitNo)
                            {
                                tDBSQLStr2 += "DELETE FROM AccessDatasTemps WHERE [Kayit No]=" + item.ToString();
                            }

                            tDBCmd = new SqlCommand(tDBSQLStr2, mDBConn);
                            TRetInt = tDBCmd.ExecuteNonQuery();
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
            }
        }


    }
}