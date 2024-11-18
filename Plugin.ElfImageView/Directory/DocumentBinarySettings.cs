using System;

namespace Plugin.ElfImageView.Directory
{
	public class DocumentBinarySettings : DocumentBaseSettings
	{
		public ElfItemType Header { get; set; }

		public String NodeName { get; set; }
	}
}