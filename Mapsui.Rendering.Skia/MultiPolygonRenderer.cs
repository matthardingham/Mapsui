﻿using Mapsui.Geometries;
using Mapsui.Layers;
using Mapsui.Styles;
using SkiaSharp;

namespace Mapsui.Rendering.Skia
{
    public static class MultiPolygonRenderer
    {
        public static void Draw(SKCanvas canvas, IReadOnlyViewport viewport, IStyle style, IFeature feature,
            MultiPolygon multiPolygon, float opacity, SymbolCache? symbolCache = null)
        {
            foreach (Polygon polygon in multiPolygon)
            {
                PolygonRenderer.Draw(canvas, viewport, style, feature, polygon, opacity, symbolCache);
            }
        }
    }
}