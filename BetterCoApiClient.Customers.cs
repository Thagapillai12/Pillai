using Newtonsoft.Json;
using RestSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WK.DE.AI.BetterCo.Facades;
using WK.DE.AI.BetterCo.Json.Common;
using WK.DE.AI.BetterCo.Json.Customer;
using WK.DE.AI.BetterCo.Json.Search;

namespace WK.DE.AI.BetterCo.ApiClient
{
	public partial class BetterCoApiClient
	{
		public BetterCoResults GetCustomers(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoResults customersResults = null;
			if (response != null)
			{
				customersResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}
			AddResultsFromAllPages(methodUrl, authToken, customersResults);
			return customersResults;
		}

		/*
		 * DD: Wenn eine Liste von "required_processes" übergeben ist, muss zu jedem übergebenen Typ ein geschlossener Prozess vorliegen.Im Beispiel ist aber lediglich der "Conflict Check" geschlossen
		 */
		/*
		 * TODO alle GetCustomersWithClosedProcesses umbenennen
		 */
		// Der Query-Parameter 'required_processes=S710_ConflictCheck' ist in der Postman-Collection angeben für CustomersWithClosedProcesses (andere S700_RiskEvaluation, S1800_OnboardingEntity_A)
		public BetterCoResults GetCustomersWithClosedProcessF710_ConflictCheck(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F710_ConflictCheck) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		// Der Query-Parameter 'required_processes=S700_RiskEvaluation' ist in FO1-AnNoText - BetterCo Integration Guide-v14_12.pdf angeben für CustomersWithClosedProcesses (andere: S1800_OnboardingEntity_A, S710_ConflictCheck)
		public BetterCoResults GetCustomersWithClosedProcessF700_RiskEvaluation(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F700_RiskEvaluation) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		// Der Query-Parameter 'required_processes=S700_RiskEvaluation' ist in FO1-AnNoText - BetterCo Integration Guide-v14_12.pdf angeben für CustomersWithClosedProcesses (andere: S1800_OnboardingEntity_A, S710_ConflictCheck)
		public BetterCoResults GetCustomersWithClosedProcessF1900_OnboardingIndividual_D(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F1900_OnboardingIndividual_D) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersWithClosedProcessF1800_OnboardingEntity_A(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F1800_OnboardingEntity_A) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersWithClosedProcessF1400_RiskEvaluation(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F1400_RiskEvaluation) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}
		
