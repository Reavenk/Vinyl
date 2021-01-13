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

// https://id3.org/
namespace PxPre.Vinyl.Meta
{
	// https://en.wikipedia.org/wiki/ID3
	public struct ID3
	{
		public string tag;
		public string title;
		public string artist;
		public string album;
		public string year;
		public string comment;
		public int track;
		public Genre genre;


		public bool Read(System.IO.BinaryReader r)
		{ 
			this.tag = System.Text.Encoding.ASCII.GetString(r.ReadBytes(3));

			if(tag != "TAG")
				return false;

			this.title = System.Text.Encoding.ASCII.GetString(r.ReadBytes(30));
			this.artist = System.Text.Encoding.ASCII.GetString(r.ReadBytes(30));
			this.album = System.Text.Encoding.ASCII.GetString(r.ReadBytes(30));
			this.year = System.Text.Encoding.ASCII.GetString(r.ReadBytes(4));

			byte [] rb = r.ReadBytes(30);
			if(rb[28] == 0)
			{
				this.comment = System.Text.Encoding.ASCII.GetString(rb);
				this.track = -1;
			}
			else
			{ 
				this.comment = System.Text.Encoding.ASCII.GetString(rb, 0, 28);
				this.track = (int)rb[29];
			}

			this.genre = (Genre)r.ReadByte();

			return true;
		}
	}
}
