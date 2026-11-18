using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StellarWatch.Interception.Modules
{
    public class TimerModule : PacketModuleBase
    {
        public TimerModule() : base("Timer", true, null)
        {
            Icon = System.Windows.Application.Current.FindResource("Clock") as Geometry;
            Description = "计时器 不使用任何端口❤";
        }

        public override void Toggle()
        {
            IsActivated = !IsActivated;
        }
    }
}
