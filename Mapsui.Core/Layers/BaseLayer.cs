using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Mapsui.Fetcher;
using Mapsui.Styles;
using Mapsui.Widgets;

namespace Mapsui.Layers
{
    public abstract class BaseLayer : ILayer
    {
        private static int _instanceCounter;
        private bool _busy;
        private bool _enabled;
        private string _name;
        private double _maxVisible;
        private double _minVisible;
        private double _opacity;
        private IStyle? _style;
        private object _tag;
        private MRect _envelope;

        /// <summary>
        /// Get a layer's styles
        /// </summary>
        /// <param name="layer">Layer, for which styles should be returned</param>
        /// <returns>Enumerable with styles belonging to layer</returns>
        public static IEnumerable<IStyle?> GetLayerStyles(ILayer? layer)
        {
            if (layer == null) return Array.Empty<IStyle>();
            var style = layer.Style as StyleCollection;
            return style?.ToArray() ?? new[] { layer.Style };
        }

        /// <summary>
        /// Creates a BaseLayer without a name
        /// </summary>
        protected BaseLayer()
        {
            Name = "Layer";
            Style = new VectorStyle();
            Enabled = true;
            MinVisible = 0;
            MaxVisible = double.MaxValue;
            Opacity = 1;
            Id = _instanceCounter++;
        }

        /// <summary>
        /// Creates a BaseLayer with a name
        /// </summary>
        /// <param name="name">Name for this layer</param>
        protected BaseLayer(string name)
            : this()
        {
            Name = name;
        }

        /// <summary>
        /// Called whenever a property changed
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// DataChanged should be triggered by any data changes
        /// </summary>
        public event DataChangedEventHandler? DataChanged;

        /// <inheritdoc />
        public int Id { get; }

        /// <inheritdoc />
        public object Tag
        {
            get => _tag;
            set
            {
                _tag = value;
                OnPropertyChanged(nameof(Tag));
            }
        }

        /// <inheritdoc />
        public double MinVisible
        {
            get => _minVisible;
            set
            {
                _minVisible = value;
                OnPropertyChanged(nameof(MinVisible));
            }
        }

        /// <inheritdoc />
        public double MaxVisible
        {
            get => _maxVisible;
            set
            {
                _maxVisible = value;
                OnPropertyChanged(nameof(MaxVisible));
            }
        }

        /// <inheritdoc />
        public bool Enabled
        {
            get => _enabled;
            set
            {
                if (_enabled == value) return;
                _enabled = value;
                OnPropertyChanged(nameof(Enabled));
            }
        }

        /// <inheritdoc />
        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        /// <inheritdoc />
        public double Opacity
        {
            get => _opacity;
            set
            {
                _opacity = value;
                OnPropertyChanged(nameof(Opacity));
            }
        }

        /// <inheritdoc />
        public bool Busy
        {
            get => _busy;
            set
            {
                if (_busy == value) return;
                _busy = value;
                OnPropertyChanged(nameof(Busy));
            }
        }

        /// <inheritdoc />
        public IStyle? Style
        {
            get => _style;
            set
            {
                _style = value;
                OnPropertyChanged(nameof(Style));
            }
        }

        /// <summary>
        /// Returns the envelope of all available data in the layer
        /// </summary>
        public virtual MRect Envelope
        {
            get => _envelope;
            protected set
            {
                _envelope = value;
                OnPropertyChanged(nameof(Envelope));
            }
        }

        /// <inheritdoc />
        public Hyperlink Attribution { get; set; }

        /// <inheritdoc />
        public virtual IReadOnlyList<double> Resolutions { get; } = new List<double>();

        /// <inheritdoc />
        public bool IsMapInfoLayer { get; set; }

        /// <inheritdoc />
        public abstract IEnumerable<IFeature> GetFeatures(MRect box, double resolution);

        /// <inheritdoc />
        public abstract void RefreshData(FetchInfo fetchInfo);

        public void DataHasChanged()
        {
            DataChanged?.Invoke(this, new DataChangedEventArgs());
        }

        public override string ToString()
        {
            return Name;
        }

        protected virtual void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        protected void OnDataChanged(DataChangedEventArgs args)
        {
            DataChanged?.Invoke(this, args);
        }
    }
}
