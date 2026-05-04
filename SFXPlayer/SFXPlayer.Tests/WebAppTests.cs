using System.Net;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;
using Xunit.Abstractions;
using SFXPlayer.classes;

namespace SFXPlayer.Tests;

public class WebAppTests : IDisposable
{
    private readonly ITestOutputHelper _output;
    private const int TestPort = 3031; // Use different port to avoid conflicts
    private readonly HttpClient _httpClient;

    public WebAppTests(ITestOutputHelper output)
    {
        _output = output;
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    [Fact]
    public async Task WebApp_StartAsync_StartsSuccessfully()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            // Act
            await WebApp.StartAsync();
            await Task.Delay(1000); // Give server more time to start

            // Assert - verify by attempting HTTP connection
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/");
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound, 
                "WebApp should be responding to HTTP requests");
            _output.WriteLine("✓ WebApp started successfully and responding to requests");
        }
        finally
        {
            // Cleanup
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_ServesStaticFiles_Successfully()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Act
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var content = await response.Content.ReadAsStringAsync();
            Assert.NotEmpty(content);
            _output.WriteLine($"✓ Static file served successfully (Length: {content.Length} bytes)");
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact]
    public async Task WebApp_ServesIndexHtml_WhenAccessingRoot()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Act
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.Equal("text/html", contentType);
            _output.WriteLine($"✓ Index.html served with correct content type: {contentType}");
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact]
    public async Task WebApp_ServesJavaScriptFiles_WithCorrectContentType()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Act
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/webapp.js");

            // Assert
            if (response.StatusCode == HttpStatusCode.OK)
            {
                var contentType = response.Content.Headers.ContentType?.MediaType;
                Assert.Contains("javascript", contentType ?? "", StringComparison.OrdinalIgnoreCase);
                _output.WriteLine($"✓ JavaScript file served with content type: {contentType}");
            }
            else
            {
                _output.WriteLine($"ℹ JavaScript file not found (this may be expected): {response.StatusCode}");
            }
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact]
    public async Task WebApp_AcceptsSignalRConnections()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        HubConnection? connection = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Act
            connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:{TestPort}/sfxhub")
                .Build();

            await connection.StartAsync();

            // Assert
            Assert.Equal(HubConnectionState.Connected, connection.State);
            _output.WriteLine("✓ SignalR connection established successfully");
        }
        finally
        {
            if (connection != null)
            {
                await connection.StopAsync();
                await connection.DisposeAsync();
            }
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_StopAsync_StopsServerGracefully()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);
            
            // Verify it's running
            var startResponse = await _httpClient.GetAsync($"http://localhost:{TestPort}/");
            Assert.True(startResponse.StatusCode == HttpStatusCode.OK || startResponse.StatusCode == HttpStatusCode.NotFound);

            // Act
            await WebApp.StopAsync();
            await Task.Delay(1000); // Give server time to stop

            // Assert - Verify server is really stopped
            var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                await client.GetAsync($"http://localhost:{TestPort}/");
            });
            
            _output.WriteLine($"✓ WebApp stopped successfully (Expected error: {exception.GetType().Name})");
        }
        finally
        {
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_HandlesMultipleStartCalls_Gracefully()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            // Act
            await WebApp.StartAsync();
            await Task.Delay(800);
            await WebApp.StartAsync(); // Second call should be ignored
            await Task.Delay(800);

            // Assert - verify by HTTP request
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/");
            Assert.True(response.StatusCode == HttpStatusCode.OK || response.StatusCode == HttpStatusCode.NotFound, 
                "WebApp should still be responding after multiple start calls");
            _output.WriteLine("✓ Multiple start calls handled gracefully");
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_ListensOnCorrectPort()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;

        try
        {
            // Act
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Assert
            var response = await _httpClient.GetAsync($"http://localhost:{TestPort}/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            _output.WriteLine($"✓ WebApp listening on correct port: {TestPort}");

            // Verify it's NOT listening on a different port
            var wrongPortException = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var wrongClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                await wrongClient.GetAsync($"http://localhost:{TestPort + 1}/");
            });
            _output.WriteLine($"✓ WebApp NOT listening on wrong port (Expected error: {wrongPortException.GetType().Name})");
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_HandlesVolumeCommand_ViaSignalR()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        HubConnection? connection = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:{TestPort}/sfxhub")
                .Build();
            await connection.StartAsync();

            // Act — invoke volume command (Program.mainForm is null so it returns early; no crash expected)
            await connection.InvokeAsync("SendCommand", "volume:75");
            await Task.Delay(300);

            // Assert — no exception means the command was handled without crashing
            Assert.Equal(HubConnectionState.Connected, connection.State);
            _output.WriteLine("✓ Volume SignalR command handled without error");
        }
        finally
        {
            if (connection != null)
            {
                await connection.StopAsync();
                await connection.DisposeAsync();
            }
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact]
    public async Task WebApp_HandlesSpeedCommand_ViaSignalR()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        HubConnection? connection = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            connection = new HubConnectionBuilder()
                .WithUrl($"http://localhost:{TestPort}/sfxhub")
                .Build();
            await connection.StartAsync();

            // Act — invoke speed command (Program.mainForm is null so it returns early; no crash expected)
            await connection.InvokeAsync("SendCommand", "speed:1.50");
            await Task.Delay(300);

            // Assert — no exception means the command was handled without crashing
            Assert.Equal(HubConnectionState.Connected, connection.State);
            _output.WriteLine("✓ Speed SignalR command handled without error");
        }
        finally
        {
            if (connection != null)
            {
                await connection.StopAsync();
                await connection.DisposeAsync();
            }
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }
}
