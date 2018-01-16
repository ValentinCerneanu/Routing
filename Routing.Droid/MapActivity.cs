using System;

using Android.App;
using Android.OS;
using Android.Gms.Maps;
using Android.Gms.Maps.Model;
using Android.Locations;
using Android.Gms.Location;
using Android.Util;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.Support.V4.Widget;
using Android.Support.Design.Widget;
using Android.Support.V7.App;
using Android.Views.InputMethods;
using System.Json;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NuGet.Modules;
using Routing.Droid.Controls;
using Newtonsoft.Json;
using System.Xml;
using System.Text;
using System.Net.Http;

namespace Routing.Droid
{
    [Activity(Label = "Routing.Droid", Theme = "@style/RoutingTheme")]
    public class MapActivity : AppCompatActivity, IOnMapReadyCallback, Android.Locations.ILocationListener
    {
        private GoogleMap GMap;
        LocationManager locMgr;
        private string latitude;
        private string longitude;
        private string provider;
        DrawerLayout drawerLayout;
        NavigationView navigationView;
        public static List<string> InvalidJsonElements;
        private Marker locationMarker;
        private Button goButton;
        public double x, y;
        EditText editText;
        private bool isThereAPoli=false;
        public PolylineOptions[] polylineoption = new PolylineOptions[30];
        public int numberOfPoly=0;

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            SetContentView(Resource.Layout.MapActivity);
            SetUpMap();
            var toolbar = FindViewById<Android.Support.V7.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);
            toolbar.Title = "";
            //Enable support action bar to display hamburger
            SupportActionBar.SetHomeAsUpIndicator(Resource.Drawable.ic_menu);
            SupportActionBar.SetDisplayHomeAsUpEnabled(true);

            drawerLayout = FindViewById<DrawerLayout>(Resource.Id.drawerlayout);
            navigationView = FindViewById<NavigationView>(Resource.Id.nav_view);
            goButton = FindViewById<Button>(Resource.Id.GoButton);
            editText = FindViewById<EditText>(Resource.Id.search);
            editText.ImeOptions = ImeAction.Search;

