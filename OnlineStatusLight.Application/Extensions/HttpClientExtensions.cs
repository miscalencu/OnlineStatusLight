using OnlineStatusLight.Application.Helpers;
using System.Net.Http.Headers;
using System.Net.Mime;

namespace OnlineStatusLight.Application.Extensions
{
    public class Response<T>
    {
        public T Data { get; set; }
        public HttpResponseMessage HttpResponse { get; set; }

        public async Task<string> GetHttpContent()
        {
            return await HttpResponse.Content.ReadAsStringAsync();
        }
    }

    public static class HttpClientExtensions
    {
        public static async Task<TOutput> ConvertResponse<TOutput>(this HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            string responseContent = await response.Content.ReadAsStringAsync();
            return typeof(TOutput) == typeof(string) ?
                (TOutput)(object)responseContent :
                SerializationHelper.FromJson<TOutput>(responseContent);
        }

        public static async Task<Response<TOutput>> PostJsonAsync<TOutput>(this HttpClient httpClient, string endpoint, object data)
        {
            var content = new StringContent(SerializationHelper.ToJson(data));
            content.Headers.ContentType = new MediaTypeHeaderValue(MediaTypeNames.Application.Json);
            var response = await httpClient.PostAsync(endpoint, content);

            return new Response<TOutput>
            {
                Data = await ConvertResponse<TOutput>(response),
                HttpResponse = response
            };
        }
    }
}
