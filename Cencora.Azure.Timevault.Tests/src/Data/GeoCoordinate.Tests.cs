// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using System.Globalization;

namespace Cencora.Azure.Timevault.Tests
{
    /// <summary>
    /// Contains unit tests for the <see cref="GeoCoordinate"/> class.
    /// </summary>
    [TestFixture]
    internal class GeoCoordinateTests
    {
        [Test]
        public void Constructor_With_NoArguments_InitializesToZero()
        {
            var coordinate = new GeoCoordinate();
            Assert.That(coordinate.Latitude, Is.EqualTo(0));
            Assert.That(coordinate.Longitude, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_With_LatitudeAndLongitude_InitializesToSpecifiedValues()
        {
            var coordinate = new GeoCoordinate(1.5, 2.6);
            Assert.That(coordinate.Latitude, Is.EqualTo(1.5));
            Assert.That(coordinate.Longitude, Is.EqualTo(2.6));
        }

        [Test]
        public void Constructor_With_LatitudeAndLongitudeStrings_InitializesToSpecifiedValues()
        {
            var coordinate = new GeoCoordinate("1.5", "2.6", CultureInfo.InvariantCulture);
            Assert.That(coordinate.Latitude, Is.EqualTo(1.5));
            Assert.That(coordinate.Longitude, Is.EqualTo(2.6));
        }

        [Test]
        public void Constructor_With_InvalidLatitudeString_ThrowsArgumentException()
        {
            Assert.That(() => new GeoCoordinate("invalid", "2.6", CultureInfo.InvariantCulture), Throws.ArgumentException);
        }

        [Test]
        public void Constructor_With_InvalidLongitudeString_ThrowsArgumentException()
        {
            Assert.That(() => new GeoCoordinate("1.5", "invalid", CultureInfo.InvariantCulture), Throws.ArgumentException);
        }

        [Test]
        public void TryParse_With_ValidLatitudeAndLongitudeStrings_ParsesSuccessfully()
        {
            var result = GeoCoordinate.TryParse("1.5", "2.6", CultureInfo.InvariantCulture, out var coordinate);
            Assert.That(result, Is.True);
            Assert.That(coordinate.Latitude, Is.EqualTo(1.5));
            Assert.That(coordinate.Longitude, Is.EqualTo(2.6));
        }

        [Test]
        public void TryParse_With_InvalidLatitudeString_FailsToParse()
        {
            var result = GeoCoordinate.TryParse("invalid", "2.6", CultureInfo.InvariantCulture, out var coordinate);
            Assert.That(result, Is.False);
            Assert.That(coordinate.Latitude, Is.EqualTo(0));
            Assert.That(coordinate.Longitude, Is.EqualTo(0));
        }

        [Test]
        public void TryParse_With_InvalidLongitudeString_FailsToParse()
        {
            var result = GeoCoordinate.TryParse("1.5", "invalid", CultureInfo.InvariantCulture, out var coordinate);
            Assert.That(result, Is.False);
            Assert.That(coordinate.Latitude, Is.EqualTo(0));
            Assert.That(coordinate.Longitude, Is.EqualTo(0));
        }

        [Test]
        public void Equals_With_EqualCoordinates_ReturnsTrue()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.6);
            Assert.That(coordinate1.Equals(coordinate2), Is.True);
            Assert.That(coordinate1 == coordinate2, Is.True);
            Assert.That(coordinate1 != coordinate2, Is.False);
        }

        [Test]
        public void Equals_With_UnequalCoordinates_ReturnsFalse()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.7);
            Assert.That(coordinate1.Equals(coordinate2), Is.False);
            Assert.That(coordinate1 == coordinate2, Is.False);
            Assert.That(coordinate1 != coordinate2, Is.True);
        }

        [Test]
        public void ToString_With_NoArguments_ReturnsFormattedString()
        {
            const double latitude = 1.5;
            const double longitude = 2.6;

            var coordinate = new GeoCoordinate(latitude, longitude);
            Assert.That(coordinate.ToString(), Is.EqualTo($"Latitude: {latitude}, Longitude: {longitude}"));
        }

        [Test]
        public void ToString_With_FormatProvider_ReturnsFormattedString()
        {
            IFormatProvider formatProvider = CultureInfo.InvariantCulture;
            const double latitude = 1.5;
            const double longitude = 2.6;

            var coordinate = new GeoCoordinate(latitude, longitude);
            Assert.That(coordinate.ToString(formatProvider), 
                Is.EqualTo($"Latitude: {latitude.ToString(formatProvider)}, Longitude: {longitude.ToString(formatProvider)}"));
        }

        [Test]
        public void GetHashCode_With_EqualCoordinates_ReturnsEqualHashCodes()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.6);
            Assert.That(coordinate1.GetHashCode(), Is.EqualTo(coordinate2.GetHashCode()));
        }

        [Test]
        public void GetHashCode_With_UnequalCoordinates_ReturnsUnequalHashCodes()
        {
            var coordinate1 = new GeoCoordinate(1.5, 2.6);
            var coordinate2 = new GeoCoordinate(1.5, 2.7);
            Assert.That(coordinate1.GetHashCode(), Is.Not.EqualTo(coordinate2.GetHashCode()));
        }
    }
}