using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Core.Services
{
    public class ProcessManagerApiClient : ApiClientBase, IProcessManagerApiClient
    {
        public async Task<ApiClientResult<ResType, ResErrorType>> GetAsync<ResType, ResErrorType>(string url, Dictionary<string, string> headers)
        {
            var config = GetConfig(headers);
            return await GetAsync<ResType, ResErrorType>(url, config).ConfigureAwait(false);
        }

        public async Task<ApiClientResult<ResType, ResErrorType>> PostAsync<ReqType, ResType, ResErrorType>(string url, ReqType content, string contentType, Dictionary<string, string> headers)
        {
            var config = GetConfig(headers);
            return await PostAsync<ResType, ResErrorType>(url, content, config).ConfigureAwait(false);
        }

        public async Task<ApiClientResult<ResType, ResErrorType>> PutAsync<ReqType, ResType, ResErrorType>(string url, ReqType content, string contentType, Dictionary<string, string> headers)
        {
            var config = GetConfig(headers);
            return await PutAsync<ResType, ResErrorType>(url, content, config).ConfigureAwait(false);
        }

        public async Task<ApiClientResult<ResType, ResErrorType>> DeleteAsync<ResType, ResErrorType>(string url, Dictionary<string, string> headers)
        {
            var config = GetConfig(headers);
            return await DeleteAsync<ResType, ResErrorType>(url, config).ConfigureAwait(false);
        }

        public ApiClientConfiguration GetConfig(Dictionary<string, string> headers)
        {
            var config = new ApiClientConfiguration()
            {
                BaseUrl = ProcessManagementConstants.BaseUrl,
                AdditionalHeaders = headers
            };

            return config;
        }
    }
}
