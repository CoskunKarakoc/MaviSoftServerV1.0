using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Data;
using System.Diagnostics;
using System.Collections;

namespace MaviSoftServerV1._0
{
    public class PanelLog
    {
        public const ushort NO_TASK = 0;

        public const ushort DB_TASK = 1;

        public const ushort IP_TASK = 2;

        public static int[] TaskPIX = new int[(int)TCONST.MAX_PANEL];

        public S_TASKLIST[,] TaskList = new S_TASKLIST[(int)TCONST.MAX_PANEL, (int)TCONST.MAX_TASK_CNT];

        private int mTaskType;

        private int mLogTaskIntParam3;

        private bool mLogTaskUpdateTable;

        private ushort mLogTaskSource;

        private S_ANSWER mSAnswer;

        public bool mTransferCompleted { get; set; }

        public bool mLogTransferCompleted { get; set; }

        public ushort mReadStep { get; set; }

        public bool mProcessTerminated { get; set; }

        public ushort mRetryCnt { get; set; }

        const ushort RETRY_COUNT = 1;

        public string mReturnStr;

        public string mLogReturnStr;

        public FrmMain mParentForm { get; set; }

        public Label lblMesaj;

        public Thread PanelThread { get; set; }

        public Thread LogThread { get; set; }

        private TcpClient mPanelClient { get; set; }

        private TcpClient mPanelClientLog { get; set; }


        public ushort mPanelIdleInterval { get; set; }

        private CommandConstants mPanelProc { get; set; }

        private CommandConstants mLogProc { get; set; }


        private ushort mPanelConState { get; set; }

        private int mPanelTCPPort { get; set; }

        private int mPanelTCPPortLog { get; set; }

        private string mPanelIPAddress { get; set; }

        private int mPanelSerialNo { get; set; }

        private DateTime mStartTime { get; set; }

        private DateTime mEndTime { get; set; }

        public DateTime mMailStartTime { get; set; }

        public DateTime mMailEndTime { get; set; }


        private DateTime mReceiveTimeStart { get; set; }

        private DateTime mReceiveTimeEnd { get; set; }


        public string mMailSendTime { get; set; }

        public ushort mActive { get; set; }

        public ushort mMemIX { get; set; }

        public ushort mTimeOut { get; set; }

        public ushort mPortType { get; set; }

        public ushort mPanelAlarmIX { get; set; }

        public int mPanelNo { get; set; }

        public string mPanelName { get; set; }

        public bool mInTime { get; set; }

        public int mConnectTimeout { get; set; }

        public SqlConnection mDBConn { get; set; }

        public string mDBSQLStr { get; set; }

        public SqlDataReader mDBReader { get; set; }

        public SqlCommand mDBCmd { get; set; }

        public string TSndAPBStr = "";

        private string DoorStatusStr = "";

        int mMailRetryCount = 0;

        List<Panel> PanelListesi = new List<Panel>();

        public List<PanelLog> LogPanelListesi = new List<PanelLog>();

        public Queue SndQueue = new Queue();

        public PanelLog(ushort MemIX, ushort TActive, int TPanelNo, ushort JTimeOut, string TIPAdress, int TMACAdress, int TCPPortOne, int TCPPortTwo, List<Panel> Panels, FrmMain parentForm)
        {
            mMemIX = MemIX;
            mActive = TActive;
            mTimeOut = JTimeOut;
            mPanelTCPPort = TCPPortOne;
            mPanelTCPPortLog = TCPPortTwo;
            mPanelIPAddress = TIPAdress;
            mPanelSerialNo = TMACAdress;
            mPanelNo = TPanelNo;
            mParentForm = parentForm;
            PanelListesi = Panels;
            mReceiveTimeStart = DateTime.Now;

            if (mTimeOut < 3 && mTimeOut > 60)
            {
                mTimeOut = 3;
            }
        }



