using AnNoText.Abrechnung.Control;
using AnNoText.Abrechnung.Control.Fees_v2;
using AnNoText.Rechnungen.Abrechnung;
using AnNoText.TB.Service.TimeEntries;
using AT.BL;
using AT.Core.Objects;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using log4net;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using WK.DE.Invoice.Contracts;
using WK.DE.OXmlMerge;

namespace AnNoText.Rechnungen.Printing
{
	public class TimeAndBillingPrintController
	{
		private static readonly Regex m_InvalidXMLChars = new Regex(@"(?<![\uD800-\uDBFF])[\uDC00-\uDFFF]|[\uD800-\uDBFF](?![\uDC00-\uDFFF])|[\x00-\x08\x0B\x0C\x0E-\x1F\x7F-\x9F\uFEFF\uFFFE\uFFFF]", RegexOptions.Compiled);

		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(TimeAndBillingPrintController));

		/// <summary>
		/// Gibt Bezeichner für die Art der Summe an, die abgerufen werden soll.
		/// </summary>
		public enum SumType
		{
			/// <summary>
			/// Unbekannt.
			/// </summary>
			Undefined,

			/// <summary>
			/// Ruft die volle Summe ohne Aufteilungen ab.
			/// </summary>
			OnlyPositionSplitted,

			/// <summary>
			/// Ruft die anteilige Summe für die Teilung der gesamten Rechnung ab (sind Positionen geteilt, wird das hierbei nicht berücksichtigt).
			/// </summary>
			TotalSplitted,
		}

		/// <summary>
		/// Stellt Informationen zu den Inhaltszeilen dar.
		/// </summary>
		private class ContentRowInformation
		{
			/// <summary>
			/// Ruft die Zeilen ab, die für jeden Zeiteintrag mit Stundensatz ausgegeben werden sollen.
			/// </summary>
			public List<TableRow> ContentRowsTypeHourlyRate
			{ get; private set; }

			/// Ruft die Zeilen ab, die für jeden Zeiteintrag mit Pauschale ausgegeben werden sollen.
			/// </summary>
			public List<TableRow> ContentRowsTypeFlatCharge
			{ get; private set; }

			/// <summary>
			/// Ruft eine ggf. vom Anwender festgelegte Gruppierung ab.
			/// </summary>
			public List<string> GroupingInformation
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeile, die zur Ausgeben von Zwischensummen verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public TableRow SpecialRowSubSum
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeile, die zur Ausgeben der Gesamtsumme verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public TableRow SpecialRowTotalSum
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeile, die zur Ausgabe von Auslagen verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public TableRow SpecialRowExpenses
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeile, die zur Ausgabe von Zusatztext (Zusatztext zur Rechnung und Reverse Charge Hinweis) verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public TableRow SpecialRowAdditionalText
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeile, die zur Ausgabe von Einführungstext zur Rechnung verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public TableRow SpecialRowTopText
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeil, die zur Ausgabe von Gruppenüberschriften verwendet werden soll, ab oder legt diese fest.
			/// </summary>
			public List<TableRow> SpecialRowGroupingHeader
			{ get; private set; }

			/// <summary>
			/// Ruft die Zeilen, die zur Ausgabe von Gruppenunterschriften verwendet werden sollen, ab oder legt diese fest.
			/// </summary>
			public List<TableRow> SpecialRowGroupingFooter
			{ get; private set; }

			/// <summary>
			/// Ruft ggf. eine Zeile ab, mit der eine manuelle Summe unter der Tabelle eingefügt werden soll (wird für Anlagen verwendet).
			/// </summary>
			public TableRow SpecialRowManualSum
			{ get; private set; }

			/// <summary>
			/// Initialisiert eine neue Instanz der ContentRowInformation-Klasse.
			/// </summary>
			public ContentRowInformation(List<TableRow> contentRowsHourlyRate, List<TableRow> contentRowsFlatCharge, TableRow specialRowSubSum, TableRow specialRowTotalSum, TableRow specialRowExpenses, TableRow specialRowAdditionalText, List<string> groupingInformation, List<TableRow> specialRowGroupingHeader, List<TableRow> specialRowGroupingFooter, TableRow specialRowManualSum, TableRow specialRowTopText)
			{
				this.ContentRowsTypeHourlyRate = contentRowsHourlyRate;
				this.ContentRowsTypeFlatCharge = contentRowsFlatCharge;
				this.SpecialRowSubSum = specialRowSubSum;
				this.SpecialRowTotalSum = specialRowTotalSum;
				this.SpecialRowExpenses = specialRowExpenses;
				this.SpecialRowAdditionalText = specialRowAdditionalText;
				this.GroupingInformation = groupingInformation;
				this.SpecialRowGroupingHeader = specialRowGroupingHeader;
				this.SpecialRowGroupingFooter = specialRowGroupingFooter;
				this.SpecialRowManualSum = specialRowManualSum;
				this.SpecialRowTopText = specialRowTopText;
			}
		}

		private IList<IInvoiceItem> m_PositionsExpensesWithTax;
		private IList<IInvoiceItem> m_PositionsExpensesWithoutTax;
		private IList<IInvoiceItem> m_PositionsPaymentsWithoutTax;
		private IList<IInvoiceItem> m_PositionsForeignMoney;
		private IList<IInvoiceItem> m_PositionsExpensesAccounts;
		private IList<IInvoiceItem> m_PositionsExpensesPayments;

		private decimal m_SummeGeGutNetto = 0.00m;
		private decimal m_SummeGeGutMwst = 0.00m;
		private decimal m_SummeGeGutStfr = 0.00m;
		private decimal m_SummeGeGutFG = 0.00m;
		private decimal m_NettoSummeSplitting = 0.00m;

		/// <summary>
		/// Get-/Setter-Variable für Culture.
		/// </summary>
		private CultureInfo m_Culture;

		/// <summary>
		/// Ruft die Spracheinstellungen, mit denen Datum und Beträge formatiert werden sollen, ab oder legt diese fest.
		/// </summary>
		public CultureInfo Culture
		{
			get { return m_Culture; }
			set { m_Culture = value; }
		}

		/// <summary>
		/// Getter-Variable für Currency.
		/// </summary>
		private string m_Currency;

		/// <summary>
		/// Ruft das Währungssymbol, mit der die Rechnung ausgegeben werden soll, ab oder legt dieses fest.
		/// </summary>
		public string Currency
		{
			get { return m_Currency; }
			set { m_Currency = value; }
		}

		/// <summary>
		/// Getter-Variable für DocumentIsLandscape .
		/// </summary>
		private bool m_DocumentIsLandscape;

		/// <summary>
		/// Ruft ab, ob für das Dokument Querformat aktiviert wurde.
		/// </summary>
		public bool DocumentIsLandscape
		{
			get { return m_DocumentIsLandscape; }
		}

		/// <summary>
		/// Get-/Setter-Variable für Invoice.
		/// </summary>
		private IRechnung m_Invoice;

