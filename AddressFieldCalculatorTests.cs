using AT.Core;
using AT.Core.DAL;
using AT.Core.Objects;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WK.DE.TestFramework;

namespace AT.BL.IntegrationTests
{
	[TestClass()]
	public class AddressFieldCalculatorTests
	{
		[ClassInitialize]
		public static void Initialize(TestContext context)
		{
			TestTools.InitializeRepository(context);
		}

		[TestInitialize]
		public void TestInizialize()
		{
			ResetDatabase.ResetParameterAnredeformen();
		}

		[TestMethod]
		public void GetddressMarriedCoupleSameName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheleute";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Meier";
			string firstname = "Michael";
			string firstnamePartner = "Michaela";
			string titel = "Dr.";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, firstname, firstnamePartner, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Frau u. Herrn" + Environment.NewLine + "Michaela u. Dr. Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressMarriedCoupleDifferentName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheleute";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Müller";
			string firstname = "Michael";
			string firstnamePartner = "Michaela";
			string titel = "Dr.";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, firstname, firstnamePartner, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Frau u. Herrn" + Environment.NewLine + "Michaela Müller u. Dr. Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressMarriedCoupleSameName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheleute";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Meier";
			string firstname = "Michael";
			string firstnamePartner = "Michaela";
			string titel = "Dr.";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, firstname, firstnamePartner, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Frau u. Herrn" + Environment.NewLine + "Michaela u. Dr. Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressMarriedCoupleSameNameDifferentAdressFirst()
		{
			// ============================== arrange ==============================
			string anredePartner1 = "Mann";
			string anredePartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Maeier";
			string firstname = "Michael";
			string firstnamePartner = "Michaela";
			string titel = "Dr.";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string streetAndNumberPartner = "Robert-Bosh-Str. 9";
			string postCodePartner = "50354";
			string countryPartner = "Deutschland";
			string cityPartner = "Hürth";
			bool differentAddress = true;
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutationPartner1 = GetAdressForMariedCouple(anredePartner1, null, null, name, null, firstname, null, titel, streetAndNumber, null, postCode, country, internationaleBezeichnung, city, differentAddress);
			var addresslabelSalutationPartner2 = GetAdressForMariedCouple(anredePartner2, null, null, namePartner, null, firstnamePartner, null, null, streetAndNumberPartner, null, postCodePartner, countryPartner, internationaleBezeichnung, cityPartner, differentAddress);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutationPartner1);
			Assert.AreEqual("Frau" + Environment.NewLine + "Michaela Maeier" + Environment.NewLine + "Robert-Bosh-Str. 9" + Environment.NewLine + "50354 Hürth", addresslabelSalutationPartner2);
		}

