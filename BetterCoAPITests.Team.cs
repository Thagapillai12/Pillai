using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WK.DE.AI.BetterCo.ApiClient;
using WK.DE.AI.BetterCo.ViewModels;

namespace WK.DE.BetterCo.IntegrationTests
{
    public partial class BetterCoAPITests
    {
        [TestMethod]
        public void CreateAgentTeamMemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var createdTeammember = betterCoApiClient.CreateAgentTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void CreateManagerTeamMemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var createdTeammember = betterCoApiClient.CreateManagerTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void UpdateTeamMemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            {
                betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            }

            var createdTeammember = betterCoApiClient.CreateManagerTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var updatedTeammember = betterCoApiClient.UpdateTeamMember(workspaceId, createdTeammember.Id, "Maxi", "Mustermanni", "maxi.mustermanni@muster.de", "AGENT","ext1i");

            Assert.IsTrue(updatedTeammember.Id.Equals(createdTeammember.Id, StringComparison.Ordinal));
            Assert.IsTrue("Maxi".Equals(updatedTeammember.FirstName, StringComparison.Ordinal));
            Assert.IsTrue("Mustermanni".Equals(updatedTeammember.LastName, StringComparison.Ordinal));
            Assert.IsTrue("maxi.mustermanni@muster.de".Equals(updatedTeammember.Email, StringComparison.Ordinal));
            Assert.IsTrue("ext1i".Equals(updatedTeammember.ExternalId, StringComparison.Ordinal));
            Assert.IsNotNull(updatedTeammember.CreatedAt);
            //Assert.IsTrue(updatedTeammember.CreatedAt < DateTime.Now);
            //Assert.IsTrue(updatedTeammember.CreatedAt > DateTime.Now.AddSeconds(-2));
            Assert.IsTrue("AGENT".Equals(updatedTeammember.Role, StringComparison.Ordinal));

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetTeammembersTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            var createdTeammember = betterCoApiClient.CreateManagerTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var teammembers = betterCoApiClient.GetTeammembers(workspaceId);

            Assert.IsNotNull(teammembers);
            Assert.IsTrue(teammembers.Results.Count == 1);

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetManagerTeammemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            //foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            //{
            //    betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            //}

            var createdTeammember = betterCoApiClient.CreateManagerTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var teammember = betterCoApiClient.GetTeammember(workspaceId, createdTeammember.Id);

            Assert.IsNotNull(teammember);
            Assert.IsTrue(teammember.Id.Equals(createdTeammember.Id, StringComparison.Ordinal));
            Assert.IsTrue("Max".Equals(teammember.FirstName, StringComparison.Ordinal));
            Assert.IsTrue("Mustermann".Equals(teammember.LastName, StringComparison.Ordinal));
            Assert.IsTrue("max.mustermann@muster.de".Equals(teammember.Email, StringComparison.Ordinal));
            Assert.IsTrue("ext1".Equals(teammember.ExternalId, StringComparison.Ordinal));
            Assert.IsNotNull(teammember.CreatedAt);
            Assert.IsTrue(teammember.CreatedAt < DateTime.Now);
            Assert.IsTrue(teammember.CreatedAt > DateTime.Now.AddSeconds(-2));
            Assert.IsTrue("MANAGER".Equals(teammember.Role, StringComparison.Ordinal));

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetAgentTeammemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            //foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            //{
            //    betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            //}

            var createdTeammember = betterCoApiClient.CreateAgentTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var teammember = betterCoApiClient.GetTeammember(workspaceId, createdTeammember.Id);

            Assert.IsNotNull(teammember);
            Assert.IsTrue(teammember.Id.Equals(createdTeammember.Id, StringComparison.Ordinal));
            Assert.IsTrue("Max".Equals(teammember.FirstName, StringComparison.Ordinal));
            Assert.IsTrue("Mustermann".Equals(teammember.LastName, StringComparison.Ordinal));
            Assert.IsTrue("max.mustermann@muster.de".Equals(teammember.Email, StringComparison.Ordinal));
            Assert.IsTrue("ext1".Equals(teammember.ExternalId, StringComparison.Ordinal));
            Assert.IsNotNull(teammember.CreatedAt);
            Assert.IsTrue(teammember.CreatedAt < DateTime.Now);
            Assert.IsTrue(teammember.CreatedAt > DateTime.Now.AddSeconds(-2));
            Assert.IsTrue("AGENT".Equals(teammember.Role, StringComparison.Ordinal));

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetTeammemberByExternalIdTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            {
                betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            }

            var createdTeammember = betterCoApiClient.CreateAgentTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var teammemberResults = betterCoApiClient.GetTeammemberByExternalId(workspaceId, "ext1");

            Assert.IsNotNull(teammemberResults);
            Assert.IsTrue(teammemberResults.Results.Count == 1);
            Assert.IsTrue(teammemberResults.Results.First().Id.Equals(createdTeammember.Id, StringComparison.Ordinal));
            Assert.IsTrue(teammemberResults.Results.First().DisplayName.Equals("Max Mustermann", StringComparison.Ordinal));

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void GetTeammemberByEmailTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            {
                betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            }

            var createdTeammember = betterCoApiClient.CreateAgentTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var teammemberResults = betterCoApiClient.GetTeammemberByEmail(workspaceId, "ext1");

            Assert.IsNotNull(teammemberResults);
            Assert.IsTrue(teammemberResults.Results.Count == 1);
            Assert.IsTrue(teammemberResults.Results.First().Id.Equals(createdTeammember.Id, StringComparison.Ordinal));
            Assert.IsTrue(teammemberResults.Results.First().DisplayName.Equals("Max Mustermann", StringComparison.Ordinal));

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);
        }

        [TestMethod]
        public void DeleteTeammemberTest()
        {
            var betterCoApiClient = new BetterCoApiClient(betterCoConfiguration);

            var workspaces = betterCoApiClient.GetWorkspaces();
            var workspaceId = workspaces.Results.Where(x => string.Equals(x.DisplayName, betterCoConfiguration.WorkspaceName)).First().Id;

            //foreach (var item in betterCoApiClient.GetTeammembers(workspaceId).Results)
            //{
            //    betterCoApiClient.DeleteTeammeber(workspaceId, item.Id);
            //}

            var createdTeammember = betterCoApiClient.CreateAgentTeamMember(workspaceId, "Max", "Mustermann", "max.mustermann@muster.de", "ext1");
            Assert.IsNotNull(createdTeammember);
            Assert.IsNotNull(createdTeammember.Id);

            var deleteResponse = betterCoApiClient.DeleteTeammeber(workspaceId, createdTeammember.Id);
            Assert.IsTrue(deleteResponse.StatusCode == System.Net.HttpStatusCode.OK);

            var teammember = betterCoApiClient.GetTeammember(workspaceId, createdTeammember.Id);

            Assert.IsNull(teammember);
        }
    }
}
