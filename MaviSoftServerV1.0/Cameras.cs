using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaviSoftServerV1._0
{
    public class Cameras
    {
       
        public int Kayit_No { get; set; }

       
        public int Kamera_No { get; set; }

       
        public string Kamera_Adi { get; set; }

       
        public int? Kamera_Tipi { get; set; }

       
        public string IP_Adres { get; set; }

       
        public int? TCP_Port { get; set; }

       
        public int? UDP_Port { get; set; }

       
        public string Kamera_Admin { get; set; }

       
        public string Kamera_Password { get; set; }

       
        public string Aciklama { get; set; }

       
        public bool? Geciste_Resim_Kayit { get; set; }

      
        public bool? Geciste_Video_Kayit { get; set; }

      
        public bool? Antipassback_Resim_Kayit { get; set; }

      
        public bool? Antipassback_Video_Kayit { get; set; }

      
        public bool? Engellenen_Resim_Kayit { get; set; }

      
        public bool? Engellenen_Video_Kayit { get; set; }

      
        public bool? Tanimsiz_Resim_Kayit { get; set; }

      
        public bool? Tanimsiz_Video_Kayit { get; set; }

      
        public int? Panel_ID { get; set; }

      
        public int? Kapi_ID { get; set; }
    }
}
