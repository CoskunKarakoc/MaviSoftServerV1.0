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
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

        }

        public SqlConnection MConn { get; set; }

        public string SQLStr { get; set; }

        public SqlCommand Comnd { get; set; }

        public SqlDataReader MReader { get; set; }


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

        public Label lbl;

        public Label denemeLab;

        public S_PORTS[] SPorts = new S_PORTS[201];

        public Panel[] Panels = new Panel[201];
        public PanelLog[] LogPanels = new PanelLog[201];

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
                lblIP[i].Location = new Point((10 + (TTC * 385)), (18 + (TLC * 15)));
                lblIP[i].Size = new Size(165, 16);
                lblIP[i].Font = new Font("Arial Narrow TUR", 7.0F);
                lblIP[i].Text = "";// "Mesaj";
                lblIP[i].TextAlign = ContentAlignment.MiddleCenter;
                lblIP[i].Visible = true;
                Controls.Add(lblIP[i]);

                // MESSAGES
                lblMsj[i] = new Label();
                lblMsj[i].AutoSize = false;
                lblMsj[i].BorderStyle = BorderStyle.FixedSingle;
                lblMsj[i].Location = new Point((174 + (TTC * 385)), (18 + (TLC * 15)));
                lblMsj[i].Size = new Size(165, 16);
                lblMsj[i].Font = new Font("Arial Narrow TUR", 7.0F);
                lblMsj[i].Text = "";// "Mesaj";
                lblMsj[i].TextAlign = ContentAlignment.MiddleCenter;
                lblMsj[i].Visible = true;
                Controls.Add(lblMsj[i]);
            }

            MConn = new SqlConnection();
            MConn.ConnectionString = @"data source = ARGE-2\SQLEXPRESS; initial catalog = MW301_DB25; integrated security = True; MultipleActiveResultSets = True;";
            SQLStr = "SELECT * FROM PanelSettings ORDER BY [Sira No]";
            try
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

                            lblIP[i].Text = SPorts[i].PanelNo.ToString() + " :: " + SPorts[i].IPAdress;
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
                        LogPanels[j] = new PanelLog(j, SPorts[j].Active, SPorts[j].PanelNo, SPorts[j].SndRcvTimeout, SPorts[j].IPAdress, SPorts[j].MACAddress, SPorts[j].TCPPortNo, 11010, this);
                        LogPanels[j].StartPanel();
                    }
                }

            }
            catch (Exception)
            {
                MessageBox.Show("SQL Server'a bağlantı kurulamadı!, Program kapatılacak - MAVİSOFT SERVER V1.0");
            }
        }
    }
}
