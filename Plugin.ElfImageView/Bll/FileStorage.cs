using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using AlphaOmega.Debug;
using Plugin.ElfImageView.Directory;
using Plugin.ElfImageView.Bll;
using SAL.Windows;

namespace Plugin.ElfImageView.Bll
{
	internal class FileStorage : IDisposable
	{
		private readonly Object _binLock = new Object();
		private readonly Dictionary<String, ElfFile> _binaries = new Dictionary<String, ElfFile>();
		private readonly Dictionary<String, FileSystemWatcher> _binaryWatcher = new Dictionary<String, FileSystemWatcher>();
		private readonly PluginWindows _plugin;

		public event EventHandler<PeListChangedEventArgs> PeListChanged;

		internal FileStorage(PluginWindows plugin)
		{
			this._plugin = plugin ?? throw new ArgumentNullException(nameof(plugin));
			this._plugin.Settings.PropertyChanged += Settings_PropertyChanged;
		}

		/// <summary>Get information about a PE file. If the file is not open, open it.</summary>
		/// <param name="filePath">Path to the file whose information you want to read.</param>
		/// <returns>Information about the PE/COFF file or null.</returns>
		public ElfFile LoadFile(String filePath)
			=> this.LoadFile(filePath, false);

		/// <summary>Get information about a PE file</summary>
		/// <param name="filePath">Path to the file whose information you want to read</param>
		/// <param name="noLoad">Search for the file in already loaded files and, if such a file is not found, do not load</param>
		/// <returns>Information about the PE/COFF file or null</returns>
		public ElfFile LoadFile(String filePath, Boolean noLoad)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			ElfFile result;
			if(noLoad)
			{
				this._binaries.TryGetValue(filePath, out result);
				return result;
			}

			if(!File.Exists(filePath))
				return null;//This is necessary to cut off files that were loaded through memory.

			result = this.LoadFile(filePath, true);
			if(result == null)
				lock(this._binLock)
				{
					result = this.LoadFile(filePath, true);
					if(result == null)
					{
						IImageLoader loader = StreamLoader.FromFile(filePath);

						result = new ElfFile(loader);
						this._binaries.Add(filePath, result);
						if(!this._binaryWatcher.ContainsKey(filePath)//When you update a file, only the file is deleted, not its monitor.
							&& this._plugin.Settings.MonitorFileChange)
							this.RegisterFileWatcher(filePath);
					}
				}
			return result;
		}

		/// <summary>Close a previously opened file</summary>
		/// <param name="filePath">Path to the file to close</param>
		public void UnloadFile(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			ElfFile info = this.LoadFile(filePath, true);
			if(info == null)
				return;//File already unloaded

			try
			{
				IWindow[] windows = this._plugin.HostWindows.Windows.ToArray();
				for(Int32 loop = windows.Length - 1; loop >= 0; loop--)
				{
					IWindow wnd = windows[loop];
					DocumentBase ctrl = wnd.Control as DocumentBase;
					if(ctrl != null && ctrl.FilePath == filePath)
						wnd.Close();
				}
				if(filePath.StartsWith(Constant.BinaryFile))//The binary file is removed from the list immediately after closing.
					this.OnPeListChanged(PeListChangeType.Removed, filePath);
			} finally
			{
				info.Dispose();
				this._binaries.Remove(filePath);
				this.UnregisterFileWatcher(filePath);
			}
		}

