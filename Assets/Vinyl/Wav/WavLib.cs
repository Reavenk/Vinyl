// MIT License
// 
// Copyright (c) 2021 Pixel Precision, LLC
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all
// copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System;
using UnityEngine;
using System.Collections.Generic;

namespace PxPre.Vinyl.Wav
{
	//	http://soundfile.sapp.org/doc/WaveFormat/
	// https://sites.google.com/site/musicgapi/technical-documents/wav-file-format

	public struct AudioChunk
	{ 
		public uint silentSamples;
		public List<float[]> channels;

		public AudioChunk(uint silentSamples)
		{ 
			this.silentSamples = 0;
			this.channels = null;
		}

		public AudioChunk(List<float[]> channels)
		{ 
			this.silentSamples = 0;
			this.channels = channels;
		}
	}

	public static class WAVUtils
	{
		public static List<ChunkTable> ParseStaticChunks(System.IO.Stream fs)
		{
			if (fs.CanRead == false)
				return new List<ChunkTable>();

			using(System.IO.BinaryReader r = new System.IO.BinaryReader(fs))
				return ParseStaticChunks(r);
		}

		public static List<ChunkTable> ParseStaticChunks(System.IO.BinaryReader r)
		{
			List<ChunkTable> ret = new List<ChunkTable>();

			int chunkID = r.ReadInt32();
			int chunkSz = r.ReadInt32();
			long startPos = r.BaseStream.Position;

			if(chunkID != (int)ChunkID.RIFF && chunkID != (int)ChunkID.RIFX)
				return ret;

			// Pass the RIFF format data (WAVE)
			r.BaseStream.Seek(startPos + 4, System.IO.SeekOrigin.Begin);

			// Get the full-file chunk
			ret.Add(new ChunkTable(chunkID, startPos, chunkSz));

			// And then do internal appending.
			ParseBoundedStaticChunksInto(r, ret, startPos + chunkSz);

			return ret;
		}

		/// <summary>
		/// Used to break into subchunks. While this is used to break from the starting RIFF chunk, it 
		/// generalizes the functionality so we can also access subchunks in wavl chunk.
		/// </summary>
		/// <param name="r">The reader to parse data - with its stream position already placed at the
		/// intended read location.</param>
		/// <param name="ret">The destination to push encountered chunks into for return to the caller.</param>
		/// <param name="bounds">The end of the region to parse.</param>
		/// <remarks>Only handles the next depth, not a recursive function.</remarks>
		public static void ParseBoundedStaticChunksInto(System.IO.BinaryReader r, List<ChunkTable> ret, long bounds)
		{
			while (r.BaseStream.Position < bounds)
			{
				int chunkID = r.ReadInt32();
				int chunkSz = r.ReadInt32();
				long startPos = r.BaseStream.Position;

				ret.Add(new ChunkTable(chunkID, startPos, chunkSz));

				r.BaseStream.Seek(startPos + chunkSz, System.IO.SeekOrigin.Begin);				
			}
		}

		public static List<AudioChunk> ParseStaticPCM(
			System.IO.Stream fs, 
			List<ChunkTable> data, 
			out ChunkFmt ? fmt, 
			bool interlace = true)
		{
				
			if(fs.CanRead == false || fs.CanSeek == false)
			{
				fmt = null;
				return new List<AudioChunk>();
			}

			using(System.IO.BinaryReader r = new System.IO.BinaryReader(fs))
			{
				return ParseStaticPCM(r, data, out fmt, interlace);
			}
		}

		public static List<AudioChunk> ParseStaticPCM(
			System.IO.BinaryReader r,
			List<ChunkTable> data,
			out ChunkFmt? fmt,
			bool interlace = true)
		{
			fmt = null;
			List < AudioChunk >  ret = new List<AudioChunk>();
			Endianness endi = Endianness.Little;

			_ProcessStaticChunksForPCM(r, data, ret, ref fmt, ref endi, interlace);
			return ret;
		}

