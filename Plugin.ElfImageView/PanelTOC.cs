using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AlphaOmega.Debug;
using AlphaOmega.Windows.Forms;
using Plugin.ElfImageView.Bll;
using SAL.Windows;
using Plugin.ElfImageView.Directory;
using Plugin.ElfImageView.Properties;

namespace Plugin.ElfImageView
{
	public partial class PanelTOC : UserControl
	{
		private const String Caption = "ELF View";
		private readonly SystemImageList _smallImageList = new SystemImageList(SystemImageListSize.SmallIcons);
		#region Properties

		private PluginWindows Plugin => (PluginWindows)this.Window.Plugin;

		private IWindow Window => (IWindow)base.Parent;

		private ElfItemType? SelectedHeader => tvToc.SelectedNode.Tag as ElfItemType?;

		private String SelectedPE
		{
			get
			{
				TreeNode node = tvToc.SelectedNode;
				while(node.Parent != null)
					node = node.Parent;
				return (String)node.Tag;
			}
		}
		#endregion Properties

		public PanelTOC()
		{
			InitializeComponent();
			gridSearch.TreeView = tvToc;
			SystemImageListHelper.SetImageList(tvToc, this._smallImageList, false);
		}

		protected override void OnCreateControl()
		{
			this.Window.SetTabPicture(Resources.iconElf);
			this.Window.Closing += new EventHandler<CancelEventArgs>(Window_Closing);
			lvInfo.Plugin = this.Plugin;

			String[] loadedFiles = this.Plugin.Settings.LoadedFiles;
			foreach(String file in loadedFiles)
				this.FillToc(file);
			this.ChangeTitle();

			this.Plugin.Binaries.PeListChanged += new EventHandler<PeListChangedEventArgs>(Plugin_PeListChanged);
			this.Plugin.Settings.PropertyChanged += Settings_PropertyChanged;
			base.OnCreateControl();
		}

		private void Window_Closing(Object sender, CancelEventArgs e)
		{
			this.Plugin.Binaries.PeListChanged -= new EventHandler<PeListChangedEventArgs>(Plugin_PeListChanged);
			this.Plugin.Settings.PropertyChanged -= Settings_PropertyChanged;
		}

		/// <summary>Изменить заголовок окна</summary>
		private void ChangeTitle()
			=> this.Window.Caption = tvToc.Nodes.Count > 0
				? String.Format("{0} ({1})", PanelTOC.Caption, tvToc.Nodes.Count)
				: this.Window.Caption = PanelTOC.Caption;

		/// <summary>Поиск узла в дереве по пути к файлу</summary>
		/// <param name="filePath">Путь к файлу</param>
		/// <returns>Найденный узел в дереве или null</returns>
		private TreeNode FindNode(String filePath)
		{
			foreach(TreeNode node in tvToc.Nodes)
				if(filePath.Equals(node.Tag))
					return node;
			return null;
		}

		protected override Boolean ProcessDialogKey(Keys keyData)
		{
			switch(keyData)
			{
			case Keys.Control | Keys.O:
				this.tsbnOpen_Click(this, EventArgs.Empty);
				return true;
			default:
				return base.ProcessDialogKey(keyData);
			}
		}

		private void Plugin_PeListChanged(Object sender, PeListChangedEventArgs e)
		{
			if(base.InvokeRequired)
				base.Invoke((MethodInvoker)delegate { this.Plugin_PeListChanged(sender, e); });
			else
				switch(e.Type)
				{
				case PeListChangeType.Added:
					TreeNode root = this.FillToc(e.FilePath);
					if(root != null)
						tvToc.SelectedNode = root;
					break;
				case PeListChangeType.Changed:
					TreeNode node = this.FindNode(e.FilePath);
					if(node.IsSelected)
						this.tvToc_AfterSelect(sender, new TreeViewEventArgs(node));
					break;
				case PeListChangeType.Removed:
					this.FindNode(e.FilePath).Remove();
					break;
				default:
					throw new NotImplementedException(e.Type.ToString());
				}
			this.ChangeTitle();
		}

		private void Settings_PropertyChanged(Object sender, PropertyChangedEventArgs e)
		{
			switch(e.PropertyName)
			{
			case nameof(PluginSettings.MaxArrayDisplay):
			case nameof(PluginSettings.ShowAsHexValue):
				if(tvToc.SelectedNode != null)
					this.tvToc_AfterSelect(sender, new TreeViewEventArgs(tvToc.SelectedNode));
				break;
			}
		}

		private void OpenBinaryDocument(ElfItemType type, String nodeName, String filePath)//TODO: Binary view not supported
			=> this.Plugin.CreateWindow<DocumentBinary, DocumentBaseSettings>(new DocumentBinarySettings() { FilePath = filePath, Header = type, NodeName = nodeName, });

