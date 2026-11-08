# Author: Tahir Ismail - ST10471483
# CoffeeNChill Part 3 - automatic end-to-end test of the API Gateway.
# Runs the same checks as the manual Swagger test plan and prints PASS / FAIL for each.
#
# Usage (from the repository root, with "docker compose up -d" running):
#   powershell -ExecutionPolicy Bypass -File .\test-gateway.ps1
#   powershell -ExecutionPolicy Bypass -File .\test-gateway.ps1 -BaseUrl http://40.127.11.155:5000

param(
    [string]$BaseUrl = "http://localhost:5000"
)

Add-Type -AssemblyName System.Net.Http
$client = New-Object System.Net.Http.HttpClient
$client.Timeout = [TimeSpan]::FromSeconds(60)

$script:passed = 0
$script:failed = 0

# Sends one HTTP request and returns the status code and response body.
function Send-Request {
    param(
        [string]$Method,
        [string]$Path,
        $Body = $null,
        [string]$Token = $null,
        $Content = $null
    )

    $httpMethod = New-Object System.Net.Http.HttpMethod -ArgumentList $Method
    $request = New-Object System.Net.Http.HttpRequestMessage -ArgumentList $httpMethod, ($BaseUrl + $Path)

    if ($Token) {
        $request.Headers.Authorization = New-Object System.Net.Http.Headers.AuthenticationHeaderValue -ArgumentList "Bearer", $Token
    }

    if ($Content) {
        $request.Content = $Content
    }
    elseif ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 5 -Compress
        $request.Content = New-Object System.Net.Http.StringContent -ArgumentList $json, ([System.Text.Encoding]::UTF8), "application/json"
    }

    try {
        $response = $client.SendAsync($request).GetAwaiter().GetResult()
        $text = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
        return @{ Code = [int]$response.StatusCode; Body = $text }
    }
    catch {
        return @{ Code = 0; Body = $_.Exception.InnerException.Message }
    }
}

# Compares the actual status code with the expected one(s) and prints the result.
function Check {
    param([string]$Name, $Result, [int[]]$Expected)

    if ($Expected -contains $Result.Code) {
        Write-Host ("PASS  {0,-58} {1}" -f $Name, $Result.Code) -ForegroundColor Green
        $script:passed++
    }
    else {
        Write-Host ("FAIL  {0,-58} got {1}, expected {2}" -f $Name, $Result.Code, ($Expected -join "/")) -ForegroundColor Red
        $snippet = "$($Result.Body)"
        if ($snippet.Length -gt 400) { $snippet = $snippet.Substring(0, 400) + "..." }
        Write-Host "      Response: $snippet" -ForegroundColor DarkYellow
        $script:failed++
    }
}

# Unique values for every run, so the script can be run again without duplicate errors.
$run = Get-Date -Format "HHmmss"
$sku1 = "TST-{0:D3}" -f (Get-Random -Minimum 100 -Maximum 999)
$sku2 = "TSP-{0:D3}" -f (Get-Random -Minimum 100 -Maximum 999)
$password = "Coffee@2026"

$staffUser = @{
    name = "Tahir"; surname = "Ismail"
    email = "staff$run@coffeenchill.co.za"; username = "staff$run"
    password = $password; confirmPassword = $password; role = "Staff"
}
$customerUser = @{
    name = "Neha"; surname = "Heeralal"
    email = "customer$run@coffeenchill.co.za"; username = "customer$run"
    password = $password; confirmPassword = $password; role = "Customer"
}
$menuItem1 = @{
    category = "Coffee"; sku = $sku1; name = "Test Cappuccino"
    description = "Created by the automatic test"; price = 32.50; isAvailable = $true
}
$menuItem2 = @{
    category = "Pastry"; sku = $sku2; name = "Test Croissant"
    description = "Created by the automatic test"; price = 28.00; isAvailable = $true
}

Write-Host ""
Write-Host "Testing $BaseUrl" -ForegroundColor Cyan
Write-Host ""

