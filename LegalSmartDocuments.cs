using AnNoText.Interface;
using AT.BL;
using AT.Core;
using AT.Core.Objects;
using AT.Core.Objects.Interfaces;
using AT.Core.Objects.Interfaces.Fisk;
using log4net;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Xml;
using WK.DE.ExceptionHandling;
using WK.DE.SmartDocuments.Contracts;
using WK.DE.SmartDocuments.UI;
using WK.DE.UI.WinForms.WindowsTaskDialog;

namespace AnNoText
{
	public class LegalSmartDocuments : ILegalSmartDocuments
	{
		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(LegalSmartDocuments));

		#region Variables

		public ObjectFactory m_ObjectFactory;
		private Func<ISmartDocsConnectionInfo, ITemplateListReader> m_TemplateListReaderCreator;
		private Func<ITemplateListReader, IDialogSmartDocsSelectTemplate> m_DialogSelectTemplateCreator;
		private Lazy<IDataTreeBuilder> m_DataBuilderCreator;
		private Func<ISmartDocsConnectionInfo, ISmartDocumentCreator> m_CreateDocumentCreator;
		private IAtDatabaseManager m_DatabaseManager;

		#endregion Variables

		#region Interop

		[DllImport("user32.dll")]
		public static extern IntPtr FindWindow(String sClassName, String sAppName);

		#endregion Interop

