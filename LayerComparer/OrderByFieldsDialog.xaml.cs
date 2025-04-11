using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows;

namespace LayerComparer
{
    [INotifyPropertyChanged]
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
                    SharedFields =
                    [
                        .. pair.First.Fields.Select(l => l.Name)
                            .Intersect(pair.Second.Fields.Select(l => l.Name))
                            // make sure that the ESRI fields appear last in the list
                            .OrderBy(name => name is "Shape_Area" or "Shape_Length")
                            // select the first common field in the list for convenience (don't know if that makes sense...)
                            .Select((name, index) => new OrderByFieldOptions { Name = name, Selected = index == 0 })
                    ]
                }.StartListeners());
        
            InitializeComponent();
        }

        public partial class ComparisonPair : ObservableObject
        {
            public required string LayerOne { get; set; }
            public required string LayerTwo { get; set; }
            public required ObservableCollection<OrderByFieldOptions> SharedFields { get; set; }
            public bool IsValidPair => SharedFields.Count > 0;

            [ObservableProperty] private string fieldListString = "";

            internal ComparisonPair StartListeners()
            {
                SharedFields.CollectionChanged += (_, _) => UpdateOrderByFieldsString();
                foreach (var field in SharedFields)
                {
                    field.PropertyChanged += (_, _) => UpdateOrderByFieldsString();
                }

                UpdateOrderByFieldsString();
                return this;
            }

            private void UpdateOrderByFieldsString()
            {
                FieldListString = string.Join(", ", SharedFields.Where(f => f.Selected).Select(f => f.Name));
            }
        }

        public partial class OrderByFieldOptions : ObservableObject
        {
            [ObservableProperty] private string name = "";

            [ObservableProperty] private bool selected;
        }

        private void Button_Run_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }
    }
}