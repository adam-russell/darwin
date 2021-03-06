﻿//*******************************************************************
//   file: Contour.cxx
//
// author: Adam Russell
//
//   mods: J H Stewman (4/12/2005)
//         -- reformatting of code and addition of comment blocks
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

using Darwin.Collections;
using Darwin.ML.Model;
using Darwin.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;

using Point = Darwin.Model.Point;

namespace Darwin.Model
{
    public class Contour : INotifyPropertyChanged
    {
		public const int MaxTurnAngle = 150;

		private double _scale;
		public double Scale
        {
			get { return _scale; }
			set
            {
				_scale = value;
				RaisePropertyChanged("Scale");
			}
        }

        private ObservableNotifiableCollection<Point> _points;
        public ObservableNotifiableCollection<Point> Points
        {
            get
            {
                if (_points == null)
                    _points = new ObservableNotifiableCollection<Point>();

                return _points;
            }
            set
            {
                _points = value;
				RaisePropertyChanged("Points");
			}
        }

		public Point this[int i]
		{
			// Routing through "Points" rather than "_points" so we have the null check
			get
			{
				return Points[i];
			}
			set
			{
				Points[i] = value;
				RaisePropertyChanged("Points");
			}
		}

		public int NumPoints
		{
			// Routing through "Points" rather than "_points" so we have the null check
			get
			{
				return Points.Count;
			}
		}

		public int Length
		{
			// Routing through "Points" rather than "_points" so we have the null check
			get
			{
				return Points.Count;
			}
		}

		private double _xoffset;
		public double XOffset
        {
			get => _xoffset;
			set
            {
				_xoffset = value;
				RaisePropertyChanged("XOffset");
            }
        }

		private double _yoffset;
		public double YOffset
		{
			get => _yoffset;
			set
			{
				_yoffset = value;
				RaisePropertyChanged("YOffset");
			}
		}

		public int XMin
        {
			get
            {
				if (Points == null || Points.Count < 1)
					return 0;

				return Points.Min(p => p.X);
            }
        }

		public int XMax
		{
			get
			{
				if (Points == null || Points.Count < 1)
					return 0;

				return Points.Max(p => p.X);
			}
		}

		public int YMin
		{
			get
			{
				if (Points == null || Points.Count < 1)
					return 0;

				return Points.Min(p => p.Y);
			}
		}

		public int YMax
		{
			get
			{
				if (Points == null || Points.Count < 1)
					return 0;

				return Points.Max(p => p.Y);
			}
		}

		public event PropertyChangedEventHandler PropertyChanged;

		public Contour()
        {
			_scale = 1.0;
			_points = new ObservableNotifiableCollection<Point>();
        }

		public Contour(Contour c, bool rescale = false)
		{
			if (c == null)
				throw new ArgumentNullException(nameof(c));


			_points = new ObservableNotifiableCollection<Point>();

			if (!rescale)
			{
				_scale = c.Scale;

				foreach (var p in c.Points)
					_points.Add(new Point(p.X, p.Y));
			}
			else
            {
				foreach (var p in c.Points)
					_points.Add(new Point((int)Math.Round(p.X / c.Scale), (int)Math.Round(p.Y / c.Scale)));

				_scale = 1.0;
            }
		}

		public Contour(FloatContour c)
        {
			_scale = 1.0;

			_points = new ObservableNotifiableCollection<Point>();

			foreach (var p in c.Points)
				_points.Add(new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));
		}

		public Contour(List<RawPoint> rawPoints, float scale, int xOffset, int yOffset)
			: this()
        {
			if (rawPoints != null)
            {
				foreach (var p in rawPoints)
					_points.Add(new Point((int)Math.Round((p.x + xOffset) / scale), (int)Math.Round((p.y + yOffset)/ scale)));
            }
        }

