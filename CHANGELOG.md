# Changelog for PommaLabs.KVLite #

### v5.2.6 (2016-06-26) ###

* [UPD] Updated System.Data.SQLite to version 1.0.102.0.
* [UPD] Moved interfaces and core classes to Finsa.CodeServices.Caching package.

### v5.2.5 (2016-04-23) ###

* [UPD] Updated System.Data.SQLite to version 1.0.101.0.

### v5.2.4 (2016-04-10) ###

* [UPD] Updated a few dependencies.
* [FIX] Minor internal rework to cache SQLite commands.

### v5.2.2 (2016-03-29) ###

* [UPD] Updated lots of dependencies.
* [FIX] Fixed a bug in type name handling during serialization and deserialization.
* [UPD] Reduced the number of parent keys for SQLite caches from 10 to 5.
* [FIX] Fixed an inizialization which was happening too early for WebCaches.
* [UPD] Now using Deflate as the default compressor. Deflate replaces Snappy, which seemed an "unsafe" solution for the long term.