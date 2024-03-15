// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Text.Json;

namespace Cencora.Azure.Timevault.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="TimevaultDocument"/> class.
    /// </summary>
    public class TimevaultDocumentTests
    {
        private Location _location;
        private GeoCoordinate _coordinate;

        public TimevaultDocumentTests()
        {
            _location = new Location
            {
                City = "Anytown",
                State = "NY",
                PostalCode = "12345",
                Country = "USA"
            };

            _coordinate = new GeoCoordinate
            {
                Latitude = 40.7128,
                Longitude = -74.0060
            };
        }

        [Fact]
        public void Constructor_With_NoArguments_InitializesToEmpty()
        {
            var location = new Location();
            var coordinate = new GeoCoordinate();

            var document = new TimevaultDocument();
            Assert.Equal(string.Empty, document.Id);
            Assert.Equal(string.Empty, document.IanaCode);
            Assert.Equal(location, document.Location);
            Assert.Equal(coordinate, document.Coordinate);
        }

        [Fact]
        public void Constructor_With_IdIanaCodeLocationAndCoordinate_InitializesToSpecifiedValues()
        {
            var document = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            Assert.Equal("123", document.Id);
            Assert.Equal("America/New_York", document.IanaCode);
            Assert.Equal(_location, document.Location);
            Assert.Equal(_coordinate, document.Coordinate);
        }

        [Fact]
        public void Constructor_Throws_WhenInvalidIdOrIanaCode()
        {
            Assert.Throws<ArgumentException>(() => new TimevaultDocument(null!, _location, _coordinate));
            Assert.Throws<ArgumentException>(() => new TimevaultDocument(string.Empty, _location, _coordinate));

            Assert.Throws<ArgumentException>(() => new TimevaultDocument("123", null!, _location, _coordinate));
            Assert.Throws<ArgumentException>(() => new TimevaultDocument("123", string.Empty, _location, _coordinate));
        }

        [Fact]
        public void Equals_With_EqualDocuments_ReturnsTrue()
        {
            var document1 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            var document2 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            Assert.True(document1.Equals(document2));
            Assert.True(document1 == document2);
            Assert.False(document1 != document2);
        }

        [Fact]
        public void Equals_With_EqualId_ReturnsTrue()
        {
            var document1 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            var document2 = new TimevaultDocument("123", "America/Chicago", _location, _coordinate);
            Assert.True(document1.Equals(document2));
            Assert.True(document1 == document2);
            Assert.False(document1 != document2);
        }

        [Fact]
        public void Equals_With_UnequalDocuments_ReturnsFalse()
        {
            var document1 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            var document2 = new TimevaultDocument("456", "America/New_York", _location, _coordinate);
            Assert.False(document1.Equals(document2));
            Assert.False(document1 == document2);
            Assert.True(document1 != document2);
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            string locationString = _location.ToString();
            string coordinateString = _coordinate.ToString();

            var document = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            string expected = $"Id: 123, IanaCode: America/New_York, Location: {locationString}, Coordinate: {coordinateString}";
            Assert.Equal(expected, document.ToString());
        }

        [Fact]
        public void GetHashCode_ReturnsSameValueForEqualDocuments()
        {
            var document1 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            var document2 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            Assert.Equal(document1.GetHashCode(), document2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_ReturnsDifferentValueForUnequalDocuments()
        {
            var document1 = new TimevaultDocument("123", "America/New_York", _location, _coordinate);
            var document2 = new TimevaultDocument("456", "America/New_York", _location, _coordinate);
            Assert.NotEqual(document1.GetHashCode(), document2.GetHashCode());
        }

        [Fact]
        public void Json_SerializesDeserializesCorrectly()
        {
            var document = new TimevaultDocument("America/New_York", _location, _coordinate);

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };
            var jsonString   = System.Text.Json.JsonSerializer.Serialize(document, options);
            var deserializedDocument = System.Text.Json.JsonSerializer.Deserialize<TimevaultDocument>(jsonString, options);

            Assert.NotNull(deserializedDocument);
            Assert.Equal(document, deserializedDocument);
        }
    }
}