using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.ComponentModel;
using System.Threading.Tasks.Dataflow;
using Newtonsoft.Json.Linq;
using System.Net.Http.Json;
using System.Reflection;
using Newtonsoft.Json;

namespace InstagramApp
{
    public class FacebookApp
    {
        private string _appClientToken;
        private string _apiKey;
        private string _apiKeySecret;
        BackgroundWorker _backgroundWorker;
        private System.Timers.Timer _timer;
        private ResponseDTO dto;
        ResponseDTO tokenDto;
        private bool isFirstTime = true;
        private string caption;

        public List<string> files { get; set; }
        // private HttpClient _httpClient;
        public event EventHandler<ResponseDTO> operationAchieved;
        public FacebookApp()
        {
            _apiKey = ConfigurationManager.AppSettings["AppId"];
            _apiKeySecret = ConfigurationManager.AppSettings["AppSecret"];
            _appClientToken = ConfigurationManager.AppSettings["AppClientToken"];
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;
            tokenDto = new ResponseDTO();
            dto = new ResponseDTO();

        }

        public void Reprocess()
        {

            Console.WriteLine("Press Ctrl+C to exit OR Ctrl+R to re-start.");
            var info = Console.ReadKey();
            ReadSpecificKey(info);

        }

        public ResponseDTO ValidateToken(ResponseDTO dto)
        {
            bool isValidToken = false;

            string appId = _apiKey;
            string secretId = _apiKeySecret;
            //Get app access token


            var parameters = new Dictionary<string, string> { { "input_token", dto.access_token }, { "access_token", $"{appId}|{secretId}" } };

            var encodedContent = new FormUrlEncodedContent(parameters);
            var queryPrm = encodedContent.ReadAsStringAsync().Result;
            using (HttpClient _httpClient = new HttpClient())
            {
                _httpClient.DefaultRequestHeaders
                .Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                _httpClient.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");

                HttpResponseMessage msg = _httpClient.GetAsync($"debug_token" + "?" + queryPrm).Result;
                GraphTokenResponse ts = JsonConvert.DeserializeObject<GraphTokenResponse>(msg.Content.ReadAsStringAsync().Result);
                if (ts.error != null)
                    return new ResponseDTO { isTokenValid = false };
                else if (ts.data != null && ts.data.is_valid == false)
                {
                    dto.isTokenValid = false;
                    dto.isToken_retrived = false;
                    return dto;
                }
                else
                {
                    dto.isTokenValid = true;
                    return dto;
                }

                //(msg.Content.ReadAsStringAsync().Result);
            }

        }

        public void Start()
        {
            //if (dto != null)
            //{
            //    dto = null;
            //    tokenDto = null;
            //}

            //dto = new ResponseDTO();
            //tokenDto = new ResponseDTO();
            Console.WriteLine("Please input image urls.You can input max of 5 or minimum of 2 urls");
            int numberOfUrls = 0;
            List<string> urls = new List<string>();
            do
            {
                string url = Console.ReadLine();
                if (!string.IsNullOrEmpty(url))
                {
                    urls.Add(url);
                }
                numberOfUrls = numberOfUrls + 1;
            } while (numberOfUrls < 5);

            if (urls.Count > 1)
            {
                IEnumerable<string> files = urls;
                this.files = files.ToList();

                Console.WriteLine("Enter description for the corousal.");
                caption = Console.ReadLine();

                Console.WriteLine("Connecting Instagram for security code");
                if (dto.isToken_retrived == false)
                {
                    GetCode();
                    Console.WriteLine("Go to the link {0}, enter the security code {1}", dto.verification_uri, dto.user_code);
                    Console.WriteLine("Waiting for authorization");
                }

                StartAuthorizationCheck(dto);
                Console.ReadLine();

                Reprocess();

            }
            else
            {
                Console.WriteLine("For corousal post,a minimum of 2 image urls are required.");
                //Console.WriteLine("");
                //Console.ReadLine();
                Reprocess();
            }

        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            this.StartAuthorizationCheck(dto);
        }

