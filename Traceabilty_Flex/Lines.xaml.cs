using System;
using System.Collections.Generic;
using System.Data;
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

namespace Traceabilty_Flex
{
    /// <summary>
    /// Interaction logic for Lines.xaml
    /// </summary>
    public partial class Lines : Window
    {
        DataTable _dtLines;

        public Lines(DataTable d)
        {
            InitializeComponent();
            _dtLines = d;
            _dtLines.Columns.Add("Check", typeof(bool));
            _dtLines.Columns[2].DefaultValue = true;
            dataGridLine.ItemsSource = _dtLines.AsDataView();
        }
    }
}
