// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.
//
// To run the tests in this file:
//
// Technique 1: Compile VisualFSharp.Unittests.dll and run it as a set of unit tests
//
// Technique 2:
//
//   Enable some tests in the #if EXE section at the end of the file, 
//   then compile this file as an EXE that has InternalsVisibleTo access into the
//   appropriate DLLs.  This can be the quickest way to get turnaround on updating the tests
//   and capturing large amounts of structured output.
(*
    cd Debug\net40\bin
    .\fsc.exe --define:EXE -r:.\Microsoft.Build.Utilities.Core.dll -o VisualFSharp.Unittests.exe -g --optimize- -r .\FSharp.LanguageService.Compiler.dll  -r .\FSharp.Editor.dll -r nunit.framework.dll ..\..\..\tests\service\FsUnit.fs ..\..\..\tests\service\Common.fs /delaysign /keyfile:..\..\..\src\fsharp\msft.pubkey ..\..\..\vsintegration\tests\unittests\SignatureHelpProviderTests.fs 
    .\VisualFSharp.Unittests.exe 
*)
// Technique 3: 
// 
//    Use F# Interactive.  This only works for FSharp.Compiler.Service.dll which has a public API
module Microsoft.VisualStudio.FSharp.Editor.Tests.Roslyn.SignatureHelpProvider

open System
open System.IO
open System.Threading
open System.Text

open NUnit.Framework

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Editor
open Microsoft.CodeAnalysis.Editor.Implementation.Debugging
open Microsoft.CodeAnalysis.Editor.Shared.Utilities
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Host
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Options
open Microsoft.CodeAnalysis.SignatureHelp
open Microsoft.CodeAnalysis.Text

open Microsoft.VisualStudio.FSharp.Editor
open Microsoft.VisualStudio.FSharp.LanguageService

open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Range

let filePath = "C:\\test.fs"

