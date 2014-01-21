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

            Plane plane = new Plane(
                new SkeletonPoint
                {
                    X = 0.0f,
                    Y = 3.4f,
                    Z = 599.3f
                },
                new SkeletonPoint
                {
                    X = 422.2f,
                    Y = 34.4f,
                    Z = 1.33f
                },
                new SkeletonPoint
                {
                    X = -40.0f,
                    Y = 345.43f,
                    Z = -5.3f
                });
            plane.getIntersectionPoint(
                new SkeletonPoint
                {
                    X = -2.0f,
                    Y = 54.4f,
                    Z = 89443.3f
                },
                new SkeletonPoint
                {
                    X = 20.0f,
                    Y = 70003.4f,
                    Z = 555.3f
                });
        }
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
            ColorImagePoint colorPoint = this.sensor.CoordinateMapper.MapDepthPointToColorPoint(DepthImageFormat.Resolution640x480Fps30, depthPoint, ColorImageFormat.RgbResolution640x480Fps30);
            return new Point(depthPoint.X, depthPoint.Y);
            //return new Point(colorPoint.X, colorPoint.Y);
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
 /*               richTextBox1.Text = "diff0 = " + (sPt0.Z * 1000 - depthPixels[640 * pt0.Y + pt0.X].Depth) + "\n";
                richTextBox1.Text += "diff1 = " + (sPt1.Z * 1000 - depthPixels[640 * pt1.Y + pt1.X].Depth) + "\n";
                richTextBox1.Text += "sPt0.Z = " + sPt0.Z + "\n";
                richTextBox1.Text += "sPt1.Z = " + sPt1.Z + "\n";
                richTextBox1.Text += "Pt0.Depth = " + depthPixels[640 * pt0.Y + pt0.X].Depth + "\n";
                richTextBox1.Text += "Pt1.Depth = " + depthPixels[640 * pt1.Y + pt1.X].Depth + "\n";
                */

                if ( plane != null )
                {
                    SkeletonPoint isp = plane.getIntersectionPoint(sPt0, sPt1);
                    return new Tuple<SkeletonPoint, SkeletonPoint>(isp, sPt1);
                }

                /*
                Vector4 dir = new Vector4()
                {
                    X = sPt1.X - sPt0.X,
                    Y = sPt1.Y - sPt0.Y,
                    Z = sPt1.Z - sPt0.Z,
                };
                float dirLen = (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y + dir.Z * dir.Z);
                dir.X /= dirLen;
                dir.Y /= dirLen;
                dir.Z /= dirLen;

                float dx = pt1.X - pt0.X;
                float dy = pt1.Y - pt0.Y;
                float len = (float)Math.Sqrt(dx * dx + dy * dy);
                dx /= len;
                dy /= len;

                const float max_t = 1;
                const float dt = 1 * 0.001f;
                float t = 0;
                float t3d = dirLen / len;
                Vector4 dir3d = new Vector4()
                {
                    X = dir.X * t3d,
                    Y = dir.Y * t3d,
                    Z = dir.Z * t3d,
                };

                float minDist = float.MaxValue;
                SkeletonPoint minPos = new SkeletonPoint();


                while (t < max_t)
                {
                    t += dt;
                    float x3d = sPt1.X + dir.X * t;
                    float y3d = sPt1.Y + dir.Y * t;
                    float z3d = sPt1.Z + dir.Z * t;
                    var sPos = new SkeletonPoint() { X = x3d, Y = y3d, Z = z3d };
                    var pt = SkeletonPointToScreen(sPos);
                    int x = pt.X;
                    int y = pt.Y;
                    if (x < 0 || 640 <= x) continue;
                    if (y < 0 || 480 <= y) continue;

                    if (depthPixels[640 * y + x].IsKnownDepth)
                    {
                        int depth = 0;
                        if (1 <= x && x < 639 && 1 <= y && y < 479)
                        {
                            for (int py = -1; py <= 1; py++)
                            for (int px = -1; px <= 1; px++)
                            {
                                depth += depthPixels[640 * (y + py) + (x + px)].Depth;
                            }
                        }
                        depth /= 9;
                        var depthPos = SkeletonDepthPointToSkeltonPoint(x, y, depth);
                        float ddx = sPos.X - depthPos.X;
                        float ddy = sPos.Y - depthPos.Y;
                        float ddz = sPos.Z - depthPos.Z;
                        float dist = (float)Math.Sqrt(ddx * ddx + ddy * ddy + ddz * ddz);
                        if (minDist < trackBar1.Value * 0.001f)
                        {
                            return new Tuple<SkeletonPoint, SkeletonPoint>(new SkeletonPoint()
                            {
                                X = x3d,
                                Y = y3d,
                                Z = z3d
                            },
                            sPt1);
                        }
                        if (minDist > dist)
                        {
                            minDist = Math.Abs(dist);
                            minPos = new SkeletonPoint()
                            {
                                X = x3d,
                                Y = y3d,
                                Z = z3d
                            };
                        }
                    }
                }
                if (minDist != float.MaxValue)
                {
//                    richTextBox1.Text += "min:" + minDist + "\n";
                    return new Tuple<SkeletonPoint, SkeletonPoint>(minPos, sPt1);
                }
                 * */
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
            planePixels.Add(sp);

            if (planePixels.Count == 4)
            {
                planePixels.RemoveAt(0);
            }
            else if(planePixels.Count == 3) 
            {
                plane = new Plane(planePixels[0], planePixels[1], planePixels[2]);
            }
        }
    }
}
