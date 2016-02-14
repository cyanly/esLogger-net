# esLogger

> High performance ElasticSearch structured logging package for C#

<p align="center">
  <img src="https://cdn.rawgit.com/cyanly/esLogger-net/gh-pages/kibana.png" alt=""/>
</p>


## Features

- [x] Structured logging with dynamic objects.
- [x] Compile time tagged source code path/module/line of logging, **No Runtime Refelection**
- [x] BulkAsync inserts into ElasticSearch.
- [x] Batched logging with high throughput.
- [x] Colorised console output

<p align="center">
  <img src="https://cdn.rawgit.com/cyanly/esLogger-net/gh-pages/consoledemo.png" alt=""/>
</p>


## Usage

``` c#
// Optional
Logger.ConnectElasticSearch();
// or
Logger.ConnectElasticSearch("http://some.where.else:9200/");

// Strings
Logger.Info("Test Info");
Logger.Warn("Test Warn");

// Dynamic objects
Logger.Info(new
{
    value = 789.12,
    message = "test 1"
});

// Nested
Logger.Warn(new
{
    number = 54321,
    message = "test 3",
    some_obj = new{
        name = "nested object",
        value = 0.99,
    }
});

// Exceptions
Logger.Error(new
{
    value = 789.12,
    message = "test 4"
}, new ApplicationException());
```



## Contributing
Contributions are welcome. 


## License
© 2016+, Chao Yan. Released under the MIT.<br>
