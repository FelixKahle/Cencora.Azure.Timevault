// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents the result of getting the IANA timezone by location.
    /// </summary>
    public struct GetIanaTimezoneByLocationResult : IEquatable<GetIanaTimezoneByLocationResult>
    {
        /// <summary>
        /// Gets or sets the location.
        /// </summary>
        public Location Location { get; set; }

        /// <summary>
        /// Gets or sets the IANA timezone.
        /// </summary>
        public string? IanaTimezone { get; set; }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return obj is GetIanaTimezoneByLocationResult other && Equals(other);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return $"Location: {Location}, IanaTimezone: {IanaTimezone}";
        }

        /// <inheritdoc/>
        public bool Equals(GetIanaTimezoneByLocationResult other)
        {
            return Location.Equals(other.Location) && IanaTimezone == other.IanaTimezone;
        }
    }
}