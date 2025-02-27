using AT.Core.BasicConfiguration.Crypto;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WK.DE.Data.IniFileParser;
using WK.DE.Data.IniFileParser.Model;

namespace AT.Core.BasicConfiguration
{
	/// <summary>
	/// Stellt Methoden für das Auslesen der SYS.INI zur Verfügung.
	/// </summary>
	public class BasicSystemConfiguration
	{
		public enum SystemDirectory
		{
			Undefined,
			SystemData,
			PostboxData,
			DataRoot,
		}

		#region Statics

		private static readonly log4net.ILog m_Logger = log4net.LogManager.GetLogger(typeof(BasicSystemConfiguration));
		private static readonly string m_ApplicationName;

		static BasicSystemConfiguration()
		{
			m_ApplicationName = "AnNoText.DataLayer " + System.Diagnostics.Process.GetCurrentProcess().ProcessName;
		}

		/// <summary>
		/// Ruft die Verbindungszeichenfolge zu einer Bürogemeinschaft ab.
		/// </summary>
		/// <param name="configuration">Die Bürogemeinschaft, zu der die Verbindungszeichenfolge abgerufen werden soll.</param>
		/// <returns></returns>
		public static string GetConnectionString(ATSystemConfiguration configuration)
		{
			var builder = new SqlConnectionStringBuilder();
			builder["Data Source"] = configuration.ServerName;
			builder["Initial Catalog"] = configuration.DatabaseName;
			switch (configuration.ConnectionSecurityType)
			{
				case ConnectionSecurityTypes.StandardSecurity:
					builder["Integrated Security"] = "false";
					builder.UserID = configuration.UserId;
					builder.Password = configuration.Password;
					break;

				case ConnectionSecurityTypes.TrustedConnection:
					builder["Integrated Security"] = "true";
					break;

				default:
					throw new NotImplementedException(configuration.ConnectionSecurityType.ToString());
			}
			if (configuration.UseEncryptedSQLConnection)
			{
				builder.Encrypt = true;
				if (configuration.TrustServerCertificate)
				{
					builder.TrustServerCertificate = true;
				}
			}
			builder.ApplicationName = m_ApplicationName;
			//builder.Encrypt = true;
			return builder.ConnectionString;
		}

		public static string GetConnectionStringOleDB(ATSystemConfiguration configuration)
		{
			var connectionString = new StringBuilder("OLEDB;");
			if (configuration.UseEncryptedSQLConnection)
			{
				connectionString.Append("Provider=MSOLEDBSQL;");
			}
			else
			{
				connectionString.Append("Provider=SQLOLEDB.1;");
			}
			connectionString.Append("Data Source=");
			connectionString.Append(configuration.ServerName);
			connectionString.Append(";Initial Catalog=");
			connectionString.Append(configuration.DatabaseName);
			connectionString.Append(";");

			switch (configuration.ConnectionSecurityType)
			{
				case ConnectionSecurityTypes.StandardSecurity:
					connectionString.Append("User ID=");
					connectionString.Append(configuration.UserId);
					connectionString.Append(";Password=");
					connectionString.Append(configuration.Password);
					//connectionString.Append(";Persist Security Info=True"); //führt dazu, dass Windows die Credentials speichert, sollte nicht aktiv sein, ggf. wird es aber vom Excel-AddIn benötigt
					connectionString.Append(";");
					break;

				case ConnectionSecurityTypes.TrustedConnection:
					connectionString.Append("Integrated Security=SSPI;");
					break;

				default:
					throw new NotImplementedException(configuration.ConnectionSecurityType.ToString());
			}

			if (configuration.UseEncryptedSQLConnection)
			{
				connectionString.Append("encrypt=yes;");
				if (configuration.TrustServerCertificate)
				{
					connectionString.Append("trustServerCertificate=yes;");
				}
			}

			connectionString.Append(";Use Procedure for Prepare=1;Auto Translate=True;Tag with column collation when possible=False");
			return connectionString.ToString();
		}

