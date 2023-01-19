# 清理
if (Test-Path "./Release") { Remove-Item "./Release" -Recurse -Force } 
if (Test-Path "./**.zip") { Remove-Item "./**.zip" -Recurse -Force } 

# 发布客户端（依赖框架模式、64位）
dotnet publish "./Demo/Demo.csproj" -c Release -o "./Release" -r win-x64 --sc false -p:PublishSingleFile=true
$version = (Get-Item "./Release/Demo.exe").VersionInfo.FileVersion
Compress-Archive -Path "./Release/**" -DestinationPath ("./" + $version + ".zip")

# 清理
if (Test-Path "./Release") { Remove-Item "./Release" -Recurse -Force } 

# 打开所在文件夹
Invoke-Item ("./" + $version + ".zip")