        public bool StartPanel()
        {
            try
            {
                mPanelProc = CommandConstants.CMD_PORT_INIT;
                mPanelIdleInterval = 0;
                mInTime = true;
                mLogProc = CommandConstants.CMD_PORT_INIT;
                LogThread = new Thread(LogThreadProccess);
                LogThread.Priority = ThreadPriority.AboveNormal;
                LogThread.IsBackground = true;
                LogThread.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public bool StopPanel()
        {
            try
            {
                mPanelClientLog.Close();
                mPanelClientLog.Dispose();
                LogThread.Abort();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }


        public void LogThreadProccess()
        {
            while (true)
            {
                Thread.Sleep(250);

                if (mActive == 0)
                    mLogProc = CommandConstants.CMD_PORT_DISABLED;


                switch (mLogProc)
                {
                    case CommandConstants.CMD_PORT_DISABLED:
                        {
                            SyncUpdateScreen("IPTAL", System.Drawing.Color.Red);

                            if (mMailRetryCount == 0)
                            {
                                SendMail("Panel Bağlantısı Yok! ", "<b>" + mPanelNo + " <i>Nolu Panel İle Bağlantı Sağlanamıyor.</i></b>", true);
                                mMailRetryCount++;
                            }
                            PanelDoorStatusDelete();
                            mLogProc = CommandConstants.CMD_PORT_CLOSE;
                        }
                        break;
                    case CommandConstants.CMD_PORT_INIT:
                        {
                            SyncUpdateScreen("AYARLANIYOR", System.Drawing.Color.SkyBlue);

                            mPanelClientLog = new TcpClient();
                            mPanelClientLog.ReceiveBufferSize = 0x1FFFF;
                            mPanelClientLog.SendBufferSize = 0x1FFFF;
                            mPanelClientLog.ReceiveTimeout = mTimeOut;
                            mPanelClientLog.SendTimeout = mTimeOut;

                            try
                            {
                                mPanelClientLog.Connect(mPanelIPAddress, mPanelTCPPortLog);
                                mLogProc = CommandConstants.CMD_PORT_CONNECT;
                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTimeOut);
                            }
                            catch (Exception)
                            {
                                mLogProc = CommandConstants.CMD_PORT_CLOSE;
                            }
                        }
                        break;
                    case CommandConstants.CMD_PORT_CONNECT:
                        {
                            SyncUpdateScreen("BAĞLANIYOR", System.Drawing.Color.Yellow);

                            mStartTime = DateTime.Now;

                            if (mStartTime > mEndTime)
                            {
                                mLogProc = CommandConstants.CMD_PORT_CLOSE;
                            }
                            else
                            {
                                if (mPanelClientLog.Connected == true)
                                {
                                    mLogProc = CommandConstants.CMD_TASK_LIST;
                                    mStartTime = DateTime.Now;
                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    mReceiveTimeStart = DateTime.Now;
                                    mReceiveTimeEnd = mReceiveTimeStart.AddSeconds(3);
                                }

                            }
                        }
                        break;

                    case CommandConstants.CMD_PORT_CLOSE:
                        {
                            SyncUpdateScreen("KAPATILIYOR", System.Drawing.Color.Yellow);

                            PanelDoorStatusDelete();
                            if (mPanelClientLog.Connected == true)
                            {
                                mPanelClientLog.Close();
                            }
                            mLogProc = CommandConstants.CMD_PORT_INIT;
                            Thread.Sleep(500);
                        }
                        break;
                    case CommandConstants.CMD_TASK_LIST:
                        {
                            while (true)
                            {
                                Thread.Sleep(5); //(50);
                                if (mPanelClientLog.Connected == false && mPanelClientLog.LingerState.Enabled == false)
                                {
                                    mLogProc = CommandConstants.CMD_PORT_CLOSE;
                                    break;
                                }
                                SyncUpdateScreen("HAZIR", System.Drawing.Color.Green);

                                mReceiveTimeStart = DateTime.Now;
                                if (mReceiveTimeStart > mReceiveTimeEnd)
                                {
                                    // Debug.WriteLine("Durdu" + mPanelNo.ToString());
                                    //if (mPanelNo == 15)
                                    //{
                                    //    Debug.WriteLine("Durdu");

                                    //}

                                    mLogProc = CommandConstants.CMD_PORT_CLOSE;
                                    break;

                                }

                                mStartTime = DateTime.Now;

                                //if (CheckSize(mPanelClientLog, (int)GetAnswerSize(CommandConstants.CMD_RCV_LOGS)))
                                if (mPanelClientLog.Available > (int)GetAnswerSize(CommandConstants.CMD_RCV_LOGS))
                                {
                                    mReceiveTimeEnd = mReceiveTimeStart.AddSeconds(3);

                                    //Debug.WriteLine("  ");
                                    //Debug.WriteLine(mPanelNo.ToString() + " Start: " + mReceiveTimeStart.ToString("yyyy-MM-dd HH:mm:ss"));
                                    //Debug.WriteLine(mPanelNo.ToString() + "  End:  " + mReceiveTimeEnd.ToString("yyyy-MM-dd HH:mm:ss"));

                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    if (ReveiveLogData(mPanelClientLog, ref mLogReturnStr))
                                    {
                                        if (ProcessReceivedData(mPanelNo, mPanelSerialNo, mLogTaskIntParam3, (CommandConstants)mTaskType, mLogTaskSource, mLogTaskUpdateTable, mLogReturnStr))
                                        {
                                            mLogTransferCompleted = true;
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }
                                else
                                {

                                    mLogProc = CommandConstants.CMD_TASK_LIST;
                                    break;
                                }

                                if (SndQueue.Count > 0)
                                {
                                    mLogProc = CommandConstants.CMD_SND_GLOBALDATAUPDATE;
                                    break;
                                }

                            }
                        }
                        break;
                    case CommandConstants.CMD_SND_GLOBALDATAUPDATE:
                        {


                            if (SendGenericDBData(mPanelClientLog))
                            {
                                mLogProc = CommandConstants.CMD_TASK_LIST;
                            }
                            else
                            {
                                mLogProc = CommandConstants.CMD_TASK_LIST;
                                break;
                            }

                        }
                        break;
                }
            }
        }




        //TODO:Beklenen Boyutla Gelen Boyutu Kıyaslama Yapıyor
        public bool CheckSize(TcpClient TClient, int TWaitSize)
        {
            try
            {
                int TRcvSize;
                TRcvSize = TClient.Available;
                if (TRcvSize >= TWaitSize)
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


        public bool ReveiveLogData(TcpClient TClient, ref string TReturnLogStr/*, CommandConstants TmpLogTaskType*/)
        {
            int TSize = GetAnswerSize(CommandConstants.CMD_RCV_LOGS);
            byte[] RcvBuffer = new byte[TSize];
            string TRcvData = null;
            int TPos;
            try
            {

                if (TClient.Available > 0)
                {
                    TClient.GetStream().Read(RcvBuffer, 0, TSize);
                    TRcvData = Encoding.UTF8.GetString(RcvBuffer);
                }
                else
                {
                    mLogProc = CommandConstants.CMD_PORT_CLOSE;
                    return false;
                }
                TPos = TRcvData.IndexOf("%" + GetCommandPrefix((ushort)CommandConstants.CMD_ADD_GLOBALDATAUPDATE));
                if (TPos > -1)
                {
                    TReturnLogStr = TRcvData;
                    mTaskType = (int)CommandConstants.CMD_ADD_GLOBALDATAUPDATE;
                    return true;
                }

                TPos = TRcvData.IndexOf("%" + GetCommandPrefix((ushort)CommandConstants.CMD_RCV_DOORSTATUS));
                if (TPos > -1)
                {
                    TReturnLogStr = TRcvData;
                    mTaskType = (int)CommandConstants.CMD_RCV_DOORSTATUS;
                    return true;
                }

                TPos = TRcvData.IndexOf("%" + GetCommandPrefix((ushort)CommandConstants.CMD_RCV_LOGS));
                if (TPos > -1)
                {
                    TReturnLogStr = TRcvData;
                    mTaskType = (int)CommandConstants.CMD_RCV_LOGS;
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


        //TODO:Değişkendeki dönen verinin veritabanına kayıt işlemlerini gerçekleştiriyor
        public bool ProcessReceivedData(int PanelNo, int PanelSerialNo, int DBIntParam3, CommandConstants TmpTaskType, ushort TmpTaskSoruce, bool TmpTaskUpdateTable, string TmpReturnStr)
        {
            StringBuilder TSndStr = new StringBuilder();
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            string tDBSQLStr2;
            SqlCommand tDBCmd2;
            SqlDataReader tDBReader;
            int TRetInt;
            int TPos;

            TPos = TmpReturnStr.IndexOf("%" + GetCommandPrefix((ushort)TmpTaskType));
            if (TPos < 0)
            {
                if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16) != mPanelSerialNo || Convert.ToInt32(TmpReturnStr.Substring(TPos + 7, 3)) != mPanelNo)
                {
                    return false;
                }
            }

            switch (TmpTaskType)
            {
                case CommandConstants.CMD_RCV_LOGS:
                    {

                        int TLocalBolgeNo = 1;
                        int TGlobalBolgeNo = 1;
                        int TMacSerial = 0;
                        int TPanel = 1;
                        int TReader = 1;
                        int TAccessResult = 0;
                        int TDoorType = 1;
                        long TUsersID = 0;
                        string TCardID = "";
                        string TLPR = "";
                        int TUserType = 1;
                        long TUserKayitNo = 0;
                        long TVisitorKayitNo = 0;
                        DateTime TDate = new DateTime();
                        string TmpName = "";
                        string TmpSurname = "";
                        string TmpTelefon = "";
                        int year = 0;
                        int month = 0;
                        int day = 0;
                        int hour = 0;
                        int minute = 0;
                        int second = 0;

                        if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16) == PanelSerialNo)
                        {
                            TMacSerial = Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16);
                            TPanel = Convert.ToInt32(TmpReturnStr.Substring(TPos + 7, 3));
                            if (TPanel > (int)TCONST.MAX_PANEL || TPanel < 1)
                                break;


                            TReader = Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 2));
                            TAccessResult = Convert.ToInt32(TmpReturnStr.Substring(TPos + 12, 2));
                            TDoorType = Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 1));
                            TUsersID = Convert.ToInt64(TmpReturnStr.Substring(TPos + 15, 6));
                            TCardID = ClearPreZeros(TmpReturnStr.Substring(TPos + 21, 10));
                            day = Convert.ToInt32(TmpReturnStr.Substring(TPos + 31, 2));
                            month = Convert.ToInt32(TmpReturnStr.Substring(TPos + 33, 2));
                            year = Convert.ToInt32(TmpReturnStr.Substring(TPos + 35, 2));
                            hour = Convert.ToInt32(TmpReturnStr.Substring(TPos + 37, 2));
                            minute = Convert.ToInt32(TmpReturnStr.Substring(TPos + 39, 2));
                            second = Convert.ToInt32(TmpReturnStr.Substring(TPos + 41, 2));
                            TDate = new DateTime(int.Parse("20" + year), month, day, hour, minute, second);

                            if (TUsersID > 100000 || TUsersID < 0)
                                break;

                            if (TmpReturnStr.Substring(TPos + 43, 10) != "**********" && TmpReturnStr.Substring(TPos + 43, 10) != "")
                                TLPR = TmpReturnStr.Substring(TPos + 43, 10);
                            else
                                TLPR = "";

                            if (TAccessResult <= 4)
                            {
                                if (TLPR.Trim() == "")
                                {
                                    if (int.Parse(TCardID) == 0 || TCardID.Trim() == "")
                                        break;
                                }
                            }

                            if (TAccessResult == 4)
                            {
                                TUsersID = 0;
                            }

                            if (TAccessResult <= 10)
                            {
                                if (TReader > 16 || TReader < 1)
                                    break;
                            }

                            if (TAccessResult < 0)
                            {
                                break;
                            }

                            if (TAccessResult > 13 && TAccessResult < 20)
                            {
                                break;
                            }

                            if (TAccessResult < 20)
                            {
                                if (TDoorType > 2 || TDoorType < 1)
                                    break;
                            }

                            if (TAccessResult >= 26 && TAccessResult <= 27)
                            {
                                if (int.Parse(TCardID) == 0 || TCardID.Trim() == "")
                                {
                                    TCardID = FindUserCardID(TUsersID);
                                }
                            }

                            if (IsDate(TDate.ToString()) == false)
                            {
                                break;
                            }

                            //TODO: Hangi kullanıcı olduğuna göre where koşulu uylunacak ProgInit'e
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();
                                    tDBSQLStr = "SELECT TOP 1 * FROM ProgInit ";
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    if (tDBReader.Read())
                                    {
                                        if (TAccessResult == 2)
                                            if ((tDBReader["LiveAPBInvalid"] as bool? ?? default(bool)) == true)
                                                break;

                                        if (TAccessResult == 0)
                                            if ((tDBReader["LiveDeniedInvalid"] as bool? ?? default(bool)) == true)
                                                break;

                                        if (TAccessResult == 4)
                                            if ((tDBReader["LiveUnknownInvalid"] as bool? ?? default(bool)) == true)
                                                break;

                                        if (TAccessResult == 5 || TAccessResult == 6 || TAccessResult == 7 || TAccessResult == 13)
                                            if ((tDBReader["LiveManuelInvalid"] as bool? ?? default(bool)) == true)
                                                break;

                                        if (TAccessResult == 8)
                                            if ((tDBReader["LiveButtonInvalid"] as bool? ?? default(bool)) == true)
                                                break;

                                        if (TAccessResult == 9 || TAccessResult == 10)
                                            if ((tDBReader["LiveProgrammedInvalid"] as bool? ?? default(bool)) == true)
                                                break;
                                    }
                                    tDBReader.Close();
                                }
                            }


                            if (TAccessResult < 4)
                            {
                                TUserType = 0;
                                TUserKayitNo = 0;

                                lock (TLockObj)
                                {
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
                                        tDBSQLStr = @"SELECT * FROM Users " +
                                      "WHERE [Kart ID] = '" + TCardID + "' " +
                                      "ORDER BY [Kayit No]";
                                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                        tDBReader = tDBCmd.ExecuteReader();
                                        if (tDBReader.Read())
                                        {
                                            TUserType = tDBReader["Kullanici Tipi"] as int? ?? default(int);
                                            TUserKayitNo = tDBReader["Kayit No"] as long? ?? default(long);
                                            TmpTelefon = tDBReader["Telefon"].ToString();
                                            TmpName = tDBReader["Adi"].ToString();
                                            TmpSurname = tDBReader["Soyadi"].ToString();
                                        }
                                        tDBReader.Close();
                                    }
                                }

                                TVisitorKayitNo = 0;
                                if (TUserType == 1)
                                {
                                    lock (TLockObj)
                                    {
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
                                            tDBSQLStr = @"SELECT * FROM Visitors " +
                                              "WHERE Visitors.[Kart ID] = '" + TCardID + "' " +
                                              "AND Visitors.Tarih = CONVERT(SMALLDATETIME,'" + TDate.ToString("MM/dd/yyyy HH:mm:ss") + "',101) " +
                                              "ORDER BY [Kayit No]";

                                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                            tDBReader = tDBCmd.ExecuteReader();
                                            if (tDBReader.Read())
                                            {
                                                TVisitorKayitNo = tDBReader["Kayit No"] as long? ?? default(long);
                                            }

                                            tDBReader.Close();
                                        }
                                    }
                                }

                            }

