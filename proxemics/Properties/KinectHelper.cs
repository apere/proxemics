using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


using Microsoft.Kinect;

namespace proxemics
{
    public struct KinectPoint
    {
        public OpenTK.Vector3d p;
        public int i, j;
        public OpenTK.Graphics.Color4 color;
        public double depthN;
        public bool isReliable;
    }

    public class KinectHelper
    {
        public KinectSensor KSensor = null;
        public CoordinateMapper coordinateMapper = null;
        protected MultiSourceFrameReader multireader;


        protected int ColorWidth = 0;
        protected int ColorHeight = 0;

        public int DepthWidth = 0;
        public int DepthHeight = 0;

        protected CameraSpacePoint[] CameraPoints;
        protected ColorSpacePoint[] Cpoints;
        protected int[] ColorIndex;
        protected PointF[] ViewTable;
        protected byte[] KColors;
        public Body[] Bodies = null;
        protected Vector4 FloorClipPlane = new Vector4();

        public KinectPoint[,] Points;

        int fcount = 0;

        public int frames = 0;

        protected bool working = false;
        protected bool newFrame = true;

        protected bool TrackSkeleton;
        protected bool TrackDepth;
        protected bool TrackColor;
       // public bool TransformPoints;

        public KinectHelper(bool trackSkeleton, bool trackDepth, bool trackColor)
        {
            TrackSkeleton = trackSkeleton;
            TrackDepth = trackDepth;
            TrackColor = trackColor;
            // TransformPoints = transformPoints;
           
        }

        public bool IsSensorOpen
        {
            get
            {
                return (KSensor != null && KSensor.IsOpen);
            }
        }

        public bool HasSkeletonData
        {
            get
            {
                return IsSensorOpen && Bodies != null;
            }
        }

        public bool HasColorData
        {
            get
            {
                return IsSensorOpen && KColors != null;
            }
        }

        public bool HasDepthData
        {
            get
            {
                return IsSensorOpen && CameraPoints != null;
            }
        }




        public void Close()
        {
            if (multireader != null)
            {
                multireader.Dispose();
                multireader = null;
            }

            if (KSensor != null)
            {
                KSensor.Close();
                KSensor = null;
            }
        }

        public void Initialize()
        {
            KSensor = KinectSensor.GetDefault();
            

            FrameSourceTypes ftypes = FrameSourceTypes.None;
            if (TrackSkeleton) ftypes |= FrameSourceTypes.Body;
            if (TrackDepth) ftypes |= FrameSourceTypes.Depth;
            if (TrackColor) ftypes |= FrameSourceTypes.Color;

            coordinateMapper = this.KSensor.CoordinateMapper;

            multireader = KSensor.OpenMultiSourceFrameReader(ftypes);
            multireader.MultiSourceFrameArrived += multireader_MultiSourceFrameArrived;
            KSensor.Open();
        }



        void multireader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            if (working) return;
            working = true;

            var frameReference = e.FrameReference;
            if (frameReference == null)
            {
                working = false;
                return;
            }

            try
            {
                var framew = e.FrameReference.AcquireFrame();

                fcount++;
                
                if (framew == null || framew.BodyFrameReference == null)
                {
                    working = false;
                    return;
                }

                if (TrackSkeleton)
                {
                    using (var frame = framew.BodyFrameReference.AcquireFrame())
                    {
                        if (frame != null)
                        {

                            if (Bodies == null)
                            {
                                Bodies = new Body[frame.BodyCount];
                            }

                            frame.GetAndRefreshBodyData(Bodies);

                        }
                    }
                }

                if (TrackColor)
                {
                    using (ColorFrame colorFrame = framew.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            if (KColors == null)
                            {
                                ColorWidth = colorFrame.FrameDescription.Width;
                                ColorHeight = colorFrame.FrameDescription.Height;
                                KColors = new byte[ColorWidth * ColorHeight * 4];
                            }
                            colorFrame.CopyConvertedFrameDataToArray(KColors, ColorImageFormat.Bgra);
                        }
                    }
                }


