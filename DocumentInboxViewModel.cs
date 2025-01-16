using DryIoc;
using log4net;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WK.DE.DocumentManagement.Contracts;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.DocumentManagement.Contracts.ViewModels;
using WK.DE.DocumentManagement.Helper;
using WK.DE.DocumentManagement.Services;
using WK.DE.DocumentManagement.ViewModels.DocumentInbox;
using WK.DE.DocumentManagement.ViewModels.Permission;
using WK.DE.DocumentManagement.ViewModels.SelectionHelper;
using WK.DE.DocumentManagement.ViewModels.ToDos;
using WK.DE.UI.Preview.ViewModel;

namespace WK.DE.DocumentManagement.ViewModels
{
	public partial class DocumentInboxViewModel : BindableBase, IDocumentInboxViewModel
	{
		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(DocumentInboxViewModel));

		private readonly IContainer m_IocContainer;

		internal IContainer IocContainer
		{
			get { return m_IocContainer; }
		}

		protected IDocumentsInboxService m_DocumentsInboxService;

		internal IDocumentsInboxService DocumentsInboxService
		{
			get { return m_DocumentsInboxService; }
		}

		protected IDocumentService m_DocumentService;

		internal IDocumentService DocumentService
		{
			get { return m_DocumentService; }
		}

		private string m_ErrorMessageWhileLoadingDocuments;

		/// <summary>
		/// Ruft einen ggf. beim Laden der Dokumente aufgetretenen Fehler ab.
		/// </summary>
		public string ErrorMessageWhileLoadingDocuments
		{
			get { return m_ErrorMessageWhileLoadingDocuments; }
			private set
			{
				if (SetProperty(ref m_ErrorMessageWhileLoadingDocuments, value))
				{
					RaisePropertyChanged(nameof(ShowErrorMessageWhileLoadingDocuments));
				}
			}
		}

		public bool ShowErrorMessageWhileLoadingDocuments
		{
			get { return !String.IsNullOrWhiteSpace(m_ErrorMessageWhileLoadingDocuments); }
		}

		private readonly IStandardDialogService m_StandardDialogService;

		public CreateToDosViewModel CreateToDosViewModel
		{ get; private set; }

		private bool m_IsLoadingDataInitially;

		/// <summary>
		/// Gets if the data is loading the first time for this view (then different wait texts are shown).
		/// </summary>
		public bool IsLoadingDataInitially
		{
			get { return m_IsLoadingDataInitially; }
			private set
			{
				if (SetProperty(ref m_IsLoadingDataInitially, value))
				{
					RaisePropertyChanged(nameof(IsListEmpty));
				}
			}
		}

		public bool IsListEmpty
		{
			get { return !m_IsLoadingDataInitially && (m_InboxEntries == null || !m_InboxEntries.Any()); }
		}

		private bool m_IsPreviewAndDetailsSectionShownInSeparateWindow;

		public bool IsPreviewAndDetailsSectionShownInSeparateWindow
		{
			get { return m_IsPreviewAndDetailsSectionShownInSeparateWindow; }
			set
			{
				if (SetProperty(ref m_IsPreviewAndDetailsSectionShownInSeparateWindow, value))
				{
					RaisePropertyChanged(nameof(IsPreviewAndDetailsSectionNotShownInSeparateWindow));
				}
			}
		}

		public bool IsPreviewAndDetailsSectionNotShownInSeparateWindow
		{
			get { return !m_IsPreviewAndDetailsSectionShownInSeparateWindow; }
		}

		public EditEntityPermissionViewModel DocumentPermissions
		{ get; private set; }

		/// <summary>
		/// Initializes a new instance of DocumentInboxViewModel.
		/// </summary>
		/// <remarks>For designer support only!</remarks>
		public DocumentInboxViewModel()
			: this(null, null)
		{
		}

