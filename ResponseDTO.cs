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

        public ErrorCodes responseCode { get; set; }
    }

    public class Container
    {
        public string Id { get; set; }
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
}
