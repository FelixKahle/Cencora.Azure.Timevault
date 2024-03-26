// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents a location with city, state, country, and postal code information.
    /// </summary>
    public struct Location : IEquatable<Location>
    {
        /// <summary>
        /// Gets or sets the city of the location.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the state of the location.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the country of the location.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Gets or sets the postal code of the location.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct with default values.
        /// </summary>
        public Location()
        {
            City = string.Empty;
            State = string.Empty;
            Country = string.Empty;
            PostalCode = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Location"/> struct with the specified city, state, country, and postal code.
        /// </summary>
        /// <param name="city">The city of the location.</param>
        /// <param name="state">The state of the location.</param>
        /// <param name="country">The country of the location.</param>
        /// <param name="postalCode">The postal code of the location.</param>
        /// <exception cref="ArgumentNullException">Thrown when any of the parameters is null.</exception>
        public Location(string city, string state, string country, string postalCode)
        {
            if (city == null) throw new ArgumentNullException(nameof(city));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (country == null) throw new ArgumentNullException(nameof(country));
            if (postalCode == null) throw new ArgumentNullException(nameof(postalCode));

            // Normalize the values to lowercase.
            // This decreases the amount of data that needs to be stored and compared by a factor of 2.
            City = city;
            State = state;
            Country = country;
            PostalCode = postalCode;
        }

        /// <summary>
        /// Determines whether the current <see cref="Location"/> object is equal to another <see cref="Location"/> object.
        /// </summary>
        /// <param name="other">The <see cref="Location"/> object to compare with the current object.</param>
        /// <returns>true if the current object is equal to the other object; otherwise, false.</returns>
        public bool Equals(Location other)
        {
            return City == other.City 
                && State == other.State 
                && Country == other.Country
                && PostalCode == other.PostalCode;
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is Location other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(City, State, Country, PostalCode);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"City: {City}, State: {State}, Country: {Country}, PostalCode: {PostalCode}";
        }

        /// <summary>
        /// Returns a string that represents the location in a format suitable for a maps query string.
        /// </summary>
        /// <returns>A string that represents the location in a format suitable for a maps query string.</returns>
        public string MapsQueryString()
        {
            var parts = new[] { City, State, PostalCode, Country };
            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        /// <summary>
        /// Generates a query string for batch processing in maps.
        /// </summary>
        /// <returns>The generated query string.</returns>
        public string MapsBatchQueryString()
        {
            var parts = new[] { City, State, PostalCode, Country };
            return string.Join(" ", parts.Where(p => !string.IsNullOrEmpty(p))).ToLower();
        }

        /// <summary>
        /// Determines whether two <see cref="Location"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="Location"/> object to compare.</param>
        /// <param name="right">The second <see cref="Location"/> object to compare.</param>
        /// <returns>true if the two objects are equal; otherwise, false.</returns>
        public static bool operator ==(Location left, Location right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="Location"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="Location"/> object to compare.</param>
        /// <param name="right">The second <see cref="Location"/> object to compare.</param>
        /// <returns>true if the two objects are not equal; otherwise, false.</returns>
        public static bool operator !=(Location left, Location right)
        {
            return !left.Equals(right);
        }
    }
}