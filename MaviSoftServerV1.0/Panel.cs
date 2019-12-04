using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
namespace MaviSoftServerV1._0
{
    public class Panel
    {
        public Panel(ushort MemIX, ushort TActive, int TPanelNo, ushort JTimeOut, string TIPAdress, int TMACAdress, int TCPPortOne, int TCPPortTwo, Form1 parentForm)
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
            mPanelTCPPortLog = (int)(mPanelTCPPortLog + Convert.ToUInt32(100));
            if (mTimeOut < 3 && mTimeOut > 60)
            {
                mTimeOut = 3;
            }
        }

        public const ushort NO_TASK = 0;

        public const ushort DB_TASK = 1;

        public const ushort IP_TASK = 2;

        public static int[] TaskPIX = new int[(int)TCONST.MAX_PANEL];

        public S_TASKLIST[,] TaskList = new S_TASKLIST[(int)TCONST.MAX_PANEL, (int)TCONST.MAX_TASK_CNT];

        private int mTaskNo;

        private int mTaskType;

        private int mTaskIntParam1;

        private int mTaskIntParam2;

        private int mTaskIntParam3;

        private int mTaskIntParam4;

        private int mTaskIntParam5;

        private string mTaskStrParam1;

        private string mTaskStrParam2;

        private string mTaskUserName;

        private bool mTaskUpdateTable;

        private ushort mTaskSource;

        private S_ANSWER mSAnswer;

        public bool mTransferCompleted { get; set; }

        public ushort mReadStep { get; set; }

        public bool mProcessTerminated { get; set; }

        public ushort mRetryCnt { get; set; }

        const ushort RETRY_COUNT = 1;

        public string mReturnStr;

        public Form1 mParentForm { get; set; }

        public Label lblMesaj;

        public Thread PanelThread { get; set; }

        public TcpClient mPanelClient { get; set; }

        public TcpClient mPanelClientLog { get; set; }

        public TcpListener mPanelListener { get; set; }

        public ushort mPanelIdleInterval { get; set; }

        public CommandConstants mPanelProc { get; set; }

        public ushort mPanelConState { get; set; }

        public int mPanelTCPPort { get; set; }

        public int mPanelTCPPortLog { get; set; }

        public string mPanelIPAddress { get; set; }

        public int mPanelSerialNo { get; set; }

        public DateTime mStartTime { get; set; }

        public DateTime mEndTime { get; set; }

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


        public bool StartPanel()
        {
            try
            {
                mPanelProc = CommandConstants.CMD_PORT_INIT;
                mPanelIdleInterval = 0;
                mInTime = true;

                mDBConn = new SqlConnection();
                mDBConn.ConnectionString = @"data source = ARGE-2\SQLEXPRESS; initial catalog = MW301_DB25; integrated security = True; MultipleActiveResultSets = True;";
                mDBConn.Open();

                PanelThread = new Thread(ProcessPanel);
                PanelThread.IsBackground = true;
                PanelThread.Start();
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }




        public void ProcessPanel()
        {
            ushort TProc = 0;
            while (true)
            {
                Thread.Sleep(5);

                if (mActive == 0)
                {
                    mPanelProc = CommandConstants.CMD_PORT_DISABLED;
                }

                switch (mPanelProc)
                {
                    case CommandConstants.CMD_PORT_DISABLED:
                        {

                            SyncUpdateScreen("IPTAL");
                        }
                        break;

                    case CommandConstants.CMD_PORT_INIT:
                        {

                            SyncUpdateScreen("AYARLANIYOR");
                            mPanelClient = new TcpClient();
                            mPanelClient.ReceiveBufferSize = 1024;
                            mPanelClient.SendBufferSize = 1024;
                            mPanelClient.ReceiveTimeout = mTimeOut;
                            mPanelClient.SendTimeout = mTimeOut;

                            //mPanelClientLog = new TcpClient();
                            //mPanelClientLog.ReceiveBufferSize = 1024;
                            //mPanelClientLog.SendBufferSize = 1024;
                            //mPanelClientLog.ReceiveTimeout = mTimeOut;
                            //mPanelClientLog.SendTimeout = mTimeOut;

                            try
                            {
                                mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                //  mPanelClientLog.Connect(mPanelIPAddress, mPanelTCPPortLog);
                                mPanelProc = CommandConstants.CMD_PORT_CONNECT;

                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTimeOut);

                            }
                            catch (Exception)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_DISABLED;
                            }

                        }
                        break;

                    case CommandConstants.CMD_PORT_CONNECT:
                        {

                            SyncUpdateScreen("BAĞLANIYOR");

                            mStartTime = DateTime.Now;

                            if (mStartTime > mEndTime)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                            }
                            else
                            {
                                if (mPanelClient.Connected == true)
                                {
                                    mPanelProc = CommandConstants.CMD_TASK_LIST;
                                }

                            }
                        }
                        break;

                    case CommandConstants.CMD_PORT_CLOSE:
                        {
                            SyncUpdateScreen("KAPATILIYOR");
                            if (mPanelClient.Connected == true)
                            {
                                mPanelClient.Close();
                                //if (mPanelClientLog.Connected == true)
                                //{
                                //    mPanelClientLog.Close();
                                //}
                            }
                            mPanelProc = CommandConstants.CMD_PORT_INIT;
                            Thread.Sleep(500);

                        }
                        break;

                    case CommandConstants.CMD_PORT_TEST:
                        {
                            //Port Test (Read RTC Command)
                            SyncUpdateScreen("PORT TEST");//, Color.Yellow)

                            mTransferCompleted = true;
                            mReadStep = 0;
                            while ((mReadStep < 1) && (mTransferCompleted == true) && (mProcessTerminated == false))
                            {
                                mRetryCnt = 0;
                                mTransferCompleted = false;

                                while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false) && (mProcessTerminated == false))
                                {

                                    ClearSocketBuffers(mPanelClient);

                                    SendTestCommand(mPanelClient);

                                    mStartTime = DateTime.Now;
                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    do
                                    {
                                        Thread.Sleep(20);
                                        mStartTime = DateTime.Now;
                                    } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                    if (mProcessTerminated == true)
                                        break;

                                    if (mStartTime >= mEndTime)
                                    {
                                        //Display Timeout&Retrying Message
                                        SyncUpdateScreen("ZAMAN AŞIMI");//, Color.LightPink)
                                        mRetryCnt++;
                                    }
                                    else
                                    {
                                        if (!ReceiveTestCommand(mPanelClient))
                                            break;
                                        else
                                            mTransferCompleted = true;
                                    }
                                }

                                mReadStep += 1;
                            }

