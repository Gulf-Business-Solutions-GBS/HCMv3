# COMMON PATHS
echo "#################### COMMON PATHS ####################"

$buildFolder = (Get-Item -Path "./" -Verbose).FullName
$slnFolder = Join-Path $buildFolder "../"
$outputFolder = Join-Path $buildFolder "outputs"
$webHostFolder = Join-Path $slnFolder "src/HCMv3.Web.Host"
$webPublicFolder = Join-Path $slnFolder "src/HCMv3.Web.Public"
$ngFolder = Join-Path $buildFolder "../../angular"

## CLEAR ######################################################################
echo "#################### CLEAR ####################"

Remove-Item $outputFolder -Force -Recurse -ErrorAction Ignore
New-Item -Path $outputFolder -ItemType Directory

## RESTORE NUGET PACKAGES #####################################################
echo "#################### RESTORE NUGET PACKAGES ####################"

Set-Location $slnFolder
dotnet restore

## PUBLISH WEB HOST PROJECT ###################################################
echo "#################### PUBLISH WEB HOST PROJECT ####################"

Set-Location $webHostFolder
dotnet publish --output (Join-Path $outputFolder "Host") --configuration Release
Copy-Item ("Dockerfile.original") (Join-Path $outputFolder "Host")

## COPY YML AND PFX FILES HOST ##############################################
echo "#################### COPY YML AND PFX FILES HOST ####################"

Set-Location $outputFolder
Copy-Item ("../host/*.yml") (Join-Path $outputFolder "Host")

## PUBLISH WEB PUBLIC PROJECT ###################################################
echo "#################### PUBLISH WEB PUBLIC PROJECT ####################"

Set-Location $webPublicFolder
yarn
yarn run build
dotnet publish --output (Join-Path $outputFolder "Public") --configuration Release
Copy-Item ("Dockerfile.original") (Join-Path $outputFolder "Public")

## COPY YML AND PFX FILES PUBLIC ##############################################
echo "#################### COPY YML AND PFX FILES PUBLIC ####################"
Set-Location $outputFolder
Copy-Item ("../public/*.yml") (Join-Path $outputFolder "Public")

## PUBLISH ANGULAR UI PROJECT #################################################
echo "#################### PUBLISH ANGULAR UI PROJECT ####################"
Set-Location $ngFolder
yarn
ng build --configuration production
$distFolder = Join-Path $ngFolder "dist"
Copy-Item $distFolder (Join-Path $outputFolder "ng/dist") -Recurse -Force
Copy-Item (Join-Path $ngFolder "*") (Join-Path $outputFolder "ng") -Recurse -Force

## COPY YML AND PFX FILES ANGULAR ##############################################
echo "#################### COPY YML AND PFX FILES ANGULAR ####################"
Set-Location $outputFolder
Copy-Item ("../ng/*.*") (Join-Path $outputFolder "ng")

## UPDATE NGINX CONFIG FOR ANGULAR SPA ROUTING ################################
echo "#################### UPDATE NGINX CONFIG FOR ANGULAR SPA ROUTING ####################"
Set-Location $outputFolder
$nginxConfPath = Join-Path $outputFolder "ng/default.conf"
if (Test-Path $nginxConfPath) {
    $content = Get-Content $nginxConfPath -Raw
    $find = "location / {"
    $replace = "location / {`n    try_files `$uri `$uri/ /index.html;"
    if (-not ($content -like "*try_files*")) {
        $newContent = $content -replace [regex]::Escape($find), $replace
        Set-Content -Path $nginxConfPath -Value $newContent
        echo "ng/default.conf has been updated for SPA routing."
    }
    else {
        echo "ng/default.conf already contains try_files. Skipping modification."
    }
} else {
    echo "Warning: ng/default.conf not found at $nginxConfPath. Manual update may be required."
}

## CREATE DOCKER IMAGES #######################################################
echo "#################### CREATE DOCKER IMAGES ####################"

# Mvc
echo "#################### CREATE DOCKER IMAGES (MVC) ####################"
Set-Location (Join-Path $outputFolder "Host")
Remove-Item ("Dockerfile")
Rename-Item -Path "Dockerfile.original" -NewName "Dockerfile"
dotnet dev-certs https -v -ep aspnetzero-devcert-host.pfx -p 2825e4d9-5cef-4373-bed3-d7ebf59de216

docker rmi mycompanynameabpzerotemplatewebhost -f
docker compose -f docker-compose.yml build

# Public
echo "#################### CREATE DOCKER IMAGES (Public) ####################"
Set-Location (Join-Path $outputFolder "Public")
Remove-Item ("Dockerfile")
Rename-Item -Path "Dockerfile.original" -NewName "Dockerfile"
dotnet dev-certs https -v -ep aspnetzero-devcert-public.pfx -p b7ca126d-5085-47a0-8ac3-1b5971bd65a1

docker rmi mycompanynameabpzerotemplatewebpublic -f
docker compose -f docker-compose.yml build

# Angular
echo "#################### CREATE DOCKER IMAGES (Angular) ####################"
Set-Location (Join-Path $outputFolder "ng")

Copy-Item (Join-Path $ngFolder "package.json") (Join-Path $outputFolder "ng")
Copy-Item (Join-Path $ngFolder "yarn.lock") (Join-Path $outputFolder "ng")

docker rmi mycompanynameabpzerotemplatewebangular -f
docker compose -f docker-compose.yml build

## FINALIZE ###################################################################
echo "#################### FINALIZE ####################"
Set-Location $outputFolder