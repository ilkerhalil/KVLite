namespace Benchmarks

open System
open KVLite
open NPerf.Framework

[<PerfTester(typeof<PersistentCache>, 10)>]
type PersistentCacheTester() = 
    member this.X = "F#"

    [<PerfTest>]
    member x.AddTimed() =
        PersistentCache.DefaultInstance.AddTimed("1", 1, DateTime.UtcNow)
        |> ignore
