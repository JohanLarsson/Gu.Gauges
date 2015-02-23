﻿using System.Windows.Controls.Primitives;

namespace Gu.Gauges
{
    using System.Windows;

    internal struct Line
    {
        internal readonly Point StartPoint;
        internal readonly Point EndPoint;

        public Line(Point startPoint, Point endPoint)
            : this()
        {
            this.StartPoint = startPoint;
            this.EndPoint = endPoint;
        }

        public Line(double actualWidth, double actualHeight, double reservedSpace, TickBarPlacement placement, bool isDirectionReversed)
        {
            Point p1;
            Point p2;
            if (placement == TickBarPlacement.Bottom || placement == TickBarPlacement.Top)
            {
                p1 = new Point(reservedSpace / 2, 0);
                p2 = new Point(actualWidth - reservedSpace / 2, 0);
            }
            else
            {
                p1 = new Point(0, actualHeight - reservedSpace / 2);
                p2 = new Point(0, reservedSpace / 2);
            }
            if (isDirectionReversed)
            {
                this.StartPoint = p2;
                this.EndPoint = p1;
            }
            else
            {
                this.StartPoint = p1;
                this.EndPoint = p2;
            }
        }

        public double Length
        {
            get
            {
                var v = this.EndPoint - this.StartPoint;
                return v.Length;
            }
        }

        public override string ToString()
        {
            return string.Format("StartPoint: ({0}, {1}), EndPoint: ({2}, {3}), Length: {4}",
                this.StartPoint.X,
                this.StartPoint.Y,
                this.EndPoint.X,
                this.EndPoint.Y,
                this.Length);
        }
    }
}