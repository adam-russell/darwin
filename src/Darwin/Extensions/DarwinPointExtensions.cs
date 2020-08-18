﻿// This file is part of DARWIN.
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
using System.Text;

namespace Darwin.Extensions
{
    public static class DarwinPointExtensions
    {
        public static System.Drawing.Point ToDrawingPoint(this Darwin.Model.Point point)
        {
            return new System.Drawing.Point(point.X, point.Y);
        }

        public static Darwin.Model.Point ToDarwinPoint(this System.Drawing.Point point)
        {
            return new Darwin.Model.Point(point.X, point.Y);
        }
    }
}