		public BetterCoResults GetCustomersWithClosedProcessF6300_GeneralConcern(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", m_F6300_GeneralConcern) });

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersWithClosedProcessesForAllProcessSpecs(string workspaceId, string organizationId)
		{
			var result = new BetterCoResults() { Results = new List<BetterCoResult>() };

			var customers1 = GetCustomersWithClosedProcessF710_ConflictCheck(workspaceId, organizationId);
			if (customers1 != null && customers1.Total > 0)
			{
				result.Total += customers1.Total;
				result.Results.AddRange(customers1.Results);
			}

			var customers2 = GetCustomersWithClosedProcessF700_RiskEvaluation(workspaceId, organizationId);
			if (customers2 != null && customers2.Total > 0)
			{
				result.Total += customers2.Total;
				result.Results.AddRange(customers2.Results);
			}

			var customers3 = GetCustomersWithClosedProcessF1900_OnboardingIndividual_D(workspaceId, organizationId);
			if (customers3 != null && customers3.Total > 0)
			{
				result.Total += customers3.Total;
				result.Results.AddRange(customers3.Results);
			}

			var customers4 = GetCustomersWithClosedProcessF1800_OnboardingEntity_A(workspaceId, organizationId);
			if (customers4 != null && customers4.Total > 0)
			{
				result.Total += customers4.Total;
				result.Results.AddRange(customers4.Results);
			}

			var customers5 = GetCustomersWithClosedProcessF1400_RiskEvaluation(workspaceId, organizationId);
			if (customers5 != null && customers5.Total > 0)
			{
				result.Total += customers5.Total;
				result.Results.AddRange(customers5.Results);
			}
			var customers6 = GetCustomersWithClosedProcessF6300_GeneralConcern(workspaceId, organizationId);
			if (customers6 != null && customers6.Total > 0)
			{
				result.Total += customers6.Total;
				result.Results.AddRange(customers6.Results);
			}
			
			return result;
		}

		public BetterCoResults GetCustomersChangesSince(string workspaceId, string organizationId, DateTime since)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/changes/" + since.ToString("yyyy-MM-ddTHH:mm:ss.fffK"); // 2023-11-05T08:35:07.077+01:00

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersChangesSinceWithRequiredProcesses(string workspaceId, string organizationId, DateTime since, List<string> requiredProcesses)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/changes/" + since.ToString("yyyy-MM-ddTHH:mm:ss.fffK"); // 2023-11-05T08:35:07.077+01:00

			IRestResponse response = null;
			if (requiredProcesses != null && requiredProcesses.Any())
			{
				var requiredProcessesParameter = string.Join(",", requiredProcesses);
				var keyValuePair = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_processes", requiredProcessesParameter) };
				response = DoGetRequest(methodUrl, authToken, keyValuePair);
			}
			else
			{
				response = DoGetRequest(methodUrl, authToken);
			}

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersChangesSinceWithRequiredProcessF60045_SearchEntityIndividualWK_Page2(string workspaceId, string organizationId, DateTime since)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/changes/" + since.ToString("yyyy-MM-ddTHH:mm:ss.fffK"); // 2023-11-05T08:35:07.077+01:00

			var keyValuePair = new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("required_process", m_F60045_SearchEntityIndividualWK_Page2) }; // FIXME doch wieder required_processes

			IRestResponse response = DoGetRequest(methodUrl, authToken, keyValuePair);

			BetterCoResults customers = null;
			if (response != null)
			{
				customers = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customers;
		}

		public BetterCoResults GetCustomersNotYetImported(string workspaceId, string organizationId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers?no_external_id_type=" + m_PartnerId;

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoResults customersResults = null;
			if (response != null)
			{
				customersResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}
			AddResultsFromAllPages(methodUrl, authToken, customersResults);

			return customersResults;
		}

		public BetterCoCustomerData MarkCustomerAsImported(string workspaceId, string organizationId, string customerId, string @externalId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId;

			object body = new
			{
				externalId = @externalId
			};

			var response = DoPatchRequest(methodUrl, authToken, body);

			BetterCoCustomerData customerData = null;
			if (response != null)
			{
				customerData = JsonConvert.DeserializeObject<BetterCoCustomerData>(response.Content);
			}

			return customerData;
		}

		public BetterCoCustomerData GetCustomerData(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId;

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoCustomerData customerData = null;
			if (response != null)
			{
				customerData = JsonConvert.DeserializeObject<BetterCoCustomerData>(response.Content);
			}

			return customerData;
		}

		public BetterCoCustomerData GetCustomerDataByUrl(string url)
		{
			var authToken = GetAuthToken();

			string methodUrl = url.Replace(m_baseUrl, "").Replace(m_restApiUrl, "");

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoCustomerData customerData = null;
			if (response != null)
			{
				customerData = JsonConvert.DeserializeObject<BetterCoCustomerData>(response.Content);
			}

			return customerData;
		}

		public BetterCoResults GetCustomerDocuments(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/documents";

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoResults customerDocumentsResults = null;
			if (response != null)
			{
				customerDocumentsResults = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}
			AddResultsFromAllPages(methodUrl, authToken, customerDocumentsResults);
			return customerDocumentsResults;
		}

		public BetterCoCertificate GetCustomerDocumentData(string workspaceId, string organizationId, string customerId, string documentId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/documents/" + documentId;

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoCertificate customerDocumentData = null;
			if (response != null)
			{
				customerDocumentData = JsonConvert.DeserializeObject<BetterCoCertificate>(response.Content);
			}

			return customerDocumentData;
		}