		public LegalSmartDocuments()
		{
			try
			{
				m_ObjectFactory = ObjectFactory.Init();
				m_TemplateListReaderCreator = new Func<ISmartDocsConnectionInfo, ITemplateListReader>((ISmartDocsConnectionInfo connection) => m_ObjectFactory.CreateTemplateListReader(connection));
				m_DialogSelectTemplateCreator = new Func<ITemplateListReader, IDialogSmartDocsSelectTemplate>((ITemplateListReader cmd) => m_ObjectFactory.CreateDialogSelectTemplate(cmd.GetTemplateList()));
				m_DataBuilderCreator = new Lazy<IDataTreeBuilder>(() =>
				{
					var dataBuilder = m_ObjectFactory.CreateDataTreeBuilder();
					ISmartDocsDataAdapter smartDocsDataAdapter1 = new DataAdapterMitarbeiter();
					dataBuilder.RegisterAdapter(typeof(Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>), smartDocsDataAdapter1);
					ISmartDocsDataAdapter smartDocsDataAdapter2 = new DataAdapterAkteBeteiligter();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAkteBeteiligter, IAnsprechpartner>), smartDocsDataAdapter2);
					ISmartDocsDataAdapter smartDocsDataAdapter3 = new DataAdapterAdditionalData();
					dataBuilder.RegisterAdapter(typeof(IEnumerable<ICaseAdditionalDataReportItem>), smartDocsDataAdapter3);
					ISmartDocsDataAdapter smartDocsDataAdapter4 = new DataAdapterOffice();
					dataBuilder.RegisterAdapter(typeof(IBuerogemeinschaft), smartDocsDataAdapter4);
					ISmartDocsDataAdapter smartDocsDataAdapter5 = new DataAdapterSubjectDictation();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAkte, IAdressat, XmlDocument>), smartDocsDataAdapter5);
					ISmartDocsDataAdapter smartDocsDataAdapter6 = new DataAdapterSubjectAccident();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAkte, IUnfallschadenDaten>), smartDocsDataAdapter6);
					ISmartDocsDataAdapter smartDocsDataAdapter7 = new DataAdapterRubrum();
					dataBuilder.RegisterAdapter(typeof(IEnumerable<IAkteBeteiligter>), smartDocsDataAdapter7);
					ISmartDocsDataAdapter smartDocsDataAdapter8 = new DataAdapterTextBody();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAdressat, bool, bool>), smartDocsDataAdapter8);
					ISmartDocsDataAdapter smartDocsDataAdapter9 = new DataAdapterCaseData();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAkte, INachlassFiskalate, INachlassZusatzangabenFiskalate>), smartDocsDataAdapter9);
					ISmartDocsDataAdapter smartDocsDataAdapter10 = new DataAdapterBKZData();
					dataBuilder.RegisterAdapter(typeof(Tuple<IForderungskontoBKZ, IAdressat, IForderungskonten>), smartDocsDataAdapter10);
					ISmartDocsDataAdapter smartDocsDataAdapter11 = new DataAdapterParticipantsCounter();
					dataBuilder.RegisterAdapter(typeof(String), smartDocsDataAdapter11);
					ISmartDocsDataAdapter smartDocsDataAdapter12 = new DataAdapterAddressee();
					dataBuilder.RegisterAdapter(typeof(Tuple<IAdressat, IAnsprechpartner, XmlDocument>), smartDocsDataAdapter12);
					ISmartDocsDataAdapter smartDocsDataAdapter13 = new DataAdapterXml();
					dataBuilder.RegisterAdapter(typeof(XmlDocument), smartDocsDataAdapter13);
					ISmartDocsDataAdapter smartDocsDataAdapter14 = new DataAdapterForeclosure();
					dataBuilder.RegisterAdapter(typeof(IEnumerable<IEnumerable<ReceivableAccountItem>>), smartDocsDataAdapter14);
					ISmartDocsDataAdapter smartDocsDataAdapter15 = new DataAdapterUvgTumb();
					dataBuilder.RegisterAdapter(typeof(Tuple<IEnumerable<IAkteBeteiligterUVG>, IEnumerable<IAkteBeteiligter>>), smartDocsDataAdapter15);

					return dataBuilder;
				});
				m_CreateDocumentCreator = new Func<ISmartDocsConnectionInfo, ISmartDocumentCreator>((ISmartDocsConnectionInfo connection) => m_ObjectFactory.CreateDocumentCreator(connection));
				m_DatabaseManager = new AtDatabaseManager();

				RegisterCallbackURL("wk");
			}
			catch (Exception exp)
			{
				ExceptionManager.Publish(null, exp, true, "Beim Initialisieren der Document Creator PLUS Schnittstelle ist ein Fehler aufgetreten.", "LegalVerbindung zu Document Creator PLUS");
			}
		}

		public void StartDocument(String officeName, ILegalSmartDocumentsData legalSmartDocumentsData)
		{
			m_Logger.Info("Start - StartDocument");

			IntPtr handle = FindWindow(null, "Dokumenterstellung");
			StartDocument(officeName, legalSmartDocumentsData, handle);

			m_Logger.Info("Stop - StartDocument");
		}

		public void StartDocument(String officeName, ILegalSmartDocumentsData legalSmartDocumentsData, IntPtr handle)
		{
			m_Logger.Info("Start - StartDocument");

			try
			{
				if (handle == null || handle == IntPtr.Zero)
					handle = FindWindow(null, "Dokumenterstellung");

				CoreFactory.InitializeFactory(false, officeName);
				m_DatabaseManager.Initialize(officeName);

				string documentCallback = Guid.NewGuid().ToString();
				int userId = ToolsLogInAndDatabase.GetLoggedInUserId();
				ISmartDocsConnectionInfo connection = m_DatabaseManager.LoadConnection(userId, string.Format("wk://{0}/{1}", documentCallback, officeName));
				m_Logger.Info("Starte Vorlagendialog");
				ISmartDocsTemplate template = CreateDialogSelectTemplate(connection, handle);
				if (template == null)
					return;

				XmlDocument xmlDocument = new XmlDocument();
				xmlDocument.Load(legalSmartDocumentsData.XMLData);
				IEnumerable<IEnumerable<ReceivableAccountItem>> receivableAccounts = GetForderungskonten(xmlDocument);

				m_DatabaseManager.SaveDocumentProerties(legalSmartDocumentsData.CaseId, documentCallback, userId);
				var caseData = m_DatabaseManager.GetAkte(legalSmartDocumentsData.CaseId);
				var participant = m_DatabaseManager.GetParticipant(legalSmartDocumentsData.ParticipantId);
				var caseContactPerson = m_DatabaseManager.GetContactPerson(legalSmartDocumentsData.ContactId);
				var caseContactPersons = m_DatabaseManager.GetAllCaseContactPersons(legalSmartDocumentsData.CaseId);
				var accidentDaten = m_DatabaseManager.GetAccidentDaten(legalSmartDocumentsData);
				var forderungskontosBKZ = m_DatabaseManager.GetForderungskontosBKZDaten(legalSmartDocumentsData);
				var nachlassZusatzangaben = m_DatabaseManager.GetNachlassZusatzangabenFiskalateDaten(legalSmartDocumentsData.CaseId);
				var nachlassFiskalate = m_DatabaseManager.GetNachlassFiskalateDaten(legalSmartDocumentsData.CaseId);
				var author = m_DatabaseManager.GetMitarbeiter(legalSmartDocumentsData.AuthorId);
				var author2 = m_DatabaseManager.GetMitarbeiter(legalSmartDocumentsData.AuthorId2);
				var assistent = m_DatabaseManager.GetMitarbeiter(legalSmartDocumentsData.SBId);
				var assistent2 = m_DatabaseManager.GetMitarbeiter(legalSmartDocumentsData.SBId2);
				var loginUser = m_DatabaseManager.GetMitarbeiter(xmlDocument);
				var uvgTumb = m_DatabaseManager.GetUvgTumb(legalSmartDocumentsData.CaseId);

				IDataTreeBuilder dataTreeBuilder = m_DataBuilderCreator.Value;
				var rootNode = dataTreeBuilder.CreateRootNode();
				var caseNode = rootNode.AddNewChild(Strings.File);

				CreateAdditionalDataNode(legalSmartDocumentsData.CaseId, dataTreeBuilder, caseNode);
				CreateCaseInfoDataNode(caseData, forderungskontosBKZ, nachlassFiskalate, nachlassZusatzangaben, dataTreeBuilder, caseNode);
				CreateParticipantDataNode(caseContactPersons, dataTreeBuilder, caseNode);
				CreateSubjectDictation(caseData, participant, xmlDocument, dataTreeBuilder, caseNode);
				CreateSubjectAccident(caseData, accidentDaten, dataTreeBuilder, caseNode);
				CreateRubrum(caseContactPersons, dataTreeBuilder, caseNode);
				CreateAdressWindow(participant, caseContactPerson, xmlDocument, dataTreeBuilder, caseNode);
				CreateOfficeNode(officeName, legalSmartDocumentsData, dataTreeBuilder, caseNode);
				CreateAuthorsAndAssistentNode(author, author2, assistent, assistent2, caseData, dataTreeBuilder, caseNode);
				CreateLogedInUserNode(loginUser, dataTreeBuilder, caseNode);
				CreateDocumentPropertiesNode(xmlDocument, dataTreeBuilder, caseNode);
				var bodyNode = CreateBody(participant, caseContactPersons, dataTreeBuilder, caseNode);
				CreateReceivableAccounts(receivableAccounts, dataTreeBuilder, caseNode);
				CreateUvgTumb(uvgTumb, caseContactPersons, dataTreeBuilder, bodyNode);

				// Let SmartDocuments create a new document
				var creator = m_CreateDocumentCreator(connection);
				creator.Data = rootNode;
				var ticketNo = creator.CreateDocument(template.ID);
				m_Logger.DebugFormat("got ticket nr {0} from SmartDocuments", ticketNo);

				if (!IsGuid(template.ID))
				{
					Process.Start(template.ID.Replace("XML:", string.Empty));
					return;
				}
			}
			catch (System.Net.WebException exp)
			{
				if (exp.Status == System.Net.WebExceptionStatus.ProtocolError
					&& exp.Message != null
					&& exp.Message.IndexOf("401", StringComparison.OrdinalIgnoreCase) > 0)
				{
					var message = "Der Zugriff auf die den Document Creator PLUS ist nicht möglich. Vermutlich ist der in der Administration eingetragene Integrationsbenutzer nicht korrekt. Bitte überprüfen Sie die Angaben in der Administration unter 'Externe Services'-'Document Creator PLUS'.";
					m_Logger.Info(message, exp);
					TaskDialog.Show(null, message, "Neues Dokument mit Document Creator PLUS erstellen", TaskDialogStandardButtons.OK, TaskDialogStandardIcon.Error);
				}
				else
				{
					ExceptionManager.Publish(null, exp, true, "Beim Erstellen eines Dokumentes mit dem Document Creator PLUS ist ein Fehler aufgetreten.", "Document Creator PLUS");
				}
			}
			catch (System.ServiceModel.FaultException exp)
			{
				if (String.Equals(exp.Message, "Only integration users can use the document creation service.", StringComparison.Ordinal))
				{
					string message = "Es existiert kein Integrationsbenutzer. Bitte beachten Sie, dass der Integrationsbenutzer in der verknüpften Legal SmartDocuments Instanz angelegt und auch über die AnNoText Administration unter 'Externe Services'-'Document Creator PLUS' entsprechend hinterlegt wurde.";
					m_Logger.Info(message, exp);
					TaskDialog.Show(null, message, "Neues Dokument mit Document Creator PLUS erstellen", TaskDialogStandardButtons.OK, TaskDialogStandardIcon.Error);
				}
				else if (String.Equals(exp.Message, "Username is not valid", StringComparison.Ordinal))
				{
					string message = "Der beim aktuellen AnNoText-Anwender hinterlegte Document Creator PLUS Logindaten scheinen nicht gültig zu sein.\r\nBitte überprüfen Sie die Angaben in der Administration unter 'Mitarbeiter'.";
					m_Logger.Info(message, exp);
					TaskDialog.Show(null, message, "Neues Dokument mit Document Creator PLUS erstellen", TaskDialogStandardButtons.OK, TaskDialogStandardIcon.Error);
				}
				else
				{
					ExceptionManager.Publish(null, exp, true, "Beim Erstellen eines Dokumentes mit Document Creator PLUS ist ein Fehler aufgetreten.", "Document Creator PLUS");
				}
			}
			catch (Exception exp)
			{
				ExceptionManager.Publish(null, exp, true, "Beim Erstellen eines Dokumentes mit Document Creator PLUS ist ein Fehler aufgetreten.", "Document Creator PLUS");
			}

			m_Logger.Info("Stop - StartDocument");
		}

		private IEnumerable<IEnumerable<ReceivableAccountItem>> GetForderungskonten(XmlDocument xmlDocument)
		{
			XmlNodeList xmlNodeList = xmlDocument.GetElementsByTagName("FKTO_COUNT");
			if (xmlNodeList == null)
				return null;

			XmlNode xmlNode = xmlNodeList.Item(0);
			if (xmlNode == null)
				return null;

			string fktoCount = xmlNode.InnerText;
			if (string.IsNullOrEmpty(fktoCount))
				return null;

			List<IEnumerable<ReceivableAccountItem>> receivableAccount = new List<IEnumerable<ReceivableAccountItem>>();
			int iFktoCount = Convert.ToInt32(fktoCount);
			for (int iLoop = 0; iLoop < iFktoCount; iLoop++)
			{
				string fktoPath = xmlDocument.GetElementsByTagName(string.Concat("FKTO_DATA_", iLoop + 1)).Item(0).InnerText;

				XmlDocument xmlDocumentFkto = new XmlDocument();
				xmlDocumentFkto.Load(fktoPath);

				var mergeData = xmlDocumentFkto.GetElementsByTagName("mergedata").Item(0);
				List<ReceivableAccountItem> tagValueList = new List<ReceivableAccountItem>();
				for (int iLoop2 = 0; iLoop2 < mergeData.ChildNodes.Count; iLoop2++)
				{
					ReceivableAccountItem receivableAccountItem = FillFkto(mergeData.ChildNodes[iLoop2]);
					if (receivableAccountItem != null)
						tagValueList.Add(receivableAccountItem);
				}

				receivableAccount.Add(tagValueList);
			}

			return receivableAccount;
		}

		private ReceivableAccountItem FillFkto(XmlNode node)
		{
			ReceivableAccountItem receivableAccountItem = null;
			if (node.NodeType == XmlNodeType.Element)
			{
				receivableAccountItem = TagMapper.GetTag(node.Name);
				if (receivableAccountItem != null && node.Name.ToLower() != "table")
					receivableAccountItem.Value = node.InnerText;
			}

			return receivableAccountItem;
		}

		private bool IsGuid(string id)
		{
			Guid result;
			return Guid.TryParse(id, out result);
		}

		private void CreateAdditionalDataNode(long caseId, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			var additionalData = m_DatabaseManager.GetAdditionalDataReportItem(caseId);
			if (additionalData != null && additionalData.Count() > 0)
				dataTreeBuilder.AddNode(Strings.AdditionalData, additionalData, caseNode);
		}

		private void CreateCaseInfoDataNode(IAkte caseData, IEnumerable<IForderungskontoBKZ> forderungskontosBKZ, INachlassFiskalate nachlassFiskalate, INachlassZusatzangabenFiskalate nachlassZusatzangaben, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			m_Logger.Info("Start - CreateCaseInfoDataNode");

			if (caseData != null)
			{
				ISmartDocsDataTreeNode smartDocsDataTreeNode = dataTreeBuilder.AddNode(Strings.FileInformation, new Tuple<IAkte, INachlassFiskalate, INachlassZusatzangabenFiskalate>(caseData, nachlassFiskalate, nachlassZusatzangaben), caseNode);

				if (forderungskontosBKZ != null)
				{
					m_Logger.Info("forderungskontosBKZ != null");

					foreach (IForderungskontoBKZ item in forderungskontosBKZ)
					{
						if (item != null)
						{
							IAdressat adressat = m_DatabaseManager.GetAdressatDaten(item.IdAdressat);
							IForderungskontenRelation forderungskontenRelation = m_DatabaseManager.GetForderungskontenRelationDaten(item.IdForderungskontoRelation);

							IForderungskonten forderungskonto = null;
							if (forderungskontenRelation != null)
								forderungskonto = m_DatabaseManager.GetForderungskontoDaten(forderungskontenRelation.IdForderungskonto);

							dataTreeBuilder.AddNode(Strings.BKZ, new Tuple<IForderungskontoBKZ, IAdressat, IForderungskonten>(item, adressat, forderungskonto), smartDocsDataTreeNode);
						}
					}
				}
			}
			m_Logger.Info("Stop - CreateCaseInfoDataNode");
		}

		private void CreateParticipantDataNode(IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			if (caseContactPersons == null)
				return;

			ISmartDocsDataTreeNode participansNode = GetOrCreateChildNode(caseNode, Strings.Participants);
			AddParticipans(Strings.Participant, caseContactPersons, dataTreeBuilder, participansNode);
		}

		private ISmartDocsDataTreeNode AddMainParticipans(string nodeName, string kennungBeteiligungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.KennungBeteiligungsart.Equals(kennungBeteiligungsart) && _.KennungBeziehungsart1.Equals("00"));
			return AddParticipans(nodeName, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddOthrParticipans(string nodeName, string kennungBeteiligungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.KennungBeteiligungsart.Equals(kennungBeteiligungsart) && !_.KennungBeziehungsart1.Equals("00"));
			return AddParticipans(nodeName, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddAllParticipans(string nodeName, string kennungBeteiligungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.KennungBeteiligungsart.Equals(kennungBeteiligungsart));
			return AddParticipans(nodeName, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddMainRelationTypeParticipans(string beziehungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.Beziehungsart != null && _.Beziehungsart.Bezeichnung.Equals(beziehungsart) && _.KennungBeziehungsart1.Equals("00"));
			return AddParticipans(beziehungsart, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddOthrRelationTypeParticipans(string beziehungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.Beziehungsart != null && _.Beziehungsart.Equals(beziehungsart) && !_.KennungBeziehungsart1.Equals("00"));
			return AddParticipans(beziehungsart, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddAllRelationTypeParticipans(string beziehungsart, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			IEnumerable<IAkteBeteiligter> akteBeteiligte = caseContactPersons.Where(_ => _.Beziehungsart != null && _.Beziehungsart.Equals(beziehungsart));
			return AddParticipans(beziehungsart, akteBeteiligte, dataTreeBuilder, parentNode);
		}

		private ISmartDocsDataTreeNode AddParticipans(string nodeName, IEnumerable<IAkteBeteiligter> akteBeteiligte, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode parentNode)
		{
			ISmartDocsDataTreeNode smartDocsDataTreeNode = null;

			try
			{
				if (akteBeteiligte != null)
				{
					foreach (IAkteBeteiligter itemAkteBeteiligter in akteBeteiligte)
					{
						var caseContactPerson = m_DatabaseManager.GetContactPersonFromAdressee(itemAkteBeteiligter.IdAdressat);
						smartDocsDataTreeNode = dataTreeBuilder.AddNode(nodeName, new Tuple<IAkteBeteiligter, IAnsprechpartner>(itemAkteBeteiligter, caseContactPerson), parentNode);
					}
				}
			}
			catch (Exception ex)
			{
				string message = "Beim schreiben des Beteiligten ist ein Fehler aufgetreten.";
				m_Logger.Error(message, ex);
				TaskDialog.Show(null, message, "Neues Dokument mit Document Creator PLUS erstellen", TaskDialogStandardButtons.OK, TaskDialogStandardIcon.Error);
			}


			return smartDocsDataTreeNode;
		}

		private void CreateAdressWindow(IAdressat participant, IAnsprechpartner caseContactPerson, XmlDocument xmlDataDocument, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			if (caseContactPerson != null)
				dataTreeBuilder.AddNode(Strings.AddressField, new Tuple<IAdressat, IAnsprechpartner, XmlDocument>(participant, caseContactPerson, xmlDataDocument), GetHeaderNode(root));
		}

		private object CreateOfficeNode(string officeName, ILegalSmartDocumentsData legalSmartDocumentsData, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			IBuerogemeinschaft office = m_DatabaseManager.GetOffice(officeName);
			if (office != null)
				return dataTreeBuilder.AddNode(Strings.LawFirmData, office, GetHeaderNode(root));

			return null;
		}

		private void CreateAuthorsAndAssistentNode(IMitarbeiter author1, IMitarbeiter author2, IMitarbeiter assistent1, IMitarbeiter assistent2, IAkte caseData, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			if (caseData != null)
			{
				if (author1 != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(author1.Standort);
					var authorReNo = m_DatabaseManager.GetReNo(author1.Kurzname);
					dataTreeBuilder.AddNode(Strings.AuthorData1, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(author1, authorReNo, standort), GetHeaderNode(caseNode));
				}
				else if (caseData.HauptAnwalt != null && caseData.HauptAnwalt.Mitarbeiter != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(caseData.HauptAnwalt.Mitarbeiter.Standort);
					var authorReNo = m_DatabaseManager.GetReNo(caseData.HauptAnwalt.Mitarbeiter.Kurzname);
					dataTreeBuilder.AddNode(Strings.AuthorData1, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(caseData.HauptAnwalt.Mitarbeiter, authorReNo, standort), GetHeaderNode(caseNode));
				}

				if (author2 != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(author2.Standort);
					var authorReNo = m_DatabaseManager.GetReNo(author2.Kurzname);
					dataTreeBuilder.AddNode(Strings.AuthorData2, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(author2, authorReNo, standort), GetHeaderNode(caseNode));
				}
				else if (caseData.Anwalt2 != null && caseData.Anwalt2.Mitarbeiter != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(caseData.Anwalt2.Mitarbeiter.Standort);
					var authorReNo = m_DatabaseManager.GetReNo(caseData.Anwalt2.Mitarbeiter.Kurzname);
					dataTreeBuilder.AddNode(Strings.AuthorData2, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(caseData.Anwalt2.Mitarbeiter, authorReNo, standort), GetHeaderNode(caseNode));
				}

				if (assistent1 != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(assistent1.Standort);
					dataTreeBuilder.AddNode(Strings.AssistentData1, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(assistent1, null, standort), GetHeaderNode(caseNode));
				}
				else if (caseData.Sachbearbeiter != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(caseData.Sachbearbeiter.Standort);
					dataTreeBuilder.AddNode(Strings.AssistentData1, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(caseData.Sachbearbeiter, null, standort), GetHeaderNode(caseNode));
				}

				if (assistent2 != null)
				{
					IParameterStandorte standort = m_DatabaseManager.GetStandort(assistent2.Standort);
					dataTreeBuilder.AddNode(Strings.AssistentData2, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(assistent2, null, standort), GetHeaderNode(caseNode));
				}
			}
		}

		private void CreateLogedInUserNode(IMitarbeiter loginUser, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			if (loginUser != null)
			{
				IParameterStandorte standort = m_DatabaseManager.GetStandort(loginUser.Standort);
				var authorReNo = m_DatabaseManager.GetReNo(loginUser.Kurzname);
				dataTreeBuilder.AddNode(Strings.LogedInUser, new Tuple<IMitarbeiter, IParameterRechtsanwaelteNotare, IParameterStandorte>(loginUser, authorReNo, standort), GetHeaderNode(caseNode));
			}
		}

		private void CreateDocumentPropertiesNode(XmlDocument xmlDocument, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			if (xmlDocument != null)
			{
				dataTreeBuilder.AddNode(Strings.DocumentProperties, xmlDocument, GetHeaderNode(caseNode));
			}
		}

		private void CreateSubjectDictation(IAkte caseData, IAdressat participant, XmlDocument xmlDocument, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			if (caseData != null)
				dataTreeBuilder.AddNode(Strings.SubjectDictation, new Tuple<IAkte, IAdressat, XmlDocument>(caseData, participant, xmlDocument), GetBetreffNode(root));
		}

		private void CreateSubjectAccident(IAkte caseData, IUnfallschadenDaten accidentDaten, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			if (caseData != null)
				dataTreeBuilder.AddNode(Strings.SubjectDamage, new Tuple<IAkte, IUnfallschadenDaten>(caseData, accidentDaten), GetBetreffNode(root));
		}

		private void CreateRubrum(IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			if (caseContactPersons != null)
				dataTreeBuilder.AddNode(Strings.Rubrum, caseContactPersons, GetRubrumNode(root));
		}

		private ISmartDocsDataTreeNode CreateBody(IAdressat caseContactPerson, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode root)
		{
			bool mandantPlural = false;
			bool gegnerPlural = false;
			ISmartDocsDataTreeNode body = null;

			if (caseContactPersons != null)
			{
				mandantPlural = caseContactPersons.Where(_ => _.KennungBeteiligungsart.Equals("10") && _.KennungBeziehungsart1.Equals("00")).Count() > 1;
				gegnerPlural = caseContactPersons.Where(_ => _.KennungBeteiligungsart.Equals("20") && _.KennungBeziehungsart1.Equals("00")).Count() > 1;
			}

			if (caseContactPerson != null)
				body = dataTreeBuilder.AddNode(Strings.TextBody, new Tuple<IAdressat, bool, bool>(caseContactPerson, mandantPlural, gegnerPlural), root);

			return body;
		}

		private void CreateReceivableAccounts(IEnumerable<IEnumerable<ReceivableAccountItem>> receivableAccounts, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode caseNode)
		{
			if (receivableAccounts != null)
				dataTreeBuilder.AddNode(Strings.ReceivableAccounts, receivableAccounts, caseNode);
		}

		private void CreateUvgTumb(IEnumerable<IAkteBeteiligterUVG> participantsUvg, IEnumerable<IAkteBeteiligter> caseContactPersons, IDataTreeBuilder dataTreeBuilder, ISmartDocsDataTreeNode rootNode)
		{
			if (participantsUvg != null)
				dataTreeBuilder.AddNode(Strings.UvgTumb, new Tuple<IEnumerable<IAkteBeteiligterUVG>, IEnumerable<IAkteBeteiligter>>( participantsUvg, caseContactPersons), rootNode);
		}

		private ISmartDocsTemplate CreateDialogSelectTemplate(ISmartDocsConnectionInfo connection, IntPtr handle)
		{
			var cmd = m_TemplateListReaderCreator(connection);
			m_ObjectFactory.SetDialogSkin();
			var test = new SkinResourceDictionary();
			var form = m_DialogSelectTemplateCreator(cmd);
			new WindowInteropHelper(form.Form).Owner = handle;
			if (form.ShowDialog() == true)
			{
				return form.SelectedTemplate;
			}
			return null;
		}

		public void RegisterCallbackURL(string schemeName)
		{
			m_Logger.Debug("Start Register");
			using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\" + schemeName))
			{
				string path = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				string applicationLocation = Path.Combine(path, "AnNoText.ImportSmartDocuments.exe");
				key.SetValue("", "URL: Sample LSD " + schemeName);
				key.SetValue("URL Protocol", "");

				using (var commandKey = key.CreateSubKey(@"shell\open\command"))
				{
					commandKey.SetValue("", "\"" + applicationLocation + "\" \"%1\"");
				}
			}
		}

		public static void UnRegisterCallbackURL(string schemeName)
		{
			if (!FindSchemeRegistration(schemeName))
				throw new Exception(string.Format("Custom scheme {0} not registered.", schemeName));

			using (var key = Registry.CurrentUser.CreateSubKey("SOFTWARE\\Classes\\"))
			{
				key.DeleteSubKeyTree(schemeName);
			}
		}

		private static bool FindSchemeRegistration(string schemeName)
		{
			return Registry.CurrentUser.OpenSubKey("SOFTWARE\\Classes\\" + schemeName) != null;
		}

		public ISmartDocsDataTreeNode GetHeaderNode(ISmartDocsDataTreeNode target)
		{
			return GetOrCreateChildNode(target, Strings.Header);
		}

		private ISmartDocsDataTreeNode GetBetreffNode(ISmartDocsDataTreeNode target)
		{
			return GetOrCreateChildNode(target, Strings.Subject);
		}

		private ISmartDocsDataTreeNode GetRubrumNode(ISmartDocsDataTreeNode target)
		{
			ISmartDocsDataTreeNode rubrum = GetOrCreateChildNode(target, Strings.Rubrum);
			if (rubrum == null)
				return null;
			return GetOrCreateChildNode(rubrum, Strings.ClaimRubrum);
		}

		private ISmartDocsDataTreeNode GetOrCreateChildNode(ISmartDocsDataTreeNode target, string childName)
		{
			ISmartDocsDataTreeNode smartDocsDataTreeNode = null;
			if (!target.TryGetChild(childName, out smartDocsDataTreeNode))
				smartDocsDataTreeNode = target.AddNewChild(childName);
			return smartDocsDataTreeNode;
		}
	}
}