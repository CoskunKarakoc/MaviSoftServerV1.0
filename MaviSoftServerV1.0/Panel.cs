using System;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
namespace MaviSoftServerV1._0
{
    public class Panel
    {
        private const ushort NO_TASK = 0;

        private const ushort DB_TASK = 1;

        private const ushort IP_TASK = 2;

        private static int[] TaskPIX = new int[(int)TCONST.MAX_PANEL];

        private S_TASKLIST[,] TaskList = new S_TASKLIST[(int)TCONST.MAX_PANEL, (int)TCONST.MAX_TASK_CNT];

        private int mTaskNo;

        public int mTempTaskNo;

        private int mTaskType;

        public int mTempTaskType;

        private int mTaskIntParam1;

        public int mTempTaskIntParam1;

        private int mTaskIntParam2;

        public int mTempTaskIntParam2;

        private int mTaskIntParam3;

        public int mTempTaskIntParam3;

        private int mTaskIntParam4;

        public int mTempTaskIntParam4;

        private int mTaskIntParam5;

        public int mTempTaskIntParam5;

        private string mTaskStrParam1;

        public string mTempTaskStrParam1;

        private string mTaskStrParam2;

        public string mTempTaskStrParam2;

        private string mTaskUserName;

        public string mTempTaskUserName;

        private bool mTaskUpdateTable;

        public bool mTempTaskUpdateTable;

        private ushort mTaskSource;

        public ushort mTempTaskSource;

        private S_ANSWER mSAnswer;

        private bool mTransferCompleted { get; set; }

        private bool mLogTransferCompleted { get; set; }

        private ushort mReadStep { get; set; }

        private ushort mRetryCnt { get; set; }

        private const ushort RETRY_COUNT = 2;

        private string mReturnStr;

        public FrmMain mParentForm { get; set; }

        public Label lblMesaj;

        public Thread PanelThread { get; set; }

        public Thread LogThread { get; set; }

        public TcpClient mPanelClient { get; set; }

        public TcpClient mPanelClientLog { get; set; }

        public TcpListener mPanelListener { get; set; }

        private ushort mPanelIdleInterval { get; set; }

        public CommandConstants mPanelProc { get; set; }

        private CommandConstants mLogProc { get; set; }

        private ushort mPanelConState { get; set; }

        private int mPanelTCPPort { get; set; }

        private int mPanelTCPPortLog { get; set; }

        private string mPanelIPAddress { get; set; }

        private int mPanelSerialNo { get; set; }

        private DateTime mStartTime { get; set; }

        private DateTime mEndTime { get; set; }

        private ushort mActive { get; set; }

        private ushort mMemIX { get; set; }

        private string CurWinStr = " ";

        private string PreWinStr = "!";

        private string CurTaskWinStr = " ";

        private string PreTaskWinStr = "!";

        private ushort mTimeOut { get; set; }

        private ushort mTaskTimeOut { get; set; }

        private ushort mPortType { get; set; }

        private ushort mPanelAlarmIX { get; set; }

        public int mPanelNo { get; set; }

        public int mPanelModel { get; set; }

        private string mPanelName { get; set; }

        private bool mInTime { get; set; }

        private int mConnectTimeout { get; set; }

        public SqlConnection mDBConn { get; set; }

        private string mDBSQLStr { get; set; }

        private SqlDataReader mDBReader { get; set; }

        private SqlCommand mDBCmd { get; set; }

        public string TSndStr = "";

        int mMailRetryCount = 0;

        StringBuilder panelAyarKodu = new StringBuilder();
        StringBuilder kapiIlk8 = new StringBuilder();
        StringBuilder kapiSon8 = new StringBuilder();

        public Panel(ushort MemIX, ushort TActive, int TPanelNo, ushort JTimeOut, string TIPAdress, int TMACAdress, int TCPPortOne, int TCPPortTwo, int TPanelModel, string PanelName, FrmMain parentForm)
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
            mPanelModel = TPanelModel;
            mPanelName = PanelName;
            mTaskTimeOut = 3;
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
                PanelThread = new Thread(ProcessPanel);
                PanelThread.Priority = ThreadPriority.Normal;
                PanelThread.IsBackground = true;
                PanelThread.Start();
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
                mPanelClient.Close();
                mPanelClient.Dispose();
                PanelThread.Abort();
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
                Thread.Sleep(50);

                if (mActive == 0)
                    mPanelProc = CommandConstants.CMD_PORT_DISABLED;

