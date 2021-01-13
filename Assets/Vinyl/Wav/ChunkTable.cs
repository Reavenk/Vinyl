namespace PxPre.Vinyl.Wav
{
	/// <summary>
	/// A chunk that was encountered in the file
	/// </summary>
	public struct ChunkTable
	{
		public int chunkID;
		public long filePos;
		public int size;

		public ChunkTable(int chunkID, long filePos, int size)
		{
			this.chunkID = chunkID;
			this.filePos = filePos;
			this.size = size;
		}
	}
}