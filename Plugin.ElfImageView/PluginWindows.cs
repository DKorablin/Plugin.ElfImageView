using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using AlphaOmega.Debug;
using Plugin.ElfImageView.Bll;
using Plugin.ElfImageView.Directory;
using Plugin.ElfImageView.Properties;
using SAL.Flatbed;
using SAL.Windows;

namespace Plugin.ElfImageView
{
	public class PluginWindows : IPlugin, IPluginSettings<PluginSettings>
	{
		#region Fields
		private TraceSource _trace;
		private PluginSettings _settings;
		private readonly Object _binLock = new Object();
		private FileStorage _binaries;
		private Dictionary<ElfItemType, Type> _directoryViewers;
		private Dictionary<String, DockState> _documentTypes;
		#endregion Fields

		#region Properties
		internal TraceSource Trace => this._trace ?? (this._trace = PluginWindows.CreateTraceSource<PluginWindows>());

		private IMenuItem MenuPeInfo { get; set; }
		private IMenuItem MenuWinApi { get; set; }

		/// <summary>Настройки для взаимодействия из хоста</summary>
		Object IPluginSettings.Settings => this.Settings;

		/// <summary>Настройки для взаимодействия из плагина</summary>
		public PluginSettings Settings
		{
			get
			{
				if(this._settings == null)
				{
					this._settings = new PluginSettings();
					this.HostWindows.Plugins.Settings(this).LoadAssemblyParameters(this._settings);
				}
				return this._settings;
			}
		}

		internal IHostWindows HostWindows { get; }

		/// <summary>Хранилище открытых файлов</summary>
		internal FileStorage Binaries
		{
			get
			{
				if(this._binaries == null)
					lock(this._binLock)
						if(this._binaries == null)
							this._binaries = new FileStorage(this);
				return this._binaries;
			}
		}

		internal Dictionary<ElfItemType, Type> DirectoryViewers
		{//TODO: Маппинг типа енума на UI документа
			get
			{
				if(this._directoryViewers == null)
				{
					this._directoryViewers = new Dictionary<ElfItemType, Type>
					{
						{ ElfItemType.StringTables, typeof(DocumentSection) },
						{ ElfItemType.Symbols, typeof(DocumentSection) },
						{ ElfItemType.Relocations, typeof(DocumentSection) },
						{ ElfItemType.RelocationsA, typeof(DocumentSection) },
						{ ElfItemType.Notes, typeof(DocumentSection) },
						{ ElfItemType.DebugStrings, typeof(DocumentSection) },
					};
				}
				return this._directoryViewers;
			}
		}

		private Dictionary<String, DockState> DocumentTypes
		{
			get
			{//TODO: Список поддерживаемых окон
				if(this._documentTypes == null)
					this._documentTypes = new Dictionary<String, DockState>()
					{
						{ typeof(DocumentSection).ToString(), DockState.Document },
						{ typeof(DocumentBinary).ToString(), DockState.Document },
						{ typeof(PanelTOC).ToString(), DockState.DockRightAutoHide },
					};
				return this._documentTypes;
			}
		}
		#endregion Properties

		public PluginWindows(IHostWindows hostWindows)
			=> this.HostWindows = hostWindows ?? throw new ArgumentNullException(nameof(hostWindows));

		public IWindow GetPluginControl(String typeName, Object args)
			=> this.CreateWindow(typeName, false, args);

		/// <summary>Get the type of the object that is used to search for the object</summary>
		/// <returns>Reflection on the type of object used for searching</returns>
		public Type GetEntityType()
			=> typeof(AlphaOmega.Debug.ElfFile);

		/// <summary>Create an instance of an object to search through reflection</summary>
		/// <remarks>To get a list, use <see cref="GetSearchObjects"/></remarks>
		/// <param name="filePath">The path to the element by which to create an instance for the search</param>
		/// <returns>The given object instance</returns>
		public Object CreateEntityInstance(String filePath)
		{
			ElfFile result = new ElfFile(StreamLoader.FromFile(filePath));
			return result;
		}

		/// <summary>Return objects for search, at the choice of the client, which will be searched</summary>
		/// <param name="folderPath">Path to folder where search for files</param>
		/// <returns>An array of files to search for, or null if the client didn't select anything</returns>
		public String[] GetSearchObjects(String folderPath)
		{
			List<String> result = new List<String>();
			foreach(String file in System.IO.Directory.GetFiles(folderPath, "*.*", System.IO.SearchOption.AllDirectories))//TODO: При переходе на .NET 4 переделать на Directory.EnumerateFiles
				if(System.IO.Path.GetExtension(file).Equals(".so", StringComparison.OrdinalIgnoreCase))
					result.Add(file);
			return result.ToArray();
		}

		Boolean IPlugin.OnConnection(ConnectMode mode)
		{
			IMenuItem menuView = this.HostWindows.MainMenu.FindMenuItem("View");
			if(menuView == null)
			{
				this.Trace.TraceEvent(TraceEventType.Error, 10, "Menu item 'View' not found");
				return false;
			}

			this.MenuWinApi = menuView.FindMenuItem("Executables");
			if(this.MenuWinApi == null)
			{
				this.MenuWinApi = menuView.Create("Executables");
				this.MenuWinApi.Name = "View.Executable";
				menuView.Items.Add(this.MenuWinApi);
			}

			this.MenuPeInfo = this.MenuWinApi.Create("&ELF View");
			this.MenuPeInfo.Name = "View.Executable.ElfView";
			this.MenuPeInfo.Click += (sender, e) => { this.CreateWindow(typeof(PanelTOC).ToString(), true); };

			this.MenuWinApi.Items.Add(this.MenuPeInfo);
			return true;
		}

