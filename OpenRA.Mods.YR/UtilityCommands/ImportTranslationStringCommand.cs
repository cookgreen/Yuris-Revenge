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
			var rulesLocalizationNode = nodes[0].Value.Nodes[0].Value.Nodes.Where(o => o.Key == "Rules").FirstOrDefault();
			var chromeLayoutsLocalizationNode = nodes[0].Value.Nodes[0].Value.Nodes.Where(o => o.Key == "ChromeLayouts").FirstOrDefault();
			var worldRulesLocalizationNode = nodes[0].Value.Nodes[0].Value.Nodes.Where(o => o.Key == "World").FirstOrDefault();
			var modContentLocalizationNode = nodes[0].Value.Nodes[0].Value.Nodes.Where(o => o.Key == "ModContent").FirstOrDefault();

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

				//------------------------------------------------------
				// Modify all the yaml files using the original mod id
				//------------------------------------------------------

				List<string> mapFolders = new List<string>();
				List<string> ruleFilePathes = new List<string>();
				List<string> chromeLayoutFilePathes = new List<string>();

				//Modify mod.yaml file
				string newModYaml = Path.Combine(newModFullPath, "mod.yaml");
				var modYamlNodes = MiniYaml.FromFile(newModYaml);
				foreach (var modYamlNode in modYamlNodes)
				{
					if (modYamlNode.Key == "Metadata")
					{
						var modYamlMetadataTitleNode = modYamlNode.Value.Nodes.Where(o => o.Key == "Title").FirstOrDefault();
						if (modYamlMetadataTitleNode != null)
						{
							localizationName = localizationName.Replace(localizationName.Substring(0, 1), localizationName.Substring(0, 1).ToUpper());
							modYamlMetadataTitleNode.Value.Value += string.Format(" ({0} Version)", localizationName);
						}
					}
					else if (modYamlNode.Key == "Packages")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							if (subYamlNode.Key == string.Format("${0}", modID))
							{
								subYamlNode.Key = string.Format("${0}", newModID);
								subYamlNode.Value.Value = newModID;
							}
							else if (subYamlNode.Key.StartsWith(string.Format("{0}|", modID)))
							{
								string oldKey = string.Format("{0}|", modID);
								subYamlNode.Key = subYamlNode.Key.Replace(
									oldKey,
									string.Format("{0}|", newModID)
								);
							}
							else if (subYamlNode.Key.StartsWith("~"))
							{
								if (subYamlNode.Key.Substring(1).StartsWith("^"))
								{
									subYamlNode.Key = "~^" + ReplacePathWithNewModID(subYamlNode.Key.Substring(2), modID, newModID);
								}
							}
						}
					}
					else if (modYamlNode.Key == "Rules" ||
							 modYamlNode.Key == "Sequences" ||
							 modYamlNode.Key == "ModelSequences" ||
							 modYamlNode.Key == "Cursors" ||
							 modYamlNode.Key == "Chrome" ||
							 modYamlNode.Key == "Assemblies" ||
							 modYamlNode.Key == "ChromeLayout" ||
							 modYamlNode.Key == "Hotkeys" ||
							 modYamlNode.Key == "Weapons" ||
							 modYamlNode.Key == "Voices" ||
							 modYamlNode.Key == "Notifications" ||
							 modYamlNode.Key == "TileSets" ||
							 modYamlNode.Key == "Music" ||
							 modYamlNode.Key == "Translations" ||
							 modYamlNode.Key == "ChromeMetrics" ||
							 modYamlNode.Key == "Missions")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							if (subYamlNode.Key.StartsWith(string.Format("{0}|", modID)))
							{
								string oldKey = string.Format("{0}|", modID);
								subYamlNode.Key = subYamlNode.Key.Replace(
									oldKey,
									string.Format("{0}|", newModID)
								);

								if (modYamlNode.Key == "Rules")
								{
									ruleFilePathes.Add(Path.Combine(newModFullPath, subYamlNode.Key.Split('|')[1]));
								}
								else if (modYamlNode.Key == "ChromeLayout")
								{
									chromeLayoutFilePathes.Add(Path.Combine(newModFullPath, subYamlNode.Key.Split('|')[1]));
								}
							}
						}
					}
					else if (modYamlNode.Key == "MapFolders")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							if (subYamlNode.Key.StartsWith(string.Format("{0}|", modID)))
							{
								string oldKey = string.Format("{0}|", modID);
								subYamlNode.Key = subYamlNode.Key.Replace(
									oldKey,
									string.Format("{0}|", newModID)
								);

								string[] tokens = subYamlNode.Key.Split('|');//Relative map folder
								mapFolders.Add(Path.Combine(newModFullPath, tokens[1]));
							}
							else if (subYamlNode.Key.StartsWith("~"))//optional map folder
							{
								string temp = subYamlNode.Key.Substring(1);
								if (temp.StartsWith("^"))
								{
									string[] pathBlocks = temp.Substring(1).Split('/');
									int idx = 0;
									string pathAfterModified = string.Empty;
									foreach (var pathBlock in pathBlocks)
									{
										if (pathBlock == modID)
										{
											pathBlocks[idx] = newModID;
										}
										pathAfterModified = Path.Combine(pathAfterModified, pathBlocks[idx]);
										idx++;
									}
									temp = "^" + pathAfterModified.Replace("\\", "/");
									string fullPath = Platform.ResolvePath(temp);
									mapFolders.Add(fullPath);
									subYamlNode.Key = "~" + temp;
								}
							}
						}
					}
					else if (modYamlNode.Key == "LoadScreen")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							if (subYamlNode.Value.Value.StartsWith(string.Format("{0}|", modID)))
							{
								string oldKey = string.Format("{0}|", modID);
								subYamlNode.Value.Value = subYamlNode.Value.Value.Replace(
									oldKey,
									string.Format("{0}|", newModID)
								);
							}
						}
					}
					else if (modYamlNode.Key == "Fonts")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							foreach (var sNode in subYamlNode.Value.Nodes)
							{
								if (sNode.Key == "Font")
								{
									if (sNode.Value.Value.StartsWith(string.Format("{0}|", modID)))
									{
										string oldKey = string.Format("{0}|", modID);
										sNode.Value.Value = sNode.Value.Value.Replace(
											oldKey,
											string.Format("{0}|", newModID)
										);
									}
								}
							}
						}
					}
					else if (modYamlNode.Key == "ModContent")
					{
						foreach (var subYamlNode in modYamlNode.Value.Nodes)
						{
							if (subYamlNode.Key == "InstallPromptMessage")
							{
								//Translate
								if (modContentLocalizationNode != null)
								{
									var installPromptLocalization = modContentLocalizationNode.Value.Nodes.Where(o => o.Key == "InstallPromptMessage").FirstOrDefault();
									if (installPromptLocalization != null)
									{
										subYamlNode.Value.Value = installPromptLocalization.Value.Value;
									}
								}
							}
							else if (subYamlNode.Key == "HeaderMessage")
							{
								//Translate
								if (modContentLocalizationNode != null)
								{
									var headerMessageLocalization = modContentLocalizationNode.Value.Nodes.Where(o => o.Key == "HeaderMessage").FirstOrDefault();
									if (headerMessageLocalization != null)
									{
										subYamlNode.Value.Value = headerMessageLocalization.Value.Value;
									}
								}
							}
							else if (subYamlNode.Key == "Packages")
							{
								foreach (var sNode in subYamlNode.Value.Nodes)
								{

									//Translate
									if (modContentLocalizationNode != null)
									{
										var packageLocalization = modContentLocalizationNode.Value.Nodes.Where(o => o.Key == "Packages").FirstOrDefault();
										if (packageLocalization != null)
										{
											var subPackageLocalizationNode = packageLocalization.Value.Nodes.Where(o => o.Key == sNode.Key).FirstOrDefault();
											if (subPackageLocalizationNode != null)
											{
												sNode.Value.Value = subPackageLocalizationNode.Value.Value;
											}
										}
									}

									foreach (var sn in sNode.Value.Nodes)
									{
										if (sn.Key == "TestFiles")
										{
											string[] contentFilePathes = sn.Value.Value.Split(',');
											int idx = 0;
											StringBuilder builder = new StringBuilder();
											foreach (var contentFilePath in contentFilePathes)
											{
												string path = null;
												var contentFilePathTemp = contentFilePath.Trim();
												if (contentFilePathTemp.StartsWith("^"))
												{
													path = contentFilePathTemp.Substring(1);
												}
												else
												{
													path = contentFilePathTemp;
												}
												contentFilePathes[idx] = "^" + ReplacePathWithNewModID(path, modID, newModID);
												builder.Append(contentFilePathes[idx] + ",");
												idx++;
											}
											sn.Value.Value = builder.ToString();
										}
									}
								}
							}
							else if (subYamlNode.Key == "Sources")
							{
								foreach (var sNode in subYamlNode.Value.Nodes)
								{
									if (sNode.Key.StartsWith(string.Format("{0}|", modID)))
									{
										string oldKey = string.Format("{0}|", modID);
										sNode.Key = sNode.Key.Replace(
											oldKey,
											string.Format("{0}|", newModID)
										);
									}
								}
							}
						}
					}
				}
				modYamlNodes.WriteToFile(newModYaml);

				//Modify all maps
				foreach (var mapFolder in mapFolders)
				{
					DirectoryInfo mapDir = new DirectoryInfo(mapFolder);
					if (!mapDir.Exists)
					{
						continue;
					}
					foreach (var directory in mapDir.EnumerateDirectories())
					{
						if (File.Exists(Path.Combine(directory.FullName, "map.bin")) &&
						   File.Exists(Path.Combine(directory.FullName, "map.yaml")) &&
						   File.Exists(Path.Combine(directory.FullName, "map.png")))
						{
							string mapYamlPath = Path.Combine(directory.FullName, "map.yaml");
							var mapYamlFile = MiniYaml.FromFile(mapYamlPath);
							foreach (var node in mapYamlFile)
							{
								if (node.Key == "RequiresMod")
								{
									node.Value.Value = newModID;
									break;
								}
							}
							mapYamlFile.WriteToFile(mapYamlPath);
						}
					}
				}

				//Translate Rules file
				foreach (var ruleFilePath in ruleFilePathes)
				{
					var ruleYamlFile = MiniYaml.FromFile(ruleFilePath);
					foreach (var node in ruleYamlFile)
					{
						if (rulesLocalizationNode != null && node.Key != "^BaseWorld")
						{
							var actorLocalizationNode = rulesLocalizationNode.Value.Nodes.Where(o => o.Key == node.Key).FirstOrDefault();
							var toolTipTraitNode = node.Value.Nodes.Where(o => o.Key == "Tooltip").FirstOrDefault();
							if (toolTipTraitNode != null && actorLocalizationNode != null)
							{
								toolTipTraitNode.Value.Nodes[0].Value.Value = actorLocalizationNode.Value.Value;
							}
						}
						else if (node.Key == "^BaseWorld")//Translate Factions
						{
							if (worldRulesLocalizationNode != null)
							{
								foreach (var worldChildNode in node.Value.Nodes)
								{
									var factionLocalizationNode = worldRulesLocalizationNode.Value.Nodes.Where(o => o.Key == worldChildNode.Key).FirstOrDefault();
									if (factionLocalizationNode != null)
									{
										foreach (var valueNode in worldChildNode.Value.Nodes)
										{
											var valueLocalizationNode = factionLocalizationNode.Value.Nodes.Where(o => o.Key == valueNode.Key).FirstOrDefault();
											if (valueLocalizationNode != null)
											{
												valueNode.Value.Value = valueLocalizationNode.Value.Value;
											}
										}
									}
								}
							}
						}
					}
					ruleYamlFile.WriteToFile(ruleFilePath);
				}

				//Translate Chrome Layouts
				foreach (var chromeLayoutFilePath in chromeLayoutFilePathes)
				{
					var chromeLayoutFile = MiniYaml.FromFile(chromeLayoutFilePath);
					foreach (var chromeNode in chromeLayoutFile)
					{
						var chromeLocalizationNode = chromeLayoutsLocalizationNode.Value.Nodes.Where(o => o.Key == chromeNode.Key).FirstOrDefault();
						if (chromeLocalizationNode != null)
						{
							translateChrome(chromeNode, chromeLocalizationNode);
						}
					}
					chromeLayoutFile.WriteToFile(chromeLayoutFilePath);
				}
			}

			Console.WriteLine("Import task has already finished!");
		}

		private void translateChrome(MiniYamlNode chromeNode, MiniYamlNode chromeLocalizationNode)
		{
			foreach(var subNode in chromeNode.Value.Nodes)
			{
				var subChromeLocalizationNode = chromeLocalizationNode.Value.Nodes.Where(o => o.Key == subNode.Key).FirstOrDefault();
				if (subChromeLocalizationNode != null)
				{
					if (!string.IsNullOrEmpty(subNode.Value.Value))
					{
						subNode.Value.Value = subChromeLocalizationNode.Value.Value;
					}
					translateChrome(subNode, subChromeLocalizationNode);
				}
			}
		}

		private string ReplacePathWithNewModID(string path, string oldModID, string newModID)
		{
			string[] pathBlocks = path.Split('/');
			int idx = 0;
			string pathAfterModified = string.Empty;
			foreach (var pathBlock in pathBlocks)
			{
				if (pathBlock == oldModID)
				{
					pathBlocks[idx] = newModID;
				}
				pathAfterModified = Path.Combine(pathAfterModified, pathBlocks[idx]);
				idx++;
			}
			return pathAfterModified.Replace("\\", "/");
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
