using Marketstack.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Throttling;

namespace Marketstack.Services
{
    internal static class HttpClientExtensions
    {

        public static List<T> GetResult<T>(this HttpClient httpClient, string url, string apiToken)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_key"] = apiToken;
            builder.Query = query.ToString();
            var pageResponse = httpClient.GetNextPageResponseSync<T>(builder, null);
            List<T> results = new List<T>();
            while (pageResponse != null)
            {
                foreach (var item in pageResponse.Data)
                {
                    results.Add(item);
                }

                pageResponse = httpClient.GetNextPageResponseSync(builder, pageResponse);
            }

            return results;
        }

        public static async IAsyncEnumerable<T> GetAsync<T>(this HttpClient httpClient, string url, string apiToken, Throttled throttled)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_key"] = apiToken;
            builder.Query = query.ToString();
            var pageResponse = await httpClient.GetNextPageResponse<T>(builder, null);

            while (pageResponse != null)
            {
                foreach (var item in pageResponse.Data)
                {
                    yield return item;
                }

                pageResponse = await throttled.Run(() => httpClient.GetNextPageResponse(builder, pageResponse));
            }
        }

        public static  T GetSingle<T>(this HttpClient httpClient, string url, string apiToken, Throttled throttled)
        {
            var builder = new UriBuilder(url);
            var query = HttpUtility.ParseQueryString(builder.Query);
            query["access_key"] = apiToken;
            builder.Query = query.ToString();
            T result =  httpClient.GetOneResult<T>(builder);
            return result;
        }

        private static T GetOneResult<T>(this HttpClient httpClient, UriBuilder builder)
        {
            using Stream s = httpClient.GetStreamAsync(builder.Uri).Result;
            using StreamReader sr = new StreamReader(s);
            using JsonReader reader = new JsonTextReader(sr);
            JsonSerializer serializer = new JsonSerializer();
            return serializer.Deserialize<T>(reader);
        }

        private static PageResponse<T> GetNextPageResponseSync<T>(this HttpClient httpClient, UriBuilder builder, PageResponse<T> lastPageResponse)
        {
            if (lastPageResponse != null)
            {
                if (lastPageResponse.IsLastResponse)
                {
                    return null;
                }

                var query = HttpUtility.ParseQueryString(builder.Query);
                query["offset"] = lastPageResponse.NextOffset.ToString();
                builder.Query = query.ToString();
            }


            using Stream s = httpClient.GetStreamAsync(builder.Uri).Result;
            using StreamReader sr = new StreamReader(s);
            using JsonReader reader = new JsonTextReader(sr);
            JsonSerializer serializer = new JsonSerializer();
            return serializer.Deserialize<PageResponse<T>>(reader);

        }

        private static async Task<PageResponse<T>> GetNextPageResponse<T>(this HttpClient httpClient, UriBuilder builder, PageResponse<T> lastPageResponse)
        {
            if (lastPageResponse != null)
            {
                if (lastPageResponse.IsLastResponse)
                {
                    return null;
                }

                var query = HttpUtility.ParseQueryString(builder.Query);
                query["offset"] = lastPageResponse.NextOffset.ToString();
                builder.Query = query.ToString();
            }


            using Stream s = await httpClient.GetStreamAsync(builder.Uri);
            using StreamReader sr = new StreamReader(s);
            using JsonReader reader = new JsonTextReader(sr);
            JsonSerializer serializer = new JsonSerializer();
            return serializer.Deserialize<PageResponse<T>>(reader);

        }
    }
}
