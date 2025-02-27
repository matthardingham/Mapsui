using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CoreFoundation;
using CoreGraphics;
using Foundation;
using Mapsui.Geometries.Utilities;
using Mapsui.UI.iOS.Extensions;
using SkiaSharp.Views.iOS;
using UIKit;

#nullable enable

namespace Mapsui.UI.iOS
{
    [Register("MapControl"), DesignTimeVisible(true)]
    public partial class MapControl : UIView, IMapControl
    {
        private readonly SKGLView _canvas = new SKGLView();
        private double _innerRotation;

        public MapControl(CGRect frame)
            : base(frame)
        {
            CommonInitialize();
            Initialize();
        }

        [Preserve]
        public MapControl(IntPtr handle) : base(handle) // used when initialized from storyboard
        {
            CommonInitialize();
            Initialize();
        }

        private void Initialize()
        {
            _invalidate = () => {
                RunOnUIThread(() => {
                    SetNeedsDisplay();
                    _canvas?.SetNeedsDisplay();
                });
            };

            BackgroundColor = UIColor.White;

            _canvas.TranslatesAutoresizingMaskIntoConstraints = false;
            _canvas.MultipleTouchEnabled = true;
            _canvas.PaintSurface += OnPaintSurface;
            AddSubview(_canvas);

            AddConstraints(new[]
            {
                NSLayoutConstraint.Create(this, NSLayoutAttribute.Leading, NSLayoutRelation.Equal, _canvas,
                    NSLayoutAttribute.Leading, 1.0f, 0.0f),
                NSLayoutConstraint.Create(this, NSLayoutAttribute.Trailing, NSLayoutRelation.Equal, _canvas,
                    NSLayoutAttribute.Trailing, 1.0f, 0.0f),
                NSLayoutConstraint.Create(this, NSLayoutAttribute.Top, NSLayoutRelation.Equal, _canvas,
                    NSLayoutAttribute.Top, 1.0f, 0.0f),
                NSLayoutConstraint.Create(this, NSLayoutAttribute.Bottom, NSLayoutRelation.Equal, _canvas,
                    NSLayoutAttribute.Bottom, 1.0f, 0.0f)
            });

            ClipsToBounds = true;
            MultipleTouchEnabled = true;
            UserInteractionEnabled = true;

            var doubleTapGestureRecognizer = new UITapGestureRecognizer(OnDoubleTapped)
            {
                NumberOfTapsRequired = 2,
                CancelsTouchesInView = false,
            };
            AddGestureRecognizer(doubleTapGestureRecognizer);

            var tapGestureRecognizer = new UITapGestureRecognizer(OnSingleTapped)
            {
                NumberOfTapsRequired = 1,
                CancelsTouchesInView = false,
            };
            tapGestureRecognizer.RequireGestureRecognizerToFail(doubleTapGestureRecognizer);
            AddGestureRecognizer(tapGestureRecognizer);

            _viewport.SetSize(ViewportWidth, ViewportHeight);
        }


        private void OnDoubleTapped(UITapGestureRecognizer gesture)
        {
            var position = GetScreenPosition(gesture.LocationInView(this));
            OnInfo(InvokeInfo(position, position, 2));
        }

        private void OnSingleTapped(UITapGestureRecognizer gesture)
        {
            var position = GetScreenPosition(gesture.LocationInView(this));
            OnInfo(InvokeInfo(position, position, 1));
        }

        private void OnPaintSurface(object sender, SKPaintGLSurfaceEventArgs args)
        {
            if (PixelDensity <= 0)
                return;

            var canvas = args.Surface.Canvas;

            canvas.Scale(PixelDensity, PixelDensity);

            CommonDrawControl(canvas);
        }

        public override void TouchesBegan(NSSet touches, UIEvent? evt)
        {
            base.TouchesBegan(touches, evt);

            _innerRotation = Viewport.Rotation;

            // We have a new interaction with the screen, so stop all navigator animations
            Navigator.StopRunningAnimation();
        }

        public override void TouchesMoved(NSSet touches, UIEvent? evt)
        {
            base.TouchesMoved(touches, evt);

            if (evt?.AllTouches.Count == 1)
            {
                if (touches.AnyObject is UITouch touch)
                {
                    var position = touch.LocationInView(this).ToMapsui();
                    var previousPosition = touch.PreviousLocationInView(this).ToMapsui();

                    _viewport.Transform(position, previousPosition);
                    RefreshGraphics();

                    _innerRotation = Viewport.Rotation;
                }
            }
            else if (evt?.AllTouches.Count >= 2)
            {
                var previousLocation = evt.AllTouches.Select(t => ((UITouch)t).PreviousLocationInView(this))
                    .Select(p => new MPoint(p.X, p.Y)).ToList();

                var locations = evt.AllTouches.Select(t => ((UITouch)t).LocationInView(this))
                    .Select(p => new MPoint(p.X, p.Y)).ToList();

                var (previousCenter, previousRadius, previousAngle) = GetPinchValues(previousLocation);
                var (center, radius, angle) = GetPinchValues(locations);

                double rotationDelta = 0;

                if (!(Map?.RotationLock ?? false))
                {
                    _innerRotation += angle - previousAngle;
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
            }
        }

        public override void TouchesEnded(NSSet touches, UIEvent? e)
        {
            Refresh();
        }

        /// <summary>
        /// Gets screen position in device independent units (or DIP or DP).
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private MPoint GetScreenPosition(CGPoint point)
        {
            return new MPoint(point.X, point.Y);
        }

        private void RunOnUIThread(Action action)
        {
            DispatchQueue.MainQueue.DispatchAsync(action);
        }

        public override CGRect Frame
        {
            get => base.Frame;
            set
            {
                _canvas.Frame = value;
                base.Frame = value;
                SetViewportSize();
                OnPropertyChanged();
            }
        }

        public override void LayoutMarginsDidChange()
        {
            if (_canvas == null) return;

            base.LayoutMarginsDidChange();
            SetViewportSize();
        }

        public void OpenBrowser(string url)
        {
            UIApplication.SharedApplication.OpenUrl(new NSUrl(url));
        }

        public new void Dispose()
        {
            Unsubscribe();
            base.Dispose();
        }

        protected override void Dispose(bool disposing)
        {
            Unsubscribe();
            base.Dispose(disposing);
        }

        private static (MPoint centre, double radius, double angle) GetPinchValues(List<MPoint> locations)
        {
            if (locations.Count < 2)
                throw new ArgumentException();

            double centerX = 0;
            double centerY = 0;

            foreach (var location in locations)
            {
                centerX += location.X;
                centerY += location.Y;
            }

            centerX = centerX / locations.Count;
            centerY = centerY / locations.Count;

            var radius = Algorithms.Distance(centerX, centerY, locations[0].X, locations[0].Y);

            var angle = Math.Atan2(locations[1].Y - locations[0].Y, locations[1].X - locations[0].X) * 180.0 / Math.PI;

            return (new MPoint(centerX, centerY), radius, angle);
        }

        private float ViewportWidth => (float)_canvas.Frame.Width; // todo: check if we need _canvas
        private float ViewportHeight => (float)_canvas.Frame.Height; // todo: check if we need _canvas

        private float GetPixelDensity()
        {
            return (float)_canvas.ContentScaleFactor; // todo: Check if I need canvas        
        }
    }
}