// Copyright 2024 Cencora, All rights reserved.
//
// Written by Felix Kahle, A123234, felix.kahle@worldcourier.de

using Azure;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

namespace Cencora.Azure.Timevault
{
    /// <summary>
    /// Represents an API response.
    /// </summary>
    public class ApiResponse : IEquatable<ApiResponse>
    {
        /// <summary>
        /// Gets or sets the error message associated with the API response.
        /// </summary>
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Gets or sets the status code of the API response.
        /// </summary>
        public int StatusCode { get; set; }

        /// <summary>
        /// Gets a value indicating whether the API response is successful.
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && ErrorMessage == null;

        /// <summary>
        /// Private constructor to prevent instantiation of the class.
        /// </summary>
        private ApiResponse()
        {
        }

        /// <summary>
        /// Sets the status code of the API response and returns the modified response.
        /// </summary>
        /// <param name="statusCode">The status code to set.</param>
        /// <returns>The modified API response.</returns>
        public ApiResponse WithStatusCode(int statusCode)
        {
            StatusCode = statusCode;
            return this;
        }

        /// <summary>
        /// Sets the error message of the API response and returns the modified response.
        /// </summary>
        /// <param name="errorMessage">The error message to set.</param>
        /// <returns>The modified API response.</returns>
        public ApiResponse WithErrorMessage(string? errorMessage)
        {
            ErrorMessage = errorMessage;
            return this;
        }

        /// <summary>
        /// Determines whether the current API response is equal to another API response.
        /// </summary>
        /// <param name="other">The API response to compare with.</param>
        /// <returns>True if the API responses are equal; otherwise, false.</returns>
        public bool Equals(ApiResponse? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return string.Equals(ErrorMessage, other.ErrorMessage) && StatusCode == other.StatusCode;
        }

        /// <summary>
        /// Implicitly converts an API response to a boolean value indicating whether the response is successful.
        /// </summary>
        /// <param name="response">The API response to convert.</param>
        /// <returns>True if the API response is successful; otherwise, false.</returns>
        public static implicit operator bool(ApiResponse response)
        {
            return response.IsSuccess;
        }

