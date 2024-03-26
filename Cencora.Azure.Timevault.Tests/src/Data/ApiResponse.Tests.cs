// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

namespace Cencora.Azure.Timevault.Tests
{
    public class ApiResponseTests
    {
        [Fact]
        public void Success_ReturnsCorrectly()
        {
            ApiResponse response = ApiResponse.Success();

            Assert.True(response.IsSuccess);
            Assert.True(response);
            Assert.Null(response.ErrorMessage);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public void Success_WithStatusCode_ReturnsCorrectly()
        {
            ApiResponse response = ApiResponse.Success(201);

            Assert.True(response.IsSuccess);
            Assert.True(response);
            Assert.Null(response.ErrorMessage);
            Assert.Equal(201, response.StatusCode);
        }

        [Fact]
        public void Error_ReturnsCorrectly()
        {
            ApiResponse response = ApiResponse.Error("Error message");

            Assert.False(response.IsSuccess);
            Assert.False(response);
            Assert.Equal("Error message", response.ErrorMessage);
            Assert.Equal(500, response.StatusCode);
        }

        [Fact]
        public void Error_WithStatusCode_ReturnsCorrectly()
        {
            ApiResponse response = ApiResponse.Error("Error message", 400);

            Assert.False(response.IsSuccess);
            Assert.False(response);
            Assert.Equal("Error message", response.ErrorMessage);
            Assert.Equal(400, response.StatusCode);
        }

        [Fact]
        public void Equals_With_EqualApiResponses_ReturnsTrue()
        {
            ApiResponse response1 = ApiResponse.Success();
            ApiResponse response2 = ApiResponse.Success();
            Assert.True(response1.Equals(response2));
            Assert.True(response1 == response2);
            Assert.False(response1 != response2);

            ApiResponse response3 = ApiResponse.Error("Error message");
            ApiResponse response4 = ApiResponse.Error("Error message");
            Assert.True(response3.Equals(response4));
            Assert.True(response3 == response4);
            Assert.False(response3 != response4);
        }

        [Fact]
        public void Equals_With_UnequalApiResponses_ReturnsFalse()
        {
            ApiResponse response1 = ApiResponse.Success();
            ApiResponse response2 = ApiResponse.Error("Error message");
            Assert.False(response1.Equals(response2));
            Assert.False(response1 == response2);
            Assert.True(response1 != response2);

            ApiResponse response3 = ApiResponse.Success();
            ApiResponse response4 = ApiResponse.Success(201);
            Assert.False(response3.Equals(response4));
            Assert.False(response3 == response4);
            Assert.True(response3 != response4);

            ApiResponse response5 = ApiResponse.Error("Error message");
            ApiResponse response6 = ApiResponse.Error("Another error message");
            Assert.False(response5.Equals(response6));
            Assert.False(response5 == response6);
            Assert.True(response5 != response6);

            ApiResponse response7 = ApiResponse.Error("Error message");
            ApiResponse response8 = ApiResponse.Error("Error message", 400);
            Assert.False(response7.Equals(response8));
            Assert.False(response7 == response8);
            Assert.True(response7 != response8);
        }

        [Fact]
        public void Equals_With_Null_ReturnsFalse()
        {
            ApiResponse response = ApiResponse.Success();
            Assert.False(response.Equals(null));
        }

        [Fact]
        public void GetHashCode_With_EqualApiResponses_ReturnsEqualHashCodes()
        {
            ApiResponse response1 = ApiResponse.Success();
            ApiResponse response2 = ApiResponse.Success();
            Assert.Equal(response1.GetHashCode(), response2.GetHashCode());

            ApiResponse response3 = ApiResponse.Error("Error message");
            ApiResponse response4 = ApiResponse.Error("Error message");
            Assert.Equal(response3.GetHashCode(), response4.GetHashCode());
        }

        [Fact]
        public void GetHashCode_With_UnequalApiResponses_ReturnsUnequalHashCodes()
        {
            ApiResponse response1 = ApiResponse.Success();
            ApiResponse response2 = ApiResponse.Error("Error message");
            Assert.NotEqual(response1.GetHashCode(), response2.GetHashCode());

            ApiResponse response3 = ApiResponse.Success();
            ApiResponse response4 = ApiResponse.Success(201);
            Assert.NotEqual(response3.GetHashCode(), response4.GetHashCode());

            ApiResponse response5 = ApiResponse.Error("Error message");
            ApiResponse response6 = ApiResponse.Error("Another error message");
            Assert.NotEqual(response5.GetHashCode(), response6.GetHashCode());

            ApiResponse response7 = ApiResponse.Error("Error message");
            ApiResponse response8 = ApiResponse.Error("Error message", 400);
            Assert.NotEqual(response7.GetHashCode(), response8.GetHashCode());
        }
    }