		#endregion

		/// <summary>
		/// Root-Schlüssel für Konfigurationen der Auskunft
		/// </summary>
		public const string REGKEY_AUSKROOT = "SOFTWARE\\AnNoText GmbH\\EuroStar\\Program\\Ausk32";

		public const string SACHGEBIET_ABLAUFSTEUERUNG_ZV = "ZV";
		public const string SACHGEBIET_ABLAUFSTEUERUNG_UN = "UN";

		/// <summary>
		/// Ruft an, ob die Konfiguration für einen Dienst geladen werden soll.
		/// </summary>
		private readonly bool m_InitForService;

		public bool InitForService {
			get
			{
				return m_InitForService;
			}
		}
		private IniData m_SysIniData;
		private IniData SysIniData
		{
			get
			{
				if (m_SysIniData == null)
				{
					try
					{
						m_SysIniData = IniFile.ReadFile(GetSysINIFileName(true));
					}
					catch (WK.DE.Data.IniFileParser.Exceptions.ParsingException exp)
					{
						throw new DatabaseConfigurationNotAvailableException(exp);
					}
				}
				return m_SysIniData;
			}
		}

		/// <summary>
		/// Getter-Variable für ATSystemConfiguration.
		/// </summary>
		private ATSystemConfiguration m_ATSystemConfiguration;
		/// <summary>
		/// Ruft die Konfiguration für die AT-System ab.
		/// </summary>
		public ATSystemConfiguration ATSystemConfiguration
		{
			get
			{
				if (m_ATSystemConfiguration == null)
					LoadOfficeConfiguration();
				return m_ATSystemConfiguration;
			}
		}

		/// <summary>
		/// Getter-Variable für InstalledOffices.
		/// </summary>
		private IReadOnlyCollection<OfficeConfiguration> m_InstalledOffices;
		/// <summary>
		/// Ruft ab.
		/// </summary>
		public IReadOnlyCollection<OfficeConfiguration> InstalledOffices
		{
			get
			{
				if (m_InstalledOffices == null)
					LoadOfficeConfiguration();
				return m_InstalledOffices;
			}
		}

		/// <summary>
		/// Getter-Variable für GER Path.
		/// </summary>
		private string m_GerPath;
		/// <summary>
		/// Ruft das GER/HTML Verzeichniss ab
		/// </summary>
		public string GerHTMLPath
		{
			get
			{
				if (m_GerPath == null)
				{
					m_GerPath = Path.Combine(ServerRootDirectory, "GER"); ;
				}
				return Path.Combine(m_GerPath, "HTML");
			}
		}

		/// <summary>
		/// Getter-Variable für MappingRootDirectory.
		/// </summary>
		private string m_MappingRootDirectory;
		/// <summary>
		/// Ruft das Mapping-Root-Verzeichnis ab.
		/// Als Service ist das Serverroot und Mappingroot identisch, da es das Mappingroot nicht existieren muss
		/// </summary>
		public string MappingRootDirectory
		{
			get
			{
				if (m_MappingRootDirectory == null)
				{
					if (m_InitForService)
					{
						m_MappingRootDirectory = ServerRootDirectory;
					}
					else
					{
						using (var optionsKey = RegistryHelper.OpenSubKey(RegistryHelper.KnownRegistryKeys.BasicConfigurationOptionsRoot))
						{
							if (optionsKey == null)
								throw new BasicSystemConfigurationException(String.Format("Der Hauptschlüssel für die AnNoText-Konfiguration ({0}) konnte nicht geöffnet werden.", RegistryHelper.GetRootKeyName(RegistryHelper.KnownRegistryKeys.BasicConfigurationOptionsRoot)));

							var mappingRoot = optionsKey.GetValue("MappingRoot", "") as string;
							if (String.IsNullOrWhiteSpace(mappingRoot))
								throw new BasicSystemConfigurationException(String.Format("Der Wert \"MappingRoot\" konnte im Hauptschlüssel der AnNoText-Konfiguration ({0}) nicht gefunden werden oder enthält keinen Wert.", RegistryHelper.GetRootKeyName(RegistryHelper.KnownRegistryKeys.BasicConfigurationOptionsRoot)));

							if (!mappingRoot.EndsWith("\\"))
								mappingRoot += "\\";

							m_MappingRootDirectory = mappingRoot;
						}
					}
				}

				return m_MappingRootDirectory;
			}
		}

