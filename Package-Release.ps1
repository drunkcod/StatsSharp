$MSBuildPath = [IO.Directory]::GetFiles(
	[IO.Path]::Combine([Environment]::GetFolderPath([Environment+SpecialFolder]::ProgramFilesX86), "MSBuild"),
	"MSBuild.exe",
	[IO.SearchOption]::AllDirectories
) | Sort-Object -Descending | Select-Object -First 1

& $MSBuildPath Build.proj /t:Build /p:Configuration=Release /p:Build=$Build /v:m /nologo
