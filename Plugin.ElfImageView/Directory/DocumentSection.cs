using System;
using System.Collections.Generic;
using System.Windows.Forms;
using AlphaOmega.Debug;
using System.IO;
using System.Collections;

namespace Plugin.ElfImageView.Directory
{
	public partial class DocumentSection : DocumentBase
	{
		private DocumentSectionSettings _settings;

		public override DocumentBaseSettings Settings => this.SettingsI;

		private DocumentSectionSettings SettingsI => this._settings ?? (this._settings = new DocumentSectionSettings());

		public DocumentSection()
			: base(ElfItemType.StringTables)
			=> this.InitializeComponent();

		protected override void SetCaption()
			=> this.Window.Caption = String.Join(" - ", new String[] { Constant.GetHeaderName(this.SettingsI.Header), Path.GetFileName(this.Settings.FilePath), });

		protected override void ShowFile(ElfFile info)
		{
			lvData.Plugin = base.Plugin;
			lvSection.Plugin = base.Plugin;

			tvSections.Nodes.Clear();

			List<TreeNode> nodes = new List<TreeNode>();
			switch(this.SettingsI.Header)
			{
			case ElfItemType.StringTables:
				foreach(StringSection section in info.GetStringSections())
					nodes.Add(new TreeNode(section.Section.Name) { Tag = section, });
				break;
			case ElfItemType.Symbols:
				foreach(SymbolSection section in info.GetSymbolSections())
					nodes.Add(new TreeNode(section.Section.Name) { Tag = section, });
				break;
			case ElfItemType.Relocations:
				foreach(RelocationSection section in info.GetRelocationSections())
					nodes.Add(new TreeNode(section.Section.Name) { Tag = section, });
				break;
			case ElfItemType.RelocationsA:
				foreach(RelocationASection section in info.GetRelocationASections())
					nodes.Add(new TreeNode(section.Section.Name) { Tag = section, });
				break;
			case ElfItemType.Notes:
				foreach(NoteSection section in info.GetNotesSections())
					nodes.Add(new TreeNode(section.Section.Name) { Tag = section, });
				break;
			case ElfItemType.DebugStrings:
				DebugStringSection debugSection = info.GetDebugStringSection();
				if(debugSection != null)
					nodes.Add(new TreeNode(debugSection.Section.Name) { Tag = debugSection, });
				break;
			default:
				throw new NotImplementedException($"Viewer for section {this.SettingsI.Header} not added");
			}

			tvSections.Nodes.AddRange(nodes.ToArray());
		}

		private void tvSections_AfterSelect(Object sender, TreeViewEventArgs e)
		{
			SectionBase section = (SectionBase)e.Node.Tag;

			lvSection.DataBind(section.Section);

			lvData.DataBind((IEnumerable)section);
		}
	}
}