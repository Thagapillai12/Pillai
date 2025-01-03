using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using WK.DE.Logging;
using WK.DE.Tools;

namespace AnNoText.AdvoAssist.Rest
{
    public class AdvoAssistClient
    {
        private static readonly ILogEx Log = LogEx.GetLogger(typeof(AdvoAssistClient));

        public static string DefaultApiRoot = "https://www.advo-assist.de/api";

        public static AdvoAssistClient CreateInstance()
        {
            return new AdvoAssistClient(DefaultApiRoot);
        }

        public string ApiRoot { get; private set; }

        public AdvoAssistClient(string apiRoot)
        {
            Guard.NotNullOrEmpty(apiRoot, "apiRoot");

            ApiRoot = apiRoot;
        }

        public Status GetStatus(string token)
        {
            Log.Debug("AdvoAssist: Get Status");

            var parameters = GetParameters();
            var requestUri = BuildRequestUri(ApiRoot, "status", token, parameters);

            var response = RestHelper.GetResponse<StatusResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not retrieve status");

            return new Status(response);
        }

        public string CreateAppointment(string token, AppointmentRequest request)
        {
            Log.Debug("AdvoAssist: Create Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "vergabe", token, parameters);

            var response = RestHelper.GetResponse<AppointmentResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not create appointment");

            return response.AppointmentId;
        }

        public void ConfirmAppointment(string token, ConfirmationRequest request)
        {
            Log.Debug("AdvoAssist: Confirm Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "confirm", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not confirm appointment");
        }

        public void CancelAppointment(string token, CancellationRequest request)
        {
            Log.Debug("AdvoAssist: Cancel Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "absage", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not cancel appointment");
        }

        public void MoveAppointment(string token, MoveAppointmentRequest request)
        {
            Log.Debug("AdvoAssist: Move Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "verschieben", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not move appointment");
        }

        public void ResetAppointment(string token, ResetAppointmentRequest request)
        {
            Log.Debug("AdvoAssist: Reset Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "reset", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not reset appointment");
        }

        public AppointmentDetails GetAppointmentDetails(string token, AppointmentDetailsRequest request)
        {
            Log.Debug("AdvoAssist: Get Appointment Details");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "termindetails", token, parameters);

            var response = RestHelper.GetResponse<AppointmentDetailsResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not retrieve appointment details");

            var details = response.Details;
            if (details != null)
                details.AppointmentId = request.AppointmentId;

            return details;
        }

        public Identity GetIdentity(string token, IdentityRequest request)
        {
            Log.Debug("AdvoAssist: Get Identity");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "portrait", token, parameters);

            var response = RestHelper.GetResponse<Identity>(requestUri);
            if (response == null)
                throw new Exception("Could not retrieve identity");

            return response;
        }

        public PaymentInfo InvoiceAppointment(string token, AppointmentInvoiceRequest request)
        {
            Log.Debug("AdvoAssist: Invoice Appointment");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "gutschrift", token, parameters);

            var response = RestHelper.GetResponse<AppointmentInvoiceResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not execute credit note billing");

