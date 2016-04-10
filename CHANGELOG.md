# Changelog for PommaLabs.KVLite #

### v5.2.4 (2016-04-10) ###

* Updated a few dependencies.
* Minor internal rework to cache SQLite commands.

### v5.2.2 (2016-03-29) ###

* Updated lots of dependencies.
* Fixed a bug in type name handling during serialization and deserialization.
* Reduced the number of parent keys for SQLite caches from 10 to 5.
* Fixed an inizialization which was happening too early for WebCaches.
* Now using Deflate as the default compressor. Deflate replaces Snappy, which seemed an "unsafe" solution for the long term.