		/// <summary>Register a file monitor for changes</summary>
		/// <param name="filePath">Path to the file whose changes to register</param>
		/// <exception cref="ArgumentNullException"><paramref name="filePath"/> is null or empty string</exception>
		/// <exception cref="FileNotFoundException">File not found</exception>
		public void RegisterFileWatcher(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));
			if(!File.Exists(filePath))
				throw new FileNotFoundException("File not found", filePath);

			FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(filePath), Path.GetFileName(filePath))
			{
				NotifyFilter = NotifyFilters.LastWrite
			};
			watcher.Changed += new FileSystemEventHandler(this.watcher_Changed);
			watcher.EnableRaisingEvents = true;
			this._binaryWatcher.Add(filePath, watcher);
		}

		public void UnregisterFileWatcher(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			if(this._binaryWatcher.TryGetValue(filePath, out FileSystemWatcher watcher))
			{
				watcher.Dispose();
				this._binaryWatcher.Remove(filePath);
			}
		}

		/// <summary>Add a file from memory to the list of open files</summary>
		/// <param name="memFile">File from memory</param>
		public void OpenFile(Byte[] memFile)
		{
			if(memFile == null || memFile.Length == 0)
				throw new ArgumentNullException(nameof(memFile));

			String name;
			lock(this._binLock)
			{
				name = this.GetBinaryUniqueName(0);
				ElfFile info = new ElfFile(new StreamLoader(new MemoryStream(memFile)));
				this._binaries.Add(name, info);
			}
			this.OnPeListChanged(PeListChangeType.Added, name);
		}

		/// <summary>Add a file to the list of open files</summary>
		/// <param name="filePath">Path to file</param>
		public Boolean OpenFile(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));
			if(filePath.StartsWith(Constant.BinaryFile))
				return false;//This is necessary to cut off files that were loaded through memory.

			String[] loadedFiles = this._plugin.Settings.LoadedFiles;
			if(loadedFiles.Contains(filePath))
				return false;
			else
			{
				List<String> files = new List<String>(loadedFiles)
				{
					filePath
				};
				this._plugin.Settings.LoadedFiles = files.ToArray();
				this._plugin.HostWindows.Plugins.Settings(this._plugin).SaveAssemblyParameters();
				this.OnPeListChanged(PeListChangeType.Added, filePath);
				return true;
			}
		}

		public void CloseFile(String filePath)
		{
			if(String.IsNullOrEmpty(filePath))
				throw new ArgumentNullException(nameof(filePath));

			String[] loadedFiles = this._plugin.Settings.LoadedFiles;
			List<String> files = new List<String>(loadedFiles);
			if(files.Remove(filePath))
			{//If this is a file from memory, then it is not in the file list.
				this._plugin.Settings.LoadedFiles = files.ToArray();
				this._plugin.HostWindows.Plugins.Settings(this._plugin).SaveAssemblyParameters();
				this.OnPeListChanged(PeListChangeType.Removed, filePath);
			}
		}

		public void Dispose()
		{
			lock(this._binLock)
			{
				this._plugin.Settings.PropertyChanged -= Settings_PropertyChanged;
				foreach(String key in this._binaries.Keys.ToArray())
				{
					ElfFile info = this._binaries[key];
					info.Dispose();
				}
				this._binaries.Clear();
				foreach(String key in this._binaryWatcher.Keys)
					this._binaryWatcher[key].Dispose();
				this._binaryWatcher.Clear();
			}
		}

		/// <summary>The list of uploaded files has changed</summary>
		/// <param name="type">Type of change</param>
		/// <param name="filePath">Path to the file where the change occurred</param>
		private void OnPeListChanged(PeListChangeType type, String filePath)
			=> this.PeListChanged?.Invoke(this, new PeListChangedEventArgs(type, filePath));

		private void Settings_PropertyChanged(Object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
			case nameof(PluginSettings.MonitorFileChange):
				if(this._plugin.Settings.MonitorFileChange)
				{
					if(this._binaryWatcher.Count == 0)
						lock(this._binLock)
						{
							if(this._binaryWatcher.Count == 0)
								foreach(String filePath in this._binaries.Keys)
									if(File.Exists(filePath))
										this.RegisterFileWatcher(filePath);
						}
				} else
				{
					if(this._binaryWatcher.Count > 0)
						lock(this._binLock)
						{
							if(this._binaryWatcher.Count > 0)
								foreach(String key in this._binaryWatcher.Keys)
									this._binaryWatcher[key].Dispose();
							this._binaryWatcher.Clear();
						}
				}
				break;
			}
		}

		private void watcher_Changed(Object sender, FileSystemEventArgs e)
		{
			FileSystemWatcher watcher = (FileSystemWatcher)sender;
			watcher.EnableRaisingEvents = false;

			try
			{//Attempting to bypass file change notification multiple times
				if(MessageBox.Show(
					String.Format("{1}{0}This file has been modified outside of the program.{0}Do you want to reload it?", Environment.NewLine, e.FullPath),
					Assembly.GetExecutingAssembly().GetName().Name,
					MessageBoxButtons.YesNo,
					MessageBoxIcon.Question) == DialogResult.Yes)
				{
					//Closing the old file.
					lock(this._binLock)
					{
						this._binaries[e.FullPath].Dispose();
						this._binaries.Remove(e.FullPath);
					}

					this.OnPeListChanged(PeListChangeType.Changed, e.FullPath);
				}
			} finally
			{
				watcher.EnableRaisingEvents = true;
			}
		}

		/// <summary>Get the unique name of a binary file</summary>
		/// <param name="index">Index, if a file with that name is already loaded</param>
		/// <returns>Unique file name</returns>
		private String GetBinaryUniqueName(UInt32 index)
		{
			String indexName = index > 0
				? String.Format("{0}[{1}]", Constant.BinaryFile, index)
				: Constant.BinaryFile;

			return this._binaries.ContainsKey(indexName)
				? GetBinaryUniqueName(checked(index + 1))
				: indexName;
		}
	}
}
