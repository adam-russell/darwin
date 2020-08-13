// Code is based on example from 29AjayKumar:
// https://www.geeksforgeeks.org/how-to-check-if-a-given-point-lies-inside-a-polygon/

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
using System.Text;

namespace Darwin.Helpers
{
    public static class PolygonHelper
    {
        private const int LargeNumber = 10000000;

        // Given three colinear points p, q, r,  
        // the function checks if point q lies 
        // on line segment 'pr' 
        private static bool onSegment(Point p, Point q, Point r)
        {
            if (q.X <= Math.Max(p.X, r.X) &&
                q.X >= Math.Min(p.X, r.X) &&
                q.X <= Math.Max(p.Y, r.Y) &&
                q.X >= Math.Min(p.Y, r.Y))
            {
                return true;
            }

            return false;
        }

        // To find orientation of ordered triplet (p, q, r). 
        // The function returns following values 
        // 0 --> p, q and r are colinear 
        // 1 --> Clockwise 
        // 2 --> Counterclockwise 
        private static int Orientation(Point p, Point q, Point r)
        {
            int val = (q.Y - p.Y) * (r.X - q.X) -
                      (q.X - p.X) * (r.Y- q.Y);

            if (val == 0)
                return 0; // colinear 

            return (val > 0) ? 1 : 2; // clock or counterclock wise 
        }

        // The function that returns true if  
        // line segment 'p1q1' and 'p2q2' intersect. 
        public static bool DoLinesIntersect(Point p1, Point q1,
                                Point p2, Point q2)
        {
            // Find the four orientations needed for  
            // general and special cases 
            int o1 = Orientation(p1, q1, p2);
            int o2 = Orientation(p1, q1, q2);
            int o3 = Orientation(p2, q2, p1);
            int o4 = Orientation(p2, q2, q1);

            // General case 
            if (o1 != o2 && o3 != o4)
                return true;

            // Special Cases 
            // p1, q1 and p2 are colinear and 
            // p2 lies on segment p1q1 
            if (o1 == 0 && onSegment(p1, p2, q1))
                return true;

            // p1, q1 and p2 are colinear and 
            // q2 lies on segment p1q1 
            if (o2 == 0 && onSegment(p1, q2, q1))
                return true;

            // p2, q2 and p1 are colinear and 
            // p1 lies on segment p2q2 
            if (o3 == 0 && onSegment(p2, p1, q2))
                return true;

            // p2, q2 and q1 are colinear and 
            // q1 lies on segment p2q2 
            if (o4 == 0 && onSegment(p2, q1, q2))
                return true;

            // Doesn't fall in any of the above cases 
            return false;
        }

        // Returns true if the point p lies inside the polygon[] with n vertices 
        public static bool IsInsidePolygon(List<Point> polygon, Point p)
        {
            // There must be at least 3 vertices in polygon
            if (polygon.Count < 3)
                return false;

            // Create a point for line segment from p to "infinite"
            Point extreme = new Point(LargeNumber, p.Y);

            // Count intersections of the above line with sides of polygon 
            int count = 0, i = 0;
            do
            {
                int next = (i + 1) % polygon.Count;

                // Check if the line segment from 'p' to  
                // 'extreme' intersects with the line  
                // segment from 'polygon[i]' to 'polygon[next]' 
                if (DoLinesIntersect(polygon[i],
                                polygon[next], p, extreme))
                {
                    // If the point 'p' is colinear with line  
                    // segment 'i-next', then check if it lies  
                    // on segment. If it lies, return true, otherwise false 
                    if (Orientation(polygon[i], p, polygon[next]) == 0)
                    {
                        return onSegment(polygon[i], p,
                                         polygon[next]);
                    }
                    count++;
                }
                i = next;
            } while (i != 0);

            // Return true if count is odd, false otherwise
            return count % 2 == 1;
        }
    }
}
