[Cmdletbinding()]
Param(
	[Parameter(Mandatory = $true, ParameterSetName = "BlockDatesStart")]
	[Parameter(Mandatory = $true, ParameterSetName = "BlockDatesEnd")]
	[Parameter(Mandatory = $true, ParameterSetName = "SelectedDates")]
	[Alias("c")][string]$ConfigurationFilePath = "configuration.json",
	
	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Parameter(ParameterSetName = "SelectedDates")]
	[Alias("r")][switch]$RemoveExistingDates,

	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Alias("wk")][switch]$IncludeWeekends,
	[Parameter(Mandatory = $true, ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Alias("start")][System.DateTime]$StartDate = [System.DateTime]::Today,
	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(Mandatory = $true, ParameterSetName = "BlockDatesEnd")]
	[Alias("end")][System.DateTime]$EndDate =  [System.DateTime]::Today.AddMonths(6),

	[Parameter(ParameterSetName = "SelectedDates")]
	[Alias("d")][string[]]$Dates,
	[Parameter(ParameterSetName = "SelectedDates")]
	[Alias("time", "t")][string]$DailyTime =  "15:00:00",
	[Parameter(ParameterSetName = "SelectedDates")]
	[Alias("e")][string]$ExportedConfigurationFilePath = $null,

	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Parameter(ParameterSetName = "SelectedDates")]
	[string]$InputDateFormat = "dd MMM yyyy",
	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Parameter(ParameterSetName = "SelectedDates")]
	[string]$OutputDateFormat = "dd MMM yyyy",
	[Parameter(ParameterSetName = "BlockDatesStart")]
	[Parameter(ParameterSetName = "BlockDatesEnd")]
	[Parameter(ParameterSetName = "SelectedDates")]
	[string]$OutputTimeFormat = "HH:mm:ss"
)

#Load Configuration
if (-not (Test-Path $ConfigurationFilePath)) { Write-Host "Configuration '$($ConfigurationFilePath)' does not exist." -Fore Yellow; exit; }
$global:c = Get-Content -Raw -Path $ConfigurationFilePath -ErrorAction Stop | ConvertFrom-Json

if ($global:c -and $global:c.SessionDates) {
	if ($RemoveExistingDates -eq $true) {
		$global:c.SessionDates.Sessions = @()
	}

	#using StartDate and EndDate
	<#
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
	#>

	#using Dates
	[System.DateTime[]]$dDates = @()
	if ($Dates) {
		$Dates | ForEach {
			$RunningDate = [System.DateTime]::ParseExact($_, "$($InputDateFormat)", (New-Object System.Globalization.CultureInfo("en-US")))
			$TimespanInfo = [System.TimeSpan]::Parse($DailyTime)
			$RunningDate = $RunningDate.Add($TimespanInfo)

			$dDates += $RunningDate
		}
	}

	$dDates = ($dDates | Sort-Object)

	if ($Dates) {
		$dDates | ForEach {
			$global:c.SessionDates.Sessions += [PSCustomObject]@{
				Date = $_.ToString("$($OutputDateFormat) $($OutputTimeFormat)")
				Enabled = $true
			}
		}
	}
}

#export
if ($ExportedConfigurationFilePath) {
	$global:c | ConvertTo-Json -Depth 20 -Compress | Set-Content -Path $ExportedConfigurationFilePath -Force
}
elseif ($ConfigurationFilePath) {
	$global:c | ConvertTo-Json -Depth 20 -Compress | Set-Content -Path $ConfigurationFilePath -Force
}

#Final Remove of the global Variable
Remove-Variable c -Scope Global
