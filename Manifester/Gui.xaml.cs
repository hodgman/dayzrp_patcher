using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;
using Forms = System.Windows.Forms;
using System.IO;
using System.Security.Cryptography;
using System.Xml;
using util;

namespace Manifester
{
	/// <summary>
	/// Interaction logic for Window1.xaml
	/// </summary>
	public partial class Window1 : Window
	{
		public Window1()
		{
			InitializeComponent();
		}

		private Forms.FolderBrowserDialog m_folderBrowser = new Forms.FolderBrowserDialog();
		private DirectoryScanner m_scanner = new DirectoryScanner();
		private MD5 m_md5 = MD5.Create();

		private void Browse_Click(object sender, RoutedEventArgs e)
		{
			m_folderBrowser.SelectedPath = projectPathBox.Text;
			m_folderBrowser.Description = "Select the project path";
			Forms.DialogResult result = m_folderBrowser.ShowDialog();
			if (result == Forms.DialogResult.OK)
			{
				projectPathBox.Text = m_folderBrowser.SelectedPath;
			}
		}

		private void button1_Click(object sender, RoutedEventArgs e)
		{
			DirectoryScanner.ScanCompleteCallback callback =
					(x, y) => Dispatcher.BeginInvoke(new DirectoryScanner.ScanCompleteCallback(DirectoryScanComplete), DispatcherPriority.Normal, new object[] { x, y });
			m_scanner.BeginScan(projectPathBox.Text, callback);
		}
		private void DirectoryScanComplete(List<FileInfo> results, bool error)
		{
			if (error)
			{
				MessageBox.Show("Something went wrong");
				return;
			}
			XmlDocument doc = new XmlDocument();
			XmlElement rootNode = doc.CreateElement("patch");
			XmlAttribute version = doc.CreateAttribute("version");
			version.Value = versionBox.Text;
			rootNode.Attributes.Append(version);
			doc.AppendChild(rootNode);
			foreach (var file in results)
			{
				XmlElement fileNode = doc.CreateElement("file");
				XmlAttribute path = doc.CreateAttribute("path");
				XmlAttribute hash = doc.CreateAttribute("hash");
				string nameValue = GetRelativeName(file);
				string hashValue;
				using (Stream stream = file.OpenRead())
				{
					hashValue = StringExtension.ToHexString(m_md5.ComputeHash(stream));
					stream.Close();
				}
				path.Value = nameValue;
				hash.Value = hashValue;
				fileNode.Attributes.Append(path);
				fileNode.Attributes.Append(hash);
				rootNode.AppendChild(fileNode);
			}
			doc.Save(outputBox.Text);
		}
		private string GetRelativeName(FileInfo file)
		{
			Uri root = new Uri(projectPathBox.Text + "/");
			Uri uri = new Uri(file.FullName);
			uri = root.MakeRelativeUri(uri);
			return uri.ToString().ToLowerInvariant();
		}
	}
}
