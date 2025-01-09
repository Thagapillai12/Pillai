using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using WK.DE.AI.BetterCo.ApiClient;
using WK.DE.AI.BetterCo.Facades;
using WK.DE.AI.BetterCo.Json.Case;
using WK.DE.AI.BetterCo.Json.Customer;
using WK.DE.AI.BetterCo.Json.LegalInfo;
using WK.DE.AI.BetterCo.Json.Process;
using WK.DE.AI.BetterCo.Mapping;
using WK.DE.AI.BetterCo.ViewModels;

namespace WK.DE.BetterCo.IntegrationTests
{
	[TestClass]
	public partial class BetterCoAPITests
	{
		private static readonly BetterCoConfiguration betterCoConfiguration 
			= new BetterCoConfiguration() {						// in Postman-COllection verwendet
				Id = "wk_annotext",								//dd_preview
				Key = "defk98jdm88ujkdl78emj8nnd",				//hgjg87fjlshr897jsd34dds
				Secret = "ff7d8bg8E5bnmhre7hnmsdfg546GHFac",	//gh7zh3fuum9kdtwr46vmkl04tg6ud4dc
				WorkspaceName = "Wolters",						// 65956c4b1b901b06757cf4c9
				OrganizationName = "WK"							//65956de31b901b06757cf4d1
			};

		
		/// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void GetWorkspacesTest()
		{
			var workspaces = new BetterCoApiClient(betterCoConfiguration).GetWorkspaces();
			
			Assert.IsNotNull(workspaces);
			Assert.IsTrue(workspaces.Total > 0);
			Assert.IsTrue(workspaces.Results.Count > 0);
			Assert.IsTrue(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetWorkspaceDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var workspaceData = betterCoApiClient.GetWorkspaceData(workspaceId);

			Assert.IsNotNull(workspaceData);
			Assert.IsTrue(workspaceData.Id.Equals(betterCoApiClient.GetWorkspaces().Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id));
			Assert.IsTrue(workspaceData.Name.Length > 0);
			Assert.IsTrue(workspaceData.Status.Length > 0);
			Assert.IsTrue(workspaceData.Type.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateWorkspaceTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var createdWorkspace = betterCoApiClient.CreateWorkspace("TestWorkspace");

			Assert.IsNotNull(createdWorkspace);
			Assert.IsFalse(string.IsNullOrWhiteSpace(createdWorkspace.Id));

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.Id, createdWorkspace.Id)).First().Id;
			Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));

			betterCoApiClient.DeleteWorkspace(workspaceId);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void DeleteWorkspaceTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var createdWorkspace = betterCoApiClient.CreateWorkspace("TestWorkspaceToDelete");