		[TestMethod]
		public void GetAddressNatPerson()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string titel = "";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressJurPerson()
		{
			// ============================== arrange ==============================
			string anredeTypG = "GmbH";
			string name = "Sanitär GmbH";
			string firstname = "";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string titel = "";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Firma" + Environment.NewLine + "Sanitär GmbH" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressNatPersonNetherlands()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string streetAndNumber = "Groot Hertoginnelaan 18-20";
			string postCode = "2517 EG";
			string country = "Niederlande";
			string city = "Den Haag";
			string adressAdditional = "";
			string titel = "";
			string internationaleBezeichnung = "NETHERLANDS";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Michael Meier" + Environment.NewLine + "Groot Hertoginnelaan 18-20" + Environment.NewLine + "2517 EG Den Haag" + Environment.NewLine +"NETHERLANDS", addresslabelSalutation);
		}
		[TestMethod]
		public void GetAddressNatPersonFrance()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string streetAndNumber = "8 Allee des Lilas";
			string postCode = "75014";
			string country = "Frankreich";
			string city = "Paris";
			string adressAdditional = "Residence Des FLEURS";
			string titel = "";
			string internationaleBezeichnung = "FRANCE";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Michael Meier" + Environment.NewLine + "Residence Des FLEURS" + Environment.NewLine + "8 Allee des Lilas" + Environment.NewLine + "75014 Paris" + Environment.NewLine+ "FRANCE", addresslabelSalutation);
		}
		[TestMethod]
		public void GetAddressNatPersonWithTitel()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string streetAndNumber = "Robert-Bosh-Str. 6";
			string postCode = "50354";
			string country = "Deutschland";
			string city = "Hürth";
			string adressAdditional = "";
			string titel = "Dr.";
			string internationaleBezeichnung = "";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "Robert-Bosh-Str. 6" + Environment.NewLine + "50354 Hürth", addresslabelSalutation);
		}

		[TestMethod]
		public void GetAddressNatPersonWithTitelForeignCountry()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "17 Rue Molière";
			string postCode = "93100";
			string country = "Frankreich";
			string city = "Montreuil";
			string adressAdditional = "";
			string internationaleBezeichnung = "FRANCE";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "17 Rue Molière" + Environment.NewLine + "93100 Montreuil" + Environment.NewLine+ "FRANCE", addresslabelSalutation);
		}

		//[TestMethod]
		//public void GetAddressNatPersonWithTitelGreatBritain()
		//{
		//	// ============================== arrange ==============================
		//	string anredeTypG = "Mann";
		//	string gender = "Mann";
		//	string name = "Meier";
		//	string firstname = "Michael";
		//	string titel = "Dr.";
		//	string streetAndNumber = "23 Belgrave Square";
		//	string postCode = "SW1X 8PZ";
		//	string country = "Großbritannien";
		//	string city = "London";
		//	string adressAdditional = "";
		//	string internationaleBezeichnung = "UNITED KINGDOM";

		//	// ==============================   act   ==============================
		//	var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

		//	// ==============================  assert ==============================
		//	Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "23 Belgrave Square" + Environment.NewLine + "London" + Environment.NewLine + "SW1X 8PZ" + Environment.NewLine + Environment.NewLine + "UNITED KINGDOM", addresslabelSalutation);
		//}
		[TestMethod]
		public void GetAddressNatPersonWithTitelUSA()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "203 East 50th St.";
			string postCode = "10022";
			string country = "Vereinigte Staaten";
			string city = "New York";
			string adressAdditional = "NY";
			string internationaleBezeichnung = "USA";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "203 East 50th St." + Environment.NewLine + "New York, NY 10022" + Environment.NewLine  + "USA", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressBelgium()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "Rue Jacques de Lalaing 8/14";
			string postCode = "1040";
			string country = "Belgien";
			string city = "Bruxelles";
			string adressAdditional = "Zusatz";
			string internationaleBezeichnung = "BELGIUM";


			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine +"Zusatz"+ Environment.NewLine + "Rue Jacques de Lalaing 8/14" + Environment.NewLine + "1040 Bruxelles" + Environment.NewLine + "BELGIUM", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressFrance()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "24 rue Marbeau";
			string postCode = "75116";
			string country = "Frankreich";
			string city = "Paris";
			string adressAdditional = "Zusatz";
			string internationaleBezeichnung = "FRANCE";


			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "Zusatz" + Environment.NewLine + "24 rue Marbeau" + Environment.NewLine + "75116 Paris" + Environment.NewLine + "FRANCE", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressUnitedKingdom()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "23 Belgrave Square";
			string postCode = "SW1X 8PZ";
			string country = "Großbritannien";
			string city = "London";
			string adressAdditional = "Zusatz";
			string internationaleBezeichnung = "UNITED KINGDOM";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "23 Belgrave Square" +Environment.NewLine + "Zusatz"+ Environment.NewLine + "London" + Environment.NewLine + "SW1X 8PZ" + Environment.NewLine + Environment.NewLine + "UNITED KINGDOM", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressIndia()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "6, 50G, Shantipath";
			string postCode = "110021";
			string country = "Indien";
			string city = "New Delhi";
			string adressAdditional = "Chanakyapuri";
			string internationaleBezeichnung = "INDIA";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "6, 50G, Shantipath"+ Environment.NewLine + "Chanakyapuri" +Environment.NewLine + "New Delhi 110021" + Environment.NewLine + "INDIA", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressMexico()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "Horacio 1506";
			string postCode = "11530";
			string country = "Mexiko";
			string city = "Ciudad de México";
			string adressAdditional = "Delegación Miguel Hidalgo";
			string internationaleBezeichnung = "MEXICO";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "Horacio 1506" + Environment.NewLine + "Delegación Miguel Hidalgo" + Environment.NewLine + "11530 Ciudad de México" + Environment.NewLine + "MEXICO", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressSpain()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "Calle de Fortuny, 8";
			string postCode = "28010";
			string country = "Spanien";
			string city = "Madrid";
			string adressAdditional = "ZUSATZ";
			string internationaleBezeichnung = "SPAIN";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "ZUSATZ"+Environment.NewLine + "Calle de Fortuny, 8" + Environment.NewLine + "28010 Madrid" + Environment.NewLine + "SPAIN", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressSouthKorea()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "416 Hangang-daero";
			string postCode = "34437";
			string country = "Republik Korea";
			string city = "Seoul";
			string adressAdditional = "ZUSATZ";
			string internationaleBezeichnung = "KOREA (REP.)";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "ZUSATZ" + Environment.NewLine + "416 Hangang-daero" + Environment.NewLine + "Seoul 34437" + Environment.NewLine + "KOREA (REP.)", addresslabelSalutation);
		}

		[TestMethod]
		public void GetddressTurkey()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Mann";
			string name = "Meier";
			string firstname = "Michael";
			string titel = "Dr.";
			string streetAndNumber = "İnönü Cd. No:10";
			string postCode = "34437";
			string country = "Türkei";
			string city = "Beyoğlu/İstanbul";
			string adressAdditional = "Gümüşsuyu Mahallesi";
			string internationaleBezeichnung = "TURKEY";

			// ==============================   act   ==============================
			var addresslabelSalutation = GetAdressForMariedCouple(anredeTypG, null, null, name, null, firstname, null, titel, streetAndNumber, adressAdditional, postCode, country, internationaleBezeichnung, city, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Herrn" + Environment.NewLine + "Dr. Michael Meier" + Environment.NewLine + "Gümüşsuyu Mahallesi" + Environment.NewLine + "İnönü Cd. No:10" + Environment.NewLine + "34437 Beyoğlu/İstanbul" + Environment.NewLine + "TURKEY", addresslabelSalutation);
		}


		[TestMethod]
		public void SalutationLegalPerson()
		{
			// ============================== arrange ==============================
			string anredeTyp = "Behörde";
			string name = "Meier";

			// ==============================   act   ==============================
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeForm = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTyp);

			var salutationLegal = Adresses.AddressFieldCalculator.GetSalutation(anredeForm, null, null, name, null, null, null);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrte Damen und Herren,", salutationLegal);
		}

		[TestMethod]
		public void SalutationMan()
		{
			// ============================== arrange ==============================
			string anredeTyp = "Mann";
			string name = "Meier";

			// ==============================   act   ==============================
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeForm = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTyp);

			var salutationLegal = Adresses.AddressFieldCalculator.GetSalutation(anredeForm, null, null, name, null, null, null);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrter Herr Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationWoman()
		{
			// ============================== arrange ==============================
			string anredeTyp = "Frau";
			string name = "Meier";
			string titel = "Dr.";

			// ==============================   act   ==============================
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeForm = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTyp);

			var salutationLegal = Adresses.AddressFieldCalculator.GetSalutation(anredeForm, null, null, name, null, titel, null);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrte Frau Dr. Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationUnderAgedGirl()
		{
			// ============================== arrange ==============================
			string anredeTyp = "Minderjähriges Mädchen";
			string name = "Meier";

			// ==============================   act   ==============================
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeForm = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTyp);

			var salutationLegal = Adresses.AddressFieldCalculator.GetSalutation(anredeForm, null, null, name, null, null, null);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrte Frau Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationUnderAgedBoy()
		{
			// ============================== arrange ==============================
			string anredeTyp = "Minderjähriger Junge";
			string name = "Meier";

			// ==============================   act   ==============================
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeForm = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTyp);
			var salutationLegal = Adresses.AddressFieldCalculator.GetSalutation(anredeForm, null, null, name, null, null, null);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrter Herr Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationMarriedCoupelSameNameDifferentAddressFirst()
		{
			// ============================== arrange ==============================
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Maeier";
			bool differentAdress = true;

			// ==============================   act   ==============================
			var salutationLegalPartner1 = GetSalutationFormMarriedCouple(anredeTypPartner1, null, null, name, namePartner, differentAdress);
			var salutationLegalPartner2 = GetSalutationFormMarriedCouple(anredeTypPartner2, null, null, name, namePartner, differentAdress);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrter Herr Meier,", salutationLegalPartner1);
			Assert.AreEqual("Sehr geehrte Frau Meier,", salutationLegalPartner2);
		}

		[TestMethod]
		public void SalutationMarriedCoupelSameName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheleute";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Meier";

			// ==============================   act   ==============================
			var salutationLegal = GetSalutationFormMarriedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrte Frau Meier," + Environment.NewLine + "sehr geehrter Herr Meier,", salutationLegal);

		}
		[TestMethod]
		public void SalutationMarriedCoupelDifferentName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheleute";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Müller";

			// ==============================   act   ==============================
			var salutationLegal = GetSalutationFormMarriedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrte Frau Müller," + Environment.NewLine + "sehr geehrter Herr Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationCohabitationCoupelSameName()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheähnliche Gemeinschaft";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Frau";
			string name = "Meier";
			string namePartner = "Meier";

			// ==============================   act   ==============================
			var salutationLegal = GetSalutationFormMarriedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, false);

			// ==============================  assert ==============================

			Assert.AreEqual("Sehr geehrte Frau Meier," + Environment.NewLine + "sehr geehrter Herr Meier,", salutationLegal);
		}

		[TestMethod]
		public void SalutationCohabitationCoupelSameGender()
		{
			// ============================== arrange ==============================
			string anredeTypG = "Eheähnliche Gemeinschaft";
			string anredeTypPartner1 = "Mann";
			string anredeTypPartner2 = "Mann";
			string name = "Meier";
			string namePartner = "Meier";

			// ==============================   act   ==============================
			var salutationLegal = GetSalutationFormMarriedCouple(anredeTypG, anredeTypPartner1, anredeTypPartner2, name, namePartner, false);

			// ==============================  assert ==============================
			Assert.AreEqual("Sehr geehrter Herr Meier," + Environment.NewLine + "sehr geehrter Herr Meier,", salutationLegal);
		}

		private static string GetSalutationFormMarriedCouple(string anredeTypG, string anredeTypPartner1, string anredeTypPartner2, string name, string namePartner, bool differentAddress)
		{
			IRepository repository = CoreFactory.GetInstance<IRepository>();
			var anredeFormGemeinsam = repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypG);
			var anredeFormPartner1 = anredeTypPartner1 == null ? null : repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypPartner1);
			var anredeFormPartner2 = anredeTypPartner2 == null ? null : repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypPartner2);

			return Adresses.AddressFieldCalculator.GetSalutation(anredeFormGemeinsam, anredeFormPartner1, anredeFormPartner2, name, namePartner, null, null);
		}

		private static string GetAdressForMariedCouple(string anredeTypG, string anredeTypPartner1, string anredeTypPartner2, string name, string namePartner, string firstname, string firstnamePartner, string titel, string streetAndNumber, string adressAdditional, string postCode, string country, string internationaleBezeichnung, string city, bool differentAddress)
		{
			IRepository m_Repository = CoreFactory.GetInstance<IRepository>();
			var anredeFormGemeinsam = m_Repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypG);
			var anredeFormPartner1 = anredeTypPartner1 == null ? null : m_Repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypPartner1);
			var anredeFormPartner2 = anredeTypPartner2 == null ? null : m_Repository.ReadOne<IParameterAnredeformen>(x => x.Bezeichnung == anredeTypPartner2);
			var addresslabelSalutation = Adresses.AddressFieldCalculator.GetAddress(anredeFormGemeinsam, anredeFormPartner1, anredeFormPartner2, firstname, firstnamePartner, name, namePartner, titel, null, streetAndNumber, postCode, country, internationaleBezeichnung, city, adressAdditional);
			return addresslabelSalutation;
		}
	}
}
