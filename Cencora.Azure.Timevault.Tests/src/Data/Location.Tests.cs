// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;
using System.Text.Json;

namespace Cencora.Azure.Timevault.Tests
{
    public class LocationTests
    {
        [Fact]
        public void Constructor_With_NoArguments_InitializesToEmpty()
        {
            var location = new Location();
            Assert.Equal(string.Empty, location.City);
            Assert.Equal(string.Empty, location.State);
            Assert.Equal(string.Empty, location.Country);
            Assert.Equal(string.Empty, location.PostalCode);
        }

        [Fact]
        public void Constructor_With_CityStateCountryAndPostalCode_InitializesToSpecifiedValues()
        {
            var location = new Location("city", "state", "country", "postalCode");
            Assert.Equal("city", location.City);
            Assert.Equal("state", location.State);
            Assert.Equal("country", location.Country);
            Assert.Equal("postalCode", location.PostalCode);
        }

        [Fact]
        public void Constructor_With_NullCity_ThrowsArgumentNullException()
        {
            var exception = Record.Exception(() => new Location(null!, "state", "country", "postalCode"));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Constructor_With_NullState_ThrowsArgumentNullException()
        {
            var exception = Record.Exception(() => new Location("city", null!, "country", "postalCode"));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Constructor_With_NullCountry_ThrowsArgumentNullException()
        {
            var exception = Record.Exception(() => new Location("city", "state", null!, "postalCode"));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Constructor_With_NullPostalCode_ThrowsArgumentNullException()
        {
            var exception = Record.Exception(() => new Location("city", "state", "country", null!));
            Assert.NotNull(exception);
        }

        [Fact]
        public void Equals_With_EqualLocations_ReturnsTrue()
        {
            var location1 = new Location("city", "state", "country", "postalCode");
            var location2 = new Location("city", "state", "country", "postalCode");
            Assert.True(location1.Equals(location2));
            Assert.True(location1 == location2);
            Assert.False(location1 != location2);
        }

        [Fact]
        public void Equals_With_UnequalLocations_ReturnsFalse()
        {
            var location1 = new Location("city", "state", "country", "postalCode");
            var location2 = new Location("city", "state", "country", "postalCode2");
            Assert.False(location1.Equals(location2));
            Assert.False(location1 == location2);
            Assert.True(location1 != location2);
        }

        [Fact]
        public void Equals_With_Null_ReturnsFalse()
        {
            var location = new Location("city", "state", "country", "postalCode");
            Assert.False(location.Equals(null));
        }

        [Fact]
        public void Equals_With_Object_ReturnsFalse()
        {
            var location = new Location("city", "state", "country", "postalCode");
            Assert.False(location.Equals(new object()));
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            var location = new Location("city", "state", "country", "postalCode");
            Assert.Equal("City: city, State: state, Country: country, PostalCode: postalCode", location.ToString());
        }

        [Fact]
        public void GetHashCode_ReturnsSameValueForEqualLocations()
        {
            var location1 = new Location("city", "state", "country", "postalCode");
            var location2 = new Location("city", "state", "country", "postalCode");
            Assert.Equal(location1.GetHashCode(), location2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ReturnsUnequalValueForUnequalLocations()
        {
            var location1 = new Location("city", "state", "country", "postalCode");
            var location2 = new Location("city", "state", "country", "postalCode2");
            Assert.NotEqual(location1.GetHashCode(), location2.GetHashCode());
        }

        [Fact]
        public void Json_SerializesDeserializesCorrectly()
        {
            var location = new Location("city", "state", "country", "postalCode");
            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonString = System.Text.Json.JsonSerializer.Serialize(location, options);
            var deserializedLocation = System.Text.Json.JsonSerializer.Deserialize<Location>(jsonString, options);

            Assert.Equal(location, deserializedLocation);
        }
    }
}