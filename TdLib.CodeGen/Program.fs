// SPDX-FileCopyrightText: 2024-2025 tdsharp contributors <https://github.com/egramtel/tdsharp>
//
// SPDX-License-Identifier: MIT

module TdLib.CodeGen.Program

open System.IO

[<EntryPoint>]
let main _ =
    let writeFile folder clientFolder name (source: string) =
        if not (Directory.Exists(folder)) then
            Directory.CreateDirectory(folder) |> ignore
        if not (Directory.Exists(folder+"/"+clientFolder)) then
            Directory.CreateDirectory(folder+"/"+clientFolder) |> ignore
        File.WriteAllText(folder+"/"+clientFolder + "/" + name, source)

    for name, source in Generator.generateAllTypes() do
        writeFile "Objects" (name + ".cs") source

    for clientName, name, source in Generator.generateAllFuncs() do
        writeFile "Functions" clientName (name + ".cs") source

    0