        /// <summary>
        /// Creates a successful API response with the specified status code.
        /// </summary>
        /// <param name="statusCode">The status code of the response. Default is 200.</param>
        /// <returns>The created successful API response.</returns>
        public static ApiResponse Success(int statusCode = 200)
        {
            return new ApiResponse
            {
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates an error API response with the specified error message and status code.
        /// </summary>
        /// <param name="error">The error message of the response.</param>
        /// <param name="statusCode">The status code of the response. Default is 500.</param>
        /// <returns>The created error API response.</returns>
        public static ApiResponse Error(string? error, int statusCode = 500)
        {
            return new ApiResponse
            {
                ErrorMessage = error,
                StatusCode = statusCode
            };
        }

        /// <inheritdoc/>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ApiResponse);
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return HashCode.Combine(ErrorMessage, StatusCode);
        }

        /// <summary>
        /// Determines whether two API responses are equal.
        /// </summary>
        /// <param name="left">The first API response to compare.</param>
        /// <param name="right">The second API response to compare.</param>
        /// <returns>True if the API responses are equal; otherwise, false.</returns>
        public static bool operator ==(ApiResponse left, ApiResponse right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two API responses are not equal.
        /// </summary>
        /// <param name="left">The first API response to compare.</param>
        /// <param name="right">The second API response to compare.</param>
        /// <returns>True if the API responses are not equal; otherwise, false.</returns>
        public static bool operator !=(ApiResponse left, ApiResponse right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Represents a generic API response.
    /// </summary>
    /// <typeparam name="T">The type of the value in the response.</typeparam>
    public class ApiResponse<T> : IEquatable<ApiResponse<T>>
    {
        /// <summary>
        /// Gets or sets the value of the ApiResponse.
        /// </summary>
        private T? _value;

        /// <summary>
        /// Gets or sets the value of the API response.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when the response is not successful.</exception>
        public T Value
        {
            get
            {
                if (!IsSuccess || _value == null)
                {
                    throw new InvalidOperationException("The response is not successful.");
                }
                return _value;
            }
            private set
            {
                _value = value;
            }
        }

        /// <summary>
        /// Gets or sets the error message associated with the API response.
        /// </summary>
        public string? ErrorMessage { get; private set; }

        /// <summary>
        /// Gets or sets the status code of the API response.
        /// </summary>
        public int StatusCode { get; private set; }

        /// <summary>
        /// Gets a value indicating whether the API response is successful.
        /// </summary>
        public bool IsSuccess => StatusCode >= 200 && StatusCode < 300 && ErrorMessage == null && _value != null;

        /// <summary>
        /// Private constructor to prevent instantiation of the class.
        /// </summary>
        private ApiResponse()
        {
        }

        /// <summary>
        /// Sets the status code of the API response.
        /// </summary>
        /// <param name="statusCode">The status code to set.</param>
        /// <returns>The updated <see cref="ApiResponse{T}"/> instance.</returns>
        public ApiResponse<T> WithStatusCode(int statusCode)
        {
            StatusCode = statusCode;
            return this;
        }

        /// <summary>
        /// Sets the error message for the API response.
        /// </summary>
        /// <param name="errorMessage">The error message to set.</param>
        /// <returns>The modified API response.</returns>
        public ApiResponse<T> WithErrorMessage(string? errorMessage)
        {
            ErrorMessage = errorMessage;
            return this;
        }

        /// <summary>
        /// Sets the value of the ApiResponse.
        /// </summary>
        /// <param name="value">The value to set.</param>
        /// <returns>The ApiResponse instance.</returns>
        public ApiResponse<T> WithValue(T value)
        {
            _value = value;
            return this;
        }

        /// <summary>
        /// Determines whether the current <see cref="ApiResponse{T}"/> object is equal to another <see cref="ApiResponse{T}"/> object.
        /// </summary>
        /// <param name="other">The <see cref="ApiResponse{T}"/> object to compare with the current object.</param>
        /// <returns><c>true</c> if the current object is equal to the <paramref name="other"/> object; otherwise, <c>false</c>.</returns>
        public bool Equals(ApiResponse<T>? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            if (GetType() != other.GetType()) return false;

            return EqualityComparer<T>.Default.Equals(_value, other._value)
               && string.Equals(ErrorMessage, other.ErrorMessage)
               && StatusCode == other.StatusCode;
        }

        /// <summary>
        /// Determines whether the current instance of <see cref="ApiResponse{T}"/> is equal to another object.
        /// </summary>
        /// <param name="obj">The object to compare with the current instance.</param>
        /// <returns><c>true</c> if the current instance is equal to the specified object; otherwise, <c>false</c>.</returns>
        public override bool Equals(object? obj)
        {
            return Equals(obj as ApiResponse<T>);
        }

        /// <summary>
        /// Serves as a hash function for the current <see cref="ApiResponse"/> object.
        /// </summary>
        /// <returns>A hash code for the current <see cref="ApiResponse"/> object.</returns>
        public override int GetHashCode()
        {
            return HashCode.Combine(_value, ErrorMessage, StatusCode);
        }

        /// <summary>
        /// Creates a successful API response with the specified value and status code.
        /// </summary>
        /// <typeparam name="T">The type of the value.</typeparam>
        /// <param name="value">The value to include in the response.</param>
        /// <param name="statusCode">The status code of the response (default is 200).</param>
        /// <returns>An instance of <see cref="ApiResponse{T}"/> representing a successful response.</returns>
        public static ApiResponse<T> Success(T value, int statusCode = 200)
        {
            return new ApiResponse<T>
            {
                _value = value,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Creates an error response with the specified error message and status code.
        /// </summary>
        /// <param name="error">The error message.</param>
        /// <param name="statusCode">The status code.</param>
        /// <returns>An instance of <see cref="ApiResponse{T}"/> representing the error response.</returns>
        public static ApiResponse<T> Error(string? error, int statusCode = 500)
        {
            return new ApiResponse<T>
            {
                ErrorMessage = error,
                StatusCode = statusCode
            };
        }

        /// <summary>
        /// Represents an API response.
        /// </summary>
        /// <typeparam name="T">The type of the response data.</typeparam>
        public static implicit operator bool(ApiResponse<T> response)
        {
            return response.IsSuccess;
        }

        /// <summary>
        /// Determines whether two instances of <see cref="ApiResponse{T}"/> are equal.
        /// </summary>
        /// <typeparam name="T">The type of the response payload.</typeparam>
        /// <param name="left">The first <see cref="ApiResponse{T}"/> to compare.</param>
        /// <param name="right">The second <see cref="ApiResponse{T}"/> to compare.</param>
        /// <returns><c>true</c> if the two instances are equal; otherwise, <c>false</c>.</returns>
        public static bool operator ==(ApiResponse<T> left, ApiResponse<T> right)
        {
            return Equals(left, right);
        }

        /// <summary>
        /// Determines whether two instances of <see cref="ApiResponse{T}"/> are not equal.
        /// </summary>
        /// <param name="left">The first <see cref="ApiResponse{T}"/> to compare.</param>
        /// <param name="right">The second <see cref="ApiResponse{T}"/> to compare.</param>
        /// <returns><c>true</c> if the two instances are not equal; otherwise, <c>false</c>.</returns>
        public static bool operator !=(ApiResponse<T> left, ApiResponse<T> right)
        {
            return !Equals(left, right);
        }
    }

    /// <summary>
    /// Provides extension methods for the <see cref="ApiResponse{T}"/> class.
    /// </summary>
    public static class ApiResponseExtensions
    {
        /// <summary>
        /// Converts an <see cref="Exception"/> to an <see cref="ApiResponse{T}"/> with an error message and status code.
        /// </summary>
        /// <typeparam name="T">The type of the response data.</typeparam>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="statusCode">The status code to include in the response. Default is 500 (Internal Server Error).</param>
        /// <returns>An <see cref="ApiResponse{T}"/> object representing the error response.</returns>
        public static ApiResponse<T> ToApiResponse<T>(this Exception exception, int statusCode = 500)
        {
            return ApiResponse<T>.Error(exception.Message, statusCode);
        }

        /// <summary>
        /// Converts an <see cref="Exception"/> to an <see cref="ApiResponse"/> object with the specified status code.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="statusCode">The status code to set for the response. Default is 500 (Internal Server Error).</param>
        /// <returns>An <see cref="ApiResponse"/> object representing the error response.</returns>
        public static ApiResponse ToApiResponse(this Exception exception, int statusCode = 500)
        {
            return ApiResponse.Error(exception.Message, statusCode);
        }

        /// <summary>
        /// Converts a <see cref="CosmosException"/> to an <see cref="ApiResponse{T}"/> object.
        /// </summary>
        /// <typeparam name="T">The type of the response payload.</typeparam>
        /// <param name="exception">The <see cref="CosmosException"/> to convert.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> object representing the converted exception.</returns>
        public static ApiResponse<T> ToApiResponse<T>(this CosmosException exception)
        {
            return ApiResponse<T>.Error(exception.Message, (int)exception.StatusCode);
        }

        /// <summary>
        /// Converts a <see cref="CosmosException"/> to an <see cref="ApiResponse"/>.
        /// </summary>
        /// <param name="exception">The <see cref="CosmosException"/> to convert.</param>
        /// <returns>An <see cref="ApiResponse"/> representing the converted exception.</returns>
        public static ApiResponse ToApiResponse(this CosmosException exception)
        {
            return ApiResponse.Error(exception.Message, (int)exception.StatusCode);
        }

        /// <summary>
        /// Converts a <see cref="RequestFailedException"/> to an <see cref="ApiResponse{T}"/> object.
        /// </summary>
        /// <typeparam name="T">The type of the response object.</typeparam>
        /// <param name="exception">The <see cref="RequestFailedException"/> to convert.</param>
        /// <returns>An <see cref="ApiResponse{T}"/> object representing the error.</returns>
        public static ApiResponse<T> ToApiResponse<T>(this RequestFailedException exception)
        {
            return ApiResponse<T>.Error(exception.Message, exception.Status);
        }

        /// <summary>
        /// Converts a <see cref="RequestFailedException"/> to an <see cref="ApiResponse"/> object.
        /// </summary>
        /// <param name="exception">The <see cref="RequestFailedException"/> to convert.</param>
        /// <returns>An <see cref="ApiResponse"/> object representing the error.</returns>
        public static ApiResponse ToApiResponse(this RequestFailedException exception)
        {
            return ApiResponse.Error(exception.Message, exception.Status);
        }

        /// <summary>
        /// Logs the response information to the provided logger.
        /// </summary>
        /// <typeparam name="T">The type of the response data.</typeparam>
        /// <param name="response">The ApiResponse object.</param>
        /// <param name="logger">The logger to use for logging.</param>
        public static void LogResponse<T>(this ApiResponse<T> response, ILogger logger)
        {
            if (!response.IsSuccess)
            {
                logger.LogError($"Error Response: {response.ErrorMessage} Status Code: {response.StatusCode}");
            }
            else
            {
                logger.LogInformation($"Success Response: {response.StatusCode}");
            }
        }

        /// <summary>
        /// Logs the response information.
        /// </summary>
        /// <param name="response">The <see cref="ApiResponse"/> object.</param>
        /// <param name="logger">The logger instance.</param>
        public static void LogResponse(this ApiResponse response, ILogger logger)
        {
            if (!response.IsSuccess)
            {
                logger.LogError($"Error Response: {response.ErrorMessage} Status Code: {response.StatusCode}");
            }
            else
            {
                logger.LogInformation($"Success Response: {response.StatusCode}");
            }
        }
    }
}