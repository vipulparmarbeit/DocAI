using DoctorAI.AppModels;
using DoctorAI.Views;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.CognitiveServices.Speech;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using DoctorAI.ExtensionMethods;
using Newtonsoft.Json;
using System.Threading;

namespace DoctorAI.SpeechToText
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        LoginApiResponse loginApiResponse;
        SpeechRecognizer basicRecognizer;
        LoginView loginView;
        bool IsLoggedIn = false;
        bool IsApplicationReadyToTakeInput = false;
        List<Symptom> Symptoms { get; set; }
        Symptom newSymptom = new Symptom();
        public MainWindow()
        {
            InitializeComponent();
            Symptoms = new List<Symptom>();
            this.Width = System.Windows.SystemParameters.WorkArea.Width;
            double x = System.Windows.SystemParameters.WorkArea.Left;// +this.Width;
            double y = System.Windows.SystemParameters.WorkArea.Bottom - this.Height;
            WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = x;
            this.Top = y;
            this.Loaded += MainWindow_Loaded;
            this.Deactivated += MainWindow_Deactivated;

            this.Topmost = true;
            loginView = new LoginView();
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var config = SpeechConfig.FromSubscription(DocAIAppContext.SPEECH_SUBSCRIPTION_CODE, DocAIAppContext.SPEECH_SUBSCRIPTION_REGION);
            config.SpeechRecognitionLanguage = "en-US";
            basicRecognizer = new SpeechRecognizer(config);
            basicRecognizer.Recognizing += BasicRecognizer_Recognizing;
            basicRecognizer.Recognized += BasicRecognizer_Recognized;

            RegistryKey appRegKey = Registry.CurrentUser.OpenSubKey(DocAIAppContext.APP_REGISTRY_ENTRY_KEY);
            Messenger.Default.Register<LoginApiResponse>(this, OnLoggin);
            if (appRegKey != null)
            {
                string userId = appRegKey.GetValue("uid").ToString();
                string chatbotUrl = appRegKey.GetValue("redirect_url").ToString();
                string returnText = appRegKey.GetValue("text").ToString();
                string user_type = appRegKey.GetValue("user_type").ToString();
                string baseUrl = appRegKey.GetValue("baseUrl").ToString();
                if(!string.IsNullOrEmpty(userId))
                {
                    IsLoggedIn = true;

                    loginApiResponse = new LoginApiResponse
                    {
                        user_id = userId,
                        user_type = user_type,
                        redirect_url = chatbotUrl,
                    };

                    btnLogin.Content = "LOG OUT";
                    btnLogin.Background = new SolidColorBrush(Colors.Red);
                    System.Diagnostics.Process.Start(chatbotUrl);
                    await TextToSpeech(returnText);
                    //TO DO : Launch update tool
                    // AutoUpdater.Start(DocAIAppContext.APP_UPDATE_URL);
                   
                }
                await basicRecognizer.StartContinuousRecognitionAsync();
            }
        }

        private async void OnLoggin(LoginApiResponse loginResponse)
        {
            this.loginApiResponse = loginResponse;
            IsLoggedIn = true;
            btnLogin.Content = "LOG OUT";
            await TextToSpeech(loginResponse.text);
        }

        private async void BasicRecognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(e.Result.Text)) return;
            string control = basicRecognizer.Properties.GetProperty(DocAIAppContext.MIC_CONTROL_KEY);
            if (control == DocAIAppContext.MIC_CONTROL_USER)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txtSpeechToText.Text = e.Result.Text.ToString();
                });
                if (IsApplicationReadyToTakeInput)
                {
                    if (!newSymptom.IsSubmitted)
                    {
                        if (newSymptom.Steps.Count == 0)
                        {
                           await HandleNewSymptom(e.Result.Text);
                        }
                        else
                        {
                            if (newSymptom.IsMultiQuestion)
                            {
                                HandleMultiQuestions(e.Result.Text);
                            }
                        }
                    }
                }
            }
            if (control == DocAIAppContext.MIC_CONTROL_APP)
            {
                basicRecognizer.Properties.SetProperty(DocAIAppContext.MIC_CONTROL_KEY, DocAIAppContext.MIC_CONTROL_USER);
                IsApplicationReadyToTakeInput = true;
            }
        }
        private async void HandleMultiQuestions(string text)
        {
            var question = newSymptom.Steps.LastOrDefault(x => x.ActionType == ActionType.MultiquestionApiCall);
            if (question != null && string.IsNullOrEmpty(question.UserInput))
            {
                string userAnwser = text.ToLower();
                if (userAnwser == "yes" || userAnwser == "yea")
                {
                    IsApplicationReadyToTakeInput = false;
                    MultiQuestionApiResponse multiQuestionApiResponse = new JavaScriptSerializer().Deserialize<MultiQuestionApiResponse>(question.ResponseData);
                    //Record user answer for symptom question
                    RecordUserAnswerForQuestion(multiQuestionApiResponse, text);
                    question.UserInput = "yes";
                 
                    //Get next question
                    multiQuestionApiResponse = await GetNewQuestion();
                    if (multiQuestionApiResponse.text.ToLower().Contains("none of the above"))
                    {
                        IsApplicationReadyToTakeInput = false;
                        //submits symptom
                        SubmitSymptom();
                        newSymptom.IsSubmitted = true;
                        newSymptom = new Symptom();
                        IsApplicationReadyToTakeInput = true;
                    }
                    else
                    {
                        await TextToSpeech(multiQuestionApiResponse.text);
                        await Task.Delay(1000);
                        IsApplicationReadyToTakeInput = true;
                    }
                }
                if (userAnwser == "no")
                {
                    IsApplicationReadyToTakeInput = false;
                    MultiQuestionApiResponse multiQuestionApiResponse = await GetNewQuestion();
                    await TextToSpeech(multiQuestionApiResponse.text);
                    await Task.Delay(1000);
                    IsApplicationReadyToTakeInput = true;
                }
            }
        }
        private void BasicRecognizer_Recognizing(object sender, SpeechRecognitionEventArgs e)
        {
        }

        private void MainWindow_Deactivated(object sender, EventArgs e)
        {
            Window window = (Window)sender;
            window.Topmost = true;
        }

        private  void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (btnLogin.Content.ToString() == "LOG IN")
            {
                loginView.Show();
                loginView.GetLoginDomains();
            }
            else if (btnLogin.Content.ToString() == "LOG OUT")
            {
                RegistryKey appRegKey = Registry.CurrentUser.OpenSubKey(DocAIAppContext.APP_REGISTRY_ENTRY_KEY);
                if (appRegKey != null)
                {
                    using (RegistryKey softRegKey = Registry.CurrentUser.OpenSubKey(@"SOFTWARE", true))
                    {
                        softRegKey.DeleteSubKey("Doca");
                        btnLogin.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#027EFF"));
                        btnLogin.Content = "LOG IN";
                        this.loginApiResponse = null;
                        IsLoggedIn = false;
                    }
                }
            }
        }

        private string RecordUserAnswerForQuestion(MultiQuestionApiResponse multiQuestionApiResponse, string yesno)
        {
            string apiResponse;
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type2);
            var requestObject = new { patient_id = loginApiResponse.user_id, type = type, question_id = multiQuestionApiResponse.question_id, text = yesno };
            var requestJson = new JavaScriptSerializer().Serialize(requestObject);

            apiResponse = RestAPI.RestApiClient.POST(DocAIAppContext.URL_SPEECH_TO_TEXT, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
            return apiResponse;
        }

        private string SubmitSymptom()
        {
            string apiResponse;
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type4);
            var requestObject = new { patient_id = loginApiResponse.user_id, type = type, question_id = "yes", text = "yes" };
            var requestJson = new JavaScriptSerializer().Serialize(requestObject);

            apiResponse = RestAPI.RestApiClient.POST(DocAIAppContext.URL_SPEECH_TO_TEXT, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
            return apiResponse;
        }

        private Symptom CallSpeechToTextApiToSubmitNewSymptom(string symptomText)
        {
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type1);
            var jj = new { patient_id = loginApiResponse.user_id, type = type, question_id = "q_1", text = symptomText };
            var json = new JavaScriptSerializer().Serialize(jj);

            string apiResponse = RestAPI.RestApiClient.POST(DocAIAppContext.URL_SPEECH_TO_TEXT, DocAIAppContext.CONTENT_TYPE_Json, json);
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);

            newSymptom = new Symptom { SymptomText = symptomText };
            newSymptom.Steps.Add(new SymptomStep
            {
                ActionType = ActionType.SpeechToTextApiCall_SymtomEntry,
                ActionDescription = DocAIAppContext.URL_SPEECH_TO_TEXT,
                RequestData = json,
                ResponseData = apiResponse,
                Status = ActionStatus.Completed
            });
            Symptoms.Add(newSymptom);
            return newSymptom;
        }
        private async Task<string>  CallResponseCollectorApi()
        {
            var requestData = new { patient_id = loginApiResponse.user_id };
            var requestJson = JsonConvert.SerializeObject(requestData); //new JavaScriptSerializer().Serialize(requestData);

            string apiResponse;
            int trail = 0;
            ResponseCollectorApiResponse responseCollectorApiResponse = new ResponseCollectorApiResponse();
            do
            {
                apiResponse = RestAPI.RestApiClient.POST1(DocAIAppContext.URL_RESPONSE_COLLECTOR, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
              
                trail = trail + 1;
                await Task.Delay(1000);
            } while (responseCollectorApiResponse.result.ToLower() != "true" && trail <= 3);
            return apiResponse;
        }
        private async Task HandleNewSymptom(string text)
        {
            string apiResponse;
            //calls Speech to text API which fills symptom (e.g. I have headache) in chatbot textbox  and submit to get questions of symptom
            CallSpeechToTextApiToSubmitNewSymptom(text);
            await Task.Delay(1000);

            //calls response collector API which decides if symptom is multi question or simple one.
            var requestData = new { patient_id = loginApiResponse.user_id };
            var requestJson = JsonConvert.SerializeObject(requestData);
            apiResponse = await CallResponseCollectorApi();
            ResponseCollectorApiResponse responseCollectorApiResponse = new JavaScriptSerializer().Deserialize<ResponseCollectorApiResponse>(apiResponse);
            if (responseCollectorApiResponse.result.ToLower() == "true")
            {
                newSymptom.IsMultiQuestion = true;
                newSymptom.Steps.Add(new SymptomStep
                {
                    ActionType = ActionType.ResponseCollectorApiCall,
                    ActionDescription = DocAIAppContext.URL_RESPONSE_COLLECTOR,
                    RequestData = requestJson,
                    ResponseData = apiResponse,
                    Status = ActionStatus.Completed
                });
                // if Symptom is multi question
                if (responseCollectorApiResponse.text_data.ToLower() == "please select one or more from below :" ||
                   responseCollectorApiResponse.text_data.ToLower() == "please answer yes or no for each of the question :")
                {
                    //speech out please select one or more below
                    await TextToSpeech(responseCollectorApiResponse.text_data);
                    await Task.Delay(500);
                    IsApplicationReadyToTakeInput = false;

                    //get first question
                    MultiQuestionApiResponse multiQuestionApiResponse = await GetNewQuestion();

                    //speak out first question to record user answer in yes or no
                    await TextToSpeech(multiQuestionApiResponse.text);
                    await Task.Delay(1000);
                    IsApplicationReadyToTakeInput = true;
                }
                else if (responseCollectorApiResponse.text_data.ToLower().StartsWith("noted that you have")) // if symptom is simple which doesn't require any input from user
                {
                    newSymptom.Steps.Add(new SymptomStep
                    {
                        ActionType = ActionType.SpeechToTextApiCall_Submit,
                        ActionDescription = "Single symptom Submit",
                        RequestData = requestJson,
                        ResponseData = apiResponse,
                        Status = ActionStatus.Completed
                    });

                    newSymptom.IsSubmitted = true;

                    //speak out "Noted your symptom"
                    await TextToSpeech(responseCollectorApiResponse.text_data);
                    await Task.Delay(1000);
                    IsApplicationReadyToTakeInput = true;
                }
            }
        }
        private async Task<MultiQuestionApiResponse> GetNewQuestion()
        {
            int trail = 0;
            string apiResponse;
            var requestData = new { patient_id = loginApiResponse.user_id };
            var requestJson = JsonConvert.SerializeObject(requestData); 
            MultiQuestionApiResponse multiQuestionApiResponse = new MultiQuestionApiResponse();
            do
            {
                apiResponse = RestAPI.RestApiClient.POST1(DocAIAppContext.URL_MULTI_QUESTION, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
                multiQuestionApiResponse = new JavaScriptSerializer().Deserialize<MultiQuestionApiResponse>(apiResponse);
                trail = trail + 1;
                await Task.Delay(1000);
            } while (multiQuestionApiResponse.result.ToLower() != "true" && trail <= 3);

            if (multiQuestionApiResponse.result.ToLower() == "true")
            {
                newSymptom.Steps.Add(new SymptomStep
                {
                    ActionType = ActionType.MultiquestionApiCall,
                    ActionDescription = DocAIAppContext.URL_MULTI_QUESTION,
                    RequestData = requestJson,
                    ResponseData = apiResponse,
                    Status = ActionStatus.Completed
                });
            }
            return multiQuestionApiResponse;
        }
        private async Task TextToSpeech(string text)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtTextToSpeech.Text = text;
            });
            IsApplicationReadyToTakeInput = false;
            string control = basicRecognizer.Properties.GetProperty(DocAIAppContext.MIC_CONTROL_KEY);
            basicRecognizer.Properties.SetProperty(DocAIAppContext.MIC_CONTROL_KEY, DocAIAppContext.MIC_CONTROL_APP);
            var config = SpeechConfig.FromSubscription(DocAIAppContext.SPEECH_SUBSCRIPTION_CODE, DocAIAppContext.SPEECH_SUBSCRIPTION_REGION);
            using (var synthesizer = new SpeechSynthesizer(config))
            {
                // Receive a text from console input and synthesize it to speaker.

                using (var result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            txtTextToSpeech.Text = string.Empty;
                        });
                    }
                   
                    else if (result.Reason == ResultReason.Canceled)
                    {
                        var cancellation = SpeechSynthesisCancellationDetails.FromResult(result);
                       // Console.WriteLine($"CANCELED: Reason={cancellation.Reason}");

                        if (cancellation.Reason == CancellationReason.Error)
                        {
                          //  Console.WriteLine($"CANCELED: ErrorCode={cancellation.ErrorCode}");
                          //  Console.WriteLine($"CANCELED: ErrorDetails=[{cancellation.ErrorDetails}]");
                          //  Console.WriteLine($"CANCELED: Did you update the subscription info?");
                        }
                    }
                    if (result.Reason == ResultReason.RecognizedSpeech)
                    {
                    }
                }
            }
        }
    }

    public enum MicControl
    {
        NotSet,
        User, 
        Application
    }
}
