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

using System.Collections.Generic;

// https://id3.org/
namespace PxPre.Vinyl.Meta
{
	// https://en.wikipedia.org/wiki/ID3
	public struct ID3_2
	{
		[System.Flags]
		public enum Flags
		{ 
			Experimental = 1 << 5,
			Extended = 1 << 6,
			Unsync = 1 << 7
		}

		public struct Frame
		{ 
			public int id;
			public int size;
			public short flags;
			public long pos;
		}

		public enum FrameType
		{
			// https://id3.org/id3v2.3.0#ISO-639-2

			AENC = ('A' << 0) | ('E' << 8) | ('N' << 16) | ( 'C' << 24), //Audio encryption
			APIC = ('A' << 0) | ('P' << 8) | ('I' << 16) | ( 'C' << 24), //Attached picture
			COMM = ('C' << 0) | ('O' << 8) | ('M' << 16) | ( 'M' << 24), //Comments
			COMR = ('C' << 0) | ('O' << 8) | ('M' << 16) | ( 'R' << 24), //Commercial frame
			ENCR = ('E' << 0) | ('N' << 8) | ('C' << 16) | ( 'R' << 24), //Encryption method registration
			EQUA = ('E' << 0) | ('Q' << 8) | ('U' << 16) | ( 'A' << 24), //Equalization
			ETCO = ('E' << 0) | ('T' << 8) | ('C' << 16) | ( 'O' << 24), //Event timing codes
			GEOB = ('G' << 0) | ('E' << 8) | ('O' << 16) | ( 'B' << 24), //General encapsulated object
			GRID = ('G' << 0) | ('R' << 8) | ('I' << 16) | ( 'D' << 24), //Group identification registration
			IPLS = ('I' << 0) | ('P' << 8) | ('L' << 16) | ( 'S' << 24), //Involved people list
			LINK = ('L' << 0) | ('I' << 8) | ('N' << 16) | ( 'K' << 24), //Linked information
			MCDI = ('M' << 0) | ('C' << 8) | ('D' << 16) | ( 'I' << 24), //Music CD identifier
			MLLT = ('M' << 0) | ('L' << 8) | ('L' << 16) | ( 'T' << 24), //MPEG location lookup table
			OWNE = ('O' << 0) | ('W' << 8) | ('N' << 16) | ( 'E' << 24), //Ownership frame
			PRIV = ('P' << 0) | ('R' << 8) | ('I' << 16) | ( 'V' << 24), //Private frame
			PCNT = ('P' << 0) | ('C' << 8) | ('N' << 16) | ( 'T' << 24), //Play counter
			POPM = ('P' << 0) | ('O' << 8) | ('P' << 16) | ( 'M' << 24), //Popularimeter
			POSS = ('P' << 0) | ('O' << 8) | ('S' << 16) | ( 'S' << 24), //Position synchronisation frame
			RBUF = ('R' << 0) | ('B' << 8) | ('U' << 16) | ( 'F' << 24), //Recommended buffer size
			RVAD = ('R' << 0) | ('V' << 8) | ('A' << 16) | ( 'D' << 24), //Relative volume adjustment
			RVRB = ('R' << 0) | ('V' << 8) | ('R' << 16) | ( 'B' << 24), //Reverb
			SYLT = ('S' << 0) | ('Y' << 8) | ('L' << 16) | ( 'T' << 24), //Synchronized lyric/text
			SYTC = ('S' << 0) | ('Y' << 8) | ('T' << 16) | ( 'C' << 24), //Synchronized tempo codes
			TALB = ('T' << 0) | ('A' << 8) | ('L' << 16) | ( 'B' << 24), //Album/Movie/Show title
			TBPM = ('T' << 0) | ('B' << 8) | ('P' << 16) | ( 'M' << 24), //BPM (beats per minute)
			TCOM = ('T' << 0) | ('C' << 8) | ('O' << 16) | ( 'M' << 24), //Composer
			TCON = ('T' << 0) | ('C' << 8) | ('O' << 16) | ( 'N' << 24), //Content type
			TCOP = ('T' << 0) | ('C' << 8) | ('O' << 16) | ( 'P' << 24), //Copyright message
			TDAT = ('T' << 0) | ('D' << 8) | ('A' << 16) | ( 'T' << 24), //Date
			TDLY = ('T' << 0) | ('D' << 8) | ('L' << 16) | ( 'Y' << 24), //Playlist delay
			TENC = ('T' << 0) | ('E' << 8) | ('N' << 16) | ( 'C' << 24), //Encoded by
			TEXT = ('T' << 0) | ('E' << 8) | ('X' << 16) | ( 'T' << 24), //Lyricist/Text writer
			TFLT = ('T' << 0) | ('F' << 8) | ('L' << 16) | ( 'T' << 24), //File type
			TIME = ('T' << 0) | ('I' << 8) | ('M' << 16) | ( 'E' << 24), //Time
			TIT1 = ('T' << 0) | ('I' << 8) | ('T' << 16) | ( '1' << 24), //Content group description
			TIT2 = ('T' << 0) | ('I' << 8) | ('T' << 16) | ( '2' << 24), //Title/songname/content description
			TIT3 = ('T' << 0) | ('I' << 8) | ('T' << 16) | ( '3' << 24), //Subtitle/Description refinement
			TKEY = ('T' << 0) | ('K' << 8) | ('E' << 16) | ( 'Y' << 24), //Initial key
			TLAN = ('T' << 0) | ('L' << 8) | ('A' << 16) | ( 'N' << 24), //Language(s)
			TLEN = ('T' << 0) | ('L' << 8) | ('E' << 16) | ( 'N' << 24), //Length
			TMED = ('T' << 0) | ('M' << 8) | ('E' << 16) | ( 'D' << 24), //Media type
			TOAL = ('T' << 0) | ('O' << 8) | ('A' << 16) | ( 'L' << 24), //Original album/movie/show title
			TOFN = ('T' << 0) | ('O' << 8) | ('F' << 16) | ( 'N' << 24), //Original filename
			TOLY = ('T' << 0) | ('O' << 8) | ('L' << 16) | ( 'Y' << 24), //Original lyricist(s)/text writer(s)
			TOPE = ('T' << 0) | ('O' << 8) | ('P' << 16) | ( 'E' << 24), //Original artist(s)/performer(s)
			TORY = ('T' << 0) | ('O' << 8) | ('R' << 16) | ( 'Y' << 24), //Original release year
			TOWN = ('T' << 0) | ('O' << 8) | ('W' << 16) | ( 'N' << 24), //File owner/licensee
			TPE1 = ('T' << 0) | ('P' << 8) | ('E' << 16) | ( '1' << 24), //Lead performer(s)/Soloist(s)
			TPE2 = ('T' << 0) | ('P' << 8) | ('E' << 16) | ( '2' << 24), //Band/orchestra/accompaniment
			TPE3 = ('T' << 0) | ('P' << 8) | ('E' << 16) | ( '3' << 24), //Conductor/performer refinement
			TPE4 = ('T' << 0) | ('P' << 8) | ('E' << 16) | ( '4' << 24), //Interpreted, remixed, or otherwise modified by
			TPOS = ('T' << 0) | ('P' << 8) | ('O' << 16) | ( 'S' << 24), //Part of a set
			TPUB = ('T' << 0) | ('P' << 8) | ('U' << 16) | ( 'B' << 24), //Publisher
			TRCK = ('T' << 0) | ('R' << 8) | ('C' << 16) | ( 'K' << 24), //Track number/Position in set
			TRDA = ('T' << 0) | ('R' << 8) | ('D' << 16) | ( 'A' << 24), //Recording dates
			TRSN = ('T' << 0) | ('R' << 8) | ('S' << 16) | ( 'N' << 24), //Internet radio station name
			TRSO = ('T' << 0) | ('R' << 8) | ('S' << 16) | ( 'O' << 24), //Internet radio station owner
			TSIZ = ('T' << 0) | ('S' << 8) | ('I' << 16) | ( 'Z' << 24), //Size
			TSRC = ('T' << 0) | ('S' << 8) | ('R' << 16) | ( 'C' << 24), //ISRC (international standard recording code)
			TSSE = ('T' << 0) | ('S' << 8) | ('S' << 16) | ( 'E' << 24), //Software/Hardware and settings used for encoding
			TYER = ('T' << 0) | ('Y' << 8) | ('E' << 16) | ( 'R' << 24), //Year
			TXXX = ('T' << 0) | ('X' << 8) | ('X' << 16) | ( 'X' << 24), //User defined text information frame
			UFID = ('U' << 0) | ('F' << 8) | ('I' << 16) | ( 'D' << 24), //1 Unique file identifier
			USER = ('U' << 0) | ('S' << 8) | ('E' << 16) | ( 'R' << 24), //23 Terms of use
			USLT = ('U' << 0) | ('S' << 8) | ('L' << 16) | ( 'T' << 24), //9 Unsychronized lyric/text transcription
			WCOM = ('W' << 0) | ('C' << 8) | ('O' << 16) | ( 'M' << 24), //Commercial information
			WCOP = ('W' << 0) | ('C' << 8) | ('O' << 16) | ( 'P' << 24), //Copyright/Legal information
			WOAF = ('W' << 0) | ('O' << 8) | ('A' << 16) | ( 'F' << 24), //Official audio file webpage
			WOAR = ('W' << 0) | ('O' << 8) | ('A' << 16) | ( 'R' << 24), //Official artist/performer webpage
			WOAS = ('W' << 0) | ('O' << 8) | ('A' << 16) | ( 'S' << 24), //Official audio source webpage
			WORS = ('W' << 0) | ('O' << 8) | ('R' << 16) | ( 'S' << 24), //Official internet radio station homepage
			WPAY = ('W' << 0) | ('P' << 8) | ('A' << 16) | ( 'Y' << 24), //Payment
			WPUB = ('W' << 0) | ('P' << 8) | ('U' << 16) | ( 'B' << 24), //Publishers official webpage
			WXXX = ('W' << 0) | ('X' << 8) | ('X' << 16) | ( 'X' << 24),  //User defined URL link frame
			TDRC = ('T' << 0) | ('D' << 8) | ('R' << 16) | ( 'C' << 24)  //User defined URL link frame
		}

