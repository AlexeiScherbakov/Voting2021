using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Voting2021.Database.Data
{
	public class Block
	{
		public virtual long Id { get; set; }

		public virtual long Height { get; set; }

		public virtual byte[] Signature { get; set; }

		public virtual DateTime Timestamp { get; set; }

		public virtual DateTime DeletedAt { get; set; }


		public virtual long StartOffset { get; set; }
		public virtual long EndOffset { get; set; }
	}
}
