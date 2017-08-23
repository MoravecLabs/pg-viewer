using Esri.ArcGISRuntime.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace PGViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Build the the view model.
            var vm = new ViewModels.MainWindowViewModel();
            this.DataContext = vm;

            // when the graphics overlay lsit changes, update the layers in the map
            vm.GraphicsOverlays.CollectionChanged += GraphicsOverlays_CollectionChanged;

            // When the map view point changes, zoom the map.
            vm.CurrentMapViewPoint.SubscribePropertyChanged((ov, nv) =>
            {
                if (nv != null)
                {
                    this.mapView.SetViewpointAsync(nv);
                }
            });
        }

        /// <summary>
        /// Handle changing the graphics overlay items that are in the map when they are added/removed from the
        /// observable collection.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GraphicsOverlays_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (e.NewItems != null)
                {
                    foreach (GraphicsOverlay i in e.NewItems)
                    {
                        this.mapView.GraphicsOverlays.Add(i);
                    }
                }
                if (e.OldItems != null)
                {
                    foreach (GraphicsOverlay i in e.OldItems)
                    {
                        this.mapView.GraphicsOverlays.Remove(i);
                    }
                }
            });
        }
    }
}