		public Contour(FloatContour c, double normalizationScale, bool rescale = false)
		{
			_scale = normalizationScale;
			_points = new ObservableNotifiableCollection<Point>();

			if (!rescale)
			{
				foreach (var p in c.Points)
					_points.Add(new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));
			}
			else
			{
				foreach (var p in c.Points)
					_points.Add(new Point((int)Math.Round(p.X / normalizationScale), (int)Math.Round(p.Y / normalizationScale)));

				_scale = 1.0;
			}
		}

		public Contour(Outline outline, double normalizationScale)
		{
			_scale = normalizationScale;
			_points = new ObservableNotifiableCollection<Point>();

			if (outline.ChainPoints != null && outline?.ChainPoints.Length > 0)
			{
				foreach (var p in outline.ChainPoints.Points)
					_points.Add(new Point((int)Math.Round(p.X), (int)Math.Round(p.Y)));
			}

			if (outline?.FeaturePointPositions.Count > 0 && Options.CurrentUserOptions.FindFeatures)
				SetFeaturePointPositions(outline.FeaturePointPositions);
		}

		public void ApplyScale()
        {
			if (_scale != 1.0 && Points?.Count > 0)
            {
				for (int i = 0; i < Points.Count; i++)
                {
					Points[i] = new Point((int)Math.Round(Points[i].X / _scale), (int)Math.Round(Points[i].Y / _scale));
				}

				_scale = 1;
            }
        }

		public void ApplyNonProportionalScale(float xScale, float yScale)
        {
			if (Points?.Count > 0)
            {
				for (int i = 0; i < Points.Count; i++)
				{
					Points[i] = new Point((int)Math.Round(Points[i].X / xScale), (int)Math.Round(Points[i].Y / yScale));
				}
			}
        }

		public void ReloadPoints(Contour c)
        {
			if (c == null)
				throw new ArgumentNullException(nameof(c));

			Points.Clear();

			foreach (var p in c.Points)
				Points.Add(new Point(p.X, p.Y));
        }

		public void SetFeaturePointPositions(List<int> featurePointPositions)
        {
			if (featurePointPositions == null)
				throw new ArgumentNullException(nameof(featurePointPositions));

			for (int i = 0; i < Points.Count; i++)
            {
				if (featurePointPositions.Contains(i))
					Points[i].Type = PointType.Feature;
				else
					Points[i].Type = PointType.Normal;
            }
        }

        public void Clear()
        {
            Points.Clear();
        }

		public void AddPoint(int x, int y)
		{
			Points.Add(new Point(x, y));
		}

        public void AddPoint(Point point)
        {
            Points.Add(point);
        }

        public void AddPoint(Point point, int position)
        {
            Points.Insert(position, point);
        }

		public void AddPoint(int x, int y, int position)
		{
			Points.Insert(position, new Point(x, y));
		}

		//    The specified number of points is removed from the front of
		//    the contour.
		public void PopFront(int numPops)
		{
			for (var i = 0; i < numPops; i++)
				RemovePoint(0);
		}

		//    The specified number of points is removed from the end of
		//    the contour.
		public void PopTail(int numPops)
		{
			for (var i = 0; i < numPops; i++)
				RemovePoint(NumPoints - 1);
		}

		public void RemovePoint(int position)
		{
			if (position < 0 || position >= NumPoints)
				throw new ArgumentOutOfRangeException(nameof(position));

			Points.RemoveAt(position);
		}

		//*******************************************************************
		//
		// bool Contour::removePoint(int x, int y)
		//
		//    Searches the contour for the point with the specified coordinates
		//    (x,y) and removes it from the list.
		//
		public bool RemovePoint(int x, int y)
		{
			for (var i = 0; i < NumPoints; i++)
			{
				if (Points[i].X == x && Points[i].Y == y)
				{
					RemovePoint(i);
					return true;
				}
			}

			return false;
		}

		//*******************************************************************
		//
		// int Contour::addPointInOrder(int x, int y)
		//
		//    Adds the point (x,y) to the contour by finding the correct location
		//    between existing contour points whcin minimizes loop-backs or zig-zags.
		//
		public int AddPointInOrder(int x, int y)
		{
			if (NumPoints < 1)
			{
				AddPoint(x, y);
				return 0;
			}

			int position = FindPositionOfClosestPoint(x, y);

			if (position < 0)
				return -1;

			// vector: pos->x,y
			double newVecX = x - Points[position].X;
			double newVecY = y - Points[position].Y;
			double magNew = Math.Sqrt(newVecX * newVecX + newVecY * newVecY);

			if (0 == position)
			{
				double nextVecX = Points[1].X - Points[0].X;
				double nextVecY = Points[1].Y - Points[0].Y;
				double nextAngle = Math.Acos((nextVecX * newVecX + nextVecY * newVecY) /
										(Math.Sqrt(nextVecX * nextVecX + nextVecY * nextVecY) * magNew));

				if ((nextAngle < Math.PI / 2) && (nextAngle > -1 * (Math.PI / 2)))
					AddPoint(x, y, 1);
				else
					AddPoint(x, y, 0);

			}
			else if ((NumPoints - 1) == position)
			{

				double nextVecX = Points[position - 1].X - Points[position].X;
				double nextVecY = Points[position - 1].Y - Points[position].Y;
				double nextAngle = Math.Acos((nextVecX * newVecX + nextVecY * newVecY) /
										(Math.Sqrt(nextVecX * nextVecX + nextVecY * nextVecY) * magNew));

				//if (abs((int)rtod(nextAngle)) < 90) add at position
				//else add at position + 1
				if (!((nextAngle < Math.PI / 2) && (nextAngle > -1 * (Math.PI / 2))))
					position++;
				AddPoint(x, y, position);
			}
			else
			{
				// decide if ray from new point to pos is more like pos+1->pos or new->pos+1
				// to determine if point should be inserted before or after pos.
				// compare bearing of pos->x,y with bearing of pos->pos+1
				double nextVecX = Points[position + 1].X - Points[position].X;
				double nextVecY = Points[position + 1].Y - Points[position].Y;
				double nextAngle = Math.Acos((nextVecX * newVecX + nextVecY * newVecY) /
										(Math.Sqrt(nextVecX * nextVecX + nextVecY * nextVecY) * magNew));
				// compare bearing of x,y->pos with bearing of x,y->pos+1
				double prevVecX = Points[position + 1].X - x;
				double prevVecY = Points[position + 1].Y - y;
				double prevAngle = Math.Acos((prevVecX * (-newVecX) + prevVecY * (-newVecY)) /
										(Math.Sqrt(prevVecX * prevVecX + prevVecY * prevVecY) * magNew));

				//if (nextAngle > prevAngle) add at position
				//else add at position + 1
				if (nextAngle <= prevAngle)
					position++;

				AddPoint(x, y, position);
			}
			return position;
		}

		//*******************************************************************
		//
		// double Contour::totalDistanceAlongContour() const
		//
		//    Returns total distance within the image that would be covered while
		//    walking the contour from end to end.  The entire contour is traversed 
		//    each time this function is called.
		//
		//    PRE:  The contour is NOT empty.
		//
		public double GetTotalDistanceAlongContour()
		{
			if (Points.Count < 2)
				return 0;

			double totalDistance = 0;

			for (int i = 1; i < Points.Count; i++)
			{
				totalDistance += MathHelper.GetDistance(Points[i].X, Points[i].Y, Points[i - 1].X, Points[i - 1].Y);
			}

			if (Options.CurrentUserOptions.ContoursAreClosedLoop)
				totalDistance += MathHelper.GetDistance(Points[Points.Count - 1].X, Points[Points.Count - 1].Y, Points[0].X, Points[0].Y);

			return totalDistance;
		}

		//////////////////////////////////////////////////////////////////////////
		// normalizeContour: called to scale Contour up or down to standard
		//    size (600 units from beginning of leading edge to tip of fin.
		//
		//***008OL new version of function
		//
		public float NormalizeContour()
		{
			ApplyScale();

			// Get X,Y Position of pseudo tip (middle point on Contour)
			float tipx = this[this.Length / 2].X, tipy = this[this.Length / 2].Y;
			float basex = (float)(0.5 * (this[0].X + this[this.Length - 1].X)),
				  basey = (float)(0.5 * (this[0].Y + this[this.Length - 1].Y));

			// Calculate distance
			float dx = tipx - basex;
			float dy = tipy - basey;
			float distance = (float)Math.Sqrt(dx * dx + dy * dy);

			// Compute rescaling factor
			float factor = 600 / distance;

			for (var i = 0; i < _points.Count; i++)
            {
				_points[i] = new Point(
					(int)Math.Round(_points[i].X * factor),
					(int)Math.Round(_points[i].Y * factor));
            }

			Scale = factor;

			return factor; //***006NC
		}

		//*******************************************************************
		//
		// Contour* createScaledContour(Contour* contour, int scale)
		//
		//    Returns a pointer to a new Contour which has been scaled to zoomed out image  
		//
		//    PRE:  It is assumed that (*this) Contour is already normalized prior to 
		//    calling this function
		//
		//    ***new function by KRD 08/16/05 for multiscaled snap-to in TraceWindow
		//
		public Contour CreateScaledContour(float scaleFactor, int xoffset, int yoffset)
		{
			if (NumPoints < 3)
				return new Contour(this);

			int x, y;

			Contour newContour = new Contour();

			for (int i = 0; i < NumPoints; i++)
			{
				// First point of new Contour is first point of old contour
				x = (int)Math.Round(Points[i].X * scaleFactor) + xoffset;
				y = (int)Math.Round(Points[i].Y * scaleFactor) + yoffset;
				newContour.AddPoint(x, y); //005CT
			}
			return newContour;
		}

		public int FindPositionOfClosestPoint(int x, int y)
        {
            if (Points.Count < 1)
                return -1;

            var inputPoint = new Point(x, y);
            double lowestDistance = -1;
            int position = -1;

            for (var i = 0; i < Points.Count; i++)
            {
                double curDistance = (Scale == 1.0) ?
					MathHelper.GetDistance(Points[i].X, Points[i].Y, inputPoint.X, inputPoint.Y) :
					MathHelper.GetDistance(Points[i].X / Scale, Points[i].Y / Scale, inputPoint.X, inputPoint.Y);

                if (position == -1 || curDistance < lowestDistance)
                {
                    lowestDistance = curDistance;
                    position = i;
                }
            }

            return position;
        }

		public void FlipHorizontally(int imageWidth)
        {
			if (Points == null)
				return;

			if (Scale == 1.0)
			{
				foreach (var p in Points)
				{
					p.X = Math.Abs(p.X - imageWidth);
				}
			}
			else
            {
				foreach (var p in Points)
				{
					p.X = (int)(Math.Abs(p.X / Scale - imageWidth) * Scale);
				}
			}
        }

		public void Rotate90CW(int imageWidth, int imageHeight)
        {
			foreach (var p in Points)
            {
				int temp = p.X;
				p.X = -1 * p.Y + imageHeight;
				p.Y = temp;
            }
        }

		public void Rotate90CCW(int imageWidth, int imageHeight)
		{
			foreach (var p in Points)
			{
				int temp = p.X;
				p.X = p.Y;
				p.Y = -1 * temp + imageWidth;
			}
		}

		public void Crop(int xMin, int yMin, int xMax, int yMax, int padding = 0)
        {
			if (Points == null)
				return;

			foreach (var p in Points)
			{
				double xScaled = p.X / Scale;
				double yScaled = p.Y / Scale;

				if (xScaled > xMax || yScaled > yMax || xScaled < xMin || yScaled < yMin)
                {
					p.Type = PointType.Flagged;
                }
				else
                {
					p.X = (int)((xScaled - xMin) * Scale) + padding;
					p.Y = (int)((yScaled - yMin) * Scale) + padding;
                }
			}

			var pointsToRemove = Points.ToList().Where(p => p.Type == PointType.Flagged);
			foreach (var pointToRemove in pointsToRemove)
				Points.Remove(pointToRemove);
		}

		//*******************************************************************
		//
		// Contour* Contour::evenlySpaceContourPoints(double space)
		//
		//    Returns a pointer to a new Contour which has evenly spaced points.  
		//
		//    PRE:  It is assumed that (*this) Contour is already normalized prior to 
		//    calling this function
		//
		//    ***006CM - new version of function by JHS 5/1/2004
		//    corrects erroneous handling of contour foldbacks in excess of
		//    150 degrees
		//
		public Contour EvenlySpaceContourPoints(double space)
		{
			if (NumPoints < 3)
				return new Contour();

			int x, y, ccx, ccy, tx, ty, vx, vy, vsx, vsy, qx = 0, qy = 0;
			int idx;                                          //005CT
			double a, b, c, sqrt_b2_4ac, t1, t2, t, tLen;              //005CT
			double curBearing, prevBearing;                            //005CT
																	   //double changeInAngle;

			// cc= (ccx,ccy) is the center point of the current circle with radius == space
			// vs= (vsx,vsy) is the beginning of the edge segment being intersected with the circle
			// p = (x,y) is the end of the vector being intersected with the circle
			//
			// there are three cases for intersections
			//
			// (1) vs == cc and dist(cc,p) > space
			//     compute Q = point on edge (cc,p) at dist space from cc
			// (2) dist(cc,p) == space
			//     Q is simply p
			// (3) dist(cc,p) < space
			//     set vs = p
			//     set p = next point in original contour
			// (4) vs != cc and dist(cc,p) > space
			//     this means that dist(vs,p) < space (from previous tests)
			//     find Q = point on edge(vs,p) at dist space from cc
			//
			// in all cases the edge (cc,Q) is the next edge in the new contour
			// and Q will be added to the new contour, cc will be moved to Q
			// vs will be moved to Q and and in case (2) p will be moved to the next p
			// ONLY IF the edge bearing is less than 150 degrees change from previous
			// edge bearing
			//
			// if the bearing change is 150 degrees or more, the edge (cc,Q) is skipped
			// and the following occurs
			//
			// case (1) increment index i (moves point p only, leaving vs == cc)
			// case (2) increment index i (moves point p only, leaving vs == cc)
			// case (4) vs is moved back to cc
			//

			// will be true when all original contour points have been used
			bool done = false;

			// Point cc (circle center & previous point added to new contour)
			ccx = Points[0].X;
			ccy = Points[0].Y;
			
			// Point vs (start of edge being tested for intersection with circle)
			vsx = Points[0].X;
			vsy = Points[0].Y;
			
			// Point p (end point of edge being tested for circle intersection)
			x = Points[1].X;
			y = Points[1].Y;

			// Index of p
			idx = 1;

			var newContour = new Contour()
			{
				Scale = Scale
			};

			// first point of new Contour is first point of old contour
			newContour.AddPoint(ccx, ccy); //005CT

			// bearing into first point should be approximately this
			prevBearing = -Math.PI * 0.25; // -45 degrees

			bool lastPoint = false;

			while (!done)
			{
				// vector from vs to p
				vx = x - vsx;
				vy = y - vsy;
				// vector from cc to p
				tx = x - ccx;
				ty = y - ccy;

				//005CT - new thru line 461
				tLen = Math.Sqrt((double)(tx * tx + ty * ty));    // dist(cc,p)

				if (tLen > space)
				{     // then evaluate intersection
					if ((ccx == vsx) && (ccy == vsy))
					{
						// case (1) Q is on edge(cc,p) at dist space from cc
						//***1.3 - added round() function - truncation was causing points to
						//         drift away from long line segments
						qx = (int)Math.Round((space / tLen) * x + (1.0 - (space / tLen)) * ccx);
						qy = (int)Math.Round((space / tLen) * y + (1.0 - (space / tLen)) * ccy);

						// check that wrapping is not occurring
						curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

						/*
						/////////////////////// begin new /////////////////////
						//***1.1ER - correct for limits (+/- pi) of atan2
						// fixing this is required to prevent pulling edge out of 
						// deep downward turned notches, but creates problem
						// with notch finding -- fix in next version
						*/
						double changeInAngle = Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing));
						if (changeInAngle > 180)
							changeInAngle = 360 - changeInAngle;

						if (changeInAngle <= 150)
						/////////////////////// end new /////////////////////


						/////////////////////// begin old //////////////////
						//if (fabs(rtod(prevBearing - curBearing)) <= MAX_TURN_ANGLE) //***1.4 - was 150
						////////////////////////end old /////////////////////
						{
							// add Q to new contour and move forward
							newContour.AddPoint(qx, qy);
							//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
							//	   << " from previous point" << endl;
							prevBearing = curBearing;
							ccx = qx;
							ccy = qy;
							vsx = qx;
							vsy = qy;
						}
						else // Change in bearing is excessive
						{
							// Skip this point Q and move p forward
							// Next time we calculate edge(cc,p) intersection with circle
							idx += 1;

							if (lastPoint)
							{
								done = true;
							}
							else if (idx < NumPoints)
							{
								vsx = ccx;
								vsy = ccy;
								x = Points[idx].X;
								y = Points[idx].Y;
							}
							else
							{
								if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
								{
									done = true;
								}
								else
								{
									lastPoint = true;
									vsx = ccx;
									vsy = ccy;
									x = Points[0].X;
									y = Points[0].Y;
								}
							}
						}
					}
					else  // cc != vs
					{
						// case (4) Q is on edge(vs,p) at dist space from cc
						tx = vsx - ccx;
						ty = vsy - ccy;
						a = vx * vx + vy * vy;
						b = 2 * (vx * tx + vy * ty);    // 2 * dot(v, t)
						c = tx * tx + ty * ty - space * space;

						if (b * b - 4 * a * c < 0.0)
							Trace.Write("Neg Radical"); // this should NEVER happen
						else
						{
							sqrt_b2_4ac = Math.Sqrt(b * b - 4 * a * c);
							t1 = (-b + sqrt_b2_4ac) / (2 * a);
							t2 = (-b - sqrt_b2_4ac) / (2 * a);
							if ((t1 >= 0) && (t1 <= 1))
								t = t1;
							else if ((t2 >= 0) && (t2 <= 1))
								t = t2;
							else
								t = 0; // this should NEVER happen, either

							qx = (int)Math.Round(vsx + (t * vx));
							qy = (int)Math.Round(vsy + (t * vy));
						}

						// check that wrapping is not occurring
						curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

						/*
						//////////////////////// begin new ///////////////////
						//***1.1ER - correct for limits (+/- pi) of atan2
						// fixing this is required to prevent pulling edge out of 
						// deep downward turned notches, but creates problem
						// with notch finding -- fix in next version
						//
						changeInAngle = fabs(rtod(prevBearing - curBearing));
						if (changeInAngle > 180)
							changeInAngle = 360 - changeInAngle;

						if (changeInAngle <= 150)
						//////////////////////// end new ////////////////////
						*/

						//////////////////////// begin old //////////////////
						if (Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing)) <= MaxTurnAngle) //***1.4 - was 150
																												/////////////////////// end old /////////////////////
						{
							// add Q to new countour and move cc forward
							newContour.AddPoint(qx, qy);
							//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
							//	   << " from previous point" << endl;
							prevBearing = curBearing;
							ccx = qx;
							ccy = qy;
							vsx = qx;
							vsy = qy;
						}
						else // change in angle is excessive
						{
							// skip this point Q, move vs back to cc
							// next time we calculate edge(cc,p) intersection with circle
							vsx = ccx;
							vsy = ccy;
						}
					}
				}
				else if (tLen < space)
				{
					// case(3) move vs and p forward
					idx += 1;

					if (lastPoint)
					{
						done = true;
					}
					else if (idx < Points.Count)
					{
						vsx = x;
						vsy = y;
						x = Points[idx].X;
						y = Points[idx].Y;
					}
					else
					{
						if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
						{
							done = true;
						}
						else
						{
							lastPoint = true;
							vsx = x;
							vsy = y;
							x = Points[0].X;
							y = Points[0].Y;
						}
					}
				}
				else // tLen == space
				{
					// case(2) point Q is simply point p
					qx = x;
					qy = y;

					// check that wrapping is not occurring
					curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

					/*
					/////////////////// begin new //////////////////////
					//***1.1ER - correct for limits (+/- pi) of atan2
					// fixing this is required to prevent pulling edge out of 
					// deep downward turned notches, but creates problem
					// with notch finding -- fix in next version
					//
					changeInAngle = fabs(rtod(prevBearing - curBearing));
					if (changeInAngle > 180)
						changeInAngle = 360 - changeInAngle;

					if (changeInAngle <= 150)
					/////////////////// end new /////////////////////
					*/

					/////////////////////// begin old /////////////////////
					if (Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing)) <= MaxTurnAngle) //***1.4 - was 150
					////////////////////// end old ///////////////////////
					{
						// add Q to new countour and move cc forward
						newContour.AddPoint(qx, qy);
						//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
						//	   << " from previous point" << endl;
						prevBearing = curBearing;
						ccx = qx;
						ccy = qy;
						vsx = qx;
						vsy = qy;
					}
					else // change in angle is excessive
					{
						// skip this point Q, move vs back to cc (if needed) and move p forward
						// next time we calculate edge(cc,p) intersection with circle
						vsx = ccx;
						vsy = ccy;
						idx += 1;

						if (lastPoint)
						{
							done = true;
						}
						else if (idx < Points.Count)
						{
							x = Points[idx].X;
							y = Points[idx].Y;
						}
						else
                        {
							if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
							{
								done = true;
							}
							else
							{
								lastPoint = true;
								x = Points[0].X;
								y = Points[0].Y;
							}
						}
					}
				}

			} // while (!done)

			return newContour;
		}

		/// <summary>
		/// Evenly space contour points, but allow greater turn angles and minor changes
		/// for closed loop contours.
		/// </summary>
		/// <param name="space"></param>
		/// <returns></returns>
		public Contour EvenlySpaceContourPoints2(double space)
		{
			if (NumPoints < 3)
				return new Contour();

			double maxTurnAngle = 360;

			int x, y, ccx, ccy, tx, ty, vx, vy, vsx, vsy, qx = 0, qy = 0;
			int idx;                                          //005CT
			double a, b, c, sqrt_b2_4ac, t1, t2, t, tLen;              //005CT
			double curBearing, prevBearing;                            //005CT
																	   //double changeInAngle;

			// cc= (ccx,ccy) is the center point of the current circle with radius == space
			// vs= (vsx,vsy) is the beginning of the edge segment being intersected with the circle
			// p = (x,y) is the end of the vector being intersected with the circle
			//
			// there are three cases for intersections
			//
			// (1) vs == cc and dist(cc,p) > space
			//     compute Q = point on edge (cc,p) at dist space from cc
			// (2) dist(cc,p) == space
			//     Q is simply p
			// (3) dist(cc,p) < space
			//     set vs = p
			//     set p = next point in original contour
			// (4) vs != cc and dist(cc,p) > space
			//     this means that dist(vs,p) < space (from previous tests)
			//     find Q = point on edge(vs,p) at dist space from cc
			//
			// in all cases the edge (cc,Q) is the next edge in the new contour
			// and Q will be added to the new contour, cc will be moved to Q
			// vs will be moved to Q and and in case (2) p will be moved to the next p
			// ONLY IF the edge bearing is less than 150 degrees change from previous
			// edge bearing
			//
			// if the bearing change is 150 degrees or more, the edge (cc,Q) is skipped
			// and the following occurs
			//
			// case (1) increment index i (moves point p only, leaving vs == cc)
			// case (2) increment index i (moves point p only, leaving vs == cc)
			// case (4) vs is moved back to cc
			//

			// will be true when all original contour points have been used
			bool done = false;

			// Point cc (circle center & previous point added to new contour)
			ccx = Points[0].X;
			ccy = Points[0].Y;

			// Point vs (start of edge being tested for intersection with circle)
			vsx = Points[0].X;
			vsy = Points[0].Y;

			// Point p (end point of edge being tested for circle intersection)
			x = Points[1].X;
			y = Points[1].Y;

			// Index of p
			idx = 1;

			var newContour = new Contour()
			{
				Scale = Scale
			};

			// First point of new Contour is first point of old contour
			newContour.AddPoint(ccx, ccy); //005CT

			// bearing into first point should be approximately this
			prevBearing = -Math.PI * 0.25; // -45 degrees

			bool lastPoint = false;

			while (!done)
			{
				// vector from vs to p
				vx = x - vsx;
				vy = y - vsy;
				// vector from cc to p
				tx = x - ccx;
				ty = y - ccy;

				//005CT - new thru line 461
				tLen = Math.Sqrt((double)(tx * tx + ty * ty));    // dist(cc,p)

				if (tLen > space)
				{     // then evaluate intersection
					if ((ccx == vsx) && (ccy == vsy))
					{
						// case (1) Q is on edge(cc,p) at dist space from cc
						//***1.3 - added round() function - truncation was causing points to
						//         drift away from long line segments
						qx = (int)Math.Round((space / tLen) * x + (1.0 - (space / tLen)) * ccx);
						qy = (int)Math.Round((space / tLen) * y + (1.0 - (space / tLen)) * ccy);

						// check that wrapping is not occurring
						curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

						/*
						/////////////////////// begin new /////////////////////
						//***1.1ER - correct for limits (+/- pi) of atan2
						// fixing this is required to prevent pulling edge out of 
						// deep downward turned notches, but creates problem
						// with notch finding -- fix in next version
						*/
						double changeInAngle = Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing));

						//if (changeInAngle > 180)
						//	changeInAngle = 360 - changeInAngle;

						if (changeInAngle <= maxTurnAngle)
						{
							// add Q to new contour and move forward
							newContour.AddPoint(qx, qy);
							//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
							//	   << " from previous point" << endl;
							prevBearing = curBearing;
							ccx = qx;
							ccy = qy;
							vsx = qx;
							vsy = qy;
						}
						else // Change in bearing is excessive
						{
							// Skip this point Q and move p forward
							// Next time we calculate edge(cc,p) intersection with circle
							idx += 1;

							if (lastPoint)
							{
								done = true;
							}
							else if (idx < NumPoints)
							{
								vsx = ccx;
								vsy = ccy;
								x = Points[idx].X;
								y = Points[idx].Y;
							}
							else
							{
								if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
								{
									done = true;
								}
								else
								{
									lastPoint = true;
									vsx = ccx;
									vsy = ccy;
									x = Points[0].X;
									y = Points[0].Y;
								}
							}
						}
					}
					else  // cc != vs
					{
						// case (4) Q is on edge(vs,p) at dist space from cc
						tx = vsx - ccx;
						ty = vsy - ccy;
						a = vx * vx + vy * vy;
						b = 2 * (vx * tx + vy * ty);    // 2 * dot(v, t)
						c = tx * tx + ty * ty - space * space;

						if (b * b - 4 * a * c < 0.0)
							Trace.Write("Neg Radical"); // this should NEVER happen
						else
						{
							sqrt_b2_4ac = Math.Sqrt(b * b - 4 * a * c);
							t1 = (-b + sqrt_b2_4ac) / (2 * a);
							t2 = (-b - sqrt_b2_4ac) / (2 * a);
							if ((t1 >= 0) && (t1 <= 1))
								t = t1;
							else if ((t2 >= 0) && (t2 <= 1))
								t = t2;
							else
								t = 0; // this should NEVER happen, either

							qx = (int)Math.Round(vsx + (t * vx));
							qy = (int)Math.Round(vsy + (t * vy));
						}

						// check that wrapping is not occurring
						curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

						/*
						//////////////////////// begin new ///////////////////
						//***1.1ER - correct for limits (+/- pi) of atan2
						// fixing this is required to prevent pulling edge out of 
						// deep downward turned notches, but creates problem
						// with notch finding -- fix in next version
						//
						changeInAngle = fabs(rtod(prevBearing - curBearing));
						if (changeInAngle > 180)
							changeInAngle = 360 - changeInAngle;

						if (changeInAngle <= 150)
						//////////////////////// end new ////////////////////
						*/

						//////////////////////// begin old //////////////////
						if (Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing)) <= maxTurnAngle) //***1.4 - was 150
																											 /////////////////////// end old /////////////////////
						{
							// add Q to new countour and move cc forward
							newContour.AddPoint(qx, qy);
							//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
							//	   << " from previous point" << endl;
							prevBearing = curBearing;
							ccx = qx;
							ccy = qy;
							vsx = qx;
							vsy = qy;
						}
						else // change in angle is excessive
						{
							// skip this point Q, move vs back to cc
							// next time we calculate edge(cc,p) intersection with circle
							vsx = ccx;
							vsy = ccy;
						}
					}
				}
				else if (tLen < space)
				{
					// case(3) move vs and p forward
					idx += 1;

					if (lastPoint)
					{
						done = true;
					}
					else if (idx < Points.Count)
					{
						vsx = x;
						vsy = y;
						x = Points[idx].X;
						y = Points[idx].Y;
					}
					else
					{
						if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
						{
							done = true;
						}
						else
						{
							lastPoint = true;
							vsx = x;
							vsy = y;
							x = Points[0].X;
							y = Points[0].Y;
						}
					}
				}
				else // tLen == space
				{
					// case(2) point Q is simply point p
					qx = x;
					qy = y;

					// check that wrapping is not occurring
					curBearing = Math.Atan2((double)(qy - ccy), (double)(qx - ccx));

					/*
					/////////////////// begin new //////////////////////
					//***1.1ER - correct for limits (+/- pi) of atan2
					// fixing this is required to prevent pulling edge out of 
					// deep downward turned notches, but creates problem
					// with notch finding -- fix in next version
					//
					changeInAngle = fabs(rtod(prevBearing - curBearing));
					if (changeInAngle > 180)
						changeInAngle = 360 - changeInAngle;

					if (changeInAngle <= 150)
					/////////////////// end new /////////////////////
					*/

					/////////////////////// begin old /////////////////////
					if (Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing)) <= maxTurnAngle) //***1.4 - was 150
																										 ////////////////////// end old ///////////////////////
					{
						// add Q to new countour and move cc forward
						newContour.AddPoint(qx, qy);
						//cout << "x=" << qx << ":" << "y=" << qy << ":" << distance(qx,qy,ccx,ccy)
						//	   << " from previous point" << endl;
						prevBearing = curBearing;
						ccx = qx;
						ccy = qy;
						vsx = qx;
						vsy = qy;
					}
					else // change in angle is excessive
					{
						// skip this point Q, move vs back to cc (if needed) and move p forward
						// next time we calculate edge(cc,p) intersection with circle
						vsx = ccx;
						vsy = ccy;
						idx += 1;

						if (lastPoint)
						{
							done = true;
						}
						else if (idx < Points.Count)
						{
							x = Points[idx].X;
							y = Points[idx].Y;
						}
						else
						{
							if (!Options.CurrentUserOptions.ContoursAreClosedLoop)
							{
								done = true;
							}
							else
							{
								lastPoint = true;
								x = Points[0].X;
								y = Points[0].Y;
							}
						}
					}
				}
			} 

			return newContour;
		}

		public void ClipToBounds()
        {
			if (Points == null || Points.Count < 1)
				return;

			int xMin = XMin;
			int yMin = YMin;

			for (int i = 0; i < Points.Count; i++)
            {
				Points[i] = new Point(Points[i].X - xMin, Points[i].Y - yMin, Points[i].Type);
            }
        }

		public void ClipToBounds(int xMin, int yMin)
        {
			for (int i = 0; i < Points.Count; i++)
			{
				Points[i] = new Point(Points[i].X - xMin, Points[i].Y - yMin, Points[i].Type);
			}
		}

		//  bool trimAndReorder(Contour &c, Contour_point_t start, Contour_point_t end)
		//  
		// Trims excess points off Contour and reorders if necessary.
		//
		// This Contour is trimmed in the following manner.  The closest point
		// to start is found and the closest point to end is found.  All
		// points in this Contour following max(closest2StartPos,closest2EndPos) 
		// are removed.  All points ahead of min(closest2StartPos,closest2EndPos) 
		// are removed.  Then if closest2EndPos < closest2StartPos, the Contour
		// is reversed.  Finally, the startPt and endPts are placed at their
		// respective ends of this Contour.
		public bool TrimAndReorder(Point startPt, Point endPt)
		{
			int closest2StartPos = FindPositionOfClosestPoint(startPt.X, startPt.Y);
			int closest2EndPos = FindPositionOfClosestPoint(endPt.X, endPt.Y);

			if (closest2StartPos < 0 || closest2EndPos < 0)
				return false;

			bool reverseIt;
			int firstIndex, secondIndex;
			if (closest2StartPos <= closest2EndPos)
			{
				reverseIt = false;
				firstIndex = closest2StartPos;
				secondIndex = closest2EndPos;
			}
			else
			{
				reverseIt = true;
				firstIndex = closest2EndPos;
				secondIndex = closest2StartPos;
			}

			// trim extra points from Contour

			PopTail(NumPoints - secondIndex - 1); // remove all points following secondIndex position
			PopFront(firstIndex);                // remove all points preceding firstIndex position

			if (reverseIt)
			{
				// reverse the point order in this Contour

				Contour c = new Contour(this); // COPY this Contour
				Clear(); // then clear out this Contour

				foreach (var p in c.Points)
				{
					AddPoint(p.X, p.Y, 0);
				}
			}

			// Prepend start Pt and append end Pt to this Contour
			AddPoint(startPt.X, startPt.Y, 0);
			AddPoint(endPt.X, endPt.Y);

			return true;
		}

		//*******************************************************************
		//
		// void Contour::removeKnots(double spacing)
		//
		//    Removes extra points along any loops (knots) that have formed in the 
		//    Contour.
		//
		//    ***006CM - revised so that the algorithm does NOT go back to
		//    beginning of list each time a point is removed -- JHS 4/28/2004
		//
		public void RemoveKnots(double spacing)
		{
			// First remove all points which are closer than spacing
			double space = spacing - 1;

			if (NumPoints < 2)
				return; // if 1 or fewer points, we're done

			// skip first pt, then need next pt to compute distance
			// index of next point (first one considered for removal)
			// TODO: Verify this logic works correctly
			bool done = false;
			int i = 1;
			while (!done)
			{
				if (MathHelper.GetDistance(Points[i].X, Points[i - 1].X, Points[i].Y, Points[i - 1].Y) < space)
				{
					RemovePoint(i);
				}
				else
				{
					i += 1;
				}

				if (i >= NumPoints)
					done = true;
			}

			if (NumPoints < 2)
				return; // if 1 or fewer points, we're done

			double curBearing, prevBearing;
			// first angle
			prevBearing = (-1 * Math.PI * 0.25); // -45 degrees

			// only delete one bad angle point from the trace at a time
			done = false;
			int j = 1;
			while (!done)
			{ 
				curBearing = Math.Atan2(Points[j].Y - Points[j - 1].Y, Points[j].X - Points[j - 1].X);

				// if angle from *temp to *temp->next doubles back,
				// then remove *temp->next

				/*
				///////////////////////////// begin new //////////////////////
				//***1.1ER - fix problem related to downward opening notches
				// this function AND evenlySpaceContourPoints() currently remove
				// all points that would allow contour to trend to left and up past
				// the horizontal - this occurs because of the limits of atan2()
				// which are +/- pi
				// in the next software version this needs to be fixed, but there 
				// is a related problem, once contours are allowed to have angles
				// up and left (absolute angles in range -180 .. -90, that is
				// the NOTCH finding process, finds the upper lip of the notch
				// not the notch itself
				// that relates to the use of the wavelet code and the absolute
				// chains which can then have notch angular changes of, say -350 when
				// the correct angular change is +10, and the notch max/min finding
				// thus selects the HUGE entry angle change rather than the
				// more modest interior change
				//
				*/
				double changeInAngle = Math.Abs(MathHelper.RadiansToDegrees(prevBearing - curBearing));
				// since atan2 returns -pi .. +pi, we must correct if bearing goes from + to -
				if (changeInAngle > 180)
					changeInAngle = 360 - changeInAngle;

				if (changeInAngle > 150)
				{
					/////////////////////////// end new //////////////////////////
					/*
					////////////////////// begin old //////////////////////////
					double changeInAngle = rtod(prevBearing - curBearing);
					if (fabs(changeInAngle) > MAX_TURN_ANGLE) //***1.4 - was 150
					////////////////////// end old ////////////////////////////
					*/
					RemovePoint(j); //***008OL
				}
				else
				{
					// angle is ok, so move on
					prevBearing = curBearing;
					j++;
				}

				if (j >= NumPoints)
					done = true;
			}
		}

		protected virtual void RaisePropertyChanged(string propertyName)
		{
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
	}
}
