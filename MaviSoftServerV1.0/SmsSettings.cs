using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public class SmsSettings
    {

        public SmsSettings()
        {
            BindProperty();
        }


        public string Kullanici_Adi { get; set; }

        public string Sifre { get; set; }

        public string Originator { get; set; }

        public bool? Gelmeyenler_Gonder { get; set; }

        public string Gelmeyenler_Mesaj { get; set; }

        public DateTime? Gelmeyenler_Saat { get; set; }

        public int? Gelmeyenler_Global_Bolge { get; set; }

        public bool? Icerde_Disarda_Gonder { get; set; }

        public string Icerde_Mesaj { get; set; }

        public string Disarda_Mesaj { get; set; }

        public DateTime? Icerde_Disarda_Saat { get; set; }

        public int? Icerde_Disarda_Global_Bolge { get; set; }

        public bool? Her_Giris_Cikista_Gonder { get; set; }

        public string Her_Giris_Cikista_Mesaj { get; set; }

        public bool? PanelBaglantiDurumu_Gonder { get; set; }

        private void BindProperty()
        {
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            lock (TLockObj)
            {
                using (var connection = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        connection.Open();
                        tDBSQLStr = "SELECT TOP 1 * FROM SmsSettings";
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        if (tDBReader.Read())
                        {
                            Kullanici_Adi = tDBReader["Kullanici Adi"].ToString();
                            Sifre = tDBReader["Sifre"].ToString();
                            Originator = tDBReader["Originator"].ToString();
                            Gelmeyenler_Gonder = tDBReader["Gelmeyenler Gonder"] as bool? ?? default(bool);
                            Gelmeyenler_Mesaj = tDBReader["Gelmeyenler Mesaj"].ToString();
                            Gelmeyenler_Saat = tDBReader["Gelmeyenler Saat"] as DateTime? ?? default(DateTime);
                            Gelmeyenler_Global_Bolge = tDBReader["Gelmeyenler Global Bolge"] as int? ?? default(int);
                            Icerde_Disarda_Gonder = tDBReader["IcerdeDisarda Gonder"] as bool? ?? default(bool);
                            Icerde_Mesaj = tDBReader["Icerde Mesaj"].ToString();
                            Disarda_Mesaj = tDBReader["Disarda MesaJ"].ToString();
                            Icerde_Disarda_Saat = tDBReader["IcerdeDisarda Saat"] as DateTime? ?? default(DateTime);
                            Icerde_Disarda_Global_Bolge = tDBReader["IcerdeDisarda Global Bolge"] as int? ?? default(int);
                            Her_Giris_Cikista_Gonder = tDBReader["HerGirisCikista Gonder"] as bool? ?? default(bool);
                            Her_Giris_Cikista_Mesaj = tDBReader["HerGirisCikista Mesaj"].ToString();
                            PanelBaglantiDurumu_Gonder = tDBReader["PanelBaglantiDurumu Gonder"] as bool? ?? default(bool);
                        }

                        tDBReader.Close();
                    }
                    catch (Exception)
                    {
                        Kullanici_Adi = null;
                        Sifre = null;
                        Originator = null;
                        Gelmeyenler_Gonder = null;
                        Gelmeyenler_Mesaj = null;
                        Gelmeyenler_Saat = null;
                        Gelmeyenler_Global_Bolge = null;
                        Icerde_Disarda_Gonder = null;
                        Icerde_Mesaj = null;
                        Disarda_Mesaj = null;
                        Icerde_Disarda_Saat = null;
                        Icerde_Disarda_Global_Bolge = null;
                        Her_Giris_Cikista_Gonder = null;
                        Her_Giris_Cikista_Mesaj = null;
                        PanelBaglantiDurumu_Gonder = null;
                    }
                }
            }
        }





    }
}
