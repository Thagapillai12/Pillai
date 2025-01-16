using DryIoc;
using log4net;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using WK.DE.DocumentManagement.Contracts.Commands;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.DocumentManagement.Contracts.ViewModels;
using WK.DE.DocumentManagement.Contracts.ViewModels.ToDos;
using WK.DE.DocumentManagement.Helper;
using WK.DE.DocumentManagement.Services;
using WK.DE.UI.Preview.ViewModel;
using WK.DE.UI.WinForms.Controls.ViewModel;

namespace WK.DE.DocumentManagement.ViewModels.ToDos
{
	public partial class ToDosViewModel : ViewModelWithFilterTreeBase, ICommandExecutionStatusVisualizer
	{
		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(ToDosViewModel));

		public enum ToDoListViewMode
		{
			None,
			ToDosForUser,
			MonitoredToDosForUser,
			ToDosForCase,
			ToDosForAddressee,
			ToDosForOffice,
			AdvoAssistToDosForOffice,
			ToDosForResources,
		}

		public DateTime? PresetForNewItem_StartDate
		{ get; set; }

		public DateTime? PresetForNewItem_EndDate
		{ get; set; }

		private Tuple<int?, bool, bool> m_ToDoListOption = null;

		public Tuple<int?, bool, bool> ToDoListOption
		{
			get
			{
				return m_ToDoListOption;
			}
			set
			{
				m_ToDoListOption = value;
				RaisePropertyChanged(nameof(ToDoListOption)); // SetProperty(ref m_ToDoListOption, value); ergibt immer false
				ShowCommentsInExtendedField = m_ToDoListOption.Item3;
			}
		}

		private IDictionary<string, Tuple<int?, bool, bool>> m_ToDoListOptions = null;

		internal IDictionary<string, Tuple<int?, bool, bool>> ToDoListOptions
		{
			get { return m_ToDoListOptions; }
		}

		private readonly ToDosOfCaseViewModel m_ToDosOfCase;

		public ToDosOfCaseViewModel ToDosOfCase
		{
			get { return m_ToDosOfCase; }
		}

		private bool m_ActiveToDoListEntryChangeSuspended;

		public ToDoListEntryViewModel ActiveToDoListEntry
		{
			get
			{
				switch (m_ActiveTab)
				{
					case ToDoTabs.ToDo: return m_ActiveToDoListEntryOnToDoTab;
					case ToDoTabs.Calendar: return m_ActiveToDoListEntryOnCalendarTab;
				}
				return null;
			}
		}

		private ToDoTabs m_ActiveTab;

		public ToDoTabs ActiveTab
		{
			get { return m_ActiveTab; }
			set
			{
				SetProperty(ref m_ActiveTab, value);
			}
		}

		private ToDoListEntryViewModel m_ActiveToDoListEntryOnCalendarTab;

		public ToDoListEntryViewModel ActiveToDoListEntryOnCalendarTab
		{
			get { return m_ActiveToDoListEntryOnCalendarTab; }
			set
			{
				if (SetProperty(ref m_ActiveToDoListEntryOnCalendarTab, value))
				{
					RefreshCanExecuteForCommands();
					m_ToDosOfCase.MainToDoEntry = m_ActiveToDoListEntryOnCalendarTab;
					RaisePropertyChanged(nameof(ViewTitleDocuments));
				}
			}
		}

		private ToDoListEntryViewModel m_ActiveToDoListEntryOnToDoTab;

		public ToDoListEntryViewModel ActiveToDoListEntryOnToDoTab
		{
			get { return m_ActiveToDoListEntryOnToDoTab; }
			set
			{
				if (m_ActiveToDoListEntryChangeSuspended)
				{
					return;
				}

				if (value == null || m_ActiveToDoListEntryOnToDoTab == null
						|| value.Id != m_ActiveToDoListEntryOnToDoTab.Id)
				{
					m_ActiveToDoListEntryOnToDoTab = value;
					RaisePropertyChanged();

					//if case view, we do not need to load todos for case, not visible in this mode
					if (this.ViewMode != ToDoListViewMode.ToDosForCase)
					{
						m_ToDosOfCase.MainToDoEntry = m_ActiveToDoListEntryOnToDoTab;
						RaisePropertyChanged(nameof(ViewTitleDocuments));
					}
				}
			}
		}

		private DocumentHistoryEntryViewModel m_ActiveDocumentOfToDo;

		public DocumentHistoryEntryViewModel ActiveDocumentOfToDo
		{
			get { return m_ActiveDocumentOfToDo; }
			set
			{
				if (value == null || m_ActiveDocumentOfToDo == null
						|| value.DocumentId != m_ActiveDocumentOfToDo.DocumentId)
				{
					m_ActiveDocumentOfToDo = value;
					RaisePropertyChanged();
					RaisePropertyChanged(nameof(OnlineSharesOfActiveDocumentOfToDo));
					this.RefreshCanExecuteForDocumentCommands();
					this.ToDosOfDocument.RefreshToDosAsync();
				}
			}
		}

		public List<IOnlineShareInformation> OnlineSharesOfActiveDocumentOfToDo
		{
			get
			{
				var activeDocumentOfToDo = this.ActiveDocumentOfToDo;
				if (activeDocumentOfToDo == null)
				{
					return new List<IOnlineShareInformation>();
				}

				var onlineShareAccounts = GetCaseOnlineShareAccounts(activeDocumentOfToDo.DocumentCaseId);

				return m_DocumentService.GetOnlineSharesByDocumentId(onlineShareAccounts, activeDocumentOfToDo.DocumentId);
			}
		}

		private IOnlineShareInformation[] m_SelectedOnlineSharesOfActiveDocumentOfToDo;

		public object[] SelectedOnlineSharesOfActiveDocumentOfToDoForBinding
		{
			get { return m_SelectedOnlineSharesOfActiveDocumentOfToDo; }
			set
			{
				if (value == null)
				{
					if (SetProperty(ref m_SelectedOnlineSharesOfActiveDocumentOfToDo, new IOnlineShareInformation[0]))
					{
						this.RefreshCanExecuteForDocumentCommands();
					}
				}
				else
				{
					if (SetProperty(ref m_SelectedOnlineSharesOfActiveDocumentOfToDo, value.OfType<IOnlineShareInformation>().ToArray()))
					{
						this.RefreshCanExecuteForDocumentCommands();
					}
				}
			}
		}

		private CalendarViewMode m_CalendarViewMode;