		private void OpenDirectoryDocument(ElfItemType type, String nodeName, String filePath)
		{
			if(this.Plugin.CreateWindow(type, new DocumentSectionSettings() { FilePath = filePath, Header = type, }) == null)
			{
				if(this.Plugin.GetSectionData(type, nodeName, filePath) is ISectionData)
					this.OpenBinaryDocument(type, nodeName, filePath);
				else
					this.Plugin.Trace.TraceInformation("Viwer {0} not implemented", type); ;
			}
		}

		private TreeNode FillToc(String filePath)
		{
			//Проверка на уже добавленные файлы в дерево
			TreeNode n = this.FindNode(filePath);
			if(n != null)
			{
				tvToc.SelectedNode = n;
				return null;
			}

			TreeNode result = new TreeNode(filePath) { Tag = filePath, };
			result.Nodes.Add(new TreeNode(String.Empty) { ImageIndex = -1, SelectedImageIndex = -1, });
			result.ImageIndex = result.SelectedImageIndex = this._smallImageList.IconIndex(filePath);
			tvToc.Nodes.Add(result);
			return result;
		}

		private static TreeNode CreateDirectoryNode(Object tag, Boolean isEmpty, ElfItemType type)
		{
			TreeNode result = new TreeNode() { Tag = type, ImageIndex = -1, SelectedImageIndex = -1, };
			if(isEmpty)
				result.SetNull();
			else
				result.ForeColor = Color.Empty;

			result.Text = Constant.GetHeaderName(type);
			return result;
		}

		private void tvToc_AfterSelect(Object sender, TreeViewEventArgs e)
		{
			lvInfo.Items.Clear();

			splitToc.Panel2Collapsed = false;
			String filePath = this.SelectedPE;
			if(e.Node.Parent == null)//Описание файла
				lvInfo.DataBind(new FileInfo(filePath));

			try
			{
				base.Cursor = Cursors.WaitCursor;
				ElfItemType? type = this.SelectedHeader;
				if(type.HasValue)//Директория PE файла
				{
					Object target = this.Plugin.GetSectionData(type.Value, e.Node.Text, filePath);
					lvInfo.DataBind(target);
				} else if(e.Node.Tag != null && e.Node.Parent != null)
					lvInfo.DataBind(e.Node.Tag);//Generic объект
			} finally
			{
				base.Cursor = Cursors.Default;
			}
		}

		private void tvToc_MouseClick(Object sender, MouseEventArgs e)
		{
			switch(e.Button)
			{
			case MouseButtons.Right:
				TreeViewHitTestInfo info = tvToc.HitTest(e.Location);
				if(info.Node != null)
				{
					tvToc.SelectedNode = info.Node;
					cmsToc.Show(tvToc, e.Location);
				}
				break;
			}
		}
		private void tvToc_MouseDoubleClick(Object sender, MouseEventArgs e)
		{
			TreeViewHitTestInfo info = tvToc.HitTest(e.Location);
			ElfItemType? type = info.Node == null ? null : info.Node.Tag as ElfItemType?;
			if(type.HasValue && !info.Node.IsNull())
			{
				String filePath = this.SelectedPE;
				this.OpenDirectoryDocument(type.Value, info.Node.Text, filePath);
			}
		}

		private void tsbnOpen_Click(Object sender, EventArgs e)
			=> this.tsbnOpen_DropDownItemClicked(sender, new ToolStripItemClickedEventArgs(tsmiOpenFile));

		private void tsbnOpen_DropDownItemClicked(Object sender, ToolStripItemClickedEventArgs e)
		{
			tsbnOpen.DropDown.Close(ToolStripDropDownCloseReason.ItemClicked);

			if(e.ClickedItem == tsmiOpenFile)
			{
				using(OpenFileDialog dlg = new OpenFileDialog() { CheckFileExists = true, Multiselect = true, Filter = "ELF files (*.so)|*.so|All files (*.*)|*.*", Title = "Choose ELF image", })
					if(dlg.ShowDialog() == DialogResult.OK)
						foreach(String filePath in dlg.FileNames)
							this.Plugin.Binaries.OpenFile(filePath);
			} else if(e.ClickedItem == tsmiOpenBase64)
			{
				using(HexLoadDlg dlg = new HexLoadDlg())
					if(dlg.ShowDialog() == DialogResult.OK)
						this.Plugin.Binaries.OpenFile(dlg.Result);
			} else
				throw new NotSupportedException(e.ClickedItem.ToString());
		}

		private void tvToc_KeyDown(Object sender, KeyEventArgs e)
		{
			switch(e.KeyData)
			{
			case Keys.Delete:
			case Keys.Back:
				this.cmsToc_ItemClicked(sender, new ToolStripItemClickedEventArgs(tsmiTocUnload));
				e.Handled = true;
				break;
			case Keys.Return:
				ElfItemType? type = this.SelectedHeader;
				if(type != null && !tvToc.SelectedNode.IsNull())
					this.OpenDirectoryDocument(type.Value, tvToc.SelectedNode.Text, this.SelectedPE);
				e.Handled = true;
				break;
			case Keys.C | Keys.Control:
				if(tvToc.SelectedNode != null)
					Clipboard.SetText(tvToc.SelectedNode.Text);
				e.Handled = true;
				break;
			}
		}

