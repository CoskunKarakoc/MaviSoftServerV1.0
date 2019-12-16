using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public class GeneralOperations
    {
        public SqlConnection mDBConn { get; set; }

        public SqlDataReader mDBReader { get; set; }

        public SqlCommand mDBCmd { get; set; }

        SqlDataReader tDBReader = null;

        SqlCommand tDBCmd;

        public SqlDataReader mDBReader2 { get; set; }

        public SqlCommand mDBCmd2 { get; set; }

        SqlDataReader tDBReader2 = null;

        SqlCommand tDBCmd2;


        public Thread OperationThread { get; set; }

        string tDBSQLStr;

        string tDBSQLStr2;

        public string mailFrom;

        public string mailHost;

        public int mailPort;

        public string mailUserName;

        public string mailPassword;

        public bool mailSSL;

        public DateTime mailSendTime;

        public DateTime CurrentTime;

        List<string> To;

        object TLockObj = new object();



        public GeneralOperations()
        {

        }

        public bool StartOperations()
        {
            try
            {
                mDBConn = new SqlConnection();
                mDBConn.ConnectionString = @"data source = ARGE-2\SQLEXPRESS; initial catalog = MW301_DB25; integrated security = True; MultipleActiveResultSets = True;";
                mDBConn.Open();


                OperationThread = new Thread(GeneralProccess);
                OperationThread.IsBackground = true;
                OperationThread.Start();
                return true;


            }
            catch (Exception)
            {
                return false;
            }
        }


        public void GeneralProccess()
        {
            while (true)
            {

                tDBSQLStr = "SELECT * FROM EMailSettings";
                tDBCmd = new SqlCommand(tDBSQLStr, mDBConn);
                tDBReader = tDBCmd.ExecuteReader();
                if (tDBReader.Read())
                {
                    mailSendTime = tDBReader["Gonderme Saati"] as DateTime? ?? default(DateTime);
                    CurrentTime = DateTime.Now;
                    if (mailSendTime.ToShortTimeString() == CurrentTime.ToShortTimeString())
                    {
                        mailFrom = tDBReader["E-Mail Adres"].ToString();
                        mailPassword = tDBReader["Sifre"].ToString();
                        mailHost = tDBReader["SMPT Server"].ToString();
                        mailPort = tDBReader["SMPT Server Port"] as int? ?? default(int);
                        mailUserName = tDBReader["Kullanici Adi"].ToString();
                        mailSSL = tDBReader["SSL Kullan"] as bool? ?? default(bool);
                        To = new List<string>();
                        if (tDBReader["Alici 1 E-Mail Gonder"] as bool? ?? default(bool) == true)
                        {
                            To.Add(tDBReader["Alici 1 E-Mail Adres"].ToString());
                        }
                        if (tDBReader["Alici 2 E-Mail Gonder"] as bool? ?? default(bool) == true)
                        {
                            To.Add(tDBReader["Alici 2 E-Mail Adres"].ToString());
                        }
                        if (tDBReader["Alici 3 E-Mail Gonder"] as bool? ?? default(bool) == true)
                        {
                            To.Add(tDBReader["Alici 3 E-Mail Adres"].ToString());
                        }
                        if (mailFrom != null && mailPassword != null && To != null)
                        {
                            SendMail("Birşeyler", To, "Fora Teknoloji Rapor", true);
                        }

                    }

                }

            }
        }

        public bool SendMail(string body, string to, string subject, bool isHtml = true)
        {
            return SendMail(body, new List<string> { to }, subject, isHtml);
        }

        public bool SendMail(string body, List<string> to, string subject, bool isHtml = true)
        {
            bool result = false;
            try
            {
                var message = new MailMessage();
                message.From = new MailAddress(mailFrom, $"MaviSoft-301 {mailUserName}");
                to.ForEach(x =>
                {
                    message.To.Add(new MailAddress(x));
                });
                message.Subject = subject;
                message.Body = body;
                message.IsBodyHtml = isHtml;
                using (var smtp = new SmtpClient(mailHost, mailPort))
                {
                    smtp.EnableSsl = mailSSL;
                    smtp.UseDefaultCredentials = false;
                    smtp.Credentials = new NetworkCredential(mailFrom, mailPassword);
                    smtp.DeliveryMethod = SmtpDeliveryMethod.Network;
                    smtp.Send(message);
                    result = true;
                }


            }

            catch (Exception e)
            {
                throw new Exception(e.Message);
            }

            return result;
        }


    }
}
