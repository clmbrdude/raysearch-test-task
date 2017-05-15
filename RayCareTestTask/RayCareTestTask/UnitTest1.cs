using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;

namespace RayCareTestTask
{
    public class UnitTest1
    {
        private const string _BASE_URL = "http://localhost:8080";
        private static UriBuilder _uriBuilder = new UriBuilder(_BASE_URL);

        [Fact]
        public async Task TestDoctor_GetList()
        {
            var doctors = await GetRequest<List<Doctor>>("doctors");
            var i = doctors.Count;
            Assert.Equal(3, i);
        }

        private static async Task<T> GetRequest<T>(string restRequest, string query = null)
        {
            _uriBuilder.Path = restRequest;
            _uriBuilder.Query = query;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(_uriBuilder.Uri);

                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
    }
}
