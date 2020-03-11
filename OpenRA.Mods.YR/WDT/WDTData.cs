using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.WDT
{
	public class WDTData
	{
		public List<WDTScenario> Scenarios;
		public List<WDTBlock> Blocks;

		public WDTData()
		{
			Scenarios = new List<WDTScenario>();
			Blocks = new List<WDTBlock>();
		}
	}
}
