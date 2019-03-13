using ProcessManager.Core.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ProcessManager.Core.Services
{
    public interface IApiClientBase
    {
        Task<ApiClientResult<TResponse, TResponseError>> GetAsync<TResponse, TResponseError>(string path, ApiClientConfiguration customConfig = null);
        Task<ApiClientResult<TResponse, TResponseError>> PostAsync<TResponse, TResponseError>(string path, object content, ApiClientConfiguration customConfig = null);
        Task<ApiClientResult<TResponse, TResponseError>> PutAsync<TResponse, TResponseError>(string path, object content, ApiClientConfiguration customConfig = null);
        Task<ApiClientResult<TResponse, TResponseError>> DeleteAsync<TResponse, TResponseError>(string path, ApiClientConfiguration customConfig = null);
    }
}
