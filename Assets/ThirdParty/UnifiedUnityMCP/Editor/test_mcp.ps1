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
            Write-Host "[$Method] ERROR: $($response.error.message)"
        }
        else {
            $preview = ($response.result | ConvertTo-Json -Compress)
            if ($preview.Length -gt 100) { $preview = $preview.Substring(0, 100) + "..." }
            Write-Host "[$Method] SUCCESS: $preview"
        }
    }
    catch {
        Write-Host "[$Method] HTTP EXCEPTION: $_"
    }
}

Send-Rpc -Method "tools/call" -Params @{ name = "unity_ping"; arguments = @{} }
Send-Rpc -Method "tools/call" -Params @{ name = "unity_editor_state"; arguments = @{} }
Send-Rpc -Method "tools/call" -Params @{ name = "unity_gameobject_manage"; arguments = @{ action = "create"; name = "McpTestObj123"; primitiveType = "Cube" } }
Send-Rpc -Method "tools/call" -Params @{ name = "unity_gameobject_manage"; arguments = @{ action = "find"; name = "McpTestObj123" } }
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_manage"; arguments = @{ action = "find"; filter = "t:Prefab" } }
Send-Rpc -Method "tools/call" -Params @{ name = "unity_console_read"; arguments = @{ maxLines = 1 } }
