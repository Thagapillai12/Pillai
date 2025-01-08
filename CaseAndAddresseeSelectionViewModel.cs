using DryIoc;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using WK.DE.DocumentManagement.Commands;
using WK.DE.DocumentManagement.Comparer;
using WK.DE.DocumentManagement.Contracts.Commands;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.DocumentManagement.Contracts.ViewModels;
using WK.DE.DocumentManagement.Properties;

namespace WK.DE.DocumentManagement.ViewModels.SelectionHelper
{
    public class CaseAndAddresseeSelectionViewModel : BindableBase
    {
        internal const int SPECIALFOLDER_ADRESSEEDOCUMENTATION_ID = 999999999;
        internal const string SPECIALFOLDER_ADRESSEEDOCUMENTATION_NAME = "Adressatenunterlagen";

        private readonly IContainer m_IocContainer;
        private readonly IStandardDialogService m_StandardDialogService;

        private IDocumentService m_DocumentService;

        public IDocumentService DocumentService
        {
            get { return m_DocumentService; }
        }

        public ICommand SearchAddresseeIdCommand
        { get; private set; }

        public ICommand AddCaseToLinkToCommand
        { get; private set; }

        public IConfigureDocumentFolderStructureCommand ConfigureDocumentFolderStructureCommand
        { get; private set; }

        public CaseToLinkViewModel FirstCaseToLink
        {
            get { return m_CasesToLink.FirstOrDefault(); }
        }

        private List<CaseToLinkViewModel> m_CasesToLink;

        public List<CaseToLinkViewModel> CasesToLink
        {
            get { return m_CasesToLink; }
        }

        public bool LinkToMultipleCasesInverted
        {
            get { return !LinkToMultipleCases; }
        }

        public bool LinkToMultipleCases
        {
            get { return m_CasesToLink.Count > 1; }
        }

        public bool CaseStateVisible
        {
            get
            {
                //allow null for designer support
                if (m_DocumentService == null)
                {
                    return true;
                }
                return m_DocumentService.Parameter.IsCaseStateActive && !this.LinkToMultipleCases;
            }
        }

        public string CaseNumber
        {
            get
            {
                var firstCaseToLink = this.FirstCaseToLink;
                return firstCaseToLink == null ? "" : firstCaseToLink.CaseNumber;
            }
            set
            {
                var firstCaseToLink = this.FirstCaseToLink;
                if (firstCaseToLink == null || !String.Equals(firstCaseToLink.CaseNumber, value, StringComparison.OrdinalIgnoreCase))
                {
                    if (this.DocumentService != null)
                    {
                        var @case = this.DocumentService.GetCaseBySearchText(value);
                        if (@case != null)
                        {
                            this.AddCaseToLinkTo(@case, true);
                        }
                    }
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
                    this.LinkToAddressee = AddresseeId > 0;
                    if (ViewMode != DocumentHistoryViewMode.Case)
                    {
                        this.RefreshCaseFolders(false);
                        RaisePropertyChanged(nameof(IsFolderSelectionActive));
                        RaisePropertyChanged(nameof(IsFolderEditButtonActive));
                    }
                }
            }
        }

        private bool m_LinkToAddressee;

        public bool LinkToAddressee
        {
            get { return m_LinkToAddressee; }
            set
            {
                if (SetProperty(ref m_LinkToAddressee, value))
                {
                    if (!value && AddresseeId > 0)
                    {
                        AddresseeId = 0;
                    }
                    if (value && !LinkToCases)
                    {
                        ViewMode = DocumentHistoryViewMode.AddresseeDocumentation;
                    }
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
                    if (m_MasterCaseId > 0)
                    {
                        this.MasterCase = m_DocumentService.GetMasterCaseById(m_MasterCaseId);
                        RaisePropertyChanged(nameof(MasterCase));
                    }
                    RaisePropertyChanged(nameof(LinkToMasterCase));
                }
            }
        }

        public IMasterCase MasterCase
        { get; private set; }

        public bool LinkToMasterCase
        {
            get { return m_MasterCaseId > 0; }
        }

