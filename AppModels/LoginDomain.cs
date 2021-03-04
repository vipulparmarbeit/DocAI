using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.AppModels
{
    public class LoginDomain
    {
        public string version_id { get; set; }
        public string version_name { get; set; }
        public string country { get; set; }
        public string url { get; set; }
    }

    public class LoginMode
    {
        public string Name { get; set; }
        public LoginModeEnum EnumName { get; set; }
    }
    public enum LoginModeEnum
    {
        Doctor,
        Nurse,
        Patient,
    }
}
