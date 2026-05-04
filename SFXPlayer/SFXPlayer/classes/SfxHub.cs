using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace SFXPlayer.classes
{
    public class SfxHub : Hub
    {
        /// <summary>
        /// Thread-safe set of connected client IP addresses (RemoteIpAddress or Connection id).
        /// </summary>
        public static readonly ConcurrentDictionary<string, string> ConnectedClientIPs
            = new ConcurrentDictionary<string, string>();

        /// <summary>Raised when the connected client count changes.</summary>
        public static event EventHandler ConnectedClientsChanged;

        public override async Task OnConnectedAsync()
        {
            // Record the client's IP address
            var ip = Context.GetHttpContext()?.Connection?.RemoteIpAddress;
            string ipStr = ip != null ? ip.MapToIPv4().ToString() : "unknown";
            ConnectedClientIPs[Context.ConnectionId] = ipStr;
            ConnectedClientsChanged?.Invoke(null, EventArgs.Empty);

            // Send the current display state to the newly connected client
            var lastMessage = WebApp.LastMessage;
            if (lastMessage != null)
            {
                await Clients.Caller.SendAsync("ReceiveUpdate",
                    System.Text.Encoding.UTF8.GetString(lastMessage));
            }
            await base.OnConnectedAsync();
        }

        public override Task OnDisconnectedAsync(Exception exception)
        {
            ConnectedClientIPs.TryRemove(Context.ConnectionId, out _);
            ConnectedClientsChanged?.Invoke(null, EventArgs.Empty);
            return base.OnDisconnectedAsync(exception);
        }

        public void SendCommand(string rawCommand)
        {
            if (Program.mainForm == null) return;

            string command = rawCommand.ToLower();
            switch (command)
            {
                case "play":
                    Program.mainForm.PlayNextCue();
                    break;
                case "stop":
                    Program.mainForm.StopAll();
                    break;
                case "previous":
                    Program.mainForm.PreviousCue();
                    break;
                case "next":
                    Program.mainForm.NextCue();
                    break;
                case "delete":
                    Program.mainForm.DeleteNextCue();
                    break;
                case "togglepause":
                    Program.mainForm.TogglePause();
                    break;
                case "autorun:true":
                    Program.mainForm.SetNextCueAutoRun(true);
                    break;
                case "autorun:false":
                    Program.mainForm.SetNextCueAutoRun(false);
                    break;
                default:
                    if (command.StartsWith("volume:") &&
                        int.TryParse(command.Substring(7), out int vol))
                    {
                        Program.mainForm.SetNextCueVolume(vol);
                    }
                    else if (command.StartsWith("speed:") &&
                        float.TryParse(command.Substring(6),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out float spd))
                    {
                        Program.mainForm.SetNextCueSpeed(spd);
                    }
                    else if (command.StartsWith("pause:") &&
                        double.TryParse(command.Substring(6),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double pauseSecs))
                    {
                        Program.mainForm.SetNextCuePauseSeconds(pauseSecs);
                    }
                    else if (command.StartsWith("fadein:") &&
                        int.TryParse(command.Substring(7), out int fadeInMs))
                    {
                        Program.mainForm.SetNextCueFadeIn(fadeInMs);
                    }
                    else if (command.StartsWith("fadeout:") &&
                        int.TryParse(command.Substring(8), out int fadeOutMs))
                    {
                        Program.mainForm.SetNextCueFadeOut(fadeOutMs);
                    }
                    else if (command.StartsWith("fadecurve:"))
                    {
                        Program.mainForm.SetNextCueFadeCurve(command.Substring(10));
                    }
                    else if (command.StartsWith("device:"))
                    {
                        // Use rawCommand to preserve original case of device name
                        Program.mainForm.SetPlaybackDevice(rawCommand.Substring(7));
                    }
                    else if (command.StartsWith("previewdevice:"))
                    {
                        // Use rawCommand to preserve original case of device name
                        Program.mainForm.SetPreviewDevice(rawCommand.Substring(14));
                    }
                    else if (command.StartsWith("seek:") &&
                        double.TryParse(rawCommand.Substring(5),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture,
                            out double seekFraction))
                    {
                        Program.mainForm.SeekPosition(seekFraction);
                    }
                    else if (command.StartsWith("goto:") &&
                        int.TryParse(command.Substring(5), out int gotoIndex))
                    {
                        Program.mainForm.GotoCue(gotoIndex);
                    }
                    else if (rawCommand.StartsWith("description:", StringComparison.OrdinalIgnoreCase))
                    {
                        Program.mainForm.SetShowDescription(rawCommand.Substring(12));
                    }
                    else if (command.StartsWith("instantplay:") &&
                        int.TryParse(command.Substring(12), out int instantIdx))
                    {
                        Program.mainForm.InstantPlayCue(instantIdx);
                    }
                    break;
            }
        }
    }
}
