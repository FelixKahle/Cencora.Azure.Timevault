// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a document in a time vault with identification, timezone information, physical address, and geographical location.
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
        /// Gets or sets the physical address associated with the document.
        /// </summary>
        public Address Address { get; set; }

        /// <summary>
        /// Gets or sets the geographical location of the document.
        /// </summary>
        public GeoCoordinate Location { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class with default values.
        /// </summary>
        public TimevaultDocument()
        {
            Id = Guid.NewGuid().ToString();
            IanaCode = string.Empty;
            Address = new Address();
            Location = new GeoCoordinate();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class with the specified values.
        /// </summary>
        /// <param name="ianaCode">The IANA timezone code for the document's associated location.</param>
        /// <param name="address">The physical address associated with the document.</param>
        /// <param name="location">The geographical location of the document.</param>
        /// <remarks>
        /// The <see cref="Id"/> property is set to a new unique identifier.
        /// </remarks>
        public TimevaultDocument(string ianaCode, Address address, GeoCoordinate location)
        {
            Id = Guid.NewGuid().ToString();
            IanaCode = ianaCode;
            Address = address;
            Location = location;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimevaultDocument"/> class with the specified values.
        /// </summary>
        /// <param name="id">The unique identifier for the document.</param>
        /// <param name="ianaCode">The IANA timezone code for the document's associated location.</param>
        /// <param name="address">The physical address associated with the document.</param>
        /// <param name="location">The geographical location of the document.</param>
        public TimevaultDocument(string id, string ianaCode, Address address, GeoCoordinate location)
        {
            Id = id;
            IanaCode = ianaCode;
            Address = address;
            Location = location;
        }

        /// <summary>
        /// Determines whether the current <see cref="TimevaultDocument"/> object is equal to another <see cref="TimevaultDocument"/> object.
        /// </summary>
        /// <remarks>
        /// Two <see cref="TimevaultDocument"/> objects are considered equal if their <see cref="Id"/> properties are equal 
        /// or if their <see cref="Id"/> properties are both <c>null</c> or <see cref="string.Empty"/> and 
        /// their <see cref="IanaCode"/>, <see cref="Address"/>, and <see cref="Location"/> properties are equal.
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
                    && Address.Equals(other.Address)
                    && Location.Equals(other.Location);
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
            return HashCode.Combine(Id, IanaCode, Address, Location);
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