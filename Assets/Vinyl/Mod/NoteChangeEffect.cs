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
    /// 
    /// </summary>
    public struct NoteChangeEffect
    {
        /// <summary>
        /// Various command messages.
        /// </summary>
        public enum Command
        {
            /// <summary>
            /// Continue playing
            /// </summary>
            Continue,

            /// <summary>
            /// Stop the song.
            /// </summary>
            Stop,
            JumpToPattern,
            BreakPattern,
            DelayPattern,
            SetBPM,         // Set Beats per minute
            SetTPD,         // Set Ticks per division
        }

        /// <summary>
        /// The notification to the manager.
        /// </summary>
        public Command cmd;

        /// <summary>
        /// The command parameter.
        /// </summary>
        public int param;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cmd">The notification command.</param>
        public NoteChangeEffect(Command cmd)
        {
            this.cmd = cmd;
            this.param = 0;
        }

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="cmd">The notification command.</param>
        /// <param name="param">The notification command parameter.</param>
        public NoteChangeEffect(Command cmd, int param)
        {
            this.cmd = cmd;
            this.param = param;
        }

        /// <summary>
        /// Create a default Continue NoteChangeEffect.
        /// </summary>
        /// <returns>A Continue NoteChangeEffect.</returns>
        public static NoteChangeEffect Continue()
        {
            return new NoteChangeEffect(Command.Continue);
        }

        /// <summary>
        /// Create Stop NoteChangeEffect.
        /// </summary>
        /// <returns>A Stop NoteChangeEffect.</returns>
        public static NoteChangeEffect Stop()
        {
            return new NoteChangeEffect(Command.Stop);
        }
    }
}
