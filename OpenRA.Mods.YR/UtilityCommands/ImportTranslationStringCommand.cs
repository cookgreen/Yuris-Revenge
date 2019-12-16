using OpenRA.FileSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.UtilityCommands
{
	public class ImportTranslationStringCommand : IUtilityCommand
	{
		public string Name { get { return "--import-translation-string"; } }

		public bool ValidateArguments(string[] args) { return args.Length >= 3; }

		[Desc("LOCALIZATIONNAME NEWMODID", "")]
		public void Run(Utility utility, string[] args)
		{
			Console.WriteLine("Starting importing the translated strings");
			//Get translated strings from LOCALIZATIONNAME.yaml
			string localizationName = args[1];
			var modData = utility.ModData;
			string localizationFile = string.Format("languages\\{0}.yaml", localizationName);
			var stream = modData.ModFiles.Open(localizationFile);
			var nodes = MiniYaml.FromStream(stream);
			if (nodes[0].Value.Nodes[0].Key != localizationName)
			{
				Console.WriteLine("Invalid localization file!");
				return;
			}
			var rulesLocalizationNode = nodes[0].Value.Nodes[0].Value.Nodes[0];

			//Get the original mod install dir
			string modID = modData.Manifest.Id;
			string modFolder = null;
			if (stream is FileStream)
			{
				var fs = stream as FileStream;
				if (fs.Name.Contains(localizationFile))
				{
					int idx = fs.Name.IndexOf(localizationFile);
					modFolder = fs.Name.Substring(0, idx);
				}
			}
			//Copy all the original mod files to the new mod which id is defined by NEWMODID parameter
			string newModID = args[2];
			if (!string.IsNullOrEmpty(modFolder))
			{
				DirectoryInfo di = new DirectoryInfo(modFolder);
				DirectoryInfo modRootDir = di.Parent;
				string newModFullPath = Path.Combine(modRootDir.FullName, newModID);

				if (Directory.Exists(newModFullPath))
				{
					Directory.Delete(newModFullPath, true);
				}
				Directory.CreateDirectory(newModFullPath);

				foreach (var fileSystemInfo in di.EnumerateFileSystemInfos())
				{
					if (fileSystemInfo.Attributes == FileAttributes.Directory)
					{
						DirectoryCopy(fileSystemInfo.FullName, Path.Combine(newModFullPath, fileSystemInfo.Name), true);
					}
					else
					{
						File.Copy(fileSystemInfo.FullName, Path.Combine(newModFullPath, fileSystemInfo.Name));
					}
				}
			}

			//Modify all the yaml files using the original mod id

			//Write all the translation strings into the new mod yaml files


			Console.WriteLine("Import task has already finished!");
		}
		private void DirectoryCopy(string sourceDirName, string destDirName, bool copySubDirs)
		{
			// Get the subdirectories for the specified directory.
			DirectoryInfo dir = new DirectoryInfo(sourceDirName);

			if (!dir.Exists)
			{
				throw new DirectoryNotFoundException(
					"Source directory does not exist or could not be found: "
					+ sourceDirName);
			}

			DirectoryInfo[] dirs = dir.GetDirectories();
			// If the destination directory doesn't exist, create it.
			if (!Directory.Exists(destDirName))
			{
				Directory.CreateDirectory(destDirName);
			}

			// Get the files in the directory and copy them to the new location.
			FileInfo[] files = dir.GetFiles();
			foreach (FileInfo file in files)
			{
				string temppath = Path.Combine(destDirName, file.Name);
				file.CopyTo(temppath, false);
			}

			// If copying subdirectories, copy them and their contents to new location.
			if (copySubDirs)
			{
				foreach (DirectoryInfo subdir in dirs)
				{
					string temppath = Path.Combine(destDirName, subdir.Name);
					DirectoryCopy(subdir.FullName, temppath, copySubDirs);
				}
			}
		}
	}
}
