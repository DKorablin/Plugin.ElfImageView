namespace Plugin.ElfImageView.Directory
{
	partial class DocumentSection
	{
		/// <summary> 
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary> 
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if(disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Component Designer generated code

		/// <summary> 
		/// Required method for Designer support - do not modify 
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.Windows.Forms.SplitContainer splitHead;
			System.Windows.Forms.SplitContainer splitMain;
			this.tvSections = new System.Windows.Forms.TreeView();
			this.lvSection = new Plugin.ElfImageView.Controls.ReflectionListView();
			this.lvData = new Plugin.ElfImageView.Controls.ReflectionArrayListView();
			splitHead = new System.Windows.Forms.SplitContainer();
			splitMain = new System.Windows.Forms.SplitContainer();
			splitHead.Panel1.SuspendLayout();
			splitHead.Panel2.SuspendLayout();
			splitHead.SuspendLayout();
			splitMain.Panel1.SuspendLayout();
			splitMain.Panel2.SuspendLayout();
			splitMain.SuspendLayout();
			this.SuspendLayout();
			// 
			// tvTables
			// 
			this.tvSections.Dock = System.Windows.Forms.DockStyle.Fill;
			this.tvSections.FullRowSelect = true;
			this.tvSections.HideSelection = false;
			this.tvSections.Location = new System.Drawing.Point(0, 0);
			this.tvSections.Name = "tvSections";
			this.tvSections.ShowLines = false;
			this.tvSections.ShowRootLines = false;
			this.tvSections.Size = new System.Drawing.Size(200, 72);
			this.tvSections.TabIndex = 0;
			this.tvSections.AfterSelect += new System.Windows.Forms.TreeViewEventHandler(this.tvSections_AfterSelect);
			// 
			// lvSection
			// 
			this.lvSection.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvSection.FullRowSelect = true;
			this.lvSection.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			this.lvSection.HideSelection = false;
			this.lvSection.Location = new System.Drawing.Point(0, 0);
			this.lvSection.MultiSelect = false;
			this.lvSection.Name = "lvSection";
			this.lvSection.Plugin = null;
			this.lvSection.Size = new System.Drawing.Size(200, 74);
			this.lvSection.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvSection.TabIndex = 0;
			this.lvSection.UseCompatibleStateImageBehavior = false;
			this.lvSection.View = System.Windows.Forms.View.Details;
			// 
			// splitHead
			// 
			splitHead.Dock = System.Windows.Forms.DockStyle.Fill;
			splitHead.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitHead.Location = new System.Drawing.Point(0, 0);
			splitHead.Name = "splitHead";
			splitHead.Orientation = System.Windows.Forms.Orientation.Horizontal;
			// 
			// splitHead.Panel1
			// 
			splitHead.Panel1.Controls.Add(this.tvSections);
			// 
			// splitHead.Panel2
			// 
			splitHead.Panel2.Controls.Add(this.lvSection);
			splitHead.Size = new System.Drawing.Size(200, 150);
			splitHead.SplitterDistance = 70;
			splitHead.TabIndex = 1;
			// 
			// splitMain
			// 
			splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
			splitMain.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
			splitMain.Location = new System.Drawing.Point(0, 0);
			splitMain.Name = "splitMain";
			// 
			// splitMain.Panel1
			// 
			splitMain.Panel1.Controls.Add(splitHead);
			// 
			// splitMain.Panel2
			// 
			splitMain.Panel2.Controls.Add(this.lvData);
			splitMain.Size = new System.Drawing.Size(240, 150);
			splitMain.SplitterDistance = 150;
			splitMain.TabIndex = 0;
			// 
			// lvTable
			// 
			this.lvData.Dock = System.Windows.Forms.DockStyle.Fill;
			this.lvData.Location = new System.Drawing.Point(0, 0);
			this.lvData.MultiSelect = false;
			this.lvData.Name = "lvData";
			this.lvData.Plugin = null;
			this.lvData.Size = new System.Drawing.Size(36, 150);
			this.lvData.Sorting = System.Windows.Forms.SortOrder.Ascending;
			this.lvData.TabIndex = 0;
			this.lvData.UseCompatibleStateImageBehavior = false;
			// 
			// DocumentStringTables
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Controls.Add(splitMain);
			this.Name = "DocumentStringTables";
			this.Size = new System.Drawing.Size(240, 150);
			splitHead.Panel1.ResumeLayout(false);
			splitHead.Panel2.ResumeLayout(false);
			splitHead.ResumeLayout(false);
			splitMain.Panel1.ResumeLayout(false);
			splitMain.Panel2.ResumeLayout(false);
			splitMain.ResumeLayout(false);
			this.ResumeLayout(false);

		}

		#endregion

		private System.Windows.Forms.TreeView tvSections;
		private Plugin.ElfImageView.Controls.ReflectionArrayListView lvData;
		private Plugin.ElfImageView.Controls.ReflectionListView lvSection;
	}
}
