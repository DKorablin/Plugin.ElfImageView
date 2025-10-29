namespace Plugin.ElfImageView.Bll
{
	/// <summary>File change type</summary>
	public enum PeListChangeType
	{
		/// <summary>Unknown change type</summary>
		None = 0,
		/// <summary>File added</summary>
		Added = 1,
		/// <summary>File removed</summary>
		Removed = 2,
		/// <summary>File changed externally</summary>
		Changed = 3,
	}
}