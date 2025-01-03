using System;
using AT.Core.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using WK.DE.SmartDocuments.Contracts;

namespace AnNoText.Abrechnung.UnitTests
{
	[TestClass]
	public class LegalSmartDocumentsTests
	{
		[TestMethod]
		public void Constructor_Simple_Instance()
		{
			// Arange

			// Act
			LegalSmartDocuments legalSmartDocuments = new LegalSmartDocuments();

			// Assert
			Assert.IsInstanceOfType(legalSmartDocuments, typeof(LegalSmartDocuments));
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException), "Der Parameter 'connectionInfo' darf nicht leer sein.")]
		public void StartDocument_NoConnection_ArgumentNullException()
		{
			// Arange
			LegalSmartDocuments legalSmartDocuments = new LegalSmartDocuments();

			// Act
			legalSmartDocuments.StartDocument(null, null);

			// Assert
		}

		[TestMethod]
		public void StartDocument_ShowDialogIsFalse_ShowDialogReturnedFalse()
		{
			// Arange
			ObjectFactory objectFactory = ObjectFactory.Init();
			LegalSmartDocuments legalSmartDocuments = new LegalSmartDocuments();

			var smartDocumentsFactoryMock = new Mock<ISmartDocumentsFactory>();
			var templateListReaderMock = new Mock<ITemplateListReader>();
			var dialogSmartDocsSelectTemplateMock = new Mock<IDialogSmartDocsSelectTemplate>();
			smartDocumentsFactoryMock.Setup(_ => _.CreateTemplateListReader(It.IsAny<ISmartDocsConnectionInfo>())).Returns(templateListReaderMock.Object);
			smartDocumentsFactoryMock.Setup(_ => _.CreateDialogSelectTemplate(It.IsAny<ISmartDocsTemplateGroup>(), It.IsAny<bool>())).Returns(dialogSmartDocsSelectTemplateMock.Object);
			objectFactory.SmartDocumentsFactory = smartDocumentsFactoryMock.Object;

            dialogSmartDocsSelectTemplateMock.Setup(_ => _.ShowDialog()).Returns(false);

            // Act
            legalSmartDocuments.StartDocument(null, null);

			// Assert
			dialogSmartDocsSelectTemplateMock.Verify(_ => _.ShowDialog(), Times.Once);
			dialogSmartDocsSelectTemplateMock.VerifyGet(_ => _.SelectedTemplate, Times.Never);
		}

        [TestMethod]
		public void StartDocument_ShowDialogIsTrue_GotTemplate()
		{
			// Arange
			ObjectFactory objectFactory = ObjectFactory.Init();
			LegalSmartDocuments legalSmartDocuments = new LegalSmartDocuments();

			var smartDocumentsFactoryMock = new Mock<ISmartDocumentsFactory>();
			var templateListReaderMock = new Mock<ITemplateListReader>();
			var dialogSmartDocsSelectTemplateMock = new Mock<IDialogSmartDocsSelectTemplate>();
			var smartDocsTemplateMock = new Mock<ISmartDocsTemplate>();
			var dataBuilderMock = new Mock<IDataTreeBuilder>();
			var smartDocsDataTreeNodeMock = new Mock<ISmartDocsDataTreeNode>();
			var smartDocumentCreatorMock = new Mock<ISmartDocumentCreator>();
			smartDocumentsFactoryMock.Setup(_ => _.CreateTemplateListReader(It.IsAny<ISmartDocsConnectionInfo>())).Returns(templateListReaderMock.Object);
			smartDocumentsFactoryMock.Setup(_ => _.CreateDialogSelectTemplate(It.IsAny<ISmartDocsTemplateGroup>(), It.IsAny<bool>())).Returns(dialogSmartDocsSelectTemplateMock.Object);
			dataBuilderMock.SetupAllProperties();
			dataBuilderMock.Setup(_ => _.CreateRootNode()).Returns(smartDocsDataTreeNodeMock.Object);
			smartDocumentsFactoryMock.SetupGet(_ => _.DataBuilder).Returns(dataBuilderMock.Object);
			smartDocumentsFactoryMock.Setup(_ => _.CreateDocumentCreator(It.IsAny<ISmartDocsConnectionInfo>())).Returns(smartDocumentCreatorMock.Object);
			objectFactory.SmartDocumentsFactory = smartDocumentsFactoryMock.Object;
			dialogSmartDocsSelectTemplateMock.Setup(_ => _.ShowDialog()).Returns(true);

			smartDocsTemplateMock.SetupGet(_ => _.ID).Returns("ID");
			dialogSmartDocsSelectTemplateMock.SetupGet(_ => _.SelectedTemplate).Returns(smartDocsTemplateMock.Object);

			// Act
			legalSmartDocuments.StartDocument(null, null);

			// Assert
			smartDocumentsFactoryMock.Verify(_ => _.CreateDialogSelectTemplate(It.IsAny<ISmartDocsTemplateGroup>(), It.IsAny<bool>()), Times.Once);
			dialogSmartDocsSelectTemplateMock.VerifyGet(_ => _.SelectedTemplate, Times.Once);
		}

		//[TestMethod]
		//public void Test()
		//{
		//	// Arange
		//	LegalSmartDocuments legalSmartDocuments = new LegalSmartDocuments();
		//	var connectionInfoMock = new Mock<IConnectionInfo>();
		//	connectionInfoMock.SetupGet(_ => _.SmartDocumentsUrl).Returns("http://10.49.250.68:8080");
		//	connectionInfoMock.SetupGet(_ => _.IgnoreProxy).Returns(true);
		//	connectionInfoMock.SetupGet(_ => _.EndUserName).Returns("ts");
		//	connectionInfoMock.SetupGet(_ => _.IntegrationUserName).Returns("ts");
		//	connectionInfoMock.SetupGet(_ => _.IntegrationUserPW).Returns("Welcome2LSD");

		//	var akteMock = new Mock<IAkte>();
		//	akteMock.SetupGet(_ => _.AZ).Returns("Aktennummer");
		//	akteMock.SetupGet(_ => _.Rubrum1).Returns("Rubrum1");
		//	akteMock.SetupGet(_ => _.KennungRA1).Returns("KennungRA1");
		//	akteMock.SetupGet(_ => _.KennungReferat).Returns("KennungReferat");
		//	akteMock.SetupGet(_ => _.Bemerkung).Returns("Bemerkung");

		//	var mandantMock = new Mock<IAkteBeteiligter>();
		//	var adressatMandantMock = new Mock<IAdressat>();
		//	adressatMandantMock.SetupGet(_ => _.Nachname).Returns("Mandant Nachname");
		//	adressatMandantMock.SetupGet(_ => _.Vorname).Returns("Mandant Vorname");
		//	adressatMandantMock.SetupGet(_ => _.AdresseGegnerMB1).Returns("Mandant AdresseGegnerMB1");
		//	adressatMandantMock.SetupGet(_ => _.AdresseGegnerMB2).Returns("Mandant AdresseGegnerMB2");
		//	adressatMandantMock.SetupGet(_ => _.AdresseGegnerMB3).Returns("Mandant AdresseGegnerMB3");
		//	mandantMock.SetupGet(_ => _.Adressat).Returns(adressatMandantMock.Object);
		//	akteMock.Setup(_ => _.HoleErstenMandantenZurAkte()).Returns(mandantMock.Object);

		//	var gegnerMock = new Mock<IAkteBeteiligter>();
		//	var adressatGegnerMock = new Mock<IAdressat>();
		//	adressatGegnerMock.SetupGet(_ => _.Nachname).Returns("Gegner Nachname");
		//	adressatGegnerMock.SetupGet(_ => _.Vorname).Returns("Gegner Vorname");
		//	adressatGegnerMock.SetupGet(_ => _.AdresseGegnerMB1).Returns("Gegner AdresseGegnerMB1");
		//	adressatGegnerMock.SetupGet(_ => _.AdresseGegnerMB2).Returns("Gegner AdresseGegnerMB2");
		//	adressatGegnerMock.SetupGet(_ => _.AdresseGegnerMB3).Returns("Gegner AdresseGegnerMB3");
		//	gegnerMock.SetupGet(_ => _.Adressat).Returns(adressatGegnerMock.Object);
		//	akteMock.Setup(_ => _.HoleErstenGegnerZurAkte()).Returns(gegnerMock.Object);

		//	// Act
		//	legalSmartDocuments.StartDocument(connectionInfoMock.Object, akteMock.Object);

		//	// Assert
		//}
	}
}