        public string LinkToCasesHeaderName
        {
            get
            {
                var casesToLinkCount = this.CasesToLink.Count;
                if (casesToLinkCount == 0)
                {
                    return Resources.SaveDocumentViewModel_LinkCaseNumberHeaderName_NoCases;
                }
                else if (casesToLinkCount == 1)
                {
                    var firstCaseToLink = this.FirstCaseToLink;
                    return String.Format(Resources.SaveDocumentViewModel_LinkCaseNumberHeaderName_OneCaseSelected, firstCaseToLink.Subject, firstCaseToLink.Cause, firstCaseToLink.CaseNumber);
                }
                return String.Format(Resources.SaveDocumentViewModel_LinkCaseNumberHeaderName_ManyCasesSelected, casesToLinkCount);
            }
        }

        public bool m_LinkToCases;

        public bool LinkToCases
        {
            get { return m_LinkToCases; }
            set
            {
                if (SetProperty(ref m_LinkToCases, value))
                {
                    if (!value && CasesToLink.Count == 1)
                    {
                        SetCasesToLink(new List<CaseToLinkViewModel>());
                    }
                }
            }
        }

        public List<FolderToSelect> Folders
        {
            get
            {
                if (ViewMode == DocumentHistoryViewMode.AddresseeDocumentation)
                {
                    return m_AddresseeFolders;
                }
                return m_CaseFolders;
            }
            set { } //only needed for Int64ToFolderToSelectConverter, so should be empty!
        }

        private List<FolderToSelect> m_AddresseeFolders;
        private List<FolderToSelect> m_CaseFolders;

        public List<FolderToSelect> CaseFolders
        {
            get { return m_CaseFolders; }
            set { } //only needed for Int64ToFolderToSelectConverter, so should be empty!
        }

        private ObservableCollection<ParticipantToSelect> m_CaseParticipants;

        public ObservableCollection<ParticipantToSelect> CaseParticipants
        {
            get { return m_CaseParticipants; }
        }

        public bool ExpanderOnlineSharesVisible
        {
            get
            {
                //for designer support return true if no document service is available
                if (this.DocumentService == null)
                {
                    return true;
                }
                return this.DocumentService.Parameter.IsOnlineSharingActive;
            }
        }

        public bool? OnlineSharesAllRowsSelected
        {
            get
            {
                if (m_OnlineShares == null)
                {
                    return false;
                }
                var countSelected = m_OnlineShares.Count(x => x.Selected);
                if (countSelected == 0)
                {
                    return false;
                }
                if (countSelected == m_OnlineShares.Count)
                {
                    return true;
                }
                return null; //TriState
            }
            set
            {
                var newSelected = value.HasValue && value.Value;
                m_OnlineShares.ForEach(x => x.Selected = newSelected);
            }
        }

        private List<OnlineShareAccountToSelect> m_OnlineShares;

        public List<OnlineShareAccountToSelect> OnlineShares
        {
            get { return m_OnlineShares; }
            private set
            {
                var currentValue = m_OnlineShares;
                if (SetProperty(ref m_OnlineShares, value))
                {
                    if (currentValue != null)
                    {
                        currentValue.ForEach(x => x.PropertyChanged -= OnlineShares_PropertyChanged);
                    }
                    m_OnlineShares.ForEach(x => x.PropertyChanged += OnlineShares_PropertyChanged);
                    RaisePropertyChanged(nameof(OnlineSharesAvailable));
                    RaisePropertyChanged(nameof(OnlineSharesNotAvailable));
                    RaisePropertyChanged(nameof(OnlineSharesAllRowsSelected));
                }
            }
        }

        private void OnlineShares_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (String.Equals(e.PropertyName, "Selected"))
            {
                RaisePropertyChanged(nameof(OnlineSharesAllRowsSelected));
            }
        }

        public bool OnlineSharesAvailable
        {
            get { return m_OnlineShares != null && m_OnlineShares.Any(); }
        }

        public bool OnlineSharesNotAvailable
        {
            get { return !OnlineSharesAvailable; }
        }

