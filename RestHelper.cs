using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Text;
using WK.DE.Logging;

namespace AnNoText.AdvoAssist.Rest
{
    public static class RestHelper
    {
        private static readonly ILogEx m_Logger = LogEx.GetLogger(typeof(RestHelper));

        private static Encoding m_DefaultEncoding = new UTF8Encoding(false);

        public static string GetResponse(string url)
        {
            return GetResponse(url, null);
        }

        public static string GetResponse(string url, Stream contentStream)
        {
            string response;

            using (var responseStream = GetResponseStream(url, contentStream))
            using (var responseReader = new StreamReader(responseStream))
            {
                try
                {
                    response = responseReader.ReadToEnd();
                }
                catch (Exception ex)
                {
                    m_Logger.Error("Request failed", ex);
                    throw;
                }
            }

            return response;
        }

        public static Stream GetResponseStream(string url)
        {
            return GetResponseStream(url, null);
        }

        public static Stream GetResponseStream(string url, Stream contentStream)
        {
            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.Headers.Add("charset", "utf-8");

            m_Logger.DebugFormat("{0}://{1} : POST {2} HTTP/{3}.{4}",
                request.RequestUri.Scheme,
                request.RequestUri.Host,
                request.RequestUri.PathAndQuery,
                request.ProtocolVersion.Major,
                request.ProtocolVersion.Minor);

            if (contentStream != null)
            {
                string content;
                var localContentStream = GetStreamAndLoggingContent(contentStream, out content, false);

                try
                {
                    if (m_Logger.IsVerboseEnabled)
                        m_Logger.VerboseFormat("Sending request data: {0}", content);

                    using (var requestStream = request.GetRequestStream())
                        localContentStream.CopyTo(requestStream);
                }
                finally
                {
                    // dispose the the content stream if it is different from the stream passed in as parameter
                    // because this means it was created by GetStreamAndLoggingContent and needs to be disposed before leaving the method
                    if (localContentStream != contentStream)
                        localContentStream.Dispose();
                }
            }

            Stream responseStream = null;
            try
            {
                var webResponse = request.GetResponse();
                responseStream = webResponse.GetResponseStream() ?? Stream.Null;

                string content;
                responseStream = GetStreamAndLoggingContent(responseStream, out content, true);

                if (m_Logger.IsVerboseEnabled)
                    m_Logger.VerboseFormat("Received response data: {0}", content);

                return responseStream;
            }
            catch (WebException ex)
            {
                m_Logger.Error("Request failed", ex);

                if (m_Logger.IsVerboseEnabled && responseStream == null)
                {
                    responseStream = ex.Response?.GetResponseStream();
                    if (responseStream != null)
                    {
                        string content;
                        responseStream = GetStreamAndLoggingContent(responseStream, out content, true);
                        m_Logger.VerboseFormat("Received response data: {0}", content);
                    }
                }

                // dispose the response stream in case of error
                if (responseStream != null)
                    responseStream.Dispose();

                throw;
            }
            catch (Exception ex)
            {
                m_Logger.Error("Request failed", ex);

                // dispose the response stream in case of error
                if (responseStream != null)
                    responseStream.Dispose();

                throw;
            }
        }

        private static Stream GetStreamAndLoggingContent(Stream stream, out string content, bool disposeInputStream, Encoding encoding = null)
        {
            if (!m_Logger.IsVerboseEnabled)
            {
                content = null;
                return stream;
            }

            MemoryStream ms = null;
            byte[] contentData;
            try
            {
                ms = new MemoryStream();
                stream.CopyTo(ms);
                ms.Position = 0;

                if (disposeInputStream)
                    stream.Dispose();

                contentData = ms.ToArray();
            }
            catch (Exception ex)
            {
                m_Logger.Error("Error when trying to copy stream for logging", ex);

                if (ms != null)
                    ms.Dispose();

                throw;
            }

            try
            {
                encoding = encoding ?? m_DefaultEncoding;
                content = encoding.GetString(contentData);
            }
            catch (Exception ex)
            {
                m_Logger.Warn(String.Format("Error when trying to decode stream content using {0} encoding, falling back to base64", encoding.WebName), ex);
                content = Convert.ToBase64String(contentData);
            }

            return ms;
        }

        public static TResult GetResponse<TResult>(string url)
        {
            return GetResponse<TResult>(url, null);
        }

        public static TResult GetResponse<TResult>(string url, Stream contentStream)
        {
            if (typeof(TResult) == typeof(Stream))
                return (TResult)(object)GetResponseStream(url, contentStream);

            var response = GetResponse(url, contentStream);

            var result = JsonConvert.DeserializeObject<TResult>(response);

            return result;
        }
    }
}