		public string identifier;
		public byte majorVersion;
		public byte minorVersion;
		public byte flags;
		public int size;

		public List<Frame> Read(System.IO.BinaryReader r)
		{
			List <Frame> ret = new List<Frame>();

			this.identifier = System.Text.Encoding.ASCII.GetString(r.ReadBytes(3));
			if(this.identifier != "ID3")
				return ret;

			this.majorVersion = r.ReadByte();
			this.minorVersion = r.ReadByte();

			this.flags = r.ReadByte();

			// They're doing some weird 7 bits per byte shenangianfoolery
			byte sz0 = r.ReadByte();
			byte sz1 = r.ReadByte();
			byte sz2 = r.ReadByte();
			byte sz3 = r.ReadByte();
			this.size = (sz0 << 21) | (sz1 << 14) | (sz2 << 7) | (sz3 << 0);


			long startPos = r.BaseStream.Position;
			long endPos = r.BaseStream.Position + this.size;

			if((this.flags & (byte)Flags.Experimental) != 0)
			{ 
				const ushort CRCDataPresentFlag = 0x8000;

				uint extSz = r.ReadUInt32();
				short extFlags = r.ReadInt16();
				uint padding = r.ReadUInt32();

				if((extFlags & CRCDataPresentFlag) != 0)
				{ 
					// Consume the CRC
					uint crc = r.ReadUInt32();
				}
			}

			while(r.BaseStream.Position < endPos)
			{
				Frame frame = new Frame();
				frame.id = r.ReadInt32();
				frame.size = ReversedEndianInt32(r);
				frame.flags = r.ReadInt16();
				frame.pos = r.BaseStream.Position;

				ret.Add(frame);
				r.BaseStream.Seek(frame.pos + frame.size, System.IO.SeekOrigin.Begin);
			}

			return ret;
		}

