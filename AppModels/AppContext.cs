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
        public const string URL_RESPONSE_COLLECTOR = "{0}/api/responseCollector.php";   //https://doctorai.ddxrx.net
        public const string URL_MULTI_QUESTION = "{0}/api/multi_question.php";
        

        public const string CONTENT_TYPE_x_www_form_urlencoded = "application/x-www-form-urlencoded";
        public const string CONTENT_TYPE_Json = "application/json";

        public const string APP_REGISTRY_ENTRY_KEY = @"SOFTWARE\Doca";

        public const string LOGIN_REST_API_PATH = "/api/app-login.php";

        public const string URL_APP_UPDATE = "http://3.16.23.187/en.xml";

        public const string URL_SPEECH_TO_TEXT = "{0}/api/speech_to_text.php";

        public const string SWEET_ALERT_SPEECH_TEXT = "Do you want to add more symptoms? Answer yes or no";

        public const string MEDICAL_DISCLAIMER = "You will use THE DDXRX Software Information only as a reference aid," +
                " and that such information is not intended to be (nor should it be used as) a substitute for the exercise of professional judgment." +
                " In view of the possibility of human error or changes in medical science," +
                " you should confirm the information in THE DDXRX Software Information through independent sources." +
                " You agree and acknowledge that you will, at all times," +
                " seek professional diagnosis and treatment for any medical condition and to discuss information obtained from THE DDXRX Software Information with their healthcare provider." +
                " The user understands that diseases that the user might consider appropriate may not appear on the list of disease links." +
                " The DDXRX software generated severity of illness scores," +
                " automatically generated encounter notes and treatments and other recommendations are educational purpose only," +
                " and not to be used for patient care. Individual physician must perform through examination of the patient and" +
                " deliver appropriate treatments without depending on THE DDXRX software." +
                "This site is designed to offer you general health information for educational purposes only." +
                "The health information furnished on this site and the interactive responses are not intended to be professional advice and are not intended to replace personal consultation with a qualified physician," +
                " pharmacist or other healthcare professional.You must always seek the advice of a professional for questions related to your disease, disease symptoms, and appropriate therapeutic treatments." +
                "If you have or suspect that you have a medical problem or condition, please contact a qualified healthcare provider immediately." +
                "By clicking on the 'I Agree' button below, you acknowledge that you have read, understand, and agree to be bound by the Terms and Conditions, " +
                "Consent for Telemedicne Treatment, Privacy Policy, Non - Disclosure Agreement and Medical Disclaimer.";

    }
}
