using log4net;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using WK.DE.AI.BetterCo.Json.Common;
using WK.DE.AI.BetterCo.Json.Process;
using WK.DE.AI.BetterCo.ViewModels;

namespace WK.DE.AI.BetterCo.ApiClient
{
	public partial class BetterCoApiClient
	{
		private static readonly ILog m_Logger = LogManager.GetLogger(typeof(BetterCoApiClient));

		private readonly string m_PartnerId;     //"wk_annotext" only used in GetCustomersNotYetImported
		private readonly string m_PartnerKey;    //"defk98jdm88ujkdl78emj8nnd";
		private readonly string m_PartnerSecret; //"ff7d8bg8E5bnmhre7hnmsdfg546GHFac";
		private readonly string m_WorkspaceName;
		//private readonly string m_OrganizationName;

		private readonly string m_baseUrl = "https://sbx-wk.betterco.ai/bcapi";
		// https://dev.betterco.ai/
	    // https://stage.betterco.ai/

		private readonly string m_restApiUrl = "/restapi/v1";

		private readonly string m_LegalInfoTypeIndividual = "INDIVIDUAL";
		private readonly string m_LegalInfoTypeEntity = "ENTITY";

		// process spec keys
		private readonly string m_F700_RiskEvaluation = "F700_RiskEvaluation";
		private readonly string m_F710_ConflictCheck = "F710_ConflictCheck";
		internal readonly string m_F1400_RiskEvaluation = "F1400_RiskEvaluation";
		private readonly string m_F6300_GeneralConcern = "F6300_GeneralConcern";
		internal readonly string m_F1800_OnboardingEntity_A = "F1800_OnboardingEntity_A";
		private readonly string m_F1800_OnboardingEntity_D = "F1800_OnboardingEntity_D";
		private readonly string m_F1900_OnboardingIndividual_D = "F1900_OnboardingIndividual_D";
		//private readonly string m_F161_Mandantenanlage = "F161_Mandantenanlage";
		//private readonly string m_F600_WebSignup = "F600_WebSignup";
		//private readonly string m_F60020_SearchIndividual = "F60020_SearchIndividual";
		//private readonly string m_F60010_SearchEntity = "F60010_SearchEntity";
		private readonly string m_F60040_SearchEntityIndividualWK = "F60040_SearchEntityIndividualWK";
		internal readonly string m_F60045_SearchEntityIndividualWK_Page2 = "F60045_SearchEntityIndividualWK_Page2";
		
		//private readonly string m_UrlAttachment = "&language=DE&mode=SUBMIT_ON_FINISH&caseName=New_Lead&defaultProcessName={0}&includeSidebar=true";
		private readonly string m_UrlAttachment = "&language=DEF&mode=SUBMIT_ON_FINISH&defaultProcessName={0}&caseName=BetterCo-Container-Akte";

		// task keys?
		//private readonly string m_P0600_webSignup = "P0600_webSignup";

		private readonly string m_60DaysExpirationPeriod = "P60D";
		private readonly string m_12HoursExpirationPeriod = "PT12H";

		private string m_token = null;

		public string WorkspaceName
		{
			get { return m_WorkspaceName ?? ""; }
		}

		//public string OrganizationName
		//{
		//	get { return m_WorkspaceName; }
		//}

		//private RestClient m_restClient = new RestClient(); // kein Performance-Gewinn gegenüber je einem RestClient in Methode? Andere Aspekte?

		private BetterCoApiClient()
        {}

		public BetterCoApiClient(BetterCoConfiguration configuration)
		{
			m_PartnerId = configuration.Id;
			m_PartnerKey = configuration.Key;
			m_PartnerSecret = configuration.Secret;
			m_WorkspaceName = configuration.WorkspaceName;
			//m_OrganizationName = officeName;
		}

		private IRestResponse DoGetRequest(string methodUrl, string authToken, List<KeyValuePair<string, string>> queryParameter = null)
		{
			IRestResponse response = DoBuildGetRequestAndExecute(methodUrl, authToken, queryParameter);
			if (response.StatusCode == HttpStatusCode.OK)
			{
				return response;
			}
			else if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// try with new token
				m_token = null;
				/*var token =*/ GetAuthToken();

				var newTokenResponse = DoBuildGetRequestAndExecute(methodUrl, m_token, queryParameter);
				if (newTokenResponse.StatusCode == HttpStatusCode.OK)
				{
					return newTokenResponse;
				}
			}
			m_Logger.Error(string.Format("Reqest failed. Status code was: {0}", response.StatusCode));
			return null;
		}

