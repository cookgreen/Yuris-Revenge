using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.WDT
{
	public class WDTDataReader
	{
		string wdtDataFile;
		public WDTDataReader(string wdtDataFile)
		{
			this.wdtDataFile = wdtDataFile;
		}

		public WDTData Reader()
		{
			WDTData wdtData = new WDTData();
			return wdtData;
		}
	}
}
