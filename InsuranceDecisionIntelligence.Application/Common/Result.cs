using System;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceDecisionIntelligence.Application.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public Error? Error { get; }

        protected Result(bool isSuccess, Error? error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException();
            if (!isSuccess && error == null)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(Error error) => new Result(false, error);

        public static Result<T> Success<T>(T value) => new Result<T>(value, true, null);
        public static Result<T> Failure<T>(Error error) => new Result<T>(default!, false, error);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        internal Result(T value, bool isSuccess, Error? error) : base(isSuccess, error)
        {
            Value = value;
        }

        public static implicit operator Result<T>(T value) => Success(value);

        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<Error, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value!) : onFailure(Error!);
        }

        public void Match(
            Action<T> onSuccess,
            Action<Error> onFailure)
        {
            if (IsSuccess)
                onSuccess(Value!);
            else
                onFailure(Error!);
        }
    }

    public class Error
    {
        public string Code { get; }
        public string Message { get; }
        public Dictionary<string, object> Metadata { get; }

        public Error(string code, string message, Dictionary<string, object> metadata = null)
        {
            Code = code;
            Message = message;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public static Error NotFound(string message = "Resource not found", Dictionary<string, object> metadata = null)
            => new("NOT_FOUND", message, metadata);

        public static Error Validation(string message = "Validation failed", Dictionary<string, object> metadata = null)
            => new("VALIDATION_ERROR", message, metadata);

        public static Error Conflict(string message = "Resource conflict", Dictionary<string, object> metadata = null)
            => new("CONFLICT", message, metadata);

        public static Error Unauthorized(string message = "Unauthorized access", Dictionary<string, object> metadata = null)
            => new("UNAUTHORIZED", message, metadata);

        public static Error Forbidden(string message = "Access forbidden", Dictionary<string, object> metadata = null)
            => new("FORBIDDEN", message, metadata);

        public static Error Internal(string message = "Internal server error", Dictionary<string, object> metadata = null)
            => new("INTERNAL_ERROR", message, metadata);

        public static Error Custom(string code, string message, Dictionary<string, object> metadata = null)
            => new(code, message, metadata);
    }

    public static class ResultExtensions
    {
        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
        {
            if (result.IsFailure)
                return result;

            return predicate(result.Value) ? result : Result.Failure<T>(error);
        }

        public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> mapper)
        {
            if (result.IsSuccess)
                return Result.Success(mapper(result.Value));
            else
                return Result.Failure<U>(result.Error);
        }

        public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, Error error)
        {
            var result = await resultTask;
            return result.Ensure(predicate, error);
        }

        public static async Task<Result<U>> Map<T, U>(this Task<Result<T>> resultTask, Func<T, U> mapper)
        {
            var result = await resultTask;
            return result.Map(mapper);
        }
    }
}
