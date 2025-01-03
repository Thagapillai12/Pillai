using AT.Core.DAL;
using DryIoc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using WK.DE.DocumentManagement.Contracts.Services;
using WK.DE.TestFramework;

namespace WK.DE.DocumentManagement.IntegrationTests
{
	[TestClass]
	public class PdfConversionTests
	{
		private static readonly string pdfWithMarkupFilename = Path.Combine(Path.GetTempPath(), @"DocWithMarkup.pdf");
		private static readonly string pdfWithoutMarkupFilename = Path.Combine(Path.GetTempPath(), @"DocWithoutMarkup.pdf");

		private static readonly string docxWithMarkupFilename = @"..\Tests\WK.DE.DocumentManagement.IntegrationTests\Resources\DocWithMarkup.docx";
		private static readonly string docxWithMarkupFilename1 = Path.Combine(Path.GetTempPath(), @"DocWithMarkup1.docx");
		private static readonly string docxWithMarkupFilename2 = Path.Combine(Path.GetTempPath(), @"DocWithMarkup2.docx");
		
		private static readonly string toBeConvertDocxFilename = Path.Combine(Path.GetTempPath(), @"Document.docx");
		private static readonly string convertedPdfFilename = Path.Combine(Path.GetTempPath(), @"Document.pdf");

		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			TestTools.InitializeRepository(context);
		}

		[TestInitialize]
		public void TestInizialize()
		{ 
			File.Delete(pdfWithMarkupFilename);
			File.Delete(pdfWithoutMarkupFilename);
			File.Delete(docxWithMarkupFilename1);
			File.Delete(docxWithMarkupFilename2);
			File.Delete(toBeConvertDocxFilename);
			File.Delete(convertedPdfFilename);
		}

		[TestMethod]
		public void DMS_PdfConversionService_PDFsFromWordFileHasTheSameNameExceptExtension()
		{
			// ============================== arrange ==============================
			var iocContainer = IocContainerHelper.CreateDocumentIocContainerFactory();
			var repository = iocContainer.Resolve<IRepository>();
			using (var pdfConversionService = iocContainer.Resolve<IPdfConversionService>())
			{

				// ==============================   act   ==============================
				File.SetAttributes(docxWithMarkupFilename, FileAttributes.Normal);
				File.Copy(docxWithMarkupFilename, toBeConvertDocxFilename);

				pdfConversionService.Convert(toBeConvertDocxFilename, true);
				var pdfWithoutMarkupFileInfo = new FileInfo(convertedPdfFilename);

				// ==============================  assert ==============================
				Assert.IsTrue(Path.GetFileNameWithoutExtension(pdfWithoutMarkupFileInfo.Name).Equals(Path.GetFileNameWithoutExtension(toBeConvertDocxFilename), StringComparison.OrdinalIgnoreCase));
			}
		}

		[TestMethod]
		public void DMS_PdfConversionService_PDFsFromWordFileThatIncludesMarkupsConvertedWithAndWithoutMarkupsAreDifferent()
		{
			// ============================== arrange ==============================
			var iocContainer = IocContainerHelper.CreateDocumentIocContainerFactory();
			var repository = iocContainer.Resolve<IRepository>();
			using (var pdfConversionService = iocContainer.Resolve<IPdfConversionService>())
			{
				File.SetAttributes(docxWithMarkupFilename, FileAttributes.Normal);
				File.Copy(docxWithMarkupFilename, docxWithMarkupFilename1);
				File.SetAttributes(docxWithMarkupFilename, FileAttributes.Normal);
				File.Copy(docxWithMarkupFilename, docxWithMarkupFilename2);

				// ==============================   act   ==============================
				pdfConversionService.Convert(docxWithMarkupFilename1, pdfWithoutMarkupFilename);
				pdfConversionService.Convert(docxWithMarkupFilename2, pdfWithMarkupFilename, true);
				var pdfWithoutMarkupFileInfo = new FileInfo(pdfWithoutMarkupFilename);
				var pdfWithMarkupFileInfo = new FileInfo(pdfWithMarkupFilename);

				// ==============================  assert ==============================
				Assert.IsTrue(pdfWithoutMarkupFileInfo.Length < pdfWithMarkupFileInfo.Length);
			}
		}
	}
}
