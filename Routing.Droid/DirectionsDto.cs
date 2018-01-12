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
    public class Directions
    {
        public List<Route> routes { get; set; }
        public string status;
    }

    public class Route
    {
        public PolyLineOverview overview_polyline { get; set; }
    }
    public class PolyLineOverview
    {
        public string points { get; set; }
    }
}