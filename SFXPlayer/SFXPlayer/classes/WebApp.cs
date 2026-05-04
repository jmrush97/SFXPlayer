using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace SFXPlayer.classes
{
    public class WebApp
    {
        const string SFXProtocol = "ws-SFX-protocol";
        public static ushort wsPort = 3030;
        private static IHost _host = null;
        private static CancellationTokenSource _cancellationTokenSource = null;
        private static readonly SemaphoreSlim _startStopLock = new SemaphoreSlim(1, 1);
        private static Task _hostRunTask = null;
        
        public static async Task StartAsync()
        {
            AppLogger.Info("WebApp.StartAsync: starting");
            await _startStopLock.WaitAsync();
            try
            {
                // Idempotent: if already started, return immediately
                if (_host != null && Serving)
                {
                    AppLogger.Info("WebApp.StartAsync: already running, skipping start");
                    Debug.WriteLine("WebApp already running, skipping start");
                    return;
                }
                
                // Clean up any previous failed state
                if (_host != null)
                {
                    try
                    {
                        await _host.StopAsync(TimeSpan.FromSeconds(1));
                        _host.Dispose();
                    }
                    catch { }
                    _host = null;
                }
                
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = new CancellationTokenSource();
                
                var builder = Host.CreateDefaultBuilder()
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder
                            .UseKestrel(options =>
                            {
                                options.ListenAnyIP(wsPort);
                            })
                            .ConfigureServices(services =>
                            {
                                // Add required services
                            })
                            .Configure(app =>
                            {
                                // Enable WebSocket support BEFORE static files
                                app.UseWebSockets(new WebSocketOptions
                                {
                                    KeepAliveInterval = TimeSpan.FromSeconds(120)
                                });
                                
                                // Handle WebSocket requests BEFORE static files
                                app.Use(async (context, next) =>
                                {
                                    if (context.WebSockets.IsWebSocketRequest)
                                    {
                                        await ProcessWebSocketRequestAsync(context);
                                    }
                                    else
                                    {
                                        await next();
                                    }
                                });
                                
                                // Serve default files (index.html, default.html, etc.)
                                app.UseDefaultFiles(new DefaultFilesOptions
                                {
                                    FileProvider = new PhysicalFileProvider(
                                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html")),
                                    RequestPath = ""
                                });
                                
                                // Serve static files (JS, CSS, images, etc.)
                                app.UseStaticFiles(new StaticFileOptions
                                {
                                    FileProvider = new PhysicalFileProvider(
                                        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "html")),
                                    RequestPath = ""
                                });
                            });
                    });
                
                _host = builder.Build();
                
                // Start the host asynchronously on a background thread
                _hostRunTask = Task.Run(async () =>
                {
                    try
                    {
                        await _host.RunAsync(_cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when stopping
                        AppLogger.Info("WebApp host cancelled normally");
                        Debug.WriteLine("WebApp host cancelled normally");
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("WebApp host encountered an error", ex);
                        Debug.WriteLine($"WebApp host error: {ex}");
                    }
                });
                
                // Wait for server to actually start listening
                await WaitForServerReady(TimeSpan.FromSeconds(5));
                
                if (Program.mainForm != null)
                {
                    Program.mainForm.DisplayChanged += UpdateWebAppsWithDisplayChangeAsync;
                }
                
                AppLogger.Info($"WebApp.StartAsync: started successfully on port {wsPort}");
                Debug.WriteLine($"WebApp started successfully on port {wsPort}");
            }
            catch (Exception ex)
            {
                AppLogger.Error("WebApp.StartAsync: failed to start", ex);
                Debug.WriteLine($"Failed to start WebApp: {ex}");
                
                // Clean up on failure
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                
                if (_host != null)
                {
                    try
                    {
                        await _host.StopAsync(TimeSpan.FromSeconds(1));
                        _host.Dispose();
                    }
                    catch { }
                    _host = null;
                }
                
                throw;
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        public static async Task StopAsync()
        {
            AppLogger.Info("WebApp.StopAsync: stopping");
            await _startStopLock.WaitAsync();
            try
            {
                if (_host == null)
                {
                    AppLogger.Info("WebApp.StopAsync: already stopped, skipping");
                    Debug.WriteLine("WebApp already stopped, skipping stop");
                    return;
                }
                
                AppLogger.Info("WebApp.StopAsync: shutting down host");
                Debug.WriteLine("Stopping WebApp...");
                
                if (Program.mainForm != null)
                {
                    Program.mainForm.DisplayChanged -= UpdateWebAppsWithDisplayChangeAsync;
                }
                
                // Close all WebSocket connections
                var socketsCopy = webSockets.ToList();
                foreach (var ws in socketsCopy)
                {
                    try
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "SFX Player closed", CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("WebApp.StopAsync: error closing WebSocket", ex);
                        Debug.WriteLine($"Error closing WebSocket: {ex}");
                    }
                }
                
                lock (webSockets)
                {
                    webSockets.Clear();
                }
                
                // Stop the host
                _cancellationTokenSource?.Cancel();
                
                try
                {
                    await _host.StopAsync(TimeSpan.FromSeconds(5));
                    
                    // Wait for the host run task to complete
                    if (_hostRunTask != null)
                    {
                        await Task.WhenAny(_hostRunTask, Task.Delay(TimeSpan.FromSeconds(5)));
                    }
                    
                    _host.Dispose();
                }
                catch (Exception ex)
                {
                    AppLogger.Error("WebApp.StopAsync: error stopping host", ex);
                    Debug.WriteLine($"Error stopping WebApp: {ex}");
                }
                finally
                {
                    _host = null;
                    _hostRunTask = null;
                    _cancellationTokenSource?.Dispose();
                    _cancellationTokenSource = null;
                }
                
                AppLogger.Info("WebApp.StopAsync: stopped successfully");
                Debug.WriteLine("WebApp stopped successfully");
            }
            finally
            {
                _startStopLock.Release();
            }
        }

        private static async Task WaitForServerReady(TimeSpan timeout)
        {
            var stopwatch = Stopwatch.StartNew();
            
            while (stopwatch.Elapsed < timeout)
            {
                try
                {
                    // Try to connect to the server
                    using var client = new System.Net.Sockets.TcpClient();
                    await client.ConnectAsync("127.0.0.1", wsPort);
                    
                    Debug.WriteLine($"WebApp server ready after {stopwatch.ElapsedMilliseconds}ms");
                    return;
                }
                catch
                {
                    // Server not ready yet, wait a bit
                    await Task.Delay(50);
                }
            }
            
            Debug.WriteLine($"WebApp server may not be ready after {timeout.TotalSeconds}s timeout");
        }

        private static readonly List<WebSocket> webSockets = new List<WebSocket>();
        private static byte[] LastMessage = null;
        private static readonly SemaphoreSlim _sendLock = new SemaphoreSlim(1, 1);

        public static bool Serving
        {
            get
            {
                if (_host == null) return false;
                
                try
                {
                    // Try to connect to verify server is actually listening
                    using var client = new System.Net.Sockets.TcpClient();
                    var connectTask = client.ConnectAsync("127.0.0.1", wsPort);
                    
                    if (connectTask.Wait(100))
                    {
                        return true;
                    }
                }
                catch
                {
                    // Server not responding
                }
                
                return false;
            }
        }

        private static async Task ProcessWebSocketRequestAsync(HttpContext context)
        {
            WebSocket ws = null;
            try
            {
                ws = await context.WebSockets.AcceptWebSocketAsync(SFXProtocol);
                AppLogger.Info($"WebApp: WebSocket connection accepted from {context.Connection.RemoteIpAddress}");
                
                if (LastMessage != null)
                {
                    await ws.SendAsync(new ArraySegment<byte>(LastMessage, 0, LastMessage.Length), 
                        WebSocketMessageType.Text, true, CancellationToken.None);
                }

                lock (webSockets)
                {
                    webSockets.Add(ws);
                }

                byte[] receiveBuffer = new byte[1024];
                while (ws.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult receiveResult = await ws.ReceiveAsync(
                        new ArraySegment<byte>(receiveBuffer), CancellationToken.None);
                    
                    if (receiveResult.MessageType == WebSocketMessageType.Close)
                    {
                        lock (webSockets)
                        {
                            webSockets.Remove(ws);
                        }
                        // Guard against the rare case where the remote end has already
                        // fully closed by the time we get here (state flips from
                        // CloseReceived to Closed), which would throw a WebSocketException.
                        if (ws.State == WebSocketState.Open || ws.State == WebSocketState.CloseReceived)
                        {
                            await ws.CloseAsync(WebSocketCloseStatus.NormalClosure,
                                "Close requested by remote", CancellationToken.None);
                        }
                    }
                    else
                    {
                        string strXML = Encoding.UTF8.GetString(receiveBuffer, 0, receiveResult.Count);
                        XmlDocument xml = new XmlDocument();
                        xml.LoadXml(strXML);
                        var nodes = xml.SelectNodes("command");
                        foreach (XmlNode childrenNode in nodes)
                        {
                            string rawCommand = childrenNode.InnerText;
                            string command = rawCommand.ToLower();
                            if (Program.mainForm != null)
                            {
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
                                        break;
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                AppLogger.Error("WebApp.ProcessWebSocketRequestAsync: WebSocket error", e);
                Debug.WriteLine($"WebSocket error: {e}");
            }
            finally
            {
                if (ws != null)
                {
                    lock (webSockets)
                    {
                        webSockets.Remove(ws);
                    }
                    ws.Dispose();
                }
            }
        }



        private static async void UpdateWebAppsWithDisplayChangeAsync(object sender, DisplaySettings e)
        {
            if (e != null)
            {
                LastMessage = Encoding.UTF8.GetBytes(e.SerializeToXmlString());
            }
            else
            {
                LastMessage = null;
            }

            if (LastMessage == null) return;

            // Serialize all broadcast attempts so concurrent timer ticks never
            // call SendAsync on the same socket simultaneously.
            await _sendLock.WaitAsync();
            try
            {
                var payload = (byte[])LastMessage?.Clone();   // true snapshot: safe against concurrent reassignment
                var socketsCopy = webSockets.ToList();
                foreach (WebSocket ws in socketsCopy)
                {
                    try
                    {
                        if (ws.State == WebSocketState.Open)
                        {
                            await ws.SendAsync(new ArraySegment<byte>(payload, 0, payload.Length),
                                WebSocketMessageType.Text, true, CancellationToken.None);
                        }
                    }
                    catch (Exception ex)
                    {
                        AppLogger.Error("WebApp: error broadcasting to WebSocket client", ex);
                        Debug.WriteLine($"Error sending to WebSocket: {ex}");

                        lock (webSockets)
                        {
                            webSockets.Remove(ws);
                        }
                    }
                }
            }
            finally
            {
                _sendLock.Release();
            }
        }
    }
}

