using Spectre.Console;
using System.Diagnostics;

namespace FireTest
{
    public static class Engine
    {
        public static async Task<List<TestResult>> SendParallelRequestsAsync(string url, int parallelRequests, Action callback)
        {
            var httpClient = new HttpClient();
            var tasks = new List<Task<TestResult>>();

            for (int i = 0; i < parallelRequests; i++)
            {
                tasks.Add(SendRequest(url, i + 1, httpClient, callback));
            }

            var results = await Task.WhenAll(tasks);
            return new List<TestResult>(results);
        }

        private static async Task<TestResult> SendRequest(string url, int iteration, HttpClient httpClient, Action callback)
        {
            var requestInfo = new TestResult
            {
                Iteration = iteration,
                StartTime = DateTime.Now
            };

            try
            {
                var stopwatch = Stopwatch.StartNew();
                var response = await httpClient.GetAsync(url);
                stopwatch.Stop();
                callback();

                requestInfo.EndTime = DateTime.Now;
                requestInfo.HTTPStatus = (int)response.StatusCode;
                requestInfo.ResponseTime = stopwatch.ElapsedMilliseconds;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in iteration {iteration}: {ex.Message}");
            }

            return requestInfo;
        }

    }
}