		/// <summary>
		/// Getter-Variable für ServerRootDirectory.
		/// </summary>
		private string m_ServerRootDirectory;
		/// <summary>
		/// Ruft das Server-Root-Verzeichnis ab.
		/// </summary>
		public string ServerRootDirectory
		{
			get
			{
				if (m_ServerRootDirectory == null)
				{
					var knownRegistryKey = m_InitForService ? RegistryHelper.KnownRegistryKeys.BasicConfigurationServiceRoot : RegistryHelper.KnownRegistryKeys.BasicConfigurationOptionsRoot;
					using (var optionsKey = RegistryHelper.OpenSubKey(knownRegistryKey))
					{
						if (optionsKey == null)
							throw new BasicSystemConfigurationException(String.Format("Der Hauptschlüssel für die AnNoText-Konfiguration ({0}) konnte nicht geöffnet werden.", RegistryHelper.GetRootKeyName(knownRegistryKey)));

						string serverRoot = optionsKey.GetValue("ServerRoot", "") as string;
						if (String.IsNullOrWhiteSpace(serverRoot))
							throw new BasicSystemConfigurationException(String.Format("Der Wert \"ServerRoot\" konnte im Hauptschlüssel der AnNoText-Konfiguration ({0}) nicht gefunden werden oder enthält keinen Wert.", RegistryHelper.GetRootKeyName(knownRegistryKey)));

						if (!serverRoot.EndsWith("\\"))
							serverRoot += "\\";

						m_ServerRootDirectory = serverRoot;
					}
				}

				return m_ServerRootDirectory;
			}
		}

		private bool? m_NoDebugInformation;
		/// <summary>
		/// Ruft ab, ob die Übermittlung von Fehlerberichten mittels Bugsplat vollständig deaktiviert werden soll.
		/// </summary>
		public bool NoDebugInformation
		{
			get
			{
				if (!m_NoDebugInformation.HasValue)
				{
					var iniValue = SysIniData["System"]["NoDebugInformation"];
					if (!String.IsNullOrWhiteSpace(iniValue) && String.Equals(iniValue, "1", StringComparison.OrdinalIgnoreCase))
					{
						m_NoDebugInformation = true;
					}
					else
					{
						m_NoDebugInformation = false;
					}
				}
				return m_NoDebugInformation.Value;
			}
		}

		private Tuple<string, string, bool> m_FaxInformation;
		/// <summary>
		/// Ruft die Information ab, wie eine FaxNummer an Outlook übertragen wird.
		/// </summary>
		/// <remarks>
		/// 
		/// </remarks>
		public Tuple<string, string, bool> GetFaxInformation()
		{
			if (m_FaxInformation == null)
			{
				var faxNumberFormat = SysIniData["System"]["OutlookFaxSelect"];
				var faxSubject = SysIniData["System"]["OutlookFaxSubject"];
				var faxConvertToPdf = SysIniData["System"]["OutlookFaxConvertToPDF"];

				m_FaxInformation = new Tuple<string, string, bool>(faxNumberFormat, faxSubject, String.Equals(faxConvertToPdf, "1", StringComparison.OrdinalIgnoreCase));
			}
			return m_FaxInformation;
		}

		private int? m_EMailWatcherWaitTimeBetweenMailsInMS;
		public int EMailWatcherWaitTimeBetweenMailsInMS
		{
			get
			{
				GetIntValueFromSysINIData(ref m_EMailWatcherWaitTimeBetweenMailsInMS, "EMailWatcher", "WaitTimeBetweenMailsInMS");
				return m_EMailWatcherWaitTimeBetweenMailsInMS.Value;
			}
		}

