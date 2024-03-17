// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents the result of converting time between timezones.
    /// </summary>
    public struct ConvertTimeBetweenTimezonesResult : IEquatable<ConvertTimeBetweenTimezonesResult>
    {
        /// <summary>
        /// Gets or sets the location from which the time is converted.
        /// </summary>
        public Location FromLocation { get; set; }

        /// <summary>
        /// Gets or sets the location to which the time is converted.
        /// </summary>
        public Location ToLocation { get; set; }

        /// <summary>
        /// Gets or sets the original time before conversion.
        /// </summary>
        public DateTime FromTime { get; set; }

        /// <summary>
        /// Gets or sets the converted time.
        /// </summary>
        public DateTime ToTime { get; set; }

        /// <summary>
        /// Determines whether this instance is equal to another <see cref="ConvertTimeBetweenTimezonesResult"/> object.
        /// </summary>
        /// <param name="other">The <see cref="ConvertTimeBetweenTimezonesResult"/> to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public bool Equals(ConvertTimeBetweenTimezonesResult other)
        {
            return FromLocation.Equals(other.FromLocation) 
                && ToLocation.Equals(other.ToLocation) 
                && FromTime.Equals(other.FromTime) 
                && ToTime.Equals(other.ToTime);
        }

        /// <summary>
        /// Determines whether this instance is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with this instance.</param>
        /// <returns><c>true</c> if the specified object is equal to this instance; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return obj is ConvertTimeBetweenTimezonesResult other && Equals(other);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A 32-bit signed integer hash code.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(FromLocation, ToLocation, FromTime, ToTime);
        }

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string that represents the current object.</returns>
        public override string ToString()
        {
            return $"FromLocation: {FromLocation}, ToLocation: {ToLocation}, FromTime: {FromTime}, ToTime: {ToTime}";
        }
    }
}