// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents an address.
    /// </summary>
    /// <remarks>
    /// All fields are required and must not be <c>null</c>.
    /// This is due to the way we store the address in the database.
    /// We enforce a unique combination of street, city, state, postal code, and country,
    /// so having any of these fields be <c>null</c> seems like a bad idea.
    /// Missing information should be represented by an empty string.
    /// </remarks>
    public struct Address : IEquatable<Address>
    {
        /// <summary>
        /// Gets or sets the street of the address.
        /// </summary>
        public string Street { get; set; }

        /// <summary>
        /// Gets or sets the city of the address.
        /// </summary>
        public string City { get; set; }

        /// <summary>
        /// Gets or sets the state of the address.
        /// </summary>
        public string State { get; set; }

        /// <summary>
        /// Gets or sets the postal code of the address.
        /// </summary>
        public string PostalCode { get; set; }

        /// <summary>
        /// Gets or sets the country of the address.
        /// </summary>
        public string Country { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> struct with default values.
        /// </summary>
        public Address()
        {
            Street = string.Empty;
            City = string.Empty;
            State = string.Empty;
            PostalCode = string.Empty;
            Country = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Address"/> struct with the specified values.
        /// </summary>
        /// <param name="street">The street of the address.</param>
        /// <param name="city">The city of the address.</param>
        /// <param name="state">The state of the address.</param>
        /// <param name="postalCode">The postal code of the address.</param>
        /// <param name="country">The country of the address.</param>
        public Address(string street, string city, string state, string postalCode, string country)
        {
            Street = street;
            City = city;
            State = state;
            PostalCode = postalCode;
            Country = country;
        }

        /// <summary>
        /// Determines whether the current <see cref="Address"/> object is equal to another <see cref="Address"/> object.
        /// </summary>
        /// <param name="other">The <see cref="Address"/> object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the other object; otherwise, <c>false</c>.</returns>
        public bool Equals(Address other)
        {
            return Street.Equals(other.Street) 
                && City.Equals(other.City) 
                && State.Equals(other.State) 
                && PostalCode.Equals(other.PostalCode) 
                && Country.Equals(other.Country);
        }

        /// <summary>
        /// Determines whether the current <see cref="Address"/> object is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the other object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return obj is Address other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for the current <see cref="Address"/> object.
        /// </summary>
        /// <returns>A hash code for the current object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(Street, City, State, PostalCode, Country);
        }

        /// <summary>
        /// Returns a string representation of the Address object.
        /// </summary>
        /// <remarks>
        /// The string contains the street, city, state, postal code, and country values, labeled with their respective names.
        /// </remarks>
        /// <returns>A string containing the street, city, state, postal code, and country of the address.</returns>
        public override string ToString()
        {
            return $"Street: {Street}, City: {City}, State: {State}, PostalCode: {PostalCode}, Country: {Country}";
        }

        /// <summary>
        /// Generates a query string representation of the address.
        /// </summary>
        /// <returns>A query string representation of the address.</returns>
        public string MapsQueryString()
        {
            var parts = new[] { Street, City, State, PostalCode, Country };
            return string.Join(", ", parts.Where(p => !string.IsNullOrEmpty(p)));
        }

        /// <summary>
        /// Determines whether two <see cref="Address"/> objects are equal.
        /// </summary>
        /// <param name="left">The first <see cref="Address"/> object to compare.</param>
        /// <param name="right">The second <see cref="Address"/> object to compare.</param>
        /// <returns><c>true</c> if the two objects are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(Address left, Address right)
        {
            return left.Equals(right);
        }

        /// <summary>
        /// Determines whether two <see cref="Address"/> objects are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="Address"/> object to compare.</param>
        /// <param name="right">The second <see cref="Address"/> object to compare.</param>
        /// <returns><c>true</c> if the two objects are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(Address left, Address right)
        {
            return !left.Equals(right);
        }
    }
}