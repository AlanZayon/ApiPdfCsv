using System;
using System.Collections.Generic;

namespace ApiPdfCsv.Shared.Results
{
    public class Result<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<ValidationError> Errors { get; set; } = new();
        public string? Exception { get; set; }

        public static Result<T> SuccessResult(T data, string? message = null)
        {
            return new Result<T>
            {
                Success = true,
                Data = data,
                Message = message ?? "Operação realizada com sucesso"
            };
        }

        public static Result<T> Failure(string message, List<ValidationError>? errors = null)
        {
            return new Result<T>
            {
                Success = false,
                Message = message,
                Errors = errors ?? new List<ValidationError>()
            };
        }

        public static Result<T> Error(string message, Exception? ex = null)
        {
            return new Result<T>
            {
                Success = false,
                Message = message,
                Exception = ex?.ToString()
            };
        }
    }
}
