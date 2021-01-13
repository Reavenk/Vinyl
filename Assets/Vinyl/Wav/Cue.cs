namespace PxPre.Vinyl.Wav
{
	public struct Cue
	{
		public int id;
		public int position;
		public int dataChunkId;
		public uint chunkStart;
		public uint blockStart;
		public uint sampleOffset;
	}
}