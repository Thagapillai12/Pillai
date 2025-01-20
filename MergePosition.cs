using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Wordprocessing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WK.DE.OXmlMerge
{
	public class MergePosition
	{
		/// <summary>
		/// Get-/Setter-Variable für ParagraphOrTable.
		/// </summary>
		private OpenXmlCompositeElement m_ParagraphOrTable;
		/// <summary>
		/// Ruft ab oder legt fest.
		/// </summary>
		public OpenXmlCompositeElement ParagraphOrTable
		{
			get { return m_ParagraphOrTable; }
			set
			{
				m_ParagraphOrTable = value;
				if (m_ParagraphOrTable != null && m_ParagraphOrTable.Parent == null)
					throw new InvalidOperationException("Paragraph oder Tabelle ohne Parent übergeben, Mischen wird in der Folge nicht möglich sein");
			}
		}

		public OpenXmlElement InsertAfter
		{ get; set; }
	}
}
