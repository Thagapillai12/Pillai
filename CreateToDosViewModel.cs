using DryIoc;
using Infragistics.Win.UltraWinGrid;
using Infragistics.Windows.Ribbon;
using log4net;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WK.DE.DocumentManagement.Commands.ToDos;
using WK.DE.DocumentManagement.Contracts.Commands;
using WK.DE.DocumentManagement.Contracts.Exceptions;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.DocumentManagement.Contracts.ViewModels;
using WK.DE.DocumentManagement.Contracts.ViewModels.ToDos;
using WK.DE.DocumentManagement.Converter;
using WK.DE.DocumentManagement.Helper;
using WK.DE.DocumentManagement.Services;
using WK.DE.DocumentManagement.Services.DateTimeAndHolidays;
using WK.DE.DocumentManagement.ViewModels.SelectionHelper;

namespace WK.DE.DocumentManagement.ViewModels.ToDos
{
	/// <summary>
	/// Defines a viewmodel for creating several todos for one or multiple documents.
	/// </summary>
	/// <remarks>Uses CreateToDoViewModel which represents one ToDo that should be created</remarks>
	public class CreateToDosViewModel : BindableBase
	{
		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(CreateToDosViewModel));

		private readonly IContainer m_IocContainer;

		public IContainer IoCContainer
		{
			get { return m_IocContainer; }
		}

		private readonly IStandardDialogService m_StandardDialogService;

		public IStandardDialogService StandardDialogService
		{
			get { return m_StandardDialogService; }
		}

		internal SelectUserFromListOrFavoritesViewModelSharedDataContainer SelectUserFromListOrFavoritesViewModelSharedDataContainer
		{ get; private set; }

		private bool m_AllowMultipleToDosOfSameType;

		private bool m_OnlyAddToDosWithAddCommand;

		public bool AllowMultipleToDoRecipients { get; set; }

		public int? CreatedThroughInboxType
		{ get; private set; }

		public bool CreateToDosForInbox
		{ get; private set; }

		private readonly IDocumentService m_DocumentService;
		private readonly IToDoService m_ToDoService;
		internal IToDoService ToDoService => m_ToDoService;
		private readonly string m_UserId;

		public bool AllToDoTypesAvailable
		{ get; private set; }

		public List<IToDoCategory> AllCategories
		{ get; private set; }

		public ICommand CreateAppointmentCommand
		{ get; private set; }

		public ICommand CreateDeadlineCommand
		{ get; private set; }

		public ICommand CreateTaskCommand
		{ get; private set; }

		public ICommand CreateFollowUpCommand
		{ get; private set; }

		public ICommand CreatePhonecallCommand
		{ get; private set; }

		public ICommand CreateRingbackCommand
		{ get; private set; }

		public ICommand AddTaskCommand
		{ get; private set; }

		public ICommand AddFollowUpCommand
		{ get; private set; }

		public ICommand AddPhonecallCommand
		{ get; private set; }

		public ICommand AddRingbackCommand
		{ get; private set; }

		private long m_CaseIdToPreconfigure;

		public long CaseIdToPreconfigure
		{
			get { return m_CaseIdToPreconfigure; }
			set { SetProperty(ref m_CaseIdToPreconfigure, value); }
		}

		private string m_DefaultPhoneNumber;

		public string DefaultPhoneNumber
		{
			get { return m_DefaultPhoneNumber; }
			set { SetProperty(ref m_DefaultPhoneNumber, value); }
		}

		public bool ToDoTypeTaskIsSelectedForCreation
		{
			get { return this.ToDosToCreate.Any(x => x.Type == ToDoType.Task); }
		}

		public bool ToDoTypeRingbackIsSelectedForCreation
		{
			get { return this.ToDosToCreate.Any(x => x.Type == ToDoType.Ringback); }
		}

		public bool ToDoTypeFollowUpIsSelectedForCreation
		{
			get { return this.ToDosToCreate.Any(x => x.Type == ToDoType.FollowUp); }
		}

		private bool m_ToDoTypeAppointmentIsSelectedForCreation;
		public bool ToDoTypeAppointmentIsSelectedForCreation
		{
			get { return m_ToDoTypeAppointmentIsSelectedForCreation; }
			set { SetProperty(ref m_ToDoTypeAppointmentIsSelectedForCreation, value); }
		}

		private bool m_ToDoTypeDeadlineIsSelectedForCreation;
		public bool ToDoTypeDeadlineIsSelectedForCreation
		{
			get { return m_ToDoTypeDeadlineIsSelectedForCreation; }
			set { SetProperty(ref m_ToDoTypeDeadlineIsSelectedForCreation, value); }
		}

		public List<IToDoCategory> SelectableValuesToDoCategoriesFollowUp
		{ get; private set; }

		public List<IToDoCategory> SelectableValuesToDoCategoriesRingback
		{ get; private set; }

		public List<IToDoCategory> SelectableValuesToDoCategoriesPhonecall
		{ get; private set; }

