using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Lexvor.API {
	public static class StaticUtils {
		public static string MaskIMEI(string imei) {
			if (imei.Length != 15 && imei.Length != 16) {
				return imei;
			}

			return $"{imei.Substring(0, 2)} {imei.Substring(2, 6)} {imei.Substring(8, 6)} {imei.Substring(14)}";
		}

		/// <summary>
		/// Strip all characters EXCEPT for numerics
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public static string NumericStrip(string input) {
			if (string.IsNullOrWhiteSpace(input)) {
				return "";
			}
			return Regex.Replace(input, @"[^\d]", "");
		}
		
		public static string FormatPhone(string phone) {
			if(string.IsNullOrWhiteSpace(phone)) {
				return "";
			}
			return Regex.Replace(phone, @"(\d{3})(\d{3})(\d{4})", "($1) $2-$3");
		}

		public static double ConvertKBToGB(int kb) {
			return Math.Round((double)kb / (1024 * 1000), 4);
		}

		public static double ConvertKBToGB(double kb) {
			return Math.Round(kb / (1024 * 1000), 4);
		}
	}
}