		public string GetCustomerDocumentAsDownload(string workspaceId, string organizationId, string customerId, string documentId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/documents/" + documentId + "/download";

			var response = DoGetRequest(methodUrl, authToken);

			//BetterCoCertificate customerDocumentData = null;
			//if (response != null)
			//{
			//	customerDocumentData = JsonConvert.DeserializeObject<BetterCoCertificate>(response.Content);
			//}

			return null;
		}

		//public string GetCustomerDocumentByDownloadUri(string downloadUri)
		//{
		//	var authToken = GetAuthToken();

		//	var response = DoGetRequest(downloadUri, authToken);

		//	if (response.Content != null)
		//	{
		//		var test = response.Content.IndexOf("stream", StringComparison.Ordinal);
		//		var test2 = response.Content.Substring(test + 7);
		//		byte[] pdfBytes = Encoding.Unicode.GetBytes(test2);
		//		var test3 = Convert.ToBase64String(pdfBytes);
		//		//File.WriteAllBytes(@"c:\temp\a.pdf", pdfBytes);
		//		File.WriteAllText(@"c:\temp\a.pdf", test3);
		//	}

		//	foreach (var item in response.Headers)
		//	{
		//		if (item.Name.Equals("Content-Disposition", StringComparison.OrdinalIgnoreCase))
		//		{
		//			return item.Value.ToString();
		//		}
		//	}

		//	return null;
		//}

		public DocumentDownloadResult GetDocumentByDownloadUri(string downloadUri, string fileName)
		{
			var authToken = GetAuthToken();

			var response = DoGetRequestDownloaData(downloadUri, authToken);

			if (response != null)
			{
				return new DocumentDownloadResult(new MemoryStream(response), fileName);
				//File.WriteAllBytes(@"c:\temp\a.pdf", response);
			}
			else
			{
				return null;
			}
		}

		public BetterCoResults GetCustomerContacts(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoResults customerContacts = null;
			if (response != null)
			{
				customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customerContacts;
		}

		public BetterCoResults GetCustomerContactsLegalReps(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("relation_type", "LEGAL_REP") }); // FIXME 'LEGAL_REP,UBO' (Array of strings) geht nicht. Liegt das an AddQueryParameter? In Postman funktioniert es.

			BetterCoResults customerContacts = null;
			if (response != null)
			{
				customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customerContacts;
		}

