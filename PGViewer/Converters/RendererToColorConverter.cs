using Esri.ArcGISRuntime.Symbology;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;

namespace PGViewer.Converters
{
    /// <summary>
    /// Converts a simple renderer to a color so that the background in the list matches the color used in the symbol.
    /// </summary>
    public class RendererToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var renderer = value as SimpleRenderer;
            var marker = renderer.Symbol as SimpleMarkerSymbol;
            if (marker != null)
            {
                return new SolidColorBrush(marker.Color);
            }
            var fill = renderer.Symbol as SimpleFillSymbol;
            if (fill != null)
            {
                return new SolidColorBrush(fill.Color);
            }

            var line = renderer.Symbol as SimpleLineSymbol;
            if (line != null)
            {
                return new SolidColorBrush(line.Color);
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
