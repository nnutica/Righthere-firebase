using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui; // Added missing using directive

namespace Firebasemauiapp.Controls
{
    // A simple half-circle discrete mood slider made of clickable dots.
    // Value mapped 1..10 across 10 dots. Supports tap + drag.
    public class MoodArcSlider : ContentView
    {
        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            nameof(Value), typeof(int?), typeof(MoodArcSlider), null, BindingMode.TwoWay, propertyChanged: OnValueChanged);

        public int? Value
        {
            get => (int?)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        private readonly AbsoluteLayout _layout = new AbsoluteLayout();
        private readonly List<View> _dots = new();
        private const int DotCount = 10; // 1..10

        // Geometry cache for drag calculations
        private double _radius;
        private double _centerX;
        private double _centerY;
        private Point _dragStart;
        // Downward arc (smile): left -> right; use lower half of the circle
        private const double StartDeg = 160.0; // left
        private const double EndDeg = 20.0;    // right

        public MoodArcSlider()
        {
            Content = _layout;
            SizeChanged += (_, _) => BuildDots();
        }

        private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MoodArcSlider slider)
                slider.UpdateDotStyles();
        }

        private void BuildDots()
        {
            if (Width <= 0 || Height <= 0) return;
            _layout.Children.Clear();
            _dots.Clear();

            // Use available width, set arc radius.
            double radius = Math.Min(Width, Height * 2) / 2.0; // ensure fits vertically as half circle
            double centerX = Width / 2.0;
            // Place circle center at top so the visible arc is the lower half (curving down)
            double centerY = 0;

            _radius = radius;
            _centerX = centerX;
            _centerY = centerY;

            // Angles for smile arc (left 160° to right 20°)
            double startDeg = StartDeg;
            double endDeg = EndDeg;
            for (int i = 0; i < DotCount; i++)
            {
                double t = i / (double)(DotCount - 1);
                // Lerp even if end < start (wrap not crossing 0 here)
                double deg = startDeg + (endDeg - startDeg) * t;
                double rad = deg * Math.PI / 180.0;
                // Larger, responsive dots with bigger tap area
                double dotRadius = Math.Max(14, Width * 0.05);
                double x = centerX + radius * Math.Cos(rad);
                double y = centerY + radius * Math.Sin(rad);

                // Create a larger invisible tap zone for easier selection
                double tapSize = dotRadius * 2.2; // generous target, avoid overlap
                var tapZone = new Grid { WidthRequest = tapSize, HeightRequest = tapSize, BackgroundColor = Colors.Transparent };

                // Visual dot inside the tap zone
                var frame = new Border
                {
                    WidthRequest = dotRadius * 2,
                    HeightRequest = dotRadius * 2,
                    BackgroundColor = Colors.Black,
                    Stroke = Colors.White,
                    StrokeThickness = 1,
                    StrokeShape = new RoundRectangle { CornerRadius = (float)dotRadius },
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
                tapZone.Add(frame);

                var tap = new TapGestureRecognizer();
                int idx = i;
                tap.Tapped += (_, __) =>
                {
                    Value = idx + 1; // map to 1..10 (0 means none)
                };
                tapZone.GestureRecognizers.Add(tap);

                // Add pan for continuous dragging starting from this dot
                var pan = new PanGestureRecognizer();
                Point startFrom = new Point(x, y);
                pan.PanUpdated += (_, e) =>
                {
                    if (e.StatusType == GestureStatus.Started)
                    {
                        _dragStart = startFrom; // start from the dot center
                    }
                    else if (e.StatusType == GestureStatus.Running)
                    {
                        var current = new Point(_dragStart.X + e.TotalX, _dragStart.Y + e.TotalY);
                        UpdateValueFromPoint(current);
                    }
                };
                tapZone.GestureRecognizers.Add(pan);

                // Position: adjust by dotRadius to center
                AbsoluteLayout.SetLayoutBounds(tapZone, new Rect(x - tapSize / 2, y - tapSize / 2, tapSize, tapSize));
                _layout.Children.Add(tapZone);
                _dots.Add(frame); // keep reference to visual dot for styling
            }

            UpdateDotStyles();
        }

        private void UpdateValueFromPoint(Point pt)
        {
            // Convert point to angle around the circle center (0..360)
            double ang = Math.Atan2(pt.Y - _centerY, pt.X - _centerX) * 180.0 / Math.PI;
            if (ang < 0) ang += 360;

            // Map along the SHORTEST path from StartDeg (left) to EndDeg (right)
            static double SignedDelta(double from, double to)
            {
                double d = (to - from + 540) % 360 - 180; // -180..180
                return d;
            }

            double start = StartDeg;
            double end = EndDeg;
            double diff = SignedDelta(start, end); // negative (~-140) for smile arc
            double d = SignedDelta(start, ang);

            // Clamp d to the arc range [min(diff,0) .. max(diff,0)]
            if (diff < 0)
                d = Math.Max(diff, Math.Min(0, d));
            else
                d = Math.Min(diff, Math.Max(0, d));

            // Progress 0..1 from left(0) to right(1)
            double t = diff == 0 ? 0 : Math.Abs(d / diff);

            // Map to discrete 0..10
            int newVal = (int)Math.Round(t * DotCount);
            newVal = Math.Max(0, Math.Min(DotCount, newVal));
            if (Value == null || newVal != Value.Value)
                Value = newVal;
        }

        private void UpdateDotStyles()
        {
            if (_dots.Count == 0) return;
            int v = Value ?? 0;
            v = Math.Max(0, Math.Min(DotCount, v));
            int activeIndex = Math.Clamp(v - 1, -1, DotCount - 1);
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] is Border f)
                {
                    bool isFilled = i < v;                // fill from left up to Value
                    bool isActive = i == activeIndex;      // last filled dot
                    f.BackgroundColor = isFilled ? Colors.White : Colors.Black;
                    f.Scale = isActive ? 1.25 : 1.0;
                    f.Stroke = Colors.White;
                    f.StrokeThickness = isActive ? 3 : 1;
                    if (isActive)
                    {
                        f.Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.25f, Radius = 8, Offset = new Point(0, 2) };
                    }
                    else
                    {
                        // Assign an empty Shadow to satisfy non-nullable requirement
                        f.Shadow = new Shadow { Opacity = 0, Radius = 0 };
                    }
                }
            }
        }

        // Default color for non-selected dots (black)
        private Color InterpolateColor(int index)
        {
            return Colors.Black;
        }
    }
}
