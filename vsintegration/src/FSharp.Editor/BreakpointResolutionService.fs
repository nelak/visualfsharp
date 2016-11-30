﻿// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.VisualStudio.FSharp.Editor

open System
open System.Composition
open System.Collections.Concurrent
open System.Collections.Generic
open System.Threading
open System.Threading.Tasks
open System.Linq

open Microsoft.CodeAnalysis
open Microsoft.CodeAnalysis.Classification
open Microsoft.CodeAnalysis.Editor
open Microsoft.CodeAnalysis.Editor.Implementation.Debugging
open Microsoft.CodeAnalysis.Editor.Shared.Utilities
open Microsoft.CodeAnalysis.Formatting
open Microsoft.CodeAnalysis.Host.Mef
open Microsoft.CodeAnalysis.Text

open Microsoft.VisualStudio.FSharp.LanguageService
open Microsoft.VisualStudio.Text
open Microsoft.VisualStudio.Text.Tagging

open Microsoft.FSharp.Compiler.Parser
open Microsoft.FSharp.Compiler.SourceCodeServices
open Microsoft.FSharp.Compiler.Range

[<Shared>]
[<ExportLanguageService(typeof<IBreakpointResolutionService>, FSharpCommonConstants.FSharpLanguageName)>]
type internal FSharpBreakpointResolutionService() =

    static member GetBreakpointLocation(sourceText: SourceText, fileName: string, textSpan: TextSpan, options: FSharpProjectOptions) = async {
        // REVIEW: ParseFileInProject can cause FSharp.Compiler.Service to become unavailable (i.e. not responding to requests) for 
        // an arbitrarily long time while it parses all files prior to this one in the project (plus dependent projects if we enable 
        // cross-project checking in multi-project solutions). FCS will not respond to other 
        // requests unless this task is cancelled. We need to check that this task is cancelled in a timely way by the
        // Roslyn UI machinery.
        let! parseResults = FSharpLanguageService.Checker.ParseFileInProject(fileName, sourceText.ToString(), options)
        let textLinePos = sourceText.Lines.GetLinePosition(textSpan.Start)
        let textLineColumn = textLinePos.Character
        let fcsTextLineNumber = Line.fromZ textLinePos.Line // Roslyn line numbers are zero-based, FSharp.Compiler.Service line numbers are 1-based

        return parseResults.ValidateBreakpointLocation(mkPos fcsTextLineNumber textLineColumn)
    }

    interface IBreakpointResolutionService with
        member this.ResolveBreakpointAsync(document: Document, textSpan: TextSpan, cancellationToken: CancellationToken): Task<BreakpointResolutionResult> =
            async {
                match FSharpLanguageService.TryGetOptionsForEditingDocumentOrProject(document)  with 
                | Some options ->
                    let! sourceText = document.GetTextAsync(cancellationToken) |> Async.AwaitTask
                    let! location = FSharpBreakpointResolutionService.GetBreakpointLocation(sourceText, document.Name, textSpan, options)
                    return match location with
                           | None -> null
                           | Some(range) -> BreakpointResolutionResult.CreateSpanResult(document, CommonRoslynHelpers.FSharpRangeToTextSpan(sourceText, range))
                | None -> return null
            } |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken
            
        // FSROSLYNTODO: enable placing breakpoints by when user suplies fully-qualified function names
        member this.ResolveBreakpointsAsync(_, _, _): Task<IEnumerable<BreakpointResolutionResult>> =
            Task.FromResult(Enumerable.Empty<BreakpointResolutionResult>())
