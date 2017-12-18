using Android.App;
using Android.Content;
using Android.OS;
using Android.Views.InputMethods;
using Android.Support.Design.Widget;
using Android.Widget;
using System;
using System.IO;
using System.Json;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Routing.Droid
{
    [Activity(Label = "Routing.Droid", Theme = "@style/RoutingTheme")]
    public class CreateAccActivity : Activity
    {
        private EditText etEmail;
        private EditText etUsername;
        private EditText etPassword;
        private EditText etRetypePassword;
        private string email, password, retypePassword, username;
        private Button btnCreateAcc;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            SetContentView(Resource.Layout.CreateAcc);

            etUsername = FindViewById<EditText>(Resource.Id.input_username);
            etEmail = FindViewById<EditText>(Resource.Id.input_email);
            etPassword = FindViewById<EditText>(Resource.Id.input_password);
            etRetypePassword = FindViewById<EditText>(Resource.Id.input_retype_password);
            btnCreateAcc = FindViewById<Button>(Resource.Id.btn_create_acc);

            btnCreateAcc.Click += async (sender, e) => {

                InputMethodManager inputManager = (InputMethodManager)this.GetSystemService(Context.InputMethodService);
                inputManager.HideSoftInputFromWindow(this.CurrentFocus.WindowToken, HideSoftInputFlags.NotAlways);

                if (validate())
                {
                    string s = validate().ToString();
                    Toast.MakeText(this, s, ToastLength.Long).Show();
                    string hashPassword = MD5Hash(password);

                    string url = "http://api.geonames.org/findNearByWeatherJSON?lat=" +
                                email +
                                "&lng=" +
                                hashPassword +
                                username +
                                "&username=vali28";

                    JsonValue json = await FetchDataAsync(url);
                 }
            };

        }

        public bool validate()
        {
            bool valid = true;

            username = etUsername.Text;
            email = etEmail.Text;
            password = etPassword.Text;
            retypePassword = etRetypePassword.Text;

            if(!(username.Length>=6 && username.Length<=15))
            {
                valid = false;
                etUsername.RequestFocus();
                etUsername.SetError("Between 4 and 15 characters", null);
            }

            if(!Android.Util.Patterns.EmailAddress.Matcher(email).Matches())
            {
                valid = false;
                etEmail.RequestFocus();
                etEmail.SetError("Enter a valid email address", null);
            }

            if (!retypePassword.Equals(password))
            {
                valid = false;
                etPassword.RequestFocus();
                etPassword.SetError("Passwords don't match", null);
            }

            if (! (password.Length >= 6))
            {
                valid = false;
                etPassword.RequestFocus();
                etPassword.SetError("Password is too short", null);
            }

            return valid;
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
    }
}