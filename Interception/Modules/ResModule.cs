using StellarWatch.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;

namespace StellarWatch.Interception.Modules
{
    public class ResModule : PacketModuleBase
    {
        PacketProviderBase provider;
        public ResModule() : base("Revive", false, InterceptionManager.GetProvider("Players"))
        {
            Description =
@"一币多拉

勾上后可在 突袭 竞速 首日等活动
无限消耗玩家右下角的单个复活币

勾上后等待3秒 可使用单币拉所有人
不生效就是没等待 10秒后自动解勾
*宗师 割喉壮举无法生效

端口[27k]";
            provider = InterceptionManager.GetProvider("Players");
        }

        public override void Toggle()
        {
            IsActivated = !IsActivated;

            if (IsActivated)
            {
                StartTime = DateTime.Now;
                disable = null;
                blacklist.Clear();
                highest = DateTime.MinValue;
                cts = new CancellationTokenSource();
            }
            else
            {
                cts.Cancel();
                blacklist.Clear();
            }
        }

        Task disable;
        CancellationTokenSource cts;
        TimeSpan delay = TimeSpan.FromSeconds(2.5);
        DateTime highest = DateTime.MinValue;
        HashSet<string> blacklist = new ();
        public override bool AllowPacket(Packet p)
        {
            if (!base.AllowPacket(p)) return false;

            if (!IsActivated) return true;

            var r = p.RemoteAddress;
            var black = blacklist.Contains(r);
            var any = blacklist.Any();

            // long time no revive
            if (DateTime.Now - StartTime > TimeSpan.FromSeconds(10) && !any)
            {
                ForceDisable();
                Logger.Debug($"{Name}: Disabled after timeout");
                return true;
            }

            if (p.Outbound) return true;// return !black;

            if (p.Length >= 1215 && p.Length <= 1225)
            {
                if (DateTime.Now > highest) highest = DateTime.Now;

                if (!black)
                {
                    blacklist.Add(r);
                    provider.Delay.RemoveAll(x => x.RemoteAddress == r);
                    Logger.Debug($"{Name}: Found at {r}");

                    disable ??= Task.Run(async () =>
                    {
                        Logger.Debug($"{Name}: Self disable timer start");

                        while (!cts.IsCancellationRequested)
                        {
                            var del = (highest + delay) - DateTime.Now;
                            if (del > TimeSpan.Zero) // delay passed since last revive
                            {
                                await Task.Delay(del, cts.Token);
                            }
                            else
                            {
                                ForceDisable();
                                Logger.Debug($"{Name}: Disabled with successful reconnect");
                            }
                        }
                    });
                }
                else Logger.Debug($"{Name}: Repeat at {r}");
                return false;
            }

            if (black || provider.Connections[r].IsReadonlyPlayerConnection()) return false;

            provider.DelayPacket(p, TimeSpan.FromSeconds(1));
            return false;
        }
    }
}