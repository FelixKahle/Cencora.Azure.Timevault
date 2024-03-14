// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Text.Json;

namespace Cencora.Azure.Timevault.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="Address"/> class.
    /// </summary>
    public class AddressTests
    {
        [Fact]
        public void Constructor_With_NoArguments_InitializesToEmptyStrings()
        {
            var address = new Address();
            Assert.Equal(string.Empty, address.Street);
            Assert.Equal(string.Empty, address.City);
            Assert.Equal(string.Empty, address.State);
            Assert.Equal(string.Empty, address.PostalCode);
            Assert.Equal(string.Empty, address.Country);
        }

        [Fact]
        public void Constructor_With_StreetCityStatePostalCodeAndCountry_InitializesToSpecifiedValues()
        {
            var address = new Address("street", "city", "state", "postalCode", "country");
            Assert.Equal("street", address.Street);
            Assert.Equal("city", address.City);
            Assert.Equal("state", address.State);
            Assert.Equal("postalCode", address.PostalCode);
            Assert.Equal("country", address.Country);
        }

        [Fact]
        public void Constructor_With_NullStreet_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Address(null!, "city", "state", "postalCode", "country"));
        }

        [Fact]
        public void Constructor_With_NullCity_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Address("street", null!, "state", "postalCode", "country"));
        }     

        [Fact]
        public void Constructor_With_NullState_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Address("street", "city", null!, "postalCode", "country"));
        }

        [Fact]
        public void Constructor_With_NullPostalCode_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Address("street", "city", "state", null!, "country"));
        }

        [Fact]
        public void Constructor_With_NullCountry_ThrowsArgumentNullException()
        {
            Assert.Throws<ArgumentNullException>(() => new Address("street", "city", "state", "postalCode", null!));
        }

        [Fact]
        public void Constructor_With_EmptyStreet_InitializesToSpecifiedValues()
        {
            var address = new Address(string.Empty, "city", "state", "postalCode", "country");
            Assert.Equal(string.Empty, address.Street);
            Assert.Equal("city", address.City);
            Assert.Equal("state", address.State);
            Assert.Equal("postalCode", address.PostalCode);
            Assert.Equal("country", address.Country);
        }

        [Fact]
        public void Constructor_With_EmptyCity_InitializesToSpecifiedValues()
        {
            var address = new Address("street", string.Empty, "state", "postalCode", "country");
            Assert.Equal("street", address.Street);
            Assert.Equal(string.Empty, address.City);
            Assert.Equal("state", address.State);
            Assert.Equal("postalCode", address.PostalCode);
            Assert.Equal("country", address.Country);
        }

        [Fact]
        public void Constructor_With_EmptyState_InitializesToSpecifiedValues()
        {
            var address = new Address("street", "city", string.Empty, "postalCode", "country");
            Assert.Equal("street", address.Street);
            Assert.Equal("city", address.City);
            Assert.Equal(string.Empty, address.State);
            Assert.Equal("postalCode", address.PostalCode);
            Assert.Equal("country", address.Country);
        }

        [Fact]
        public void Constructor_With_EmptyPostalCode_InitializesToSpecifiedValues()
        {
            var address = new Address("street", "city", "state", string.Empty, "country");
            Assert.Equal("street", address.Street);
            Assert.Equal("city", address.City);
            Assert.Equal("state", address.State);
            Assert.Equal(string.Empty, address.PostalCode);
            Assert.Equal("country", address.Country);
        }

        [Fact]
        public void Constructor_With_EmptyCountry_InitializesToSpecifiedValues()
        {
            var address = new Address("street", "city", "state", "postalCode", string.Empty);
            Assert.Equal("street", address.Street);
            Assert.Equal("city", address.City);
            Assert.Equal("state", address.State);
            Assert.Equal("postalCode", address.PostalCode);
            Assert.Equal(string.Empty, address.Country);
        }

        [Fact]
        public void Equals_With_EqualAddresses_ReturnsTrue()
        {
            var address1 = new Address("street", string.Empty, "state", "postalCode", "country");
            var address2 = new Address("street", string.Empty, "state", "postalCode", "country");
            Assert.True(address1.Equals(address2));
            Assert.True(address1 == address2);
            Assert.False(address1 != address2);
        }

        [Fact]
        public void Equals_With_UnequalAddresses_ReturnsFalse()
        {
            var address1 = new Address("street", string.Empty, "state", "postalCode", "country");
            var address2 = new Address("street1", string.Empty, "state", "postalCode", "country");
            Assert.False(address1.Equals(address2));
            Assert.False(address1 == address2);
            Assert.True(address1 != address2);
        }

        [Fact]
        public void Equals_With_Null_ReturnsFalse()
        {
            var address = new Address("street", string.Empty, "state", "postalCode", "country");
            Assert.False(address.Equals(null));
        }

        [Fact]
        public void Equals_With_Object_ReturnsFalse()
        {
            var address = new Address("street", string.Empty, "state", "postalCode", "country");
            Assert.False(address.Equals(new object()));
        }

        [Fact]
        public void ToString_ReturnsFormattedString()
        {
            string street = "street";
            string city = "city";
            string state = "state";
            string postalCode = "postalCode";
            string country = "country";

            var address = new Address(street, city, state, postalCode, country);
            Assert.Equal($"Street: {street}, City: {city}, State: {state}, PostalCode: {postalCode}, Country: {country}", address.ToString());
        }

        [Fact]
        public void MapsQueryString_With_EmptyStreet_ReturnsFormattedString()
        {
            var address = new Address(string.Empty, "city", "state", "postalCode", "country");
            Assert.Equal("city, state, postalCode, country", address.MapsQueryString());
        }

        [Fact]
        public void MapsQueryString_With_EmptyCityEmptyState_ReturnsFormattedString()
        {
            var address = new Address("street", string.Empty, string.Empty, "postalCode", "country");
            Assert.Equal("street, postalCode, country", address.MapsQueryString());
        }

        [Fact]
        public void GetHashCode_With_EqualAddresses_ReturnsEqualHashCodes()
        {
            var address1 = new Address("street", "city", "state", "postalCode", "country");
            var address2 = new Address("street", "city", "state", "postalCode", "country");
            Assert.Equal(address1.GetHashCode(), address2.GetHashCode());
        }

        [Fact]
        public void GetHashCode_With_UnequalAddresses_ReturnsUnequalHashCodes()
        {
            var address1 = new Address("street", "city", "state", "postalCode", "country");
            var address2 = new Address("street1", "city", "state", "postalCode", "country");
            Assert.NotEqual(address1.GetHashCode(), address2.GetHashCode());
        }

        [Fact]
        public void Json_SerializesDeserializesCorrectly()
        {
            var address = new Address
            {
                Street = "123 Main St",
                City = "Anytown",
                State = "NY",
                PostalCode = "12345",
                Country = "USA"
            };

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            var jsonString   = System.Text.Json.JsonSerializer.Serialize(address, options);
            var deserializedAddress = System.Text.Json.JsonSerializer.Deserialize<Address>(jsonString, options);
            
            Assert.Equal(address, deserializedAddress);
        }
    }
}