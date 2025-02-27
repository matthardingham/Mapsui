﻿using System.Collections.Generic;
using Mapsui.Geometries;
using Mapsui.GeometryLayer;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using Mapsui.UI;
using Mapsui.Utilities;

namespace Mapsui.Samples.Common.Maps
{
    public class LabelsSample : ISample
    {
        public string Name => "Labels";
        public string Category => "Symbols";

        public void Setup(IMapControl mapControl)
        {
            mapControl.Map = CreateMap();
        }

        public static Map CreateMap()
        {
            var map = new Map();
            map.Layers.Add(OpenStreetMap.CreateTileLayer());
            map.Layers.Add(CreateLayer());
            return map;
        }

        public static ILayer CreateLayer()
        {
            var features = new List<IGeometryFeature>
            {
                CreateFeatureWithDefaultStyle(),
                CreateFeatureWithRightAlignedStyle(),
                CreateFeatureWithBottomAlignedStyle(),
                CreateFeatureWithColors(),
                CreatePolygonWithLabel(),
                CreateFeatureWithHalo(),
                CreateFeatureWithTailTruncation(),
                CreateFeatureWithMiddleTruncation(),
                CreateFeatureWithHeadTruncation(),
                CreateFeatureWithWordWrapLeft(),
                CreateFeatureWithWordWrapCenter(),
                CreateFeatureWithWordWrapRight(),
                CreateFeatureWithCharacterWrap(),
            };

            var memoryProvider = new GeometryMemoryProvider<IGeometryFeature>(features);

            return new MemoryLayer { Name = "Points with labels", DataSource = memoryProvider };
        }

        private static GeometryFeature CreateFeatureWithDefaultStyle()
        {
            var featureWithDefaultStyle = new GeometryFeature { Geometry = new Point(0, 0) };
            featureWithDefaultStyle.Styles.Add(new LabelStyle { Text = "Default Label" });
            return featureWithDefaultStyle;
        }

        private static GeometryFeature CreateFeatureWithColors()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(0, -7000000) };
            featureWithColors.Styles.Add(CreateColoredLabelStyle());
            return featureWithColors;
        }

        private static GeometryFeature CreateFeatureWithBottomAlignedStyle()
        {
            var featureWithBottomAlignedStyle = new GeometryFeature { Geometry = new Point(0, -5000000) };
            featureWithBottomAlignedStyle.Styles.Add(new LabelStyle
            {
                Text = "Bottom\nAligned",
                BackColor = new Brush(Color.Gray),
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom
            });
            return featureWithBottomAlignedStyle;
        }

        private static GeometryFeature CreateFeatureWithRightAlignedStyle()
        {
            var featureWithRightAlignedStyle = new GeometryFeature { Geometry = new Point(0, -2000000) };
            featureWithRightAlignedStyle.Styles.Add(new LabelStyle
            {
                Text = "Right Aligned",
                BackColor = new Brush(Color.Gray),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Right
            });
            return featureWithRightAlignedStyle;
        }

        private static GeometryFeature CreatePolygonWithLabel()
        {
            var polygon = new GeometryFeature
            {
                Geometry = Geometry.GeomFromText(
                    "POLYGON((-1000000 -10000000, 1000000 -10000000, 1000000 -8000000, -1000000 -8000000, -1000000 -10000000))")
            };
            polygon.Styles.Add(new LabelStyle
            {
                Text = "Polygon",
                BackColor = new Brush(Color.Gray),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center
            });
            return polygon;
        }

        private static IStyle CreateColoredLabelStyle()
        {
            return new LabelStyle
            {
                Text = "Colors",
                BackColor = new Brush(Color.Blue),
                ForeColor = Color.White
            };
        }

        private static IGeometryFeature CreateFeatureWithTailTruncation()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(8000000, 2000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                Font = new Font { FontFamily = "Courier New", Bold = true, Italic = true, },
                BackColor = new Brush(Color.Transparent),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                MaxWidth = 10,
                WordWrap = LabelStyle.LineBreakMode.TailTruncation
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithHeadTruncation()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(-8000000, 2000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                Font = new Font { Size = 16, Bold = true, Italic = false, },
                BackColor = new Brush(Color.Transparent),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Right,
                MaxWidth = 10,
                WordWrap = LabelStyle.LineBreakMode.HeadTruncation
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithMiddleTruncation()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(0, 2000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                Font = new Font { Size = 30 },
                BackColor = new Brush(Color.Transparent),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2),
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                MaxWidth = 10,
                WordWrap = LabelStyle.LineBreakMode.MiddleTruncation
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithWordWrapLeft()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(-8000000, 6000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                BackColor = new Brush(Color.Gray),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2),
                MaxWidth = 10,
                WordWrap = LabelStyle.LineBreakMode.WordWrap,
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Left,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Top,
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithWordWrapCenter()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(0, 6000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                BackColor = new Brush(Color.Transparent),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2),
                MaxWidth = 10,
                LineHeight = 1.2,
                WordWrap = LabelStyle.LineBreakMode.WordWrap,
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithWordWrapRight()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(8000000, 6000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                BackColor = new Brush(Color.Gray),
                ForeColor = Color.White,
                MaxWidth = 12,
                WordWrap = LabelStyle.LineBreakMode.WordWrap,
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Right,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Bottom,
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithCharacterWrap()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(0, 10000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Long line break mode test",
                BackColor = null,
                ForeColor = Color.Black,
                MaxWidth = 6,
                WordWrap = LabelStyle.LineBreakMode.CharacterWrap,
                HorizontalAlignment = LabelStyle.HorizontalAlignmentEnum.Center,
                VerticalAlignment = LabelStyle.VerticalAlignmentEnum.Center,
            });
            return featureWithColors;
        }

        private static IGeometryFeature CreateFeatureWithHalo()
        {
            var featureWithColors = new GeometryFeature { Geometry = new Point(0, -12000000) };
            featureWithColors.Styles.Add(new LabelStyle
            {
                Text = "Halo Halo Halo",
                BackColor = new Brush(Color.Transparent),
                ForeColor = Color.White,
                Halo = new Pen(Color.Black, 2)
            });
            return featureWithColors;
        }
    }
}