using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AnNoText.AdvoAssist.Rest;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace AnNoText.Abrechnung.UnitTests.AdvoAssist
{
    [TestClass]
    public class AdvoAssistClientTests
    {
        private static string m_Token13980 = "8066a3e99b704d24512b76edc057344f";
        private static string m_Token13981 = "d9ea10e11929c48c51f34db839e519af";

        private static AppointmentRequest GetRequestForFixedAmountAppointment()
        {
            var now = DateTime.Now;
            var request = new AppointmentRequest()
            {
                RemunerationType = RemunerationType.Fixed,
                RemunerationAmount = 110,

                Type = AppointmentType.CourtHearing,
                CourtId = 24,

                Date = now.AddDays(14),
                Deadline = now.AddDays(7),

                AreaOfExpertise = "Fachgebiet",
                Description = "Beschreibung",

                ConfirmationMode = ConfirmationMode.Controlled,
            };

            return request;
        }

        private static AppointmentRequest GetRequestForOpenAmountAppointment()
        {
            var now = DateTime.Now;
            var request = new AppointmentRequest()
            {
                RemunerationType = RemunerationType.Open,

                Type = AppointmentType.CourtHearing,
                CourtId = 3086,

                Date = now.AddDays(14),
                Deadline = now.AddDays(7),

                AreaOfExpertise = "Fachgebiet",
                Description = "Beschreibung",

                ConfirmationMode = ConfirmationMode.Controlled,
                BillingMode = BillingMode.CreditNote,
            };

            return request;
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void GetStatus()
        {
            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(m_Token13981);

            status.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void GetStatusRepresentative()
        {
            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(m_Token13981);

            status.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void CreateAppointmentWithFixedAmount()
        {
            var request = GetRequestForFixedAmountAppointment();

            var client = AdvoAssistClient.CreateInstance();

            // delete existsing appointments on this date to be able to create a new one (might produced conflicts otherwise)
            DeleteAppointments(client, request.Date);

            var appointmentId = client.CreateAppointment(m_Token13980, request);

            appointmentId.Should().NotBeEmpty().And.NotBe("0");
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void CreateAppointmentWithOpenAmount()
        {
            var request = GetRequestForFixedAmountAppointment();

            var client = AdvoAssistClient.CreateInstance();

            // delete existsing appointments on this date to be able to create a new one (might produced conflicts otherwise)
            DeleteAppointments(client, request.Date);

            var appointmentId = client.CreateAppointment(m_Token13980, request);

            appointmentId.Should().NotBeEmpty().And.NotBe("0");
        }

        private void DeleteAppointments(AdvoAssistClient client, DateTime? date = null)
        {
            var status = client.GetStatus(m_Token13980);
            var appointments = status.Appointments.Where(x => date == null || x.Date.Date == date.Value.Date).ToArray();
            foreach (var appointment in appointments)
            {
                var cancellation = new CancellationRequest()
                {
                    AppointmentId = appointment.Id,
                    Reason = "Automated Test",
                };
                client.CancelAppointment(m_Token13980, cancellation);
            }
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void GetAppointmentDetailsFromOwner()
        {
            var client = AdvoAssistClient.CreateInstance();

            // create appointment on account 1
            var request = GetRequestForFixedAmountAppointment();
            DeleteAppointments(client, request.Date);
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            // get appointment details on account 1 (same account)
            var details = client.GetAppointmentDetails(m_Token13980, new AppointmentDetailsRequest() { AppointmentId = appointmentId });

            details.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // Its currently not possible to get appointment details as a representative (accepted appointments are not returned by GetStatus)
        public void GetAppointmentDetailsFromRepresentative()
        {
            var client = AdvoAssistClient.CreateInstance();

            // create appointment on account 1
            var request = GetRequestForFixedAmountAppointment();
            DeleteAppointments(client, request.Date);
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            // 2. break debugger at GetStatus below
            // 3. note the appointment id
            // 4. make an offer for this appointment from account 2
            // 5. resume debugger

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);
            appointment.Offers.Should().NotBeEmpty();

            // 6. confirm appointment

            client.ConfirmAppointment(m_Token13980, new ConfirmationRequest()
            {
                AppointmentId = appointment.Id,
                ApplicantIdentityId = appointment.Offers.First().ApplicantIdentityId,
            });

            // 7. get appointment details from account 2 (different account)

            var status2 = client.GetStatus(m_Token13981);
            var appointment2 = status2.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);
            var details2 = client.GetAppointmentDetails(m_Token13981, new AppointmentDetailsRequest() { AppointmentId = appointmentId });

            appointment2.Should().NotBeNull();
            details2.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // Can only be executed manually in debugger
        public void ConfirmAppointment()
        {
            var client = AdvoAssistClient.CreateInstance();

            // 1. create appointment with account 1

            var request = GetRequestForFixedAmountAppointment();
            DeleteAppointments(client, request.Date);
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            // 2. break debugger at GetStatus below
            // 3. note the appointment id
            // 4. make an offer for this appointment from account 2
            // 5. resume debugger

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);
            appointment.Offers.Should().NotBeEmpty();

            // 5. confirm appointment

            client.ConfirmAppointment(m_Token13980, new ConfirmationRequest()
            {
                AppointmentId = appointment.Id,
                ApplicantIdentityId = appointment.Offers.First().ApplicantIdentityId,
            });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void CancelAppointment()
        {
            var client = AdvoAssistClient.CreateInstance();

            DeleteAppointments(client);
            var request = GetRequestForFixedAmountAppointment();
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);

            appointment.Should().NotBeNull();

            client.CancelAppointment(m_Token13980, new CancellationRequest()
            {
                AppointmentId = appointment.Id,
                Reason = "Automated Test",
            });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void MoveAppointment()
        {
            var client = AdvoAssistClient.CreateInstance();

            DeleteAppointments(client);
            var request = GetRequestForFixedAmountAppointment();
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);

            appointment.Should().NotBeNull();

            client.MoveAppointment(m_Token13980, new MoveAppointmentRequest()
            {
                AppointmentId = appointment.Id,
                Date = DateTime.Now.AddDays(21),
                Deadline = DateTime.Now.AddDays(6),
            });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void ResetAppointment()
        {
            var client = AdvoAssistClient.CreateInstance();

            DeleteAppointments(client);
            var request = GetRequestForFixedAmountAppointment();
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);

            appointment.Should().NotBeNull();

            client.ResetAppointment(m_Token13980, new ResetAppointmentRequest()
            {
                AppointmentId = appointment.Id,
            });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void AllFileOperations()
        {
            var client = AdvoAssistClient.CreateInstance();

            DeleteAppointments(client);
            var request = GetRequestForFixedAmountAppointment();
            var appointmentId = client.CreateAppointment(m_Token13980, request);

            var status = client.GetStatus(m_Token13980);
            var appointment = status.Appointments.FirstOrDefault(x => x.Status == AppointmentStatus.Advertised && x.Id == appointmentId);

            appointment.Should().NotBeNull();

            var fileData = Encoding.ASCII.GetBytes("Hello World!");
            string fileId;
            using (var uploadStream = new MemoryStream(fileData))
            {
                fileId = client.UploadFile(m_Token13980, new UploadFileRequest()
                {
                    AppointmentId = appointment.Id,
                    FileName = "Test.txt",
                    FileStream = uploadStream,
                });
            }

            var files = client.GetFiles(m_Token13980, new AppointmentFilesRequest() { AppointmentId = appointment.Id });
            var file = files.FirstOrDefault(x => x.Id == fileId);

            byte[] downloadData;
            using (var dataStream = new MemoryStream())
            {
                var downloadStream = client.DownloadFile(m_Token13980, new DownloadFileRequest()
                {
                    AppointmentId = appointment.Id,
                    FileId = fileId,
                });
                downloadStream.CopyTo(dataStream);
                downloadStream.Close();

                downloadData = dataStream.ToArray();
            }

            downloadData.Should().Equal(fileData);

            client.DeleteFile(m_Token13980, new DeleteFileRequest()
            {
                AppointmentId = appointment.Id,
                FileId = fileId,
            });

            var filesAfterDelete = client.GetFiles(m_Token13980, new AppointmentFilesRequest() { AppointmentId = appointment.Id });
            var fileExists = filesAfterDelete.Any(x => x.Id == fileId);

            fileExists.Should().BeFalse();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // use for checking files of a specific appointment, otherwise handle by AllFileOperations
        public void GetFiles()
        {
            var token = m_Token13980;
            var appointmentId = "393415";

            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(token);
            var appointment = status.Appointments.FirstOrDefault(x => x.Id == appointmentId);

            appointment.Should().NotBeNull();

            var files = client.GetFiles(m_Token13980, new AppointmentFilesRequest() { AppointmentId = appointmentId });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // can only be executed on an appointment that was confirmed and has passed (enter appointment id manually)
        public void GetReport()
        {
            var token = m_Token13980;
            var appointmentId = "393415";

            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(token);
            var appointment = status.Appointments.FirstOrDefault(x => x.Id == appointmentId);
            var details = client.GetAppointmentDetails(token, new AppointmentDetailsRequest() { AppointmentId = appointmentId });

            var report = client.GetReport(token, new AppointmentReportRequest()
            {
                AppointmentId = appointmentId,
            });

            report.Should().NotBeNull();
            //report.Text.Should().NotBeNull();

            Trace.WriteLine(report.Text);
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // can only be executed on an appointment that was confirmed and has passed (enter appointment id manually)
        public void RateAppointment()
        {
            var token = m_Token13980;
            var appointmentId = "393415";

            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(token);
            var appointment = status.Appointments.FirstOrDefault(x => x.Id == appointmentId);
            var details = client.GetAppointmentDetails(token, new AppointmentDetailsRequest() { AppointmentId = appointmentId });

            client.Rate(token, new RateAppointmentRequest()
            {
                AppointmentId = appointmentId,
                Rating = 4,
                Comment = "Alles gut."
            });
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void GetStatistics()
        {
            var client = AdvoAssistClient.CreateInstance();

            var statistics = client.GetStatistics(m_Token13980, new StatisticsRequest());

            statistics.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        public void GetIdentity()
        {
            var client = AdvoAssistClient.CreateInstance();

            var identity = client.GetIdentity(m_Token13980, new IdentityRequest() { IdentityId = "13980" });

            identity.Should().NotBeNull();
        }

        [TestMethod]
        [TestProperty("Subject", "AdvoAssistClient")]
        [Ignore] // can only be executed on an appointment that was confirmed and has passed (enter appointment id manually)
        public void InvoiceAppointment()
        {
            var token = m_Token13980;
            var appointmentId = "393415";

            var client = AdvoAssistClient.CreateInstance();

            var status = client.GetStatus(token);

            var appointment = status.Appointments.FirstOrDefault(x => x.Id == appointmentId);

            appointment.Should().NotBeNull();

            var info = client.InvoiceAppointment(token, new AppointmentInvoiceRequest()
            {
                AppointmentId = appointment.Id,
                InvoiceNumber = "12345",
                RemunerationValue = 500,
            });

            info.Should().NotBeNull();

            Trace.WriteLine(info.AccountNumber);
            Trace.WriteLine(info.AccountHolder);
            Trace.WriteLine(info.Amount);
        }
    }
}