                            if (mTransferCompleted == true)
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            else
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;

                        }
                        break;

                    case CommandConstants.CMD_TASK_LIST:
                        {
                            if (mPanelClient.Connected == false)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            ClearSocketBuffers(mPanelClient);
                            SyncUpdateScreen("HAZIR");
                            Thread.Sleep(250);

                            mTaskSource = SyncGetNewTask();
                            //if (mTaskSource == IP_TASK)
                            //{
                            //    TProc = TaskList[mMemIX, TaskPIX[mMemIX]].CmdID;
                            //}
                            if (mTaskSource == DB_TASK)
                            {
                                TProc = (ushort)mTaskType;
                                if (TProc > 0)
                                {
                                    mPanelProc = (CommandConstants)TProc;
                                }
                                else
                                {
                                    mPanelProc = CommandConstants.CMD_TASK_LIST;
                                }
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }


                            //if (TProc != (ushort)mPanelProc && TProc != 0)
                            //{
                            //    mPanelProc = (CommandConstants)TProc;
                            //}

                        }
                        break;

                    case CommandConstants.CMD_RCV_USER:
                    case CommandConstants.CMD_RCVALL_USER:
                    case CommandConstants.CMD_RCV_ACCESSGROUP:
                    case CommandConstants.CMD_RCVALL_LOCALCAPACITYCOUNTERS:
                    case CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS:
                    case CommandConstants.CMD_RCV_TIMEGROUP:
                    case CommandConstants.CMD_RCV_RELAYPROGRAM:
                    case CommandConstants.CMD_RCV_LOGCOUNT:
                    case CommandConstants.CMD_RCV_MAXINCOUNTERS:
                    case CommandConstants.CMD_RCV_ACCESSCOUNTERS:
                    case CommandConstants.CMD_RCV_LOGSETTINGS:
                    case CommandConstants.CMD_RCV_LIFTGROUP:
                    case CommandConstants.CMD_RCV_USERALARM:
                    case CommandConstants.CMD_RCV_LOGS:
                    case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    case CommandConstants.CMD_RCV_LOCALINTERLOCK:
                        {
                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            SyncUpdateScreen(GetScreenMessage((CommandConstants)mTaskType));
                            mTransferCompleted = true;
                            mReadStep = 0;
                            while ((mReadStep < 1) && (mTransferCompleted == true) && (mProcessTerminated == false))
                            {
                                mRetryCnt = 0;
                                mTransferCompleted = false;

                                while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false) && (mProcessTerminated == false))
                                {

                                    ClearSocketBuffers(mPanelClient);

                                    //ReciveGenericDBData(mPanelClient, mTaskIntParam1, 0, 0, (CommandConstants)mTaskType);
                                    SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, (ushort)mTaskType);
                                    mStartTime = DateTime.Now;
                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    do
                                    {
                                        Thread.Sleep(20);
                                        mStartTime = DateTime.Now;
                                    } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                    if (mProcessTerminated == true)
                                        break;

                                    if (mStartTime >= mEndTime)
                                    {
                                        //Display Timeout&Retrying Message
                                        SyncUpdateScreen("ZAMAN AŞIMI");//, Color.LightPink)
                                        mRetryCnt++;
                                    }
                                    else
                                    {
                                        if (GAReciveGenericDBData(mPanelClient, ref mReturnStr, (CommandConstants)mTaskType))
                                        {
                                            if (ProcessReceivedData(mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, (CommandConstants)mTaskType, mTaskSource, mTaskUpdateTable, mReturnStr))
                                            {
                                                mTransferCompleted = true;
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                }

                                mReadStep += 1;
                            }

                            if (mTransferCompleted == true)
                            {
                                if (mTaskSource == IP_TASK)
                                {
                                    TransferAnswer((ushort)CommandConstants.CMD_OK);
                                    DeleteTaskFromTaskList();
                                }
                                else
                                {
                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            else
                            {
                                if (mTaskSource == IP_TASK)
                                {
                                    TransferAnswer((ushort)CommandConstants.CMD_NOTPROCESSED);
                                    DeleteTaskFromTaskList();
                                }
                                else
                                {
                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_ERROR);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                        }
                        break;
                    case CommandConstants.CMD_SNDALL_USER:
                    case CommandConstants.CMD_SND_USER:
                    case CommandConstants.CMD_SND_ACCESSGROUP:
                    case CommandConstants.CMD_SND_TIMEGROUP:
                    case CommandConstants.CMD_ERS_USER:
                    case CommandConstants.CMD_ERSALL_USER:
                    case CommandConstants.CMD_ERS_ACCESSGROUP:
                    case CommandConstants.CMD_ERSALL_ACCESSGROUP:
                    case CommandConstants.CMD_ERS_APBCOUNTERS:
                    case CommandConstants.CMD_ERSALL_APBCOUNTERS:
                    case CommandConstants.CMD_SND_LOCALCAPACITYCOUNTERS:
                    case CommandConstants.CMD_SND_MAXUSERID:
                    case CommandConstants.CMD_ERS_LIFTGROUP:
                    case CommandConstants.CMD_ERSALL_LIFTGROUP:
                    case CommandConstants.CMD_ERS_USERALARM:
                    case CommandConstants.CMD_ERSALL_USERALARM:
                    case CommandConstants.CMD_ERS_ALARMFIRE_STATUS:
                    case CommandConstants.CMD_ERS_DOORALARM_STATUS:
                    case CommandConstants.CMD_SNDALL_GROUPCALENDAR:
                    case CommandConstants.CMD_SND_GROUPCALENDAR:
                    case CommandConstants.CMD_ERS_LOGCOUNT:
                    case CommandConstants.CMD_SND_LOGSETTINGS:
                    case CommandConstants.CMD_SND_LOCALINTERLOCK:
                    case CommandConstants.CMD_ERS_ACCESSCOUNTERS:
                    case CommandConstants.CMD_ERSALL_ACCESSCOUNTERS:
                    case CommandConstants.CMD_SND_DOORTRIGGER:
                    case CommandConstants.CMD_SND_DOORFORCEOPEN:
                    case CommandConstants.CMD_SND_DOORFORCECLOSE:
                    case CommandConstants.CMD_SND_DOORFREE:
                    case CommandConstants.CMD_SND_USERALARM:
                    case CommandConstants.CMD_SND_LIFTGROUP:
                    case CommandConstants.CMD_SND_GENERALSETTINGS:
                    case CommandConstants.CMD_SND_RELAYPROGRAM:
                        {
                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            SyncUpdateScreen(GetScreenMessage((CommandConstants)mTaskType));
                            mTransferCompleted = true;
                            mReadStep = 0;
                            while ((mReadStep < 1) && (mTransferCompleted == true) && (mProcessTerminated == false))
                            {
                                mRetryCnt = 0;
                                mTransferCompleted = false;

                                while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false) && (mProcessTerminated == false))
                                {

                                    ClearSocketBuffers(mPanelClient);

                                    SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, (ushort)mTaskType);

                                    mStartTime = DateTime.Now;
                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    do
                                    {
                                        Thread.Sleep(20);
                                        mStartTime = DateTime.Now;
                                    } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                    if (mProcessTerminated == true)
                                        break;

                                    if (mStartTime >= mEndTime)
                                    {
                                        //Display Timeout&Retrying Message
                                        SyncUpdateScreen("ZAMAN AŞIMI");//, Color.LightPink)
                                        mRetryCnt++;
                                    }
                                    else
                                    {
                                        if (!ReceiveGenericAnswerData(mPanelClient, (CommandConstants)mTaskType))
                                        {
                                            break;
                                        }
                                        else
                                        {
                                            mTransferCompleted = true;
                                        }
                                    }
                                }
                                mReadStep += 1;
                            }

                            if (mTransferCompleted == true)
                            {
                                if (mTaskSource == IP_TASK)
                                {
                                    TransferAnswer((ushort)CommandConstants.CMD_OK);
                                    DeleteTaskFromTaskList();
                                }
                                else
                                {
                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            else
                            {
                                if (mTaskSource == IP_TASK)
                                {
                                    TransferAnswer((ushort)CommandConstants.CMD_NOTPROCESSED);
                                    DeleteTaskFromTaskList();
                                }
                                else
                                {
                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_ERROR);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }

                        }
                        break;

                    case CommandConstants.CMD_RCV_RTC:
                        {
                            if (!mPanelClient.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }
                            SyncUpdateScreen("SAAT OKUMA");
                            Thread.Sleep(250);

                            mTransferCompleted = true;
                            mReadStep = 0;
                            while ((mReadStep < 1) && (mTransferCompleted == true) && (mProcessTerminated == false))
                            {
                                mRetryCnt = 0;
                                mTransferCompleted = false;

                                while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false) && (mProcessTerminated == false))
                                {

                                    ClearSocketBuffers(mPanelClient);
                                    SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, (ushort)mTaskType);
                                    //SendIPTaskCommand(mPanelClient, 0, 0, 0, (ushort)mTaskType);

                                    mStartTime = DateTime.Now;
                                    mEndTime = mStartTime.AddSeconds(mTimeOut);
                                    do
                                    {
                                        Thread.Sleep(20);
                                        mStartTime = DateTime.Now;
                                    } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                    if (mProcessTerminated == true)
                                        break;

                                    if (mStartTime >= mEndTime)
                                    {
                                        //Display Timeout&Retrying Message
                                        SyncUpdateScreen("ZAMAN AŞIMI");//, Color.LightPink)
                                        mRetryCnt++;
                                    }
                                    else
                                    {
                                        if (ReceiveIPTaskCommand(mPanelClient, ref mReturnStr, (ushort)mTaskType))
                                        {
                                            SyncUpdateScreen(mReturnStr);
                                            Thread.Sleep(1000);
                                            break;
                                        }
                                        else
                                            mTransferCompleted = true;
                                    }
                                }

                                mReadStep += 1;
                            }

                            if (mTransferCompleted == true)
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            else
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                        }
                        break;

                    case CommandConstants.CMD_ZERO:
                        {
                            DeleteTaskFromTaskList();
                            mPanelProc = CommandConstants.CMD_TASK_LIST;
                        }
                        break;

                    default:
                        {
                            mPanelProc = CommandConstants.CMD_PORT_DISABLED;
                            break;
                        }

                }


            }

        }




        //TODO:Görev Listesinden Durumu Yeni Olan Görevleri Alıyor
        public ushort SyncGetNewTask()
        {
            object TLockObj = new object();
            ushort TTaskOk = 0;


            //lock (TLockObj)
            //{
            //    for (int i = 0; i < (int)TCONST.MAX_TASK_CNT; i++)
            //    {
            //        if (TaskPIX[mMemIX] < (int)TCONST.MAX_TASK_CNT-1)
            //        {
            //            TaskPIX[mMemIX] += 1;
            //        }
            //        else
            //        {
            //            TaskPIX[mMemIX] = 0;
            //        }
            //        if (TaskList[mMemIX, TaskPIX[mMemIX]].CmdID != 0)
            //        {
            //            break;
            //        }
            //    }

            //    if (TaskList[mMemIX, TaskPIX[mMemIX]].CmdID > 0)
            //    {
            //        return IP_TASK;
            //    }
            //}

            lock (TLockObj)
            {
                //DB TASK

                //mDBSQLStr = "Select * from TaskList where [Panel No]=" + mPanelNo + " And [Durum Kodu]=0 Order By [Grup No]";
                mDBSQLStr = "Select * from TaskList where [Panel No]=" + mPanelNo + " AND [Durum Kodu]=" + 1 + " Order By [Kayit No]";
                mDBCmd = new SqlCommand(mDBSQLStr, mDBConn);
                mDBReader = mDBCmd.ExecuteReader();

                while (mDBReader.Read())
                {
                    if ((mDBReader["Kayit No"] as int? ?? default(int)) > 0 && (mDBReader["Gorev Kodu"] as int? ?? default(int)) > 0 && (mDBReader["IntParam 1"] as int? ?? default(int)) > 0)
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
                    //mTaskStrParam1 = mDBReader["StrParam 1"].ToString();
                    //mTaskStrParam2 = mDBReader["StrParam 2"].ToString();
                    mTaskUserName = mDBReader["Kullanici Adi"].ToString();
                    mTaskUpdateTable = (bool)mDBReader["Tablo Guncelle"];
                    return DB_TASK;
                }
                else
                {
                    mTaskNo = 0;
                    mTaskType = 0;
                    return NO_TASK;

                }

            }
        }

        //TODO:Veritabanından Gelen Görevi Panele Gönderme
        public bool SendGenericDBData(TcpClient TClient, int TmpIntParam1, int TmpIntParam2, int TmpIntParam3, ushort TmpTaskType)
        {
            StringBuilder TSndStr = new StringBuilder();
            byte[] TSndBytes;

            TSndStr = BuiltDBCommandString(TmpIntParam1, TmpIntParam2, TmpIntParam3, TmpTaskType);

            try
            {
                var netStream = TClient.GetStream();
                if (netStream.CanWrite)
                {
                    TSndBytes = Encoding.UTF8.GetBytes(TSndStr.ToString());
                    netStream.Write(TSndBytes, 0, TSndBytes.Length);
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

        //TODO:Gelen Görev Tipine Göre Panele Gönderilecek Olan Komutu Oluşturma
        public StringBuilder BuiltDBCommandString(int DBIntParam1, int DBIntParam2, int DBIntParam3, ushort DBTaskType)
        {

            StringBuilder TSndStr = new StringBuilder();
            ushort TDataInt;
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader = null;
            string tDBSQLStr2;
            SqlCommand tDBCmd2;
            SqlDataReader tDBReader2 = null;
            DateTime tDate = DateTime.Now;


            /*2*/
            if (DBTaskType == (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*3*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GENERALSETTINGS)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM PanelSettings " +
                        "WHERE [Panel ID] = " + DBIntParam1.ToString();
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));

                        if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 0)
                            TSndStr.Append("0");
                        else
                            TSndStr.Append("1");

                        TSndStr.Append("000000");

                        for (int i = 1; i < 9; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel M" + i + " Role"] as int? ?? default(int), "D2"));
                        }

                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Alarm Role"] as int? ?? default(int), "D2"));

                        for (int i = 1; i < 5; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel GW" + i] as int? ?? default(int), "D3"));
                        }

                        for (int i = 1; i < 5; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel IP" + i] as int? ?? default(int), "D3"));
                        }

                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel TCP Port"] as int? ?? default(int), "D5"));

                        for (int i = 1; i < 5; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Subnet" + i] as int? ?? default(int), "D3"));
                        }

                        for (int i = 1; i < 5; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Remote IP" + i] as int? ?? default(int), "D3"));
                        }

                        TSndStr.Append("00"); // Panel Buton Detector - Panel Buton Detector Type

                        if ((tDBReader["Global Zone Interlock Active"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");


                        if ((tDBReader["Same Door Multiple Reader"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");


                        if ((tDBReader["Interlock Active"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");


                        TSndStr.Append(ConvertToTypeInt(tDBReader["Lift Capacity"] as int? ?? default(int), "D1"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Expansion"] as int? ?? default(int), "D1"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Expansion 2"] as int? ?? default(int), "D1"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Model"] as int? ?? default(int), "D1"));
                        if ((tDBReader["Status Data Update Time"] as int? ?? default(int)) == 0)
                            TSndStr.Append("0");//Değişimde
                        else
                            TSndStr.Append("1");//Her Saniye

                        if ((tDBReader["Status Data Update"] as bool? ?? default(bool)))
                            TSndStr.Append("1");//Gönder
                        else
                            TSndStr.Append("0");//Gönder

                        if ((tDBReader["Status Data Update Type"] as int? ?? default(int)) == 0)
                            TSndStr.Append("0");//Remote IP
                        else
                            TSndStr.Append("1");//Yayın

                        for (int i = 1; i < 9; i++)
                        {
                            if ((tDBReader["Lokal APB" + i] as bool? ?? default(bool)))
                                TSndStr.Append("1");
                            else
                                TSndStr.Append("0");
                        }

                        if ((tDBReader["Global APB"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Global MaxIn Count Control"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Global Access Count Control"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Global Capacity Control"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Global Sequental Access Control"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block"] as int? ?? default(int), "D3"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block Type"] as int? ?? default(int), "D1"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block HourMinSec"] as int? ?? default(int), "D1"));

                        if ((tDBReader["Panel Alarm Mode"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Alarm Mode Role Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Fire Mode"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Fire Mode Role Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Door Alarm Role Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Alarm Broadcast Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Fire Broadcast Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        if ((tDBReader["Panel Door Alarm Broadcast Ok"] as bool? ?? default(bool)))
                            TSndStr.Append("1");
                        else
                            TSndStr.Append("0");

                        for (int i = 1; i < 9; i++)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Global Bolge" + i] as int? ?? default(int), "D3"));
                        }

                        for (int i = 1; i < 9; i++)
                        {
                            if ((tDBReader["Panel Local Capacity" + i] as bool? ?? default(bool)))
                                TSndStr.Append("1");
                            else
                                TSndStr.Append("0");

                            if ((tDBReader["Panel Local Capacity Clear" + i] as bool? ?? default(bool)))
                                TSndStr.Append("1");
                            else
                                TSndStr.Append("0");

                            TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Local Capacity Value" + i] as int? ?? default(int), "D6"));
                        }

                        if (tDBReader["Panel Name"].ToString().Length < 16)
                        {
                            string space = "";
                            for (int i = 0; i < 16 - tDBReader["Panel Name"].ToString().Length; i++)
                            {
                                space += " ";
                            }
                            TSndStr.Append(tDBReader["Panel Name"].ToString() + space);
                        }
                        else
                        {
                            TSndStr.Append(tDBReader["Panel Name"].ToString().Substring(0, 16));
                        }

                        /*WIG Reader Settings*/

                        for (int i = 1; i < 17; i++)
                        {
                            tDBSQLStr2 = "SELECT * FROM ReaderSettingsNew " +
                                "WHERE [Panel ID] = " + DBIntParam1 + " AND [WKapi ID] = " + i;
                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                            tDBReader2 = tDBCmd2.ExecuteReader();
                            if (tDBReader2.Read())
                            {
                                if ((tDBReader2["WKapi Aktif"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Role No"] as int? ?? default(int), "D2"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Tipi"] as int? ?? default(int), "D1"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi WIGType"] as int? ?? default(int), "D1"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Lokal Bolge"] as int? ?? default(int), "D1"));

                                if ((tDBReader2["WKapi Sirali Gecis Ana Kapi"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                if ((tDBReader2["WKapi Coklu Onay"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                if ((tDBReader2["WKapi Alarm Modu"] as int? ?? default(int)) == 0)
                                    TSndStr.Append("0");
                                else
                                    TSndStr.Append("1");

                                if ((tDBReader2["WKapi Yangin Modu"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                if ((tDBReader2["WKapi Pin Dogrulama"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                TSndStr.Append("0");//Parking Gate


                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Acik Sure"] as int? ?? default(int), "D3"));

                                if ((tDBReader2["WKapi Acik Sure Alarmi"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");


                                if ((tDBReader2["WKapi Zorlama Alarmi"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");


                                if ((tDBReader2["WKapi Acilma Alarmi"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                if ((tDBReader2["WKapi Panik Buton Alarmi"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Harici Alarm Rolesi"] as int? ?? default(int), "D2"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Itme Gecikmesi"] as int? ?? default(int), "D1"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi User Count"] as int? ?? default(int), "D1"));

                                if (tDBReader2["WKapi Adi"].ToString().Length < 15)
                                {
                                    string space = "";
                                    for (int j = 0; j < 15 - tDBReader2["WKapi Adi"].ToString().Length; j++)
                                    {
                                        space += " ";
                                    }
                                    TSndStr.Append(tDBReader2["WKapi Adi"].ToString() + space);
                                }
                                else
                                {
                                    TSndStr.Append(tDBReader2["WKapi Adi"].ToString().Substring(0, 15));
                                }

                                TSndStr.Append("0");//Buton detector function

                            }
                        }
                        TSndStr.Append("**\r");
                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }
                }
            }
            /*4*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_TIMEGROUP)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM TimeGroups " +
                        "WHERE [Zaman Grup No]=" + DBIntParam1.ToString();
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {

                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Zaman Grup No"] as int? ?? default(int), "D4"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int), "D2"));
                        if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 0 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 7)
                        {
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000" + "000000000000");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 1 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 2)
                        {
                            if (IsDate(tDBReader["Baslangic Tarihi"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeDatetime(tDBReader["Baslangic Tarihi"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("000000");
                            }
                            if (IsDate(tDBReader["Bitis Tarihi"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeDatetime(tDBReader["Bitis Tarihi"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("000000");
                            }
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");

                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 3 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 4)
                        {
                            if (IsDate(tDBReader["Baslangic Saati"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTimeWithSecond(tDBReader["Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("000000");
                            }
                            if (IsDate(tDBReader["Bitis Saati"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTimeWithSecond(tDBReader["Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("000000");
                            }
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 5)
                        {
                            TDataInt = 0;
                            if ((bool)tDBReader["Pazartesi"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Sali"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Carsamba"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Persembe"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Cuma"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Cumartesi"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Pazar"] == true)
                                TDataInt += 64;
                            TSndStr.Append(TDataInt.ToString("X2"));
                            if ((tDBReader["Ilave Saat Kontrolu"] as bool? ?? default(bool)))
                            {
                                TSndStr.Append("01");
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Ilave Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Ilave Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000000000");
                            }
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 6)
                        {
                            //Monthly
                            TDataInt = 0;
                            if ((bool)tDBReader["Gun1"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Gun2"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Gun3"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Gun4"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Gun5"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Gun6"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Gun7"] == true)
                                TDataInt += 64;
                            if ((bool)tDBReader["Gun8"] == true)
                                TDataInt += 128;
                            TSndStr.Append(TDataInt.ToString("X2"));

                            TDataInt = 0;
                            if ((bool)tDBReader["Gun9"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Gun10"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Gun11"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Gun12"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Gun13"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Gun14"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Gun15"] == true)
                                TDataInt += 64;
                            if ((bool)tDBReader["Gun16"] == true)
                                TDataInt += 128;
                            TSndStr.Append(TDataInt.ToString("X2"));

                            TDataInt = 0;
                            if ((bool)tDBReader["Gun17"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Gun18"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Gun19"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Gun20"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Gun21"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Gun22"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Gun23"] == true)
                                TDataInt += 64;
                            if ((bool)tDBReader["Gun24"] == true)
                                TDataInt += 128;
                            TSndStr.Append(TDataInt.ToString("X2"));

                            TDataInt = 0;
                            if ((bool)tDBReader["Gun25"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Gun26"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Gun27"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Gun28"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Gun29"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Gun30"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Gun31"] == true)
                                TDataInt += 64;
                            TSndStr.Append(TDataInt.ToString("X2"));


                            TSndStr.Append("0000");
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");

                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 8 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 9 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 10)
                        {
                            if (IsDate(tDBReader["Baslangic Saati 1"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslangic Saati 1"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Baslangic Saati 2"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslangic Saati 2"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Baslangic Saati 3"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslangic Saati 3"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 11 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 12)
                        {
                            //Six Hour Allow Or Block
                            //Time 1
                            if (IsDate(tDBReader["Baslama Saat 1"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 1"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 1"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 1"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            //Time 2
                            if (IsDate(tDBReader["Baslama Saat 2"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 2"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 2"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 2"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            //Time 3
                            if (IsDate(tDBReader["Baslama Saat 3"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 3"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 3"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 3"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            //Time 4
                            if (IsDate(tDBReader["Baslama Saat 4"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 4"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 4"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 4"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            //Time 5
                            if (IsDate(tDBReader["Baslama Saat 5"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 5"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 5"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 5"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            //Time 6
                            if (IsDate(tDBReader["Baslama Saat 6"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Baslama Saat 6"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            if (IsDate(tDBReader["Bitis Saat 6"].ToString()) == true)
                            {
                                TSndStr.Append(ConvertToTypeTime(tDBReader["Bitis Saat 6"] as DateTime? ?? default(DateTime), "D2"));
                            }
                            else
                            {
                                TSndStr.Append("0000");
                            }
                            TSndStr.Append("000000000000");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 13)
                        {
                            TDataInt = 0;
                            if ((bool)tDBReader["Pazartesi"] == true)
                                TDataInt = 1;
                            if ((bool)tDBReader["Sali"] == true)
                                TDataInt += 2;
                            if ((bool)tDBReader["Carsamba"] == true)
                                TDataInt += 4;
                            if ((bool)tDBReader["Persembe"] == true)
                                TDataInt += 8;
                            if ((bool)tDBReader["Cuma"] == true)
                                TDataInt += 16;
                            if ((bool)tDBReader["Cumartesi"] == true)
                                TDataInt += 32;
                            if ((bool)tDBReader["Pazar"] == true)
                                TDataInt += 64;
                            TSndStr.Append(TDataInt.ToString("X2"));

                            //Pazartesi Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazartesi Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazartesi Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Sali Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Sali Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Sali Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Carsamba Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Carsamba Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Carsamba Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Persembe Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Persembe Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Persembe Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Cuma Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cuma Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cuma Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Cumartesi Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cumartesi Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cumartesi Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Pazar Two Hour Block
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazar Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazar Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            TSndStr.Append("00");
                        }
                        else if ((tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 14 || (tDBReader["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 15)
                        {
                            //Twin Access + Each Day Different
                            TDataInt = 0;
                            if ((tDBReader["Pazartesi"] as bool? ?? default(bool)) == true)
                                TDataInt = 1;
                            if ((tDBReader["Sali"] as bool? ?? default(bool)) == true)
                                TDataInt += 2;
                            if ((tDBReader["Carsamba"] as bool? ?? default(bool)) == true)
                                TDataInt += 4;
                            if ((tDBReader["Persembe"] as bool? ?? default(bool)) == true)
                                TDataInt += 8;
                            if ((tDBReader["Cuma"] as bool? ?? default(bool)) == true)
                                TDataInt += 16;
                            if ((tDBReader["Cumartesi"] as bool? ?? default(bool)) == true)
                                TDataInt += 32;
                            if ((tDBReader["Pazar"] as bool? ?? default(bool)) == true)
                                TDataInt += 64;
                            TSndStr.Append(TDataInt.ToString("X2"));

                            //Pazartesi Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazartesi Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Pazartesi Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazartesi Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Sali Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Sali Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Sali Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Sali Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Carsamba Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Carsamba Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Carsamba Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Carsamba Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Persembe Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Persembe Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Persembe Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Persembe Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Cuma Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cuma Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Cuma Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cuma Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Cumartesi Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cumartesi Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Cumartesi Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Cumartesi Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            //Pazar Start Time 1
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazar Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                            //Pazar Start Time 2
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Pazar Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));

                            TSndStr.Append("00");
                        }
                        else
                        {
                            TSndStr.Append("000000000000");
                            TSndStr.Append("000000000000" + "000000000000" + "000000000000" + "000000000000");
                        }
                        //Multiple Hour Access - Additional Time
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Ek Saat"] as int? ?? default(int), "D1"));
                        TSndStr.Append("**\r");
                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }
                }

            }
            /*5*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_TIMEGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("***\r");
            }
            /*6*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_ACCESSGROUP)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM GroupsMaster WHERE [Grup No]=" + DBIntParam1;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No"] as int? ?? default(int), "D4"));
                        for (int i = 1; i < 9; i++)
                        {
                            tDBSQLStr2 = "SELECT * FROM GroupsDetailNew WHERE [Panel No]=" + mPanelNo + " AND [Grup No]=" + DBIntParam1 + " AND [Kapi No]=" + i + " ORDER BY [Kayit No]";
                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                            tDBReader2 = tDBCmd2.ExecuteReader();
                            if (tDBReader2.Read())
                            {
                                //Time Groups For Each Readers
                                if ((tDBReader2["Kapi Zaman Grup No"] as int? ?? default(int)) != 0)
                                {
                                    TSndStr.Append((tDBReader2["Kapi Zaman Grup No"] as int? ?? default(int)).ToString("D3"));
                                }
                                else
                                {
                                    TSndStr.Append("001");
                                }

                            }
                        }
                        if (tDBReader["Gece Antipassback Sil"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        TSndStr.Append("00000000000000000000000");
                        if ((tDBReader["Grup Gecis Sayisi"] as int? ?? default(int)) <= 255)
                        {
                            TSndStr.Append(ConvertToTypeInt((tDBReader["Grup Gecis Sayisi"] as int? ?? default(int)), "D5"));
                        }
                        else
                        {
                            TSndStr.Append("00000");
                        }
                        if ((tDBReader["Grup Gecis Sayisi Global Bolge No"] as int? ?? default(int)) > 0 && (tDBReader["Grup Gecis Sayisi Global Bolge No"] as int? ?? default(int)) < 1000)
                        {
                            TSndStr.Append(ConvertToTypeInt((tDBReader["Grup Gecis Sayisi Global Bolge No"] as int? ?? default(int)), "D3"));
                        }
                        else
                        {
                            TSndStr.Append("001");
                        }
                        //Access Counter Periode (Daily Or Monthly)
                        TSndStr.Append(ConvertToTypeInt((tDBReader["Gunluk Aylik"] as int? ?? default(int)), "D1"));
                        if ((tDBReader["Grup Icerdeki Kisi Sayisi"] as int? ?? default(int)) <= 10000)
                        {
                            TSndStr.Append(ConvertToTypeInt((tDBReader["Grup Icerdeki Kisi Sayisi"] as int? ?? default(int)), "D5"));
                        }
                        else
                        {
                            TSndStr.Append("00000");
                        }
                        if ((tDBReader["Grup Icerdeki Kisi Sayisi Global Bolge No"] as int? ?? default(int)) > 0 && (tDBReader["Grup Icerdeki Kisi Sayisi Global Bolge No"] as int? ?? default(int)) < 1000)
                        {
                            TSndStr.Append(ConvertToTypeInt((tDBReader["Grup Icerdeki Kisi Sayisi Global Bolge No"] as int? ?? default(int)), "D3"));
                        }
                        else
                        {
                            TSndStr.Append("001");
                        }
                        for (int i = 1; i < 17; i++)
                        {
                            tDBSQLStr2 = "SELECT * FROM GroupsDetailNew WHERE [Panel No]=" + mPanelNo + " AND [Grup No]=" + DBIntParam1 + " AND [Kapi No]=" + i + " ORDER BY [Kayit No]";
                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                            tDBReader2 = tDBCmd2.ExecuteReader();
                            if (tDBReader2.Read())
                            {
                                //Time Groups For Each Readers
                                if (tDBReader2["Kapi Aktif"] as bool? ?? default(bool))
                                {
                                    TSndStr.Append("1");
                                }
                                else
                                {
                                    TSndStr.Append("0");
                                }

                            }
                        }
                        if (tDBReader["Mukerrer Engelleme Gecersiz"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("0");
                        }
                        else
                        {
                            TSndStr.Append("1");
                        }
                        if (tDBReader["Lokal Kapasite Gecersiz"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("0");
                        }
                        else
                        {
                            TSndStr.Append("1");
                        }
                        if (tDBReader["Gece Icerdeki Kisi Sayisini Sil"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if (tDBReader["Antipassback Gecersiz"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("0");
                        }
                        else
                        {
                            TSndStr.Append("1");
                        }
                        for (int i = 1; i < 9; i++)
                        {
                            tDBSQLStr2 = "SELECT * FROM GroupsDetailNew WHERE [Panel No]=" + mPanelNo + " AND [Grup No]=" + DBIntParam1 + " AND [Kapi No]=" + i + " ORDER BY [Kayit No]";
                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                            tDBReader2 = tDBCmd2.ExecuteReader();
                            if (tDBReader2.Read())
                            {
                                //Time Groups For Each Readers
                                if ((tDBReader2["Kapi Asansor Bolge No"] as int? ?? default(int)) > 0 && (tDBReader2["Kapi Asansor Bolge No"] as int? ?? default(int)) < 256)
                                {
                                    TSndStr.Append(ConvertToTypeInt((tDBReader2["Kapi Asansor Bolge No"] as int? ?? default(int)), "D3"));
                                }
                                else
                                {
                                    TSndStr.Append("001");
                                }

                            }
                        }



                        TSndStr.Append("**\r");
                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }
                }
            }
            /*7*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("***\r");
            }
            /*8-9*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_USER || DBTaskType == (ushort)CommandConstants.CMD_SNDALL_USER)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "Select * from Users where ID=" + DBIntParam1;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["ID"] as int? ?? default(int), "D6"));
                        TSndStr.Append(ConvertToTypeInt(Convert.ToInt32(tDBReader["Kart ID"]), "D10"));
                        if (tDBReader["Sifre"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Sifre"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0000");
                        }
                        if (tDBReader["Grup No"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }
                        if ((tDBReader["Grup Takvimi Aktif"] as bool? ?? default(bool)) == true)
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if (tDBReader["Grup Takvimi No"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Grup Takvimi No"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0000");
                        }
                        if (tDBReader["Visitor Grup No"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Visitor Grup No"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }
                        if ((tDBReader["Sureli Kullanici"] as bool? ?? default(bool)) == true)
                        {
                            TSndStr.Append("1");
                            TSndStr.Append(ConvertToTypeDatetime(tDBReader["Bitis Tarihi"] as DateTime? ?? default(DateTime), "D2"));
                        }
                        else
                        {
                            TSndStr.Append("0");
                            TSndStr.Append(ConvertToTypeDatetime(DateTime.Now, "D2"));
                        }
                        if ((tDBReader["3 Grup"] as bool? ?? default(bool)) == true)
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if (tDBReader["Grup No 2"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 2"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }
                        if (tDBReader["Grup No 3"].ToString() != null)
                        {
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 3"] as int? ?? default(int), "D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }
                        TSndStr.Append("00");
                        TSndStr.Append("**\r");

                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }
                }
            }
            /*10-11*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_USER)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
            }
            /*12*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GROUPCALENDAR || DBTaskType == (ushort)CommandConstants.CMD_SNDALL_GROUPCALENDAR)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM TimeZoneCalendar WHERE [Grup Takvimi No] =" + DBIntParam1;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Grup Takvimi No"] as int? ?? default(int), "D4"));
                        if (tDBReader["Grup Takvimi Tipi"].ToString() == "0")
                        {
                            TSndStr.Append("0");
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Saat 1"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(tDBReader["Saat 2"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeDatetime(DateTime.Now, "D2"));
                            TSndStr.Append(ConvertToTypeDatetime(DateTime.Now, "D2"));
                        }
                        else
                        {
                            TSndStr.Append("1");
                            TSndStr.Append(ConvertToTypeDatetime(tDBReader["Tarih 1"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeDatetime(tDBReader["Tarih 2"] as DateTime? ?? default(DateTime), "D2"));
                            TSndStr.Append(ConvertToTypeTime(DateTime.Now, "D2"));
                            TSndStr.Append(ConvertToTypeTime(DateTime.Now, "D2"));
                        }
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 1"] as int? ?? default(int), "D4"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 2"] as int? ?? default(int), "D4"));
                        TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 3"] as int? ?? default(int), "D4"));
                        TSndStr.Append("**\r");
                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }

                }
            }
            /*13*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_USER)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
            }
            /*14*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_USER)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("***\r");
            }
            /*15*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOGCOUNT)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*17*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_LOGCOUNT)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*18*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_MAXINCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");
            }
            /*19*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ACCESSCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
            }
            /*20*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_ACCESSCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*21*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_ACCESSCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
            }
            /*22*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*23*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");
            }
            /*24*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_APBCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*25*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_APBCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");

            }
            /*26*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_RTC)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(tDate.Day.ToString("D2"));
                TSndStr.Append(tDate.Month.ToString("D2"));
                TSndStr.Append(tDate.Year.ToString("D2").Substring(2, 2));
                TSndStr.Append(tDate.Hour.ToString("D2"));
                TSndStr.Append(tDate.Minute.ToString("D2"));
                TSndStr.Append(tDate.Second.ToString("D2"));
                TSndStr.Append(((int)tDate.DayOfWeek).ToString("D2"));
                TSndStr.Append("**\r");
            }
            /*27*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_RTC)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*31*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_RELAYPROGRAM)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM ProgRelay2 " +
                        "WHERE [Panel No] = " + DBIntParam1.ToString() +
                        " AND [Haftanin Gunu] = " + DBIntParam2.ToString() +
                        " AND [Zaman Dilimi] = " + DBIntParam3.ToString();
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        TSndStr.Append(ConvertToTypeInt((tDBReader["Haftanin Gunu"] as int? ?? default(int)), "D2"));
                        TSndStr.Append(ConvertToTypeInt((tDBReader["Zaman Dilimi"] as int? ?? default(int)), "D2"));
                        if (tDBReader["Aktif"] as bool? ?? default(bool))
                        {
                            TSndStr.Append("0");
                        }
                        else
                        {
                            TSndStr.Append("1");
                        }
                        TSndStr.Append(ConvertToTypeTime(tDBReader["Saat 1"] as DateTime? ?? default(DateTime), "D2"));
                        TSndStr.Append(ConvertToTypeTime(tDBReader["Saat 2"] as DateTime? ?? default(DateTime), "D2"));
                        for (int i = 1; i < 17; i++)
                        {
                            var durum = "Durum " + i;
                            var role = "Role " + i;
                            if (tDBReader[durum] as bool? ?? default(bool))
                            {
                                if (tDBReader[role] as bool? ?? default(bool))
                                {
                                    TSndStr.Append("1");
                                }
                                else
                                {
                                    TSndStr.Append("2");
                                }
                            }
                            else
                            {
                                TSndStr.Append("0");
                            }
                        }



                        TSndStr.Append("**\r");

                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }
                }
            }
            /*32*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_RELAYPROGRAM)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D2"));
                TSndStr.Append(DBIntParam2.ToString("D2"));
                TSndStr.Append("**\r");
            }
            /*35*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D2"));
                TSndStr.Append("**\r");
            }
            /*36*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LOCALCAPACITYCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D2"));
                TSndStr.Append(DBIntParam2.ToString("D5"));
                TSndStr.Append("**\r");
            }
            /*37*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_MAXUSERID)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
            }
            /*38*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LOGSETTINGS)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM PanelSettings " +
                        " WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                        " AND [Panel ID] = " + mPanelNo.ToString();

                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        if ((tDBReader["Offline Antipassback"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((bool)tDBReader["Offline Blocked Request"])
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Offline Undefined Transition"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Offline Manuel Operations"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Offline Button Triggering"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Offline Scheduled Transactions"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        TSndStr.Append("**\r");

                    }
                    else
                    {
                        TSndStr.Remove(1, TSndStr.Length);
                        TSndStr.Append("ERR");
                    }

                }

            }
            /*39*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOGSETTINGS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*40*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LIFTGROUP)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM LiftGroups WHERE [Asansor Grup No] = " + DBIntParam1;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        if ((tDBReader["Asansor Grup No"] as int? ?? default(int)) > 1 && (tDBReader["Asansor Grup No"] as int? ?? default(int)) <= 255)
                        {
                            TSndStr.Append(((tDBReader["Asansor Grup No"] as int? ?? default(int))).ToString("D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }

                        for (int i = 1; i < 65; i++)
                        {
                            string column = "Kat " + i;
                            if ((tDBReader[column] as bool? ?? default(bool)))
                            {
                                TSndStr.Append("1");
                            }
                            else
                            {
                                TSndStr.Append("0");
                            }
                        }
                        TSndStr.Append("0000000000000000");
                        TSndStr.Append("**\r");
                    }
                }
            }
            /*41*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LIFTGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");
            }
            /*42*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_LIFTGROUP || DBTaskType == (ushort)CommandConstants.CMD_ERSALL_LIFTGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*43*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_USERALARM)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM Alarmlar WHERE [Alarm No] = " + DBIntParam1;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        if ((tDBReader["Alarm No"] as int? ?? default(int)) > 0 && (tDBReader["Alarm No"] as int? ?? default(int)) <= 2000)
                        {
                            TSndStr.Append(((tDBReader["Alarm No"] as int? ?? default(int))).ToString("D4"));
                        }
                        else
                        {
                            TSndStr.Append("0001");
                        }
                        if ((tDBReader["Alarm Tipi"] as int? ?? default(int)) > 0 && (tDBReader["Alarm Tipi"] as int? ?? default(int)) <= 2)
                        {
                            TSndStr.Append(((tDBReader["Alarm Tipi"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("01");
                        }
                        if ((tDBReader["Panel No"] as int? ?? default(int)) >= 0 && (tDBReader["Panel No"] as int? ?? default(int)) <= 255)
                        {
                            TSndStr.Append(((tDBReader["Panel No"] as int? ?? default(int))).ToString("D3"));
                        }
                        else
                        {
                            TSndStr.Append("000");
                        }
                        if ((tDBReader["Kapi No"] as int? ?? default(int)) >= 0 && (tDBReader["Kapi No"] as int? ?? default(int)) <= 16)
                        {
                            TSndStr.Append(((tDBReader["Kapi No"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        TSndStr.Append("0000000");
                        if ((tDBReader["Alarm Rolesi"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Kapi Rolesi"] as bool? ?? default(bool)))
                        {
                            TSndStr.Append("1");
                        }
                        else
                        {
                            TSndStr.Append("0");
                        }
                        if ((tDBReader["Kapi Role No"] as int? ?? default(int)) >= 0 && (tDBReader["Kapi Role No"] as int? ?? default(int)) <= 16)
                        {
                            TSndStr.Append(((tDBReader["Kapi Role No"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["User ID"] as int? ?? default(int)) >= 1 && (tDBReader["User ID"] as int? ?? default(int)) <= 100000)
                        {
                            TSndStr.Append(((tDBReader["User ID"] as int? ?? default(int))).ToString("D6"));
                        }
                        else
                        {
                            TSndStr.Append("000001");
                        }
                        TSndStr.Append("**\r");
                    }
                }

            }
            /*44*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_USERALARM)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");

            }
            /*45*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_USERALARM || DBTaskType == (ushort)CommandConstants.CMD_ERSALL_USERALARM)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*46*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ALARMFIRE_STATUS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*47*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_DOORALARM_STATUS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("****\r");
            }
            /*49*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LOCALINTERLOCK)
            {
                lock (TLockObj)
                {
                    tDBSQLStr = "SELECT * FROM PanelSettings " +
                        " WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                        " AND [Panel ID] = " + mPanelNo.ToString();
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                        TSndStr.Append(mPanelSerialNo.ToString("X4"));
                        TSndStr.Append(mPanelNo.ToString("D3"));
                        if ((tDBReader["LocalInterlock G1-1"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((int)tDBReader["LocalInterlock G1-1"]).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G1-2"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((int)tDBReader["LocalInterlock G1-2"]).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G2-1"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G2-1"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G2-2"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G2-2"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G3-1"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G3-1"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G3-2"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G3-2"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G4-1"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G4-1"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        if ((tDBReader["LocalInterlock G4-2"] as int? ?? default(int)) >= 0)
                        {
                            TSndStr.Append(((tDBReader["LocalInterlock G4-2"] as int? ?? default(int))).ToString("D2"));
                        }
                        else
                        {
                            TSndStr.Append("00");
                        }
                        TSndStr.Append("**\r");
                    }
                }
            }
            /*50*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOCALINTERLOCK)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
            }
            /*DOOR TRIGGER*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORTRIGGER)
            {
                if (DBIntParam1 == 1)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("10000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 2)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("01000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 3)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00100000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 4)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00010000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 5)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00001000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 6)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000100000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 7)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000010000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 8)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000001000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 9)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000100000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 10)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000010000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 11)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 12)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 13)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000010000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 14)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000001000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 15)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000100");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 16)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("0000000000000010");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 17)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000001");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }

            }
            /*DOOR OPEN*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFORCEOPEN)
            {
                if (DBIntParam1 == 1)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("10000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 2)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("01000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 3)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00100000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 4)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00010000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 5)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00001000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 6)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000100000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 7)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000010000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 8)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000001000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 9)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000100000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 10)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000010000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 11)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 12)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 13)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000010000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 14)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000001000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 15)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000100");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 16)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("0000000000000010");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 17)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000001");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
            }
            /*DOOR CLOSE*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFORCECLOSE)
            {
                if (DBIntParam1 == 1)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("10000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 2)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("01000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 3)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00100000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 4)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00010000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 5)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00001000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 6)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000100000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 7)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000010000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 8)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000001000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 9)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000100000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 10)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000010000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 11)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 12)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 13)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000010000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 14)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000001000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 15)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000100");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 16)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("0000000000000010");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 17)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000001");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
            }
            /*DOOR FREE*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFREE)
            {
                if (DBIntParam1 == 1)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("10000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 2)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("01000000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 3)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00100000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 4)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00010000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 5)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00001000000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 6)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000100000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 7)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000010000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 8)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000001000000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 9)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000100000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 10)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000010000000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 11)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 12)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000100000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 13)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000010000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 14)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000001000");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 15)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000100");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 16)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("0000000000000010");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
                if (DBIntParam1 == 17)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append("00000000000000001");
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append("**\r");
                }
            }

            return TSndStr;
        }

        //TODO:Panele Gönderilen Görevlerden Dönen Sonuçları Alma
        public bool ReceiveGenericAnswerData(TcpClient TClient, CommandConstants TmpTaskType)
        {
            int TSize = (int)GetAnswerSize(TmpTaskType);
            byte[] RcvBuffer = new byte[TSize];
            string TRcvData = null;
            int TPos;
            try
            {
                if (TClient.Available > 0)
                {
                    TClient.GetStream().Read(RcvBuffer, 0, TSize);
                    TRcvData = Encoding.UTF8.GetString(RcvBuffer, 0, TSize);
                }
                else
                {
                    return false;
                }

                TPos = TRcvData.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));
                if (TPos > -1)
                {
                    if (TRcvData.Substring(TPos + 10, 1) == "O")
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
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

        //TODO:Veritabanından Gelen Görevin Durumunu Güncelleme
        public bool SyncUpdateTaskStatus(int tTaskNo, ushort tTaskStatus)
        {
            object TLockObj = new object();
            ushort TTaskOk = 0;
            int TRetInt;
            if (tTaskNo <= 0)
            {
                return false;
            }

            lock (TLockObj)
            {
                //mDBConn = new SqlConnection();
                //mDBConn.ConnectionString = @"data source = ARGE-2\SQLEXPRESS; initial catalog = MW301_DB25; integrated security = True; MultipleActiveResultSets = True;";
                //mDBConn.Open();
                mDBSQLStr = "UPDATE TaskList SET [Durum Kodu]=" + tTaskStatus + " WHERE [Kayit No]=" + tTaskNo;
                mDBCmd = new SqlCommand(mDBSQLStr, mDBConn);
                TRetInt = mDBCmd.ExecuteNonQuery();
                var status = mDBConn.State;

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

        //TODO:Dönen verinin parçalanması için değişkene atıyor
        public bool GAReciveGenericDBData(TcpClient TClient, ref string TReturnStr, CommandConstants TmpTaskType)
        {
            int TSize = GetAnswerSize(TmpTaskType);
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
                    return false;
                }
                TPos = TRcvData.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));
                if (TPos > -1)
                {
                    TReturnStr = TRcvData;
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
        public bool ProcessReceivedData(int DBIntParam1, int DBIntParam2, int DBIntParam3, CommandConstants TmpTaskType, ushort TmpTaskSoruce, bool TmpTaskUpdateTable, string TmpReturnStr)
        {
            StringBuilder TSndStr = new StringBuilder();
            ushort TDataInt;
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            string tDBSQLStr2;
            SqlCommand tDBCmd2;
            SqlDataReader tDBReader;
            int TRetInt;
            int TPos;
            byte TByte1;
            byte TByte2;
            int TLong;
            int SI;
            int TInt;

            TPos = TmpReturnStr.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));
            if (TPos < 0)
            {
                if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16) != mPanelSerialNo || Convert.ToInt32(TmpReturnStr.Substring(TPos + 7, 3)) != mPanelNo)
                {
                    return false;
                }
            }

            switch (TmpTaskType)
            {

                case CommandConstants.CMD_RCV_USER:
                case CommandConstants.CMD_RCVALL_USER:
                    {
                        if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6)) == DBIntParam1)
                        {
                            if (TmpTaskUpdateTable)
                            {
                                lock (TLockObj)
                                {
                                    tDBSQLStr = "SELECT * FROM Users WHERE [ID]=" + DBIntParam1;
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    DateTime tDateTime;
                                    if (TmpReturnStr.Substring(TPos + 44, 2) == "00" || TmpReturnStr.Substring(TPos + 46, 2) == "00")
                                    {
                                        tDateTime = Convert.ToDateTime("01/01/" + TmpReturnStr.Substring(TPos + 48, 2));
                                    }
                                    else
                                    {
                                        tDateTime = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 44, 2) + "/" + TmpReturnStr.Substring(TPos + 46, 2) + "/20" + TmpReturnStr.Substring(TPos + 48, 2));
                                    }
                                    if (!tDBReader.Read())
                                    {


                                        tDBSQLStr2 = "INSERT INTO Users (ID,[Kart ID],Sifre,[Grup No],[Grup Takvimi Aktif],[Grup Takvimi No],[Visitor Grup No],[Sureli Kullanici],[Bitis Tarihi],[3 Grup],[Grup No 2],[Grup No 3])" +
                                            "VALUES (" +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 16, 10)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 26, 4)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 30, 4)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 34, 1)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 35, 4)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 39, 4)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 43, 1)) + "," +
                                            "'" + tDateTime + "'," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 50, 1)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 51, 4)) + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 55, 4)) + ")";
                                    }
                                    else
                                    {
                                        tDBSQLStr2 = "UPDATE Users" +
                                            " SET " +
                                            " [Kart ID] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 16, 10)) + "," +
                                            " [Sifre] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 26, 4)) + "," +
                                            " [Grup No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 30, 4)) + "," +
                                            " [Grup Takvimi Aktif] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 34, 1)) + "," +
                                            " [Grup Takvimi No] = " + Convert.ToInt16(TmpReturnStr.Substring(TPos + 35, 4)) + "," +
                                            " [Visitor Grup No] = " + Convert.ToInt16(TmpReturnStr.Substring(TPos + 39, 4)) + "," +
                                            " [Sureli Kullanici] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 43, 1)) + "," +
                                            " [Bitis Tarihi] = " + "'" + tDateTime + "'," +
                                            " [3 Grup] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 50, 1)) + "," +
                                            " [Grup No 2] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 51, 4)) + "," +
                                            " [Grup No 3] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 55, 4)) +
                                            " WHERE [ID] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6));
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
                    }
                    break;
                case CommandConstants.CMD_RCV_ACCESSGROUP:
                case CommandConstants.CMD_RCVALL_ACCESSGROUP:
                    {
                        byte[] DoorPermission = new byte[/*(int)TCONST.MAX_READER + 1*/ 8];
                        ushort[] DoorTimeGroup = new ushort[/*(int)TCONST.MAX_READER + 1*/ 8];
                        ushort[] DoorLiftGroups = new ushort[/*(int)TCONST.MAX_READER + 1*/ 8];

                        if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 4)) == DBIntParam1)
                        {
                            for (int i = 0; i <= /*(int)TCONST.MAX_READER*/ 7; i++)
                            {
                                DoorPermission[i] = Convert.ToByte(TmpReturnStr.Substring(TPos + 79 + i, 1));
                                if (DoorPermission[i] > 1)
                                {
                                    DoorPermission[i] = 0;
                                }
                                DoorTimeGroup[i] = Convert.ToUInt16(TmpReturnStr.Substring(TPos + 14 + (i * 3), 3));
                                if (DoorTimeGroup[i] > 255)
                                {
                                    DoorTimeGroup[i] = 1;
                                }
                                DoorLiftGroups[i] = Convert.ToUInt16(TmpReturnStr.Substring(TPos + 99 + (i * 3), 3));
                                if (DoorLiftGroups[i] > 255)
                                {
                                    DoorLiftGroups[i] = 1;
                                }
                            }
                            if (TmpTaskUpdateTable)
                            {
                                //lock (TLockObj)
                                //{
                                //    tDBSQLStr = "SELECT * FROM GroupsMaster WHERE [Grup No]=" + DBIntParam1;
                                //    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                //    tDBReader = tDBCmd.ExecuteReader();
                                //    if (!tDBReader.Read())
                                //    {
                                //        tDBSQLStr2 = "INSERT INTO GroupsMaster ([Grup No],[Grup Adi],[Grup Gecis Sayisi],[Grup Gecis Sayisi Global Bolge No]," +
                                //            "[Grup Icerdeki Kisi Sayisi],[Grup Icerdeki Kisi Sayisi Global Bolge No]," +
                                //            "[Mukerrer Engelleme Gecersiz],[Lokal Kapasite Gecersiz],[Antipassback Gecersiz]," +
                                //            "[Gece Icerdeki Kisi Sayisini Sil],[Gunluk Aylik])" +
                                //            "VALUES " +
                                //            "(" +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 4)) + "," +
                                //            "'Grup " + DBIntParam1.ToString().Trim() + "'," +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 62, 5)) + "," +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 67, 3)) + "," +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 71, 5)) + "," +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 76, 3)) + "," +
                                //            Convert.ToByte(TmpReturnStr.Substring(TPos + 96, 1)) + "," +
                                //            Convert.ToByte(TmpReturnStr.Substring(TPos + 97, 1)) + "," +
                                //            Convert.ToByte(TmpReturnStr.Substring(TPos + 98, 1)) + "," +
                                //            Convert.ToInt32(TmpReturnStr.Substring(TPos + 97, 1)) + "," +
                                //            Convert.ToByte(TmpReturnStr.Substring(TPos + 70, 1)) + ")";
                                //    }
                                //    else
                                //    {
                                //        tDBSQLStr2 = "UPDATE GroupsMaster " +
                                //            "SET " +
                                //            "[Grup Adi] = 'Grup " + DBIntParam1.ToString().Trim() + "'," +
                                //            "[Grup Gecis Sayisi] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 62, 5)) + "," +
                                //            "[Grup Gecis Sayisi Global Bolge No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 67, 3)) + "," +
                                //            "[Grup Icerdeki Kisi Sayisi] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 71, 5)) + "," +
                                //            "[Grup Icerdeki Kisi Sayisi Global Bolge No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 76, 3)) + "," +
                                //            "[Mukerrer Engelleme Gecersiz] = " + Convert.ToByte(TmpReturnStr.Substring(TPos + 96, 1)) + "," +
                                //            "[Lokal Kapasite Gecersiz] = " + Convert.ToByte(TmpReturnStr.Substring(TPos + 97, 1)) + "," +
                                //            "[Antipassback Gecersiz] = " + Convert.ToByte(TmpReturnStr.Substring(TPos + 98, 1)) + "," +
                                //            "[Gece Icerdeki Kisi Sayisini Sil] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 97, 1)) + "," +
                                //            "[Gunluk Aylik] = " + Convert.ToByte(TmpReturnStr.Substring(TPos + 70, 1)) + " " +
                                //            "WHERE [Grup No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 4));
                                //    }

                                //    tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                //    TRetInt = tDBCmd2.ExecuteNonQuery();
                                //    if (TRetInt <= 0)
                                //    {
                                //        return false;
                                //    }
                                //}

                                lock (TLockObj)
                                {

                                    for (int i = 0; i <= /*(int)TCONST.MAX_READER*/ 7; i++)
                                    {

                                        tDBSQLStr = "SELECT * FROM GroupsDetail " +
                                            "WHERE [Seri No] = " + mPanelSerialNo.ToString() + " " +
                                            "AND [Panel No] = " + mPanelNo.ToString().Trim() + " " +
                                            "AND [Grup No] = " + (i + 1).ToString().Trim();
                                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                        tDBReader = tDBCmd.ExecuteReader();
                                        if (!tDBReader.Read())
                                        {
                                            tDBSQLStr2 = "INSERT INTO GroupsDetail " +
                                                "([Grup No],[Panel No],[Kapi No]," +
                                                "[Kapi Gecis],[Kapi Zaman Grup No],[Kapi Asansor Grup No]) " +
                                                "VALUES " +
                                                "(" +
                                                DBIntParam1.ToString().Trim() + "," +
                                                mPanelNo.ToString().Trim() + "," +
                                                (i + 1).ToString().Trim() + "," +
                                                DoorPermission[i].ToString() + "," +
                                                DoorTimeGroup[i].ToString() + "," +
                                                DoorLiftGroups[i].ToString() + "," +
                                                DoorLiftGroups[i].ToString() + ")";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 = "UPTADE GroupsDetail " +
                                                "SET " +
                                                "[Kapi Gecis] = " + DoorPermission[i].ToString() + "," +
                                                "[Kapi Zaman Grup No] = " + DoorTimeGroup[i].ToString() + "," +
                                                "[Kapi Asansor Grup No] = " + DoorLiftGroups[i].ToString() + "," +
                                                "WHERE [Grup No] = " + DBIntParam1.ToString().Trim() + " " +
                                                "AND [Panel No] = " + mPanelNo.ToString().Trim() + " " +
                                                "AND [Kapi No] = " + (i + 1).ToString().Trim();
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
                        }
                    }
                    break;
                case CommandConstants.CMD_RCV_TIMEGROUP:
                case CommandConstants.CMD_RCVALL_TIMEGROUP:
                    {
                        byte[] MonthlyProgram = new byte[31];
                        byte[] WeeklyProgram = new byte[7];
                        byte WeeklyProgramPlusTimeCheck = 0;
                        byte HourAddition;
                        DateTime StartDate = new DateTime();
                        StartDate = DateTime.Now;
                        DateTime StartHour = new DateTime();
                        DateTime WeeklyProgramPlusStartHour = new DateTime();
                        DateTime EndDate = new DateTime();
                        EndDate = DateTime.Now;
                        DateTime EndHour = new DateTime();
                        DateTime WeeklyProgramPlusEndHour = new DateTime();
                        DateTime[] WeeklyStartHour = new DateTime[7];
                        DateTime[] WeeklyEndHour = new DateTime[7];
                        DateTime[] DailyStartHour3Slot = new DateTime[3];
                        DateTime[] DailyStartHour6Slot = new DateTime[6];
                        DateTime[] DailyEndHour6Slot = new DateTime[6];
                        if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 4)) == DBIntParam1)
                        {
                            if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 0)//TimeGroupType
                            {
                                for (int i = 0; i < MonthlyProgram.Length; i++)
                                {
                                    MonthlyProgram[i] = 0;
                                }
                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyProgram[i] = 0;
                                }
                                WeeklyProgramPlusTimeCheck = 0;
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 1 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 2)//TimeGroupType
                            {
                                if (TmpReturnStr.Substring(TPos + 16, 2) == "00" || TmpReturnStr.Substring(TPos + 18, 2) == "00")
                                    StartDate = Convert.ToDateTime("01/01/" + TmpReturnStr.Substring(TPos + 20, 2));
                                else
                                    StartDate = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 16, 2) + "/" + TmpReturnStr.Substring(TPos + 18, 2) + "/20" + TmpReturnStr.Substring(TPos + 20, 2));
                                if (TmpReturnStr.Substring(TPos + 22, 2) == "00" || TmpReturnStr.Substring(TPos + 24, 2) == "00")
                                    EndDate = Convert.ToDateTime("01/01/" + TmpReturnStr.Substring(TPos + 26, 2));
                                else
                                    EndDate = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 22, 2) + "/" + TmpReturnStr.Substring(TPos + 24, 2) + "/20" + TmpReturnStr.Substring(TPos + 26, 2));
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 3 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 4)//TimeGroupType
                            {
                                StartHour = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 16, 2) + ":" + TmpReturnStr.Substring(TPos + 18, 2) + ":" + TmpReturnStr.Substring(TPos + 20, 2));
                                EndHour = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 22, 2) + ":" + TmpReturnStr.Substring(TPos + 24, 2) + ":" + TmpReturnStr.Substring(TPos + 26, 2));
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 5)//TimeGroupType
                            {
                                TByte1 = Convert.ToByte(TmpReturnStr.Substring(TPos + 16, 2), 16);
                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyProgram[i] = 0;
                                    if (((2 ^ i) & TByte1) != 0)
                                    {
                                        WeeklyProgram[i] = 1;
                                    }
                                    //Additional Two Hour Block
                                    WeeklyProgramPlusTimeCheck = Convert.ToByte(TmpReturnStr.Substring(TPos + 18, 2));
                                    if (WeeklyProgramPlusTimeCheck != 0)
                                    {
                                        WeeklyProgramPlusStartHour = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 20, 2) + ":" + TmpReturnStr.Substring(TPos + 22, 2) + ":00");
                                        WeeklyProgramPlusEndHour = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 24, 2) + ":" + TmpReturnStr.Substring(TPos + 26, 2) + ":00");
                                    }
                                }
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 13)//TimeGroupType
                            {
                                //Weekly And Each Day Time Limit
                                TByte1 = Convert.ToByte(TmpReturnStr.Substring(TPos + 16, 2), 16);
                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyProgram[i] = 0;
                                    if (((2 ^ i) & TByte1) != 0)
                                    {
                                        WeeklyProgram[i] = 1;
                                    }
                                }
                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyStartHour[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 18 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 20 + (i * 8), 2) + ":00");
                                    WeeklyEndHour[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 22 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 24 + (i * 8), 2) + ":00");
                                }
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 6)//TimeGroupType
                            {
                                //Monthly
                                TLong = Convert.ToByte(TmpReturnStr.Substring(TPos + 16, 8), 16);
                                for (int i = MonthlyProgram.Length - 1; i >= 0; i++)
                                {
                                    MonthlyProgram[30 - i] = 0;
                                    if (((2 ^ i) & TLong) != 0)
                                    {
                                        MonthlyProgram[30 - i] = 1;
                                    }
                                }
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 8 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 9 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 10)//TimeGroupType
                            {
                                //Multi Hour Allow Or Block
                                DailyStartHour3Slot[0] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 16, 2) + ":" + TmpReturnStr.Substring(TPos + 18, 2) + ":00");
                                DailyStartHour3Slot[1] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 16, 2) + ":" + TmpReturnStr.Substring(TPos + 18, 2) + ":00");
                                DailyStartHour3Slot[2] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 16, 2) + ":" + TmpReturnStr.Substring(TPos + 18, 2) + ":00");
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 14 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 15)//TimeGroupType
                            {
                                //Twin Access + Each Day Different
                                TByte1 = Convert.ToByte(TmpReturnStr.Substring(TPos + 16, 2), 16);
                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyProgram[i] = 0;
                                    if (((2 ^ i) & TByte1) != 0)
                                    {
                                        WeeklyProgram[i] = 1;
                                    }
                                }

                                for (int i = 0; i < WeeklyProgram.Length; i++)
                                {
                                    WeeklyStartHour[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 18 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 20 + (i * 8), 2) + ":00");
                                    WeeklyEndHour[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 22 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 24 + (i * 8), 2) + ":00");
                                }
                            }
                        }
                        else if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 11 || Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) == 12)//TimeGroupType
                        {
                            for (int i = 0; i < 6; i++)
                            {
                                DailyStartHour6Slot[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 18 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 20 + (i * 8), 2) + ":00");
                                DailyEndHour6Slot[i] = Convert.ToDateTime(TmpReturnStr.Substring(TPos + 22 + (i * 8), 2) + ":" + TmpReturnStr.Substring(TPos + 24 + (i * 8), 2) + ":00");
                            }
                        }
                        else
                        {
                            for (int i = 0; i < MonthlyProgram.Length; i++)
                            {
                                MonthlyProgram[i] = 0;
                            }
                            for (int i = 0; i < WeeklyProgram.Length; i++)
                            {
                                WeeklyProgram[i] = 0;
                            }
                            WeeklyProgramPlusTimeCheck = 0;
                        }
                        HourAddition = Convert.ToByte(TmpReturnStr.Substring(TPos + 74, 1));

                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                tDBSQLStr = "SELECT * FROM TimeGroups " +
                                    "WHERE [Zaman Grup No] = " + DBIntParam1.ToString();
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                tDBReader = tDBCmd.ExecuteReader();
                                if (!tDBReader.Read())
                                {
                                    tDBSQLStr2 = "INSERT INTO TimeGroups " +
                                        "(" +
                                        "[Zaman Grup No],[Zaman Grup Adi],[Gecis Sinirlama Tipi], " +
                                        "[Baslangic Tarihi],[Bitis Tarihi],[Baslangic Saati],[Bitis Saati]," +
                                        "Pazartesi,Sali,Carsamba,Persembe,Cuma,Cumartesi,Pazar," +
                                        "Gun1,Gun2,Gun3,Gun4,Gun5,Gun6,Gun7,Gun8,Gun9,Gun10,Gun11,Gun12,Gun13,Gun14,Gun15,Gun16," +
                                        "Gun17,Gun18,Gun19,Gun20,Gun21,Gun22,Gun23,Gun24,Gun25,Gun26,Gun27,Gun28,Gun29,Gun30,Gun31," +
                                        "[Baslangic Saati 1],[Baslangic Saati 2],[Baslangic Saati 3],[Ek Saat]," +
                                        "[Ilave Saat Kontrolu],[Ilave Baslangic Saati],[Ilave Bitis Saati]," +
                                        "[Baslama Saat 1],[Baslama Saat 2],[Baslama Saat 3],[Baslama Saat 4],[Baslama Saat 5],[Baslama Saat 6]," +
                                        "[Bitis Saat 1],[Bitis Saat 2],[Bitis Saat 3],[Bitis Saat 4],[Bitis Saat 5],[Bitis Saat 6]," +
                                        "[Pazartesi Baslangic Saati],[Pazartesi Bitis Saati],[Sali Baslangic Saati],[Sali Bitis Saati]," +
                                        "[Carsamba Baslangic Saati],[Carsamba Bitis Saati],[Persembe Baslangic Saati],[Persembe Bitis Saati]," +
                                        "[Cuma Baslangic Saati],[Cuma Bitis Saati],[Cumartesi Baslangic Saati],[Cumartesi Bitis Saati]," +
                                        "[Pazar Baslangic Saati],[Pazar Bitis Saati]" +
                                        ")" +
                                        "VALUES " +
                                        "(" +
                                        DBIntParam1.ToString() + "," +
                                        "'Zaman Grup " + DBIntParam1.ToString() + "'," +
                                        Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) + "," +
                                        "'" + StartDate.ToString("yyyy-MM-dd") + "'," +
                                        "'" + EndDate.ToString("yyyy-MM-dd") + "'," +
                                        "'" + StartHour.ToString("hh:mm:ss") + "'," +
                                        "'" + EndHour.ToString("hh:mm:ss") + "'," +
                                        WeeklyProgram[0].ToString() + "," + WeeklyProgram[1].ToString() + "," +
                                        WeeklyProgram[2].ToString() + "," + WeeklyProgram[3].ToString() + "," +
                                        WeeklyProgram[4].ToString() + "," + WeeklyProgram[5].ToString() + "," +
                                        WeeklyProgram[6].ToString() + "," +
                                        MonthlyProgram[0].ToString() + "," + MonthlyProgram[1].ToString() + "," +
                                        MonthlyProgram[2].ToString() + "," + MonthlyProgram[3].ToString() + "," +
                                        MonthlyProgram[4].ToString() + "," + MonthlyProgram[5].ToString() + "," +
                                        MonthlyProgram[6].ToString() + "," + MonthlyProgram[7].ToString() + "," +
                                        MonthlyProgram[8].ToString() + "," + MonthlyProgram[9].ToString() + "," +
                                        MonthlyProgram[10].ToString() + "," + MonthlyProgram[11].ToString() + "," +
                                        MonthlyProgram[12].ToString() + "," + MonthlyProgram[13].ToString() + "," +
                                        MonthlyProgram[14].ToString() + "," + MonthlyProgram[15].ToString() + "," +
                                        MonthlyProgram[16].ToString() + "," + MonthlyProgram[17].ToString() + "," +
                                        MonthlyProgram[18].ToString() + "," + MonthlyProgram[19].ToString() + "," +
                                        MonthlyProgram[20].ToString() + "," + MonthlyProgram[21].ToString() + "," +
                                        MonthlyProgram[22].ToString() + "," + MonthlyProgram[23].ToString() + "," +
                                        MonthlyProgram[24].ToString() + "," + MonthlyProgram[25].ToString() + "," +
                                        MonthlyProgram[26].ToString() + "," + MonthlyProgram[27].ToString() + "," +
                                        MonthlyProgram[28].ToString() + "," + MonthlyProgram[29].ToString() + "," +
                                        MonthlyProgram[30].ToString() + "," +
                                        "'" + DailyStartHour3Slot[0].ToString("hh:mm:ss") + "'," +
                                        "'" + DailyStartHour3Slot[1].ToString("hh:mm:ss") + "'," +
                                        "'" + DailyStartHour3Slot[2].ToString("hh:mm:ss") + "'," +
                                        +HourAddition + "," +
                                        +WeeklyProgramPlusTimeCheck + "," +
                                        "'" + WeeklyProgramPlusStartHour.ToString("hh:mm:ss") + "'," +
                                        "'" + WeeklyProgramPlusEndHour.ToString("hh:mm:ss") + "'," +
                                        "'" + DailyStartHour6Slot[0].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyStartHour6Slot[1].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyStartHour6Slot[2].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyStartHour6Slot[3].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyStartHour6Slot[4].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyStartHour6Slot[5].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[0].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[1].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[2].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[3].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[4].ToString("hh:mm:ss") + "'" +
                                        "'" + DailyEndHour6Slot[5].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[0].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[0].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[1].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[1].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[2].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[2].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[3].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[3].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[4].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[4].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[5].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[5].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyStartHour[6].ToString("hh:mm:ss") + "'" +
                                        "'" + WeeklyEndHour[6].ToString("hh:mm:ss") + "'" +
                                        ")";
                                }
                                else
                                {
                                    tDBSQLStr2 = "UPDATE TimeGroups " +
                                        "SET " +
                                         "[Zaman Grup Adi] = 'Zaman Grup " + DBIntParam1.ToString() + "'," +
                                             "[Gecis Sinirlama Tipi] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 2)) + "," +
                                             "[Baslangic Tarihi] = '" + StartDate.ToString("yyyy-MM-dd") + "'," +
                                             "[Bitis Tarihi] = '" + EndDate.ToString("yyyy-MM-dd") + "'," +
                                             "[Baslangic Saati] = '" + StartHour.ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saati] = '" + EndHour.ToString("hh:mm:ss") + "'," +
                                             "[Pazartesi] = " + WeeklyProgram[0].ToString() + "," +
                                             "[Sali] = " + WeeklyProgram[1].ToString() + "," +
                                             "[Carsamba] = " + WeeklyProgram[2].ToString() + "," +
                                             "[Persembe] = " + WeeklyProgram[3].ToString() + "," +
                                             "[Cuma] = " + WeeklyProgram[4].ToString() + "," +
                                             "[Cumartesi] = " + WeeklyProgram[5].ToString() + "," +
                                             "[Pazar] = " + WeeklyProgram[6].ToString() + "," +
                                             "[Gun1] = " + MonthlyProgram[0].ToString() + "," +
                                             "[Gun2] = " + MonthlyProgram[1].ToString() + "," +
                                             "[Gun3] = " + MonthlyProgram[2].ToString() + "," +
                                             "[Gun4] = " + MonthlyProgram[3].ToString() + "," +
                                             "[Gun5] = " + MonthlyProgram[4].ToString() + "," +
                                             "[Gun6] = " + MonthlyProgram[5].ToString() + "," +
                                             "[Gun7] = " + MonthlyProgram[6].ToString() + "," +
                                             "[Gun8] = " + MonthlyProgram[7].ToString() + "," +
                                             "[Gun9] = " + MonthlyProgram[8].ToString() + "," +
                                             "[Gun10] = " + MonthlyProgram[9].ToString() + "," +
                                             "[Gun11] = " + MonthlyProgram[10].ToString() + "," +
                                             "[Gun12] = " + MonthlyProgram[11].ToString() + "," +
                                             "[Gun13] = " + MonthlyProgram[12].ToString() + "," +
                                             "[Gun14] = " + MonthlyProgram[13].ToString() + "," +
                                             "[Gun15] = " + MonthlyProgram[14].ToString() + "," +
                                             "[Gun16] = " + MonthlyProgram[15].ToString() + "," +
                                             "[Gun17] = " + MonthlyProgram[16].ToString() + "," +
                                             "[Gun18] = " + MonthlyProgram[17].ToString() + "," +
                                             "[Gun19] = " + MonthlyProgram[18].ToString() + "," +
                                             "[Gun20] = " + MonthlyProgram[19].ToString() + "," +
                                             "[Gun21] = " + MonthlyProgram[20].ToString() + "," +
                                             "[Gun22] = " + MonthlyProgram[21].ToString() + "," +
                                             "[Gun23] = " + MonthlyProgram[22].ToString() + "," +
                                             "[Gun24] = " + MonthlyProgram[23].ToString() + "," +
                                             "[Gun25] = " + MonthlyProgram[24].ToString() + "," +
                                             "[Gun26] = " + MonthlyProgram[25].ToString() + "," +
                                             "[Gun27] = " + MonthlyProgram[26].ToString() + "," +
                                             "[Gun28] = " + MonthlyProgram[27].ToString() + "," +
                                             "[Gun29] = " + MonthlyProgram[28].ToString() + "," +
                                             "[Gun30] = " + MonthlyProgram[29].ToString() + "," +
                                             "[Gun31] = " + MonthlyProgram[30].ToString() + "," +
                                             "[Baslangic Saati 1] = '" + DailyStartHour3Slot[0].ToString("hh:mm:ss") + "'," +
                                             "[Baslangic Saati 2] = '" + DailyStartHour3Slot[1].ToString("hh:mm:ss") + "'," +
                                             "[Baslangic Saati 3] = '" + DailyStartHour3Slot[2].ToString("hh:mm:ss") + "'," +
                                             "[Ek Saat] = " + HourAddition.ToString() + "," +
                                             "[Ilave Saat Kontrolu] = " + WeeklyProgramPlusTimeCheck.ToString() + "," +
                                             "[Ilave Baslangic Saati] = '" + WeeklyProgramPlusStartHour.ToString("hh:mm:ss") + "'," +
                                             "[Ilave Bitis Saati] = '" + WeeklyProgramPlusEndHour.ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 1] = '" + DailyStartHour6Slot[0].ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 2] = '" + DailyStartHour6Slot[1].ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 3] = '" + DailyStartHour6Slot[2].ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 4] = '" + DailyStartHour6Slot[3].ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 5] = '" + DailyStartHour6Slot[4].ToString("hh:mm:ss") + "'," +
                                             "[Baslama Saat 6] = '" + DailyStartHour6Slot[5].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 1] = '" + DailyEndHour6Slot[0].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 2] = '" + DailyEndHour6Slot[1].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 3] = '" + DailyEndHour6Slot[2].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 4] = '" + DailyEndHour6Slot[3].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 5] = '" + DailyEndHour6Slot[4].ToString("hh:mm:ss") + "'," +
                                             "[Bitis Saat 6] = '" + DailyEndHour6Slot[5].ToString("hh:mm:ss") + "'," +
                                             "[Pazartesi Baslangic Saati] = " + "'" + WeeklyStartHour[0].ToString("hh:mm:ss") + "'," +
                                             "[Pazartesi Bitis Saati] = " + "'" + WeeklyEndHour[0].ToString("hh:mm:ss") + "'," +
                                             "[Sali Baslangic Saati] = " + "'" + WeeklyStartHour[1].ToString("hh:mm:ss") + "'," +
                                             "[Sali Bitis Saati] = " + "'" + WeeklyEndHour[1].ToString("hh:mm:ss") + "'," +
                                             "[Carsamba Baslangic Saati] = " + "'" + WeeklyStartHour[2].ToString("hh:mm:ss") + "'," +
                                             "[Carsamba Bitis Saati] = " + "'" + WeeklyEndHour[2].ToString("hh:mm:ss") + "'," +
                                             "[Persembe Baslangic Saati] = " + "'" + WeeklyStartHour[3].ToString("hh:mm:ss") + "'," +
                                             "[Persembe Bitis Saati] = " + "'" + WeeklyEndHour[3].ToString("hh:mm:ss") + "'," +
                                             "[Cuma Baslangic Saati] = " + "'" + WeeklyStartHour[4].ToString("hh:mm:ss") + "'," +
                                             "[Cuma Bitis Saati] = " + "'" + WeeklyEndHour[4].ToString("hh:mm:ss") + "'," +
                                             "[Cumartesi Baslangic Saati] = " + "'" + WeeklyStartHour[5].ToString("hh:mm:ss") + "'," +
                                             "[Cumartesi Bitis Saati] = " + "'" + WeeklyEndHour[5].ToString("hh:mm:ss") + "'," +
                                             "[Pazar Baslangic Saati] = " + "'" + WeeklyStartHour[6].ToString("hh:mm:ss") + "'," +
                                             "[Pazar Bitis Saati] = " + "'" + WeeklyEndHour[6].ToString("hh:mm:ss") + "' " +
                                             "WHERE [Zaman Grup No] = " + DBIntParam1.ToString();
                                }
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                TRetInt = tDBCmd2.ExecuteNonQuery();
                                if (TRetInt > 0)
                                    return true;
                                else
                                    return false;
                            }
                        }
                    }
                    break;
                case CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS:
                case CommandConstants.CMD_RCVALL_LOCALCAPACITYCOUNTERS:
                    {

                    }
                    break;
                case CommandConstants.CMD_RCV_RELAYPROGRAM:
                    {
                        byte[] Relay = new byte[16];
                        byte[] RelayState = new byte[16];
                        if (TmpReturnStr.Substring(TPos + 10, 1) == "O" && TmpReturnStr.Substring(TPos + 11, 1) == DBIntParam1.ToString() && TmpReturnStr.Substring(TPos + 12, 2) == DBIntParam2.ToString())
                        {
                            for (int i = 0; i < 16; i++)
                            {
                                if (Convert.ToByte(TmpReturnStr.Substring(TPos + 23 + i, 1)) == 0)
                                {
                                    Relay[i] = 0;
                                    RelayState[i] = 0;
                                }
                                else
                                {
                                    Relay[i] = 1;
                                    RelayState[i] = Convert.ToByte(TmpReturnStr.Substring(TPos + 24 + i, 1));
                                    if (RelayState[i] > 2)
                                        RelayState[i] = 0;
                                }
                            }
                            if (TmpTaskUpdateTable)
                            {
                                lock (TLockObj)
                                {
                                    tDBSQLStr = "SELECT * FROM ProgRelay2 " +
                                        "WHERE [Panel No] = " + mPanelNo.ToString() + " " +
                                        "AND [Haftanin Gunu] = " + DBIntParam1.ToString() + " " +
                                        "AND [Zaman Dilimi] = " + DBIntParam2.ToString();
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    if (!tDBReader.Read())
                                    {
                                        tDBSQLStr2 = "INSERT INTO ProgRelay2 " +
                                            "([Panel No],[Haftanin Gunu],[Zaman Dilimi],Aktif,[Saat 1],[Saat 2]," +
                                            "[Role 1],[Role 2],[Role 3],[Role 4],[Role 5],[Role 6],[Role 7],[Role 8]," +
                                            "[Role 9],[Role 10],[Role 11],[Role 12],[Role 13],[Role 14],[Role 15],[Role 16])" +
                                            "VALUES " +
                                            "(" +
                                            mPanelNo.ToString() + "," +
                                            DBIntParam1.ToString() + "," +
                                            DBIntParam2.ToString() + "," +
                                            Convert.ToInt32(TmpReturnStr.Substring(TPos + 14, 1)) + "," +
                                            "'" + (Convert.ToDateTime(TmpReturnStr.Substring(TPos + 15, 2) + ":" + TmpReturnStr.Substring(TPos + 17, 2) + ":00")).ToString("hh:mm:ss") + "'" +
                                            "'" + (Convert.ToDateTime(TmpReturnStr.Substring(TPos + 19, 2) + ":" + TmpReturnStr.Substring(TPos + 21, 2) + ":00")).ToString("hh:mm:ss") + "'" +
                                            Relay[0].ToString() + "," +
                                            Relay[1].ToString() + "," +
                                            Relay[2].ToString() + "," +
                                            Relay[3].ToString() + "," +
                                            Relay[4].ToString() + "," +
                                            Relay[5].ToString() + "," +
                                            Relay[6].ToString() + "," +
                                            Relay[7].ToString() + "," +
                                            Relay[8].ToString() + "," +
                                            Relay[9].ToString() + "," +
                                            Relay[10].ToString() + "," +
                                            Relay[11].ToString() + "," +
                                            Relay[12].ToString() + "," +
                                            Relay[13].ToString() + "," +
                                            Relay[14].ToString() + "," +
                                            Relay[15].ToString() + "," +
                                            RelayState[0].ToString() + "," +
                                            RelayState[1].ToString() + "," +
                                            RelayState[2].ToString() + "," +
                                            RelayState[3].ToString() + "," +
                                            RelayState[4].ToString() + "," +
                                            RelayState[5].ToString() + "," +
                                            RelayState[6].ToString() + "," +
                                            RelayState[7].ToString() + "," +
                                            RelayState[8].ToString() + "," +
                                            RelayState[9].ToString() + "," +
                                            RelayState[10].ToString() + "," +
                                            RelayState[11].ToString() + "," +
                                            RelayState[12].ToString() + "," +
                                            RelayState[13].ToString() + "," +
                                            RelayState[14].ToString() + "," +
                                            RelayState[15].ToString() + ")";
                                    }
                                    else
                                    {
                                        tDBSQLStr2 = "UPTADE ProgRelay2 " +
                                            "SET " +
                                              "[Aktif] =   " + Convert.ToInt16(TmpReturnStr.Substring(TPos + 14, 1)) +
                                              "[Saat 1] =  '" + (Convert.ToDateTime(TmpReturnStr.Substring(TPos + 15, 2) + ":" + TmpReturnStr.Substring(TPos + 17, 2) + ":00")).ToString("hh:mm:ss") + "'," +
                                              "[Saat 2] =  '" + (Convert.ToDateTime(TmpReturnStr.Substring(TPos + 19, 2) + ":" + TmpReturnStr.Substring(TPos + 21, 2) + ":00")).ToString("hh:mm:ss") + "'," +
                                              "[Role 1] = " + Relay[0].ToString() + ", " +
                                              "[Role 2] = " + Relay[1].ToString() + ", " +
                                              "[Role 3] = " + Relay[2].ToString() + ", " +
                                              "[Role 4] = " + Relay[3].ToString() + ", " +
                                              "[Role 5] = " + Relay[4].ToString() + ", " +
                                              "[Role 6] = " + Relay[5].ToString() + ", " +
                                              "[Role 7] = " + Relay[6].ToString() + ", " +
                                              "[Role 8] = " + Relay[7].ToString() + ", " +
                                              "[Role 9] = " + Relay[8].ToString() + ", " +
                                              "[Role 10] =" + Relay[9].ToString() + ", " +
                                              "[Role 11] =" + Relay[10].ToString() + ", " +
                                              "[Role 12] =" + Relay[11].ToString() + ", " +
                                              "[Role 13] =" + Relay[12].ToString() + ", " +
                                              "[Role 14] =" + Relay[13].ToString() + ", " +
                                              "[Role 15] =" + Relay[14].ToString() + ", " +
                                              "[Role 16] =" + Relay[15].ToString() + ", " +
                                              "[Durum 1] =" + RelayState[0].ToString() + ", " +
                                              "[Durum 2] =" + RelayState[1].ToString() + ", " +
                                              "[Durum 3] =" + RelayState[2].ToString() + ", " +
                                              "[Durum 4] =" + RelayState[3].ToString() + ", " +
                                              "[Durum 5] =" + RelayState[4].ToString() + ", " +
                                              "[Durum 6] =" + RelayState[5].ToString() + ", " +
                                              "[Durum 7] =" + RelayState[6].ToString() + ", " +
                                              "[Durum 8] =" + RelayState[7].ToString() + ", " +
                                              "[Durum 9] =" + RelayState[8].ToString() + ", " +
                                              "[Durum 10]=" + RelayState[9].ToString() + ", " +
                                              "[Durum 11]=" + RelayState[10].ToString() + ", " +
                                              "[Durum 12]=" + RelayState[11].ToString() + ", " +
                                              "[Durum 13]=" + RelayState[12].ToString() + ", " +
                                              "[Durum 14]=" + RelayState[13].ToString() + ", " +
                                              "[Durum 15]=" + RelayState[14].ToString() + ", " +
                                              "[Durum 16]=" + RelayState[15].ToString() + " " +
                                              "WHERE [Panel No] = " + mPanelNo.ToString() + " " +
                                              "AND [Haftanin Gunu] = " + DBIntParam1.ToString() + " " +
                                              "AND [Zaman Dilimi] = " + DBIntParam2.ToString();

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
                    }
                    break;
                case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    {
                        int Aktif;
                        int ID;
                        int TCPPort;
                        string IPAdress;
                        int[] LocalGateway = new int[4];
                        int[] LocalIPAddress = new int[4];
                        int LocalTCPPort;
                        int[] LocalSubnetMask = new int[4];
                        int[] RemoteIPAddress = new int[4];
                        int[] PanelGlobalGroups = new int[8];
                        int GlobalAccessCountControl;
                        int GlobalMaxInCountControl;
                        int GlobalSequentalAccess;
                        int GlobalCapacityControl;
                        int[] LocalCapacityActive = new int[8];
                        int[] LocalCapacityClear = new int[8];
                        long[] LocalCapacityValue = new long[8];
                        int PanelCapacityGroupNo;
                        int PanelCapacityNumber;
                        int PanelCapacityControlActive;
                        int ClearPanelCapacityAtNight;
                        int ButtonActive;
                        int StatusDataUpdate;
                        int StatusDataUpdateType;
                        int StatusDataUpdateTime;
                        int GlobalZoneInterlockActive;
                        int InterlockActive;
                        int SameDoorMultipleReader;
                        int LiftCapacity;
                        int PanelExpansion;
                        int PanelExpansion2;
                        int PanelModel;
                        string PanelName;
                        int PanelSameTagBlockTime;
                        int PanelSameTagBlockType;
                        int PanelSameTagBlockMinSecHour;
                        int PanelWorkingMode;
                        int PanelCardCount;
                        int PanelButtonDetector;
                        int PanelButtonDetectorTime;
                        int PanelAlarmModeRelayOk;
                        int PanelAlarmMode;
                        int PanelFireModeRelayOk;
                        int PanelFireMode;
                        int PanelDoorAlarmRelayOk;
                        int PanelAlarmBroadcastOk;
                        int PanelFireBroadcastOk;
                        int PanelDoorAlarmBroadcastOk;
                        int[] ReaderActive = new int[8];
                        string[] ReaderName = new string[8];
                        int[] ReaderModel = new int[8];
                        int[] ReaderDoorType = new int[8];
                        int[] ReaderRelayTime = new int[8];
                        int[] ReaderWorkingMode = new int[8];
                        int[] ReaderAccessControlMode = new int[8];
                        int[] ReaderLocalGroup = new int[8];
                        int[] WIGReaderActive = new int[16];
                        string[] WIGReaderName = new string[16];
                        int[] WIGReaderRelayNo = new int[16];
                        int[] WIGReaderRelayTime = new int[16];
                        int[] WIGReaderDoorType = new int[16];
                        int[] WIGReaderWIGType = new int[16];
                        int[] WIGReaderLocalGroup = new int[16];
                        int[] WIGReaderAccessControlMode = new int[16];
                        int[] WIGReaderSeqAccessMain = new int[16];
                        int[] WIGReaderMultipleAuthorization = new int[16];
                        int[] WIGReaderAlarmLock = new int[16];
                        int[] WIGReaderFireUnlock = new int[16];
                        int[] WIGReaderPINVerify = new int[16];
                        int[] WIGReaderParkingGate = new int[16];
                        int[] WIGReaderDoorOpenTime = new int[16];
                        int[] WIGReaderDoorOpenTimeAlarm = new int[16];
                        int[] WIGReaderDoorForcingAlarm = new int[16];
                        int[] WIGReaderDoorOpenAlarm = new int[16];
                        int[] WIGReaderDoorPanicButtonAlarm = new int[16];
                        int[] WIGReaderDoorExternalAlarmRelay = new int[16];
                        int[] WIGReaderButtonDetectorFunction = new int[16];
                        int[] WIGReaderDoorPushDelay = new int[16];
                        int[] WIGReaderUserCount = new int[16];
                        int PanelLocalAPB;
                        int[] PanelLocalAPBs = new int[8];
                        int[] PanelMasterRelayTime = new int[8];
                        int PanelAlarmRelayTime;
                        int PanelMACAddress;
                        long SeriNo;
                        int PanelGlobalAPB;
                        int PanelGroupNo;

                        //Panel MAC Address
                        SI = 3;
                        PanelMACAddress = int.Parse(TmpReturnStr.Substring(SI, 4), NumberStyles.HexNumber);


                        //Panel ID
                        SI = SI + 4;
                        ID = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));

                        //Panel Working Mode
                        SI = SI + 3;
                        if (TmpReturnStr.Substring(SI, 1) == "0")
                            PanelWorkingMode = 0;
                        else
                            PanelWorkingMode = 1;

                        //User Count
                        SI = SI + 1;
                        PanelCardCount = Convert.ToInt32(TmpReturnStr.Substring(SI, 6));

                        //Master Relay Times
                        SI = SI + 6;
                        for (int i = 0; i < 8; i++)
                        {
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 2), 2)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 2), 2)) <= 30)
                            {
                                PanelMasterRelayTime[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 2), 2));
                            }
                        }

                        //Alarm Relay Time
                        SI = SI + 16;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) <= 30)
                            PanelAlarmRelayTime = Convert.ToInt32(TmpReturnStr.Substring(SI, 2));
                        else
                            PanelAlarmRelayTime = 0;
                        //Panel TCP/IP Settings
                        SI = SI + 2;
                        LocalGateway[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                        LocalGateway[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                        LocalGateway[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                        LocalGateway[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                        SI = SI + 12;
                        LocalIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                        LocalIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                        LocalIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                        LocalIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                        SI = SI + 12;
                        LocalTCPPort = Convert.ToInt32(TmpReturnStr.Substring(SI, 5));


                        SI = SI + 5;
                        LocalSubnetMask[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                        LocalSubnetMask[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                        LocalSubnetMask[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                        LocalSubnetMask[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                        SI = SI + 12;
                        RemoteIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                        RemoteIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                        RemoteIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                        RemoteIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                        //Panel Button Detector
                        SI = SI + 12;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 1)
                            PanelButtonDetector = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            PanelButtonDetector = 0;


                        //Panel Button Detector Time
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 9)
                            PanelButtonDetectorTime = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            PanelButtonDetectorTime = 1;

                        //Global Zone Interlock Active
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 1)
                            GlobalZoneInterlockActive = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            GlobalZoneInterlockActive = 0;

                        //Same Door Multiple Reader
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 1)
                            SameDoorMultipleReader = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            SameDoorMultipleReader = 0;

                        //Interlock Active
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 1)
                            InterlockActive = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            InterlockActive = 0;

                        //Lift Capacity
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) <= 6)
                            LiftCapacity = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));
                        else
                            LiftCapacity = 0;

                        //Panel Expansion
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            PanelExpansion = 1;
                        else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 2)
                            PanelExpansion = 2;
                        else
                            PanelExpansion = 0;

                        //Panel Expansion 2
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 2)
                            PanelExpansion2 = 2;
                        else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 3)
                            PanelExpansion2 = 3;
                        else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 4)
                            PanelExpansion2 = 4;
                        else
                            PanelExpansion2 = 0;

                        //Panel Model
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 5)
                            PanelModel = 5;
                        else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 6)
                            PanelModel = 6;
                        else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 7)
                            PanelModel = 7;
                        else
                            PanelModel = 5;


                        //Status Data Update Time
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            StatusDataUpdateTime = 1;
                        else
                            StatusDataUpdateTime = 0;


                        //Status Data Update
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            StatusDataUpdate = 1;
                        else
                            StatusDataUpdate = 0;


                        //Status Data Update Type
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            StatusDataUpdateType = 1;
                        else
                            StatusDataUpdateType = 0;

                        //Local Antipassback
                        SI = SI + 1;
                        for (int i = 0; i < 8; i++)
                        {
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + i, 1)) == 1)
                                PanelLocalAPBs[i] = 1;
                            else
                                PanelLocalAPBs[i] = 0;
                        }


                        //Global Antipassback
                        SI = SI + 8;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            PanelGlobalAPB = 1;
                        else
                            PanelGlobalAPB = 0;


                        //Global MaxIn Count Control
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            GlobalMaxInCountControl = 1;
                        else
                            GlobalMaxInCountControl = 0;

                        //Global Access Count Control
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            GlobalAccessCountControl = 1;
                        else
                            GlobalAccessCountControl = 0;


                        //Global Capacity Control
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            GlobalCapacityControl = 1;
                        else
                            GlobalCapacityControl = 0;


                        //Global Sequental Access Control
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            GlobalSequentalAccess = 1;
                        else
                            GlobalSequentalAccess = 0;

                        //Same Tag Block Time
                        SI = SI + 1;
                        PanelSameTagBlockTime = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));

                        //Same Tag Block Type
                        SI = SI + 3;
                        PanelSameTagBlockType = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));

                        //Same Tag Block Second-Minute-Hour
                        SI = SI + 1;
                        PanelSameTagBlockMinSecHour = Convert.ToInt32(TmpReturnStr.Substring(SI, 1));

                        //Alarm & Fire Modes
                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelAlarmMode = 0;
                        else
                            PanelAlarmMode = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelAlarmModeRelayOk = 0;
                        else
                            PanelAlarmModeRelayOk = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelFireMode = 0;
                        else
                            PanelFireMode = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelFireModeRelayOk = 0;
                        else
                            PanelFireModeRelayOk = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelDoorAlarmRelayOk = 0;
                        else
                            PanelDoorAlarmRelayOk = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelAlarmBroadcastOk = 0;
                        else
                            PanelAlarmBroadcastOk = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelFireBroadcastOk = 0;
                        else
                            PanelFireBroadcastOk = 1;


                        SI = SI + 1;
                        if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            PanelDoorAlarmBroadcastOk = 0;
                        else
                            PanelDoorAlarmBroadcastOk = 1;


                        //Global Zones
                        SI = SI + 1;
                        for (int i = 0; i < 8; i++)
                        {
                            PanelGlobalGroups[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 3), 1));
                        }


                        //Capacity Values
                        SI = SI + 24;
                        for (int i = 0; i < 8; i++)
                        {
                            //Active
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 8), 1)) == 1)
                                LocalCapacityActive[i] = 1;
                            else
                                LocalCapacityActive[i] = 0;

                            LocalCapacityClear[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 8) + 1, 1));
                            LocalCapacityValue[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * 8) + 2, 6));
                        }
                        //LCD Line 1
                        SI = SI + 64;
                        PanelName = TmpReturnStr.Substring(SI, 16);


                        //WIEGAND Reader Settings
                        SI = SI + 16;
                        int RFrm = 39;
                        for (int i = 0; i < 16; i++)
                        {
                            //Active
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm), 1)) == 1)
                                WIGReaderActive[i] = 1;
                            else
                                WIGReaderActive[i] = 0;

                            //WIG Reader Relay No
                            WIGReaderRelayNo[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 1, 2));


                            //WIG Reader Door Type
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 3, 1)) == 1)
                                WIGReaderDoorType[i] = 1; //Giriş
                            else
                                WIGReaderDoorType[i] = 2; //Çıkış


                            //WIG Reader WIG Type
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 4, 1)) == 1)
                                WIGReaderWIGType[i] = 1;
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 4, 1)) == 2)
                                WIGReaderWIGType[i] = 2;
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 4, 1)) == 3)
                                WIGReaderWIGType[i] = 3;
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 4, 1)) == 4)
                                WIGReaderWIGType[i] = 4;
                            else
                                WIGReaderWIGType[i] = 0;


                            //WIG Reader Local Group
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 5, 1)) <= 8)
                                WIGReaderLocalGroup[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 5, 1));
                            else
                                WIGReaderLocalGroup[i] = 0;

                            //WIG Reader Sequental Access Main Door
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 6, 1)) == 1)
                                WIGReaderSeqAccessMain[i] = 1;
                            else
                                WIGReaderSeqAccessMain[i] = 0;

                            //WIG Reader Multiple Authorization
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 7, 1)) == 1)
                                WIGReaderMultipleAuthorization[i] = 1;
                            else
                                WIGReaderMultipleAuthorization[i] = 0;

                            //WIG Reader Alarm Lock
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 8, 1)) == 1)
                                WIGReaderAlarmLock[i] = 1;
                            else
                                WIGReaderAlarmLock[i] = 0;

                            //WIG Reader Fire Unlock
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 9, 1)) == 1)
                                WIGReaderFireUnlock[i] = 1;
                            else
                                WIGReaderFireUnlock[i] = 0;

                            //WIG Reader Pin Verify
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 10, 1)) == 1)
                                WIGReaderPINVerify[i] = 1;
                            else
                                WIGReaderPINVerify[i] = 0;

                            //WIG Reader Lift Active
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 11, 1)) == 1)
                                WIGReaderParkingGate[i] = 1;
                            else
                                WIGReaderParkingGate[i] = 0;

                            //WIG Reader Door Open Time
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 12, 3)) >= 1 && Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 12, 3)) <= 999)
                                WIGReaderDoorOpenTime[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 12, 3));
                            else
                                WIGReaderDoorOpenTime[i] = 20;

                            //WIG Reader Door Open Time Alarm
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 15, 1)) == 1)
                                WIGReaderDoorOpenTimeAlarm[i] = 1;
                            else
                                WIGReaderDoorOpenTimeAlarm[i] = 0;

                            //WIG Reader Door Forcing Alarm
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 16, 1)) == 1)
                                WIGReaderDoorForcingAlarm[i] = 1;
                            else
                                WIGReaderDoorForcingAlarm[i] = 0;

                            //WIG Reader Door Open Alarm
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 17, 1)) == 1)
                                WIGReaderDoorOpenAlarm[i] = 1;
                            else
                                WIGReaderDoorOpenAlarm[i] = 0;

                            //WIG Reader Door Panic Button Alarm
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 18, 1)) == 1)
                                WIGReaderDoorPanicButtonAlarm[i] = 1;
                            else
                                WIGReaderDoorPanicButtonAlarm[i] = 0;

                            //WIG Reader External Alarm Relay
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 19, 2)) >= 1 && Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 19, 2)) <= 64)
                                WIGReaderDoorExternalAlarmRelay[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 19, 2));
                            else
                                WIGReaderDoorExternalAlarmRelay[i] = 1;

                            //WIG Reader Door Open Time 
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 21, 1)) >= 1 && Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 21, 1)) <= 9)
                                WIGReaderDoorPushDelay[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 21, 1));
                            else
                                WIGReaderDoorPushDelay[i] = 3;

                            //WIG Reader User Count
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 22, 1)) >= 2 && Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 22, 1)) <= 6)
                                WIGReaderUserCount[i] = Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 22, 1));
                            else
                                WIGReaderUserCount[i] = 2;

                            //WIG Reader Name
                            if (TmpReturnStr.Substring(SI + (i * RFrm) + 23, 15).Contains("\0"))
                            {
                                WIGReaderName[i] = "";
                            }
                            else
                            {
                                WIGReaderName[i] = TmpReturnStr.Substring(SI + (i * RFrm) + 23, 15);
                            }


                            //WIG Reader Button Loop Function
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI + (i * RFrm) + 38, 1)) == 1)
                                WIGReaderButtonDetectorFunction[i] = 1;
                            else
                                WIGReaderButtonDetectorFunction[i] = 0;
                        }
                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                tDBSQLStr = "SELECT * FROM PanelSettings " +
                                    "WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                    "AND [Panel ID] = " + mPanelNo.ToString();
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                tDBReader = tDBCmd.ExecuteReader();
                                if (!tDBReader.Read())
                                {
                                    tDBSQLStr2 = "INSERT INTO PanelSettings " +
                                        "(" +
                                        "[Seri No],[Panel ID],[Panel Model],[Kontrol Modu]," +
                                        "[Panel M1 Role],[Panel M2 Role],[Panel M3 Role],[Panel M4 Role],[Panel M5 Role],[Panel M6 Role],[Panel M7 Role],[Panel M8 Role],[Panel Alarm Role]," +
                                        "[Panel Alarm Mode],[Panel Alarm Mode Role Ok],[Panel Fire Mode],[Panel Fire Mode Role Ok],[Panel Door Alarm Role Ok],[Panel Alarm Broadcast Ok],[Panel Fire Broadcast Ok],[Panel Door Alarm Broadcast Ok]," +
                                        "[Panel GW1],[Panel GW2],[Panel GW3],[Panel GW4]," +
                                        "[Panel IP1],[Panel IP2],[Panel IP3],[Panel IP4]," +
                                        "[Panel TCP Port],[Panel Subnet1],[Panel Subnet2],[Panel Subnet3],[Panel Subnet4]," +
                                        "[Panel Remote IP1],[Panel Remote IP2],[Panel Remote IP3],[Panel Remote IP4]," +
                                        "[Panel Name],[Panel Same Tag Block],[Panel Same Tag Block Type],[Panel Same Tag Block HourMinSec],[Status Data Update]," +
                                        "[Status Data Update Type],[Status Data Update Time],[Same Door Multiple Reader],[Interlock Active],[Global Zone Interlock Active]," +
                                        "[Panel Button Detector],[Panel Button Detector Time],[Lift Capacity],[Panel Expansion],[Panel Expansion 2]," +
                                        "[Lokal APB1],[Lokal APB2],[Lokal APB3],[Lokal APB4],[Lokal APB5],[Lokal APB6],[Lokal APB7],[Lokal APB8]," +
                                        "[Global APB]," +
                                        "[Global MaxIn Count Control],[Global Access Count Control],[Global Capacity Control],[Global Sequental Access Control]," +
                                        "[Panel Global Bolge1],[Panel Global Bolge2],[Panel Global Bolge3],[Panel Global Bolge4],[Panel Global Bolge5],[Panel Global Bolge6],[Panel Global Bolge7],[Panel Global Bolge8]," +
                                        "[Panel Local Capacity1],[Panel Local Capacity2],[Panel Local Capacity3],[Panel Local Capacity4],[Panel Local Capacity5],[Panel Local Capacity6],[Panel Local Capacity7],[Panel Local Capacity8]," +
                                        "[Panel Local Capacity Clear1],[Panel Local Capacity Clear2],[Panel Local Capacity Clear3],[Panel Local Capacity Clear4],[Panel Local Capacity Clear5],[Panel Local Capacity Clear6],[Panel Local Capacity Clear7],[Panel Local Capacity Clear8]," +
                                        "[Panel Local Capacity Value1],[Panel Local Capacity Value2],[Panel Local Capacity Value3],[Panel Local Capacity Value4],[Panel Local Capacity Value5],[Panel Local Capacity Value6],[Panel Local Capacity Value7],[Panel Local Capacity Value8]" +
                                        ")" +
                                        "VALUES " +
                                        "(" +
                                        PanelMACAddress + "," + ID + "," + PanelModel + "," + PanelWorkingMode + "," +
                                        PanelMasterRelayTime[0] + "," + PanelMasterRelayTime[1] + "," + PanelMasterRelayTime[2] + "," + PanelMasterRelayTime[3] + "," +
                                        PanelMasterRelayTime[4] + "," + PanelMasterRelayTime[5] + "," + PanelMasterRelayTime[6] + "," + PanelMasterRelayTime[7] + "," +
                                        PanelAlarmRelayTime + "," + PanelAlarmMode + "," + PanelAlarmModeRelayOk + "," + PanelFireMode + "," + PanelFireModeRelayOk + "," + PanelDoorAlarmRelayOk + "," +
                                        PanelAlarmBroadcastOk + "," + PanelFireBroadcastOk + "," + PanelDoorAlarmBroadcastOk + "," +
                                        LocalGateway[0] + "," + LocalGateway[1] + "," + LocalGateway[2] + "," + LocalGateway[3] + "," +
                                        LocalIPAddress[0] + "," + LocalIPAddress[1] + "," + LocalIPAddress[2] + "," + LocalIPAddress[3] + "," +
                                        LocalTCPPort + "," + LocalSubnetMask[0] + "," + LocalSubnetMask[1] + "," + LocalSubnetMask[2] + "," + LocalSubnetMask[3] + "," +
                                        RemoteIPAddress[0] + "," + RemoteIPAddress[1] + "," + RemoteIPAddress[2] + "," + RemoteIPAddress[3] + "," +
                                        "'" + PanelName + "'," + PanelSameTagBlockTime + "," + PanelSameTagBlockType + "," + PanelSameTagBlockMinSecHour + "," + StatusDataUpdate + "," +
                                        StatusDataUpdateType + "," + StatusDataUpdateTime + "," + SameDoorMultipleReader + "," + InterlockActive + "," + GlobalZoneInterlockActive + "," + PanelButtonDetector + "," +
                                        PanelButtonDetectorTime + "," + LiftCapacity + "," + PanelExpansion + "," + PanelExpansion2 + "," +
                                        PanelLocalAPBs[0] + "," + PanelLocalAPBs[1] + "," + PanelLocalAPBs[2] + "," + PanelLocalAPBs[3] + "," + PanelLocalAPBs[4] + "," + PanelLocalAPBs[5] + "," + PanelLocalAPBs[6] + "," + PanelLocalAPBs[7] + "," +
                                        PanelGlobalAPB + "," + GlobalMaxInCountControl + "," + GlobalAccessCountControl + "," + GlobalCapacityControl + "," + GlobalSequentalAccess + "," +
                                        PanelGlobalGroups[0] + "," + PanelGlobalGroups[1] + "," + PanelGlobalGroups[2] + "," + PanelGlobalGroups[3] + "," + PanelGlobalGroups[4] + "," + PanelGlobalGroups[5] + "," + PanelGlobalGroups[6] + "," + PanelGlobalGroups[7] + "," +
                                        LocalCapacityActive[0] + "," + LocalCapacityActive[1] + "," + LocalCapacityActive[2] + "," + LocalCapacityActive[3] + "," + LocalCapacityActive[4] + "," + LocalCapacityActive[5] + "," + LocalCapacityActive[6] + "," + LocalCapacityActive[7] + "," +
                                        LocalCapacityClear[0] + "," + LocalCapacityClear[1] + "," + LocalCapacityClear[2] + "," + LocalCapacityClear[3] + "," + LocalCapacityClear[4] + "," + LocalCapacityClear[5] + "," + LocalCapacityClear[6] + "," + LocalCapacityClear[7] + "," +
                                        LocalCapacityValue[0] + "," + LocalCapacityValue[1] + "," + LocalCapacityValue[2] + "," + LocalCapacityValue[3] + "," + LocalCapacityValue[4] + "," + LocalCapacityValue[5] + "," + LocalCapacityValue[6] + "," + LocalCapacityValue[7] +
                                        ")";

                                }
                                else
                                {
                                    tDBSQLStr2 = "UPDATE PanelSettings " +
                                        "SET " +
                                        "[Seri No] = " + PanelMACAddress + "," +
                                        "[Panel ID] = " + ID + "," +
                                        "[Panel Model] = " + PanelModel + "," +
                                        "[Kontrol Modu] = " + PanelWorkingMode + "," +
                                        "[Panel M1 Role] = " + PanelMasterRelayTime[0] + "," +
                                        "[Panel M2 Role] = " + PanelMasterRelayTime[1] + "," +
                                        "[Panel M3 Role] = " + PanelMasterRelayTime[2] + "," +
                                        "[Panel M4 Role] = " + PanelMasterRelayTime[3] + "," +
                                        "[Panel M5 Role] = " + PanelMasterRelayTime[4] + "," +
                                        "[Panel M6 Role] = " + PanelMasterRelayTime[5] + "," +
                                        "[Panel M7 Role] = " + PanelMasterRelayTime[6] + "," +
                                        "[Panel M8 Role] = " + PanelMasterRelayTime[7] + "," +
                                        "[Panel Alarm Role] = " + PanelAlarmRelayTime + "," +
                                        "[Panel Alarm Mode] = " + PanelAlarmMode + "," +
                                        "[Panel Alarm Mode Role Ok] = " + PanelAlarmModeRelayOk + "," +
                                        "[Panel Fire Mode] = " + PanelFireMode + "," +
                                        "[Panel Fire Mode Role Ok] = " + PanelFireModeRelayOk + "," +
                                        "[Panel Door Alarm Role Ok] = " + PanelDoorAlarmRelayOk + "," +
                                        "[Panel Alarm Broadcast Ok] = " + PanelAlarmBroadcastOk + "," +
                                        "[Panel Fire Broadcast Ok] = " + PanelFireBroadcastOk + "," +
                                        "[Panel Door Alarm Broadcast Ok] = " + PanelDoorAlarmBroadcastOk + "," +
                                        "[Panel GW1] = " + LocalGateway[0] + "," +
                                        "[Panel GW2] = " + LocalGateway[1] + "," +
                                        "[Panel GW3] = " + LocalGateway[2] + "," +
                                        "[Panel GW4] = " + LocalGateway[3] + "," +
                                        "[Panel IP1] = " + LocalIPAddress[0] + "," +
                                        "[Panel IP2] = " + LocalIPAddress[1] + "," +
                                        "[Panel IP3] = " + LocalIPAddress[2] + "," +
                                        "[Panel IP4] = " + LocalIPAddress[3] + "," +
                                        "[Panel TCP Port] = " + LocalTCPPort + "," +
                                        "[Panel Subnet1] = " + LocalSubnetMask[0] + "," +
                                        "[Panel Subnet2] = " + LocalSubnetMask[1] + "," +
                                        "[Panel Subnet3] = " + LocalSubnetMask[2] + "," +
                                        "[Panel Subnet4] = " + LocalSubnetMask[3] + "," +
                                        "[Panel Remote IP1] = " + RemoteIPAddress[0] + "," +
                                        "[Panel Remote IP2] = " + RemoteIPAddress[1] + "," +
                                        "[Panel Remote IP3] = " + RemoteIPAddress[2] + "," +
                                        "[Panel Remote IP4] = " + RemoteIPAddress[3] + "," +
                                        "[Panel Name] = '" + PanelName + "'," +
                                        "[Panel Same Tag Block] = " + PanelSameTagBlockTime + "," +
                                        "[Panel Same Tag Block Type] = " + PanelSameTagBlockType + "," +
                                        "[Panel Same Tag Block HourMinSec] = " + PanelSameTagBlockMinSecHour + "," +
                                        "[Status Data Update] = " + StatusDataUpdate + "," +
                                        "[Status Data Update Type] = " + StatusDataUpdateType + "," +
                                        "[Status Data Update Time] = " + StatusDataUpdateTime + "," +
                                        "[Same Door Multiple Reader] = " + SameDoorMultipleReader + "," +
                                        "[Interlock Active] = " + InterlockActive + "," +
                                        "[Global Zone Interlock Active] = " + GlobalZoneInterlockActive + "," +
                                        "[Panel Button Detector] = " + PanelButtonDetector + "," +
                                        "[Panel Button Detector Time] = " + PanelButtonDetectorTime + "," +
                                        "[Lift Capacity] = " + LiftCapacity + "," +
                                        "[Panel Expansion] = " + PanelExpansion + "," +
                                        "[Panel Expansion 2] = " + PanelExpansion2 + "," +
                                        "[Lokal APB1] = " + PanelLocalAPBs[0] + "," +
                                        "[Lokal APB2] = " + PanelLocalAPBs[1] + "," +
                                        "[Lokal APB3] = " + PanelLocalAPBs[2] + "," +
                                        "[Lokal APB4] = " + PanelLocalAPBs[3] + "," +
                                        "[Lokal APB5] = " + PanelLocalAPBs[4] + "," +
                                        "[Lokal APB6] = " + PanelLocalAPBs[5] + "," +
                                        "[Lokal APB7] = " + PanelLocalAPBs[6] + "," +
                                        "[Lokal APB8] = " + PanelLocalAPBs[7] + "," +
                                        "[Global APB] = " + PanelGlobalAPB + "," +
                                        "[Global MaxIn Count Control] = " + GlobalMaxInCountControl + "," +
                                        "[Global Access Count Control] = " + GlobalAccessCountControl + "," +
                                        "[Global Capacity Control] = " + GlobalCapacityControl + "," +
                                        "[Global Sequental Access Control] = " + GlobalSequentalAccess + "," +
                                        "[Panel Global Bolge1] = " + PanelGlobalGroups[0] + "," +
                                        "[Panel Global Bolge2] = " + PanelGlobalGroups[1] + "," +
                                        "[Panel Global Bolge3] = " + PanelGlobalGroups[2] + "," +
                                        "[Panel Global Bolge4] = " + PanelGlobalGroups[3] + "," +
                                        "[Panel Global Bolge5] = " + PanelGlobalGroups[4] + "," +
                                        "[Panel Global Bolge6] = " + PanelGlobalGroups[5] + "," +
                                        "[Panel Global Bolge7] = " + PanelGlobalGroups[6] + "," +
                                        "[Panel Global Bolge8] = " + PanelGlobalGroups[7] + "," +
                                        "[Panel Local Capacity1] = " + LocalCapacityActive[0] + "," +
                                        "[Panel Local Capacity2] = " + LocalCapacityActive[1] + "," +
                                        "[Panel Local Capacity3] = " + LocalCapacityActive[2] + "," +
                                        "[Panel Local Capacity4] = " + LocalCapacityActive[3] + "," +
                                        "[Panel Local Capacity5] = " + LocalCapacityActive[4] + "," +
                                        "[Panel Local Capacity6] = " + LocalCapacityActive[5] + "," +
                                        "[Panel Local Capacity7] = " + LocalCapacityActive[6] + "," +
                                        "[Panel Local Capacity8] = " + LocalCapacityActive[7] + "," +
                                        "[Panel Local Capacity Clear1] = " + LocalCapacityClear[0] + "," +
                                        "[Panel Local Capacity Clear2] = " + LocalCapacityClear[1] + "," +
                                        "[Panel Local Capacity Clear3] = " + LocalCapacityClear[2] + "," +
                                        "[Panel Local Capacity Clear4] = " + LocalCapacityClear[3] + "," +
                                        "[Panel Local Capacity Clear5] = " + LocalCapacityClear[4] + "," +
                                        "[Panel Local Capacity Clear6] = " + LocalCapacityClear[5] + "," +
                                        "[Panel Local Capacity Clear7] = " + LocalCapacityClear[6] + "," +
                                        "[Panel Local Capacity Clear8] = " + LocalCapacityClear[7] + "," +
                                        "[Panel Local Capacity Value1] = " + LocalCapacityValue[0] + "," +
                                        "[Panel Local Capacity Value2] = " + LocalCapacityValue[1] + "," +
                                        "[Panel Local Capacity Value3] = " + LocalCapacityValue[2] + "," +
                                        "[Panel Local Capacity Value4] = " + LocalCapacityValue[3] + "," +
                                        "[Panel Local Capacity Value5] = " + LocalCapacityValue[4] + "," +
                                        "[Panel Local Capacity Value6] = " + LocalCapacityValue[5] + "," +
                                        "[Panel Local Capacity Value7] = " + LocalCapacityValue[6] + "," +
                                        "[Panel Local Capacity Value8] = " + LocalCapacityValue[7] + " " +
                                        "WHERE [Panel ID] = " + ID + " " +
                                        "AND [Seri No] = " + PanelMACAddress;
                                }
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                TRetInt = tDBCmd2.ExecuteNonQuery();
                                if (TRetInt <= 0)
                                {
                                    return false;
                                }

                            }

                            lock (TLockObj)
                            {
                                bool Result = false;
                                for (int i = 0; i < 16; i++)
                                {
                                    tDBSQLStr = "";
                                    StringBuilder tVeriable = new StringBuilder();
                                    tDBSQLStr = "SELECT * FROM ReaderSettingsNew " +
                                        "WHERE [Seri No] = " + mPanelSerialNo.ToString();
                                    tDBSQLStr += " AND [WKapi ID] =" + (i + 1).ToString();
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    if (!tDBReader.Read())
                                    {
                                        tVeriable.Append("INSERT INTO ReaderSettingsNew ");
                                        tVeriable.Append("(");
                                        tVeriable.Append("[Seri No],");
                                        tVeriable.Append("[Sira No],");
                                        tVeriable.Append("[Panel ID],");
                                        tVeriable.Append("[Panel Name],");
                                        tVeriable.Append("[WKapi ID],");
                                        tVeriable.Append("[WKapi Aktif],");
                                        tVeriable.Append("[WKapi Lift Aktif],");
                                        tVeriable.Append("[WKapi Role No],");
                                        tVeriable.Append("[WKapi Adi],");
                                        tVeriable.Append("[WKapi Kapi Tipi],");
                                        tVeriable.Append("[WKapi WIGType],");
                                        tVeriable.Append("[WKapi Lokal Bolge],");
                                        tVeriable.Append("[WKapi Gecis Modu],");
                                        tVeriable.Append("[WKapi Alarm Modu],");
                                        tVeriable.Append("[WKapi Yangin Modu],");
                                        tVeriable.Append("[WKapi Pin Dogrulama],");
                                        tVeriable.Append("[WKapi Ana Alarm Rolesi],");
                                        tVeriable.Append("[WKapi Sirali Gecis Ana Kapi],");
                                        tVeriable.Append("[WKapi Coklu Onay],");
                                        tVeriable.Append("[WKapi Acik Sure],");
                                        tVeriable.Append("[WKapi Acik Sure Alarmi],");
                                        tVeriable.Append("[WKapi Zorlama Alarmi],");
                                        tVeriable.Append("[WKapi Acilma Alarmi],");
                                        tVeriable.Append("[WKapi Harici Alarm Rolesi],");
                                        tVeriable.Append("[WKapi Panik Buton Alarmi],");
                                        tVeriable.Append("[WKapi Itme Gecikmesi],");
                                        tVeriable.Append("[WKapi User Count],");

                                        tDBSQLStr2 = tVeriable.ToString().Substring(0, tVeriable.Length - 1);
                                        tDBSQLStr2 += ")";
                                        tDBSQLStr2 += "VALUES ";
                                        tDBSQLStr2 += "(";
                                        tDBSQLStr2 += mPanelSerialNo.ToString() + ",";
                                        tDBSQLStr2 += ID.ToString() + ",";
                                        tDBSQLStr2 += ID.ToString() + ",";
                                        tDBSQLStr2 += "'" + PanelName + "',";
                                        tDBSQLStr2 += (i + 1).ToString() + ",";

                                        if (WIGReaderActive[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        if (WIGReaderParkingGate[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        tDBSQLStr2 += WIGReaderRelayNo[i].ToString() + ",";
                                        tDBSQLStr2 += "'" + WIGReaderName[i] + "',";
                                        tDBSQLStr2 += WIGReaderDoorType[i].ToString() + ",";
                                        tDBSQLStr2 += WIGReaderWIGType[i].ToString() + ",";
                                        tDBSQLStr2 += WIGReaderLocalGroup[i].ToString() + ",";
                                        tDBSQLStr2 += WIGReaderAccessControlMode[i].ToString() + ",";
                                        tDBSQLStr2 += WIGReaderAlarmLock[i] + ",";
                                        tDBSQLStr2 += WIGReaderFireUnlock[i] + ",";
                                        tDBSQLStr2 += WIGReaderPINVerify[i] + ",";
                                        if (WIGReaderButtonDetectorFunction[i] == 1)//Ana Alarm Rolesi Yerine
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        if (WIGReaderSeqAccessMain[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        if (WIGReaderMultipleAuthorization[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        tDBSQLStr2 += WIGReaderDoorOpenTime[i] + ",";
                                        if (WIGReaderDoorOpenTimeAlarm[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        if (WIGReaderDoorForcingAlarm[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        if (WIGReaderDoorOpenAlarm[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        tDBSQLStr2 += WIGReaderDoorExternalAlarmRelay[i].ToString() + ",";
                                        if (WIGReaderDoorPanicButtonAlarm[i] == 1)
                                        {
                                            tDBSQLStr2 += "1,";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 += "0,";
                                        }
                                        tDBSQLStr2 += WIGReaderDoorPushDelay[i].ToString() + ",";
                                        tDBSQLStr2 += WIGReaderUserCount[i].ToString() + ",";











                                        tDBSQLStr2 = tDBSQLStr2.Substring(0, tDBSQLStr2.Length - 1);
                                        tDBSQLStr2 += ")";
                                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                        TRetInt = tDBCmd2.ExecuteNonQuery();
                                        if (TRetInt > 0)
                                        {
                                            Result = true;
                                            tVeriable = new StringBuilder();
                                            tDBSQLStr2 = "";
                                        }
                                        else
                                        {
                                            Result = false;
                                            tVeriable = new StringBuilder();
                                            tDBSQLStr2 = "";
                                        }

                                    }
                                    else
                                    {
                                        tVeriable.Append(" UPDATE ReaderSettingsNew ");
                                        tVeriable.Append("SET ");
                                        tVeriable.Append("[WKapi Aktif] = " + WIGReaderActive[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Adi] = '" + WIGReaderName[i] + "',");
                                        tVeriable.Append("[WKapi Kapi Tipi] = " + WIGReaderDoorType[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Role No] = " + WIGReaderRelayNo[i].ToString() + ",");
                                        tVeriable.Append("[WKapi WIGType] = " + WIGReaderWIGType[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Lokal Bolge] = " + WIGReaderLocalGroup[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Gecis Modu] = " + WIGReaderAccessControlMode[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Sirali Gecis Ana Kapi] = " + WIGReaderSeqAccessMain[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Coklu Onay] = " + WIGReaderMultipleAuthorization[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Alarm Modu] = " + WIGReaderAlarmLock[i] + ",");
                                        tVeriable.Append("[WKapi Yangin Modu] = " + WIGReaderFireUnlock[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Pin Dogrulama] = " + WIGReaderPINVerify[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Lift Aktif] = " + WIGReaderParkingGate[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Acik Sure] = " + WIGReaderDoorOpenTime[i] + ",");
                                        tVeriable.Append("[WKapi Acik Sure Alarmi] = " + WIGReaderDoorOpenTimeAlarm[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Zorlama Alarmi] = " + WIGReaderDoorForcingAlarm[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Acilma Alarmi] = " + WIGReaderDoorOpenAlarm[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Panik Buton Alarmi] = " + WIGReaderDoorPanicButtonAlarm[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Harici Alarm Rolesi] = " + WIGReaderDoorExternalAlarmRelay[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Ana Alarm Rolesi] = " + WIGReaderButtonDetectorFunction[i].ToString() + ",");
                                        tVeriable.Append("[WKapi Itme Gecikmesi] = " + WIGReaderDoorPushDelay[i].ToString() + ",");
                                        tVeriable.Append("[WKapi User Count] = " + WIGReaderUserCount[i].ToString() + ",");
                                        tDBSQLStr2 = tVeriable.ToString().Substring(0, tVeriable.Length - 1);
                                        tDBSQLStr2 += " WHERE [Seri No] = " + mPanelSerialNo.ToString();
                                        tDBSQLStr2 += " AND [Panel ID] = " + ID.ToString();
                                        tDBSQLStr2 += " AND [WKapi ID] = " + (i + 1).ToString();
                                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                        TRetInt = tDBCmd2.ExecuteNonQuery();
                                        if (TRetInt > 0)
                                        {
                                            Result = true;
                                            tVeriable = new StringBuilder();
                                            tDBSQLStr2 = "";
                                        }
                                        else
                                        {
                                            Result = false;
                                            tVeriable = new StringBuilder();
                                            tDBSQLStr2 = "";
                                        }
                                    }
                                }
                                if (Result)
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
                    break;
                case CommandConstants.CMD_RCV_LOGSETTINGS:
                    {

                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                tDBSQLStr = "SELECT * FROM PanelSettings " +
                                    "WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                    "AND [Panel ID] = " + mPanelNo.ToString();
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                tDBReader = tDBCmd.ExecuteReader();
                                if (!tDBReader.Read())
                                {
                                    return false;
                                }
                                else
                                {
                                    tDBSQLStr2 = "UPDATE PanelSettings SET " +
                                   "[Offline Antipassback] = " + TmpReturnStr.Substring(10, 1) + "," +
                                   "[Offline Blocked Request] = " + TmpReturnStr.Substring(11, 1) + "," +
                                   "[Offline Undefined Transition] = " + TmpReturnStr.Substring(12, 1) + "," +
                                   "[Offline Manuel Operations] = " + TmpReturnStr.Substring(13, 1) + "," +
                                   "[Offline Button Triggering] = " + TmpReturnStr.Substring(14, 1) + "," +
                                   "[Offline Scheduled Transactions] = " + TmpReturnStr.Substring(15, 1);
                                    tDBSQLStr2 += " WHERE [Seri No] = " + mPanelSerialNo.ToString();
                                    tDBSQLStr2 += " AND [Panel ID] = " + mPanelNo.ToString();
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
                    break;
                case CommandConstants.CMD_RCV_LOCALINTERLOCK:
                    {
                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                tDBSQLStr = "SELECT * FROM PanelSettings" +
                                    " WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                    " AND [Panel ID] = " + mPanelNo.ToString();
                                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                tDBReader = tDBCmd.ExecuteReader();
                                if (tDBReader.Read())
                                {
                                    tDBSQLStr2 = "UPDATE PanelSettings SET" +
                                        "[LocalInterlock G1-1] = " + TmpReturnStr.Substring(10, 2) + "," +
                                        "[LocalInterlock G1-2] = " + TmpReturnStr.Substring(12, 2) + "," +
                                        "[LocalInterlock G2-1] = " + TmpReturnStr.Substring(14, 2) + "," +
                                        "[LocalInterlock G2-2] = " + TmpReturnStr.Substring(16, 2) + "," +
                                        "[LocalInterlock G3-1] = " + TmpReturnStr.Substring(18, 2) + "," +
                                        "[LocalInterlock G3-2] = " + TmpReturnStr.Substring(20, 2) + "," +
                                        "[LocalInterlock G4-1] = " + TmpReturnStr.Substring(22, 2) + "," +
                                        "[LocalInterlock G4-2] = " + TmpReturnStr.Substring(24, 2);
                                    tDBSQLStr2 += " WHERE [Seri No] = " + mPanelSerialNo.ToString();
                                    tDBSQLStr2 += " AND [Panel ID] = " + mPanelNo.ToString();
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
                    break;
                default:
                    break;
            }



            return false;
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
                case (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP:
                    return "ES";
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
                default:
                    return 0;
            }
        }

        //TODO:Ekrandaki Label Text'lerini Güncelleme
        delegate void TextDegisDelegate(string TMsg);
        public void SyncUpdateScreen(string TMsg)
        {
            Thread.Sleep(20);
            object frmMainLock = new object();
            lock (frmMainLock)
            {


                if (mParentForm.lblMsj[mMemIX].InvokeRequired == true)
                {
                    TextDegisDelegate del = new TextDegisDelegate(SyncUpdateScreen);
                    mParentForm.Invoke(del, new object[] { TMsg });

                }
                else
                {
                    if (TMsg != mParentForm.lblMsj[mMemIX].Text)
                    {
                        mParentForm.lblMsj[mMemIX].Text = TMsg;

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

        public string ConvertToTypeInt(int reader, string Type)
        {
            if (reader != -1)
            {
                return reader.ToString(Type);
            }
            else
            {
                return "";
            }
        }

        public string ConvertToTypeDatetime(DateTime date, string Type)
        {
            if (date != null)
            {
                return date.Day.ToString(Type) + date.Month.ToString(Type) + date.Year.ToString(Type).Substring(2, 2);
            }
            return "";
        }

        public string ConvertToTypeTime(DateTime date, string Type)
        {
            if (date != null)
            {
                return date.Hour.ToString(Type) + date.Minute.ToString(Type);
            }
            return "";
        }

        public string ConvertToTypeTimeWithSecond(DateTime date, string Type)
        {
            if (date != null)
            {
                return date.Hour.ToString(Type) + date.Minute.ToString(Type) + date.Second.ToString(Type);
            }
            return "";
        }

        public bool IsNumeric(string str)
        {
            double myNum = 0;
            if (double.TryParse(str, out myNum))
            {
                return true;
            }
            return false;
        }

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

        /***************************************************************************/

        public bool ClearSocketBuffers(TcpClient TClient/*, TcpClient TClientLog*/)
        {
            byte[] DummyBuffer;
            byte[] DummyBufferLog;
            StringBuilder sBuilder = new StringBuilder();
            //string TRcvData=null;
            //string TRcvDataTr = null;
            int TSize;
            int TSizeLog;
            try
            {
                if (TClient.Available > 0 /*&& TClientLog.Available > 0*/)
                {
                    var netStream = TClient.GetStream();
                    //var netStreamLog = TClientLog.GetStream();
                    if (netStream.CanRead/* && netStreamLog.CanRead*/)
                    {
                        TSize = TClient.Available;
                        DummyBuffer = new byte[TSize];
                        netStream.Read(DummyBuffer, 0, TSize);
                        //TSizeLog = TClientLog.Available;
                        //DummyBufferLog = new byte[TSizeLog];
                        //netStreamLog.Read(DummyBufferLog, 0, TSizeLog);
                    }
                    else
                    {
                        return true;
                    }
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

        public void DeleteTaskFromTaskList()
        {
            object TLockObj = new object();
            lock (TLockObj)
            {
                TaskList[mMemIX, TaskPIX[mMemIX]].CmdID = 0;
                TaskList[mMemIX, TaskPIX[mMemIX]].CmdNum = 0;
                TaskList[mMemIX, TaskPIX[mMemIX]].PortNo = 0;
                TaskList[mMemIX, TaskPIX[mMemIX]].RODASOFTClientID = 0;
                TaskList[mMemIX, TaskPIX[mMemIX]].SenderClient = null;
            }

        }//Görev Listesinden Silme İşlemi Yapıyor

        public void SendTestCommand(TcpClient TClient)
        {
            StringBuilder TSndStr = new StringBuilder();
            byte[] TSndBytes = new byte[1024];
            TSndStr.Append("%UR");
            TSndStr.Append(mPanelSerialNo.ToString("X4"));
            TSndStr.Append(mPanelNo.ToString("D3"));
            TSndStr.Append("**" + "\r");

            try
            {

                var netStream = TClient.GetStream();
                if (netStream.CanWrite)
                {
                    TSndBytes = Encoding.ASCII.GetBytes(TSndStr.ToString());
                    netStream.Write(TSndBytes, 0, TSndBytes.Length);
                }
                else
                {
                    // SyncUpdateScreen("Panele veri gönderme işlemi başarısız");
                }
            }
            catch (Exception)
            {
                // SyncUpdateScreen("Panele veri gönderme işlemi başarısız");
            }

        }//Panel'e Test Komutu Gönderiyor

        public bool ReceiveTestCommand(TcpClient TClient)
        {
            ushort TSize = 1024;
            byte[] RcvBuffer = new byte[TSize];
            string TRcvData = null;

            try
            {
                if (TClient.Available > 0)
                {
                    TClient.GetStream().Read(RcvBuffer, 0, RcvBuffer.Length);
                    TRcvData = Encoding.ASCII.GetString(RcvBuffer);
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
        }//Panel'den Test Komutunun Sonucunu Alıyor

        public void SendIPTaskCommand(TcpClient TClient, int TmpIntParam1, int TmpIntParam2, int TmpIntParam3, ushort TmpTaskType)
        {

            StringBuilder TSndStr = new StringBuilder();

            byte[] TSndBytes = new byte[1024];
            TSndStr.Append("%" + GetCommandPrefix(TmpTaskType));
            TSndStr.Append(mPanelSerialNo.ToString("X4"));
            TSndStr.Append(mPanelNo.ToString("D3"));
            TSndStr.Append("**" + "\r");

            try
            {

                var netStream = TClient.GetStream();
                if (netStream.CanWrite)
                {
                    TSndBytes = Encoding.ASCII.GetBytes(TSndStr.ToString());
                    netStream.Write(TSndBytes, 0, TSndBytes.Length);

                }
                else
                {
                    // SyncUpdateScreen("Panele veri gönderme işlemi başarısız");
                }
            }
            catch (Exception)
            {
                // SyncUpdateScreen("Panele veri gönderme işlemi başarısız");
            }

        }

        public bool ReceiveIPTaskCommand(TcpClient TClient, ref string TReturnStr, ushort TmpTaskType)
        {
            ushort TSize = 1024;
            byte[] RcvBuffer = new byte[TSize];
            string TRcvData = null;
            int TPos;
            try
            {
                if (TClient.Available > 0)
                {
                    TClient.GetStream().Read(RcvBuffer, 0, RcvBuffer.Length);
                    TRcvData = Encoding.ASCII.GetString(RcvBuffer);
                }
                else
                {
                    return false;
                }

                //TPos = TRcvData.IndexOf("$" & GetCommandPrefix(TmpTaskType));
                TPos = TRcvData.IndexOf("$UR");

                if (TPos > -1)
                {
                    TReturnStr = TRcvData;
                    return true;
                }
                else
                    return false;
            }
            catch (Exception)
            {
                return false;
            }

        }

        public bool TransferAnswer(ushort TRetCode)
        {
            byte[] TSndBytes;
            object TLockObj = new object();

            lock (TLockObj)
            {
                mSAnswer.Size = (int)SizeConstants.SIZE_ANSWER_DATA;
                mSAnswer.RetCode = TRetCode;
                mSAnswer.CmdNum = TaskList[mMemIX, TaskPIX[mMemIX]].CmdNum;
                TSndBytes = new byte[mSAnswer.Size];
                try
                {
                    var netStream = TaskList[mMemIX, TaskPIX[mMemIX]].SenderClient.GetStream();
                    if (netStream.CanWrite)
                    {
                        netStream.Write(TSndBytes, 0, TSndBytes.Length);
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






    }
}
