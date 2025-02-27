// Copyright 2008 - Paul den Dulk (Geodan)
// 
// This file is part of SharpMap.
// Mapsui is free software; you can redistribute it and/or modify
// it under the terms of the GNU Lesser General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// SharpMap is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU Lesser General Public License for more details.

// You should have received a copy of the GNU Lesser General Public License
// along with SharpMap; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA f

#nullable enable

using System;
using System.Threading.Tasks;
using Windows.Devices.Sensors;
using Windows.Foundation;
using Windows.System;
#if __WINUI__
using System.Runtime.Versioning;
using Mapsui.UI.WinUI.Extensions;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using SkiaSharp.Views.Windows;
using HorizontalAlignment = Microsoft.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Microsoft.UI.Xaml.VerticalAlignment;
#else
using Mapsui.UI.Uwp.Extensions;
using Windows.Graphics.Display;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Shapes;
using SkiaSharp.Views.UWP;
using HorizontalAlignment = Windows.UI.Xaml.HorizontalAlignment;
using VerticalAlignment = Windows.UI.Xaml.VerticalAlignment;
#endif

#if __WINUI__
[assembly: SupportedOSPlatform("windows10.0.18362.0")]
namespace Mapsui.UI.WinUI
#else
namespace Mapsui.UI.Uwp
#endif
{
    public partial class MapControl : Grid, IMapControl
    {
        private readonly Rectangle _selectRectangle = CreateSelectRectangle();
        private readonly SKXamlCanvas _canvas = CreateRenderTarget();
        private double _innerRotation;

        public MouseWheelAnimation MouseWheelAnimation { get; } = new MouseWheelAnimation { Duration = 0 };

        public MapControl()
        {
            CommonInitialize();
            Initialize();
        }

        private void Initialize()
        {
            _invalidate = () => {
                // The commented out code crashes the app when MouseWheelAnimation.Duration > 0. Could be a bug in SKXamlCanvas
                //if (Dispatcher.HasThreadAccess) _canvas?.Invalidate();
                //else RunOnUIThread(() => _canvas?.Invalidate());
                RunOnUIThread(() => _canvas?.Invalidate());
            };

            Background = new SolidColorBrush(Colors.White); // DON'T REMOVE! Touch events do not work without a background

            Children.Add(_canvas);
            Children.Add(_selectRectangle);

            _canvas.PaintSurface += Canvas_PaintSurface;

            Loaded += MapControlLoaded;

            SizeChanged += MapControlSizeChanged;

            PointerWheelChanged += MapControl_PointerWheelChanged;

            ManipulationMode = ManipulationModes.Scale | ManipulationModes.TranslateX | ManipulationModes.TranslateY | ManipulationModes.Rotate;
            ManipulationStarted += OnManipulationStarted;
            ManipulationDelta += OnManipulationDelta;
            ManipulationCompleted += OnManipulationCompleted;

            ManipulationInertiaStarting += OnManipulationInertiaStarting;

            Tapped += OnSingleTapped;
            DoubleTapped += OnDoubleTapped;

            var orientationSensor = SimpleOrientationSensor.GetDefault();
            if (orientationSensor != null)
                orientationSensor.OrientationChanged += (sender, args) => RunOnUIThread(() => Refresh());
        }

        private void OnManipulationCompleted(object sender, ManipulationCompletedRoutedEventArgs e)
        {
            RefreshData();
            Console.WriteLine(Guid.NewGuid());
        }

        private void OnManipulationStarted(object sender, ManipulationStartedRoutedEventArgs e)
        {
            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            _innerRotation = _viewport.Rotation;
        }

        private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
        {
            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            var tapPosition = e.GetPosition(this).ToMapsui();
            OnInfo(InvokeInfo(tapPosition, tapPosition, 2));
        }

        private void OnSingleTapped(object sender, TappedRoutedEventArgs e)
        {
            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            var tabPosition = e.GetPosition(this).ToMapsui();
            OnInfo(InvokeInfo(tabPosition, tabPosition, 1));
        }

        private static Rectangle CreateSelectRectangle()
        {
            return new Rectangle
            {
                Fill = new SolidColorBrush(Colors.Red),
                Stroke = new SolidColorBrush(Colors.Black),
                StrokeThickness = 3,
                RadiusX = 0.5,
                RadiusY = 0.5,
                StrokeDashArray = new DoubleCollection { 3.0 },
                Opacity = 0.3,
                VerticalAlignment = VerticalAlignment.Top,
                HorizontalAlignment = HorizontalAlignment.Left,
                Visibility = Visibility.Collapsed
            };
        }

        private static SKXamlCanvas CreateRenderTarget()
        {
            return new SKXamlCanvas
            {
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Background = new SolidColorBrush(Colors.Transparent)
            };
        }

        private void MapControl_PointerWheelChanged(object sender, PointerRoutedEventArgs e)
        {
            if (_map?.ZoomLock ?? true) return;
            if (!Viewport.HasSize) return;

            var currentPoint = e.GetCurrentPoint(this);

            var mousePosition = new MPoint(currentPoint.RawPosition.X, currentPoint.RawPosition.Y);

            var resolution = MouseWheelAnimation.GetResolution(currentPoint.Properties.MouseWheelDelta, _viewport, _map);
            // Limit target resolution before animation to avoid an animation that is stuck on the max resolution, which would cause a needless delay
            resolution = Map.Limiter.LimitResolution(resolution, Viewport.Width, Viewport.Height, Map.Resolutions, Map.Envelope);
            Navigator.ZoomTo(resolution, mousePosition, MouseWheelAnimation.Duration, MouseWheelAnimation.Easing);

            e.Handled = true;
        }

        private void MapControlLoaded(object sender, RoutedEventArgs e)
        {
            SetViewportSize();
        }

        private void MapControlSizeChanged(object sender, SizeChangedEventArgs e)
        {
            Clip = new RectangleGeometry { Rect = new Rect(0, 0, ActualWidth, ActualHeight) };
            SetViewportSize();
        }

        private void RunOnUIThread(Action action)
        {
#if __WINUI__
            Task.Run(() => DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Normal, () => action()));
#else
            Task.Run(() => Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action()));
#endif
        }

        private void Canvas_PaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        {
            if (PixelDensity <= 0)
                return;

            var canvas = e.Surface.Canvas;

            canvas.Scale(PixelDensity, PixelDensity);

            CommonDrawControl(canvas);
        }

        [Obsolete("Use MapControl.Navigate.NavigateTo instead", true)]
        public void ZoomToBox(Geometries.Point beginPoint, Geometries.Point endPoint) { }

        private static void OnManipulationInertiaStarting(object sender, ManipulationInertiaStartingRoutedEventArgs e)
        {
            e.TranslationBehavior.DesiredDeceleration = 25 * 96.0 / (1000.0 * 1000.0);
        }

        private void OnManipulationDelta(object sender, ManipulationDeltaRoutedEventArgs e)
        {
            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();

            var center = e.Position.ToMapsui();
            var radius = e.Delta.Scale;
            var rotation = e.Delta.Rotation;

            var previousCenter = e.Position.ToMapsui().Offset(-e.Delta.Translation.X, -e.Delta.Translation.Y);
            var previousRadius = 1f;
            var previousRotation = 0f;

            double rotationDelta = 0;

            if (!(Map?.RotationLock ?? false))
            {
                _innerRotation += rotation - previousRotation;
                _innerRotation %= 360;

                if (_innerRotation > 180)
                    _innerRotation -= 360;
                else if (_innerRotation < -180)
                    _innerRotation += 360;

                if (Viewport.Rotation == 0 && Math.Abs(_innerRotation) >= Math.Abs(UnSnapRotationDegrees))
                    rotationDelta = _innerRotation;
                else if (Viewport.Rotation != 0)
                {
                    if (Math.Abs(_innerRotation) <= Math.Abs(ReSnapRotationDegrees))
                        rotationDelta = -Viewport.Rotation;
                    else
                        rotationDelta = _innerRotation - Viewport.Rotation;
                }
            }

            _viewport.Transform(center, previousCenter, radius / previousRadius, rotationDelta);
            RefreshGraphics();
            e.Handled = true;
        }

        public void OpenBrowser(string url)
        {
            Task.Run(() => Launcher.LaunchUriAsync(new Uri(url)));
        }

        private float ViewportWidth => (float)ActualWidth;
        private float ViewportHeight => (float)ActualHeight;

        private float GetPixelDensity()
        {
#if __WINUI__
            return (float)XamlRoot.RasterizationScale;
#else
            return (float)DisplayInformation.GetForCurrentView().RawPixelsPerViewPixel;
#endif
        }

    }
}