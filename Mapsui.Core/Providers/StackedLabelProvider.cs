﻿using System.Collections.Generic;
using System.Linq;
using Mapsui.Layers;
using Mapsui.Styles;
using Mapsui.Styles.Thematics;

namespace Mapsui.Providers
{
    public class StackedLabelProvider : IProvider<IFeature>
    {
        private const int SymbolSize = 32; // todo: determine margin by symbol size
        private const int BoxMargin = SymbolSize / 2;

        private readonly IProvider<IFeature> _provider;
        private readonly LabelStyle _labelStyle;

        public StackedLabelProvider(IProvider<IFeature> provider, LabelStyle labelStyle, Pen? rectangleLine = null,
            Brush? rectangleFill = null)
        {
            _provider = provider;
            _labelStyle = labelStyle;
            _rectangleLine = rectangleLine ?? new Pen(Color.Gray);
            _rectangleFill = rectangleFill;
        }

        public string CRS { get; set; }

        private readonly Brush _rectangleFill;

        private readonly Pen _rectangleLine;

        public IEnumerable<IFeature> GetFeatures(FetchInfo fetchInfo)
        {
            var features = _provider.GetFeatures(fetchInfo);
            return GetFeaturesInView(fetchInfo.Resolution, _labelStyle, features, _rectangleLine, _rectangleFill);
        }

        public MRect GetExtent()
        {
            return _provider.GetExtent();
        }

        private static List<IFeature> GetFeaturesInView(double resolution, LabelStyle labelStyle,
            IEnumerable<IFeature> features, Pen line, Brush fill)
        {
            var margin = resolution * 50;
            var clusters = new List<Cluster>();
            // todo: repeat until there are no more merges
            ClusterFeatures(clusters, features, margin, labelStyle, resolution);

            const int textHeight = 18;

            var results = new List<IFeature>();

            foreach (var cluster in clusters)
            {
                if (cluster.Features.Count > 1) results.Add(CreateBoxFeature(resolution, cluster, line, fill));

                var offsetY = double.NaN;

                var orderedFeatures = cluster.Features.OrderBy(f => f.Extent.Centroid.Y);

                foreach (var pointFeature in orderedFeatures)
                {
                    var position = CalculatePosition(cluster);

                    offsetY = CalculateOffsetY(offsetY, textHeight);

                    var labelText = labelStyle.GetLabelText(pointFeature);
                    var labelFeature = CreateLabelFeature(position, labelStyle, offsetY, labelText);

                    results.Add(labelFeature);
                }
            }
            return results;
        }

        private static double CalculateOffsetY(double offsetY, int textHeight)
        {
            if (double.IsNaN(offsetY)) // first time
                offsetY = textHeight * 0.5 + BoxMargin;
            else
                offsetY += textHeight; // todo: get size from text (or just pass stack nr)
            return offsetY;
        }

        private static MPoint CalculatePosition(Cluster cluster)
        {
            // Since the box can be rotated, find the minimal Y value of all 4 corners
            var rotatedBox = cluster.Box.Rotate(0); // todo: Add rotation '-viewport.Rotation'
            var minY = rotatedBox.Vertices.Select(v => v.Y).Min();
            var position = new MPoint(cluster.Box.Centroid.X, minY);
            return position;
        }

        private static IFeature CreateLabelFeature(MPoint position, LabelStyle labelStyle, double offsetY,
            string text)
        {
            return new PointFeature
            {
                Point = position,
                Styles = new[]
                {
                    new LabelStyle(labelStyle)
                    {
                        Offset = {Y = offsetY},
                        LabelMethod = _ => text
                    }
                }
            };
        }

        private static IFeature CreateBoxFeature(double resolution, Cluster cluster, Pen line,
            Brush fill)
        {
            return new RectFeature
            {
                Rect = GrowBox(cluster.Box, resolution),
                Styles = new[]
                {
                    new VectorStyle
                    {
                        Outline = line,
                        Fill = fill
                    }
                }
            };
        }

        private static MRect? GrowBox(MRect box, double resolution)
        {
            const int symbolSize = 32; // todo: determine margin by symbol size
            const int boxMargin = symbolSize / 2;
            return box.Grow(boxMargin * resolution);
        }

        private static void ClusterFeatures(
            ICollection<Cluster> clusters,
            IEnumerable<IFeature> features,
            double minDistance,
            IStyle layerStyle,
            double resolution)
        {
            var style = layerStyle;

            // todo: This method should repeated several times until there are no more merges
            foreach (var feature in features.OrderBy(f => f.Extent.Centroid.Y))
            {
                if (layerStyle is IThemeStyle themeStyle)
                    style = themeStyle.GetStyle(feature);

                if ((style == null) ||
                    (style.Enabled == false) ||
                    (style.MinVisible > resolution) ||
                    (style.MaxVisible < resolution)) continue;

                var found = false;
                foreach (var cluster in clusters)
                    if (cluster.Box.Grow(minDistance).Contains(feature.Extent.Centroid))
                    {
                        cluster.Features.Add(feature);
                        cluster.Box = cluster.Box.Join(feature.Extent);
                        found = true;
                        break;
                    }

                if (found) continue;

                clusters.Add(new Cluster
                {
                    Box = feature.Extent.Clone(),
                    Features = new List<IFeature> { feature }
                });
            }
        }

        private class Cluster
        {
            public MRect? Box { get; set; }
            public IList<IFeature>? Features { get; set; }
        }
    }
}