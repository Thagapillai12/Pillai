using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WK.DE.OXmlMerge
{
	public class AnNoTextFieldExecuter : FieldExecuterBase
	{
		private WordprocessingDocument m_WordprocessingDocument;
		private Func<string, string> m_GetPlaceHolderValueFunc;
		private Func<string, List<string[]>> m_GetTableContentFunc;

        public AnNoTextFieldExecuter()
			: base()
		{
			m_MergeParameters.InsertTabBeforeLineFeed = false;
            m_MergeParameters.HandleSpecialHashtags = false;

        }

		public AnNoTextFieldExecuter(WordprocessingDocument wordprocessingDocument, IMergePlaceHolderValueService placeHolderService)
			: this(wordprocessingDocument, placeHolderService == null ? (Func<string, string>)null : placeHolderService.GetPlaceHolderValue, placeHolderService == null ? (Func<string, List<string[]>>)null : placeHolderService.GetTableContent)
		{
			if (placeHolderService == null)
			{
				throw new ArgumentNullException("placeHolderService");
			}
		}

		public AnNoTextFieldExecuter(WordprocessingDocument wordprocessingDocument, Func<string, string> getPlaceHolderValueFunc, Func<string, List<string[]>> getTableContentFunc)
		{
			if (wordprocessingDocument == null)
			{
				throw new ArgumentNullException("wordprocessingDocument");
			}
			if (getPlaceHolderValueFunc == null)
			{
				throw new ArgumentNullException("getPlaceHolderValueFunc");
			}
			m_WordprocessingDocument = wordprocessingDocument;
			m_GetPlaceHolderValueFunc = getPlaceHolderValueFunc;
			m_GetTableContentFunc = getTableContentFunc;
		}

		public void ExecuteAllFields()
		{
			var mainDocumentPart = m_WordprocessingDocument.MainDocumentPart;
			var bodyElements = mainDocumentPart.Document.Body.ChildElements.OfType<OpenXmlCompositeElement>().ToList();
			ReplacePlaceHolders(bodyElements, m_GetPlaceHolderValueFunc, m_GetTableContentFunc);
		}

		public void ReplacePlaceHolders(IEnumerable<OpenXmlCompositeElement> paragraphsOrTables, Func<string, string> getPlaceHolderValueFunc, Func<string, List<string[]>> getTableContentFunc)
		{
			foreach (var paragraphOrTable in paragraphsOrTables)
			{
				var paragraph = paragraphOrTable as Paragraph;
				if (paragraph != null)
				{
					ReplacePlaceHolders(paragraph, getPlaceHolderValueFunc);
				}
				else
				{
					var table = paragraphOrTable as Table;
					if (table != null)
					{
						if (!FillTableWithContentRows(table, getTableContentFunc))
						{
							ReplacePlaceHolders(table, getPlaceHolderValueFunc);
						}
					}
				}
			}
		}

		private bool FillTableWithContentRows(Table table, Func<string, List<string[]>> getTableContentFunc)
		{
			foreach (var tableRow in table.ChildElements.OfType<TableRow>().ToList()) //ToList, da weiter unten die Zeilen ggf. entfernt und Kopien hinzugefügt werden
			{
				var placeHoldersInRow = PlaceHolderInfoService.GetPlaceHolders(tableRow);
				if (placeHoldersInRow.Count == 1)
				{
					var placeHolderName = PlaceHolderInfoService.GetPlaceHolderName(placeHoldersInRow.First());
					if (placeHolderName.StartsWith("TABELLE_"))
					{
						var tableContent = getTableContentFunc(placeHolderName);
						if (tableContent != null)
						{
							TableRow tableRowToInsertAfter = tableRow;
							foreach (var row in tableContent) 
							{
								var tableRowCopy = (TableRow)tableRow.CloneNode(true);
								FillTableCellWithContent(tableRowCopy, row);
								table.InsertAfter(tableRowCopy, tableRowToInsertAfter);
								tableRowToInsertAfter = tableRowCopy;
							}
						}
						tableRow.Remove();
						return true;
					}
				}
			}
			return false;
		}

		private void FillTableCellWithContent(TableRow tableRow, string[] row)
		{
			int index = 0;
			foreach (var tableCell in tableRow.ChildElements.OfType<TableCell>())
			{
				var paragraphs = tableCell.ChildElements.OfType<Paragraph>();
				if (paragraphs.Count() > 1)
				{
					paragraphs.Skip(1).ToList().ForEach(x => x.Remove());
				}

				var paragraph = paragraphs.FirstOrDefault();
				if (paragraph == null)
				{
					paragraph = new Paragraph();
					tableCell.AppendChild(paragraph);
				}

				var runs = paragraph.ChildElements.OfType<Run>();
				if (runs.Count() > 1)
				{
					runs.Skip(1).ToList().ForEach(x => x.Remove());
				}
				var run = runs.FirstOrDefault();
				if (run == null)
				{
					run = new Run();
					paragraph.AppendChild(run);
				}

				var text = "";
				if (row.Length > index)
				{
					text = row[index++];
				}


				if (text.StartsWith(@"{\") && text.EndsWith("}"))
				{
					//erstmal die klammern raus......
					text = text.Substring(1, text.Length - 2);
					string[] v = text.Split(' ');

					int idx = 0;

					//ggf. Fettdruck aktivieren
					if (text.Contains(@"\b"))
					{
						var runProperties = run.GetFirstChild<RunProperties>();
						if (runProperties == null)
						{
							runProperties = new RunProperties();
							run.AppendChild(runProperties);
						}
						if (runProperties.GetFirstChild<Bold>() == null)
						{
							runProperties.AppendChild(new Bold());
						}

						idx = text.IndexOf(@"\b");
						text = text.Remove(idx, 3);
					}
					//ggf. Kursivdruck aktivieren
					if (text.Contains(@"\i"))
					{
						var runProperties = run.GetFirstChild<RunProperties>();
						if (runProperties == null)
						{
							runProperties = new RunProperties();
							run.AppendChild(runProperties);
						}
						if (runProperties.GetFirstChild<Italic>() == null)
						{
							runProperties.AppendChild(new Italic());
						}

						idx = text.IndexOf(@"\i");
						text = text.Remove(idx, 3);
					}
					//ggf. Unterstrichen aktivieren
					if (text.Contains(@"\ul"))
					{
						var runProperties = run.GetFirstChild<RunProperties>();
						if (runProperties == null)
						{
							runProperties = new RunProperties();
							run.AppendChild(runProperties);
						}

						if (runProperties.GetFirstChild<Underline>() == null)
						{
							runProperties.AppendChild(new Underline());
						}

						idx = text.IndexOf(@"\ul");
						text = text.Remove(idx, 4);
					}
					//ggf. Durchgestrichen aktivieren
					if (text.Contains(@"\strike"))
					{
						var runProperties = run.GetFirstChild<RunProperties>();
						if (runProperties == null)
						{
							runProperties = new RunProperties();
							run.AppendChild(runProperties);
						}

						if (runProperties.GetFirstChild<Strike>() == null)
						{
							runProperties.AppendChild(new Strike());
						}

						idx = text.IndexOf(@"\strike");
						text = text.Remove(idx, 8);
					}
					//ggf. Farbe aktivieren
					if (text.Contains(@"\cf"))
					{
						idx = text.IndexOf(@"\cf");
						int idx2 = text.IndexOf(' ');

						string sColor = text.Substring(idx, idx2 - idx);
						sColor = sColor.Substring(3);

						DocumentFormat.OpenXml.Wordprocessing.Color color = new DocumentFormat.OpenXml.Wordprocessing.Color();
						color.Val = System.Drawing.ColorTranslator.ToHtml(System.Drawing.Color.FromArgb(AnNoText.Core.ValidColors.GetColor(Convert.ToInt32(sColor)).ToArgb()));

						var runProperties = run.GetFirstChild<RunProperties>();
						if (runProperties == null)
						{
							runProperties = new RunProperties();
							run.AppendChild(runProperties);
						}

						if (runProperties.GetFirstChild<Color>() == null)
						{
							runProperties.AppendChild(color);
						}
						
						text = text.Remove(idx, (idx2 - idx) + 1);
					}
				}

				run.RemoveAllChildren<Text>();
				run.Append(new Text(text));
			}
		}

		public void ReplacePlaceHolders(Table table, Func<string, string> getPlaceHolderValueFunc)
		{
			foreach (var tableRow in table.ChildElements.OfType<TableRow>())
			{
				ReplacePlaceHolders(tableRow, getPlaceHolderValueFunc);
			}
		}

		public void ReplacePlaceHolders(TableRow tableRow, Func<string, string> getPlaceHolderValueFunc)
		{
			tableRow.ChildElements.OfType<TableCell>().ToList().ForEach(x => ReplacePlaceHolders(x, getPlaceHolderValueFunc));
		}

		public void ReplacePlaceHolders(TableCell tableCell, Func<string, string> getPlaceHolderValueFunc)
		{
			ReplacePlaceHolders(tableCell.ChildElements.OfType<OpenXmlCompositeElement>().ToList(), getPlaceHolderValueFunc, null);
		}

		public void ReplacePlaceHolders(Paragraph paragraph, Func<string, string> getPlaceHolderValueFunc)
		{
			var placeHolders = PlaceHolderInfoService.GetPlaceHolders(paragraph);
			while (placeHolders != null && placeHolders.Count > 0)
			{
				var placeHolderName = PlaceHolderInfoService.GetPlaceHolderName(placeHolders[0]);
				ReplacePlaceHolderWithValue(placeHolders[0], getPlaceHolderValueFunc(placeHolderName));
				placeHolders = PlaceHolderInfoService.GetPlaceHolders(paragraph);
			}
		}

		public void ReplacePlaceHolderWithValue(PlaceHolderInfo placeHolder, string content)
		{
			Text startOpenBrace = null;
			var contentToRemove = new List<OpenXmlElement>();

			var startElement = placeHolder.StartElement;
			if (startElement == placeHolder.EndElement)
			{
				string textBefore = startElement.Text.Substring(0, placeHolder.StartIndexInStartElement);
				string textAfter = startElement.Text.Substring(placeHolder.EndIndexInEndElement + 1);

				if (textBefore.Length > 0)
				{
					var originalRun = (Run)startElement.Parent;
					var runCopy = (Run)originalRun.Clone();
					var textCopy = runCopy.GetFirstChild<Text>();
					textCopy.Text = textBefore;
					EnsureWhiteSpace(textCopy);
					originalRun.Parent.InsertBefore(runCopy, originalRun);
				}

				if (textAfter.Length > 0)
				{
					var originalRun = (Run)startElement.Parent;
					var runCopy = (Run)originalRun.Clone();
					var textCopy = runCopy.GetFirstChild<Text>();
					textCopy.Text = textAfter;
					EnsureWhiteSpace(textCopy);
					originalRun.Parent.InsertAfter(runCopy, originalRun);
				}

				startOpenBrace = startElement;
			}
			else
			{
				var hasContentElements = placeHolder.ContentElements.Any();

				if (placeHolder.StartIndexInStartElement == 0 && hasContentElements)
					contentToRemove.Add(startElement);
				else
				{
					startElement.Text = startElement.Text.Substring(0, placeHolder.StartIndexInStartElement);
					if (!hasContentElements)
					{
						startOpenBrace = (Text)startElement.Clone();
						startElement.Parent.InsertAfter(startOpenBrace, startElement);
					}
					EnsureWhiteSpace(startElement);
				}

				var endElement = placeHolder.EndElement;
				if (placeHolder.EndIndexInEndElement == endElement.Text.Length - 1)
					contentToRemove.Add(endElement);
				else
				{
					endElement.Text = endElement.Text.Substring(placeHolder.EndIndexInEndElement + 1);
					EnsureWhiteSpace(endElement);
				}

				if (hasContentElements)
				{
					startOpenBrace = placeHolder.ContentElements.First();
					contentToRemove.AddRange(placeHolder.ContentElements.Skip(1));
				}
			}
			contentToRemove.Add(startOpenBrace);

			var mergePosition = new MergePosition();
			mergePosition.ParagraphOrTable = (Paragraph)startOpenBrace.Parent.Parent;
			mergePosition.InsertAfter = startOpenBrace.Parent;

			var defaultRunProperties = GetDefaultRunProperties(mergePosition.InsertAfter, true);

			if (content == null)
				content = "";

            if (!m_MergeParameters.HandleSpecialHashtags)
                content = content.Replace("#", "##");
			var contentParts = content.Replace("\\", "\\\\").Replace(Environment.NewLine, "#r").Replace("\n", "#w").Replace("\t", "#e").Split('#');

            //wenn der Text nicht mit einem Steuerzeichen anfängt, dann den PlainText einfügen
            if (!content.StartsWith("$") && !content.StartsWith("#"))
				InsertRun(mergePosition, contentParts[0], defaultRunProperties);

			//sofern ein Zeilenvorschub eingefügt wird, gibt es Items, die im aktuellen Paragraphen hängen
			//  diese müssen ganz zum Schluss wieder in den letzten Paragraphen eingefügt werden
			//  mindestens gehört hierzu die schließende Klammer des Hyperlinks
			List<OpenXmlElement> savedItemsForLastParagraph = null;
			SectionProperties savedSectionPropertiesForLastParagraph = null;

            bool previousWasHashtag = false;
			foreach (var part in contentParts.Skip(1))
			{
                //der Text ist leer, wenn zwei # hintereinander eingegeben wurden --> Syntax um ein # in den Text zu bekommen (eine Art Escape-Sequenz also)
				if (part.Length == 0)
				{
					InsertRun(mergePosition, "#", defaultRunProperties);
                    previousWasHashtag = true;
                    continue;
				}

                if (previousWasHashtag)
                {
                    InsertRun(mergePosition, part, (IEnumerable<OpenXmlLeafElement>)null, defaultRunProperties);
                    previousWasHashtag = false;
                    continue;
                }

				string partOutput = part.Substring(1);

                if (part.StartsWith("e"))
                {
                    InsertRun(mergePosition, partOutput, new TabChar(), defaultRunProperties);
                }
                else if (part.StartsWith("w"))
                {
                    var additionalRunElements = new List<OpenXmlLeafElement>();
                    if (m_MergeParameters.InsertTabBeforeLineFeed)
                        additionalRunElements.Add(new TabChar());
                    additionalRunElements.Add(new Break());
                    InsertRun(mergePosition, partOutput, additionalRunElements, defaultRunProperties);
                }
                else if (part.StartsWith("r"))
                {
                    SaveItemsForLastParagraph(mergePosition, ref savedItemsForLastParagraph, ref savedSectionPropertiesForLastParagraph);

                    InsertParagraph(mergePosition, defaultRunProperties, null);
                    InsertRun(mergePosition, partOutput, defaultRunProperties);
                }
                else
                {
                    InsertRun(mergePosition, "#" + part, defaultRunProperties);
                }
            }

            var parents = contentToRemove.Select(x => x.Parent).Distinct().ToList();
			contentToRemove.ForEach(x => x.Remove());
			foreach (var parent in parents.Where(x => x.ChildElements.Count == 0 && x.Parent != null))
				parent.Remove();
		}

		private void EnsureWhiteSpace(Text text)
		{
			if (text.Text.EndsWith(" ") || text.Text.StartsWith(" "))
				text.Space = SpaceProcessingModeValues.Preserve;
			else
				text.Space = null;
		}
	}
}
