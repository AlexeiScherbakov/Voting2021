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

namespace TransactionDumpFileComparer
{
	/// <summary>
	/// Interaction logic for ShardCompareVisualizer.xaml
	/// </summary>
	public partial class ShardCompareVisualizer
		: UserControl
	{
		public ShardCompareVisualizer()
		{
			InitializeComponent();
		}

		public void LoadCompareResult(ShardInformationCompareResult result)
		{
			unique1.Text = "Уникальные в 1: "+result.Unique1;
			unique2.Text = "Уникальные в 2: " + result.Unique2;
			same.Text = "Одинаковые: " + result.Same;
			different.Text = "Разные: " + result.NotSame;
		}
	}
}