		private static void _ProcessStaticChunksForPCM(
		System.IO.BinaryReader r, 
		List<ChunkTable> chunks, 
		List<AudioChunk> ret, 
		ref ChunkFmt? fmt,
		ref Endianness endi,
		bool interlace = true)
		{
			foreach (ChunkTable ct in chunks)
			{
				switch ((ChunkID)ct.chunkID)
				{
					case ChunkID.RIFX:
						endi = Endianness.Big;
						break;

					case ChunkID.fmt:
						r.BaseStream.Seek(ct.filePos, System.IO.SeekOrigin.Begin);

						ChunkFmt cfmt = new ChunkFmt();
						cfmt.Read(r);
						fmt = cfmt;
						break;

					case ChunkID.data:
						if (fmt.HasValue == false)
							continue;

						if(fmt.Value.numChannels == 0)
							continue;

						r.BaseStream.Seek(ct.filePos, System.IO.SeekOrigin.Begin);

						{ 
							// NOTE: BIG ENDIAN UNIMPLEMENTED

							AudioChunk ac = new AudioChunk();
							if(fmt.Value.numChannels == 1 || interlace == true)
							{
								byte[] rb = r.ReadBytes(ct.size);

								switch (fmt.Value.sigBitsPerSample)
								{
									case 8:
										{
											int sct = ct.size;
											float [] rf = new float[sct];
											ac.channels = new List<float[]>();
											ac.channels.Add(rf);
											for(int i = 0; i < sct; ++i)
												rf[i] = (float)(rb[i] - 128) / 128.0f;
											ret.Add(ac);
										}
										break;

									case 16:
										{
											int sct = ct.size / 2;
											float[] rf = new float[sct];
											ac.channels = new List<float[]>();
											ac.channels.Add(rf);
											for (int i = 0; i < sct; ++i)
												rf[i] = (float)System.BitConverter.ToInt16(rb, i * 2) / (float)short.MaxValue;
											ret.Add(ac);
										}
										break;

									case 24:
										{
											const float maxSigned24 = 8388608.0f; // 24^2/2.0
											int sct = ct.size / 3;
											float[] rf = new float[sct];
											ac.channels = new List<float[]>();
											ac.channels.Add(rf);
											for (int i = 0; i < sct; ++i)
											{
												byte b0 = rb[i * 3 + 0];
												byte b1 = rb[i * 3 + 1];
												byte b2 = rb[i * 3 + 2];

												const int hiBit = 1 << 23;
												const int negFlags = hiBit - 1;
												int v = (b0 << 0) | (b1 << 8) | (b2 << 16);

												if((hiBit & v) == 0)
													rf[i] = (float)(v - 1) / maxSigned24;
												else
													rf[i] = (float)(-(~v & negFlags) - 1) / maxSigned24;
											}
											ret.Add(ac);
										}
										break;

									case 32:
										{
											int sct = ct.size / 4;
											float[] rf = new float[sct];
											ac.channels = new List<float[]>();
											ac.channels.Add(rf);
											if(fmt.Value.compressionCode == (int)CompressionCode.Float)
											{ 
												System.Buffer.BlockCopy(rb, 0, rf, 0, ct.size);
											}
											else
											{
												
												for (int i = 0; i < sct; ++i)
													rf[i] = (float)System.BitConverter.ToInt32(rb, i * 4) / (float)int.MaxValue;

											}
											ret.Add(ac);
										}
										break;

									case 64:
										{
											int sct = ct.size / 8;
											float[] rf = new float[sct];
											ac.channels = new List<float[]>();
											ac.channels.Add(rf);
											if (fmt.Value.compressionCode == (int)CompressionCode.Float)
											{
												for (int i = 0; i < sct; ++i)
													rf[i] = (float)System.BitConverter.ToDouble(rb, i * 8);
											}
											else
											{

												for (int i = 0; i < sct; ++i)
													rf[i] = (float)System.BitConverter.ToInt64(rb, i * 8) / (float)long.MaxValue;

											}
											ret.Add(ac);
										}
										break;

									default:
										continue;
								}
							}
							else
							{
								// UNIMPLEMENTED
							}
						}

						break;

					case ChunkID.wavl:
						if (fmt.HasValue == false)
							continue;

						r.BaseStream.Seek(ct.filePos, System.IO.SeekOrigin.Begin);

						List<ChunkTable> alternatingDataChunks = new List<ChunkTable>();
						ParseBoundedStaticChunksInto(r, alternatingDataChunks, ct.filePos + ct.size);

						// Recursion
						_ProcessStaticChunksForPCM(r, alternatingDataChunks, ret, ref fmt, ref endi);
						break;

					case ChunkID.slnt:
						if (fmt.HasValue == false)
							continue;

						uint silentSamples = r.ReadUInt32();
						ret.Add(new AudioChunk(silentSamples));
						break;
				}
			}
		}

