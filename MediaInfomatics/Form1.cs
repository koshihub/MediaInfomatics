using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Kinect;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;

namespace MediaInfomatics
{
    public partial class Form1 : Form
    {
        //
        const string Host = "127.0.0.1";
        const int Port = 8890;
        TcpListener server;
        TcpClient client;
        bool IsSendText = false;
        string SendText;
        byte[] recieveTextBytes = new byte[1];

        //
        KinectSensor sensor;
        byte[] colorPixels;
        Bitmap colorBitmap;
        DepthImagePixel[] depthPixels;
        Plane plane;
        List<SkeletonPoint> planePixels;

        bool RGBMode = true;
        bool DepthMode = false;

        #region Initialize
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }

            if (null != this.sensor)
            {
                this.sensor.SkeletonStream.Enable();
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new DepthImagePixel[this.sensor.DepthStream.FramePixelDataLength];
                this.planePixels = new List<SkeletonPoint>();
                this.plane = null;
                this.colorBitmap = new Bitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;
                this.sensor.DepthFrameReady += this.SensorDepthFrameReady;
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            cameraCanvas.Image = new Bitmap(640, 480);

            // Processingと接続
            try
            {
                System.Threading.Tasks.Task.Factory.StartNew(() =>
                {
                    // サーバーを作る
                    server = new TcpListener(IPAddress.Parse(Host), Port);

                    // サーバーを接続待機させる
                    server.Start();
                    Console.WriteLine("connecting...");
                    // サーバーから見たクライアントがclient、AcceptTcpClientで接続を待つ
                    client = server.AcceptTcpClient();
                    Console.WriteLine("connected!");

                    while (client.Connected)
                    {
                        System.Threading.Thread.Sleep(16);
                        if (IsSendText)
                        {
                            IsSendText = false;
                            SendQuery(SendText);
                        }
                    }
                });
            }
            catch (SocketException ee)
            {
                Console.WriteLine(ee.Message);
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
            }
        }

        #region ネットワーク通信

        public string SendQuery(string query)
        {
            try
            {
                if (client != null)
                {
                    var sw = Stopwatch.StartNew();
                    var stream = client.GetStream();
                    byte[] sendData = Encoding.UTF8.GetBytes(query + ";");
                    stream.Write(sendData, 0, sendData.Length);
                    int total = 0;

                    while (true)
                    {
                        if (total >= recieveTextBytes.Length) break;
                        int readSize = stream.Read(recieveTextBytes, total, recieveTextBytes.Length - total);
                        if (readSize <= 0) break;
                        total += readSize;
                    }

                    // 文字列の長さを測定
                    int length = 0;
                    for (; length < recieveTextBytes.Length; length++)
                    {
                        if (recieveTextBytes[length] == 0) break;
                    }

                    return Encoding.UTF8.GetString(recieveTextBytes, 0, length);
                }
                return "";
            }
            catch (Exception ee)
            {
                Console.WriteLine(ee.Message);
                return "";
            }
        }
        #endregion


