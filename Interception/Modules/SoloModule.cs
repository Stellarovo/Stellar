using StellarWatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

using WindivertDotnet;

namespace StellarWatch.Interception.Modules
{
    public class SoloModule : PacketModuleBase
    {
        public SoloModule() : base("Solo", true, InterceptionManager.GetProvider("Players"))
        {
            Icon = System.Windows.Application.Current.FindResource("LonelyIcon") as Geometry;
            Description = "使用端口[443]阻止匹配队友";
        }

        public override void Toggle()
        {
            IsActivated = !IsActivated;
        }

        public override bool AllowPacket(Packet packet)
        {
            if (!base.AllowPacket(packet)) return false;

            return !IsActivated;
        }
    }
}
