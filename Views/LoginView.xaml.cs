using DoctorAI.AppModels;
using DoctorAI.RestAPI;
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

        bool LoginToApi()
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

                string apiResponse = RestApiClient.POST(url, DocAIAppContext.CONTENT_TYPE_x_www_form_urlencoded, postData);
                loginApiResponse = new JavaScriptSerializer().Deserialize<LoginApiResponse>(apiResponse);

                isloginSuccess = loginApiResponse.result_type.Equals("success");
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
            if (vm.SelectedDomain != null)
            {
                bool isSucceess = LoginToApi();
                if (isSucceess)
                {
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
                    //    PolicyForm frmPolicy = new PolicyForm(objcloud.redirect_url);
                       // frmPolicy.ShowDialog();
                      
                    }
                    this.Hide();
                    System.Diagnostics.Process.Start(loginApiResponse.redirect_url);
                    Messenger.Default.Send<LoginApiResponse>(loginApiResponse);
                }
                else
                    MessageBox.Show("Logging failed");
            }
         
        }

        private void BtnCancle_Click(object sender, RoutedEventArgs e)
        {
            this.Hide();
        }
    }


}
