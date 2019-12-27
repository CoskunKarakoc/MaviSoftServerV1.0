using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public class MailSettings
    {
        public string EMail_Adres { get; set; }

        public string Kullanici_Adi { get; set; }

        public string Password { get; set; }

        public string MailHost { get; set; }

        public int MailPort { get; set; }

        public string MailUser { get; set; }

        public bool SSL { get; set; }

        public DateTime? Gonderme_Saati { get; set; }

        public int Authentication { get; set; }

        public bool Gelmeyenler_Raporu { get; set; }

        public string Alici_1_EmailAdress { get; set; }

        public bool Alici_1_EmailGonder { get; set; }

        public string Alici_2_EmailAdress { get; set; }

        public bool Alici_2_EmailGonder { get; set; }

        public string Alici_3_EmailAdress { get; set; }

        public bool Alici_3_EmailGonder { get; set; }
    }
}