		private byte[] DoGetRequestDownloaData(string methodUrl, string authToken, List<KeyValuePair<string, string>> queryParameter = null)
		{
			try
			{
				return DoBuildGetRequestAndExecuteDownloadData(methodUrl, authToken, queryParameter); // TODO auch hier 2x versuchen?
			}
			catch (Exception ex)
			{
				m_Logger.Error(string.Format("DownloadData request failed. Exception was  was: {0}", ex.Message + "\n" + ex.StackTrace));
				return null;
			}
		}

		private IRestResponse DoBuildGetRequestAndExecute(string methodUrl, string authToken, List<KeyValuePair<string, string>> queryParameter)
		{
			RestClient restClient = null;
			RestRequest request;
			BuildGetRequest(methodUrl, authToken, queryParameter, ref restClient, out request);
			//return m_restClient.Execute(request);
			return restClient.Execute(request);
		}

		private byte[] DoBuildGetRequestAndExecuteDownloadData(string methodUrl, string authToken, List<KeyValuePair<string, string>> queryParameter)
		{
			RestClient restClient = null;
			RestRequest request;
			BuildGetRequest(methodUrl, authToken, queryParameter, ref restClient, out request);
			//return m_restClient.Execute(request);
			return restClient.DownloadData(request, true);
		}

		private void BuildGetRequest(string methodUrl, string authToken, List<KeyValuePair<string, string>> queryParameter, ref RestClient restClient, out RestRequest request)
		{
			//m_restClient.BaseUrl = new Uri(m_baseUrl + m_restApiUrl + methodUrl);
			if (methodUrl.StartsWith("https"))
			{
				restClient = new RestClient(methodUrl);
			}
			else
			{
				restClient = new RestClient(m_baseUrl + m_restApiUrl + methodUrl);
			}

			request = new RestRequest(Method.GET);
			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("Authorization", "Bearer " + authToken);
			request.AddHeader("Accept", "*/*");

			if (queryParameter != null && queryParameter.Any())
			{
				foreach (var qp in queryParameter)
				{
					request.AddQueryParameter(qp.Key, HttpUtility.UrlEncode(qp.Value));
				}
			}

			m_Logger.Debug(string.Format("Es wird diese Uri für den GET-Request verwendet: {0}", restClient.BuildUri(request).OriginalString));
		}

