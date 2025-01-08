using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;
using WK.DE.DocumentManagement.ViewModels.SelectionHelper;

namespace WK.DE.DocumentManagement.IntegrationTests
{
    [TestClass]
    public class CaseAndAddresseeSelectionViewModelTests
    {
        /// <summary>
        /// check if initialization of object is fine.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_NoCaseSelected()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================

            // ==============================  assert ==============================
            Assert.AreEqual(0, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNull(viewModel.FirstCaseToLink);
            Assert.AreEqual(1, viewModel.CaseFolders.Count);
            Assert.AreEqual(0, viewModel.CaseFolders.First().Id);
            Assert.AreEqual("", viewModel.CaseFolders.First().Name);
            Assert.AreEqual("", viewModel.CaseNumber);
            Assert.AreEqual(1, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            Assert.AreEqual(0, viewModel.CasesToLink.Count);
            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsFalse(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zu einer Akte herstellen", viewModel.LinkToCasesHeaderName);
            Assert.IsFalse(viewModel.LinkToMultipleCases);
            Assert.AreEqual(0, viewModel.OnlineShares.Count);
            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }

        /// <summary>
        /// check if setting the case number results is selecting one case.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_FirstCaseNumberSet()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================
            viewModel.CaseNumber = "00202/14";

            // ==============================  assert ==============================
            Assert.AreEqual(21, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            notfiyPropertyChangedTestHelper.ResetIndexForAssertRaiseHistory();
            Assert.AreEqual("CasesToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("FirstCaseToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("CaseNumber", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("LinkToCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("ViewMode", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("LinkToCasesHeaderName", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("LinkToMultipleCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("LinkToMultipleCasesInverted", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("CaseParticipants", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("AddresseeId", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("OnlineShares", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("OnlineSharesAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("OnlineSharesNotAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("OnlineSharesAllRowsSelected", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("MasterCase", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("IsFolderSelectionActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            Assert.AreEqual("IsFolderEditButtonActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());

            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNotNull(viewModel.FirstCaseToLink);
            Assert.AreEqual("00202/14", viewModel.FirstCaseToLink.CaseNumber);
            Assert.AreEqual(2, viewModel.FirstCaseToLink.CaseStateId);
            Assert.AreEqual(8, viewModel.CaseFolders.Count);
            Assert.AreEqual("00202/14", viewModel.CaseNumber);

            Assert.AreEqual(4, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual(1000 + index, viewModel.CaseParticipants.Skip(index + 1).First().AddresseeId);
                Assert.AreEqual("Group: Participant " + index, viewModel.CaseParticipants.Skip(index + 1).First().Name);
            }

            Assert.AreEqual(1, viewModel.CasesToLink.Count);
            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsTrue(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zur Akte: Subject 2 - Cause 2", viewModel.LinkToCasesHeaderName);
            Assert.IsFalse(viewModel.LinkToMultipleCases);

            Assert.AreEqual(3, viewModel.OnlineShares.Count);
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual(3000 + index, viewModel.OnlineShares.Skip(index).First().Id);
                Assert.AreEqual("OAUserName" + index, viewModel.OnlineShares.Skip(index).First().UserName);
            }

            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }

        /// <summary>
        /// check if adding a second case results is selecting two cases and shrinking participants and folders.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_TwoCasesSet()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4713);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================
            viewModel.CaseNumber = "00201/14";
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);

            // ==============================  assert ==============================
            Assert.AreEqual(38, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            notfiyPropertyChangedTestHelper.ResetIndexForAssertRaiseHistory();
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual("CasesToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("FirstCaseToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseNumber", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                if (index == 0) // checkbox LinkToCases is only set for the first case, not the second one
                {
                    Assert.AreEqual("LinkToCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("ViewMode", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                }
                Assert.AreEqual("LinkToCasesHeaderName", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCasesInverted", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseParticipants", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("AddresseeId", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineShares", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesNotAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAllRowsSelected", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("MasterCase", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderSelectionActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderEditButtonActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            }

            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNotNull(viewModel.FirstCaseToLink);
            Assert.AreEqual("00201/14", viewModel.FirstCaseToLink.CaseNumber);
            Assert.AreEqual(1, viewModel.FirstCaseToLink.CaseStateId);
            //TODODMS: muss noch korrekt implementiert werden: Assert.AreEqual(7, viewModel.CaseFolders.Count);
            Assert.AreEqual("00201/14", viewModel.CaseNumber);

            Assert.AreEqual(2, viewModel.CasesToLink.Count);
            var firstCase = viewModel.CasesToLink.First();
            Assert.AreEqual("00201/14", firstCase.CaseNumber);
            Assert.AreEqual(1, firstCase.CaseStateId);
            var secondCase = viewModel.CasesToLink.Skip(1).First();
            Assert.AreEqual("00202/14", secondCase.CaseNumber);
            Assert.AreEqual(2, secondCase.CaseStateId);

            Assert.AreEqual(3, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(1000 + index, viewModel.CaseParticipants.Skip(index + 1).First().AddresseeId);
                Assert.AreEqual("Group: Participant " + index, viewModel.CaseParticipants.Skip(index + 1).First().Name);
            }

            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsTrue(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zu 2 Akten", viewModel.LinkToCasesHeaderName);
            Assert.IsTrue(viewModel.LinkToMultipleCases);

            Assert.AreEqual(2, viewModel.OnlineShares.Count);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(3000 + index, viewModel.OnlineShares.Skip(index).First().Id);
                Assert.AreEqual("OAUserName" + index, viewModel.OnlineShares.Skip(index).First().UserName);
            }

            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }

        /// <summary>
        /// check if adding a second case and removing it again results is selecting only one case with folders and participants from first one.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_TwoCasesSetAndSecondRemoved()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4713);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================
            viewModel.CaseNumber = "00201/14";
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);
            viewModel.CasesToLink[1].RemoveCaseToLinkCommand.Execute(null);

            // ==============================  assert ==============================
            Assert.AreEqual(53, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            notfiyPropertyChangedTestHelper.ResetIndexForAssertRaiseHistory();
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual("CasesToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("FirstCaseToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseNumber", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                if (index == 0) // checkbox LinkToCases is only set for the first case, not the second one
                {
                    Assert.AreEqual("LinkToCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("ViewMode", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                }
                Assert.AreEqual("LinkToCasesHeaderName", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCasesInverted", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseParticipants", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("AddresseeId", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineShares", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesNotAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAllRowsSelected", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("MasterCase", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderSelectionActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderEditButtonActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            }

            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNotNull(viewModel.FirstCaseToLink);
            Assert.AreEqual("00201/14", viewModel.FirstCaseToLink.CaseNumber);
            Assert.AreEqual(1, viewModel.FirstCaseToLink.CaseStateId);
            //TODODMS: muss noch korrekt implementiert werden: Assert.AreEqual(7, viewModel.CaseFolders.Count);
            Assert.AreEqual("00201/14", viewModel.CaseNumber);

            Assert.AreEqual(1, viewModel.CasesToLink.Count);
            var firstCase = viewModel.CasesToLink.First();
            Assert.AreEqual("00201/14", firstCase.CaseNumber);
            Assert.AreEqual(1, firstCase.CaseStateId);

            Assert.AreEqual(3, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(1000 + index, viewModel.CaseParticipants.Skip(index + 1).First().AddresseeId);
                Assert.AreEqual("Group: Participant " + index, viewModel.CaseParticipants.Skip(index + 1).First().Name);
            }

            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsTrue(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zur Akte: Subject 1 - Cause 1", viewModel.LinkToCasesHeaderName);
            Assert.IsFalse(viewModel.LinkToMultipleCases);

            Assert.AreEqual(2, viewModel.OnlineShares.Count);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(3000 + index, viewModel.OnlineShares.Skip(index).First().Id);
                Assert.AreEqual("OAUserName" + index, viewModel.OnlineShares.Skip(index).First().UserName);
            }

            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }

        /// <summary>
        /// check if adding a second case and removing first one results is selecting only one case with folders and participants from second one.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_TwoCasesSetAndFirstRemoved()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4713);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================
            viewModel.CaseNumber = "00201/14";
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);
            viewModel.CasesToLink[0].RemoveCaseToLinkCommand.Execute(null);

            // ==============================  assert ==============================
            Assert.AreEqual(53, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            notfiyPropertyChangedTestHelper.ResetIndexForAssertRaiseHistory();
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual("CasesToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("FirstCaseToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseNumber", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                if (index == 0) // checkbox LinkToCases is only set for the first case, not the second one
                {
                    Assert.AreEqual("LinkToCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("ViewMode", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                }
                Assert.AreEqual("LinkToCasesHeaderName", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCasesInverted", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseParticipants", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("AddresseeId", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineShares", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesNotAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAllRowsSelected", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("MasterCase", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderSelectionActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderEditButtonActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            }

            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNotNull(viewModel.FirstCaseToLink);
            Assert.AreEqual("00202/14", viewModel.FirstCaseToLink.CaseNumber);
            Assert.AreEqual(2, viewModel.FirstCaseToLink.CaseStateId);
            //TODODMS: muss noch korrekt implementiert werden: Assert.AreEqual(7, viewModel.CaseFolders.Count);
            Assert.AreEqual("00202/14", viewModel.CaseNumber);

            Assert.AreEqual(1, viewModel.CasesToLink.Count);
            var firstCase = viewModel.CasesToLink.First();
            Assert.AreEqual("00202/14", firstCase.CaseNumber);
            Assert.AreEqual(2, firstCase.CaseStateId);

            Assert.AreEqual(4, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual(1000 + index, viewModel.CaseParticipants.Skip(index + 1).First().AddresseeId);
                Assert.AreEqual("Group: Participant " + index, viewModel.CaseParticipants.Skip(index + 1).First().Name);
            }

            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsTrue(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zur Akte: Subject 2 - Cause 2", viewModel.LinkToCasesHeaderName);
            Assert.IsFalse(viewModel.LinkToMultipleCases);

            Assert.AreEqual(3, viewModel.OnlineShares.Count);
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual(3000 + index, viewModel.OnlineShares.Skip(index).First().Id);
                Assert.AreEqual("OAUserName" + index, viewModel.OnlineShares.Skip(index).First().UserName);
            }

            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }

        /// <summary>
        /// check if adding four cases in total and removing one results is selecting three cases with folders and participants from left three ones.
        /// </summary>
        [TestMethod]
        public void DMS_UI_SaveToCase_FourCasesRemovingThird()
        {
            // ============================== arrange ==============================
            var iocContainerMock = MockingHelper.GetIOCContainerMock(5, true);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4713);
            var standardDialogServiceMock = MockingHelper.GetStandardDialogServiceMock();

            var viewModel = new CaseAndAddresseeSelectionViewModel(iocContainerMock, standardDialogServiceMock);

            var notfiyPropertyChangedTestHelper = new NotifyPropertyChangedTestHelper(viewModel);

            // ==============================   act   ==============================
            viewModel.CaseNumber = "00201/14";
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4714);
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);
            MockingHelper.ReRegisterExtendedUIServicesMock(iocContainerMock, 4715);
            viewModel.AddCaseToLinkToCommand.Execute(viewModel);
            viewModel.CasesToLink[2].RemoveCaseToLinkCommand.Execute(null);

            // ==============================  assert ==============================
            Assert.AreEqual(89, notfiyPropertyChangedTestHelper.RaiseHistory.Count);
            notfiyPropertyChangedTestHelper.ResetIndexForAssertRaiseHistory();
            for (int index = 0; index < 3; index++)
            {
                Assert.AreEqual("CasesToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("FirstCaseToLink", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseNumber", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                if (index == 0) // checkbox LinkToCases is only set for the first case, not the second one
                {
                    Assert.AreEqual("LinkToCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                    Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                }
                Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("ViewMode", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToCasesHeaderName", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCases", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("LinkToMultipleCasesInverted", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseFolders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("Folders", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("CaseParticipants", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("AddresseeId", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineShares", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesNotAvailable", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("OnlineSharesAllRowsSelected", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("MasterCase", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderSelectionActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
                Assert.AreEqual("IsFolderEditButtonActive", notfiyPropertyChangedTestHelper.GetNextNameForAssertRaiseHistory());
            }

            Assert.AreEqual(0, viewModel.AddresseeId);
            Assert.IsNotNull(viewModel.FirstCaseToLink);
            Assert.AreEqual("00201/14", viewModel.FirstCaseToLink.CaseNumber);
            Assert.AreEqual(1, viewModel.FirstCaseToLink.CaseStateId);
            //TODODMS: muss noch korrekt implementiert werden: Assert.AreEqual(7, viewModel.CaseFolders.Count);
            Assert.AreEqual("00201/14", viewModel.CaseNumber);

            Assert.AreEqual(3, viewModel.CasesToLink.Count);
            var firstCase = viewModel.CasesToLink.First();
            Assert.AreEqual("00201/14", firstCase.CaseNumber);
            Assert.AreEqual(1, firstCase.CaseStateId);

            var secondCase = viewModel.CasesToLink.Skip(1).First();
            Assert.AreEqual("00202/14", secondCase.CaseNumber);
            Assert.AreEqual(2, secondCase.CaseStateId);

            var thirdCase = viewModel.CasesToLink.Skip(2).First();
            Assert.AreEqual("00204/14", thirdCase.CaseNumber);
            Assert.AreEqual(4, thirdCase.CaseStateId);

            Assert.AreEqual(3, viewModel.CaseParticipants.Count);
            Assert.AreEqual(0, viewModel.CaseParticipants.First().AddresseeId);
            Assert.AreEqual("", viewModel.CaseParticipants.First().Name);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(1000 + index, viewModel.CaseParticipants.Skip(index + 1).First().AddresseeId);
                Assert.AreEqual("Group: Participant " + index, viewModel.CaseParticipants.Skip(index + 1).First().Name);
            }

            Assert.IsTrue(viewModel.ExpanderOnlineSharesVisible);
            Assert.IsFalse(viewModel.LinkToAddressee);
            Assert.IsTrue(viewModel.LinkToCases);
            Assert.AreEqual("Verknüpfung zu 3 Akten", viewModel.LinkToCasesHeaderName);
            Assert.IsTrue(viewModel.LinkToMultipleCases);

            Assert.AreEqual(2, viewModel.OnlineShares.Count);
            for (int index = 0; index < 2; index++)
            {
                Assert.AreEqual(3000 + index, viewModel.OnlineShares.Skip(index).First().Id);
                Assert.AreEqual("OAUserName" + index, viewModel.OnlineShares.Skip(index).First().UserName);
            }

            Assert.AreEqual(false, viewModel.OnlineSharesAllRowsSelected);
        }
    }
}