﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Xunit;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Schema;
using Newtonsoft.Json.Schema.Generation;
using Newtonsoft.Json.Linq;

namespace RayCareTestTask
{
    public class UnitTest1
    {
        private const string BASE_URL = "http://localhost:8080";
        private const string PATIENT_IMG_URL = "https://www.shutterstock.com/image-photo/senior-patient-having-consultation-doctor-office-317554598?src=sIRXfoOz58oVBMY0JCMAZQ-1-4";
        private static UriBuilder UriBuilder = new UriBuilder(BASE_URL);

        #region Doctor
        [Fact]
        public async Task TestDoctor_HasUniqueId()
        {
            using (NodeRunner.Start())
            {
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var numDoctors = doctors.Count;
                var numIds = doctors.Select(d => d.id).Distinct().Count();
                Assert.Equal(numDoctors, numIds);
            }
        }
        [Fact]
        public async Task TestDoctor_HasValidSchema()
        {
            using (NodeRunner.Start())
            {
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var errorString = await ValidateResponse<Doctor>("doctors", doctors.First().id);
                Assert.True(errorString == null, errorString);
            }
        }
        [Fact]
        public async Task TestDoctor_AddPatient_AllDoctorsHasValidSchema()
        {
            using (NodeRunner.Start())
            {
                await AddPatient("John Gill", Condition.headandneckcancer);
                var doctors = await GetRequest<List<Doctor>>("doctors");
                foreach (var doctor in doctors)
                {
                    var errorString = await ValidateResponse<Doctor>("doctors", doctor.id);
                    Assert.True(errorString == null, errorString);
                }
            }
        }
        [Fact]
        public async Task TestDoctor_HasValidImage()
        {
            using (NodeRunner.Start())
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
        #endregion
        #region Machine
        [Fact]
        public async Task TestMachine_HasValidSchema()
        {
            using (NodeRunner.Start())
            {
                var machines = await GetRequest<List<Machine>>("machines");
                foreach (var machine in machines)
                {
                    var errorString = await ValidateResponse<Machine>("machines", machine.id);
                    Assert.True(errorString == null, errorString);
                }
            }
        }
        #endregion
        #region Room
        [Fact]
        public async Task TestRoom_HasValidMachine()
        {
            using (NodeRunner.Start())
            {
                var rooms = await GetRequest<List<Room>>("rooms");
                var machinesInRooms = rooms.Where(r => r.treatmentMachineId != null).Select(r => r.treatmentMachineId);
                var machines = await GetRequest<List<Machine>>("machines");
                var machineIds = machines.Select(m => m.id);
                var notValidIds = machineIds.Except(machineIds);
                Assert.Equal(0, notValidIds.Count());
            }
        }
        #endregion
        #region Image
        [Fact]
        public async Task TestImage_Add_SucceedsAndReturnsId()
        {
            using (NodeRunner.Start())
            {
                var id = await AddImage();
                var image = await GetRequest<Image>("images", id);
                Assert.Equal(id, image.id);
                Assert.NotEmpty(id);
            }
        }
        #endregion
        #region Patient
        [Fact]
        public async Task TestPatient_AddPatient_SucceedsAndReturnsId()
        {
            using (NodeRunner.Start())
            {
                var patient = new Patient
                {
                    name = "Dag",
                    condition = Condition.flu,
                    imageId = await AddImage()
                };
                var response = await PostRequest("patients", patient);
                var readPatient = await GetRequest<Patient>("patients", response.id);
                Assert.Equal(response.id, readPatient.id);
                Assert.NotEmpty(response.id);
            }
        }

        [Fact]
        public async Task TestPatient_Add_AllPropertiesStored()
        {
            using (NodeRunner.Start())
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
        #endregion
        #region Consultation
        [Fact]
        public async Task TestConsultation_AddPatient_ConsultationHasValidSchema()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Lynn Hill", Condition.flu);
                var errorString = await ValidateResponse<Consultation>("consultations", consultation.id);
                Assert.True(errorString == null, errorString);
            }
        }
        [Fact]
        public async Task TestConsultation_AddPatient_ConsultationIsCreatedForNextDay()
        {
            using (NodeRunner.Start())
            {
                var patient = await AddPatient("John Sherman", Condition.headandneckcancer);
                var consultations = await GetConsultations();
                var consultation = consultations.Where(c => c.patientId == patient.id).Single();

                Assert.NotEmpty(consultation.id);
                Assert.Equal(DateTime.Now.Date, consultation.registrationDate.Date);
                Assert.Equal(patient.id, consultation.patientId);
                Assert.Equal(DateTime.Now.AddDays(1).Date, consultation.consultationDate.Date);
            }
        }
        [Fact]
        public async Task TestConsultation_AddTwoPatients_FirstConsultationStaysSame()
        {
            using (NodeRunner.Start())
            {
                var initialConsultation = await AddPatientGetConsultation("First patient", Condition.flu);
                await AddPatient("Second patient", Condition.flu);
                var allConsultations = await GetConsultations();
                Assert.Equal(initialConsultation, allConsultations.Where(c => c.id == initialConsultation.id).Single());
            }
        }
        [Fact]
        public async Task TestConsultation_AddBreastPatient_SchedulesWithOncologist()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Dag", Condition.breastcancer);
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var doctor = doctors.Where(d => d.id == consultation.doctorId).Single();
                Assert.True(doctor.roles.Any(r => r == Role.Oncologist), "Breast cancer patient is scheduled with oncologist");
            }
        }
        [Fact]
        public async Task TestConsultation_AddHeadAndNeckPatient_SchedulesWithOncologist()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Dag", Condition.headandneckcancer);
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var doctor = doctors.Where(d => d.id == consultation.doctorId).Single();
                Assert.True(doctor.roles.Any(r => r == Role.Oncologist), "Head and neck cancer patient is scheduled with oncologist");
            }
        }

        [Fact]
        public async Task TestConsultation_AddBreastPatient_GetsRoomWithTreatmentMachine()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Dag", Condition.breastcancer);
                var rooms = await GetRequest<List<Room>>("rooms");
                var room = rooms.Where(r => r.id == consultation.roomId).Single();
                Assert.NotNull(room.treatmentMachineId);
            }
        }

        [Fact]
        public async Task TestConsultation_AddHeadAndNeckPatient_GetsRoomWithAdvancedTreatmentMachine()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Dag", Condition.breastcancer);
                var rooms = await GetRequest<List<Room>>("rooms");
                var room = rooms.Where(r => r.id == consultation.roomId).Single();
                Assert.NotNull(room.treatmentMachineId);
                var machines = await GetRequest<List<Machine>>("machines");
                var machine = machines.Where(m => m.id == room.treatmentMachineId).Single();
                Assert.True(machine.capability == Capability.advanced, "Head and neck patient gets advanced machine");
            }
        }

        [Fact]
        public async Task TestConsultation_AddFluPatient_SchedulesWithGeneralPractitioner()
        {
            using (NodeRunner.Start())
            {
                var consultation = await AddPatientGetConsultation("Dag", Condition.flu);
                var doctors = await GetRequest<List<Doctor>>("doctors");
                var doctor = doctors.Where(d => d.id == consultation.doctorId).Single();
                Assert.True(doctor.roles.Any(r => r == Role.GeneralPractitioner), "Flu patient is scheduled with general practitioner");
            }
        }

        [Fact]
        public async Task TestConsultation_BookAllOncologists_NoDoubleBookingsAndCorrectDays()
        {
            using (NodeRunner.Start())
            {
                var config = await GetConfiguration();
                var numConsultations = config.Oncologists * 2;
                Assert.True(config.AdvancedMachines + config.SimpleMachines >= config.Oncologists);
                var expectDates = new List<DateTime>();
                foreach (var i in Enumerable.Range(0, numConsultations))
                {
                    await AddPatient("Patient " + i, Condition.breastcancer);
                    expectDates.Add(DateTime.Now.AddDays(1 + i / config.Oncologists).Date);
                }
                var consultations = await GetConsultations();
                var numUnique = consultations.Select(c => new { c.consultationDate.Date, c.doctorId }).Distinct().Count();
                Assert.Equal(numConsultations, numUnique);
                Assert.Equal(expectDates, consultations.Select(c => c.consultationDate.Date).OrderBy(d => d));
            }
        }

        [Fact]
        public async Task TestConsultation_BookRooms_NoDoubleBookingsAndCorrectDays()
        {
            using (NodeRunner.Start())
            {
                var config = await GetConfiguration();
                var consultationsPerDay = Math.Min(config.AdvancedMachines, config.Oncologists);
                var numConsultations = consultationsPerDay * 2;
                var expectDates = new List<DateTime>();
                foreach (var i in Enumerable.Range(0, numConsultations))
                {
                    await AddPatient("Patient " + i, Condition.headandneckcancer);
                    expectDates.Add(DateTime.Now.AddDays(1 + i / consultationsPerDay).Date);
                }
                var consultations = await GetConsultations();
                var numUnique = consultations.Select(c => new { c.consultationDate.Date, c.roomId }).Distinct().Count();
                Assert.Equal(numConsultations, numUnique);
                Assert.Equal(expectDates, consultations.Select(c => c.consultationDate.Date).OrderBy(d => d));
            }
        }
        [Fact]
        public async Task TestConsultation_AddTwoFluPatientsAndTwoHeadAndNeckPatients_DontCrash()
        {
            using (NodeRunner.Start())
            {
                var config = await GetConfiguration();
                foreach (var i in Enumerable.Range(1, config.GeneralPractitioners))
                {
                    await AddPatient("Flu patient " + i, Condition.flu);
                }
                foreach (var i in Enumerable.Range(1, config.AdvancedMachines))
                {
                    await AddPatient("Head and neck patient " + i, Condition.headandneckcancer);
                }
                var consultations = await GetConsultations();
            }
        }
        [Fact]
        public async Task TestConsultation_AddTwoFluPatientsAndTwoBreastPatients_DontCrash()
        {
            using (NodeRunner.Start())
            {
                var config = await GetConfiguration();
                foreach (var i in Enumerable.Range(1, config.GeneralPractitioners))
                {
                    await AddPatient("Flu patient " + i, Condition.flu);
                }
                foreach (var i in Enumerable.Range(1, config.AdvancedMachines))
                {
                    await AddPatient("Head and neck patient " + i, Condition.breastcancer);
                }
                Assert.True(true);
            }
        }
        #endregion
        #region Utilities
        //
        // Utility methods below
        //
        private static async Task<List<Consultation>> GetConsultations()
        {
            return await GetRequest<List<Consultation>>("consultations");
        }
        private static async Task<HospitalConfiguration>GetConfiguration()
        {
            var machines = await GetRequest<List<Machine>>("machines");
            var rooms = await GetRequest<List<Room>>("rooms");
            var machineCapabilities = rooms.Join(machines, r => r.treatmentMachineId, m => m.id, (r, m) => m.capability);
            var doctors = await GetRequest <List<Doctor>> ("doctors");
            return new HospitalConfiguration(
                machineCapabilities.Where(c => c == Capability.advanced).Count(),
                machineCapabilities.Where(c => c == Capability.simple).Count(),
                rooms.Count,
                doctors.Count,
                doctors.Where(d => d.roles.Any(r => r == Role.Oncologist)).Count(),
                doctors.Where(d => d.roles.Any(r => r == Role.GeneralPractitioner)).Count()
                );
        }
        private static async Task<Consultation> AddPatientGetConsultation(string name, Condition condition)
        {
            var patient = await AddPatient(name, condition);
            var consultations = await GetConsultations();
            var consultation = consultations.Where(c => c.patientId == patient.id).Single();
            return consultation;
        }
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
            if (id != null)
            {
                restRequest += "/" + id;
            }
            UriBuilder.Path = restRequest;
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(UriBuilder.Uri))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<T>(content);
                }
            }
        }
        private static async Task<string> ValidateResponse<T>(string restRequest, string id)
        {
            restRequest += "/" + id;
            UriBuilder.Path = restRequest;

            var schemaGenerator = new JSchemaGenerator();
            schemaGenerator.GenerationProviders.Add(new StringEnumGenerationProvider());
            var schema = schemaGenerator.Generate(typeof(T));
            
            using (var httpClient = new HttpClient())
            {
                using (var response = await httpClient.GetAsync(UriBuilder.Uri))
                {
                    response.EnsureSuccessStatusCode();
                    var content = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(content);
                    IList<ValidationError> errors;
                    if (!obj.IsValid(schema, out errors))
                    {
                        return errors.Select(e => string.Format("{0}, value is {1}",e.Message, e.Value)).Aggregate((buf, next) => buf + "\n" + next);
                    }
                    return null;
                }
            }
        }
        private static async Task<T> PostRequest<T>(string restRequest, T data)
        {
            UriBuilder.Path = restRequest;
            UriBuilder.Query = null;
            using (var httpClient = new HttpClient())
            {
                var jsonString = JsonConvert.SerializeObject(data);
                using (var request = new StringContent(jsonString, Encoding.UTF8, "application/json"))
                {
                    using (var response = await httpClient.PostAsync(UriBuilder.Uri, request))
                    {
                        response.EnsureSuccessStatusCode();
                        var content = await response.Content.ReadAsStringAsync();
                        return JsonConvert.DeserializeObject<T>(content);
                    }
                }
            }
        }
        #endregion

    }
}
