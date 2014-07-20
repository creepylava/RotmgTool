using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace RotmgTool
{
	internal class XmlData
	{
		public string Version { get; private set; }

		public XmlData(XDocument doc)
		{
			Document = doc;
		}

		private XDocument doc;

		public XDocument Document
		{
			get { return doc; }
			set
			{
				doc = value;
				Initialize();
			}
		}

		private Dictionary<ushort, XElement> elements;

		public XElement this[ushort objType]
		{
			get { return elements[objType]; }
		}

		public IList<ushort> PlayerTypes { get; private set; }

		private static ushort ParseObjType(string objType)
		{
			if (objType.StartsWith("0x"))
				return ushort.Parse(objType.Substring(2), NumberStyles.HexNumber);
			return ushort.Parse(objType);
		}

		private void Initialize()
		{
			Version = Document.Root.Attribute("version").Value;
			elements = new Dictionary<ushort, XElement>();
			foreach (var elem in doc.Root.Elements("Object"))
			{
				string typeString = elem.Attribute("type").Value;
				elements[ParseObjType(typeString)] = elem;
			}

			PlayerTypes = elements
				.Where(elem =>
				{
					var cls = elem.Value.Element("Class");
					return cls != null && cls.Value == "Player";
				})
				.Select(kvp => kvp.Key)
				.ToList();
		}
	}
}