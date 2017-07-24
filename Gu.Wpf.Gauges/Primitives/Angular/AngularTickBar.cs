namespace Gu.Wpf.Gauges
{
    using System;
    using System.Windows;
    using System.Windows.Media;

    public class AngularTickBar : AngularGeometryTickBar
    {
        /// <summary>
        /// Identifies the <see cref="P:LinearTickBar.TickWidth" /> dependency property.
        /// </summary>
        public static readonly DependencyProperty TickWidthProperty = DependencyProperty.Register(
            nameof(TickWidth),
            typeof(double),
            typeof(AngularTickBar),
            new FrameworkPropertyMetadata(
                1.0d,
                FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty TickShapeProperty = DependencyProperty.Register(
            nameof(TickShape),
            typeof(TickShape),
            typeof(AngularTickBar),
            new FrameworkPropertyMetadata(
                default(TickShape),
                FrameworkPropertyMetadataOptions.AffectsRender,
                (d, e) => ((AngularTickBar)d).ResetPen()));

        static AngularTickBar()
        {
            StrokeProperty.OverrideMetadata(
                typeof(AngularTickBar),
                new FrameworkPropertyMetadata(
                    Brushes.Black,
                    FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.SubPropertiesDoNotAffectRender,
                    (d, e) => ((AngularTickBar)d).ResetPen()));
        }

        /// <summary>
        /// Gets or sets the <see cref="P:LinearTickBar.TickWidth" />
        /// The default is 1.
        /// For TickShape.Arc the arc length on the outer diameter.
        /// </summary>
        public double TickWidth
        {
            get => (double)this.GetValue(TickWidthProperty);
            set => this.SetValue(TickWidthProperty, value);
        }

        /// <summary>
        /// Specifies if ticks are drawn as rectangles or arcs.
        /// </summary>
        public TickShape TickShape
        {
            get => (TickShape)this.GetValue(TickShapeProperty);
            set => this.SetValue(TickShapeProperty, value);
        }

        protected override Geometry DefiningGeometry => throw new InvalidOperationException("Uses OnRender");

        protected bool IsFilled
        {
            get
            {
                var strokeThickness = this.GetStrokeThickness();
                return this.Thickness > strokeThickness &&
                       this.TickWidth > strokeThickness;
            }
        }

        protected override double GetStrokeThickness()
        {
            var strokeThickness = base.GetStrokeThickness();
            if (this.TickWidth <= 2 * strokeThickness)
            {
                return this.TickWidth;
            }

            return strokeThickness;
        }

        protected override Size MeasureOverride(Size availableSize)
        {
            if (DoubleUtil.LessThanOrClose(this.Thickness, 0) ||
                double.IsInfinity(this.Thickness) ||
                this.AllTicks == null)
            {
                return default(Size);
            }

            var arc = new ArcInfo(default(Point), this.Thickness, this.Start, this.End);
            var rect = default(Rect);
            rect.Union(arc.StartPoint);
            rect.Union(arc.EndPoint);
            foreach (var quadrant in arc.QuadrantPoints)
            {
                rect.Union(quadrant);
            }

            return rect.Inflate(this.Padding).Size;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            var strokeThickness = this.GetStrokeThickness();
            var w = this.TickWidth > strokeThickness
                ? this.TickWidth / 2
                : strokeThickness / 2;
            var arc = new ArcInfo(default(Point), 1, this.Start, this.End);
            this.Overflow = arc.Overflow(w, this.Padding);
            return finalSize;
        }

        protected override void OnRender(DrawingContext dc)
        {
            if ((this.Pen == null && this.Fill == null) ||
                this.AllTicks == null ||
                DoubleUtil.AreClose(this.EffectiveValue, this.Minimum))
            {
                return;
            }

            var arc = ArcInfo.Fit(this.RenderSize, this.Padding, this.Start, this.End);
            var value = this.EffectiveValue;
            if (value < this.Maximum)
            {
                dc.PushClip(this.CreateClipGeometry(arc));
            }

            var strokeThickness = this.GetStrokeThickness();
            if (this.TickWidth <= strokeThickness &&
                this.TickShape == TickShape.Rectangle)
            {
                foreach (var tick in this.AllTicks)
                {
                    var angle = Interpolate.Linear(this.Minimum, this.Maximum, tick)
                                           .Clamp(0, 1)
                                           .Interpolate(this.Start, this.End, this.IsDirectionReversed);
                    var po = arc.GetPoint(angle);
                    var pi = arc.GetPointAtRadiusOffset(angle, -this.Thickness);
                    dc.DrawLine(this.Pen, po, pi);
                }
            }
            else
            {
                var geometry = new PathGeometry();
                foreach (var tick in this.AllTicks)
                {
                    geometry.Figures.Add(this.CreateTick(arc, tick, strokeThickness));
                    if (tick > value)
                    {
                        break;
                    }
                }

                if (this.IsFilled)
                {
                    dc.DrawGeometry(this.Fill, this.Pen, geometry);
                }
                else
                {
                    dc.DrawGeometry(this.Stroke, null, geometry);
                }
            }

            if (value < this.Maximum)
            {
                dc.Pop();
            }
        }

        /// <summary>
        /// Create the clip geometry that clips to current value.
        /// If not desired override and return Geometry.Empty
        /// </summary>
        /// <param name="arc">The bounding arc.</param>
        protected virtual Geometry CreateClipGeometry(ArcInfo arc)
        {
            var effectiveAngle = Interpolate.Linear(this.Minimum, this.Maximum, this.EffectiveValue)
                .Interpolate(this.Start, this.End, this.IsDirectionReversed);
            var geometry = new PathGeometry();
            var w = this.TickWidth;
            var delta = arc.GetDelta(w, arc.Radius - this.Thickness);
            var inflated = new ArcInfo(
                arc.Center,
                arc.Radius + w,
                arc.StartAngle - delta,
                arc.EndAngle + delta);
            var figure = inflated.CreateArcPathFigure(
                this.IsDirectionReversed ? inflated.EndAngle : inflated.StartAngle,
                effectiveAngle,
                inflated.Radius,
                0);
            geometry.Figures.Add(figure);
            return geometry;
        }

        /// <summary>
        /// Create a <see cref="PathFigure"/> for the current tick.
        /// </summary>
        /// <param name="arc">The bounding arc.</param>
        /// <param name="value">The tick value.</param>
        /// <param name="strokeThickness">The stroke thickness.</param>
        protected virtual PathFigure CreateTick(ArcInfo arc, double value, double strokeThickness)
        {
            var interpolation = Interpolate.Linear(this.Minimum, this.Maximum, value)
                                           .Clamp(0, 1);
            var angle = interpolation.Interpolate(this.Start, this.End, this.IsDirectionReversed);
            switch (this.TickShape)
            {
                case TickShape.Arc:
                    if (this.IsFilled)
                    {
                        var delta = arc.GetDelta((this.TickWidth - strokeThickness) / 2);
                        return arc.CreateArcPathFigure(angle - delta, angle + delta, this.Thickness, strokeThickness);
                    }
                    else
                    {
                        var delta = arc.GetDelta(this.TickWidth / 2);
                        return arc.CreateArcPathFigure(angle - delta, angle + delta, this.Thickness, 0);
                    }

                case TickShape.Rectangle:
                    {
                        var w = this.TickWidth - strokeThickness;
                        var delta = arc.GetDelta(w / 2);
                        var po1 = arc.GetPointAtRadiusOffset(angle - delta, -strokeThickness / 2);
                        var po2 = arc.GetPointAtRadiusOffset(angle + delta, -strokeThickness / 2);
                        var ip = arc.GetPointAtRadiusOffset(angle, -this.Thickness + (strokeThickness / 2));
                        var v = po1 - po2;
                        var pi1 = ip - (v / 2);
                        var pi2 = pi1 + v;
                        var isStroked = DoubleUtil.GreaterThan(strokeThickness, 0);
                        return new PathFigure(
                            pi2,
                            new[]
                            {
                            new LineSegment(po1, isStroked),
                            new LineSegment(po2, isStroked),
                            new LineSegment(pi1, isStroked),
                            new LineSegment(pi2, isStroked),
                            },
                            closed: true);
                    }

                case TickShape.RingSection:
                    {
                        var w = this.TickWidth - strokeThickness;
                        var deltaO = arc.GetDelta(w / 2);
                        var po1 = arc.GetPointAtRadiusOffset(angle - deltaO, -strokeThickness / 2);
                        var po2 = arc.GetPointAtRadiusOffset(angle + deltaO, -strokeThickness / 2);
                        var ip = arc.GetPointAtRadiusOffset(angle, -this.Thickness + (strokeThickness / 2));
                        var v = (po1 - po2) / 2;
                        var ri = arc.Radius - this.Thickness + (strokeThickness / 2);
                        var ai1 = arc.GetAngle(ip - v);
                        var pi1 = arc.GetPointAtRadius(ai1, ri);
                        var deltaI = 2 * Vector.AngleBetween(arc.GetPoint(angle) - arc.Center, pi1 - arc.Center);
                        var ai2 = ai1 - deltaI;
                        var isStroked = DoubleUtil.GreaterThan(strokeThickness, 0);
                        return new PathFigure(
                            po1,
                            new PathSegment[]
                            {
                                arc.CreateArcSegment(
                                    angle - deltaO,
                                    angle + deltaO,
                                    arc.Radius - (strokeThickness / 2),
                                    strokeThickness > 0),
                                new LineSegment(pi1, isStroked),
                                arc.CreateArcSegment(
                                    ai1,
                                    ai2,
                                    ri,
                                    strokeThickness > 0),
                                new LineSegment(po1, isStroked),
                            },
                            closed: true);
                    }

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}