			Assert.IsNotNull(createdWorkspace);
			Assert.IsFalse(string.IsNullOrWhiteSpace(createdWorkspace.Id));

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.Id, createdWorkspace.Id)).First().Id;
			Assert.IsFalse(string.IsNullOrWhiteSpace(workspaceId));

			var response = betterCoApiClient.DeleteWorkspace(workspaceId);

			Assert.IsNotNull(response);
			Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetOrganizationsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);

			Assert.IsNotNull(organizations);
			Assert.IsTrue(organizations.Total > 0);
			Assert.IsTrue(organizations.Results.Count > 0);
			Assert.IsTrue(organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetWorkspaceOrganizationDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var organizationData = betterCoApiClient.GetOrganizationData(workspaceId, organizationId);

			Assert.IsNotNull(organizationData);
			Assert.IsTrue(organizationData.Id.Equals(organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id));
			Assert.IsTrue(organizationData.CreatedAt != null);
			Assert.IsTrue(organizationData.UpdatedAt != null);
			Assert.IsTrue(organizationData.UpdatedBy.Length > 0);
			Assert.IsTrue(organizationData.LegalInfo.LegalName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateWorkspaceOrganizationTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizationCreateResult = betterCoApiClient.CreateWorkspaceOrganization(workspaceId, "TestOrganization");

			Assert.IsNotNull(organizationCreateResult);
			Assert.IsNotNull(organizationCreateResult.Id);

            var createdOrganization = betterCoApiClient.GetOrganizationData(workspaceId, organizationCreateResult.Id);

            Assert.IsNotNull(createdOrganization);
            Assert.IsTrue(createdOrganization.LegalInfo.LegalName.Equals("TestOrganization"));

            betterCoApiClient.DeleteWorkspaceOrganization(workspaceId, organizationCreateResult.Id);
		}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void UpdateWorkspaceOrganizationTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizationCreateResult = betterCoApiClient.CreateWorkspaceOrganization(workspaceId, "TestOrganization");

            Assert.IsNotNull(organizationCreateResult);
            Assert.IsNotNull(organizationCreateResult.Id);

			var createdOrganization = betterCoApiClient.GetOrganizationData(workspaceId, organizationCreateResult.Id);

			Assert.IsNotNull(createdOrganization);
			Assert.IsTrue(createdOrganization.LegalInfo.LegalName.Equals("TestOrganization"));

			betterCoApiClient.UpdateWorkspaceOrganization(workspaceId, createdOrganization.Id, "TestOrganization_Updated");

            var updatedOrganization = betterCoApiClient.GetOrganizationData(workspaceId, organizationCreateResult.Id);

            betterCoApiClient.DeleteWorkspaceOrganization(workspaceId, organizationCreateResult.Id);

            Assert.IsTrue(updatedOrganization.LegalInfo.LegalName.Equals("TestOrganization_Updated"));
        }

        /// <summary>
        /// 
        /// </summary>
        //[TestMethod]
        //public void CreateWorkspaceOrganizationTest()
        //{
        //	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
        //	var workspaces = betterCoApiClient.GetWorkspaces();

        //	var betterCoCreateOrganizationResult = betterCoApiClient.CreateWorkspaceOrganization(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, "Test_WK_KH"); // man kann in einem workspace beliebig viele orgs mit gleichem Namen nanlegen

        //	Assert.IsNotNull(betterCoCreateOrganizationResult);
        //	Assert.IsNotNull(betterCoCreateOrganizationResult.Id);
        //	Assert.IsTrue(betterCoCreateOrganizationResult.Id.Length > 0);
        //}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void DeleteWorkspaceOrganizationTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;
			var createdOrganization = betterCoApiClient.CreateWorkspaceOrganization(workspaceId, "TestOrganizationToDelete");

			Assert.IsNotNull(createdOrganization);
			Assert.IsFalse(string.IsNullOrWhiteSpace(createdOrganization.Id));

			var response = betterCoApiClient.DeleteWorkspaceOrganization(workspaceId, createdOrganization.Id);
            betterCoApiClient.DeleteWorkspaceOrganization(workspaceId, "672228c8cbf47464f2232f98");

            Assert.IsNotNull(response);
			Assert.IsTrue(response.StatusCode == System.Net.HttpStatusCode.OK);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersWithClosedProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersWithClosedProcessF710_ConflictCheck(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersWithClosedProcessesTest2()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersWithClosedProcessF700_RiskEvaluation(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			//Assert.IsTrue(customers.Total > 0);
			//Assert.IsTrue(customers.Results.Count > 0);
			//Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersWithClosedProcessesTest3()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersWithClosedProcessF1900_OnboardingIndividual_D(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			//Assert.IsTrue(customers.Total > 0);
			//Assert.IsTrue(customers.Results.Count > 0);
			//Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		public void GetCustomersWithClosedProcessesTest4()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersWithClosedProcessF1800_OnboardingEntity_A(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			//Assert.IsTrue(customers.Total > 0);
			//Assert.IsTrue(customers.Results.Count > 0);
			//Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		public void GetCustomersWithClosedProcessesTest5()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;
			
			var customers = betterCoApiClient.GetCustomersWithClosedProcessF1400_RiskEvaluation(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			//Assert.IsTrue(customers.Total > 0);
			//Assert.IsTrue(customers.Results.Count > 0);
			//Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersWithClosedProcessesForAllProcessSpecsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;
			
			var customers = betterCoApiClient.GetCustomersWithClosedProcessesForAllProcessSpecs(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			//Assert.IsTrue(customers.Total > 0);
			//Assert.IsTrue(customers.Results.Count > 0);
			//Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersChangedSinceTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;
			
			var customers = betterCoApiClient.GetCustomersChangesSince(workspaceId, organizationId, DateTime.Now.AddDays(-120));

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersChangesSinceWithRequiredProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersChangesSinceWithRequiredProcesses(workspaceId, organizationId, DateTime.Now.AddDays(-200), new List<string>() { "F710_ConflictCheck" });

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersChangesSinceWithRequiredProcessF60045_SearchEntityIndividualWK_Page2Test()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersChangesSinceWithRequiredProcessF60045_SearchEntityIndividualWK_Page2(workspaceId, organizationId, DateTime.Now.AddDays(-200));

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomersNotYetImportedTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomersNotYetImported(workspaceId, organizationId);

			// wie kommt man von customer wieder an process ran?

			Assert.IsNotNull(customers);
			Assert.IsTrue(customers.Total > 0);
			Assert.IsTrue(customers.Results.Count > 0);
			Assert.IsTrue(customers.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		///	Auskommentiert, da derzeit via API nicht umkehrbar 
		/// </summary>
		//[TestMethod]
		//public void MarkCustomerAsImportedTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
		//	var workspaces = betterCoApiClient.GetWorkspaces();

		//	var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);

		//	var customers = betterCoApiClient.GetCustomers(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id);

		//	var customerMarkedAsImported = betterCoApiClient.MarkCustomerAsImported(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customers.Results.First().Id, "123456");

		//	Assert.IsNotNull(customerMarkedAsImported);
		//}

		[TestMethod]
		public void MarkCustomerAsImportedTest2()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customerToCreate = new BetterCoCreateCustomerData() { LegalInfo = new BetterCoLegalInfo() { FirstName = "Pater", LastName = "Noster" } };

			var createdCustomerId = betterCoApiClient.CreateIndiviualCustomer(workspaceId, organizationId,customerToCreate);

			var customerData = betterCoApiClient.GetCustomerData(workspaceId, organizationId, createdCustomerId.Id);
			Assert.IsNotNull(customerData);
			Assert.IsFalse(customerData.ExternalIdentifiers.Any());

			var notYetImportedCustomers = betterCoApiClient.GetCustomersNotYetImported(workspaceId, organizationId);
			Assert.IsTrue(notYetImportedCustomers.Results.Where(x => x.Id == createdCustomerId.Id).Any());

			var customerMarkedAsImported = betterCoApiClient.MarkCustomerAsImported(
				workspaceId,
				organizationId,
				createdCustomerId.Id,
				"12345"
			);
			Assert.IsNotNull(customerMarkedAsImported);
			Assert.IsTrue(customerMarkedAsImported.ExternalIdentifiers.Any());
			Assert.IsTrue(customerMarkedAsImported.ExternalIdentifiers.First().ExternalId.Equals("12345", StringComparison.Ordinal));
			var notYetImportedCustomers2 = betterCoApiClient.GetCustomersNotYetImported(workspaceId, organizationId);
			Assert.IsFalse(notYetImportedCustomers2.Results.Where(x => x.Id == createdCustomerId.Id).Any());

			betterCoApiClient.DeleteCustomer(workspaceId, organizationId, createdCustomerId.Id);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerData = betterCoApiClient.GetCustomerData(workspaceId, organizationId, customers.Results.First().Id);

			Assert.IsNotNull(customerData);
			//Assert.IsTrue(customerData.Id.Equals(customers.Results.First().Id)); warum unterscheiden die sich???
			Assert.IsTrue(customerData.LegalInfo.LegalName.Length > 0);
			Assert.IsTrue(customerData.LegalInfo.LegalName.Equals(customers.Results.First().DisplayName));
			Assert.IsTrue(customerData.CreatedAt != null);
			Assert.IsTrue(customerData.UpdatedAt != null);
			Assert.IsTrue(customerData.UpdatedBy.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDataByUrlTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerData = betterCoApiClient.GetCustomerDataByUrl(customers.Results.First().Url);

			Assert.IsNotNull(customerData);
			//Assert.IsTrue(customerData.Id.Equals(customers.Results.First().Id)); warum unterscheiden die sich???
			Assert.IsTrue(customerData.LegalInfo.LegalName.Length > 0);
			Assert.IsTrue(customerData.LegalInfo.LegalName.Equals(customers.Results.First().DisplayName));
			Assert.IsTrue(customerData.CreatedAt != null);
			Assert.IsTrue(customerData.UpdatedAt != null);
			Assert.IsTrue(customerData.UpdatedBy.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDocumentsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerDocuments = betterCoApiClient.GetCustomerDocuments(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			Assert.IsNotNull(customerDocuments);
			Assert.IsTrue(customerDocuments.Results.Count > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDocumentDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerDocuments = betterCoApiClient.GetCustomerDocuments(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var customerDocumentData = betterCoApiClient.GetCustomerDocumentData(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, customerDocuments.Results.First().Id);

			Assert.IsNotNull(customerDocumentData);
			Assert.IsTrue(customerDocumentData.DownloadURI.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDocumentAsDownloadTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerDocuments = betterCoApiClient.GetCustomerDocuments(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var customerDocumentDownload = betterCoApiClient.GetCustomerDocumentAsDownload(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, customerDocuments.Results.First().Id);

			Assert.IsNull(customerDocumentDownload);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerDocumentByDownloadUriTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerDocuments = betterCoApiClient.GetCustomerDocuments(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var customerDocumentData = betterCoApiClient.GetCustomerDocumentData(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, customerDocuments.Results.First().Id);

			var customerDocumentDownload = betterCoApiClient.GetDocumentByDownloadUri(customerDocumentData.DownloadURI, customerDocumentData.FileName);

			Assert.IsNotNull(customerDocumentDownload);
			Assert.IsNotNull(customerDocumentDownload.DocumentStream);

		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerContacts = betterCoApiClient.GetCustomerContacts(workspaceId, organizationId, customers.Results.First().Id);

			Assert.IsNotNull(customerContacts);
			Assert.IsTrue(customerContacts.Results.Count > 0);
			Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactsFilteredByLegalRepsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

				var customerContacts = betterCoApiClient.GetCustomerContactsLegalReps(
				workspaceId,
				organizationId,
				customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(customerContacts);
			//Assert.IsTrue(customerContacts.Results.Count > 0);
			//Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactsFilteredByLegalRepIdTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerContacts = betterCoApiClient.GetCustomerContactsLegalRepsByRelationId(
			workspaceId,
			organizationId,
			customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).Skip(1).First().Id
		);

			Assert.IsNotNull(customerContacts);
			//Assert.IsTrue(customerContacts.Results.Count > 0);
			//Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		}

		///// <summary>
		///// 
		///// </summary>
		//[TestMethod]
		//public void GetCustomerContactsFilteredByLegalRepsAndUbosTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
		//	var workspaces = betterCoApiClient.GetWorkspaces();
		//	var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

		//	var organizations = betterCoApiClient.GetOrganizations(workspaceId);
		//	var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

		//	var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

		//	var customerContacts = betterCoApiClient.GetCustomerContactsByRelationIds(workspaceId, organizationId, customers.Results.First().Id);

		//	Assert.IsNotNull(customerContacts);
		//	//Assert.IsTrue(customerContacts.Results.Count > 0);
		//	//Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		//}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactsFilteredByUbosTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerContacts = betterCoApiClient.GetCustomerContactsUbos(workspaceId, organizationId, customers.Results.First().Id);

			Assert.IsNotNull(customerContacts);
			//Assert.IsTrue(customerContacts.Results.Count > 0);
			//Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactsFilteredByUbosIdTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerContacts = betterCoApiClient.GetCustomerContactsUbosByRelationId(workspaceId, organizationId, customers.Results.First().Id);

			Assert.IsNotNull(customerContacts);
			//Assert.IsTrue(customerContacts.Results.Count > 0);
			//Assert.IsTrue(customerContacts.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerContactDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var customerContacts = betterCoApiClient.GetCustomerContacts(workspaceId, organizationId, customers.Results.First().Id);

			var contactData = betterCoApiClient.GetCustomerContactData(workspaceId, organizationId, customers.Results.First().Id, customerContacts.Results.First().Id);

			Assert.IsNotNull(contactData);
			//Assert.IsTrue(customerData.Id.Equals(customers.Results.First().Id)); warum unterscheiden die sich???
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateMinimumIndiviualCustomerTest()
		{
			//var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			//var workspaces = betterCoApiClient.GetWorkspaces();
			//var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);

			//var customerToCreate = new BetterCoCreateCustomerData() { LegalInfo = new BetterCoLegalInfo() { FirstName = "Pater", LastName = "Noster" } };

			//var createdCustomerId = betterCoApiClient.CreateIndiviualCustomer(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customerToCreate);

			//Assert.IsNotNull(createdCustomerId);
			//Assert.IsFalse(string.IsNullOrWhiteSpace(createdCustomerId.Id));
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateIndiviualCustomerTest()
		{
			//var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			//var workspaces = betterCoApiClient.GetWorkspaces();
			//var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);

			//var customerToCreate = 
			//	new BetterCoCreateCustomerData() { 
			//		LegalInfo = new BetterCoLegalInfo() {
			//			FirstName = "Pater",
			//			LastName = "Noster",
			//			BirthDate = "1966-08-24",
			//			BirthPlace = "New City",
			//			Salutation = "Mr.",
			//			Gender = "OTHER",
			//			Nationality = "DE"
			//		} 
			//};

			//var createdCustomerId = betterCoApiClient.CreateIndiviualCustomer(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customerToCreate);

			//Assert.IsNotNull(createdCustomerId);
			//Assert.IsFalse(string.IsNullOrWhiteSpace(createdCustomerId.Id));
		}

		/// <summary>
		/// BetterCoCustomerData verwenden geht nicht, da wird id angemoppert
		/// </summary>
		//[TestMethod]
		//public void CreateIndiviualCustomerTest2()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
		//	var workspaces = betterCoApiClient.GetWorkspaces();

		//	var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);
		//	var customerToCreate =
		//		new BetterCoCustomerData()
		//		{
		//			LegalInfo = new BetterCoLegalInfo()
		//			{
		//				FirstName = "Pater",
		//				LastName = "Noster",
		//				BirthDate = "1966-08-24",
		//				BirthPlace = "New City",
		//				Salutation = "Mr.",
		//				Gender = "OTHER",
		//				Nationality = "DE"
		//			}
		//		};

		//	var createdCustomerId = betterCoApiClient.CreateIndiviualCustomer2(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customerToCreate);

		//	Assert.IsNotNull(createdCustomerId);
		//	Assert.IsFalse(string.IsNullOrWhiteSpace(createdCustomerId.Id));
		//}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateMinimumEntityCustomerTest()
		{
			//var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			//var workspaces = betterCoApiClient.GetWorkspaces();

			//var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);
			//var customerToCreate = new BetterCoCreateCustomerData() {
			//	LegalInfo = new BetterCoLegalInfo() {
			//		LegalName = "The API-GmbH" 
			//	}
			//};

			//var createdCustomerId = betterCoApiClient.CreateEntityCustomer(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customerToCreate);

			//Assert.IsNotNull(createdCustomerId);
			//Assert.IsFalse(string.IsNullOrWhiteSpace(createdCustomerId.Id));
		}

		/// <summary>
		/// Geht nicht "error":"Bad Request","status":400,"message":"Cannot read JSON. Invalid fields: ['legalInfo','legalType','additionalData']."
		/// </summary>
		[TestMethod]
		public void CreateEntityCustomerTest()
		{
			//var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			//var workspaces = betterCoApiClient.GetWorkspaces();
			//var organizations = betterCoApiClient.GetOrganizations(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id);

			//var customerToCreate = new BetterCoCreateCustomerData() {
			//	LegalInfo = new BetterCoLegalInfo() {
			//		LegalName = "The API-GmbH",
			//		LegalType =  new BetterCoLegalType() {
			//			Id = "1070"
			//			//,
			//			//AdditionalData = new BetterCoAdditionalData()
			//		},
			//		RegisterData = new List<BetterCoRegisterData>() {
			//			new BetterCoRegisterData() {
			//				Subject = "something",
			//				RegisterCountry = "DE",
			//				RegisterZone = "Charlottenburg (Berlin)",
			//				RegisterId = "HRB 161161 B",
			//				CurrentRegisterDate = "1961-10-08",
			//				RegisterLegalName = "API GmbH",
			//				RegisterLegalType = "GmbH",
			//				RegisterStatus = "ACTIVE"
			//			}
			//		}
			//	} 
			//};

			//var createdCustomerId = betterCoApiClient.CreateEntityCustomer(workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id, organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id, customerToCreate);

			//Assert.IsNotNull(createdCustomerId);
			//Assert.IsFalse(string.IsNullOrWhiteSpace(createdCustomerId.Id));
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateCustomerCaseTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var cases = betterCoApiClient.GetCases(
				workspaceId,
				organizationId,
				customerId
			);

			Assert.IsNotNull(cases);
			Assert.IsTrue(cases.Total > 0);
			Assert.IsTrue(cases.Results.Count > 0);
			Assert.IsTrue(cases.Results.First().DisplayName.Length > 0);
			Assert.IsTrue(cases.Results.Where(x => string.Equals(x.Id, createdCustomerCaseResult.Id)).Any());

			betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id);
		}

		[TestMethod]
		public void DeleteCustomerTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customerToCreate = new BetterCoCreateCustomerData() { LegalInfo = new BetterCoLegalInfo() { FirstName = "Pater", LastName = "Noster" } };

			var createdCustomerId = betterCoApiClient.CreateIndiviualCustomer(workspaceId, organizationId, customerToCreate);

			var deleteCustomerResponse = betterCoApiClient.DeleteCustomer(workspaceId, organizationId, createdCustomerId.Id);
			Assert.IsNotNull(deleteCustomerResponse);

			var deletedCutomerData = betterCoApiClient.GetCustomerData(workspaceId, organizationId, createdCustomerId.Id);
			Assert.IsNull(deletedCutomerData);
		}

        [TestMethod]
        public void DeleteCustomerWithForceTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var deleteCustomerResponse = betterCoApiClient.DeleteCustomerWithForce("654628ab501f682f6ce8d077", "6548ba7bfe5d6647a4925154", "65f1bc5da0f7e5353998c82a"); // Wolters N2
            //Assert.IsNotNull(deleteCustomerResponse);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void GetCasesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(
				workspaceId,
				organizationId,
				customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(cases);
			Assert.IsTrue(cases.Total > 0);
			Assert.IsTrue(cases.Results.Count > 0);
			Assert.IsTrue(cases.Results.First().DisplayName.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCaseDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(
				workspaceId,
				organizationId,
				customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id
			);

			var @case = betterCoApiClient.GetCaseData(
				workspaceId, 
				organizationId,
				customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id,
				cases.Results.First().Id
			);

			Assert.IsNotNull(@case);
			Assert.IsTrue(@case.Status.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetProcesses(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Total > 0);
			Assert.IsTrue(processes.Results.Count > 0);
			Assert.IsTrue(processes.Results.First().DisplayName.Length > 0);
            Assert.IsTrue(processes.Results.First().Url.Contains("/cases/"));
        }

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerCaseOpenProcesses()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetCustomerCaseOpenProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id,
				cases.Results.First().Id
			);

			var firstFoundOpenProcess = betterCoApiClient.GetProcessData(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id, processes.Results.First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Total > 0);
			Assert.IsTrue(processes.Results.Count > 0);
			Assert.IsTrue(processes.Results.First().DisplayName.Length > 0);
			Assert.IsTrue(firstFoundOpenProcess.Status.Equals("OPEN"));
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetAllCustomerProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetAllCustomerProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => new List<string>() { "OPEN", "CLOSED", "COMPLETED" }.Contains(x.Status)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerOpenProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerOpenProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerClosedProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerClosedProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}
			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerCompletedProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerCompletedProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}
			
			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("COMPLETED", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}


		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerShareableProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerShareableProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => x.Token.Token.Length > 0).ToList().Count == allProcessData.Count);// Ist das eindeutig für Shared?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerNonShareableProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerNonShareableProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => x.Token.Token.Length > 0).ToList().Count == allProcessData.Count);// Ist das eindeutig für Shared?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerShareableOpenProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerShareableOpenProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => x.Token.Token.Length > 0).ToList().Count == allProcessData.Count);// Ist das eindeutig für Shared?		}
			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetCustomerShareableClosedProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var processes = betterCoApiClient.GetCustomerShareableClosedProcesses(
				workspaceId,
				organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id
			);

			Assert.IsNotNull(processes);
			Assert.IsTrue(processes.Count > 0);

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in processes)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Where(x => x.Token.Token.Length > 0).ToList().Count == allProcessData.Count);// Ist das eindeutig für Shared?		}
			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetAllCustomersShareableOpenProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var allCustomersShareableOpenProcesses = new List<CustomerCaseProcessResult>();
			foreach (var customer in customers.Results)
			{
				var customerCaseProcesses = betterCoApiClient.GetCustomerShareableOpenProcesses(
					workspaceId,
					organizationId,
					customer.Id
				);
				if (customerCaseProcesses != null && customerCaseProcesses.Any())
				{
					allCustomersShareableOpenProcesses.AddRange(customerCaseProcesses);
				}
			}

			var allProcessData = new List<BetterCoProcessData>();
			foreach (var customerCaseProcess in allCustomersShareableOpenProcesses)
			{
				allProcessData.Add(betterCoApiClient.GetProcessData(workspaceId, organizationId, customerCaseProcess.CustomerId, customerCaseProcess.CaseId, customerCaseProcess.ProcessId));
			}

			Assert.IsTrue(allProcessData.Any());
			Assert.IsTrue(allProcessData.Where(x => x.Token.Token.Length > 0).ToList().Count == allProcessData.Count);// Ist das eindeutig für Shared?		}
			Assert.IsTrue(allProcessData.Where(x => x.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase)).ToList().Count == allProcessData.Count);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetProcesses(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id);

			var processData = betterCoApiClient.GetProcessData(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id, processes.Results.First().Id);

			Assert.IsNotNull(processData);
			Assert.IsTrue(processData.Id.Equals(processes.Results.First().Id));
			//Assert.IsTrue(processData.Name.Equals(processes.Results.First().DisplayName)); // was sollte gleich sein?
			//Assert.IsTrue(processData.Description.Equals(processes.Results.First().DisplayName)); // was sollte gleich sein?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetAvailableProcessesTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var availableProcesses = betterCoApiClient.GetAvailableProcesses(workspaceId);

			Assert.IsNotNull(availableProcesses);
			Assert.IsTrue(availableProcesses.Count > 0);
			Assert.IsTrue(availableProcesses.First().Shareable == true || availableProcesses.First().Shareable == false);
		}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetAvailableProcessesGroupedTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var availableGroupedProcessTypes = betterCoApiClient.GetAvailableProcessesGrouped(workspaceId);

            Assert.IsNotNull(availableGroupedProcessTypes);
            Assert.IsTrue(availableGroupedProcessTypes.Count > 0);
            Assert.IsTrue(availableGroupedProcessTypes.First().BetterCoProcessTypes.First().Shareable == true || availableGroupedProcessTypes.First().BetterCoProcessTypes.First().Shareable == false);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void InstantiateOrganizationProcessTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var createdPorcessId = betterCoApiClient.InstantiateOrganizationProcess(
				workspaceId,
				organizationId,
				"F600_WebSignup"); // F161_Mandantenanlage wie im PDF steht geht nicht?

			Assert.IsNotNull(createdPorcessId);
			Assert.IsFalse(string.IsNullOrWhiteSpace(createdPorcessId.Id));
		}

		/// <summary>
		/// 
		/// </summary>
		//[TestMethod]
		//public void InstantiateOrganizationWebSignUpProcessTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
		//	var workspaces = betterCoApiClient.GetWorkspaces();
		//	var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

		//	var organizations = betterCoApiClient.GetOrganizations(workspaceId);
		//	var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

		//	var createdWebSignUpUrl = betterCoApiClient.InstantiateOrganizationWebSignUpProcess(
		//		workspaceId,
		//		organizationId,
		//		"", "");

		//	Assert.IsNotNull(createdWebSignUpUrl);
		//	Assert.IsFalse(string.IsNullOrWhiteSpace(createdWebSignUpUrl));
		//	Assert.IsTrue(createdWebSignUpUrl.StartsWith("https://"));
		//}

		/// <summary>
		/// 
		/// </summary>
		//[TestMethod]
		//public void InstantiateOrganizationSearchIndividualProcessTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
		//	var workspaces = betterCoApiClient.GetWorkspaces();
		//	var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

		//	var organizations = betterCoApiClient.GetOrganizations(workspaceId);
		//	var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

		//	var createdWebSignUpUrl = betterCoApiClient.InstantiateOrganizationSearchIndividualProcess(
		//		workspaceId,
		//		organizationId,
		//		"", "");

		//	Assert.IsNotNull(createdWebSignUpUrl);
		//	Assert.IsFalse(string.IsNullOrWhiteSpace(createdWebSignUpUrl));
		//	Assert.IsTrue(createdWebSignUpUrl.StartsWith("https://"));
		//}

		/// <summary>
		/// 
		/// </summary>
		//[TestMethod]
		//public void InstantiateOrganizationSearchEntityProcessTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			
		//	var workspaces = betterCoApiClient.GetWorkspaces();
		//	var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

		//	var organizations = betterCoApiClient.GetOrganizations(workspaceId);
		//	var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

		//	var createdWebSignUpUrl = betterCoApiClient.InstantiateOrganizationSearchEntityProcess(
		//		workspaceId,
		//		organizationId,
		//		"", "");

		//	Assert.IsNotNull(createdWebSignUpUrl);
		//	Assert.IsFalse(string.IsNullOrWhiteSpace(createdWebSignUpUrl));
		//	Assert.IsTrue(createdWebSignUpUrl.StartsWith("https://"));
		//}

		/// <summary>
		/// 
		/// </summary>
		//[TestMethod]
		//public void InstantiateOrganizationSearchEntityIndividualWKProcessTest()
		//{
		//	var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

		//	var workspaces = betterCoApiClient.GetWorkspaces();
		//	var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

		//	var organizations = betterCoApiClient.GetOrganizations(workspaceId);
		//	var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

		//	var createdWebSignUpUrl = betterCoApiClient.InstantiateOrganizationSearchEntityIndividualWKProcess(workspaceId, organizationId);

		//	Assert.IsNotNull(createdWebSignUpUrl);
		//	Assert.IsFalse(string.IsNullOrWhiteSpace(createdWebSignUpUrl));
		//	Assert.IsTrue(createdWebSignUpUrl.StartsWith("https://"));
		//}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void InstantiateOrganizationCreateEntityIndividualWKProcessTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var createProcessResult = betterCoApiClient.InstantiateOrganizationCreateEntityIndividualWKProcess(workspaceId, organizationId);

			Assert.IsNotNull(createProcessResult);
			Assert.IsFalse(string.IsNullOrWhiteSpace(createProcessResult.ProcessId));
			Assert.IsFalse(string.IsNullOrWhiteSpace(createProcessResult.ShareUrl));
			Assert.IsTrue(createProcessResult.ShareUrl.StartsWith("https://"));
		}
		
		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateCustomerCaseProcessTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F1400_RiskEvaluation");

			Assert.IsNotNull(createdCustomerCaseProcessResult);
			Assert.IsNotNull(createdCustomerCaseProcessResult.Id);

			var processData = betterCoApiClient.GetProcessData(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsNotNull(processData);
			Assert.IsNotNull(processData.Id);
			Assert.IsNotNull(processData.Token);
			Assert.IsTrue(processData.Token.IsExpired == false);

			betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
		}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void CloseProcessTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
            var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

            var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

            var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F1400_RiskEvaluation");

            Assert.IsNotNull(createdCustomerCaseProcessResult);
            Assert.IsNotNull(createdCustomerCaseProcessResult.Id);

            var processData = betterCoApiClient.GetProcessData(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

            Assert.IsNotNull(processData);
            Assert.IsNotNull(processData.Id);
            Assert.IsNotNull(processData.Token);
            Assert.IsTrue(processData.Token.IsExpired == false);

			var closeProcessResult = betterCoApiClient.CloseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, processData.Id);

            Assert.IsNotNull(closeProcessResult);
			Assert.IsTrue(closeProcessResult.Contains("Fehler beim Schließen des Prozesses"));

            betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void InvalidateProcessTokenTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F1400_RiskEvaluation");

			var processData = betterCoApiClient.GetProcessData(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsNotNull(processData);
			Assert.IsNotNull(processData.Id);
			Assert.IsNotNull(processData.Token);
			Assert.IsTrue(processData.Token.IsExpired == false);

			var betterCoProcessToken = betterCoApiClient.InvalidateProcessToken(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, processData.Id);
			
			Assert.IsNotNull(betterCoProcessToken);
			Assert.IsTrue(betterCoProcessToken.IsExpired == true);

			betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void SetProcessTokenTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F1400_RiskEvaluation");

			var betterCoProcessToken = betterCoApiClient.InvalidateProcessToken(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsNotNull(betterCoProcessToken);
			Assert.IsTrue(betterCoProcessToken.IsExpired);

			var betterCoInstantiateProcessResultToken = betterCoApiClient.Set12HoursExpiryTimeForProcessToken(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsNotNull(betterCoInstantiateProcessResultToken);
			Assert.IsFalse(betterCoInstantiateProcessResultToken.IsExpired);

			var processData = betterCoApiClient.GetProcessData(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsNotNull(processData);
			Assert.IsNotNull(processData.Id);
			Assert.IsNotNull(processData.Token);
			Assert.IsFalse(processData.Token.IsExpired);

			betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void CreateProcessShareTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;
			
			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F6300_GeneralConcern");

			var result = betterCoApiClient.CreateProcessShare(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			Assert.IsTrue(result != null);

			betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessShareTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);
			var customerId = customers.Results.Where(x => string.Equals(x.DisplayName, "Founders1 GmbH")).First().Id;

			var createdCustomerCaseResult = betterCoApiClient.CreateCustomerCase(workspaceId, organizationId, customerId, "Testakte", "Testbeschreibung");

			var createdCustomerCaseProcessResult = betterCoApiClient.CreateCustomerCaseProcess(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, "F1400_RiskEvaluation");

			var processShareId = betterCoApiClient.CreateProcessShare(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id);

			var result = betterCoApiClient.GetProcessShare(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id, createdCustomerCaseProcessResult.Id, processShareId);

			Assert.IsTrue(result != null);
			Assert.IsTrue("DEF".Equals(result.Language, StringComparison.Ordinal));
            Assert.IsTrue(result.MfaEnabled == true);
            Assert.IsTrue(result.MfaCode.Length > 0);

            betterCoApiClient.DeleteCase(workspaceId, organizationId, customerId, createdCustomerCaseResult.Id); // werden Prozesse mitgelöscht?
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessDocumentsTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetProcesses(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id);

			var processDocuments = betterCoApiClient.GetProcessDocuments(
				workspaceId, organizationId, 
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, 
				cases.Results.First().Id,
				processes.Results.First().Id
			);
			Assert.IsNotNull(processDocuments);
			Assert.IsTrue(processDocuments.Results.Count > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessDocumentDataTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetProcesses(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id);

			var processDocuments = betterCoApiClient.GetProcessDocuments(
				workspaceId, organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id,
				cases.Results.First().Id,
				processes.Results.First().Id
			);

			var processDocumentData = betterCoApiClient.GetProcessDocumentData(
				workspaceId, organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id,
				cases.Results.First().Id,
				processes.Results.First().Id,
				processDocuments.Results.First().Id
			);

			Assert.IsNotNull(processDocumentData);
			Assert.IsTrue(processDocumentData.DownloadURI.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetProcessDocumentByDownloadUriTest()
		{
			var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
			var workspaces = betterCoApiClient.GetWorkspaces();
			var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

			var organizations = betterCoApiClient.GetOrganizations(workspaceId);
			var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

			var customers = betterCoApiClient.GetCustomers(workspaceId, organizationId);

			var cases = betterCoApiClient.GetCases(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id);

			var processes = betterCoApiClient.GetProcesses(workspaceId, organizationId, customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id, cases.Results.First().Id);

			var processDocuments = betterCoApiClient.GetProcessDocuments(
				workspaceId, organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id,
				cases.Results.First().Id,
				processes.Results.First().Id
			);

			var processDocumentData = betterCoApiClient.GetProcessDocumentData(
				workspaceId, organizationId,
				customers.Results.Where(x => x.DisplayName.Equals("Founders1 GmbH")).First().Id,
				cases.Results.First().Id,
				processes.Results.First().Id,
				processDocuments.Results.First().Id
			);

			var customerDocumentDownload = betterCoApiClient.GetDocumentByDownloadUri(processDocumentData.DownloadURI, processDocumentData.FileName);

			Assert.IsNotNull(customerDocumentDownload);
			Assert.IsNotNull(customerDocumentDownload.DocumentStream);
			Assert.IsTrue(customerDocumentDownload.DocumentStream.Length > 0);
		}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetCustomersProcessesTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            var processes = betterCoApiClient.GetCustomersProcesses(workspaceId, organizationId);

            Assert.IsNotNull(processes);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetCustomersOpenProcessesTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            var processes = betterCoApiClient.GetCustomersOpenProcesses(workspaceId, organizationId);

            Assert.IsNotNull(processes);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetCustomersClosedProcessesTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            var processes = betterCoApiClient.GetCustomersClosedProcesses(workspaceId, organizationId);

            Assert.IsNotNull(processes);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetCustomersProcessesByTypeTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            var processes = betterCoApiClient.GetCustomersProcessesByType(workspaceId, organizationId, "F60045_SearchEntityIndividualWK_Page2");
			// F1400_RiskEvaluation, F6300_GeneralConcern, F1800_OnboardingEntity_A

            Assert.IsNotNull(processes);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetCustomersProcessesByStateAndTypeTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);
            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var organizations = betterCoApiClient.GetOrganizations(workspaceId);
            var organizationId = organizations.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.OrganizationName)).First().Id;

            //var processes = betterCoApiClient.GetCustomersProcessesByStateAndType(workspaceId, organizationId, "CLOSED", "F1400_RiskEvaluation");
            var processes = betterCoApiClient.GetCustomersProcessesByStateAndType(workspaceId, organizationId, "OPEN", "F60045_SearchEntityIndividualWK_Page2");

            Assert.IsNotNull(processes);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void GetRelationTypesTest()
		{
			var relationTypes = new BetterCoApiClient(betterCoConfiguration).GetRelationTypes();

			Assert.IsNotNull(relationTypes);
			Assert.IsNotNull(relationTypes.Count > 0);
			Assert.IsNotNull(relationTypes.First().Id.Length > 0);
			Assert.IsNotNull(relationTypes.First().DeValue.Length > 0);
			Assert.IsNotNull(relationTypes.First().EnValue.Length > 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void GetLegalFormTypesTest()
		{
			var legalFormTypes = new BetterCoApiClient(betterCoConfiguration).GetLegalFormTypes();

			Assert.IsNotNull(legalFormTypes);
			Assert.IsNotNull(legalFormTypes.Count > 0);
			Assert.IsNotNull(legalFormTypes.First().Id.Length > 0);
			Assert.IsNotNull(legalFormTypes.First().DeValue.Length > 0);
			Assert.IsNotNull(legalFormTypes.First().EnValue.Length > 0);
		}

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
        public void GetDocumentTypesTest()
        {
            var documentTypes = new BetterCoApiClient(betterCoConfiguration).GetDocumentTypes();

            Assert.IsNotNull(documentTypes);
            Assert.IsNotNull(documentTypes.Count > 0);
            Assert.IsNotNull(documentTypes.First().Id.Length > 0);
            Assert.IsNotNull(documentTypes.First().DeValue.Length > 0);
            Assert.IsNotNull(documentTypes.First().EnValue.Length > 0);
        }

        /// <summary>
        /// 
        /// </summary>
        [TestMethod]
		public void GetSearchCompanyResults()
		{
			var searchResults = new BetterCoApiClient(betterCoConfiguration).GetSearchCompanies();

			Assert.IsNotNull(searchResults); // liefert manchmal null
			//Assert.IsNotNull(searchResults.Count == 0);
		}

		/// <summary>
		/// 
		/// </summary>
		[TestMethod]
		public void KeinApiTest_MussHierRaus()
		{
			BetterCoCaseAML caseAML = new BetterCoCaseAML
			{
				FinancialSectorsApplicable = true
			};

			BetterCoFinancialSector financialSector1 = new BetterCoFinancialSector
			{
				DeTitle = "Test1"
			};

			BetterCoFinancialSector financialSector2 = new BetterCoFinancialSector
			{
				DeTitle = "Test2"
			};

			caseAML.FinancialSectors = new List<BetterCoFinancialSector> () { financialSector1, financialSector2 };

			var result = BetterCoJsonDataToBetterCoViewModel.GetCatalogueBusinessesForDisplay(caseAML);

			Assert.IsNotNull(result);
			Assert.IsFalse(string.IsNullOrEmpty(result));
			Assert.IsTrue(result.Contains("/"));
		}

    }
}