        public CaseAndAddresseeSelectionViewModel(IContainer iocContainer, IStandardDialogService standardDialogService)
        {
            m_IocContainer = iocContainer;
            if (m_IocContainer != null)
            {
                m_DocumentService = m_IocContainer.Resolve<IDocumentService>();
            }
            m_StandardDialogService = standardDialogService;

            this.SearchAddresseeIdCommand = new DelegateCommand(this.SearchAddresseeId_Execute);
            this.AddCaseToLinkToCommand = new DelegateCommand(this.AddCaseToLinkToCommand_Execute);
            this.ConfigureDocumentFolderStructureCommand = new ConfigureDocumentFolderStructureCommand(m_IocContainer, standardDialogService);
            this.AddCaseToLinkTo(null, true);
        }

        public void RefreshCachedData()
        {
            this.RefreshCaseFolders(false);
            this.RefreshCaseParticipants();
        }

        private void SearchAddresseeId_Execute()
        {
            var extendedUIServices = m_IocContainer.Resolve<IExtendedUIServices>();
            var addresseeInfo = extendedUIServices.SearchAddresseeId(m_StandardDialogService);
            if (addresseeInfo == null)
            {
                return;
            }
            this.CaseParticipants.Add(new ParticipantToSelect(addresseeInfo.Item1, Resources.CaseAndAddresseeSelectionViewModel_Prefix_OtherParticipant + addresseeInfo.Item2));
            this.AddresseeId = addresseeInfo.Item1;
            if (ViewMode == DocumentHistoryViewMode.AddresseeDocumentation)
            {
                RefreshCaseFolders(true);
            }
        }

        private void AddCaseToLinkToCommand_Execute()
        {
            var uiServices = m_IocContainer.Resolve<IExtendedUIServices>();
            var caseId = uiServices.SearchCaseId(m_StandardDialogService);
            if (!caseId.HasValue)
            {
                return;
            }

            var @case = m_DocumentService.GetCase(caseId.Value);
            AddCaseToLinkTo(@case, false);
        }

        public void AddCaseToLinkTo(ICase @case, bool resetCurrentCasesToLink)
        {
            AddCaseToLinkTo(@case, @case != null ? @case.CaseStateId : 0, resetCurrentCasesToLink);
        }

        public void AddCaseToLinkTo(ICase @case, long caseStateId, bool resetCurrentCasesToLink)
        {
            var casesToLink = new List<CaseToLinkViewModel>();
            if (!resetCurrentCasesToLink)
            {
                casesToLink = m_CasesToLink.ToList(); //needed for databinding with XamDataGrid
            }

            //check if cases is already in list
            if (@case != null && !casesToLink.Any(x => x.Id == @case.Id))
            {
                var caseFolders = this.DocumentService.GetHistoryFoldersByIdAndViewMode(@case.Id, 0, DocumentHistoryViewMode.Case);
                casesToLink.Add(new CaseToLinkViewModel(@case, caseStateId, this.RemoveCaseToLink, caseFolders));
            }

            this.SetCasesToLink(casesToLink);
        }

        private void SetCasesToLink(List<CaseToLinkViewModel> casesToLink)
        {
            m_CasesToLink = casesToLink;
            RaisePropertyChanged("CasesToLink");
            RaisePropertyChanged("FirstCaseToLink");
            RaisePropertyChanged("CaseNumber");
            if (casesToLink.Any())
            {
                this.LinkToCases = true;
                ViewMode = DocumentHistoryViewMode.Case;
            }
            else
            {
                this.LinkToCases = false;
                if (ViewMode == DocumentHistoryViewMode.Case)
                {
                    ViewMode = DocumentHistoryViewMode.AddresseeDocumentation;
                }
            }
            RaisePropertyChanged("LinkToCasesHeaderName");
            RaisePropertyChanged("LinkToMultipleCases");
            RaisePropertyChanged("LinkToMultipleCasesInverted");
            this.RefreshCaseFolders(false);
            this.RefreshCaseParticipants();
            this.RefreshOnlineSharesFromCase();
            RaisePropertyChanged("MasterCase");
            RaisePropertyChanged(nameof(IsFolderSelectionActive));
            RaisePropertyChanged(nameof(IsFolderEditButtonActive));
        }