		private int? m_EMailWatcherWaitTimeAfterStartOfApplicationInMinutes;

		public int EMailWatcherWaitTimeAfterStartOfApplicationInMinutes
		{
			get
			{
				GetIntValueFromSysINIData(ref m_EMailWatcherWaitTimeAfterStartOfApplicationInMinutes, "EMailWatcher", "WaitTimeAfterStartOfApplicationInMinutes");
				return m_EMailWatcherWaitTimeAfterStartOfApplicationInMinutes.Value;
			}
		}

		private int? m_EMailWatcherDaysToTakeIntoAccount;

		public int EMailWatcherDaysToTakeIntoAccount
		{
			get
			{
				GetIntValueFromSysINIData(ref m_EMailWatcherDaysToTakeIntoAccount, "EMailWatcher", "DaysToTakeIntoAccount");
				return m_EMailWatcherDaysToTakeIntoAccount.Value;
			}
		}

		/// <summary>
		/// Initialisiert eine neue Instanz der BasicSystemConfiguration-Klasse.
		/// </summary>
		/// <param name="initForService">Gibt an, ob die Konfiguration für einen Dienst geladen werden soll.</param>
		public BasicSystemConfiguration(bool initForService)
		{
			m_InitForService = initForService;
		}

		private void GetIntValueFromSysINIData(ref int? instanceIntVariable, string sysIniSection, string sysIniValue)
		{
			if (instanceIntVariable.HasValue)
			{
				return;
			}

			var iniSection = SysIniData[sysIniSection];
			if (iniSection == null)
			{
				instanceIntVariable = 0;
				return;
			}

			var iniValue = iniSection[sysIniValue];
			if (iniValue == null)
			{
				instanceIntVariable = 0;
				return;
			}
			int temp;
			if (!Int32.TryParse(iniValue, out temp))
			{
				instanceIntVariable = 0;
				return;
			}

			instanceIntVariable = temp;
		}

		/// <summary>
		/// Lädt die Konfiguration der Bürogemeinschaften.
		/// </summary>
		private void LoadOfficeConfiguration()
		{
			var result = GetInstalledOffices();
			m_ATSystemConfiguration = result.Item1;
			m_InstalledOffices = result.Item2.AsReadOnly();
		}

