using DoctorAI.AppModels;
using GalaSoft.MvvmLight;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DoctorAI.ViewModel
{
   
    public class LoginViewModel : ViewModelBase
    {
        public ObservableCollection<LoginDomain> LoginDomainList { get; set; }
        public List<LoginMode> LoginModes { get; set; }
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public LoginViewModel()
        {
            IsDisclaimerVisible = false;
            IsAgreed = true;
            LoginDomainList = new ObservableCollection<LoginDomain>();
            LoginModes = new List<LoginMode>();
            LoginModes.Add(new LoginMode { Name = "Doctor", EnumName = LoginModeEnum.Doctor });
            LoginModes.Add(new LoginMode { Name = "Nurse", EnumName = LoginModeEnum.Nurse });
            LoginModes.Add(new LoginMode { Name = "Patient", EnumName = LoginModeEnum.Patient });
        }

        string username;
        public string UserName
        {
            get { return username; }
            set
            {
                username = value;
                RaisePropertyChanged(() => UserName);
            }
        }

        string password;
        public string Password
        {
            get { return password; }
            set
            {
                password = value;
                RaisePropertyChanged(() => Password);
            }
        }

        public string DisclaimerText
        {
            get { return DocAIAppContext.MEDICAL_DISCLAIMER; }
        }
        bool _isDisclaimerVisible;
        public bool IsDisclaimerVisible
        {
            get { return _isDisclaimerVisible; }
            set
            {
                _isDisclaimerVisible = value;
                RaisePropertyChanged(() => IsDisclaimerVisible);
            }
        }
        bool _isAgreed;
        public bool IsAgreed
        {
            get { return _isAgreed; }
            set
            {
                _isAgreed = value;
                RaisePropertyChanged(() => IsAgreed);
            }
        }
        //
        LoginMode loginAs;
        public LoginMode LoginAs
        {
            get { return loginAs; }
            set
            {
                loginAs = value;
                RaisePropertyChanged(() => LoginAs);
            }
        }

        LoginDomain selectedDomain;
        public LoginDomain SelectedDomain
        {
            get { return selectedDomain; }
            set
            {
                selectedDomain = value;
                RaisePropertyChanged(() => SelectedDomain);
            }
        }


        string errorMessage;
        public string ErrorMessage
        {
            get { return errorMessage; }
            set
            {
                errorMessage = value;
                RaisePropertyChanged(() => ErrorMessage);
            }
        }

    }
}