		public DocumentInboxViewModel(IContainer iocContainer, IStandardDialogService standardDialogService)
		{
			m_IocContainer = iocContainer;
			if (m_IocContainer != null)
			{
				m_DocumentsInboxService = m_IocContainer.Resolve<IDocumentsInboxService>();
				m_DocumentService = m_IocContainer.Resolve<IDocumentService>();

				this.DocumentPermissions = new EditEntityPermissionViewModel(m_IocContainer, m_StandardDialogService);
				this.DocumentPermissions.EditForNewDocument = true;
			}
			m_StandardDialogService = standardDialogService;
			m_DocumentPreviewViewModel = new DocumentPreviewViewModel();
			m_DocumentPreviewViewModel.IsOpenInNewWindowPossible = false;
			m_DocumentPreviewViewModel.IsSwitchOriginalToWorkingCopyPossible = false;

			this.CreateToDosViewModel = new CreateToDosViewModel(m_IocContainer, m_StandardDialogService, true, true, 2, true, true);

			InitializeCommands();
			RefreshSelectableValues();

			m_CaseAndAddresseeSelectionViewModel = new CaseAndAddresseeSelectionViewModel(iocContainer, m_StandardDialogService);
			m_CaseAndAddresseeSelectionViewModel.SelectFolderOnConfigureDocumentFolderStructure = true;
			m_CaseAndAddresseeSelectionViewModel.RefreshCachedData();
			m_CaseAndAddresseeSelectionViewModel.PropertyChanged += CaseAndAddresseeSelectionViewModel_PropertyChanged;

			this.RefreshCanExecuteForCommands();
		}

