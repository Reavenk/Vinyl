namespace PxPre.Vinyl.Wav
{
	public struct ChunkFmt
	{
		public ushort compressionCode;
		public ushort numChannels;
		public uint sampleRate;
		public uint avgBytesPerSecond;
		public ushort blockAlign;
		public ushort sigBitsPerSample;
		public ushort extraFormatBytes;

		public void Read(System.IO.BinaryReader r)
		{ 
			this.compressionCode	= r.ReadUInt16();
			this.numChannels		= r.ReadUInt16();
			this.sampleRate			= r.ReadUInt32();
			this.avgBytesPerSecond	= r.ReadUInt32();
			this.blockAlign			= r.ReadUInt16();
			this.sigBitsPerSample	= r.ReadUInt16();
			this.extraFormatBytes	= r.ReadUInt16();
		}
	}
}