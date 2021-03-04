using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DoctorAI.AppModels
{
    public class DocAIAppContext
    {
        public const string MIC_CONTROL_USER = "User";
        public const string MIC_CONTROL_APP = "Application";
        public const string MIC_CONTROL_KEY = "MicControl";
        public const string SPEECH_SUBSCRIPTION_CODE = "7b2227d849764e738cdfeab0a7880d53";
        public const string SPEECH_SUBSCRIPTION_REGION = "centralus";

        public const string URL_LOGIN_DOMAIN_LIST = "http://hidoctor.ddxrx.net/api/app-version.php";
        public const string URL_RESPONSE_COLLECTOR = "https://doctorai.ddxrx.net/api/responseCollector.php";
        public const string URL_MULTI_QUESTION = "https://doctorai.ddxrx.net/api/multi_question.php";
        

        public const string CONTENT_TYPE_x_www_form_urlencoded = "application/x-www-form-urlencoded";
        public const string CONTENT_TYPE_Json = "application/json";

        public const string APP_REGISTRY_ENTRY_KEY = @"SOFTWARE\Doca";

        public const string LOGIN_REST_API_PATH = "/api/app-login.php";

        public const string URL_APP_UPDATE = "http://3.16.23.187/en.xml";

        public const string URL_SPEECH_TO_TEXT = "https://doctorai.ddxrx.net/api/speech_to_text.php";

    }
}
