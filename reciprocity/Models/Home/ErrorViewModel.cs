using System;
using System.Net;

namespace reciprocity.Models.Home
{
    public class ErrorViewModel
    {
        public int StatusCode { get; set; }
        public string RequestId { get; set; }

        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
        public string StatusCodeDescription => ((HttpStatusCode)StatusCode).ToString();

    }
}