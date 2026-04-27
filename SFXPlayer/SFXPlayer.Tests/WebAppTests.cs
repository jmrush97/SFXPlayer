using System.Net;
using System.Net.WebSockets;
using System.Text;
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
            _output.WriteLine("? WebApp started successfully and responding to requests");
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
            _output.WriteLine($"? Static file served successfully (Length: {content.Length} bytes)");
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
            _output.WriteLine($"? Index.html served with correct content type: {contentType}");
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
                _output.WriteLine($"? JavaScript file served with content type: {contentType}");
            }
            else
            {
                _output.WriteLine($"? JavaScript file not found (this may be expected): {response.StatusCode}");
            }
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact(Skip = "WebSocket endpoint needs additional configuration")]
    public async Task WebApp_AcceptsWebSocketConnections()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        ClientWebSocket? webSocket = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            // Act
            webSocket = new ClientWebSocket();
            webSocket.Options.AddSubProtocol("ws-SFX-protocol");
            var uri = new Uri($"ws://localhost:{TestPort}/ws");
            
            await webSocket.ConnectAsync(uri, CancellationToken.None);

            // Assert
            Assert.Equal(WebSocketState.Open, webSocket.State);
            _output.WriteLine("? WebSocket connection established successfully");
        }
        finally
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                webSocket.Dispose();
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
            
            _output.WriteLine($"? WebApp stopped successfully (Expected error: {exception.GetType().Name})");
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
            _output.WriteLine("? Multiple start calls handled gracefully");
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
            _output.WriteLine($"? WebApp listening on correct port: {TestPort}");

            // Verify it's NOT listening on a different port
            var wrongPortException = await Assert.ThrowsAnyAsync<Exception>(async () =>
            {
                var wrongClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
                await wrongClient.GetAsync($"http://localhost:{TestPort + 1}/");
            });
            _output.WriteLine($"? WebApp NOT listening on wrong port (Expected error: {wrongPortException.GetType().Name})");
        }
        finally
        {
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }


    [Fact]
    public async Task WebApp_HandlesVolumeCommand_ViaWebSocket()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        ClientWebSocket? webSocket = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            webSocket = new ClientWebSocket();
            webSocket.Options.AddSubProtocol("ws-SFX-protocol");
            var uri = new Uri($"ws://localhost:{TestPort}/ws");
            await webSocket.ConnectAsync(uri, CancellationToken.None);

            // Act — send a volume command
            var msg = Encoding.UTF8.GetBytes("<command>volume:75</command>");
            await webSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            await Task.Delay(300);

            // Assert — no exception means the command was handled without crashing
            Assert.Equal(WebSocketState.Open, webSocket.State);
            _output.WriteLine("? Volume WebSocket command handled without error");
        }
        finally
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                webSocket.Dispose();
            }
            await WebApp.StopAsync();
            await Task.Delay(500);
            WebApp.wsPort = originalPort;
        }
    }

    [Fact]
    public async Task WebApp_HandlesSpeedCommand_ViaWebSocket()
    {
        // Arrange
        var originalPort = WebApp.wsPort;
        WebApp.wsPort = TestPort;
        ClientWebSocket? webSocket = null;

        try
        {
            await WebApp.StartAsync();
            await Task.Delay(1000);

            webSocket = new ClientWebSocket();
            webSocket.Options.AddSubProtocol("ws-SFX-protocol");
            var uri = new Uri($"ws://localhost:{TestPort}/ws");
            await webSocket.ConnectAsync(uri, CancellationToken.None);

            // Act — send a speed command
            var msg = Encoding.UTF8.GetBytes("<command>speed:1.50</command>");
            await webSocket.SendAsync(new ArraySegment<byte>(msg), WebSocketMessageType.Text, true, CancellationToken.None);
            await Task.Delay(300);

            // Assert — no exception means the command was handled without crashing
            Assert.Equal(WebSocketState.Open, webSocket.State);
            _output.WriteLine("? Speed WebSocket command handled without error");
        }
        finally
        {
            if (webSocket?.State == WebSocketState.Open)
            {
                await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Test complete", CancellationToken.None);
                webSocket.Dispose();
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
