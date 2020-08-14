﻿//*******************************************************************
//   file: ImageMod.cxx
//
// author: J H Stewman (1/24/2007)
//
//   mods: 
//
// contains classes for keeping track of applied image modifications
//
// used in TraceWindow to build list of modifications applied to
// original image in preparation for tracing, or to reproduce
// same sequence when loading a previsously traced and saved fin
//
// used in MatchResultsWindow when loading results or changing 
// selected fin, so that the modified image can be recreated from
// the original for both selected and unknown fins
//
//*******************************************************************

// This file is part of DARWIN.
// Copyright (C) 1994 - 2020
//
// DARWIN is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// DARWIN is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with DARWIN.  If not, see<https://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Darwin.Database
{
    public enum ImageModType
    {
        IMG_none = 0,
        IMG_flip = 1,
        IMG_contrast = 2,
        IMG_brighten = 3,
        IMG_crop = 4,
        IMG_undo = 5,
        IMG_redo = 6,
        IMG_contrast2 = 7,
        IMG_rotate90cw = 8,
        IMG_rotate90ccw = 9
    }

    public class ImageMod
    {
        public ImageModType Op { get; set; }              // image modification type
        private int
            min, max,        // values used in contrast modification
            amount,          // amount adjusted +/- for brightness adjustment
            xMin, yMin,      // boundaries for cropping
            xMax, yMax;      // ditto

        public ImageMod(ImageMod modToCopy)
        {
            Op = modToCopy.Op;
            min = modToCopy.min;
            max = modToCopy.max;
            amount = modToCopy.amount;
            xMin = modToCopy.xMin;
            yMin = modToCopy.yMin;
            xMax = modToCopy.xMax;
            yMax = modToCopy.yMax;
        }

        // the values are used depending on the ImageModtype
        // op == IMAG_flip, no values used
        // op == IMG_contrast, min is val1, and max is val2
        // op == IMG_contrast2, level is val1
        // op == IMG_brighten, amount is val1
        // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
        // op == IMG_undo, no values used
        // op == IMG_redo, no values used
        public ImageMod(ImageModType op, int val1 = 0, int val2 = 0, int val3 = 0, int val4 = 0)
        {
            // the values are used depending on the ImageModtype
            Op = op;
            if (ImageModType.IMG_flip == op
                || ImageModType.IMG_rotate90cw == op
                || ImageModType.IMG_rotate90ccw == op
                || ImageModType.IMG_undo == op
                || ImageModType.IMG_redo == op
                || ImageModType.IMG_none == op)
            {
                // op == IMAG_flip, IMG_undo, or IMG_redo ... then no values used
            }
            else if (ImageModType.IMG_contrast == op)
            {
                // op == IMG_contrast, min is val1, and max is val2
                min = val1;
                max = val2;
            }
            else if (ImageModType.IMG_brighten == op || ImageModType.IMG_contrast2 == op)
            {
                // op == IMG_brighten, amount is val1
                amount = val1;
            }
            else if (ImageModType.IMG_crop == op)
            {
                // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
                xMin = val1;
                yMin = val2;
                xMax = val3;
                yMax = val4;
            }
            else
                Op = ImageModType.IMG_none;
        }


        // the values are used depending on the ImageModtype
        // op == IMAG_flip, no values used
        // op == IMG_contrast, min is val1, and max is val2
        // op == IMG_brighten, amount is val1
        // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
        // op == IMG_undo, no values used
        // op == IMG_redo, no values used
        public void Set(ImageModType op, int val1 = 0, int val2 = 0, int val3 = 0, int val4 = 0)
        {
            // the values are used depending on the ImageModtype
            Op = op;
            if (ImageModType.IMG_flip == op
                || ImageModType.IMG_rotate90cw == op
                || ImageModType.IMG_rotate90ccw == op
                || ImageModType.IMG_undo == op
                || ImageModType.IMG_redo == op)
            {
                // op == IMAG_flip, IMG_undo, or IMG_redo ... then no values used
                min = max = amount = xMin = yMin = xMax = yMax = 0;
            }
            else if (ImageModType.IMG_contrast == op)
            {
                // op == IMG_contrast, min is val1, and max is val2
                min = val1;
                max = val2;
                amount = xMin = yMin = xMax = yMax = 0;
            }
            else if (ImageModType.IMG_brighten == op || ImageModType.IMG_contrast2 == op)
            {
                // op == IMG_brighten, amount is val1
                amount = val1;
                min = max = xMin = yMin = xMax = yMax = 0;
            }
            else if (ImageModType.IMG_crop == op)
            {
                // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
                xMin = val1;
                yMin = val2;
                xMax = val3;
                yMax = val4;
                min = max = amount = 0;
            }
            else
                Op = ImageModType.IMG_none;
        }

        // the values are used depending on the ImageModtype
        // op == IMAG_flip, no values used
        // op == IMG_contrast, min is val1, and max is val2
        // op == IMG_brighten, amount is val1
        // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
        // op == IMG_undo, no values used
        // op == IMG_redo, no values used
        public void Get(out ImageModType op, out int val1, out int val2, out int val3, out int val4)
        {
            val1 = val2 = val3 = val4 = 0;
            // the values are used depending on the ImageModtype
            op = Op;
            if (ImageModType.IMG_flip == op
                || ImageModType.IMG_rotate90cw == op
                || ImageModType.IMG_rotate90ccw == op
                || ImageModType.IMG_undo == op
                || ImageModType.IMG_redo == op
                || ImageModType.IMG_none == op)
            {
                // op == IMAG_flip, IMG_undo, or IMG_redo ... then no values used
                val1 = val2 = val3 = val4 = 0;
            }
            else if (ImageModType.IMG_contrast == op)
            {
                // op == IMG_contrast, min is val1, and max is val2
                val1 = min;
                val2 = max;
                val3 = val4 = 0;
            }
            else if (ImageModType.IMG_brighten == op || ImageModType.IMG_contrast2 == op)
            {
                // op == IMG_brighten, amount is val1
                val1 = amount;
                val2 = val3 = val4 = 0;
            }
            else if (ImageModType.IMG_crop == op)
            {
                // op == IMG_crop, xMin is val1, yMin is val2, xMax is val3, yMax is val4
                val1 = xMin;
                val2 = yMin;
                val3 = xMax;
                val4 = yMax;
            }
            else
                Trace.WriteLine("error in modList::get()"); // shouldn't get here
        }
    }
}
