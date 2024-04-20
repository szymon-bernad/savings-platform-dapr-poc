param(
 [string]$ContainerRegName,
 [switch]$DoAzLogin,
 [switch]$SkipApi,
 [switch]$SkipEventStore,
 [switch]$SkipProxy,
 [switch]$DeployToAzEnv)
 

function GetContainerVer {
	param ([string]$vercmd)
	
	$version = (Invoke-Expression $vercmd | Out-String) -split '\r\n' | Sort-Object -Descending | Select-Object -First 1
	$version = [Convert]::ToInt32($version.Substring($version.LastIndexOf('.') + 1))
	$version
}

if ($DoAzLogin)
{
	az login
	$acrLoginCmd = ('az acr login -n {0}' -f $ContainerRegName)
	Invoke-Expression $acrLoginCmd
}

if (!$SkipApi)
{
	
	$apiVerCmd = 'docker images savings-platform-poc-api --format "{{.Tag}}"'
	$verNo = GetContainerVer($apiVerCmd)

	Write-Output "Api Image Version: " $verNo
	
	Push-Location -Path ..\SavingsPlatform.Api\
	
	$path = Get-Location
	
	$path = $path.ToString().Replace('\','\\')
	$buildcmd = ('docker build -t "savings-platform-poc-api:0.{0}" -f ./Dockerfile ..' -f (++$verNo))
	
	Invoke-Expression $buildcmd
	$targetImg = ('{0}.azurecr.io/savings-platform-poc-api:0.{1}' -f $ContainerRegName, $verNo)
	$tagcmd = ('docker tag savings-platform-poc-api:0.{0} {1}' -f $verNo, $targetImg)
	Invoke-Expression $tagcmd
	$pushcmd = ('docker push {0}' -f $targetImg)
	Invoke-Expression $pushcmd
	
	$Env:API_IMGVER = ('0.{0}' -f $verNo)
	Pop-Location
}

if (!$SkipEventStore)
{
	Write-Output $verNo
	$esVerCmd = 'docker images savings-platform-poc-eventstore --format "{{.Tag}}"'
	$verNo = GetContainerVer($apiVerCmd)

	Write-Output "EventStore Image Version: " $verNo
	
	Push-Location -Path ..\SavingsPlatform.EventStore\
	
	$path = Get-Location
	
	$path = $path.ToString().Replace('\','\\')
	$buildcmd = ('docker build -t "savings-platform-poc-eventstore:0.{0}" -f ./Dockerfile ..' -f (++$verNo))
	
	Invoke-Expression $buildcmd
	$targetImg = ('{0}.azurecr.io/savings-platform-poc-eventstore:0.{1}' -f $ContainerRegName, $verNo)
	$tagcmd = ('docker tag savings-platform-poc-eventstore:0.{0} {1}' -f $verNo, $targetImg)
	Invoke-Expression $tagcmd
	$pushcmd = ('docker push {0}' -f $targetImg)
	Invoke-Expression $pushcmd
	
	$Env:EVT_IMGVER = ('0.{0}' -f $verNo)
	Pop-Location
}

if (!$SkipProxy)
{
	Write-Output $verNo
	$esVerCmd = 'docker images savings-platform-poc-paymentproxy --format "{{.Tag}}"'
	$verNo = GetContainerVer($apiVerCmd)

	Write-Output "EventStore Image Version: " $verNo
	
	Push-Location -Path ..\SavingsPlatform.PaymentProxy\
	
	$path = Get-Location
	
	$path = $path.ToString().Replace('\','\\')
	$buildcmd = ('docker build -t "savings-platform-poc-paymentproxy:0.{0}" -f ./Dockerfile ..' -f (++$verNo))
	
	Invoke-Expression $buildcmd
	$targetImg = ('{0}.azurecr.io/savings-platform-poc-paymentproxy:0.{1}' -f $ContainerRegName, $verNo)
	$tagcmd = ('docker tag savings-platform-poc-paymentproxy:0.{0} {1}' -f $verNo, $targetImg)
	Invoke-Expression $tagcmd
	$pushcmd = ('docker push {0}' -f $targetImg)
	Invoke-Expression $pushcmd
	
	$Env:PAY_IMGVER = ('0.{0}' -f $verNo)
	Pop-Location
}

if ($DeployToAzEnv)
{
	$rgExists = (az group exists -n savings-platform-poc-rg)
	Write-Output $rgExists
	if ($rgExists -eq 'false')
	{
		Write-Output 'Creating resource-group...'
		az group create -n savings-platform-poc-rg --location westeurope
	}
	
	do {
		$rgExists = (az group exists -n savings-platform-poc-rg)
		Start-Sleep -Seconds 1
	}
	while ($rgExists -eq 'false')
	
	az deployment group create --name savings-platform-deploy2 --resource-group savings-platform-poc-rg --template-file main.bicep --parameters main.params.bicepparam
}