		public List<IToDoCategory> SelectableValuesToDoCategoriesTask
		{ get; private set; }

		public List<ToDoPrioritySelectionViewModel> SelectableValuesToDoPriorities
		{ get; private set; }

		public List<ReminderMinutesViewModel> SelectableValuesReminderMinutes
		{ get; private set; }

		public List<IToDoCommentTemplate> SelectableValuesComment
		{ get; private set; }

		public List<KeyValuePair<string, int>> SelectableValuesToDoStateFollowUp
		{ get; private set; }

		public List<KeyValuePair<string, int>> SelectableValuesToDoStateRingback
		{ get; private set; }

		public List<KeyValuePair<string, int>> SelectableValuesToDoStatePhonecall
		{ get; private set; }

		public List<KeyValuePair<string, int>> SelectableValuesToDoStateTask
		{ get; private set; }

		private bool m_IsInEditMode;

		public bool IsInEditMode
		{
			get { return m_IsInEditMode; }
			set { SetProperty(ref m_IsInEditMode, value); }
		}

		private bool m_IsAdditionalRecipientPossible;

		public bool IsAdditionalRecipientPossible
		{
			get { return m_IsAdditionalRecipientPossible; }
			set { SetProperty(ref m_IsAdditionalRecipientPossible, value); }
		}

		private List<IToDoDisclosureGroup> m_ToDoDisclosureGroups;

		public List<IToDoDisclosureGroup> ToDoDisclosureGroups
		{
			get { return m_ToDoDisclosureGroups; }
			set { SetProperty(ref m_ToDoDisclosureGroups, value); }
		}

		private string m_userIdToPreconfigure;

		public string UserIdToPreconfigure
		{
			get { return m_userIdToPreconfigure; }
			set { SetProperty(ref m_userIdToPreconfigure, value); }
		}

		private ObservableCollection<CreateToDoViewModel> m_ToDosToCreate;

		public ObservableCollection<CreateToDoViewModel> ToDosToCreate
		{
			get { return m_ToDosToCreate; }
			set
			{
				var oldValue = m_ToDosToCreate;
				if (SetProperty(ref m_ToDosToCreate, value))
				{
					if (oldValue != null)
					{
						oldValue.CollectionChanged -= ToDosToCreate_CollectionChanged;
					}
					this.RaisePropertyChanged(nameof(AnyToDoToCreate));
					if (m_ToDosToCreate != null)
					{
						m_ToDosToCreate.CollectionChanged += ToDosToCreate_CollectionChanged;
					}
				}
			}
		}

		private CreateToDoViewModel m_SelectedToDoToCreate;

		public CreateToDoViewModel SelectedToDoToCreate
		{
			get { return m_SelectedToDoToCreate; }
			set { SetProperty(ref m_SelectedToDoToCreate, value); }
		}

		public string MessageTitle
		{
			get { return this.IsInEditMode ? "ToDos bearbeiten" : "ToDos anlegen"; }
		}

		private void ToDosToCreate_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
		{
			this.RaisePropertyChanged(nameof(AnyToDoToCreate));
			RaisePropertyChanged(nameof(ToDoTypeFollowUpIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeRingbackIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeTaskIsSelectedForCreation));

			if (m_SelectedToDoToCreate != null
				&& !this.ToDosToCreate.Contains(m_SelectedToDoToCreate))
			{
				m_SelectedToDoToCreate = null;
			}

			if (m_SelectedToDoToCreate == null && this.ToDosToCreate.Any())
			{
				this.SelectedToDoToCreate = this.ToDosToCreate.Last();
			}
		}

		public bool AnyToDoToCreate
		{
			get { return m_ToDosToCreate != null && m_ToDosToCreate.Count > 0; }
		}

		public string CurrentUserId
		{
			get { return m_UserId; }
		}

        internal bool CanCreateAllToDos(CaseAndAddresseeSelectionViewModel caseAndAddresseeSelectionViewModel)
		{
			var cases = caseAndAddresseeSelectionViewModel?.CasesToLink?.Select(x => x.Case);
			return CanCreateAllToDos(cases);
        }

        internal bool CanCreateAllToDos(IEnumerable<ICase> casesToLink)
		{
			try
			{
				if (!AllCasesAreNotFiled(casesToLink))
				{
					m_StandardDialogService.ShowMessageInformation("Die Akte, zu der das ToDo erstellt werden soll, ist bereits abgelegt. Sie können keine neuen ToDos mehr zu dieser Akte erstellen.", this.MessageTitle);
					return false;
				}
				if (!AllToDosHaveValidRecipients())
				{
					m_StandardDialogService.ShowMessageInformation("Bitte wählen Sie für alle ToDos einen Mitarbeiter aus, dem der Eintrag zugeordnet werden soll.", this.MessageTitle);
					return false;
				}
				if (!AllToDosHaveValidMonitorInfo())
				{
					m_StandardDialogService.ShowMessageInformation("Bitte geben Sie für alle ToDos gültige Daten für die Nachverfolgung der ToDos ein. Die Eingabe von Stunden und Minuten muss im Format \"hh:mm\" erfolgen, also z.B. \"12:15\" für 12 Stunden und 15 Minuten.", this.MessageTitle);
					return false;
				}
				if (!AllToDosHaveValidCategory())
				{
					m_StandardDialogService.ShowMessageInformation("Bitte wählen Sie für alle ToDos eine Kategorie aus.", this.MessageTitle);
					return false;
				}
				if (!AllToDosAreInFutureOrShouldNotBeChecked())
				{
					return false;
				}
				if (!CheckIfAllRecipientsHavePermissions())
				{
					return false;
				}
				return true;
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Prüfen, ob anzulegende ToDos gültig sind, ist ein Fehler aufgetreten. Die ToDos können nicht angelegt werden", this.MessageTitle);
				return false;
			}
		}