		private void cmsToc_Opening(Object sender, CancelEventArgs e)
		{
			TreeNode node = tvToc.SelectedNode;
			tsmiTocUnload.Visible = tsmiTocExplorerView.Visible = tsmiTocBinView.Visible = false;
			Boolean showUnload = false;
			Boolean showBinView = false;

			if(node != null)
			{
				tsmiTocUnload.Visible = tsmiTocExplorerView.Visible = showUnload = node.Parent == null;//PE File

				ElfItemType? type = node.Tag as ElfItemType?;
				if(!node.IsNull() && type != null)
				{
					String filePath = this.SelectedPE;

					showBinView = this.Plugin.GetSectionData(type.Value, node.Text, filePath) is ISectionData;
					tsmiTocBinView.Visible = showBinView;
				}
			}

			e.Cancel = !showUnload && !showBinView;
		}

		private void cmsToc_ItemClicked(Object sender, ToolStripItemClickedEventArgs e)
		{
			TreeNode node = tvToc.SelectedNode;
			if(e.ClickedItem == tsmiTocUnload)
			{
				if(node != null && node.Parent == null)
				{
					this.Plugin.Binaries.UnloadFile(this.SelectedPE);
					this.Plugin.Binaries.CloseFile(this.SelectedPE);
				}
			} else if(e.ClickedItem == tsmiTocBinView)
			{
				String filePath = this.SelectedPE;
				ElfItemType type = (ElfItemType)node.Tag;
				this.OpenBinaryDocument(type, node.Text, filePath);
			} else if(e.ClickedItem == tsmiTocExplorerView)
			{
				String filePath = this.SelectedPE;
				Shell32.OpenFolderAndSelectItem(Path.GetDirectoryName(filePath), Path.GetFileName(filePath));
			} else
				throw new NotImplementedException(e.ClickedItem.ToString());
		}

		private void tvToc_BeforeExpand(Object sender, TreeViewCancelEventArgs e)
		{
			if(e.Action == TreeViewAction.Expand && e.Node.IsClosedRootNode())
			{
				String filePath = (String)e.Node.Tag;

				ElfFile info;
				try
				{
					info = this.Plugin.Binaries.LoadFile(filePath);
					if(info == null)
					{
						e.Node.Nodes[0].SetException("Error opening file");
						return;
					} else
						e.Node.Nodes.Clear();
				} catch(Exception exc)
				{
					e.Node.Nodes[0].SetException(exc);
					e.Node.Nodes[0].Tag = exc;
					return;
				}

				List<TreeNode> nodes = new List<TreeNode>();
				nodes.AddRange(new TreeNode[]
				{
					PanelTOC.CreateDirectoryNode(null, false, ElfItemType.Identification),
					PanelTOC.CreateDirectoryNode(null, false, ElfItemType.Header),
				});

				TreeNode sections = PanelTOC.CreateDirectoryNode(null, false, ElfItemType.Sections);
				foreach(Section section in info.Sections)
					sections.Nodes.Add(new TreeNode(section.Name) { Tag = ElfItemType.SectionHeader, ImageIndex = -1, SelectedImageIndex = -1, });

				nodes.AddRange(new TreeNode[]
				{
					PanelTOC.CreateDirectoryNode(null, info.GetStringSections().FirstOrDefault() == null, ElfItemType.StringTables),
					PanelTOC.CreateDirectoryNode(null, info.GetSymbolSections().FirstOrDefault() == null, ElfItemType.Symbols),
					PanelTOC.CreateDirectoryNode(null, info.GetRelocationSections().FirstOrDefault() == null, ElfItemType.Relocations),
					PanelTOC.CreateDirectoryNode(null, info.GetRelocationASections().FirstOrDefault() == null, ElfItemType.RelocationsA),
					PanelTOC.CreateDirectoryNode(null, info.GetNotesSections().FirstOrDefault() == null, ElfItemType.Notes),
					PanelTOC.CreateDirectoryNode(null, info.GetDebugStringSection() == null, ElfItemType.DebugStrings),
					sections,
				});

				e.Node.Nodes.AddRange(nodes.ToArray());
			}
		}

		private void tvToc_DragEnter(Object sender, DragEventArgs e)
			=> e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Move : DragDropEffects.None;

		private void tvToc_DragDrop(Object sender, DragEventArgs e)
		{
			foreach(String filePath in (String[])e.Data.GetData(DataFormats.FileDrop))
				this.Plugin.Binaries.OpenFile(filePath);
		}
	}
}
