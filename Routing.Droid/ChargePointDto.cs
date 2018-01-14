using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Routing.Droid
{
    public class ChargePointDto
    {
        //public int Id { get; set; }

        public string Name { get; set; }

        public string Info { get; set; }

        public float Latitude { get; set; }

        public float Longitude { get; set; }

        public ChargePointDto(string name, string latitude, string longitude, string info)
        {
            //Id = Int32.Parse(id);

            Name = name;

            Latitude = float.Parse(latitude, CultureInfo.InvariantCulture.NumberFormat);

            Longitude = float.Parse(longitude, CultureInfo.InvariantCulture.NumberFormat);

            Info = info;
        }
    }

}