using DryIoc;
using log4net;
using NetOffice.WordApi.Enums;
using System;
using System.IO;
using System.Linq;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.Tools;

namespace WK.DE.DocumentManagement.Services
{
    public class PdfConversionService : IDisposable, IPdfConversionService
    {
        private static readonly ILog m_Logger = LogManager.GetLogger(typeof(PdfConversionService));

        private string[] m_ConvertableExtensions;

        private readonly IContainer m_IocContainer;

        private bool m_DisposedValue;
        private NetOffice.WordApi.Application m_WordApplication;
        private NetOffice.PowerPointApi.Application m_PowerpointApplication;

        public PdfConversionService(IContainer iocContainer)
        {
            Guard.NotNull(iocContainer, nameof(iocContainer));

            m_IocContainer = iocContainer;

			m_ConvertableExtensions = FileExtensionsService.GetFileExtensions(FileExtensionGroup.Word)
                                        .Union(FileExtensionsService.GetFileExtensions(FileExtensionGroup.Powerpoint))
                                        .Union(FileExtensionsService.GetFileExtensions(FileExtensionGroup.PDF))
                                        .Union(FileExtensionsService.GetFileExtensions(FileExtensionGroup.Mail))
                                        .Union(FileExtensionsService.GetFileExtensions(FileExtensionGroup.Image))
										.Union(FileExtensionsService.GetFileExtensions(FileExtensionGroup.XPS))
										.ToArray();

            if (iocContainer.IsRegistered<ILegacyDocumentHandlingService>())
            {
                var legacyDocumentHandlingService = iocContainer.Resolve<ILegacyDocumentHandlingService>();
				var pdfConvertableFileExctions = legacyDocumentHandlingService.GetPdfConvertableExtensions();
                if (pdfConvertableFileExctions != null && pdfConvertableFileExctions.Any())
                {
					m_ConvertableExtensions = m_ConvertableExtensions.Union(pdfConvertableFileExctions).ToArray();
				}
			}
        }

        public bool CanConvertDocument(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            return m_ConvertableExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase);
        }

        public string GetConvertableExtensionsFriendlyName()
        {
            var friendlyNames = new string[] { "Word-Dokumente und -Vorlagen", "E-Mails", "Powerpoint-Folien", "Bild-Dateien", "XPS-Dokumente" };
            if (m_IocContainer.IsRegistered<ILegacyDocumentHandlingService>())
            {
                var legacyDocumentHandlingService = m_IocContainer.Resolve<ILegacyDocumentHandlingService>();
                friendlyNames = friendlyNames.Union(legacyDocumentHandlingService.GetPdfConvertableExtensionsFriendlyName()).ToArray();
			}
            return String.Join(Environment.NewLine, friendlyNames);
        }

        public string Convert(string sourceFileName, bool exportWordDocumentWithMarkups = false)
        {
            var targetFileName = Path.ChangeExtension(sourceFileName, ".pdf");
            Convert(sourceFileName, targetFileName, exportWordDocumentWithMarkups);
            return targetFileName;
        }

