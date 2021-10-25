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

using Voting2021.BlockchainClient;

namespace EncodingConverter
{
	/// <summary>
	/// Interaction logic for MainWindow.xaml
	/// </summary>
	public partial class MainWindow
		: Window
	{
		public MainWindow()
		{
			InitializeComponent();
		}

		private bool _blockReentry = false;

		private void HexTextInput(object sender, TextChangedEventArgs e)
		{
			
			if (_blockReentry)
			{
				return;
			}
			_blockReentry = true;
			byte[] bytes = null;
			bool error = false;
			try
			{
				bytes = Convert.FromHexString(hexTextBox.Text);
			}
			catch (Exception)
			{
				error = true;
			}
			if (!error)
			{
				base64TextBox.Text = Convert.ToBase64String(bytes);
				base58TextBox.Text = Base58.EncodePlain(bytes);
			}
			else
			{
				base64TextBox.Text = base58TextBox.Text = "";
			}

			_blockReentry = false;
		}

		private void Base64TextInput(object sender, TextChangedEventArgs e)
		{
			if (_blockReentry)
			{
				return;
			}
			_blockReentry = true;
			byte[] bytes = null;
			bool error = false;
			try
			{
				bytes = Convert.FromBase64String(base64TextBox.Text);
			}
			catch (Exception)
			{
				error = true;
			}
			if (!error)
			{
				hexTextBox.Text = Convert.ToHexString(bytes);
				base58TextBox.Text = Base58.EncodePlain(bytes);
			}
			else
			{
				hexTextBox.Text = base58TextBox.Text = "";
			}

			_blockReentry = false;
		}

		private void Base58TextInput(object sender, TextChangedEventArgs e)
		{
			if (_blockReentry)
			{
				return;
			}
			_blockReentry = true;
			byte[] bytes = null;
			bool error = false;
			try
			{
				bytes = Base58.DecodePlain(base58TextBox.Text);
			}
			catch (Exception)
			{
				error = true;
			}
			if (!error)
			{
				hexTextBox.Text = Convert.ToHexString(bytes);
				base64TextBox.Text = Convert.ToBase64String(bytes);
			}
			else
			{
				hexTextBox.Text = base64TextBox.Text = "";
			}

			_blockReentry = false;
		}
	}
}
