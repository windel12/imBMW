using System.ComponentModel;
using OnBoardMonitorEmulator.DevicesEmulation;

namespace OnBoardMonitorEmulator
{
    public class ViewModel : INotifyPropertyChanged
    {
        public ViewModel()
        {
            Consumption1 = (float)9.9;
            Consumption2 = (float)12.1;
            //VolumioReadiness = true;
        }

        public float Consumption1
        {
            get { return InstrumentClusterElectronicsEmulator.Consumption1; }
            set
            {
                if (InstrumentClusterElectronicsEmulator.Consumption1 != value)
                {
                    InstrumentClusterElectronicsEmulator.Consumption1 = value;
                    OnPropertyChanged(nameof(Consumption1));
                }
            }
        }

        public float Consumption2
        {
            get { return InstrumentClusterElectronicsEmulator.Consumption2; }
            set
            {
                if (InstrumentClusterElectronicsEmulator.Consumption2 != value)
                {
                    InstrumentClusterElectronicsEmulator.Consumption2 = value;
                    OnPropertyChanged(nameof(Consumption2));
                }
            }
        }

        private bool _volumioReadiness;
        public bool VolumioReadiness
        {
            get { return _volumioReadiness; }
            set
            {
                _volumioReadiness = value;
                OnPropertyChanged(nameof(VolumioReadiness));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
