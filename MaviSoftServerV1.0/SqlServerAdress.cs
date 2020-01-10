using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public static class SqlServerAdress
    {
        public static string Adres = "data source = .; initial catalog = MW301_DB25; integrated security = True; MultipleActiveResultSets = True;";

        public static string GetAdress()
        {
            return Adres;
        }

        public static void SetAdres(string AdresParam)
        {
            Adres = AdresParam;
        }
    }
}
