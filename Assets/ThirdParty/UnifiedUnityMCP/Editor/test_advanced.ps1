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

Write-Host "--- Testing Advanced MCP Tools ---"

# 1. Properties 
# First find directional light
Send-Rpc -Method "tools/call" -Params @{ name = "unity_gameobject_manage"; arguments = @{ action = "find"; name = "Directional Light" } }
# Assume it found the light, we want to test property tool - we will test getting 'transform' instance ID later or just reading from time? We can't guarantee ID. Let's test reading Time.time (not a component, but still). Oh wait, property tool only works on Components. We can't reliably test this without a known ID. Let's skip automatic component property test.

# 2. Scene
Send-Rpc -Method "tools/call" -Params @{ name = "unity_scene_manage"; arguments = @{ action = "list_build_scenes" } }

# 3. Asset
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_create"; arguments = @{ action = "folder"; path = "Assets/AdvancedToolTest" } }

# 4. Menu
Send-Rpc -Method "tools/call" -Params @{ name = "unity_editor_execute_menu"; arguments = @{ menuPath = "Edit/Play" } }

# 5. Build
Send-Rpc -Method "tools/call" -Params @{ name = "unity_build_manage"; arguments = @{ action = "get_defines" } }
