using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
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
using System.Windows.Shapes;
using static LayerComparer.OrderByFieldsDialog;

namespace LayerComparer
{
    /// <summary>
    /// Interaction logic for OrderByFieldsDialog.xaml
    /// </summary>
    public partial class OrderByFieldsDialog : Window
    {
        public IEnumerable<ComparisonPair> LayerComparisonPairs { get; private set; } = [];

        public OrderByFieldsDialog(IEnumerable<LayerInfo> layersLeft, IEnumerable<LayerInfo> layersRight)
        {
            LayerComparisonPairs = layersLeft.Zip(layersRight)
                .Select(pair => new ComparisonPair
                {
                    LayerOne = pair.First.Name,
                    LayerTwo = pair.Second.Name,
                    SharedFields = [.. pair.First.Fields.Select(l => l.Name)
                                                     .Intersect(pair.Second.Fields.Select(l => l.Name))
                                                     .Select((name, index) => new OrderByFieldOptions { Name = name, Selected = index == 0})]
                });

            InitializeComponent();
        }

        public class ComparisonPair
        {
            public required string LayerOne { get; set; }
            public required string LayerTwo { get; set; }
            public required ObservableCollection<OrderByFieldOptions> SharedFields { get; set; }
            public bool IsValidPair => SharedFields.Count > 0;
        }

        public class OrderByFieldOptions
        {
            public required string Name { get; set; }
            public bool Selected { get; set; }
        }
    }

    [ValueConversion(typeof(IEnumerable<OrderByFieldOptions>), typeof(string))]
    public class TestConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is IEnumerable<OrderByFieldOptions> orderByFieldOptions)
            {
                return string.Join(", ", orderByFieldOptions.Where(f => f.Selected).Select(f => f.Name));
            }
            throw new NotImplementedException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
