using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RX_SSDV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static MainWindow instance;
        public static MainWindow Instance
        {
            get
            {
                return instance;
            }
        }

        public CanvasGraphicDrawer spectrumDrawer;
        public CanvasGraphicDrawer constellationDrawer;

        public Action onSizeChange;

        private MainDSP mainDSP;

        public MainWindow()
        {
            instance = this;
            SizeChanged += OnWindowsSizeChange;

            InitializeComponent();
            Init();
        }

        private void Init()
        {
            Settings.ApplySettings(); //init language
            InitDrawer();
            InitDSP();
        }

        private void InitDSP()
        {
            mainDSP = new MainDSP(spectrumDrawer, constellationDrawer);
        }

        private void InitDrawer()
        {
            spectrumDrawer = new CanvasGraphicDrawer(590, 387, spectrum, spectrumDisplay);
            constellationDrawer = new CanvasGraphicDrawer(100, 100, constellation, constellationDisplay);
        }


        private void OnWindowsSizeChange(object sender, SizeChangedEventArgs arg)
        {
            onSizeChange();
        }

        private void dataSourceType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (dataSourceType.SelectedIndex)
            {
                case 0:
                    SampleSource.sourceType = SampleSource.DataSourceType.BasebandFile;
                    break;
                case 1:
                    SampleSource.sourceType = SampleSource.DataSourceType.RTLSDR;
                    break;
            }
        }

        private void audioPathInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            SampleSource.audioFilePath = audioPathInput.Text;
            if(!SampleSource.audioPathEdited)
                SampleSource.audioPathEdited = true;
        }

        private void browseAudioFileBtn_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFile = new OpenFileDialog();
            openFile.Filter = "Wav文件|*.wav|任意文件|*.";
            if(openFile.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SampleSource.audioFilePath = audioPathInput.Text;
                audioPathInput.Text = openFile.FileName;
                if (!SampleSource.audioPathEdited)
                    SampleSource.audioPathEdited = true;
            }
        }

        private void playAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Play();
        }

        private void pauseAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Pause();
        }

        private void stopAudioBtn_Click(object sender, RoutedEventArgs e)
        {
            SampleSource.Stop();
        }

        private void bandWidthInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int bandwidth = 1;
            if(int.TryParse(bandWidthInput.Text, out bandwidth))
            {
                if (bandwidth < 1)
                    bandwidth = 1;

                bandWidthInput.Text = $"{bandwidth}";
                if (mainDSP != null)
                {
                    mainDSP.bandwidth = bandwidth;
                    mainDSP.UpdateFilter();
                }
            }
            else
            {
                bandWidthInput.Text = "0";
            }
        }

        private void freqShiftInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int freqShift = 0;
            if (int.TryParse(freqShiftInput.Text, out freqShift))
            {
                freqShiftInput.Text = $"{freqShift}";
                if (mainDSP != null)
                {
                    mainDSP.frequencyShift = freqShift;
                    mainDSP.UpdateFilter();
                }
            }
            else
            {
                freqShiftInput.Text = "0";
            }
        }

        private void enableFilterBox_Click(object sender, RoutedEventArgs e)
        {
            mainDSP.EnableFilter = (bool)enableFilterBox.IsChecked;
        }

        private void enableProcessorBox_Click(object sender, RoutedEventArgs e)
        {
            mainDSP.EnableProcess = (bool)enableProcessorBox.IsChecked;
        }

        private void spectrumPeriodInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int period = mainDSP.spectrumPeriod;
            if (int.TryParse(spectrumPeriodInput.Text, out period))
            {
                spectrumPeriodInput.Text = $"{period}";
                if (mainDSP != null)
                {
                    mainDSP.spectrumPeriod = period;
                }
            }
            else
            {
                spectrumPeriodInput.Text = mainDSP.spectrumPeriod.ToString();
            }
        }

        //private void applyFilterBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (mainDSP != null)
        //        mainDSP.UpdateFilter();
        //}
    }
}