using log4net;
using Microsoft.Win32;
using System;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;

namespace WK.DE.DocumentManagement.Helper
{
	public static class OutlookFontHelper
	{
		private static ILog m_Logger = LogManager.GetLogger(typeof(OutlookFontHelper));

		public static Font GetDefaultHtmlMailFont()
		{
			var fontDefinition = GetDefaultHtmlMailFontDefinitionFromRegistry();
			return GetFontFromFontDefinition(fontDefinition);
		}

		public static string GetDefaultHtmlMailFontDefinitionFromRegistry()
		{
			var outlookVersion = GetOutlookVersionFromRegistry();
			if (outlookVersion == null)
			{
				m_Logger.Debug("no outlook version determined");
				return null;
			}

			m_Logger.DebugFormat("outlook version is: {0}", outlookVersion);

			var keyName = $"Software\\Microsoft\\Office\\{outlookVersion}.0\\Common\\MailSettings";

			using (var keyCurrentUser = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64))
			using (var keyMailSettings = keyCurrentUser.OpenSubKey(keyName))
			{
				if (keyMailSettings == null)
				{
					m_Logger.DebugFormat("key not found: HKEY_CURRENT_USER\\{0}", keyName);
					return null;
				}
				m_Logger.DebugFormat("key was opened: HKEY_CURRENT_USER\\{0}", keyName);

				var registryValue = keyMailSettings.GetValue("ComposeFontComplex", null);
				if (registryValue == null)
				{
					m_Logger.Debug("value not found: ComposeFontComplex");
					return null;
				}
				var decodedRegistryValue = Encoding.Default.GetString((byte[])registryValue);
				m_Logger.DebugFormat("value ComposeFontComplex: {0}", registryValue);
				m_Logger.DebugFormat("decoded value ComposeFontComplex: {0}", decodedRegistryValue);
				return decodedRegistryValue;
			}
		}

		private static string GetOutlookVersionFromRegistry()
		{
			using (var keyOutlook = Registry.ClassesRoot.OpenSubKey("Outlook.Application\\CurVer"))
			{
				if (keyOutlook == null)
				{
					m_Logger.Debug("key not found: HKEY_CLASSES_ROOT\\Outlook.Application\\CurVer");
				}

				var valueVersion = keyOutlook.GetValue("", null) as string;
				m_Logger.Debug(valueVersion);
				if (String.IsNullOrWhiteSpace(valueVersion))
				{
					m_Logger.Debug("key has no value: HKEY_CLASSES_ROOT\\Outlook.Application\\CurVer");
					return null;
				}
				m_Logger.Debug("HKEY_CLASSES_ROOT\\Outlook.Application\\CurVer = " + valueVersion);

				var parts = valueVersion.Split('.');
				if (parts.Length >= 3)
				{
					return parts[2];
				}
				return null;
			}
		}

		public static Font GetFontFromFontDefinition(string fontDefinition)
		{
			if (String.IsNullOrWhiteSpace(fontDefinition))
			{
				return null;
			}

			string fontFamily, fontSizeStr, fontWeight;
			ExtractFontDetails(fontDefinition, out fontFamily, out fontSizeStr, out fontWeight);

			if (fontFamily == null)
			{
				return null;
			}

			var fontSize = 11f;
			if (fontSizeStr != null)
			{
				if (!float.TryParse(fontSizeStr, out fontSize))
				{
					fontSize = 11f;
				}
			}

			var fontStyle = FontStyle.Regular;
			if (fontWeight != null)
			{
				if (!Enum.TryParse<FontStyle>(fontWeight, true, out fontStyle))
				{
					fontStyle = FontStyle.Regular;
				}
			}

			return new Font(fontFamily, fontSize, fontStyle);
		}

		static void ExtractFontDetails(string htmlString, out string fontFamily, out string fontSize, out string fontWeight)
		{
			string fontFamilyPattern = @"font-family:(.*?);";
			string fontSizePattern = @"mso-ansi-font-size:(.*?);";
			string fontWeightPattern = @"font-weight:(.*?);";

			fontFamily = Extract(htmlString, fontFamilyPattern);
			if (fontFamily != null)
			{
				var fontFamilyParts = fontFamily.Split(',');
				fontFamily = fontFamilyParts[0].Trim();
				if (fontFamily.StartsWith("\"")
					&& fontFamily.EndsWith("\""))
				{
					fontFamily = fontFamily.Substring(1, fontFamily.Length - 2);
				}
			}
			fontSize = Extract(htmlString, fontSizePattern);
			if (fontSize != null)
			{
				if (fontSize.EndsWith("pt"))
				{
					fontSize = fontSize.Substring(0, fontSize.Length - 2);
				}
				fontSize = fontSize.Replace(".", ",");
			}
			fontWeight = Extract(htmlString, fontWeightPattern);
		}

		static string Extract(string htmlString, string pattern)
		{
			var regex = new Regex(pattern);
			var match = regex.Match(htmlString);

			if (match.Success)
			{
				return match.Groups[1].Value;
			}
			else
			{
				return null;
			}
		}
	}
}
