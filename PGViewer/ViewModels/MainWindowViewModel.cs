using Esri.ArcGISRuntime.Geometry;
using Esri.ArcGISRuntime.Mapping;
using Esri.ArcGISRuntime.Symbology;
using Esri.ArcGISRuntime.UI;
using MoravecLabs.Atom;
using MoravecLabs.Infrastructure;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace PGViewer.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public MainWindowViewModel()
        {
            this.Map = new Atom<Map>(new Map(Basemap.CreateImageryWithLabelsVector()), this, nameof(this.Map));
            this.ConnectionString = new Atom<string>(string.Empty, this, nameof(this.ConnectionString));
            this.SQLString = new Atom<string>(string.Empty, this, nameof(this.SQLString));
            this.RunSqlCommand = new DelegateCommand(() => { this.ExecuteSQL(); }, () => true);
            this.ErrorMessage = new Atom<string>(string.Empty, this, nameof(this.ErrorMessage));
            this.DeleteLayer = new DelegateCommand<GraphicsOverlay>((o) => this.GraphicsOverlays.Remove(o), (o) => true);
            this.GraphicsOverlays = new ObservableCollection<GraphicsOverlay>();
            this.CurrentMapViewPoint = new Atom<Viewpoint>(null, this, nameof(this.CurrentMapViewPoint));
        }
        /// <summary>
        /// Gets the Map object.
        /// </summary>
        public Atom<Map> Map { get; private set; }
        /// <summary>
        /// Gets the Connection String set in the GUI.
        /// </summary>
        public Atom<string> ConnectionString { get; private set; }
        /// <summary>
        /// Gets the SQL string set in the GUI.
        /// </summary>
        public Atom<string> SQLString { get; private set; }
        /// <summary>
        /// Sets the error message at the bottom of the GUI.
        /// </summary>
        public Atom<string> ErrorMessage { get; private set; }

        /// <summary>
        /// Sets the current map view, causes the map to zoom/pan when changed.
        /// </summary>
        public Atom<Viewpoint> CurrentMapViewPoint { get; private set; }

        /// <summary>
        /// Gets or Sets the observable collection of graphics overlays, these are "bound" via code behind to the mapview.
        /// </summary>
        public ObservableCollection<GraphicsOverlay> GraphicsOverlays { get; set; }

        /// <summary>
        /// Gets or Sets the command to run sql.
        /// </summary>
        public ICommand RunSqlCommand { get; set; }

        /// <summary>
        /// Gets or sets the command to remove a specified layer from the amp.
        /// </summary>
        public ICommand DeleteLayer { get; set; }

        /// <summary>
        /// Private method to compute the extent given two geometries.  
        /// Used to build the full extent of the layer for zooming the map.
        /// </summary>
        /// <param name="g1"></param>
        /// <param name="g2"></param>
        /// <returns></returns>
        private static Geometry BuildExtent(Geometry g1, Geometry g2)
        {
            if (g1 == null)
            {
                return g2.Extent;
            }
            else
            {
                return GeometryEngine.CombineExtents(g1, g2);
            }
        }

        /// <summary>
        /// Converts the input PostGIS object to an Esri Graphic.
        /// </summary>
        /// <param name="inputGeometry"></param>
        /// <param name="color"></param>
        /// <param name="renderer"></param>
        /// <returns></returns>
        private static Graphic ConvertToEsriGraphic(object inputGeometry, System.Windows.Media.Color color, out Renderer renderer)
        {
            switch (inputGeometry.GetType().ToString())
            {
                case "NpgsqlTypes.PostgisPoint":
                    var t = inputGeometry as NpgsqlTypes.PostgisPoint;
                    var geom = new MapPointBuilder(t.X, t.Y, new SpatialReference(Convert.ToInt32(t.SRID))).ToGeometry();
                    var pointSymbol = new SimpleMarkerSymbol(SimpleMarkerSymbolStyle.Circle, color, 10);
                    var graphic = new Graphic(geom, pointSymbol);
                    renderer = new SimpleRenderer(pointSymbol);
                    return graphic;
                case "NpgsqlTypes.PostgisPolygon":
                    var p = inputGeometry as NpgsqlTypes.PostgisPolygon;
                    var polygon = new PolygonBuilder(new SpatialReference(Convert.ToInt32(p.SRID)));

                    foreach (var ring in p)
                    {
                        var part = ring.Select(i => new MapPointBuilder(i.X, i.Y).ToGeometry());
                        polygon.AddPart(part);
                    }
                    var polygonSymbol = new SimpleFillSymbol(SimpleFillSymbolStyle.Solid, color, new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, System.Windows.Media.Colors.Black, 3));
                    renderer = new SimpleRenderer(polygonSymbol);
                    return new Graphic(polygon.ToGeometry(), polygonSymbol);
                case "NpgsqlTypes.PostgisLineString":
                    var l = inputGeometry as NpgsqlTypes.PostgisLineString;
                    var polyline = new PolylineBuilder(new SpatialReference(Convert.ToInt32(l.SRID)));
                    polyline.AddPoints(l.Select(i => new MapPointBuilder(i.X, i.Y).ToGeometry()));
                    var polylineSymbol = new SimpleLineSymbol(SimpleLineSymbolStyle.Solid, color, 3);
                    renderer = new SimpleRenderer(polylineSymbol);
                    return new Graphic(polyline.ToGeometry(), polylineSymbol);

                default:
                    throw new Exception($"Unsupported Geometry type: {inputGeometry.GetType().ToString()}");
            }
        }

        /// <summary>
        ///  Executes the SQL using the specified connection string.
        /// </summary>
        private async void ExecuteSQL()
        {
            // Display message at the bottom to let the user know the app is running still.
            this.ErrorMessage.Value = "Working...";
            Geometry extent = null;
            var graphicsOverlay = new GraphicsOverlay()
            {
                Id = this.SQLString.Value
            };

            // Collect data in a background thread to keep the GUI responsive.
            await Task.Run(() =>
            {           
                // Build and test the connection string.
                var conn = new NpgsqlConnection(this.ConnectionString.Value);

                try
                {
                    conn.Open();
                }
                catch(Exception e)
                {
                    this.ErrorMessage.Value = e.Message;
                    return;
                }

                // Build and test the SQL and run it.
                var command = new NpgsqlCommand(this.SQLString.Value, conn);
                NpgsqlDataReader reader;

                try
                {
                    reader = command.ExecuteReader();
                }
                catch(Exception e)
                {
                    this.ErrorMessage.Value = e.Message;
                    return;
                }
                
                // Generate a random color to use in the symbol for this layer.
                var r = new Random();
                var color = System.Windows.Media.Color.FromRgb((byte)r.Next(1, 255), (byte)r.Next(1, 255), (byte)r.Next(1, 233));

                while (reader.Read())
                {
                    // Find the geometry field.
                    var geometryFieldId = -1;
                    if (geometryFieldId == -1)
                    {
                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var fieldType = reader.GetFieldType(i);
                            if (fieldType == typeof(NpgsqlTypes.PostgisGeometry))
                            {
                                geometryFieldId = i;
                                break;
                            }
                        }
                    }
                    if (geometryFieldId == -1)
                    {
                        this.ErrorMessage.Value = "No supported geometry column";
                        return;
                    }
                    var data = reader.GetValue(geometryFieldId);
                    try
                    {
                        var graphic = ConvertToEsriGraphic(data, color, out var renderer);
                        graphicsOverlay.Graphics.Add(graphic);
                        graphicsOverlay.Renderer = renderer;
                        extent = BuildExtent(extent, graphic.Geometry);

                    }
                    catch(Exception e)
                    {
                        this.ErrorMessage.Value = e.Message;
                        return;
                    }

                }
                conn.Close();
                this.ErrorMessage.Value = string.Empty;
            });

            // If the error message is not empty at this point, then something bad happened so we can't add the 
            // overlay and we can't zoom.
            if (this.ErrorMessage.Value != string.Empty)
            {
                return;
            }
            this.CurrentMapViewPoint.Value = new Viewpoint(extent);
            this.GraphicsOverlays.Add(graphicsOverlay);

        }
    }
}
