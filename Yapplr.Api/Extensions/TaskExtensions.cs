using System;
using System.Threading.Tasks;
using Yapplr.Api.Services;

namespace Yapplr.Api.Extensions
{
    public static class TaskExtensions
    {
        public static async Task<T> WithSmartRetry<T>(
            this Task<T> task, 
            ISmartRetryService retryService, 
            string operationName)
        {
            return await retryService.ExecuteWithRetryAsync(
                async () => await task,
                operationName);
        }
        
        public static async Task WithSmartRetry(
            this Task task, 
            ISmartRetryService retryService, 
            string operationName)
        {
            await retryService.ExecuteWithRetryAsync(
                async () => {
                    await task;
                    return true;
                },
                operationName);
        }
    }
}