using Android.App;
using Android.Widget;
using Android.OS;
using Android.Views;
using Android.Content;
using Android.Database;
using Android.Net;
using Android.Graphics;
using System.Net;
using System.Text;
using Java.Lang;
using Java.IO;
using Android.Provider;

namespace UploadImage
{
    [Activity(Label = "图片上传", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity, View.IOnClickListener
    {
        ImageView imageView = null;
        ProgressDialog progress = null;
        File _file;
        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);
            initView();
        }
        public void initView()
        {
            imageView = (ImageView)FindViewById(Resource.Id.image);
            imageView.SetOnClickListener(this);
        }

        public void OnClick(View v)
        {
            chooseAlertDialog();
        }
        private void takePhoto()
        {
            var _dir = new File(Environment.GetExternalStoragePublicDirectory(Environment.DirectoryPictures), "uploadDemo");
            if (!_dir.Exists())
            {
                _dir.Mkdirs();
            }
            Intent intent = new Intent(MediaStore.ActionImageCapture);
            _file = new File(_dir, "myphoto_"+System.DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg");
            intent.PutExtra(MediaStore.ExtraOutput, Uri.FromFile(_file));
            StartActivityForResult(intent, 0);
        }
        private void chooseAlertDialog()
        {
            //Inflate layout
            var view = LayoutInflater.Inflate(Resource.Layout.chooseDialog, null);
            var takeBtn = view.FindViewById<Button>(Resource.Id.takeBtns);
            var chooseBtns = view.FindViewById<Button>(Resource.Id.chooseBtns);
            var cancelBtn = view.FindViewById<Button>(Resource.Id.cancel);
            var builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            cancelBtn.Click += delegate
            {
                builder.Dismiss();
            };

            takeBtn.Click += delegate
            {
                builder.Dismiss();
                takePhoto();
            };
            chooseBtns.Click += delegate
             {
                 builder.Dismiss();
                 Intent intent = new Intent(Intent.ActionPick, MediaStore.Images.Media.ExternalContentUri);
                 StartActivityForResult(intent, 1);
             };
            builder.Show();
        }
        private void inputAlertDialog(string filename, byte[] imgbyte)
        {
            //Inflate layout
            var view = LayoutInflater.Inflate(Resource.Layout.inputDialog, null);
            var okBtn = view.FindViewById<Button>(Resource.Id.okBtns);
            var cancelBtn = view.FindViewById<Button>(Resource.Id.cancelBtns);
            var fileName = view.FindViewById<EditText>(Resource.Id.fileName);
            fileName.SetText(filename, TextView.BufferType.Editable);
            var builder = new AlertDialog.Builder(this).Create();
            builder.SetView(view);
            builder.SetCanceledOnTouchOutside(false);
            cancelBtn.Click += delegate
            {
                builder.Dismiss();
                builder.Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);
                //Toast.MakeText(this, "Alert dialog dismissed!", ToastLength.Short).Show();
            };

            okBtn.Click += delegate
            {
                builder.Dismiss();
                builder.Window.SetSoftInputMode(SoftInput.StateAlwaysHidden);
                new Handler().Post(() => uploadFile(fileName.Text, imgbyte));
            };
            builder.Show();
            ShowKeyboard(fileName, builder);
        }

        protected override void OnActivityResult(int requestCode, Result resultCode, Intent data)
        {
            if (requestCode == 0 && resultCode == Result.Ok)
            {
                //Intent mediaScanIntent = new Intent(Intent.ActionMediaScannerScanFile);
                //Uri contentUri = Uri.FromFile(_file);
                //mediaScanIntent.SetData(contentUri);
                //SendBroadcast(mediaScanIntent);
                ContentValues values = new ContentValues();
                values.Put(MediaStore.MediaColumns.Data, _file.Path);
                ContentResolver.Insert(MediaStore.Images.Media.ExternalContentUri, values);
                var img = compressImage(_file.Path);
                var filename = _file.Name.Split('.');
                imageView.SetImageBitmap(img);
                var imgBytes = ImageToByte(img);
                inputAlertDialog(filename[0], imgBytes);
            }
            else if (requestCode == 1 && resultCode == Result.Ok)
            {
                var img = compressImage(GetPathToImage(data.Data));
                var path = GetPathToImage(data.Data).Split('/');
                var filename = path[path.Length - 1].Split('.');
                imageView.SetImageBitmap(img);
                var imgBytes = ImageToByte(img);
                inputAlertDialog(filename[0], imgBytes);
            }
        }
        private void ShowKeyboard(EditText userInput, AlertDialog dialog)
        {
            userInput.RequestFocus();
            userInput.SelectAll();
            dialog.Window.SetSoftInputMode(SoftInput.StateAlwaysVisible);
        }

        private async void uploadFile(string filename, byte[] imgBytes)
        {
            using (var webClient = new WebClient())
            {
                var mConnectivity = (ConnectivityManager)GetSystemService(Context.ConnectivityService);
                var info = mConnectivity.ActiveNetworkInfo;
                if (info != null && info.IsConnected)
                {
                    if (info.GetState() == NetworkInfo.State.Connected)
                    {
                        progress = new ProgressDialog(this);
                        progress.SetProgressStyle(ProgressDialogStyle.Horizontal);
                        progress.Progress = 0;
                        progress.Show();
                        webClient.UploadProgressChanged += new UploadProgressChangedEventHandler(UploadProgress);
                        webClient.UploadDataCompleted += new UploadDataCompletedEventHandler(UploadComplete);
                        webClient.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                        string postData = "FileName=" + filename + ".jpg&UploadFile=" + WebUtility.UrlEncode(System.Convert.ToBase64String(imgBytes));
                        //string postData = "FileName=test.jpg&UploadFile=test";
                        byte[] byteArray = Encoding.UTF8.GetBytes(postData);
                        byte[] response = await webClient.UploadDataTaskAsync("http://192.168.5.100:8008/getImage.ashx", byteArray);
                        string s = webClient.Encoding.GetString(response);
                        Toast.MakeText(this, s, ToastLength.Short).Show();
                    }
                }
                else
                    Toast.MakeText(this, "网络不可用", ToastLength.Short).Show();
            }
        }
        private void UploadComplete(object sender, UploadDataCompletedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.
            progress.Dismiss();
        }
        private void UploadProgress(object sender, UploadProgressChangedEventArgs e)
        {
            // Displays the operation identifier, and the transfer progress.            
            //progress.Progress = e.ProgressPercentage;   
            double percentage = (double.Parse(e.BytesSent.ToString()) / double.Parse(e.TotalBytesToSend.ToString())) * 100;
            progress.Progress = int.Parse(Math.Floor(percentage).ToString());
        }
        private string GetPathToImage(Uri uri)
        {
            string path = null;
            // The projection contains the columns we want to return in our query.
            string[] projection = new[] { Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data };
            using (ICursor cursor = ContentResolver.Query(uri, projection, null, null, null))
            {
                if (cursor != null)
                {
                    int columnIndex = cursor.GetColumnIndexOrThrow(Android.Provider.MediaStore.Images.Media.InterfaceConsts.Data);
                    cursor.MoveToFirst();
                    path = cursor.GetString(columnIndex);
                }
            }
            return path;
        }
        private static byte[] ImageToByte(Bitmap img)
        {
            System.IO.MemoryStream stream = new System.IO.MemoryStream();
            img.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);
            return stream.ToArray();
        }
        private Bitmap compressImage(string path)
        {
            int width = 1024;
            int height = 768;
            int scale = 1;
            var opts = new BitmapFactory.Options();
            opts.InJustDecodeBounds = true;
            BitmapFactory.DecodeFile(path, opts);
            if (opts.OutWidth > opts.OutHeight && opts.OutWidth > width)
            {//如果宽度大的话根据宽度固定大小缩放  
                scale = (int)(opts.OutWidth / width);
            }
            else if (opts.OutWidth < opts.OutHeight && opts.OutWidth > height)
            {//如果高度高的话根据宽度固定大小缩放  
                scale = (int)(opts.OutHeight / height);
            }
            opts.InJustDecodeBounds = false;
            opts.InSampleSize = scale;
            Bitmap test = BitmapFactory.DecodeFile(path, opts);
            return BitmapFactory.DecodeFile(path, opts);
        }
    }
}


