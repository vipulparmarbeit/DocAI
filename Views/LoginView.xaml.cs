using DoctorAI.AppModels;
using DoctorAI.Logging;
using DoctorAI.RestAPI;
using DoctorAI.SpeechToText;
using DoctorAI.ViewModel;
using GalaSoft.MvvmLight.Messaging;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace DoctorAI.Views
{
    /// <summary>
    /// Interaction logic for LoginView.xaml
    /// </summary>
    public partial class LoginView : Window
    {
        LoginViewModel vm;
        LoginApiResponse loginApiResponse;
        public LoginView()
        {
            InitializeComponent();

            vm = new LoginViewModel();

            this.Width = System.Windows.SystemParameters.WorkArea.Width;
            double x = System.Windows.SystemParameters.WorkArea.Left;
            double y = System.Windows.SystemParameters.WorkArea.Bottom - this.Height;
            WindowStartupLocation = WindowStartupLocation.Manual;
            this.Left = x;
            this.Top = y;
            this.Topmost = true;
            this.DataContext = vm;
            vm.LoginAs = vm.LoginModes.FirstOrDefault(z => z.EnumName == LoginModeEnum.Patient);
        }

        public void GetLoginDomains()
        {
            try
            {
                if (vm.LoginDomainList.Count > 0) return;

                string apiResponse = RestApiClient.POST(DocAIAppContext.URL_LOGIN_DOMAIN_LIST, DocAIAppContext.CONTENT_TYPE_x_www_form_urlencoded, string.Empty);
                var objUrls = new JavaScriptSerializer().Deserialize<LoginDomain[]>(apiResponse);
                foreach (var item in objUrls)
                {
                    vm.LoginDomainList.Add(item);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
            }
        }

       private bool  LoginToApi()
        {
            try
            {
                bool isloginSuccess = false;

                string url = "https://" + vm.SelectedDomain.url + DocAIAppContext.LOGIN_REST_API_PATH;
                ServicePointManager.Expect100Continue = true;
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls
                       | SecurityProtocolType.Tls11
                       | SecurityProtocolType.Tls12
                       | SecurityProtocolType.Ssl3;

                string postData = "username=" + vm.UserName + "&password=" + vm.Password + "&url=" + vm.SelectedDomain.url;

                MainWindow.baseURL = "https://" + vm.SelectedDomain.url;

                Logger.Log("LOGIN API CALL");
                Logger.Log("----------------------------------------------------------------------------------------");
                Logger.Log(string.Format("URL:{0}", url));
                Logger.Log(string.Format("Request body:{0}", postData));
                int trail = 0;
                string apiResponse = string.Empty;
                do
                {
                    Logger.Log(string.Format("Attempt # :{0}", trail + 1));
                    apiResponse = RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_x_www_form_urlencoded, postData);
                    Logger.Log(string.Format("Response :{0}", apiResponse));
                    loginApiResponse = new JavaScriptSerializer().Deserialize<LoginApiResponse>(apiResponse);
                    trail = trail + 1;
                    isloginSuccess = loginApiResponse.result_type.Equals("success");
                    Thread.Sleep(1000);
                } while (!isloginSuccess && trail <= 5);
                Logger.Log("----------------------------------------------------------------------------------------");
               
                if (!isloginSuccess) loginApiResponse = null;
                return isloginSuccess;
            }
            catch (Exception e)
            {
                loginApiResponse = null;
                return false;
            }
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (vm != null)
            { vm.Password = ((PasswordBox)sender).Password; }
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            Login();
        }

        private void BtnShowDisclaimer_Click(object sender, RoutedEventArgs e)
        {
            vm.IsDisclaimerVisible = true;
            vm.ErrorMessage = string.Empty;
        }
        private void btnDisclaimerCancel_Click(object sender, RoutedEventArgs e)
        {
            vm.IsDisclaimerVisible = false;
        }
        
        void Login()
        {
            if (vm.SelectedDomain != null)
            {
                bool isSucceess = LoginToApi();
                if (isSucceess)
                {
                    vm.ErrorMessage = string.Empty;
                    RegistryKey appRegKey = Registry.CurrentUser.OpenSubKey(DocAIAppContext.APP_REGISTRY_ENTRY_KEY);
                    if (appRegKey == null)
                    {
                        RegistryKey key = Registry.CurrentUser.CreateSubKey(DocAIAppContext.APP_REGISTRY_ENTRY_KEY);

                        //storing the values  
                        key.SetValue("uid", loginApiResponse.user_id);
                        key.SetValue("username", loginApiResponse.username);
                        key.SetValue("group_id", loginApiResponse.group_id);
                        key.SetValue("user_type", loginApiResponse.user_type);
                        key.SetValue("redirect_url", loginApiResponse.redirect_url);
                        key.SetValue("text", loginApiResponse.text);
                        key.SetValue("baseUrl", "https://" + vm.SelectedDomain.url);
                        key.Close();

                    }
                    this.Hide();
                    System.Diagnostics.Process.Start(loginApiResponse.redirect_url);
                    Messenger.Default.Send<LoginApiResponse>(loginApiResponse);
                    vm.IsDisclaimerVisible = false;
                }
                else
                    vm.ErrorMessage = "Logging failed";
            }
        }

        private void BtnCancle_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }
}
