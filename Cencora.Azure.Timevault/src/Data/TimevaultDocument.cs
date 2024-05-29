// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a document in a time vault with identification, timezone information, physical Location, and geographical location.
    /// </summary>
    public class TimevaultDocument : IEquatable<TimevaultDocument>
    {
        /// <summary>
        /// Gets or sets the unique identifier for the document.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Gets or sets the IANA timezone code for the document's associated location.
        /// </summary>
        public string IanaCode { get; set; }

        /// <summary>
        /// Gets or sets the physical Location associated with the document.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Gets or sets the geographical location of the document.
        /// </summary>
        public GeoCoordinate Coordinate { get; set; }

        /// <summary>
        /// Gets or sets the timestamp of the last IANA code update for the <see cref="Coordinate"/> / <see cref="Location"/>.
        /// </summary>
        public DateTime LastIanaCodeUpdateTimestamp { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class.
        /// </summary>
        public TimevaultDocument()
        {
            Id = string.Empty;
            IanaCode = string.Empty;
            Location = new Location();
            Coordinate = new GeoCoordinate();
            LastIanaCodeUpdateTimestamp = default;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class with the specified values.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code for the document's associated location.</param>
        /// <param name="Location">The physical Location associated with the document.</param>
        /// <param name="location">The geographical location of the document.</param>
        /// <param name="lastIanaCodeUpdateTimestamp">The timestamp of the last IANA code update.</param>
        /// <remarks>
        /// The <see cref="Id"/> property is set to a new unique identifier.
        /// </remarks>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="ianaCode"/> is <c>null</c> or empty.</exception>
        public TimevaultDocument(string ianaCode, Location Location, GeoCoordinate location, DateTime lastIanaCodeUpdateTimestamp)
            : this(Guid.NewGuid().ToString(), ianaCode, Location, location, lastIanaCodeUpdateTimestamp)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class with the specified values.
        /// </summary>
        /// <param name="id">The unique identifier for the document.</param>
        /// <param name="ianaCode">The IANA timezone code for the document's associated location.</param>
        /// <param name="">The physical Location associated with the document.</param>
        /// <param name="location">The geographical location of the document.</param>
        /// <param name="lastIanaCodeUpdateTimestamp">The timestamp of the last IANA code update.</param>
        /// <exception cref="ArgumentException">Thrown when the <paramref name="id"/> or <paramref name="ianaCode"/> is <c>null</c> or empty.</exception>
        public TimevaultDocument(string id, string ianaCode, Location location, GeoCoordinate coordinate, DateTime lastIanaCodeUpdateTimestamp)
        {
            if (string.IsNullOrEmpty(id)) throw new ArgumentException("The document identifier cannot be null or empty.", nameof(id));
            if (string.IsNullOrEmpty(ianaCode)) throw new ArgumentException("The IANA timezone code cannot be null or empty.", nameof(ianaCode));

            Id = id;
            IanaCode = ianaCode;
            Location = location;
            Coordinate = coordinate;
            LastIanaCodeUpdateTimestamp = lastIanaCodeUpdateTimestamp;
        }

        /// <summary>
        /// Determines whether the current <see cref="TimevaultDocument"/> object is equal to another <see cref="TimevaultDocument"/> object.
        /// </summary>
        /// <remarks>
        /// Two <see cref="TimevaultDocument"/> objects are considered equal if their <see cref="Id"/> properties are equal 
        /// or if their <see cref="Id"/> properties are both <c>null</c> or <see cref="string.Empty"/> and 
        /// their <see cref="IanaCode"/>, <see cref="Location"/>, and <see cref="Coordinate"/> properties are equal.
        /// </remarks>
        /// <param name="other">The <see cref="TimevaultDocument"/> object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the other object; otherwise, <c>false</c>.</returns>
        public bool Equals(TimevaultDocument? other)
        {
            if (other is null)
            {
                return false;
            }

            if (string.IsNullOrEmpty(Id) || string.IsNullOrEmpty(other.Id))
            {
                return IanaCode.Equals(other.IanaCode)
                    && Location.Equals(other.Location)
                    && Coordinate.Equals(other.Coordinate)
                    && LastIanaCodeUpdateTimestamp.Equals(other.LastIanaCodeUpdateTimestamp);
            }

            return Id.Equals(other.Id);
        }

        /// <summary>
        /// Determines whether the current <see cref="TimevaultDocument"/> object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the specified object is equal to the current object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return obj is TimevaultDocument other && Equals(other);
        }

        /// <summary>
        /// Computes a hash code for the <see cref="TimevaultDocument"/> object.
        /// </summary>
        /// <returns>A hash code for the current <see cref="TimevaultDocument"/> object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Id, IanaCode, Location, Coordinate, LastIanaCodeUpdateTimestamp);
        }

        /// <summary>
        /// Returns a string that represents the current <see cref="TimevaultDocument"/> object.
        /// </summary>
        /// <param name="formatProvider">An object that supplies culture-specific formatting information.</param>
        /// <returns>A string representation of the current <see cref="TimevaultDocument"/> object.</returns>
        public string ToString(IFormatProvider formatProvider)
        {
            string timestampString = LastIanaCodeUpdateTimestamp.ToString(formatProvider);
            return $"Id: {Id}, IanaCode: {IanaCode}, Location: {Location}, Coordinate: {Coordinate}, LastIanaCodeUpdateTimestamp: {timestampString}";
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return ToString(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Determines whether two TimevaultDocument objects are equal.
        /// </summary>
        /// <param name="left">The first TimevaultDocument object to compare.</param>
        /// <param name="right">The second TimevaultDocument object to compare.</param>
        /// <returns>True if the two TimevaultDocument objects are equal; otherwise, false.</returns>
        public static bool operator ==(TimevaultDocument? left, TimevaultDocument? right)
        {
            return left?.Equals(right) ?? right is null;
        }

        /// <summary>
        /// Determines whether two <see cref="TimevaultDocument"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="TimevaultDocument"/> to compare.</param>
        /// <param name="right">The second <see cref="TimevaultDocument"/> to compare.</param>
        /// <returns><c>true</c> if the two <see cref="TimevaultDocument"/> objects are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(TimevaultDocument? left, TimevaultDocument? right)
        {
            return !(left == right);
        }
    }
}