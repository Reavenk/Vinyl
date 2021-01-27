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

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Vinyl.Mod
{
    /// <summary>
    /// Effect values for the various mod effects.
    /// 
    /// Effects involving two nibbles are bit-merged together.
    /// 
    /// Behaviour spec at http://www.aes.id.au/modformat.html
    /// </summary>
    public enum Effect
    {
        None                    = -1, // Not in file format, used for state tracking
        Arpegio                 = 0,
        SlideUp                 = 1,
        SlideDown               = 2,
        SlideNote               = 3,
        Vibrato                 = 4,
        SlideCont               = 5,
        VibratoCont             = 6,
        Tremolo                 = 7,
        SetPan                  = 8,
        SetSampleOffset         = 9,
        VolumeSlide             = 10,
        PositionJump            = 11,
        SetVolume               = 12,
        PatternBreak            = 13,
        SetFilter               = (14 << 8) | (0),
        SetFinSlideUp           = (14 << 8) | (1),
        SetFinSlideDown         = (14 << 8) | (2),
        SetGlissando            = (14 << 8) | (3),
        SetVibratoForm          = (14 << 8) | (4),
        SetFinetune             = (14 << 8) | (5),
        LoopPattern             = (14 << 8) | (6),
        SetTremoloForm          = (14 << 8) | (7),
        Unused                  = (14 << 8) | (8),
        Retrigger               = (14 << 8) | (9),
        FineVolumeUp            = (14 << 8) | (10),
        FineVolumeDown          = (14 << 8) | (11),
        CutSample               = (14 << 8) | (12),
        DelaySample             = (14 << 8) | (13),
        DelayPattern            = (14 << 8) | (14),
        InvertLoop              = (14 << 8) | (15),
        SetSpeed                = 15
    }
}
