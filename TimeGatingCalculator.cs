using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AnNoText.TB.Service.TimeEntries
{
	/// <summary>
	/// Stellt Methoden zur Berechnung der Taktung bzw. getakteter Zeiten bereit.
	/// </summary>
	public static class TimeGatingCalculator
	{
		/// <summary>
		/// Stellt sicher, dass mit einer Taktung auch sicher gerechnet werden kann.
		/// </summary>
		/// <param name="gatingSize"></param>
		/// <returns>Mit einer Taktung von 0 Minuten kann z.B. nicht gerechnet werden.</returns>
		public static TimeSpan MakeGatingTimeCalculatable(TimeSpan gatingSize)
		{
			return TimeSpan.FromMinutes(Math.Max(gatingSize.TotalMinutes, 1));
		}

		/// <summary>
		/// Ruft die getaktete Zeit ab.
		/// </summary>
		public static TimeSpan GetGatedTime(TimeSpan ungatedTime, TimeSpan gatingSize)
        {
            var seconds = (Decimal)(Int64)ungatedTime.TotalSeconds;

			var numberOfGatings = (Int32)Math.Ceiling((seconds/60) / (decimal)gatingSize.TotalMinutes);
			var gatedTime = TimeSpan.FromMinutes((numberOfGatings * gatingSize.TotalMinutes));
			return gatedTime;
		}
	}
}
