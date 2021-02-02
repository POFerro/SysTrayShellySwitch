using System.Net;

namespace POF.Shelly
{
    public class HttpErrorDetails
    {
        public HttpStatusCode StatusCode { get; set; }
        public string ReasonPhrase { get; set; }
        public string ErrorMessage { get; set; }
    }
}