                if (TrackDepth)
                {
                    using (DepthFrame depthFrame = framew.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                        {
                            if (CameraPoints == null)
                            {
                                DepthWidth = depthFrame.FrameDescription.Width;
                                DepthHeight = depthFrame.FrameDescription.Height;

                                CameraPoints = new CameraSpacePoint[DepthWidth * DepthHeight];
                                Cpoints = new ColorSpacePoint[DepthWidth * DepthHeight];

                            }
                            using (Microsoft.Kinect.KinectBuffer depthBuffer = depthFrame.LockImageBuffer())
                            {
                                minReliableDistance = depthFrame.DepthMinReliableDistance;
                                maxReliableDistance = depthFrame.DepthMaxReliableDistance;
                                // verify data and write the color data to the display bitmap
                                if (((DepthWidth * DepthHeight) == (depthBuffer.Size / depthFrame.FrameDescription.BytesPerPixel)) && (depthFrame.FrameDescription.Width == DepthWidth) && (depthFrame.FrameDescription.Height == DepthHeight))
                                {
                                    // Note: In order to see the full range of depth (including the less reliable far field depth)
                                    // we are setting maxDepth to the extreme potential depth threshold
                                   ushort maxDepth = ushort.MaxValue;

                                    // If you wish to filter by reliable depth distance, uncomment the following line:
                                    maxDepth = depthFrame.DepthMaxReliableDistance;

                                    this.ProcessDepthFrameData(depthBuffer.UnderlyingBuffer, depthBuffer.Size, depthFrame.DepthMinReliableDistance, maxDepth);
                                }
                            }
                        }
                    }
                }

                PostProc();

     
            }
            catch (Exception eex)
            {
                Console.WriteLine(eex.Message);
                working = false;
                return;
            }

