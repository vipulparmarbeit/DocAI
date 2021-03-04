using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.AppModels
{
   public class LoginApiResponse
    {
        public string result { get; set; }
        public string result_type { get; set; }
        public string desc { get; set; }
        public string user_id { get; set; }
        public string username { get; set; }
        public string password { get; set; }
        public string group_id { get; set; }
        public string user_type { get; set; }
        public string redirect_url { get; set; }
        public string text { get; set; }
    }

    public class SpeechToTextApiResponse
    {
        public string user_id { get; set; }
        public string text { get; set; }
        public string type { get; set; }
        public string speech { get; set; }
        public string question_id { get; set; }
    }

    public class ResponseCollectorApiResponse
    {
        public string user_id { get; set; }
        public string patient_id { get; set; }
        public string text_data { get; set; }
        public string bot_text_id { get; set; }
        public string result { get; set; }
    }

    public class MultiQuestionApiResponse
    {
        public string user_id { get; set; }
        public string question_id { get; set; }
        public string type { get; set; }
        public string id { get; set; }
        public string result { get; set; }
        public string text { get; set; }
    }

}
