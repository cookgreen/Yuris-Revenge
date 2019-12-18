using OpenRA.Mods.YR.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.UtilityCommands
{
	public class ExportTranslationStringCommand : IUtilityCommand
	{
		public string Name { get { return "--export-translation-string"; } }
		bool IUtilityCommand.ValidateArguments(string[] args) { return args.Length >= 2; }

		[Desc("LOCALIZATIONNAME", "Export strings in rules, sequences and chrome into a yaml file with the localization name")]
		public void Run(Utility utility, string[] args)
		{
			var modData = utility.ModData;

			var localizationName = args[1];

			var localizationFile = localizationName + ".yaml";
			if(File.Exists(localizationFile))
			{
				File.Delete(localizationFile);
			}
			using (StreamWriter writer = new StreamWriter(File.Create(localizationName + ".yaml")))
			{
				List<MiniYamlNode> subNodes = new List<MiniYamlNode>();
				List<MiniYamlNode> ruleNodes = new List<MiniYamlNode>();
				foreach (var f in modData.Manifest.Rules)
				{
					var actors = MiniYaml.FromStream(modData.DefaultFileSystem.Open(f), f);

					// TODO: maybe can export actorInfos
					foreach (var actor in actors)
					{
						foreach (var trait in actor.Value.Nodes)
						{
							// TODO: export the string which has translation attribute
							if (trait.Key == "Tooltip")
							{
								ruleNodes.Add(new MiniYamlNode(actor.Key, new MiniYaml(trait.Value.Nodes[0].Value.Value)));
							}
						}
					}
				}
				subNodes.Add(new MiniYamlNode("Rules", new MiniYaml(null, ruleNodes)));

				List<MiniYamlNode> nodes = new List<MiniYamlNode>();
				MiniYamlNode node = new MiniYamlNode(localizationName, new MiniYaml(localizationName.SetFirstLetterUpper(), subNodes));
				nodes.Add(node);
				
				MiniYaml tranlsation = new MiniYaml(null, nodes);
				foreach (var line in tranlsation.ToLines(localizationName))
				{
					writer.WriteLine(line);
				}
			}
		}
	}
}