            working = false;
        }

        protected ushort minReliableDistance;
        protected ushort maxReliableDistance;
        

        public OpenTK.Graphics.Color4 ColorAt(double x, double y, double z)
        {

            CameraSpacePoint camp = new CameraSpacePoint();
            return ColorAt(camp);
        }

        public OpenTK.Graphics.Color4 ColorAt(CameraSpacePoint cp)
        {
            if (!HasColorData || !HasDepthData || coordinateMapper == null || KColors==null) return OpenTK.Graphics.Color4.Black;

            ColorSpacePoint colp = coordinateMapper.MapCameraPointToColorSpace(cp);

            

            int j = (int)(colp.Y + 0.5);
            int i = (int)(colp.X + 0.5);
            if (j < 0 || j >= ColorHeight || i<0 || i> ColorWidth) return OpenTK.Graphics.Color4.Black;

            int k = (j * ColorWidth + i) * 4;

            OpenTK.Graphics.Color4 c = new OpenTK.Graphics.Color4();
            c.R = KColors[k + 2] / 255.0f;
            c.G = KColors[k + 1] / 255.0f;
            c.B = KColors[k] / 255.0f;
            c.A = 1.0f;

            return c;
        }

        private void ProcessDepthFrameData(IntPtr depthFrameData, uint depthFrameDataSize, ushort minDepth, ushort maxDepth)
        {

            if (ViewTable == null)
            {
                ViewTable = coordinateMapper.GetDepthFrameToCameraSpaceTable();
                ColorIndex = new int[DepthWidth * DepthHeight];
            }
           // if (TransformPoints)
            //{
                coordinateMapper.MapDepthFrameToCameraSpaceUsingIntPtr(depthFrameData, depthFrameDataSize, CameraPoints);

                if (TrackColor)
                {
                    coordinateMapper.MapDepthFrameToColorSpaceUsingIntPtr(depthFrameData, depthFrameDataSize, Cpoints);
                    for (int k = 0; k < Cpoints.Length; ++k)
                    {
                        int j = (int)(Cpoints[k].Y + 0.5);
                        int i = (int)(Cpoints[k].X + 0.5);
                        if (j < 0) j = 0;
                        else if (j >= ColorHeight) j = ColorHeight - 1;

                        if (i < 0) i = 0;
                        else if (i >= ColorWidth) i = ColorWidth - 1;

                        ColorIndex[k] = j * ColorWidth + i;
                    }
                }
           // }

        }

        private void PostProc()
        {
            if (!TrackDepth || CameraPoints == null) return;
            if (Points==null)
            {
                Points = new KinectPoint[DepthWidth, DepthHeight];
                for (int j=0; j<DepthHeight; ++j)
                {
                    for(int i=0; i<DepthWidth; ++i)
                    {
                        Points[i, j] = new KinectPoint();
                        Points[i, j].i = i;
                        Points[i, j].j = j;
                    }
                }
            }

            double mind = 0.8;
            double maxd = 5.0;
            double dmul = 1.0 / (maxd - mind);
            float mulcol = 1.0f / 255.0f;
            int k = 0;
            DepthSpacePoint dp = new DepthSpacePoint();
          
            for (int j = 0; j < DepthHeight; ++j)
            {
                for (int i = 0; i < DepthWidth; ++i)
                {
                    if (!double.IsNaN(CameraPoints[k].Z) && !double.IsInfinity(CameraPoints[k].Z))
                    {
                        Points[i, j].p = new OpenTK.Vector3d(CameraPoints[k].X, CameraPoints[k].Y, CameraPoints[k].Z);
                        Points[i, j].depthN = (Points[i, j].p.Z - mind) * dmul;
                        Points[i, j].isReliable = true;
                    }
                    else
                    {
                        dp.X = i;
                        dp.Y = j;
                        CameraSpacePoint cp= coordinateMapper.MapDepthPointToCameraSpace(dp, maxReliableDistance);

                        Points[i, j].p = new OpenTK.Vector3d(cp.X, cp.Y, cp.Z);
                        Points[i, j].depthN = (Points[i, j].p.Z - mind) * dmul;
                        Points[i, j].isReliable = false;
                    }
                    if (TrackColor)
                    {
                        Points[i, j].color.B = KColors[ColorIndex[k]*4]*mulcol;
                        Points[i, j].color.G = KColors[ColorIndex[k]*4+1] * mulcol;
                        Points[i, j].color.R = KColors[ColorIndex[k]*4+2] * mulcol;
                        Points[i, j].color.A = 1.0f;
                    }
                    else
                    {
                        Points[i, j].color.R = 1.0f;
                        Points[i, j].color.G = 1.0f;
                        Points[i, j].color.B = 1.0f;
                        Points[i, j].color.A = 1.0f;
                    }

                    k++;
                }
            }
        }
    }
    /*public class KinectHelper
    {
        public KinectSensor KSensor = null;
        public BodyFrameReader BodyReader = null;

        

        public int DisplayWidth = 0;
        public int DisplayHeight = 0;

        public int DWidth = 0;
        public int DHeight = 0;

        public Body[] Bodies = null;
        public Vector4 FloorClipPlane = new Vector4();

        public bool IsWorking = false;
        public bool HasNewFrame = true;
        int fcount = 0;

        public int frames = 0;

        public KinectHelper(bool trackSkeleton, bool trackDepth, bool trackColor, bool transformPoints)
        {
        }

        public bool IsSensorOpen
        {
            get
            {
                return (KSensor != null && KSensor.IsOpen);
            }
        }

        public bool HasSkeletonData
        {
            get
            {
                return IsSensorOpen && Bodies != null;
            }
        }


        public void Close()
        {
            if (BodyReader != null)
            {
                BodyReader.Dispose();
                BodyReader = null;
            }

            if (KSensor != null)
            {
                KSensor.Close();
                KSensor = null;
            }
        }

        public void Initialize()
        {
            KSensor = KinectSensor.GetDefault();

            BodyReader = KSensor.BodyFrameSource.OpenReader();
            BodyReader.FrameArrived += BodyReader_FrameArrived;

            KSensor.Open();
        }

        void BodyReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            if (IsWorking) return;
            IsWorking = true;

            var frameReference = e.FrameReference;
            if (frameReference == null)
            {
                IsWorking = false;
                return;
            }

            try
            {

                using (BodyFrame frame = e.FrameReference.AcquireFrame())
                {
                    if (frame == null)
                    {
                        IsWorking = false;
                        return;
                    }
                    FloorClipPlane = frame.FloorClipPlane;

                    fcount++;

                    if (Bodies == null || Bodies.Length != frame.BodyCount)
                    {
                        Bodies = new Body[frame.BodyCount];
                    }

                    frame.GetAndRefreshBodyData(Bodies);
                    HasNewFrame = true;
                }

            }
            catch (Exception eex)
            {
                Console.WriteLine(eex.Message);
                IsWorking = false;
                return;
            }

            IsWorking = false;
        }
        
        
    }*/
}