                switch (mPanelProc)
                {
                    case CommandConstants.CMD_PORT_DISABLED:
                        {
                            CurWinStr = "IPTAL";
                            if (CurWinStr != PreWinStr)
                            {
                                SyncUpdateScreen(CurWinStr, System.Drawing.Color.Red);
                                PreWinStr = CurWinStr;
                            }
                            mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                        }
                        break;
                    case CommandConstants.CMD_PORT_INIT:
                        {
                            CurWinStr = "AYARLANIYOR";
                            if (CurWinStr != PreWinStr)
                            {
                                SyncUpdateScreen(CurWinStr, System.Drawing.Color.SkyBlue);
                                PreWinStr = CurWinStr;
                            }
                            mPanelClient = new TcpClient();
                            mPanelClient.ReceiveBufferSize = 65536;
                            mPanelClient.SendBufferSize = 65536;
                            mPanelClient.ReceiveTimeout = mTimeOut;
                            mPanelClient.SendTimeout = mTimeOut;

                            try
                            {
                                mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                mPanelProc = CommandConstants.CMD_PORT_CONNECT;

                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTimeOut);

                            }
                            catch (Exception)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                            }

                        }
                        break;
                    case CommandConstants.CMD_PORT_CONNECT:
                        {
                            CurWinStr = "BAGLANIYOR";
                            if (CurWinStr != PreWinStr)
                            {
                                SyncUpdateScreen(CurWinStr, System.Drawing.Color.Yellow);
                                PreWinStr = CurWinStr;
                            }
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
                            CurWinStr = "KAPATILIYOR";
                            if (CurWinStr != PreWinStr)
                            {
                                SyncUpdateScreen(CurWinStr, System.Drawing.Color.Yellow);
                                PreWinStr = CurWinStr;
                            }
                            //if (mMailRetryCount == 0)
                            //{
                            //    SendMail("Panel Bağlantısı Yok! ", "<b>" + mPanelNo + " <i>Nolu "+mPanelName+" İsimli Panel İle Bağlantı Sağlanamıyor.</i></b>", true);
                            //    SendSms sendSms = new SendSms(new SmsSettings());
                            //    sendSms.PanelBaglantiDurumu(mPanelNo + " Nolu "+mPanelName+" İsimli Panel İle Bağlantı Sağlanamıyor.");
                            //    mMailRetryCount++;
                            //}

                            if (mPanelClient.Connected == true)
                            {
                                mPanelClient.Close();
                            }
                            mPanelProc = CommandConstants.CMD_PORT_INIT;
                            Thread.Sleep(500);

                        }
                        break;
                    case CommandConstants.CMD_PORT_TEST:
                        {
                            CurWinStr = "PORT TEST";
                            if (CurWinStr != PreWinStr)
                            {
                                SyncUpdateScreen(CurWinStr, System.Drawing.Color.RoyalBlue);
                                PreWinStr = CurWinStr;
                            }
                            mRetryCnt = 0;
                            mTransferCompleted = false;

                            while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false))
                            {

                                ClearSocketBuffers(mPanelClient, null);

                                SendTestCommand(mPanelClient);

                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTimeOut);
                                do
                                {
                                    Thread.Sleep(20);
                                    mStartTime = DateTime.Now;
                                } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                if (mStartTime >= mEndTime)
                                {
                                    SyncUpdateScreen("ZAMAN AŞIMI", System.Drawing.Color.Red);
                                }
                                else
                                {
                                    if (!ReceiveTestCommand(mPanelClient))
                                        break;
                                    else
                                        mTransferCompleted = true;
                                }

                                mRetryCnt++;
                            }
                            if (mTransferCompleted == true)
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            else
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;

                        }
                        break;
                    case CommandConstants.CMD_TASK_LIST:
                        {
                            mMailRetryCount = 0;//Panel Bağlantısı Kesildiğinde Mail Atma Sayısı Kontrolü
                            if (mPanelClient.Connected == false && mPanelClient.LingerState.Enabled == false)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }
                            ClearSocketBuffers(mPanelClient, null);

                            Thread.Sleep(50);//CHANGE:250'den 50 ye düşürüldü test amaçlı 03032020 tarihinde
                            mTaskSource = mTempTaskSource;
                            mTaskNo = mTempTaskNo;
                            mTaskType = mTempTaskType;
                            mTaskIntParam1 = mTempTaskIntParam1;
                            mTaskIntParam2 = mTempTaskIntParam2;
                            mTaskIntParam3 = mTempTaskIntParam3;
                            mTaskIntParam4 = mTempTaskIntParam4;
                            mTaskIntParam5 = mTempTaskIntParam5;
                            mTaskStrParam1 = mTempTaskStrParam1;
                            mTaskStrParam2 = mTempTaskStrParam2;
                            mTaskUserName = mTempTaskUserName;
                            mTaskUpdateTable = mTempTaskUpdateTable;
                            if (mTaskSource == IP_TASK)
                            {
                                TProc = TaskList[mMemIX, TaskPIX[mMemIX]].CmdID;
                                mPanelProc = (CommandConstants)TProc;
                            }
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
                                PreWinStr = ".!";
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                                CurWinStr = "HAZIR";
                                if (CurWinStr != PreWinStr)
                                {
                                    SyncUpdateScreen(CurWinStr, System.Drawing.Color.Green);
                                    PreWinStr = CurWinStr;
                                }
                            }
                            if (mPanelProc == CommandConstants.CMD_RCV_LOGS)
                            {
                                SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, mTaskStrParam1, (ushort)mTaskType);
                            }

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
                    case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    case CommandConstants.CMD_RCV_LOCALINTERLOCK:
                    case CommandConstants.CMD_RCV_GENERALSETTINGS_1:
                    case CommandConstants.CMD_RCV_GENERALSETTINGS_2:
                    case CommandConstants.CMD_RCV_GENERALSETTINGS_3:
                        {
                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            CurTaskWinStr = GetScreenMessage((CommandConstants)mTaskType);
                            if (CurTaskWinStr != PreTaskWinStr)
                            {
                                SyncUpdateScreen(CurTaskWinStr, System.Drawing.Color.Blue);
                                PreTaskWinStr = CurTaskWinStr;
                            }

                            mRetryCnt = 0;
                            mTransferCompleted = false;

                            while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false))
                            {
                                ClearSocketBuffers(mPanelClient, null);

                                if (!SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, mTaskStrParam1, (ushort)mTaskType))
                                {
                                    mTransferCompleted = false;
                                    break;
                                }

                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTaskTimeOut);
                                do
                                {
                                    Thread.Sleep(20);
                                    mStartTime = DateTime.Now;
                                    if (!mPanelClient.Client.Connected)
                                    {
                                        try
                                        {
                                            mPanelClient.Close();
                                            mPanelClient = new TcpClient();
                                            mPanelClient.ReceiveBufferSize = 65536;
                                            mPanelClient.SendBufferSize = 65536;
                                            mPanelClient.ReceiveTimeout = mTimeOut;
                                            mPanelClient.SendTimeout = mTimeOut;
                                            mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                if (mStartTime >= mEndTime)
                                {
                                    SyncUpdateScreen("ZAMAN AŞIMI", System.Drawing.Color.Red);
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

                                mRetryCnt++;
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

                                    if (mPanelProc == CommandConstants.CMD_RCV_LOGS)
                                        break;

                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED, mPanelProc);
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

                                    if (mPanelProc == CommandConstants.CMD_RCV_LOGS)
                                        break;

                                    // DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_TIMOUT, mPanelProc);

                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            mTaskTimeOut = 3;
                        }
                        break;
                    case CommandConstants.CMD_SND_RTC:
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
                    case CommandConstants.CMD_ERSALL_TIMEGROUP:
                    case CommandConstants.CMD_SND_GENERALSETTINGS_1:
                    case CommandConstants.CMD_SND_GENERALSETTINGS_2:
                    case CommandConstants.CMD_SND_GENERALSETTINGS_3:
                        {
                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }
                            CurTaskWinStr = GetScreenMessage((CommandConstants)mTaskType);
                            if (CurTaskWinStr != PreTaskWinStr)
                            {
                                SyncUpdateScreen(CurTaskWinStr, System.Drawing.Color.Blue);
                                PreTaskWinStr = CurTaskWinStr;
                            }
                            mRetryCnt = 0;
                            mTransferCompleted = false;
                            while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false))
                            {

                                ClearSocketBuffers(mPanelClient, null);
                                if (!SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, mTaskStrParam1, (ushort)mTaskType))
                                {
                                    mTransferCompleted = false;
                                    break;
                                }

                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTaskTimeOut);
                                do
                                {
                                    Thread.Sleep(20);
                                    mStartTime = DateTime.Now;

                                    if (!mPanelClient.Client.Connected)
                                    {
                                        try
                                        {
                                            mPanelClient.Close();
                                            mPanelClient = new TcpClient();
                                            mPanelClient.ReceiveBufferSize = 65536;
                                            mPanelClient.SendBufferSize = 65536;
                                            mPanelClient.ReceiveTimeout = mTimeOut;
                                            mPanelClient.SendTimeout = mTimeOut;
                                            mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                        }
                                        catch
                                        {
                                            SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_TIMOUT, mPanelProc);
                                        }
                                    }

                                } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                if (mStartTime >= mEndTime)
                                {

                                    //Display Timeout&Retrying Message
                                    SyncUpdateScreen("ZAMAN AŞIMI", System.Drawing.Color.Red);
                                }
                                else
                                {
                                    if (ReceiveGenericAnswerData(mPanelClient, (CommandConstants)mTaskType) == false)
                                    {
                                        mRetryCnt++;//break;
                                    }
                                    else
                                    {
                                        mTransferCompleted = true;
                                        break;
                                    }
                                }
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
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED, mPanelProc);
                                }

                                if ((CommandConstants)mTaskType == CommandConstants.CMD_SND_GENERALSETTINGS || (CommandConstants)mTaskType == CommandConstants.CMD_SND_GENERALSETTINGS_1)
                                    mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                else
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
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_TIMOUT, mPanelProc);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            mTaskTimeOut = 3;
                        }
                        break;
                    case CommandConstants.CMD_RCV_RTC:
                        {

                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            CurTaskWinStr = GetScreenMessage((CommandConstants)mTaskType);
                            if (CurTaskWinStr != PreTaskWinStr)
                            {
                                SyncUpdateScreen(CurTaskWinStr, System.Drawing.Color.Blue);
                                PreTaskWinStr = CurTaskWinStr;
                            }


                            mRetryCnt = 0;
                            mTransferCompleted = false;

                            while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false))
                            {
                                ClearSocketBuffers(mPanelClient, null);
                                SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, mTaskStrParam1, (ushort)mTaskType);
                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTaskTimeOut);
                                do
                                {
                                    Thread.Sleep(20);
                                    mStartTime = DateTime.Now;
                                    if (!mPanelClient.Client.Connected)
                                    {
                                        try
                                        {
                                            mPanelClient.Close();
                                            mPanelClient = new TcpClient();
                                            mPanelClient.ReceiveBufferSize = 65536;
                                            mPanelClient.SendBufferSize = 65536;
                                            mPanelClient.ReceiveTimeout = mTimeOut;
                                            mPanelClient.SendTimeout = mTimeOut;
                                            mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                if (mStartTime >= mEndTime)
                                {
                                    SyncUpdateScreen("ZAMAN AŞIMI", System.Drawing.Color.Red);
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

                                mRetryCnt++;
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

                                    if (mPanelProc == CommandConstants.CMD_RCV_LOGS)
                                        break;

                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED, mPanelProc);
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

                                    if (mPanelProc == CommandConstants.CMD_RCV_LOGS)
                                        break;

                                    //DB Task
                                    SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_ERROR, mPanelProc);
                                }
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            mTaskTimeOut = 3;
                        }
                        break;
                    case CommandConstants.CMD_ZERO:
                        {
                            DeleteTaskFromTaskList();
                            mPanelProc = CommandConstants.CMD_TASK_LIST;
                        }
                        break;
                    case CommandConstants.CMD_SND_GLOBALDATAUPDATE:
                        {
                            if (SendGenericDBData(mPanelClient, mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, mTaskStrParam1, (ushort)mTaskType))
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                                mTaskSource = 0;
                                mTaskType = 0;
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                                mTaskSource = 0;
                                mTaskType = 0;
                            }


                        }
                        break;
                    case CommandConstants.CMD_RCV_LOGS:
                        {
                            if (!mPanelClient.Client.Connected)
                            {
                                mPanelProc = CommandConstants.CMD_PORT_CLOSE;
                                break;
                            }

                            CurTaskWinStr = GetScreenMessage((CommandConstants)mTaskType);
                            if (CurTaskWinStr != PreTaskWinStr)
                            {
                                SyncUpdateScreen(CurTaskWinStr, System.Drawing.Color.Blue);
                                PreTaskWinStr = CurTaskWinStr;
                            }
                            mRetryCnt = 0;
                            mTransferCompleted = false;

                            while ((mRetryCnt < RETRY_COUNT) && (mTransferCompleted == false))
                            {
                                mStartTime = DateTime.Now;
                                mEndTime = mStartTime.AddSeconds(mTaskTimeOut);
                                do
                                {
                                    Thread.Sleep(20);
                                    mStartTime = DateTime.Now;
                                    if (!mPanelClient.Client.Connected)
                                    {
                                        try
                                        {
                                            mPanelClient.Close();
                                            mPanelClient = new TcpClient();
                                            mPanelClient.ReceiveBufferSize = 65536;
                                            mPanelClient.SendBufferSize = 65536;
                                            mPanelClient.ReceiveTimeout = mTimeOut;
                                            mPanelClient.SendTimeout = mTimeOut;
                                            mPanelClient.Connect(mPanelIPAddress, mPanelTCPPort);
                                        }
                                        catch
                                        {

                                        }
                                    }
                                } while ((mStartTime < mEndTime) && (CheckSize(mPanelClient, (int)GetAnswerSize((CommandConstants)mTaskType)) == false));

                                if (mStartTime >= mEndTime)
                                {
                                    //Display Timeout&Retrying Message
                                    SyncUpdateScreen("ZAMAN AŞIMI", System.Drawing.Color.Red);
                                }
                                else
                                {
                                    if (GAReciveGenericDBData(mPanelClient, ref mReturnStr, (CommandConstants)mTaskType))
                                    {
                                        if (!ProcessReceivedData(mTaskIntParam1, mTaskIntParam2, mTaskIntParam3, (CommandConstants)mTaskType, mTaskSource, mTaskUpdateTable, mReturnStr))
                                        {
                                            break;
                                        }
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }

                                mRetryCnt++;
                            }
                            mTaskTimeOut = 3;
                        }
                        break;
                    default:
                        {
                            if (NoTaskDelete(mTaskNo))
                            {
                                mPanelProc = CommandConstants.CMD_TASK_LIST;
                            }
                            else
                            {
                                mPanelProc = CommandConstants.CMD_PORT_DISABLED;
                            }
                            mTaskTimeOut = 3;
                            break;
                        }
                }

            }

        }


        /// <summary>
        /// Veritabanından Gelen Görevi Panele Gönderme
        /// </summary>
        /// <param name="TClient"></param>
        /// <param name="TmpIntParam1"></param>
        /// <param name="TmpIntParam2"></param>
        /// <param name="TmpIntParam3"></param>
        /// <param name="TmpStrParam1"></param>
        /// <param name="TmpTaskType"></param>
        /// <returns></returns>
        public bool SendGenericDBData(TcpClient TClient, int TmpIntParam1, int TmpIntParam2, int TmpIntParam3, string TmpStrParam1, ushort TmpTaskType)
        {
            byte[] TSndBytes;
            if (TmpTaskType != (ushort)CommandConstants.CMD_SND_GLOBALDATAUPDATE)
            {
                TSndStr = BuiltDBCommandString(TmpIntParam1, TmpIntParam2, TmpIntParam3, TmpStrParam1, TmpTaskType).ToString();
            }
            try
            {
                var netStream = TClient.GetStream();
                if (netStream.CanWrite && !TSndStr.Contains("ERR"))
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

        /// <summary>
        /// Gelen Görev Tipine Göre Panel'e Gönderilecek Olan Komut Oluşturma 
        /// </summary>
        /// <param name="DBIntParam1"></param>
        /// <param name="DBIntParam2"></param>
        /// <param name="DBIntParam3"></param>
        /// <param name="DBStrParam1"></param>
        /// <param name="DBTaskType"></param>
        /// <returns></returns>
        public string BuiltDBCommandString(int DBIntParam1, int DBIntParam2, int DBIntParam3, string DBStrParam1, ushort DBTaskType)
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
                mTaskTimeOut = 3;
            }
            /*3*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GENERALSETTINGS)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                tDBSQLStr2 = "SELECT * FROM ReaderSettingsNewMS " +
                                    "WHERE [Panel ID] = " + DBIntParam1.ToString();
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                tDBReader2 = tDBCmd2.ExecuteReader();
                                if (tDBReader2.Read())
                                {
                                    if ((tDBReader["Panel M1 Role"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("01");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel M1 Role"] as int? ?? default(int), "D2"));//PS
                                    }
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Tipi"] as int? ?? default(int), "D1"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["New Device ID"] as int? ?? default(int), "D3"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Kontrol Modu"] as int? ?? default(int), "D1"));//RS
                                    if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 0)//PS
                                        TSndStr.Append("0");// StanAlone
                                    else if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 1)
                                        TSndStr.Append("1");//Online
                                    else
                                        TSndStr.Append("2");

                                    TSndStr.Append("0000");

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Gecis Modu"] as int? ?? default(int), "D1"));//RS

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block"] as int? ?? default(int), "D2"));//PS

                                    if ((tDBReader2["RS485 Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }
                                    string LCDMessage = tDBReader2["LCD Row Message"].ToString();//RS
                                    if (LCDMessage.Length > 16)
                                    {
                                        LCDMessage = LCDMessage.Substring(0, 16);
                                    }
                                    else if (LCDMessage.Length < 16)
                                    {
                                        int count = (16 - LCDMessage.Length);
                                        for (int i = 0; i < count; i++)
                                        {
                                            LCDMessage += " ";
                                        }
                                    }

                                    TSndStr.Append(LCDMessage);

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel GW" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Subnet" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel TCP Port"] as int? ?? default(int), "D5"));//PS

                                    if ((tDBReader2["WKapi Keypad Status"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("00001234");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int), "D8"));
                                    }

                                    if ((tDBReader2["RS485 Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Mifare Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["Mifare Kart Data Type"] as int? ?? default(int), "D1"));//RS

                                    if ((tDBReader2["UDP Haberlesme"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Multiple Clock Mode Counter Usage"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Pass Counter Auto Delete Cancel"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader["Panel Same Tag Block HourMinSec"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Access Counter Kontrol"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    for (int i = 1; i < 5; i++)//RS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Remote IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(5700, "D5"));//Remote TCP Port

                                    if ((tDBReader2["Kart ID 32 Bit Clear"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }


                                    if ((tDBReader2["Turnstile Arm Tracking"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append("0");//Device Model


                                    TSndStr.Append("00000000");//Ekleme Kartı
                                    TSndStr.Append("00000000");//Silme Kartı Kartı

                                    TSndStr.Append("00000000000000");

                                    TSndStr.Append("**\r");
                                    mTaskTimeOut = 3;
                                }
                                else
                                {
                                    TSndStr.Clear();
                                    TSndStr.Append("ERR");
                                }
                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
                else
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
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

                                if ((tDBReader["DHCP Enabled"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                TSndStr.Append("00000");

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

                                // Panel Button Detector
                                if ((tDBReader["Panel Button Detector"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                //Panel Button Detector Time
                                if ((tDBReader["Panel Button Detector Time"] as int? ?? default(int)) <= 0 || (tDBReader["Panel Button Detector Time"] as int? ?? default(int)) > 9)
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Button Detector Time"] as int? ?? default(int), "D1"));


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
                                    TSndStr.Append(ConvertTurkceKarekter(tDBReader["Panel Name"].ToString()) + space);
                                }
                                else
                                {
                                    TSndStr.Append(ConvertTurkceKarekter(tDBReader["Panel Name"].ToString().Substring(0, 16)));
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

                                        if ((tDBReader2["WKapi Alarm Modu"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");

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
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString()) + space);
                                        }
                                        else
                                        {
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString().Substring(0, 15)));
                                        }

                                        //Buton detector mode
                                        if ((tDBReader2["WKapi Ana Alarm Rolesi"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");
                                    }
                                }
                                TSndStr.Append("**\r");
                                mTaskTimeOut = 3;

                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
            }
            /*4*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_TIMEGROUP)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
                    }
                }

            }
            /*Zaman Gruplarının Silinmesi*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_TIMEGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*5*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_TIMEGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("***\r");
                mTaskTimeOut = 3;
            }
            /*6*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_ACCESSGROUP)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
                        tDBSQLStr = "SELECT * FROM GroupsMaster WHERE [Grup No]=" + DBIntParam1;
                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                        tDBReader = tDBCmd.ExecuteReader();
                        if (tDBReader.Read())
                        {
                            TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                            TSndStr.Append(mPanelSerialNo.ToString("X4"));
                            TSndStr.Append(mPanelNo.ToString("D3"));
                            TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No"] as int? ?? default(int), "D4"));
                            for (int i = 1; i <= 8; i++)
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
                            for (int i = 1; i <= 16; i++)
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
                                TSndStr.Append("1");
                            }
                            else
                            {
                                TSndStr.Append("0");
                            }

                            if (tDBReader["Lokal Kapasite Gecersiz"] as bool? ?? default(bool))
                            {
                                TSndStr.Append("1");
                            }
                            else
                            {
                                TSndStr.Append("0");
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
                                TSndStr.Append("1");
                            }
                            else
                            {
                                TSndStr.Append("0");
                            }

                            for (int i = 1; i <= 8; i++)
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*Geçiş Gruplarının Silinmesi*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*8-9*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_USER || DBTaskType == (ushort)CommandConstants.CMD_SNDALL_USER)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    string tDBSQLStr3 = "";
                    SqlCommand tDBCmd3;
                    SqlDataReader tDBReader3 = null;
                    string tDBSQLStr4 = "";
                    SqlCommand tDBCmd4;
                    SqlDataReader tDBReader4 = null;
                    string tDBSQLStr5 = "";
                    SqlCommand tDBCmd5;
                    SqlDataReader tDBReader5 = null;
                    string PersonelAdiSoyadi = "";
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM Users WHERE ID = " + DBIntParam1;
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader["ID"] as int? ?? default(int), "D5"));
                                TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID"]), "D10"));
                                if (tDBReader["Sifre"].ToString() != null && tDBReader["Sifre"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Sifre"] as int? ?? default(int), "D8"));
                                }
                                else
                                {
                                    TSndStr.Append("00000001");
                                }
                                string Adi = tDBReader["Adi"].ToString();
                                if (Adi.Length < 7)
                                {
                                    int count = 7 - Adi.Length;
                                    for (int i = 0; i < count; i++)
                                    {
                                        Adi += " ";
                                    }
                                }
                                else
                                {
                                    Adi = Adi.Substring(0, 7);
                                }
                                string Soyadi = tDBReader["Soyadi"].ToString();
                                if (Soyadi.Length < 7)
                                {
                                    int count = 7 - Soyadi.Length;
                                    for (int i = 0; i < count; i++)
                                    {
                                        Soyadi += " ";
                                    }
                                }
                                else
                                {
                                    Soyadi = Soyadi.Substring(0, 7);
                                }


                                PersonelAdiSoyadi = Adi + " " + Soyadi;
                                PersonelAdiSoyadi = ConvertNameSurname(PersonelAdiSoyadi);
                                TSndStr.Append(PersonelAdiSoyadi);
                                if (tDBReader["Grup No 1"].ToString() != null)
                                {
                                    tDBSQLStr2 = "SELECT TOP 1 * FROM GroupsDetailNew WHERE [Grup No]=" + (tDBReader["Grup No 1"] as int? ?? default(int)) + " AND [Panel No]=" + mPanelNo.ToString() + " ORDER BY [Kapi No]";
                                    tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                    tDBReader2 = tDBCmd2.ExecuteReader();
                                    if (tDBReader2.Read())
                                    {
                                        if ((tDBReader2["Kapi Aktif"] as bool? ?? default(bool)) == true)
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");

                                        TSndStr.Append("1");//MS Ikinci Okuyu İzin Default 1
                                        TSndStr.Append("000");//Default Grup No

                                        if (tDBReader2["Kapi Zaman Grup No"].ToString() != null && tDBReader2["Kapi Zaman Grup No"].ToString() != "")
                                        {
                                            tDBSQLStr3 = "SELECT TOP 1 * FROM  TimeGroups WHERE [Zaman Grup No]=" + tDBReader2["Kapi Zaman Grup No"].ToString() + " ORDER BY [Zaman Grup No]";
                                            tDBCmd3 = new SqlCommand(tDBSQLStr3, mDBConn);
                                            tDBReader3 = tDBCmd3.ExecuteReader();
                                            if (tDBReader3.Read())
                                            {
                                                if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) >= 0 && (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) <= 8)
                                                {
                                                    TSndStr.Append(ConvertToTypeInt((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)), "D2"));
                                                }
                                                else
                                                {
                                                    TSndStr.Append("00");
                                                }
                                                if ((tDBReader3["Ek Saat"] as int? ?? default(int)) > 0 && (tDBReader3["Ek Saat"] as int? ?? default(int)) <= 8)
                                                {
                                                    TSndStr.Append(ConvertToTypeInt((tDBReader3["Ek Saat"] as int? ?? default(int)), "D1"));
                                                }
                                                else
                                                {
                                                    TSndStr.Append("0");
                                                }
                                                tDBSQLStr4 = "SELECT * FROM GroupsMaster WHERE [Grup No]=" + tDBReader["Grup No 1"].ToString();
                                                tDBCmd4 = new SqlCommand(tDBSQLStr4, mDBConn);
                                                tDBReader4 = tDBCmd4.ExecuteReader();
                                                if (tDBReader4.Read())
                                                {
                                                    if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 0 || (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 7)
                                                    {
                                                        TSndStr.Append("000");
                                                    }
                                                    else
                                                    {
                                                        if ((tDBReader4["Grup Gecis Sayisi"] as int? ?? default(int)) > -1)
                                                        {
                                                            TSndStr.Append(ConvertToTypeInt((tDBReader4["Grup Gecis Sayisi"] as int? ?? default(int)), "D3"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("000");
                                                        }
                                                    }
                                                    if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 0 || (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 7)
                                                    {
                                                        TSndStr.Append("000000000000");
                                                    }
                                                    else if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 1 || (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 2)
                                                    {
                                                        if (IsDate(tDBReader3["Baslangic Tarihi"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeDatetime(tDBReader3["Baslangic Tarihi"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("000000");
                                                        }
                                                        if (IsDate(tDBReader3["Bitis Tarihi"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeDatetime(tDBReader3["Bitis Tarihi"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("000000");
                                                        }
                                                    }
                                                    else if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 3 || (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 4)
                                                    {
                                                        if (IsDate(tDBReader3["Baslangic Saati"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeTimeWithSecond(tDBReader3["Baslangic Saati"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("000000");
                                                        }
                                                        if (IsDate(tDBReader3["Bitis Saati"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeTimeWithSecond(tDBReader3["Bitis Saati"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("000000");
                                                        }
                                                    }
                                                    else if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 5)
                                                    {
                                                        TDataInt = 0;
                                                        if ((bool)tDBReader3["Pazartesi"] == true)
                                                            TDataInt = 1;
                                                        if ((bool)tDBReader3["Sali"] == true)
                                                            TDataInt += 2;
                                                        if ((bool)tDBReader3["Carsamba"] == true)
                                                            TDataInt += 4;
                                                        if ((bool)tDBReader3["Persembe"] == true)
                                                            TDataInt += 8;
                                                        if ((bool)tDBReader3["Cuma"] == true)
                                                            TDataInt += 16;
                                                        if ((bool)tDBReader3["Cumartesi"] == true)
                                                            TDataInt += 32;
                                                        if ((bool)tDBReader3["Pazar"] == true)
                                                            TDataInt += 64;
                                                        TSndStr.Append(TDataInt.ToString("X2"));
                                                        TSndStr.Append("0000000000");
                                                    }
                                                    else if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 6)
                                                    {
                                                        //Monthly
                                                        TDataInt = 0;
                                                        if ((bool)tDBReader3["Gun1"] == true)
                                                            TDataInt = 1;
                                                        if ((bool)tDBReader3["Gun2"] == true)
                                                            TDataInt += 2;
                                                        if ((bool)tDBReader3["Gun3"] == true)
                                                            TDataInt += 4;
                                                        if ((bool)tDBReader3["Gun4"] == true)
                                                            TDataInt += 8;
                                                        if ((bool)tDBReader3["Gun5"] == true)
                                                            TDataInt += 16;
                                                        if ((bool)tDBReader3["Gun6"] == true)
                                                            TDataInt += 32;
                                                        if ((bool)tDBReader3["Gun7"] == true)
                                                            TDataInt += 64;
                                                        if ((bool)tDBReader3["Gun8"] == true)
                                                            TDataInt += 128;
                                                        TSndStr.Append(TDataInt.ToString("X2"));

                                                        TDataInt = 0;
                                                        if ((bool)tDBReader3["Gun9"] == true)
                                                            TDataInt = 1;
                                                        if ((bool)tDBReader3["Gun10"] == true)
                                                            TDataInt += 2;
                                                        if ((bool)tDBReader3["Gun11"] == true)
                                                            TDataInt += 4;
                                                        if ((bool)tDBReader3["Gun12"] == true)
                                                            TDataInt += 8;
                                                        if ((bool)tDBReader3["Gun13"] == true)
                                                            TDataInt += 16;
                                                        if ((bool)tDBReader3["Gun14"] == true)
                                                            TDataInt += 32;
                                                        if ((bool)tDBReader3["Gun15"] == true)
                                                            TDataInt += 64;
                                                        if ((bool)tDBReader3["Gun16"] == true)
                                                            TDataInt += 128;
                                                        TSndStr.Append(TDataInt.ToString("X2"));

                                                        TDataInt = 0;
                                                        if ((bool)tDBReader3["Gun17"] == true)
                                                            TDataInt = 1;
                                                        if ((bool)tDBReader3["Gun18"] == true)
                                                            TDataInt += 2;
                                                        if ((bool)tDBReader3["Gun19"] == true)
                                                            TDataInt += 4;
                                                        if ((bool)tDBReader3["Gun20"] == true)
                                                            TDataInt += 8;
                                                        if ((bool)tDBReader3["Gun21"] == true)
                                                            TDataInt += 16;
                                                        if ((bool)tDBReader3["Gun22"] == true)
                                                            TDataInt += 32;
                                                        if ((bool)tDBReader3["Gun23"] == true)
                                                            TDataInt += 64;
                                                        if ((bool)tDBReader3["Gun24"] == true)
                                                            TDataInt += 128;
                                                        TSndStr.Append(TDataInt.ToString("X2"));

                                                        TDataInt = 0;
                                                        if ((bool)tDBReader3["Gun25"] == true)
                                                            TDataInt = 1;
                                                        if ((bool)tDBReader3["Gun26"] == true)
                                                            TDataInt += 2;
                                                        if ((bool)tDBReader3["Gun27"] == true)
                                                            TDataInt += 4;
                                                        if ((bool)tDBReader3["Gun28"] == true)
                                                            TDataInt += 8;
                                                        if ((bool)tDBReader3["Gun29"] == true)
                                                            TDataInt += 16;
                                                        if ((bool)tDBReader3["Gun30"] == true)
                                                            TDataInt += 32;
                                                        if ((bool)tDBReader3["Gun31"] == true)
                                                            TDataInt += 64;
                                                        TSndStr.Append(TDataInt.ToString("X2"));
                                                        TSndStr.Append("0000");
                                                    }
                                                    else if ((tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 8 || (tDBReader3["Gecis Sinirlama Tipi"] as int? ?? default(int)) == 9)
                                                    {
                                                        if (IsDate(tDBReader3["Baslangic Saati 1"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeTime(tDBReader3["Baslangic Saati 1"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("0000");
                                                        }
                                                        if (IsDate(tDBReader3["Baslangic Saati 2"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeTime(tDBReader3["Baslangic Saati 2"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("0000");
                                                        }
                                                        if (IsDate(tDBReader3["Baslangic Saati 3"].ToString()) == true)
                                                        {
                                                            TSndStr.Append(ConvertToTypeTime(tDBReader3["Baslangic Saati 3"] as DateTime? ?? default(DateTime), "D2"));
                                                        }
                                                        else
                                                        {
                                                            TSndStr.Append("0000");
                                                        }
                                                    }
                                                    else
                                                    {
                                                        TSndStr.Append("000000000000");
                                                    }
                                                    //TODO:MS 1010 lar güncellendiğinde komuta bunlarda eklenecek.
                                                    //TSndStr.Append("0000");
                                                    //if (tDBReader["Kart ID 2"].ToString() != null && tDBReader["Kart ID 2"].ToString() != "")
                                                    //{
                                                    //    TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID 2"]), "D10"));
                                                    //}
                                                    //else
                                                    //{
                                                    //    TSndStr.Append("0000000000");
                                                    //}
                                                    //TSndStr.Append("0000");
                                                    //if (tDBReader["Kart ID 3"].ToString() != null && tDBReader["Kart ID 3"].ToString() != "")
                                                    //{
                                                    //    TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID 3"]), "D10"));
                                                    //}
                                                    //else
                                                    //{
                                                    //    TSndStr.Append("0000000000");
                                                    //}
                                                    //TSndStr.Append("0000");
                                                    //if (tDBReader["Plaka"].ToString() != null && tDBReader["Plaka"].ToString() != "")
                                                    //{//TODO:Plakalara arası başı ve sonu boşluk olmayacak 
                                                    //    if (tDBReader["Plaka"].ToString().Length > 10)
                                                    //    {
                                                    //        TSndStr.Append(tDBReader["Plaka"].ToString().Substring(0, 10).Trim().Replace(" ", ""));
                                                    //    }
                                                    //    else if (tDBReader["Plaka"].ToString().Length < 10)
                                                    //    {
                                                    //        string plaka = "";
                                                    //        for (int i = 0; i < (10 - tDBReader["Plaka"].ToString().Length); i++)
                                                    //        {
                                                    //            plaka += "0";
                                                    //        }
                                                    //        plaka += tDBReader["Plaka"].ToString().Trim().Replace(" ", "");
                                                    //        TSndStr.Append(plaka);
                                                    //    }
                                                    //    else
                                                    //    {
                                                    //        TSndStr.Append(tDBReader["Plaka"].ToString().Trim().Replace(" ", ""));
                                                    //    }
                                                    //}
                                                    //else
                                                    //{
                                                    //    TSndStr.Append("0000000000");
                                                    //}

                                                    TSndStr.Append("**\r");
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        TSndStr.Clear();
                                        TSndStr.Append("ERR");
                                    }

                                }
                                else
                                {

                                    TSndStr.Clear();
                                    TSndStr.Append("ERR");
                                }
                            }
                            else
                            {
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
                else
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "Select * from Users where ID=" + DBIntParam1;
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                TSndStr.Append(ConvertToTypeInt(tDBReader["ID"] as int? ?? default(int), "D6"));
                                TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID"]), "D10"));
                                if (tDBReader["Sifre"].ToString() != null && tDBReader["Sifre"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Sifre"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0001");
                                }
                                if (tDBReader["Grup No"].ToString() != null)
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if ((tDBReader["Grup Takvimi Aktif"] as bool? ?? default(bool)) == true)
                                {
                                    TSndStr.Append("1");
                                }
                                else
                                {
                                    TSndStr.Append("0");
                                }
                                if (tDBReader["Grup Takvimi No"].ToString() != null && tDBReader["Grup Takvimi No"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup Takvimi No"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0001");
                                }
                                if (tDBReader["Visitor Grup No"].ToString() != null && tDBReader["Visitor Grup No"].ToString() != "")
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
                                if (tDBReader["Grup No 2"].ToString() != null && tDBReader["Grup No 2"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 2"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if (tDBReader["Grup No 3"].ToString() != null && tDBReader["Grup No 3"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 3"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                TSndStr.Append("0000");
                                if (tDBReader["Kart ID 2"].ToString() != null && tDBReader["Kart ID 2"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID 2"]), "D10"));
                                }
                                else
                                {
                                    TSndStr.Append("0000000000");
                                }
                                TSndStr.Append("0000");
                                if (tDBReader["Kart ID 3"].ToString() != null && tDBReader["Kart ID 3"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt64(Convert.ToInt64(tDBReader["Kart ID 3"]), "D10"));
                                }
                                else
                                {
                                    TSndStr.Append("0000000000");
                                }
                                TSndStr.Append("0000");
                                if (tDBReader["Plaka"].ToString() != null && tDBReader["Plaka"].ToString() != "")
                                {
                                    if (tDBReader["Plaka"].ToString().Length > 10)
                                    {
                                        TSndStr.Append(tDBReader["Plaka"].ToString().Substring(0, 10).Trim().Replace(" ", ""));
                                    }
                                    else if (tDBReader["Plaka"].ToString().Length < 10)
                                    {
                                        string plaka = "";
                                        for (int i = 0; i < (10 - tDBReader["Plaka"].ToString().Length); i++)
                                        {
                                            plaka += "0";
                                        }
                                        plaka += tDBReader["Plaka"].ToString().Trim().Replace(" ", "");
                                        TSndStr.Append(plaka);
                                    }
                                    else
                                    {
                                        TSndStr.Append(tDBReader["Plaka"].ToString().Trim().Replace(" ", ""));
                                    }
                                }
                                else
                                {
                                    TSndStr.Append("0000000000");
                                }
                                TSndStr.Append("0");//TODO:Plaka Firmware eklenince silinecek
                                                    //TODO: Plaka Firmware eklenince açılacak
                                                    //if (tDBReader["Gecis Modu"].ToString() != null && tDBReader["Gecis Modu"].ToString() != "")
                                                    //{
                                                    //    TSndStr.Append(ConvertToTypeInt(tDBReader["Gecis Modu"] as int? ?? default(int), "D1"));
                                                    //}
                                                    //else
                                                    //{
                                                    //    TSndStr.Append("0");
                                                    //}

                                if (tDBReader["Grup No 4"].ToString() != null && tDBReader["Grup No 4"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 4"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if (tDBReader["Grup No 5"].ToString() != null && tDBReader["Grup No 5"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 5"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if (tDBReader["Grup No 6"].ToString() != null && tDBReader["Grup No 6"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 6"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if (tDBReader["Grup No 7"].ToString() != null && tDBReader["Grup No 7"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 7"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }
                                if (tDBReader["Grup No 8"].ToString() != null && tDBReader["Grup No 8"].ToString() != "")
                                {
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Grup No 8"] as int? ?? default(int), "D4"));
                                }
                                else
                                {
                                    TSndStr.Append("0000");
                                }

                                TSndStr.Append("000");//23 Karakter 0
                                TSndStr.Append("**\r");
                                mTaskTimeOut = 3;
                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
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
                mTaskTimeOut = 3;
            }
            /*12*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GROUPCALENDAR || DBTaskType == (ushort)CommandConstants.CMD_SNDALL_GROUPCALENDAR)
            {
                lock (TLockObj)
                {

                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
                    }
                }
            }
            /*13*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_USER)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append(DBIntParam1.ToString("D5"));
                    TSndStr.Append("**\r");
                    mTaskTimeOut = 3;
                }
                else
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append(DBIntParam1.ToString("D6"));
                    TSndStr.Append("**\r");
                    mTaskTimeOut = 3;
                }
            }
            /*14*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_USER)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 30;
            }
            /*15*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOGCOUNT)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*16*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOGS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*17*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_LOGCOUNT)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*18*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_MAXINCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*19*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ACCESSCOUNTERS)
            {

                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append(DBIntParam1.ToString("D5"));
                    TSndStr.Append("**\r");
                    mTaskTimeOut = 3;
                }
                else
                {
                    TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                    TSndStr.Append(mPanelSerialNo.ToString("X4"));
                    TSndStr.Append(mPanelNo.ToString("D3"));
                    TSndStr.Append(DBIntParam1.ToString("D6"));
                    TSndStr.Append("**\r");
                    mTaskTimeOut = 3;
                }

            }
            /*20*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_ACCESSCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*21*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_ACCESSCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*22*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*23*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ACCESSGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D4"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*24*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERSALL_APBCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*25*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_APBCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D6"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
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
                mTaskTimeOut = 3;
            }
            /*27*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_RTC)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*31*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_RELAYPROGRAM)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            if ((tDBReader["Haftanin Gunu"] as int? ?? default(int)) > 7)
                            {
                                tDBSQLStr2 = "SELECT * FROM TatilGunus WHERE " +
                                    "[Ozel Gun No]=" + DBIntParam2.ToString();
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                tDBReader2 = tDBCmd2.ExecuteReader();
                                if (tDBReader2.Read())
                                {
                                    TSndStr.Append(ConvertToTypeInt((tDBReader["Haftanin Gunu"] as int? ?? default(int)), "D2"));
                                    TSndStr.Append(ConvertToTypeInt((tDBReader["Zaman Dilimi"] as int? ?? default(int)), "D2"));
                                    TSndStr.Append(ConvertToTypeDatetime(tDBReader2["Tarih"] as DateTime? ?? default(DateTime), "D2"));
                                }
                                else
                                {
                                    TSndStr.Append("01");
                                    TSndStr.Append(ConvertToTypeInt((tDBReader["Zaman Dilimi"] as int? ?? default(int)), "D2"));
                                    TSndStr.Append("000000");
                                }

                            }
                            else
                            {
                                TSndStr.Append(ConvertToTypeInt((tDBReader["Haftanin Gunu"] as int? ?? default(int)), "D2"));
                                TSndStr.Append(ConvertToTypeInt((tDBReader["Zaman Dilimi"] as int? ?? default(int)), "D2"));
                                TSndStr.Append("000000");
                            }

                            if (tDBReader["Aktif"] as bool? ?? default(bool) == true)
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
                                if (tDBReader[durum] as bool? ?? default(bool) == true)
                                {
                                    if (tDBReader[role] as bool? ?? default(bool) == true)
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*35*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_LOCALCAPACITYCOUNTERS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(DBIntParam1.ToString("D2"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
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
                mTaskTimeOut = 3;
            }
            /*37*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_MAXUSERID)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                if (mPanelModel == (int)PanelModel.Panel_1010)
                    TSndStr.Append(DBIntParam1.ToString("D5"));
                else
                    TSndStr.Append(DBIntParam1.ToString("D6"));

                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*38*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LOGSETTINGS)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            if ((tDBReader["Offline Blocked Request"] as bool? ?? default(bool)))
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*40*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LIFTGROUP)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*42*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_LIFTGROUP || DBTaskType == (ushort)CommandConstants.CMD_ERSALL_LIFTGROUP)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*43*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_USERALARM)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*45*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_USERALARM || DBTaskType == (ushort)CommandConstants.CMD_ERSALL_USERALARM)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*46*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_ALARMFIRE_STATUS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*47*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_ERS_DOORALARM_STATUS)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("****\r");
                mTaskTimeOut = 3;
            }
            /*49*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_LOCALINTERLOCK)
            {
                lock (TLockObj)
                {
                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                    {
                        mDBConn.Open();
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
                            mTaskTimeOut = 3;
                        }
                        else
                        {
                            TSndStr.Clear();
                            TSndStr.Append("ERR");
                        }
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
                mTaskTimeOut = 3;
            }
            /*DOOR TRIGGER*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORTRIGGER)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(mTaskStrParam1.ToString());
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*DOOR OPEN*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFORCEOPEN)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(mTaskStrParam1.ToString());
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*DOOR CLOSE*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFORCECLOSE)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(mTaskStrParam1.ToString());
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*DOOR FREE*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_DOORFREE)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append(mTaskStrParam1.ToString());
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*Yeni Panel Ayar Gönderme Fortigate 3'Bölünme 1.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_1)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                tDBSQLStr2 = "SELECT * FROM ReaderSettingsNewMS " +
                                    "WHERE [Panel ID] = " + DBIntParam1.ToString();
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                tDBReader2 = tDBCmd2.ExecuteReader();
                                if (tDBReader2.Read())
                                {
                                    if ((tDBReader["Panel M1 Role"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("01");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel M1 Role"] as int? ?? default(int), "D2"));//PS
                                    }
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Tipi"] as int? ?? default(int), "D1"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["New Device ID"] as int? ?? default(int), "D3"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Kontrol Modu"] as int? ?? default(int), "D1"));//RS
                                    if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 0)//PS
                                        TSndStr.Append("0");// StanAlone
                                    else if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 1)
                                        TSndStr.Append("1");//Online
                                    else
                                        TSndStr.Append("2");

                                    TSndStr.Append("0000");

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Gecis Modu"] as int? ?? default(int), "D1"));//RS

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block"] as int? ?? default(int), "D2"));//PS

                                    if ((tDBReader2["RS485 Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }
                                    string LCDMessage = tDBReader2["LCD Row Message"].ToString();//RS
                                    if (LCDMessage.Length > 16)
                                    {
                                        LCDMessage = LCDMessage.Substring(0, 16);
                                    }
                                    else if (LCDMessage.Length < 16)
                                    {
                                        int count = (16 - LCDMessage.Length);
                                        for (int i = 0; i < count; i++)
                                        {
                                            LCDMessage += " ";
                                        }
                                    }

                                    TSndStr.Append(LCDMessage);

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel GW" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Subnet" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel TCP Port"] as int? ?? default(int), "D5"));//PS

                                    if ((tDBReader2["WKapi Keypad Status"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("00001234");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int), "D8"));
                                    }

                                    if ((tDBReader2["RS485 Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Mifare Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["Mifare Kart Data Type"] as int? ?? default(int), "D1"));//RS

                                    if ((tDBReader2["UDP Haberlesme"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Multiple Clock Mode Counter Usage"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Pass Counter Auto Delete Cancel"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader["Panel Same Tag Block HourMinSec"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Access Counter Kontrol"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    for (int i = 1; i < 5; i++)//RS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Remote IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(5700, "D5"));//Remote TCP Port

                                    if ((tDBReader2["Kart ID 32 Bit Clear"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }


                                    if ((tDBReader2["Turnstile Arm Tracking"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append("0");//Device Model


                                    TSndStr.Append("00000000");//Ekleme Kartı
                                    TSndStr.Append("00000000");//Silme Kartı Kartı

                                    TSndStr.Append("00000000000000");

                                    TSndStr.Append("**\r");
                                    mTaskTimeOut = 3;
                                }
                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
                else
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
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

                                if ((tDBReader["DHCP Enabled"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                TSndStr.Append("00000");

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

                                // Panel Button Detector
                                if ((tDBReader["Panel Button Detector"] as bool? ?? default(bool)))
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append("0");

                                //Panel Button Detector Time
                                if ((tDBReader["Panel Button Detector Time"] as int? ?? default(int)) <= 0 || (tDBReader["Panel Button Detector Time"] as int? ?? default(int)) > 9)
                                    TSndStr.Append("1");
                                else
                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Button Detector Time"] as int? ?? default(int), "D1"));


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
                                    TSndStr.Append(ConvertTurkceKarekter(tDBReader["Panel Name"].ToString()) + space);
                                }
                                else
                                {
                                    TSndStr.Append(ConvertTurkceKarekter(tDBReader["Panel Name"].ToString().Substring(0, 16)));
                                }

                                TSndStr.Append("**\r");
                                mTaskTimeOut = 3;

                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
            }
            /*Yeni Panel Ayar Gönderme Fortigate 3'Bölünme 2.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_2)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                tDBSQLStr2 = "SELECT * FROM ReaderSettingsNewMS " +
                                    "WHERE [Panel ID] = " + DBIntParam1.ToString();
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                tDBReader2 = tDBCmd2.ExecuteReader();
                                if (tDBReader2.Read())
                                {
                                    if ((tDBReader["Panel M1 Role"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("01");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel M1 Role"] as int? ?? default(int), "D2"));//PS
                                    }
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Tipi"] as int? ?? default(int), "D1"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["New Device ID"] as int? ?? default(int), "D3"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Kontrol Modu"] as int? ?? default(int), "D1"));//RS
                                    if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 0)//PS
                                        TSndStr.Append("0");// StanAlone
                                    else if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 1)
                                        TSndStr.Append("1");//Online
                                    else
                                        TSndStr.Append("2");

                                    TSndStr.Append("0000");

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Gecis Modu"] as int? ?? default(int), "D1"));//RS

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block"] as int? ?? default(int), "D2"));//PS

                                    if ((tDBReader2["RS485 Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }
                                    string LCDMessage = tDBReader2["LCD Row Message"].ToString();//RS
                                    if (LCDMessage.Length > 16)
                                    {
                                        LCDMessage = LCDMessage.Substring(0, 16);
                                    }
                                    else if (LCDMessage.Length < 16)
                                    {
                                        int count = (16 - LCDMessage.Length);
                                        for (int i = 0; i < count; i++)
                                        {
                                            LCDMessage += " ";
                                        }
                                    }

                                    TSndStr.Append(LCDMessage);

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel GW" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Subnet" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel TCP Port"] as int? ?? default(int), "D5"));//PS

                                    if ((tDBReader2["WKapi Keypad Status"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("00001234");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int), "D8"));
                                    }

                                    if ((tDBReader2["RS485 Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Mifare Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["Mifare Kart Data Type"] as int? ?? default(int), "D1"));//RS

                                    if ((tDBReader2["UDP Haberlesme"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Multiple Clock Mode Counter Usage"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Pass Counter Auto Delete Cancel"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader["Panel Same Tag Block HourMinSec"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Access Counter Kontrol"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    for (int i = 1; i < 5; i++)//RS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Remote IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(5700, "D5"));//Remote TCP Port

                                    if ((tDBReader2["Kart ID 32 Bit Clear"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }


                                    if ((tDBReader2["Turnstile Arm Tracking"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append("0");//Device Model


                                    TSndStr.Append("00000000");//Ekleme Kartı
                                    TSndStr.Append("00000000");//Silme Kartı Kartı

                                    TSndStr.Append("00000000000000");

                                    TSndStr.Append("**\r");
                                    mTaskTimeOut = 3;
                                }
                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
                else
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));


                                /*WIG Reader Settings*/

                                for (int i = 1; i < 9; i++)
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

                                        if ((tDBReader2["WKapi Alarm Modu"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");

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
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString()) + space);
                                        }
                                        else
                                        {
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString().Substring(0, 15)));
                                        }

                                        //Buton detector mode
                                        if ((tDBReader2["WKapi Ana Alarm Rolesi"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");
                                    }
                                }
                                TSndStr.Append("**\r");
                                mTaskTimeOut = 3;

                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
            }
            /*Yeni Panel Ayar Gönderme Fortigate 3'Bölünme 3.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_3)
            {
                if (mPanelModel == (int)PanelModel.Panel_1010)
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));
                                tDBSQLStr2 = "SELECT * FROM ReaderSettingsNewMS " +
                                    "WHERE [Panel ID] = " + DBIntParam1.ToString();
                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                tDBReader2 = tDBCmd2.ExecuteReader();
                                if (tDBReader2.Read())
                                {
                                    if ((tDBReader["Panel M1 Role"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("01");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel M1 Role"] as int? ?? default(int), "D2"));//PS
                                    }
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Tipi"] as int? ?? default(int), "D1"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["New Device ID"] as int? ?? default(int), "D3"));//RS
                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Kontrol Modu"] as int? ?? default(int), "D1"));//RS
                                    if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 0)//PS
                                        TSndStr.Append("0");// StanAlone
                                    else if ((tDBReader["Kontrol Modu"] as int? ?? default(int)) == 1)
                                        TSndStr.Append("1");//Online
                                    else
                                        TSndStr.Append("2");

                                    TSndStr.Append("0000");

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Kapi Gecis Modu"] as int? ?? default(int), "D1"));//RS

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Same Tag Block"] as int? ?? default(int), "D2"));//PS

                                    if ((tDBReader2["RS485 Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }
                                    string LCDMessage = tDBReader2["LCD Row Message"].ToString();//RS
                                    if (LCDMessage.Length > 16)
                                    {
                                        LCDMessage = LCDMessage.Substring(0, 16);
                                    }
                                    else if (LCDMessage.Length < 16)
                                    {
                                        int count = (16 - LCDMessage.Length);
                                        for (int i = 0; i < count; i++)
                                        {
                                            LCDMessage += " ";
                                        }
                                    }

                                    TSndStr.Append(LCDMessage);

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel GW" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    for (int i = 1; i < 5; i++)//PS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Subnet" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader["Panel TCP Port"] as int? ?? default(int), "D5"));//PS

                                    if ((tDBReader2["WKapi Keypad Status"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("00001234");
                                    }
                                    else
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader2["WKapi Keypad Menu Password"] as int? ?? default(int), "D8"));
                                    }

                                    if ((tDBReader2["RS485 Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Wiegand Reader Type"] as int? ?? default(int)) == 0)//RS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Mifare Reader Status"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append(ConvertToTypeInt(tDBReader2["Mifare Kart Data Type"] as int? ?? default(int), "D1"));//RS

                                    if ((tDBReader2["UDP Haberlesme"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Multiple Clock Mode Counter Usage"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader2["Pass Counter Auto Delete Cancel"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    if ((tDBReader["Panel Same Tag Block HourMinSec"] as int? ?? default(int)) == 0)//PS
                                    {
                                        TSndStr.Append("0");
                                    }
                                    else
                                    {
                                        TSndStr.Append("1");
                                    }

                                    if ((tDBReader2["Access Counter Kontrol"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    for (int i = 1; i < 5; i++)//RS
                                    {
                                        TSndStr.Append(ConvertToTypeInt(tDBReader["Panel Remote IP" + i] as int? ?? default(int), "D3"));
                                    }

                                    TSndStr.Append(ConvertToTypeInt(5700, "D5"));//Remote TCP Port

                                    if ((tDBReader2["Kart ID 32 Bit Clear"] as bool? ?? default(bool)) == true)
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }


                                    if ((tDBReader2["Turnstile Arm Tracking"] as bool? ?? default(bool)) == true)//RS
                                    {
                                        TSndStr.Append("1");
                                    }
                                    else
                                    {
                                        TSndStr.Append("0");
                                    }

                                    TSndStr.Append("0");//Device Model


                                    TSndStr.Append("00000000");//Ekleme Kartı
                                    TSndStr.Append("00000000");//Silme Kartı Kartı

                                    TSndStr.Append("00000000000000");

                                    TSndStr.Append("**\r");
                                    mTaskTimeOut = 3;
                                }
                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
                else
                {
                    lock (TLockObj)
                    {
                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                        {
                            mDBConn.Open();
                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                "WHERE [Panel ID] = " + DBIntParam1.ToString();
                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                            tDBReader = tDBCmd.ExecuteReader();
                            if (tDBReader.Read())
                            {
                                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                                TSndStr.Append(mPanelNo.ToString("D3"));


                                /*WIG Reader Settings*/

                                for (int i = 9; i < 17; i++)
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

                                        if ((tDBReader2["WKapi Alarm Modu"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");

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
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString()) + space);
                                        }
                                        else
                                        {
                                            TSndStr.Append(ConvertTurkceKarekter(tDBReader2["WKapi Adi"].ToString().Substring(0, 15)));
                                        }

                                        //Buton detector mode
                                        if ((tDBReader2["WKapi Ana Alarm Rolesi"] as bool? ?? default(bool)))
                                            TSndStr.Append("1");
                                        else
                                            TSndStr.Append("0");
                                    }
                                }
                                TSndStr.Append("**\r");
                                mTaskTimeOut = 3;

                            }
                            else
                            {
                                TSndStr.Clear();
                                TSndStr.Append("ERR");
                            }
                        }
                    }
                }
            }
            /*Yeni Panel Ayar Alma Fortigate 3'Bölünme 1.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_1)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*Yeni Panel Ayar Alma Fortigate 3'Bölünme 2.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_2)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }
            /*Yeni Panel Ayar Alma Fortigate 3'Bölünme 3.Bölüm*/
            else if (DBTaskType == (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_3)
            {
                TSndStr.Append("%" + GetCommandPrefix(DBTaskType));
                TSndStr.Append(mPanelSerialNo.ToString("X4"));
                TSndStr.Append(mPanelNo.ToString("D3"));
                TSndStr.Append("**\r");
                mTaskTimeOut = 3;
            }


            return TSndStr.ToString();
        }

        /// <summary>
        /// Panel'e Gönderilen Görevlerden Dönen Sonuçları Alma
        /// </summary>
        /// <param name="TClient"></param>
        /// <param name="TmpTaskType"></param>
        /// <returns></returns>
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

                if (TmpTaskType == CommandConstants.CMD_SND_DOORFORCECLOSE)
                    TPos = TRcvData.IndexOf("$FO");
                else
                    TPos = TRcvData.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));

                if (TPos > -1)
                {

                    if (mPanelModel == (int)PanelModel.Panel_1010)
                    {
                        if (TmpTaskType == CommandConstants.CMD_SND_GENERALSETTINGS)
                        {
                            if (TRcvData.Substring(TPos + 13, 1) == "O")
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
                            if (TRcvData.Substring(TPos + 10, 1) == "O")
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                    }
                    else
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

        /// <summary>
        /// Beklenen Boyutla Gelen Boyutu Kıyaslama Yapıyor
        /// </summary>
        /// <param name="TClient"></param>
        /// <param name="TWaitSize"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Yapılan Görevin Durumunu Görev Tablosunda Güncelleme
        /// </summary>
        /// <param name="tTaskNo"></param>
        /// <param name="tTaskStatus"></param>
        /// <param name="TaskTypeCode"></param>
        /// <returns></returns>
        public bool SyncUpdateTaskStatus(int tTaskNo, ushort tTaskStatus, CommandConstants TaskTypeCode)
        {
            object TLockObj = new object();
            int TRetInt;
            if (tTaskNo <= 0)
            {
                return false;
            }

            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    mDBSQLStr = "UPDATE TaskList SET [Durum Kodu]=" + tTaskStatus + " WHERE [Kayit No]=" + tTaskNo;
                    mDBCmd = new SqlCommand(mDBSQLStr, mDBConn);
                    TRetInt = mDBCmd.ExecuteNonQuery();

                    if (TRetInt > 0)
                    {
                        if (TaskTypeCode == CommandConstants.CMD_RCV_LOGS)
                            CheckDeleteAfterReceiving(tTaskNo);
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }

        }

        /// <summary>
        /// Panel'den Gelen Verinin Parçalanması İçin Değişkene Atama
        /// </summary>
        /// <param name="TClient"></param>
        /// <param name="TReturnStr"></param>
        /// <param name="TmpTaskType"></param>
        /// <returns></returns>
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
                    TPos = TRcvData.IndexOf("$FD");
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


            }
            catch (Exception)
            {

                return false;
            }

        }

        /// <summary>
        /// Panel'den Log Data'larını Almak İçin Kullanılan Metot
        /// </summary>
        /// <param name="TClient"></param>
        /// <param name="TReturnLogStr"></param>
        /// <param name="TmpLogTaskType"></param>
        /// <returns></returns>
        public bool ReveiveLogData(TcpClient TClient, ref string TReturnLogStr, CommandConstants TmpLogTaskType)
        {
            int TSize = GetAnswerSize(TmpLogTaskType);
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
                TPos = TRcvData.IndexOf("$" + GetCommandPrefix((ushort)TmpLogTaskType));
                if (TPos > -1)
                {
                    TReturnLogStr = TRcvData;
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

        /// <summary>
        /// Panel'den Dönen Verinin Veritabanına kayıt işlemleri gerçekleştiriliyor.
        /// </summary>
        /// <param name="DBIntParam1"></param>
        /// <param name="DBIntParam2"></param>
        /// <param name="DBIntParam3"></param>
        /// <param name="TmpTaskType"></param>
        /// <param name="TmpTaskSoruce"></param>
        /// <param name="TmpTaskUpdateTable"></param>
        /// <param name="TmpReturnStr"></param>
        /// <returns></returns>
        public bool ProcessReceivedData(int DBIntParam1, int DBIntParam2, int DBIntParam3, CommandConstants TmpTaskType, ushort TmpTaskSoruce, bool TmpTaskUpdateTable, string TmpReturnStr)
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
            byte TByte1;
            int TLong;
            int SI;

            if (TmpTaskType == CommandConstants.CMD_RCV_LOGS)
            {
                TPos = TmpReturnStr.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));
                if (TPos < 0)
                {
                    TPos = TmpReturnStr.IndexOf("$FD");
                    if (TPos < 0)
                    {
                        SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_ERROR, TmpTaskType);
                        mPanelProc = CommandConstants.CMD_TASK_LIST;
                        return false;
                    }
                    else
                    {
                        SyncUpdateTaskStatus(mTaskNo, (ushort)CTaskStates.TASK_COMPLETED, TmpTaskType);
                        mPanelProc = CommandConstants.CMD_TASK_LIST;
                        return true;
                    }

                }
            }
            else
            {
                TPos = TmpReturnStr.IndexOf("$" + GetCommandPrefix((ushort)TmpTaskType));
                if (TPos < 0)
                {
                    if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16) != mPanelSerialNo || Convert.ToInt32(TmpReturnStr.Substring(TPos + 7, 3)) != mPanelNo)
                    {
                        return false;
                    }
                }
            }


            switch (TmpTaskType)
            {
                case CommandConstants.CMD_RCV_USER:
                case CommandConstants.CMD_RCVALL_USER:
                    {
                        if (mPanelModel != (int)PanelModel.Panel_1010)
                        {
                            if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6)) == DBIntParam1)
                            {
                                if (TmpTaskUpdateTable)
                                {
                                    lock (TLockObj)
                                    {
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
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


                                                tDBSQLStr2 = "INSERT INTO Users (ID,[Kart ID],Sifre,[Grup No],[Grup Takvimi Aktif],[Grup Takvimi No],[Visitor Grup No],[Sureli Kullanici],[Bitis Tarihi],[3 Grup],[Grup No 2],[Grup No 3],[Kart ID 2],[Kart ID 2],[Plaka],[Grup No 4],[Grup No 5],[Grup No 6],[Grup No 7],[Grup No 8])" +
                                                    "VALUES (" +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 16, 10).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 26, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 30, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 34, 1).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 35, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 39, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 43, 1).Trim()) + "," +
                                                    "'" + tDateTime + "'," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 50, 1).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 51, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 55, 4).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 63, 10).Trim()) + "," +
                                                    Convert.ToInt32(TmpReturnStr.Substring(TPos + 77, 10).Trim()) + "," +
                                                    "'" + TmpReturnStr.Substring(TPos + 91, 10).Trim() + "," +
                                                     Convert.ToInt32(TmpReturnStr.Substring(TPos + 102, 4).Trim()) + "," +
                                                     Convert.ToInt32(TmpReturnStr.Substring(TPos + 106, 4).Trim()) + "," +
                                                     Convert.ToInt32(TmpReturnStr.Substring(TPos + 110, 4).Trim()) + "," +
                                                     Convert.ToInt32(TmpReturnStr.Substring(TPos + 114, 4).Trim()) + "," +
                                                     Convert.ToInt32(TmpReturnStr.Substring(TPos + 118, 4).Trim()) + "')";
                                            }
                                            else
                                            {
                                                tDBSQLStr2 = "UPDATE Users" +
                                                    " SET " +
                                                    " [Kart ID] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 16, 10).Trim()) + "," +
                                                    " [Sifre] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 26, 4).Trim()) + "," +
                                                    " [Grup No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 30, 4).Trim()) + "," +
                                                    " [Grup Takvimi Aktif] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 34, 1).Trim()) + "," +
                                                    " [Grup Takvimi No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 35, 4).Trim()) + "," +
                                                    " [Visitor Grup No] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 39, 4).Trim()) + "," +
                                                    " [Sureli Kullanici] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 43, 1).Trim()) + "," +
                                                    " [Bitis Tarihi] = " + "'" + tDateTime + "'," +
                                                    " [3 Grup] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 50, 1).Trim()) + "," +
                                                    " [Grup No 2] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 51, 4).Trim()) + "," +
                                                    " [Grup No 3] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 55, 4).Trim()) + "," +
                                                    " [Kart ID 2] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 63, 10).Trim()) + "," +
                                                    " [Kart ID 3] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 77, 10).Trim()) + "," +
                                                    " [Plaka] = '" + TmpReturnStr.Substring(TPos + 91, 10).Trim() + "'" + "," +
                                                    " [Grup No 4] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 102, 4).Trim()) + "," +
                                                    " [Grup No 5] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 106, 4).Trim()) + "," +
                                                    " [Grup No 6] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 110, 4).Trim()) + "," +
                                                    " [Grup No 7] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 114, 4).Trim()) + "," +
                                                    " [Grup No 8] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 118, 4).Trim()) +
                                                    " WHERE [ID] = " + Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 6).Trim());
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
                        return false;
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
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
                                        for (int i = 0; i <= 7; i++)
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
                                if (mDBConn.State != ConnectionState.Open)
                                    mDBConn.Open();

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
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
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
                    }
                    break;
                case CommandConstants.CMD_RCV_GENERALSETTINGS:
                    {
                        if (mPanelModel == (int)PanelModel.Panel_1010)
                        {

                            int ID;
                            int NewID;
                            int PanelMACAddress;
                            int MasterRoleNo;
                            int[] LocalGateway = new int[4];
                            int[] LocalIPAddress = new int[4];
                            int LocalTCPPort;
                            int RemoteTCPPort;
                            int[] LocalSubnetMask = new int[4];
                            int[] RemoteIPAddress = new int[4];
                            int WKapi_Kapi_Tipi;
                            int WKapi_Kapi_Kontrol_Modu;
                            int Kontrol_Modu;
                            int WKapi_Kapi_Gecis_Modu;
                            int RS485_Reader_Type;
                            string LCD_Row_Message;
                            int WKapi_Keypad_Status;
                            int WKapi_Keypad_Menu_Password;
                            int RS485_Reader_Status;
                            int Wiegand_Reader_Status;
                            int Wiegand_Reader_Type;
                            int Mifare_Reader_Status;
                            int Mifare_Kart_Data_Type;
                            int UDP_Haberlesme;
                            int Multiple_Clock_Mode_Counter_Usage;
                            int Kart_ID_32_Bit_Clear;
                            int Pass_Counter_Auto_Delete_Cancel;
                            int Access_Counter_Kontrol;
                            int Turnstile_Arm_Tracking;
                            int Panel_Same_Tag_Block_HourMinSec;
                            int Mukkerrer_Engelleme_Suresi;


                            SI = 3;
                            PanelMACAddress = int.Parse(TmpReturnStr.Substring(SI, 4), NumberStyles.HexNumber);

                            SI = 7;
                            ID = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));

                            SI = 10;
                            MasterRoleNo = Convert.ToInt32(TmpReturnStr.Substring(SI, 2));

                            SI = 12;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                                WKapi_Kapi_Tipi = 1;
                            else
                                WKapi_Kapi_Tipi = 0;


                            SI = 13;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 3)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 3)) < 255)
                            {
                                NewID = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                            }
                            else
                            {
                                NewID = 0;
                            }

                            SI = 16;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                WKapi_Kapi_Kontrol_Modu = 0;
                            }
                            else
                            {
                                WKapi_Kapi_Kontrol_Modu = 1;
                            }

                            SI = 17;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Kontrol_Modu = 0;
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            {
                                Kontrol_Modu = 1;
                            }
                            else
                            {
                                Kontrol_Modu = 2;
                            }

                            SI = 22;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                WKapi_Kapi_Gecis_Modu = 0;
                            }
                            else
                            {
                                WKapi_Kapi_Gecis_Modu = 1;
                            }


                            SI = 23;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) <= 99)
                            {
                                Mukkerrer_Engelleme_Suresi = Convert.ToInt32(TmpReturnStr.Substring(SI, 2));
                            }
                            else
                            {
                                Mukkerrer_Engelleme_Suresi = 0;
                            }

                            SI = 25;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                RS485_Reader_Type = 0;
                            }
                            else
                            {
                                RS485_Reader_Type = 1;
                            }
                            SI = 26;
                            LCD_Row_Message = TmpReturnStr.Substring(SI, 16);

                            SI = 42;
                            LocalGateway[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                            LocalGateway[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                            LocalGateway[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                            LocalGateway[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                            SI = 54;
                            LocalIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                            LocalIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                            LocalIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                            LocalIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                            SI = 66;
                            LocalSubnetMask[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                            LocalSubnetMask[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                            LocalSubnetMask[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                            LocalSubnetMask[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                            SI = 78;
                            LocalTCPPort = Convert.ToInt32(TmpReturnStr.Substring(SI, 5));


                            SI = 83;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                WKapi_Keypad_Status = 0;
                            }
                            else
                            {
                                WKapi_Keypad_Status = 1;
                            }

                            SI = 84;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 8)) != 0)
                            {
                                WKapi_Keypad_Menu_Password = Convert.ToInt32(TmpReturnStr.Substring(SI, 8));
                            }
                            else
                            {
                                WKapi_Keypad_Menu_Password = 1234;
                            }






                            SI = 92;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                RS485_Reader_Status = 0;
                            }
                            else
                            {
                                RS485_Reader_Status = 1;
                            }

                            SI = 93;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Wiegand_Reader_Status = 0;
                            }
                            else
                            {
                                Wiegand_Reader_Status = 1;
                            }

                            SI = 94;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Wiegand_Reader_Type = 0;
                            }
                            else
                            {
                                Wiegand_Reader_Type = 1;
                            }
                            SI = 95;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Mifare_Reader_Status = 0;
                            }
                            else
                            {
                                Mifare_Reader_Status = 1;
                            }

                            SI = 96;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Mifare_Kart_Data_Type = 0;
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                            {
                                Mifare_Kart_Data_Type = 1;
                            }
                            else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 2)
                            {
                                Mifare_Kart_Data_Type = 2;
                            }
                            else
                            {
                                Mifare_Kart_Data_Type = 3;
                            }

                            SI = 97;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                UDP_Haberlesme = 0;
                            }
                            else
                            {
                                UDP_Haberlesme = 1;
                            }

                            SI = 98;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Multiple_Clock_Mode_Counter_Usage = 0;
                            }
                            else
                            {
                                Multiple_Clock_Mode_Counter_Usage = 1;
                            }

                            SI = 99;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Pass_Counter_Auto_Delete_Cancel = 0;
                            }
                            else
                            {
                                Pass_Counter_Auto_Delete_Cancel = 1;
                            }

                            SI = 100;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Panel_Same_Tag_Block_HourMinSec = 0;
                            }
                            else
                            {
                                Panel_Same_Tag_Block_HourMinSec = 1;
                            }

                            SI = 101;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Access_Counter_Kontrol = 0;
                            }
                            else
                            {
                                Access_Counter_Kontrol = 1;
                            }

                            SI = 102;
                            RemoteIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                            RemoteIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                            RemoteIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                            RemoteIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                            SI = 114;
                            RemoteTCPPort = Convert.ToInt32(TmpReturnStr.Substring(SI, 5));

                            SI = 119;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Kart_ID_32_Bit_Clear = 0;
                            }
                            else
                            {
                                Kart_ID_32_Bit_Clear = 1;
                            }

                            SI = 120;
                            if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                            {
                                Turnstile_Arm_Tracking = 0;
                            }
                            else
                            {
                                Turnstile_Arm_Tracking = 1;
                            }
                            if (TmpTaskUpdateTable)
                            {
                                lock (TLockObj)
                                {
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
                                        tDBSQLStr = "SELECT * FROM ReaderSettingsNewMS " +
                                            " WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                            " AND [Panel ID] = " + mPanelNo.ToString();
                                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                        tDBReader = tDBCmd.ExecuteReader();
                                        if (!tDBReader.Read())
                                        {
                                            tDBSQLStr2 = "INSERT INTO [ReaderSettingsNewMS]" +
           "([Seri No],[Sira No],[Panel ID],[Panel Name],[WKapi ID],[New Device ID],[WKapi Kapi Tipi],[WKapi Kapi Kontrol Modu]," +
            "[WKapi Kapi Gecis Modu],[RS485 Reader Type],[LCD Row Message],[WKapi Keypad Status],[WKapi Keypad Menu Password]," +
            "[RS485 Reader Status],[Wiegand Reader Status],[Wiegand Reader Type],[Mifare Reader Status],[Mifare Kart Data Type]," +
            "[UDP Haberlesme],[Multiple Clock Mode Counter Usage],[Kart ID 32 Bit Clear],[Pass Counter Auto Delete Cancel]," +
            "[Access Counter Kontrol],[Turnstile Arm Tracking])" +
            "VALUES(" +
            PanelMACAddress + "," + ID + "," + ID + ",'MAVISOFT MS-1010'," + 1 + "," + NewID + "," +
            "" + WKapi_Kapi_Tipi + "," + WKapi_Kapi_Kontrol_Modu + "," + WKapi_Kapi_Gecis_Modu + "," + RS485_Reader_Type + "," +
            "'" + LCD_Row_Message + "'," + WKapi_Keypad_Status + "," + WKapi_Keypad_Menu_Password + "," + RS485_Reader_Status + "," +
            "" + Wiegand_Reader_Status + "," + Wiegand_Reader_Type + "," + Mifare_Reader_Status + "," + Mifare_Kart_Data_Type + "," +
            "" + UDP_Haberlesme + "," + Multiple_Clock_Mode_Counter_Usage + "," + Kart_ID_32_Bit_Clear + "," +
            "" + Pass_Counter_Auto_Delete_Cancel + "," + Access_Counter_Kontrol + "," + Turnstile_Arm_Tracking + "";
                                        }
                                        else
                                        {
                                            tDBSQLStr2 = "UPDATE [ReaderSettingsNewMS]" +
                                         " SET " +
                                         "[Seri No] = " + PanelMACAddress + "," +
                                         "[Sira No] = " + ID + "," +
                                         "[Panel ID] = " + ID + "" +
                                         ",[Panel Name] = 'MAVISOFT MS-1010'," +
                                         "[WKapi ID] = " + 1 + "," +
                                         "[New Device ID] = " + NewID + "," +
                                         "[WKapi Kapi Tipi] = " + WKapi_Kapi_Tipi + "," +
                                         "[WKapi Kapi Kontrol Modu] = " + WKapi_Kapi_Kontrol_Modu + "," +
                                         "[WKapi Kapi Gecis Modu] = " + WKapi_Kapi_Gecis_Modu + "," +
                                         "[RS485 Reader Type] = " + RS485_Reader_Type + "," +
                                         "[LCD Row Message] = '" + LCD_Row_Message + "'," +
                                         "[WKapi Keypad Status] = " + WKapi_Keypad_Status + "," +
                                         "[WKapi Keypad Menu Password] = " + WKapi_Keypad_Menu_Password + "," +
                                         "[RS485 Reader Status] = " + RS485_Reader_Status + "," +
                                         "[Wiegand Reader Status] = " + Wiegand_Reader_Status + "," +
                                         "[Wiegand Reader Type] = " + Wiegand_Reader_Type + "," +
                                         "[Mifare Reader Status] = " + Mifare_Reader_Status + "," +
                                         "[Mifare Kart Data Type] = " + Mifare_Kart_Data_Type + "," +
                                         "[UDP Haberlesme] = " + UDP_Haberlesme + "," +
                                         "[Multiple Clock Mode Counter Usage] = " + Multiple_Clock_Mode_Counter_Usage + "," +
                                         "[Kart ID 32 Bit Clear] = " + Kart_ID_32_Bit_Clear + "," +
                                         "[Pass Counter Auto Delete Cancel] = " + Pass_Counter_Auto_Delete_Cancel + "," +
                                         "[Access Counter Kontrol] = " + Access_Counter_Kontrol + "," +
                                         "[Turnstile Arm Tracking] = " + Turnstile_Arm_Tracking + "" +
                                         " WHERE [Panel ID] = " + mPanelNo.ToString() +
                                         " AND [Seri No] = " + mPanelSerialNo.ToString();
                                        }
                                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                        TRetInt = tDBCmd2.ExecuteNonQuery();
                                        if (TRetInt <= 0)
                                        {
                                            return false;
                                        }
                                    }
                                }

                                lock (TLockObj)
                                {
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
                                        tDBSQLStr = "SELECT * FROM PanelSettings " +
                                            "WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                            "AND [Panel ID] = " + mPanelNo.ToString();
                                        tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                        tDBReader = tDBCmd.ExecuteReader();
                                        if (tDBReader.Read())
                                        {
                                            tDBSQLStr2 = "UPDATE PanelSettings " +
                                                "SET " +
                                                "[Panel M1 Role] = " + MasterRoleNo + "," +
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
                                                "[Panel Same Tag Block HourMinSec] = " + Panel_Same_Tag_Block_HourMinSec +
                                                " WHERE [Panel ID] = " + mPanelNo.ToString() +
                                                " AND [Seri No] = " + PanelMACAddress;
                                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                            TRetInt = tDBCmd2.ExecuteNonQuery();
                                            if (TRetInt <= 0)
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
                            }
                        }
                        else
                        {
                            int ID;
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
                            int[] PanelLocalAPBs = new int[8];
                            int[] PanelMasterRelayTime = new int[8];
                            int PanelAlarmRelayTime;
                            int PanelMACAddress;
                            int PanelGlobalAPB;

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
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
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
                                }

                                lock (TLockObj)
                                {
                                    using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                    {
                                        mDBConn.Open();
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
                        }
                    }
                    break;
                case CommandConstants.CMD_RCV_LOGSETTINGS:
                    {

                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();
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
                    }
                    break;
                case CommandConstants.CMD_RCV_LOCALINTERLOCK:
                    {
                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();
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
                    }
                    break;
                case CommandConstants.CMD_RCV_LOGS:
                    {

                        try
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

                            if (Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16) == mPanelSerialNo)
                            {
                                TMacSerial = Convert.ToInt32(TmpReturnStr.Substring(TPos + 3, 4), 16);
                                TPanel = Convert.ToInt32(TmpReturnStr.Substring(TPos + 7, 3));
                                if (TPanel > (int)TCONST.MAX_PANEL || TPanel < 1)
                                    break;


                                TReader = Convert.ToInt32(TmpReturnStr.Substring(TPos + 10, 2));
                                TDoorType = Convert.ToInt32(TmpReturnStr.Substring(TPos + 12, 1));
                                TAccessResult = Convert.ToInt32(TmpReturnStr.Substring(TPos + 13, 2));
                                TUsersID = Convert.ToInt64(TmpReturnStr.Substring(TPos + 15, 6));
                                TCardID = FindUserCardID(TUsersID);
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 21, 2), out day)) == false || day <= 0)
                                    day = DateTime.Now.Day;
                                // day = Convert.ToInt32(TmpReturnStr.Substring(TPos + 21, 2));
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 23, 2), out month)) == false || month <= 0)
                                    month = DateTime.Now.Month;
                                // month = Convert.ToInt32(TmpReturnStr.Substring(TPos + 23, 2));
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 25, 2), out year)) == false || year <= -1)
                                {
                                    string temp = DateTime.Now.Year.ToString();
                                    year = Convert.ToInt32(temp.Substring(2, 2));
                                }
                                // year = Convert.ToInt32(TmpReturnStr.Substring(TPos + 25, 2));
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 27, 2), out hour)) == false)
                                    hour = DateTime.Now.Hour;
                                // hour = Convert.ToInt32(TmpReturnStr.Substring(TPos + 27, 2));
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 29, 2), out minute)) == false)
                                    minute = DateTime.Now.Minute;
                                // minute = Convert.ToInt32(TmpReturnStr.Substring(TPos + 29, 2));
                                if ((int.TryParse(TmpReturnStr.Substring(TPos + 31, 2), out second)) == false)
                                    second = DateTime.Now.Second;
                                //second = Convert.ToInt32(TmpReturnStr.Substring(TPos + 31, 2));
                                TDate = new DateTime(int.Parse("20" + year), month, day, hour, minute, second);
                                if (!IsDate(TDate.ToString("yyyy-MM-dd HH:mm:ss")))
                                    TDate = DateTime.Now;

                                if (TUsersID > 100000 || TUsersID < 0)
                                    break;

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
                                    tDBSQLStr = "SELECT * FROM AccessDatas WHERE [Kart ID] = " + TCardID + " AND [Panel ID] = " + TPanel + " AND [Kapi ID] = " + TReader + " AND [Tarih] = '" + TDate.ToString("yyyy-MM-dd HH:mm:ss") + "'";
                                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    tDBReader = tDBCmd.ExecuteReader();
                                    if (!tDBReader.Read())
                                    {
                                        if (TAccessResult == 4)
                                            TUserKayitNo = 1;

                                        tDBSQLStr2 = @"INSERT INTO AccessDatas " +
                                           "([Panel ID],[Lokal Bolge No],[Global Bolge No],[Kapi ID],ID,[Kart ID]," +
                                           "Plaka,Tarih,[Gecis Tipi],Kod,[Kullanici Tipi],[Visitor Kayit No]," +
                                           "[User Kayit No],Kontrol,[Canli Resim])" +
                                           "VALUES " +
                                           "(" +
                                           TPanel + "," + TLocalBolgeNo + "," + TGlobalBolgeNo + "," + TReader + "," +
                                           TUsersID + "," + TCardID + ",'" + TLPR + "','" + TDate.ToString("yyyy-MM-dd HH:mm:ss") + "'," +
                                           (TDoorType - 1) + "," + TAccessResult + "," + TUserType + "," + TVisitorKayitNo + "," +
                                           TUserKayitNo + "," + 0 + "," + "'user_1.jpg'" + ")";
                                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                        TRetInt = tDBCmd2.ExecuteNonQuery();
                                        if (TRetInt <= 0)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                            mPanelProc = CommandConstants.CMD_RCV_LOGS;
                            break;
                        }
                        catch (Exception)
                        {
                            break;
                        }
                    }
                case CommandConstants.CMD_RCV_RTC:
                    {
                        int day;
                        int month;
                        int year;
                        int hour;
                        int minute;
                        int second;
                        if (TmpTaskUpdateTable)
                        {
                            lock (TLockObj)
                            {
                                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                {
                                    mDBConn.Open();
                                    DateTime tDateTime;
                                    if (TmpReturnStr.Substring(TPos + 10, 2) == "00" || TmpReturnStr.Substring(TPos + 12, 2) == "00")
                                    {
                                        tDateTime = Convert.ToDateTime("01/01/" + TmpReturnStr.Substring(TPos + 14, 2));
                                    }
                                    else
                                    {
                                        day = Convert.ToInt32(TmpReturnStr.Substring(10, 2));
                                        month = Convert.ToInt32(TmpReturnStr.Substring(12, 2));
                                        year = Convert.ToInt32(TmpReturnStr.Substring(14, 2));
                                        hour = Convert.ToInt32(TmpReturnStr.Substring(16, 2));
                                        minute = Convert.ToInt32(TmpReturnStr.Substring(18, 2));
                                        second = Convert.ToInt32(TmpReturnStr.Substring(20, 2));
                                        tDateTime = new DateTime(int.Parse("20" + year), month, day, hour, minute, second);
                                    }

                                    // tDBSQLStr = "UPDATE PanelSettings SET [Panel Saati] = '" + tDateTime + "' WHERE [Seri No] = " + mPanelSerialNo.ToString();
                                    // tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                    //TRetInt = tDBCmd.ExecuteNonQuery();
                                    //if (TRetInt > 0)
                                    //{
                                    //    return true;
                                    //}
                                    //else
                                    //{
                                    //    return false;
                                    //}
                                    return true;
                                }
                            }
                        }
                    }
                    break;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_1:
                    {
                        if (TmpReturnStr.Length >= 230)
                            panelAyarKodu.Append(TmpReturnStr.Substring(0, 230));
                        else
                            panelAyarKodu.Append("");

                        return true;
                    }
                    break;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_2:
                    {
                        if (TmpReturnStr.Length >= 324)
                            kapiIlk8.Append(TmpReturnStr.Substring(10, 312));
                        else
                            kapiIlk8.Append("");
                        return true;
                    }
                    break;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_3:
                    {
                        if (TmpReturnStr.Length >= 324)
                            TmpReturnStr = panelAyarKodu + kapiIlk8.ToString() + TmpReturnStr.Substring(10, 312) + "**";
                        else
                            TmpReturnStr = "";

                        if (TmpReturnStr.Length >= 846)
                        {
                            if (mPanelModel == (int)PanelModel.Panel_1010)
                            {

                                int ID;
                                int NewID;
                                int PanelMACAddress;
                                int MasterRoleNo;
                                int[] LocalGateway = new int[4];
                                int[] LocalIPAddress = new int[4];
                                int LocalTCPPort;
                                int RemoteTCPPort;
                                int[] LocalSubnetMask = new int[4];
                                int[] RemoteIPAddress = new int[4];
                                int WKapi_Kapi_Tipi;
                                int WKapi_Kapi_Kontrol_Modu;
                                int Kontrol_Modu;
                                int WKapi_Kapi_Gecis_Modu;
                                int RS485_Reader_Type;
                                string LCD_Row_Message;
                                int WKapi_Keypad_Status;
                                int WKapi_Keypad_Menu_Password;
                                int RS485_Reader_Status;
                                int Wiegand_Reader_Status;
                                int Wiegand_Reader_Type;
                                int Mifare_Reader_Status;
                                int Mifare_Kart_Data_Type;
                                int UDP_Haberlesme;
                                int Multiple_Clock_Mode_Counter_Usage;
                                int Kart_ID_32_Bit_Clear;
                                int Pass_Counter_Auto_Delete_Cancel;
                                int Access_Counter_Kontrol;
                                int Turnstile_Arm_Tracking;
                                int Panel_Same_Tag_Block_HourMinSec;
                                int Mukkerrer_Engelleme_Suresi;


                                SI = 3;
                                PanelMACAddress = int.Parse(TmpReturnStr.Substring(SI, 4), NumberStyles.HexNumber);

                                SI = 7;
                                ID = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));

                                SI = 10;
                                MasterRoleNo = Convert.ToInt32(TmpReturnStr.Substring(SI, 2));

                                SI = 12;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                                    WKapi_Kapi_Tipi = 1;
                                else
                                    WKapi_Kapi_Tipi = 0;


                                SI = 13;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 3)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 3)) < 255)
                                {
                                    NewID = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                                }
                                else
                                {
                                    NewID = 0;
                                }

                                SI = 16;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    WKapi_Kapi_Kontrol_Modu = 0;
                                }
                                else
                                {
                                    WKapi_Kapi_Kontrol_Modu = 1;
                                }

                                SI = 17;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Kontrol_Modu = 0;
                                }
                                else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                                {
                                    Kontrol_Modu = 1;
                                }
                                else
                                {
                                    Kontrol_Modu = 2;
                                }

                                SI = 22;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    WKapi_Kapi_Gecis_Modu = 0;
                                }
                                else
                                {
                                    WKapi_Kapi_Gecis_Modu = 1;
                                }


                                SI = 23;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) > 0 && Convert.ToInt32(TmpReturnStr.Substring(SI, 2)) <= 99)
                                {
                                    Mukkerrer_Engelleme_Suresi = Convert.ToInt32(TmpReturnStr.Substring(SI, 2));
                                }
                                else
                                {
                                    Mukkerrer_Engelleme_Suresi = 0;
                                }

                                SI = 25;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    RS485_Reader_Type = 0;
                                }
                                else
                                {
                                    RS485_Reader_Type = 1;
                                }
                                SI = 26;
                                LCD_Row_Message = TmpReturnStr.Substring(SI, 16);

                                SI = 42;
                                LocalGateway[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                                LocalGateway[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                                LocalGateway[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                                LocalGateway[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                                SI = 54;
                                LocalIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                                LocalIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                                LocalIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                                LocalIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                                SI = 66;
                                LocalSubnetMask[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                                LocalSubnetMask[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                                LocalSubnetMask[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                                LocalSubnetMask[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                                SI = 78;
                                LocalTCPPort = Convert.ToInt32(TmpReturnStr.Substring(SI, 5));


                                SI = 83;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    WKapi_Keypad_Status = 0;
                                }
                                else
                                {
                                    WKapi_Keypad_Status = 1;
                                }

                                SI = 84;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 8)) != 0)
                                {
                                    WKapi_Keypad_Menu_Password = Convert.ToInt32(TmpReturnStr.Substring(SI, 8));
                                }
                                else
                                {
                                    WKapi_Keypad_Menu_Password = 1234;
                                }






                                SI = 92;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    RS485_Reader_Status = 0;
                                }
                                else
                                {
                                    RS485_Reader_Status = 1;
                                }

                                SI = 93;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Wiegand_Reader_Status = 0;
                                }
                                else
                                {
                                    Wiegand_Reader_Status = 1;
                                }

                                SI = 94;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Wiegand_Reader_Type = 0;
                                }
                                else
                                {
                                    Wiegand_Reader_Type = 1;
                                }
                                SI = 95;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Mifare_Reader_Status = 0;
                                }
                                else
                                {
                                    Mifare_Reader_Status = 1;
                                }

                                SI = 96;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Mifare_Kart_Data_Type = 0;
                                }
                                else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 1)
                                {
                                    Mifare_Kart_Data_Type = 1;
                                }
                                else if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 2)
                                {
                                    Mifare_Kart_Data_Type = 2;
                                }
                                else
                                {
                                    Mifare_Kart_Data_Type = 3;
                                }

                                SI = 97;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    UDP_Haberlesme = 0;
                                }
                                else
                                {
                                    UDP_Haberlesme = 1;
                                }

                                SI = 98;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Multiple_Clock_Mode_Counter_Usage = 0;
                                }
                                else
                                {
                                    Multiple_Clock_Mode_Counter_Usage = 1;
                                }

                                SI = 99;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Pass_Counter_Auto_Delete_Cancel = 0;
                                }
                                else
                                {
                                    Pass_Counter_Auto_Delete_Cancel = 1;
                                }

                                SI = 100;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Panel_Same_Tag_Block_HourMinSec = 0;
                                }
                                else
                                {
                                    Panel_Same_Tag_Block_HourMinSec = 1;
                                }

                                SI = 101;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Access_Counter_Kontrol = 0;
                                }
                                else
                                {
                                    Access_Counter_Kontrol = 1;
                                }

                                SI = 102;
                                RemoteIPAddress[0] = Convert.ToInt32(TmpReturnStr.Substring(SI, 3));
                                RemoteIPAddress[1] = Convert.ToInt32(TmpReturnStr.Substring(SI + 3, 3));
                                RemoteIPAddress[2] = Convert.ToInt32(TmpReturnStr.Substring(SI + 6, 3));
                                RemoteIPAddress[3] = Convert.ToInt32(TmpReturnStr.Substring(SI + 9, 3));

                                SI = 114;
                                RemoteTCPPort = Convert.ToInt32(TmpReturnStr.Substring(SI, 5));

                                SI = 119;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Kart_ID_32_Bit_Clear = 0;
                                }
                                else
                                {
                                    Kart_ID_32_Bit_Clear = 1;
                                }

                                SI = 120;
                                if (Convert.ToInt32(TmpReturnStr.Substring(SI, 1)) == 0)
                                {
                                    Turnstile_Arm_Tracking = 0;
                                }
                                else
                                {
                                    Turnstile_Arm_Tracking = 1;
                                }
                                if (TmpTaskUpdateTable)
                                {
                                    lock (TLockObj)
                                    {
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
                                            tDBSQLStr = "SELECT * FROM ReaderSettingsNewMS " +
                                                " WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                                " AND [Panel ID] = " + mPanelNo.ToString();
                                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                            tDBReader = tDBCmd.ExecuteReader();
                                            if (!tDBReader.Read())
                                            {
                                                tDBSQLStr2 = "INSERT INTO [ReaderSettingsNewMS]" +
               "([Seri No],[Sira No],[Panel ID],[Panel Name],[WKapi ID],[New Device ID],[WKapi Kapi Tipi],[WKapi Kapi Kontrol Modu]," +
                "[WKapi Kapi Gecis Modu],[RS485 Reader Type],[LCD Row Message],[WKapi Keypad Status],[WKapi Keypad Menu Password]," +
                "[RS485 Reader Status],[Wiegand Reader Status],[Wiegand Reader Type],[Mifare Reader Status],[Mifare Kart Data Type]," +
                "[UDP Haberlesme],[Multiple Clock Mode Counter Usage],[Kart ID 32 Bit Clear],[Pass Counter Auto Delete Cancel]," +
                "[Access Counter Kontrol],[Turnstile Arm Tracking])" +
                "VALUES(" +
                PanelMACAddress + "," + ID + "," + ID + ",'MAVISOFT MS-1010'," + 1 + "," + NewID + "," +
                "" + WKapi_Kapi_Tipi + "," + WKapi_Kapi_Kontrol_Modu + "," + WKapi_Kapi_Gecis_Modu + "," + RS485_Reader_Type + "," +
                "'" + LCD_Row_Message + "'," + WKapi_Keypad_Status + "," + WKapi_Keypad_Menu_Password + "," + RS485_Reader_Status + "," +
                "" + Wiegand_Reader_Status + "," + Wiegand_Reader_Type + "," + Mifare_Reader_Status + "," + Mifare_Kart_Data_Type + "," +
                "" + UDP_Haberlesme + "," + Multiple_Clock_Mode_Counter_Usage + "," + Kart_ID_32_Bit_Clear + "," +
                "" + Pass_Counter_Auto_Delete_Cancel + "," + Access_Counter_Kontrol + "," + Turnstile_Arm_Tracking + "";
                                            }
                                            else
                                            {
                                                tDBSQLStr2 = "UPDATE [ReaderSettingsNewMS]" +
                                             " SET " +
                                             "[Seri No] = " + PanelMACAddress + "," +
                                             "[Sira No] = " + ID + "," +
                                             "[Panel ID] = " + ID + "" +
                                             ",[Panel Name] = 'MAVISOFT MS-1010'," +
                                             "[WKapi ID] = " + 1 + "," +
                                             "[New Device ID] = " + NewID + "," +
                                             "[WKapi Kapi Tipi] = " + WKapi_Kapi_Tipi + "," +
                                             "[WKapi Kapi Kontrol Modu] = " + WKapi_Kapi_Kontrol_Modu + "," +
                                             "[WKapi Kapi Gecis Modu] = " + WKapi_Kapi_Gecis_Modu + "," +
                                             "[RS485 Reader Type] = " + RS485_Reader_Type + "," +
                                             "[LCD Row Message] = '" + LCD_Row_Message + "'," +
                                             "[WKapi Keypad Status] = " + WKapi_Keypad_Status + "," +
                                             "[WKapi Keypad Menu Password] = " + WKapi_Keypad_Menu_Password + "," +
                                             "[RS485 Reader Status] = " + RS485_Reader_Status + "," +
                                             "[Wiegand Reader Status] = " + Wiegand_Reader_Status + "," +
                                             "[Wiegand Reader Type] = " + Wiegand_Reader_Type + "," +
                                             "[Mifare Reader Status] = " + Mifare_Reader_Status + "," +
                                             "[Mifare Kart Data Type] = " + Mifare_Kart_Data_Type + "," +
                                             "[UDP Haberlesme] = " + UDP_Haberlesme + "," +
                                             "[Multiple Clock Mode Counter Usage] = " + Multiple_Clock_Mode_Counter_Usage + "," +
                                             "[Kart ID 32 Bit Clear] = " + Kart_ID_32_Bit_Clear + "," +
                                             "[Pass Counter Auto Delete Cancel] = " + Pass_Counter_Auto_Delete_Cancel + "," +
                                             "[Access Counter Kontrol] = " + Access_Counter_Kontrol + "," +
                                             "[Turnstile Arm Tracking] = " + Turnstile_Arm_Tracking + "" +
                                             " WHERE [Panel ID] = " + mPanelNo.ToString() +
                                             " AND [Seri No] = " + mPanelSerialNo.ToString();
                                            }
                                            tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                            TRetInt = tDBCmd2.ExecuteNonQuery();
                                            if (TRetInt <= 0)
                                            {
                                                panelAyarKodu.Clear();
                                                return false;
                                            }
                                        }
                                    }

                                    lock (TLockObj)
                                    {
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
                                            tDBSQLStr = "SELECT * FROM PanelSettings " +
                                                "WHERE [Seri No] = " + mPanelSerialNo.ToString() +
                                                "AND [Panel ID] = " + mPanelNo.ToString();
                                            tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                                            tDBReader = tDBCmd.ExecuteReader();
                                            if (tDBReader.Read())
                                            {
                                                tDBSQLStr2 = "UPDATE PanelSettings " +
                                                    "SET " +
                                                    "[Panel M1 Role] = " + MasterRoleNo + "," +
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
                                                    "[Panel Same Tag Block HourMinSec] = " + Panel_Same_Tag_Block_HourMinSec +
                                                    " WHERE [Panel ID] = " + mPanelNo.ToString() +
                                                    " AND [Seri No] = " + PanelMACAddress;
                                                tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                                                TRetInt = tDBCmd2.ExecuteNonQuery();
                                                if (TRetInt <= 0)
                                                {
                                                    panelAyarKodu.Clear();
                                                    return false;
                                                }
                                                else
                                                {
                                                    panelAyarKodu.Clear();
                                                    return true;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                int ID;
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
                                int[] PanelLocalAPBs = new int[8];
                                int[] PanelMasterRelayTime = new int[8];
                                int PanelAlarmRelayTime;
                                int PanelMACAddress;
                                int PanelGlobalAPB;

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
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
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
                                                panelAyarKodu.Clear();
                                                kapiIlk8.Clear();
                                                kapiSon8.Clear();
                                                return false;
                                            }
                                        }
                                    }

                                    lock (TLockObj)
                                    {
                                        using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                                        {
                                            mDBConn.Open();
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
                                                panelAyarKodu.Clear();
                                                kapiIlk8.Clear();
                                                kapiSon8.Clear();
                                                return true;
                                            }
                                            else
                                            {
                                                panelAyarKodu.Clear();
                                                kapiIlk8.Clear();
                                                kapiSon8.Clear();
                                                return false;
                                            }
                                        }
                                    }

                                }
                            }
                        }
                        else
                        {
                            panelAyarKodu.Clear();
                            kapiIlk8.Clear();
                            kapiSon8.Clear();
                            return false;
                        }
                    }
                    break;
                default:
                    break;
            }
            return false;
        }

        /// <summary>
        /// Veritabanında kullanıcı ID'sine göre Kart ID bulma
        /// </summary>
        /// <param name="TempUser"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Geçiş verileri ekleme işlemlerinde kullanıcının lokal bölgesini bulma
        /// </summary>
        /// <param name="MacSerial"></param>
        /// <param name="Reader"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Geçiş verileri ekleme işlemlerinde kullanıcının global bölgesini bulma
        /// </summary>
        /// <param name="MacSerial"></param>
        /// <param name="LokalBolgeNo"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Kullanıcının Kart ID değerinin 0'lardan arındırılması
        /// </summary>
        /// <param name="CardID"></param>
        /// <returns></returns>
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

        /// <summary>
        /// Olay Geçiş Hafızası Alındıktan Sonra Panel Hafızası Silme Görevi Oluşturuyor.
        /// </summary>
        /// <param name="KayitNo">Görev tablosunda ki başarılı olan görevin kayıt numarası</param>
        public void CheckDeleteAfterReceiving(int KayitNo)
        {
            object TLockObj = new object();
            int TDataInt;
            string tDBSQLStr = "";
            string tDBSQLStr2 = "";
            string tDBSQLStr3 = "";
            SqlCommand tDBCmd;
            SqlCommand tDBCmd2;
            SqlCommand tDBCmd3;
            SqlDataReader tDBReader;
            SqlDataReader tDBReader2;
            bool DeleteAfterRcv = false;
            int DurumKodu = 0;
            int GorevKodu = 0;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT TOP 1 [DeleteAfterReceiving] FROM ProgInit";
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        DeleteAfterRcv = tDBReader[0] as bool? ?? default(bool);
                    }
                    if (DeleteAfterRcv == true)
                    {
                        tDBSQLStr2 = "SELECT TOP 1 [Durum Kodu],[Gorev Kodu] FROM TaskList WHERE [Kayit No] = " + KayitNo;
                        tDBCmd2 = new SqlCommand(tDBSQLStr2, mDBConn);
                        tDBReader2 = tDBCmd2.ExecuteReader();
                        if (tDBReader2.Read())
                        {
                            DurumKodu = tDBReader2[0] as int? ?? default(int);
                            GorevKodu = tDBReader2[1] as int? ?? default(int);
                        }
                        if (GorevKodu == (int)CommandConstants.CMD_RCV_LOGS && DurumKodu == 2)
                        {
                            tDBSQLStr3 += "INSERT INTO TaskList ([Gorev Kodu], [IntParam 1], [Panel No], [Durum Kodu], Tarih, [Kullanici Adi], [Tablo Guncelle])" +
                          " VALUES(" +
                           (int)CommandConstants.CMD_ERS_LOGCOUNT + "," + 1 + "," + mPanelNo + "," + 1 + "," + "'" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "'," + "'System'," + 0 + ") ";

                            tDBCmd3 = new SqlCommand(tDBSQLStr3, mDBConn);
                            TDataInt = tDBCmd3.ExecuteNonQuery();
                        }

                    }
                }
            }

        }

        /// <summary>
        /// Görev Tipine Göre Komut Prefixi Getiriyor.
        /// </summary>
        /// <param name="DBTaskType"></param>
        /// <returns></returns>
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
                case (ushort)CommandConstants.CMD_RCV_LOGS:
                    return "DD";
                case (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_1:
                    return "D0";
                case (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_2:
                    return "D1";
                case (ushort)CommandConstants.CMD_SND_GENERALSETTINGS_3:
                    return "D2";
                case (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_1:
                    return "U0";
                case (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_2:
                    return "U1";
                case (ushort)CommandConstants.CMD_RCV_GENERALSETTINGS_3:
                    return "U2";
                default:
                    return "ERR";
            }
        }

        /// <summary>
        /// Görev Tipine Göre Beklenen Cevap Boyutu Dönderiyor.
        /// </summary>
        /// <param name="TmpTaskType"></param>
        /// <returns></returns>
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
                    {
                        if (mPanelModel == (int)PanelModel.Panel_1010)
                            return (int)SizeConstants.SIZE_RCV_DEVICE_SETTINGS_MS1010;
                        else
                            return (int)SizeConstants.SIZE_SEND_DEVICE_SETTINGS;
                    }
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
                    return (int)SizeConstants.SIZE_RCV_LOGS;
                case CommandConstants.CMD_SND_GENERALSETTINGS_1:
                case CommandConstants.CMD_SND_GENERALSETTINGS_2:
                case CommandConstants.CMD_SND_GENERALSETTINGS_3:
                    return (int)SizeConstants.SIZE_STANDART_ANSWER;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_1:
                    return (int)SizeConstants.SIZE_RCV_GENELSETTINGS_1;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_2:
                    return (int)SizeConstants.SIZE_RCV_GENELSETTINGS_2;
                case CommandConstants.CMD_RCV_GENERALSETTINGS_3:
                    return (int)SizeConstants.SIZE_RCV_GENELSETTINGS_3;
                default:
                    return 0;
            }
        }

        /// <summary>
        /// Ekranda ki Label Text'lerini Güncelleme
        /// </summary>
        /// <param name="TMsg"></param>
        /// <param name="color"></param>
        delegate void TextDegisDelegate(string TMsg, System.Drawing.Color color);
        public void SyncUpdateScreen(string TMsg, System.Drawing.Color color)
        {
            Thread.Sleep(20);
            object frmMainLock = new object();
            lock (frmMainLock)
            {
                if (mParentForm.lblMsj[mMemIX].InvokeRequired == true)
                {
                    TextDegisDelegate del = new TextDegisDelegate(SyncUpdateScreen);
                    mParentForm.Invoke(del, new object[] { TMsg, color });
                }
                else
                {
                    if (TMsg != mParentForm.lblMsj[mMemIX].Text)
                    {
                        mParentForm.lblMsj[mMemIX].Text = TMsg;
                        mParentForm.lblMsj[mMemIX].BackColor = color;
                    }
                }
            }
        }

        /// <summary>
        /// Görev Tipine Göre String Mesaj Dönderiyor
        /// </summary>
        /// <param name="TmpTaskType"></param>
        /// <returns></returns>
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
                case CommandConstants.CMD_SND_GENERALSETTINGS_1:
                    return "GENEL AYARLARI GÖNDER-1";
                case CommandConstants.CMD_SND_GENERALSETTINGS_2:
                    return "GENEL AYARLARI GÖNDER-2";
                case CommandConstants.CMD_SND_GENERALSETTINGS_3:
                    return "GENEL AYARLARI GÖNDER-3";
                case CommandConstants.CMD_RCV_GENERALSETTINGS_1:
                    return "GENEL AYARLARI AL-1";
                case CommandConstants.CMD_RCV_GENERALSETTINGS_2:
                    return "GENEL AYARLARI AL-2";
                case CommandConstants.CMD_RCV_GENERALSETTINGS_3:
                    return "GENEL AYARLARI AL-3";
                default:
                    return "BILINMEYEN İŞLEM";
            }
        }

        /// <summary>
        /// Var olan görevler dışında bir görev gelmesi durumunda görevi 'HATA' olarak güncelliyor.
        /// </summary>
        /// <param name="TaskNo">Görev Listesinde ki Kayit No</param>
        /// <returns></returns>
        public bool NoTaskDelete(int TaskNo)
        {
            object TLockObj = new object();
            string tDBSQLStr = "";
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            string tDBSQLStr2 = "";
            SqlCommand tDBCmd2;
            int TRetInt;
            lock (TLockObj)
            {
                using (mDBConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    mDBConn.Open();
                    tDBSQLStr = "SELECT * FROM TaskList WHERE [Kayit No] = " + TaskNo;
                    tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                    tDBReader = tDBCmd.ExecuteReader();
                    if (tDBReader.Read())
                    {
                        tDBSQLStr2 = "UPDATE TaskList SET [Durum Kodu] = " + 3 + " WHERE [Kayit No] = " + TaskNo;
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
                    else
                    {
                        return true;
                    }
                }
            }
        }

        /// <summary>
        /// Mail gönderme rutini
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
        /// Veritabanından mail ayarlarını alma
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

        /********************Kendi Convert Metotlarım********************Kendi Convert Metotlarım********************/
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

        public string ConvertToTypeInt64(long reader, string Type)
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

        public bool ClearSocketBuffers(TcpClient TClient, int? Size)
        {
            byte[] DummyBuffer;
            StringBuilder sBuilder = new StringBuilder();
            int TSize;
            try
            {
                if (TClient.Available > 0)
                {
                    var netStream = TClient.GetStream();
                    if (netStream.CanRead)
                    {
                        if (Size == null)
                        {
                            TSize = TClient.Available;
                        }
                        else
                        {
                            TSize = (int)Size;
                        }

                        DummyBuffer = new byte[TSize];
                        netStream.Read(DummyBuffer, 0, TSize);
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

        }//Görev Listesinden Silme İşlemi Yapıyor IP Tasklar İçin

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


        /// <summary>
        /// MS1010 Cihazlar İçin Ekranda Gösterilecek Ad ve Soyadı Türkçe Karakterlerden Arındırmak.
        /// </summary>
        /// <param name="adSoyad">Ad ve Soyadın Birleştirilmiş Hali</param>
        /// <returns></returns>
        public string ConvertNameSurname(string adSoyad)
        {
            adSoyad = adSoyad.Replace('ö', 'o');
            adSoyad = adSoyad.Replace('ü', 'u');
            adSoyad = adSoyad.Replace('ğ', 'g');
            adSoyad = adSoyad.Replace('ş', 's');
            adSoyad = adSoyad.Replace('ı', 'i');
            adSoyad = adSoyad.Replace('ç', 'c');
            adSoyad = adSoyad.Replace('Ö', 'O');
            adSoyad = adSoyad.Replace('Ü', 'U');
            adSoyad = adSoyad.Replace('Ğ', 'G');
            adSoyad = adSoyad.Replace('Ş', 'S');
            adSoyad = adSoyad.Replace('İ', 'I');
            adSoyad = adSoyad.Replace('Ç', 'C');

            return adSoyad;
        }

        public string ConvertTurkceKarekter(string karakter)
        {
            karakter = karakter.Replace('ö', 'o');
            karakter = karakter.Replace('ü', 'u');
            karakter = karakter.Replace('ğ', 'g');
            karakter = karakter.Replace('ş', 's');
            karakter = karakter.Replace('ı', 'i');
            karakter = karakter.Replace('ç', 'c');
            karakter = karakter.Replace('Ö', 'O');
            karakter = karakter.Replace('Ü', 'U');
            karakter = karakter.Replace('Ğ', 'G');
            karakter = karakter.Replace('Ş', 'S');
            karakter = karakter.Replace('İ', 'I');
            karakter = karakter.Replace('Ç', 'C');

            return karakter;
        }



    }
}