		/// <summary>
		/// Ruft die Rechnung, die gedruckt werden soll, ab oder legt diese fest.
		/// </summary>
		public IRechnung Invoice
		{
			get { return m_Invoice; }
			set { m_Invoice = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für InvoicesInOtherInvoices.
		/// </summary>
		private IList<IRechnungskopfAZ> m_InvoicesInOtherInvoices;

		/// <summary>
		/// Ruft alle Rechnungen ab, die in anderen Rechnungen übernommen wurden, ab oder legt diese fest.
		/// </summary>
		/// <remarks>entspricht m_RechnungenInRechnungUebernommen (Detailsmischen)</remarks>
		public IList<IRechnungskopfAZ> InvoicesInOtherInvoices
		{
			get { return m_InvoicesInOtherInvoices; }
			set { m_InvoicesInOtherInvoices = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für Invoices.
		/// </summary>
		private IList<IAuslagen> m_Invoices;

		/// <summary>
		/// Ruft alle Rechnungen ab, die in der aktuellen Rechnung enthalten sind, ab oder legt diese fest.
		/// </summary>
		/// <remarks>entspricht m_VorschussRechnungen (Detailsmischen)</remarks>
		public IList<IAuslagen> Invoices
		{
			get { return m_Invoices; }
			set { m_Invoices = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für Invoices.
		/// </summary>
		private IList<IAuslagen> m_Payments;

		/// <summary>
		/// Ruft alle Rechnungen ab, die in der aktuellen Rechnung enthalten sind, ab oder legt diese fest.
		/// </summary>
		/// <remarks>entspricht m_GeldeingaengeGutschrift (Detailsmischen)</remarks>
		public IList<IAuslagen> Payments
		{
			get { return m_Payments; }
			set { m_Payments = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für PerformancePeriodBegin.
		/// </summary>
		private DateTime? m_PerformancePeriodBegin;

		/// <summary>
		/// Ruft den Beginn des Leistungszeitraums ab oder legt diesen fest.
		/// </summary>
		public DateTime? PerformancePeriodBegin
		{
			get { return m_PerformancePeriodBegin; }
			set { m_PerformancePeriodBegin = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für PerformancePeriodEnd.
		/// </summary>
		private DateTime? m_PerformancePeriodEnd;

		/// <summary>
		/// Ruft das Ende des Leistungszeitraums ab oder legt diesen fest.
		/// </summary>
		public DateTime? PerformancePeriodEnd
		{
			get { return m_PerformancePeriodEnd; }
			set { m_PerformancePeriodEnd = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für PerformanceInvoiceDate.
		/// </summary>
		private DateTime? m_PerformanceInvoiceDate;

		/// <summary>
		/// Ruft das Rechnungsdatum ab oder legt diesen fest.
		/// </summary>
		public DateTime? PerformanceInvoiceDate
		{
			get { return m_PerformanceInvoiceDate; }
			set { m_PerformanceInvoiceDate = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für PerformanceInvoiceDate.
		/// </summary>
		private string m_PerformanceInvoiceNumber;

		/// <summary>
		/// Ruft das Rechnungsnummer ab oder legt diesen fest.
		/// </summary>
		public string PerformanceInvoiceNumber
		{
			get { return m_PerformanceInvoiceNumber; }
			set { m_PerformanceInvoiceNumber = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für PrintReverseCharge.
		/// </summary>
		private bool m_PrintReverseCharge;

		/// <summary>
		/// Ruft ab oder legt fest, ob der Reverse-Charge-Hinweis mit auf die Rechnung aufgebracht werden soll.
		/// </summary>
		public bool PrintReverseCharge
		{
			get { return m_PrintReverseCharge; }
			set { m_PrintReverseCharge = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für InvoiceUnit.
		/// </summary>
		private string m_InvoiceUnit;

		/// <summary>
		/// Ruft die Ausgabe der Einheiten ab oder legt diese fest.
		/// </summary>
		public string InvoiceUnit
		{
			get { return m_InvoiceUnit; }
			set { m_InvoiceUnit = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für InvoiceUnit.
		/// </summary>
		private UnitType m_InvoiceUnitType;

		/// <summary>
		/// Ruft den Typ der Einheiten ab oder legt diesen fest
		/// </summary>
		public UnitType InvoiceUnitType
		{
			get { return m_InvoiceUnitType; }
			set { m_InvoiceUnitType = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für TranslationService.
		/// </summary>
		private ITranslationService m_TranslationService;

		/// <summary>
		/// Ruft den Service für die Übersetzungen ab oder legt diesen fest.
		/// </summary>
		public ITranslationService TranslationService
		{
			get { return m_TranslationService; }
			set { m_TranslationService = value; }
		}

		/// <summary>
		/// Get-/Setter-Variable für LineTB.
		/// </summary>
		private string m_LineTB;

		/// <summary>
		/// Ruft ZEILE "TIME" ab.
		/// </summary>
		public string LineTB
		{
			get { return m_LineTB; }
			set { m_LineTB = value; }
		}

		public int[] SpaltenbreitenTB20thOfAPoint { get; set; }
		public bool IstStornorechnungMoeglich { get; set; }
		public bool IstTBZeileSummeHonorare { get; set; }
		public IKostenberechnungHost KostenberechnungHost { get; set; }
		public bool IstAnnotext { get; set; }
		public string UstSatz { get; set; }
		public string Tabelle { get; set; }
		public int IdVR { get; set; }
		public string StrKuerzel { get; set; }


		/// <summary>
		/// Ruft eine Auflistung der Zeiteinträge, die ausgedruckt werden sollen, ab oder legt diese fest.
		/// </summary>
		public IList<IErfassteZeiten> TimeEntries
		{ get; set; }

		/// <summary>
		/// Ruft eine gefilterte Auflistung der auszugebenden Zeiteinträge ab.
		/// </summary>
		private IList<IErfassteZeiten> TimeEntriesFiltered
		{
			get
			{
				if (!TimeEntries0PercentInOutput)
					return this.TimeEntries.Where(x => x.Bewertung != 0).ToList();
				return this.TimeEntries;
			}
		}

		/// <summary>
		/// Ruft ab oder legt fest, ob Zeiteinträge mit 0% Bewertung im Ausdruck ausgegeben werden sollen.
		/// </summary>
		public bool TimeEntries0PercentInOutput
		{ get; set; }

		/// <summary>
		/// Ruft den Teilungsfaktor für die Positionen ab.
		/// </summary>
		public decimal InvoiceSplittingFactor
		{
			get { return GetSplittingFactor(m_Invoice.TeilungsArt, m_Invoice.TeilungsFaktor); }
		}

		/// <summary>
		/// Getter-Variable für DocumentIsLandscape .
		/// </summary>
		private decimal m_TotalSumInvoice;

		/// <summary>
		/// Ruft ab, ob für das Dokument Querformat aktiviert wurde.
		/// </summary>
		public decimal TotalSumInvoice
		{
			get { return m_TotalSumInvoice; }
		}

		//Gequotelte Rechnungen
		public DetailsRechnung DetailsRechnung;
		public List<AnteileSummeNetto> ListAnteileSummeNetto;
		public int ZaehlerRechnung;
		public int ZaehlerStornierung;
		public bool IstAnlage;



		/// <summary>
		/// Initialisiert eine neue Instanz der TimeAndBillingPrintController-Klasse.
		/// </summary>
		public TimeAndBillingPrintController()
		{
			this.TimeEntries0PercentInOutput = true;
			m_Currency = "€";
			m_DocumentIsLandscape = true;

			IsolatedStorageHelper.VerifySecurityEvidenceForIsolatedStorage();
		}

		/// <summary>
		/// Erwartet in dem angegebenen Word-Template-Dokument eine Tabelle mit Zeilen, die Platzhalter enthalten, füllt diese Tabelle mit Daten der TimeEntries aus und speichert dieses unter der angegebenen Zieldatei.
		/// </summary>
		public void CreateDocument(string templateDocument, string targetFileName)
		{
			File.Copy(templateDocument, targetFileName, true);
			CreateDocument(targetFileName);
		}

		/// <summary>
		/// Erwartet in dem angegebenen Word-Dokument eine Tabelle mit Zeilen, die Platzhalter enthalten und füllt diese Tabelle mit Daten der TimeEntries aus.
		/// </summary>
		public void CreateDocument(string targetFileName)
		{
			using (var memoryStream = new MemoryStream())
			{
				var bytes = File.ReadAllBytes(targetFileName);
				memoryStream.Write(bytes, 0, bytes.Length);

				using (var wordDocument = WordprocessingDocument.Open(memoryStream, true))
					CreateDocument(wordDocument);

				File.WriteAllBytes(targetFileName, memoryStream.ToArray());
			}
		}

		/// <summary>
		/// Erwartet in dem angegebenen Word-Dokument eine Tabelle mit Zeilen, die Platzhalter enthalten und füllt diese Tabelle mit Daten der TimeEntries aus.
		/// </summary>
		public void CreateDocument(WordprocessingDocument targetDocument)
		{
			if (targetDocument == null)
			{
				throw new ArgumentNullException(nameof(targetDocument));
			}
			if (m_TranslationService == null)
			{
				throw new ArgumentNullException("TranslationService", "Es wurde keine Instanz des TranslationService zugewiesen, es kann nicht gemischt werden.");
			}
			if (m_Invoice == null)
			{
				throw new ArgumentNullException("Invoice", "Es wurde keine Instanz der zu druckenden Rechnung zugewiesen, es kann nicht gemischt werden.");
			}

			AnalyzePositions();

			var body = targetDocument.MainDocumentPart?.Document?.Body;
			if (body == null)
			{
				throw new ArgumentNullException("body", "time and billing template document does not contain body");
			}
			if (body.ChildElements == null)
			{
				throw new ArgumentNullException("childelements", "time and billing template document does not contain child elements");
			}
			var bodyElements = body.ChildElements.OfType<OpenXmlCompositeElement>().ToList();

			//Haupt-Tabelle ausfindig machen
			var tables = bodyElements.OfType<Table>();
			foreach (var table in tables)
			{
				ProcessTable(table);
			}

			//in anderen Hauptparagraphen ggf. Platzhalter tauschen
			var paragraphs = bodyElements.OfType<Paragraph>();
			foreach (var paragraph in paragraphs)
			{
				Func<string, string> getPlaceHolderValue = (placeHolderName) =>
				{
					return GetValueForPlaceHolder(placeHolderName, this.TimeEntriesFiltered, null, null);
				};
				new AnNoTextFieldExecuter().ReplacePlaceHolders(paragraph, getPlaceHolderValue);
			}
		}

		public void AnalyzePositions()
		{
			var urkunde = m_Invoice.Urkunden.FirstOrDefault();
			if (urkunde != null)
			{
				var lineTimeBilling = urkunde.Items.FirstOrDefault(x => x.Identifier == "TIME");
				if (lineTimeBilling != null)
					m_LineTB = lineTimeBilling.FeeDescription;
				m_PositionsExpensesWithTax = urkunde.Items.Where(x => x.Identifier != "TIME" && x.Identifier != "VSR" && x.Identifier != "GEGUT" && x.Type != "L" && x.ExpenseCategoryCode != "L").ToList();
				m_PositionsExpensesWithoutTax = urkunde.Items.Where(x => x.Identifier != "TIME" && x.Identifier != "VSR" && x.Identifier != "GEGUT" && x.Identifier != "FREMDG" && x.Amount >= 0.00m && (x.ExpenseCategoryCode == "L" || x.Type == "L")).ToList();
				m_PositionsPaymentsWithoutTax = urkunde.Items.Where(x => x.Identifier != "TIME" && x.Identifier != "VSR" && x.Identifier != "GEGUT" && x.Identifier != "FREMDG" && x.Amount < 0.00m && (x.ExpenseCategoryCode == "L" || x.Type == "L")).ToList();
				m_PositionsForeignMoney = urkunde.Items.Where(x => x.Identifier == "FREMDG").ToList();
				m_PositionsExpensesAccounts = urkunde.Items.Where(x => x.Identifier == "VSR").ToList();
				m_PositionsExpensesPayments = urkunde.Items.Where(x => x.Identifier == "GEGUT").ToList();
			}
			else
			{
				m_PositionsExpensesWithTax = new List<IInvoiceItem>();
				m_PositionsExpensesWithoutTax = new List<IInvoiceItem>();
				m_PositionsPaymentsWithoutTax = new List<IInvoiceItem>();
				m_PositionsForeignMoney = new List<IInvoiceItem>();
				m_PositionsExpensesAccounts = new List<IInvoiceItem>();
				m_PositionsExpensesPayments = new List<IInvoiceItem>();
			}
		}

		/// <summary>
		/// Füllt die Platzhalter der Tabelle aus.
		/// </summary>
		private void ProcessTable(Table table)
		{
			//Tabelle in Header, eigentliche Zeilen für den Inhalt und Footer unterteilen
			//	die Inhaltszeilen werden dann pro TimeEntry vervielfacht
			List<TableRow> headerRows, contentRows, footerRows, topRows;
			List<PlaceHolderInfo> contentPlaceHolders;
			SplitTableRows(table, out topRows, out headerRows, out contentRows, out footerRows, out contentPlaceHolders, SpaltenbreitenTB20thOfAPoint);

			if (contentRows == null)
			{
				m_Logger.Debug("Keine Platzhalter erkannt");
				return;
			}

			//alle FooterRows zunächst aus der Tabelle ausklinken, diese werden dann später wieder angefügt
			headerRows.ForEach(x => x.Remove());
			CleanEmptyRows(headerRows);

			if (topRows != null)
			{
				topRows.ForEach(x => x.Remove());
				CleanEmptyRows(topRows);
				//if(topRows.Count > 0)
				InsertTopText(table, topRows[0]);
			}
			//jetzt alle Footer wieder einfügen
			headerRows.ForEach(x => ProcessPlaceHolders(x, this.TimeEntriesFiltered, null, null));
			headerRows.ForEach(x => table.Append((TableRow)x.Clone()));

			//CleanEmptyRows(headerRows);

			//alle FooterRows zunächst aus der Tabelle ausklinken, diese werden dann später wieder angefügt
			footerRows.ForEach(x => x.Remove());
			CleanEmptyRows(footerRows);

			//alle ContentRows auch ausklinken und dann pro TimeEntry wieder einfügen
			contentRows.ForEach(x => x.Remove());

			var contentRowInformation = ExtractSpecialContentRows(contentRows);

			bool disableSubSumNet = false;
			var contentPlaceHoldersNames = contentPlaceHolders.Select(x => PlaceHolderInfoService.GetPlaceHolderName(x)).ToList();
			if (contentRowInformation.GroupingInformation.Count == 1 && String.Compare(contentRowInformation.GroupingInformation[0], "GRP_EZ", true) == 0)
			{
				InsertTimeEntriesOneTotalRow(table, contentRowInformation);
				disableSubSumNet = true;
			}
			else if (contentRowInformation.GroupingInformation.Count > 0)
			{
				InsertGroupedTimeEntries(table, contentRowInformation, 0, this.TimeEntriesFiltered);
			}
			else
			{
				InsertTimeEntiresOneRowPerTimeEntry(table, contentRowInformation);
			}
			InsertExpensesWithVAT(table, contentRowInformation, ref disableSubSumNet);

			if (m_Invoice.TeilungsArt == ItemDivisionType.Keine)
			{
				if (!disableSubSumNet)
					InsertSubSumNet(table, contentRowInformation, SumType.OnlyPositionSplitted, "SUMTN");

				InsertVAT(table, contentRowInformation, SumType.OnlyPositionSplitted);

				var subSumNetWithTax = InsertSubSumNetWithTax(table, contentRowInformation, SumType.OnlyPositionSplitted);
				bool removeSubSumNetWithTax = true;

				InsertExpensesWithoutVAT(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				InsertPaymentsWithoutVAT(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				InsertForeignMoney(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);
				if (m_Invoices != null && m_Invoices.Count > 0)
					InsertExpensesAccounts(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				if (m_Payments != null && m_Payments.Count > 0)
					InsertExpensesPayments(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				if (removeSubSumNetWithTax && subSumNetWithTax != null)
					subSumNetWithTax.Remove();

				if (GetSumTotal(SumType.OnlyPositionSplitted) > 0.00m)
					InsertTotalSum(table, contentRowInformation, SumType.OnlyPositionSplitted, "SUMTI");
				else
					InsertTotalSum(table, contentRowInformation, SumType.OnlyPositionSplitted, "GUTHA");
			}
			else
			{
				//****************
				//Zeilen vor dem Anteil, Gesamtnettobetrag, Ust, Auslagen
				if (!IstAnlage && ZaehlerRechnung == 0 && ListAnteileSummeNetto != null && ListAnteileSummeNetto.Count() > 0)
					BerechneNettoSummenBeiQuotelung(this.GetSumNet(SumType.OnlyPositionSplitted), ListAnteileSummeNetto);
				if (!disableSubSumNet)
					InsertSubSumNet(table, contentRowInformation, SumType.OnlyPositionSplitted, "TAXIT");

				InsertVAT(table, contentRowInformation, SumType.OnlyPositionSplitted);

				var subSumNetWithTax = InsertSubSumNetWithTax(table, contentRowInformation, SumType.OnlyPositionSplitted);
				bool removeSubSumNetWithTax = true;

				InsertExpensesWithoutVAT(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				InsertPaymentsWithoutVAT(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				InsertForeignMoney(table, contentRowInformation, SumType.OnlyPositionSplitted, ref removeSubSumNetWithTax);

				if (removeSubSumNetWithTax && subSumNetWithTax != null)
					subSumNetWithTax.Remove();

				if (GetSumTotal(SumType.OnlyPositionSplitted) > 0.00m)
					InsertTotalSum(table, contentRowInformation, SumType.OnlyPositionSplitted, "SUMTS");
				else
					InsertTotalSum(table, contentRowInformation, SumType.OnlyPositionSplitted, "GUTHA");
				//****************

				if (!IstAnlage && ListAnteileSummeNetto != null && ListAnteileSummeNetto.Count() > 0)
				{
					if (IstStornorechnungMoeglich)
						m_NettoSummeSplitting = ListAnteileSummeNetto[0].AnteilSummeNetto[ZaehlerStornierung];
					else
						m_NettoSummeSplitting = ListAnteileSummeNetto[0].AnteilSummeNetto[ZaehlerRechnung];
					var NettoSumme = Gebuehrenberechnung.Runden(this.GetSumNet(SumType.TotalSplitted));
					if (NettoSumme != 0.00m && NettoSumme != m_NettoSummeSplitting)
						DetailsRechnungAnpassen(NettoSumme, m_NettoSummeSplitting, DetailsRechnung);
				}
				InsertSplitting(table, contentRowInformation);

				InsertVAT(table, contentRowInformation, SumType.TotalSplitted);

				subSumNetWithTax = InsertSubSumNetWithTax(table, contentRowInformation, SumType.TotalSplitted);
				removeSubSumNetWithTax = true;

				InsertExpensesWithoutVAT(table, contentRowInformation, SumType.TotalSplitted, ref removeSubSumNetWithTax);

				InsertPaymentsWithoutVAT(table, contentRowInformation, SumType.TotalSplitted, ref removeSubSumNetWithTax);

				InsertForeignMoney(table, contentRowInformation, SumType.TotalSplitted, ref removeSubSumNetWithTax);

				if (m_Invoices != null && m_Invoices.Count > 0)
					InsertExpensesAccounts(table, contentRowInformation, SumType.TotalSplitted, ref removeSubSumNetWithTax);

				if (m_Payments != null && m_Payments.Count > 0)
					InsertExpensesPayments(table, contentRowInformation, SumType.TotalSplitted, ref removeSubSumNetWithTax);

				if (removeSubSumNetWithTax && subSumNetWithTax != null)
					subSumNetWithTax.Remove();

				if (GetSumTotal(SumType.TotalSplitted) > 0.00m)
					InsertTotalSum(table, contentRowInformation, SumType.TotalSplitted, "SUMTI");
				else
					InsertTotalSum(table, contentRowInformation, SumType.TotalSplitted, "GUTHA");
			}

			if (contentRowInformation.SpecialRowManualSum != null)
			{
				var contentRowCopy = (TableRow)contentRowInformation.SpecialRowManualSum.Clone();
				ProcessPlaceHolders(contentRowCopy, this.TimeEntriesFiltered, null, null);
				table.Append(contentRowCopy);
			}

			InsertAdditionalText(table, contentRowInformation);

			//jetzt alle Footer wieder einfügen
			footerRows.ForEach(x => table.Append((TableRow)x.Clone()));
		}

		private void InsertGroupedTimeEntries(Table table, ContentRowInformation contentRowInformation, int groupingDepth, IEnumerable<IErfassteZeiten> timeEntries)
		{
			var groupingInformation = contentRowInformation.GroupingInformation[groupingDepth];

			if (String.Compare(groupingInformation, "GRP_ABRECHB_EZ", true) == 0)
			{
				var groups = timeEntries.GroupBy(x => new { x.IdMitarbeiterAbrechnung, x.MitarbeiterAbrechnung.Nachname, x.Stundensatz }).ToDictionary(x => x.Key);
				foreach (var group in groups.OrderBy(x => x.Key.Nachname).ThenBy(x => x.Key.Stundensatz))
				{
					var groupedTimeEntries = group.Value;
					InsertGroupHeader(table, contentRowInformation, groupingDepth, groupedTimeEntries);

					if (groupingDepth + 1 < contentRowInformation.GroupingInformation.Count)
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth + 1, groupedTimeEntries);
					else
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth, true, groupedTimeEntries);

					InsertGroupFooter(table, contentRowInformation, groupingDepth, groupedTimeEntries);
				}
			}
			else if (String.Compare(groupingInformation, "GRP_ABRECHB_D", true) == 0)
			{
				var groups = timeEntries.GroupBy(x => new { x.MitarbeiterAbrechnung.Nachname }).ToDictionary(x => x.Key);
				foreach (var group in groups.OrderBy(x => x.Key.Nachname))
				{
					var groupedTimeEntries = group.Value;
					InsertGroupHeader(table, contentRowInformation, groupingDepth, groupedTimeEntries);

					if (groupingDepth + 1 < contentRowInformation.GroupingInformation.Count)
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth + 1, groupedTimeEntries);
					else
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth, false, groupedTimeEntries);

					InsertGroupFooter(table, contentRowInformation, groupingDepth, groupedTimeEntries);
				}
			}
			else if (String.Compare(groupingInformation, "GRP_AKTE_EZ", true) == 0
						|| String.Compare(groupingInformation, "GRP_AKTE_D", true) == 0)
			{
				foreach (var group in timeEntries.OrderBy(x => x.Akte != null ? x.Akte.Jahr2000 : "").ThenBy(x => x.Akte != null ? x.Akte.AZ2000 : "")
					.GroupBy(x => x.Akte.AZ))
				{
					var groupedTimeEntries = group.ToList();
					InsertGroupHeader(table, contentRowInformation, groupingDepth, groupedTimeEntries);

					if (groupingDepth + 1 < contentRowInformation.GroupingInformation.Count)
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth + 1, groupedTimeEntries);
					else
						InsertGroupedTimeEntries(table, contentRowInformation, groupingDepth, String.Compare(groupingInformation, "GRP_AKTE_EZ", true) == 0, groupedTimeEntries);

					InsertGroupFooter(table, contentRowInformation, groupingDepth, groupedTimeEntries);
				}
			}
		}

		private void InsertSubSumNet(Table table, ContentRowInformation contentRowInformation, SumType sumType, string textId)
		{
			InsertSum(table, contentRowInformation.SpecialRowSubSum, this.GetSumNet(sumType), m_TranslationService.GetText(textId));
		}

		private void InsertSplitting(Table table, ContentRowInformation contentRowInformation)
		{
			if (m_Invoice.TeilungsArt != ItemDivisionType.Keine)
			{
				string text = GetSplittingText(m_Invoice.TeilungsArt, m_Invoice.TeilungsFaktor, null);
				var nettoSummeSplitted = GetSumNet(SumType.TotalSplitted);
				if (m_NettoSummeSplitting > 0.00m)
					nettoSummeSplitted = m_NettoSummeSplitting;
				InsertSum(table, contentRowInformation.SpecialRowSubSum, nettoSummeSplitted, text + Environment.NewLine + m_TranslationService.GetText("SUMTN"));
			}
		}

		private void InsertTopText(Table table, TableRow topRow)
		{
			var topText = new StringBuilder();
			if (!String.IsNullOrWhiteSpace(m_Invoice.Einfuehrungstext))
			{
				topText.Append(m_Invoice.Einfuehrungstext);
				topText.Append(Environment.NewLine);
			}

			if (topText.Length > 0 && topRow != null)
			{
				var specialPlaceHolder = new Dictionary<string, string>();
				specialPlaceHolder.Add("M_RE_EINFUEHRUNGSTEXT", topText.ToString());

				var topRowCopy = (TableRow)topRow.Clone();
				ProcessPlaceHolders(topRowCopy, null, null, specialPlaceHolder);
				table.Append(topRowCopy);
			}
		}

		private void InsertAdditionalText(Table table, ContentRowInformation contentRowInformation)
		{
			var additionalText = new StringBuilder();
			if (!String.IsNullOrWhiteSpace(m_Invoice.Zusatztext))
				additionalText.Append(m_Invoice.Zusatztext);

			if (this.PrintReverseCharge)
			{
				if (additionalText.Length > 0)
					additionalText.AppendLine();
				additionalText.Append(m_TranslationService.GetText("REVERSE"));
			}

			if (additionalText.Length > 0 && contentRowInformation.SpecialRowAdditionalText != null)
			{
				additionalText.Insert(0, Environment.NewLine);
				var specialPlaceHolder = new Dictionary<string, string>();
				specialPlaceHolder.Add("M_RE_ZUSATZTEXTE", additionalText.ToString());

				var contentRowCopy = (TableRow)contentRowInformation.SpecialRowAdditionalText.Clone();
				ProcessPlaceHolders(contentRowCopy, null, null, specialPlaceHolder);
				table.Append(contentRowCopy);
			}
		}

		private void InsertVAT(Table table, ContentRowInformation contentRowInformation, SumType sumType)
		{
			if (contentRowInformation.SpecialRowExpenses != null)
			{
				string vatText = String.Format(m_Culture, "{1:F2} % {0}", m_TranslationService.GetText("7008"), m_Invoice.Mehrwertsteuersatz);
				if (sumType == SumType.OnlyPositionSplitted && m_Invoice.TeilungsArt != ItemDivisionType.Keine)
					vatText = m_TranslationService.GetText("TAXIF") + " " + vatText;
				InsertExpenseRow(table, contentRowInformation, GetSumTaxOnNet(sumType), vatText, false);
			}
		}

		private void InsertTotalSum(Table table, ContentRowInformation contentRowInformation, SumType sumType, string textId)
		{
			m_TotalSumInvoice = GetSumTotal(sumType);
			InsertSum(table, contentRowInformation.SpecialRowTotalSum, GetSumTotal(sumType), m_TranslationService.GetText(textId));
		}

		private TableRow InsertSubSumNetWithTax(Table table, ContentRowInformation contentRowInformation, SumType sumType)
		{
			return InsertSum(table, contentRowInformation.SpecialRowSubSum, GetSumNetWithTax(sumType), m_TranslationService.GetText("SUMSB"));
		}

		private TableRow InsertSum(Table table, TableRow tableRowTemplate, decimal sum, string sumText)
		{
			if (tableRowTemplate == null)
				return null;

			var sumStr = this.ConvertToString(sum, true);

			var specialPlaceHolder = new Dictionary<string, string>();
			specialPlaceHolder.Add("M_RE_SUBSUM", sumStr);
			specialPlaceHolder.Add("M_RE_TOTALSUM", sumStr);
			specialPlaceHolder.Add("M_RE_SUBSUMTEXT", sumText);
			specialPlaceHolder.Add("M_RE_TOTALSUMTEXT", sumText);

			var contentRowCopy = (TableRow)tableRowTemplate.Clone();
			ProcessPlaceHolders(contentRowCopy, null, null, specialPlaceHolder);
			table.Append(contentRowCopy);

			return contentRowCopy;
		}

		private void InsertExpensesWithVAT(Table table, ContentRowInformation contentRowInformation, ref bool disableSubSumNet)
		{
			if (m_PositionsExpensesWithTax.Count > 0)
			{
				disableSubSumNet = false; //sofern die Netto-Zwischensumme abgeschaltet wurde, hier wieder aktivieren
				InsertExpenses(table, contentRowInformation, m_PositionsExpensesWithTax);
			}
		}

		private void InsertExpensesWithoutVAT(Table table, ContentRowInformation contentRowInformation, SumType sumType, ref bool removeSubSumNetWithTax)
		{
			if (sumType == SumType.TotalSplitted)
			{
				if (GetSumExpensesWithoutTax(sumType) != 0.00m)
				{
					removeSubSumNetWithTax = false;
					InsertExpenseRow(table, contentRowInformation, GetSumExpensesWithoutTax(sumType), m_TranslationService.GetText("VERKO"), false);
				}
			}
			else
			{
				if (m_PositionsExpensesWithoutTax.Any())
				{
					removeSubSumNetWithTax = false;
					InsertExpenses(table, contentRowInformation, m_PositionsExpensesWithoutTax);
				}
			}
		}

		private void InsertPaymentsWithoutVAT(Table table, ContentRowInformation contentRowInformation, SumType sumType, ref bool removeSubSumNetWithTax)
		{
			if (sumType == SumType.TotalSplitted)
			{
				if (GetSumPaymentsWithoutTax(sumType) != 0.00m)
				{
					removeSubSumNetWithTax = false;
					InsertExpenseRow(table, contentRowInformation, GetSumPaymentsWithoutTax(sumType), m_TranslationService.GetText("ZAHLF"), false);
				}
			}
			else
			{
				if (m_PositionsPaymentsWithoutTax.Any())
				{
					removeSubSumNetWithTax = false;
					InsertExpenses(table, contentRowInformation, m_PositionsPaymentsWithoutTax);
				}
			}
		}

		private void InsertForeignMoney(Table table, ContentRowInformation contentRowInformation, SumType sumType, ref bool removeSubSumNetWithTax)
		{
			if (sumType == SumType.TotalSplitted)
			{
				if (GetSumForeignMoney(sumType) != 0.00m)
				{
					removeSubSumNetWithTax = false;
					InsertExpenseRow(table, contentRowInformation, GetSumForeignMoney(sumType), m_TranslationService.GetText("ZAHLU"), false);
				}
			}
			else
			{
				if (m_PositionsForeignMoney.Any())
				{
					removeSubSumNetWithTax = false;
					InsertExpenses(table, contentRowInformation, m_PositionsForeignMoney);
				}
			}
		}

		private void InsertExpensesAccounts(Table table, ContentRowInformation contentRowInformation, SumType sumType, ref bool removeSubSumNetWithTax)
		{
			if (m_PositionsExpensesAccounts.Any())
			{
				removeSubSumNetWithTax = false;
				InsertSum(table, contentRowInformation.SpecialRowSubSum, GetSumTotalWithoutAccounts(sumType), m_TranslationService.GetText("SUMSB"));
				InsertExpensesAccount(table, contentRowInformation, m_PositionsExpensesAccounts);
			}
		}

		private void InsertExpensesPayments(Table table, ContentRowInformation contentRowInformation, SumType sumType, ref bool removeSubSumNetWithTax, bool istGE = false)
		{
			if (m_PositionsExpensesPayments.Any())
			{
				removeSubSumNetWithTax = false;
				InsertSum(table, contentRowInformation.SpecialRowSubSum, GetSumTotalWithoutAccounts(sumType), m_TranslationService.GetText("SUMSB"));
				InsertPayments(table, contentRowInformation, m_PositionsExpensesPayments);
			}
		}

		private void InsertExpenses(Table table, ContentRowInformation contentRowInformation, IEnumerable<IInvoiceItem> expenses)
		{
			if (contentRowInformation.SpecialRowExpenses == null)
				return;

			if (!expenses.Any())
				return;

			foreach (var expense in expenses)
			{
				var expenseAmount = expense.Amount;
				var expenseText = expense.FeeDescription;
				bool additionalLineFeed = false;
				switch (expense.DivisionType)
				{
					case ItemDivisionType.Keine:
						break;

					case ItemDivisionType.ProzentualAuf:
					case ItemDivisionType.ProzentualAb:
						additionalLineFeed = true;
						expenseText += Environment.NewLine + GetSplittingText(expense.DivisionType, expense.DivisionFactor, expense.Amount);
						expenseAmount = expense.AmountAfterSplit;
						break;

					case ItemDivisionType.QuotelungAuf:
					case ItemDivisionType.QuotelungAb:
						additionalLineFeed = true;
						expenseText += Environment.NewLine + GetSplittingText(expense.DivisionType, expense.DivisionFactor, expense.Amount);
						expenseAmount = expense.AmountAfterSplit;
						break;

					default:
						throw new NotImplementedException(expense.DivisionType.ToString());
				}
				if (expense.ItemType == ItemType.Geldbewegung && !String.IsNullOrWhiteSpace(expense.Date))
					expenseText += " " + m_TranslationService.GetText("VOMDATUM") + " " + expense.Date;
				else if (expense.Identifier == "7003" && expense.Quantity > 0)
				{
					expenseText += " (" + expense.Quantity.ToString() + " km à " + ConvertToString(expense.FeeFactor) + ")";
					if (!String.IsNullOrWhiteSpace(expense.Date))
						expenseText += " " + m_TranslationService.GetText("VOMDATUM") + " " + expense.Date;
				}
				InsertExpenseRow(table, contentRowInformation, expenseAmount, expenseText, additionalLineFeed);
			}
		}

		private void InsertExpensesAccount(Table table, ContentRowInformation contentRowInformation, IEnumerable<IInvoiceItem> expenses)
		{
			if (contentRowInformation.SpecialRowExpenses == null)
				return;

			if (!expenses.Any())
				return;

			foreach (var expense in expenses)
			{
				decimal dBetragStpfl = 0.00m, dBetragUst = 0.00m, dBetragStfra = 0.00m;
				var rnr = expense.FeeDescription.Substring(0, 10);
				if (expense.FeeName == rnr)
					GetSumVSR1(m_Invoices, expense.IDExpenses, ref dBetragStpfl, ref dBetragUst, ref dBetragStfra);
				else
					GetSumVSR2(m_InvoicesInOtherInvoices, rnr, ref dBetragStpfl, ref dBetragUst, ref dBetragStfra);
				var expenseAmount = dBetragStpfl + dBetragUst;
				var expenseText = m_TranslationService.GetText("VSRSTPF") + rnr;
				if (IstStornorechnungMoeglich)
					expenseText = expenseText.Replace("Abzgl", "Zuzgl");
				if (!String.IsNullOrWhiteSpace(expense.Date))
					expenseText += " " + m_TranslationService.GetText("VOMDATUM") + " " + expense.Date;
				expenseText += Environment.NewLine + m_TranslationService.GetText("ENTH") + " " + String.Format(m_Culture, "{0:F2}", m_Invoice.Mehrwertsteuersatz) + " % " + m_TranslationService.GetText("7008") + " " + ConvertToString(Math.Abs(dBetragUst)) + ")";
				bool additionalLineFeed = false;
				InsertExpenseRow(table, contentRowInformation, expenseAmount, expenseText, additionalLineFeed);
				if (dBetragStfra != 0.00m)
				{
					expenseAmount = dBetragStfra;
					expenseText = m_TranslationService.GetText("VSRSTFR") + expense.FeeName;
					if (IstStornorechnungMoeglich)
						expenseText = expenseText.Replace("Abzgl", "Zuzgl");
					if (!String.IsNullOrWhiteSpace(expense.Date))
						expenseText += " " + m_TranslationService.GetText("VOMDATUM") + " " + expense.Date;
					additionalLineFeed = false;
					InsertExpenseRow(table, contentRowInformation, expenseAmount, expenseText, additionalLineFeed);
				}
			}
		}

		private void InsertPayments(Table table, ContentRowInformation contentRowInformation, IEnumerable<IInvoiceItem> expenses)
		{
			if (contentRowInformation.SpecialRowExpenses == null)
				return;

			if (!expenses.Any())
				return;

			decimal dBetragStpfl = 0.00m, dBetragUst = 0.00m, dBetragStfra = 0.00m, fremdgeld = 0.00m, diff = 0.00m;
			decimal summeNetto = GetSumNet(SumType.OnlyPositionSplitted), summeMwst = GetSumTaxOnNet(SumType.OnlyPositionSplitted), summeStfr = GetSumExpensesWithoutTax(SumType.OnlyPositionSplitted);
			bool additionalLineFeed = false;

			foreach (var expense in expenses)
			{
				fremdgeld = GetSumGEGUT(m_Payments, expense.IDExpenses) * -1.00m;
				if (summeStfr > 0.00m && fremdgeld > 0.00m && m_SummeGeGutStfr < summeStfr)
				{
					if (fremdgeld > summeStfr)
						diff = summeStfr - m_SummeGeGutStfr;
					else
					{
						if (fremdgeld + m_SummeGeGutStfr > summeStfr)
							diff = summeStfr - m_SummeGeGutStfr;
						else
							diff = fremdgeld;
					}
					dBetragStfra = diff;
					fremdgeld -= diff;
					m_SummeGeGutStfr += diff;
				}
				if (fremdgeld > 0.00m && summeNetto > 0.00m && m_SummeGeGutNetto < summeNetto)
				{
					var fremdgeldNetto = Gebuehrenberechnung.Runden(Gebuehrenberechnung.BerechneNettoVonBrutto(fremdgeld, m_Invoice.Mehrwertsteuersatz));
					var fremdgeldUst = fremdgeld - fremdgeldNetto;

					if (fremdgeldNetto > summeNetto)
						diff = summeNetto - m_SummeGeGutNetto;
					else
					{
						if (fremdgeldNetto + m_SummeGeGutNetto > summeNetto)
							diff = summeNetto - m_SummeGeGutNetto;
						else
							diff = fremdgeldNetto;
					}
					dBetragStpfl = diff;
					dBetragUst = Gebuehrenberechnung.Runden(Gebuehrenberechnung.BerechneUStVonNetto(dBetragStpfl, m_Invoice.Mehrwertsteuersatz));
					fremdgeld -= (dBetragStpfl + dBetragUst);
					m_SummeGeGutNetto += dBetragStpfl;
					m_SummeGeGutMwst += dBetragUst;
					if (((summeNetto + summeMwst) - (m_SummeGeGutNetto + m_SummeGeGutMwst)) > fremdgeld)
					{
						m_SummeGeGutNetto += fremdgeld;
						dBetragStpfl += fremdgeld;
						fremdgeld = 0.00m;
					}
				}
				m_SummeGeGutFG += fremdgeld;
				if (dBetragStpfl > 0.00m)
					dBetragStpfl *= -1.00m;
				if (dBetragUst > 0.00m)
					dBetragUst *= -1.00m;
				if (dBetragStfra > 0.00m)
					dBetragStfra *= -1.00m;

				var expenseText = m_TranslationService.GetText("ZAHLU");
				if (!String.IsNullOrWhiteSpace(expense.Date))
					expenseText += " " + m_TranslationService.GetText("VOMDATUM") + " " + expense.Date;
				var expenseAmount = dBetragStpfl + dBetragUst + dBetragStfra;
				InsertExpenseRow(table, contentRowInformation, expenseAmount, expenseText, additionalLineFeed);
			}
			if (m_SummeGeGutFG > 0.00m)
			{
				var expenseTextFG = "Abzgl. sonstiger Zahlungen";
				InsertExpenseRow(table, contentRowInformation, m_SummeGeGutFG * -1.00m, expenseTextFG, additionalLineFeed);
			}
		}

		private void GetSumVSR2(IList<IRechnungskopfAZ> liste, string rnr, ref decimal betragStpfl, ref decimal betragUst, ref decimal betragStfra)
		{
			var vsr = liste.FirstOrDefault(x => x.Rechnungsnummer == rnr);
			if (vsr != null)
			{
				betragStpfl = vsr.ZahlungsbetragBrutto - vsr.ZahlungsbetragEnthalteneUSt - vsr.Rechnungsdetails[0].ZahlungsbetragSteuerfreieAuslagen;
				betragUst = vsr.ZahlungsbetragEnthalteneUSt;
				betragStfra = vsr.Rechnungsdetails[0].ZahlungsbetragSteuerfreieAuslagen;
				betragStpfl *= -1.00m;
				betragUst *= -1.00m;
				betragStfra *= -1.00m;
			}
		}

		private void GetSumVSR1(IList<IAuslagen> liste, int id, ref decimal betragStpfl, ref decimal betragUst, ref decimal betragStfra)
		{
			var vsr = liste.FirstOrDefault(x => x.ID == id);
			if (vsr != null)
			{
				betragStpfl = vsr.ZahlungsBetrag - vsr.EnthalteneMehrwertsteuerZahlung - vsr.ZahlungSteuerfreieAuslagen;
				betragUst = vsr.EnthalteneMehrwertsteuerZahlung;
				betragStfra = vsr.ZahlungSteuerfreieAuslagen;
				betragStpfl *= -1.00m;
				betragUst *= -1.00m;
				betragStfra *= -1.00m;
			}
		}

		private decimal GetSumGEGUT(IList<IAuslagen> liste, long id)
		{
			var ge = liste.FirstOrDefault(x => x.ID == id);
			if (ge != null)
			{
				return ge.Umsatz * -1.00m;
			}
			return 0.00m;
		}

		private void InsertExpenseRow(Table table, ContentRowInformation contentRowInformation, decimal expenseAmount, string expenseText, bool additionalLineFeed)
		{
			if (contentRowInformation.SpecialRowExpenses == null)
				return;

			var specialPlaceHolder = new Dictionary<string, string>();
			specialPlaceHolder.Add("M_RE_AUSLAGE_TEXT", expenseText);
			specialPlaceHolder.Add("M_RE_AUSLAGE_BETRAG", (additionalLineFeed ? Environment.NewLine : "") + ConvertToString(expenseAmount, true));

			var contentRowCopy = (TableRow)contentRowInformation.SpecialRowExpenses.Clone();
			ProcessPlaceHolders(contentRowCopy, null, null, specialPlaceHolder);
			table.Append(contentRowCopy);
		}

		private ContentRowInformation ExtractSpecialContentRows(List<TableRow> contentRows)
		{
			TableRow specialRowSubSum = null;
			TableRow specialRowTotalSum = null;
			TableRow specialRowExpenses = null;
			TableRow specialRowAdditionalText = null;
			TableRow specialRowManualSum = null;
			TableRow specialRowTopText = null;
			var specialRowGroupingHeader = new List<TableRow>();
			var specialRowGroupingFooter = new List<TableRow>();
			var groupingInformation = new List<string>();

			foreach (var contentRow in contentRows.ToList())
			{
				var placeHolders = PlaceHolderInfoService.GetPlaceHolders(contentRow);
				var placeHolderNames = placeHolders.Select(x => PlaceHolderInfoService.GetPlaceHolderName(x));

				if (placeHolderNames.Contains("M_RE_SUBSUM"))
				{
					specialRowSubSum = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("M_RE_TOTALSUM"))
				{
					specialRowTotalSum = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("M_RE_AUSLAGE_BETRAG"))
				{
					specialRowExpenses = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("M_RE_ZUSATZTEXTE"))
				{
					specialRowAdditionalText = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("GRP_EZ") || placeHolderNames.Contains("GRP_ABRECHB_EZ") || placeHolderNames.Contains("GRP_ABRECHB_D") || placeHolderNames.Contains("GRP_AKTE_EZ") || placeHolderNames.Contains("GRP_AKTE_D"))
				{
					groupingInformation.Add(placeHolderNames.First());
					if (placeHolderNames.Count() > 1)
						specialRowGroupingHeader.Add(contentRow);
					else
						specialRowGroupingHeader.Add(null);
					specialRowGroupingFooter.Add(null);
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("GRP_ABRECHB_ENDE"))
				{
					int index = groupingInformation.IndexOf("GRP_ABRECHB_EZ");
					if (index == -1)
						index = groupingInformation.IndexOf("GRP_ABRECHB_D");

					if (index == -1)
						throw new InvalidOperationException("Fehlerhafte Tabellenbeschreibung, zum Gruppenende \"GRP_ABRECHB_ENDE\" wurde kein Gruppenanfang definiert.");

					specialRowGroupingFooter[index] = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("GRP_AKTE_ENDE"))
				{
					int index = groupingInformation.IndexOf("GRP_AKTE_EZ");
					if (index == -1)
						index = groupingInformation.IndexOf("GRP_AKTE_D");

					if (index == -1)
						throw new InvalidOperationException("Fehlerhafte Tabellenbeschreibung, zum Gruppenende \"GRP_AKTE_ENDE\" wurde kein Gruppenanfang definiert.");

					specialRowGroupingFooter[index] = contentRow;
					contentRows.Remove(contentRow);
				}
				else if (placeHolderNames.Contains("M_LE_SUM_NET")
					|| placeHolderNames.Contains("M_LE_SUM_STD")
					|| placeHolderNames.Contains("M_LE_SUM_MIN")
					|| placeHolderNames.Contains("M_LE_SUM_STDMIN")
					|| placeHolderNames.Contains("M_LE_SUM_DAUER")
					|| placeHolderNames.Contains("M_LE_SUM_DAUERUNBEW"))
				{
					specialRowManualSum = contentRow;
				}
			}

			if (specialRowTotalSum == null)
				specialRowTotalSum = specialRowSubSum;

			if (specialRowSubSum == null)
				specialRowSubSum = specialRowTotalSum;

			//sofern nur die manuelle Summenzeile angegeben ist, diese als gesondert behandeln
			//	sonst annehmen, dass diese im Fließtext vorhanden ist
			if (specialRowTotalSum == null && specialRowSubSum == null && specialRowManualSum != null)
				contentRows.Remove(specialRowManualSum);
			else
				specialRowManualSum = null;

			var contentRowsHourlyRate = new List<TableRow>();
			var contentRowsFlatCharge = new List<TableRow>();
			if (contentRows != null)
			{
				foreach (var contentRow in contentRows)
				{
					var contentRowType = 0;
					foreach (var tableCell in contentRow.ChildElements.OfType<TableCell>())
					{
						var rowPlaceHolders = PlaceHolderInfoService.GetPlaceHolders(tableCell);
						if (rowPlaceHolders.Any(x => String.Compare(PlaceHolderInfoService.GetPlaceHolderName(x), "M_LE_ZT_S", true) == 0))
							contentRowType = 1;
						else if (rowPlaceHolders.Any(x => String.Compare(PlaceHolderInfoService.GetPlaceHolderName(x), "M_LE_ZT_P", true) == 0))
							contentRowType = 2;
					}

					if (contentRowType == 1)
						contentRowsHourlyRate.Add(contentRow);
					else
						contentRowsFlatCharge.Add(contentRow);
				}
			}

			return new ContentRowInformation(contentRowsHourlyRate, contentRowsFlatCharge, specialRowSubSum, specialRowTotalSum, specialRowExpenses, specialRowAdditionalText, groupingInformation, specialRowGroupingHeader, specialRowGroupingFooter, specialRowManualSum, specialRowTopText);
		}

		private void CleanEmptyRows(List<TableRow> contentRows)
		{
			foreach (var tableRow in contentRows.ToList())
			{
				if (tableRow.ChildElements.OfType<TableCell>().All(x => x.InnerText.Length == 0))
				{
					contentRows.Remove(tableRow);
					if (tableRow.Parent != null)
						tableRow.Remove();
				}
			}
		}

		/// <summary>
		/// Fügt eine Gesamtsummenzeile für alle Zeiteinträge ein.
		/// </summary>
		private void InsertTimeEntriesOneTotalRow(Table table, ContentRowInformation contentRowInformation)
		{
			InsertTimeEntryRow(table, contentRowInformation, this.TimeEntriesFiltered, null, "0");
		}

		/// <summary>
		/// Fügt gruppierte Zeiteinträge in das Dokument ein.
		/// </summary>
		private void InsertGroupedTimeEntries(Table table, ContentRowInformation contentRowInformation, int groupingDepth, bool oneRow, IEnumerable<IErfassteZeiten> groupedTimeEntries)
		{
			if (oneRow)
				InsertTimeEntryRow(table, contentRowInformation, groupedTimeEntries, groupedTimeEntries, null);
			else
				foreach (var timeEntry in GetOrderedTimeEntries(groupedTimeEntries))
					InsertTimeEntryRow(table, contentRowInformation, new IErfassteZeiten[] { timeEntry }, groupedTimeEntries, null);
		}

		private IEnumerable<IErfassteZeiten> GetOrderedTimeEntries(IEnumerable<IErfassteZeiten> timeEntries)
		{
			return timeEntries.OrderBy(x => x.Datum).ThenBy(x => x.Id);
		}

		/// <summary>
		/// Fügt einen Gruppenheader ein, sofern hierzu eine Zeilendefinitione vorliegt
		/// </summary>
		private void InsertGroupHeader(Table table, ContentRowInformation contentRowInformation, int groupingDepth, IEnumerable<IErfassteZeiten> groupedTimeEntries)
		{
			if (contentRowInformation.SpecialRowGroupingHeader[groupingDepth] == null)
				return;

			var contentRow = contentRowInformation.SpecialRowGroupingHeader[groupingDepth];
			var contentRowCopy = (TableRow)contentRow.Clone();
			ProcessPlaceHolders(contentRowCopy, groupedTimeEntries, groupedTimeEntries, null);
			if (!IstTBZeileSummeHonorare && KostenberechnungHost != null)
				SaveRowToList(contentRowCopy,true);
			table.Append(contentRowCopy);
		}

		/// <summary>
		/// Fügt einen Gruppenheader ein, sofern hierzu eine Zeilendefinitione vorliegt
		/// </summary>
		private void InsertGroupFooter(Table table, ContentRowInformation contentRowInformation, int groupingDepth, IEnumerable<IErfassteZeiten> groupedTimeEntries)
		{
			if (contentRowInformation.SpecialRowGroupingFooter[groupingDepth] == null)
				return;

			var contentRow = contentRowInformation.SpecialRowGroupingFooter[groupingDepth];
			var contentRowCopy = (TableRow)contentRow.Clone();
			ProcessPlaceHolders(contentRowCopy, groupedTimeEntries, groupedTimeEntries, null);
			table.Append(contentRowCopy);
		}

		/// <summary>
		/// Fügt für jeden Zeiteintrag eine Zeile in das Dokument ein.
		/// </summary>
		private void InsertTimeEntiresOneRowPerTimeEntry(Table table, ContentRowInformation contentRowInformation)
		{
			foreach (var timeEntry in GetOrderedTimeEntries(this.TimeEntriesFiltered))
				InsertTimeEntryRow(table, contentRowInformation, new IErfassteZeiten[] { timeEntry }, null, null);
		}

		private void InsertTimeEntryRow(Table table, ContentRowInformation contentRowInformation, IEnumerable<IErfassteZeiten> timeEntries, IEnumerable<IErfassteZeiten> timeEntriesFromGroup, string timeEntryTypeOverride)
		{
			var timeEntryType = timeEntries.First().Taetigkeit != null ? timeEntries.First().Taetigkeit.Art : "0";
			if (timeEntryTypeOverride != null)
				timeEntryType = timeEntryTypeOverride;

			List<TableRow> contentRows;
			if (timeEntryType == "1")
				contentRows = contentRowInformation.ContentRowsTypeFlatCharge;
			else
				contentRows = contentRowInformation.ContentRowsTypeHourlyRate;

			foreach (var contentRow in contentRows)
			{
				var contentRowCopy = (TableRow)contentRow.Clone();
				ProcessPlaceHolders(contentRowCopy, timeEntries, timeEntriesFromGroup, null);
				if (!IstTBZeileSummeHonorare && KostenberechnungHost != null)
					SaveRowToList(contentRowCopy);
				table.Append(contentRowCopy);
			}
		}

		private void SaveRowToList(TableRow contentRowCopy, bool istHeader = false)
		{
			if (m_Invoice.Urkunden[0].ItemsTimeEntries == null)
				m_Invoice.Urkunden[0].ItemsTimeEntries = new List<IInvoiceItem>();
			var NewPos = KostenberechnungHost.ErstellePosition();
			NewPos.Initialize(false, IstAnnotext);
			NewPos.IDFeeItem = 0;
			NewPos.DivisionType = ItemDivisionType.Keine;
			NewPos.TaxPercentage = UstSatz;
			if(istHeader)
				NewPos.CalculationMethod = "T";
			else
				NewPos.CalculationMethod = "B";
			NewPos.IsFeeCharging = false;
			NewPos.IsExpensesTaxfree = false;
			NewPos.Status = ItemStatus.added;
			NewPos.Identifier = StrKuerzel;
			NewPos.IDExpenses = (int)IdVR;
			NewPos.Table = Tabelle;
			NewPos.ExpenseCategoryCode = "T";
			NewPos.AmountAfterSplit = 0.00m;

			for (int i = 0; i < contentRowCopy.Descendants<TableCell>().Count();i++)
			{
				var cell = contentRowCopy.Descendants<TableCell>().ElementAt(i);
				if(i == 0)
					NewPos.FeeDescription = cell.InnerText;

				if (i == 1)
				{
					var strBetrag = cell.InnerText.Trim();
					if (!String.IsNullOrEmpty(strBetrag) && strBetrag[strBetrag.Length - 1] == '€')
					{
						strBetrag = strBetrag.Substring(0, strBetrag.Length - 2);
						NewPos.Amount = NewPos.AmountAfterSplit =  Convert.ToDecimal(strBetrag);
					}
				}
				//MessageBox.Show(cell.InnerText);
			}
			m_Invoice.Urkunden[0].ItemsTimeEntries.Add(NewPos);

		}

		private static decimal GetSplittingFactor(ItemDivisionType splittingType, string splittingFactor)
		{
			switch (splittingType)
			{
				case ItemDivisionType.Keine:
					return 1m;

				case ItemDivisionType.ProzentualAb:
				case ItemDivisionType.ProzentualAuf:
					return Convert.ToDecimal(splittingFactor) / 100m;

				case ItemDivisionType.QuotelungAb:
				case ItemDivisionType.QuotelungAuf:
					var parts = splittingFactor.Split('/');
					if (parts.Length == 2)
						return Convert.ToDecimal(parts[0]) / Convert.ToDecimal(parts[1]);
					throw new InvalidOperationException(String.Format("Die Teilung \"{0}\" ist keine gültige Angabe", splittingFactor));

				default:
					throw new NotImplementedException(splittingType.ToString());
			}
		}

		private string GetSplittingText(ItemDivisionType splittingType, string splittingFactor, decimal? totalSum)
		{
			string result;
			switch (splittingType)
			{
				case ItemDivisionType.ProzentualAb:
				case ItemDivisionType.ProzentualAuf:
					result = (GetSplittingFactor(splittingType, splittingFactor) * 100).ToString("F2", m_Culture) + "%";
					break;

				case ItemDivisionType.QuotelungAb:
				case ItemDivisionType.QuotelungAuf:
					result = splittingFactor;
					break;

				default:
					throw new NotImplementedException(splittingType.ToString());
			}
			if (totalSum.HasValue)
				return String.Format("{1} {0} von {3}{2}", result, m_TranslationService.GetText("SHAVA"), m_TranslationService.GetText("SHAIS"), ConvertToString(totalSum.Value, true));
			return String.Format("{1} {0}{2}", result, m_TranslationService.GetText("SHAVA"), m_TranslationService.GetText("SHAIS"));
		}

		private void ProcessPlaceHolders(TableRow tableRow, IEnumerable<IErfassteZeiten> timeEntries, IEnumerable<IErfassteZeiten> timeEntriesFromGroup, Dictionary<string, string> specialPlaceHolder)
		{
			Func<string, string> getPlaceHolderValue = (placeHolderName) =>
			{
				var value = GetValueForPlaceHolder(placeHolderName, timeEntries, timeEntriesFromGroup, specialPlaceHolder);
				return value;
			};
			new AnNoTextFieldExecuter().ReplacePlaceHolders(tableRow, getPlaceHolderValue);
		}

		private string GetValueForPlaceHolder(string placeHolderName, IEnumerable<IErfassteZeiten> timeEntries, IEnumerable<IErfassteZeiten> timeEntriesFromGroup, Dictionary<string, string> specialPlaceHolder)
		{
			if (String.Compare(placeHolderName, "M_RE_LZSTART", true) == 0)
				return this.ConvertToString(m_PerformancePeriodBegin);
			else if (String.Compare(placeHolderName, "M_RE_LZENDE", true) == 0)
				return this.ConvertToString(m_PerformancePeriodEnd);
			else if (String.Compare(placeHolderName, "M_RE_DAT", true) == 0)
				return this.ConvertToString(m_PerformanceInvoiceDate);
			else if (String.Compare(placeHolderName, "M_RE_RNR", true) == 0)
				return m_PerformanceInvoiceNumber;
			else if (String.Compare(placeHolderName, "M_LE_SUM_NET", true) == 0)
				return this.ConvertToString(GetSumTimeEntries(SumType.OnlyPositionSplitted), true);
			else if (String.Compare(placeHolderName, "M_LE_SUM_STD", true) == 0)
				return MinuteConverter.ToHour(GetGatedMinutes(this.TimeEntriesFiltered));
			else if (String.Compare(placeHolderName, "M_LE_SUM_MIN", true) == 0)
				return MinuteConverter.ToMinutes(GetGatedMinutes(this.TimeEntriesFiltered));
			else if (String.Compare(placeHolderName, "M_LE_SUM_STDMIN", true) == 0)
				return MinuteConverter.ToHourAndMinutes(GetGatedMinutes(this.TimeEntriesFiltered));
			else if (String.Compare(placeHolderName, "M_LE_SUM_DAUER", true) == 0)
				return GetDefaultMinutesForOutput(GetGatedMinutes(this.TimeEntriesFiltered));
			else if (String.Compare(placeHolderName, "M_LE_SUM_DAUERUNBEW", true) == 0)
				return GetDefaultMinutesForOutput(GetNettoGatedMinutes(this.TimeEntriesFiltered));
			else if (String.Compare(placeHolderName, "M_LE_GRP_NET", true) == 0)
				return this.ConvertToString(timeEntriesFromGroup.Sum(x => x.Gesamtbetrag), true);
			else if (String.Compare(placeHolderName, "M_LE_GRP_STD", true) == 0)
				return MinuteConverter.ToHour(GetGatedMinutes(timeEntriesFromGroup));
			else if (String.Compare(placeHolderName, "M_LE_GRP_MIN", true) == 0)
				return MinuteConverter.ToMinutes(GetGatedMinutes(timeEntriesFromGroup));
			else if (String.Compare(placeHolderName, "M_LE_GRP_STDMIN", true) == 0)
				return MinuteConverter.ToHourAndMinutes(GetGatedMinutes(timeEntriesFromGroup));
			else if (String.Compare(placeHolderName, "M_LE_GRP_DAUER", true) == 0)
				return GetDefaultMinutesForOutput(GetGatedMinutes(timeEntriesFromGroup));
			else if (String.Compare(placeHolderName, "M_LE_GRP_DAUERUNBEW", true) == 0)
				return GetDefaultMinutesForOutput(GetNettoGatedMinutes(timeEntriesFromGroup));
			else if (String.Compare(placeHolderName, "M_LE_AK_AZ", true) == 0)
				return timeEntries.First().Akte.AZ;
			else if (String.Compare(placeHolderName, "M_LE_AK_RUBMA", true) == 0)
				return RemoveInvalidXMLChars(timeEntries.First().Akte.Rubrum1Lang);
			else if (String.Compare(placeHolderName, "M_LE_AK_RUBGE", true) == 0)
				return RemoveInvalidXMLChars(timeEntries.First().Akte.Rubrum2Lang);
			else if (String.Compare(placeHolderName, "M_LE_AK_RUBRUM", true) == 0)
				return RemoveInvalidXMLChars(timeEntries.First().Akte.RubrumLangEineZeile);
			else if (String.Compare(placeHolderName, "M_LE_AK_WEGEN", true) == 0)
				return RemoveInvalidXMLChars(timeEntries.First().Akte.BetreffLang);
			else if (String.Compare(placeHolderName, "M_LE_ZE_LFDNR", true) == 0)
				return (GetOrderedTimeEntries(this.TimeEntriesFiltered).ToList().IndexOf(timeEntries.First()) + 1).ToString();
			else if (String.Compare(placeHolderName, "M_LE_ZE_STD_DEZIMAL", true) == 0)
				return MinuteConverter.ToHoursDecimal(GetGatedMinutes(timeEntries));
			else if (String.Compare(placeHolderName, "M_LE_ZE_STD", true) == 0)
				return MinuteConverter.ToHour(GetGatedMinutes(timeEntries));
			else if (String.Compare(placeHolderName, "M_LE_ZE_MIN", true) == 0)
				return MinuteConverter.ToMinutes(GetGatedMinutes(timeEntries));
			else if (String.Compare(placeHolderName, "M_LE_ZE_STDMIN", true) == 0)
				return MinuteConverter.ToHourAndMinutes(GetGatedMinutes(timeEntries));
			else if (String.Compare(placeHolderName, "M_LE_ZE_EINHEITEN", true) == 0)
				return MinuteConverter.ToUnits(GetGatedMinutes(timeEntries), (int)timeEntries.First().Taktung);
			else if (String.Compare(placeHolderName, "M_LE_ZE_DAUER", true) == 0)
				return GetDefaultMinutesForOutput(timeEntries.Sum(x => x.AnzahlMinutenBrutto));
			else if (String.Compare(placeHolderName, "M_LE_ZE_DAUERUNBEW", true) == 0)
				return GetDefaultMinutesForOutput(GetNettoGatedMinutes(timeEntries));
			else if (String.Compare(placeHolderName, "M_LE_ZE_STDSATZ", true) == 0)
				return ConvertToString(timeEntries.First().Stundensatz);
			else if (String.Compare(placeHolderName, "M_LE_ZE_DAT", true) == 0)
				return ConvertToString(timeEntries.First().Datum);
			else if (String.Compare(placeHolderName, "M_LE_ZE_UVON", true) == 0)
				return ConvertToTimeString(timeEntries.First().ZeitVon);
			else if (String.Compare(placeHolderName, "M_LE_ZE_UBIS", true) == 0)
				return ConvertToTimeString(timeEntries.First().ZeitBis);
			else if (placeHolderName.StartsWith("M_LE_ZE_ABRECHB_", StringComparison.CurrentCultureIgnoreCase))
				return GetValueForUserPlaceHolder(timeEntries.First().MitarbeiterAbrechnung, placeHolderName.Substring(16));
			else if (placeHolderName.StartsWith("M_LE_ZE_LEISTB_", StringComparison.CurrentCultureIgnoreCase))
				return GetValueForUserPlaceHolder(timeEntries.First().MitarbeiterOwner, placeHolderName.Substring(15));
			else if (placeHolderName.StartsWith("M_LE_ZE_UMSATZB_", StringComparison.CurrentCultureIgnoreCase))
				return GetValueForUserPlaceHolder(timeEntries.First().MitarbeiterUmsatz, placeHolderName.Substring(16));
			else if (String.Compare(placeHolderName, "M_LE_ZE_NET", true) == 0)
				return ConvertToString(timeEntries.Sum(x => x.Gesamtbetrag), true);
			else if (String.Compare(placeHolderName, "M_LE_ZE_BEWPROZ", true) == 0)
				return timeEntries.First().BewertungRechnung.ToString("n") + "%";
			else if (String.Compare(placeHolderName, "M_LE_ZE_TAT", true) == 0)
				return String.Join(Environment.NewLine, timeEntries.Select(x => RemoveInvalidXMLChars(x.Kurzbezeichnung)));
			else if (String.Compare(placeHolderName, "M_LE_ZE_NOTIZ", true) == 0)
				return String.Join(Environment.NewLine, timeEntries.Select(x => RemoveInvalidXMLChars(x.Kommentar)));
			else if (String.Compare(placeHolderName, "M_EINHEITEN", true) == 0)
				return InvoiceUnit;
			else if (String.Compare(placeHolderName, "M_LE_ZT_S", true) == 0
					|| String.Compare(placeHolderName, "M_LE_ZT_P", true) == 0)
				return "";
			else if (String.Compare(placeHolderName, "M_RE_ZEILE_TB", true) == 0)
				return m_LineTB;
			else if (specialPlaceHolder != null && specialPlaceHolder.ContainsKey(placeHolderName))
				return specialPlaceHolder[placeHolderName];
			else if (placeHolderName.StartsWith("GRP_"))
				return "";

			return String.Format("<unbekannt: {0}>", placeHolderName);
		}

		private long GetNettoGatedMinutes(IEnumerable<IErfassteZeiten> timeEntries)
		{
			return timeEntries.Sum(x => (long)TimeGatingCalculator.GetGatedTime(TimeSpan.FromSeconds(x.AnzahlMinutenNetto), TimeSpan.FromMinutes(x.Taktung)).TotalMinutes);
		}

		private string GetValueForUserPlaceHolder(IMitarbeiter mitarbeiter, string placeHolderName)
		{
			if (mitarbeiter == null)
				return "";

			if (String.Compare(placeHolderName, "TVNNAME", true) == 0)
				return RemoveInvalidXMLChars(mitarbeiter.VorNachname);
			else if (String.Compare(placeHolderName, "NTVNAME", true) == 0)
				return RemoveInvalidXMLChars(mitarbeiter.NachVorname);
			else if (String.Compare(placeHolderName, "TITEL", true) == 0)
				return mitarbeiter.Titel;
			else if (String.Compare(placeHolderName, "NNAME", true) == 0)
				return RemoveInvalidXMLChars(mitarbeiter.Nachname);
			else if (String.Compare(placeHolderName, "VNAME", true) == 0)
				return RemoveInvalidXMLChars(mitarbeiter.Vorname);
			else if (String.Compare(placeHolderName, "INI", true) == 0)
			{
				if (mitarbeiter.RANOT != null)
					return mitarbeiter.RANOT.Kennung;
				return mitarbeiter.Initialen;
			}

			return String.Format("<unbekannt: {0}>", placeHolderName);
		}

		private long GetGatedMinutes(IEnumerable<IErfassteZeiten> timeEntries)
		{
			return timeEntries.Sum(x => x.AnzahlMinutenBrutto);
		}

		private string GetDefaultMinutesForOutput(long minutes)
		{
			if (InvoiceUnitType == UnitType.Minutes)
				return MinuteConverter.ToMinutes(minutes);
			else if (InvoiceUnitType == UnitType.HoursDecimal)
				return MinuteConverter.ToHoursDecimal(minutes);

			return MinuteConverter.ToHour(minutes);
		}

		private string ConvertToString(DateTime? dateTime)
		{
			return dateTime.HasValue ? dateTime.Value.ToString("d", m_Culture) : "";
		}

		private string ConvertToTimeString(DateTime? dateTime)
		{
			return dateTime.HasValue ? dateTime.Value.ToString("HH:mm", m_Culture) : "";
		}

		private string ConvertToString(decimal decimalValue, bool negieren = false)
		{
			if (IstStornorechnungMoeglich && negieren)
				decimalValue *= -1.00m;
			string currency = m_Currency.Length > 1 ? " " + m_Currency + " " : m_Currency;
			return decimalValue.ToString("C", m_Culture).Replace(m_Culture.NumberFormat.CurrencySymbol, currency).Replace("  ", " ").Trim();
		}

		private static void SplitTableRows(Table table, out List<TableRow> topRows, out List<TableRow> headerRows, out List<TableRow> contentRows, out List<TableRow> footerRows, out List<PlaceHolderInfo> contentPlaceHolders, int[] breiten)
		{
			topRows = null;
			headerRows = new List<TableRow>();
			footerRows = null;
			contentRows = null;

			contentPlaceHolders = new List<PlaceHolderInfo>();

			foreach (var tableRow in table.ChildElements.OfType<TableRow>())
			{
				if (breiten != null)
					SetWidth(tableRow, breiten);
				var rowPlaceHolders = new List<PlaceHolderInfo>();
				foreach (var tableCell in tableRow.ChildElements.OfType<TableCell>())
					rowPlaceHolders.AddRange(PlaceHolderInfoService.GetPlaceHolders(tableCell));

				if (rowPlaceHolders.Any()
					&& (rowPlaceHolders.Count > 1 || (String.Compare(PlaceHolderInfoService.GetPlaceHolderName(rowPlaceHolders[0]), "M_EINHEITEN", true) != 0 && String.Compare(PlaceHolderInfoService.GetPlaceHolderName(rowPlaceHolders[0]), "M_RE_EINFUEHRUNGSTEXT", true) != 0)))
				{
					if (contentRows == null)
					{
						contentRows = new List<TableRow>();
						footerRows = new List<TableRow>();
					}
					contentRows.Add(tableRow);
					contentPlaceHolders.AddRange(rowPlaceHolders);
				}
				else
				{
					if (rowPlaceHolders.Any()
						&& (tableRow == table.ChildElements.OfType<TableRow>().First() && String.Compare(PlaceHolderInfoService.GetPlaceHolderName(rowPlaceHolders[0]), "M_RE_EINFUEHRUNGSTEXT", true) == 0))
					{
						topRows = new List<TableRow>();
						topRows.Add(tableRow);
					}
					else if (contentRows == null)
						headerRows.Add(tableRow);
					else
						footerRows.Add(tableRow);
				}
			}
		}

		/// <summary>
		/// Ruft die Summe der Auslagen ab.
		/// </summary>
		/// <param name="expenses">Die Auslagen, deren Summe abgerufen werden soll.</param>
		/// <param name="sumType">Die Art der Summe, die abgerufen werden soll.</param>
		/// <returns></returns>
		private decimal GetSumExpenses(IList<IInvoiceItem> expenses, SumType sumType)
		{
			switch (sumType)
			{
				case SumType.OnlyPositionSplitted:
					return expenses.Sum(x => x.DivisionType == ItemDivisionType.Keine ? x.Amount : x.AmountAfterSplit);

				case SumType.TotalSplitted:
					return expenses.Sum(x => x.AmountAfterSplit);

				default:
					throw new NotImplementedException(sumType.ToString());
			}
		}

		/// <summary>
		/// Ruft die Netto-Summer aller Zeiteinträge ab, wie Sie auf der Rechnung ausgegeben werden sollen.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumTimeEntries(SumType sumType)
		{
			switch (sumType)
			{
				case SumType.OnlyPositionSplitted:
					return this.TimeEntriesFiltered.Sum(x => x.Gesamtbetrag);

				case SumType.TotalSplitted:
					return GetSumTimeEntries(SumType.OnlyPositionSplitted) * InvoiceSplittingFactor;

				default:
					throw new NotImplementedException(sumType.ToString());
			}
		}

		/// <summary>
		/// Ruft die Summe aller steuerpflichtigen Auslagen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumExpensesWithTax(SumType sumType)
		{
			return GetSumExpenses(m_PositionsExpensesWithTax, sumType);
		}

		/// <summary>
		/// Ruft die Netto-Summe aller Zeiteinträge sowie aller steuerpflichten Auslagen ohne Steuer ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumNet(SumType sumType)
		{
			return GetSumTimeEntries(sumType) + GetSumExpensesWithTax(sumType);
		}

		/// <summary>
		/// Ruft die Steuer auf alle Zeiteinträge sowie aller steuerpflichten Auslagen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumTaxOnNet(SumType sumType)
		{
			var sumNet = GetSumNet(sumType);
			if (sumType == SumType.TotalSplitted && m_NettoSummeSplitting != 0.00m)
				sumNet = m_NettoSummeSplitting;

			return sumNet * (m_Invoice.Mehrwertsteuersatz / 100);
		}

		/// <summary>
		/// Ruft die Netto-Summe aller Zeiteinträge sowie aller steuerpflichten Auslagen inkl. Steuer ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumNetWithTax(SumType sumType)
		{
			var sumNetWithTax = GetSumNet(sumType);
			if (sumType == SumType.TotalSplitted && m_NettoSummeSplitting != 0.00m)
				sumNetWithTax = m_NettoSummeSplitting;

			return sumNetWithTax + GetSumTaxOnNet(sumType);
		}

		/// <summary>
		/// Ruft die Summe aller steuerfreien Auslagen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumExpensesWithoutTax(SumType sumType)
		{
			return GetSumExpenses(m_PositionsExpensesWithoutTax, sumType);
		}

		/// <summary>
		/// Ruft die Summe aller Zahlungen steuerfreien Auslagen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumPaymentsWithoutTax(SumType sumType)
		{
			return GetSumExpenses(m_PositionsPaymentsWithoutTax, sumType);
		}

		/// <summary>
		/// Ruft die Summe aller steuerfreien Auslagen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumForeignMoney(SumType sumType)
		{
			return GetSumExpenses(m_PositionsForeignMoney, sumType);
		}

		/// <summary>
		/// Ruft die Summe aller übernommenen Rechnungspositionen ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumExpensesAccounts(SumType sumType)
		{
			return GetSumExpenses(m_PositionsExpensesAccounts, sumType);
		}

		/// <summary>
		/// Ruft die Zwischensumme nach stfr Auslagen und Zahlungen.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumTotalWithoutAccounts(SumType sumType)
		{
			return GetSumNetWithTax(sumType) + GetSumExpensesWithoutTax(sumType) + GetSumPaymentsWithoutTax(sumType) + GetSumForeignMoney(sumType);
		}

		public decimal GetSumPayments(SumType sumType)
		{
			return GetSumExpenses(m_PositionsExpensesPayments, sumType);
		}

		/// <summary>
		/// Ruft die Gesamtsumme der Rechnung ab.
		/// </summary>
		/// <param name="sumType">Gibt an, ob bei einer geteilten Rechnung nur der Anteil geliefert werden soll, oder der volle Betrag.</param>
		public decimal GetSumTotal(SumType sumType)
		{
			var summeWithTax = GetSumNetWithTax(sumType);
			var summeExpensesWithoutTax = GetSumExpensesWithoutTax(sumType);
			var summePaymentsWithoutTax = GetSumPaymentsWithoutTax(sumType);
			var summeExpensesAccounts = GetSumExpensesAccounts(sumType);
			var summeForeignMoney = GetSumForeignMoney(sumType);
			var summePayments = GetSumPayments(sumType);
			if (m_Invoice.TeilungsArt == ItemDivisionType.Keine)
				return GetSumNetWithTax(sumType) + GetSumExpensesWithoutTax(sumType) + GetSumPaymentsWithoutTax(sumType) + GetSumExpensesAccounts(sumType) + GetSumForeignMoney(sumType) + GetSumPayments(sumType);
			else
			{
				if (sumType == SumType.OnlyPositionSplitted)
					return GetSumNetWithTax(sumType) + GetSumExpensesWithoutTax(sumType) + GetSumPaymentsWithoutTax(sumType) + GetSumForeignMoney(sumType) + +GetSumPayments(sumType);
				else
					return GetSumNetWithTax(sumType) + GetSumExpensesWithoutTax(sumType) + GetSumPaymentsWithoutTax(sumType) + GetSumExpensesAccounts(sumType) + GetSumForeignMoney(sumType) + +GetSumPayments(sumType);
			}
		}

		private static void SetWidth(TableRow row, int[] columnWidth)
		{
			int[] iWeitenNeu = new int[columnWidth.Length];
			if (columnWidth.Count() == 3)
			{
				iWeitenNeu[0] = columnWidth[0] + columnWidth[1];
				iWeitenNeu[1] = columnWidth[2];
			}
			else
			{
				iWeitenNeu = columnWidth;
			}
			var tableCells = row.ChildElements.OfType<TableCell>().ToList();
			if (tableCells.Count == 2)
			{
				int i = 0;
				tableCells.ForEach(x =>
				{
					x.GetFirstChild<TableCellProperties>().GetFirstChild<TableCellWidth>().Width = iWeitenNeu[i++].ToString();
				});
			}
		}

		/// <summary>
		/// removes any unusual unicode characters that can't be encoded into XML
		/// </summary>
		private static string RemoveInvalidXMLChars(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			return m_InvalidXMLChars.Replace(text, "");
		}

		private static List<AnteileSummeNetto> BerechneNettoSummenBeiQuotelung(decimal SummeGesamt, List<AnteileSummeNetto> listAnteileSummeNetto)
		{
			decimal SummeNettoAnteile = 0.00m, delta = 0.00m;
			int i;
			//var SummeNettoAnteile = 0.00m;
			listAnteileSummeNetto[0].GesamtsummeNetto = SummeGesamt;
			for (i = 0; i < listAnteileSummeNetto[0].Faktor.Count(); i++)
			{
				listAnteileSummeNetto[0].AnteilSummeNetto[i] = Gebuehrenberechnung.Runden(SummeGesamt * listAnteileSummeNetto[0].Faktor[i]);
				SummeNettoAnteile += listAnteileSummeNetto[0].AnteilSummeNetto[i];
			}
			i = listAnteileSummeNetto[0].Faktor.Count() - 1;
			delta = Math.Abs(SummeNettoAnteile) - Math.Abs(SummeGesamt);
			while (Math.Abs(delta) > 0.0099999m)
			{
				if (delta > 0.00m)
				{
					listAnteileSummeNetto[0].AnteilSummeNetto[i] -= 0.01m;
					delta -= 0.01m;
					i--;
					if (i == 0)
						i = listAnteileSummeNetto[0].Faktor.Count() - 1;
				}
				else
				{
					listAnteileSummeNetto[0].AnteilSummeNetto[i] += 0.01m;
					delta += 0.01m;
					i--;
					if (i == 0)
						i = listAnteileSummeNetto[0].Faktor.Count() - 1;
				}
			}
			return listAnteileSummeNetto;

		}

		private static void DetailsRechnungAnpassen(decimal anteilSummePositionen, decimal anteilSummeNetto, DetailsRechnung detailsRechnung)
		{
			decimal delta = Math.Abs(anteilSummePositionen) - Math.Abs(anteilSummeNetto);
			int i = 0;
			while (Math.Abs(delta) > 0.0099999m)
			{
				if (delta > 0.00m)
				{
					if (i == 3 && detailsRechnung.SonstigeStpflA > 0.00m)
					{
						detailsRechnung.SonstigeStpflA -= 0.01m;
						delta -= 0.01m;
					}
					if (i == 2 && detailsRechnung.Auskunftskosten > 0.00m)
					{
						detailsRechnung.Auskunftskosten -= 0.01m;
						delta -= 0.01m;
					}
					if (i == 1 && detailsRechnung.Reisekosten > 0.00m)
					{
						detailsRechnung.Reisekosten -= 0.01m;
						delta -= 0.01m;
					}
					if (i == 0 && detailsRechnung.Gebuehren > 0.00m)
					{
						detailsRechnung.Gebuehren -= 0.01m;
						delta -= 0.01m;
					}
					i++;
					if (i == 4)
						i = 0;
				}
				else
				{
					if (i == 3 && detailsRechnung.SonstigeStpflA > 0.00m)
					{
						detailsRechnung.SonstigeStpflA += 0.01m;
						delta += 0.01m;
					}
					if (i == 2 && detailsRechnung.Auskunftskosten > 0.00m)
					{
						detailsRechnung.Auskunftskosten += 0.01m;
						delta += 0.01m;
					}
					if (i == 1 && detailsRechnung.Reisekosten > 0.00m)
					{
						detailsRechnung.Reisekosten += 0.01m;
						delta += 0.01m;
					}
					if (i == 0 && detailsRechnung.Gebuehren > 0.00m)
					{
						detailsRechnung.Gebuehren += 0.01m;
						delta += 0.01m;
					}
					i++;
					if (i == 4)
						i = 0;
				}
				detailsRechnung.BerechneSummen(0.00m, 0.00m, 0.00m, 0.00m, 0.00m, 0.00m, true);
			}

		}
	}
}