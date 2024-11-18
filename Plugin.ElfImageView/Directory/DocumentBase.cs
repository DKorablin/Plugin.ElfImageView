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
	/// <summary>Базовый класс отображения пользовательского интерфейса</summary>
	public abstract partial class DocumentBase : UserControl, IPluginSettings<DocumentBaseSettings>
	{
		private readonly ElfItemType _peType;
		private DocumentBaseSettings _settings;

		protected PluginWindows Plugin => (PluginWindows)this.Window.Plugin;

		protected IWindow Window => (IWindow)base.Parent;

		/// <summary>Путь к открытому файлу в текущем документе</summary>
		internal String FilePath => this.Settings.FilePath;

		Object IPluginSettings.Settings => this.Settings;

		public virtual DocumentBaseSettings Settings
			=> this._settings ?? (this._settings = new DocumentBaseSettings());

		protected virtual void SetCaption()
			=> this.Window.Caption = String.Join(" - ", new String[] { Path.GetFileName(this.Settings.FilePath), Constant.GetHeaderName(this._peType), });

		public DocumentBase(ElfItemType type)
		{
			this._peType = type;
			InitializeComponent();
		}

		protected override void OnCreateControl()
		{
			this.Window.Shown += Window_Shown;
			this.Window.Closed += Window_Closed;
			this.Plugin.Settings.PropertyChanged += Settings_PropertyChanged;
			this.Plugin.Binaries.PeListChanged += Plugin_PeListChanged;
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
			this.Plugin.Settings.PropertyChanged -= Settings_PropertyChanged;
			this.Plugin.Binaries.PeListChanged -= Plugin_PeListChanged;
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

		/// <summary>Получить путь к файлу</summary>
		/// <param name="fileName">Наименование файла к кторому необходимо получить путь</param>
		/// <returns>Физический путь к файлу или null</returns>
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

		/// <summary>Open file if it's opened from the window for examle through Drag'n'Drop</summary>
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
				this.Plugin.Binaries.OpenFile(this.FilePath);//Файл открыт. Необходимо обновить список открытых файлов (При необходимости)

				this.SetCaption();
				this.ShowFile(info);
			}
		}

		/// <summary>Получить информацию о открытом файле</summary>
		/// <returns>Корневая директория описателя PE файла</returns>
		protected ElfFile GetFile()
			=> this.Plugin.Binaries.LoadFile(this.FilePath, false);

		/// <summary>Отобразить файл в окне</summary>
		/// <param name="info">Информация о файле</param>
		protected abstract void ShowFile(ElfFile info);
	}
}