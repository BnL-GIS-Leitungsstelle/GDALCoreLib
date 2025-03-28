using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace LayerComparer
{
    /// <summary>
    /// Interaction logic for LayerTable.xaml
    /// </summary>
    public partial class LayerTable : UserControl
    {
        public DataGrid? LinkedWith
        {
            get { return (DataGrid)GetValue(LinkedWithProperty); }
            set { SetValue(LinkedWithProperty, value); }
        }

        // Using a DependencyProperty as the backing store for LinkedWith.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty LinkedWithProperty =
            DependencyProperty.Register("LinkedWith", typeof(DataGrid), typeof(LayerTable), new PropertyMetadata(null));

        public DataGrid DataGridSource => dataGrid;

        public LayerTable()
        {
            InitializeComponent();
        }

        private void Expander_Toggled(object sender, RoutedEventArgs e)
        {
            for (var vis = sender as Visual; vis != null; vis = VisualTreeHelper.GetParent(vis) as Visual)
            {
                if (vis is DataGridRow row)
                {
                    row.DetailsVisibility = row.DetailsVisibility == Visibility.Visible ? Visibility.Collapsed : Visibility.Visible;
                    break;
                }
            }
        }

        private void DataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selectedIndices = dataGrid.SelectedItems.Cast<LayerInfo>().Select(dataGrid.Items.IndexOf);

            if (LinkedWith != null)
            {
                LinkedWith.SelectedItems.Clear();
                foreach (var index in selectedIndices)
                {
                    if (LinkedWith.Items.Count <= index) continue;
                    LinkedWith.SelectedItems.Add(LinkedWith.Items[index]);
                }
            }
        }
    }
}