                            TLocalBolgeNo = LokalBolgeNo(TMacSerial, TReader);
                            if (TLocalBolgeNo < 1 && TLocalBolgeNo > 8)
                            {
                                TLocalBolgeNo = 1;
                            }
                            TGlobalBolgeNo = GlobalBolgeNo(TMacSerial, TLocalBolgeNo);
                            if (TGlobalBolgeNo < 1 && TGlobalBolgeNo > 999)
                            {
                                TGlobalBolgeNo = 1;
                            }
                        }
                        lock (TLockObj)
                        {
                            using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                            {
                                mDBConn.Open();
                                if (TAccessResult == 4)
                                    TUserKayitNo = 1;

                                tDBSQLStr = @"INSERT INTO AccessDatas " +
                                   "([Panel ID],[Lokal Bolge No],[Global Bolge No],[Kapi ID],ID,[Kart ID]," +
                                   "Plaka,Tarih,[Gecis Tipi],Kod,[Kullanici Tipi],[Visitor Kayit No]," +
                                   "[User Kayit No],Kontrol,[Canli Resim])" +
                                   "VALUES " +
                                   "(" +
                                   TPanel + "," + TLocalBolgeNo + "," + TGlobalBolgeNo + "," + TReader + "," +
                                   TUsersID + "," + TCardID + ",'" + TLPR + "','" + TDate.ToString("yyyy-MM-dd HH:mm:ss") + "'," +
                                   (TDoorType - 1) + "," + TAccessResult + "," + TUserType + "," + TVisitorKayitNo + "," +
                                   TUserKayitNo + "," + 0 + "," + "'user_1.jpg'" + ")";
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                TRetInt = tDBCmd.ExecuteNonQuery();
                                if (TRetInt <= 0)
                                {
                                    return false;
                                }
                            }
                        }
                    }
                    break;
                case CommandConstants.CMD_RCV_DOORSTATUS:
                    {
                        byte[] DoorStatus = new byte[16];
                        byte[] DoorSensor = new byte[16];
                        byte[] DoorButton = new byte[16];

                        byte FireAlarm = 0;
                        byte RobberAlarm = 0;
                        byte DoorAlarm = 0;
                        if (DoorStatusStr != TmpReturnStr)
                        {
                            DoorStatusStr = TmpReturnStr;
                            PanelDoorStatusDelete();
                            if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 4, 4), 16) == PanelSerialNo)
                            {
                                for (int i = 0; i <= 15; i++)
                                {
                                    DoorStatus[i] = Convert.ToByte(TmpReturnStr.Substring(TPos + 11 + i, 1));
                                    if (DoorStatus[i] > 1)
                                    {
                                        DoorStatus[i] = 0;
                                    }
                                    DoorSensor[i] = Convert.ToByte(TmpReturnStr.Substring(TPos + 27 + i, 1));
                                    if (DoorSensor[i] > 1)
                                    {
                                        DoorSensor[i] = 0;
                                    }
                                    DoorButton[i] = Convert.ToByte(TmpReturnStr.Substring(TPos + 43 + i, 1));
                                    if (DoorButton[i] > 1)
                                    {
                                        DoorButton[i] = 0;
                                    }
                                }
                                RobberAlarm = Convert.ToByte(TmpReturnStr.Substring(TPos + 59, 1));
                                if (RobberAlarm > 1)
                                    RobberAlarm = 0;

                                FireAlarm = Convert.ToByte(TmpReturnStr.Substring(TPos + 60, 1));
                                if (FireAlarm > 1)
                                {
                                    FireAlarm = 0;
                                }

                                DoorAlarm = Convert.ToByte(TmpReturnStr.Substring(TPos + 61, 1));
                                if (DoorAlarm > 1)
                                {
                                    DoorAlarm = 0;
                                }
                            }
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();

                                    tDBSQLStr = "SELECT * FROM DoorStatus WHERE [Seri No] = " + mPanelSerialNo.ToString().Trim() + " " +
                                     " AND [Panel ID] = " + mPanelNo.ToString().Trim();
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    if (!tDBReader.Read())
                                    {
                                        tDBSQLStr2 = "INSERT INTO DoorStatus " +
                                            "([Panel ID],[Seri No],[Hirsiz Alarm Durumu],[Yangin Alarm Durumu],[Kapi Alarm Durumu]," +
                                            "[Kapi 1 Baglanti],[Kapi 2 Baglanti],[Kapi 3 Baglanti],[Kapi 4 Baglanti],[Kapi 5 Baglanti],[Kapi 6 Baglanti],[Kapi 7 Baglanti],[Kapi 8 Baglanti]," +
                                            "[Kapi 9 Baglanti],[Kapi 10 Baglanti],[Kapi 11 Baglanti],[Kapi 12 Baglanti],[Kapi 13 Baglanti],[Kapi 14 Baglanti],[Kapi 15 Baglanti],[Kapi 16 Baglanti]," +
                                            "[Kapi 1 Sensor],[Kapi 2 Sensor],[Kapi 3 Sensor],[Kapi 4 Sensor],[Kapi 5 Sensor],[Kapi 6 Sensor],[Kapi 7 Sensor],[Kapi 8 Sensor]," +
                                            "[Kapi 9 Sensor],[Kapi 10 Sensor],[Kapi 11 Sensor],[Kapi 12 Sensor],[Kapi 13 Sensor],[Kapi 14 Sensor],[Kapi 15 Sensor],[Kapi 16 Sensor]," +
                                            "[Kapi 1 Button],[Kapi 2 Button],[Kapi 3 Button],[Kapi 4 Button],[Kapi 5 Button],[Kapi 6 Button],[Kapi 7 Button],[Kapi 8 Button]," +
                                            "[Kapi 9 Button],[Kapi 10 Button],[Kapi 11 Button],[Kapi 12 Button],[Kapi 13 Button],[Kapi 14 Button],[Kapi 15 Button],[Kapi 16 Button])" +
                                            "VALUES " +
                                            "(";
                                        tDBSQLStr2 += mPanelNo.ToString() + ",";
                                        tDBSQLStr2 += mPanelSerialNo.ToString() + ",";
                                        tDBSQLStr2 += RobberAlarm + ",";
                                        tDBSQLStr2 += FireAlarm + ",";
                                        tDBSQLStr2 += DoorAlarm + ",";
                                        for (int i = 0; i <= 15; i++)
                                        {
                                            tDBSQLStr2 += DoorStatus[i] + ",";
                                        }
                                        for (int i = 0; i <= 15; i++)
                                        {
                                            tDBSQLStr2 += DoorSensor[i] + ",";
                                        }
                                        for (int i = 0; i <= 15; i++)
                                        {
                                            if (i == 15)
                                            {
                                                tDBSQLStr2 += DoorButton[i] + ")";
                                            }
                                            else
                                            {
                                                tDBSQLStr2 += DoorButton[i] + ",";
                                            }

                                        }
                                        tDBReader.Close();
                                    }
                                    else
                                    {
                                        tDBSQLStr2 = "UPDATE DoorStatus " +
                                            "SET " +
                                            "[Panel ID] = " + mPanelNo.ToString() + "," +
                                            "[Seri No] = " + mPanelSerialNo.ToString() + "," +
                                            "[Hirsiz Alarm Durumu] = " + RobberAlarm + "," +
                                            "[Yangin Alarm Durumu] = " + FireAlarm + "," +
                                            "[Kapi Alarm Durumu] = " + DoorAlarm + "," +
                                            "[Kapi 1 Baglanti] = " + DoorStatus[0] + "," +
                                            "[Kapi 2 Baglanti] = " + DoorStatus[1] + "," +
                                            "[Kapi 3 Baglanti] = " + DoorStatus[2] + "," +
                                            "[Kapi 4 Baglanti] = " + DoorStatus[3] + "," +
                                            "[Kapi 5 Baglanti] = " + DoorStatus[4] + "," +
                                            "[Kapi 6 Baglanti] = " + DoorStatus[5] + "," +
                                            "[Kapi 7 Baglanti] = " + DoorStatus[6] + "," +
                                            "[Kapi 8 Baglanti] = " + DoorStatus[7] + "," +
                                            "[Kapi 9 Baglanti] = " + DoorStatus[8] + "," +
                                            "[Kapi 10 Baglanti] = " + DoorStatus[9] + "," +
                                            "[Kapi 11 Baglanti] = " + DoorStatus[10] + "," +
                                            "[Kapi 12 Baglanti] = " + DoorStatus[11] + "," +
                                            "[Kapi 13 Baglanti] = " + DoorStatus[12] + "," +
                                            "[Kapi 14 Baglanti] = " + DoorStatus[13] + "," +
                                            "[Kapi 15 Baglanti] = " + DoorStatus[14] + "," +
                                            "[Kapi 16 Baglanti] = " + DoorStatus[15] + "," +
                                            "[Kapi 1 Sensor] = " + DoorSensor[0] + "," +
                                            "[Kapi 2 Sensor] = " + DoorSensor[1] + "," +
                                            "[Kapi 3 Sensor] = " + DoorSensor[2] + "," +
                                            "[Kapi 4 Sensor] = " + DoorSensor[3] + "," +
                                            "[Kapi 5 Sensor] = " + DoorSensor[4] + "," +
                                            "[Kapi 6 Sensor] = " + DoorSensor[5] + "," +
                                            "[Kapi 7 Sensor] = " + DoorSensor[6] + "," +
                                            "[Kapi 8 Sensor] = " + DoorSensor[7] + "," +
                                            "[Kapi 9 Sensor] = " + DoorSensor[8] + "," +
                                            "[Kapi 10 Sensor] = " + DoorSensor[9] + "," +
                                            "[Kapi 11 Sensor] = " + DoorSensor[10] + "," +
                                            "[Kapi 12 Sensor] = " + DoorSensor[11] + "," +
                                            "[Kapi 13 Sensor] = " + DoorSensor[12] + "," +
                                            "[Kapi 14 Sensor] = " + DoorSensor[13] + "," +
                                            "[Kapi 15 Sensor] = " + DoorSensor[14] + "," +
                                            "[Kapi 16 Sensor] = " + DoorSensor[15] + "," +
                                            "[Kapi 1 Button] = " + DoorButton[0] + "," +
                                            "[Kapi 2 Button] = " + DoorButton[1] + "," +
                                            "[Kapi 3 Button] = " + DoorButton[2] + "," +
                                            "[Kapi 4 Button] = " + DoorButton[3] + "," +
                                            "[Kapi 5 Button] = " + DoorButton[4] + "," +
                                            "[Kapi 6 Button] = " + DoorButton[5] + "," +
                                            "[Kapi 7 Button] = " + DoorButton[6] + "," +
                                            "[Kapi 8 Button] = " + DoorButton[7] + "," +
                                            "[Kapi 9 Button] = " + DoorButton[8] + "," +
                                            "[Kapi 10 Button] = " + DoorButton[9] + "," +
                                            "[Kapi 11 Button] = " + DoorButton[10] + "," +
                                            "[Kapi 12 Button] = " + DoorButton[11] + "," +
                                            "[Kapi 13 Button] = " + DoorButton[12] + "," +
                                            "[Kapi 14 Button] = " + DoorButton[13] + "," +
                                            "[Kapi 15 Button] = " + DoorButton[14] + "," +
                                            "[Kapi 16 Button] = " + DoorButton[15];
                                    }
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
                        return true;
                    }
                case CommandConstants.CMD_ADD_GLOBALDATAUPDATE:
                    {
                        SyncUpdateScreen("GLOBAL DATA", System.Drawing.Color.Green);
                        object obj = new object();
                        lock (obj)
                        {
                            try
                            {
                                foreach (var logPanel in LogPanelListesi)
                                {
                                    if (logPanel.mPanelNo != mPanelNo)
                                    {
                                        logPanel.SndQueue.Enqueue(TmpReturnStr);
                                    }
                                }
                                return true;
                            }
                            catch (Exception)
                            {
                                return false;
                            }
                        }
                    }
                default:
                    break;
            }
            return false;
        }

        public bool SendMail(string subject, string body = null, bool isHtml = true)
        {
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

        //TODO:Veritabanından Gelen Görevi Panele Gönderme
        public bool SendGenericDBData(TcpClient TClient)
        {
            byte[] TSndBytes;
            try
            {
                string SendStr = SndQueue.Dequeue().ToString();

                var netStream = TClient.GetStream();
                if (netStream.CanWrite)
                {
                    TSndBytes = Encoding.UTF8.GetBytes(SendStr.ToString());
                    netStream.Write(TSndBytes, 0, TSndBytes.Length);
                    return true;
                }
                else
                {
                    SndQueue.Enqueue(SendStr);
                    return false;
                }
            }
            catch (Exception)
            {

                return false;
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
        }

        public void PanelDoorStatusDelete()
        {
            string tDBSQLStr;
            SqlCommand tDBCmd;
            object TLockObj = new object();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "DELETE FROM DoorStatus WHERE DoorStatus.[Panel ID] = " + mPanelNo;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBCmd.ExecuteNonQuery();
                }
            }
        }

        public string ClearPreZeros(string CardID)
        {
            string TSortStr;
            int TLen;
            TSortStr = CardID.Trim();
            TLen = CardID.Length;
            if (TLen > 0 && TSortStr != "0")
            {
                for (int i = 0; i <= TLen - 1; i++)
                {
                    if (TSortStr.Substring(0, 1) == "0")
                    {
                        if (i < (TLen - 1))
                        {
                            TSortStr = TSortStr.Substring(1);
                        }
                    }
                }
            }

            if (TSortStr == "")
                TSortStr = "0";

            return TSortStr;
        }

        public string FindUserCardID(long TempUser)
        {
            string FindUserCardID = "0";
            string FindUserBString = "";
            SqlCommand FindUserDBCommand;
            SqlDataReader FindUserDBReader;
            object TLockObj = new object();
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    FindUserBString = @"SELECT Users.ID, Users.[Kart ID] FROM Users " +
               "WHERE  Users.ID = " + TempUser + " " +
               "ORDER BY Users.ID";
                    FindUserDBCommand = new SqlCommand(FindUserBString, mDBConn);
                    FindUserDBReader = FindUserDBCommand.ExecuteReader();
                    if (FindUserDBReader.Read())
                    {
                        FindUserCardID = FindUserDBReader["Kart ID"].ToString().Trim();
                    }
                    else
                    {
                        FindUserCardID = "0";
                    }
                }
            }



            return FindUserCardID;
        }

        public int LokalBolgeNo(int MacSerial, int Reader)
        {
            string LBNDBString = "";
            SqlCommand LBNDBCommand;
            SqlDataReader LBNDBReader;
            object TLockObj = new object();
            int TLokalBolgeNo = 1;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    LBNDBString = "SELECT * FROM ReaderSettingsNew WHERE [Seri No]=" + MacSerial + " AND [WKapi ID]=" + Reader;
                    LBNDBCommand = new SqlCommand(LBNDBString, mDBConn);
                    LBNDBReader = LBNDBCommand.ExecuteReader();
                    if (LBNDBReader.Read())
                    {
                        TLokalBolgeNo = LBNDBReader["WKapi Lokal Bolge"] as int? ?? default(int);
                    }
                }
            }

            return TLokalBolgeNo;
        }

        public int GlobalBolgeNo(int MacSerial, int LokalBolgeNo)
        {
            string GBNDBString = "";
            SqlCommand GBNDBCommand;
            SqlDataReader GBNDBReader;
            object TLockObj = new object();
            int TGlobalBolgeNo = 1;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    GBNDBString = "SELECT * FROM PanelSettings WHERE [Seri No]=" + MacSerial;
                    GBNDBCommand = new SqlCommand(GBNDBString, mDBConn);
                    GBNDBReader = GBNDBCommand.ExecuteReader();
                    if (GBNDBReader.Read())
                    {
                        if (LokalBolgeNo == 1)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge1"] as int? ?? default(int);
                        if (LokalBolgeNo == 2)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge2"] as int? ?? default(int);
                        if (LokalBolgeNo == 3)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge3"] as int? ?? default(int);
                        if (LokalBolgeNo == 4)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge4"] as int? ?? default(int);
                        if (LokalBolgeNo == 5)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge5"] as int? ?? default(int);
                        if (LokalBolgeNo == 6)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge6"] as int? ?? default(int);
                        if (LokalBolgeNo == 7)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge7"] as int? ?? default(int);
                        if (LokalBolgeNo == 8)
                            TGlobalBolgeNo = GBNDBReader["Panel Global Bolge8"] as int? ?? default(int);
                    }
                }
            }

            return TGlobalBolgeNo;
        }

        //TODO:Görev Tipine Göre Komut Prefixini Getiriyor
        public string GetCommandPrefix(ushort DBTaskType)
        {
            switch (DBTaskType)
            {
                case (ushort)CommandConstants.CMD_RCV_TIMEGROUP:
                case (ushort)CommandConstants.CMD_RCVALL_TIMEGROUP:
                    return "SG";
                case (ushort)CommandConstants.CMD_RCV_RTC:
                    return "UR";
                case (ushort)CommandConstants.CMD_SND_TIMEGROUP:
                    return "NG";
                case (ushort)CommandConstants.CMD_SND_USER:
                    return "NL";
                case (ushort)CommandConstants.CMD_SND_ACCESSGROUP:
                    return "TN";
                case (ushort)CommandConstants.CMD_RCV_USER:
                    return "UL";
                case (ushort)CommandConstants.CMD_RCV_ACCESSGROUP:
                    return "TS";
                case (ushort)CommandConstants.CMD_ERS_USER:
                    return "DC";
                case (ushort)CommandConstants.CMD_ERSALL_USER:
                    return "EC";
                case (ushort)CommandConstants.CMD_ERS_ACCESSGROUP:
                    return "MS";
                //case (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP:
                //    return "ES";
                case (ushort)CommandConstants.CMD_ERS_ACCESSCOUNTERS:
                    return "EP";
                case (ushort)CommandConstants.CMD_ERSALL_ACCESSCOUNTERS:
                    return "ES";
                case (ushort)CommandConstants.CMD_ERS_APBCOUNTERS:
                    return "SA";
                case (ushort)CommandConstants.CMD_ERSALL_APBCOUNTERS:
                    return "PA";
                case (ushort)CommandConstants.CMD_SND_LOCALCAPACITYCOUNTERS:
                    return "GW";
                case (ushort)CommandConstants.CMD_SND_MAXUSERID:
                    return "MU";
                case (ushort)CommandConstants.CMD_ERSALL_LIFTGROUP:
                case (ushort)CommandConstants.CMD_ERS_LIFTGROUP:
                    return "EL";
                case (ushort)CommandConstants.CMD_ERS_USERALARM:
                case (ushort)CommandConstants.CMD_ERSALL_USERALARM:
                    return "EA";
                case (ushort)CommandConstants.CMD_ERS_ALARMFIRE_STATUS:
                    return "AS";
                case (ushort)CommandConstants.CMD_ERS_DOORALARM_STATUS:
                    return "ED";
                case (ushort)CommandConstants.CMD_RCV_RELAYPROGRAM:
                    return "ZR";
                case (ushort)CommandConstants.CMD_SNDALL_GROUPCALENDAR:
                case (ushort)CommandConstants.CMD_SND_GROUPCALENDAR:
                    return "CZ";
                case (ushort)CommandConstants.CMD_RCV_LOGCOUNT:
                    return "RC";
                case (ushort)CommandConstants.CMD_ERS_LOGCOUNT:
                    return "EM";
                case (ushort)CommandConstants.CMD_RCV_MAXINCOUNTERS:
                    return "MR";
                case (ushort)CommandConstants.CMD_RCV_ACCESSCOUNTERS:
                    return "RP";
                case (ushort)CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS:
                    return "GR";
                case (ushort)CommandConstants.CMD_RCV_LOGSETTINGS:
                    return "LR";
                case (ushort)CommandConstants.CMD_RCV_LIFTGROUP:
                    return "RG";
                case (ushort)CommandConstants.CMD_RCV_USERALARM:
                    return "RU";
                case (ushort)CommandConstants.CMD_SND_RTC:
                    return "DR";
                case (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS:
                    return "US";
                case (ushort)CommandConstants.CMD_SND_LOGSETTINGS:
                    return "LW";
                case (ushort)CommandConstants.CMD_RCV_LOCALINTERLOCK:
                    return "IR";
                case (ushort)CommandConstants.CMD_SND_LOCALINTERLOCK:
                    return "IW";
                case (ushort)CommandConstants.CMD_SND_DOORTRIGGER:
                case (ushort)CommandConstants.CMD_SND_DOORFREE:
                    return "DT";
                case (ushort)CommandConstants.CMD_SND_DOORFORCEOPEN:
                    return "FO";
                case (ushort)CommandConstants.CMD_SND_DOORFORCECLOSE:
                    return "FC";
                case (ushort)CommandConstants.CMD_SND_USERALARM:
                    return "AU";
                case (ushort)CommandConstants.CMD_SND_LIFTGROUP:
                    return "LG";
                case (ushort)CommandConstants.CMD_SND_GENERALSETTINGS:
                    return "DS";
                case (ushort)CommandConstants.CMD_SND_RELAYPROGRAM:
                    return "ZW";
                case (ushort)CommandConstants.CMD_ERSALL_TIMEGROUP:
                    return "EG";
                case (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP:
                    return "TE";
                case (ushort)CommandConstants.CMD_RCV_DOORSTATUS:
                    return "CMS";
                case (ushort)CommandConstants.CMD_RCV_LOGS:
                    return "CD";
                case (ushort)CommandConstants.CMD_ADD_GLOBALDATAUPDATE:
                    return "APB";
                default:
                    return "ERR";
            }
        }

        //TODO:Görev Tipine Göre Beklenen Cevap Boyutunu Getiriyor
        public int GetAnswerSize(CommandConstants TmpTaskType)
        {

            switch (TmpTaskType)
            {
                case CommandConstants.CMD_SND_TIMEGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_ACCESSGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_RTC:
                    return (int)SizeConstants.SIZE_SEND_RTC;
                case CommandConstants.CMD_SND_USER:
                    return (int)SizeConstants.SIZE_SEND_USER_INFO;
                case CommandConstants.CMD_RCV_USER:
                    return (int)SizeConstants.SIZE_RCV_USER;
                case CommandConstants.CMD_RCV_ACCESSGROUP:
                    return (int)SizeConstants.SIZE_RCV_ACCESSGROUP;
                case CommandConstants.CMD_ERS_USER:
                    return (int)SizeConstants.SIZE_ERSALL_USER;
                case CommandConstants.CMD_ERSALL_USER:
                    return (int)SizeConstants.SIZE_ERSALL_USER;
                case CommandConstants.CMD_ERS_ACCESSGROUP:
                case CommandConstants.CMD_ERSALL_ACCESSGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERSALL_ACCESSCOUNTERS:
                case CommandConstants.CMD_ERS_ACCESSCOUNTERS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERS_APBCOUNTERS:
                case CommandConstants.CMD_ERSALL_APBCOUNTERS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_LOCALCAPACITYCOUNTERS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_MAXUSERID:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERSALL_LIFTGROUP:
                case CommandConstants.CMD_ERS_LIFTGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERS_USERALARM:
                case CommandConstants.CMD_ERSALL_USERALARM:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERS_ALARMFIRE_STATUS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERS_DOORALARM_STATUS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_TIMEGROUP:
                    return (int)SizeConstants.SIZE_SEND_TIMEGROUP_INFO;
                case CommandConstants.CMD_RCV_RELAYPROGRAM:
                    return (int)SizeConstants.SIZE_ALARM_INFO;
                case CommandConstants.CMD_SND_GROUPCALENDAR:
                case CommandConstants.CMD_SNDALL_GROUPCALENDAR:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_LOGCOUNT:
                    return (int)SizeConstants.SIZE_SEND_LOG_COUNTER;
                case CommandConstants.CMD_ERS_LOGCOUNT:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_MAXINCOUNTERS:
                    return (int)SizeConstants.SIZE_READ_MAXIN_COUNTER_SINGLE;
                case CommandConstants.CMD_RCV_ACCESSCOUNTERS:
                    return (int)SizeConstants.SIZE_READ_ACCESS_COUNTER_SINGLE;
                case CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS:
                    return (int)SizeConstants.SIZE_LOCAL_CAPACITY_COUNTER;
                case CommandConstants.CMD_RCV_LOGSETTINGS:
                    return (int)SizeConstants.SIZE_LOCAL_CAPACITY_COUNTER;
                case CommandConstants.CMD_RCV_LIFTGROUP:
                    return (int)SizeConstants.SIZE_LIFT_GROUP;
                case CommandConstants.CMD_RCV_USERALARM:
                    return (int)SizeConstants.SIZE_ALARM_INFO;
                case CommandConstants.CMD_SND_RTC:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    return (int)SizeConstants.SIZE_SEND_DEVICE_SETTINGS;
                case CommandConstants.CMD_SND_LOGSETTINGS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_LOCALINTERLOCK:
                    return (int)SizeConstants.SIZE_RCV_LOCALINTERLOCK;
                case CommandConstants.CMD_SND_LOCALINTERLOCK:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_DOORTRIGGER:
                case CommandConstants.CMD_SND_DOORFORCEOPEN:
                case CommandConstants.CMD_SND_DOORFORCECLOSE:
                case CommandConstants.CMD_SND_DOORFREE:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_USERALARM:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_LIFTGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_GENERALSETTINGS:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_SND_RELAYPROGRAM:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_ERSALL_TIMEGROUP:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_LOGS:
                    return (int)SizeConstants.SIZE_STATUS_DATA;
                default:
                    return 0;
            }
        }

        //TODO:Ekrandaki Label Text'lerini Güncelleme
        delegate void TextDegisDelegate(string TMsg, System.Drawing.Color color);
        public void SyncUpdateScreen(string TMsg, System.Drawing.Color color)
        {
            Thread.Sleep(20);
            object frmMainLock = new object();
            lock (frmMainLock)
            {


                if (mParentForm.lblMsjLog[mMemIX].InvokeRequired == true)
                {
                    TextDegisDelegate del = new TextDegisDelegate(SyncUpdateScreen);
                    mParentForm.Invoke(del, new object[] { TMsg, color });

                }
                else
                {
                    if (TMsg != mParentForm.lblMsjLog[mMemIX].Text)
                    {
                        mParentForm.lblMsjLog[mMemIX].Text = TMsg;
                        mParentForm.lblMsjLog[mMemIX].BackColor = color;

                    }

                }
            }
        }

        //TODO:Görev Tipine Göre String Mesaj Dönderiyor
        public string GetScreenMessage(CommandConstants TmpTaskType)
        {
            switch (TmpTaskType)
            {

                case CommandConstants.CMD_SND_TIMEGROUP:
                    return "ZAMAN GRUBU GÖNDER";
                case CommandConstants.CMD_SNDALL_TIMEGROUP:
                    return "ZAMAN GRUBU GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_TIMEGROUP:
                    return "ZAMAN GRUBU AL";
                case CommandConstants.CMD_RCVALL_TIMEGROUP:
                    return "ZAMAN GRUBU AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_TIMEGROUP:
                    return "ZAMAN GRUBU SİL";
                case CommandConstants.CMD_ERSALL_TIMEGROUP:
                    return "ZAMAN GRUBU SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_ACCESSGROUP:
                    return "GEÇİŞ GRUBU GÖNDER";
                case CommandConstants.CMD_SNDALL_ACCESSGROUP:
                    return "GEÇİŞ GRUBU GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_ACCESSGROUP:
                    return "GEÇİŞ GRUBU AL";
                case CommandConstants.CMD_RCVALL_ACCESSGROUP:
                    return "GEÇİŞ GRUBU AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_ACCESSGROUP:
                    return "GEÇİŞ GRUBU SİL";
                case CommandConstants.CMD_ERSALL_ACCESSGROUP:
                    return "GEÇİŞ GRUBU SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_USER:
                    return "KULLANICI GÖNDER";
                case CommandConstants.CMD_SND_USER_LU:
                    return "KULLANICI GÖNDER";
                case CommandConstants.CMD_SNDALL_USER:
                    return "KULLANICI GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_SNDALL_USER_LU:
                    return "KULLANICI GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_USER:
                    return "KULLANICI AL";
                case CommandConstants.CMD_RCV_USER_LU:
                    return "KULLANICI AL";
                case CommandConstants.CMD_RCVALL_USER:
                    return "KULLANICI AL (TÜMÜ)";
                case CommandConstants.CMD_RCVALL_USER_LU:
                    return "KULLANICI AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_USER:
                    return "KULLANICI SİL";
                case CommandConstants.CMD_ERSALL_USER:
                    return "KULLANICI SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_MAXUSERID:
                    return "KULLANICI SAYISI GÖNDER";
                case CommandConstants.CMD_SND_LIFTGROUP:
                    return "ASANSÖR GRUBU GÖNDER";
                case CommandConstants.CMD_SNDALL_LIFTGROUP:
                    return "ASANSÖR GRUBU GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_LIFTGROUP:
                    return "ASANSÖR GRUBU AL";
                case CommandConstants.CMD_RCVALL_LIFTGROUP:
                    return "ASANSÖR GRUBU AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_LIFTGROUP:
                    return "ASANSÖR GRUBU SİL";
                case CommandConstants.CMD_ERSALL_LIFTGROUP:
                    return "ASANSÖR GRUBU SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_USERALARM:
                    return "KULLANICI ALARM GÖNDER";
                case CommandConstants.CMD_SNDALL_USERALARM:
                    return "KULLANICI ALARM GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_USERALARM:
                    return "KULLANICI ALARM AL";
                case CommandConstants.CMD_RCVALL_USERALARM:
                    return "KULLANICI ALARM AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_USERALARM:
                    return "KULLANICI ALARM SİL";
                case CommandConstants.CMD_ERSALL_USERALARM:
                    return "KULLANICI ALARM SİL (TÜMÜ)";
                case CommandConstants.CMD_ERS_ALARMFIRE_STATUS:
                    return "YANGIN-HIRSIZ ALARM İPTAL";
                case CommandConstants.CMD_ERS_DOORALARM_STATUS:
                    return "KAPI ALARM İPTAL";
                case CommandConstants.CMD_SND_GROUPCALENDAR:
                    return "GRUP TAKVİMİ GÖNDER";
                case CommandConstants.CMD_SNDALL_GROUPCALENDAR:
                    return "GRUP TAKVİMİ GÖNDER (TÜMÜ)";
                case CommandConstants.CMD_RCV_GROUPCALENDAR:
                    return "GRUP TAKVİMİ AL";
                case CommandConstants.CMD_RCVALL_GROUPCALENDAR:
                    return "GRUP TAKVİMİ AL (TÜMÜ)";
                case CommandConstants.CMD_ERS_GROUPCALENDAR:
                    return "GRUP TAKVİMİ SİL";
                case CommandConstants.CMD_ERSALL_GROUPCALENDAR:
                    return "GRUP TAKVİMİ SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_LOCALCAPACITYCOUNTERS:
                    return "LOKAL KAPASİTE AYARLA";
                case CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS:
                    return "LOKAL KAPASİTE AL";
                case CommandConstants.CMD_RCV_MAXINCOUNTERS:
                    return "İÇERDEKİ KİŞİ SAYISI AL";
                case CommandConstants.CMD_ERS_MAXINCOUNTERS:
                    return "İÇERDEKİ KİŞİ SAYISI SİL";
                case CommandConstants.CMD_ERSALL_MAXINCOUNTERS:
                    return "İÇERDEKİ KİŞİ SAYISI SİL (TÜMÜ)";
                case CommandConstants.CMD_RCV_SAMETAGCOUNTERS:
                    return "MÜKERRER ENGELLEME OKU";
                case CommandConstants.CMD_ERS_SAMETAGCOUNTERS:
                    return "MÜKERRER ENGELLEME İPTAL";
                case CommandConstants.CMD_ERSALL_SAMETAGCOUNTERS:
                    return "MÜKERRER ENGELLEME İPTAL (TÜMÜ)";
                case CommandConstants.CMD_RCV_ACCESSCOUNTERS:
                    return "GEÇİŞ SAYACI OKU";
                case CommandConstants.CMD_ERS_ACCESSCOUNTERS:
                    return "GEÇİŞ SAYACI SİL";
                case CommandConstants.CMD_ERSALL_ACCESSCOUNTERS:
                    return "GEÇİŞ SAYACI SİL (TÜMÜ)";
                case CommandConstants.CMD_ERS_APBCOUNTERS:
                    return "ANTIPASSBACK SAYACI SİL";
                case CommandConstants.CMD_ERSALL_APBCOUNTERS:
                    return "ANTİPASSBACK SAYACI SİL (TÜMÜ)";
                case CommandConstants.CMD_SND_RELAYPROGRAM:
                    return "KAPI RÖLE PROGRAMI GÖNDER";
                case CommandConstants.CMD_RCV_RELAYPROGRAM:
                    return "KAPI RÖLE PROGRAMI AL";
                case CommandConstants.CMD_ERS_RELAYPROGRAM:
                    return "KAPI RÖLE PROGRAMI SİL";
                case CommandConstants.CMD_SND_RTC:
                    return "PANEL SAATİ AYARLA";
                case CommandConstants.CMD_RCV_RTC:
                    return "PANEL SAATİ OKU";
                case CommandConstants.CMD_RCV_LOGCOUNT:
                    return "OLAY SAYISI OKU";
                case CommandConstants.CMD_ERS_LOGCOUNT:
                    return "OLAY SAYISI SİL";
                case CommandConstants.CMD_RCV_LOGS:
                    return "OLAY HAFIZASINI AL";
                case CommandConstants.CMD_SND_LOGSETTINGS:
                    return "OLAY HAFIZA AYARLARINI GÖNDER";
                case CommandConstants.CMD_RCV_LOGSETTINGS:
                    return "OLAY HAFIZA AYARLARINI AL";
                case CommandConstants.CMD_RCV_NEWACCESS:
                    return "ONLINE GEÇİŞ KONTROL";
                case CommandConstants.CMD_SND_AUTH:
                    return "ONLINE GEÇİŞ CEVAP";
                case CommandConstants.CMD_SND_GENERALSETTINGS:
                    return "GENEL AYARLARI GÖNDER";
                case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    return "GENEL AYARLARI AL";
                case CommandConstants.CMD_RCV_FIRMWAREVERSION:
                    return "FIRMWARE VERSIYON OKU";
                case CommandConstants.CMD_SND_DOORTRIGGER:
                    return "KAPI TETİKLE";
                case CommandConstants.CMD_SND_DOORFORCEOPEN:
                    return "KAPI AÇ (SUREKLI)";
                case CommandConstants.CMD_SND_DOORFORCECLOSE:
                    return "KAPI KAPAT (SUREKLI)";
                case CommandConstants.CMD_SND_DOORFREE:
                    return "KAPI SERBEST";
                default:
                    return "BILINMEYEN İŞLEM";
            }
        }

        //TODO:Kendi Yazdığım Kodlar*************************Kendi Yazdığım Kodlar**********************************
        public bool IsDate(string str)
        {
            try
            {
                DateTime tmp;
                if (DateTime.TryParse(str, out tmp) == true)
                    return true;
                else
                    return false;


            }
            catch (Exception)
            {

                return false;
            }
        }












    }
}