		public CalendarViewMode CalendarViewMode
		{
			get { return m_CalendarViewMode; }
			set
			{
				if (SetProperty(ref m_CalendarViewMode, value))
				{
					RaisePropertyChanged(nameof(IsCalendarViewActive));
				}
			}
		}

		public bool IsCalendarViewActive
		{
			get { return m_CalendarViewMode != CalendarViewMode.None; }
		}

		private List<IOnlineShareAccount> GetCaseOnlineShareAccounts(long caseID)
		{
			return m_DocumentService.GetOnlineShareAccountsByCaseId(caseID);
		}

		private string m_BusyDescription;

		public string BusyDescription
		{
			get { return m_BusyDescription ?? "Bitte warten..."; }
			set { SetProperty(ref m_BusyDescription, value); }
		}

		private bool m_IsBusy;

		public bool IsBusy
		{
			get { return m_IsBusy; }
			set
			{
				if (SetProperty(ref m_IsBusy, value))
				{
					RaisePropertyChanged(nameof(IsNotBusy));
					if (IsNotBusy)
					{
						this.BusyDescription = null;
					}
				}
			}
		}

		public bool IsNotBusy
		{
			get { return !this.IsBusy; }
		}

		public string ViewTitle
		{
			get { return m_FilteredToDoListEntries == null ? "ToDos" : String.Format("ToDos ({0})", m_FilteredToDoListEntries.Count); }
		}

		public string ViewTitleCalendar
		{
			get { return "Kalender"; }
		}

		public string ViewTitleDocuments
		{
			get { return m_ActiveToDoListEntryOnToDoTab == null ? "verknüpfte Dokumente" : String.Format("verknüpfte Dokumente ({0})", m_ActiveToDoListEntryOnToDoTab.DocumentsOfToDo.Count); }
		}

		public override bool ShowWorkingCopies
		{
			get { return true; }
		}

		public bool AllToDoTypesAvailable
		{ get; private set; }

		public bool IsAdvoAssistActive
		{ get; private set; }

		private bool m_ShowAdvoAssistMenuItems;

		public bool ShowAdvoAssistMenuItems
		{
			get { return m_ShowAdvoAssistMenuItems; }
			set { SetProperty(ref m_ShowAdvoAssistMenuItems, value); }
		}

		private ToDoListEntryViewModel[] m_SelectedToDoListEntriesForBinding;

		public object[] SelectedToDoListEntriesForBinding
		{
			get { return m_SelectedToDoListEntriesForBinding; }
			set
			{
				if (value == null)
				{
					if (SetProperty(ref m_SelectedToDoListEntriesForBinding, new ToDoListEntryViewModel[0]))
					{
						this.RefreshCanExecuteForCommands();
						this.RefreshAdvoAssistMenuItemVisibility();
					}
				}
				else
				{
					if (SetProperty(ref m_SelectedToDoListEntriesForBinding, value.OfType<ToDoListEntryViewModel>().ToArray()))
					{
						this.RefreshCanExecuteForCommands();
						this.RefreshAdvoAssistMenuItemVisibility();

						var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
						if (firstSelectedToDo != null)
						{
							this.ActiveDocumentOfToDo = firstSelectedToDo.DocumentsOfToDo.FirstOrDefault();
						}
					}
				}
			}
		}

		private List<KeyValuePair<ToDoColorSettingType, Color>> m_CurrentUserCalendarColorSettings;

		public List<KeyValuePair<ToDoColorSettingType, Color>> CurrentUserCalendarColorSettings
		{
			get
			{
				if (m_CurrentUserCalendarColorSettings == null)
				{
					m_CurrentUserCalendarColorSettings = new List<KeyValuePair<ToDoColorSettingType, Color>>();

					var currentUserInformationService = m_IocContainer.Resolve<ICurrentUserInformationService>();
					if (currentUserInformationService != null)
					{
						var currentUser = currentUserInformationService.UserId;
						m_CurrentUserCalendarColorSettings = m_ToDoService.GetToDoColorSettings(currentUser);
					}
				}

				return m_CurrentUserCalendarColorSettings;
			}
		}

		private void RefreshAdvoAssistMenuItemVisibility()
		{
			if (this.IsAdvoAssistActive && m_SelectedToDoListEntriesForBinding != null && m_SelectedToDoListEntriesForBinding.All(x => x.Type == ToDoType.Appointment))
			{
				ShowAdvoAssistMenuItems = true;
				return;
			}
			ShowAdvoAssistMenuItems = false;
		}

		private List<ToDoListEntryViewModel> m_FilteredToDoListEntries;

		public IEnumerable<ToDoListEntryViewModel> FilteredToDoListEntries
		{
			get { return m_FilteredToDoListEntries == null ? null : m_FilteredToDoListEntries.ToList(); } //.ToList() wichtig für DataBinding mit XamDataGrid
			private set
			{
				var list = value as List<ToDoListEntryViewModel>;
				if (list == null && value != null)
				{
					list = value.ToList();
				}
				if (SetProperty(ref m_FilteredToDoListEntries, list))
				{
					RaisePropertyChanged(nameof(IsListEmpty));
					RaisePropertyChanged(nameof(ViewTitle));
				}
			}
		}

		private bool m_IsToDoMainGroupingVisible;

		public bool IsToDoMainGroupingVisible
		{
			get { return m_IsToDoMainGroupingVisible; }
			set { SetProperty(ref m_IsToDoMainGroupingVisible, value); }
		}

		private bool m_IsToDoLinkedGroupingVisible;

