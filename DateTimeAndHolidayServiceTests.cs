using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WK.DE.DocumentManagement.Services.DateTimeAndHolidays;

namespace WK.DE.DocumentManagement.IntegrationTests.DateTimeAndHoliday
{
	[TestClass]
	public class DateTimeAndHolidayServiceTests
	{
		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString1()
		{
			// ============================== arrange ==============================
			var input = "10:10";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(10, hours);
			Assert.AreEqual(10, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString2()
		{
			// ============================== arrange ==============================
			var input = "11:12";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(11, hours);
			Assert.AreEqual(12, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString3()
		{
			// ============================== arrange ==============================
			var input = "0:0";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(0, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString4()
		{
			// ============================== arrange ==============================
			var input = "00:00";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(0, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString5()
		{
			// ============================== arrange ==============================
			var input = "06:03";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(6, hours);
			Assert.AreEqual(3, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString6()
		{
			// ============================== arrange ==============================
			var input = "00:03";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsTrue(isValid);
			Assert.IsTrue(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(3, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString7()
		{
			// ============================== arrange ==============================
			var input = "06:61";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsFalse(isValid);
			Assert.IsFalse(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(0, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString8()
		{
			// ============================== arrange ==============================
			var input = "25:61";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsFalse(isValid);
			Assert.IsFalse(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(0, minutes);
		}

		[TestMethod]
		public void DTH_GetHoursAndMinutesFromString9()
		{
			// ============================== arrange ==============================
			var input = "25:17";
			short hours, minutes;

			// ==============================   act   ==============================
			var isValid = DateTimeAndHolidayService.GetHoursAndMinutesFromString(input, out hours, out minutes);
			var isValid2 = DateTimeAndHolidayService.IsHoursAndMinutesStringValid(input);

			// ==============================  assert ==============================
			Assert.IsFalse(isValid);
			Assert.IsFalse(isValid2);
			Assert.AreEqual(0, hours);
			Assert.AreEqual(0, minutes);
		}

		[TestMethod]
		public void DTH_GetDateTimeWithoutSeconds1()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2021, 10, 13, 13, 48, 47);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetDateTimeWithoutSeconds(input);

			// ==============================  assert ==============================
			Assert.AreEqual(new DateTime(2021, 10, 13, 13, 48, 0), result);
		}

		[TestMethod]
		public void DTH_GetDateTimeWithoutSeconds2()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2021, 10, 13, 13, 48, 0);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetDateTimeWithoutSeconds(input);

			// ==============================  assert ==============================
			Assert.AreEqual(new DateTime(2021, 10, 13, 13, 48, 0), result);
		}

		[TestMethod]
		public void DTH_GetHolidayDescription1()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2021, 12, 25, 11, 12, 13);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetHolidayDescription(input, HolidayType.PublicHoliday);

			// ==============================  assert ==============================
			Assert.AreEqual("\"1. Weihnachtsfeiertag\" am Samstag, 25. Dezember 2021 ist ein bundeseinheitlicher Feiertag.", result);
		}

		[TestMethod]
		public void DTH_GetHolidayDescription2()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2021, 03, 08, 11, 12, 13);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetHolidayDescription(input, HolidayType.PublicHoliday);

			// ==============================  assert ==============================
			Assert.AreEqual("\"Internationaler Frauentag\" am Montag, 8. März 2021 ist in Berlin ein Feiertag.", result);
		}

		[TestMethod]
		public void DTH_GetHolidayDescription3()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2021, 06, 03, 11, 12, 13);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetHolidayDescription(input, HolidayType.PublicHoliday);

			// ==============================  assert ==============================
			var stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("\"Fronleichnam\" am Donnerstag, 3. Juni 2021 ist in folgenden Bundesländern ein Feiertag:");
			stringBuilder.AppendLine("  - Baden-Württemberg");
			stringBuilder.AppendLine("  - Bayern");
			stringBuilder.AppendLine("  - Hessen");
			stringBuilder.AppendLine("  - Nordrhein-Westfalen");
			stringBuilder.AppendLine("  - Rheinland-Pfalz");
			stringBuilder.AppendLine("  - Saarland");
			stringBuilder.AppendLine("  - Sachsen");
			stringBuilder.Append("  - Thüringen");
			Assert.AreEqual(stringBuilder.ToString(), result);
		}

		[TestMethod]
		public void DTH_GetBusinessDayBeforePublicHolidayAndWeekend()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2024, 11, 01);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetBusinessDayBeforePublicHolidayAndWeekend(input);

			// ==============================  assert ==============================
			Assert.AreEqual(new DateTime(2024, 10, 30), result);
		}

		[TestMethod]
		public void DTH_GetBusinessDayAfterPublicHolidayAndWeekend()
		{
			// ============================== arrange ==============================
			var input = new DateTime(2024, 11, 01);

			// ==============================   act   ==============================
			var result = DateTimeAndHolidayService.GetBusinessDayAfterPublicHolidayAndWeekend(input);

			// ==============================  assert ==============================
			Assert.AreEqual(new DateTime(2024, 11, 04), result);
		}
	}
}
