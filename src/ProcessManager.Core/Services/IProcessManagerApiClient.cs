using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Core.Services
{
    public interface IProcessManagerApiClient
    {
        Task<ApiClientResult<ResType, ResErrorType>> GetAsync<ResType, ResErrorType>(string url, Dictionary<string, string> headers);
        Task<ApiClientResult<ResType, ResErrorType>> PostAsync<ReqType, ResType, ResErrorType>(string url, ReqType content, string contentType, Dictionary<string, string> headers);
        Task<ApiClientResult<ResType, ResErrorType>> PutAsync<ReqType, ResType, ResErrorType>(string url, ReqType content, string contentType, Dictionary<string, string> headers);
        Task<ApiClientResult<ResType, ResErrorType>> DeleteAsync<ResType, ResErrorType>(string url, Dictionary<string, string> headers);
    }
}
