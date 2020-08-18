using System;
using System.Collections.Generic;
using System.Text;

namespace Darwin.Model
{
    public class GeoLocation
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }

        public GeoLocation()
        {
        }

        public GeoLocation(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }
    }
}