        private bool AllCasesAreNotFiled(IEnumerable<ICase> casesToLink)
        {
			if (casesToLink == null)
			{
				return true;
			}
			return casesToLink.All(x => !x.IsFiled);
        }

        public bool AllToDosHaveValidRecipients()
		{
			return !m_ToDosToCreate.Any(x => x.RecipientSelectionViewModel.RecipientsToAdd.Count == 0);
		}

		public bool AllToDosHaveValidMonitorInfo()
		{
			return !m_ToDosToCreate.Any(x => x.IsMonitoringActive && !DateTimeAndHolidayService.IsHoursAndMinutesStringValid(x.MonitoringHoursMinutes));
		}

		public bool AllToDosHaveValidCategory()
		{
			bool allToDosHaveValidCategory = false;

			allToDosHaveValidCategory = !m_ToDosToCreate.Any(tdo => tdo.Category == null || !tdo.SelectableValuesToDoCategoriesForCurrentType.Any(cat => cat.Id == tdo.CategoryId));

			if (!allToDosHaveValidCategory)
			{
				//Allow edit of todos created by import-service of type task with category of type debtCollection
				if (m_ToDosToCreate.Any(tdo => tdo.Type == ToDoType.Task && AllCategories.Where(cat => cat.Type == ToDoType.DebtCollection).Any(y => y.Id == tdo.CategoryId)))
				{
					allToDosHaveValidCategory = true;
				}
			}

			return allToDosHaveValidCategory;
			
		}

		private bool AllToDosAreInFutureOrShouldNotBeChecked()
		{
			var toDosInPast = this.GetToDosInPast();
			if (toDosInPast.Any())
			{
				var text = new StringBuilder();
				foreach (var toDoInPast in toDosInPast)
				{
					text.Append("Der Eintrag \"");
					text.Append(toDoInPast.HeaderDisplayText);
					text.Append("\" soll zu ");
					if (toDoInPast.DueDateWithoutTime)
					{
						text.Append(toDoInPast.DueDate.ToString("f"));
					}
					else
					{
						text.Append(toDoInPast.DueDate.ToString("D"));
					}
					text.Append(" angelegt werden.");
					text.AppendLine();
				}
				text.AppendLine();
				if (toDosInPast.Count() > 1)
				{
					text.Append("Sind Sie sicher, dass die Einträge in der Vergangenheit angelegt werden sollen?");
				}
				else
				{
					text.Append("Sind Sie sicher, dass der Eintrag in der Vergangenheit angelegt werden soll?");
				}
				if (m_StandardDialogService.ShowMessageInformation(text.ToString(), this.MessageTitle, new string[] { "In Vergangenheit anlegen", "Nein" }, 1001) == 1001)
				{
					return false;
				}
			}

			var toDosOnWeekend = this.GetToDosOnWeekend();
			if (toDosOnWeekend.Any())
			{
				var text = new StringBuilder();
				foreach (var toDoOnWeekend in toDosOnWeekend)
				{
					text.Append("Der Eintrag \"");
					text.Append(toDoOnWeekend.HeaderDisplayText);
					text.Append("\" fällt auf einen ");
					text.Append(toDoOnWeekend.DueDate.ToString("dddd"));
					text.Append(".");
					text.AppendLine();
				}
				text.AppendLine();
				if (toDosOnWeekend.Count() > 1)
				{
					text.Append("Sind Sie sicher, dass die Einträge für ein Wochenende angelegt werden sollen?");
				}
				else
				{
					text.Append("Sind Sie sicher, dass der Eintrag für ein Wochenende angelegt werden soll?");
				}
				var result = m_StandardDialogService.ShowMessageInformation(text.ToString(), this.MessageTitle, new string[] { "Am Wochenende anlegen", "Auf Freitag anpassen", "Auf Montag anpassen", "Nein" }, 1001);
				if (result == 1001)
				{
					foreach (var toDoOnWeekend in toDosOnWeekend)
					{
						toDoOnWeekend.DueDate = DateTimeAndHolidayService.GetBusinessDayBeforePublicHolidayAndWeekend(toDoOnWeekend.DueDate);
					}
				}
				if (result == 1002)
				{
					foreach (var toDoOnWeekend in toDosOnWeekend)
					{
						toDoOnWeekend.DueDate = DateTimeAndHolidayService.GetBusinessDayAfterPublicHolidayAndWeekend(toDoOnWeekend.DueDate);
					}
				}
				else if (result == 1003)
				{
					return false;
				}
			}

			var toDosOnPublicHoliday = this.GetToDosOnPublicHoliday();
			if (toDosOnPublicHoliday.Any())
			{
				var text = new StringBuilder();
				foreach (var toDoOnPublicHoliday in toDosOnPublicHoliday)
				{
					var publicHolidays = DateTimeAndHolidayService.GetHolidayInfo(toDoOnPublicHoliday.DueDate, HolidayType.PublicHoliday);

					text.Append("Der Eintrag \"");
					text.Append(toDoOnPublicHoliday.HeaderDisplayText);
					text.Append("\" fällt auf den Feiertag \"");
					var publicHoliday = publicHolidays.First();
					text.Append(publicHoliday.Name);
					text.Append("\" (");
					text.Append(publicHoliday.Date.ToLongDateString());
					text.Append(").");
					text.AppendLine();
				}
				text.AppendLine();
				if (toDosOnPublicHoliday.Count() > 1)
				{
					text.Append("Sind Sie sicher, dass die Einträge für einen Feiertag angelegt werden sollen?");
				}
				else
				{
					text.Append("Sind Sie sicher, dass der Eintrag für einen Feiertag angelegt werden soll?");
				}
				var result = m_StandardDialogService.ShowMessageInformation(text.ToString(), this.MessageTitle, new string[] { "Am Feiertag anlegen", "Auf vorherigen Werktag anpassen", "Auf folgenden Werktag anpassen", "Nein" }, 1002);
				if (result == 1001)
				{
					foreach (var toDoOnPublicHoliday in toDosOnPublicHoliday)
					{
						toDoOnPublicHoliday.DueDate = DateTimeAndHolidayService.GetBusinessDayBeforePublicHolidayAndWeekend(toDoOnPublicHoliday.DueDate);
					}
				}
				else if (result == 1002)
				{
					foreach (var toDoOnPublicHoliday in toDosOnPublicHoliday)
					{
						toDoOnPublicHoliday.DueDate = DateTimeAndHolidayService.GetBusinessDayAfterPublicHolidayAndWeekend(toDoOnPublicHoliday.DueDate);
					}
				}
				else if (result == 1003)
				{
					return false;
				}
			}
			return true;
		}

