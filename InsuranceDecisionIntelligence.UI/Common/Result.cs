using System;
using System.Collections.Generic;
using System.Linq;

namespace InsuranceDecisionIntelligence.UI.Common
{
    public class Result
    {
        public bool IsSuccess { get; }
        public bool IsFailure => !IsSuccess;
        public UIError Error { get; }

        protected Result(bool isSuccess, UIError error)
        {
            if (isSuccess && error != null)
                throw new InvalidOperationException();
            if (!isSuccess && error == null)
                throw new InvalidOperationException();

            IsSuccess = isSuccess;
            Error = error;
        }

        public static Result Success() => new Result(true, null);
        public static Result Failure(UIError error) => new Result(false, error);

        public static Result<T> Success<T>(T value) => new Result<T>(value, true, null);
        public static Result<T> Failure<T>(UIError error) => new Result<T>(default, false, error);
    }

    public class Result<T> : Result
    {
        public T Value { get; }

        internal Result(T value, bool isSuccess, UIError error) : base(isSuccess, error)
        {
            Value = value;
        }

        public static implicit operator Result<T>(T value) => Success(value);

        public TResult Match<TResult>(
            Func<T, TResult> onSuccess,
            Func<UIError, TResult> onFailure)
        {
            return IsSuccess ? onSuccess(Value) : onFailure(Error);
        }

        public void Match(
            Action<T> onSuccess,
            Action<UIError> onFailure)
        {
            if (IsSuccess)
                onSuccess(Value);
            else
                onFailure(Error);
        }
    }

    public class UIError
    {
        public string Code { get; }
        public string Message { get; }
        public string Details { get; }
        public Dictionary<string, object> Metadata { get; }

        public UIError(string code, string message, string details = null, Dictionary<string, object> metadata = null)
        {
            Code = code;
            Message = message;
            Details = details;
            Metadata = metadata ?? new Dictionary<string, object>();
        }

        public static UIError NetworkError(string message = "Network connection failed", string details = null)
            => new("NETWORK_ERROR", message, details);

        public static UIError ApiError(string message = "API call failed", string details = null)
            => new("API_ERROR", message, details);

        public static UIError ValidationError(string message = "Validation failed", string details = null)
            => new("VALIDATION_ERROR", message, details);

        public static UIError NotFoundError(string message = "Resource not found", string details = null)
            => new("NOT_FOUND", message, details);

        public static UIError UnauthorizedError(string message = "Unauthorized access", string details = null)
            => new("UNAUTHORIZED", message, details);

        public static UIError InternalError(string message = "Internal error occurred", string details = null)
            => new("INTERNAL_ERROR", message, details);

        public static UIError FileError(string message = "File operation failed", string details = null)
            => new("FILE_ERROR", message, details);

        public static UIError Custom(string code, string message, string details = null, Dictionary<string, object> metadata = null)
            => new(code, message, details, metadata);
    }

    public static class ResultExtensions
    {
        public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, UIError error)
        {
            if (result.IsFailure)
                return result;

            return predicate(result.Value) ? result : Result.Failure<T>(error);
        }

        public static Result<U> Map<T, U>(this Result<T> result, Func<T, U> mapper)
        {
            return result.IsSuccess ? Result.Success(mapper(result.Value)) : Result.Failure<U>(result.Error);
        }

        public static async Task<Result<T>> Ensure<T>(this Task<Result<T>> resultTask, Func<T, bool> predicate, UIError error)
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
