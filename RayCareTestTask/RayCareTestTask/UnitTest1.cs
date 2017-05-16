using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Text;
using System.Configuration;

namespace RayCareTestTask
{
    public class UnitTest1
    {
        private const string BASE_URL = "http://localhost:8080";
        private const string PATIENT_IMG_URL = "https://www.shutterstock.com/image-photo/senior-patient-having-consultation-doctor-office-317554598?src=sIRXfoOz58oVBMY0JCMAZQ-1-4";
        private static UriBuilder UriBuilder = new UriBuilder(BASE_URL);

        [Fact]
        public async Task TestDoctor_HasUniqueId()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var numDoctors = doctors.Count;
                var numIds = doctors.Select(d => d.id).Distinct().Count();
                Assert.Equal(numDoctors, numIds);
            }
        }
        [Fact]
        public async Task TestDoctor_HasValidImage()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var images = await GetRequest<List<Image>>("images");
                var uris = doctors.Join(images, d => d.imageId, i => i.id, (d, i) => new Uri(i.url));
                using (var httpClient = new HttpClient())
                {
                    foreach (var uri in uris)
                    {
                        var response = await httpClient.GetAsync(uri);
                        Assert.True(response.IsSuccessStatusCode, string.Format("{0} is a valid URI", uri));
                    }
                }
            }
        }

        [Fact]
        public async Task TestImage_Add_SucceedsAndReturnsId()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var id = await AddImage();
                Assert.NotEmpty(id);
            }
        }

        [Fact]
        public async Task TestPatient_AddPatient_SucceedsAndReturnsId()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var patient = new Patient
                {
                    name = "Dag",
                    condition = Condition.flu,
                    imageId = await AddImage()
                };
                var response = await PostRequest("patients", patient);
                Assert.NotEmpty(response.id);
            }
        }

        [Fact]
        public async Task TestPatient_Add_AllPropertiesStored()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var imageId = await AddImage();
                var patient = new Patient
                {
                    name = "Dag",
                    condition = Condition.flu,
                    imageId = imageId
                };
                var response = await PostRequest("patients", patient);
                var storedPatient = await GetRequest<Patient>("patients", response.id);
                Assert.Equal(response.id, storedPatient.id);
                Assert.Equal("Dag", storedPatient.name);
                Assert.Equal(imageId, storedPatient.imageId);
                Assert.Equal(Condition.flu, storedPatient.condition);
            }
        }

        [Fact]
        public async Task TestConsulatation_AddBreastPatient_SchedulesWithOncologist()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var patient = await AddPatient("Dag", Condition.breastcancer);
                var consultations = await GetRequest<List<Consultation>>("consultations");
                var consultation = consultations.Where(c => c.patientId == patient.id).Single();
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var doctor = doctors.Where(d => d.id == consultation.doctorId).Single();
                Assert.True(doctor.roles.Any(r => r == Role.oncologist), "Breast cancer patient is scheduled with oncologist");
            }
        }

        [Fact]
        public async Task TestConsulatation_AddBreastPatient_GetsRoomWithTreatmentMachine()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var patient = await AddPatient("Dag", Condition.breastcancer);
                var consultations = await GetRequest<List<Consultation>>("consultations");
                var consultation = consultations.Where(c => c.patientId == patient.id).Single();
                var rooms = await GetRequest<List<Room>>("rooms");
                var room = rooms.Where(r => r.id == consultation.roomId).Single();
                Assert.NotNull(room.treatmentMachineId);
            }
        }

        [Fact]
        public async Task TestConsulatation_AddHeadAndNeckPatient_GetsRoomWithAdvancedTreatmentMachine()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var patient = await AddPatient("Dag", Condition.breastcancer);
                var consultations = await GetRequest<List<Consultation>>("consultations");
                var consultation = consultations.Where(c => c.patientId == patient.id).Single();
                var rooms = await GetRequest<List<Room>>("rooms");
                var room = rooms.Where(r => r.id == consultation.roomId).Single();
                Assert.NotNull(room.treatmentMachineId);
                var machines = await GetRequest<List<Machine>>("machines");
                var machine = machines.Where(m => m.id == room.treatmentMachineId).Single();
                Assert.True(machine.capability == Capability.advanced, "Head and neck patient gets advanced machine");
            }
        }

        [Fact]
        public async Task TestConsulatation_AddFluPatient_SchedulesWithGeneralPractitioner()
        {
            using (NodeRunner.Start(ConfigurationManager.AppSettings["jsFile"]))
            {
                var patient = await AddPatient("Dag", Condition.flu);
                var consultations = await GetRequest<List<Consultation>>("consultations");
                var consultation = consultations.Where(c => c.patientId == patient.id).Single();
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var doctor = doctors.Where(d => d.id == consultation.doctorId).Single();
                Assert.True(doctor.roles.Any(r => r == Role.generalpractitioner), "Flu patient is scheduled with general practitioner");
            }
        }

        //
        // Utility methods below
        //
        private static async Task<Patient> AddPatient(string name, Condition condition)
        {
            var patient = new Patient
            {
                name = name,
                condition = condition,
                imageId = await AddImage()
            };
            return await PostRequest("patients", patient);
        }
        private static async Task<string> AddImage()
        {
            var image = new Image
            {
                url = PATIENT_IMG_URL
            };
            var response = await PostRequest("images", image);
            return response.id;
        }
        private static async Task<T> GetRequest<T>(string restRequest, string id = null)
        {
            if(id != null)
            {
                restRequest += "/" + id;
            }
            UriBuilder.Path = restRequest;
            using (var httpClient = new HttpClient())
            {
                var response = await httpClient.GetAsync(UriBuilder.Uri);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
        private static async Task<T> PostRequest<T>(string restRequest, T data)
        {
            UriBuilder.Path = restRequest;
            UriBuilder.Query = null;
            using (var httpClient = new HttpClient())
            {
                var jsonString = JsonConvert.SerializeObject(data);
                var request = new StringContent(jsonString, Encoding.UTF8, "application/json");
                var response = await httpClient.PostAsync(UriBuilder.Uri, request);
                response.EnsureSuccessStatusCode();
                var content = await response.Content.ReadAsStringAsync();
                return JsonConvert.DeserializeObject<T>(content);
            }
        }
    }
}
