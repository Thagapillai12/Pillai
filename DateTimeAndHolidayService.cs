using System;
using System.Collections.Generic;
using System.Linq;
using WK.DE.DocumentManagement.Contracts.Services;

namespace WK.DE.DocumentManagement.Services.DateTimeAndHolidays
{
	public static class DateTimeAndHolidayService
	{
		public static bool IsHoursAndMinutesStringValid(string hoursAndMinutesString)
		{
			short hours, minutes;
			return GetHoursAndMinutesFromString(hoursAndMinutesString, out hours, out minutes);
		}

		/// <summary>
		/// Gets the number of minutes and hours from a string in format &quot;hh:mm&quot;, e.g. &quot;10:12&quot;.
		/// </summary>
		public static bool GetHoursAndMinutesFromString(string hoursAndMinutesString, out short hours, out short minutes)
		{
			hours = 0;
			minutes = 0;

			if (String.IsNullOrWhiteSpace(hoursAndMinutesString))
			{
				return false;
			}

			var monitoringParts = hoursAndMinutesString.Split(':');
			if (monitoringParts.Length == 2 && Int16.TryParse(monitoringParts[0], out hours)
				&& Int16.TryParse(monitoringParts[1], out minutes))
			{
				if (hours <= 24 && minutes <= 60)
				{
					return true;
				}
			}
			hours = 0;
			minutes = 0;
			return false;
		}

		/// <summary>
		/// Gets the given date and time without seconds fragment.
		/// </summary>
		public static DateTime GetDateTimeWithoutSeconds(DateTime dateTime)
		{
			return dateTime.AddSeconds(-dateTime.Second);
		}

		public static bool IsDateTimeOnWeekend(DateTime dateTime)
		{
			return dateTime.DayOfWeek == DayOfWeek.Saturday || dateTime.DayOfWeek == DayOfWeek.Sunday;
		}

		public static DateTime GetBusinessDayBeforeWeekend(DateTime dateTime)
		{
			if (dateTime.DayOfWeek == DayOfWeek.Saturday)
			{
				return dateTime.AddDays(-1);
			}
			else if (dateTime.DayOfWeek == DayOfWeek.Sunday)
			{
				return dateTime.AddDays(-2);
			}
			return dateTime;
		}

		public static DateTime GetBusinessDayBeforePublicHolidayAndWeekend(DateTime dateTime)
		{
			var result = dateTime;
			while (IsDateTimeOnHoliday(result, HolidayType.PublicHoliday)
				|| IsDateTimeOnWeekend(result))
			{
				result = result.AddDays(-1);
			}
			return result;
		}

		public static DateTime GetBusinessDayAfterPublicHolidayAndWeekend(DateTime dateTime)
		{
			var result = dateTime;
			while (IsDateTimeOnHoliday(result, HolidayType.PublicHoliday)
				|| IsDateTimeOnWeekend(result))
			{
				result = result.AddDays(1);
			}
			return result;
		}

		internal static bool IsDateTimeOnHoliday(DateTime dateTime, HolidayType holidayType)
		{
			return IsDateTimeOnHoliday(dateTime, holidayType, null);
		}

		internal static bool IsDateTimeOnHoliday(DateTime dateTime, HolidayType holidayType, IPublicHolidayCalendarService publicHolidayCalendarService)
		{
			var knownHolidaysFromDate = PublicHolidayCalculator.GetKnownPublicHolidayFromDate(dateTime);
			if (publicHolidayCalendarService != null)
			{
				var holidayNamesToWarn = publicHolidayCalendarService.GetHolidayNamesToWarn();
				knownHolidaysFromDate = knownHolidaysFromDate.Where(x => holidayNamesToWarn.Contains(x.Name, StringComparer.OrdinalIgnoreCase)).ToList();
			}
			return knownHolidaysFromDate.Any(x => x.Type == holidayType);
		}

		internal static List<PublicHolidayCalculator.PublicHolidayInfo> GetHolidayInfo(DateTime dateTime, HolidayType holidayType)
		{
			return PublicHolidayCalculator.GetKnownPublicHolidayFromDate(dateTime).Where(x => x.Type == holidayType).ToList();
		}

		public static string GetHolidayDescription(DateTime dateTime, HolidayType holidayType)
		{
			var holidayInfo = DateTimeAndHolidayService.GetHolidayInfo(dateTime, holidayType);
			if (holidayInfo == null || !holidayInfo.Any())
			{
				return null;
			}

			string text;
			var holidayInfoGroupedByFederalState = holidayInfo.GroupBy(x => x.FederalState);
			if (holidayInfoGroupedByFederalState.Count() == 1)
			{
				var federalState = holidayInfoGroupedByFederalState.First().First().FederalState;
				if (federalState == FederalState.All)
				{
					text = String.Format("\"{0}\" am {1} ist ein bundeseinheitlicher Feiertag.", holidayInfo.First().Name, dateTime.ToString("D"));
				}
				else
				{
					text = String.Format("\"{0}\" am {1} ist in {2} ein Feiertag.", holidayInfo.First().Name, dateTime.ToString("D"), GetFederalStateFriendlyName(federalState));
				}
			}
			else
			{
				var federalStates = Environment.NewLine + "  - " + String.Join(Environment.NewLine + "  - ", holidayInfoGroupedByFederalState.Select(x => GetFederalStateFriendlyName(x.First().FederalState)));
				text = String.Format("\"{0}\" am {1} ist in folgenden Bundesländern ein Feiertag:{2}", holidayInfo.First().Name, dateTime.ToString("D"), federalStates);
			}

			return text;
		}

		public static string GetFederalStateFriendlyName(FederalState federalState)
		{
			switch (federalState)
			{
				case FederalState.BadenWuerttemberg: return "Baden-Württemberg";
				case FederalState.Bavaria: return "Bayern";
				case FederalState.BavariaAugsburg: return "Augsburg";
				case FederalState.Berlin: return "Berlin";
				case FederalState.Brandenburg: return "Brandenburg";
				case FederalState.Bremen: return "Bremen";
				case FederalState.Hamburg: return "Hamburg";
				case FederalState.Hesse: return "Hessen";
				case FederalState.LowerSaxony: return "Niedersachsen";
				case FederalState.MecklenburgWesternPomerania: return "Mecklenburg-Vorpommern";
				case FederalState.NorthRhineWestphalia: return "Nordrhein-Westfalen";
				case FederalState.RhinelandPalatinate: return "Rheinland-Pfalz";
				case FederalState.Saarland: return "Saarland";
				case FederalState.Saxony: return "Sachsen";
				case FederalState.SaxonyAnhalt: return "Sachsen-Anhalt";
				case FederalState.SchleswigHolstein: return "Schleswig-Holstein";
				case FederalState.Thuringia: return "Thüringen";
				default:
					throw new NotImplementedException(federalState.ToString());
			}
		}
	}
}