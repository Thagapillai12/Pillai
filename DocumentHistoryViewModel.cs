using DryIoc;
using Infragistics;
using log4net;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using WK.DE.DocumentManagement.Commands;
using WK.DE.DocumentManagement.Contracts.Enums;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.DocumentManagement.Contracts.ViewModels;
using WK.DE.DocumentManagement.Contracts.ViewModels.MobileCase;
using WK.DE.DocumentManagement.Contracts.ViewModels.PdfEditor;
using WK.DE.DocumentManagement.Contracts.ViewModels.ToDos;
using WK.DE.DocumentManagement.Helper;
using WK.DE.DocumentManagement.Helper.ViewModels;
using WK.DE.DocumentManagement.Services;
using WK.DE.DocumentManagement.Tags;
using WK.DE.DocumentManagement.ViewModels.Briefcase;
using WK.DE.DocumentManagement.ViewModels.DocumentLabelJuxtaposition;
using WK.DE.DocumentManagement.ViewModels.FilterTree;
using WK.DE.DocumentManagement.ViewModels.KNM;
using WK.DE.DocumentManagement.ViewModels.LegalPrime;
using WK.DE.DocumentManagement.ViewModels.MobileCase;
using WK.DE.DocumentManagement.ViewModels.SelectionHelper;
using WK.DE.DocumentManagement.Views;

namespace WK.DE.DocumentManagement.ViewModels
{
    public partial class DocumentHistoryViewModel : ViewModelWithFilterTreeBase
    {
        private static readonly ILog m_Logger = LogManager.GetLogger(typeof(DocumentHistoryViewModel));
        private List<DocumentHistoryEntryViewModel> m_AllDocuments;
        internal const string DATEFORMAT = "dd.MM.yyyy HH:mm:ss";

        internal const bool SAVECHANGESDIRECTLY = true;

        private bool m_HasToRefreshAfterEditModeEnded;
        private readonly IUsageTrackingService m_UsageTrackingService;

        private IPermissionService m_PermissionService;

        internal IPermissionService PermissionService
        {
            get { return m_PermissionService; }
        }

        public enum FilterTextModes
        {
            None,
            OnProperties,
            KNW,
        }

        public bool IsOnlineSharingActive
        {
            get
            {
                if (!ShowCaseSpecificProperties)
                {
                    return false;
                }
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, online sharing will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsOnlineSharingActive;
            }
        }

        public bool IsERVActive
        {
            get
            {
                if (!ShowCaseSpecificProperties)
                {
                    return false;
                }
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, online sharing will not be visible");
                }
                return m_DocumentService.Parameter.IsERVActive;
            }
        }

		public bool IsEAkteMultipleSelected
		{
			get
			{
				if (this.ActiveTab == DocumentHistoryTabs.DocumentList)
				{
					if (m_SelectedDocumentHistoryItemsOnDocumentsTab != null && m_SelectedDocumentHistoryItemsOnDocumentsTab.Length == 1)
					{
						return true;
					}
				}
				return false;
			}
		}

		public void RaiseIsEAkteMultipleSelected()
		{
			RaisePropertyChanged(nameof(IsEAkteMultipleSelected));
		}

		public bool ShowDeleteDocumentButton
        {
            get
            {
                if (IsShowingDeleted || m_DocumentService == null)
                {
                    return false;
                }
                return !m_DocumentService.Parameter.IsDocumentDeleteDisabled;
            }
        }

        public bool ShowFinalDeleteDocumentButton
        {
            get
            {
                if (IsNotShowingDeleted || m_DocumentService == null)
                {
                    return false;
                }
                return !m_DocumentService.Parameter.IsDocumentDeleteDisabled;
            }
        }

        private bool m_AllToDoTypesAvailable;

        public bool AllToDoTypesAvailable
        {
            get { return m_AllToDoTypesAvailable; }
            set { SetProperty(ref m_AllToDoTypesAvailable, value); }
        }

        private bool m_AllowMultipleDocumentSelection;

        public bool AllowMultipleDocumentSelection
        {
            get { return m_AllowMultipleDocumentSelection; }
            set { SetProperty(ref m_AllowMultipleDocumentSelection, value); }
        }

        private bool m_AllowFolderConfiguration;

        public bool AllowFolderConfiguration
        {
            get { return m_AllowFolderConfiguration; }
            set { SetProperty(ref m_AllowFolderConfiguration, value); }
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
            get { return this.IsDocumentPreviewSupportedInCurrentProcess && !m_IsPreviewAndDetailsSectionShownInSeparateWindow; }
        }

        private bool? m_IsDocumentPreviewSupportedInCurrentProcess;

        public bool IsDocumentPreviewSupportedInCurrentProcess
        {
            get
            {
                if (!m_IsDocumentPreviewSupportedInCurrentProcess.HasValue)
                {
                    var notShownInThisProcesses = new string[] { "POWERPNT", "WINWORD", "EXCEL", "OUTLOOK" };
                    var currentProcessName = Process.GetCurrentProcess().ProcessName;
                    m_IsDocumentPreviewSupportedInCurrentProcess = !notShownInThisProcesses.Contains(currentProcessName, StringComparer.OrdinalIgnoreCase);
                }
                return m_IsDocumentPreviewSupportedInCurrentProcess.Value;
            }
        }

        private bool m_IsDocumentPdfAndOnlineSharingActive;

        public bool IsDocumentPdfAndOnlineSharingActive
        {
            get { return m_IsDocumentPdfAndOnlineSharingActive; }
            set { SetProperty(ref m_IsDocumentPdfAndOnlineSharingActive, value); }
        }

        private bool m_IsWorkingCopyActive;

        public bool IsWorkingCopyActive
        {
            get { return m_IsWorkingCopyActive; }
            set
            {
                SetProperty(ref m_IsWorkingCopyActive, value);
                IsWorkingCopyActiveAndOnlyOneDocumentSelected = value && GetDocumentsToExecuteCommandOn().Count() == 1;
            }
        }

        private bool m_IsWorkingCopyActiveAndOnlyOneDocumentSelected;

        public bool IsWorkingCopyActiveAndOnlyOneDocumentSelected
        {
            get { return m_IsWorkingCopyActiveAndOnlyOneDocumentSelected; }
            set { SetProperty(ref m_IsWorkingCopyActiveAndOnlyOneDocumentSelected, value); }
        }

        private long m_ActiveWorkingCopyID;

        public long ActiveWorkingCopyID
        {
            get { return m_ActiveWorkingCopyID; }
            set { SetProperty(ref m_ActiveWorkingCopyID, value); }
        }

        public bool AreAllToDosOfTypeAppointment
        {
            get
            {
                var activeToDoItem = this.ActiveDocumentHistoryItemOnDocumentsTab?.ActiveToDoItem;
                return activeToDoItem?.Type == ToDoType.Appointment;
            }
        }

