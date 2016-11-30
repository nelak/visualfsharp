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
open Microsoft.CodeAnalysis.Editor.Implementation
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
[<ExportLanguageService(typeof<ILanguageDebugInfoService>, FSharpCommonConstants.FSharpLanguageName)>]
type internal FSharpLanguageDebugInfoService() =

    static member GetDataTipInformation(sourceText: SourceText, position: int, tokens: List<ClassifiedSpan>): TextSpan option =
        let tokenIndex = tokens |> Seq.tryFindIndex(fun t -> t.TextSpan.Contains(position))

        if tokenIndex.IsNone then
            None
        else
            let token = tokens.[tokenIndex.Value]
        
            match token.ClassificationType with

            | ClassificationTypeNames.StringLiteral ->
                Some(token.TextSpan)

            | ClassificationTypeNames.Identifier ->
                let textLine = sourceText.Lines.GetLineFromPosition(position)
                let textLinePos = sourceText.Lines.GetLinePosition(position)
                let textLineColumn = textLinePos.Character
                match QuickParse.GetCompleteIdentifierIsland false (textLine.ToString()) textLineColumn with
                | None -> None
                | Some(island, islandEnd, _) ->
                    let islandDocumentStart = textLine.Start + islandEnd - island.Length
                    Some(TextSpan.FromBounds(islandDocumentStart, islandDocumentStart + island.Length))

            | _ -> None


    interface ILanguageDebugInfoService with
        
        // FSROSLYNTODO: This is used to get function names in breakpoint window. It should return fully qualified function name and line offset from the start of the function.
        member this.GetLocationInfoAsync(_, _, _): Task<DebugLocationInfo> =
            Task.FromResult(Unchecked.defaultof<DebugLocationInfo>)

        member this.GetDataTipInfoAsync(document: Document, position: int, cancellationToken: CancellationToken): Task<DebugDataTipInfo> =
            async {
                let defines = FSharpLanguageService.GetCompilationDefinesForEditingDocument(document)  
                let! sourceText = document.GetTextAsync(cancellationToken) |> Async.AwaitTask
                let textSpan = TextSpan.FromBounds(0, sourceText.Length)
                let tokens = CommonHelpers.getColorizationData(document.Id, sourceText, textSpan, Some(document.Name), defines, cancellationToken)
                let result = 
                    match FSharpLanguageDebugInfoService.GetDataTipInformation(sourceText, position, tokens) with
                    | None -> DebugDataTipInfo()
                    | Some(textSpan) -> DebugDataTipInfo(textSpan, sourceText.GetSubText(textSpan).ToString())
                return result
            } |> CommonRoslynHelpers.StartAsyncAsTask cancellationToken
            
            