Write-Host "--- A: No token ---" -ForegroundColor Cyan
Check "A0 Swagger JSON loads"                  (Send-Request GET "/swagger/v1/swagger.json") @(200)
Check "A1 GET /api/menu (public)"              (Send-Request GET "/api/menu") @(200)
Check "A2 POST /api/menu without token"        (Send-Request POST "/api/menu" $menuItem1) @(401)
Check "A3 GET /api/orders without token"       (Send-Request GET "/api/orders") @(401)
Check "A4 GET /api/documents without token"    (Send-Request GET "/api/documents") @(401)

Write-Host "--- B: Register and log in ---" -ForegroundColor Cyan
Check "B1 Register Staff user"                 (Send-Request POST "/api/auth/register" $staffUser) @(200, 201)
Check "B2 Register Customer user"              (Send-Request POST "/api/auth/register" $customerUser) @(200, 201)
Check "B3 Register duplicate Staff user"       (Send-Request POST "/api/auth/register" $staffUser) @(409)
$weak = $customerUser.Clone(); $weak.email = "weak$run@coffeenchill.co.za"; $weak.username = "weak$run"
$weak.password = "coffee"; $weak.confirmPassword = "coffee"
Check "B4 Register with weak password"         (Send-Request POST "/api/auth/register" $weak) @(400)
Check "B5 Login with wrong password"           (Send-Request POST "/api/auth/login" @{ usernameOrEmail = $staffUser.email; password = "WrongPass@1" }) @(401)

$customerLogin = Send-Request POST "/api/auth/login" @{ usernameOrEmail = $customerUser.email; password = $password }
Check "B6 Login as Customer"                   $customerLogin @(200)
$staffLogin = Send-Request POST "/api/auth/login" @{ usernameOrEmail = $staffUser.email; password = $password }
Check "B7 Login as Staff"                      $staffLogin @(200)

$customerToken = $null; $staffToken = $null
try { $customerToken = ($customerLogin.Body | ConvertFrom-Json).token } catch {}
try { $staffToken = ($staffLogin.Body | ConvertFrom-Json).token } catch {}
if (-not $staffToken -or -not $customerToken) {
    Write-Host "Could not read the tokens from the login responses - stopping here." -ForegroundColor Red
    Write-Host "Staff login response: $($staffLogin.Body)"
    exit 1
}

Write-Host "--- C: Customer token (wrong role) ---" -ForegroundColor Cyan
Check "C1 GET /api/menu as Customer"           (Send-Request GET "/api/menu" -Token $customerToken) @(200)
Check "C2 POST /api/menu as Customer"          (Send-Request POST "/api/menu" $menuItem1 $customerToken) @(403)
Check "C3 GET /api/orders as Customer"         (Send-Request GET "/api/orders" -Token $customerToken) @(403)
Check "C4 GET /api/documents as Customer"      (Send-Request GET "/api/documents" -Token $customerToken) @(403)

Write-Host "--- D: Staff token (menu) ---" -ForegroundColor Cyan
Check "D1 POST /api/menu ($sku1)"              (Send-Request POST "/api/menu" $menuItem1 $staffToken) @(201)
Check "D2 POST /api/menu ($sku2)"              (Send-Request POST "/api/menu" $menuItem2 $staffToken) @(201)
$badSku = $menuItem1.Clone(); $badSku.sku = "coffee1"
Check "D3 POST /api/menu with invalid SKU"     (Send-Request POST "/api/menu" $badSku $staffToken) @(400)
Check "D4 GET /api/menu"                       (Send-Request GET "/api/menu" -Token $staffToken) @(200)
Check "D5 GET /api/menu/Coffee/$sku1"          (Send-Request GET "/api/menu/Coffee/$sku1" -Token $staffToken) @(200)
Check "D6 GET /api/menu/category/Pastry"       (Send-Request GET "/api/menu/category/Pastry" -Token $staffToken) @(200)
$updated = $menuItem1.Clone(); $updated.price = 35.00
Check "D7 PUT /api/menu/Coffee/$sku1"          (Send-Request PUT "/api/menu/Coffee/$sku1" $updated $staffToken) @(200)

