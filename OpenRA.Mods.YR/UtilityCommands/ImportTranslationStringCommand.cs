using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.UtilityCommands
{
	public class ImportTranslationStringCommand : IUtilityCommand
	{
		public string Name { get { return "--import-translation-string"; } }

		public bool ValidateArguments(string[] args) { return args.Length >= 2; }

		[Desc("LOCALIZATIONNAME NEWMODID", "")]
		public void Run(Utility utility, string[] args)
		{
			Console.WriteLine("Starting importing the translated strings");
			//Get translated strings from LOCALIZATIONNAME.yaml

			//Get the original mod install dir

			//Copy all the original mod files to the new mod which id is defined by NEWMODID parameter

			//Write all the translation strings into the new mod yaml files
			Console.WriteLine("Import task has already finished!");
		}
	}
}
