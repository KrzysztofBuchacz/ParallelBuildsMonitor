using System.ComponentModel;


namespace ParallelBuildsMonitor
{
    /// <summary>
    /// Provide interaction DataModel with UI.
    /// </summary>
    // [ImplementPropertyChanged] comes from https://www.nuget.org/packages/PropertyChanged.Fody/
    // It is implementing NotifyPropertyChanged for all public class properties,
    // so they can be easy bind to UI, without "PropertyChanged()" in each property.
    // [ImplementPropertyChanged]
    public class ViewModel : System.ComponentModel.INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged = (sender, e) => { };

        #region Creator and Constructors
        private ViewModel()
        {
        }

        private static ViewModel instance = null;
        // Singleton
        public static ViewModel Instance
        {
            get
            {
                if (instance == null)
                    instance = new ViewModel();
                return instance;
            }
        }
        #endregion


        private bool isGraphDrawn = false;
        public bool IsGraphDrawn
        {
            get
            {
                return isGraphDrawn;
            }
            set
            {
                if (isGraphDrawn == value)
                    return;

                isGraphDrawn = value;
                PropertyChanged(this, new System.ComponentModel.PropertyChangedEventArgs(nameof(IsGraphDrawn)));
            }
        }
    }
}
