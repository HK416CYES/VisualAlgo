param(
    [Parameter(Mandatory = $true)]
    [string]$CommandJson,

    [int[]]$Ports = @(6400, 6401)
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

function Test-UnityPort {
    param(
        [int]$Port
    )

    $client = [System.Net.Sockets.TcpClient]::new()
    try {
        $client.Connect("127.0.0.1", $Port)
        $stream = $client.GetStream()
        $stream.ReadTimeout = 1500
        $buffer = New-Object byte[] 64
        $count = $stream.Read($buffer, 0, $buffer.Length)
        if ($count -gt 0) {
            $text = [System.Text.Encoding]::UTF8.GetString($buffer, 0, $count)
            if ($text -like "WELCOME UNITY-MCP*") {
                return $client
            }
        }
    } catch {
        $client.Dispose()
        return $null
    }

    $client.Dispose()
    return $null
}

function Write-BigEndianLength {
    param(
        [ulong]$Length
    )

    $header = New-Object byte[] 8
    $header[0] = [byte](($Length -shr 56) -band 0xFF)
    $header[1] = [byte](($Length -shr 48) -band 0xFF)
    $header[2] = [byte](($Length -shr 40) -band 0xFF)
    $header[3] = [byte](($Length -shr 32) -band 0xFF)
    $header[4] = [byte](($Length -shr 24) -band 0xFF)
    $header[5] = [byte](($Length -shr 16) -band 0xFF)
    $header[6] = [byte](($Length -shr 8) -band 0xFF)
    $header[7] = [byte]($Length -band 0xFF)
    return $header
}

function Read-Exact {
    param(
        [System.IO.Stream]$Stream,
        [int]$Length
    )

    $buffer = New-Object byte[] $Length
    $offset = 0
    while ($offset -lt $Length) {
        $read = $Stream.Read($buffer, $offset, $Length - $offset)
        if ($read -le 0) {
            throw "Socket closed before reading expected bytes."
        }

        $offset += $read
    }

    return $buffer
}

$client = $null
foreach ($port in $Ports) {
    $client = Test-UnityPort -Port $port
    if ($client -ne $null) {
        break
    }
}

if ($client -eq $null) {
    throw "No active Unity MCP endpoint found on ports: $($Ports -join ', ')."
}

try {
    $stream = $client.GetStream()
    $stream.ReadTimeout = 30000
    $stream.WriteTimeout = 30000

    $payload = [System.Text.Encoding]::UTF8.GetBytes($CommandJson)
    $header = Write-BigEndianLength -Length ([ulong]$payload.Length)
    $stream.Write($header, 0, $header.Length)
    $stream.Write($payload, 0, $payload.Length)
    $stream.Flush()

    $responseHeader = Read-Exact -Stream $stream -Length 8
    [ulong]$responseLength = 0
    for ($i = 0; $i -lt 8; $i++) {
        $responseLength = ($responseLength -shl 8) -bor [ulong]$responseHeader[$i]
    }

    if ($responseLength -le 0 -or $responseLength -gt 67108864) {
        throw "Invalid response length: $responseLength"
    }

    $responseBody = Read-Exact -Stream $stream -Length ([int]$responseLength)
    [System.Text.Encoding]::UTF8.GetString($responseBody)
} finally {
    if ($client -ne $null) {
        $client.Dispose()
    }
}