		/// <summary>
		/// Ermittelt die installierten Bürogemeinschaften.
		/// </summary>
		private Tuple<ATSystemConfiguration, List<OfficeConfiguration>> GetInstalledOffices()
		{
			var installedOffices = new List<OfficeConfiguration>();

			var sysIniData = SysIniData;
			string connectionSecurityTypeStr = sysIniData["MSSQLSERVER"]["CONNECTION"];
			var connectionSecurityType = ConnectionSecurityTypes.Unknown;
			if (String.Equals(connectionSecurityTypeStr, "Standard_Security", StringComparison.OrdinalIgnoreCase))
			{
				connectionSecurityType = ConnectionSecurityTypes.StandardSecurity;
			}
			else if (String.Equals(connectionSecurityTypeStr, "Trusted_Connection", StringComparison.OrdinalIgnoreCase))
			{
				connectionSecurityType = ConnectionSecurityTypes.TrustedConnection;
			}

			var serverName = sysIniData["MSSQLSERVER"]["Server"] ?? "";
			var userId = sysIniData["MSSQLSERVER"]["Uid"] ?? "";
			var passwordEncrypted = sysIniData["MSSQLSERVER"]["Pwd"] ?? "";
			var password = DecryptSQLServerPasswort(passwordEncrypted);
			var useEncryptedSQLConnection = GetBooleanValue(sysIniData["MSSQLSERVER"]["UseEncryptedSQLConnection"]);
			var trustServerCertificate = GetBooleanValue(sysIniData["MSSQLSERVER"]["UseTrustServerCertificate"]);
			var atsystemDatabaseName = sysIniData["MSSQLSERVER"]["ATSYSTEMDATABASENAME"] ?? "ATSYSTEM";

			var atSystemConfiguration = new ATSystemConfiguration(serverName, atsystemDatabaseName, userId, password, connectionSecurityType, false, useEncryptedSQLConnection, trustServerCertificate);

			foreach (var installedOfficeName in GetInstalledOfficeNames())
			{
				var section = sysIniData[installedOfficeName];
				if (!section.Any())
				{
					section = sysIniData[installedOfficeName + "]"];
					if (section.Any())
					{
						m_Logger.WarnFormat("Fehlerhafter Sektionskopf in SYS.INI: {0}", installedOfficeName);
					}
					else
					{
						m_Logger.Error(String.Format("Sektion für Bürogemeinschaft \"{0}\" nicht gefunden", installedOfficeName));
					}
				}

				var databaseName = section["DATABASE"] ?? "";

				string remoteDatabaseStr = section["REMOTE"];
				bool remoteDatabase = !String.IsNullOrWhiteSpace(remoteDatabaseStr) && String.Compare(remoteDatabaseStr, "1", false) == 0;

				string officeServerName = serverName;
				string officeUserId = userId;
				string officePassword = password;
				var officeUseEncryptedSQLConnection = useEncryptedSQLConnection;
				var officeTrustServerCertificate = trustServerCertificate;
				if (remoteDatabase)
				{
					officeServerName = section["Server"] ?? "";
					officeUserId = section["Uid"] ?? "";
					var officePasswordEncrypted = section["Pwd"] ?? "";
					officePassword = DecryptSQLServerPasswort(officePasswordEncrypted);
					officeUseEncryptedSQLConnection = GetBooleanValue(section["UseEncryptedSQLConnection"], useEncryptedSQLConnection);
					officeTrustServerCertificate = GetBooleanValue(section["UseTrustServerCertificate"], trustServerCertificate);
				}

				var automaticDocumentNumbering = String.Equals(section["AUTOMATICDOCNR"], "1", StringComparison.OrdinalIgnoreCase);
				var allowSubDocuments = String.Equals(section["ALLOWSUBDOCUMENTS"], "1", StringComparison.OrdinalIgnoreCase);
				var configurationFilesDirectory = section["CONFIGFILES"];
				var templateFilesDirectory = section["TEMPLATES"];
				var automaticDeleteDocumentsForView = String.Equals(section["AUTOMATICDELETEDOCUMENTSFORVIEW"], "1", StringComparison.OrdinalIgnoreCase);
				var notaryDocuments = section["NOTARYDOCUMENTS"];
				var notaryFiles = section["NOTARYFILES"];

				var installedOffice = new OfficeConfiguration(installedOfficeName, officeServerName, databaseName, officeUserId, officePassword, connectionSecurityType, remoteDatabase, automaticDocumentNumbering, allowSubDocuments, officeUseEncryptedSQLConnection, officeTrustServerCertificate, configurationFilesDirectory, templateFilesDirectory, automaticDeleteDocumentsForView, notaryFiles, notaryDocuments);
				installedOffices.Add(installedOffice);
			}

			return new Tuple<ATSystemConfiguration, List<OfficeConfiguration>>(atSystemConfiguration, installedOffices);
		}

		private static bool GetBooleanValue(string iniValue)
		{
			return GetBooleanValue(iniValue, false);
		}

		private static bool GetBooleanValue(string iniValue, bool defaultValue)
		{
			if (String.IsNullOrWhiteSpace(iniValue))
			{
				return defaultValue;
			}
			return String.Equals(iniValue, "1", StringComparison.OrdinalIgnoreCase);
		}