    public class GenericApiResponseTests
    {
        [Fact]
        public void Success_ReturnsCorrectly()
        {
            ApiResponse<int> response = ApiResponse<int>.Success(100);

            Assert.Equal(100, response.Value);
            Assert.True(response.IsSuccess);
            Assert.True(response);
            Assert.Null(response.ErrorMessage);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public void Success_WithStatusCode_ReturnsCorrectly()
        {
            ApiResponse<int> response = ApiResponse<int>.Success(201, 200);

            Assert.Equal(201, response.Value);
            Assert.True(response.IsSuccess);
            Assert.True(response);
            Assert.Null(response.ErrorMessage);
            Assert.Equal(200, response.StatusCode);
        }

        [Fact]
        public void Error_ReturnsCorrectly()
        {
            ApiResponse<int> response = ApiResponse<int>.Error("Error message");

            Assert.False(response.IsSuccess);
            Assert.False(response);
            Assert.Equal("Error message", response.ErrorMessage);
            Assert.Equal(500, response.StatusCode);

            Assert.Throws<InvalidOperationException>(() => { var _ = response.Value; });
        }

        [Fact]
        public void Error_WithStatusCode_ReturnsCorrectly()
        {
            ApiResponse<int> response = ApiResponse<int>.Error("Error message", 400);

            Assert.False(response.IsSuccess);
            Assert.False(response);
            Assert.Equal("Error message", response.ErrorMessage);
            Assert.Equal(400, response.StatusCode);

            Assert.Throws<InvalidOperationException>(() => { var _ = response.Value; });
        }

        [Fact]
        public void Equals_With_EqualApiResponses_ReturnsTrue()
        {
            ApiResponse<int> response1 = ApiResponse<int>.Success(6000);
            ApiResponse<int> response2 = ApiResponse<int>.Success(6000);
            Assert.True(response1.Equals(response2));
            Assert.True(response1 == response2);
            Assert.False(response1 != response2);

            ApiResponse<int> response3 = ApiResponse<int>.Error("Error message");
            ApiResponse<int> response4 = ApiResponse<int>.Error("Error message");
            Assert.True(response3.Equals(response4));
            Assert.True(response3 == response4);
            Assert.False(response3 != response4);
        }

        [Fact]
        public void Equals_With_UnequalApiResponses_ReturnsFalse()
        {
            ApiResponse<int> response1 = ApiResponse<int>.Success(6000);
            ApiResponse<int> response2 = ApiResponse<int>.Success(6001);
            Assert.False(response1.Equals(response2));
            Assert.False(response1 == response2);
            Assert.True(response1 != response2);

            ApiResponse<int> response3 = ApiResponse<int>.Success(6000);
            ApiResponse<int> response4 = ApiResponse<int>.Success(6000, 201);
            Assert.False(response3.Equals(response4));
            Assert.False(response3 == response4);
            Assert.True(response3 != response4);

            ApiResponse<int> response5 = ApiResponse<int>.Error("Error message");
            ApiResponse<int> response6 = ApiResponse<int>.Error("Another error message");
            Assert.False(response5.Equals(response6));
            Assert.False(response5 == response6);
            Assert.True(response5 != response6);

            ApiResponse<int> response7 = ApiResponse<int>.Error("Error message", 400);
            ApiResponse<int> response8 = ApiResponse<int>.Error("Error message", 500);
            Assert.False(response7.Equals(response8));
            Assert.False(response7 == response8);
            Assert.True(response7 != response8);
        }

        [Fact]
        public void Equals_With_Null_ReturnsFalse()
        {
            ApiResponse<int> response = ApiResponse<int>.Success(200);
            Assert.False(response.Equals(null));
        }

        [Fact]
        public void GetHashCode_With_EqualApiResponses_ReturnsEqualHashCodes()
        {
            ApiResponse<int> response1 = ApiResponse<int>.Success(200);
            ApiResponse<int> response2 = ApiResponse<int>.Success(200);
            Assert.Equal(response1.GetHashCode(), response2.GetHashCode());

            ApiResponse<int> response3 = ApiResponse<int>.Error("Error message");
            ApiResponse<int> response4 = ApiResponse<int>.Error("Error message");
            Assert.Equal(response3.GetHashCode(), response4.GetHashCode());
        }

        [Fact]
        public void GetHashCode_With_UnequalApiResponses_ReturnsUnequalHashCodes()
        {
            ApiResponse<int> response1 = ApiResponse<int>.Success(400);
            ApiResponse<int> response2 = ApiResponse<int>.Error("Error message");
            Assert.NotEqual(response1.GetHashCode(), response2.GetHashCode());

            ApiResponse<int> response3 = ApiResponse<int>.Success(400);
            ApiResponse<int> response4 = ApiResponse<int>.Success(201);
            Assert.NotEqual(response3.GetHashCode(), response4.GetHashCode());

            ApiResponse<int> response5 = ApiResponse<int>.Error("Error message");
            ApiResponse<int> response6 = ApiResponse<int>.Error("Another error message");
            Assert.NotEqual(response5.GetHashCode(), response6.GetHashCode());

            ApiResponse<int> response7 = ApiResponse<int>.Error("Error message");
            ApiResponse<int> response8 = ApiResponse<int>.Error("Error message", 400);
            Assert.NotEqual(response7.GetHashCode(), response8.GetHashCode());
        }
    }
}