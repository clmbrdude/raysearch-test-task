using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Text;

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

        [Fact]
        public async Task TestMachine_GetList()
        {
            var machines = await GetRequest<List<Machine>>("machines");
            var cap = machines.Select(m => m.capability);
            //Assert.True(cap.All(c => c == Capability.Advanced || c == Capability.Simple));
            Assert.True(machines.Where(m => m.name == "MM50").Single().capability == Capability.simple);
        }

        [Fact]
        public async Task TestPatient_AddPatient_Succeeds()
        {
            var patient = new Patient
            {
                name = "Dag",
                condition = Condition.flu,
                imageId = "blaha"
            };
            var response = await PostRequest("patients", patient);
            Assert.NotEmpty(response.id);
        }

        //
        // Utility methods below
        //

        private static async Task<T> GetRequest<T>(string restRequest, string query = null)
        {
            _uriBuilder.Path = restRequest;
            _uriBuilder.Query = query;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(_uriBuilder.Uri);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
        private static async Task<T> PostRequest<T>(string restRequest, T data)
        {
            _uriBuilder.Path = restRequest;
            _uriBuilder.Query = null;
            using (var httpClient = new HttpClient())
            {
                var jsonString = JsonConvert.SerializeObject(data);
                var request = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(_uriBuilder.Uri, request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
    }
}