		private string DecryptSQLServerPasswort(string passwordEncrypted)
		{
			string password;
			var crypto = new AnNoTextCryptoWithDotNet();
			var result = crypto.Decrypt(passwordEncrypted, "SQLServerPasswort");
			if (result.Trim().Length == 0)
				password = passwordEncrypted;
			else
				password = result;

			return password;
		}

		/// <summary>
		/// Ruft die Namen der installierten und vom Update zu aktualisierenden Bürogemeinschaften ab.
		/// </summary>
		private IList<string> GetInstalledOfficeNames()
		{
			var sysIniData = SysIniData;

			var installedOffices = sysIniData["INSTALLEDOFFICE"];
			if (!installedOffices.Any())
				return null;

			var lastOfficeNumber = sysIniData["Parameters"]["LastOfficeNumber"];
			int lastOfficeNumberInt;
			if (String.IsNullOrEmpty(lastOfficeNumber) || !Int32.TryParse(lastOfficeNumber, out lastOfficeNumberInt))
				lastOfficeNumberInt = -1;

			var result = new List<string>();
			foreach (var installedOffice in installedOffices)
			{
				var officeNumber = installedOffice.KeyName;
				if (String.IsNullOrEmpty(officeNumber))
					continue;
				if (officeNumber.StartsWith("#"))
					continue;
				int officeNumberInt;
				if (!Int32.TryParse(officeNumber, out officeNumberInt))
					continue;
				if (lastOfficeNumberInt != -1 && lastOfficeNumberInt < officeNumberInt)
					continue;

				result.Add(installedOffice.Value);
			}
			return result;
		}

		public string GetSystemDirectory(SystemDirectory systemDirectory, bool expandMappings)
		{
			string sectionName;
			string keyName;
			switch (systemDirectory)
			{
				case SystemDirectory.SystemData:
					sectionName = "SYSTEM_DB";
					keyName = "SysDB";
					break;

				case SystemDirectory.PostboxData:
					sectionName = "SYSTEM_DB";
					keyName = "PoBoxDB";
					break;

				case SystemDirectory.DataRoot:
					sectionName = "PARAMETERS";
					keyName = "DATAROOT";
					break;

				default:
					throw new ArgumentException(systemDirectory.ToString(), "systemDirectory");
			}

			var directory = SysIniData[sectionName][keyName] ?? "";

			if (expandMappings)
			{
				var startPosition = directory.IndexOf(@":\");
				if (startPosition > 0)
				{
					var laufwerk = directory.Substring(0, startPosition + 2);
					var mappingPath = SysIniData["MAPPINGS"][laufwerk];
					if (!String.IsNullOrEmpty(mappingPath))
					{
						directory = Path.Combine(mappingPath, directory.Substring(startPosition + 2));
					}
				}
			}

			return directory;
		}

		/// <summary>
		/// Ruft den vollständigen Pfad zur SYS.INI ab.
		/// </summary>
		/// <param name="checkIfFileExists">Gibt an, ob geprüft werden soll, ob die Datei vorhanden ist.</param>
		public string GetSysINIFileName(bool checkIfFileExists)
		{
			var result = Path.Combine(ServerRootDirectory, "sys\\sys.ini");

			if (checkIfFileExists && !File.Exists(result))
				throw new FileNotFoundException(result);

			return result;
		}

		/// <summary>
		/// Ruft den vollständigen Pfad zu STAMM.INI ab.
		/// </summary>
		/// <returns></returns>
		public string GetStammINIFileName()
		{
			string mappingRoot = this.MappingRootDirectory;
			string iniFileName = Path.Combine(mappingRoot, "bin32", "stamm.ini");
			return iniFileName;
		}

		public string GetSettingFromSysIni(string sectionName, string keyName, string defaultValue)
		{
			var result = defaultValue ?? "";
			var section = SysIniData[sectionName];
			if (section != null)
			{
				var key = section[keyName];
				if (!String.IsNullOrWhiteSpace(key))
				{
					result = key;
				}
			}
			return result;
		}
	}
}
