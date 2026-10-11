[CmdletBinding(SupportsShouldProcess)]
param([Parameter(Mandatory)][string]$ReceiverPath,[string]$BonjourPath,[switch]$Remove)
$ErrorActionPreference = 'Stop'
$receiver = [IO.Path]::GetFullPath($ReceiverPath)
if (!$Remove -and !(Test-Path -LiteralPath $receiver -PathType Leaf)) { throw 'Receiver executable does not exist.' }
$group = 'WindowsAirplay local receiver'
$rules = @(
    @{ Name='WindowsAirplay-Receiver-TCP'; Program=$receiver; Protocol='TCP'; Ports='35000-35002' },
    @{ Name='WindowsAirplay-Receiver-UDP'; Program=$receiver; Protocol='UDP'; Ports='35000-35002' }
)
if ($BonjourPath) {
    $bonjour = [IO.Path]::GetFullPath($BonjourPath)
    if (!$Remove -and !(Test-Path -LiteralPath $bonjour -PathType Leaf)) { throw 'Bonjour executable does not exist.' }
    $rules += @{ Name='WindowsAirplay-Bonjour-mDNS'; Program=$bonjour; Protocol='UDP'; Ports='5353' }
}
foreach ($rule in $rules) {
    if ($PSCmdlet.ShouldProcess($rule.Program, "$($rule.Name): $($rule.Protocol) $($rule.Ports), Private profile, LocalSubnet only; remove=$Remove")) {
        $existing = Get-NetFirewallRule -Name $rule.Name -ErrorAction SilentlyContinue
        if ($existing) {
            $app = $existing | Get-NetFirewallApplicationFilter
            if ($existing.Group -ne $group -or $app.Program -ne $rule.Program) { throw "Rule $($rule.Name) belongs to another executable or application. No change made to that rule." }
            Remove-NetFirewallRule -Name $rule.Name
        }
        if (!$Remove) {
            New-NetFirewallRule -Name $rule.Name -DisplayName $rule.Name -Group $group -Direction Inbound -Action Allow -Profile Private -RemoteAddress LocalSubnet -Program $rule.Program -Protocol $rule.Protocol -LocalPort $rule.Ports | Out-Null
        }
    }
}
