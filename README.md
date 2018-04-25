CacheManager.FileCaching
========================

[![Build status](https://ci.appveyor.com/api/projects/status/pi3k1r41y2j2l5ov/branch/master?svg=true)](https://ci.appveyor.com/project/maldworth/cachemanager-filecaching/branch/master)
[![NuGet](https://img.shields.io/nuget/v/cachemanager.filecaching.svg)](https://www.nuget.org/packages/CacheManager.FileCaching/)

Overview
--------

This library has implemented CacheManager.Core's BaseCacheHandle&lt;T&gt; using
[FileCache][1] as our persistent store. In order to support NetStandard right now
you must add a nuget source for CacheManager.Core's preview builds on [myget][2].
This dependency will be removed once it's fully released (likely after Net Core 2.1)
release.

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
[right now][3]_

Please look in the unit tests for other examples of caching Complex Objects.

    [1]: https://www.nuget.org/packages/FileCache
    [2]: https://www.myget.org/F/cachemanager/api/v3/index.json
    [3]: https://github.com/MichaCo/CacheManager/issues/221