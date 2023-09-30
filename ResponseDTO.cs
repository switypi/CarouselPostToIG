using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace InstagramApp
{
    public class ResponseDTO
    {
        public string code {  get; set; }
        public string user_code { get; set; }
        public string verification_uri { get; set; }
        public  int interval { get; set; }
        public string access_token { get; set; }

        public List<string> files { get; set; }

        public List<DataExt> data { get; set; }
        public InstagramBusinessAccount instagram_business_account { get; set; }

        public bool isToken_retrived { get; set; }

        public ErrorCodes responseCode { get; set; }

        public bool isTokenValid { get; set; }
    }

    public class Container
    {
        public string id { get; set; }
        public Error error { get; set; }
       
    }

    public class Error
    {
        public string message { get; set; }

    }

    public class DataExt
    {
        public string access_token { get; set; }
        public InstagramBusinessAccount instagram_business_account { get; set; }
        public string id { get; set; }//page id
    }
    public class InstagramBusinessAccount
    {
        public string id { get; set; }
    }

    public enum ErrorCodes
    {
        InvalidToken,
        ServerError,
        Success,


    }

    public class TokenDataItem
    {
        /// <summary>
        /// Check for Fb/IG
        /// </summary>
        public bool is_valid { get; set; }
        public ErrorInfo error { get; set; }

    }
    public class ErrorInfo
    {
        public string message { get; set; }
        public string type { get; set; }
    }

    public class GraphTokenResponse
    {
        public ErrorInfo error { get; set; }
        public string error_description { get; set; }
        public TokenDataItem data { get; set; }
    }
}
