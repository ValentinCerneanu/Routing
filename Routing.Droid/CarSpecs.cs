using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using Java.Lang;

namespace Routing.Droid
{
    [Activity(Label = "Routing.Droid", Theme = "@style/RoutingTheme", NoHistory = true)]
    public class CarSpecs : AppCompatActivity
    {
        ISharedPreferences specs;
        string name;
        string range;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            SetContentView(Resource.Layout.CarSpecs);
            TextView text1 = FindViewById<TextView>(Resource.Id.no_car);
            Button AddCar = FindViewById<Button>(Resource.Id.AddCar);
            Button EditCar = FindViewById<Button>(Resource.Id.EditCar);
           
            specs = PreferenceManager.GetDefaultSharedPreferences(this);
            name = specs.GetString("name", null);
            range = specs.GetString("range", null);
            if(name == null)
            {
                //there is NO car
                text1.Visibility = ViewStates.Visible;
                AddCar.Visibility = ViewStates.Visible;
                EditCar.Visibility = ViewStates.Invisible;
            }
            else
            {
                //there is a car
                EditCar.Visibility = ViewStates.Visible;
                AddCar.Visibility = ViewStates.Invisible;
                TextView text3 = FindViewById<TextView>(Resource.Id.nameCar);
                TextView text4 = FindViewById<TextView>(Resource.Id.rangeCar);
                text3.Text = "Name: " + name;
                text4.Text = "Range: " + range + " km";
                text3.Visibility = ViewStates.Visible;
                text4.Visibility = ViewStates.Visible;
            }
        
            AddCar.Click += (sender, e) => {
                Dialog();
            };
            EditCar.Click += (sender, e) => {
                Dialog();
            };
        }
        private void Dialog()
        {
            LayoutInflater layoutInflater = LayoutInflater.From(this);
            View view = layoutInflater.Inflate(Resource.Layout.user_input_car_specs, null);
            Android.Support.V7.App.AlertDialog.Builder alertbuilder = new Android.Support.V7.App.AlertDialog.Builder(this);
            alertbuilder.SetView(view);
            var name = view.FindViewById<EditText>(Resource.Id.car_model);
            var range = view.FindViewById<EditText>(Resource.Id.rangeMax);
            alertbuilder.SetCancelable(false)
                            .SetPositiveButton("Add", delegate
                            {
                                SaveCredetials(name.Text, range.Text);
                                StartActivity(typeof(CarSpecs));
                            })
                            .SetNegativeButton("Cancel", delegate
                            {
                                alertbuilder.Dispose();
                            });
            Android.Support.V7.App.AlertDialog dialog = alertbuilder.Create();
            dialog.Show();
        }
        private void SaveCredetials(string name, string range)
        {
            specs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISharedPreferencesEditor editor = specs.Edit();
            editor.PutString("name", name);
            editor.PutString("range", range);
            editor.Apply();
        }

        //public override void OnBackPressed()
        //{
        //    StartActivity(typeof(MapActivity));
        //}
    }
}