		private void DocumentPreviewViewModel_StampAddedToDocument(UI.WinForms.Controls.ViewModel.StampAddedToDocumentArguments args)
		{
			try
			{
				var currentDocument = m_DocumentPreviewViewModel.CurrentDocument;
				if (currentDocument == null)
				{
					throw new InvalidOperationException("no document visible in preview, not stamp could be added");
				}

				var documentStream = currentDocument.GetResolvedStream();

				//save document to disc, memorystream could not be stamped
				var tempFileName = FileNameService.GetTempFilePath(".pdf");
				using (var fileToStamp = File.Open(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
				{
					fileToStamp.SetLength(0);
					documentStream.Position = 0;
					documentStream.CopyTo(fileToStamp);
				}

				currentDocument.DiposeStreamIfIndicated();
				currentDocument.SetStream(null);

				var stampImageFilePath = args.StampImageFilePath;
				var pdfUtilityService = m_IocContainer.Resolve<IPdfUtilityService>();
				pdfUtilityService.AddStampToPDF(tempFileName, stampImageFilePath, 3, args.PageNumber, args.PageNumber, args.RectangleLeft, args.RectangleTop, args.RectangleRight, args.RectangleBottom, args.Rotation, true, null);

				var documentId = Convert.ToInt64(currentDocument.UniqueIdentifier);
				using (var stampedDocumentContent = File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					m_DocumentsInboxService.UpdateDocumentContent(documentId, stampedDocumentContent);
				}

				m_DocumentPreviewViewModel.CurrentDocument = currentDocument;
				// TODODMS tempFileName und stampImageFilePath löschen (%LOCALAPPDATA%\Temp) richtig?
				File.Delete(tempFileName);
				File.Delete(stampImageFilePath);
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Einfügen eines Stempels ist ein Fehler aufgetreten.", "Stempel einfügen");
			}
		}

		private void CaseAndAddresseeSelectionViewModel_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (String.Equals(e.PropertyName, nameof(CaseAndAddresseeSelectionViewModel.CasesToLink), StringComparison.OrdinalIgnoreCase)
				|| String.Equals(e.PropertyName, nameof(CaseAndAddresseeSelectionViewModel.FirstCaseToLink), StringComparison.OrdinalIgnoreCase))
			{
				if (this.CaseAndAddresseeSelectionViewModel.CasesToLink.Any())
				{
					if (ActiveInboxEntry == null)
					{
						return;
					}

					if (ActiveInboxEntry?.DocumentOwnerUserId == null)
					{
						var caseResponsibilities = m_DocumentService.GetCaseResponsibilities(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Id);
						if (caseResponsibilities != null)
						{
							ActiveInboxEntry.DocumentOwnerUserId = caseResponsibilities.LawyerUserId;
						}
					}
					else
					{
						if (String.Equals(e.PropertyName, nameof(CaseAndAddresseeSelectionViewModel.FirstCaseToLink), StringComparison.OrdinalIgnoreCase))
						{
							var caseResponsibilities = m_DocumentService.GetCaseResponsibilities(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Id);
							if (caseResponsibilities != null)
							{
								ActiveInboxEntry.DocumentOwnerUserId = caseResponsibilities.LawyerUserId;
								this.CreateToDosViewModel.UpdateExistingToDosToCreate(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case);
							}
						}
					}
					if (!string.IsNullOrEmpty(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case.CourtInstance.CourtCaseNumberFirstInstance)
						|| !string.IsNullOrEmpty(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case.CourtInstance.CourtCaseNumberSecondInstance)
						|| !string.IsNullOrEmpty(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case.CourtInstance.CourtCaseNumberThirdInstance))
					{
						ActiveInboxEntry.DocumentCorrespondenceType = DocumentCorrespondenceType.Judicially;
					}

					string docOwnerToPreconfigre = null;
					var toDoService = m_IocContainer.Resolve<IToDoService>();
					if (toDoService != null && toDoService.Parameter.UseDocOwnerAsToDoOwner)
					{
						docOwnerToPreconfigre = ActiveInboxEntry?.DocumentOwnerUserId;
						this.CreateToDosViewModel.UserIdToPreconfigure = docOwnerToPreconfigre;
					}
					
					this.CreateToDosViewModel.CaseIdToPreconfigure = this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Id;

					if (toDoService != null && toDoService.Parameter.NewPostboxScanToCaseOwnerType == PreconfiguredToDoOwnerType.DocumentType)
					{
						var userId = toDoService.GetToDoUserIdForDocType(Path.GetExtension(ActiveInboxEntry.DocumentOriginalFileName), null);
						if (!string.IsNullOrEmpty(userId))
						{
							this.CreateToDosViewModel.PreconfigureByCase(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case, ActiveInboxEntry?.DocumentDisplayName, userId);
						}
						else
						{
							this.CreateToDosViewModel.PreconfigureByCase(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case, ActiveInboxEntry?.DocumentDisplayName, docOwnerToPreconfigre);
						}
					}
					else
					{
						this.CreateToDosViewModel.PreconfigureByCase(this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.Case, ActiveInboxEntry?.DocumentDisplayName, docOwnerToPreconfigre);
					}




					this.DocumentPermissions.SetEntityIdsAndCase(null, m_CaseAndAddresseeSelectionViewModel.FirstCaseToLink?.Case);
				}
			}
			else if (String.Equals(e.PropertyName, nameof(m_CaseAndAddresseeSelectionViewModel.Folders)))
			{
				if (this.ActiveInboxEntry != null)
				{
					var folderId = this.ActiveInboxEntry.DocumentFolderId;
					if (!m_CaseAndAddresseeSelectionViewModel.Folders.Any(x => x.Id == folderId))
					{
						this.ActiveInboxEntry.DocumentFolderId = 0;
					}
				}
				//fire event in action, because is must be fired after CaseFolders was fully handled by UI
				TaskHelper.FireAndForget(() => this.RaisePropertyChanged(nameof(this.ActiveInboxEntry)));
			}
			else if (String.Equals(e.PropertyName, nameof(m_CaseAndAddresseeSelectionViewModel.SelectFolderId)))
			{
				if (ActiveInboxEntry != null)
				{
					ActiveInboxEntry.DocumentFolderId = m_CaseAndAddresseeSelectionViewModel.SelectFolderId;
				}
			}
		}

		#region TestRelayCommand

		private ICommand m_TestCommand;

		public ICommand TestCommand
		{
			get
			{
				if (m_TestCommand == null)
				{
					m_TestCommand = new DelegateCommand(ExecuteTestCommand, CanExecuteTestCommand);
				}
				return m_TestCommand;
			}
		}

		private void ExecuteTestCommand()
		{
			MessageBox.Show("From TestCommand");
		}

		private bool CanExecuteTestCommand()
		{
			return true;
		}

		#endregion TestRelayCommand

		private DocumentPreviewViewModel m_DocumentPreviewViewModel;

		public DocumentPreviewViewModel DocumentPreviewViewModel
		{
			get { return m_DocumentPreviewViewModel; }
			private set { SetProperty(ref m_DocumentPreviewViewModel, value); }
		}

		private string m_InboxAlias;

		public string InboxAlias
		{
			get { return m_InboxAlias; }
			set
			{
				if (SetProperty(ref m_InboxAlias, value))
				{
					this.SaveChangesIfNeeded();
					this.RefreshDocumentsAsync();
					this.MoveToPostboxDocumentInboxCommand.InboxAlias = m_InboxAlias;
				}
			}
		}

		private string m_InboxFriendlyAlias;

		public string InboxFriendlyAlias
		{
			get { return String.IsNullOrWhiteSpace(m_InboxFriendlyAlias) ? this.InboxAlias : m_InboxFriendlyAlias; }
			set { SetProperty(ref m_InboxFriendlyAlias, value); }
		}

		private TodoInboxType m_InboxType;

		public TodoInboxType InboxType
		{
			get { return m_InboxType; }
			set
			{
				if (SetProperty(ref m_InboxType, value))
				{
					if (TodoInboxType.Bea.Equals(m_InboxType) || TodoInboxType.Bebpo.Equals(m_InboxType))
					{
						ViewMode = DocumentInboxViewMode.Erv;
						if (DocumentPreviewViewModel != null && DocumentPreviewViewModel.StampCreationFunction != null)
						{
							DocumentPreviewViewModel.StampCreationFunction = null;
							m_DocumentPreviewViewModel.StampAddedToDocument -= DocumentPreviewViewModel_StampAddedToDocument;
						}
						DocumentPreviewViewModel.IsDeletePagesPossibleByContext = false;
						DocumentPreviewViewModel.IsRotateDocumentPossibleByContext = false;
						DocumentPreviewViewModel.DocumentRotated -= DocumentPreviewViewModel_DocumentRotated;
					}
					else
					{
						ViewMode = DocumentInboxViewMode.ScannerInbox;
						if (m_IocContainer != null)
						{
							m_DocumentPreviewViewModel.StampCreationFunction = () =>
							{
								var stampTitle = m_DocumentService.Parameter.StampTitle;

								var stampCreationService = m_IocContainer.Resolve<IStampCreationService>();
								stampCreationService.OfficeName = stampTitle;
								using (var stampImage = stampCreationService.RenderIncomingPost(DateTime.Today))
								{
									var stampImageFilePath = FileNameService.GetTempFilePath(".png");
									stampImage.Save(stampImageFilePath, ImageFormat.Png);
									return stampImageFilePath;
								}
							};
							m_DocumentPreviewViewModel.IsStampDocumentPossible = true;
						}
						m_DocumentPreviewViewModel.StampAddedToDocument += DocumentPreviewViewModel_StampAddedToDocument;
						DocumentPreviewViewModel.IsDeletePagesPossibleByContext = true;
						DocumentPreviewViewModel.IsRotateDocumentPossibleByContext = true;
						DocumentPreviewViewModel.DocumentRotated += DocumentPreviewViewModel_DocumentRotated;
					}
					this.SaveChangesIfNeeded();
					this.RefreshDocumentsAsync();
				}
			}
		}

		private void DocumentPreviewViewModel_DocumentRotated()
		{
			try
			{
				var currentDocument = DocumentPreviewViewModel.CurrentDocument;
				if (currentDocument == null)
				{
					throw new InvalidOperationException("no document visible in preview, no page rotation possible");
				}

				var documentStream = currentDocument.GetResolvedStream();
				//save document to disc, memorystream could not be stamped
				var tempFileName = FileNameService.GetTempFilePath(".pdf");
				using (var fileToStamp = File.Open(tempFileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None))
				{
					fileToStamp.SetLength(0);
					documentStream.Position = 0;
					documentStream.CopyTo(fileToStamp);
				}

				currentDocument.DiposeStreamIfIndicated();
				currentDocument.SetStream(null);

				var documentId = Convert.ToInt64(currentDocument.UniqueIdentifier);
				using (var documentContent = File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
				{
					m_DocumentsInboxService.UpdateDocumentContent(documentId, documentContent);
				}
				currentDocument.SetStream(m_DocumentsInboxService.GetContentStream(ActiveInboxEntry));
				DocumentPreviewViewModel.CurrentDocument = currentDocument;
				File.Delete(tempFileName);
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Drehen von Dokumentseiten ist ein Fehler aufgetreten.", "Dokument drehen");
			}
		}

		private DocumentInboxViewMode m_ViewMode;

		public DocumentInboxViewMode ViewMode
		{
			get { return m_ViewMode; }
			set
			{
				if (SetProperty(ref m_ViewMode, value))
				{
					RaisePropertyChanged(nameof(IsViewModeNone));
					RaisePropertyChanged(nameof(IsViewModeErv));
				}
			}
		}

		public bool IsViewModeErv
		{
			get { return DocumentInboxViewMode.Erv == ViewMode; }
		}

		public bool IsViewModeNone
		{
			get { return DocumentInboxViewMode.ScannerInbox == ViewMode; }
		}

		private IList<IDocumentInboxEntry> m_InboxEntries;

		public IList<IDocumentInboxEntry> InboxEntries
		{
			get { return m_InboxEntries; }
			private set
			{
				if (SetProperty(ref m_InboxEntries, value))
				{
					RaisePropertyChanged(nameof(IsListEmpty));
				}
			}
		}

		private DocumentInboxEntryBaseViewModel m_ActiveInboxEntry;

		public DocumentInboxEntryBaseViewModel ActiveInboxEntry
		{
			get { return m_ActiveInboxEntry; }
			set
			{
				// wenn der gleiche wie der aktive geklickt wird ist value = null
				var lastActiveEntry = m_ActiveInboxEntry;
				if (SetProperty(ref m_ActiveInboxEntry, value))
				{
					if (lastActiveEntry != null)
					{
						lastActiveEntry.PropertyChanged -= ActiveInboxEntry_PropertyChanged;
						var lastActiveMainEntry = lastActiveEntry as DocumentInboxMainEntryViewModel;
						if (lastActiveMainEntry != null)
						{
							lastActiveMainEntry.AddresseeId = m_CaseAndAddresseeSelectionViewModel.AddresseeId;
							lastActiveMainEntry.DocumentCaseNumber = m_CaseAndAddresseeSelectionViewModel.CaseNumbers;
							if (m_CaseAndAddresseeSelectionViewModel.FirstCaseToLink != null)
								lastActiveMainEntry.CaseStateId = m_CaseAndAddresseeSelectionViewModel.FirstCaseToLink.CaseStateId;
							UpdateInboxEntryByCaseAndAdresseeViewModel(lastActiveMainEntry);
						}
						var lastActiveSubEntry = lastActiveEntry as DocumentInboxSubEntryViewModel;
						if (lastActiveSubEntry != null)
						{
							((DocumentInboxSubEntryViewModel)lastActiveSubEntry).MainEntryViewModel.DocumentCaseNumber = m_CaseAndAddresseeSelectionViewModel.CaseNumbers;
							((DocumentInboxSubEntryViewModel)lastActiveSubEntry).MainEntryViewModel.AddresseeId = m_CaseAndAddresseeSelectionViewModel.AddresseeId;
							if (m_CaseAndAddresseeSelectionViewModel.FirstCaseToLink != null)
								((DocumentInboxSubEntryViewModel)lastActiveSubEntry).MainEntryViewModel.CaseStateId = m_CaseAndAddresseeSelectionViewModel.FirstCaseToLink.CaseStateId;
							UpdateInboxEntryByCaseAndAdresseeViewModel(((DocumentInboxSubEntryViewModel)lastActiveSubEntry).MainEntryViewModel);
						}
					}
					RefreshPreview();

					//var caseNumber = "";
					if (m_ActiveInboxEntry != null)
					{
						m_ActiveInboxEntry.PropertyChanged += ActiveInboxEntry_PropertyChanged;
						//var lastActiveMainEntry = lastActiveEntry as DocumentInboxMainEntryViewModel;
						//if (lastActiveMainEntry != null)
						//{
						//    //caseNumber = mainEntry.DocumentCaseNumber;
						//}
						if (IsViewModeErv)
						{
							if (m_ActiveInboxEntry is DocumentInboxMainEntryViewModel)
							{
								var existingCase = m_DocumentService.GetCaseBySearchText(m_ActiveInboxEntry.DocumentReceiverCaseNumber);
								if (existingCase != null && m_ActiveInboxEntry.Cases.Count == 0)
								{
									m_ActiveInboxEntry.Cases.Add(existingCase);
								}
							}
							else if (m_ActiveInboxEntry is DocumentInboxSubEntryViewModel)
							{
								var mainEntryCases = ((DocumentInboxSubEntryViewModel)m_ActiveInboxEntry).MainEntryViewModel.Cases;
								m_ActiveInboxEntry.Cases.Clear();
								foreach (var item in mainEntryCases)
								{
									m_ActiveInboxEntry.Cases.Add(item);
								}
								m_ActiveInboxEntry.AddresseeId = ((DocumentInboxSubEntryViewModel)m_ActiveInboxEntry).MainEntryViewModel.AddresseeId;
								m_ActiveInboxEntry.CaseStateId = ((DocumentInboxSubEntryViewModel)m_ActiveInboxEntry).MainEntryViewModel.CaseStateId;
							}
						}
					}
					//m_CaseAndAddresseeSelectionViewModel.CaseNumber = caseNumber;

					//var documentsToAddInformation = new DocumentsToAddInformation();
					//this.UpdateCaseAndAdresseeInformation(documentsToAddInformation);

					this.CreateToDosViewModel.ClearToDoViewModels();
					this.UpdateCaseAndAdresseeViewModelByInboxEntry(m_ActiveInboxEntry);

					CaseAndAddresseeSelectionViewModel.RefreshCachedData();
				}
			}
		}

		private void ActiveInboxEntry_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			if (String.Equals(e.PropertyName, nameof(DocumentInboxEntryBaseViewModel.DocumentFolderId)))
			{
				if (ActiveInboxEntry != null)
				{
					if (ActiveInboxEntry.DocumentFolderId != 0)
					{
						var folder = CaseAndAddresseeSelectionViewModel.Folders.FirstOrDefault(x => x.Id == ActiveInboxEntry.DocumentFolderId);
						if (folder != null)
						{
							if (folder.Color != ColorService.COLOR_NOCOLORSELECTED && folder.Color != Color.Empty)
							{
								ActiveInboxEntry.DocumentColor = folder.Color;
							}
						}
					}
					else
					{
						ActiveInboxEntry.DocumentColor = ColorService.COLOR_NOCOLORSELECTED;
					}
				}
			}
		}

		private DocumentInboxEntryBaseViewModel[] m_SelectedInboxEntries;

		public object[] SelectedInboxEntries
		{
			get { return m_SelectedInboxEntries; }
			set
			{
				if (value == null)
				{
					if (SetProperty(ref m_SelectedInboxEntries, new DocumentInboxEntryBaseViewModel[0]))
					{
						this.RefreshCanExecuteForCommands();
					}
				}
				else
				{
					if (SetProperty(ref m_SelectedInboxEntries, value.OfType<DocumentInboxEntryBaseViewModel>().ToArray()))
					{
						this.RefreshCanExecuteForCommands();
					}
				}
			}
		}

		private void RefreshPreview()
		{
			var document = m_SelectedInboxEntries.FirstOrDefault();
			if (document != null)
			{
				m_DocumentPreviewViewModel.Documents = GetDocumentsToPreview(document);
			}
			else
			{
				m_DocumentPreviewViewModel.Documents = new List<DocumentToPreviewViewModel>();
			}
		}

		internal List<DocumentToPreviewViewModel> GetDocumentsToPreview(IDocumentInboxSubEntry activeInboxEntry)
		{
			var documentsToPreview = new List<DocumentToPreviewViewModel>();

			var documentToPreview = new DocumentToPreviewViewModel();
			documentToPreview.DisposeStreamType = DisposeStreamMethod.DisposeOnPreviewUnload;
			documentToPreview.FileNameToDeterminePreview = FileNameService.GetValidFileName(activeInboxEntry.DocumentOriginalFileName);
			documentToPreview.FriendlyStandaloneName = activeInboxEntry.DocumentDisplayName;
			documentToPreview.IsDocumentWorkingCopy = false;
			documentToPreview.ResolveStreamAction = (string uniqueIdentifier) =>
			{
				//in ResolveStreamAction no reference to currentHistoryItem is allowed, because parameter is not available in action anymore
				var createXJustizPreviewResult = DocumentPreviewHelper.HandleXJustizXMLFiles(m_IocContainer, DocumentPreviewHelper.PreviewArea.DocumentInbox, documentToPreview, uniqueIdentifier, (docId) =>
				{
					var inboxDocumentId = Convert.ToInt64(uniqueIdentifier);
					//return m_DocumentsInboxService.GetContentStream(inboxDocumentId);
					return m_DocumentsInboxService.GetContentStream(activeInboxEntry);
				});
				if (createXJustizPreviewResult?.PdfFileStream != null)
				{
					return createXJustizPreviewResult.PdfFileStream;
				}

				return m_DocumentsInboxService.GetContentStream(activeInboxEntry);
			};
			DocumentPreviewHelper.SetTagOnDocumentToPreview(documentToPreview, DocumentMainType.BeA);

			documentToPreview.SelectionDropDownName = "Original";
			documentToPreview.UniqueIdentifier = activeInboxEntry.DocumentId.ToString();
			documentsToPreview.Add(documentToPreview);

			return documentsToPreview;
		}

		private string m_BusyDocumentsDescription;

		public string BusyDocumentsDescription
		{
			get { return m_BusyDocumentsDescription; }
			set { SetProperty(ref m_BusyDocumentsDescription, value); }
		}

		private void SaveChangesIfNeeded()
		{
			if (m_ActiveInboxEntry != null)
			{
				SaveChanges();
			}
		}

		private CaseAndAddresseeSelectionViewModel m_CaseAndAddresseeSelectionViewModel;

		public CaseAndAddresseeSelectionViewModel CaseAndAddresseeSelectionViewModel
		{
			get { return m_CaseAndAddresseeSelectionViewModel; }
			set { SetProperty(ref m_CaseAndAddresseeSelectionViewModel, value); }
		}

		private bool m_IsBusyDocuments;

		public bool IsBusyDocuments
		{
			get { return m_IsBusyDocuments; }
			set
			{
				if (SetProperty(ref m_IsBusyDocuments, value))
				{
					if (!value)
					{
						this.BusyDocumentsDescription = null;
					}
				}
			}
		}

		public void RefreshDocumentsAsync()
		{
			this.IsBusyDocuments = true;
			this.BusyDocumentsDescription = Properties.Resources.BusyDescription_DocumentsLoading;

			if (!m_IsLoadingDataInitially && m_InboxEntries == null)
			{
				this.IsLoadingDataInitially = true;
			}

			TaskHelper.FireAndForget(() => this.RefreshDocumentsInternal());
		}

		/// <summary>
		/// Gets the list of documents for which a command should be executed.
		/// </summary>
		public IList<IDocumentInboxSubEntry> GetDocumentsToExecuteCommandOn()
		{
			if (m_SelectedInboxEntries != null)
			{
				return m_SelectedInboxEntries;
			}
			else
			{
				return new DocumentInboxMainEntryViewModel[0];
			}
		}

		private void RefreshDocumentsInternal()
		{
			if (m_DocumentsInboxService == null)
			{
				m_Logger.Warn("no DocumentsInboxService available, no refresh possible");
				this.IsBusyDocuments = false;
				return;
			}

			try
			{
				if (String.IsNullOrWhiteSpace(this.InboxAlias))
				{
					m_Logger.Warn("no InboxAlias defined, no refresh possible");
					this.InboxEntries = null;
					return;
				}

				var inboxEntries = m_DocumentsInboxService.GetInboxByAlias(this.InboxAlias, this.InboxType);
				inboxEntries.ToList().ForEach(x => x.ResetIsDirty());
				foreach (var mainEntry in inboxEntries)
				{
					mainEntry.NewDocumentDateTime = mainEntry.DocumentDateTime;
					((DocumentInboxMainEntryViewModel)mainEntry).SubEntries.ForEach(x => x.NewDocumentDateTime = x.DocumentDateTime);

					((DocumentInboxMainEntryViewModel)mainEntry).SubEntries.ForEach(x => x.ResetIsDirty());
				}
				this.InboxEntries = inboxEntries;

				this.ErrorMessageWhileLoadingDocuments = "";
			}
			catch (Exception exp)
			{
				this.ErrorMessageWhileLoadingDocuments = "Fehler beim Laden der Dokumente: " + exp.Message;
				m_Logger.Error(exp);
			}
			finally
			{
				this.IsLoadingDataInitially = false;
				this.IsBusyDocuments = false;
			}
		}

		public void SaveChanges()
		{
			if (m_InboxEntries == null)
			{
				return;
			}

			if (m_ActiveInboxEntry != null)
			{
				UpdateInboxEntryByCaseAndAdresseeViewModel(m_ActiveInboxEntry);
			}

			var documentsToSave = m_InboxEntries.Where(x => x.IsDirty).ToList<IDocumentInboxSubEntry>();
			foreach (var mainEntry in m_InboxEntries)
			{
				if (((DocumentInboxMainEntryViewModel)mainEntry).SubEntries != null)
				{
					documentsToSave.AddRange(((DocumentInboxMainEntryViewModel)mainEntry).SubEntries.Where(x => x.IsDirty).ToList());
				}
			}
			m_DocumentsInboxService.SaveDocumentProperties(documentsToSave.Union(documentsToSave));

			foreach (var entry in m_InboxEntries)
			{
				entry.ResetIsDirty();
				((DocumentInboxMainEntryViewModel)entry).SubEntries?.ForEach(x => x.ResetIsDirty());
			}
		}

		public void UpdateCaseAndAdresseeInformation(DocumentsToAddInformation documentsToAddInformation)
		{
			this.CaseAndAddresseeSelectionViewModel.UpdateCaseAndAdresseeInformation(documentsToAddInformation);
		}

		private void UpdateCaseAndAdresseeViewModelByInboxEntry(DocumentInboxEntryBaseViewModel activeInboxEntry)
		{
			if (activeInboxEntry != null)
			{
				this.CaseAndAddresseeSelectionViewModel.AddresseeId = activeInboxEntry.AddresseeId;
			}
			else
			{
				this.CaseAndAddresseeSelectionViewModel.AddresseeId = 0;
			}

			if (activeInboxEntry != null && activeInboxEntry.Cases.Any())
			{
				this.CaseAndAddresseeSelectionViewModel.AddCaseToLinkTo(activeInboxEntry.Cases.First(), true);
				foreach (var @case in activeInboxEntry.Cases.Skip(1))
				{
					this.CaseAndAddresseeSelectionViewModel.AddCaseToLinkTo(@case, false);
				}
				this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.CaseStateId = activeInboxEntry.CaseStateId; // ???
			}
			else
			{
				this.CaseAndAddresseeSelectionViewModel.CaseNumber = "";
				this.CaseAndAddresseeSelectionViewModel.AddCaseToLinkTo(null, true); // das dann noch nötig?
			}
		}

		private void UpdateInboxEntryByCaseAndAdresseeViewModel(DocumentInboxEntryBaseViewModel activeInboxEntry)
		{
			activeInboxEntry.AddresseeId = this.CaseAndAddresseeSelectionViewModel.AddresseeId;
			if (this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink != null)
			{
				activeInboxEntry.CaseIds = this.CaseAndAddresseeSelectionViewModel.CasesToLink.Select(x => x.Id.ToString()).ToList();
				activeInboxEntry.CaseStateId = this.CaseAndAddresseeSelectionViewModel.FirstCaseToLink.CaseStateId; //TODODMS: CaseStateId muss eigentlich auch pro Case gespeichert werden
				activeInboxEntry.Cases = CaseAndAddresseeSelectionViewModel.CasesToLink.Select(x => x.Case).ToList();
			}
			else
			{
				activeInboxEntry.CaseIds = new List<string>();
				activeInboxEntry.CaseStateId = 0;
				activeInboxEntry.Cases = new List<ICase>();
			}
		}

		private bool m_IsEdaMahnVerfahrenCommandEnabled;

		public bool IsEdaMahnVerfahrenCommandEnabled
		{
			get => m_IsEdaMahnVerfahrenCommandEnabled;
			set => SetProperty(ref m_IsEdaMahnVerfahrenCommandEnabled, value);
		}

		private DelegateCommand m_EdaMahnVerfahrenCommand;

		public ICommand EdaMahnVerfahrenCommand
		{
			// TODODMS ggf. eigenes IDocumentInboxCommand bauen
			get
			{
				if (m_EdaMahnVerfahrenCommand == null)
				{
					m_EdaMahnVerfahrenCommand = new DelegateCommand(EdaMahnVerfahren);
				}
				return m_EdaMahnVerfahrenCommand;
			}
		}

		private void EdaMahnVerfahren()
		{
			try
			{
				DocumentInboxMainEntryViewModel mainInboxEntry = null;

				if (m_ActiveInboxEntry is DocumentInboxSubEntryViewModel subEntry)
				{
					mainInboxEntry = subEntry.MainEntryViewModel;
				}
				else
				{
					mainInboxEntry = m_ActiveInboxEntry as DocumentInboxMainEntryViewModel;
				}

				if (mainInboxEntry == null)
				{
					m_StandardDialogService.ShowMessageError("Die Auswahl der zu importierenden EDA-Datei ist nicht korrekt, die Datei kann nicht verarbeitet werden.", "EDA Mahnverfahren");
				}

				var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
				legacyLawyerUIServices.StartEdaMahnVerfahren(mainInboxEntry.Id, DocumentMainType.BeA);

				var ervService = m_IocContainer.Resolve<IErvService>();
				var beaMessagesToDelete = new List<Tuple<string, string>>();

				bool deleteEntryAfterImport = true;

#if DEBUG
				var channel = this.InboxType == TodoInboxType.Bea ? "beA" : "beBPo";
				if (m_StandardDialogService.ShowMessageInformation($"Soll der Eintrag \"{mainInboxEntry.DocumentDisplayName}\" aus dem Posteingang gelöscht werden ?", channel + " Nachricht", new string[] { "Ja", "Nein" }) == 1001)
				{
					deleteEntryAfterImport = false;
				}
#endif
				if (deleteEntryAfterImport)
				{
					if (this.InboxType == TodoInboxType.Bea)
					{
						ervService.MoveMessageToProcessedFolderOnRemoteSystem(mainInboxEntry.BeaPostfachId, mainInboxEntry.BeaMessageId);
					}
					beaMessagesToDelete.Add(Tuple.Create(mainInboxEntry.BeaPostfachId, mainInboxEntry.BeaMessageId));
				}

				var userInformationService = m_IocContainer.Resolve<ICurrentUserInformationService>();
				var userId = userInformationService.UserId;
				m_DocumentsInboxService.DeleteCompleteBeaMessage(userId, beaMessagesToDelete, PostInAuditActionType.BeAMessageDeletedAfterDirect);

				this.RefreshDocumentsAsync();
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Bei der Vearbeitung der EDA Datei ist ein Fehler aufgetreten.", "EDA Mahnverfahren");
			}
		}
	}
}