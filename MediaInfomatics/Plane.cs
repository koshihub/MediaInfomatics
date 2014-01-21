
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace MediaInfomatics
{
    class Plane
    {
        double a, b, c, d;
        public Plane(SkeletonPoint p1, SkeletonPoint p2, SkeletonPoint p3)
        {
            a = (p2.Y - p1.Y) * (p3.Z - p1.Z) - (p3.Y - p1.Y) * (p2.Z - p1.Z);
            b = (p2.Z - p1.Z) * (p3.X - p1.X) - (p3.Z - p1.Z) * (p2.X - p1.X);
            c = (p2.X - p1.X) * (p3.Y - p1.Y) - (p3.X - p1.X) * (p2.Y - p1.Y);
            d = -(a * p1.X + b * p1.Y + c * p1.Z);
        }

        public SkeletonPoint getIntersectionPoint(SkeletonPoint p1, SkeletonPoint p2)
        {
            Vector4 dir = new Vector4()
            {
                X = p2.X - p1.X,
                Y = p2.Y - p1.Y,
                Z = p2.Z - p1.Z
            };

            double dDe = a * dir.X + b * dir.Y + c * dir.Z;
            if (dDe == 0.0)
            {
                return new SkeletonPoint();
            }

            double t = - ( a * p1.X + b * p1.Y + c * p1.Z + d ) / dDe;
            SkeletonPoint p = new SkeletonPoint()
            {
                X = (float)(p1.X + t * dir.X),
                Y = (float)(p1.Y + t * dir.Y),
                Z = (float)(p1.Z + t * dir.Z)
            };
            return p;
        }

        public double checkPlane(SkeletonPoint p)
        {
            return (p.X * a + p.Y * b + p.Z * c + d);
        }
    }
}