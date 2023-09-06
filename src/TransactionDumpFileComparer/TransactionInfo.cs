using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OxyPlot;

namespace TransactionDumpFileComparer
{
	internal class TransactionInfo
	{
		public string Key { get; set; }
		public byte[] Transaction { get; set; }
	}
}
