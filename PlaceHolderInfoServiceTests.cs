using AnNoText.Rechnungen.Printing;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using WK.DE.OXmlMerge;

namespace AnNoText.Abrechnung.IntegrationTests
{
	[TestClass]
	public class PlaceHolderInfoServiceTests
	{
		private object OptimizeForUnitTest(string xml)
		{
			return Regex.Replace(xml, " w:rsid(R)?(P)?(RPr)?(RDefault)?(Tr)?=\"[0-9A-Z]*\"", "");
		}

		[TestMethod]
		public void PlaceHolder_Get_SeveralFromOneText()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00C41A69\" w:rsidRDefault=\"006A218E\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[UMSATZUSER] ([STD] Std. à [STDSATZ])</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			// ==============================   act   ==============================
			var placeHolderInfos = PlaceHolderInfoService.GetPlaceHolders(sourceParagraph);
			Assert.AreEqual(3, placeHolderInfos.Count);
			string name0 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[0]);
			string name1 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[1]);
			string name2 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[2]);

			// ==============================  assert ==============================
			Assert.AreEqual("UMSATZUSER", name0);
			Assert.AreEqual("STD", name1);
			Assert.AreEqual("STDSATZ", name2);
		}

		[TestMethod]
		public void PlaceHolder_Replace_SeveralFromOneText()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00C41A69\" w:rsidRDefault=\"006A218E\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[UMSATZUSER] ([STD] Std. à [STDSATZ])</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				if (placeHolderName == "STDSATZ")
					return "100";

				else if (placeHolderName == "STD")
					return "2";

				else if (placeHolderName == "UMSATZUSER")
					return "Mr. Smith";

				return placeHolderName;
			};

			// ==============================   act   ==============================
			new AnNoTextFieldExecuter().ReplacePlaceHolders(sourceParagraph, getPlaceHolderValue);

			string targetXml = sourceParagraph.OuterXml;

			// ==============================  assert ==============================
			string expectedXML = "<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:rsidR=\"00C41A69\" w:rsidRDefault=\"006A218E\"><w:r><w:t>Mr. Smith</w:t><w:t xml:space=\"preserve\"> (</w:t><w:t xml:space=\"preserve\">2</w:t><w:t xml:space=\"preserve\"> Std. à </w:t><w:t xml:space=\"preserve\">100</w:t><w:t>)</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /></w:p>";
			Assert.AreEqual(OptimizeForUnitTest(expectedXML), OptimizeForUnitTest(targetXml));
		}

		[TestMethod]
		public void PlaceHolder_Get_OneFromSeveralTexts()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"00825400\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[</w:t></w:r><w:r w:rsidRPr=\"00103F2D\" w:rsidR=\"00103F2D\"><w:t>M_ZE_LFDNR</w:t></w:r><w:r><w:t>]</w:t></w:r><w:r w:rsidR=\"000E5C71\"><w:t>.</w:t></w:r></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			// ==============================   act   ==============================
			var placeHolderInfos = PlaceHolderInfoService.GetPlaceHolders(sourceParagraph);
			Assert.AreEqual(1, placeHolderInfos.Count);
			string name0 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[0]);

			// ==============================  assert ==============================
			Assert.AreEqual("M_ZE_LFDNR", name0);
		}

		[TestMethod]
		public void PlaceHolder_Replace_OneFromSeveralTexts()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"00825400\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[</w:t></w:r><w:r w:rsidRPr=\"00103F2D\" w:rsidR=\"00103F2D\"><w:t>M_ZE_LFDNR</w:t></w:r><w:r><w:t>]</w:t></w:r><w:r w:rsidR=\"000E5C71\"><w:t>.</w:t></w:r></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				if (placeHolderName == "M_ZE_LFDNR")
					return "BlindeKuh";

				return placeHolderName;
			};

			// ==============================   act   ==============================
			new AnNoTextFieldExecuter().ReplacePlaceHolders(sourceParagraph, getPlaceHolderValue);

			string targetXml = sourceParagraph.OuterXml;

			// ==============================  assert ==============================
			Assert.AreEqual(OptimizeForUnitTest("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>BlindeKuh</w:t></w:r><w:r><w:t>.</w:t></w:r></w:p>"), OptimizeForUnitTest(targetXml));
		}

		[TestMethod]
		public void PlaceHolder_Replace_OneFromSeveralTexts_ContentWithCr()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"00825400\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[</w:t></w:r><w:r w:rsidRPr=\"00103F2D\" w:rsidR=\"00103F2D\"><w:t>M_ZE_LFDNR</w:t></w:r><w:r><w:t>]</w:t></w:r><w:r w:rsidR=\"000E5C71\"><w:t>.</w:t></w:r></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				if (placeHolderName == "M_ZE_LFDNR")
					return "BlindeKuh\nist ein Spiel";

				return placeHolderName;
			};

			// ==============================   act   ==============================
			new AnNoTextFieldExecuter().ReplacePlaceHolders(sourceParagraph, getPlaceHolderValue);
			
			string targetXml = sourceParagraph.OuterXml;

			// ==============================  assert ==============================
			Assert.AreEqual(OptimizeForUnitTest("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>BlindeKuh</w:t></w:r><w:r><w:t>.</w:t></w:r></w:p>"), OptimizeForUnitTest(targetXml));
		}

		[TestMethod]
		public void PlaceHolder_Get_SeveralFromSeveralTexts_StartAndContentInFirst()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"000E5C71\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[M_ZE_UMSATZB_</w:t></w:r><w:r><w:t>V</w:t></w:r><w:r w:rsidRPr=\"000E5C71\"><w:t>NAME</w:t></w:r><w:r><w:t>]</w:t></w:r><w:r><w:t xml:space=\"preserve\"> [M_ZE_UMSATZB_</w:t></w:r><w:r w:rsidRPr=\"000E5C71\"><w:t>NAME</w:t></w:r><w:r><w:t>] (</w:t></w:r><w:r><w:t>[</w:t></w:r><w:r><w:t>M_ZE_</w:t></w:r><w:r><w:t>STD] Std. à [</w:t></w:r><w:r><w:t>M_ZE_</w:t></w:r><w:r><w:t>STDSATZ]</w:t></w:r><w:r><w:t>)</w:t></w:r></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			// ==============================   act   ==============================
			var placeHolderInfos = PlaceHolderInfoService.GetPlaceHolders(sourceParagraph);
			Assert.AreEqual(4, placeHolderInfos.Count);
			string name0 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[0]);
			string name1 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[1]);
			string name2 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[2]);
			string name3 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[3]);

			// ==============================  assert ==============================
			Assert.AreEqual("M_ZE_UMSATZB_VNAME", name0);
			Assert.AreEqual("M_ZE_UMSATZB_NAME", name1);
			Assert.AreEqual("M_ZE_STD", name2);
			Assert.AreEqual("M_ZE_STDSATZ", name3);
		}

		[TestMethod]
		public void PlaceHolder_Replace_SeveralFromSeveralTexts_StartAndContentInFirst()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:p w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"000E5C71\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:r><w:t>[M_ZE_UMSATZB_</w:t></w:r><w:r><w:t>V</w:t></w:r><w:r w:rsidRPr=\"000E5C71\"><w:t>NAME</w:t></w:r><w:r><w:t>]</w:t></w:r><w:r><w:t xml:space=\"preserve\"> [M_ZE_UMSATZB_</w:t></w:r><w:r w:rsidRPr=\"000E5C71\"><w:t>NAME</w:t></w:r><w:r><w:t>] (</w:t></w:r><w:r><w:t>[</w:t></w:r><w:r><w:t>M_ZE_</w:t></w:r><w:r><w:t>STD] Std. à [</w:t></w:r><w:r><w:t>M_ZE_</w:t></w:r><w:r><w:t>STDSATZ]</w:t></w:r><w:r><w:t>)</w:t></w:r></w:p>";
			var sourceParagraph = new Paragraph(sourceXML);

			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				if (placeHolderName == "M_ZE_UMSATZB_VNAME")
					return "Vorname";

				else if (placeHolderName == "M_ZE_UMSATZB_NAME")
					return "Nachname";

				else if (placeHolderName == "M_ZE_STD")
					return "2";

				else if (placeHolderName == "M_ZE_STDSATZ")
					return "100";

				return placeHolderName;
			};

			// ==============================   act   ==============================
			new AnNoTextFieldExecuter().ReplacePlaceHolders(sourceParagraph, getPlaceHolderValue);

			string targetXml = sourceParagraph.OuterXml;

			// ==============================  assert ==============================
			Assert.AreEqual(OptimizeForUnitTest("<w:p xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\" w:rsidR=\"00825400\" w:rsidP=\"00825400\" w:rsidRDefault=\"000E5C71\"><w:r><w:t>Vorname</w:t></w:r><w:r><w:t xml:space=\"preserve\"> </w:t></w:r><w:r w:rsidRPr=\"000E5C71\"><w:t>Nachname</w:t></w:r><w:r><w:t xml:space=\"preserve\"> (</w:t></w:r><w:r><w:t>2</w:t></w:r><w:r><w:t xml:space=\"preserve\"> Std. à </w:t></w:r><w:r><w:t>100</w:t></w:r><w:r><w:t>)</w:t></w:r></w:p>"), OptimizeForUnitTest(targetXml));
		}

		[TestMethod]
		public void PlaceHolder_Get_OneFromSeveralTexts_StartAndContentInFirst()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:tr w:rsidR=\"00B3244E\" w:rsidTr=\"00116AA3\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tc><w:tcPr><w:tcW w:w=\"7650\" w:type=\"dxa\" /><w:gridSpan w:val=\"3\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"007379E2\"><w:pPr><w:spacing w:before=\"120\" /></w:pPr><w:r><w:t>[M_RE_SUB</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /><w:r><w:t>SUMTEXT]</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"1554\" w:type=\"dxa\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"00B3244E\"><w:pPr><w:spacing w:before=\"120\" /><w:jc w:val=\"right\" /></w:pPr><w:r><w:t>[M_RE_</w:t></w:r><w:r w:rsidR=\"007379E2\"><w:t>SUBSUM</w:t></w:r><w:r><w:t>]</w:t></w:r></w:p></w:tc></w:tr>";
			var sourceTableRow = new TableRow(sourceXML);

			// ==============================   act   ==============================
			var placeHolderInfos = PlaceHolderInfoService.GetPlaceHolders(sourceTableRow);
			Assert.AreEqual(2, placeHolderInfos.Count);
			string name0 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[0]);
			string name1 = PlaceHolderInfoService.GetPlaceHolderName(placeHolderInfos[1]);

			// ==============================  assert ==============================
			Assert.AreEqual("M_RE_SUBSUMTEXT", name0);
			Assert.AreEqual("M_RE_SUBSUM", name1);
		}

		[TestMethod]
		public void PlaceHolder_Replace_OneFromSeveralTexts_StartAndContentInFirst()
		{
			// ============================== arrange ==============================
			var sourceXML = "<w:tr w:rsidR=\"00B3244E\" w:rsidTr=\"00116AA3\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tc><w:tcPr><w:tcW w:w=\"7650\" w:type=\"dxa\" /><w:gridSpan w:val=\"3\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"007379E2\"><w:pPr><w:spacing w:before=\"120\" /></w:pPr><w:r><w:t>[M_RE_SUB</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /><w:r><w:t>SUMTEXT]</w:t></w:r></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"1554\" w:type=\"dxa\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"00B3244E\"><w:pPr><w:spacing w:before=\"120\" /><w:jc w:val=\"right\" /></w:pPr><w:r><w:t>[M_RE_</w:t></w:r><w:r w:rsidR=\"007379E2\"><w:t>SUBSUM</w:t></w:r><w:r><w:t>]</w:t></w:r></w:p></w:tc></w:tr>";
			var sourceTableRow = new TableRow(sourceXML);

			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				if (placeHolderName == "M_RE_SUBSUMTEXT")
					return "Zwischensumme";

				else if (placeHolderName == "M_RE_SUBSUM")
					return "200";

				return placeHolderName;
			};

			// ==============================   act   ==============================
			new AnNoTextFieldExecuter().ReplacePlaceHolders(sourceTableRow, getPlaceHolderValue);

			string targetXml = sourceTableRow.OuterXml;

			// ==============================  assert ==============================
			string expectedXML = "<w:tr w:rsidR=\"00B3244E\" w:rsidTr=\"00116AA3\" xmlns:w=\"http://schemas.openxmlformats.org/wordprocessingml/2006/main\"><w:tc><w:tcPr><w:tcW w:w=\"7650\" w:type=\"dxa\" /><w:gridSpan w:val=\"3\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"007379E2\"><w:pPr><w:spacing w:before=\"120\" /></w:pPr><w:r><w:t /><w:t>Zwischensumme</w:t></w:r><w:bookmarkStart w:name=\"_GoBack\" w:id=\"0\" /><w:bookmarkEnd w:id=\"0\" /></w:p></w:tc><w:tc><w:tcPr><w:tcW w:w=\"1554\" w:type=\"dxa\" /><w:tcBorders><w:bottom w:val=\"single\" w:color=\"auto\" w:sz=\"4\" w:space=\"0\" /></w:tcBorders></w:tcPr><w:p w:rsidR=\"00B3244E\" w:rsidP=\"00BE6428\" w:rsidRDefault=\"00B3244E\"><w:pPr><w:spacing w:before=\"120\" /><w:jc w:val=\"right\" /></w:pPr><w:r><w:t>200</w:t></w:r></w:p></w:tc></w:tr>";
			Assert.AreEqual(OptimizeForUnitTest(expectedXML), OptimizeForUnitTest(targetXml));
		}
	}
}
