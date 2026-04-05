using RX_SSDV.Decoder;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace RX_SSDV.UI
{
    public class SatDataUI
    {
        public MainWindow main;
        private static SatDataUI instance;
        public static SatDataUI Instance
        {
            get
            {
                return instance; 
            }
        }

        public List<PacketInfo> packets = new List<PacketInfo>();

        public static ImagePacket currentImagePacket;

        public SatDataUI(MainWindow mainWindow)
        {
            main = mainWindow;
            main.satDataList.ItemsSource = packets;

            instance = this;
        }

        public static void RegisterPacket(PacketInfo packet)
        {
            Instance.packets.Add(packet);
            Instance.main.Dispatcher.Invoke(() =>
            {
                Instance.main.satDataList.Items.Refresh();
            });
        }
        public static void ClearPackets()
        {
            Instance.packets.Clear();
            Instance.main.Dispatcher.Invoke(() => {
                Instance.main.satDataList.Items.Refresh();
            });
            HideAllViewer();
        }

        public static void SelectPacket(PacketInfo? packet)
        {
            if(packet == null) return;
            else
            {
                PacketData data = packet.data;
                if(data.packetType == PacketData.PacketType.Image)
                {
                    currentImagePacket = (ImagePacket)data;
                    UpdateImage();
                    Instance.main.Dispatcher.Invoke(() =>
                    {
                        ShowImageViewer();
                    });
                }
                else
                {
                    Instance.main.Dispatcher.Invoke(() =>
                    {
                        HideAllViewer(); //For other viewers
                    });
                }
            }
        }

        public static void UpdateImage()
        {
            Instance.main.Dispatcher.Invoke(() =>
            {
                try
                {
                    BitmapImage targetImage = new BitmapImage(new Uri(currentImagePacket.imagePath, UriKind.Absolute));
                    FileInfo imgFileInfo = new FileInfo(currentImagePacket.imagePath);
                    long fileSize = imgFileInfo.Length;
                    Instance.main.packetImage.Visibility = System.Windows.Visibility.Visible;
                    Instance.main.packetImage.Source = targetImage;
                    Instance.main.packetImageProperty.Content = $"{imgFileInfo.Name} - {targetImage.Format.ToString()} {targetImage.PixelWidth}*{targetImage.PixelHeight} {fileSize / 1024f:F2}KB";
                }
                catch (Exception)
                {
                    Instance.main.packetImage.Visibility = System.Windows.Visibility.Collapsed;
                    Instance.main.packetImageProperty.Content = "The image cannot be loaded, it may be damaged or still decoding.";
                }
            });
        }

        public static void ShowImageViewer()
        {
            HideAllViewer();
            Instance.main.packetImageView.Visibility = System.Windows.Visibility.Visible;
        }

        public static void HideAllViewer()
        {
            Instance.main.packetImageView.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
