using System;

namespace Plugin.ElfImageView
{
	public enum ElfItemType
	{
		Identification,
		Header,
		Sections,
		SectionHeader,

		StringTables,
		Symbols,
		Relocations,
		RelocationsA,
		Notes,

		DebugStrings,
	}
}