		public bool IsToDoLinkedGroupingVisible
		{
			get { return m_IsToDoLinkedGroupingVisible; }
			set { SetProperty(ref m_IsToDoLinkedGroupingVisible, value); }
		}

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
			get { return !m_IsLoadingDataInitially && (m_FilteredToDoListEntries == null || !m_FilteredToDoListEntries.Any()); }
		}

		private ToDoListViewMode m_ViewMode;

		public ToDoListViewMode ViewMode
		{
			get { return m_ViewMode; }
			set
			{
				if (SetProperty(ref m_ViewMode, value))
				{
					if (m_ToDoListOptions == null)
					{
						var extendedUIServices = m_IocContainer.Resolve<IExtendedUIServices>();
						m_ToDoListOptions = extendedUIServices.GetToDoListsOptions();
					}

					if (m_ToDoListOptions == null)
					{
						ToDoListOption = new Tuple<int?, bool, bool>(null, false, false);
					}
					else
					{
						switch (m_ViewMode)
						{
							case ToDoListViewMode.ToDosForCase:
								ToDoListOption = m_ToDoListOptions["ReportCase"];
								break;

							case ToDoListViewMode.ToDosForAddressee:
								ToDoListOption = m_ToDoListOptions["ReportAddressee"];
								break;

							case ToDoListViewMode.ToDosForUser:
							case ToDoListViewMode.MonitoredToDosForUser:
							case ToDoListViewMode.None:
								ToDoListOption = m_ToDoListOptions["ReportToDo"];
								break;

							case ToDoListViewMode.ToDosForOffice:
							case ToDoListViewMode.AdvoAssistToDosForOffice:
							case ToDoListViewMode.ToDosForResources:
								ToDoListOption = m_ToDoListOptions["ReportOffice"];
								break;

							default:
								break;
						}
					}

					if (m_ViewMode == ToDoListViewMode.ToDosForResources)
					{
						IsPreviewAndDetailsSectionShownInSeparateWindow = true;
					}
					else
					{
						IsPreviewAndDetailsSectionShownInSeparateWindow = false;
					}

					this.FilteredToDoListEntries = null;
					RaisePropertyChanged(nameof(AreMonitorColumnsVisible));
					RaisePropertyChanged(nameof(IsFilterByStateAvailable));
					RaisePropertyChanged(nameof(IsFilterByAppointmentSubTypeAvailable));
					RaisePropertyChanged(nameof(IsFilterByDeadlineSubTypeAvailable));
					RaisePropertyChanged(nameof(IsFilterByDebtCollectionSubTypeAvailable));
					RaisePropertyChanged(nameof(IsOpenMainEntityAvailable));
					RaisePropertyChanged(nameof(IsToDosOfCaseViewActive));
					RaisePropertyChanged(nameof(IsAvailableShowDebtCollectionBatchToDos));
					this.RefreshToDosAsync();
				}
			}
		}

		private DateTime m_FilterDateTo;

		public DateTime FilterDateTo
		{
			get { return m_FilterDateTo; }
			set
			{
				var newDate = value.Date;
				if (SetProperty(ref m_FilterDateTo, newDate))
				{
					this.RefreshToDosAsync();
				}
			}
		}

		private DateTime? m_FilterDateFrom;

		public DateTime? FilterDateFrom
		{
			get { return m_FilterDateFrom; }
			set
			{
				var newDate = value.HasValue ? value.Value.Date : (DateTime?)null;
				if (SetProperty(ref m_FilterDateFrom, newDate))
				{
					this.RefreshToDosAsync();
				}
			}
		}

		private ToDoType? m_FilterToDoType;

		public ToDoType? FilterToDoType
		{
			get { return m_FilterToDoType; }
			set
			{
				try
				{
					if (SetProperty(ref m_FilterToDoType, value))
					{
						m_Logger.DebugFormat("FilterToDoType changed to {0}", this.FilterToDoType);
						m_FilteredToDoListEntries = null;
						this.RaisePropertyChangedAfterFilterToDoTypeChanged();
						this.RefreshToDosAsync();
					}
				}
				catch (Exception exp)
				{
					m_Logger.Error(exp);
					throw;
				}
			}
		}

		private ToDoSubType? m_FilterToDoSubType;

		public ToDoSubType? FilterToDoSubType
		{
			get { return m_FilterToDoSubType; }
			set
			{
				try
				{
					if (SetProperty(ref m_FilterToDoSubType, value))
					{
						m_Logger.DebugFormat("FilterToDoSubType changed to {0}", this.FilterToDoSubType);
						m_FilteredToDoListEntries = null;
						RaisePropertyChanged(nameof(FilteredToDoListEntries));
						this.RefreshToDosAsync();
					}
				}
				catch (Exception exp)
				{
					m_Logger.Error(exp);
					throw;
				}
			}
		}

		/// <summary>
		/// Gets a list of ids of user groups to which the current user is assigned.
		/// </summary>
		private List<long> m_AssignedToDoUserGroupIds;

		private long m_FilterAddresseeId;

		/// <summary>
		/// Gets or sets the Id of the addressee which ToDos are listed (only applicable in ToDoListViewMode.ToDosForAddressee).
		/// </summary>
		public long FilterAddresseeId
		{
			get { return m_FilterAddresseeId; }
			set
			{
				if (SetProperty(ref m_FilterAddresseeId, value))
				{
					this.FilteredToDoListEntries = null;
					this.RefreshToDosAsync();
				}
			}
		}

		private long m_FilterCaseId;

		/// <summary>
		/// Gets or sets the Id of the case which ToDos are listed (only applicable in ToDoListViewMode.ToDosForCase).
		/// </summary>
		public long FilterCaseId
		{
			get { return m_FilterCaseId; }
			set
			{
				if (SetProperty(ref m_FilterCaseId, value))
				{
					this.FilteredToDoListEntries = null;
					this.RefreshToDosAsync();
				}
			}
		}

		private string m_FilterUserId;

		/// <summary>
		/// Gets or sets the Id of the user whoms ToDos are listed (only applicable in ToDoListViewMode.ToDosForUser).
		/// </summary>
		public string FilterUserId
		{
			get { return m_FilterUserId; }
			set
			{
				if (SetProperty(ref m_FilterUserId, value))
				{
					m_AssignedToDoUserGroupIds = null;
					this.FilteredToDoListEntries = null;
					this.RefreshToDosAsync();
				}
			}
		}

		private long m_FilterResourceId;

		/// <summary>
		/// Gets or sets the Id of the resource for wich ToDos are listed (only applicable in ToDoListViewMode.ToDosForResource).
		/// </summary>
		public long FilterResourceId
		{
			get { return m_FilterResourceId; }
			set
			{
				if (SetProperty(ref m_FilterResourceId, value))
				{
					this.FilteredToDoListEntries = null;
					this.RefreshToDosAsync();
				}
			}
		}

		private int? m_FilterInboxType;

		public int? FilterInboxType
		{
			get { return m_FilterInboxType; }
			set
			{
				if (SetProperty(ref m_FilterInboxType, value))
				{
					this.FilteredToDoListEntries = null;
					this.RefreshToDosAsync();
				}
			}
		}

		public ToDoStateFilterViewModel ToDoStateFilter
		{ get; private set; }

		private AppointmentSubTypes m_FilterAppointmentSubType;

		/// <summary>
		/// Gets or sets the state for the filter of the view.
		/// </summary>
		public AppointmentSubTypes FilterAppointmentSubType
		{
			get { return m_FilterAppointmentSubType; }
			set
			{
				if (SetProperty(ref m_FilterAppointmentSubType, value))
				{
					RaisePropertyChanged(nameof(IsFilterAppointmentSubTypeAll));
					RaisePropertyChanged(nameof(IsFilterAppointmentSubTypeCourt));
					RaisePropertyChanged(nameof(IsFilterAppointmentSubTypeMeeting));
					RaisePropertyChanged(nameof(IsFilterAppointmentSubTypeOther));
					this.RefreshToDosAsync();
				}
			}
		}

		public bool IsFilterAppointmentSubTypeAll
		{
			get { return m_FilterAppointmentSubType == AppointmentSubTypes.All; }
		}

		public bool IsFilterAppointmentSubTypeCourt
		{
			get { return (m_FilterAppointmentSubType & AppointmentSubTypes.Court) == AppointmentSubTypes.Court; }
		}

		public bool IsFilterAppointmentSubTypeMeeting
		{
			get { return (m_FilterAppointmentSubType & AppointmentSubTypes.Meeting) == AppointmentSubTypes.Meeting; }
		}

		public bool IsFilterAppointmentSubTypeOther
		{
			get { return (m_FilterAppointmentSubType & AppointmentSubTypes.Other) == AppointmentSubTypes.Other; }
		}

		private void ToggleFilterAppointmentSubType(AppointmentSubTypes appointmentSubType)
		{
			if (appointmentSubType == AppointmentSubTypes.All)
			{
				if (m_FilterAppointmentSubType != AppointmentSubTypes.All)
				{
					this.FilterAppointmentSubType = AppointmentSubTypes.All;
				}
				else
				{
					this.FilterAppointmentSubType = AppointmentSubTypes.None;
				}
			}
			else
			{
				if ((m_FilterAppointmentSubType & appointmentSubType) == appointmentSubType)
				{
					this.FilterAppointmentSubType ^= appointmentSubType;
				}
				else
				{
					this.FilterAppointmentSubType |= appointmentSubType;
				}
			}
		}

		private DeadlineSubTypes m_FilterDeadlineSubType;

		/// <summary>
		/// Gets or sets the state for the filter of the view.
		/// </summary>
		public DeadlineSubTypes FilterDeadlineSubType
		{
			get { return m_FilterDeadlineSubType; }
			set
			{
				if (SetProperty(ref m_FilterDeadlineSubType, value))
				{
					RaisePropertyChanged(nameof(IsFilterDeadlineSubTypeAll));
					RaisePropertyChanged(nameof(IsFilterDeadlineSubTypeMain));
					RaisePropertyChanged(nameof(IsFilterDeadlineSubTypeFirstReminder));
					RaisePropertyChanged(nameof(IsFilterDeadlineSubTypeSecondReminder));
					this.RefreshToDosAsync();
				}
			}
		}

		public bool IsFilterDeadlineSubTypeAll
		{
			get { return m_FilterDeadlineSubType == DeadlineSubTypes.All; }
		}

		public bool IsFilterDeadlineSubTypeMain
		{
			get { return (m_FilterDeadlineSubType & DeadlineSubTypes.Main) == DeadlineSubTypes.Main; }
		}

		public bool IsFilterDeadlineSubTypeFirstReminder
		{
			get { return (m_FilterDeadlineSubType & DeadlineSubTypes.FirstReminder) == DeadlineSubTypes.FirstReminder; }
		}

		public bool IsFilterDeadlineSubTypeSecondReminder
		{
			get { return (m_FilterDeadlineSubType & DeadlineSubTypes.SecondReminder) == DeadlineSubTypes.SecondReminder; }
		}

		private void ToggleFilterDeadlineSubType(DeadlineSubTypes deadlineSubType)
		{
			if (deadlineSubType == DeadlineSubTypes.All)
			{
				if (m_FilterDeadlineSubType != DeadlineSubTypes.All)
				{
					this.FilterDeadlineSubType = DeadlineSubTypes.All;
				}
				else
				{
					this.FilterDeadlineSubType = DeadlineSubTypes.None;
				}
			}
			else
			{
				if ((m_FilterDeadlineSubType & deadlineSubType) == deadlineSubType)
				{
					this.FilterDeadlineSubType ^= deadlineSubType;
				}
				else
				{
					this.FilterDeadlineSubType |= deadlineSubType;
				}
			}
		}

		private DebtCollectionSubTypes m_FilterDebtCollectionSubType;

		/// <summary>
		/// Gets or sets the state for the filter of the view.
		/// </summary>
		public DebtCollectionSubTypes FilterDebtCollectionSubType
		{
			get { return m_FilterDebtCollectionSubType; }
			set
			{
				if (SetProperty(ref m_FilterDebtCollectionSubType, value))
				{
					RaisePropertyChanged(nameof(IsFilterDebtCollectionSubTypeAll));
					RaisePropertyChanged(nameof(IsFilterDebtCollectionSubTypeController));
					RaisePropertyChanged(nameof(IsFilterDebtCollectionSubTypeImport));
					RaisePropertyChanged(nameof(IsFilterDebtCollectionSubTypeMeasure));
					RaisePropertyChanged(nameof(IsFilterDebtCollectionSubTypeCourtComplaint));
					this.RefreshToDosAsync();
				}
			}
		}

		public bool IsFilterDebtCollectionSubTypeAll
		{
			get { return m_FilterDebtCollectionSubType == DebtCollectionSubTypes.All; }
		}

		public bool IsFilterDebtCollectionSubTypeController
		{
			get { return (m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Controller) == DebtCollectionSubTypes.Controller; }
		}

		public bool IsFilterDebtCollectionSubTypeImport
		{
			get { return (m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Import) == DebtCollectionSubTypes.Import; }
		}

		public bool IsFilterDebtCollectionSubTypeMeasure
		{
			get { return (m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Measure) == DebtCollectionSubTypes.Measure; }
		}

		public bool IsFilterDebtCollectionSubTypeCourtComplaint
		{
			get { return (m_FilterDebtCollectionSubType & DebtCollectionSubTypes.CourtComplaint) == DebtCollectionSubTypes.CourtComplaint; }
		}

		private void ToggleFilterDebtCollectionSubType(DebtCollectionSubTypes debtCollectionSubType)
		{
			if (debtCollectionSubType == DebtCollectionSubTypes.All)
			{
				if (m_FilterDebtCollectionSubType != DebtCollectionSubTypes.All)
				{
					this.FilterDebtCollectionSubType = DebtCollectionSubTypes.All;
				}
				else
				{
					this.FilterDebtCollectionSubType = DebtCollectionSubTypes.None;
				}
			}
			else
			{
				if ((m_FilterDebtCollectionSubType & debtCollectionSubType) == debtCollectionSubType)
				{
					this.FilterDebtCollectionSubType ^= debtCollectionSubType;
				}
				else
				{
					this.FilterDebtCollectionSubType |= debtCollectionSubType;
				}
			}
		}

		public bool IsFilingCaseActive
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
					m_Logger.Error("documentservice does not provide parameters, online sharing will not be visible");
				}
				return m_DocumentService.Parameter.IsFilingCaseActive;
			}
		}

		public bool IsOnlineSharingActive
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
					m_Logger.Error("documentservice does not provide parameters, online sharing will not be visible");
				}
				return m_DocumentService.Parameter.IsOnlineSharingActive;
			}
		}

		public bool IsToDosOfCaseViewActive
		{
			get
			{
				//in case view never show the ToDos of the case, does not make sense
				if (this.ViewMode == ToDoListViewMode.ToDosForCase)
				{
					return false;
				}

				if (this.ViewMode == ToDoListViewMode.ToDosForResources)
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
				return m_DocumentService.Parameter.IsToDosOfCaseViewActive;
			}
		}

		public bool IsFilterByAppointmentSubTypeAvailable
		{
			get { return this.FilterToDoType == ToDoType.Appointment; }
		}

		public bool IsFilterByDeadlineSubTypeAvailable
		{
			get { return this.FilterToDoType == ToDoType.Deadline; }
		}

		public bool IsFilterByDebtCollectionSubTypeAvailable
		{
			get { return this.FilterToDoType == ToDoType.DebtCollection; }
		}

		public bool IsOpenMainEntityAvailable
		{
			get { return this.ViewMode != ToDoListViewMode.ToDosForCase; }
		}

		private string m_ErrorMessageWhileLoadingToDos;

		/// <summary>
		/// Ruft einen ggf. beim Laden der ToDos aufgetretenen Fehler ab.
		/// </summary>
		public string ErrorMessageWhileLoadingToDos
		{
			get { return m_ErrorMessageWhileLoadingToDos; }
			private set
			{
				if (SetProperty(ref m_ErrorMessageWhileLoadingToDos, value))
				{
					RaisePropertyChanged(nameof(ShowErrorMessageWhileLoadingToDos));
				}
			}
		}

		private bool m_ShowDebtCollectionBatchToDos;

		public bool ShowDebtCollectionBatchToDos
		{
			get { return m_ShowDebtCollectionBatchToDos; }
			set
			{
				if (SetProperty(ref m_ShowDebtCollectionBatchToDos, value))
				{
					this.RefreshToDosAsync();
				}
			}
		}

		public bool IsAvailableShowDebtCollectionBatchToDos
		{
			get
			{
				return this.AllToDoTypesAvailable
					&& ((this.ViewMode == ToDoListViewMode.ToDosForUser && !m_FilterToDoType.HasValue)
					|| (m_FilterToDoType.HasValue && m_FilterToDoType.Value == ToDoType.DebtCollection))
					;
			}
		}

		internal bool? ExcludeDebtCollectionBatchToDos
		{
			get
			{
				if (!this.IsAvailableShowDebtCollectionBatchToDos)
				{
					return null;
				}
				return !this.ShowDebtCollectionBatchToDos;
			}
		}

		public bool ShowErrorMessageWhileLoadingToDos
		{
			get { return !String.IsNullOrWhiteSpace(m_ErrorMessageWhileLoadingToDos); }
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

		public bool IsFilterByStateAvailable
		{
			get { return this.ViewMode != ToDoListViewMode.MonitoredToDosForUser; }
		}

		private bool m_IsFilterByDateToAvailable;

		public bool IsFilterByDateToAvailable
		{
			get { return m_IsFilterByDateToAvailable; }
			set { SetProperty(ref m_IsFilterByDateToAvailable, value); }
		}

		public bool AreMonitorColumnsVisible
		{
			get { return this.ViewMode == ToDoListViewMode.MonitoredToDosForUser; }
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

		private bool m_ShowCommentsInExtendedField;

		public bool ShowCommentsInExtendedField
		{
			get { return m_ShowCommentsInExtendedField; }
			set { SetProperty(ref m_ShowCommentsInExtendedField, value); }
		}

		public ToDosOfDocumentViewModel ToDosOfDocument
		{ get; private set; }

		public ToDosViewModel()
			: this(null, null, ToDoListViewMode.ToDosForUser, "1")
		{
			m_IsFilterByDateToAvailable = true;
		}

		public ToDosViewModel(IContainer iocContainer, IStandardDialogService standardDialogService, ToDoListViewMode toDoListViewMode, string userOrCaseId)
			: base(iocContainer, standardDialogService)
		{
			m_FilterDateTo = DateTime.Today;
			this.ToDoStateFilter = new ToDoStateFilterViewModel(this.RefreshToDosAsync);
			m_FilterAppointmentSubType = AppointmentSubTypes.All;
			m_FilterDeadlineSubType = DeadlineSubTypes.All;
			m_FilterDebtCollectionSubType = DebtCollectionSubTypes.All;
			m_ToDosOfCase = new ToDosOfCaseViewModel(this);
			this.ToDosOfDocument = new ToDosOfDocumentViewModel(this);

			if (m_IocContainer != null)
			{
				var toDoService = m_IocContainer.Resolve<IToDoService>();
				this.AllToDoTypesAvailable = toDoService.Parameter.AllToDoTypesAvailable;

				var extendedUIServices = m_IocContainer.Resolve<IExtendedUIServices>();
				m_ToDoListOptions = extendedUIServices.GetToDoListsOptions();
			}
			else
			{
				this.AllToDoTypesAvailable = true;
			}

			if (m_IocContainer != null && iocContainer.IsRegistered<IAdvoAssistService>())
			{
				var advoassistService = m_IocContainer.Resolve<IAdvoAssistService>();
				this.IsAdvoAssistActive = advoassistService.IsConfigured;
			}
			else
			{
				this.IsAdvoAssistActive = false;
			}

			this.ViewMode = toDoListViewMode;
			if (this.ViewMode == ToDoListViewMode.ToDosForUser)
			{
				this.FilterUserId = userOrCaseId;
			}
			else if (this.ViewMode == ToDoListViewMode.ToDosForCase)
			{
				long caseId;
				if (Int64.TryParse(userOrCaseId, out caseId))
				{
					this.FilterCaseId = caseId;
				}
			}
			else if (this.ViewMode == ToDoListViewMode.MonitoredToDosForUser)
			{
				this.ToDoStateFilter.FilterToDoState = ToDoState.FilterAll;
			}

			this.RefreshSelectableValues();

			this.FilteredToDoListEntries = new List<ToDoListEntryViewModel>();

			m_DocumentPreviewViewModel = new DocumentPreviewViewModel();
			m_DocumentPreviewViewModel.IsSwitchOriginalToWorkingCopyPossible = false;
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

			this.InitializeCommands();
		}

		public void RefreshToDosAsync()
		{
			if (m_IocContainer == null)
			{
				m_Logger.Warn("no iocContianer, data could not be refreshed, only supported for unit testing");
				return;
			}

			this.IsBusy = true;
			this.BusyDescription = Properties.Resources.BusyDescription_ToDosLoading;

			if (!m_IsLoadingDataInitially && m_FilteredToDoListEntries == null)
			{
				this.IsLoadingDataInitially = true;
			}

			TaskHelper.FireAndForget(() => this.RefreshToDosInternal());
			m_Logger.Debug("refresh in background called");
		}

		private void RefreshToDosInternal()
		{
			try
			{
				List<IToDoListEntry> toDoListEntriesFromDatabase = null;
				switch (this.ViewMode)
				{
					case ToDoListViewMode.ToDosForUser:
						if (m_FilterUserId == null)
						{
							toDoListEntriesFromDatabase = new List<IToDoListEntry>();
							m_Logger.Warn("no user id set in viewmode ToDosForUser, no data will be read");
						}
						else
						{
							if (m_AssignedToDoUserGroupIds == null)
							{
								m_AssignedToDoUserGroupIds = m_ToDoService.GetAssignedToDoUserGroupIdsByUserId(m_FilterUserId);
							}
							toDoListEntriesFromDatabase = m_ToDoService.GetToDosByUserId(
								m_FilterUserId, m_AssignedToDoUserGroupIds, m_FilterToDoType, m_FilterToDoSubType, this.ExcludeDebtCollectionBatchToDos, m_FilterInboxType,
								this.ToDoStateFilter.FilterToDoState, m_FilterDateFrom, m_FilterDateTo, ToDoListOption.Item1, ToDoListOption.Item2
							);
						}
						break;

					case ToDoListViewMode.ToDosForOffice:
						if (!m_FilterToDoType.HasValue || m_FilterToDoType == ToDoType.None)
						{
							m_Logger.Warn("in ViewMode ToDosForOffice a ToDoType for filtering has to be defined");
							this.m_IsLoadingDataInitially = true;
							this.FilteredToDoListEntries = new List<ToDoListEntryViewModel>();
							return;
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetToDosForOffice(
								m_FilterToDoType, m_FilterToDoSubType, this.ToDoStateFilter.FilterToDoState, m_FilterDateFrom, m_FilterDateTo, ToDoListOption.Item1, ToDoListOption.Item2
							);
						}
						break;

					case ToDoListViewMode.AdvoAssistToDosForOffice:
						if (!m_FilterToDoType.HasValue || m_FilterToDoType == ToDoType.None)
						{
							m_Logger.Warn("in ViewMode ToDosForOffice a ToDoType for filtering has to be defined");
							this.m_IsLoadingDataInitially = true;
							this.FilteredToDoListEntries = new List<ToDoListEntryViewModel>();
							return;
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetAdvoAssistToDosForOffice(
								m_FilterToDoType, m_FilterToDoSubType, this.ToDoStateFilter.FilterToDoState, m_FilterDateFrom, m_FilterDateTo, ToDoListOption.Item1, ToDoListOption.Item2
							);
						}
						break;

					case ToDoListViewMode.ToDosForCase:
						if (m_FilterCaseId == 0)
						{
							toDoListEntriesFromDatabase = new List<IToDoListEntry>();
							m_Logger.Warn("no case id set in viewmode ToDosForCase, no data will be read");
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetToDosByCaseId(
								m_FilterCaseId, m_FilterToDoType, m_FilterToDoSubType, this.ExcludeDebtCollectionBatchToDos, this.ToDoStateFilter.FilterToDoState, ToDoListOption.Item1, ToDoListOption.Item2
							);
						}
						break;

					case ToDoListViewMode.ToDosForAddressee:
						if (m_FilterAddresseeId == 0)
						{
							toDoListEntriesFromDatabase = new List<IToDoListEntry>();
							m_Logger.Warn("no addressee id set in viewmode ToDosForAddressee, no data will be read");
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetToDosByAddresseeId(
								m_FilterAddresseeId, m_FilterToDoType, m_FilterToDoSubType, this.ExcludeDebtCollectionBatchToDos, this.ToDoStateFilter.FilterToDoState, ToDoListOption.Item1, ToDoListOption.Item2
							);
						}
						break;

					case ToDoListViewMode.MonitoredToDosForUser:
						if (m_FilterUserId == null)
						{
							toDoListEntriesFromDatabase = new List<IToDoListEntry>();
							m_Logger.Debug("no user id set in viewmode MonitoredToDosForUser, no data will be read");
						}
						else if (m_FilterToDoType.HasValue)
						{
							throw new NotImplementedException("FilterToDoType for MonitoredToDosForUser not implemented");
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetMonitoredToDosByUserId(m_FilterUserId, m_FilterDateTo);
						}
						break;

					case ToDoListViewMode.ToDosForResources:
						if (m_FilterResourceId == 0)
						{
							toDoListEntriesFromDatabase = new List<IToDoListEntry>();
							m_Logger.Warn("no resource id set in viewmode ToDosForResources, no data will be read");
						}
						else
						{
							toDoListEntriesFromDatabase = m_ToDoService.GetToDosByResourceId(m_FilterResourceId, m_FilterToDoType, m_FilterToDoSubType, this.ToDoStateFilter.FilterToDoState, m_FilterDateFrom, m_FilterDateTo);
						}
						break;

					case ToDoListViewMode.None:
						throw new InvalidOperationException(this.ViewMode.ToString());

					default:
						throw new NotImplementedException(this.ViewMode.ToString());
				}
				m_Logger.DebugFormat("got {0} entries from database", toDoListEntriesFromDatabase.Count);
				var filteredToDoListEntries = toDoListEntriesFromDatabase.OfType<ToDoListEntryViewModel>().OrderByDescending(x => x.DueDate).ToList();
				filteredToDoListEntries = FilterToDoListEntriesBySubTypes(filteredToDoListEntries).ToList();

				m_Logger.DebugFormat("{0} entries after filtering", filteredToDoListEntries.Count);
				this.FilteredToDoListEntries = filteredToDoListEntries;

				this.ErrorMessageWhileLoadingToDos = "";
			}
			catch (Exception exp)
			{
				m_Logger.Error(exp);
				this.ErrorMessageWhileLoadingToDos = "Fehler beim Laden der ToDos: " + exp.Message;
			}
			finally
			{
				try
				{
					this.IsLoadingDataInitially = false;
					this.IsBusy = false;
				}
				catch (Exception exp)
				{
					m_Logger.Error(exp);
				}
			}
		}

		private IEnumerable<ToDoListEntryViewModel> FilterToDoListEntriesBySubTypes(IEnumerable<ToDoListEntryViewModel> filteredToDoListEntries)
		{
			if (this.IsFilterByAppointmentSubTypeAvailable)
			{
				if ((m_FilterAppointmentSubType & AppointmentSubTypes.Court) != AppointmentSubTypes.Court)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.AppointmentCourt);
				}
				if ((m_FilterAppointmentSubType & AppointmentSubTypes.Meeting) != AppointmentSubTypes.Meeting)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.AppointmentMeeting);
				}
				if ((m_FilterAppointmentSubType & AppointmentSubTypes.Other) != AppointmentSubTypes.Other)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.AppointmentOther);
				}
			}

			if (this.IsFilterByDeadlineSubTypeAvailable)
			{
				if ((m_FilterDeadlineSubType & DeadlineSubTypes.Main) != DeadlineSubTypes.Main)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DeadlineMain);
				}
				if ((m_FilterDeadlineSubType & DeadlineSubTypes.SecondReminder) != DeadlineSubTypes.SecondReminder)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DeadlineSecondReminder);
				}
				if ((m_FilterDeadlineSubType & DeadlineSubTypes.FirstReminder) != DeadlineSubTypes.FirstReminder)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DeadlineFirstReminder);
				}
			}

			if (this.IsFilterByDebtCollectionSubTypeAvailable)
			{
				if ((m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Controller) != DebtCollectionSubTypes.Controller)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DebtCollectionController);
				}
				if ((m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Import) != DebtCollectionSubTypes.Import)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DebtCollectionImport);
				}
				if ((m_FilterDebtCollectionSubType & DebtCollectionSubTypes.Measure) != DebtCollectionSubTypes.Measure)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DebtCollectionMeasure);
				}
				if ((m_FilterDebtCollectionSubType & DebtCollectionSubTypes.CourtComplaint) != DebtCollectionSubTypes.CourtComplaint)
				{
					filteredToDoListEntries = filteredToDoListEntries.Where(x => x.SubType != ToDoSubType.DebtCollectionCourtComplaint);
				}
			}

			return filteredToDoListEntries;
		}

		public void SetStatusText(string statusText)
		{
			this.BusyDescription = statusText;
			this.IsBusy = true;
		}

		public void ResetStatusText()
		{
			this.IsBusy = false;
		}

		public void SaveChanges()
		{
			if (m_FilteredToDoListEntries == null || !m_FilteredToDoListEntries.Select(x => x.DocumentsOfToDo).Any())
			{
				return;
			}

			var changedDocuments = m_FilteredToDoListEntries.Select(x => x.DocumentsOfToDo.Where(y => y.IsDirty == true)).ToList();

			if (changedDocuments != null)
			{
				foreach (var document in changedDocuments)
				{
					m_DocumentService.SaveDocumentProperties(document);
				}
			}

			m_FilteredToDoListEntries.ForEach(x => x.DocumentsOfToDo.ForEach(y => y.ResetIsDirty()));
		}

		private DocumentPreviewViewModel m_DocumentPreviewViewModel;

		public DocumentPreviewViewModel DocumentPreviewViewModel
		{
			get { return m_DocumentPreviewViewModel; }
			private set { SetProperty(ref m_DocumentPreviewViewModel, value); }
		}

		private void DocumentPreviewViewModel_StampAddedToDocument(StampAddedToDocumentArguments args)
		{
			try
			{
				var currentDocument = m_DocumentPreviewViewModel.CurrentDocument;
				if (currentDocument == null)
				{
					throw new InvalidOperationException("no document visible in preview, not stamp could be added");
				}

				//close current document in preview
				m_DocumentPreviewViewModel.CurrentDocument = null;

				var documentId = Convert.ToInt64(currentDocument.UniqueIdentifier);
				if (ActiveDocumentOfToDo?.DocumentSignatureMode != DocumentSignatureMode.NotSigned)
				{
					m_StandardDialogService.ShowMessageInformation("Das Dokument ist signiert. Es kann kein Stempel aufgebracht werden.", "Stempel auf Dokument aufbringen");
					currentDocument.DiposeStreamIfIndicated();
					currentDocument.SetStream(null);
					var lastDocumentStream = m_DocumentService.GetDocumentContentStream(documentId, false);
					lastDocumentStream.Position = 0;
					m_DocumentService.UpdateDocumentContent(documentId, lastDocumentStream);
					m_DocumentPreviewViewModel.CurrentDocument = currentDocument;
				}
				else
				{
					// TODODMS nahezu identischer Code zu DocumentInboxViewModel.DocumentPreviewViewModel_StampAddedToDocument - auslagern?

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

					using (var stampedDocumentContent = File.Open(tempFileName, FileMode.Open, FileAccess.Read, FileShare.Read))
					{
						m_DocumentService.UpdateDocumentContent(documentId, stampedDocumentContent);
					}

					m_DocumentPreviewViewModel.CurrentDocument = currentDocument;
					// TODODMS tempFileName und stampImageFilePath löschen (%LOCALAPPDATA%\Temp) richtig?
					File.Delete(tempFileName);
					File.Delete(stampImageFilePath);
				}
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Einfügen eines Stempels ist ein Fehler aufgetreten.", "Stempel einfügen");
			}
		}

		public void SuspendActiveToDoListEntryChange()
		{
			m_ActiveToDoListEntryChangeSuspended = true;
		}

		public void ResumeActiveToDoListEntryChange()
		{
			m_ActiveToDoListEntryChangeSuspended = false;
		}

		/// <summary>
		/// Sets toDoType, toDoSubType, dateFrom and dateTo in one go and refreshs todos only once, if necessary.
		/// </summary>
		public void SetFilter(ToDoType? toDoType, ToDoSubType? toDoSubType, int? filterInboxType, DateTime? dateFrom, DateTime dateTo)
		{
			bool hasChanged = false;
			if (!EqualityComparer<ToDoType?>.Default.Equals(m_FilterToDoType, toDoType))
			{
				m_FilterToDoType = toDoType;
				m_FilteredToDoListEntries = null;
				RaisePropertyChanged(nameof(FilterToDoType));
				this.RaisePropertyChangedAfterFilterToDoTypeChanged();
				hasChanged = true;
			}
			if (!EqualityComparer<ToDoSubType?>.Default.Equals(m_FilterToDoSubType, toDoSubType))
			{
				m_FilterToDoSubType = toDoSubType;
				m_FilteredToDoListEntries = null;
				RaisePropertyChanged(nameof(FilterToDoSubType));
				this.RaisePropertyChangedAfterFilterToDoTypeChanged();
				hasChanged = true;
			}
			if (!EqualityComparer<DateTime?>.Default.Equals(m_FilterDateFrom, dateFrom))
			{
				m_FilterDateFrom = dateFrom;
				RaisePropertyChanged(nameof(FilterDateFrom));
				hasChanged = true;
			}
			if (!EqualityComparer<DateTime>.Default.Equals(m_FilterDateTo, dateTo))
			{
				m_FilterDateTo = dateTo;
				RaisePropertyChanged(nameof(FilterDateTo));
				hasChanged = true;
			}
			if (!EqualityComparer<int?>.Default.Equals(m_FilterInboxType, filterInboxType))
			{
				m_FilterInboxType = filterInboxType;
				RaisePropertyChanged(nameof(FilterInboxType));
				hasChanged = true;
			}
			if (hasChanged)
			{
				this.RefreshToDosAsync();
			}
		}

		private void RaisePropertyChangedAfterFilterToDoTypeChanged()
		{
			RaisePropertyChanged(nameof(IsFilterByAppointmentSubTypeAvailable));
			RaisePropertyChanged(nameof(IsFilterByDeadlineSubTypeAvailable));
			RaisePropertyChanged(nameof(IsFilterByDebtCollectionSubTypeAvailable));
			RaisePropertyChanged(nameof(IsAvailableShowDebtCollectionBatchToDos));
			RaisePropertyChanged(nameof(FilteredToDoListEntries));
		}

		public void SetActiveToDoListEntryOnCalendarTabById(long toDoId)
		{
			var filteredEntries = this.FilteredToDoListEntries;
			if (filteredEntries != null)
			{
				var todo = filteredEntries.FirstOrDefault(x => x != null && x.Id == toDoId);
				this.ActiveToDoListEntryOnCalendarTab = todo;
			}
		}

		public bool IsToDoTypeDeptCollection
		{
			get
			{
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					return ToDoType.DebtCollection == firstSelectedToDo.Type;
				}
				return false;
			}
		}

		public bool IsToDoMultipleSelected
		{
			get
			{
				if (m_ActiveTab == ToDoTabs.ToDo)
				{
					if (m_SelectedToDoListEntriesForBinding == null)
					{
						return false;
					}
					if (m_SelectedToDoListEntriesForBinding.Count() == 1)
					{
						return true;
					}
				}
				return false;
			}
		}

		public void RaiseIsToDoMultipleSelected()
		{
			RaisePropertyChanged(nameof(IsToDoMultipleSelected));
		}

		public void RaiseIsToDoTypeDeptCollection()
		{
			RaisePropertyChanged(nameof(IsToDoTypeDeptCollection));
		}

		public bool IsMarkForSupplementaryLetterOfFormalNoticeEnabled
		{
			get
			{
				bool result = false;
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					if (m_IocContainer != null)
					{
						var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
						result = legacyLawyerUIServices.IsMarkForSupplementaryLetterOfFormalNoticeEnabled(1L);
					}
				}
				return result;
			}
		}

		public bool IsDoNotMarkForLetterOfFormalNoticeAnyMoreEnabled
		{
			get
			{
				bool result = false;
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					if (m_IocContainer != null)
					{
						var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
						result = legacyLawyerUIServices.IsDoNotMarkForLetterOfFormalNoticeAnyMoreEnabled(firstSelectedToDo.Id);
					}
				}
				return result;
			}
		}

		public void RaiseAreSupplementaryLetterOfFormalNoticeEnabled()
		{
			RaisePropertyChanged(nameof(IsMarkForSupplementaryLetterOfFormalNoticeEnabled));
			RaisePropertyChanged(nameof(IsDoNotMarkForLetterOfFormalNoticeAnyMoreEnabled));
		}

		public bool IsClaimsManagementCommandToShow
		{
			get
			{
				bool result = false;
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					if (m_IocContainer != null)
					{
						var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
						result = legacyLawyerUIServices.ForderungsmanagementShow(firstSelectedToDo.Id);
					}
				}
				return result;
			}
		}

		public bool IsClaimsManagementCommandEnabled
		{
			get
			{
				bool result = false;
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					if (m_IocContainer != null)
					{
						var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
						result = legacyLawyerUIServices.ForderungsmanagementEnable(firstSelectedToDo.Id);
					}
				}
				return result;
			}
		}

		public bool IsDispositionsCommandToShow
		{
			get
			{
				bool result = false;
				if (m_SelectedToDoListEntriesForBinding == null)
				{
					return false;
				}
				var firstSelectedToDo = m_SelectedToDoListEntriesForBinding.FirstOrDefault();
				if (firstSelectedToDo != null)
				{
					if (m_IocContainer != null)
					{
						var legacyLawyerUIServices = m_IocContainer.Resolve<ILegacyLawyerUIServices>();
						result = legacyLawyerUIServices.VerfuegungenShow(firstSelectedToDo.Id);
					}
				}
				return result;
			}
		}

		public void RaiseDispositionsAndClaimsManagementToShow()
		{
			RaisePropertyChanged(nameof(IsClaimsManagementCommandToShow));
			RaisePropertyChanged(nameof(IsDispositionsCommandToShow));
			RaisePropertyChanged(nameof(IsClaimsManagementCommandEnabled));
		}
	}
}