using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public class SendSms
    {
        private SmsSettings _smsSettings;
        public SendSms(SmsSettings smsSettings)
        {
            _smsSettings = smsSettings;
        }

        public void HerGirisCikistaMesajGonder(string Kart_ID, long User_ID, int Gecis_Tipi = 0)
        {
            var messageUser = GetUserWithKartIDAndUserID(Kart_ID, User_ID);
            if (_smsSettings.Her_Giris_Cikista_Gonder == true && Gecis_Tipi == 0)
            {

                var XMLCode = SmsOperationCode.GetSmsXMLCodeOneToMany(_smsSettings.Kullanici_Adi, _smsSettings.Sifre, "", "", _smsSettings.Originator, _smsSettings.Her_Giris_Cikista_Mesaj, new List<string> { messageUser.Telefon });

            }
            else if (_smsSettings.Her_Giris_Cikista_Gonder == true && Gecis_Tipi == 1)
            {
                var XMLCode = SmsOperationCode.GetSmsXMLCodeOneToMany(_smsSettings.Kullanici_Adi, _smsSettings.Sifre, "", "", _smsSettings.Originator, _smsSettings.Her_Giris_Cikista_Mesaj, new List<string> { messageUser.Telefon });
            }
        }


        public void IcerdeDisardaRaporMesajıGonder()
        {
            if (_smsSettings.Icerde_Disarda_Gonder == true && IcerdekiKullaniciListesi().Count > 0)
            {
                var XMLCode = SmsOperationCode.GetSmsXMLCodeOneToMany(_smsSettings.Kullanici_Adi, _smsSettings.Sifre, "", "", _smsSettings.Originator, _smsSettings.Gelmeyenler_Mesaj, IcerdekiKullaniciListesi());
            }
        }


        public void GelmeyenMesajiGonder()
        {
            if (_smsSettings.Gelmeyenler_Gonder == true && GelmeyenKullaniListesi().Count > 0)
            {
                var XMLCode = SmsOperationCode.GetSmsXMLCodeOneToMany(_smsSettings.Kullanici_Adi, _smsSettings.Sifre, "", "", _smsSettings.Originator, _smsSettings.Gelmeyenler_Mesaj, GelmeyenKullaniListesi());
            }
        }


        public void PanelBaglantiDurumu(string Mesaj)
        {
            if (_smsSettings.PanelBaglantiDurumu_Gonder == true && PanelBaglatiSMSListesi().Count > 0)
            {
                var XMLCode = SmsOperationCode.GetSmsXMLCodeOneToMany(_smsSettings.Kullanici_Adi, _smsSettings.Sifre, "", "", _smsSettings.Originator, Mesaj, PanelBaglatiSMSListesi());
            }
        }


        private List<SmsUserEntity> GetUser()
        {
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<SmsUserEntity> smsUsers = new List<SmsUserEntity>();
            lock (TLockObj)
            {
                using (var connection = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        connection.Open();
                        tDBSQLStr = "SELECT * FROM Users WHERE Telefon IS NOT NULL";
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            var entity = new SmsUserEntity
                            {
                                Adi = tDBReader["Adi"].ToString(),
                                Soyadi = tDBReader["Soyadi"].ToString(),
                                Telefon = tDBReader["Telefon"].ToString()
                            };

                            if (entity.Telefon.Length > 11)
                            {
                                entity.Telefon = entity.Telefon.Substring(0, 11);
                            }
                            else if (entity.Telefon.Length < 11)
                            {
                                string temp = "";
                                for (int i = 0; i < (11 - entity.Telefon.Length); i++)
                                {
                                    temp += "0";
                                }
                                entity.Telefon += temp;
                            }

                            var CC = "9" + entity.Telefon;
                            entity.Telefon = CC;

                            smsUsers.Add(entity);
                        }

                        tDBReader.Close();
                    }
                    catch (Exception)
                    {
                        smsUsers = null;
                    }
                }
                return smsUsers;
            }

        }

        private SmsUserEntity GetUserWithKartIDAndUserID(string KartID, long UserID)
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
                        tDBSQLStr = "SELECT * FROM Users WHERE [Kart ID]='" + KartID + "' AND ID=" + UserID;
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        if (tDBReader.Read())
                        {
                            var entity = new SmsUserEntity
                            {
                                Adi = tDBReader["Adi"].ToString(),
                                Soyadi = tDBReader["Soyadi"].ToString(),
                                Telefon = tDBReader["Telefon"].ToString()
                            };
                            tDBReader.Close();

                            if (entity.Telefon.Length > 11)
                            {
                                entity.Telefon = entity.Telefon.Substring(0, 11);
                            }
                            else if (entity.Telefon.Length < 11)
                            {
                                string temp = "";
                                for (int i = 0; i < (11 - entity.Telefon.Length); i++)
                                {
                                    temp += "0";
                                }
                                entity.Telefon += temp;
                            }

                            var CC = "9" + entity.Telefon;
                            entity.Telefon = CC;

                            return entity;
                        }
                        return new SmsUserEntity();
                    }
                    catch (Exception)
                    {
                        return new SmsUserEntity();
                    }
                }

            }

        }

        private List<string> GelmeyenKullaniListesi()
        {
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<SmsUserEntity> smsUsers = new List<SmsUserEntity>();
            List<string> telefonListesi = new List<string>();
            lock (TLockObj)
            {
                using (var connection = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        connection.Open();
                        tDBSQLStr = @"  SELECT Users.Adi, Users.Soyadi, Users.Telefon 
                                         FROM Users
                                         WHERE Users.ID > 0 AND Telefon IS NOT NULL
                                         AND Users.ID <> ALL (SELECT DISTINCT AccessDatas.ID
                                         FROM AccessDatas 
                                         WHERE AccessDatas.[Kullanici Tipi] = 0 
                                         AND AccessDatas.Kod = 1";
                        tDBSQLStr += " AND AccessDatas.Tarih >= CONVERT(SMALLDATETIME,'" + DateTime.Now.Date.AddSeconds(1).ToString("dd/MM/yyyy HH:mm:ss") + "',103) ";
                        tDBSQLStr += " AND AccessDatas.Tarih <= CONVERT(SMALLDATETIME,'" + DateTime.Now.Date.AddHours(23).AddMinutes(59).AddSeconds(59).ToString("dd/MM/yyyy HH:mm:ss") + "',103)";
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            var entity = new SmsUserEntity
                            {
                                Adi = tDBReader[0].ToString(),
                                Soyadi = tDBReader[1].ToString(),
                                Telefon = tDBReader[2].ToString()
                            };

                            if (entity.Telefon.Length > 11)
                            {
                                entity.Telefon = entity.Telefon.Substring(0, 11);
                            }
                            else if (entity.Telefon.Length < 11)
                            {
                                string temp = "";
                                for (int i = 0; i < (11 - entity.Telefon.Length); i++)
                                {
                                    temp += "0";
                                }
                                entity.Telefon += temp;
                            }

                            var CC = "9" + entity.Telefon;
                            entity.Telefon = CC;
                            telefonListesi.Add(entity.Telefon);
                            smsUsers.Add(entity);
                        }
                        tDBReader.Close();
                    }
                    catch (Exception)
                    {
                        smsUsers = null;
                    }
                    return telefonListesi;
                }
            }
        }

        private List<string> IcerdekiKullaniciListesi()
        {
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<SmsUserEntity> smsUsers = new List<SmsUserEntity>();
            List<string> telefonListesi = new List<string>();
            lock (TLockObj)
            {
                using (var connection = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        connection.Open();
                        tDBSQLStr = @"SELECT TTT.Adi, TTT.Soyadi,TTT.Telefon
                                        FROM AccessDatas
                                        INNER JOIN  (SELECT AccessDatas.[User Kayit No], Users.Adi, Users.Soyadi,Users.Telefon , Sirketler.Adi AS Sirket, Departmanlar.Adi AS Departman, MAX(AccessDatas.[Tarih]) AS Tarih  FROM Users
                                        LEFT OUTER JOIN AccessDatas  ON Users.[Kayit No] = AccessDatas.[User Kayit No]  LEFT OUTER JOIN Sirketler  ON Users.[Sirket No] = Sirketler.[Sirket No]
                                        LEFT OUTER JOIN Departmanlar  ON Users.[Departman No] = Departmanlar.[Departman No]
                                        WHERE AccessDatas.[Lokal Bolge No] = 1 AND AccessDatas.[Kullanici Tipi] = 0 AND AccessDatas.ID > 0  AND AccessDatas.Kod = 1 AND Users.Telefon IS NOT NULL
                                        GROUP BY AccessDatas.[User Kayit No], Users.Adi, Users.Soyadi,Users.Telefon, Sirketler.Adi, Departmanlar.Adi) TTT ON AccessDatas.[User Kayit No] = TTT.[User Kayit No] AND AccessDatas.Tarih = TTT.Tarih
                                        WHERE AccessDatas.[Gecis Tipi] = 0";
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            var entity = new SmsUserEntity
                            {
                                Adi = tDBReader[0].ToString(),
                                Soyadi = tDBReader[1].ToString(),
                                Telefon = tDBReader[2].ToString()
                            };

                            if (entity.Telefon.Length > 11)
                            {
                                entity.Telefon = entity.Telefon.Substring(0, 11);
                            }
                            else if (entity.Telefon.Length < 11)
                            {
                                string temp = "";
                                for (int i = 0; i < (11 - entity.Telefon.Length); i++)
                                {
                                    temp += "0";
                                }
                                entity.Telefon += temp;
                            }
                            var CC = "9" + entity.Telefon;
                            entity.Telefon = CC;
                            telefonListesi.Add(entity.Telefon);
                            smsUsers.Add(entity);
                        }
                        tDBReader.Close();
                    }
                    catch (Exception)
                    {
                        smsUsers = null;
                    }
                    return telefonListesi;
                }
            }
        }

        private List<string> PanelBaglatiSMSListesi()
        {
            object TLockObj = new object();
            string tDBSQLStr;
            SqlCommand tDBCmd;
            SqlDataReader tDBReader;
            List<string> telefonListesi = new List<string>();
            lock (TLockObj)
            {
                using (var connection = new SqlConnection(SqlServerAdress.Adres))
                {

                    try
                    {
                        connection.Open();
                        tDBSQLStr = @"SELECT * FROM SMSForPanelStatus";
                        tDBCmd = new SqlCommand(tDBSQLStr, connection);
                        tDBReader = tDBCmd.ExecuteReader();
                        while (tDBReader.Read())
                        {
                            var telNo = tDBReader[1].ToString();

                            if (telNo.Length > 11)
                            {
                                telNo = telNo.Substring(0, 11);
                            }
                            else if (telNo.Length < 11)
                            {
                                string temp = "";
                                for (int i = 0; i < (11 - telNo.Length); i++)
                                {
                                    temp += "0";
                                }
                                telNo += temp;
                            }
                            var CC = "9" + telNo;
                            telefonListesi.Add(CC);
                        }
                        tDBReader.Close();
                    }
                    catch (Exception)
                    {
                        telefonListesi = null;
                    }
                    return telefonListesi;
                }
            }
        }


    }

    public class SmsUserEntity
    {
        public string Telefon { get; set; }

        public string Adi { get; set; }

        public string Soyadi { get; set; }
    }





}
