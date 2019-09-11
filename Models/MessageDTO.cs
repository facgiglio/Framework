using System;
using System.Collections.Generic;
using System.Linq;

namespace Framework.Models
{
    public class MessageDTO
    {
        public string Result {get;set;}
        public string Message { get; set; }

        public MessageDTO(string result, string message)
        {
            Result = result;
            Message = message;
        }
    }
}