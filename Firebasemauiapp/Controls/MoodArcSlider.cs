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
            nameof(Value), typeof(int), typeof(MoodArcSlider), 5, BindingMode.TwoWay, propertyChanged: OnValueChanged);

        public int Value
        {
            get => (int)GetValue(ValueProperty);
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
        private const double StartDeg = 200.0;
        private const double EndDeg = 340.0;

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
            double centerY = radius; // center above arc (arc is lower half of circle)

            _radius = radius;
            _centerX = centerX;
            _centerY = centerY;

            // Angles for smile arc (200° to 340°) degrees converted to radians
            // Angles for smile arc (200° to 340°)
            double startDeg = StartDeg;
            double endDeg = EndDeg;
            for (int i = 0; i < DotCount; i++)
            {
                double t = i / (double)(DotCount - 1);
                double deg = startDeg + (endDeg - startDeg) * t;
                double rad = deg * Math.PI / 180.0;
                // Larger, responsive dots with bigger tap area
                double dotRadius = Math.Max(14, Width * 0.05);
                double x = centerX + radius * Math.Cos(rad);
                double y = centerY + radius * Math.Sin(rad);

                // Create a larger invisible tap zone for easier selection
                double tapSize = dotRadius * 2.6; // generous target
                var tapZone = new Grid { WidthRequest = tapSize, HeightRequest = tapSize, BackgroundColor = Colors.Transparent };

                // Visual dot inside the tap zone
                var frame = new Border
                {
                    WidthRequest = dotRadius * 2,
                    HeightRequest = dotRadius * 2,
                    BackgroundColor = InterpolateColor(i),
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
                    Value = idx + 1; // map to 1..10
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
            // Convert point to angle around the circle center
            double ang = Math.Atan2(pt.Y - _centerY, pt.X - _centerX) * 180.0 / Math.PI;
            if (ang < 0) ang += 360; // normalize to 0..360

            // Clamp to arc range
            double clamped = Math.Max(StartDeg, Math.Min(EndDeg, ang));
            double t = (clamped - StartDeg) / (EndDeg - StartDeg);
            int newVal = (int)Math.Round(t * 9) + 1; // 1..10
            if (newVal != Value)
                Value = newVal;
        }

        private void UpdateDotStyles()
        {
            if (_dots.Count == 0) return;
            int activeIndex = Math.Clamp(Value - 1, 0, DotCount - 1);
            for (int i = 0; i < _dots.Count; i++)
            {
                if (_dots[i] is Border f)
                {
                    bool isActive = i == activeIndex;
                    f.BackgroundColor = InterpolateColor(i);
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

        // Simple gradient: first half lighten (white), move to orange (#D18B2A) on right half
        private Color InterpolateColor(int index)
        {
            // White to #D18B2A gradient
            Color start = Colors.White;
            Color end = Color.FromArgb("#D18B2A");
            double t = index / (double)(DotCount - 1);
            // Slight bias: keep first 2 dots pure white
            if (index <= 1) return start;
            byte r = (byte)(start.Red * 255 + (end.Red * 255 - start.Red * 255) * t);
            byte g = (byte)(start.Green * 255 + (end.Green * 255 - start.Green * 255) * t);
            byte b = (byte)(start.Blue * 255 + (end.Blue * 255 - start.Blue * 255) * t);
            return Color.FromRgb(r, g, b);
        }
    }
}
