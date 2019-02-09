CacheManager.FileCaching
========================

Build Status
------------

Branch | Status
--- | :---:
master | [![master](https://ci.appveyor.com/api/projects/status/pi3k1r41y2j2l5ov/branch/master?svg=true)](https://ci.appveyor.com/project/maldworth/cachemanager-filecaching/branch/master)
develop | [![develop](https://ci.appveyor.com/api/projects/status/pi3k1r41y2j2l5ov/branch/develop?svg=true)](https://ci.appveyor.com/project/maldworth/cachemanager-filecaching/branch/develop)

Nuget Package
-------------

| Package Name | FullFramework | .NET Standard |
| ------------ | :-----------: | :-----------: |
| [![NuGet](https://img.shields.io/nuget/v/cachemanager.filecaching.svg)](https://www.nuget.org/packages/CacheManager.FileCaching) | 4.0/4.5 | 2.0 |

Overview
--------

This library has implemented CacheManager.Core's BaseCacheHandle&lt;T&gt; using
[FileCache](https://www.nuget.org/packages/FileCache) as our persistent store.
Cache Manager 2.0.0-prerelease must be used in order to support NetStandard.

Quick Start
-----------

I assume you are already familiar with CacheManager, so really the only thing you
need to get going is configure.

```csharp
var config = new ConfigurationBuilder()
    .WithJsonSerializer() // Only need this line if targeting netstandard
    .WithFileCacheHandle()
    .Build();

var _cache = new BaseCacheManager<string>(config);

// Then somewhere else in your code
_cache.Add("mykey","myvalue");

var result = _cache.Get<string>("mykey");
```

**_Note_**: _NetStandard target of CacheManager doesn't support BinarySerializer
[right now](https://github.com/MichaCo/CacheManager/issues/221)_

Please look in the unit tests for other examples of caching Complex Objects.