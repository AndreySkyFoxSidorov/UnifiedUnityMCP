$Endpoint = "http://127.0.0.1:18008/mcp"

function Send-Rpc {
    param(
        [string]$Method,
        [hashtable]$Params = @{}
    )
    $payload = @{
        jsonrpc = "2.0"
        id      = 1
        method  = $Method
        params  = $Params
    }
    
    $json = $payload | ConvertTo-Json -Depth 5 -Compress
    
    try {
        $response = Invoke-RestMethod -Uri $Endpoint -Method Post -Body $json -ContentType "application/json"
        
        if ($response.error) {
            Write-Host "[$Method] ERROR: $($response.error.message)" -ForegroundColor Red
        }
        else {
            $preview = ($response.result | ConvertTo-Json -Compress)
            if ($preview.Length -gt 150) { $preview = $preview.Substring(0, 150) + "..." }
            Write-Host "[$Method] SUCCESS: $preview" -ForegroundColor Green
        }
    }
    catch {
        Write-Host "[$Method] HTTP EXCEPTION: $_" -ForegroundColor Red
    }
}

Write-Host "--- Stopping Server ---"
## To force a restart we can just execute menu to restart or use a tool. Wait, we can't stop the server via API easily unless we use execute_menu_item if there was one. We will just tell the user to click the toolbar button to refresh it, or we can see if it automatically reloaded. Let's just run tools.

Send-Rpc -Method "tools/list"
