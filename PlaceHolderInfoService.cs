using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WK.DE.OXmlMerge
{
	public class PlaceHolderInfoService
	{
		public static List<PlaceHolderInfo> GetPlaceHolders(TableRow tableRow)
		{
			return tableRow.ChildElements.OfType<TableCell>().SelectMany(x => GetPlaceHolders(x)).ToList();
		}

		public static List<PlaceHolderInfo> GetPlaceHolders(TableCell tableCell)
		{
			return tableCell.ChildElements.OfType<Paragraph>().SelectMany(x => GetPlaceHolders(x)).ToList();
		}

		public static List<PlaceHolderInfo> GetPlaceHolders(Paragraph paragraph)
		{
			var result = new List<PlaceHolderInfo>();

			Text startElement = null;
			int startIndexInStartElement = -1;
			var contentElements = new List<Text>();
			foreach (var run in paragraph.ChildElements.OfType<Run>())
			{
				var textElements = run.ChildElements.OfType<Text>().ToList();

				foreach (var textElement in textElements)
				{
					int startIndex = textElement.Text.IndexOf("[");
					do
					{
						if (startIndex >= 0)
						{
							if (startElement != null)
							{
								var endIndex = textElement.Text.IndexOf("]");
								if (endIndex >= 0)
								{
									if (textElement.Text.IndexOf(" ", 0, endIndex) >= 0)
									{
										startElement = null;
										contentElements = new List<Text>();
									}
									else
									{
										result.Add(new PlaceHolderInfo(startElement, startIndexInStartElement, contentElements, textElement, endIndex));
										startElement = null;
										contentElements = new List<Text>();
									}
								}
							}
							else
							{
								startElement = textElement;
								startIndexInStartElement = startIndex;

								//Falle, dass [ und ] im gleichen Text-Element liegen
								var endIndex = textElement.Text.IndexOf("]", startIndex);
								if (endIndex >= 0)
								{
									result.Add(new PlaceHolderInfo(startElement, startIndexInStartElement, contentElements, textElement, endIndex));
									startElement = null;
									contentElements = new List<Text>();

									startIndex = endIndex;
								}
								else
									break;
							}
						}
						else if (startElement != null)
						{
							var endIndex = textElement.Text.IndexOf("]");
							if (endIndex >= 0)
							{
								if (textElement.Text.IndexOf(" ", 0, endIndex) >= 0)
								{
									startElement = null;
									contentElements = new List<Text>();
								}
								else
								{
									result.Add(new PlaceHolderInfo(startElement, startIndexInStartElement, contentElements, textElement, endIndex));
									startElement = null;
									contentElements = new List<Text>();
								}
							}
							else
								contentElements.Add(textElement);
							break; //break while
						}
						else
							break;
					}
					while ((startIndex = textElement.Text.IndexOf("[", startIndex)) >= 0);
				}
			}

			return result;
		}

		public static bool GetPlaceHolder(Text textElement, out int start, out int end)
		{
			start = end = -1;

			var text = textElement.Text;
			var startBrace = text.IndexOf("[");
			if (startBrace >= 0)
			{
				int endBrace = text.IndexOf("]", startBrace);
				if (endBrace > startBrace)
				{
					start = startBrace;
					end = endBrace;
					return true;
				}
			}
			return false;
		}

		public static string GetPlaceHolderName(PlaceHolderInfo placeHolderInfo)
		{
			if (placeHolderInfo.StartElement == placeHolderInfo.EndElement)
			{
				return placeHolderInfo.StartElement.Text.Substring(placeHolderInfo.StartIndexInStartElement + 1, placeHolderInfo.EndIndexInEndElement - placeHolderInfo.StartIndexInStartElement - 1);
			}
			else
			{
				var result = placeHolderInfo.StartElement.Text.Substring(placeHolderInfo.StartIndexInStartElement + 1);
				result += String.Join("", placeHolderInfo.ContentElements.Select(x => x.Text));
				result += placeHolderInfo.EndElement.Text.Substring(0, placeHolderInfo.EndIndexInEndElement);
				return result;
			}
		}
	}
}