        private void RemoveCaseToLink(long caseId)
        {
            var caseToSelect = m_CasesToLink.FirstOrDefault(x => x.Id == caseId);
            if (caseToSelect != null)
            {
                var casesToLink = this.CasesToLink.ToList(); //needed for databinding with XamDataGrid
                casesToLink.Remove(caseToSelect);
                SetCasesToLink(casesToLink);
            }
        }

        internal void RefreshCaseFolders(bool forceReloadCachedFoldersOnCases)
        {
            var caseFolders = new List<FolderToSelect>();
            var addresseeFolders = new List<FolderToSelect>();
            if (!m_CasesToLink.Any())
            {
                if (ViewMode == DocumentHistoryViewMode.AddresseeDocumentation)
                {
                    if (AddresseeId > 0)
                    {
                        var currentAddresseeFolders = this.DocumentService.GetHistoryFoldersByIdAndViewMode(0, AddresseeId, DocumentHistoryViewMode.AddresseeDocumentation);
                        FolderToSelect.CreateFolders(addresseeFolders, currentAddresseeFolders, 1, "");
                        if (addresseeFolders.Count(x => String.Equals(x.Name, SPECIALFOLDER_ADRESSEEDOCUMENTATION_NAME)) == 0)
                        {
                            addresseeFolders.Insert(0, new FolderToSelect(SPECIALFOLDER_ADRESSEEDOCUMENTATION_ID, SPECIALFOLDER_ADRESSEEDOCUMENTATION_NAME, null));
                        }
                        addresseeFolders.Insert(0, new FolderToSelect(0, "E-Akte", null));
                    }
                    else
                    {
                        addresseeFolders.Add(new FolderToSelect(0, "", null));
                    }
                }
                else
                {
                    caseFolders.Add(new FolderToSelect(0, "", null));
                }
            }
            else
            {
                caseFolders.Add(new FolderToSelect(0, Resources.Global_DocumentMainNodeName, null));

                if (forceReloadCachedFoldersOnCases)
                {
                    foreach (var caseToLink in m_CasesToLink)
                    {
                        var currentCaseFolders = this.DocumentService.GetHistoryFoldersByIdAndViewMode(caseToLink.Id, 0, DocumentHistoryViewMode.Case);
                        caseToLink.RefreshCachedFolders(currentCaseFolders);
                    }
                }

                //get all folders which are same in all cases
                var folders = m_CasesToLink.First().Folders;
                foreach (var caseToLink in m_CasesToLink.Skip(1))
                {
                    folders = folders.Intersect(caseToLink.Folders, new DocumentHistoryFolderEqualityByNameComparer()).ToList();
                }
                FolderToSelect.CreateFolders(caseFolders, folders, 1, "");
            }
            m_CaseFolders = caseFolders;
            m_AddresseeFolders = addresseeFolders;

            RaisePropertyChanged(nameof(CaseFolders));
            RaisePropertyChanged(nameof(Folders));
            SelectFolderId = 0;
        }

        private void RefreshCaseParticipants()
        {
            var caseParticipants = new List<ParticipantToSelect>();
            if (!m_CasesToLink.Any())
            {
                caseParticipants.Add(new ParticipantToSelect(0, ""));
                AddOtherParticipant(caseParticipants);
            }
            else
            {
                //get all participants which are same in all cases
                var participants = m_CasesToLink.First().Participants;
                foreach (var caseToLink in m_CasesToLink.Skip(1))
                {
                    participants = participants.Intersect(caseToLink.Participants, new CaseParticipantEqualityByAddresseeIdComparer()).ToList();
                }
                caseParticipants.Add(new ParticipantToSelect(0, ""));
                if (participants != null)
                {
                    caseParticipants.AddRange(participants.Select(x => new ParticipantToSelect(x)));
                }
                AddOtherParticipant(caseParticipants);
            }

            m_CaseParticipants = new ObservableCollection<ParticipantToSelect>(caseParticipants);

            RaisePropertyChanged(nameof(CaseParticipants));
            RaisePropertyChanged(nameof(AddresseeId)); //without this PropertyChange ComboBox for Adressee is showing an error
        }

