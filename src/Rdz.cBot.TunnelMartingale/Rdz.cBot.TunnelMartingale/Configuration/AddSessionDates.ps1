[Cmdletbinding()]
Param(
	[Parameter(Mandatory = $false, Position = 0)][Alias("c")][string]$ConfigurationFilePath = "configuration.json",
	[Parameter(Mandatory = $false, Position = 2)][switch]$RemoveExistingDates,
	[Parameter(Mandatory = $false, Position = 3)][switch]$IncludeWeekends,
	[Parameter(Mandatory = $false, Position = 4)][Alias("start")][System.DateTime]$StartDate = [System.DateTime]::Today,
	[Parameter(Mandatory = $false, Position = 5)][Alias("end")][System.DateTime]$EndDate =  [System.DateTime]::Today.AddMonths(6),
	[Parameter(Mandatory = $false, Position = 6)][Alias("time")][string]$DailyTime =  "15:00:00",
	[Parameter(Mandatory = $false, Position = 1)][Alias("e")][string]$ExportedConfigurationFilePath = "configuration-export.json"
)

#Load Configuration
if (-not (Test-Path $ConfigurationFilePath)) { Write-Host "Configuration '$($ConfigurationFilePath)' does not exist." -Fore Yellow; exit; }
$global:c = Get-Content -Raw -Path $ConfigurationFilePath -ErrorAction Stop | ConvertFrom-Json

if ($global:c -and $global:c.SessionDates -and $global:c.SessionDates.Sessions) {
	if ($RemoveExistingDates -eq $true) {
		$global:c.SessionDates.Sessions = @()
	}

	$RunningDate = $StartDate
	$TimespanInfo = [System.TimeSpan]::Parse($DailyTime)
	$RunningDate = $RunningDate.Add($TimespanInfo)

	while ($RunningDate -le $EndDate) {
		if (($IncludeWeekends -eq $false -and -not("Saturday|Sunday".Contains($RunningDate.DayOfWeek.ToString()))) -or ($IncludeWeekends -eq $true)) {
			$global:c.SessionDates.Sessions += [PSCustomObject]@{
				Date = $RunningDate.ToString("dd MMM yyyy HH:mm:ss")
				Enabled = $true
			}
		}

		$RunningDate = $RunningDate.AddDays(1)
	}
}

#export
if ($ExportedConfigurationFilePath) {
	$global:c | ConvertTo-Json -Depth 20 -Compress | Set-Content -Path $ExportedConfigurationFilePath
}

#Final Remove of the global Variable
Remove-Variable c -Scope Global
