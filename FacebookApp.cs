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
        private bool isFirstTime = true;

        public List<string> files { get; set; }
        // private HttpClient _httpClient;
        public FacebookApp()
        {
            _apiKey = ConfigurationManager.AppSettings["AppId"];
            _apiKeySecret = ConfigurationManager.AppSettings["AppSecret"];
            _appClientToken = ConfigurationManager.AppSettings["AppClientToken"];
            _backgroundWorker = new BackgroundWorker();
            _backgroundWorker.DoWork += _backgroundWorker_DoWork;
            _backgroundWorker.RunWorkerCompleted += _backgroundWorker_RunWorkerCompleted;


        }

        private void _timer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            this.StartAuthorizationCheck(dto);
        }

        private void _backgroundWorker_RunWorkerCompleted(object? sender, RunWorkerCompletedEventArgs e)
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
                Console.WriteLine((e.Result as ResponseDTO).responseCode.ToString());

                dto = (e.Result as ResponseDTO);

                Console.WriteLine("Connecting to get token.");
                ResponseDTO tokenDto = this.GetToken(dto);
                if (tokenDto != null && tokenDto.responseCode == ErrorCodes.ServerError)
                {
                    Console.WriteLine("Unable to get token. Pls try after some time");
                }
                else
                {
                    this.PostToInstagram(tokenDto);
                }
            }
        }

        private void _backgroundWorker_DoWork(object? sender, DoWorkEventArgs e)
        {
            dto = (e.Argument as ResponseDTO);
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
                        //if (isFirstTime)
                        //{
                        //    _timer.Start();
                        //    this.isFirstTime = false;
                        //}
                    }
                    else
                    {
                        ResponseDTO dto = System.Text.Json.JsonSerializer.Deserialize<ResponseDTO>(msg.Content.ReadAsStringAsync().Result);
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

        public ResponseDTO GetCode()
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/device/");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var parameters = new Dictionary<string, string> { { "access_token", _apiKey+"|"+_appClientToken }, { "scope", "business_management,instagram_content_publish,pages_show_list" }};
                var encodedContent = new FormUrlEncodedContent(parameters);
                HttpResponseMessage msg = client.PostAsync($"login", encodedContent).Result;
                if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                {
                    ResponseDTO res = System.Text.Json.JsonSerializer.Deserialize<ResponseDTO>(msg.Content.ReadAsStringAsync().Result);
                    return res;
                }
                else
                {
                    return new ResponseDTO { code = "" };
                }
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

        public async Task PostToInstagram(ResponseDTO dto)
        {
            List<Task<JObject>> taskList = new List<Task<JObject>>();
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

                HttpResponseMessage msg = await client.PostAsync($"{endpoint}", encodedContent);
                taskList.Add(msg.Content.ReadFromJsonAsync<JObject>());
            }
            Task t = Task.WhenAll(taskList.ToArray());

        }

        public Container CreateSingleContainer(string containerIds, ResponseDTO dto)
        {
            var endpoint = dto.instagram_business_account.id + "/media";
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "media_type ", "CAROUSEL" }, { "children ", containerIds } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"{endpoint}", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        return res;
                    }
                    else
                    {
                        return new Container { Id = "" };
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
            var endpoint = dto.instagram_business_account.id + "/media_publish";
            try
            {
                using (var client = new HttpClient())
                {
                    client.BaseAddress = new Uri("https://graph.facebook.com/v17.0/");
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    var parameters = new Dictionary<string, string> { { "creation_id", containerId } };
                    var encodedContent = new FormUrlEncodedContent(parameters);
                    HttpResponseMessage msg = client.PostAsync($"{endpoint}", encodedContent).Result;
                    if (msg.StatusCode == System.Net.HttpStatusCode.OK)
                    {
                        Container res = System.Text.Json.JsonSerializer.Deserialize<Container>(msg.Content.ReadAsStringAsync().Result);
                        return res;
                    }
                    else
                    {
                        return new Container { Id = "" };
                    }
                }
            }
            catch (Exception ex)
            {

                throw;
            }
        }
    }
}
