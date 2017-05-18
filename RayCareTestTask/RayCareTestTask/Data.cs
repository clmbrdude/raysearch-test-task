using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace RayCareTestTask
{
    public enum Role
    {
        oncologist,
        generalpractitioner
    }
    public enum Capability
    {
        advanced,
        simple
    }
    public enum Condition
    {
        breastcancer,
        headandneckcancer,
        flu
    }
    public class Machine
    {
        public string name { get; set; }
        public Capability capability { get; set; }
        public string id { get; set; }
    }
    public class Doctor
    {
        public string name { get; set; }
        [JsonProperty(ItemConverterType = typeof(StringEnumConverter))]
        public List<Role> roles { get; set; }
        public string imageId { get; set; }
        public string id { get; set; }
    }
        public class Image
    {
        public string url { get; set; }
        public string id { get; set; }
    }
    public class Room
    {
        public string name { get; set; }
        public string treatmentMachineId { get; set; }
        public string id { get; set; }
    }
    public class Patient
    {
        public string id { get; set; }
        public string name { get; set; }
        [JsonConverter(typeof(StringEnumConverter))]
        public Condition condition { get; set; }
        public string imageId { get; set; }
    }
    public class Consultation
    {
        public string id { get; set; }
        public DateTime registrationDate { get; set; }
        public string patientId { get; set; }
        public string doctorId { get; set; }
        public string roomId { get; set; }
        public DateTime consultationDate { get; set; }
    }
}
