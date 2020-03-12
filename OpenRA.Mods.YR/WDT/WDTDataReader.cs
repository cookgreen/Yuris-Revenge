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

		public WDTData Read(ModData modData)
		{
			WDTData wdtData = new WDTData();
			
			List<MiniYamlNode> miniYaml = MiniYaml.FromStream(modData.DefaultFileSystem.Open("wdt_data.yaml"));
			var rootNode = miniYaml.FirstOrDefault();

			if (rootNode != null)
			{
				var scenarioNode = rootNode.Value.Nodes[0];
				var blocksNode = rootNode.Value.Nodes[1];

				if (scenarioNode != null && blocksNode != null)
				{
					foreach (var node in scenarioNode.Value.Nodes)
					{
						WDTScenario wdtScenario = new WDTScenario();
						wdtScenario.Key = node.Key;
						wdtScenario.Name = node.Value.Nodes[0].Value.Value;
						wdtScenario.BackgroundImage = node.Value.Nodes[1].Value.Value;
						wdtData.Scenarios.Add(wdtScenario);
					}
					foreach (var scenarioKeyNode in blocksNode.Value.Nodes)
					{
						foreach (var blockNode in scenarioKeyNode.Value.Nodes)
						{
							if (!wdtData.Blocks.ContainsKey(scenarioKeyNode.Key))
							{
								wdtData.Blocks.Add(scenarioKeyNode.Key, new List<WDTBlock>());
							}
							WDTBlock block = new WDTBlock();
							block.Name = blockNode.Value.Nodes[0].Value.Value;
							block.Image = blockNode.Value.Nodes[1].Value.Value;
							wdtData.Blocks[scenarioKeyNode.Key].Add(block);
						}
					}
				}
			}

			return wdtData;
		}
	}
}
