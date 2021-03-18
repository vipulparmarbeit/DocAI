using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.AppModels
{
    public class SymptomStep
    {
        public ActionType ActionType { get; set; }
        public string ActionDescription { get; set; }

        public ActionStatus Status { get; set; }

        /// <summary>
        /// Json Request Data to REST API
        /// </summary>
        public string RequestData { get; set; }

        /// <summary>
        /// Json Response from REST API
        /// </summary>
        public string ResponseData { get; set; }

        public string UserInput { get; set; }
    }

    public class Symptom
    {
        public Symptom()
        {
            Steps = new List<SymptomStep>();
        }
        public string SymptomText { get; set; }
        public List<SymptomStep> Steps { get; set; }

        public bool IsSubmitted { get; set; }

        public bool IsMultiQuestion { get; set; }

        public static string GetSpeechToType_Text(SpeechToTextApiType speechToTextApiType)
        {
            string type = "type_1";
            if (speechToTextApiType == SpeechToTextApiType.Type1) type = "type_1";
            else if (speechToTextApiType == SpeechToTextApiType.Type2) type = "type_2";
            else if (speechToTextApiType == SpeechToTextApiType.Type3) type = "type_3";
            else if (speechToTextApiType == SpeechToTextApiType.Type4) type = "type_4";
            else if (speechToTextApiType == SpeechToTextApiType.Type6) type = "type_6";
            return type;
        }
    }

    public enum ActionType
    {
        SpeechToTextApiCall_SymtomEntry, //Type 1
        SpeechToTextApiCall_QuestionReply, //Type 2 -- Yes / No
        SpeechToTextApiCall_Submit, //Type 4
        ResponseCollectorApiCall, // multi question or single question for symptoms
        MultiquestionApiCall,
        SweetAlert,
    }

    public enum ActionStatus
    {
        NotStarted,
        Completed
    }

    public enum SpeechToTextApiType
    {
        Type1, //Symptom entry in chatbot
        Type2, // Yes or No to Question in symptom
        Type3, // Mark Yes or no for question in diagnostic 
        Type4,  //Symptom Submission
        Type6  //sweet alert
    }

    public enum MicControl
    {
        Application,
        User
    }
}
