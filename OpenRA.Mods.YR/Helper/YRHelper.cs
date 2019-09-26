using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenRA.Mods.YR.Helper
{
	public static class YRHelper
	{
		public static string SetFirstLetterUpper(this string str)
		{
			if (!string.IsNullOrWhiteSpace(str))
			{
				string upperFirst = str.Substring(0, 1).ToUpper();
				var newStr = str.Replace(str.Substring(0, 1), upperFirst);
				return newStr;
			}
			else
			{
				return null;
			}
		}
	}
}
