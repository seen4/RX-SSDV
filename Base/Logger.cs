using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace RX_SSDV.Base
{
    public class Logger
    {
        private static Logger instance;
        public static Logger Instance
        {
            get
            {
                if (instance == null)
                    instance = new Logger();
                return instance;
            }
        }

        public TextBox logDisplay;

        private StringBuilder logTextBuilder;
        public string logText;

        public Logger()
        {
            logTextBuilder = new StringBuilder();
        }

        public Logger(TextBox logDisplay)
        {
            this.logDisplay = logDisplay;
            logTextBuilder = new StringBuilder();
        }

        public static void Log(string message)
        {
            Instance.logTextBuilder.Append(message);
            UpdateLogText();
        }

        public static void LogInfo(string message)
        {
            Log($"[I] {message}\n");
        }

        public static void LogWarn(string message)
        {
            Log($"[W] {message}\n");
        }

        public static void LogErr(string message)
        {
            Log($"[E] {message}\n");
        }

        public static void CLog(string message)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                Log(message);
            });
        }

        public static void CLogInfo(string message)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                LogInfo(message);
            });
        }

        public static void CLogWarn(string message)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                LogWarn(message);
            });
        }

        public static void CLogErr(string message)
        {
            MainWindow.Instance.Dispatcher.Invoke(() =>
            {
                LogErr(message);
            });
        }

        public static void UpdateLogText()
        {
            if (Instance.logDisplay == null)
                return;

            Instance.logText = Instance.logTextBuilder.ToString();
            Instance.logDisplay.Text = Instance.logText;
        }

        public static void ClearLog()
        {
            Instance.logTextBuilder.Clear();
            UpdateLogText();
        }

        public static void PrintArr(IList arrInput, int arrSize, string msg)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;

            LogInfo(msg);
            for (int i = 0; i < arrSize; i++)
            {
                sb.Append(arrInput[i]);
                count++;

                if (count >= 32)
                {
                    count = 0;
                    LogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            if (count != 0)
            {
                LogInfo(sb.ToString());
                count = 0;
            }
            sb.Clear();
        }

        public static void CPrintArr(IList arrInput, int arrSize, string msg)
        {
            StringBuilder sb = new StringBuilder();
            int count = 0;

            CLogInfo(msg);
            for (int i = 0; i < arrSize; i++)
            {
                sb.Append(arrInput[i]);
                count++;

                if (count >= 32)
                {
                    count = 0;
                    CLogInfo(sb.ToString());
                    sb.Clear();
                }
            }
            if(count != 0)
            {
                CLogInfo(sb.ToString());
                count = 0;
            }
            sb.Clear();
        }
    }
}
