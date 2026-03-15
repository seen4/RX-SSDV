using RX_SSDV.Base;
using RX_SSDV.CCSDS;
using RX_SSDV.DSP;
using RX_SSDV.Graphic;
using RX_SSDV.IO;
using RX_SSDV.Test;
using RX_SSDV.Utils;
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
using Application = System.Windows.Forms.Application;

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
        public CanvasGraphicDrawer constellationDrawerProcessed;

        public Action onSizeChange;

        private MainDSP mainDSP;

        public MainWindow()
        {
            instance = this;
            SizeChanged += OnWindowsSizeChange;

            LogLogo();
            Logger.Log("[I] Initializing components...");
            InitializeComponent();
            Logger.Log(" done\n");
            Logger.Instance.logDisplay = logText;

            Init();

            //ViterbiTest test = new ViterbiTest();
            //test.Test();
            //DelayTest.Test();
            //MDecoderTest.Test();
            //DeframerTest.Test();
            //CCSDS_Test.Test();
        }

        private void Init()
        {
            Logger.Log("[I] Starting up...");
            Settings.ApplySettings(); //init language

            InitDrawer();
            InitDSP();
            Logger.Log(" done\n");
        }

        private void InitDSP()
        {
            mainDSP = new MainDSP(spectrumDrawer, constellationDrawer, constellationDrawerProcessed);
            SetDSPArguments();
        }

        private void InitDrawer()
        {
            spectrumDrawer = new CanvasGraphicDrawer(590, 387, spectrum, spectrumDisplay);
            constellationDrawer = new CanvasGraphicDrawer(100, 100, oriCon, oriConDisplay);
            constellationDrawerProcessed = new CanvasGraphicDrawer(100, 100, constellation, constellationDisplay);
        }

        private void SetDSPArguments()
        {
            bandWidthInput.Text = mainDSP.bandwidth.ToString();
            freqShiftInput.Text = mainDSP.frequencyShift.ToString();
            drawerPeriodInput.Text = mainDSP.spectrumPeriod.ToString();
            constellationScaleBox.Text = mainDSP.ConstellationMultiply.ToString();
        }

        private void LogLogo()
        {
            Logger.Log(@"  ____   __  __          ____    ____    ____   __     __" + "\n" +
                       @" |  _ \  \ \/ /         / ___|  / ___|  |  _ \  \ \   / /" + "\n" +
                       @" | |_) |  \  /   _____  \___ \  \___ \  | | | |  \ \ / / " + "\n" +
                       @" |  _ <   /  \  |_____|  ___) |  ___) | | |_| |   \ V /  " + "\n" +
                       @" |_| \_\ /_/\_\         |____/  |____/  |____/     \_/   " + "\n" +
                       @"                                  " + "\n");
            Logger.Log($"RX-SSDV ver. {Application.ProductVersion}\n");
            Logger.Log($"By AstarLC(BI2QXZ), Polygone_(BG5JSB)\n\n");
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

        private void drawerPeriodInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int period = mainDSP.spectrumPeriod;
            if (int.TryParse(drawerPeriodInput.Text, out period))
            {
                drawerPeriodInput.Text = $"{period}";
                if (mainDSP != null)
                {
                    mainDSP.spectrumPeriod = period;
                }
            }
            else
            {
                drawerPeriodInput.Text = mainDSP.spectrumPeriod.ToString();
            }
        }

        private void constellationScaleBox_LostFocus(object sender, RoutedEventArgs e)
        {
            float consScale = mainDSP.ConstellationMultiply;
            if (float.TryParse(constellationScaleBox.Text, out consScale))
            {
                constellationScaleBox.Text = $"{consScale}";
                if (mainDSP != null)
                {
                    mainDSP.ConstellationMultiply = consScale;
                }
            }
            else
            {
                constellationScaleBox.Text = mainDSP.ConstellationMultiply.ToString();
            }
        }

        private void sampleReadPeriodInput_LostFocus(object sender, RoutedEventArgs e)
        {
            int period = SampleSource.readPeriod;
            if (int.TryParse(sampleReadPeriodInput.Text, out period))
            {
                period = Math.Abs(period);
                sampleReadPeriodInput.Text = $"{period}";
                SampleSource.readPeriod = period;
            }
            else
            {
                constellationScaleBox.Text = SampleSource.readPeriod.ToString();
            }
        }

        private void freqShiftBox_LostFocus(object sender, RoutedEventArgs e)
        {
            float freqShift = mainDSP.bpskDemod.freqShift.Freq;
            if (float.TryParse(freqShiftBox.Text, out freqShift))
            {
                freqShiftBox.Text = $"{freqShift}";
                mainDSP.bpskDemod.freqShift.Freq = freqShift;
            }
            else
            {
                freqShiftBox.Text = mainDSP.bpskDemod.freqShift.Freq.ToString();
            }
        }

        //private void applyFilterBtn_Click(object sender, RoutedEventArgs e)
        //{
        //    if (mainDSP != null)
        //        mainDSP.UpdateFilter();
        //}
    }
}