        unsafe private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    colorFrame.CopyPixelDataTo(this.colorPixels);
                    if (RGBMode)
                    {
                        var lck = colorBitmap.LockBits(new Rectangle(0, 0, 640, 480), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                        byte* p = (byte*)lck.Scan0;
                        for (int i = 0; i < colorPixels.Length / 4; i++)
                        {
                            p[4 * i] = colorPixels[4 * i];
                            p[4 * i + 1] = colorPixels[4 * i + 1];
                            p[4 * i + 2] = colorPixels[4 * i + 2];
                            p[4 * i + 3] = colorPixels[4 * i + 3];
                        }
                        colorBitmap.UnlockBits(lck);
                    }
                }
            }
        }
        unsafe private void SensorDepthFrameReady(object sender, DepthImageFrameReadyEventArgs e)
        {
            using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
            {
                if (depthFrame != null)
                {
                    depthFrame.CopyDepthImagePixelDataTo(this.depthPixels);
                    if (DepthMode)
                    {
                        var lck = colorBitmap.LockBits(new Rectangle(0, 0, 640, 480), System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
                        byte* p = (byte*)lck.Scan0;
                        for (int i = 0; i < depthPixels.Length; i++)
                        {
                            p[4 * i] =
                            p[4 * i + 1] =
                            p[4 * i + 2] =
                            p[4 * i + 3] = (byte)(depthPixels[i].Depth >> 4);
                        }
                        colorBitmap.UnlockBits(lck);
                    }
                }
            }
        }

        private void SensorSkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
        {
            Skeleton[] skeletons = new Skeleton[0];

            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
                    skeletonFrame.CopySkeletonDataTo(skeletons);
                }
            }

            using (var g = Graphics.FromImage(cameraCanvas.Image))
            {
                g.Clear(Color.Black);
                g.DrawImage(colorBitmap, Point.Empty);
                if (skeletons.Length != 0)
                {
                    foreach (Skeleton skel in skeletons)
                    {
                        if (skel.TrackingState != SkeletonTrackingState.NotTracked)
                        {
                            var pt = SkeletonPointToScreen(skel.Position);
                            if (skel.TrackingState == SkeletonTrackingState.Tracked)
                            {
                                this.DrawBonesAndJoints(skel, g);
                                var ray = GetPointingPosition(skel);
                                if (ray != null)
                                {
                                    SkeletonPoint intersect = ray.Item1;
                                    if (stagePoints.Count >= 3)
                                    {
                                        float vx0 = stagePoints[1].X - stagePoints[0].X;
                                        float vy0 = stagePoints[1].Y - stagePoints[0].Y;
                                        float vx1 = stagePoints[2].X - stagePoints[0].X;
                                        float vy1 = stagePoints[2].Y - stagePoints[0].Y;
                                        float dx = intersect.X - stagePoints[0].X;
                                        float dy = intersect.Y - stagePoints[0].Y;
                                        float tx = (dx * vx0 + dy * vy0) / (vx0 * vx0 + vy0 * vy0);
                                        float ty = (dx * vx1 + dy * vy1) / (vx1 * vx1 + vy1 * vy1);
                                        SendText = tx + " " + ty;
                                        IsSendText = true;
                                        string message = "(tx, ty) = (" + tx + "," + ty + ")";
                                        g.DrawString(message, new Font("Arial", 12), Brushes.Red, new PointF(10, 10));
                                    }
                                    g.DrawLine(new Pen(Brushes.Red, 5),
                                        SkeletonPointToScreen(ray.Item2),
                                        SkeletonPointToScreen(ray.Item1));
                                }
                            }
                        }
                    }
                }
                foreach (SkeletonPoint sp in planePixels)
                {
                    Point p = SkeletonPointToScreen(sp);
                    g.FillEllipse(Brushes.Green, p.X - 5, p.Y - 5, 11, 11);
                }

                foreach (SkeletonPoint sp in stagePoints)
                {
                    Point p = SkeletonPointToScreen(sp);
                    g.FillEllipse(Brushes.Black, p.X - 5, p.Y - 5, 11, 11);
                }
                if (stagePoints.Count >= 2)
                {
                    Point p0 = SkeletonPointToScreen(stagePoints[0]);
                    Point p1 = SkeletonPointToScreen(stagePoints[1]);
                    g.DrawLine(new Pen(Color.Yellow)
                    {
                        CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(5, 5)
                    }, p0, p1);
                }
                if (stagePoints.Count >= 3)
                {
                    Point p0 = SkeletonPointToScreen(stagePoints[0]);
                    Point p1 = SkeletonPointToScreen(stagePoints[2]);
                    g.DrawLine(new Pen(Color.Cyan)
                    {
                        CustomEndCap = new System.Drawing.Drawing2D.AdjustableArrowCap(5, 5)
                    }, p0, p1);
                }

            }
        }
        #endregion

        #region Draw
        /// Draws a skeleton's bones and joints
        private void DrawBonesAndJoints(Skeleton skeleton, Graphics g)
        {
            // Render Torso
            this.DrawBone(skeleton, g, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, g, JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, g, JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, g, JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, g, JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, g, JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, g, JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, g, JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, g, JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, g, JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, g, JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, g, JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, g, JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, g, JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, g, JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, g, JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, g, JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (drawBrush != null)
                {
                    g.DrawEllipse(new Pen(Brushes.Blue), new Rectangle(SkeletonPointToScreen(joint.Position), new Size(3, 3)));
                }
            }
        }
        /// Draws a bone line between two joints
        private void DrawBone(Skeleton skeleton, Graphics g, JointType jointType0, JointType jointType1)
        {
            Joint joint0 = skeleton.Joints[jointType0];
            Joint joint1 = skeleton.Joints[jointType1];

            // If we can't find either of these joints, exit
            if (joint0.TrackingState == JointTrackingState.NotTracked ||
                joint1.TrackingState == JointTrackingState.NotTracked)
            {
                return;
            }

            // Don't draw if both points are inferred
            if (joint0.TrackingState == JointTrackingState.Inferred &&
                joint1.TrackingState == JointTrackingState.Inferred)
            {
                return;
            }

            // We assume all drawn bones are inferred unless BOTH joints are tracked
            Pen drawPen = new Pen(Brushes.Blue, 3);

            g.DrawLine(drawPen, this.SkeletonPointToScreen(joint0.Position), this.SkeletonPointToScreen(joint1.Position));
        }


        private Point SkeletonPointToScreen(SkeletonPoint skelpoint)
        {
            // Convert point to depth space.  
            // We are not using depth directly, but we do want the points in our 640x480 output resolution.
            DepthImagePoint depthPoint = this.sensor.CoordinateMapper.MapSkeletonPointToDepthPoint(skelpoint, DepthImageFormat.Resolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
        }

        private SkeletonPoint SkeletonDepthPointToSkeltonPoint(int x, int y, int depth)
        {
            return this.sensor.CoordinateMapper.MapDepthPointToSkeletonPoint(DepthImageFormat.Resolution640x480Fps30,
                new DepthImagePoint()
                {
                    Depth = depth,
                    X = x,
                    Y = y
                });
        }        
        
        #endregion

        private void timer_Tick(object sender, EventArgs e)
        {
            cameraCanvas.Invoke((Action)(() => cameraCanvas.Invalidate()));
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RGBMode = true;
            DepthMode = false;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            RGBMode = false;
            DepthMode = true;
        }

        Tuple<SkeletonPoint, SkeletonPoint> GetPointingPosition(Skeleton skelton)
        {
            if (skelton.Joints[JointType.ShoulderLeft].TrackingState == JointTrackingState.Tracked &&
                skelton.Joints[JointType.HandLeft].TrackingState == JointTrackingState.Tracked)
            {
                SkeletonPoint sPt0 = skelton.Joints[JointType.ElbowLeft].Position;
                SkeletonPoint sPt1 = skelton.Joints[JointType.HandLeft].Position;
                Point pt0 = SkeletonPointToScreen(sPt0);
                Point pt1 = SkeletonPointToScreen(sPt1);

                if ( plane != null )
                {
                    SkeletonPoint isp = plane.getIntersectionPoint(sPt1, sPt0);
                    if (!(isp.X == 0 && isp.Y == 0 && isp.Z == 0)) 
                    return new Tuple<SkeletonPoint, SkeletonPoint>(isp, sPt1);
                }
            }
            return null;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            label1.Text = trackBar1.Value + "";
        }

        private void cameraCanvas_MouseClick(object sender, MouseEventArgs e)
        {
            int depth = depthPixels[e.Y * 640 + e.X].Depth;
            SkeletonPoint sp = SkeletonDepthPointToSkeltonPoint(e.X, e.Y, depth);
            if (e.Button == System.Windows.Forms.MouseButtons.Left)
            {
                planePixels.Add(sp);
                if (planePixels.Count == 4) planePixels.RemoveAt(0);
                else if (planePixels.Count == 3) plane = new Plane(planePixels[0], planePixels[1], planePixels[2]);
                stagePoints.Add(sp);
                if (stagePoints.Count >= 4) stagePoints.RemoveAt(0);
            }
        }

        List<SkeletonPoint> stagePoints = new List<SkeletonPoint>();
    }
}