		public ID3 ReadToID31(System.IO.BinaryReader r)
		{
			List<Frame> lst = Read(r);

			ID3 ret = new ID3();
			ret.tag = "ID3v2";

			foreach(Frame f in lst)
			{ 
				switch((FrameType)f.id)
				{ 
					case FrameType.TPE1:
					case FrameType.TPE2:
					case FrameType.TPE3:
					case FrameType.TPE4:
						r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						ret.artist = ReadID3String(r, f.size);
						break;

					case FrameType.TIT1:
					case FrameType.TIT2:
					case FrameType.TIT3:
						r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						ret.title = ReadID3String(r, f.size);
						break;

					case FrameType.COMM:
						r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						ret.comment = ReadID3String(r, f.size);
						break;

					case FrameType.TALB:
						r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						ret.album = ReadID3String(r, f.size);
						break;

					case FrameType.TRCK:
						r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						ret.track = r.ReadByte();
						break;

					case FrameType.TCON: // We currenty don't have inverse mapping for the origin
						// r.BaseStream.Seek(f.pos, System.IO.SeekOrigin.Begin);
						// MISSING
						break;
				}
			}

			return ret;
		}

		public static int ReversedEndianInt32(System.IO.BinaryReader r)
		{
			byte b0 = r.ReadByte();
			byte b1 = r.ReadByte();
			byte b2 = r.ReadByte();
			byte b3 = r.ReadByte();

			return (b0 << 24) | (b1 << 16) | (b2 << 8) | (b3 << 0);
		}

		public static string ReadID3String(System.IO.BinaryReader r, int size)
		{
			// TODO: String parsing isn't rigerous, just assumes
			// ASCII format
			//
			// consume a byte. I think the first byte is the string type, but we're
			// assuming ASCII for now.
			r.ReadByte();

			return System.Text.Encoding.ASCII.GetString(r.ReadBytes(size-1));
		}
	}
}