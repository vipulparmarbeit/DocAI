using GalaSoft.MvvmLight;

namespace DoctorAI.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel()
        {
           
        }

        private bool isAppInSleepMode;
        public bool IsAppInSleepMode
        {
            get
            {
                return isAppInSleepMode;
            }
            set
            {
                isAppInSleepMode = value;
                RaisePropertyChanged(() => IsAppInSleepMode);
            }
        }
            
    }
}