		public static float []  GetChannel(List<PxPre.Vinyl.Wav.AudioChunk> sections, int channel)
		{ 
			// Cheapeast and most probable situation
			if(sections.Count == 1 && channel == 0)
			{ 
				if(sections[0].channels != null)
				{
					return sections[0].channels[0];
				}
				else if(sections[0].silentSamples != 0)
				{ 
					float [] ret = new float[sections[0].silentSamples];
					for(int i = 0; i < ret.Length; ++i)
						ret[i] = 0.0f;

					return ret;
				}

				return new float[]{0.0f};
			}

			int ct = 0;
			foreach(AudioChunk ac in sections)
			{ 
				if(ac.channels != null)
					ct += ac.channels[channel].Length;
				else
					ct += (int)ac.silentSamples;
			}

			float [] stitchRet = new float[ct];
			int offset = 0;
			foreach(AudioChunk ac in sections)
			{
				if(ac.channels != null)
				{
					float [] data = ac.channels[channel];
					System.Buffer.BlockCopy(data, 0, stitchRet, offset, data.Length * 4);
					offset += data.Length * 4;
				}
				else
				{ 
					int end = offset + (int)ac.silentSamples;
					for(; offset < end; ++offset)
						stitchRet[offset] = 0.0f;
				}
			}

			return stitchRet;
		}

		private static void _WriteHeader(
			System.IO.BinaryWriter writer, 
			CompressionCode compression, 
			int sampleBytes,
			int sampleRate, 
			int channels, 
			int datasize)
		{
			// http://soundfile.sapp.org/doc/WaveFormat/
			writer.Write((int)ChunkID.RIFF);
			writer.Write(36 + datasize);
			writer.Write((int)ChunkID.WAVE);

			writer.Write((int)ChunkID.fmt);
			writer.Write(16);
			writer.Write((ushort)compression);
			writer.Write((ushort)channels);
			writer.Write(sampleRate);
			writer.Write(sampleRate * channels * sampleBytes);
			writer.Write((short)(channels * sampleBytes));
			writer.Write((short)(sampleBytes * 8));

			writer.Write((int)ChunkID.data);
			writer.Write(datasize);

			// The invoking caller is charged with filling the rest of the 
			// content with PCM data
		}

		public static void CreateSimpleWavBinary(System.IO.BinaryWriter writer, byte[] pcm, int sampleRate, int channels)
		{
			_WriteHeader(writer, CompressionCode.PCM, 1, sampleRate, channels, pcm.Length);
			writer.Write(pcm);
		}

		public static void CreateSimpleWavBinary(System.IO.BinaryWriter writer, short [] pcm, int sampleRate, int channels)
		{
			_WriteHeader(writer, CompressionCode.PCM, 2, sampleRate, channels, pcm.Length * 2);

			byte [] rb = new byte[pcm.Length * 2];
			System.Buffer.BlockCopy(pcm, 0, rb, 0, pcm.Length * 2);
			writer.Write(rb);
		}

		public static void CreateSimpleWavBinary(System.IO.BinaryWriter writer, int[] pcm, int sampleRate, int channels)
		{
			_WriteHeader(writer, CompressionCode.PCM, 4, sampleRate, channels, pcm.Length * 4);

			byte[] rb = new byte[pcm.Length * 4];
			System.Buffer.BlockCopy(pcm, 0, rb, 0, pcm.Length * 4);
			writer.Write(rb);
		}

		public static void CreateSimpleWavBinary(System.IO.BinaryWriter writer, float [] pcm, int sampleRate, int channels)
		{
			_WriteHeader(writer, CompressionCode.Float, 4, sampleRate, channels, pcm.Length * 4);

			byte[] rb = new byte[pcm.Length * 4];
			System.Buffer.BlockCopy(pcm, 0, rb, 0, pcm.Length * 4);
			writer.Write(rb);
		}

		public static byte [] CreateSimpleWaveBytes(float[] pcm, int sampleRate, int channels)
		{ 
			byte [] rb = new byte[44 + pcm.Length * 4];

			System.IO.MemoryStream ms = new System.IO.MemoryStream(rb);
			System.IO.BinaryWriter w = new System.IO.BinaryWriter(ms);
			CreateSimpleWavBinary(w, pcm, sampleRate, channels);

			return rb;

		}

		public static void CreateSimpleWavBinary(System.IO.BinaryWriter writer, double[] pcm, int sampleRate, int channels)
		{
			_WriteHeader(writer, CompressionCode.Float, 8, sampleRate, channels, pcm.Length * 8);

			byte[] rb = new byte[pcm.Length * 8];
			System.Buffer.BlockCopy(pcm, 0, rb, 0, pcm.Length * 8);
			writer.Write(rb);
		}

	}
}