		private IRestResponse DoPostRequest(string methodUrl, string authToken, object jsonBody = null)
		{
			IRestResponse response = DoBuildAndExecutePostRequest(methodUrl, authToken, jsonBody);
			if (response.StatusCode == HttpStatusCode.Created)
			{
				return response;
			}
			else if (response.StatusCode == HttpStatusCode.OK) // kommt bei InstantiateOrganizationProcess, soll das so?
			{
				return response;
			}
			else if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// try with new token
				m_token = null;
				var token = GetAuthToken();

				var newTokenResponse = DoBuildAndExecutePostRequest(methodUrl, token, jsonBody);
				if (newTokenResponse.StatusCode == HttpStatusCode.Created)
				{
					return newTokenResponse;
				}
			}
			m_Logger.Error(string.Format("Reqest failed. Status code was: {0}", response.StatusCode));
			return null;
		}

		private IRestResponse DoBuildAndExecutePostRequest(string methodUrl, string authToken, object jsonBody)
		{
			//m_restClient.BaseUrl = new Uri(m_baseUrl + m_restApiUrl + methodUrl);
			var restClient = new RestClient(m_baseUrl + m_restApiUrl + methodUrl);

			var request = new RestRequest(Method.POST);

			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("Authorization", "Bearer " + authToken);
			request.AddHeader("content-type", "application/json");

			if (jsonBody != null)
			{
				request.AddJsonBody(JsonConvert.SerializeObject(jsonBody));
			}

			m_Logger.Debug(string.Format("Es wird diese Uri für den POST-Request verwendet: {0}", restClient.BuildUri(request).OriginalString));
			//return m_restClient.Execute(request);
			return restClient.Execute(request);
		}

		private IRestResponse DoPatchRequest(string methodUrl, string authToken, object jsonBody = null)
		{
			IRestResponse response = DoBuildAndExecutePatchRequest(methodUrl, authToken, jsonBody);
			if (response.StatusCode == HttpStatusCode.OK)
			{
				return response;
			}
			else if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// try with new token
				m_token = null;
				var token = GetAuthToken();

				var newTokenResponse = DoBuildAndExecutePatchRequest(methodUrl, token, jsonBody);
				if (newTokenResponse.StatusCode == HttpStatusCode.OK)
				{
					return newTokenResponse;
				}
			}
			m_Logger.Error(string.Format("Reqest failed. Status code was: {0}", response.StatusCode));
			return null;
		}

		private IRestResponse DoBuildAndExecutePatchRequest(string methodUrl, string authToken, object jsonBody)
		{
			//m_restClient.BaseUrl = new Uri(m_baseUrl + m_restApiUrl + methodUrl);
			var restClient = new RestClient(m_baseUrl + m_restApiUrl + methodUrl);

			var request = new RestRequest(Method.PATCH);

			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("Authorization", "Bearer " + authToken);
			request.AddHeader("content-type", "application/json");

			if (jsonBody != null)
			{
				request.AddJsonBody(JsonConvert.SerializeObject(jsonBody));
			}

			m_Logger.Debug(string.Format("Es wird diese Uri für den PATCH-Request verwendet: {0}", restClient.BuildUri(request).OriginalString));
			//return m_restClient.Execute(request);
			return restClient.Execute(request);
		}

		private IRestResponse DoDeleteRequest(string methodUrl, string authToken, object jsonBody = null)
		{
			IRestResponse response = DoBuildAndExecuteDeleteRequest(methodUrl, authToken, jsonBody);
			if (response.StatusCode == HttpStatusCode.OK)
			{
				return response;
			}
			else if (response.StatusCode == HttpStatusCode.Unauthorized)
			{
				// try with new token
				m_token = null;
				var token = GetAuthToken();

				var newTokenResponse = DoBuildAndExecuteDeleteRequest(methodUrl, token, jsonBody);
				if (newTokenResponse.StatusCode == HttpStatusCode.OK)
				{
					return newTokenResponse;
				}
			}
			m_Logger.Error(string.Format("Reqest failed. Status code was {0}, content was {1}.", response.StatusCode, response.Content));
			return null;
		}

		private IRestResponse DoBuildAndExecuteDeleteRequest(string methodUrl, string authToken, object jsonBody)
		{
			//m_restClient.BaseUrl = new Uri(m_baseUrl + m_restApiUrl + methodUrl);
			var restClient = new RestClient(m_baseUrl + m_restApiUrl + methodUrl);

			var request = new RestRequest(Method.DELETE);

			request.AddHeader("cache-control", "no-cache");
			request.AddHeader("Authorization", "Bearer " + authToken);
			request.AddHeader("content-type", "application/json");

			if (jsonBody != null)
			{
				request.AddJsonBody(JsonConvert.SerializeObject(jsonBody));
			}

			m_Logger.Debug(string.Format("Es wird diese Uri für den DELETE-Request verwendet: {0}", restClient.BuildUri(request).OriginalString));
			//return m_restClient.Execute(request);
			return restClient.Execute(request);
		}

		private void AddResultsFromAllPages(string methodUrl, string authToken, BetterCoResults betterCoResults)
		{
            // FIXME queryParameter berücksichtigen
            if (betterCoResults != null)
			{
				int count = 1;
				while (betterCoResults.Total > betterCoResults.Results.Count && count < betterCoResults.TotalPages)
				{
					var nextResponse = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("page", count.ToString()) });
					if (nextResponse != null)
					{
						var nextBetterCoResults = JsonConvert.DeserializeObject<BetterCoResults>(nextResponse.Content);
						betterCoResults.Results.AddRange(nextBetterCoResults.Results);
						count++;
					}
				}
			}
		}

        private void AddResultsFromAllPages(string methodUrl, string authToken, BetterCoProcessResults betterCoResults, List<KeyValuePair<string, string>> queryParameter = null)
        {
            if (betterCoResults != null)
            {
                int count = 1;
                while (betterCoResults.Total > betterCoResults.Results.Count && count < betterCoResults.TotalPages)
                {
					var allQueryParameter = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("page", count.ToString())};
					if (queryParameter != null)
					{
                        allQueryParameter.AddRange(queryParameter);
                    }
                    var nextResponse = DoGetRequest(methodUrl, authToken, allQueryParameter);
                    if (nextResponse != null)
                    {
                        var nextBetterCoResults = JsonConvert.DeserializeObject<BetterCoProcessResults>(nextResponse.Content);
                        betterCoResults.Results.AddRange(nextBetterCoResults.Results);
                        count++;
                    }
                }
            }
        }
    }
}
