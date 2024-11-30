/*
==========================================================================
This file is part of Briefing Room for DCS World, a mission
generator for DCS World, by @akaAgar (https://github.com/akaAgar/briefing-room-for-dcs)

Briefing Room for DCS World is free software: you can redistribute it
and/or modify it under the terms of the GNU General Public License
as published by the Free Software Foundation, either version 3 of
the License, or (at your option) any later version.

Briefing Room for DCS World is distributed in the hope that it will
be useful, but WITHOUT ANY WARRANTY; without even the implied warranty
of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Briefing Room for DCS World. If not, see https://www.gnu.org/licenses/
==========================================================================
*/

using System;
using System.Collections.Generic;
using System.Linq;

namespace BriefingRoom4DCS
{
    public readonly struct Coordinates : ICloneable
    {
        internal static readonly Coordinates Zero = new(0, 0);

        internal readonly double X;

        internal readonly double Y;

        internal Coordinates(double both) { X = both; Y = both; }
        internal Coordinates(double[] arr) { X = arr[0]; Y = arr[1]; }
        internal Coordinates(List<double> arr) { X = arr[0]; Y = arr[1]; }

        internal Coordinates(double x, double y) { X = x; Y = y; }

        internal Coordinates(Coordinates source) { X = source.X; Y = source.Y; }

        internal Coordinates(string coordinatesString)
        {
            try
            {
                string[] xAndYStrings = coordinatesString.Split(',');

                X = Toolbox.StringToDouble(xAndYStrings[0]);
                Y = Toolbox.StringToDouble(xAndYStrings[1]);
            }
            catch (Exception)
            {
                X = 0; Y = 0;
            }
        }

        public static Coordinates operator +(Coordinates coo1, Coordinates coo2) { return new Coordinates(coo1.X + coo2.X, coo1.Y + coo2.Y); }

        public static Coordinates operator -(Coordinates coo1, Coordinates coo2) { return new Coordinates(coo1.X - coo2.X, coo1.Y - coo2.Y); }

        public static Coordinates operator *(Coordinates coo, double mul) { return new Coordinates(coo.X * mul, coo.Y * mul); }

        public static Coordinates operator *(Coordinates coo1, Coordinates coo2) { return new Coordinates(coo1.X * coo2.X, coo1.Y * coo2.Y); }

        public static Coordinates operator /(Coordinates coo1, Coordinates coo2) { return new Coordinates(coo1.X / coo2.X, coo1.Y / coo2.Y); }

        public static Coordinates operator /(Coordinates coo, double div) { return new Coordinates(coo.X / div, coo.Y / div); }

        public override string ToString() { return Toolbox.ValToString(X) + "," + Toolbox.ValToString(Y); }

        public double[] ToArray() => new double[] { X, Y };

        internal static Coordinates FromAngleInDegrees(double angle)
        {
            return FromAngleInRadians(angle * Toolbox.DEGREES_TO_RADIANS);
        }

        internal static Coordinates FromAngleInRadians(double angle)
        {
            return new Coordinates(Math.Cos(angle), Math.Sin(angle));
        }

        internal static double ToAngleInRadians(Coordinates center, Coordinates waypoint)
        {
            var delta = waypoint - center;
            var rawRads = Math.Atan2(delta.Y, delta.X);
            return rawRads >= 0 ? rawRads : Toolbox.TWO_PI + rawRads;
        }

        internal static Coordinates Lerp(Coordinates coo1, Coordinates coo2, double value)
        {
            return new Coordinates(Toolbox.Lerp(coo1.X, coo2.X, value), Toolbox.Lerp(coo1.Y, coo2.Y, value));
        }

        internal static Coordinates FromAngleAndDistance(Coordinates coordinates, MinMaxD distanceMinMax, double angle)
        {
            return FromAngleAndDistance(coordinates, distanceMinMax.GetValue(), angle);
        }

        internal static Coordinates FromAngleAndDistance(Coordinates coordinates, double distance, double angle)
        {
            return new Coordinates(
                coordinates.X + distance * Math.Cos(angle * Toolbox.DEGREES_TO_RADIANS),
                coordinates.Y + distance * Math.Sin(angle * Toolbox.DEGREES_TO_RADIANS));
        }


        internal static Coordinates CreateRandom(double min, double max)
        {
            return CreateRandom(new MinMaxD(min, max));
        }

        internal static Coordinates CreateRandom(MinMaxD minMax)
        {
            return FromAngleInDegrees(Toolbox.RandomDouble(360.0)) * minMax.GetValue();
        }


        internal static Coordinates CreateRandom(Coordinates coordinates, MinMaxD minMax) => FromAngleAndDistance(coordinates, minMax, Toolbox.RandomDouble(360.0));

        internal static Coordinates GetCenter(params Coordinates[] coordinates)
        {
            if ((coordinates == null) || (coordinates.Length == 0)) return new Coordinates();

            Coordinates center = new();
            for (int i = 0; i < coordinates.Length; i++) center += coordinates[i];

            center /= coordinates.Length;

            return center;
        }

        internal double GetDistanceFrom(Coordinates other) { return Math.Sqrt(GetSquaredDistanceFrom(other)); }

        internal double GetSquaredDistanceFrom(Coordinates other) { return Math.Pow(X - other.X, 2) + Math.Pow(Y - other.Y, 2); }

        public object Clone() { return new Coordinates(X, Y); }

        internal string ToLuaTable()
        {
            return $"{{ [\"x\"] = {Toolbox.ValToString(X)}, [\"y\"] = {Toolbox.ValToString(Y)} }}";
        }

        internal Coordinates CreateNearRandom(double min, double max)
        {
            return CreateNearRandom(new MinMaxD(min, max));
        }

        internal double GetLength()
        {
            return Math.Sqrt(Math.Pow(X, 2) + Math.Pow(Y, 2));
        }

        internal Coordinates Normalize()
        {
            double length = GetLength();
            if (length == 0) return Zero;
            return new Coordinates(X / length, Y / length);
        }

        internal Coordinates CreateNearRandom(MinMaxD minMax)
        {
            double randX = minMax.GetValue() * Toolbox.RandomFrom(1, -1);
            double randY = minMax.GetValue() * Toolbox.RandomFrom(1, -1);

            return new Coordinates(X + randX, Y + randY);
        }

        internal static Coordinates Sum(IEnumerable<Coordinates> coordinates)
        {
            return new Coordinates(
                (from coordinate in coordinates select coordinate.X).Sum(),
                (from coordinate in coordinates select coordinate.Y).Sum());
        }

        internal double GetHeadingFrom(Coordinates lastWP)
        {
            return Math.Round(ToAngleInRadians(lastWP, this) * Toolbox.RADIANS_TO_DEGREES);
        }
    }
}
