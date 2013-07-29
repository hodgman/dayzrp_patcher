using System;
using System.Threading;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Xml;

namespace util
{
	public static class StreamExtension
	{
		public static T Deserialize<T>(this Stream stream)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			return (T)formatter.Deserialize(stream);
		}
		public static void Serialize<T>(this Stream stream, T obj)
		{
			BinaryFormatter formatter = new BinaryFormatter();
			formatter.Serialize(stream, obj);
		}
	}
	public static class StringExtension
	{
		public static string ToHexString(byte[] ba)
		{
			StringBuilder hex = new StringBuilder(ba.Length * 2);
			foreach (byte b in ba)
				hex.AppendFormat("{0:x2}", b);
			return hex.ToString();
		}
		public static byte[] HexToBytes(this String hex)
		{
			int numBytes = hex.Length / 2;
			byte[] bytes = new byte[numBytes];
			for (int i = 0; i < numBytes * 2; i += 2)
				bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			return bytes;
		}
	}
	public static class XmlNodeExtension
	{
		public static string Attribute(this XmlNode node, string attribute, string defaultValue)
		{
			XmlAttribute xmlAttribute = node.Attributes[attribute];
			return xmlAttribute != null ? xmlAttribute.InnerText : defaultValue;
		}
	}
}