		public BetterCoResults GetCustomerContactsLegalRepsByRelationId(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("relation_ids", "9010") }); // FIXME 'LEGAL_REP,UBO' (Array of strings) geht nicht. Liegt das an AddQueryParameter? In Postman funktioniert es.

			BetterCoResults customerContacts = null;
			if (response != null)
			{
				customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customerContacts;
		}

		public BetterCoResults GetCustomerContactsUbos(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("relation_type", "UBO") }); // FIXME 'LEGAL_REP,UBO' (Array of strings) geht nicht. Liegt das an AddQueryParameter? In Postman funktioniert es.

			BetterCoResults customerContacts = null;
			if (response != null)
			{
				customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customerContacts;
		}

		public BetterCoResults GetCustomerContactsUbosByRelationId(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("relation_ids", "9030") });

			BetterCoResults customerContacts = null;
			if (response != null)
			{
				customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
			}

			return customerContacts;
		}

		//public BetterCoResults GetCustomerContactsByRelationIds(string workspaceId, string organizationId, string customerId)
		//{
		//	var authToken = GetAuthToken();

		//	string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts";

		//	var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("relation_ids", "9030, 9010") });

		//	BetterCoResults customerContacts = null;
		//	if (response != null)
		//	{
		//		customerContacts = JsonConvert.DeserializeObject<BetterCoResults>(response.Content);
		//	}

		//	return customerContacts;
		//}

		public BetterCoCustomerContact GetCustomerContactData(string workspaceId, string organizationId, string customerId, string contactId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/contacts/" + contactId;

			var response = DoGetRequest(methodUrl, authToken);

			BetterCoCustomerContact customerContact = null;
			if (response != null)
			{
				customerContact = JsonConvert.DeserializeObject<BetterCoCustomerContact>(response.Content);
			}

			return customerContact;
		}

		public BetterCoSearchCompanyResults GetSearchCompanies()
		{
			var authToken = GetAuthToken();

			string methodUrl = "/search/customers";

			var response = DoGetRequest(methodUrl, authToken, new List<KeyValuePair<string, string>>() { new KeyValuePair<string, string>("query", "'Founders1 GmbH'") }); // FIXME wie muss query aussehen um Ergebnisse zu bekommen; nicht erklärt in https://sbx-wk.betterco.ai/bcapi/apidoc.html#tag/Search/operation/companiesSearch AUßerdem gibt es manchmal (meist beim ersten Ausführen) einen Internal Server Error

			BetterCoSearchCompanyResults searchCompanyResults = null;
			if (response != null)
			{
				searchCompanyResults = JsonConvert.DeserializeObject<BetterCoSearchCompanyResults>(response.Content);
			}

			return searchCompanyResults;
		}

		public BetterCoCreateResult CreateIndiviualCustomer(string workspaceId, string organizationId, BetterCoCreateCustomerData betterCoCustomerData)
		{	
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			betterCoCustomerData.Type = m_LegalInfoTypeIndividual;

			var response = DoPostRequest(methodUrl, authToken, betterCoCustomerData);

			BetterCoCreateResult id = null;
			if (response != null)
			{
				id = JsonConvert.DeserializeObject<BetterCoCreateResult>(response.Content);
			}

			return id;
		}

		//public BetterCoCreateResult CreateIndiviualCustomer2(string workspaceId, string organizationId, BetterCoCustomerData betterCoCustomerData)
		//{
		//	var authToken = GetAuthToken();

		//	string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

		//	betterCoCustomerData.Type = m_LegalInfoTypeIndividual;

		//	var response = DoPostRequest(methodUrl, authToken, betterCoCustomerData);

		//	BetterCoCreateResult id = null;
		//	if (response != null)
		//	{
		//		id = JsonConvert.DeserializeObject<BetterCoCreateResult>(response.Content);
		//	}

		//	return id;
		//}

		public BetterCoCreateResult CreateEntityCustomer(string workspaceId, string organizationId, BetterCoCreateCustomerData betterCoCustomerData)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers";

			betterCoCustomerData.Type = m_LegalInfoTypeEntity;

			var response = DoPostRequest(methodUrl, authToken, betterCoCustomerData);

			BetterCoCreateResult id = null;
			if (response != null)
			{
				id = JsonConvert.DeserializeObject<BetterCoCreateResult>(response.Content);
			}

			return id;
		}

		public BetterCoCreateResult CreateCustomerCase(string workspaceId, string organizationId, string customerId, string name, string description)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "/cases";

			BetterCoCreateCustomerCaseBody betterCoCreateCustomerCaseData = new BetterCoCreateCustomerCaseBody() { Name = name, Decsription = description };

			var response = DoPostRequest(methodUrl, authToken, betterCoCreateCustomerCaseData);

			BetterCoCreateResult id = null;
			if (response != null)
			{
				id = JsonConvert.DeserializeObject<BetterCoCreateResult>(response.Content);
			}

			return id;
		}

		public IRestResponse DeleteCustomer(string workspaceId, string organizationId, string customerId)
		{
			var authToken = GetAuthToken();

			string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId;

			return DoDeleteRequest(methodUrl, authToken);
		}

        public IRestResponse DeleteCustomerWithForce(string workspaceId, string organizationId, string customerId)
        {
            var authToken = GetAuthToken();

            string methodUrl = "/workspaces/" + workspaceId + "/organizations/" + organizationId + "/customers/" + customerId + "?force=true";

            return DoDeleteRequest(methodUrl, authToken);
        }


    }
}
