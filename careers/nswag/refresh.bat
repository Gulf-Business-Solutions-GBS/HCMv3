@echo off
REM Regenerates the careers portal's NSwag service proxies.
REM
REM Prerequisite: HCMv3.Web.Host must be RUNNING (https://localhost:44301 by default).
REM
REM Step 1 downloads the full OpenAPI document and reduces it to the three careers services, so the public
REM portal bundle never carries the shape of the internal admin API. Step 2 runs NSwag over that reduced
REM document. See filter-swagger.mjs for the reasoning.
REM
REM This is the careers portal's OWN NSwag pipeline. It does not read, write or share anything with the
REM admin SPA's ..\..\angular\nswag configuration.

setlocal

if "%SWAGGER_URL%"=="" set SWAGGER_URL=https://localhost:44301/swagger/v1/swagger.json

echo [1/2] Filtering OpenAPI document from %SWAGGER_URL% ...
node "%~dp0filter-swagger.mjs" "%SWAGGER_URL%" "%~dp0careers.swagger.json"
if errorlevel 1 goto :error

echo [2/2] Generating TypeScript service proxies ...
call "%~dp0..\node_modules\.bin\nswag" run "%~dp0service.config.nswag"
if errorlevel 1 goto :error

echo Done.
exit /b 0

:error
echo.
echo FAILED. Is HCMv3.Web.Host running at %SWAGGER_URL% ?
exit /b 1
