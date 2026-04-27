# SFXPlayer WebApp Integration Tests

This test project contains automated integration tests for the SFXPlayer WebApp service.

## Tests Included

### WebApp Service Tests
- **WebApp_StartAsync_StartsSuccessfully**: Verifies the web server starts correctly
- **WebApp_ServesStaticFiles_Successfully**: Confirms static files (HTML, JS, CSS) are served
- **WebApp_ServesIndexHtml_WhenAccessingRoot**: Validates the default page is served at root URL
- **WebApp_ServesJavaScriptFiles_WithCorrectContentType**: Checks JavaScript files have correct MIME types
- **WebApp_AcceptsWebSocketConnections**: Tests WebSocket connectivity for real-time communication
- **WebApp_StopAsync_StopsServerGracefully**: Ensures clean shutdown of the web service
- **WebApp_HandlesMultipleStartCalls_Gracefully**: Verifies idempotent start behavior
- **WebApp_ListensOnCorrectPort**: Confirms the service listens on the configured port

## Running Tests

### Automatic (Post-Build)
Tests run automatically after each Debug build of the main SFXPlayer project.

### Manual Execution
```powershell
# Run all tests
dotnet test

# Run only WebApp tests
dotnet test --filter "FullyQualifiedName~WebAppTests"

# Run with detailed output
dotnet test --logger "console;verbosity=detailed"

# Run a specific test
dotnet test --filter "WebApp_StartAsync_StartsSuccessfully"
```

## Test Configuration

- **Test Port**: 3031 (different from production port 3030 to avoid conflicts)
- **Timeout**: 10 seconds per HTTP request
- **Framework**: .NET 8.0
- **Test Framework**: xUnit

## Notes

- Tests use a different port (3031) to avoid conflicts with running instances
- Tests clean up resources properly using `IDisposable`
- WebSocket tests verify the custom protocol "ws-SFX-protocol"
- Static file tests validate correct content types and responses

## Troubleshooting

If tests fail:
1. Check that port 3031 is not in use
2. Ensure the `html` folder and files exist in the build output
3. Verify firewall settings allow localhost connections
4. Check test output for specific error messages
