using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayCareTestTask
{
    public class Machine
    {
        public string name { get; set; }
        public string capability { get; set; }
        public string id { get; set; }
    }
    public class Doctor
    {
        public string name { get; set; }
        public List<string> roles { get; set; }
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

}
