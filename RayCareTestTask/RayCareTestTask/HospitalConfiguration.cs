using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RayCareTestTask
{
    class HospitalConfiguration
    {
        public HospitalConfiguration(int advancedMachines, int simpleMachines, int rooms, int doctors, int oncologists, int generalPractitioners)
        {
            AdvancedMachines = advancedMachines;
            SimpleMachines = simpleMachines;
            Rooms = rooms;
            Doctors = Doctors;
            Oncologists = oncologists;
            GeneralPractitioners = generalPractitioners;

        }
        public int AdvancedMachines { get; private set; }
        public int SimpleMachines { get; private set; }
        public int Rooms { get; private set; }
        public int Doctors { get; private set; }
        public int Oncologists { get; private set; }
        public int GeneralPractitioners { get; private set; }
    }
}
