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

namespace MediaInfomatics
{
    public partial class Form1 : Form
    {
        KinectSensor sensor;
        byte[] colorPixels;
        Bitmap colorBitmap;
        bool IsSendText = false;
        string SendText;
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Look through all sensors and start the first connected one.
            // This requires that a Kinect is connected at the time of app startup.
            // To make your app robust against plug/unplug, 
            // it is recommended to use KinectSensorChooser provided in Microsoft.Kinect.Toolkit (See components in Toolkit Browser).
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
                // Turn on the skeleton stream to receive skeleton frames
                this.sensor.SkeletonStream.Enable();

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.SkeletonFrameReady += this.SensorSkeletonFrameReady;

                // Turn on the color stream to receive color frames
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);

                // Allocate space to put the pixels we'll receive
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];

                // This is the bitmap we'll display on-screen
                this.colorBitmap = new Bitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, System.Drawing.Imaging.PixelFormat.Format32bppRgb);

                // Add an event handler to be called whenever there is new color frame data
                this.sensor.ColorFrameReady += this.SensorColorFrameReady;


                // Start the sensor!
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }
            pictureBox1.Image = new Bitmap(640, 480);
        }
        unsafe private void SensorColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    // Copy the pixel data from the image to a temporary array
                    colorFrame.CopyPixelDataTo(this.colorPixels);

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

            using (var g = Graphics.FromImage(pictureBox1.Image))
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

                                var posL = SkeletonPointToScreen(skel.Joints[JointType.HandLeft].Position);
                                var posR = SkeletonPointToScreen(skel.Joints[JointType.HandRight].Position);
                                SendText = posL.X + " " + posL.Y + " " + posR.X + " " + posR.Y;
                                IsSendText = true;

                    //            treeView1.Nodes[0].Text = string.Format("Left Hand: ({0}, {1})", posL.X, posL.Y);
                    //            treeView1.Nodes[1].Text = string.Format("Right Hand: ({0}, {1})", posR.X, posR.Y);
                            }
                        }
                    }
                }
            }
        }
        /// <summary>
        /// Draws a skeleton's bones and joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        private void DrawBonesAndJoints(Skeleton skeleton, Graphics g)
        {
            // Render Torso
            this.DrawBone(skeleton, g, JointType.Head, JointType.ShoulderCenter);
            this.DrawBone(skeleton, g,  JointType.ShoulderCenter, JointType.ShoulderLeft);
            this.DrawBone(skeleton, g,  JointType.ShoulderCenter, JointType.ShoulderRight);
            this.DrawBone(skeleton, g,  JointType.ShoulderCenter, JointType.Spine);
            this.DrawBone(skeleton, g,  JointType.Spine, JointType.HipCenter);
            this.DrawBone(skeleton, g,  JointType.HipCenter, JointType.HipLeft);
            this.DrawBone(skeleton, g,  JointType.HipCenter, JointType.HipRight);

            // Left Arm
            this.DrawBone(skeleton, g,  JointType.ShoulderLeft, JointType.ElbowLeft);
            this.DrawBone(skeleton, g,  JointType.ElbowLeft, JointType.WristLeft);
            this.DrawBone(skeleton, g,  JointType.WristLeft, JointType.HandLeft);

            // Right Arm
            this.DrawBone(skeleton, g,  JointType.ShoulderRight, JointType.ElbowRight);
            this.DrawBone(skeleton, g,  JointType.ElbowRight, JointType.WristRight);
            this.DrawBone(skeleton, g,  JointType.WristRight, JointType.HandRight);

            // Left Leg
            this.DrawBone(skeleton, g,  JointType.HipLeft, JointType.KneeLeft);
            this.DrawBone(skeleton, g,  JointType.KneeLeft, JointType.AnkleLeft);
            this.DrawBone(skeleton, g,  JointType.AnkleLeft, JointType.FootLeft);

            // Right Leg
            this.DrawBone(skeleton, g,  JointType.HipRight, JointType.KneeRight);
            this.DrawBone(skeleton, g,  JointType.KneeRight, JointType.AnkleRight);
            this.DrawBone(skeleton, g,  JointType.AnkleRight, JointType.FootRight);

            // Render Joints
            foreach (Joint joint in skeleton.Joints)
            {
                Brush drawBrush = null;

                if (drawBrush != null)
                {
                    g.DrawEllipse(new Pen(Brushes.Blue), new Rectangle( SkeletonPointToScreen(joint.Position), new Size( 3, 3)));
                }
            }
        } /// <summary>
        /// Draws a bone line between two joints
        /// </summary>
        /// <param name="skeleton">skeleton to draw bones from</param>
        /// <param name="drawingContext">drawing context to draw to</param>
        /// <param name="jointType0">joint to start drawing from</param>
        /// <param name="jointType1">joint to end drawing at</param>
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

        byte[] recieveTextBytes = new byte[1];

        private void timer1_Tick(object sender, EventArgs e)
        {
            pictureBox1.Invoke((Action)(() => pictureBox1.Invalidate()));
        }

    }
}
