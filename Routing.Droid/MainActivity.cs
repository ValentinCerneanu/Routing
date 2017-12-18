using Android.App;
using Android.Widget;
using Android.Content;
using Android.OS;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Json;
using System.Net;
using System.IO;
using System;
using Android.Views.InputMethods;

namespace Routing.Droid
{
    [Activity(Label = "Routing.Droid", MainLauncher = true, Theme = "@style/RoutingTheme")]
    public class MainActivity : Activity
    {
        private EditText etEmail;
        private EditText etPassword;
        private string email, password;
        private Button btnLogin;
        private Button btnCreateAcc;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            etEmail = FindViewById<EditText>(Resource.Id.input_email);
            etPassword = FindViewById<EditText>(Resource.Id.input_password);
            btnLogin = FindViewById<Button>(Resource.Id.btn_login);
            btnCreateAcc = FindViewById<Button>(Resource.Id.btn_create_acc);

            btnLogin.Click += async (sender, e) => {

                InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);

                email = etEmail.Text;
                password = etPassword.Text;

                string hashPassword = MD5Hash(password);

                string url = "http://api.geonames.org/findNearByWeatherJSON?lat=" +
                          email +
                          "&lng=" +
                          hashPassword +
                          "&username=vali28";

                JsonValue json = await FetchDataAsync(url);
            };

            btnCreateAcc.Click += (sender, e) => {
                StartActivity(typeof(CreateAccActivity));

            };


            /*
            if (email != null && password != null)
            {

                Log.Info("LoginForm:", "NOT NULL");
                //utilizatorul NU trebuie sa introduca datele
                //new AsyncLogin().execute(email, password);
                Log.Info("LoginForm:", email);
                Log.Info("LoginForm:", password);
            }
            else
            {
                //utilizatorul trebuie sa introduca datele
                Log.Info("LoginForm:", "NULL");
            }
            */
        }

        public static string MD5Hash(string input)
        {
            StringBuilder hash = new StringBuilder();
            MD5CryptoServiceProvider md5provider = new MD5CryptoServiceProvider();
            byte[] bytes = md5provider.ComputeHash(new UTF8Encoding().GetBytes(input));

            for (int i = 0; i < bytes.Length; i++)
            {
                hash.Append(bytes[i].ToString("x2"));
            }
            return hash.ToString();
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

    }
}

