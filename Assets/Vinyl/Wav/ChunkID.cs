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

namespace PxPre.Vinyl.Wav
{
	public enum ChunkID
	{
		// How I wish I had macros or const compile time functions...
		RIFF	= ('R' << 0) | ('I' << 8) | ('F' << 16) | ('F' << 24),	// WAV file header,  little endian data.
		RIFX	= ('R' << 0) | ('I' << 8) | ('F' << 16) | ('X' << 24),	// WAV file header, big endian data.
		WAVE	= ('W' << 0) | ('A' << 8) | ('V' << 16) | ('E' << 24),	// WAV format id
		data	= ('d' << 0) | ('a' << 8) | ('t' << 16) | ('a' << 24),	// WAV PCM chunk
		fmt		= ('f' << 0) | ('m' << 8) | ('t' << 16) | (' ' << 24),  // Format description
		fact	= ('f' << 0) | ('a' << 8) | ('c' << 16) | ('t' << 24),  // Compression data
		wavl	= ('w' << 0) | ('a' << 8) | ('v' << 16) | ('l' << 24),  // Alternating silence and PCM
		slnt	= ('s' << 0) | ('l' << 8) | ('n' << 16) | ('t' << 24),  // Silent chunk
		cue		= ('c' << 0) | ('u' << 8) | ('e' << 16) | (' ' << 24),  // Annotation data
		plst	= ('p' << 0) | ('l' << 8) | ('s' << 16) | ('t' << 24),  // List of cues
		list	= ('l' << 0) | ('i' << 8) | ('s' << 16) | ('t' << 24),  // An associated data list chunk is used to define text labels and names
		adtl	= ('l' << 0) | ('i' << 8) | ('s' << 16) | ('t' << 24),  // Constant used for list chunk
		labl	= ('l' << 0) | ('a' << 8) | ('b' << 16) | ('l' << 24),  // The label chunk is always contained inside of an associated data list chunk.
		ltxt	= ('l' << 0) | ('t' << 8) | ('x' << 16) | ('t' << 24),  // The labeled text chunk is always contained inside of an associated data list chunk.
		note	= ('n' << 0) | ('o' << 8) | ('t' << 16) | ('e' << 24),  // The label chunk is always contained inside of an associated data list chunk
		smpl	= ('s' << 0) | ('m' << 8) | ('p' << 16) | ('l' << 24),  // Metadata for music hardware
		inst	= ('i' << 0) | ('n' << 8) | ('s' << 16) | ('t' << 24),  // Data to describe the waveform as an instrument note
		id3		= ('i' << 0) | ('d' << 8) | ('3' << 16) | (' ' << 24)   // MP3 song metadata
	}
}