// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a geographic coordinate consisting of latitude and longitude.
    /// </summary>
    public struct GeoCoordinate : IEquatable<GeoCoordinate>
    {
        /// <summary>
        /// Gets or sets the latitude of the coordinate.
        /// </summary>
        public double Latitude { get; set; }

        /// <summary>
        /// Gets or sets the longitude of the coordinate.
        /// </summary>
        public double Longitude { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoCoordinate"/> struct with latitude and longitude set to 0.
        /// </summary>
        public GeoCoordinate()
        {
            Latitude = 0;
            Longitude = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoCoordinate"/> struct with the specified latitude and longitude.
        /// </summary>
        /// <param name="latitude">The latitude of the coordinate.</param>
        /// <param name="longitude">The longitude of the coordinate.</param>
        public GeoCoordinate(double latitude, double longitude)
        {
            Latitude = latitude;
            Longitude = longitude;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoCoordinate"/> struct with the specified latitude and longitude strings.
        /// </summary>
        /// <param name="latitude">The latitude of the coordinate as a string.</param>
        /// <param name="longitude">The longitude of the coordinate as a string.</param>
        /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
        public GeoCoordinate(string latitude, string longitude, IFormatProvider formatProvider)
        {
            formatProvider ??= System.Globalization.CultureInfo.InvariantCulture;
            if (!double.TryParse(latitude, formatProvider, out var lat))
            {
                throw new ArgumentException($"Invalid latitude: {latitude}");
            }
            if (!double.TryParse(longitude, formatProvider, out var lon))
            {
                throw new ArgumentException($"Invalid longitude: {longitude}");
            }
            Latitude = lat;
            Longitude = lon;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GeoCoordinate"/> class with the specified latitude and longitude strings.
        /// </summary>
        /// <param name="latitude">The latitude of the coordinate.</param>
        /// <param name="longitude">The longitude of the coordinate.</param>
        public GeoCoordinate(string latitude, string longitude) : 
            this(latitude, longitude, System.Globalization.CultureInfo.InvariantCulture)
        {
        }

        /// <summary>
        /// Determines whether the current <see cref="GeoCoordinate"/> object is equal to another <see cref="GeoCoordinate"/> object.
        /// </summary>
        /// <param name="other">The <see cref="GeoCoordinate"/> object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the other object; otherwise, <c>false</c>.</returns>
        public bool Equals(GeoCoordinate other)
        {
            return Latitude == other.Latitude && Longitude == other.Longitude;
        }

        /// <summary>
        /// Determines whether the current <see cref="GeoCoordinate"/> object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the other object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return obj is GeoCoordinate other && Equals(other);
        }

        /// <summary>
        /// Returns a string representation of the GeoCoordinate object.
        /// </summary>
        /// <remarks>
        /// The string contains the latitude and longitude values, labeled with their respective names.
        /// </remarks>
        /// <returns>A string containing the latitude and longitude values.</returns>
        public override string ToString()
        {
            return $"Latitude: {Latitude}, Longitude: {Longitude}";
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="GeoCoordinate"/> object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Latitude, Longitude);
        }

        /// <summary>
        /// Determines whether two GeoCoordinate objects are equal.
        /// </summary>
        /// <param name="left">The first GeoCoordinate to compare.</param>
        /// <param name="right">The second GeoCoordinate to compare.</param>
        /// <returns>true if the two GeoCoordinate objects are equal; otherwise, false.</returns>
        public static bool operator ==(GeoCoordinate left, GeoCoordinate right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two GeoCoordinate objects are not equal.
        /// </summary>
        /// <param name="left">The first GeoCoordinate to compare.</param>
        /// <param name="right">The second GeoCoordinate to compare.</param>
        /// <returns>true if the two GeoCoordinate objects are not equal; otherwise, false.</returns>
        public static bool operator !=(GeoCoordinate left, GeoCoordinate right)
        {
            return !left.Equals(right);
        }

        /// <summary>
        /// Tries to parse the latitude and longitude strings into a <see cref="GeoCoordinate"/> object.
        /// </summary>
        /// <param name="latitudeString">The latitude string to parse.</param>
        /// <param name="longitudeString">The longitude string to parse.</param>
        /// <param name="formatProvider">An object that provides culture-specific formatting information.</param>
        /// <param name="result">When this method returns, contains the parsed <see cref="GeoCoordinate"/> object if the parsing succeeded, or a default <see cref="GeoCoordinate"/> object if the parsing failed.</param>
        /// <returns><c>true</c> if the parsing succeeded; otherwise, <c>false</c>.</returns>
        public static bool TryParse(string latitudeString, string longitudeString, IFormatProvider formatProvider, out GeoCoordinate result)
        {
            result = new GeoCoordinate();
            if (!double.TryParse(latitudeString, formatProvider, out var latitude))
            {
                return false;
            }

            if (!double.TryParse(longitudeString, formatProvider, out var longitude))
            {
                return false;
            }

            result = new GeoCoordinate(latitude, longitude);
            return true;
        }
    }
}