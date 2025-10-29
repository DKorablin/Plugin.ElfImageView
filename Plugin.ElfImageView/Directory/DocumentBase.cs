using System;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using AlphaOmega.Debug;
using Plugin.ElfImageView.Bll;
using SAL.Flatbed;
using SAL.Windows;

namespace Plugin.ElfImageView.Directory
{
	/// <summary>Base class for displaying the user interface</summary>
	public abstract partial class DocumentBase : UserControl, IPluginSettings<DocumentBaseSettings>
	{
		private readonly ElfItemType _peType;
		private DocumentBaseSettings _settings;

		protected PluginWindows Plugin => (PluginWindows)this.Window.Plugin;

		protected IWindow Window => (IWindow)base.Parent;

		/// <summary>Path to the open file in the current document</summary>
		internal String FilePath => this.Settings.FilePath;

		Object IPluginSettings.Settings => this.Settings;

		public virtual DocumentBaseSettings Settings
			=> this._settings ?? (this._settings = new DocumentBaseSettings());

		protected virtual void SetCaption()
			=> this.Window.Caption = String.Join(" - ", new String[] { Path.GetFileName(this.Settings.FilePath), Constant.GetHeaderName(this._peType), });

		protected DocumentBase(ElfItemType type)
		{
			this._peType = type;
			this.InitializeComponent();
		}

		protected override void OnCreateControl()
		{
			this.Window.Shown += this.Window_Shown;
			this.Window.Closed += this.Window_Closed;
			this.Plugin.Settings.PropertyChanged += this.Settings_PropertyChanged;
			this.Plugin.Binaries.PeListChanged += this.Plugin_PeListChanged;
			base.OnCreateControl();
			this.DataBind();
		}

		private void Window_Shown(Object sender, EventArgs e)
		{
			var info = this.GetFile();
			if(info == null)
			{
				this.Plugin.Trace.TraceInformation("File {0} not found", this.FilePath);
				this.Window.Close();
			}
		}

		private void Window_Closed(Object sender, EventArgs e)
		{
			this.Plugin.Settings.PropertyChanged -= this.Settings_PropertyChanged;
			this.Plugin.Binaries.PeListChanged -= this.Plugin_PeListChanged;
		}

		private void Plugin_PeListChanged(Object sender, PeListChangedEventArgs e)
		{
			if(base.InvokeRequired)
				base.Invoke((MethodInvoker)delegate { this.Plugin_PeListChanged(sender, e); });
			else
				switch(e.Type)
				{
				case PeListChangeType.Changed:
					if(e.FilePath == this.FilePath)
					{
						ElfFile info = this.GetFile();
						this.ShowFile(info);
					}
					break;
				}
		}

		private void Settings_PropertyChanged(Object sender, PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
			case nameof(PluginSettings.MaxArrayDisplay):
			case nameof(PluginSettings.ShowAsHexValue):
				var info = this.GetFile();
				this.ShowFile(info);
				break;
			}
		}

		/// <summary>Get the path to the file</summary>
		/// <param name="fileName">Name of the file to which you want to get the path</param>
		/// <returns>Physical path to the file or null</returns>
		protected String GetFilePath(String fileName)
		{
			String directoryName = Path.GetDirectoryName(this.FilePath);
			String systemDirectory = Environment.GetFolderPath(Environment.SpecialFolder.System);

			String path = null;
			String sysPath = Path.Combine(systemDirectory, fileName);
			String localPath = Path.Combine(directoryName, fileName);

			if(File.Exists(sysPath))
				path = sysPath;
			else if(File.Exists(localPath))
				path = localPath;

			if(path == null)
				this.Plugin.Trace.TraceInformation("Module {1} not found at:{0}\tSystem: {2}{0}\tLocal: {3}", Environment.NewLine, fileName, sysPath, localPath);
			return path;
		}

		/// <summary>Open file if it's opened from the window for example through Drag'n'Drop</summary>
		/// <param name="filePath">File path to open</param>
		protected void OpenFile(String filePath)
		{
			if(this.FilePath == null || !this.FilePath.Equals(filePath, StringComparison.OrdinalIgnoreCase))
			{
				this.Plugin.Binaries.OpenFile(filePath);
				var directory = this.Plugin.Binaries.LoadFile(filePath);
				this.Settings.FilePath = filePath;

				this.SetCaption();
				this.ShowFile(directory);
			}
		}

		private void DataBind()
		{
			var info = this.GetFile();
			if(info != null)
			{
				this.Plugin.Binaries.OpenFile(this.FilePath);//The file is open. The list of open files needs to be refreshed (if necessary).

				this.SetCaption();
				this.ShowFile(info);
			}
		}

		/// <summary>Get information about an open file</summary>
		/// <returns>The root directory of the PE file handle</returns>
		protected ElfFile GetFile()
		=> this.Plugin.Binaries.LoadFile(this.FilePath, false);

		/// <summary>Display the file in a window</summary>
		/// <param name="info">File information</param>
		protected abstract void ShowFile(ElfFile info);
	}
}