		Boolean IPlugin.OnDisconnection(DisconnectMode mode)
		{
			if(this.MenuPeInfo != null)
				this.HostWindows.MainMenu.Items.Remove(this.MenuPeInfo);
			if(this.MenuWinApi != null && this.MenuWinApi.Items.Count == 0)
				this.HostWindows.MainMenu.Items.Remove(this.MenuWinApi);

			if(NodeExtender._nullFont != null)
				NodeExtender._nullFont.Dispose();

			if(this._binaries != null)
				this._binaries.Dispose();
			return true;
		}

		internal String FormatValue(Object value)
			=> value == null
				? null
				: this.FormatValue(value.GetType(), value);

		internal String FormatValue(MemberInfo info, Object value)
		{
			if(value == null)
				return null;

			Type type = info.GetMemberType();

			if(type.IsEnum)
				return value.ToString();
			else if(type == typeof(Char))
			{
				switch((Char)value)
				{
				case '\'':	return "\\\'";
				case '\"':	return "\\\"";
				case '\0':	return "\\0";
				case '\a':	return "\\a";
				case '\b':	return "\\b";
				case '\f':	return "\\b";
				case '\t':	return "\\t";
				case '\n':	return "\\n";
				case '\r':	return "\\r";
				case '\v':	return "\\v";
				default:	return value.ToString();
				}
			} else if(value is IFormattable)
			{
				type = type.GetRealType();//INullable<Enum>
				if(type.IsEnum)
					return value.ToString();

				switch(Convert.GetTypeCode(value))
				{
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.UInt16:
				case TypeCode.Int32:
				case TypeCode.UInt32:
				case TypeCode.Int64:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					if(this.Settings.ShowAsHexValue)
						return "0x" + ((IFormattable)value).ToString("X", CultureInfo.CurrentCulture);
					else
						return ((IFormattable)value).ToString("n0", CultureInfo.CurrentCulture);
				default:
					return value.ToString();
				}
			} else
			{
				Type elementType = type.HasElementType ? type.GetElementType() : null;
				if(elementType != null && elementType.IsPrimitive && type.BaseType == typeof(Array))
				{
					Int32 index = 0;
					Array arr = (Array)value;
					StringBuilder values = new StringBuilder($"{elementType}[{this.FormatValue(arr.Length)}]");
					if(this.Settings.MaxArrayDisplay > 0)
					{
						values.Append(" { ");
						foreach(Object item in arr)
						{
							if(index++ > this.Settings.MaxArrayDisplay)
							{
								values.Append("...");
								break;
							}

							values.Append((this.FormatValue(item) ?? Resources.NullString) + ", ");
						}
						values.Append("}");
					}
					return values.ToString();
				} else
					return value.ToString();
			}
		}

		internal Object GetSectionData(ElfItemType type, String nodeName, String filePath)
		{
			ElfFile info = this.Binaries.LoadFile(filePath);

			return this.GetSectionData(type, nodeName, info);
		}

		/// <summary>Получить объект, соответсвующий определённому идентификатору енума</summary>
		/// <param name="type">Тип заголовка</param>
		/// <param name="filePath">Путь к PE файлу</param>
		/// <returns></returns>
		internal Object GetSectionData(ElfItemType type, String nodeName, ElfFile info)
		{
			switch(type)
			{
			case ElfItemType.Identification:
				return info.Header.Identification;
			case ElfItemType.Header:
				return info.Header.Header;
			case ElfItemType.Sections:
				return info.Sections;
			case ElfItemType.SectionHeader:
				foreach(Section section in info.Sections)
					if(section.Name == nodeName)
						return section;
				throw new ArgumentException($"Field '{nodeName}' not found");
			case ElfItemType.StringTables:
				return info.GetStringSections().Select(p => p.Section).ToArray();
			case ElfItemType.Symbols:
				return info.GetSymbolSections().Select(p => p.Section).ToArray();
			case ElfItemType.Relocations:
				return info.GetRelocationSections().Select(p => p.Section).ToArray();
			case ElfItemType.RelocationsA:
				return info.GetRelocationASections().Select(p => p.Section).ToArray();
			case ElfItemType.Notes:
				return info.GetNotesSections().Select(p => p.Section).ToArray();
			case ElfItemType.DebugStrings:
				DebugStringSection sectionWrapper = info.GetDebugStringSection();
				return sectionWrapper == null
					? null
					: sectionWrapper.Section;
			default:
				throw new NotImplementedException($"Data retrieval for type '{type}' not found");
			}
		}

		internal IWindow CreateWindow(String typeName, Boolean searchForOpened, Object args = null)
			=> this.DocumentTypes.TryGetValue(typeName, out DockState state)
				? this.HostWindows.Windows.CreateWindow(this, typeName, searchForOpened, state, args)
				: null;

		internal IWindow CreateWindow(ElfItemType typeName, DocumentBaseSettings args)
			=> this.DirectoryViewers.TryGetValue(typeName, out Type type)
				? this.HostWindows.Windows.CreateWindow(this, type.ToString(), true, DockState.Document, args)
				: null;

		internal IWindow CreateWindow<T, A>(A args) where T : class, IPluginSettings<A> where A : class
		{
			String type = typeof(T).ToString();
			return this.DocumentTypes.TryGetValue(type, out DockState state)
				? this.HostWindows.Windows.CreateWindow(this, type, true, state, args)
				: null;
		}

		private static TraceSource CreateTraceSource<T>(String name = null) where T : IPlugin
		{
			TraceSource result = new TraceSource(typeof(T).Assembly.GetName().Name + name);
			result.Switch.Level = SourceLevels.All;
			result.Listeners.Remove("Default");
			result.Listeners.AddRange(System.Diagnostics.Trace.Listeners);
			return result;
		}
	}
}