        public bool IsPostageTrackingActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, postage tracking will not be visible");
                }
                return m_DocumentService.Parameter.IsPostageTrackingActive;
            }
        }

        public bool ShowCaseSpecificProperties
        {
            get
            {
                return m_ViewMode == DocumentHistoryViewMode.Case || m_ViewMode == DocumentHistoryViewMode.SearchTerm;
            }
        }

        public bool ShowCaseNumberAndSubject
        {
            get
            {
                return m_ViewMode == DocumentHistoryViewMode.Addressee || m_ViewMode == DocumentHistoryViewMode.SearchTerm;
            }
        }

        public bool IsDocumentNumberingActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, document numbering will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsDocumentNumberingActive != DocumentNumbering.None;
            }
        }

        public bool IsCoverageInquiryActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, coverage inquiry will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsCoverageInquiryActive;
            }
        }

        public bool IsGDVActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, coverage inquiry will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsGDVActive;
            }
        }

        private bool? m_IsKNMActive;

        public bool IsKNMActive
        {
            get
            {
                if (!m_IsKNMActive.HasValue)
                {
                    if (m_IocContainer != null)
                    {
                        var knmSettingsViewModel = new KNMSettingsViewModel(m_IocContainer, m_StandardDialogService);
                        m_IsKNMActive = knmSettingsViewModel.IsKNMServerActivated;
                    }
                    else
                    {
                        m_IsKNMActive = false;
                    }
                }
                return m_IsKNMActive.Value;
            }
        }

        private bool? m_IsLegalPrimeActive;

        public bool IsLegalPrimeActive
        {
            get
            {
                if (!m_IsLegalPrimeActive.HasValue)
                {
                    if (m_IocContainer != null)
                    {
                        var legalPrimeSettingsViewModel = new LegalPrimeSettingsViewModel(m_IocContainer, m_StandardDialogService);
                        m_IsLegalPrimeActive = legalPrimeSettingsViewModel.IsLegalPrimeActive;
                    }
                    else
                    {
                        m_IsLegalPrimeActive = false;
                    }
                }
                return m_IsLegalPrimeActive.Value;
            }
        }

        public bool IsMobileCaseActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, mobile cases will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsMobileCaseActive;
            }
        }

        private IMobileCase m_CurrentMobileCase;

        public IMobileCase CurrentMobileCase
        {
            get
            {
                if (m_CurrentMobileCase == null)
                {
                    if (this.ViewMode == DocumentHistoryViewMode.Case && m_IocContainer.IsRegistered<IMobileCaseService>())
                    {
                        var mobileCaseService = m_IocContainer.Resolve<IMobileCaseService>();
                        m_CurrentMobileCase = mobileCaseService.GetMobileCase(this.CaseId, this.UserId);
                    }
                }
                return m_CurrentMobileCase;
            }
        }

        public bool CurrentMobileCaseInCloud
        {
            get
            {
                var currentMobileCase = this.CurrentMobileCase;
                if (currentMobileCase == null)
                {
                    return false;
                }
                return currentMobileCase.CloudSynchronisationMode != CloudSynchronisationMode.InActive;
            }
        }

        public string CurrentMobileCaseStatusText
        {
            get
            {
                var currentMobileCase = this.CurrentMobileCase;
                if (currentMobileCase == null)
                {
                    return "Sie haben kein SAA-Konto, keine Synchronisation mit der Cloud möglich";
                }
                if (currentMobileCase.CloudSynchronisationMode == CloudSynchronisationMode.InActive)
                {
                    return "Keine SAA für Sie zu dieser Akte vorhanden";
                }
                switch (currentMobileCase.Status)
                {
                    case MobileCaseStatus.None:
                        return "Status unbekannt";

                    case MobileCaseStatus.AllSynched:
                        return "Die SAA ist vollständig synchronisiert mit der Cloud";

                    case MobileCaseStatus.Error:
                        return "Bei der Synchronisierung ist ein Fehler aufgetreten.";

                    case MobileCaseStatus.WaitingForPacking:
                        return "SAA wartet auf packen";

                    case MobileCaseStatus.WaitingForUploading:
                        return "SAA wartet auf hochladen";

                    case MobileCaseStatus.IsBeingUploaded:
                        return "SAA wird gepackt";

                    case MobileCaseStatus.IsBeingPacked:
                        return "SAA wird hochgeladen";

                    case MobileCaseStatus.WaitingForSharing:
                        return "SAA wartet auf Teilung mit einem anderen Mitarbeiter";
                }
                return "Unbekannter Fehler bei der Ermittlung des Status";
            }
        }

        private List<MobileCaseSettingsOnBehalfUserViewModel> m_MobileCaseSettingsOnBehalfUsers;

        public List<MobileCaseSettingsOnBehalfUserViewModel> MobileCaseSettingsOnBehalfUsers
        {
            get
            {
                if (m_MobileCaseSettingsOnBehalfUsers == null)
                {
                    m_MobileCaseSettingsOnBehalfUsers = new List<MobileCaseSettingsOnBehalfUserViewModel>();

                    if (m_IocContainer.IsRegistered<IMobileCaseService>())
                    {
                        var mobileCaseService = m_IocContainer.Resolve<IMobileCaseService>();
                        foreach (var userWithOnlineAccount in mobileCaseService.GetUsersWithOnlineAccount().OrderBy(x => x.FriendlyName).ThenBy(x => x.Id))
                        {
                            if (!String.Equals(userWithOnlineAccount.Id, this.UserId, StringComparison.OrdinalIgnoreCase))
                            {
                                m_MobileCaseSettingsOnBehalfUsers.Add(new MobileCaseSettingsOnBehalfUserViewModel(this, userWithOnlineAccount, this.MobileCaseSettingsCommand));
                            }
                        }
                    }
                }
                return m_MobileCaseSettingsOnBehalfUsers;
            }
        }

        public bool IsTeamDocsActive
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                if (m_DocumentService.Parameter == null)
                {
                    m_Logger.Error("documentservice does not provide parameters, teamdocs will not be visible");
                    return false;
                }
                return m_DocumentService.Parameter.IsTeamDocsActive;
            }
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

        private bool m_IsNotShowingDeleted;

        public bool IsNotShowingDeleted
        {
            get { return m_IsNotShowingDeleted; }
            set
            {
                SetProperty(ref m_IsNotShowingDeleted, value);
                IsShowingDeleted = !value;
            }
        }

        private bool m_IsShowingDeleted;

        public bool IsShowingDeleted
        {
            get { return m_IsShowingDeleted; }
            set
            {
                SetProperty(ref m_IsShowingDeleted, value);
                RaisePropertyChanged(nameof(ShowDeleteDocumentButton));
                RaisePropertyChanged(nameof(ShowFinalDeleteDocumentButton));
            }
        }

        private string m_BusyDocumentsDescription;

        public string BusyDocumentsDescription
        {
            get { return m_BusyDocumentsDescription; }
            set { SetProperty(ref m_BusyDocumentsDescription, value); }
        }

        /// <summary>
        /// Get-/Set-Variable für IsDocumentGroupingVisible.
        /// </summary>
        private bool m_IsDocumentGroupingVisible;

        /// <summary>
        /// Gets or sets if the groupby-box for Document-List should be visible.
        /// </summary>
        public bool IsDocumentGroupingVisible
        {
            get { return m_IsDocumentGroupingVisible; }
            set { SetProperty(ref m_IsDocumentGroupingVisible, value); }
        }

        /// <summary>
        /// Get-/Set-Variable für IsToDoGroupingVisible.
        /// </summary>
        private bool m_IsToDoGroupingVisible;

        /// <summary>
        /// Gets or sets if the groupby-box for ToDo-List should be visible.
        /// </summary>
        public bool IsToDoGroupingVisible
        {
            get { return m_IsToDoGroupingVisible; }
            set { SetProperty(ref m_IsToDoGroupingVisible, value); }
        }

        /// <summary>
        /// Get-/Setter-Variable für CaseId.
        /// </summary>
        private long m_CaseId;

        /// <summary>
        /// Ruft die Id der Akte ab, zu der die Dokumente angezeigt werden sollen, oder legt diese fest.
        /// ACHTUNG: CaseId als letztes setzen, da nur dann die Dokumente geladen werden
        /// </summary>
        public long CaseId
        {
            get { return m_CaseId; }
            set
            {
                if (SetProperty(ref m_CaseId, value))
                {
                    this.RefreshAllCachedData(false);
                    RaisePropertyChanged(nameof(CaseId));
                }
                else
                {
                    this.RefreshDocumentsAsync();
                }
                m_DocumentTagTemplate = null;
                RaisePropertyChanged(nameof(DocumentTagTemplate));
                this.RefreshCurrentMobileCase();

                if (m_PreselectFolderTree && (this.DocumentListFilterTree.IsMetaDataFilterVisible || this.DocumentListFilterTree.IsStructureDataFilterVisible))
                {
                    if (this.Folders.Any())
                    {
                        this.DocumentListFilterTree.IsFolderFilterVisible = true;
                    }
                }
                if (DiscFolderMappingFilterTree == null)
                {
                    BuildDiscFolderMappingFilterTree(true);
                }
                else
                {
                    BuildDiscFolderMappingFilterTree(DiscFolderMappingFilterTree.IsMetaDataFilterVisible);
                    DiscFolderMapping.RefreshDocumentsAsync();
                }
                RaisePropertyChanged(nameof(DiscFolderMapping));
                if (ActiveTab == DocumentHistoryTabs.DiscFolderMapping && DiscFolderMapping.ExistsMappedFolder == false)
                {
                    ActiveTab = DocumentHistoryTabs.DocumentList;
                }
            }
        }

        private long m_MasterCaseId;

        public long MasterCaseId
        {
            get { return m_MasterCaseId; }
            set
            {
                if (SetProperty(ref m_MasterCaseId, value))
                {
                    m_Case = null;
                    m_Folders = null;
                    m_FolderAddresseDocumentation = null;
                    this.RefreshDocumentFolders();
                    this.RefreshSelectableValues();
                    this.RefreshDocumentsAsync();
                }
            }
        }

        private long m_AddresseeId;

        public long AddresseeId
        {
            get { return m_AddresseeId; }
            set
            {
                if (SetProperty(ref m_AddresseeId, value))
                {
                    m_Case = null;
                    m_Folders = null;
                    m_FolderAddresseDocumentation = null;
                    this.RefreshDocumentFolders();
                    this.RefreshSelectableValues();
                    this.RefreshDocumentsAsync();
                }
            }
        }

        private IDocumentSearchTerms m_SearchTerms;

        public IDocumentSearchTerms SearchTerms
        {
            get { return m_SearchTerms; }
            set
            {
                if (SetProperty(ref m_SearchTerms, value))
                {
                    m_Case = null;
                    m_Folders = null;
                    m_FolderAddresseDocumentation = null;
                    this.RefreshSelectableValues();
                }
            }
        }

        private ICase m_Case;

        /// <summary>
        /// Ruft die Akte und entsprechende Informationen ab.
        /// </summary>
        internal ICase Case
        {
            get
            {
                if (m_Case == null && m_CaseId > 0)
                {
                    m_Case = m_DocumentService.GetCase(m_CaseId);
                }
                return m_Case;
            }
        }

        private List<IOnlineShareAccount> m_CaseOnlineShareAccounts;

        private List<IOnlineShareAccount> CaseOnlineShareAccounts
        {
            get
            {
                if (m_CaseOnlineShareAccounts == null)
                {
                    if (this.Case != null)
                    {
                        m_CaseOnlineShareAccounts = m_DocumentService.GetOnlineShareAccountsByCaseId(this.Case.Id);
                    }
                    else
                    {
                        m_CaseOnlineShareAccounts = new List<IOnlineShareAccount>();
                    }
                }
                return m_CaseOnlineShareAccounts;
            }
        }

        private IDocumentTagTemplate m_DocumentTagTemplate;

        public IDocumentTagTemplate DocumentTagTemplate
        {
            get
            {
                if (m_DocumentTagTemplate == null)
                {
                    if (this.Case != null)
                    {
                        var tagService = m_IocContainer.Resolve<IDocumentTagService>();
                        m_DocumentTagTemplate = tagService.GetTagTemplateById(this.Case.TagTemplateId);
                    }
                }
                return m_DocumentTagTemplate;
            }
        }

        public bool HasModuleLicenceLabels
        { get; private set; }

        public bool HasModuleLicenceMindMap
        { get; private set; }

        public bool HasModuleLicenceTimeLine
        { get; private set; }

        private DocumentHistoryTabs m_ActiveTab;

        public DocumentHistoryTabs ActiveTab
        {
            get { return m_ActiveTab; }
            set
            {
                var oldValue = m_ActiveTab;
                if (SetProperty(ref m_ActiveTab, value))
                {
                    this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
                    switch (value)
                    {
                        case DocumentHistoryTabs.DocumentList:
                            m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_Documents");
                            break;

                        case DocumentHistoryTabs.DiscFolderMapping:
                            m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_DiscFolderMapping");
                            break;

                        case DocumentHistoryTabs.EventEMail:
                            m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_EMails");
                            break;

                        case DocumentHistoryTabs.EventERV:
                            m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_ERV");
                            break;

                        case DocumentHistoryTabs.EventOther:
                            m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_Others");
                            break;

                        case DocumentHistoryTabs.TimeLine:
                            if (this.HasModuleLicenceTimeLine)
                            {
                                m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_Timeline");
                                EnrichMindmap(m_DocumentHistoryEntries);
                            }
                            break;

                        case DocumentHistoryTabs.MindMap:
                            if (this.HasModuleLicenceMindMap)
                            {
                                m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_MindMap");
                                EnrichMindmap(m_DocumentHistoryEntries);
                            }
                            break;

                        case DocumentHistoryTabs.Labels:
                            if (this.HasModuleLicenceLabels)
                            {
                                m_UsageTrackingService?.TrackEvent("DMS", "Case_Tab_Labels");
                                if (m_DocumentLabelJuxtaposition != null)
                                {
                                    m_DocumentLabelJuxtaposition.SaveViewConfiguration();
                                }
                                this.DocumentLabelJuxtaposition = new DocumentLabelJuxtapositionViewModel(this);
                            }
                            break;
                    }

                    this.RefreshCanExecuteForDocumentCommands();
                }
            }
        }

        private IDocumentHistoryFolder m_FolderAddresseDocumentation;

        private IList<IDocumentHistoryFolder> m_Folders;

        internal IList<IDocumentHistoryFolder> Folders
        {
            get
            {
                if (m_Folders == null)
                {
                    if (m_ViewMode == DocumentHistoryViewMode.Addressee || m_ViewMode == DocumentHistoryViewMode.Participant)
                    {
                        m_Folders = new List<IDocumentHistoryFolder>();
                        m_FolderAddresseDocumentation = null;
                    }
                    else
                    {
                        m_Folders = m_DocumentService.GetHistoryFoldersByIdAndViewMode(this.CaseId, this.AddresseeId, m_ViewMode);
                        var ordnerAdressatenunterlagen = m_Folders?.FirstOrDefault(x => String.Equals(x.Name, "Adressatenunterlagen"));
                        if (ordnerAdressatenunterlagen != null)
                        {
                            m_Folders = ordnerAdressatenunterlagen.SubFolders;
                            m_FolderAddresseDocumentation = ordnerAdressatenunterlagen;
                        }
                        else
                        {
                            m_FolderAddresseDocumentation = null;
                        }
                    }
                }
                return m_Folders;
            }
        }

        private DiscFolderMappingViewModel m_DiscFolderMapping;

        public DiscFolderMappingViewModel DiscFolderMapping
        {
            get { return m_DiscFolderMapping; }
            private set { SetProperty(ref m_DiscFolderMapping, value); }
        }

        /// <summary>
        /// Get-/Setter-Variable für FolderId.
        /// </summary>
        private string m_TagName;

        /// <summary>
        /// Ruft den Namen des Tags ab, zu der die Dokumente angezeigt werden sollen, oder legt diese fest.
        /// </summary>
        public string TagName
        {
            get { return m_TagName; }
            set { SetProperty(ref m_TagName, value); }
        }

        private string m_UserId;

        public string UserId
        {
            get
            {
                if (m_UserId == null)
                {
                    var userInformationService = m_IocContainer.Resolve<ICurrentUserInformationService>();
                    m_UserId = userInformationService.UserId;
                }
                return m_UserId;
            }
        }

        /// <summary>
        /// Gets the user id to preselect in the working copies.
        /// </summary>
        /// <remarks>
        /// Usually the working copy of the current logged in user should be preselected.
        /// As an exception in the labels view the user to show data for could be configured. In this case, the working copies of the configured user have to be preselected,
        /// otherwise the labels in the working copies would not be visible.
        /// </remarks>
        public string WorkingCopyUserIdToPreselect
        {
            get
            {
                if (this.ActiveTab == DocumentHistoryTabs.Labels
                    && m_DocumentLabelJuxtaposition != null)
                {
                    return m_DocumentLabelJuxtaposition.UserId;
                }
                return this.UserId;
            }
        }

        /// <summary>
        /// Getter-Variable für DocumentHistoryEntries.
        /// </summary>
        private List<DocumentHistoryEntryViewModel> m_DocumentHistoryEntries;

        /// <summary>
        /// Ruft die Einträge ab, die in der Historie angezeigt werden sollen.
        /// </summary>
        public IEnumerable<DocumentHistoryEntryViewModel> DocumentHistoryEntries
        {
            get { return m_DocumentHistoryEntries; }
        }

        private DocumentHistoryEventGroupViewModel m_HistoryEventGroupMail;

        /// <summary>
        /// Gets information about events in document history related mails.
        /// </summary>
        public DocumentHistoryEventGroupViewModel HistoryEventGroupMail
        {
            get { return m_HistoryEventGroupMail; }
        }

        private DocumentHistoryEventGroupViewModel m_HistoryEventGroupERV;

        /// <summary>
        /// Gets a list of events representating court messages (beA, beN, beBPo).
        /// </summary>
        public DocumentHistoryEventGroupViewModel HistoryEventGroupERV
        {
            get { return m_HistoryEventGroupERV; }
        }

        private DocumentHistoryEventGroupViewModel m_HistoryEventGroupOther;

        /// <summary>
        /// Gets the list of events which are not located as any of the special types.
        /// </summary>
        public DocumentHistoryEventGroupViewModel HistoryEventGroupOther
        {
            get { return m_HistoryEventGroupOther; }
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

        public FilterTextModeViewModel[] AvailableFilterTextModes
        {
            get
            {
                if (this.IsKNMActive)
                {
                    return new FilterTextModeViewModel[] { new FilterTextModeViewModel(FilterTextModes.OnProperties), new FilterTextModeViewModel(FilterTextModes.KNW) };
                }
                else
                {
                    return new FilterTextModeViewModel[] { new FilterTextModeViewModel(FilterTextModes.OnProperties) };
                }
            }
        }

        private string m_TextBoxFilterTextWatermarkContent = "Bitte hier den Suchtext eingeben";

        public string TextBoxFilterTextWatermarkContent
        {
            get { return m_TextBoxFilterTextWatermarkContent; }
            set { SetProperty(ref m_TextBoxFilterTextWatermarkContent, value); }
        }

        private FilterTextModes m_FilterTextMode;

        public FilterTextModes FilterTextMode
        {
            get { return m_FilterTextMode; }
            set
            {
                if (SetProperty(ref m_FilterTextMode, value))
                {
                    m_Logger.DebugFormat("FilterTextMode: {0}", m_FilterTextMode);
                    RaisePropertyChanged(nameof(IsFilterTextModeKNMActive));
                    if (this.FilterTextMode == FilterTextModes.OnProperties)
                    {
                        TextBoxFilterTextWatermarkContent = "Bitte hier den Suchtext eingeben";
                        PerformFiltering();
                    }
                    else
                    {
                        TextBoxFilterTextWatermarkContent = "Bitte hier den Suchtext eingeben und mit Enter bestätigen";
                    }
                }
            }
        }

        public bool IsFilterTextModeKNMActive
        {
            get { return m_FilterTextMode == FilterTextModes.KNW; }
        }

        /// <summary>
        /// Get-/Setter-Variable für FilterText.
        /// </summary>
        private string m_FilterText;

        /// <summary>
        /// Ruft den FilterText ab oder legt diesen fest.
        /// </summary>
        public string FilterText
        {
            get { return m_FilterText; }
            set
            {
                if (SetProperty(ref m_FilterText, value))
                {
                    var filterTextMode = this.FilterTextMode;

                    m_Logger.DebugFormat("Filter text changed, filterTextMode={0}", filterTextMode);

                    //wenn nur in der Liste gesucht werden soll, dann die Filterung direkt durchführen, bei Verwendung des Wissensmanagements muss explizit auf den Button geklickt werden
                    if (filterTextMode == FilterTextModes.OnProperties)
                    {
                        PerformFiltering();
                    }
                }
            }
        }

        private List<MindMapNodeViewModel> m_MindMap;

        public List<MindMapNodeViewModel> MindMap
        {
            get { return m_MindMap; }
            private set { SetProperty(ref m_MindMap, value); }
        }

        public IEnumerable<MindMapStructureTypeViewModel> MindMapStructureLevel1SelectableValues
        {
            get { return GetMindMapStructureSelectableValues(new DocumentMindMapBuilderService.StructureType[0]); }
        }

        public IEnumerable<MindMapStructureTypeViewModel> MindMapStructureLevel2SelectableValues
        {
            get { return GetMindMapStructureSelectableValues(new DocumentMindMapBuilderService.StructureType[] { this.MindMapStructureLevel1SelectedValue }); }
        }

        public IEnumerable<MindMapStructureTypeViewModel> MindMapStructureLevel3SelectableValues
        {
            get { return GetMindMapStructureSelectableValues(new DocumentMindMapBuilderService.StructureType[] { this.MindMapStructureLevel1SelectedValue, this.MindMapStructureLevel2SelectedValue }); }
        }

        public IEnumerable<MindMapStructureTypeViewModel> MindMapStructureLevel4SelectableValues
        {
            get { return GetMindMapStructureSelectableValues(new DocumentMindMapBuilderService.StructureType[] { this.MindMapStructureLevel1SelectedValue, this.MindMapStructureLevel2SelectedValue, this.MindMapStructureLevel3SelectedValue }); }
        }

        public IEnumerable<MindMapStructureTypeViewModel> MindMapStructureLevel5SelectableValues
        {
            get { return GetMindMapStructureSelectableValues(new DocumentMindMapBuilderService.StructureType[] { this.MindMapStructureLevel1SelectedValue, this.MindMapStructureLevel2SelectedValue, this.MindMapStructureLevel3SelectedValue, this.MindMapStructureLevel4SelectedValue }); }
        }

        private IEnumerable<MindMapStructureTypeViewModel> GetMindMapStructureSelectableValues(IEnumerable<DocumentMindMapBuilderService.StructureType> structureTypesToFilter)
        {
            if (structureTypesToFilter.Any()) //do not allow no selection for first level of map
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.None);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.Classification))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.Classification);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.CorrespondenceType))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.CorrespondenceType);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.Date))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.Date);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.Month))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.Month);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.Source))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.Source);
            }
            if (!structureTypesToFilter.Contains(DocumentMindMapBuilderService.StructureType.Year))
            {
                yield return new MindMapStructureTypeViewModel(DocumentMindMapBuilderService.StructureType.Year);
            }
        }

        private DocumentMindMapBuilderService.StructureType m_MindMapStructureLevel1SelectedValue;

        public DocumentMindMapBuilderService.StructureType MindMapStructureLevel1SelectedValue
        {
            get { return m_MindMapStructureLevel1SelectedValue; }
            set
            {
                if (SetProperty(ref m_MindMapStructureLevel1SelectedValue, value))
                {
                    RaisePropertyChanged(nameof(MindMapStructureLevel2SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel2SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel3SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel3SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectedValue));
                    if (m_MindMap != null)
                    {
                        EnrichMindmap(m_DocumentHistoryEntries);
                    }
                }
            }
        }

        private DocumentMindMapBuilderService.StructureType m_MindMapStructureLevel2SelectedValue;

        public DocumentMindMapBuilderService.StructureType MindMapStructureLevel2SelectedValue
        {
            get { return m_MindMapStructureLevel2SelectedValue; }
            set
            {
                if (SetProperty(ref m_MindMapStructureLevel2SelectedValue, value))
                {
                    RaisePropertyChanged(nameof(MindMapStructureLevel3SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel3SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectedValue));
                    if (m_MindMap != null)
                    {
                        EnrichMindmap(m_DocumentHistoryEntries);
                    }
                }
            }
        }

        private DocumentMindMapBuilderService.StructureType m_MindMapStructureLevel3SelectedValue;

        public DocumentMindMapBuilderService.StructureType MindMapStructureLevel3SelectedValue
        {
            get { return m_MindMapStructureLevel3SelectedValue; }
            set
            {
                if (SetProperty(ref m_MindMapStructureLevel3SelectedValue, value))
                {
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel4SelectedValue));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectedValue));
                    if (m_MindMap != null)
                    {
                        EnrichMindmap(m_DocumentHistoryEntries);
                    }
                }
            }
        }

        private DocumentMindMapBuilderService.StructureType m_MindMapStructureLevel4SelectedValue;

        public DocumentMindMapBuilderService.StructureType MindMapStructureLevel4SelectedValue
        {
            get { return m_MindMapStructureLevel4SelectedValue; }
            set
            {
                if (SetProperty(ref m_MindMapStructureLevel4SelectedValue, value))
                {
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectableValues));
                    RaisePropertyChanged(nameof(MindMapStructureLevel5SelectedValue));
                    if (m_MindMap != null)
                    {
                        EnrichMindmap(m_DocumentHistoryEntries);
                    }
                }
            }
        }

        private DocumentMindMapBuilderService.StructureType m_MindMapStructureLevel5SelectedValue;

        public DocumentMindMapBuilderService.StructureType MindMapStructureLevel5SelectedValue
        {
            get { return m_MindMapStructureLevel5SelectedValue; }
            set
            {
                if (SetProperty(ref m_MindMapStructureLevel5SelectedValue, value))
                {
                    if (m_MindMap != null)
                    {
                        EnrichMindmap(m_DocumentHistoryEntries);
                    }
                }
            }
        }

        private ObservableCollection<IBriefcase> m_NotCachedBriefcases;

        public ObservableCollection<IBriefcase> NotCachedBriefcases
        {
            get { return m_NotCachedBriefcases; }
            set { SetProperty(ref m_NotCachedBriefcases, value); }
        }

        private ObservableCollection<IBriefcase> m_NotCachedBriefcasesForWorkingCopies;

        public ObservableCollection<IBriefcase> NotCachedBriefcasesForWorkingCopies
        {
            get { return m_NotCachedBriefcasesForWorkingCopies; }
            set { SetProperty(ref m_NotCachedBriefcasesForWorkingCopies, value); }
        }

        private ObservableCollection<IBriefcase> m_NotCachedSelectedPagesToBriefcases;

        public ObservableCollection<IBriefcase> NotCachedSelectedPagesToBriefcases
        {
            get { return m_NotCachedSelectedPagesToBriefcases; }
            set { SetProperty(ref m_NotCachedSelectedPagesToBriefcases, value); }
        }

        private ObservableCollection<IBriefcase> m_NotCachedSelectedPagesToBriefcasesForWorkingCopies;

        public ObservableCollection<IBriefcase> NotCachedSelectedPagesToBriefcasesForWorkingCopies
        {
            get { return m_NotCachedSelectedPagesToBriefcasesForWorkingCopies; }
            set { SetProperty(ref m_NotCachedSelectedPagesToBriefcasesForWorkingCopies, value); }
        }

        internal void LoadBriefcases()
        {
            var briefcaseService = m_IocContainer.Resolve<IBriefcaseService>();
            var briefcases = briefcaseService.GetBriefcasesByCaseAndUserId(m_CaseId, UserId);

            this.NotCachedBriefcases = new ObservableCollection<IBriefcase>(LoadBriefcaseViewModelForContextMenu(briefcases, SendDocumentToBriefcaseCommand));
            this.NotCachedBriefcasesForWorkingCopies = new ObservableCollection<IBriefcase>(LoadBriefcaseViewModelForContextMenu(briefcases, SendWorkingCopyToBriefcaseCommand));
            this.NotCachedSelectedPagesToBriefcases = new ObservableCollection<IBriefcase>(LoadBriefcaseViewModelForContextMenu(briefcases, SendDocumentToBriefcaseAndExtractPagesCommand));
            this.NotCachedSelectedPagesToBriefcasesForWorkingCopies = new ObservableCollection<IBriefcase>(LoadBriefcaseViewModelForContextMenu(briefcases, SendWorkingCopyToBriefcaseAndExtractPagesCommand));
        }

        private List<IBriefcase> LoadBriefcaseViewModelForContextMenu(IEnumerable<IBriefcase> briefcases, ICommand command)
        {
            var result = new List<IBriefcase>();
            foreach (var briefcase in briefcases)
            {
                var briefcaseWithCommand = new BriefcaseWithCommandViewModel(briefcase);
                briefcaseWithCommand.Id = briefcase.Id;
                briefcaseWithCommand.ExecutingCommand = command;
                briefcaseWithCommand.CaseId = briefcase.CaseId;
                result.Add(briefcaseWithCommand);
            }
            result.Add(new BriefcaseSeparator());
            result.Add(new BriefcaseWithCommandViewModel("Neue Postmappe...", command));
            if (this.IsSpecialBriefcaseCreationAvailable)
            {
                result.Add(new BriefcaseWithCommandViewModel(String.Format("Neue Postmappe {0}...", this.SpecialBriefcaseCreationName), command));
            }
            result.Add(new BriefcaseWithCommandViewModel("Zur Postmappe einer anderen Akte hinzufügen...", command));

            return result;
        }

        private void SetMindMapFilter(bool? email, bool? erv, bool? other)
        {
            if (email.HasValue)
            {
                if (email.Value)
                {
                    m_MindMapShowLinkedDocumentsEMail = true;
                }
                else
                {
                    m_MindMapShowLinkedDocumentsEMail = false;
                }
            }

            if (erv.HasValue)
            {
                if (erv.Value)
                {
                    m_MindMapShowLinkedDocumentsERV = true;
                }
                else
                {
                    m_MindMapShowLinkedDocumentsERV = false;
                }
            }

            if (other.HasValue)
            {
                if (other.Value)
                {
                    m_MindMapShowLinkedDocumentsOther = true;
                }
                else
                {
                    m_MindMapShowLinkedDocumentsOther = false;
                }
            }

            RaisePropertyChanged("MindMapShowLinkedDocumentsEMail");
            RaisePropertyChanged("MindMapShowLinkedDocumentsERV");
            RaisePropertyChanged("MindMapShowLinkedDocumentsOther");
            if (m_MindMap != null)
            {
                EnrichMindmap(m_DocumentHistoryEntries);
            }
        }

        private bool m_MindMapShowLinkedDocumentsEMail;

        public bool MindMapShowLinkedDocumentsEMail
        {
            get { return m_MindMapShowLinkedDocumentsEMail; }
            set { SetMindMapFilter(value, null, null); }
        }

        private bool m_MindMapShowLinkedDocumentsERV;

        public bool MindMapShowLinkedDocumentsERV
        {
            get { return m_MindMapShowLinkedDocumentsERV; }
            set { SetMindMapFilter(null, value, null); }
        }

        private bool m_MindMapShowLinkedDocumentsOther;

        public bool MindMapShowLinkedDocumentsOther
        {
            get { return m_MindMapShowLinkedDocumentsOther; }
            set { SetMindMapFilter(null, null, value); }
        }

        private bool m_IsInEditMode;

        public bool IsInEditMode
        {
            get { return m_IsInEditMode; }
            set
            {
                SetProperty(ref m_IsInEditMode, value);
                if (!value && m_HasToRefreshAfterEditModeEnded)
                {
                    RefreshDocumentsAsync();
                }
            }
        }

        private DocumentLabelJuxtapositionViewModel m_DocumentLabelJuxtaposition;

        /// <summary>
        /// Ruft das ViewModel für die Gegenüberstellung ab.
        /// </summary>
        /// <remarks>Wird erst geladen, wenn der Anwender tatsächlich mindestens einmal den Tabreiter ausgewählt hat.</remarks>
        public DocumentLabelJuxtapositionViewModel DocumentLabelJuxtaposition
        {
            get { return m_DocumentLabelJuxtaposition; }
            private set { SetProperty(ref m_DocumentLabelJuxtaposition, value); }
        }

        private List<DocumentHistoryEntryViewModel> m_FilteredDocumentHistoryEntries;

        /// <summary>
        /// Ruft die durch FilterText gefilterten Einträge ab.
        /// </summary>
        public IEnumerable<DocumentHistoryEntryViewModel> FilteredDocumentHistoryEntries
        {
            get
            {
                if (m_FilteredDocumentHistoryEntries != null)
                {
                    return m_FilteredDocumentHistoryEntries;
                }

                m_Logger.DebugFormat("FilterTextMode: {0}", this.FilterTextMode.ToString());

                if (this.FilterTextMode == FilterTextModes.OnProperties)
                {
                    try
                    {
                        var documentEntriesFilteredByTree = this.GetPrefilteredHistoryEntries();

                        if (documentEntriesFilteredByTree == null)
                        {
                            m_Logger.Debug("no prefiltered history entries");
                        }

                        if (String.IsNullOrWhiteSpace(m_FilterText))
                        {
                            m_Logger.Debug("FilterText is empty, no filtering performed");
                        }
                        else
                        {
                            m_Logger.DebugFormat("FilterText: {0}", m_FilterText);

                            var participantIds = new List<long>();
                            var @case = this.Case;
                            var userIdToFilter = new List<string>();
                            if (@case == null)
                            {
                                m_Logger.Debug("no case with participants to filter by");
                            }
                            else
                            {
                                if (@case.Participants == null)
                                {
                                    m_Logger.Debug("no case participants to filter by");
                                }
                                else
                                {
                                    participantIds = @case.Participants.Where(x => x.FirstName.IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                                                               || x.FirstName.IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0).Select(x => x.ParticipantId).ToList();
                                }
                            }

                            if (this.SelectableValuesDocumentCreator != null)
                            {
                                m_Logger.Debug("start filtering users");
                                //suche nach Namen des Erstellers, hier zunächst alle passenden UserIds ermitteln, in Suche dann die UserIds abgleichen
                                userIdToFilter = this.SelectableValuesDocumentCreator.Where(x => x != null && x.FriendlyName != null && x.FriendlyName.IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) > 0).Select(x => x.Id).ToList();
                                m_Logger.DebugFormat("userIdToFilter count: {0}", userIdToFilter.Count);
                            }
                            else
                            {
                                m_Logger.Debug("no users to filter by");
                            }

                            documentEntriesFilteredByTree = documentEntriesFilteredByTree.Where(x =>
                                    x.EventLastChangedDT.ToString(DATEFORMAT).IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                    || x.DocumentLastChangedDT.ToString(DATEFORMAT).IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                    || (x.DocumentDisplayName ?? "").IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                    || (x.DocumentNumber ?? "").IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                    || participantIds.Contains(x.DocumentAddresseeId)
                                    || userIdToFilter.Contains(x.DocumentOwnerUserId ?? "", StringComparer.CurrentCultureIgnoreCase)
                                    || (x.DocumentComment ?? "").IndexOf(m_FilterText, StringComparison.OrdinalIgnoreCase) >= 0
                                    );
                        }

                        m_Logger.Debug("set filterresults");

                        m_FilteredDocumentHistoryEntries = documentEntriesFilteredByTree
                                .OrderByDescending(x => x.DocumentLastChangedDT)
                                .ThenBy(x => x.DocumentMainType)
                                .ToList();

                        m_Logger.DebugFormat("Filter results: {0}", m_FilteredDocumentHistoryEntries.Count().ToString());

                        this.RefreshFilteredDocumentHistoryEntriesCountText();
                    }
                    catch (Exception exp)
                    {
                        m_Logger.Error("search failed", exp);
                        throw exp;
                    }
                }
                else
                {
                    m_Logger.Debug("Filter for KNM");

                    m_FilteredDocumentHistoryEntries = new List<DocumentHistoryEntryViewModel>();
                    TaskHelper.FireAndForget(() => this.PerformFilteringWithKNMServer());
                }
                return m_FilteredDocumentHistoryEntries;
            }
        }

        private string m_FilteredDocumentHistoryEntriesCountText;

        public string FilteredDocumentHistoryEntriesCountText
        {
            get { return m_FilteredDocumentHistoryEntriesCountText; }
            private set { SetProperty(ref m_FilteredDocumentHistoryEntriesCountText, value); }
        }

        private string m_FilteredDiscFolderMappingEntriesCountText;

        public string FilteredDiscFolderMappingEntriesCountText
        {
            get { return m_FilteredDiscFolderMappingEntriesCountText; }
            private set { SetProperty(ref m_FilteredDiscFolderMappingEntriesCountText, value); }
        }

        private string m_FilteredMindMapEntriesCountText;

        public string FilteredMindMapEntriesCountText
        {
            get { return m_FilteredMindMapEntriesCountText; }
            private set { SetProperty(ref m_FilteredMindMapEntriesCountText, value); }
        }

        private string m_FilteredTimelineEntriesCountText;

        public string FilteredTimelineEntriesCountText
        {
            get { return m_FilteredTimelineEntriesCountText; }
            private set { SetProperty(ref m_FilteredTimelineEntriesCountText, value); }
        }

        private bool m_FilteringInKNMRunning;

        public bool FilteringInKNMRunning
        {
            get { return m_FilteringInKNMRunning; }
            private set { SetProperty(ref m_FilteringInKNMRunning, value); }
        }

        private IEnumerable<DocumentHistoryEntryViewModel> GetPrefilteredHistoryEntries()
        {
            IEnumerable<DocumentHistoryEntryViewModel> documentEntriesFilteredByTree = m_DocumentHistoryEntries;
            if (documentEntriesFilteredByTree == null)
            {
                m_Logger.Debug("no DocumentHistoryEntries to prefilter");
                return new List<DocumentHistoryEntryViewModel>();
            }

            m_Logger.Debug("apply prefilter");

            documentEntriesFilteredByTree = this.DocumentListFilterTree.ApplyFilter(documentEntriesFilteredByTree, m_ViewMode, GetRootFolderId());

            m_Logger.DebugFormat("prefiltered documents: {0}", documentEntriesFilteredByTree.Count());

            return documentEntriesFilteredByTree;
        }

        private long GetRootFolderId()
        {
            if (m_ViewMode == DocumentHistoryViewMode.AddresseeDocumentation)
            {
                if (m_FolderAddresseDocumentation == null)
                {
                    //force reload of folders
                    this.Folders.ToArray();
                }
                if (m_FolderAddresseDocumentation == null)
                {
                    m_Logger.Warn("no root folder for addresseedocumentation available");
                    return 0;
                }
                return m_FolderAddresseDocumentation.Id;
            }
            return 0;
        }

        private CancellationTokenSource m_CancelKNMFiltering;

        private async void PerformFilteringWithKNMServer()
        {
            try
            {
                if (this.FilteringInKNMRunning)
                {
                    return;
                }

                this.ErrorMessageWhileLoadingDocuments = null;

                m_CancelKNMFiltering = new CancellationTokenSource();

                this.FilteringInKNMRunning = true;
                this.BusyDocumentsDescription = "Suche im Wissensmanagement wird durchgeführt...";
                this.IsBusyDocuments = true;

                var searchService = new KnowledgeManagementSearchService(m_IocContainer);
                var documentIds = await searchService.SearchForDocumentIds(this.Case.CaseNumber, this.FilterText, m_CancelKNMFiltering.Token);

                var documentEntriesFilteredByTree = this.GetPrefilteredHistoryEntries();
                documentEntriesFilteredByTree = documentEntriesFilteredByTree.Where(x => documentIds.Contains(x.DocumentId));

                m_FilteredDocumentHistoryEntries = documentEntriesFilteredByTree
                        .OrderByDescending(x => x.DocumentLastChangedDT)
                        .ThenBy(x => x.DocumentMainType)
                        .ToList();

                this.RefreshFilteredDocumentHistoryEntriesCountText();

                RaisePropertyChanged(nameof(FilteredDocumentHistoryEntries));
            }
            catch (Exception exp)
            {
                //if user has canceled the search, then do not show error message
                if (m_CancelKNMFiltering != null && m_CancelKNMFiltering.IsCancellationRequested)
                {
                    return;
                }

                m_Logger.Error(exp);
                this.ErrorMessageWhileLoadingDocuments = "Fehler bei der Suche im Wissensmanagement: " + exp.Message;
            }
            finally
            {
                m_CancelKNMFiltering = null;

                this.IsBusyDocuments = false;
                this.FilteringInKNMRunning = false;
            }
        }

        private void CancelFilteringCommand_Executed()
        {
            m_CancelKNMFiltering?.Cancel();
        }

        private DocumentHistoryViewMode m_ViewMode;

        public DocumentHistoryViewMode ViewMode
        {
            get { return m_ViewMode; }
            set
            {
                if (SetProperty(ref m_ViewMode, value))
                {
                    switch (m_ViewMode)
                    {
                        case DocumentHistoryViewMode.Addressee:
                            m_UsageTrackingService.TrackEvent("DMS", "Addressee");
                            break;

                        case DocumentHistoryViewMode.AddresseeDocumentation:
                            m_UsageTrackingService.TrackEvent("DMS", "AddresseeDocuments");
                            break;

                        case DocumentHistoryViewMode.Case:
                            m_UsageTrackingService.TrackEvent("DMS", "Case");
                            break;
                    }
                    CopyDocumentCommand.ViewMode = MoveDocumentCommand.ViewMode = m_ViewMode;

                    if (m_ViewMode == DocumentHistoryViewMode.SearchTerm)
                    {
                        InitializeBasicFilterAndMindMapTree();

                        this.DocumentListFilterTree.HideAllFilters();
                    }
                    else
                    {
                        var extendedUIServices = m_IocContainer.Resolve<IExtendedUIServices>();
                        var filterStatus = extendedUIServices.GetDocumentHistoryFilterSettings();

                        m_PreselectFolderTree = false;
                        switch (filterStatus.FilterStatusDocumentTab)
                        {
                            case DocumentHistoryFilterSettings.DocumentTabFilterStatus.Collapsed:
                                this.DocumentListFilterTree.HideAllFilters();
                                break;

                            case DocumentHistoryFilterSettings.DocumentTabFilterStatus.ExpandedWithFilterMetaData:
                                this.DocumentListFilterTree.IsMetaDataFilterVisible = true;
                                break;

                            case DocumentHistoryFilterSettings.DocumentTabFilterStatus.ExpandedWithFilterStructureData:
                                this.DocumentListFilterTree.IsStructureDataFilterVisible = true;
                                break;

                            case DocumentHistoryFilterSettings.DocumentTabFilterStatus.ExpandedWithFoldersIfAny:
                                this.DocumentListFilterTree.IsMetaDataFilterVisible = true;
                                m_PreselectFolderTree = true;
                                break;
                        }

                        bool eventFilterVisible = filterStatus.FilterStatusEventTab == DocumentHistoryFilterSettings.FilterStatus.Expanded;
                        this.HistoryEventGroupERV.FilterTree.IsMetaDataFilterVisible = eventFilterVisible;
                        this.HistoryEventGroupMail.FilterTree.IsMetaDataFilterVisible = eventFilterVisible;
                        this.HistoryEventGroupOther.FilterTree.IsMetaDataFilterVisible = eventFilterVisible;
                        //this.DiscFolderMappingFilterTree.IsMetaDataFilterVisible = true;
                        this.MindMapFilterTree.IsMetaDataFilterVisible = filterStatus.FilterStatusMindMapTab == DocumentHistoryFilterSettings.FilterStatus.Expanded;
                        this.TimelineFilterTree.IsMetaDataFilterVisible = filterStatus.FilterStatusTimelineTab == DocumentHistoryFilterSettings.FilterStatus.Expanded;
                    }
                    RaisePropertyChanged(nameof(ShowCaseSpecificProperties));
                    RaisePropertyChanged(nameof(ShowCaseNumberAndSubject));
                    this.DocumentListFilterTree.IsFolderFilterAvailable = this.MindMapFilterTree.IsFolderFilterAvailable = this.TimelineFilterTree.IsFolderFilterAvailable =
                        m_ViewMode == DocumentHistoryViewMode.Case || m_ViewMode == DocumentHistoryViewMode.AddresseeDocumentation;
                    this.DocumentListFilterTree.IsStructureDataFilterAvailable = m_ViewMode == DocumentHistoryViewMode.Case;
                }
            }
        }

        public override bool ShowWorkingCopies
        {
            get { return m_ViewMode != DocumentHistoryViewMode.SearchTerm; }
        }

        public bool SelectKeywordsOnlyFromList
        { get; }

        public bool DoNotSelectKeywordsOnlyFromList
        {
            get { return !SelectKeywordsOnlyFromList; }
        }

        /// <summary>
        /// Initialisiert eine neue Instanz der DocumentHistoryViewModel-Klasse ohne Daten zur Verwendung im Designer.
        /// </summary>
        public DocumentHistoryViewModel()
            : this(null, null)
        {
        }

        public DocumentHistoryViewModel(IContainer iocContainer, IStandardDialogService standardDialogService)
            : base(iocContainer, standardDialogService)
        {
            m_AllowMultipleDocumentSelection = true;

            m_HistoryEventGroupMail = new DocumentHistoryEventGroupViewModel(this, FilterTreeType.GroupMail, true, false);
            m_HistoryEventGroupMail.PropertyChanged += HistoryEventGroupMail_PropertyChanged;
            m_HistoryEventGroupERV = new DocumentHistoryEventGroupViewModel(this, FilterTreeType.GroupERV, true, true);
            m_HistoryEventGroupERV.PropertyChanged += HistoryEventGroupERV_PropertyChanged;
            m_HistoryEventGroupOther = new DocumentHistoryEventGroupViewModel(this, FilterTreeType.GroupOther, false, false);
            m_HistoryEventGroupOther.PropertyChanged += HistoryEventGroupOther_PropertyChanged;
            m_DiscFolderMapping = new DiscFolderMappingViewModel(this);
            m_DiscFolderMapping.PropertyChanged += DiscFolderMapping_PropertyChanged;
            m_DocumentHistoryEntries = new List<DocumentHistoryEntryViewModel>();

            m_FilterTextMode = FilterTextModes.OnProperties;

            this.InitializeDocumentCommands();
            this.InitializeToDoCommands();

            this.AddCommandTimelineDocumentClick();

            RefreshSelectableValues();

            m_MindMapStructureLevel1SelectedValue = DocumentMindMapBuilderService.StructureType.CorrespondenceType;
            m_MindMapStructureLevel2SelectedValue = DocumentMindMapBuilderService.StructureType.Source;
            m_MindMapStructureLevel3SelectedValue = DocumentMindMapBuilderService.StructureType.Year;
            m_MindMapStructureLevel4SelectedValue = DocumentMindMapBuilderService.StructureType.Date;

            InitializeBasicFilterAndMindMapTree();

            if (iocContainer != null)
            {
                var toDoService = m_IocContainer.Resolve<IToDoService>();
                this.AllToDoTypesAvailable = toDoService.Parameter.AllToDoTypesAvailable;

                var licenceService = m_IocContainer.IsRegistered<ILicenceService>() ? m_IocContainer.Resolve<ILicenceService>() : null;
                if (licenceService != null)
                {
                    this.HasModuleLicenceLabels = licenceService.IsModuleLicenced(ModuleToLicence.Labels);
                    this.HasModuleLicenceMindMap = licenceService.IsModuleLicenced(ModuleToLicence.MindMap);
                    this.HasModuleLicenceTimeLine = licenceService.IsModuleLicenced(ModuleToLicence.Timeline);
                }

                m_UsageTrackingService = m_IocContainer.Resolve<IUsageTrackingService>();

                m_PermissionService = m_IocContainer.Resolve<IPermissionService>();

                if (m_PermissionService.GetFunctionPermissionStatus(FunctionPermission.CaseFolderConfiguration, UserId) == PermissionStatus.Granted)
                {
                    AllowFolderConfiguration = true;
                }
                else
                {
                    AllowFolderConfiguration = false;
                }
                SelectKeywordsOnlyFromList = m_DocumentService.Parameter.SelectKeyWordsOnlyFromList;
            }
            else
            {
                this.AllToDoTypesAvailable = true;
            }
        }

        private void DiscFolderMapping_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, nameof(DiscFolderMapping.ActiveDocument), StringComparison.OrdinalIgnoreCase))
            {
                this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
            }
        }

        private void HistoryEventGroupMail_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, "ActiveDocument", StringComparison.OrdinalIgnoreCase))
            {
                RaisePropertyChanged("ActiveDocumentHistoryItemOnEventEMailTab");
                if (this.ActiveTab == DocumentHistoryTabs.EventEMail)
                {
                    this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
                }
            }
        }

        private void HistoryEventGroupERV_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, "ActiveDocument", StringComparison.OrdinalIgnoreCase))
            {
                RaisePropertyChanged("ActiveDocumentHistoryItemOnEventERVTab");
                if (this.ActiveTab == DocumentHistoryTabs.EventERV)
                {
                    this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
                }
            }
        }

        private void HistoryEventGroupOther_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, "ActiveDocument", StringComparison.OrdinalIgnoreCase))
            {
                RaisePropertyChanged("ActiveDocumentHistoryItemOnEventOtherTab");
                if (this.ActiveTab == DocumentHistoryTabs.EventOther)
                {
                    this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
                }
            }
        }

        /// <summary>
        /// Gets the list of documents for which a command should be executed.
        /// </summary>
        public IEnumerable<DocumentHistoryEntryViewModel> GetDocumentsToExecuteCommandOn()
        {
            if (this.ActiveTab == DocumentHistoryTabs.DocumentList)
            {
                if (m_SelectedDocumentHistoryItemsOnDocumentsTab != null && m_SelectedDocumentHistoryItemsOnDocumentsTab.Length > 0)
                {
                    return m_SelectedDocumentHistoryItemsOnDocumentsTab;
                }
            }
            if (this.ActiveTab == DocumentHistoryTabs.DiscFolderMapping)
            {
                if (DiscFolderMapping.SelectedDocumentsForBinding != null && DiscFolderMapping.SelectedDocumentsForBinding.Length > 0)
                {
                    return DiscFolderMapping.SelectedDocumentsForBinding as DocumentHistoryEntryViewModel[];
                }
            }
            if (this.ActiveDocumentHistoryItemOnActiveTab != null)
            {
                return new DocumentHistoryEntryViewModel[] { this.ActiveDocumentHistoryItemOnActiveTab };
            }
            return new DocumentHistoryEntryViewModel[0];
        }

        /// <summary>
        /// Gets the list of documents for which a command should be executed.
        /// </summary>
        public IEnumerable<DocumentHistoryEntryViewModel> GetLatestVersionOfDocumentsToExecuteCommandOn()
        {
            var selectedDocumentHistoryEntryViewModel = new List<DocumentHistoryEntryViewModel>();

            if (this.ActiveTab == DocumentHistoryTabs.DocumentList)
            {
                if (m_SelectedDocumentHistoryItemsOnDocumentsTab != null && m_SelectedDocumentHistoryItemsOnDocumentsTab.Length > 0)
                {
                    selectedDocumentHistoryEntryViewModel.AddRange(m_SelectedDocumentHistoryItemsOnDocumentsTab);
                }
            }
            if (!selectedDocumentHistoryEntryViewModel.Any() && this.ActiveDocumentHistoryItemOnActiveTab != null)
            {
                selectedDocumentHistoryEntryViewModel.Add(this.ActiveDocumentHistoryItemOnActiveTab);
            }

            var latestVersionsOfSelectedDocumentHistoryEntryViewModel = new List<DocumentHistoryEntryViewModel>();
            foreach (var documentHistoryEntryViewModel in selectedDocumentHistoryEntryViewModel)
            {
                var latestVersionOfDocument = GetLatestVersionOfDocument(documentHistoryEntryViewModel);
                if (!latestVersionsOfSelectedDocumentHistoryEntryViewModel.Any(x => x.DocumentId == latestVersionOfDocument.DocumentId))
                {
                    latestVersionsOfSelectedDocumentHistoryEntryViewModel.Add(latestVersionOfDocument);
                }
            }

            return latestVersionsOfSelectedDocumentHistoryEntryViewModel.ToArray();
        }

        private DocumentHistoryEntryViewModel GetLatestVersionOfDocument(DocumentHistoryEntryViewModel documentHistoryEntryViewModel)
        {
            if (documentHistoryEntryViewModel.DocumentVersioningNextDocumentId == 0 || documentHistoryEntryViewModel.DocumentVersioningLevel == DocumentVersioningLevel.LatestMainVersion)
            {
                return documentHistoryEntryViewModel;
            }

            if (m_AllDocuments == null)
            {
                RefreshDocumentsAsync();
            }

            DocumentHistoryEntryViewModel nextDocumentHistoryEntryViewModel = documentHistoryEntryViewModel;

            while (nextDocumentHistoryEntryViewModel.DocumentVersioningLevel != DocumentVersioningLevel.LatestMainVersion || nextDocumentHistoryEntryViewModel.DocumentVersioningNextDocumentId > 0)
            {
                nextDocumentHistoryEntryViewModel = m_AllDocuments.FirstOrDefault(x => x.DocumentId == nextDocumentHistoryEntryViewModel.DocumentVersioningNextDocumentId);
            }

            return nextDocumentHistoryEntryViewModel;
        }

        /// <summary>
        /// Refreshs cached information about folders.
        /// </summary>
        public void RefreshAllCachedData(bool ignoreDirtyEntries)
        {
            m_Case = null;
            m_Folders = null;
            m_FolderAddresseDocumentation = null;
            this.RefreshDocumentFolders();
            this.RefreshSelectableValues();
            this.RefreshStructureDataFilterAsync();
            this.RefreshDocumentsAsync(ignoreDirtyEntries);
            m_CaseOnlineShareAccounts = null;

            RaisePropertyChanged("CaseOnlineShareAccounts");
        }

        private void RefreshStructureDataFilterAsync()
        {
            if (IocContainer != null)
            {
                TaskHelper.FireAndForget(() => this.RefreshStructureDataFilterInternal());
            }
        }

        protected void RefreshStructureDataFilterInternal()
        {
            try
            {
                if (IocContainer != null && Case != null)
                {
                    var tagViewModel = new TagViewModel(IocContainer, DocumentTagTemplate, Case.Participants);
                    tagViewModel.PropertyChanged += TagViewModel_PropertyChanged;

                    if (m_DocumentListFilterTree.TagViewModel != tagViewModel)
                    {
                        m_DocumentListFilterTree.TagViewModel = tagViewModel;
                        m_DocumentListFilterTree.StructureDataFilter = tagViewModel.GetStructureDataFilter();
                    }

                    if (m_MindMapFilterTree.TagViewModel != tagViewModel)
                    {
                        m_MindMapFilterTree.TagViewModel = tagViewModel;
                        m_MindMapFilterTree.StructureDataFilter = tagViewModel.GetStructureDataFilter();
                    }

                    if (m_TimelineFilterTree.TagViewModel != tagViewModel)
                    {
                        m_TimelineFilterTree.TagViewModel = tagViewModel;
                        m_TimelineFilterTree.StructureDataFilter = tagViewModel.GetStructureDataFilter();
                    }
                }
            }
            catch (Exception exp)
            {
                m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Laden der Filter über die Strukturmuster ist ein Fehler aufgetreten.", "Strukturmuster laden");
            }
        }

        private void RefreshFilteredDocumentHistoryEntriesCountText()
        {
            if (m_DocumentHistoryEntries.Count == 1)
            {
                this.FilteredDocumentHistoryEntriesCountText = String.Format("{0} von {1} Dokument", m_FilteredDocumentHistoryEntries.Count, m_DocumentHistoryEntries.Count);
            }
            else
            {
                this.FilteredDocumentHistoryEntriesCountText = String.Format("{0} von {1} Dokumenten", m_FilteredDocumentHistoryEntries.Count, m_DocumentHistoryEntries.Count);
            }
        }

        public void RefreshCurrentMobileCase()
        {
            m_CurrentMobileCase = null;
            RaisePropertyChanged(nameof(CurrentMobileCase));
            RaisePropertyChanged(nameof(CurrentMobileCaseInCloud));
            RaisePropertyChanged(nameof(CurrentMobileCaseStatusText));
        }

        /// <summary>
        /// Aktualisiert die anzuzeigenden Dokumente.
        /// </summary>
        public void RefreshDocumentsAsync()
        {
            RefreshDocumentsAsync(false);
        }

        /// <summary>
        /// Aktualisiert die anzuzeigenden Dokumente.
        /// </summary>
        public void RefreshDocumentsAsync(bool ignoreDirtyEntries)
        {
            if (this.IsInEditMode)
            {
                m_HasToRefreshAfterEditModeEnded = true;
                return;
            }
            this.IsBusyDocuments = true;
            this.BusyDocumentsDescription = Properties.Resources.BusyDescription_DocumentsLoading;

            TaskHelper.FireAndForget(() => this.RefreshDocumentsInternal(ignoreDirtyEntries));
        }

        private void RefreshDocumentsInternal(bool ignoreDirtyEntries)
        {
            try
            {
                var showDeleted = false;
                if (m_DocumentService.Parameter.DeleteOnlyLogical)
                {
                    showDeleted = ((FilterTreeCheckableFilterViewModel)m_DocumentListFilterTree.MetaDataFilterTree.First(x => String.Equals(x.Key, FILTERTREECATEGORY_KEY_OTHER, StringComparison.OrdinalIgnoreCase)).Items[0]).IsChecked;
                    if (!m_IsNotShowingDeleted.Equals(!showDeleted))
                        IsNotShowingDeleted = !showDeleted;
                }
                else if (!m_IsNotShowingDeleted)
                {
                    IsNotShowingDeleted = true;
                }

                List<DocumentHistoryEntryViewModel> newDocumentHistoryEntries = null;

                switch (m_ViewMode)
                {
                    case DocumentHistoryViewMode.Case:
                        if (m_CaseId > 0)
                        {
                            newDocumentHistoryEntries = m_DocumentService.GetDocumentsByCaseAndFolderId(m_CaseId, null).OfType<DocumentHistoryEntryViewModel>().ToList();
                        }
                        break;

                    case DocumentHistoryViewMode.MasterCase:
                        if (m_MasterCaseId > 0)
                        {
                            newDocumentHistoryEntries = m_DocumentService.GetDocumentsByMasterCaseId(m_MasterCaseId).OfType<DocumentHistoryEntryViewModel>().ToList();
                        }
                        break;

                    case DocumentHistoryViewMode.Addressee:
                        if (m_AddresseeId > 0)
                        {
                            newDocumentHistoryEntries = m_DocumentService.GetDocumentsByAddresseeId(m_AddresseeId, false).OfType<DocumentHistoryEntryViewModel>().ToList();
                        }
                        break;

                    case DocumentHistoryViewMode.AddresseeDocumentation:
                        if (m_AddresseeId > 0)
                        {
                            newDocumentHistoryEntries = m_DocumentService.GetDocumentsByAddresseeId(m_AddresseeId, true).OfType<DocumentHistoryEntryViewModel>().ToList();
                        }
                        break;

                    case DocumentHistoryViewMode.Participant:
                        if (m_CaseId > 0 && m_AddresseeId > 0)
                        {
                            newDocumentHistoryEntries = m_DocumentService.GetDocumentsByCaseAndAddresseeId(m_CaseId, m_AddresseeId).OfType<DocumentHistoryEntryViewModel>().ToList();
                        }
                        break;

                    case DocumentHistoryViewMode.SearchTerm:
                        newDocumentHistoryEntries = m_DocumentService.GetDocumentsBySearchTerm(m_SearchTerms, !IsNotShowingDeleted).OfType<DocumentHistoryEntryViewModel>().ToList();
                        break;

                    default:
                        throw new NotImplementedException("ViewMode: " + m_ViewMode.ToString());
                }
                if (newDocumentHistoryEntries == null)
                {
                    return;
                }
                newDocumentHistoryEntries = ApplyFixedFileExtensionFilter(newDocumentHistoryEntries);
                m_AllDocuments = newDocumentHistoryEntries.ToList(); //copy the list, to find all files and previous versions (even if sub version have been remove)
                m_AllDocuments.ForEach(x => x.Initializing = true);
                EnrichIsInMobileCase(newDocumentHistoryEntries);
                if (m_ViewMode != DocumentHistoryViewMode.SearchTerm)
                {
                    EnrichSubVersions(newDocumentHistoryEntries, !IsNotShowingDeleted);
                }
                EnrichEventType(newDocumentHistoryEntries);
                if (this.ActiveTab == DocumentHistoryTabs.MindMap)
                {
                    EnrichMindmap(newDocumentHistoryEntries);
                }
                m_AllDocuments.ForEach(x => x.Initializing = false);
                if (this.IsInEditMode)
                {
                    m_HasToRefreshAfterEditModeEnded = true;
                    return;
                }
                var activeDocumentHistoryItemOnDocumentsTab = m_ActiveDocumentHistoryItemOnDocumentsTab;
                var listOfdocumentIdsNotToRefresh = m_DocumentHistoryEntries.Where(x => x != null && (x.IsDirty || x.PreviousVersion != null && x.PreviousVersions.Any(y => y != null && y.IsDirty))).Select(x => x.DocumentId);
                if (!ignoreDirtyEntries && listOfdocumentIdsNotToRefresh.Any())
                {
                    m_DocumentHistoryEntries.RemoveAll(x => !listOfdocumentIdsNotToRefresh.Contains(x.DocumentId));
                    m_DocumentHistoryEntries.AddRange(newDocumentHistoryEntries.Where(x => !listOfdocumentIdsNotToRefresh.Contains(x.DocumentId)).ToList());
                }
                else
                {
                    m_DocumentHistoryEntries = newDocumentHistoryEntries.ToList();
                }
                DocumentHistoryEntryViewModel newActiveDocumentHistoryItemOnDocumentsTab = null;
                if (activeDocumentHistoryItemOnDocumentsTab != null)
                {
                    newActiveDocumentHistoryItemOnDocumentsTab = m_DocumentHistoryEntries.FirstOrDefault(x => x.DocumentId == activeDocumentHistoryItemOnDocumentsTab.DocumentId);
                }
                if (newActiveDocumentHistoryItemOnDocumentsTab == null)
                {
                    newActiveDocumentHistoryItemOnDocumentsTab = m_DocumentHistoryEntries.OrderBy(x => x.DocumentLastChangedDT).LastOrDefault();
                }
                RaisePropertyChanged(nameof(MindMap));
                RaisePropertyChanged(nameof(DocumentHistoryEntries));
                RaisePropertyChanged(nameof(DiscFolderMapping));

                this.ActiveDocumentHistoryItemOnDocumentsTab = newActiveDocumentHistoryItemOnDocumentsTab;
                RaisePropertyChanged(nameof(FilterTree));
                PerformFiltering();
                m_HasToRefreshAfterEditModeEnded = false;

                this.ErrorMessageWhileLoadingDocuments = "";

                RefreshSelectableValuesDocumentAddressee();
            }
            catch (Exception exp)
            {
                m_Logger.Error(exp);
                this.ErrorMessageWhileLoadingDocuments = "Fehler beim Laden der Dokumente: " + exp.Message;
            }
            finally
            {
                this.IsBusyDocuments = false;
            }
        }

        private List<DocumentHistoryEntryViewModel> ApplyFixedFileExtensionFilter(List<DocumentHistoryEntryViewModel> newDocumentHistoryEntries)
        {
            if (m_FixedFilterFileExtensionGroup == FileExtensionGroup.None)
            {
                return newDocumentHistoryEntries;
            }

            var validExtensions = FileExtensionsService.GetFileExtensions(m_FixedFilterFileExtensionGroup);
            return newDocumentHistoryEntries.Where(x => validExtensions.Contains(Path.GetExtension(x.DocumentOriginalFileName), StringComparer.OrdinalIgnoreCase)).ToList();
        }

        private void EnrichMindmap(List<DocumentHistoryEntryViewModel> documentHistoryEntries)
        {
            m_Logger.Debug(WK.DE.Logging.LogConstants.MethodStart);

            var filteredDocumentHistoryEntries = this.MindMapFilterTree.ApplyFilter(documentHistoryEntries, m_ViewMode, GetRootFolderId()).ToList();

            var showLinksForEventTypeEMail = this.MindMapShowLinkedDocumentsEMail;
            var showLinksForEventTypeERV = this.MindMapShowLinkedDocumentsERV;
            var showLinksForEventTypeOther = this.MindMapShowLinkedDocumentsOther;

            var getTuple = new Func<Tuple<DocumentCorrespondenceType, string, string>, Tuple<string, string>>((input) =>
            {
                return new Tuple<string, string>(input.Item2, input.Item3);
            });
            var documentCorrespondenceTypeNames = ComboBoxValueService.GetAvailableDocumentCorrespondenceTypes().ToDictionary(x => x.Item1, x => getTuple(x));
            var documentClassificationNames = ComboBoxValueService.GetAvailableDocumentClassifications().ToDictionary(x => x.Item1, x => x.Item2);
            var documentSourceNames = ComboBoxValueService.GetAvailableDocumentSources().ToDictionary(x => x.Item1, x => x.Item2);

            var structureTypes = new List<DocumentMindMapBuilderService.StructureType>();
            structureTypes.Add(this.MindMapStructureLevel1SelectedValue);
            structureTypes.Add(this.MindMapStructureLevel2SelectedValue);
            structureTypes.Add(this.MindMapStructureLevel3SelectedValue);
            structureTypes.Add(this.MindMapStructureLevel4SelectedValue);
            structureTypes.Add(this.MindMapStructureLevel5SelectedValue);
            while (structureTypes.Contains(DocumentMindMapBuilderService.StructureType.None))
            {
                structureTypes.Remove(DocumentMindMapBuilderService.StructureType.None);
            }

            var documentMindMapBuilderService = new DocumentMindMapBuilderService(documentCorrespondenceTypeNames, documentClassificationNames, documentSourceNames);
            var mindMap = documentMindMapBuilderService.CreateMindMap(filteredDocumentHistoryEntries, showLinksForEventTypeEMail, showLinksForEventTypeERV, showLinksForEventTypeOther, structureTypes, this.MindMapNodeClicked);

            m_Logger.Debug("mindmap created, triggering PropertyChanged");

            this.MindMap = mindMap;

            var count = filteredDocumentHistoryEntries.Count;
            if (count == 1)
            {
                this.FilteredMindMapEntriesCountText = String.Format("{0} von {1} Dokument", count, m_DocumentHistoryEntries.Count);
            }
            else
            {
                this.FilteredMindMapEntriesCountText = String.Format("{0} von {1} Dokumenten", count, m_DocumentHistoryEntries.Count);
            }

            m_Logger.Debug(WK.DE.Logging.LogConstants.MethodEnd);
        }

        private void MindMapNodeClicked(DocumentHistoryEntryViewModel documentVersionEntry)
        {
            this.ActiveDocumentHistoryItemOnMindMapTab = documentVersionEntry;
        }

        private void EnrichSubVersions(List<DocumentHistoryEntryViewModel> documentHistoryEntries, bool showDeleted)
        {
            m_Logger.Debug(WK.DE.Logging.LogConstants.MethodStart);

            var subVersionEntries = documentHistoryEntries.Where(x => x.DocumentVersioningLevel != DocumentVersioningLevel.LatestMainVersion).ToList(); //.ToList, da sich die Auflistung ändert
            foreach (var subVersionEntry in subVersionEntries.OrderBy(x => x.DocumentId))
            {
                subVersionEntry.EventType = EventType.None;
                if (subVersionEntry.IsDeleted == showDeleted)
                {
                    SetNextVersion(subVersionEntry, subVersionEntry.DocumentId, showDeleted);
                }
            }
            subVersionEntries.Where(x => x.DocumentVersioningLevel != DocumentVersioningLevel.LatestMainVersion).ToList().ForEach(x => documentHistoryEntries.Remove(x));

            foreach (var mainVersion in documentHistoryEntries.Where(x => x.PreviousVersion != null))
            {
                if (String.Equals(mainVersion.DocumentVersion, "1", StringComparison.OrdinalIgnoreCase))
                {
                    RenumberDocumentVersions(mainVersion);
                }
            }

            documentHistoryEntries.Where(y => y.IsDeleted != showDeleted).ToList().ForEach(x => documentHistoryEntries.Remove(x));

            m_Logger.Debug(WK.DE.Logging.LogConstants.MethodEnd);
        }

        private void SetNextVersion(DocumentHistoryEntryViewModel subVersionEntry, long documentId, bool showDeleted)
        {
            IEnumerable<DocumentHistoryEntryViewModel> nextVersions;
            if (subVersionEntry.DocumentVersioningLevel == DocumentVersioningLevel.PreviousVersionLevel2)
            {
                nextVersions = m_AllDocuments.Where(x => x.DocumentVersioningPreviousSubDocumentId == documentId);
                if (!nextVersions.Any())
                {
                    nextVersions = m_AllDocuments.Where(x => x.DocumentVersioningPreviousDocumentId == documentId);
                }
            }
            else
            {
                nextVersions = m_AllDocuments.Where(x => x.DocumentVersioningPreviousDocumentId == documentId);
            }
            var nextVersionsCount = nextVersions.Count();
            if (nextVersionsCount > 0)
            {
                DocumentHistoryEntryViewModel nextVersion;
                if (nextVersionsCount == 1)
                {
                    nextVersion = nextVersions.First();
                }
                else
                {
                    nextVersion = nextVersions.FirstOrDefault(x => x.DocumentVersioningLevel == DocumentVersioningLevel.LatestMainVersion);
                    if (nextVersion == null)
                    {
                        nextVersion = nextVersions.OrderByDescending(x => x.DocumentId).First();
                    }
                }
                if (nextVersion.IsDeleted != showDeleted)
                {
                    if (nextVersion.DocumentVersioningLevel != DocumentVersioningLevel.LatestMainVersion)
                    {
                        SetNextVersion(subVersionEntry, nextVersion.DocumentId, showDeleted);
                    }
                    else
                    {
                        subVersionEntry.DocumentVersioningLevel = DocumentVersioningLevel.LatestMainVersion;
                    }
                }
                else
                {
                    if (nextVersion.PreviousVersion != null)
                    {
                        var nextDocumentVersion = nextVersion.DocumentVersion.Split('.')[0];
                        while (nextVersion.PreviousVersion != null && String.Equals(nextVersion.PreviousVersion.DocumentVersion.Split('.')[0], nextDocumentVersion))
                        {
                            nextVersion = nextVersion.PreviousVersion;
                        }
                        if (nextVersion.PreviousVersion != null)
                        {
                            var subEntry = subVersionEntry;
                            while (subEntry.PreviousVersion != null && String.Equals(subEntry.PreviousVersion.DocumentVersion.Split('.')[0], nextDocumentVersion))
                            {
                                subEntry = subEntry.PreviousVersion;
                            }
                            subEntry.PreviousVersion = nextVersion.PreviousVersion;
                            subEntry.PreviousVersion.DocumentVersioningNextDocumentId = nextVersion.DocumentId;
                        }
                    }
                    nextVersion.PreviousVersion = subVersionEntry;
                    subVersionEntry.DocumentVersioningNextDocumentId = nextVersion.DocumentId;
                    m_Logger.DebugFormat("document {0} is a former version and will be shown to item {1}", subVersionEntry.DocumentId, nextVersion.DocumentId);
                }
            }
            else
            {
                //hier tritt der besondere Fall ein, dass von einer vorherigen Version eine neue Hauptversion gezogen wurde
                //  dann gibt es mehrere Einträge, die als Vorgängerversion die alte Version haben
                //  der alte Haupteintrag muss dann eingereiht werden, hierzu dann diesen Eintrag als neuen Eintrag zwischen der Ursprungsversion
                //  und dem aktuellen Haupteintrag einfügen
                var previousVersion = m_AllDocuments.FirstOrDefault(x => x.DocumentId == subVersionEntry.DocumentVersioningPreviousDocumentId);
                if (previousVersion != null)
                {
                    var nextVersion = m_AllDocuments.FirstOrDefault(x => x.PreviousVersion == previousVersion);
                    if (nextVersion != null)
                    {
                        if (nextVersion.IsDeleted != showDeleted)
                        {
                            if (nextVersion.DocumentVersioningLevel != DocumentVersioningLevel.LatestMainVersion)
                            {
                                SetNextVersion(subVersionEntry, nextVersion.DocumentId, showDeleted);
                            }
                            else
                            {
                                subVersionEntry.DocumentVersioningLevel = DocumentVersioningLevel.LatestMainVersion;
                            }
                        }
                        else
                        {
                            nextVersion.PreviousVersion = subVersionEntry;
                            subVersionEntry.PreviousVersion = previousVersion;
                            subVersionEntry.DocumentVersioningNextDocumentId = nextVersion.DocumentId;
                            previousVersion.DocumentVersioningNextDocumentId = subVersionEntry.DocumentId;
                        }
                    }
                }
                else
                {
                    m_Logger.WarnFormat("document {0} is a former version, current version could not be found", subVersionEntry.DocumentId);
                }
            }
        }

        private void EnrichIsInMobileCase(List<DocumentHistoryEntryViewModel> newDocumentHistoryEntries)
        {
            if (this.ViewMode != DocumentHistoryViewMode.Case)
            {
                return;
            }

            var currentMobileCase = this.CurrentMobileCase;
            if (currentMobileCase?.SelectedDocumentIds == null)
            {
                return;
            }
            if (!currentMobileCase.SelectedDocumentIds.Any())
            {
                return;
            }

            newDocumentHistoryEntries.ForEach(x => x.DocumentIsInMobileCase = currentMobileCase.SelectedDocumentIds.Contains(x.DocumentId));
        }

        private int RenumberDocumentVersions(DocumentHistoryEntryViewModel mainVersion)
        {
            if (mainVersion.PreviousVersion == null)
            {
                mainVersion.DocumentVersion = "1";
                return 1;
            }
            var subVersion = RenumberDocumentVersions(mainVersion.PreviousVersion);
            subVersion++;
            mainVersion.DocumentVersion = subVersion.ToString();
            return subVersion;
        }

        private void EnrichEventType(IList<DocumentHistoryEntryViewModel> documentHistoryEntries)
        {
            var historyEventsMail = new List<HistoryEventViewModel>();
            var historyEventsERV = new List<HistoryEventViewModel>();
            var historyEventsOther = new List<HistoryEventViewModel>();

            foreach (var group in documentHistoryEntries.GroupBy(x => x.EventId))
            {
                var eventType = EventType.None;

                if (group.Key > 0 && group.Count() > 1
                    || (group.Any() && group.First().DocumentMainType != DocumentMainType.None))
                {
                    eventType = EventType.Other;

                    HistoryEventViewModel historyEvent;

                    var document = group.OrderBy(x => x.DocumentId).First();

                    switch (document.DocumentMainType)
                    {
                        case DocumentMainType.EMail:
                            historyEvent = new HistoryEventWithHeadDocumentViewModel(document, group);
                            eventType = document.MessageDirection == MessageDirection.Incoming ? EventType.Mail_Incoming : EventType.Mail_Outgoing;
                            historyEventsMail.Add(historyEvent);
                            break;

                        case DocumentMainType.BeA:
                            historyEvent = new HistoryErvEventWithHeadDocumentViewModel(document, group);
                            eventType = document.MessageDirection == MessageDirection.Incoming ? EventType.BeA_Message_Incoming : EventType.BeA_Message_Outgoing;
                            historyEventsERV.Add(historyEvent);
                            break;

                        case DocumentMainType.BeN:
                            historyEvent = new HistoryErvEventWithHeadDocumentViewModel(document, group);
                            eventType = document.MessageDirection == MessageDirection.Incoming ? EventType.BeN_Message_Incoming : EventType.BeN_Message_Outgoing;
                            historyEventsERV.Add(historyEvent);
                            break;

                        case DocumentMainType.BeBPo:
                            historyEvent = new HistoryErvEventWithHeadDocumentViewModel(document, group);
                            eventType = document.MessageDirection == MessageDirection.Incoming ? EventType.BeBPo_Message_Incoming : EventType.BeBPo_Message_Outgoing;
                            historyEventsERV.Add(historyEvent);
                            break;

                        default:
                            historyEvent = new HistoryEventViewModel();
                            historyEventsOther.Add(historyEvent);
                            historyEvent.Documents.AddRange(group);
                            break;
                    }

                    historyEvent.EventId = document.EventId;
                    historyEvent.EventName = document.EventName;
                    historyEvent.EventCreationUserId = document.EventCreationUserId;
                    historyEvent.EventLastChangedDT = document.EventLastChangedDT;
                    historyEvent.ResetIsDirty();
                }

                foreach (var entry in group)
                {
                    entry.EventType = eventType;
                }
            }

            m_HistoryEventGroupMail.HistoryEvents = historyEventsMail.OrderByDescending(x => x.EventLastChangedDT).ToList();
            m_Logger.DebugFormat("# of email events: {0}", m_HistoryEventGroupMail.HistoryEvents.Count);
            m_HistoryEventGroupERV.HistoryEvents = historyEventsERV.OrderByDescending(x => x.EventLastChangedDT).ToList();
            m_Logger.DebugFormat("# of erv events: {0}", m_HistoryEventGroupERV.HistoryEvents.Count);
            m_HistoryEventGroupOther.HistoryEvents = historyEventsOther.OrderByDescending(x => x.EventLastChangedDT).ToList();
            m_Logger.DebugFormat("# of other events: {0}", m_HistoryEventGroupOther.HistoryEvents.Count);
        }

        /// <summary>
        /// Führt die Suche nach Dokumenten aus.
        /// </summary>
        internal void PerformFiltering()
        {
            m_Logger.Debug("Filtering started");
            m_FilteredDocumentHistoryEntries = null;
            RaisePropertyChanged(nameof(FilteredDocumentHistoryEntries));
            RaisePropertyChanged(nameof(DocumentsForTimeline)); //TODODMS: muss bei vollständiger Implementierung des Taggings ggf. noch angepasst werden
        }

        public void SaveChanges()
        {
            var documentsToSave = m_AllDocuments.Where(x => x.IsDirty).ToList();
            var historyEventsToSave = m_HistoryEventGroupERV.HistoryEvents.Where(x => x.IsDirty).Union(m_HistoryEventGroupMail.HistoryEvents.Where(x => x.IsDirty)).Union(m_HistoryEventGroupOther.HistoryEvents.Where(x => x.IsDirty)).ToList();
            foreach (var historyEvent in historyEventsToSave)
            {
                var documentHistoryEntryToChange = documentsToSave.Where(x => x.EventId == historyEvent.EventId).ToList();
                if (documentHistoryEntryToChange.Count() == 0)
                {
                    var documentToSave = m_AllDocuments.First(x => x.EventId == historyEvent.EventId);
                    documentsToSave.Add(documentToSave);
                    documentHistoryEntryToChange.Add(documentToSave);
                }
                foreach (var documentHistoryEnty in documentHistoryEntryToChange)
                {
                    documentHistoryEnty.EventName = historyEvent.EventName;
                    documentHistoryEnty.EventCreationUserId = historyEvent.EventCreationUserId;
                    documentHistoryEnty.EventLastChangedDT = historyEvent.EventLastChangedDT;
                }
            }

            var documentsWithoutPermission = new List<DocumentHistoryEntryViewModel>();
            foreach (var document in documentsToSave)
            {
                if (!PermissionsHelper.HasUserDocumentPermission(m_PermissionService, this.UserId, document, EntityPermission.DocumentEditProperties))
                {
                    documentsWithoutPermission.Add(document);
                }
            }

            if (documentsWithoutPermission.Any())
            {
                var documentsNotExportableCount = documentsWithoutPermission.Count();
                var infoText = documentsWithoutPermission.Count() > 1 ? String.Format("Sie besitzen nicht die nötigen Berechtigungen, um {0} der ausgewählten Dokumente zu bearbeiten.", documentsNotExportableCount) : String.Format("Sie besitzen nicht die nötigen Berechtigungen um das Dokument \"{0}\" zu bearbeiten.", documentsWithoutPermission.First().DocumentDisplayName);
                m_StandardDialogService.ShowMessageInformation(infoText, "Dokumenteigenschaften bearbeiten");
                documentsToSave = documentsToSave.Where(x => !documentsWithoutPermission.Contains(x)).ToList();
            }

            if (documentsToSave.Any())
            {
                m_DocumentService.SaveDocumentProperties(documentsToSave);
            }

            documentsToSave.ForEach(x => x.ResetIsDirty());
            historyEventsToSave.ForEach(x => x.ResetIsDirty());
            if (m_MindMap != null)
            {
                EnrichMindmap(m_DocumentHistoryEntries);
            }
        }

        public bool ShowSaveChangesOnTabMessage()
        {
            if (m_AllDocuments == null || !m_AllDocuments.Any(x => x.IsDirty == true))
            {
                return false;
            }

            if (m_StandardDialogService.ShowMessageInformation("Sollen die geänderten Daten gespeichert werden?", "Datenänderungen festgestellt!", new string[] { "Ja", "Nein" }) == 1000)
            {
                return true;
            }
            return false;
        }

        #region CommandTimelineClicks

        public ICommand CommandTimelineDocumentClick { get; private set; }

        public ICommand CommandTimelineDocumentDoubleClick { get; private set; }

        private void AddCommandTimelineDocumentClick()
        {
            CommandTimelineDocumentClick = new DelegateCommand<object>(OnCommandTimelineDocumentClickExecuted);
            CommandTimelineDocumentDoubleClick = new DelegateCommand<object>(OnCommandTimelineDocumentDoubleClickExecuted);
        }

        public void OnCommandTimelineDocumentClickExecuted(object parameter)
        {
            string title = parameter as string;
            if (!String.IsNullOrWhiteSpace(title))
            {
                ActiveDocumentHistoryItemOnTimeLineTab = DocumentHistoryEntries.FirstOrDefault(x => x.DocumentDisplayName == title);
            }
        }

        public void OnCommandTimelineDocumentDoubleClickExecuted(object parameter)
        {
            OnCommandTimelineDocumentClickExecuted(parameter);
            this.OpenDocumentCommand.Execute(this);
        }

        #endregion CommandTimelineClicks

        internal void DeleteWorkingCopy(long workingCopyId)
        {
            try
            {
                var result = m_StandardDialogService.ShowMessageInformation("Möchten Sie die aktuell ausgewählte Arbeitskopie wirklich löschen?", "Arbeitskopie löschen", new string[] { "Arbeitskopie löschen", "Nein" }, 1001);
                if (result != 1000)
                {
                    return;
                }

                var workingCopyService = m_IocContainer.Resolve<IDocumentWorkingCopyService>();
                workingCopyService.DeleteWorkingCopy(workingCopyId);

                this.ActiveDocumentHistoryItemOnActiveTab.RefreshDocumentWorkingCopies();
                this.RaiseActiveDocumentHistoryItemOnActiveTabEvent();
            }
            catch (Exception exp)
            {
                m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Löschen der Arbeitskopie ist ein Fehler aufgetreten.", "Arbeitskopie löschen");
            }
        }

        public DocumentHistoryViewModel DataContext
        {
            get { return this; }
        }

        internal void RaiseActiveDocumentHistoryItemOnActiveTabEvent()
        {
            RaisePropertyChanged(nameof(ActiveDocumentHistoryItemOnActiveTab));
            RaisePropertyChanged(nameof(EventDocumentsOfActiveDocumentHistoryItemOnActiveTab));
            RaisePropertyChanged(nameof(OnlineSharesOfActiveHistoryItemOnActiveTab));
        }

        #region Commands

        private ICommand m_ManagePdfEditorWorkspacesCommand;

        public ICommand ManagePdfEditorWorkspacesCommand
        {
            get { return m_ManagePdfEditorWorkspacesCommand ?? (m_ManagePdfEditorWorkspacesCommand = new DelegateCommand(ManagePdfEditorWorkspaces)); }
        }

        private void ManagePdfEditorWorkspaces()
        {
            var window = new WindowManagePdfEditorWorkspaces
            {
                ViewModel = new ManagePdfEditorWorkspacesViewModel(PdfEditorWorkspaceInformations, IocContainer, StandardDialogService)
            };
            m_StandardDialogService.ShowDialog(window);
        }

        #endregion Commands

        internal void LoadUsersPdfEditorWorkspaces()
        {
            var pdfEditorService = m_IocContainer.Resolve<IPdfEditorService>();
            PdfEditorWorkspaceInformations = pdfEditorService.GetPdfEditorWorkspaceInformations(UserId);
        }

        private IList<IPdfEditorWorkspaceInformation> m_PdfEditorWorkspaceInformation;

        public IList<IPdfEditorWorkspaceInformation> PdfEditorWorkspaceInformations
        {
            get { return m_PdfEditorWorkspaceInformation; }
            set { SetProperty(ref m_PdfEditorWorkspaceInformation, value); }
        }

        internal void ExportFolderToDisk(FilterTreeSubItemViewModel filterTreeSubItemViewModel)
        {
            try
            {
                if (filterTreeSubItemViewModel != null)
                {
                    long folderId = 0;
                    if (filterTreeSubItemViewModel.FolderId > 0)
                    {
                        folderId = filterTreeSubItemViewModel.FolderId;
                    }
                    else if (!(String.Equals(filterTreeSubItemViewModel.Key, "All") && String.Equals(filterTreeSubItemViewModel.Name, "Alle Ordner")))
                    {
                        return;
                    }
                    System.Threading.Tasks.Task<Contracts.Commands.UICommandResult> exportDocumentsAsyncTask = ExportFolderToDiskCommand.ExportDocumentsAsync(IocContainer, StandardDialogService, CaseId, folderId);
                }
            }
            catch (Exception exp)
            {
                m_StandardDialogService.ShowMessageError("Beim Exportieren des Ordners ist ein Fehler aufgetreten.", "Ordner exportieren");
                m_Logger.Error(exp);
            }
        }
    }
}