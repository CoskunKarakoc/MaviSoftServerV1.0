using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SqlClient;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MaviSoftServerV1._0
{
    public partial class FrmMain : Form
    {
        public SqlConnection MConn { get; set; }

        public List<Panel> AktifPanelListesi;

        public List<PanelLog> AktifPanelLogListesi;

        public string SQLStr { get; set; }

        public SqlCommand Comnd { get; set; }

        public SqlDataReader MReader { get; set; }

        public FrmMain()
        {
            InitializeComponent();
            AktifPanelListesi = new List<Panel>();
            AktifPanelLogListesi = new List<PanelLog>();
        }


        public struct S_PORTS
        {
            public ushort Active;
            public int TCPPortNo;
            public string IPAdress;
            public int MACAddress;
            public ushort ConnTimeout;
            public ushort SndRcvTimeout;
            public int PanelNo;
        }

        public Label[] lblIP = new Label[201];

        public Label[] lblMsj = new Label[201];

        public Label[] lblMsjLog = new Label[201];

        public Label lbl;

        public Label denemeLab;

        public S_PORTS[] SPorts = new S_PORTS[201];

        public Panel[] Panels = new Panel[201];

        public PanelLog[] LogPanels = new PanelLog[201];

        public SystemManager PanelOuther;

        public Queue<KeyValuePair<int, string>> APBList = new Queue<KeyValuePair<int, string>>();

        private void Form1_Load(object sender, EventArgs e)
        {

            int TTC = 0;
            int TLC = 0;
            for (int i = 0; i < (int)TCONST.MAX_PANEL; i++)
            {
                TLC = (i % 50);
                TTC = (i / 50);

                // PANEL IPs
                lblIP[i] = new Label();
                lblIP[i].AutoSize = false;
                lblIP[i].BorderStyle = BorderStyle.FixedSingle;
                lblIP[i].Location = new Point((10 + (TTC * 381)), (18 + (TLC * 15)));
                lblIP[i].Size = new Size(165, 16); lblIP[i].Size = new Size(140, 16);
                lblIP[i].Font = new Font("Arial Narrow TUR", 7.0F); lblIP[i].Font = new Font("Arial TUR", 7.0F);
                lblIP[i].Text = "";// "Mesaj";
                lblIP[i].TextAlign = ContentAlignment.MiddleCenter;
                lblIP[i].Visible = true;
                Controls.Add(lblIP[i]);

                // MESSAGES
                lblMsj[i] = new Label();
                lblMsj[i].AutoSize = false;
                lblMsj[i].BorderStyle = BorderStyle.FixedSingle;
                lblMsj[i].Location = new Point((149 + (TTC * 381)), (18 + (TLC * 15)));
                lblMsj[i].Size = new Size(165, 16); lblMsj[i].Size = new Size(110, 16);
                lblMsj[i].Font = new Font("Arial Narrow TUR", 7.0F); lblMsj[i].Font = new Font("Arial TUR", 7.0F);
                lblMsj[i].Text = "";// "Mesaj";
                lblMsj[i].TextAlign = ContentAlignment.MiddleCenter;
                lblMsj[i].Visible = true;
                Controls.Add(lblMsj[i]);


                // MESSAGES LOG
                lblMsjLog[i] = new Label();
                lblMsjLog[i].AutoSize = false;
                lblMsjLog[i].BorderStyle = BorderStyle.FixedSingle;
                lblMsjLog[i].Location = new Point((258 + (TTC * 381)), (18 + (TLC * 15)));
                lblMsjLog[i].Size = new Size(110, 16);
                lblMsjLog[i].Font = new Font("Arial TUR", 7.0F);
                lblMsjLog[i].Text = "";// "Mesaj";
                lblMsjLog[i].TextAlign = ContentAlignment.MiddleCenter;
                lblMsjLog[i].Visible = true;
                Controls.Add(lblMsjLog[i]);

            }

            //MConn.ConnectionString = SqlServerAdress.GetAdress();
            SQLStr = "SELECT * FROM PanelSettings ORDER BY [Sira No]";
            try
            {


                using (MConn = new SqlConnection(SqlServerAdress.Adres))
                {
                    MConn.Open();

                    Comnd = new SqlCommand(SQLStr, MConn);
                    MReader = Comnd.ExecuteReader();

                    int i = 0;
                    while (MReader.Read())
                    {
                        if (i <= (int)TCONST.MAX_PANEL)
                        {
                            if (MReader["Panel ID"] as int? != 0 && MReader["Panel IP1"].ToString() != "" && MReader["Panel IP2"].ToString() != "")
                            {
                                SPorts[i].Active = 1;
                                SPorts[i].PanelNo = MReader["Panel ID"] as int? ?? default(int);
                                SPorts[i].IPAdress = MReader["Panel IP1"].ToString().Trim() + "." + MReader["Panel IP2"].ToString().Trim() + "." + MReader["Panel IP3"].ToString().Trim() + "." + MReader["Panel IP4"].ToString().Trim();
                                SPorts[i].TCPPortNo = MReader["Panel TCP Port"] as int? ?? default(int);
                                SPorts[i].MACAddress = MReader["Seri No"] as int? ?? default(int);
                                SPorts[i].ConnTimeout = 3;
                                SPorts[i].SndRcvTimeout = 3;

                                lblIP[i].Text = SPorts[i].PanelNo.ToString() + ">" + SPorts[i].MACAddress.ToString("X4") + ">" + SPorts[i].IPAdress;
                            }
                            else
                            {
                                SPorts[i].Active = 0;
                            }
                        }
                        i++;
                    }

                    for (ushort j = 0; j <= (ushort)TCONST.MAX_PANEL; j++)
                    {
                        if (SPorts[j].Active == 1)
                        {
                            Panels[j] = new Panel(j, SPorts[j].Active, SPorts[j].PanelNo, SPorts[j].SndRcvTimeout, SPorts[j].IPAdress, SPorts[j].MACAddress, SPorts[j].TCPPortNo, 11010, this);
                            Panels[j].StartPanel();
                            AktifPanelListesi.Add(Panels[j]);
                            LogPanels[j] = new PanelLog(j, SPorts[j].Active, SPorts[j].PanelNo, SPorts[j].SndRcvTimeout, SPorts[j].IPAdress, SPorts[j].MACAddress, SPorts[j].TCPPortNo, 11010, AktifPanelListesi, this);
                            LogPanels[j].StartPanel();
                            AktifPanelLogListesi.Add(LogPanels[j]);
                        }
                    }
                    foreach (var logPanel in LogPanels)
                    {
                        if (logPanel != null)
                        {
                            foreach (var panel in LogPanels)
                            {
                                if (panel != null)
                                {
                                    logPanel.LogPanelListesi.Add(panel);
                                }
                            }
                        }
                    }

                    PanelOuther = new SystemManager(AktifPanelListesi, AktifPanelLogListesi, this);
                    PanelOuther.StartPanelOuther();
                }
            }
            catch (Exception)
            {
                MessageBox.Show("SQL Server'a bağlantı kurulamadı!, Program kapatılacak - MAVİSOFT SERVER V1.0");
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            foreach (var panel in AktifPanelListesi)
            {
                panel.StopPanel();
            }
            foreach (var logPanel in AktifPanelLogListesi)
            {
                logPanel.StopPanel();
            }
            Environment.Exit(0);
        }


    }
}