        public void Convert(string sourceFileName, string targetFileName, bool exportWordDocumentWithMarkups = false)
        {
            var extensionGroup = FileExtensionsService.GetGroupFromExtension(sourceFileName);
            m_Logger.DebugFormat("converting file \"{0}\", recognized file group {1}", sourceFileName, extensionGroup);
			switch (extensionGroup)
            {
                case FileExtensionGroup.Word:
                    ConvertWordToPdf(sourceFileName, targetFileName, exportWordDocumentWithMarkups, true);
                    return;

                case FileExtensionGroup.Text:
                    ConvertWordToPdf(sourceFileName, targetFileName, false, true);
                    return;

                case FileExtensionGroup.Powerpoint:
                    ConvertPowerpointToPdf(sourceFileName, targetFileName);
                    return;

                case FileExtensionGroup.PDF:
					if (File.Exists(sourceFileName) && !string.Equals(sourceFileName, targetFileName, StringComparison.OrdinalIgnoreCase))
					{
                        File.Copy(sourceFileName, targetFileName, true);
                    }
                    return;

                case FileExtensionGroup.Mail:
                    ConvertMailToPdf(sourceFileName, targetFileName);
                    return;

                case FileExtensionGroup.Image:
                    ConvertImageToPdf(sourceFileName, targetFileName);
                    return;

                case FileExtensionGroup.XPS:
					ConvertXpsToPdf(sourceFileName, targetFileName);
                    return;

				default:
                    if (m_IocContainer.IsRegistered<ILegacyDocumentHandlingService>())
                    {
						m_Logger.Debug("using ILegacyDocumentHandlingService to convert file");

						var extension = Path.GetExtension(sourceFileName);

						var legacyDocumentHandlingService = m_IocContainer.Resolve<ILegacyDocumentHandlingService>();
                        var possibleExtensions = legacyDocumentHandlingService.GetPdfConvertableExtensions();
                        if (possibleExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase))
                        {
							legacyDocumentHandlingService.ConvertFileToPdf(sourceFileName, targetFileName);
							return;
						}
					}
                    else
                    {
						m_Logger.Debug("no ILegacyDocumentHandlingService, can not convert file");
					}
					break;

			}
            throw new NotSupportedException(String.Format("conversion of extension \"{0}\" not supported", Path.GetExtension(sourceFileName)));
        }

		private void ConvertWordToPdf(string sourceFileName, string targetFileName, bool exportDocumentWithMarkup, bool exportAsPdfA)
        {
            if (m_WordApplication == null)
            {
                m_WordApplication = new NetOffice.WordApi.Application();
            }

            var exportItem = WdExportItem.wdExportDocumentContent;
            if (exportDocumentWithMarkup)
            {
                exportItem = WdExportItem.wdExportDocumentWithMarkup;
            }

            using (var document = m_WordApplication.Documents.Open(sourceFileName, false, true, false))
            {
                try
                {
                    document.ExportAsFixedFormat(targetFileName, WdExportFormat.wdExportFormatPDF, null, null, null, null, null, exportItem, null, null, null, null, null, exportAsPdfA);

                    document.Close(false);
                }
                catch (Exception exp)
                {
                    if (exp.InnerException != null && exp.InnerException.InnerException != null && exp.InnerException.InnerException.Message.Contains("Wir konnten Ihre Datei leider nicht finden. Wurde sie verschoben, umbenannt oder gelöscht?"))
                    {
                        m_Logger.ErrorFormat("Exception while converting word-document to PDF/A: {0}", exp.InnerException.InnerException.Message);
                        m_Logger.ErrorFormat("Failed to export word-document:{0} to PDF/A, retrying PDF", sourceFileName);

                        document.ExportAsFixedFormat(targetFileName, WdExportFormat.wdExportFormatPDF, null, null, null, null, null, exportItem, null, null, null, null, null, false);

                        document.Close(false);
                    }
					else
					{
                        throw exp;
                    }
                }
            }
            File.Delete(sourceFileName);
        }

        private void ConvertPowerpointToPdf(string sourceFileName, string targetFileName)
        {
            if (m_PowerpointApplication == null)
            {
                m_PowerpointApplication = new NetOffice.PowerPointApi.Application();
            }
            using (var presentation = m_PowerpointApplication.Presentations.Open(sourceFileName))
            {
                presentation.SaveAs(targetFileName, NetOffice.PowerPointApi.Enums.PpSaveAsFileType.ppSaveAsPDF);
                presentation.Close();
            }
            File.Delete(sourceFileName);
        }

		private void ConvertMailToPdf(string sourceFileName, string targetFileName)
		{
			var tempPath = TempPathManager.GetPrivateTempPath();

			var pdfFile = wk.MessageReader.MessageReader.ConvertEmailToPdf(sourceFileName, tempPath, false);

			File.Move(pdfFile, targetFileName);

            TempPathManager.CleanTempPath(tempPath);
		}

        private void ConvertImageToPdf(string sourceFileName, string targetFileName)
        {
            var pdfUtilityService = m_IocContainer.Resolve<IPdfUtilityService>();
            pdfUtilityService.CreateNewPdfFromImages(new string[] { sourceFileName }, targetFileName);
		}

		private void ConvertXpsToPdf(string sourceFileName, string targetFileName)
		{
            var pdfUtilityService = m_IocContainer.Resolve<IPdfUtilityService>();
            pdfUtilityService.ResavePDF(sourceFileName, targetFileName, 0);
		}

		protected virtual void Dispose(bool disposing)
        {
            if (!m_DisposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                if (m_WordApplication != null)
                {
                    m_WordApplication.Quit();
                    m_WordApplication.Dispose();
                    m_WordApplication = null;
                }

                if (m_PowerpointApplication != null)
                {
                    m_PowerpointApplication.Quit();
                    m_PowerpointApplication.Dispose();
                    m_PowerpointApplication = null;
                }

                m_DisposedValue = true;
            }
        }

        ~PdfConversionService()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        public void Dispose()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
	}
}