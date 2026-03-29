using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RX_SSDV.Utils
{
    public class ColorFade
    {
        //tnx to deepseek

        public static Color GetColorHSV(double amplitude, double minDB, double maxDB)
        {
            double t = (amplitude - minDB) / (maxDB - minDB);
            t = Math.Clamp(t, 0.0, 1.0);

            // Hue: 从 240 (蓝) 递减到 0 (红)
            double hue = 240.0 * (1.0 - t);
            // 饱和度 = 1.0, 亮度 = 1.0 (全彩)
            return HsvToRgb(hue, 1.0, 1.0);
        }

        // HSV 转 RGB 辅助函数
        private static Color HsvToRgb(double hue, double sat, double val)
        {
            // 标准算法，hue: 0-360, sat/val: 0-1
            double c = val * sat;
            double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = val - c;
            double rp = 0, gp = 0, bp = 0;
            if (hue < 60) { rp = c; gp = x; bp = 0; }
            else if (hue < 120) { rp = x; gp = c; bp = 0; }
            else if (hue < 180) { rp = 0; gp = c; bp = x; }
            else if (hue < 240) { rp = 0; gp = x; bp = c; }
            else if (hue < 300) { rp = x; gp = 0; bp = c; }
            else { rp = c; gp = 0; bp = x; }
            return Color.FromArgb(255, (int)((rp + m) * 255), (int)((gp + m) * 255), (int)((bp + m) * 255));
        }
    }
}
