using System;

using Android.App;
using Android.Widget;
using Android.OS;

using Android.Provider; // app access to camera
using Android.Content; // app access to Intent 
using Android.Graphics; // bitmap to convert image file
using System.IO;
using static Android.Provider.MediaStore; // enables app to find real path of file and send it to API
using Newtonsoft.Json;
using RestSharp;
using Android.Database;

namespace XamarinImageUploader.Droid
{
	[Activity (Label = "OCR demo", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
	{
        public static readonly int PickImageId = 1000;
        public static readonly int PictureTakenId = 100;
        private ImageView imageView;
        private Android.Net.Uri imageUri;
        private String imagePath;
        private Button apiButton;
        private String imageNameIs = "tbd";
        private TextView resultView;
        
        protected override void OnCreate (Bundle bundle)
		{
			base.OnCreate (bundle);

            SetContentView(Resource.Layout.Main);
            imageView = FindViewById<ImageView>(Resource.Id.RootImageView);
            resultView = FindViewById<TextView>(Resource.Id.OCR_Result);
            Button selectButton = FindViewById<Button>(Resource.Id.Select);
            selectButton.Click += SelectButtonOnClick;
            
            Button showButton = FindViewById<Button>(Resource.Id.Show);
            showButton.Click += ShowButtonOnClick;

            apiButton = FindViewById<Button>(Resource.Id.TestAPI);
            apiButton.Click += TestAPI;
        }

        private void TestAPI (object sender, EventArgs eventArgs)
        {
            
            resultView.Text = "Calling API...";

            // send image to API on:
            String serverIP = "172.24.252.44"; // this IP must be updated to match localhost IP prior to running this app.
            int portNumber = 45455;
            String url = "http://" + serverIP + ":" + portNumber;
            var client = new RestClient(url);

            // TEMPLATE var request = new RestRequest("resource/{id}", Method.POST);
            var request = new RestRequest("api/license/ParseLicense", Method.POST);

            if(imagePath == null)
            {
                resultView.Text = "No image selected";
            }
            else
            {
                resultView.Text = "Processing image";
                request.AddFile("file", imagePath, null);
                
                var response = client.Post(request); // submit request to API
                var licenseDto = JsonConvert.DeserializeObject<ParserResponse>(response.Content); // interpret response
                resultView.Text = licenseDto.RawData; // display response
                
            }

        }

        // pick a picture from the phone storage system.
        private void SelectButtonOnClick(object sender, EventArgs eventArgs)
        {
            Intent = new Intent();
            Intent.SetType("image/*");
            Intent.SetAction(Intent.ActionGetContent);
            StartActivityForResult(Intent.CreateChooser(Intent, "Select Picture"), PickImageId);
        }

        // convert image from camera to JPEG and extract its Uri
        private void GetBitUri(Bitmap imageBitmap)
        {
            MemoryStream outputStream = new MemoryStream();
            imageBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, outputStream);

            String imagePath = Images.Media.InsertImage(Application.Context.ContentResolver, imageBitmap, imageNameIs, null);
            imageUri = Android.Net.Uri.Parse(imagePath);

        }

        // app's response to user actions (pick file from phone or take new picture)
        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if ((requestCode == PickImageId) && (resultCode == Result.Ok) && (data != null))
            {
                imageUri = data.Data;
                imageView.SetImageURI(imageUri);
                apiButton.Enabled = true;
                imagePath = getRealPathFromURI(this, imageUri);
                resultView.Text = "Real Path is: " + imagePath; // For testing only
            }
            else if ((requestCode == PictureTakenId) && (resultCode == Result.Ok) && (data != null)) // PICTURE TAKEN SUCCESSFULLY
            {
                Bitmap camBitmap = (Bitmap)data.Extras.Get("data");
                GetBitUri(camBitmap);
            }
        }

        // TAKE PICTURE
        private void ShowButtonOnClick(object sender, EventArgs eventArgs)
        {

            Intent camIntent = new Intent(MediaStore.ActionImageCapture);
            StartActivityForResult(camIntent, PictureTakenId);

        }

        // extract real image path for API post
        private String getRealPathFromURI(Context context, Android.Net.Uri imageUri)
        {
            
            string id = DocumentsContract.GetDocumentId(imageUri);
            Android.Net.Uri contentUri = ContentUris.WithAppendedId(
                            Android.Net.Uri.Parse("content://downloads/public_downloads"), long.Parse(id));

            ICursor cursor = null;
            try
            {
                String[] MediaData = { MediaStore.Images.Media.InterfaceConsts.Data };
                cursor = context.ContentResolver.Query(contentUri, MediaData, null, null, null);
                int column_index = cursor.GetColumnIndexOrThrow(MediaStore.Images.Media.InterfaceConsts.Data);
                cursor.MoveToLast();

                String pathIs = cursor.GetString(column_index);

                imagePath = pathIs;

                return pathIs;
            }
            catch (Exception e)
            {
                resultView.Text = "Path: getRealPathFromURI Exception : " + e.ToString();
                return "";
            }
            finally
            {
                if (cursor != null)
                {
                    cursor.Close();
                }
            }
        }
    }
}