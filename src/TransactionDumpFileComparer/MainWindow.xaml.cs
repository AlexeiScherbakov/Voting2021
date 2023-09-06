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

using Microsoft.Win32;

namespace TransactionDumpFileComparer
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
		: Window
	{
		private ShardInformationContext _dump1;
		private ShardInformationContext _dump2;

		public MainWindow()
		{
			InitializeComponent();
		}

		private string OpenFile(string title)
		{
			var o = new OpenFileDialog()
			{
				Title = title
			};
			if (o.ShowDialog(this) != true)
			{
				return null;
			}
			return o.FileName;
		}

		private async void LoadDump1Click(object sender, RoutedEventArgs e)
		{
			var file = OpenFile("Первый дамп");
			if (file is null)
			{
				return;
			}
			busy.IsBusy = true;
			_dump1 = await LoadShardFileAsync(file);
			vis1.LoadShardInformationContext(_dump1);
			busy.IsBusy = false;
		}

		private async void LoadDump2Click(object sender, RoutedEventArgs e)
		{
			var file = OpenFile("Второй дамп");
			if (file is null)
			{
				return;
			}
			busy.IsBusy = true;
			_dump2 =await LoadShardFileAsync(file);
			vis2.LoadShardInformationContext(_dump2);
			busy.IsBusy = false;
		}

		private Task<ShardInformationContext> LoadShardFileAsync(string fileName)
		{
			return Task.Run(() =>
			{
				return ShardInformationContext.LoadFromFile(fileName);
			});
		}

		private void MenuItem_Click(object sender, RoutedEventArgs e)
		{
			if (_dump1 is null || _dump2 is null)
			{
				return;
			}

			var result = ShardInformationContext.Compare(_dump1, _dump2);
			compareVis.LoadCompareResult(result);
		}
	}
}
