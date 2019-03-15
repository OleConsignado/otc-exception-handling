RD /S /Q %userprofile%\NetPackages\otc-exception-handling
MD %userprofile%\NetPackages\otc-exception-handling

dotnet pack -o %userprofile%\NetPackages\otc-exception-handling -c Debug /p:Version="3.2.0-debug-1" --include-symbols