        private void _backgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
        {
            try
            {

                if ((e.Result as ResponseDTO).responseCode == ErrorCodes.ServerError)
                {
                    _timer.Start();
                    this.isFirstTime = false;
                }
                else if ((e.Result as ResponseDTO).responseCode == ErrorCodes.Success)
                {
                    _timer.Stop();
                    _timer.Dispose();
                    _backgroundWorker.Dispose();
                    Console.WriteLine((e.Result as ResponseDTO).responseCode.ToString());

                    dto = (e.Result as ResponseDTO);


                    Console.WriteLine("Connecting to get token.");
                    if (dto.isToken_retrived == false)
                    {
                        tokenDto = this.GetToken(dto);
                        dto.isToken_retrived = true;
                        dto.access_token = tokenDto.data.First().access_token;
                    }

                    if (tokenDto != null && tokenDto.responseCode == ErrorCodes.ServerError)
                    {
                        Console.WriteLine("Unable to get token. Pls try after some time");
                    }
                    else
                    {
                        Console.WriteLine("Token retrieval successful.");


                        Console.WriteLine("Preparing for Instagram corousal post.1-Generating containers");
                        var containerIds = this.PostToInstagram(tokenDto);
                        if (containerIds.Count > 1)
                        {

                            Console.WriteLine("Preparing for Instagram corousal post. 2-Combining containers.");

                            Container cn = CreateSingleContainer(caption, string.Join(",", containerIds), tokenDto);
                            Console.WriteLine("Preparing for Instagram corousal post. 3-Posting to Instagram.");
                            if (cn.id.Length > 0)
                            {
                                Container res = PublishContainer(cn.id, tokenDto);
                                if (res.id.Length > 0)
                                {
                                    Console.WriteLine("carousel post is successfull.");
                                    // Console.WriteLine("Press Enter..");
                                }
                                else
                                {
                                    Console.WriteLine("carousel post is un-successfull.Try after some time");
                                    //Console.WriteLine("Press Enter..");
                                }
                            }
                            else
                            {
                                Console.WriteLine("carousel post is un-successfull.Try after some time");
                                //Console.WriteLine("Press Enter..");
                            }
                            // Reprocess();
                            operationAchieved?.Invoke(this, dto);
                        }
                        else
                        {
                            Console.WriteLine("carousel post is un-successfull.Try after some time");
                            //Reprocess();
                            //Console.ReadLine();
                            operationAchieved?.Invoke(this, dto);
                        }

                    }

                }
                else if ((e.Result as ResponseDTO).responseCode == ErrorCodes.InvalidToken)
                {
                    Console.WriteLine("Token has expired.Re-starting the token generation process.");
                    Console.Clear();

                    Start();
                    //StartAuthorizationCheck(dto);
                    Console.ReadLine();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured." + ex.Message);
                //Reprocess();
                operationAchieved?.Invoke(this, dto);
            }
        }

        private void _backgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            dto = (e.Argument as ResponseDTO);
            if (dto.isToken_retrived == false)
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/device/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "access_token", _apiKey + "|" + _appClientToken }, { "code", dto.code } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"login_status", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        if (msg.Content.ReadAsStringAsync().Result.Contains("error"))
                        {
                            e.Result = new ResponseDTO { responseCode = ErrorCodes.ServerError };

                        }
                        else
                        {
                            dto = System.Text.Json.JsonSerializer.Deserialize<ResponseDTO>(msg.Content.ReadAsStringAsync().Result);
                            dto.responseCode = ErrorCodes.Success;
                            e.Result = dto;
                        }
                    }
                    else
                    {
                        e.Result = new ResponseDTO { responseCode = ErrorCodes.ServerError };
                    }
                }
            }
            else
            {

                dto = ValidateToken(dto);
                if (dto.isTokenValid)
                    dto.responseCode = ErrorCodes.Success;
                else
                    dto.responseCode = ErrorCodes.InvalidToken;
                e.Result = dto;
            }
        }

        public void GetCode()
        {
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/device/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "access_token", _apiKey + "|" + _appClientToken }, { "scope", "business_management,instagram_content_publish,pages_show_list,instagram_basic" } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"login", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        dto = System.Text.Json.JsonSerializer.Deserialize<ResponseDTO>(msg.Content.ReadAsStringAsync().Result);
                        //dto;
                    }
                    else
                    {
                        dto = new ResponseDTO { code = "" };
                    }
                }
                //return dto;
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured. " + ex.Message);
                Reprocess();
            }
        }

        public void StartAuthorizationCheck(ResponseDTO dto)
        {
            if (_timer == null)
            {
                _timer = new(interval: dto.interval * 1000);
                _timer.Elapsed += _timer_Elapsed;
            }
            if (!_backgroundWorker.IsBusy)
                _backgroundWorker.RunWorkerAsync(dto);
        }

        public ResponseDTO GetToken(ResponseDTO dto)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var parametersGet = new Dictionary<string, string> { { "access_token", dto.access_token }, { "fields", "access_token,instagram_business_account,id" } };
                var encodeGet = new FormUrlEncodedContent(parametersGet);
                var queryGet = encodeGet.ReadAsStringAsync().Result;

                HttpResponseMessage msg = client.GetAsync($"me/accounts?" + queryGet).Result;
                if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseDTO res = System.Text.Json.JsonSerializer.Deserialize<ResponseDTO>(msg.Content.ReadAsStringAsync().Result);

                    return res;
                }
                else
                {
                    return new ResponseDTO { data = null, responseCode = ErrorCodes.ServerError };
                }
            }
        }

        public List<string> PostToInstagram(ResponseDTO dto)
        {
            List<string> taskList = new List<string>();
            var endpoint = dto.data.First().instagram_business_account.id + "/media";
            var socketsHttpHandler = new SocketsHttpHandler()
            {
                MaxConnectionsPerServer = 5
            };
            var client = new HttpClient(socketsHttpHandler);

            client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            foreach (var req in this.files)
            {

                var parameters = new Dictionary<string, string> { { "is_carousel_item", "true" }, { "access_token", dto.data.First().access_token }, { "image_url", req } };
                var encodedContent = new FormUrlEncodedContent(parameters);

                HttpResponseMessage msg = client.PostAsync($"{endpoint}", encodedContent).Result;
                if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                    taskList.Add(res.id);
                }
                else
                {
                    Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                    Console.WriteLine(res.error.message + $"  {req}");
                }
            }
            return taskList;
        }

        public Container CreateSingleContainer(string caption, string containerIds, ResponseDTO dto)
        {
            var endpoint = dto.data.First().instagram_business_account.id + "/media";
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "media_type", "CAROUSEL" }, { "children", containerIds }, { "caption", caption }, { "access_token", dto.data.First().access_token } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"{endpoint}", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        return res;
                    }
                    else
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        Console.WriteLine(res.error.message);
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }

        public Container PublishContainer(string containerId, ResponseDTO dto)
        {
            var endpoint = dto.data.First().instagram_business_account.id + "/media_publish";
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "creation_id", containerId }, { "access_token", dto.data.First().access_token } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"{endpoint}", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        return res;
                    }
                    else
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        return res;
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }


        private void ReadSpecificKey(ConsoleKeyInfo info)
        {
            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.R)
            {

                Console.Clear();
                Start();
                //Console.ReadLine();
            }

            if (info.Modifiers.HasFlag(ConsoleModifiers.Control) && info.Key == ConsoleKey.C)
            {
                Environment.Exit(0);
            }
        }
    }
}
