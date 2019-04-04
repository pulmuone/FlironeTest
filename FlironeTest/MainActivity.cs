using Android.App;
using Android.OS;
using Android.Support.V7.App;
using Android.Runtime;
using Android.Widget;

using Device = Com.Flir.Flironesdk.Device;
using Exception = System.Exception;
using FlirUsbDevice = Com.Flir.Flironesdk.FlirUsbDevice;
using Frame = Com.Flir.Flironesdk.Frame;
using FrameProcessor = Com.Flir.Flironesdk.FrameProcessor;
using LoadedFrame = Com.Flir.Flironesdk.LoadedFrame;
using RenderedImage = Com.Flir.Flironesdk.RenderedImage;
using SimulatedDevice = Com.Flir.Flironesdk.SimulatedDevice;
using Com.Flir.Flironesdk;
using System.Collections.Generic;
using Android.Graphics;
using Java.Text;
using Android.Support.V4.App;
using Android;
using Android.Content.PM;

namespace FlironeTest
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true)]
    [Android.App.IntentFilter(actions: new[] { "android.hardware.usb.action.USB_DEVICE_ATTACHED" })]
    [Android.App.MetaData(Android.Hardware.Usb.UsbManager.ActionUsbAccessoryDetached, Resource = "@xml/device_filter")]
    public class MainActivity : AppCompatActivity, Device.IDelegate, FrameProcessor.IDelegate, Device.IStreamDelegate
    {
        private Device flirDevice;
        private FrameProcessor frameProcessor;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.activity_main);

            try
            {
                List<RenderedImage.ImageType> lstType = new List<RenderedImage.ImageType>();
                lstType.Add(RenderedImage.ImageType.BlendedMSXRGBA8888Image);
                //lstType.Add(RenderedImage.ImageType.ThermalLinearFlux14BitImage);
                //lstType.Add(RenderedImage.ImageType.ThermalRGBA8888Image);
                //lstType.Add(RenderedImage.ImageType.ThermalRadiometricKelvinImage);
                //lstType.Add(RenderedImage.ImageType.VisibleAlignedRGBA8888Image);
                //lstType.Add(RenderedImage.ImageType.VisibleUnalignedYUV888Image);
                //lstType.Add(RenderedImage.ImageType.VisualJPEGImage);
                //lstType.Add(RenderedImage.ImageType.VisualYCbCr888Image);

                frameProcessor = new FrameProcessor(this, this, lstType);

                //아래꺼는 Simulated 테스트에서만 사용, 장비 연결할 경우는 주석 처리
                flirDevice = new SimulatedDevice(this, this, Resources.OpenRawResource(Resource.Raw.sampleframes), 10);
            }
            catch(Exception e)
            {
                System.Console.WriteLine(e.Message);
            }
        }

        protected override void OnResume()
        {
            base.OnResume();
            //Device.StartDiscovery(Application.Context, this);
            Device.StartDiscovery(this, this);
        }

        protected override void OnPause()
        {
            base.OnPause();
            Device.StopDiscovery();
        }

        public void OnAutomaticTuningChanged(bool p0)
        {
            
        }

        /// <summary>
        /// 카메라 연결되면 Device값 연결
        /// </summary>
        /// <param name="device"></param>
        public void OnDeviceConnected(Device device)
        {
            //flirDevice = device; //장비 연결할 경우는 여기 주석 해제
            device.StartFrameStream(this);
        }

        public void OnDeviceDisconnected(Device p0)
        {
            flirDevice = null;
        }

        /// <summary>
        /// 이미지 스캔되면 최종으로 이쪽에서 처리함.
        /// 여기서 renderedImage의 Bitmap 이미지를 처리 하면됩니다. 
        /// </summary>
        /// <param name="p0"></param>
        public void OnFrameProcessed(RenderedImage renderedImage)
        {

            //온도 값이 이렇게 계산하고 있습니다. 
            //PreviewActivity.java 소스 중에 onFrameProcessed () 여기 함수부분을 C#으로 변경.
            //if (renderedImage.InvokeImageType(). == RenderedImage.ImageType.ThermalRadiometricKelvinImage)
            if (renderedImage.InvokeImageType() == RenderedImage.ImageType.BlendedMSXRGBA8888Image)
            {
                int[] thermalPixels = renderedImage.ThermalPixelValues();


                int width = renderedImage.Width();
                int height = renderedImage.Height();
                int centerPixelIndex = width * (height / 2) + (width / 2);
                int[] centerPixelIndexes = new int[] {
                    centerPixelIndex, centerPixelIndex-1, centerPixelIndex+1,
                    centerPixelIndex - width,
                    centerPixelIndex - width - 1,
                    centerPixelIndex - width + 1,
                    centerPixelIndex + width,
                    centerPixelIndex + width - 1,
                    centerPixelIndex + width + 1
                };

                double averageTemp = 0;

                for (int i = 0; i < centerPixelIndexes.Length; i++)
                {
                    int pixelValue = (thermalPixels[centerPixelIndexes[i]]);
                    averageTemp += (((double)pixelValue) - averageTemp) / ((double)i + 1);
                }

                //사진에 찍힌 온도값..
                double averageC = (averageTemp / 100) - 273.15;

                System.Console.WriteLine(string.Format("======= 캡쳐한 사진의 온도====== : {0}", averageC.ToString()));
            }
        }

        public void OnTuningStateChanged(Device.TuningState p0)
        {
            
        }

        public void OnFrameReceived(Frame frame)
        {
            frameProcessor.ProcessFrame(frame);
        }


        protected override void OnStart()
        {
            base.OnStart();

            //https://developer.android.com/guide/topics/security/permissions#normal-dangerous
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                List<string> permissions = new List<string>();

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.WriteExternalStorage) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.WriteExternalStorage);
                }

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.Camera) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.Camera);
                }

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.RecordAudio) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.RecordAudio);
                }

                if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessFineLocation) != (int)Permission.Granted)
                {
                    permissions.Add(Manifest.Permission.AccessFineLocation);
                }

                //그룹중에 하나만 얻으면 그룹 전체 권한을 얻기 때문에 하나만 물어보면 됨.
                //if (ActivityCompat.CheckSelfPermission(this, Manifest.Permission.AccessCoarseLocation) != (int)Permission.Granted)
                //{
                //    permissions.Add(Manifest.Permission.AccessCoarseLocation);
                //}
                 
                if (permissions.Count > 0)
                {
                    ActivityCompat.RequestPermissions(this, permissions.ToArray(), 1);
                }
            }
        }
    }
}