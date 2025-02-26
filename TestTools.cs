using AT.Core;
using AT.Core.BasicConfiguration;
using AT.Core.DAL;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net.Appender;
using log4net.Core;
using log4net.Layout;
using log4net.Repository.Hierarchy;

namespace WK.DE.TestFramework
{
	public class TestTools
	{
		public static void InitializeRepository(TestContext context)
		{
			ATSystemConfiguration systemConfiguration;
			OfficeConfiguration officeConfiguration;
			InitializeRepository(context, out systemConfiguration, out officeConfiguration);
		}
		public static void InitializeRepository(TestContext context, out ATSystemConfiguration systemConfiguration, out OfficeConfiguration officeConfiguration)
		{
			var configuration = GetConfigurationInformation();

			CoreFactory.InitializeDefaultRepositoryForTest(configuration.Item1, configuration.Item2);

			systemConfiguration = configuration.Item1;
			officeConfiguration = configuration.Item2;
		}

		internal static Tuple<ATSystemConfiguration, OfficeConfiguration> GetConfigurationInformation()
		{
            ATSystemConfiguration atsystemConfiguration;
            OfficeConfiguration officeConfiguration;

            var localBuildSettings = "c:\\tfs\\LocalDatabaseSettings.txt";
            if (File.Exists(localBuildSettings))
            {
                var serverNameTextAnNoText = File.ReadAllLines(localBuildSettings).FirstOrDefault(x => x.StartsWith("serverNameTextAnNoText:"));
                serverNameTextAnNoText = serverNameTextAnNoText.Substring("serverNameTextAnNoText:".Length);

                var databaseNameTextAnNoTextOffice = File.ReadAllLines(localBuildSettings).FirstOrDefault(x => x.StartsWith("databaseNameTextAnNoTextOffice:"));
                databaseNameTextAnNoTextOffice = databaseNameTextAnNoTextOffice.Substring("databaseNameTextAnNoTextOffice:".Length);

                var databaseNameTextAnNoTextSystem = File.ReadAllLines(localBuildSettings).FirstOrDefault(x => x.StartsWith("databaseNameTextAnNoTextSystem:"));
                databaseNameTextAnNoTextSystem = databaseNameTextAnNoTextSystem.Substring("databaseNameTextAnNoTextSystem:".Length);

                //atsystemConfiguration = new ATSystemConfiguration(serverNameTextAnNoText, databaseNameTextAnNoTextSystem, null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
                //officeConfiguration = new OfficeConfiguration("OFFICE", serverNameTextAnNoText, databaseNameTextAnNoTextOffice, null, null);

                atsystemConfiguration = new ATSystemConfiguration("localhost\\ANNOTEXT", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
                officeConfiguration = new OfficeConfiguration("OFFICE", "localhost\\ANNOTEXT", "ATUT_BUSINESSNB", null, null);

                return new Tuple<ATSystemConfiguration, OfficeConfiguration>(atsystemConfiguration, officeConfiguration);
            }

            switch (Environment.MachineName)
			{
                case "01HW2149893":
                    atsystemConfiguration = new ATSystemConfiguration("localhost\\ANNOTEXT\\ANNOTEXT", "IntegrationTests_AnNoTextSystem", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
                    officeConfiguration = new OfficeConfiguration("OFFICE", "localhost\\ANNOTEXT\\ANNOTEXT", "IntegrationTests_AnNoTextOffice", null, null);
                    break;

                case "STEPHANGRUN8DA3": //Parallels VM Stephan Grunewald
					atsystemConfiguration = new ATSystemConfiguration(@"localhost\SQL2019", "ATSYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", @"localhost\SQL2019", "DEMO", null, null);
					break;
		
				case "EUDE22LRLP00004": //Laptop Stephan Grunewald
					atsystemConfiguration = new ATSystemConfiguration(@"localhost\SQL2017", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", @"localhost\SQL2017", "ATUT_DEMO", null, null);
					break;

				case "DE66LRL3100VD": // Laptop Birgit
					atsystemConfiguration = new ATSystemConfiguration("localhost", "ATUT_SYSTEMQ", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost", "ATUT_BUSINESSQ", null, null);
					break;

				case "EUDE35LRLP01465": //Laptop Alexander Markelov
					atsystemConfiguration = new ATSystemConfiguration("localhost\\SQL2019", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost\\SQL2019", "ATUT_BUSINESSNB", null, null);
					break;

				case "DE66LRL254V7G": //Laptop Klaus Hermes
					atsystemConfiguration = new ATSystemConfiguration("localhost", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost", "ATUT_DEMO", null, null);
					break;

				case "DE66LSWL3415F7": //Laptop Nico Schumacher
					atsystemConfiguration = new ATSystemConfiguration("localhost", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost", "ATUT_DEMO", null, null);
					break;

				case "DE66LRL4099Y8": //Laptop Michael Engelmann
					atsystemConfiguration = new ATSystemConfiguration("DE66LRL4099Y8\\SQL2019", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "DE66LRL4099Y8\\SQL2019", "ATUT_DEMO", null, null);
					break;

				case "DE66LRL4099Y5": //Laptop Sebastian Altenschmidt
					atsystemConfiguration = new ATSystemConfiguration("localhost\\ANNOTEXT", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost\\ANNOTEXT", "ATUT_BUSINESS", null, null);
					break;

				default:
					atsystemConfiguration = new ATSystemConfiguration("localhost", "ATUT_SYSTEM", null, null, ConnectionSecurityTypes.TrustedConnection, false, false, false);
					officeConfiguration = new OfficeConfiguration("OFFICE", "localhost", "ATUT_BUSINESSNB", null, null);
					break;
			}

			return new Tuple<ATSystemConfiguration, OfficeConfiguration>(atsystemConfiguration, officeConfiguration);
		}

		internal static void AssertByteContent(Stream expected, Stream actual)
		{
			Assert.AreEqual(expected.Length, actual.Length, "Länge der Streams stimmt nicht überein.");

			expected.Seek(0, SeekOrigin.Begin);
			actual.Seek(0, SeekOrigin.Begin);

			var bytesExpected = new byte[expected.Length];
			var bytesActual = new byte[actual.Length];

			expected.Read(bytesExpected, 0, (int)expected.Length);
			actual.Read(bytesActual, 0, (int)actual.Length);

			//wenn die Bytes identisch sind, Methode verlassen, sonst Byte by Byte Vergleich um genaue Stelle ausgeben zu können
			if (bytesExpected.SequenceEqual(bytesActual))
				return;

			bytesActual = bytesActual.ToArray();
			if (bytesExpected.SequenceEqual(bytesActual))
				return;

			for (int index = 0; index < actual.Length; index++)
				Assert.AreEqual(bytesExpected[index], bytesActual[index], String.Format("Byte {0} nicht identisch", index));
		}

		internal static void AssertByteContent(byte[] expected, Stream actual)
        {
			Assert.AreEqual(expected.Length, actual.Length, "Länge des Streams stimmt nicht mit Länge des Arrays überein.");

			actual.Seek(0, SeekOrigin.Begin);
			for (int index = 0; index < actual.Length; index++)
				Assert.AreEqual(expected[index], actual.ReadByte(), String.Format("Byte {0} nicht identisch", index));
        }

		internal static void AssertByteContent(byte[] expected, byte[] actual)
        {
			Assert.AreEqual(expected.Length, actual.Length, "Länge des neuen Arrays stimmt nicht mit Länge der Vorlage überein.");

			for (int index = 0; index < actual.Length; index++)
				Assert.AreEqual(expected[index], actual[index], String.Format("Byte {0} nicht identisch", index));
        }

		internal static void InitializeTraceLogging()
		{
			log4net.Config.BasicConfigurator.Configure(new TraceAppender()
			{
				Threshold = Level.Debug,
				Layout = new PatternLayout("%-7level %date [%2thread] %type{1} %M - %message%newline")
			});

			var hierarchy = (Hierarchy)log4net.LogManager.GetRepository();
			hierarchy.Root.Level = Level.Debug;
			hierarchy.Root.Log(hierarchy.Root.Level, "Logging enabled", null);
		}
    }
}
