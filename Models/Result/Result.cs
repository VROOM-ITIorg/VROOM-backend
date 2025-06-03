using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VROOM.Models
{
    public class Result
    {
        public readonly string Error;

        public bool IsSuccess { get; private set; }
        public string Message { get; private set; }

        private Result(bool isSuccess, string message)
        {
            IsSuccess = isSuccess;
            Message = message;
        }

        public static Result Success(string message) => new Result(true, message);
        public static Result Failure(string message) => new Result(false, message);
    }
}
