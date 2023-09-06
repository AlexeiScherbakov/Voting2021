using System;
using System.Collections.Generic;
using System.ComponentModel;
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
	/// Interaction logic for ShardDumpVisualizer.xaml
	/// </summary>
	public partial class ShardDumpVisualizer
		: UserControl
	{
		public ShardDumpVisualizer()
		{
			InitializeComponent();
		}


		public void LoadShardInformationContext(ShardInformationContext context)
		{
			transactionCount.Text = "Всего транзакций: " + context.TransactionCount;

			var stats = new List<StatViewModel>();
			foreach(var pair in context.TransactionTypes)
			{
				stats.Add(new StatViewModel()
				{
					Name = pair.Key,
					Count = pair.Value
				});
			}
			grid.ItemsSource = stats;
		}


		private sealed class StatViewModel
			:INotifyPropertyChanged
		{
			public string Name { get; set; }
			public int Count { get; set; }

			public event PropertyChangedEventHandler? PropertyChanged;
		}
	}
}