            return new PaymentInfo(response);
        }

        public Statistics GetStatistics(string token, StatisticsRequest request)
        {
            Log.Debug("AdvoAssist: Get Statistics");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "stats", token, parameters);

            var response = RestHelper.GetResponse<StatisticsResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not execute credit note billing");

            var statistics = new Statistics()
            {
                Appointments = response.Appointments,
                RemunerationAverages = response.RemunerationAverages,
            };

            return statistics;
        }

        public string UploadFile(string token, UploadFileRequest request)
        {
            Log.Debug("AdvoAssist: Upload File");

            VerifyRequest(request);

            var requestUri = BuildRequestUri(ApiRoot, "upload");
            var parameters = GetParameters(request);

            UploadFileResponse response;
            using (var contentStream = BuildContentStream(token, parameters))
            {
                response = RestHelper.GetResponse<UploadFileResponse>(requestUri, contentStream);
                if (!response.Success)
                    throw new Exception("Could not upload file");
            }

            return response.FileId;
        }

        public IEnumerable<File> GetFiles(string token, AppointmentFilesRequest request)
        {
            Log.Debug("AdvoAssist: Get Files");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "dateien", token, parameters);

            var response = RestHelper.GetResponse<AppointmentFilesResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not retrieve file list");

            return response.Files;
        }

        public Stream DownloadFile(string token, DownloadFileRequest fileRequest)
        {
            Log.Debug("AdvoAssist: Download File");

            VerifyRequest(fileRequest);

            var parameters = GetParameters(fileRequest);
            var requestUri = BuildRequestUri(ApiRoot, "download", token, parameters);

            var response = RestHelper.GetResponse<Stream>(requestUri);

            return response;
        }

        public void DeleteFile(string token, DeleteFileRequest request)
        {
            Log.Debug("AdvoAssist: Delete File");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "datei-entfernen", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not delete file");
        }

        public void Rate(string token, RateAppointmentRequest request)
        {
            Log.Debug("AdvoAssist: Rate");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "bewertung", token, parameters);

            var response = RestHelper.GetResponse<ApiResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not rate representative");
        }

        public Report GetReport(string token, AppointmentReportRequest request)
        {
            Log.Debug("AdvoAssist: Get Report");

            VerifyRequest(request);

            var parameters = GetParameters(request);
            var requestUri = BuildRequestUri(ApiRoot, "bericht", token, parameters);

            var response = RestHelper.GetResponse<AppointmentReportResponse>(requestUri);
            if (!response.Success)
                throw new Exception("Could not retrieve appointment report");

            return new Report() { Text = response.Report };
        }

        private void VerifyRequest(AppointmentRequest request)
        {
            if (request.Date.Date < DateTime.Today)
                throw new InvalidOperationException("Date must not lie in the past");
            if (request.Deadline.Date < DateTime.Today)
                throw new InvalidOperationException("Deadline must not lie in the past");
            if (request.Deadline.Date > DateTime.Today.AddDays(7))
                throw new InvalidOperationException("Deadline must lie within the next 7 days");
            if (request.Deadline > request.Date)
                throw new InvalidOperationException("Deadline must lie before appointment date");

            if (request.Type == AppointmentType.CourtHearing && request.CourtId == 0)
                throw new InvalidOperationException("Court ID must be specified");
            else if (request.Type != AppointmentType.CourtHearing && String.IsNullOrEmpty(request.PostalCode))
                throw new InvalidOperationException("Postal code must not be empty");

            if (String.IsNullOrEmpty(request.AreaOfExpertise))
                throw new InvalidOperationException("Area of expertise must not be empty");
            if (String.IsNullOrEmpty(request.Description))
                throw new InvalidOperationException("Description must not be empty");

            if (request.ConfirmationMode == ConfirmationMode.Direct && request.RemunerationType == RemunerationType.Open)
                throw new InvalidOperationException("Remuneration type must not be Open when using direct confirmation mode");

            if (request.NumberOfAppointments < 0 || request.NumberOfAppointments > 99)
                throw new InvalidOperationException("Number of appointments must be in the range 0-99");
        }

        private void VerifyRequest(ConfirmationRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.ApplicantIdentityId))
                throw new InvalidOperationException("Applicant identity id must not be empty");
        }

        private void VerifyRequest(CancellationRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.Reason))
                throw new InvalidOperationException("Reason must not be empty");
        }

        private void VerifyRequest(MoveAppointmentRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");

            if (request.Date.Date < DateTime.Today)
                throw new InvalidOperationException("Date must not lie in the past");
            if (request.Deadline.Date < DateTime.Today)
                throw new InvalidOperationException("Deadline must not lie in the past");
            if (request.Deadline.Date > DateTime.Today.AddDays(7))
                throw new InvalidOperationException("Deadline must lie within the next 7 days");
            if (request.Deadline > request.Date)
                throw new InvalidOperationException("Deadline must lie before appointment date");
        }

        private void VerifyRequest(ResetAppointmentRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
        }

        private void VerifyRequest(AppointmentDetailsRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
        }

        private void VerifyRequest(IdentityRequest request)
        {
            if (String.IsNullOrEmpty(request.IdentityId))
                throw new InvalidOperationException("Identity id must not be empty");
        }

        private void VerifyRequest(UploadFileRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.FileName))
                throw new InvalidOperationException("File name must not be empty");
            if (request.FileStream == null)
                throw new InvalidOperationException("Stream must not be null");
        }

        private void VerifyRequest(AppointmentFilesRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
        }

        private void VerifyRequest(DownloadFileRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.FileId))
                throw new InvalidOperationException("File id must not be empty");
        }

        private void VerifyRequest(DeleteFileRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.FileId))
                throw new InvalidOperationException("File id must not be empty");
        }

        private void VerifyRequest(RateAppointmentRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (request.Rating < 1 || request.Rating > 5)
                throw new InvalidOperationException("Rating must lie in the range 1-5");
        }

        private void VerifyRequest(AppointmentReportRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
        }

        private void VerifyRequest(AppointmentInvoiceRequest request)
        {
            if (String.IsNullOrEmpty(request.AppointmentId))
                throw new InvalidOperationException("Appointment id must not be empty");
            if (String.IsNullOrEmpty(request.InvoiceNumber))
                throw new InvalidOperationException("Invoice number must not be empty");
        }

        private void VerifyRequest(StatisticsRequest request)
        {
            if (request.Year > DateTime.Today.Year)
                throw new InvalidOperationException("Year must not lie in the future");
            if (request.Month < 1 || request.Month > 12)
                throw new InvalidOperationException("Month must lie in the range 1-12");
        }

        private Dictionary<string, object> GetParameters()
        {
            var parameters = new Dictionary<string, object>();
            return parameters;
        }

        private Dictionary<string, object> GetParameters(AppointmentRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("typ", request.Type.Map());
            parameters.Add("beweisaufnahme", MapTakingOfEvidence(request.TakingOfEvidence));
            parameters.Add("honorar", request.RemunerationType.Map(request.RemunerationAmount));
            parameters.Add("zuschlag", request.SupplementAmount.ToString());
            parameters.Add("streitwert", request.ClaimValue.ToString());

            parameters.Add("tag", request.Date.Day.ToString());
            parameters.Add("monat", request.Date.Month.ToString());
            parameters.Add("jahr", request.Date.Year.ToString());
            parameters.Add("stunde", request.Date.Hour.ToString());
            parameters.Add("minute", request.Date.Minute.ToString());

            parameters.Add("d_tag", request.Deadline.Day.ToString());
            parameters.Add("d_monat", request.Deadline.Month.ToString());
            parameters.Add("d_jahr", request.Deadline.Year.ToString());
            parameters.Add("d_stunde", request.Deadline.Hour.ToString());
            parameters.Add("d_minute", request.Deadline.Minute.ToString());

            parameters.Add("gericht", request.CourtId.ToString());
            parameters.Add("plz", request.Type == AppointmentType.CourtHearing ? null : request.PostalCode);
            parameters.Add("dauer", request.DurationDescription);
            parameters.Add("fachrichtung", request.AreaOfExpertise);
            if (request.FieldOfLaw.HasValue)
                parameters.Add("rechtsgebiet", request.FieldOfLaw.Value.Map());

            parameters.Add("beschreibung", request.Description);
            parameters.Add("az", request.CaseReference);
            parameters.Add("parteien", request.Parties);
            parameters.Add("anzahl", request.NumberOfAppointments.ToString());

            parameters.Add("confirm", request.ConfirmationMode.Map());
            parameters.Add("bericht", request.ReportType.Map());
            parameters.Add("gutschrift", request.BillingMode.Map());

            return parameters;
        }

        private Dictionary<string, object> GetParameters(ConfirmationRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);
            parameters.Add("kanzlei", request.ApplicantIdentityId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(CancellationRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);
            parameters.Add("grund", request.Reason);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(MoveAppointmentRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);

            parameters.Add("tag", request.Date.Day.ToString());
            parameters.Add("monat", request.Date.Month.ToString());
            parameters.Add("jahr", request.Date.Year.ToString());
            parameters.Add("stunde", request.Date.Hour.ToString());
            parameters.Add("minute", request.Date.Minute.ToString());

            parameters.Add("d_tag", request.Deadline.Day.ToString());
            parameters.Add("d_monat", request.Deadline.Month.ToString());
            parameters.Add("d_jahr", request.Deadline.Year.ToString());
            parameters.Add("d_stunde", request.Deadline.Hour.ToString());
            parameters.Add("d_minute", request.Deadline.Minute.ToString());

            return parameters;
        }

        private Dictionary<string, object> GetParameters(ResetAppointmentRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(AppointmentDetailsRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(IdentityRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.IdentityId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(UploadFileRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);

            parameters.Add("filename", request.FileName);

            byte[] data;
            using (var ms = new MemoryStream())
            {
                request.FileStream.CopyTo(ms);
                data = ms.ToArray();
            }
            parameters.Add("base64", Convert.ToBase64String(data));

            return parameters;
        }

        private Dictionary<string, object> GetParameters(AppointmentFilesRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(DownloadFileRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);
            parameters.Add("id", request.FileId);
            parameters.Add("base64", 0); // download in binary mode

            return parameters;
        }

        private Dictionary<string, object> GetParameters(DeleteFileRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);
            parameters.Add("id", request.FileId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(RateAppointmentRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);
            parameters.Add("sterne", request.Rating);
            parameters.Add("kommentar", request.Comment);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(AppointmentReportRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("termin", request.AppointmentId);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(AppointmentInvoiceRequest request)
        {
            var parameters = new Dictionary<string, object>();

            parameters.Add("nr", request.AppointmentId);
            parameters.Add("honorar_final", request.RemunerationValue);
            parameters.Add("lfd_nr", request.InvoiceNumber);

            return parameters;
        }

        private Dictionary<string, object> GetParameters(StatisticsRequest request)
        {
            var parameters = new Dictionary<string, object>();

            if (request.Year.HasValue)
                parameters.Add("jahr", request.Year);
            if (request.Month.HasValue)
                parameters.Add("monat", request.Month);

            return parameters;
        }

        private Stream BuildContentStream(string token, Dictionary<string, object> parameters)
        {
            var queryString = BuildQueryString(token, parameters);

            var queryData = new UTF8Encoding(false).GetBytes(queryString);
            var stream = new MemoryStream(queryData);

            return stream;
        }

        private string BuildQueryString(string token, Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();

            var parametersCopy = new Dictionary<string, object>(parameters)
            {
                {"token", token}
            };

            var parameterString = String.Join("&", parametersCopy
                .Where(x => x.Value != null)
                .Select(x => String.Format("{0}={1}", x.Key, HttpUtility.UrlEncode(x.Value.ToString()))));

            sb.Append(parameterString);

            return sb.ToString();
        }

        private string BuildRequestUri(string apiRoot, string endpoint)
        {
            var sb = new StringBuilder();

            sb.Append(apiRoot);
            if (!apiRoot.EndsWith("/"))
                sb.Append("/");
            sb.Append(endpoint);
            sb.Append("/?");

            return sb.ToString();
        }

        private string BuildRequestUri(string apiRoot, string endpoint, string token, Dictionary<string, object> parameters)
        {
            var sb = new StringBuilder();

            sb.Append(BuildRequestUri(apiRoot, endpoint));
            sb.Append(BuildQueryString(token, parameters));

            return sb.ToString();
        }

        public static string MapTakingOfEvidence(bool value)
        {
            return value ? "1" : "0";
        }
    }
}