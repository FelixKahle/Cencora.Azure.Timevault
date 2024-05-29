// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;
using System.Text.Json;

namespace Cencora.Azure.Timevault.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="GeoCoordinate"/> class.
    /// </summary>
    public class GeoCoordinateTests
    {
        [Fact]
        public void Constructor_With_NoArguments_InitializesToZero()
        {
            var coordinate = new GeoCoordinate();
            Assert.Equal(0, coordinate.Latitude);
            Assert.Equal(0, coordinate.Longitude);
        }

        [Fact]
        public void Constructor_With_LatitudeAndLongitude_InitializesToSpecifiedValues()
        {
            var coordinate = new GeoCoordinate(1.5, 2.5);
            Assert.Equal(1.5, coordinate.Latitude);
            Assert.Equal(2.5, coordinate.Longitude);
        }

        [Fact]
        public void Constructor_With_LatitudeAndLongitudeStrings_InitializesToSpecifiedValues()
        {
            var coordinate = new GeoCoordinate("1.5", "2.5");
            Assert.Equal(1.5, coordinate.Latitude);
            Assert.Equal(2.5, coordinate.Longitude);
        }

        [Fact]
        public void Constructor_With_InvalidLatitudeString_ThrowsArgumentException()
        {
            var exception = Record.Exception(() => new GeoCoordinate("invalid", "2.6", CultureInfo.InvariantCulture));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Constructor_With_InvalidLongitudeString_ThrowsArgumentException()
        {
           var exception = Record.Exception(() => new GeoCoordinate("1.6", "invalid", CultureInfo.InvariantCulture));
           Assert.NotNull(exception);
        }

        [Fact]
        public void TryParse_With_ValidLatitudeAndLongitudeStrings_ParsesSuccessfully()
        {
            var result = GeoCoordinate.TryParse("1.5", "2.6", CultureInfo.InvariantCulture, out var coordinate);
            Assert.True(result);
            Assert.Equal(1.5, coordinate.Latitude);
            Assert.Equal(2.6, coordinate.Longitude);
        }

        [Fact]
        public void TryParse_With_InvalidLatitudeString_FailsToParse()
        {
            var result = GeoCoordinate.TryParse("invalid", "2.6", CultureInfo.InvariantCulture, out var coordinate);
            Assert.False(result);
            Assert.Equal(0, coordinate.Latitude);
            Assert.Equal(0, coordinate.Longitude);
        }

        [Fact]
        public void TryParse_With_InvalidLongitudeString_FailsToParse()
        {
            var result = GeoCoordinate.TryParse("1.5", "invalid", CultureInfo.InvariantCulture, out var coordinate);
            Assert.False(result);
            Assert.Equal(0, coordinate.Latitude);
            Assert.Equal(0, coordinate.Longitude);
        }

        [Fact]
        public void Equals_With_EqualCoordinates_ReturnsTrue()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.6);
            Assert.True(coordinate1.Equals(coordinate2));
            Assert.True(coordinate1 == coordinate2);
            Assert.False(coordinate1 != coordinate2);
        }

        [Fact]
        public void Equals_With_UnequalCoordinates_ReturnsFalse()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.7);
            Assert.False(coordinate1.Equals(coordinate2));
            Assert.False(coordinate1 == coordinate2);
            Assert.True(coordinate1 != coordinate2);
        }

        [Fact]
        public void Equals_With_Null_ReturnsFalse()
        {
            var coordinate = new GeoCoordinate(1.5, 2.6);
            Assert.False(coordinate.Equals(null));
        }

        [Fact]
        public void Equals_With_Object_ReturnsFalse()
        {
            var coordinate = new GeoCoordinate(1.5, 2.6);
            Assert.False(coordinate.Equals(new object()));
        }

        [Fact]
        public void ToString_With_NoArguments_ReturnsFormattedString()
        {
            const double latitude = 1.5;
            const double longitude = 2.6;

            var coordinate = new GeoCoordinate(latitude, longitude);
            Assert.Equal($"Latitude: {latitude}, Longitude: {longitude}", coordinate.ToString());
        }

        [Fact]
        public void ToString_With_FormatProvider_ReturnsFormattedString()
        {
            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            const double latitude = 1.5;
            const double longitude = 2.6;

            var coordinate = new GeoCoordinate(latitude, longitude);
            Assert.Equal($"Latitude: {latitude.ToString(formatProvider)}, Longitude: {longitude.ToString(formatProvider)}",
                coordinate.ToString(formatProvider));
        }

        [Fact]
        public void GetHashCode_With_EqualCoordinates_ReturnsEqualHashCodes()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.6);
            Assert.Equal(coordinate1.GetHashCode(), coordinate2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_With_UnequalCoordinates_ReturnsUnequalHashCodes()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.7);
            Assert.NotEqual(coordinate1.GetHashCode(), coordinate2.GetHashCode());
        }

        [Fact]
        public void Json_SerializesDeserializesCorrectly()
        {
            var coordinate = new GeoCoordinate(1.5, 2.5);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonString = System.Text.Json.JsonSerializer.Serialize(coordinate, options);
            var deserializedCoordinate = System.Text.Json.JsonSerializer.Deserialize<GeoCoordinate>(jsonString, options);

            Assert.Equal(coordinate, deserializedCoordinate);
        }
    }
}