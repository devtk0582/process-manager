using Newtonsoft.Json;
using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Core.Services
{
    public abstract class ApiClientBase : IApiClientBase
    {
        private ApiClientConfiguration _config;

        public void Configure(ApiClientConfiguration config)
        {
            _config = config;
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> GetAsync<TResponse, TResponseError>(string path, ApiClientConfiguration customConfig = null)
        {
            return await GetResponse<TResponse, TResponseError>(path, HttpMethod.Get, customConfig);
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> PostAsync<TResponse, TResponseError>(string path, object content, ApiClientConfiguration customConfig = null)
        {
            return await GetResponse<TResponse, TResponseError>(path, HttpMethod.Post, customConfig, content);
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> PutAsync<TResponse, TResponseError>(string path, object content, ApiClientConfiguration customConfig = null)
        {
            return await GetResponse<TResponse, TResponseError>(path, HttpMethod.Put, customConfig, content);
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> DeleteAsync<TResponse, TResponseError>(string path, ApiClientConfiguration customConfig = null)
        {
            return await GetResponse<TResponse, TResponseError>(path, HttpMethod.Delete, customConfig);
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> GetResponse<TResponse, TResponseError>(string path, HttpMethod method, ApiClientConfiguration customConfig = null, object content = null)
        {
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            var baseUrl = customConfig?.BaseUrl != null ? customConfig.BaseUrl : _config?.BaseUrl;
            var request = CreateHttpRequestMessage($"{baseUrl}{path}", method, content, customConfig);
            var response = await ProcessRequestAsync(request);
            stopWatch.Stop();
            var result = await ProcessResponseAsync<TResponse, TResponseError>(response);
            result.ResponseTime = stopWatch.Elapsed;
            return result;
        }

        public virtual HttpRequestMessage CreateHttpRequestMessage(string url, HttpMethod method, object content = null, ApiClientConfiguration customConfig = null)
        {
            var httpRequest = new HttpRequestMessage(method, url);

            if (customConfig?.AdditionalHeaders != null)
            {
                foreach (var header in customConfig?.AdditionalHeaders)
                    httpRequest.Headers.Add(header.Key, header.Value);
            }

            if (!string.IsNullOrEmpty(customConfig?.AcceptHeader.Value))
                httpRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(customConfig.AcceptHeader.Value));

            if (content != null)
            {
                var strContent = new StringContent(JsonConvert.SerializeObject(content));
                strContent.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                httpRequest.Content = strContent;
            }
            return httpRequest;
        }

        public virtual async Task<ApiClientResult<TResponse, TResponseError>> ProcessResponseAsync<TResponse, TResponseError>(HttpResponseMessage response)
        {
            var apiResult = new ApiClientResult<TResponse, TResponseError> { HttpStatusCode = response.StatusCode };

            if (response.Content == null)
            {
                return apiResult;
            }

            if (response.IsSuccessStatusCode)
            {
                apiResult.Result = typeof(TResponse) == typeof(string) ?
                                (TResponse)Convert.ChangeType((await response.Content.ReadAsStringAsync()), typeof(TResponse))
                                : (await response.Content.ReadAsAsync<TResponse>());
            }
            else
            {
                apiResult.ErrorResult = typeof(TResponseError) == typeof(string) ?
                                (TResponseError)Convert.ChangeType((await response.Content.ReadAsStringAsync()), typeof(TResponseError))
                                : (await response.Content.ReadAsAsync<TResponseError>());
            }

            return apiResult;
        }


        public virtual async Task<HttpResponseMessage> ProcessRequestAsync(HttpRequestMessage request)
        {
            using (var client = GetHttpClient())
            {
                return await client.SendAsync(request);
            }
        }

        public virtual HttpClient GetHttpClient()
        {
            var httpClient = new HttpClient();

            if (_config == null)
            {
                return httpClient;
            }

            if (_config.AcceptHeader.Key != null && _config.AcceptHeader.Value != null)
            {
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(_config.AcceptHeader.Value));
            }

            if (_config.IdentityHeader.Key != null && _config.IdentityHeader.Value != null)
                httpClient.DefaultRequestHeaders.Add(_config.IdentityHeader.Key, _config.IdentityHeader.Value);

            if (_config.TimeOutMinutes > 0)
                httpClient.Timeout = TimeSpan.FromMinutes(_config.TimeOutMinutes);

            if (_config.AdditionalHeaders != null)
            {
                foreach (var header in _config.AdditionalHeaders)
                {
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }
            }
            return httpClient;
        }
    }
}
