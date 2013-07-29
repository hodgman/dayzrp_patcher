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
	public class PatchManifest
	{
		public string version;
		public class AssetInfo
		{
			public Uri uri;
			public byte[] hash;
		}
		public Dictionary<string, AssetInfo> assets = new Dictionary<string, AssetInfo>();
		public AssetInfo GetFileInfo(string name)
		{
			AssetInfo asset;
			assets.TryGetValue(name.ToLowerInvariant(), out asset);
			return asset;
		}
		public static PatchManifest Parse(XmlDocument doc, string baseUrl)
		{
			PatchManifest patchInfo = new PatchManifest();
			XmlNode xmlPatch = doc.SelectSingleNode("patch");
			patchInfo.version = xmlPatch.Attribute("version", "");
			XmlNodeList xmlFiles = xmlPatch.SelectNodes("file");
			Uri baseUri = new Uri(baseUrl);
			foreach (XmlNode xmlFile in xmlFiles)
			{
				string path = xmlFile.Attribute("path", "");
				string hash = xmlFile.Attribute("hash", "");
				string defaultUrl = Path.Combine(baseUrl, patchInfo.version + "/" + path);
				string url = xmlFile.Attribute("url", defaultUrl);
				if (String.IsNullOrEmpty(path) || String.IsNullOrEmpty(hash) || String.IsNullOrEmpty(url))
					return null;
				Uri uri;
				if (!Uri.TryCreate(baseUri, url, out uri))
					return null;
				PatchManifest.AssetInfo asset = new PatchManifest.AssetInfo
				{
					uri = uri,
					hash = hash.HexToBytes()
				};
				patchInfo.assets.Add(path.ToLowerInvariant(), asset);
			}
			return patchInfo;
		}
	}
}