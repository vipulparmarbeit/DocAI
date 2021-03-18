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
using DoctorAI.Logging;
using DoctorAI.ViewModel;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Automation;

namespace DoctorAI.SpeechToText
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        [DllImport("user32.dll")]
        static extern bool IsIconic(IntPtr hWnd);

        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        LoginApiResponse loginApiResponse;
        SpeechRecognizer basicRecognizer;
        LoginView loginView;
        bool IsLoggedIn = false;
        bool IsApplicationReadyToTakeInput = false;
        List<Symptom> Symptoms { get; set; }
        Symptom newSymptom = new Symptom();

        public static string baseURL { get; set; }

        private MainViewModel vm;
        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = vm  = new MainViewModel();
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
            try
            {

                RegistryKey appRegKey = Registry.CurrentUser.OpenSubKey(DocAIAppContext.APP_REGISTRY_ENTRY_KEY);
                Messenger.Default.Register<LoginApiResponse>(this, OnLoggin);
                string returnText = "Hi";
                if (appRegKey != null)
                {
                    string userId = appRegKey.GetValue("uid").ToString();
                    string chatbotUrl = appRegKey.GetValue("redirect_url").ToString();
                    returnText = appRegKey.GetValue("text").ToString();
                    string user_type = appRegKey.GetValue("user_type").ToString();
                    string baseUrl = appRegKey.GetValue("baseUrl").ToString();
                    MainWindow.baseURL = baseUrl;
                    if (!string.IsNullOrEmpty(userId))
                    {
                        IsLoggedIn = true;

                        loginApiResponse = new LoginApiResponse
                        {
                            user_id = userId,
                            user_type = user_type,
                            redirect_url = chatbotUrl,
                            text = returnText
                        };

                        btnLogin.Content = "LOG OUT";
                        btnLogin.Background = new SolidColorBrush(Colors.Red);
                        System.Diagnostics.Process.Start(chatbotUrl);
                       
                        //TO DO : Launch update tool
                        // AutoUpdater.Start(DocAIAppContext.APP_UPDATE_URL);

                    }

                }
                if (IsLoggedIn)
                {
                    Logger.Log("Log-in successful");
                    Logger.Log(loginApiResponse.redirect_url);

                    var config = SpeechConfig.FromSubscription(DocAIAppContext.SPEECH_SUBSCRIPTION_CODE, DocAIAppContext.SPEECH_SUBSCRIPTION_REGION);
                    config.SpeechRecognitionLanguage = "en-US";
                    basicRecognizer = new SpeechRecognizer(config);
                    basicRecognizer.Recognizing += BasicRecognizer_Recognizing;
                    basicRecognizer.Recognized += BasicRecognizer_Recognized;
                    await basicRecognizer.StartContinuousRecognitionAsync();
                    await TextToSpeech(returnText);
                    await Task.Delay(500);
                    GiveMicControl(MicControl.User);
                }

            }
            catch (Exception ex)
            {
                Logger.Log(ex, ex.Message);
            }
        }

        private async void OnLoggin(LoginApiResponse loginResponse)
        {
            this.loginApiResponse = loginResponse;
            IsLoggedIn = true;
            btnLogin.Content = "LOG OUT";
            btnLogin.Background = new SolidColorBrush(Colors.Red);
            Logger.Log("Log-in successful");
            if(basicRecognizer == null)
            {
                var config = SpeechConfig.FromSubscription(DocAIAppContext.SPEECH_SUBSCRIPTION_CODE, DocAIAppContext.SPEECH_SUBSCRIPTION_REGION);
                config.SpeechRecognitionLanguage = "en-US";
                basicRecognizer = new SpeechRecognizer(config);
                basicRecognizer.Recognizing += BasicRecognizer_Recognizing;
                basicRecognizer.Recognized += BasicRecognizer_Recognized;
                await basicRecognizer.StartContinuousRecognitionAsync();
            }
            await TextToSpeech(loginResponse.text);
            await Task.Delay(1000);
            GiveMicControl(MicControl.User);
        }

        private async void BasicRecognizer_Recognized(object sender, SpeechRecognitionEventArgs e)
        {
            if (!IsLoggedIn || string.IsNullOrEmpty(e.Result.Text)) return;
            string control = basicRecognizer.Properties.GetProperty(DocAIAppContext.MIC_CONTROL_KEY);
            if (vm.IsAppInSleepMode)
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {
                    bool isAppWakeupFromSleep = IsWakeupcall(e.Result.Text);
                    if(isAppWakeupFromSleep)
                    {
                        vm.IsAppInSleepMode = false;
                        await TextToSpeech(this.loginApiResponse.text);
                        await Task.Delay(1000);
                        GiveMicControl(MicControl.User);
                    }
                }
                return;
            }
            else
            {
                if (!string.IsNullOrEmpty(e.Result.Text))
                {

                    bool isAppToGotoSleep = IsAppToSleep(e.Result.Text);
                    if (isAppToGotoSleep)
                    {
                        GiveMicControl(MicControl.Application);
                        vm.IsAppInSleepMode = true;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            txtSpeechToText.Text = string.Empty;
                        });
                        return;
                    }
                }
            }
            if (control == DocAIAppContext.MIC_CONTROL_USER)
            {
                
                string userinput = e.Result.Text;
                userinput = userinput.Trim().TrimEnd('.');
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txtSpeechToText.Text = userinput;
                });
                if (IsApplicationReadyToTakeInput)
                {
                    bool isAppTorefresh = IsRefreshApp(userinput);
                    if(isAppTorefresh)
                    {
                        RefreshChatbot();
                        return;
                    }
                    var sweetAlert = newSymptom.Steps.FirstOrDefault(x => x.ActionType == ActionType.SweetAlert);
                    if (sweetAlert != null && newSymptom.IsSubmitted)
                    {
                        Logger.Log(string.Format("User Speaks:{0}", userinput));
                        //handle sweet alert response
                        RecordSweetAlertAnswer(userinput);
                        if (userinput.ToLower().Trim('.') == "yes")
                        {
                            newSymptom = new Symptom();
                        }
                    }
                    else if (!newSymptom.IsSubmitted || string.IsNullOrEmpty(newSymptom.SymptomText))
                    {
                        if (newSymptom.Steps.Count == 0)
                        {
                           await HandleNewSymptom(userinput);
                        }
                        else if (newSymptom.IsMultiQuestion)
                        {
                            HandleMultiQuestions(userinput);
                        }
                    }
                }
            }
            if (control == DocAIAppContext.MIC_CONTROL_APP)
            {
               // basicRecognizer.Properties.SetProperty(DocAIAppContext.MIC_CONTROL_KEY, DocAIAppContext.MIC_CONTROL_USER);
              //  IsApplicationReadyToTakeInput = true;
            }
        }
        private async void HandleMultiQuestions(string text)
        {
            var question = newSymptom.Steps.LastOrDefault(x => x.ActionType == ActionType.MultiquestionApiCall);
            if (question != null && string.IsNullOrEmpty(question.UserInput))
            {
                string userAnwser = text.ToLower();
                if (userAnwser == "yes" || userAnwser == "yea" || userAnwser == "yes." || userAnwser == "yea.")
                {
                    userAnwser = "yes";
                }
                else if (userAnwser == "no" || userAnwser == "No.")
                {
                    userAnwser = "no";
                }
                if (userAnwser == "yes" || userAnwser == "no")
                {
                    GiveMicControl(MicControl.Application);
                    MultiQuestionApiResponse multiQuestionApiResponse = new JavaScriptSerializer().Deserialize<MultiQuestionApiResponse>(question.ResponseData);
                    //Record user answer for symptom question
                    RecordUserAnswerForQuestion(multiQuestionApiResponse, userAnwser);
                    question.UserInput = userAnwser;
                    Logger.Log(string.Format("User Speaks :{0}", userAnwser));
                    //Get next question
                    multiQuestionApiResponse = await GetNewQuestion();
                    if (multiQuestionApiResponse.result.ToLower() == "false" || multiQuestionApiResponse.text.ToLower().Contains("none of the above"))
                    {
                        //submits symptom
                        string apiResponse = SubmitSymptom();

                        SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
                        newSymptom.IsSubmitted = true;
                        var submittedSymtoms = this.Symptoms.Where(x => x.IsSubmitted);
                        if (submittedSymtoms.Count() >= 2)
                        {
                            string textToSpeech = "Do you want to add more symptoms? Answer yes or no";
                            newSymptom.Steps.Add(new SymptomStep
                            {
                                ActionType = ActionType.SweetAlert,
                                ActionDescription = textToSpeech,
                                RequestData = string.Empty,
                                ResponseData = string.Empty,
                                Status = ActionStatus.Completed
                            });
                            
                            await TextToSpeech(textToSpeech);
                            await Task.Delay(3000);
                        }
                        else
                        {
                            string textToSpeech = string.IsNullOrEmpty(speechToTextApiResponse.text) ? string.Format("Noted you have  {0}. What else you have",
                          newSymptom.SymptomText) : speechToTextApiResponse.text;
                            await TextToSpeech(textToSpeech);
                            await Task.Delay(3000);
                            newSymptom = new Symptom();
                            Logger.Log("Application is ready to take new symptom.");
                        }
                    }
                    else
                    {
                        await TextToSpeech(multiQuestionApiResponse.text);
                        await Task.Delay(5000);
                    }
                    GiveMicControl(MicControl.User);
                }

                /*
                if (userAnwser == "no" || userAnwser == "no.")
                {
                    Logger.Log(string.Format("User speaks :{0}", "No"));
                    GiveMicControl(MicControl.Application);
                    MultiQuestionApiResponse multiQuestionApiResponse = await GetNewQuestion();
                    if (multiQuestionApiResponse.result.ToLower() == "true")
                    {
                        if(multiQuestionApiResponse.text.ToLower().Contains("none of the above"))
                        {
                            //submit symptom
                            string apiResponse = SubmitSymptom();

                            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
                            newSymptom.IsSubmitted = true;
                            string textToSpeech = string.IsNullOrEmpty(speechToTextApiResponse.text) ? string.Format("Noted you have  {0}. What else you have",
                                newSymptom.SymptomText) : speechToTextApiResponse.text;

                            await TextToSpeech(textToSpeech);
                            await Task.Delay(1000);
                            newSymptom = new Symptom();
                          
                        }
                        else
                        {
                            await TextToSpeech(multiQuestionApiResponse.text);
                            await Task.Delay(1000);

                          
                        }
                    }
                    GiveMicControl(MicControl.User);
                }
                */
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
            string url = string.Format(DocAIAppContext.URL_SPEECH_TO_TEXT, MainWindow.baseURL);
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type2);
            Logger.Log("Speech to Text API CALL To record user answer");
            Logger.Log("----------------------------------------------------------------------------------------");
            var requestObject = new { patient_id = loginApiResponse.user_id, type = type, question_id = multiQuestionApiResponse.question_id, text = yesno };
            var requestJson = new JavaScriptSerializer().Serialize(requestObject);
            Logger.Log(string.Format("Request body:{0}", requestJson));
            apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
            Logger.Log(string.Format("Response :{0}", apiResponse));
            Logger.Log("----------------------------------------------------------------------------------------");
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
            return apiResponse;
        }

        private string RecordSweetAlertAnswer(string yesno)
        {
            string apiResponse = string.Empty;
            string answer = string.Empty;
            if(yesno.ToLower().TrimEnd('.').ToLower() == "yes" || yesno.ToLower().Trim('.' )== "yea")
            {
                answer = "yes";
            }
            else if (yesno.ToLower().TrimEnd('.') == "no")
            {
                answer = "no";
            }

            if(answer == "yes" || answer =="no")
            {
                string url = string.Format(DocAIAppContext.URL_SPEECH_TO_TEXT, MainWindow.baseURL);
                string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type6);
                Logger.Log("Speech to Text API CALL To record Sweet Alert answer");
                Logger.Log("----------------------------------------------------------------------------------------");
                var requestObject = new { patient_id = loginApiResponse.user_id, type = type, question_id = "q_1", text = yesno };
                var requestJson = new JavaScriptSerializer().Serialize(requestObject);
                Logger.Log(string.Format("Request body:{0}", requestJson));
                apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
                Logger.Log(string.Format("Response :{0}", apiResponse));
                Logger.Log("----------------------------------------------------------------------------------------");
                SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
               
            }
            return apiResponse;
        }

        private string SubmitSymptom()
        {
            string apiResponse;
            string url = string.Format(DocAIAppContext.URL_SPEECH_TO_TEXT, MainWindow.baseURL);
            Logger.Log("Speech to Text API CALL for submission");
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type4);
            Logger.Log("----------------------------------------------------------------------------------------");
            Logger.Log(string.Format("URL:{0}", url));
            var requestObject = new { patient_id = loginApiResponse.user_id, type = type, question_id = "yes", text = "yes" };
            var requestJson = new JavaScriptSerializer().Serialize(requestObject);
            Logger.Log(string.Format("Request body:{0}", requestJson));
            apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
            Logger.Log(string.Format("Response :{0}", apiResponse));
            Logger.Log("----------------------------------------------------------------------------------------");
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
            return apiResponse;
        }

        private Symptom CallSpeechToTextApiToSubmitNewSymptom(string symptomText)
        {
            string url = string.Format(DocAIAppContext.URL_SPEECH_TO_TEXT, MainWindow.baseURL);
            string type = Symptom.GetSpeechToType_Text(SpeechToTextApiType.Type1);
            var request = new { patient_id = loginApiResponse.user_id, type = type, question_id = "q_1", text = symptomText };
            var requestJson = new JavaScriptSerializer().Serialize(request);

            Logger.Log(string.Format("Symptom :{0}", symptomText));
            Logger.Log("Speech to Text API CALL to get symptom detail");
            Logger.Log("----------------------------------------------------------------------------------------");
            Logger.Log(string.Format("URL:{0}", url));
            Logger.Log(string.Format("Request body:{0}", requestJson));
            string apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
            SpeechToTextApiResponse speechToTextApiResponse = new JavaScriptSerializer().Deserialize<SpeechToTextApiResponse>(apiResponse);
            Logger.Log(string.Format("Response :{0}", apiResponse));
            Logger.Log("----------------------------------------------------------------------------------------");
            newSymptom = new Symptom { SymptomText = symptomText };
            newSymptom.Steps.Add(new SymptomStep
            {
                ActionType = ActionType.SpeechToTextApiCall_SymtomEntry,
                ActionDescription = url,
                RequestData = requestJson,
                ResponseData = apiResponse,
                Status = ActionStatus.Completed
            });
            Symptoms.Add(newSymptom);
            return newSymptom;
        }
        private async Task<string>  CallResponseCollectorApi()
        {
            var requestData = new { patient_id = loginApiResponse.user_id };
            var requestJson = JsonConvert.SerializeObject(requestData); 

            string apiResponse;
            int trail = 0;
            string url = string.Format(DocAIAppContext.URL_RESPONSE_COLLECTOR, MainWindow.baseURL);
            Logger.Log("Response Collector API CALL");
            Logger.Log("----------------------------------------------------------------------------------------");
            Logger.Log(string.Format("URL:{0}", url));
            Logger.Log(string.Format("Request body:{0}", requestJson));
            ResponseCollectorApiResponse responseCollectorApiResponse = new ResponseCollectorApiResponse();
            do
            {
                Logger.Log(string.Format("Attempt # :{0}", trail + 1));
                apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
                Logger.Log(string.Format("Response :{0}", apiResponse));
                responseCollectorApiResponse = new JavaScriptSerializer().Deserialize<ResponseCollectorApiResponse>(apiResponse);
                trail = trail + 1;
                await Task.Delay(1000);
            } while (responseCollectorApiResponse.result.ToLower() != "true" && trail <= 5);
            Logger.Log("----------------------------------------------------------------------------------------");
            return apiResponse;
        }
        private async Task HandleNewSymptom(string text)
        {
            try
            {
                GiveMicControl(MicControl.Application);
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
                    newSymptom.Steps.Add(new SymptomStep
                    {
                        ActionType = ActionType.ResponseCollectorApiCall,
                        ActionDescription = string.Format(DocAIAppContext.URL_RESPONSE_COLLECTOR, MainWindow.baseURL),
                        RequestData = requestJson,
                        ResponseData = apiResponse,
                        Status = ActionStatus.Completed
                    });
                    // if Symptom is multi question
                    if (responseCollectorApiResponse.text_data.ToLower() == "please select one or more from below :" ||
                       responseCollectorApiResponse.text_data.ToLower() == "please answer yes or no for each of the question :")
                    {
                        newSymptom.IsMultiQuestion = true;

                        var submittedSymtoms = this.Symptoms.Where(x => x.IsSubmitted);
                        if (submittedSymtoms.Count() >= 2)
                        {
                            Logger.Log("SWEET ALERT appears");
                            RecordSweetAlertAnswer("yes");
                        }

                        //speech out please select one or more below
                        await TextToSpeech(responseCollectorApiResponse.text_data);
                        await Task.Delay(500);
                        //get first question
                        MultiQuestionApiResponse multiQuestionApiResponse = await GetNewQuestion();

                        //speak out first question to record user answer in yes or no
                        await TextToSpeech(multiQuestionApiResponse.text);
                        await Task.Delay(1000);
                       
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

                        var submittedSymtoms = this.Symptoms.Where(x => x.IsSubmitted);
                        if (submittedSymtoms.Count() >= 2)
                        {
                            await TextToSpeech(responseCollectorApiResponse.text_data);
                            await Task.Delay(2000);

                            Logger.Log("SWEET ALERT appears");
                            string textToSpeech = DocAIAppContext.SWEET_ALERT_SPEECH_TEXT;
                            newSymptom.Steps.Add(new SymptomStep
                            {
                                ActionType = ActionType.SweetAlert,
                                ActionDescription = textToSpeech,
                                RequestData = string.Empty,
                                ResponseData = string.Empty,
                                Status = ActionStatus.Completed
                            });

                            await TextToSpeech(textToSpeech);
                            await Task.Delay(2000);
                        }
                        else
                        {
                            await TextToSpeech(responseCollectorApiResponse.text_data);
                            await Task.Delay(2000);
                            newSymptom = new Symptom();
                            Logger.Log("Application is ready to take new symptom.");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
            }
            finally
            {
                GiveMicControl(MicControl.User);
            }
        }

        private void GiveMicControl(MicControl micControl)
        {
            if(micControl == MicControl.Application)
            {
                IsApplicationReadyToTakeInput = false;
                basicRecognizer.Properties.SetProperty(DocAIAppContext.MIC_CONTROL_KEY, DocAIAppContext.MIC_CONTROL_APP);
            }
            else
            {
                IsApplicationReadyToTakeInput = true;
                basicRecognizer.Properties.SetProperty(DocAIAppContext.MIC_CONTROL_KEY, DocAIAppContext.MIC_CONTROL_USER);
            }
        }
        private async Task<MultiQuestionApiResponse> GetNewQuestion()
        {
            int trail = 0;
            string apiResponse;
            var requestData = new { patient_id = loginApiResponse.user_id };
            var requestJson = JsonConvert.SerializeObject(requestData); 
            MultiQuestionApiResponse multiQuestionApiResponse = new MultiQuestionApiResponse();
            Logger.Log("Multi Question API CALL");
            Logger.Log("----------------------------------------------------------------------------------------");
            string url = string.Format(DocAIAppContext.URL_MULTI_QUESTION, MainWindow.baseURL);
            Logger.Log(string.Format("URL:{0}", url));
            Logger.Log(string.Format("Request body:{0}", requestJson));
            do
            {
                Logger.Log(string.Format("Attempt # :{0}", trail + 1));
                apiResponse = RestAPI.RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_Json, requestJson);
                Logger.Log(string.Format("Response :{0}", apiResponse));
                multiQuestionApiResponse = new JavaScriptSerializer().Deserialize<MultiQuestionApiResponse>(apiResponse);
                trail = trail + 1;
                await Task.Delay(1000);
            } while (multiQuestionApiResponse.result.ToLower() != "true" && trail <= 5);
            Logger.Log("----------------------------------------------------------------------------------------");
            if (multiQuestionApiResponse.result.ToLower() == "true")
            {
                newSymptom.Steps.Add(new SymptomStep
                {
                    ActionType = ActionType.MultiquestionApiCall,
                    ActionDescription = url,
                    RequestData = requestJson,
                    ResponseData = apiResponse,
                    Status = ActionStatus.Completed
                });
            }
            return multiQuestionApiResponse;
        }
        private async Task TextToSpeech(string text)
        {
            if (string.IsNullOrEmpty(text)) return;
            Logger.Log(string.Format("Application Speaks to user:{0}", text));
            Application.Current.Dispatcher.Invoke(() =>
            {
                txtTextToSpeech.Text = text;
            });
            GiveMicControl(MicControl.Application);
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
                            txtSpeechToText.Text = string.Empty;
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

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            if(IsLoggedIn)
            {
                Logger.Log("Application gets RESET");
                newSymptom = new Symptom();
                txtSpeechToText.Text = string.Empty;
                txtTextToSpeech.Text = string.Empty;
                Symptoms.Clear();
                GiveMicControl(MicControl.User);
            }
        }

        private bool IsAppToSleep(string text)
        {
            bool isAppToSleep = false;
            string speech = text.ToLower();

            if (speech.Contains("stop") ||
                speech.StartsWith("genie stop") ||
                speech.StartsWith("hello genie stop") ||
                speech.StartsWith("hey genie stop") ||
                speech.Contains("go to sleep") ||
                speech.Contains("sleep") ||
                speech.StartsWith("genie sleep") ||
                speech.StartsWith("hello genie sleep") ||
                speech.StartsWith("hey genie sleep")
                )
            {
                isAppToSleep = true;
            }
            return isAppToSleep;
        }

        private bool IsWakeupcall(string text)
        {
            bool returnVal = false;

            string speech = text.ToLower();
            if (speech.Contains("wake up") ||
                 speech.StartsWith("genie wake up") ||
                 speech.StartsWith("hello genie wake up") ||
                 speech.StartsWith("hey genie wake up") ||
                 speech.StartsWith("hello genie restart") ||
                 speech.Contains("start") ||
                  speech.Equals("hi genie") ||
                   speech.Equals("hey genie")
                   )
            {
                returnVal = true;
            }
            return returnVal;
        }

        private bool IsRefreshApp(string text)
        {
            bool returnVal = false;
            string speech = text.ToLower();
            if (speech.Contains("refresh") ||
                speech.Contains("restart") ||
                speech.StartsWith("hi genie refresh") ||
                speech.StartsWith("hi genie restart")
                )
            {
                returnVal = true;
            }
            return returnVal;
        }

        private async void RefreshChatbot()
        {
            GiveMicControl(MicControl.Application);
            await TextToSpeech("Please wait.. Working on it.");

            string browser = GetDefaultBrowser();
            browser = browser.ToLower();
            string browserProcName = "chrome";
            if (browser.Contains("edge"))  //MSEdgeHTM
            {
                browserProcName = "msedge";
            }
            else if (browser.Contains("chrome"))  //chrome
            {
                browserProcName = "chrome";
            }
            Logger.Log(string.Format("Default Browser:{0}", browser));
            bool isdone = RefreshchatbotPage(browserProcName);
            if (isdone)
            {
                System.Diagnostics.Process.Start(loginApiResponse.redirect_url);
                Thread.Sleep(1000);
                Logger.Log("Application gets Refreshed");
                newSymptom = new Symptom();
                Application.Current.Dispatcher.Invoke(() =>
                {
                    txtSpeechToText.Text = string.Empty;
                    txtTextToSpeech.Text = string.Empty;
                    Symptoms.Clear();
                });
            }
            await TextToSpeech("Chatbot has been refreshed");
            GiveMicControl(MicControl.User);
        }

        private string GetDefaultBrowser()
        {
            string browser = "chrome";
            RegistryKey regkey;
            // Check if we are on Vista or Higher
            OperatingSystem OS = Environment.OSVersion;
            if ((OS.Platform == PlatformID.Win32NT) && (OS.Version.Major >= 6))
            {
                regkey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\shell\\Associations\\UrlAssociations\\http\\UserChoice", false);
                if (regkey != null)
                {
                    browser = regkey.GetValue("Progid").ToString();
                }
                else
                {
                    regkey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\IE.HTTP\\shell\\open\\command", false);
                    browser = regkey.GetValue("").ToString();
                }
            }
            else
            {
                regkey = Registry.ClassesRoot.OpenSubKey("http\\shell\\open\\command", false);
                browser = regkey.GetValue("").ToString();
            }
            return browser;
        }

        private bool RefreshchatbotPage(string browser)
        {
            bool isdone = false;
            Process[] processes = Process.GetProcessesByName(browser);
            if (processes.Length > 0)
            {
                List<string> titles = new List<string>();
                IntPtr hWnd = IntPtr.Zero;
                int id = 0;
                int numBrowserTabs = processes.Length;
                foreach (Process proc in processes)
                {
                    if (proc.MainWindowTitle.Length > 0)
                    {
                        hWnd = proc.MainWindowHandle;
                        id = proc.Id;
                        break;
                    }
                }

                bool isMinimized = IsIconic(hWnd);
                if (isMinimized)
                {
                    ShowWindow(hWnd, 9); //restore tab
                    Thread.Sleep(1000);
                }
                SetForegroundWindow(hWnd);
                System.Windows.Forms.SendKeys.SendWait("^1"); // change focus to first tab
                Thread.Sleep(100);
                int next = 1;
                string title;
                while (next <= numBrowserTabs)
                {
                    try
                    {
                        // title = Process.GetProcessById(id).MainWindowTitle.Replace(" - Google Chrome", "");
                        title = Process.GetProcessById(id).MainWindowTitle;
                        var process = Process.GetProcessById(id);
                        AutomationElement root = AutomationElement.FromHandle(process.MainWindowHandle);
                        var SearchBar = root.FindFirst(TreeScope.Descendants, new PropertyCondition(AutomationElement.NameProperty, "Address and search bar"));
                        if(SearchBar != null)
                        {
                            bool valuePatternExist = (bool)SearchBar.GetCurrentPropertyValue(AutomationElement.IsValuePatternAvailableProperty);
                            if (valuePatternExist)
                            {
                                ValuePattern val = SearchBar.GetCurrentPattern(ValuePattern.Pattern) as ValuePattern;
                                if (val.Current.Value.Contains("chat-bot.php"))
                                {
                                    System.Windows.Forms.SendKeys.SendWait("^{w}"); // close tab.
                                    isdone = true;
                                    Thread.Sleep(100);
                                }
                            }
                        }
                        /*
                        if (title.ToLower().Contains("doctor ai") ||
                            title.ToLower().Contains("ddxrx") ||
                            title.ToLower().Contains("human anatomy"))
                        {
                            //       System.Windows.Forms.SendKeys.SendWait("^{r}"); // refresh tab.
                            System.Windows.Forms.SendKeys.SendWait("^{w}"); // close tab.
                            isdone = true;
                            Thread.Sleep(100);
                        }
                        */
                        next++;
                        System.Windows.Forms.SendKeys.SendWait("^{TAB}"); // change focus to next tab
                        Thread.Sleep(100);
                    }
                    catch (Exception ex)
                    {
                        next++;
                    }
                }
            }
            return isdone;
        }
    }
}
