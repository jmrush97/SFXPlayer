using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;

namespace SFXPlayer.classes
{
    public class WebApp
    {
        public static ushort wsPort = 3030;
        private static IHost _host = null;
        private static CancellationTokenSource _cancellationTokenSource = null;
        private static readonly SemaphoreSlim _startStopLock = new SemaphoreSlim(1, 1);
        private static Task _hostRunTask = null;
        private static IHubContext<SfxHub> _hubContext = null;
        
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
                                services.AddSignalR();
                            })
                            .Configure(app =>
                            {
                                app.UseRouting();
                                
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

                                app.UseEndpoints(endpoints =>
                                {
                                    endpoints.MapHub<SfxHub>("/sfxhub");
                                });
                            });
                    });
                
                _host = builder.Build();
                
                // Grab the hub context before starting so broadcast can reach connected clients
                _hubContext = _host.Services.GetRequiredService<IHubContext<SfxHub>>();
                
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
                
                // Stop the host — SignalR handles disconnecting clients gracefully
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
                    _hubContext = null;
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

        // Last display-state message; exposed so SfxHub can send it to newly connected clients
        public static byte[] LastMessage = null;

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

            if (LastMessage == null || _hubContext == null) return;

            var xmlString = Encoding.UTF8.GetString(LastMessage);
            try
            {
                await _hubContext.Clients.All.SendAsync("ReceiveUpdate", xmlString);
            }
            catch (Exception ex)
            {
                AppLogger.Error("WebApp: error broadcasting to SignalR clients", ex);
                Debug.WriteLine($"Error sending to SignalR clients: {ex}");
            }
        }
    }
}
