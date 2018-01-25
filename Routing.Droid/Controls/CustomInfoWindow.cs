using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Gms.Maps.Model;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using static Android.Gms.Maps.GoogleMap;

namespace Routing.Droid.Controls
{
    public class CustomInfoWindow : Java.Lang.Object, IInfoWindowAdapter
    {

        private LayoutInflater inflater;
        private View infoWindow;

        public void Dispose()
        {
            return;
        }

        public View GetInfoContents(Marker marker)
        {
            if (infoWindow == null)
                infoWindow = inflater.Inflate(Resource.Layout.MapInfoWindow, null);

            TextView textSnippet = (TextView)infoWindow.FindViewById<TextView>(Resource.Id.textViewSnipped);
            textSnippet.Text= marker.Snippet;
            TextView textTitle = (TextView)infoWindow.FindViewById<TextView>(Resource.Id.textViewTitle);
            textTitle.Text = marker.Title;
            return infoWindow;
        }

        public View GetInfoWindow(Marker marker)
        {
            return null;
        }

        public CustomInfoWindow(LayoutInflater inflater)
        {
            this.inflater = inflater;
        }
    }
}