		public bool CheckIfAllRecipientsHavePermissions()
		{
			var permissionService = m_IocContainer.Resolve<IPermissionService>();
			var usersWithoutPermission = new List<string>();

			if (m_ToDosToCreate == null || !m_ToDosToCreate.Any())
			{
				return true;
			}

			foreach (var todo in m_ToDosToCreate)
			{
				var recipients = todo.RecipientSelectionViewModel.RecipientsToAdd;
				if (recipients != null && recipients.Any())
				{
					foreach (var recipient in recipients)
					{
						if (permissionService.GetPermissionStatusByUserAndToDoOwnerId(m_UserId, recipient.Id) != PermissionStatus.Granted)
						{
							if (!usersWithoutPermission.Contains(recipient.Id))
							{
								usersWithoutPermission.Add(recipient.Id);
							}
						}
					}
				}
			}

			if (!usersWithoutPermission.Any())
			{
				return true;
			}

			if (usersWithoutPermission.Count == 1)
			{
				var ownerUserName = ToDoHelper.GetUserNameFromId(m_IocContainer, usersWithoutPermission.First());
				var messageText = String.Format("Sie können der Person \"{0}\" keine ToDos anlegen, da Ihnen die notwendige Berechtigung zum Zugriff auf die ToDos der Person fehlen.", ownerUserName);
				messageText += Environment.NewLine + Environment.NewLine + "Bitte wenden Sie sich an Ihren Administrator.";
				m_StandardDialogService.ShowMessageWarning(messageText, MessageTitle);
				return false;
			}
			else
			{
				var messageText = String.Format("Sie können für {0} Personen keine ToDos anlegen, da Ihnen die notwendige Berechtigung zum Zugriff auf die ToDos dieser Personen fehlen.", usersWithoutPermission.Count);
				messageText += Environment.NewLine + Environment.NewLine + "Bitte wenden Sie sich an Ihren Administrator.";
				m_StandardDialogService.ShowMessageWarning(messageText, MessageTitle);
				return false;
			}
		}

		public IEnumerable<CreateToDoViewModel> GetToDosInPast()
		{
			//add 5 minutes to date if time is important, otherwise user gets this message just on creating a new task without changing date in any way
			var potentialToDosInPast = m_ToDosToCreate.Where(x => (x.DueDateWithoutTime && x.DueDate.Date < DateTime.Today)
				|| (x.DueDateWithTime && x.DueDate.AddMinutes(5) < DateTime.Now));

			//if ToDos are already created and now are edited, than only check those ToDos where the DueDate has been changed
			if (this.IsInEditMode)
			{
				return potentialToDosInPast.Where(x => x.DueDateWasChangedFromUser);
			}
			return potentialToDosInPast;
		}