            editText.EditorAction += async (sender, e) =>
            {
                if (e.ActionId == ImeAction.Search)
                {
                    //Toast.MakeText(Application.Context, editText.Text, ToastLength.Long).Show();
                    InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                    inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);

                    await SearchAsync(editText.Text);
                }
                else
                {
                    e.Handled = false;
                }
            };
            //dasdas.SelectNodes("");
            goButton.Click += async (sender, e) => {
                GoButtonClickedAsync();
            };

            navigationView.NavigationItemSelected += (sender, e) =>
            {
                e.MenuItem.SetChecked(true);
                InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
                //react to click here and swap fragments or navigate

                var menuItem = e.MenuItem;
                menuItem.SetChecked(!menuItem.IsChecked);
                drawerLayout.CloseDrawers();

                switch (menuItem.ItemId)
                {

                    case Resource.Id.nav_nearest_charging_stations:

                        //Toast.MakeText(Application.Context, "nav_nearest_charging_stations selected", ToastLength.Long).Show();
                        NearestChargingStationsAsync();
                        break;

                    case Resource.Id.nav_trips:

                        Toast.MakeText(Application.Context, "nav_trips selected", ToastLength.Long).Show();
                        break;

                    case Resource.Id.nav_my_cars:

                        Toast.MakeText(Application.Context, "nav_my_cars selected", ToastLength.Long).Show();
                        break;

                    case Resource.Id.nav_settings:

                        Toast.MakeText(Application.Context, "nav_settings selected", ToastLength.Long).Show();
                        break;

                    case Resource.Id.nav_logout:

                        Toast.MakeText(Application.Context, "nav_logout selected", ToastLength.Long).Show();
                        break;

                    default:

                        Toast.MakeText(Application.Context, "Something Wrong", ToastLength.Long).Show();
                        break;
                }
            };
        }

        public async Task SearchAsync(String input)
        {
            string url = "http://chargeapi.azurewebsites.net/api/chargepoint/angle/" + latitude + "/" + longitude + "/" + 46.013260 + "/" + 25.623746;

            JsonValue json = await FetchDataAsync(url);
            IList<ChargePointDto> pointsToDestination;
            pointsToDestination = DeserializeToList<ChargePointDto>(json.ToString());

            foreach (var point in pointsToDestination)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(",", ",\n"));
            }
            
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(midPoint(Convert.ToDouble(latitude), Convert.ToDouble(longitude), 46.013260, 25.623746), 8);
            GMap.MoveCamera(camera);

            //Toast.MakeText(Application.Context, editText.Text, ToastLength.Long).Show();
        }

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        public LatLng midPoint(double lat1, double lon1, double lat2, double lon2)
        {

            double dLon = DegreeToRadian(lon2 - lon1);

            //convert to radians
            lat1 = DegreeToRadian(lat1);
            lat2 = DegreeToRadian(lat2);
            lon1 = DegreeToRadian(lon1);

            double Bx = Math.Cos(lat2) * Math.Cos(dLon);
            double By = Math.Cos(lat2) * Math.Sin(dLon);
            double lat3 = Math.Atan2(Math.Sin(lat1) + Math.Sin(lat2), Math.Sqrt((Math.Cos(lat1) + Bx) * (Math.Cos(lat1) + Bx) + By * By));
            double lon3 = lon1 + Math.Atan2(By, Math.Cos(lat1) + Bx);

            //print out in degrees
            return new LatLng(RadianToDegree(lat3), RadianToDegree(lon3));
        }

        private async void GoButtonClickedAsync()
        {
            goButton.Visibility = ViewStates.Invisible;
            if(isThereAPoli == true)
            {
                polylineoption[numberOfPoly-1].Visible(false);
                this.RunOnUiThread(() => GMap.AddPolyline(polylineoption[numberOfPoly-1]));
            }
            string url = "https://maps.googleapis.com/maps/api/directions/json?origin="
                + latitude + "," + longitude + "&destination=" + x + "," + y + "&key=AIzaSyBeT4UxwuGgyndiaiagBgY-thD09SvOEGE";
     
            string json = await FetchGoogleDataAsync(url);
            Log.Error("lv", json);
            DirectionsDto directions = JsonConvert.DeserializeObject<DirectionsDto>(json);

            var lstDecodedPoints = FnDecodePolylinePoints(directions.routes[0].overview_polyline.points);
            var latLngPoints = new LatLng[lstDecodedPoints.Count];
            int index = 0;
            foreach (Android.Locations.Location loc in lstDecodedPoints)
            {
                latLngPoints[index++] = new LatLng(loc.Latitude, loc.Longitude);
            }
            // Create polyline 
            polylineoption[numberOfPoly].InvokeColor(Android.Graphics.Color.Green);
            polylineoption[numberOfPoly].Geodesic(true);
            polylineoption[numberOfPoly].Add(latLngPoints);

            isThereAPoli = true;

            // Add polyline to map
            this.RunOnUiThread(() =>
                GMap.AddPolyline(polylineoption[numberOfPoly]));
            numberOfPoly++;
    }

        private async Task<String> FetchGoogleDataAsync(string url)
        {
            // Create an HTTP web request using the URL:
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            request.ContentType = "application/xml";
            request.Method = "GET";

            // Send the request to the server and wait for the response:
            using (WebResponse response = await request.GetResponseAsync())
            {
                // Get a stream representation of the HTTP web response:
                using (Stream stream = response.GetResponseStream())
                {
                    // Use this stream to build a JSON document object:

                    //return response.GetResponseStream().ToString();

                    StreamReader sr = new StreamReader(stream, Encoding.UTF8);
                    //retrun html code 
                    string content = sr.ReadToEnd();
                    Android.Util.Log.Error("lv++", content);
                    return content;

                }
            }
        }

        List<Android.Locations.Location> FnDecodePolylinePoints(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                return null;
            var poly = new List<Android.Locations.Location>();
            char[] polylinechars = encodedPoints.ToCharArray();
            int index = 0;

            int currentLat = 0;
            int currentLng = 0;
            int next5bits;
            int sum;
            int shifter;

            try
            {
                while (index < polylinechars.Length)
                {
                    // calculate next latitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length)
                        break;

                    currentLat += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);

                    //calculate next longitude
                    sum = 0;
                    shifter = 0;
                    do
                    {
                        next5bits = (int)polylinechars[index++] - 63;
                        sum |= (next5bits & 31) << shifter;
                        shifter += 5;
                    } while (next5bits >= 32 && index < polylinechars.Length);

                    if (index >= polylinechars.Length && next5bits >= 32)
                        break;

                    currentLng += (sum & 1) == 1 ? ~(sum >> 1) : (sum >> 1);
                    Location p = new Location("");
                    p.Latitude = Convert.ToDouble(currentLat) / 100000.0;
                    p.Longitude = Convert.ToDouble(currentLng) / 100000.0;
                    poly.Add(p);
                }
            }
            catch
            {


            }
            return poly;
        }

        private void MapOnMarkerClick(object sender, GoogleMap.MarkerClickEventArgs markerClickEventArgs)
        {
            markerClickEventArgs.Handled = true;

            if (markerClickEventArgs.Marker.Title != "You")
            {
                goButton.Visibility = ViewStates.Visible;
            }
            else
            {
                goButton.Visibility = ViewStates.Invisible;
            }

            markerClickEventArgs.Marker.ShowInfoWindow();

            x = markerClickEventArgs.Marker.Position.Latitude;
            y = markerClickEventArgs.Marker.Position.Longitude;
        }

        public async void NearestChargingStationsAsync()
        {
            string url = "http://chargeapi.azurewebsites.net/api/chargepoint/" +
                         latitude + "/" +
                         longitude + "/50";
            JsonValue json = await FetchDataAsync(url);
            IList<ChargePointDto> points;
            points = DeserializeToList<ChargePointDto>(json.ToString());

            foreach(var point in points)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(",", ",\n"));
                
            }
            for (int i = 0; i <= 20; i++)
                polylineoption[i] = new PolylineOptions();

            LatLng location = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(location, 10);
            GMap.MoveCamera(camera);
        }

        public static IList<T> DeserializeToList<T>(string jsonString)
        {
            InvalidJsonElements = null;
            var array = JArray.Parse(jsonString);
            IList<T> objectsList = new List<T>();

            foreach (var item in array)
            {
                try
                {
                    // CorrectElements
                    objectsList.Add(item.ToObject<T>());
                }
                catch (Exception ex)
                {
                    InvalidJsonElements = InvalidJsonElements ?? new List<string>();
                    InvalidJsonElements.Add(item.ToString());
                }
            }
            return objectsList;
        }

        public void AddNewPoint(string name, float pointLat, float pointLong, string info)
        {
            LatLng point = new LatLng(Convert.ToDouble(pointLat), Convert.ToDouble(pointLong));
            MarkerOptions options = new MarkerOptions().SetPosition(point).SetTitle(name).SetSnippet(info).SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueGreen));
            GMap.AddMarker(options);
        }

        private void MapOnClick(object sender, GoogleMap.MapClickEventArgs mapClickEventArgs)
        {
            goButton.Visibility = ViewStates.Invisible;
        }

        private async Task<JsonValue> FetchDataAsync(string url)
        {
            // Create an HTTP web request using the URL:
            HttpWebRequest request = (HttpWebRequest)HttpWebRequest.Create(new Uri(url));
            request.ContentType = "application/json";
            request.Method = "GET";

            // Send the request to the server and wait for the response:
            using (WebResponse response = await request.GetResponseAsync())
            {
                // Get a stream representation of the HTTP web response:
                using (Stream stream = response.GetResponseStream())
                {
                    // Use this stream to build a JSON document object:
                    JsonValue jsonDoc = await Task.Run(() => JsonObject.Load(stream));
                    Console.Out.WriteLine("Response: {0}", jsonDoc.ToString());

                    // Return the JSON document:
                    return jsonDoc;
                }
            }
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Android.Resource.Id.Home:
                    drawerLayout.OpenDrawer(Android.Support.V4.View.GravityCompat.Start);

                    InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                    inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);

                    return true;
            }
            return base.OnOptionsItemSelected(item);
        }

        private void SetUpMap()
        {
            if (GMap == null)
            {
                FragmentManager.FindFragmentById<MapFragment>(Resource.Id.googlemap).GetMapAsync(this);
            }
        }

        public void OnMapReady(GoogleMap googleMap)
        {
            this.GMap = googleMap;
            //GMap.SetInfoWindowAdapter(new CustomInfoWindow(this.LayoutInflater));
            GMap.MarkerClick += MapOnMarkerClick;
            GMap.MapClick += MapOnClick;

            LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(latlng, 15);
            GMap.MoveCamera(camera);

            if (locationMarker != null) locationMarker.Remove();
            var options = new MarkerOptions().SetPosition(latlng).SetTitle("You").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            locationMarker = GMap.AddMarker(options);
        }

        protected override void OnStart()
        {
            base.OnStart();
            Log.Debug("tag", "OnStart called");
        }

        protected override void OnResume()
        {
            base.OnResume();
            Log.Debug("tag", "OnResume called");

            // initialize location manager
            locMgr = GetSystemService(Context.LocationService) as LocationManager;

            // pass in the provider (GPS), 
            // the minimum time between updates (in seconds), 
            // the minimum distance the user needs to move to generate an update (in meters),
            // and an ILocationListener (recall that this class impletents the ILocationListener interface)
            if (locMgr.AllProviders.Contains(LocationManager.NetworkProvider)
                && locMgr.IsProviderEnabled(LocationManager.NetworkProvider))
            {
                locMgr.RequestLocationUpdates(LocationManager.NetworkProvider, 2000, 1, this);
            }
            else
            {
                Toast.MakeText(this, "The Network Provider does not exist or is not enabled!", ToastLength.Long).Show();
            }


            // Comment the line above, and uncomment the following, to test 
            // the GetBestProvider option. This will determine the best provider
            // at application launch. Note that once the provide has been set
            // it will stay the same until the next time this method is called

            /*var locationCriteria = new Criteria();

			locationCriteria.Accuracy = Accuracy.Coarse;
			locationCriteria.PowerRequirement = Power.Medium;

			string locationProvider = locMgr.GetBestProvider(locationCriteria, true);

			Log.Debug(tag, "Starting location updates with " + locationProvider.ToString());
			locMgr.RequestLocationUpdates (locationProvider, 2000, 1, this);*/
        }

        protected override void OnPause()
        {
            base.OnPause();

            // stop sending location updates when the application goes into the background
            // to learn about updating location in the background, refer to the Backgrounding guide
            // http://docs.xamarin.com/guides/cross-platform/application_fundamentals/backgrounding/

            // RemoveUpdates takes a pending intent - here, we pass the current Activity
            locMgr.RemoveUpdates(this);
            Log.Debug("tag", "Location updates paused because application is entering the background");
        }

        protected override void OnStop()
        {
            base.OnStop();
            Log.Debug("tag", "OnStop called");
        }

        public void OnLocationChanged(Android.Locations.Location location)
        {
            Log.Debug("tag", "Location changed");
            latitude = location.Latitude.ToString();
            longitude = location.Longitude.ToString();
            provider = location.Provider.ToString();
            OnMapReady(GMap);
        }
        public void OnProviderDisabled(string provider)
        {
            Log.Debug("tag", provider + " disabled by user");
        }
        public void OnProviderEnabled(string provider)
        {
            Log.Debug("tag", provider + " enabled by user");
        }
        public void OnStatusChanged(string provider, Availability status, Bundle extras)
        {
            Log.Debug("tag", provider + " availability has changed to " + status.ToString());
        }

    }
}