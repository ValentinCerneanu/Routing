using System;
using System.Collections.Generic;
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
    public class PointDto
    {
        public Point p{ get; set; }

    }
    public class Point
    {
        public double lat { get; set; }
        public double lng { get; set; }
    }
}