        private void AddOtherParticipant(List<ParticipantToSelect> caseParticipants)
        {
            // participant which is stored to an entry (can be any addressee) should be always in the List (otherwise a number is displayed in the ComboBox instead of the Display-Name)
            var caseParticipant = caseParticipants.Where(x => x.AddresseeId == AddresseeId);
            if (!caseParticipant.Any())
            {
                var anotherAdressee = m_DocumentService.GetParticipant(AddresseeId);
                if (anotherAdressee != null)
                {
                    var participantToSelect = new ParticipantToSelect(anotherAdressee);
                    participantToSelect.Name = Resources.CaseAndAddresseeSelectionViewModel_Prefix_OtherParticipant + participantToSelect.Name;
                    caseParticipants.Add(participantToSelect);
                }
            }
        }

        /// <summary>
        /// Refreshes the selectable online shares from information of the selected case.
        /// </summary>
        private void RefreshOnlineSharesFromCase()
        {
            var onlineShares = new List<OnlineShareAccountToSelect>();
            var firstCaseToLink = this.FirstCaseToLink;
            if (firstCaseToLink != null)
            {
                var onlineShareAccounts = this.DocumentService.GetOnlineShareAccountsByCaseId(firstCaseToLink.Case.Id);
                if (onlineShareAccounts != null)
                {
                    onlineShares.AddRange(onlineShareAccounts.Select(x => new OnlineShareAccountToSelect(x)));
                }
            }
            this.OnlineShares = onlineShares;
        }

        internal void UpdateCaseAndAdresseeInformation(DocumentsToAddInformation documentsToAddInformation)
        {
            if (this.CasesToLink.Any())
            {
                documentsToAddInformation.CasesToLink = this.CasesToLink.Select(x => new CaseToLinkInformation(x.Case, x.CaseStateId)).ToList();
            }
            else
            {
                documentsToAddInformation.CasesToLink?.Clear();
            }
            if (this.LinkToAddressee)
            {
                documentsToAddInformation.AddresseeId = this.AddresseeId;
            }
            if (this.LinkToMasterCase)
            {
                documentsToAddInformation.MasterCaseId = this.MasterCaseId;
            }
        }

        public void SelectParticipantByEMail(string emailAddress)
        {
            if (m_CaseParticipants == null || !m_CaseParticipants.Any())
            {
                return;
            }

            var participant = m_CaseParticipants.FirstOrDefault(x => x.EMailAddresses != null && x.EMailAddresses.Contains(emailAddress, StringComparer.OrdinalIgnoreCase));
            if (participant == null)
            {
                return;
            }

            this.AddresseeId = participant.AddresseeId;
        }

        public bool IsFolderSelectionActive
        {
            get
            {
                return (CasesToLink != null && CasesToLink.Count > 0) || AddresseeId > 0;
            }
        }

        public bool IsFolderEditButtonActive
        {
            get
            {
                return (CasesToLink != null && CasesToLink.Count == 1) || AddresseeId > 0;
            }
        }

        public string CaseNumbers
        {
            get
            {
                return string.Join(",", CasesToLink.Select((x) => x.CaseNumber));
            }
        }

        private DocumentHistoryViewMode m_ViewMode = DocumentHistoryViewMode.None;

        public DocumentHistoryViewMode ViewMode
        {
            get
            {
                if (m_ViewMode == DocumentHistoryViewMode.None)
                {
                    if (FirstCaseToLink != null && FirstCaseToLink.Id > 0)
                    {
                        if (AddresseeId > 0)
                        {
                            m_ViewMode = DocumentHistoryViewMode.Participant;
                        }
                        else
                        {
                            m_ViewMode = DocumentHistoryViewMode.Case;
                        }
                    }
                    else if (AddresseeId > 0)
                    {
                        m_ViewMode = DocumentHistoryViewMode.AddresseeDocumentation;
                    }
                }
                return m_ViewMode;
            }
            set
            {
                if (value != m_ViewMode)
                {
                    m_ViewMode = value;
                    RefreshCaseFolders(true);
                    RaisePropertyChanged(nameof(ViewMode));
                }
            }
        }

        public bool SelectFolderOnConfigureDocumentFolderStructure
        { get; internal set; }

        private long m_SelectFolderId;

        public long SelectFolderId
        {
            get { return m_SelectFolderId; }
            internal set
            {
                SetProperty(ref m_SelectFolderId, value);
            }
        }
    }
}