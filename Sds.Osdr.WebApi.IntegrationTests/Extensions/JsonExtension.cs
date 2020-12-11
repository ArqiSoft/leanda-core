using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sds.Osdr.WebApi.IntegrationTests.EndPoints;

namespace Sds.Osdr.WebApi.IntegrationTests.Extensions
{
    public static class JsonExtension
    {
        //should be romeved soon
        //public static async Task<string> ReadJsonAsync(this OsdrWebClient client, string url)
        //{
        //    var response = await client.GetData(url);
        //    response.EnsureSuccessStatusCode();
        //    return await response.Content.ReadAsStringAsync();
        //}

        //public static async Task<T> ReadObjectAsync<T>(this OsdrWebClient client, string url) where T : class
        //{
        //    var json = await client.ReadJsonAsync(url);
        //    return JsonConvert.DeserializeObject<T>(json);
        //}
        //

        public static async Task<JObject> ReadAsJObjectAsync(this HttpContent content)
        {
            return JObject.Parse(await content.ReadAsStringAsync());
        }

        public static async Task<JArray> ReadAsJArrayAsync(this HttpContent content)
        {
            return JArray.Parse(await content.ReadAsStringAsync());
        }

        public static int ContainsNodes(this JToken nodes, IList<Guid> internalIds)
        {
            var countValid = nodes.Count(node => internalIds.Contains(node["id"].ToObject<Guid>()));
            return countValid;
        }
    }
}