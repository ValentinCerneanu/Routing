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
using System.Text;
using System.Net.Http;
using System.Xml;
using Android.Preferences;

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
        private Button goStation;
        private Button goDestination;
        private Button AddButton;
        private Button goPlan;
        private Button goNavigate;
        public double x, y;
        EditText editText;
        private bool isThereAPoli=false;
        private int isItCentered = 0;
        IList<ChargePointDto> points;
        private bool nearest = false;
        private string destinationLat = "0", destinationLng = "0";
        private string startLat = "0", startLng = "0";
        private string waypoints="0";
        IList<ChargePointDto> viaPointsToDestination;
        bool plan = false;
        string name;
        string range;
        

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
            editText = FindViewById<EditText>(Resource.Id.search);
            editText.ImeOptions = ImeAction.Search;
            goStation = FindViewById<Button>(Resource.Id.GoStationButton);
            goDestination = FindViewById<Button>(Resource.Id.GoDestinationButton);
            AddButton = FindViewById<Button>(Resource.Id.AddButton);
            goPlan = FindViewById<Button>(Resource.Id.GoPlanButton);
            goNavigate = FindViewById<Button>(Resource.Id.GoNavigate);

           // ISharedPreferences specs = CarSpecs.specs;
            CarSpecs.specs = PreferenceManager.GetDefaultSharedPreferences(this);
            name = CarSpecs.specs.GetString("name", null);
            range = CarSpecs.specs.GetString("range", null);
            if (range == null & name == null)
                range = "100km";

            editText.EditorAction += async (sender, e) =>
            {
                if (e.ActionId == ImeAction.Search)
                {
                    //Toast.MakeText(Application.Context, editText.Text, ToastLength.Long).Show();
                    InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                    inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);
                    goStation.Visibility = ViewStates.Invisible;
                    AddButton.Visibility = ViewStates.Invisible;
                    nearest = false;
                    waypoints = "0";
                    if(viaPointsToDestination!=null)
                        viaPointsToDestination.Clear();
                    await SearchAsync(editText.Text);
                }
                else
                {
                    e.Handled = false;
                }
            };
            goStation.Click += async (sender, e) => {
                GoButtonClickedAsync();
            };

            goPlan.Click += async (sender, e) => {
                GoPlanButtonClickedAsync();
            };

            goDestination.Click += async (sender, e) => {
                GoDestinationClickedAsync();
            };

            AddButton.Click += async (sender, e) =>
            {
                AddStationToRoute();
            };

            goNavigate.Click += async (sender, e) =>
            {
                var gmmIntentUri = Android.Net.Uri.Parse("google.navigation:q=" + x + "," + y + "&mode=d");
                if (plan == false || nearest == true)
                {
                    //de la locatia actuala
                    gmmIntentUri = Android.Net.Uri.Parse("google.navigation:q=" + x + "," + y + "&mode=d");
                }
                else
                {
                    gmmIntentUri = Android.Net.Uri.Parse("google.navigation:q=" + destinationLat + "," + destinationLng + "&mode=d");
                }
                Intent mapIntent = new Intent(Intent.ActionView, gmmIntentUri);
                mapIntent.SetPackage("com.google.android.apps.maps");
                StartActivity(mapIntent);

            };

            navigationView.NavigationItemSelected += (sender, e) =>
            {
                e.MenuItem.SetChecked(true);
                InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);

                var menuItem = e.MenuItem;
                menuItem.SetChecked(!menuItem.IsChecked);
                drawerLayout.CloseDrawers();

                switch (menuItem.ItemId)
                {
                    case Resource.Id.nav_nearest_charging_stations:

                        //Toast.MakeText(Application.Context, "nav_nearest_charging_stations selected", ToastLength.Long).Show();
                        goNavigate.Visibility = ViewStates.Invisible;
                        NearestChargingStationsAsync();
                        nearest = true;
                        break;

                    case Resource.Id.nav_trips:
                        nearest = false;
                        LayoutInflater layoutInflater = LayoutInflater.From(this);
                        View view = layoutInflater.Inflate(Resource.Layout.user_input_dialog_box, null);
                        Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
                        alertbuilder.SetView(view);
                        var start = view.FindViewById<EditText>(Resource.Id.editTextStart);
                        var destination = view.FindViewById<EditText>(Resource.Id.editTextDestination);
                        alertbuilder.SetCancelable(false)
                        .SetPositiveButton("Start", delegate
                        {
                            plan = true;
                            waypoints = "0";
                            if (viaPointsToDestination != null)
                                viaPointsToDestination.Clear();
                            Plan(start.Text, destination.Text);
                        })
                        .SetNegativeButton("Cancel", delegate
                        {
                            alertbuilder.Dispose();
                        });
                        Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
                        dialog.Show();
                        //Toast.MakeText(Application.Context, "nav_trips selected", ToastLength.Long).Show();
                        break;

                    case Resource.Id.nav_my_cars:

                        StartActivity(typeof(CarSpecs));
                        break;

                    case Resource.Id.nav_logout:

                        this.FinishAffinity();
                        break;

                    default:

                        Toast.MakeText(Application.Context, "Something Wrong", ToastLength.Long).Show();
                        break;
                }
            };
        }
        private async void GoPlanButtonClickedAsync()
        {
            goPlan.Visibility = ViewStates.Invisible;
            goStation.Visibility = ViewStates.Invisible;
            goDestination.Visibility = ViewStates.Invisible;
            goNavigate.Visibility = ViewStates.Invisible;
            plan = true;
            nearest = false;

            string url = "https://maps.googleapis.com/maps/api/directions/json?origin="
                + startLat + "," + startLng + "&destination=" + destinationLat + "," + destinationLng + "&key=AIzaSyBeT4UxwuGgyndiaiagBgY-thD09SvOEGE";

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
            PolylineOptions polylineoption = new PolylineOptions();
            polylineoption.InvokeColor(Android.Graphics.Color.Green);
            polylineoption.Geodesic(true);
            polylineoption.Add(latLngPoints);
            isThereAPoli = true;

            // Add polyline to map
            this.RunOnUiThread(() =>
                GMap.AddPolyline(polylineoption));
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(MidPoint(Convert.ToDouble(startLat), Convert.ToDouble(startLng), x, y), 8);
            GMap.MoveCamera(camera);

            goNavigate.Visibility = ViewStates.Visible;
        }

        public async Task Plan(String startText, String destinationText)
        {
            goStation.Visibility = ViewStates.Invisible;
            goDestination.Visibility = ViewStates.Invisible;
            AddButton.Visibility = ViewStates.Invisible;
            goNavigate.Visibility = ViewStates.Invisible;
            plan = true;
            nearest = false;

            startText = startText.Trim();
            destinationText = destinationText.Trim();
            string url1 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/destination/" + startText;
            string json1 = await FetchGoogleDataAsync(url1);
            string url2 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/destination/" + destinationText;
            string json2 = await FetchGoogleDataAsync(url2);

            XmlDocument xml1 = new XmlDocument();
            xml1.LoadXml(json1);
            XmlNodeList xnList1 = xml1.SelectNodes("/location");

            XmlDocument xml2 = new XmlDocument();
            xml2.LoadXml(json2);
            XmlNodeList xnList2 = xml2.SelectNodes("/location");

            destinationLat = "0"; destinationLng = "0";
            startLat = "0"; startLng = "0";
            string url3 = "0";

            foreach (XmlNode xn in xnList1)
            {
                startLat = xn["lat"].InnerText;
                startLng = xn["lng"].InnerText;
                //Console.WriteLine("Name: {0} {1}", destinationLat, destinationLng);
            }
            foreach (XmlNode xn in xnList2)
            {
                destinationLat = xn["lat"].InnerText;
                destinationLng = xn["lng"].InnerText;
                //Console.WriteLine("Name: {0} {1}", destinationLat, destinationLng);
            }


            url3 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/route/" + startLat + "/" + startLng + "/" + destinationLat + "/" + destinationLng + "/" + range;

            //for polyline 
            x = Convert.ToDouble(destinationLat);
            y = Convert.ToDouble(destinationLng);

            JsonValue json3 = await FetchDataAsync(url3);
            viaPointsToDestination = DeserializeToList<ChargePointDto>(json3.ToString());

            GMap.Clear();
            goPlan.Visibility = ViewStates.Visible;
            //location
            LatLng latlng = new LatLng(Convert.ToDouble(startLat), Convert.ToDouble(startLng));
            var options = new MarkerOptions().SetPosition(latlng).SetTitle(startText).SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            Marker DestinationMarker = GMap.AddMarker(options);

            //destination
            LatLng destination = new LatLng(Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng));
            MarkerOptions options1 = new MarkerOptions().SetPosition(destination).SetTitle(destinationText).SetSnippet("Destination").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            GMap.AddMarker(options1);

            //stations
            foreach (var point in viaPointsToDestination)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(", ", "\n"));
            }

            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(MidPoint(Convert.ToDouble(startLat), Convert.ToDouble(startLng), Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng)), 8);
            GMap.MoveCamera(camera);

            //Toast.MakeText(Application.Context, editText.Text, ToastLength.Long).Show();
        }

        public async Task AddStationToRoute()
        {
            string json;
            if (plan == false)
            {
                if (waypoints.Equals("0"))
                {
                    waypoints = x + "," + y;
                }
                else
                    waypoints = waypoints + "|" + x + "," + y;

                AddButton.Visibility = ViewStates.Invisible;
                string url = "https://maps.googleapis.com/maps/api/directions/json?origin="
                    + latitude + "," + longitude + "&destination=" + destinationLat + "," + destinationLng
                    + "&waypoints=" + waypoints + "&key=AIzaSyBeT4UxwuGgyndiaiagBgY-thD09SvOEGE";

                json = await FetchGoogleDataAsync(url);

                GMap.Clear();
                //location
                LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
                var options = new MarkerOptions().SetPosition(latlng).SetTitle("You").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                locationMarker = GMap.AddMarker(options);

                //destination
                LatLng destination = new LatLng(Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng));
                MarkerOptions options1 = new MarkerOptions().SetPosition(destination).SetTitle(editText.Text).SetSnippet("Destination").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                GMap.AddMarker(options1);
            }
            else
            {
                if (waypoints.Equals("0"))
                {
                    waypoints = x + "," + y;
                }
                else
                    waypoints = waypoints + "|" + x + "," + y;

                AddButton.Visibility = ViewStates.Invisible;
                string url = "https://maps.googleapis.com/maps/api/directions/json?origin="
                    + startLat + "," + startLng + "&destination=" + destinationLat + "," + destinationLng
                    + "&waypoints=" + waypoints + "&key=AIzaSyBeT4UxwuGgyndiaiagBgY-thD09SvOEGE";

                json = await FetchGoogleDataAsync(url);

                GMap.Clear();
                //location
                LatLng latlng = new LatLng(Convert.ToDouble(startLat), Convert.ToDouble(startLng));
                var options = new MarkerOptions().SetPosition(latlng).SetTitle("Start").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                Marker DestinationMarker = GMap.AddMarker(options);

                //destination
                LatLng destination = new LatLng(Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng));
                MarkerOptions options1 = new MarkerOptions().SetPosition(destination).SetTitle("Destination").SetSnippet("Destination").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                GMap.AddMarker(options1);
            }

            //stations
            foreach (var point in viaPointsToDestination)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(", ", "\n"));
            }

            DirectionsDto directions = JsonConvert.DeserializeObject<DirectionsDto>(json);

            var lstDecodedPoints = FnDecodePolylinePoints(directions.routes[0].overview_polyline.points);
            var latLngPoints = new LatLng[lstDecodedPoints.Count];
            int index = 0;
            foreach (Android.Locations.Location loc in lstDecodedPoints)
            {
                latLngPoints[index++] = new LatLng(loc.Latitude, loc.Longitude);
            }
            // Create polyline 
            PolylineOptions polylineoption = new PolylineOptions();
            polylineoption.InvokeColor(Android.Graphics.Color.Green);
            polylineoption.Geodesic(true);
            polylineoption.Add(latLngPoints);
            isThereAPoli = true;

            // Add polyline to map
            this.RunOnUiThread(() =>
                GMap.AddPolyline(polylineoption));
        }
        public async Task SearchAsync(String input)
        {
            goStation.Visibility = ViewStates.Invisible;
            goNavigate.Visibility = ViewStates.Invisible;

            input = input.Trim();
            string url1 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/destination/" + input;
            string json1 = await FetchGoogleDataAsync(url1);

            XmlDocument xml = new XmlDocument();
            xml.LoadXml(json1); 
            XmlNodeList xnList = xml.SelectNodes("/location");

            destinationLat = "0"; destinationLng = "0";
            string url2 = "0";
            foreach (XmlNode xn in xnList)
            {
                destinationLat = xn["lat"].InnerText;
                destinationLng = xn["lng"].InnerText;

                //url2 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/angle/" + latitude + "/" + longitude + "/" + destinationLat + "/" + destinationLng;
                url2 = "http://chargetogoapi.azurewebsites.net/api/chargepoint/route/" + latitude + "/" + longitude + "/" + destinationLat + "/" + destinationLng + "/" + range;
                //Console.WriteLine("Name: {0} {1}", destinationLat, destinationLng);
            }

            //for polyline 
            x = Convert.ToDouble(destinationLat);
            y = Convert.ToDouble(destinationLng);

            JsonValue json2 = await FetchDataAsync(url2);
            viaPointsToDestination = DeserializeToList<ChargePointDto>(json2.ToString());

            GMap.Clear();
            goDestination.Visibility = ViewStates.Visible;
            //location
            LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            var options = new MarkerOptions().SetPosition(latlng).SetTitle("You").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            locationMarker = GMap.AddMarker(options);

            //destination
            LatLng destination = new LatLng(Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng));
            MarkerOptions options1 = new MarkerOptions().SetPosition(destination).SetTitle(input).SetSnippet("Destination").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            GMap.AddMarker(options1);

            //stations
            foreach (var point in viaPointsToDestination)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(", ", "\n"));
            }
            
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(MidPoint(Convert.ToDouble(latitude), Convert.ToDouble(longitude), Convert.ToDouble(destinationLat), Convert.ToDouble(destinationLng)), 8);
            GMap.MoveCamera(camera);

            //Toast.MakeText(Application.Context, editText.Text, ToastLength.Long).Show();
        }

        private async void GoDestinationClickedAsync()
        {
            goStation.Visibility = ViewStates.Invisible;
            goDestination.Visibility = ViewStates.Invisible;
            plan = false;
           
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
            PolylineOptions polylineoption = new PolylineOptions();
            polylineoption.InvokeColor(Android.Graphics.Color.Green);
            polylineoption.Geodesic(true);
            polylineoption.Add(latLngPoints);
            isThereAPoli = true;

            // Add polyline to map
            this.RunOnUiThread(() =>
                GMap.AddPolyline(polylineoption));
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(MidPoint(Convert.ToDouble(latitude), Convert.ToDouble(longitude), x, y), 8);
            GMap.MoveCamera(camera);

            goNavigate.Visibility = ViewStates.Visible;
        }

        private async void GoButtonClickedAsync()
        {
            goStation.Visibility = ViewStates.Invisible;
            if(isThereAPoli == true)
            {
                GMap.Clear();
                foreach (var point in points)
                {
                    AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(",", ",\n"));

                }
                LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
                var options = new MarkerOptions().SetPosition(latlng).SetTitle("You").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
                locationMarker = GMap.AddMarker(options);
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
            PolylineOptions polylineoption = new PolylineOptions();
            polylineoption.InvokeColor(Android.Graphics.Color.Green);
            polylineoption.Geodesic(true);
            polylineoption.Add(latLngPoints);
            isThereAPoli = true;

            // Add polyline to map
            this.RunOnUiThread(() =>
                GMap.AddPolyline(polylineoption));
            CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(MidPoint(Convert.ToDouble(latitude), Convert.ToDouble(longitude), x, y), 11);
            GMap.MoveCamera(camera);

            goNavigate.Visibility = ViewStates.Visible;
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
                    Log.Error("lv++", content);
                    return content;
                }
            }
        }

        List<Location> FnDecodePolylinePoints(string encodedPoints)
        {
            if (string.IsNullOrEmpty(encodedPoints))
                return null;
            var poly = new List<Location>();
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
                    Location p = new Location("")
                    {
                        Latitude = Convert.ToDouble(currentLat) / 100000.0,
                        Longitude = Convert.ToDouble(currentLng) / 100000.0
                    };
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
            AddButton.Visibility = ViewStates.Invisible;
            markerClickEventArgs.Handled = true;
            if (nearest == true)
            {
                if (markerClickEventArgs.Marker.Title != "You")
                {
                    goStation.Visibility = ViewStates.Visible;
                }
                else
                {
                    goStation.Visibility = ViewStates.Invisible;
                }
            }
            else
            {
                if(markerClickEventArgs.Marker.Title != "You" && markerClickEventArgs.Marker.Snippet !="Destination")
                {
                    AddButton.Visibility = ViewStates.Visible;
                }
            }

            markerClickEventArgs.Marker.ShowInfoWindow();

            x = markerClickEventArgs.Marker.Position.Latitude;
            y = markerClickEventArgs.Marker.Position.Longitude;

            //LatLng location = new LatLng(Convert.ToDouble(x), Convert.ToDouble(y));
            //CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(location, 10);
            //GMap.MoveCamera(camera);
        }

        public async void NearestChargingStationsAsync()
        {
            string url = "http://chargetogoapi.azurewebsites.net/api/chargepoint/" +
                         latitude + "/" +
                         longitude + "/50";
            JsonValue json = await FetchDataAsync(url);
            points = DeserializeToList<ChargePointDto>(json.ToString());

            goDestination.Visibility = ViewStates.Invisible;
            GMap.Clear();
            LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            var options = new MarkerOptions().SetPosition(latlng).SetTitle("You").SetIcon(BitmapDescriptorFactory.DefaultMarker(BitmapDescriptorFactory.HueAzure));
            locationMarker = GMap.AddMarker(options);

            foreach (var point in points)
            {
                AddNewPoint(point.Name, point.Latitude, point.Longitude, point.Info.Replace(", ", "\n"));
            }

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
            goStation.Visibility = ViewStates.Invisible;
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

        private double DegreeToRadian(double angle)
        {
            return Math.PI * angle / 180.0;
        }

        private double RadianToDegree(double angle)
        {
            return angle * (180.0 / Math.PI);
        }

        public LatLng MidPoint(double lat1, double lon1, double lat2, double lon2)
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
            GMap.SetInfoWindowAdapter(new CustomInfoWindow(LayoutInflater));
            GMap.MarkerClick += MapOnMarkerClick;
            GMap.MapClick += MapOnClick;

            LatLng latlng = new LatLng(Convert.ToDouble(latitude), Convert.ToDouble(longitude));
            if(isItCentered < 2)
            {
                CameraUpdate camera = CameraUpdateFactory.NewLatLngZoom(latlng, 14);
                GMap.MoveCamera(camera);
                isItCentered++; 
            }

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