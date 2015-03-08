# PommaLabs.KVLite #

A simple, timed and persistent key-value store based on SQLite. KVLite offers both a persistent and an in-memory implementation of that kind of store. KVLite also implements a caching bootstrapper for NancyFX, which can be configured to be either persistent or volatile.

## Summary ##

* Build status on [AppVeyor](https://ci.appveyor.com): [![Build status](https://ci.appveyor.com/api/projects/status/362eo5bmrbtjp203)](https://ci.appveyor.com/project/pomma89/kvlite)
* Current release: `v1.5.1`
* [Doxygen](http://www.stack.nl/~dimitri/doxygen/index.html) documentation: http://goo.gl/x1fCjT
* [NuGet](https://www.nuget.org) package(s):
    + [PommaLabs.KVLite](https://www.nuget.org/packages/PommaLabs.KVLite/), includes Core and all native libraries.
    + [PommaLabs.KVLite (Core)](https://www.nuget.org/packages/PommaLabs.KVLite.Core/), all managed APIs.
    + [PommaLabs.KVLite (Web API Output Cache Provider)](https://www.nuget.org/packages/PommaLabs.KVLite.WebApi/)
    + [PommaLabs.KVLite (Nancy Caching Bootstrapper)](https://www.nuget.org/packages/PommaLabs.KVLite.Nancy/)