		public IEnumerable<CreateToDoViewModel> GetToDosOnWeekend()
		{
			return m_ToDosToCreate.Where(x => x.Category != null && x.Category.CheckPublicHolidays && DateTimeAndHolidayService.IsDateTimeOnWeekend(x.DueDate));
		}

		public IEnumerable<CreateToDoViewModel> GetToDosOnPublicHoliday()
		{
			IPublicHolidayCalendarService publicHolidayCalendarService = null;
			if (m_IocContainer.IsRegistered<IPublicHolidayCalendarService>())
			{
				publicHolidayCalendarService = m_IocContainer.Resolve<IPublicHolidayCalendarService>();
			}
			return m_ToDosToCreate.Where(x => x.Category != null && x.Category.CheckPublicHolidays && DateTimeAndHolidayService.IsDateTimeOnHoliday(x.DueDate, HolidayType.PublicHoliday, publicHolidayCalendarService));
		}

		public CreateToDosViewModel(IContainer iocContainer, IStandardDialogService standardDialogService, bool allowMultipleToDosOfSameType, bool allowMultipleToDoRecipients, int? createdThroughInboxType, bool onlyAddToDoWithAddCommand, bool createToDosForInbox)
			:this(iocContainer, standardDialogService, allowMultipleToDosOfSameType, allowMultipleToDoRecipients, createdThroughInboxType)
		{
			m_OnlyAddToDosWithAddCommand = onlyAddToDoWithAddCommand;
			this.CreateToDosForInbox = createToDosForInbox;
		}

		public CreateToDosViewModel(IContainer iocContainer, IStandardDialogService standardDialogService, bool allowMultipleToDosOfSameType, bool allowMultipleToDoRecipients, int? createdThroughInboxType)
		{
			m_IocContainer = iocContainer;
			m_StandardDialogService = standardDialogService;
			m_AllowMultipleToDosOfSameType = allowMultipleToDosOfSameType;
			AllowMultipleToDoRecipients = allowMultipleToDoRecipients;

			this.CreatedThroughInboxType = createdThroughInboxType;

			if (m_IocContainer != null)
			{
				this.SelectUserFromListOrFavoritesViewModelSharedDataContainer = new SelectUserFromListOrFavoritesViewModelSharedDataContainer(m_IocContainer);
			}

			this.ToDosToCreate = new ObservableCollection<CreateToDoViewModel>();

			if (m_IocContainer != null)
			{
				var userInformationService = m_IocContainer.Resolve<ICurrentUserInformationService>();
				m_UserId = userInformationService.UserId;

				m_DocumentService = m_IocContainer.Resolve<IDocumentService>();

				m_ToDoService = m_IocContainer.Resolve<IToDoService>();
				this.AllToDoTypesAvailable = m_ToDoService.Parameter.AllToDoTypesAvailable;
				this.AllCategories = ComboBoxValueService.GetSelectableValuesToDoCategories(m_ToDoService);
				this.IsAdditionalRecipientPossible = string.Equals(m_ToDoService.Parameter.SpecialUserID, "FIS", StringComparison.OrdinalIgnoreCase);

				if (IsAdditionalRecipientPossible)
				{
					var groups = new List<IToDoDisclosureGroup>();
					var emptyGroup = new ToDoDisclosureGroupViewModel();
					emptyGroup.DisclosureGroupRecipientType = ToDoDisclosureGroupRecipientType.None;
					groups.Add(emptyGroup);
					var allDisclosureGroups = m_ToDoService.GetDisclosureGroups();

					foreach (var group in allDisclosureGroups)
					{
						groups.Add(group);
					}

					this.m_ToDoDisclosureGroups = groups;
				}


				SelectableValuesToDoCategoriesFollowUp = this.AllCategories.Where(x => x.Type == ToDoType.FollowUp && x.IsActive).ToList();

				SelectableValuesToDoCategoriesRingback = this.AllCategories.Where(x => x.Type == ToDoType.Ringback && x.IsActive).ToList();

				SelectableValuesToDoCategoriesPhonecall = this.AllCategories.Where(x => x.Type == ToDoType.Phonecall && x.IsActive).ToList();

				SelectableValuesToDoCategoriesTask = this.AllCategories.Where(x => x.Type == ToDoType.Task && x.IsActive).ToList();

				SelectableValuesComment = ComboBoxValueService.GetSelectableValuesToDoCommentTemplates(m_ToDoService);

				SelectableValuesToDoPriorities = ComboBoxValueService.GetSelectableValuesToDoPriorities();

				SelectableValuesReminderMinutes = ComboBoxValueService.GetSelectableValuesReminderMinutes();
			}

			SetSelectableValuesForToDoState();

			CreateAppointmentCommand = new DelegateCommand(CreateAppointmentToDoCommand_Executed);
			CreateDeadlineCommand = new DelegateCommand(CreateDeadlineToDoCommand_Executed);
			CreateFollowUpCommand = new DelegateCommand(CreateFollowUpToDoCommand_Executed);
			CreateRingbackCommand = new DelegateCommand(CreateRingbackToDoCommand_Executed);
			CreatePhonecallCommand = new DelegateCommand(CreatePhonecallToDoCommand_Executed);
			CreateTaskCommand = new DelegateCommand(CreateTaskToDoCommand_Executed);

			AddFollowUpCommand = new DelegateCommand(AddFollowUpToDoCommand_Executed);
			AddRingbackCommand = new DelegateCommand(AddRingbackToDoCommand_Executed);
			AddPhonecallCommand = new DelegateCommand(AddPhonecallToDoCommand_Executed);
			AddTaskCommand = new DelegateCommand(AddTaskToDoCommand_Executed);
		}

