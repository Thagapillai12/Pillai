using Newtonsoft.Json;
using RestSharp;
using System.Collections.Generic;
using WK.DE.AI.BetterCo.Json.Common;
using WK.DE.AI.BetterCo.Json.Team;
using WK.DE.AI.BetterCo.Mapping;

namespace WK.DE.AI.BetterCo.ApiClient
{
    public partial class BetterCoApiClient
	{
        public BetterCoCreateResult CreateAgentTeamMember(string workspaceId, string firstName, string lastName, string email, string externalId)
        {
            return CreateTeamMember(workspaceId, firstName, lastName, email, StringConstants.BETTERCO_TEAM_AGENT, externalId);
        }

        public BetterCoCreateResult CreateManagerTeamMember(string workspaceId, string firstName, string lastName, string email, string externalId)
        {
            return CreateTeamMember(workspaceId, firstName, lastName, email, StringConstants.BETTERCO_TEAM_MANAGER, externalId);
        }

        private BetterCoCreateResult CreateTeamMember(string workspaceId, string firstName, string lastName, string email, string role, string externalId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/team";

			var response = DoPostRequest(methodUrl, authToken, new BetterCoCreateTeammemberBody() { FirstName = firstName, LastName = lastName, Email = email, Role = role, ExternalId = externalId });

			BetterCoCreateResult createResult = null;
			if (response != null)
			{
				createResult = JsonConvert.DeserializeObject<BetterCoCreateResult>(response.Content);
			}

			return createResult;
		}

        public BetterCoTeammember UpdateTeamMember(string workspaceId, string teammemberId, string firstName, string lastName, string email, string role, string externalId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team/" + teammemberId;

            var response = DoPatchRequest(methodUrl, authToken, new BetterCoCreateTeammemberBody() { FirstName = firstName, LastName = lastName, Email = email, Role = role, ExternalId = externalId });

            BetterCoTeammember updateTeamMember = null;
            if (response != null)
            {
                updateTeamMember = JsonConvert.DeserializeObject<BetterCoTeammember>(response.Content);
            }

            return updateTeamMember;
        }

        public BetterCoResults GetTeammembers(string workspaceId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team";

            var response = DoGetRequest(methodUrl, authToken);

            BetterCoResults betterCoResults = null;
            if (response != null)
            {
                betterCoResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
            }

            return betterCoResults;
        }

        public BetterCoTeammember GetTeammember(string workspaceId, string teammemberId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team/" + teammemberId;

            var response = DoGetRequest(methodUrl, authToken);

            BetterCoTeammember teammember = null;
            if (response != null)
            {
                teammember = JsonConvert.DeserializeObject<BetterCoTeammember>(response.Content);
            }

            return teammember;
        }

        public BetterCoResults GetTeammemberByExternalId(string workspaceId, string externalId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team";

            var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("query", externalId) });

            BetterCoResults betterCoResults = null;
            if (response != null)
            {
                betterCoResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
            }

            return betterCoResults;
        }

        public BetterCoResults GetTeammemberByEmail(string workspaceId, string email)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team";

            var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("query", email) });

            BetterCoResults betterCoResults = null;
            if (response != null)
            {
                betterCoResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
            }

            return betterCoResults;
        }

        public IRestResponse DeleteTeammeber(string workspaceId, string teammemberId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/team/" + teammemberId;

            return DoDeleteRequest(methodUrl, authToken);
        }
    }
}
