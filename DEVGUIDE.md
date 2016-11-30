# F# Compiler, Core Library and Visual F# Tools Open Contribution Repository

This repo is where you can contribute to the F# compiler, core library and the Visual F# Tools.
To learn what F# is and why it's interesting, go to [fsharp.org](http://fsharp.org). To get a free F# environment, go to [fsharp.org](http://fsharp.org/use/windows).

**Compiler Technical Documentation**

The primary technical documents for the F# compiler code are

* [The F# Language and Core Library RFC Process](http://fsharp.github.io/2016/09/26/fsharp-rfc-process.html)

* [The F# Language Specification](http://fsharp.org/specs/language-spec/)

* [The F# Compiler Technical Guide](http://fsharp.github.io/2015/09/29/fsharp-compiler-guide.html) 
  maintained by contributors to this repository.  Please read
  and contribute to that guide.

**License**
> Contributions made to this repo are subject to terms and conditions of the Apache License, Version 2.0. A copy of the license can be found in the [License.txt](License.txt) file at the root of this distribution.
> By using this source code in any fashion, you are agreeing to be bound by the terms of the Apache License, Version 2.0. You must not remove this notice, or any other, from this software.

**Questions?** If you have questions about the source code, please ask in the issues.

## Quick Start: Build, Test, Develop

### F# Compiler (Linux)

Currently you can do on Linux a bootstrap of the Mono version of the compiler.  Full testing is not enabled,
nor is a .NET Core build of the compiler.

First [install Mono](http://www.mono-project.com/docs/getting-started/install/linux/).  
Then:
    
    ./build.sh

results will be in ``Debug\net40\bin\...``.  This doesn't do any testing (beyond the bootstrap). You can
run the compiler ``fsc.exe`` and F# Interactive ``fsi.exe`` by hand to test it.

These steps are tested under the Linux/Mono configuration(s) in ``.travis.yml`` (Ubuntu).

### F# Compiler (Windows)

On Windows you can build the F# compiler for .NET Framework as follows:

    build.cmd

This is the same as

    build.cmd net40

There are various qualifiers:

    build.cmd release         -- build release (the default)
    build.cmd debug           -- build debug instead of release

    build.cmd net40           -- build .NET Framework compiler (the default)
    build.cmd coreclr         -- build .NET Core compiler 
    build.cmd vs              -- build the Visual F# IDE Tools (see below)
    build.cmd pcls            -- build the PCL FSharp.Core libraries
    build.cmd all             -- build all 

    build.cmd proto           -- force the rebuild of the Proto bootstrap compiler in addition to other things

    build.cmd test            -- build default targets, run suitable tests
    build.cmd net40 test      -- build net40, run suitable tests
    build.cmd coreclr test    -- build coreclr, run suitable tests
    build.cmd vs test         -- build Visual F# IDE Tools, run all tests (see below)
    build.cmd all test        -- build all, run all tests

    build.cmd test-smoke      -- build, run smoke tests
    build.cmd test-net40-fsharp     -- build, run tests\fsharp suite for .NET Framework
    build.cmd test-net40-fsharpqa   -- build, run tests\fsharpqa suite for .NET Framework

After you build the first time you can open and use this solution:

    .\FSharp.sln

or just build it directly:

    msbuild FSharp.sln 

Building ``FSharp.sln`` builds nearly everything. However building portable profiles of 
FSharp.Core.dll is not included.  If you are just developing the core compiler and library
then building the solution will be enough.

### Notes on the .NET Framework build

1. The `update.cmd` script adds required strong name validation skips, and NGens the compiler and libraries. This requires admin privileges.
1. The compiler binaries produced are "private" and strong-named signed with a test key.
1. Some additional tools are required to build the compiler, notably `fslex.exe`, `fsyacc.exe`, `FSharp.PowerPack.Build.Tasks.dll`, `FsSrGen.exe`, `FSharp.SRGen.Build.Tasks.dll`, and the other tools found in the `lkg` directory.
1. The overall bootstrapping process executes as follows
 - We first need an existing F# compiler. We use the one in the `lkg` directory. Let's assume this compiler has an `FSharp.Core.dll` with version X.
 - We use this compiler to compile the source in this distribution, to produce a "proto" compiler, dropped to the `proto` directory. When run, this compiler still relies on `FSharp.Core.dll` with version X.
 - We use the proto compiler to compile the source for `FSharp.Core.dll` in this distribution.
 - We use the proto compiler to compile the source for `FSharp.Compiler.dll`, `fsc.exe`, `fsi.exe`, and other binaries found in this distribution.

### Configuring proxy server

If you are behind a proxy server, NuGet client tool must be configured to use it:

    .nuget\nuget.exe config -set http_proxy=proxy.domain.com:8080 -ConfigFile .nuget\NuGet.Config
    .nuget\nuget.exe config -set http_proxy.user=user_name -ConfigFile .nuget\NuGet.Config
    .nuget\nuget.exe config -set http_proxy.password=user_password -ConfigFile .nuget\NuGet.Config

Where you should set proper proxy address, user name and password.

# The Visual F# IDE Tools (Windows Only)

To build and test Visual F# IDE Tools, you must use [Visual Studio "vNext" (aka "Dev15")](https://www.visualstudio.com/en-us/downloads/visual-studio-next-downloads-vs.aspx). This is the one after Visual Studio 2015 (aka "Dev 14").  You must also install Visual Studio SDK (also called _Visual Studio Extensibility SDK_ on the Visual Studio installer) before building Visual F# IDE Tools.
Please ensure that the Visual Studio SDK version is matched with your current Visual Studio to ensure successful builds. For example: Visual Studio 2015 Update 1 requires Visual Studio 2015 SDK Update 1. Any installation of Visual Studio 2015 and later provides Visual Studio SDK as part of the installation of Visual Studio 2015 as feature installation. 

    build.cmd vs              -- build the Visual F# IDE Tools (see below)
    build.cmd vs test         -- build Visual F# IDE Tools, run all tests (see below)

Use ``VisualFSharp.sln`` if you're building the Visual F# IDE Tools.


## [Optional] Install the Visual F# IDE Tools  (Windows Only)

At time of writing, the Visual F# IDE Tools can only be installed into Visual Studio "Next" releases.
The new builds of the Visual F# IDE Tools can no longer be installed into Visual Studio 2015.

You can install Visual Studio "Next" from https://www.visualstudio.com/en-us/downloads/visual-studio-next-downloads-vs.aspx.

**Note:** This step will install a VSIX extension into Visual Studio "Next" that changes the Visual F# IDE Tools 
components installed in that VS installation.  You can revert this step by disabling or uninstalling the addin.

For **Debug**, uninstall then reinstall:

    VSIXInstaller.exe  /a /u:"VisualFSharp"
    VSIXInstaller.exe /a debug\net40\bin\VisualFSharpOpenSource.vsix

For **Release**, uninstall then reinstall:

    VSIXInstaller.exe  /a /u:"VisualFSharp"
    VSIXInstaller.exe /a release\net40\bin\VisualFSharpOpenSource.vsix

Restart Visual Studio, it should now be running your freshly-built Visual F# IDE Tools with updated F# Interactive.

### [Optional] F5 testing of local changes

To test your changes locally _without_ overwriting your default installed F# tools, set the `VisualFSharp\Vsix\VisualFSharpOpenSource`
project as the startup project.  When you hit F5 a new instance of Visual Studio will be started in the `FSharpDev` hive with your
changes, but the root (default) hive will remain untouched.

### [Optional] Rapid deployment of incremental changes to Visual F# IDE Tools components

For the brave, you can rapidly deploy incrementally updated versions of Visual F# IDE Tool components such as ``FSHarp.Editor.dll`` by copying them directly into the extension directory in your user AppData folder:

    xcopy /y debug\net40\bin\FSharp.* "%USERPROFILE%\AppData\Local\Microsoft\VisualStudio\15.0_7c5620b7FSharpDev\Extensions\Microsoft.VisualFSharpTools\Visual F# Tools\15.4.1.9055"

This gives a much tighter inner development loop than uninstalling/reinstalling the VSIX, as you do not have to restart VIsual Studio. Caveat emptor.

### [Optional] Clobber the F# SDK on the machine

**Note:** Step #3 below will clobber the machine-wide installed F# SDK on your machine. This replaces the ``fsi.exe``/``fsiAnyCpu.exe`` used by Visual F# Interactive and the ``fsc.exe`` used by ``Microsoft.FSharp.targets``.  Repairing Visual Studio 15 is currently the only way to revert this step.  

For **Debug**:

1. Run ``vsintegration\update-vsintegration.cmd debug`` (clobbers the installed F# SDK)

For **Release**:

1. Run ``vsintegration\update-vsintegration.cmd release`` (clobbers the installed F# SDK)




## Resources

The primary technical guide to the core compiler code is [The F# Compiler Technical Guide](http://fsharp.github.io/2015/09/29/fsharp-compiler-guide.html).  Please read and contribute to that guide.