let PathRelativeToTestAssembly p = Path.Combine(Path.GetDirectoryName(Uri( System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath), p)

let internal options = { 
    ProjectFileName = "C:\\test.fsproj"
    ProjectFileNames =  [| filePath |]
    ReferencedProjects = [| |]
    OtherOptions = [| "-r:" + PathRelativeToTestAssembly(@"UnitTestsResources\MockTypeProviders\DummyProviderForLanguageServiceTesting.dll") |]
    IsIncompleteTypeCheckEnvironment = true
    UseScriptResolutionRules = false
    LoadTime = DateTime.MaxValue
    UnresolvedReferences = None
    ExtraProjectInfo = None
}

[<Test>]
let ShouldGiveSignatureHelpAtCorrectMarkers() =
    let manyTestCases = 
        [ ("""
//1
System.Console.WriteLine(1,arg1=2)

""",
            [(".", None); 
             ("System", None); 
             ("WriteLine", None);
             ("(", Some ("[7..40)", 0, 2, None)); 
             (",", Some ("[7..40)", 1, 2, Some "arg1"));
             ("arg", Some ("[7..40)", 1, 2, Some "arg1"));
             ("arg1", Some ("[7..40)", 1, 2, Some "arg1"));
             ("=", Some ("[7..40)", 1, 2, Some "arg1")); 
             ("2", Some ("[7..40)", 0, 2, None));
             (")", None)]);
          ( """
//2
open System
Console.WriteLine([(1,2)])
""",
            [
             ("WriteLine(", Some ("[20..45)", 0, 0, None));
             (",", None); 
             ("[(", Some ("[20..45)", 0, 1, None))
            ]);
          ( """
//3
type foo = N1.T< 
type foo2 = N1.T<Param1=
type foo3 = N1.T<ParamIgnored=
type foo4 = N1.T<Param1=1,
type foo5 = N1.T<Param1=1,ParamIgnored=
""",
            [("type foo = N1.T<", Some ("[18..26)", 0, 0, None));
             ("type foo2 = N1.T<", Some ("[38..52)", 0, 0, Some "Param1"));
             ("type foo2 = N1.T<Param1", Some ("[38..52)", 0, 1, Some "Param1"));
             ("type foo2 = N1.T<Param1=", Some ("[38..52)", 0, 1, Some "Param1"));
             ("type foo3 = N1.T<", Some ("[64..84)", 0, 0, Some "ParamIgnored"));
             ("type foo3 = N1.T<ParamIgnored=", Some ("[64..84)", 0, 1, Some "ParamIgnored"));
             ("type foo4 = N1.T<Param1", Some ("[96..112)", 0, 2, Some "Param1"));
             ("type foo4 = N1.T<Param1=", Some ("[96..112)", 0, 2, Some "Param1"));
             ("type foo4 = N1.T<Param1=1", Some ("[96..112)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1", Some ("[124..153)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=", Some ("[124..153)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=1", Some ("[124..153)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=1,", Some ("[124..153)", 1, 2, Some "ParamIgnored"));
             ("type foo5 = N1.T<Param1=1,ParamIgnored",Some ("[124..153)", 1, 2, Some "ParamIgnored"));
             ("type foo5 = N1.T<Param1=1,ParamIgnored=",Some ("[124..153)", 1, 2, Some "ParamIgnored"))
            ]);
          ( """
//4
type foo = N1.T< > 
type foo2 = N1.T<Param1= >
type foo3 = N1.T<ParamIgnored= >
type foo4 = N1.T<Param1=1, >
type foo5 = N1.T<Param1=1,ParamIgnored= >
""",
            [("type foo = N1.T<", Some ("[18..24)", 0, 0, None));
             ("type foo2 = N1.T<", Some ("[40..53)", 0, 0, Some "Param1"));
             ("type foo2 = N1.T<Param1", Some ("[40..53)", 0, 1, Some "Param1"));
             ("type foo2 = N1.T<Param1=", Some ("[40..53)", 0, 1, Some "Param1"));
             ("type foo3 = N1.T<", Some ("[68..87)", 0, 0, Some "ParamIgnored"));
             ("type foo3 = N1.T<ParamIgnored=", Some ("[68..87)", 0, 1, Some "ParamIgnored"));
             ("type foo4 = N1.T<Param1", Some ("[102..117)", 0, 2, Some "Param1"));
             ("type foo4 = N1.T<Param1=", Some ("[102..117)", 0, 2, Some "Param1"));
             ("type foo4 = N1.T<Param1=1", Some ("[102..117)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1", Some ("[132..160)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=", Some ("[132..160)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=1", Some ("[132..160)", 0, 2, Some "Param1"));
             ("type foo5 = N1.T<Param1=1,", Some ("[132..160)", 1, 2, Some "ParamIgnored"));
             ("type foo5 = N1.T<Param1=1,ParamIgnored",Some ("[132..160)", 1, 2, Some "ParamIgnored"));
             ("type foo5 = N1.T<Param1=1,ParamIgnored=",Some ("[132..160)", 1, 2, Some "ParamIgnored"))])
//Test case 5
          ( """let _ = System.DateTime(""",
            [("let _ = System.DateTime(",  Some ("[8..24)", 0, 0, None)) ])
          ]

    for (fileContents, testCases) in manyTestCases do
      printfn "Test case: fileContents = %s..." fileContents.[2..4]
      let actual = 
        [ for (marker, expected) in testCases do
            printfn "Test case: marker = %s" marker 

            let caretPosition = fileContents.IndexOf(marker) + marker.Length

            let documentationProvider = 
                { new IDocumentationBuilder with
                    override doc.AppendDocumentationFromProcessedXML(appendTo,processedXml,showExceptions, showReturns, paramName) = ()
                    override doc.AppendDocumentation(appendTo,filename,signature, showExceptions, showReturns, paramName) = ()
                } 

            let triggerChar = if marker = "," then Some ',' elif marker = "(" then Some '(' elif marker = "<" then Some '<' else None
            let triggered = FSharpSignatureHelpProvider.ProvideMethodsAsyncAux(documentationProvider, SourceText.From(fileContents), caretPosition, options, triggerChar, filePath, 0) |> Async.RunSynchronously
            FSharpLanguageService.Checker.ClearLanguageServiceRootCachesAndCollectAndFinalizeAllTransients()
            let actual = 
                match triggered with 
                | None -> None
                | Some (results,applicableSpan,argumentIndex,argumentCount,argumentName) -> Some (applicableSpan.ToString(),argumentIndex,argumentCount,argumentName)

            if expected <> actual then Assert.Fail(sprintf "FSharpCompletionProvider.ProvideMethodsAsyncAux() gave unexpected results, expected %A, got %A" expected actual)

            yield (marker, actual) ] 
      () 
      // Use this to print out data to update the test cases, after uncommenting the assert
      //printfn "(\"\"\"%s\n\"\"\",\n%s)" fileContents ((sprintf "%A" actual).Replace("null","None"))




#if EXE
ShouldGiveSignatureHelpAtCorrectMarkers()
#endif
