#execute in developer shell of visual studio
$GitVersion_NuGetVersion = "2.24.1"

$pattern1 = '\[assembly: AssemblyVersion\("(.*)"\)\]'
$pattern2 = '\[assembly: AssemblyFileVersion\("(.*)"\)\]'

#AssemblyFileVersion

Get-ChildItem AssemblyInfo.cs -Recurse | % { 

        (Get-Content $_.FullName)| %{ 
                    $_ -replace $pattern1, ('[assembly: AssemblyVersion("{0}")]' -f $GitVersion_NuGetVersion) -replace $pattern2, ('[assembly: AssemblyFileVersion("{0}")]' -f $GitVersion_NuGetVersion)                       
        } | Set-Content $_ }


#if($_ -match $pattern1){
#    #$fileVersion = [version]$matches[1]
#    #$newVersion = "{0}.{1}.{2}.{3}" -f $fileVersion.Major, $fileVersion.Minor, $fileVersion.Build, ($fileVersion.Revision + 1)
#    '[assembly: AssemblyVersion({0})]' -f $GitVersion_NuGetVersion
# } else {
#    # Output line as is
#    $_
# }    
        

##DevShell:
msbuild Source\HelixToolkit.AppVeyor.sln /property:Platform="Any CPU" /property:Version=$GitVersion_NuGetVersion /verbosity:minimal /p:SourceLinkCreate=true /m /property:Configuration=Release

#Create Nuget Packages
Invoke-WebRequest -Uri "https://dist.nuget.org/win-x86-commandline/latest/nuget.exe" -OutFile "nuget.exe"

Get-ChildItem Source\*.nuspec | % {.\nuget.exe pack $_ -version $GitVersion_NuGetVersion -Symbols}