Write-Host "--- E: Orders ---" -ForegroundColor Cyan
$order = @{
    orderId = "ORD-TEST-$run"; customerName = "Neha Heeralal"
    selectedItemSKUs = @($sku1, $sku2); totalPrice = 63.00
    orderTimestamp = (Get-Date).ToString("yyyy-MM-ddTHH:mm:sszzz")
}
$queued = Send-Request POST "/api/orders/queue" $order
Check "E1 POST /api/orders/queue (no token)"   $queued @(202)
$orderId = $null
try { $orderId = ($queued.Body | ConvertFrom-Json).orderId } catch {}

if ($orderId) {
    Write-Host "      Order ID from server: $orderId - waiting 5 seconds for the queue trigger..."
    Start-Sleep -Seconds 5
    Check "E2 GET /api/orders"                 (Send-Request GET "/api/orders" -Token $staffToken) @(200)
    Check "E3 GET /api/orders/$orderId"        (Send-Request GET "/api/orders/$orderId" -Token $staffToken) @(200)
    $advance = @{ orderId = $orderId }
    Check "E4 Advance to Preparing"            (Send-Request PUT "/api/orders/status" $advance $staffToken) @(200)
    Check "E5a Advance to Ready"               (Send-Request PUT "/api/orders/status" $advance $staffToken) @(200)
    Check "E5b Advance to Collected"           (Send-Request PUT "/api/orders/status" $advance $staffToken) @(200)
    Check "E6 Advance past Collected"          (Send-Request PUT "/api/orders/status" $advance $staffToken) @(400)
    Check "E7 PUT /api/orders/{id}/status"     (Send-Request PUT "/api/orders/$orderId/status" @{ status = "Preparing" } $staffToken) @(200)
    Check "E7b Advance status as Customer"     (Send-Request PUT "/api/orders/status" $advance $customerToken) @(403)
}
else {
    Write-Host "      No orderId in the E1 response - skipping E2 to E7." -ForegroundColor DarkYellow
}
$badOrder = $order.Clone(); $badOrder.selectedItemSKUs = @("XXX-999")
Check "E8 Queue order with unknown SKU"        (Send-Request POST "/api/orders/queue" $badOrder) @(400)

Write-Host "--- F: Documents (Staff) ---" -ForegroundColor Cyan
$pdfPath = Join-Path $PSScriptRoot "docs\postman\Test Dcoument\Test File.pdf"
if (Test-Path $pdfPath) {
    $multipart = New-Object System.Net.Http.MultipartFormDataContent
    $fileContent = New-Object System.Net.Http.ByteArrayContent -ArgumentList (, [System.IO.File]::ReadAllBytes($pdfPath))
    $fileContent.Headers.ContentType = New-Object System.Net.Http.Headers.MediaTypeHeaderValue -ArgumentList "application/pdf"
    $multipart.Add($fileContent, "file", "Test File.pdf")
    Check "F1 POST /api/documents/upload"      (Send-Request POST "/api/documents/upload" -Token $staffToken -Content $multipart) @(200, 201)
}
else {
    Write-Host "      Test PDF not found at $pdfPath - skipping F1." -ForegroundColor DarkYellow
}
$docs = Send-Request GET "/api/documents" -Token $staffToken
Check "F2 GET /api/documents"                  $docs @(200)
Check "F3 GET /api/documents/download/..."     (Send-Request GET ("/api/documents/download/" + [Uri]::EscapeDataString("Test File.pdf")) -Token $staffToken) @(200)

Write-Host "--- G: Clean-up ---" -ForegroundColor Cyan
Check "G1 DELETE /api/menu/Pastry/$sku2"       (Send-Request DELETE "/api/menu/Pastry/$sku2" -Token $staffToken) @(204)
Check "G2 GET deleted item"                    (Send-Request GET "/api/menu/Pastry/$sku2") @(404)
Check "G3 DELETE /api/menu/Coffee/$sku1"       (Send-Request DELETE "/api/menu/Coffee/$sku1" -Token $staffToken) @(204)

Write-Host ""
$colour = if ($script:failed -eq 0) { "Green" } else { "Yellow" }
Write-Host ("Done: {0} passed, {1} failed" -f $script:passed, $script:failed) -ForegroundColor $colour
