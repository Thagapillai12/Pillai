using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WK.DE.DocumentManagement.ViewModels.DocumentInbox;

namespace WK.DE.DocumentManagement.IntegrationTests.DocumentInbox
{
	[TestClass]
	public class DocumentInboxWithInboxSelectionViewModelTests
	{
		[TestMethod]
		public void DMS_Inbox_PreselectFirstDocumentInbox()
		{
			// ============================== arrange ==============================
			var viewModel = new DocumentInboxWithInboxSelectionViewModel();

			// ==============================   act   ==============================
			var preselectedDocumentInbox = viewModel.SelectionTreeNodes.First().Items.Skip(1).First().Items.First() as InboxSelectionTreeNodeSelectableDocumentInboxViewModel;

			// ==============================  assert ==============================
			Assert.AreEqual(preselectedDocumentInbox.DocumentInboxConfiguration, viewModel.SelectedInbox);
			Assert.IsTrue(viewModel.IsDocumentInboxSelected);
			Assert.IsFalse(viewModel.IsToDosInboxSelected);
			Assert.IsFalse(viewModel.IsMonitoredToDosSelected);
		}

		[TestMethod]
		public void DMS_Inbox_SelectToDos()
		{
			// ============================== arrange ==============================
			var viewModel = new DocumentInboxWithInboxSelectionViewModel();

			// ==============================   act   ==============================
			var preselectedDocumentInbox = viewModel.SelectionTreeNodes.First().Items.Skip(1).First().Items.First() as InboxSelectionTreeNodeSelectableDocumentInboxViewModel;
			preselectedDocumentInbox.IsSelected = false;
			viewModel.SelectionTreeNodes.First().IsSelected = true;

			// ==============================  assert ==============================
			Assert.IsFalse(viewModel.IsDocumentInboxSelected);
			Assert.IsTrue(viewModel.IsToDosInboxSelected);
			Assert.IsFalse(viewModel.IsMonitoredToDosSelected);

			Assert.IsNull(viewModel.SelectedInbox);
			Assert.AreEqual("1", viewModel.SelectedToDosUserId);
			Assert.AreEqual("1", viewModel.ToDosInbox.FilterUserId);
			Assert.IsNull(viewModel.SelectedMonitoredToDosUserId);
			Assert.IsNull(viewModel.MonitoredToDos.FilterUserId);
		}

		[TestMethod]
		public void DMS_Inbox_SelectMonitoredToDos()
		{
			// ============================== arrange ==============================
			var viewModel = new DocumentInboxWithInboxSelectionViewModel();

			// ==============================   act   ==============================
			var preselectedDocumentInbox = viewModel.SelectionTreeNodes.First().Items.Skip(1).First().Items.First() as InboxSelectionTreeNodeSelectableDocumentInboxViewModel;
			preselectedDocumentInbox.IsSelected = false;
			viewModel.SelectionTreeNodes.Skip(1).First().Items.First().IsSelected = true;

			// ==============================  assert ==============================
			Assert.IsFalse(viewModel.IsDocumentInboxSelected);
			Assert.IsFalse(viewModel.IsToDosInboxSelected);
			Assert.IsTrue(viewModel.IsMonitoredToDosSelected);

			Assert.IsNull(viewModel.SelectedInbox);
			Assert.IsNull(viewModel.SelectedToDosUserId);
			Assert.IsNull(viewModel.ToDosInbox.FilterUserId);
			Assert.AreEqual("2", viewModel.SelectedMonitoredToDosUserId);
			Assert.AreEqual("2", viewModel.MonitoredToDos.FilterUserId);
		}

		[TestMethod]
		public void DMS_Inbox_SelectDocumentInbox()
		{
			// ============================== arrange ==============================
			var viewModel = new DocumentInboxWithInboxSelectionViewModel();

			// ==============================   act   ==============================
			var preselectedDocumentInbox = viewModel.SelectionTreeNodes.First().Items.Skip(1).First().Items.First() as InboxSelectionTreeNodeSelectableDocumentInboxViewModel;
			preselectedDocumentInbox.IsSelected = false;
			viewModel.SelectionTreeNodes.First().IsSelected = true;
			viewModel.SelectionTreeNodes.First().IsSelected = false;
			preselectedDocumentInbox.IsSelected = true;

			// ==============================  assert ==============================
			Assert.IsTrue(viewModel.IsDocumentInboxSelected);
			Assert.IsFalse(viewModel.IsToDosInboxSelected);
			Assert.IsFalse(viewModel.IsMonitoredToDosSelected);

			Assert.AreEqual(preselectedDocumentInbox.DocumentInboxConfiguration, viewModel.SelectedInbox);
			Assert.IsNull(viewModel.SelectedToDosUserId);
			Assert.IsNull(viewModel.ToDosInbox.FilterUserId);
			Assert.IsNull(viewModel.SelectedMonitoredToDosUserId);
			Assert.IsNull(viewModel.MonitoredToDos.FilterUserId);
		}
	}
}
