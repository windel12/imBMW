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
        }

        public float Consumption1
        {
            get { return InstrumentClusterElectronicsEmulator.Consumption1; }
            set
            {
                if (InstrumentClusterElectronicsEmulator.Consumption1 != value)
                {
                    InstrumentClusterElectronicsEmulator.Consumption1 = value;
                    OnPropertyChange("Consumption1");
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
                    OnPropertyChange("Consumption2");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChange(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
