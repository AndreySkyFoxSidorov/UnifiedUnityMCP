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

Write-Host "--- Testing unity_asset_meta ---"

# Let's create an asset first to test metadata on
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_create"; arguments = @{ action = "material"; path = "Assets/MetaTestMaterial.mat" } }

# 1. Dump properties of the material's importer
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_meta"; arguments = @{ action = "dump"; path = "Assets/MetaTestMaterial.mat" } }

# 2. Get asset bundle name
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_meta"; arguments = @{ action = "get"; path = "Assets/MetaTestMaterial.mat"; property = "m_AssetBundleName" } }

# 3. Set asset bundle name
Send-Rpc -Method "tools/call" -Params @{ name = "unity_asset_meta"; arguments = @{ action = "set"; path = "Assets/MetaTestMaterial.mat"; property = "m_AssetBundleName"; value = "testbundle" } }
