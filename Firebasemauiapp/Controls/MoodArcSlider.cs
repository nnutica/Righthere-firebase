using System;
using System.Collections.Generic;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Controls.Shapes;
using Microsoft.Maui;

namespace Firebasemauiapp.Controls
{
    // Horizontal linear mood slider with single row of connected dots
    // Value mapped 0..10 across 10 dots. Supports tap + drag.
    // COLOR CONFIGURATION:
    // - Line 87, 120: Unfilled color = #F8A33A (orange)
    // - Line 182, 183: Filled color = Colors.White (white)
    // - Line 184, 185: Unfilled color = #F8A33A (orange)
    // - Line 192: Filled line color = Colors.White (white)
    // - Line 192: Unfilled line color = #F8A33A (orange)
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
        private readonly List<Border> _dots = new();
        private readonly List<BoxView> _lines = new();
        private const int DotCount = 10;
        private bool _isBuilt = false;

        public MoodArcSlider()
        {
            Content = _layout;
            SizeChanged += OnSizeChanged;
            Loaded += OnLoaded;
        }

        private void OnSizeChanged(object sender, EventArgs e)
        {
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(50);
                BuildDots();
            });
        }

        private void OnLoaded(object sender, EventArgs e)
        {
            Dispatcher.Dispatch(async () =>
            {
                await Task.Delay(50);
                BuildDots();
            });
        }

        private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is MoodArcSlider slider)
                slider.UpdateDotStyles();
        }

        private void BuildDots()
        {
            // Use WidthRequest/HeightRequest as fallback if Width/Height not set yet
            double actualWidth = Width > 0 ? Width : WidthRequest;
            double actualHeight = Height > 0 ? Height : HeightRequest;

            if (actualWidth <= 0 || actualHeight <= 0) return;
            if (_isBuilt) return; // Prevent rebuilding multiple times

            try
            {
                _layout.Children.Clear();
                _dots.Clear();
                _lines.Clear();
                _isBuilt = true;

                double dotSize = 24; // Optimized size to not overlap lines
                double lineThickness = 8; // Increased from 3 to 5
                double spacing = (actualWidth - dotSize * DotCount) / (DotCount - 1);
                double rowY = (actualHeight - dotSize) / 2; // Center vertically

                // Build lines first (so they appear behind dots)
                for (int i = 0; i < DotCount - 1; i++)
                {
                    double x = i * (dotSize + spacing);
                    double y = rowY;

                    var line = new BoxView
                    {
                        Color = Color.FromArgb("#CE8A30"), // Default: orange (unfilled)
                        HeightRequest = lineThickness
                    };
                    AbsoluteLayout.SetLayoutBounds(line, new Rect(x + dotSize, y + dotSize / 2 - lineThickness / 2, spacing, lineThickness));
                    _layout.Children.Add(line);
                    _lines.Add(line);
                }

                // Build dots on top of lines
                for (int i = 0; i < DotCount; i++)
                {
                    double x = i * (dotSize + spacing);
                    double y = rowY;

                    var dot = CreateDot(dotSize, i);
                    AbsoluteLayout.SetLayoutBounds(dot, new Rect(x, y, dotSize, dotSize));
                    _layout.Children.Add(dot);
                    _dots.Add(dot);
                }

                UpdateDotStyles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MoodArcSlider] BuildDots error: {ex.Message}");
                _isBuilt = false; // Allow retry
            }
        }

        private Border CreateDot(double size, int index)
        {
            var dot = new Border
            {
                WidthRequest = size,
                HeightRequest = size,
                BackgroundColor = Color.FromArgb("#ac7328df"), // Default: orange (unfilled)
                Stroke = Color.FromArgb("#ac7328df"),
                StrokeThickness = 5, // Increased from 2 to 5 for thicker border
                StrokeShape = new RoundRectangle { CornerRadius = (float)(size / 2) }
            };

            var tap = new TapGestureRecognizer();
            int idx = index % DotCount;
            tap.Tapped += (_, __) =>
            {
                Value = idx + 1;
            };
            dot.GestureRecognizers.Add(tap);

            var pan = new PanGestureRecognizer();
            pan.PanUpdated += (_, e) =>
            {
                if (e.StatusType == GestureStatus.Running)
                {
                    double actualWidth = Width > 0 ? Width : WidthRequest;
                    double spacing = (actualWidth - size * DotCount) / (DotCount - 1);
                    UpdateValueFromX(e.TotalX + (idx * (size + spacing)));
                }
            };
            dot.GestureRecognizers.Add(pan);

            return dot;
        }

        private void UpdateValueFromX(double x)
        {
            double actualWidth = Width > 0 ? Width : WidthRequest;
            if (actualWidth <= 0) return;

            double dotSize = 28; // Must match BuildDots
            double spacing = (actualWidth - dotSize * DotCount) / (DotCount - 1);
            double totalStep = dotSize + spacing;

            int newVal = (int)Math.Round(x / totalStep);
            newVal = Math.Max(0, Math.Min(DotCount, newVal));

            if (Value == null || newVal != Value.Value)
                Value = newVal;
        }

        private void UpdateDotStyles()
        {
            if (_dots.Count == 0) return;
            int v = Value ?? 0;
            v = Math.Max(0, Math.Min(DotCount, v));

            // Update all dots in single row
            // Filled = solid white circle, Unfilled = hollow orange circle
            for (int i = 0; i < _dots.Count; i++)
            {
                bool isFilled = i < v;

                if (isFilled)
                {
                    // Filled: solid white circle
                    _dots[i].BackgroundColor = Colors.White;
                    _dots[i].Stroke = Colors.White;
                }
                else
                {
                    // Unfilled: hollow circle with orange border
                    _dots[i].BackgroundColor = Colors.Transparent;
                    _dots[i].Stroke = Color.FromArgb("#CE8A30");
                }
            }

            // Update connecting lines
            for (int i = 0; i < _lines.Count; i++)
            {
                bool isFilled = i < v - 1;
                _lines[i].Color = isFilled ? Colors.White : Color.FromArgb("#CE8A30");
            }
        }
    }
}