		private void SetSelectableValuesForToDoState()
		{
			SelectableValuesToDoStateFollowUp = new List<KeyValuePair<string, int>> 
			{ new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Done), (int)ToDoState.Done),
				new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Obsolete), (int)ToDoState.Obsolete) };

			SelectableValuesToDoStatePhonecall = new List<KeyValuePair<string, int>>
			{ new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Done), (int)ToDoState.Done),
				new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Obsolete), (int)ToDoState.Obsolete) };

			SelectableValuesToDoStateRingback = new List<KeyValuePair<string, int>>
			{ new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Done), (int)ToDoState.Done),
				new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Obsolete), (int)ToDoState.Obsolete) };

			SelectableValuesToDoStateTask = new List<KeyValuePair<string, int>>
			{ new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Done), (int)ToDoState.Done),
				new KeyValuePair<string, int>(ToDoStateToStringConverter.ConvertToDoStateToString(ToDoState.Obsolete), (int)ToDoState.Obsolete) };

		}

		private void CreateToDoCommand_Executed(ToDoType toDoType)
		{
			if (m_AllowMultipleToDosOfSameType && !m_OnlyAddToDosWithAddCommand)
			{
				CreateToDoOfType(toDoType);
			}
			else
			{
				if (m_ToDosToCreate.Any(x => x.Type == toDoType))
				{
					var toDoToRemove = m_ToDosToCreate.FirstOrDefault(x => x.Type == toDoType);
					if (toDoToRemove != null)
					{
						m_ToDosToCreate.Remove(toDoToRemove);
					}
				}
				else
				{
					CreateToDoOfType(toDoType);
				}
			}
			RaisePropertyChanged(nameof(ToDoTypeFollowUpIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeRingbackIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeTaskIsSelectedForCreation));
		}

		private void AddToDoCommand_Executed(ToDoType toDoType)
		{
			CreateToDoOfType(toDoType);

			RaisePropertyChanged(nameof(ToDoTypeFollowUpIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeRingbackIsSelectedForCreation));
			RaisePropertyChanged(nameof(ToDoTypeTaskIsSelectedForCreation));
		}

		private void CreateToDoOfType(ToDoType todoType)
		{
			var createToDoViewModel = new CreateToDoViewModel(this);
			createToDoViewModel.Type = todoType;
			if (createToDoViewModel.IsPhoneNumberPossible && !String.IsNullOrWhiteSpace(this.DefaultPhoneNumber))
			{
				createToDoViewModel.PhoneNumber = this.DefaultPhoneNumber;
			}
			if (CaseIdToPreconfigure > 0)
			{
				var todoService = m_IocContainer.Resolve<IToDoService>();
				var documentService = m_IocContainer.Resolve<IDocumentService>();

				IUser userToPreconfigure = null;
				if (todoService.Parameter.UseDocOwnerAsToDoOwner && !string.IsNullOrEmpty(m_userIdToPreconfigure) && !this.CreateToDosForInbox)
				{
					userToPreconfigure = documentService.GetUserList().Where(x => x.Id == m_userIdToPreconfigure).FirstOrDefault();
				}

				if (userToPreconfigure != null)
				{
					createToDoViewModel.PreconfigureForDocOwner(userToPreconfigure);
				}
				else
				{
					var caseResponsibilities = documentService.GetCaseResponsibilities(this.CaseIdToPreconfigure);
					if (caseResponsibilities != null)
					{
						var preconfiguredToDoOwnerType = PreconfiguredToDoOwnerType.Lawyer;
						var createToDoType = GetCreateToDoType(todoType);
						if (createToDoType != CreateToDoType.None)
						{
							preconfiguredToDoOwnerType = todoService.Parameter.GetPreconfiguredToDoOwnerType(createToDoType);
						}
						createToDoViewModel.PreconfigureByCase(preconfiguredToDoOwnerType, caseResponsibilities);
					}
				}
			}
			this.ToDosToCreate.Add(createToDoViewModel);
			this.SelectedToDoToCreate = createToDoViewModel;
		}

		internal CreateToDoType GetCreateToDoType(ToDoType toDoType)
		{
			switch (toDoType)
			{
				case ToDoType.Appointment: return CreateToDoType.Appointment;
				case ToDoType.Deadline: return CreateToDoType.Deadline;
				case ToDoType.FollowUp: return CreateToDoType.FollowUp;
				case ToDoType.Ringback: return CreateToDoType.Ringback;
				case ToDoType.Phonecall: return CreateToDoType.Phonecall;
				case ToDoType.Task: return CreateToDoType.Task;
				default: return CreateToDoType.None;
			}
		}

		private void CreateAppointmentToDoCommand_Executed()
		{
			this.ToDoTypeAppointmentIsSelectedForCreation = !this.ToDoTypeAppointmentIsSelectedForCreation;
		}

		private void CreateDeadlineToDoCommand_Executed()
		{
			this.ToDoTypeDeadlineIsSelectedForCreation = !this.ToDoTypeDeadlineIsSelectedForCreation;
		}

		private void CreateFollowUpToDoCommand_Executed()
		{
			CreateToDoCommand_Executed(ToDoType.FollowUp);
		}

		private void CreateRingbackToDoCommand_Executed()
		{
			CreateToDoCommand_Executed(ToDoType.Ringback);
		}

		private void CreateTaskToDoCommand_Executed()
		{
			CreateToDoCommand_Executed(ToDoType.Task);
		}

		private void CreatePhonecallToDoCommand_Executed()
		{
			CreateToDoCommand_Executed(ToDoType.Phonecall);
		}

		private void AddFollowUpToDoCommand_Executed()
		{
			AddToDoCommand_Executed(ToDoType.FollowUp);
		}

		private void AddRingbackToDoCommand_Executed()
		{
			AddToDoCommand_Executed(ToDoType.Ringback);
		}

		private void AddTaskToDoCommand_Executed()
		{
			AddToDoCommand_Executed(ToDoType.Task);
		}

		private void AddPhonecallToDoCommand_Executed()
		{
			AddToDoCommand_Executed(ToDoType.Phonecall);
		}

		internal async Task CreateToDos(IEnumerable<IDocumentHistoryEntry> documentsToLink)
		{
			var documentsToLinkInformation = documentsToLink.Select(x => new ToDoDocumentToLinkInformation(x.DocumentCaseId, x.EventId, x.DocumentId)).ToList();
			await CreateToDosInternal(null, documentsToLinkInformation);
			foreach (var documentToLink in documentsToLink.OfType<DocumentHistoryEntryViewModel>())
			{
				documentToLink.RefreshToDosOfDocument();
			}
		}

		internal async Task CreateToDos(List<ToDoDocumentToLinkInformation> documentsToLink)
		{
			await CreateToDosInternal(null, documentsToLink);
		}

		internal async Task CreateToDos(long? caseId)
		{
			await CreateToDosInternal(caseId, null);
		}

		private async Task CreateToDosInternal(long? caseId, List<ToDoDocumentToLinkInformation> documentsToLink)
		{
			try
			{
				var usageTrackingService = m_IocContainer.Resolve<IUsageTrackingService>();
				foreach (var todoToCreate in this.ToDosToCreate)
				{
					var toDoCreateOrUpdateInfo = todoToCreate.CreateToDoCreateOrUpdateInfo();
					toDoCreateOrUpdateInfo.CaseId = caseId;
					toDoCreateOrUpdateInfo.DocumentsToLink = documentsToLink;
					toDoCreateOrUpdateInfo.CreatedThroughInboxType = this.CreatedThroughInboxType;

					string toDoCreationNotPossibleMessage = null;
					await TaskHelper.Run(() =>
					{
						try
						{
							foreach (var recipientToAdd in todoToCreate.RecipientSelectionViewModel.RecipientsToAdd)
							{
								CreateToDoViewModel.SetOwnerInformation(toDoCreateOrUpdateInfo, recipientToAdd);
								m_ToDoService.CreateToDo(m_UserId, toDoCreateOrUpdateInfo);

								usageTrackingService?.TrackEvent("TODO", "Created_" + todoToCreate.Type.ToString());
							}

							if (todoToCreate.SelectedToDoDisclosureGroup != null && todoToCreate.SelectedToDoDisclosureGroup.DisclosureGroupRecipientType != ToDoDisclosureGroupRecipientType.None)
							{
								var allUsers = m_DocumentService.GetUserList();
								IUser lawyerOfCase = null;

								if (caseId.HasValue)
								{
									var caseResponsibilities = m_DocumentService.GetCaseResponsibilities(toDoCreateOrUpdateInfo.CaseId.Value);
									lawyerOfCase = allUsers.FirstOrDefault(x => x.Id == caseResponsibilities.LawyerUserId);
								}
								
								var user = allUsers.FirstOrDefault(x => x.Id == toDoCreateOrUpdateInfo.OwnerUserId);
								var helper = new ToDoDisclosureGroupCreationHelper();
								var splitDisclosureGroups = helper.SplitDisclosureGroup(todoToCreate.SelectedToDoDisclosureGroup, user, lawyerOfCase, m_DocumentService, allUsers);
								var additionalToDosToCreate = new List<ToDoCreateOrUpdateInfo>();

								foreach (var group in splitDisclosureGroups)
								{
									var updateInfo = helper.CreateUpdateInfo(AllCategories, toDoCreateOrUpdateInfo, group);
									if (updateInfo != null)
									{
										additionalToDosToCreate.Add(updateInfo);
									}
								}

								if (additionalToDosToCreate.Any())
								{
									foreach (var additionalToDoTo in additionalToDosToCreate)
									{
										m_ToDoService.CreateToDo(m_UserId, additionalToDoTo);
									}
								}
							}
						}
						catch (ToDoValuesNotEnsuredException)
						{
							toDoCreationNotPossibleMessage = "Das neue ToDo konnten nicht gespeichert werden. Das ursprüngliche ToDo wurde inzwischen von einem anderen Mitarbeiter geändert oder bereits abgeschlossen.";
						}
						catch (ToDoCouldNotBeDirectedException exp)
						{
							var userName = ToDoHelper.GetUserNameFromId(m_IocContainer, exp.CurrentEditingUserId);
							toDoCreationNotPossibleMessage = String.Format("Das Ursprungs-ToDo kann nicht verfügt werden, da es aktuell durch den Mitarbeiter \"{0}\" in Bearbeitung ist.", userName);
						}
					}, m_StandardDialogService, "Beim Speichern eines ToDos ist ein Fehler aufgetreten.", todoToCreate.MessageTitle);

					if (toDoCreationNotPossibleMessage != null)
					{
						m_StandardDialogService.ShowMessageWarning(toDoCreationNotPossibleMessage, todoToCreate.MessageTitle);
					}

				}
			}
			catch (Exception exp)
			{
				m_StandardDialogService.RaiseExceptionOccuredEvent(exp, "Beim Anlegen der ToDos ist ein Fehler aufgetreten.", this.MessageTitle);
			}
		}

		internal void UpdateExistingToDosToCreate(ICase @case)
		{
			if (this.ToDosToCreate?.Count > 0)
			{			
				foreach (var viewModelUserUpdate in this.ToDosToCreate)
				{
					viewModelUserUpdate.SelectUserFromListOrFavorites.CaseIdToLoadUsersFor = @case.Id;

					var caseResponsibilities = m_DocumentService.GetCaseResponsibilities(@case.Id);
					if (caseResponsibilities == null) continue;
					var preconfiguredToDoOwnerType = m_ToDoService.Parameter.GetPreconfiguredToDoOwnerType(GetCreateToDoType(viewModelUserUpdate.Type));
					viewModelUserUpdate.PreconfigureByCase(preconfiguredToDoOwnerType, caseResponsibilities);
				}
			}
		}

		/// <summary>
		/// Configures todos to create by given case.
		/// </summary>
		internal void PreconfigureByCase(ICase @case, string documentName, string userIdToPreconfigure)
		{
			if (m_ToDoService.Parameter.NewPostboxScanToCaseToDoCategoryId == 0)
			{
				m_Logger.Info("no todo category configured, nothing todo");
				return;
			}
			if (m_ToDoService.Parameter.NewPostboxScanToCaseOwnerType == PreconfiguredToDoOwnerType.None)
			{
				m_Logger.Info("no todo owner type configured, nothing todo");
				return;
			}

			if (@case == null)
			{
				m_Logger.Warn("no case provided");
				return;
			}

			var caseResponsibilities = m_DocumentService.GetCaseResponsibilities(@case.Id);
			if (caseResponsibilities == null)
			{
				m_Logger.WarnFormat("no case responsibilities for case {0} available", @case.Id);
				return;
			}

			if (this.ToDosToCreate.Any())
			{
				m_Logger.Info("already todos to create configured, nothing to do");
				return;
			}

			var createToDoViewModel = new CreateToDoViewModel(this);
			createToDoViewModel.Type = ToDoType.Task;
			createToDoViewModel.CategoryId = m_ToDoService.Parameter.NewPostboxScanToCaseToDoCategoryId;
			createToDoViewModel.Subject += " " + documentName;

			IUser usertoPreconfigure = null;

			if (!string.IsNullOrEmpty(userIdToPreconfigure))
			{
				usertoPreconfigure = m_DocumentService.GetUserList().Where(x => x.Id == userIdToPreconfigure).FirstOrDefault();
			}

			if (usertoPreconfigure != null)
			{
				createToDoViewModel.PreconfigureForDocOwner(usertoPreconfigure);
			}
			else
			{
				createToDoViewModel.PreconfigureByCase(m_ToDoService.Parameter.NewPostboxScanToCaseOwnerType, caseResponsibilities);
			}

			this.ToDosToCreate.Add(createToDoViewModel);
		}

		internal void ClearToDoViewModels()
		{
			this.ToDosToCreate.Clear();
		}
	}
}