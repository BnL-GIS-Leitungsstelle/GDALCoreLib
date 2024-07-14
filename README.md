# GDALTools_new

## Synopsis

This project is a proof-of-concept to investigate the amount of functionality of new the GDAL/OGR C#-wrapper for .NET 8). It continues to migrate functions from the Repo OgcTools, which is running on the old official GDAL/OGR Net-Framework Wrapper provided by nuget. The aim is to get a fully migrated library with GDAL-functionality  running in .NET 8.
First questions: How much is working with shape (SHP), geopackage (GPKG) and esri-filegeodatabase (FGDB) ? What functionality is included ?
The project is intended to be used as a library-project.
It is inspired by the project/repo "OgcTools".
As of Version GDAL-Core-Lib 3.7.2 in FGDB read/write-actions are supported (former read-only), as well as a few raster-formats in FGDB.

## Motivation

The Project exists, because of the change from .NET-Framwork to .NET 8, driven by microsoft. The library-project OgcTools should get migrated to .NET 8 to stay in an modern environment to enable the use of modern techniques, like Web-API, Blazor, running on linux, etc. 

## Roadmap

The following ideas will show the directions and options of future enhancements I want to accomplish:

* feature-wise comparison and editing attributes, supported by SQL
* geometry-comparison of two geometries
* editing vector-layer in FGDB
* option for more intense geometry-checker



## Architecture

So far, starting with the geodata-storage-formats, the first access-level deals with the basic handling of these.
The second level handles and extracts the geographical layers for further processing in the next level..


Intended architecture:
![Architecture](https://user-images.githubusercontent.com/9255514/148600464-78823074-0bcd-479f-9e5b-0940bc905838.png)

LayerAccessor: Options to use CopyLayer - Method
![CopyLayer-Options](https://user-images.githubusercontent.com/9255514/128594122-9fd6ab65-70ae-4b01-bacb-5f32b6f59245.png)
![CopyLayer-Options](https://github.com/user-attachments/assets/f049c87e-f9be-4a5a-aa22-9c3df3b534b3)

## Installation Instructions

Provide code examples and explanations of how to get the project.

## How to use

Show first steps, how to use this project.

## Code Example

Show what the library does as concisely as possible, developers should be able to figure out **how** your project solves their problem by looking at the code example. Make sure the API you are showing off is obvious, and that your code is short and concise.

## API Reference

Depending on the size of the project, if it is small and simple enough the reference docs can be added to the README. For medium size to larger projects it is important to at least provide a link to where the API reference docs live.

## Tests

Describe and show how to run the tests with code examples.

## Contributors

Let people know how they can dive into the project, include important links to things like issue trackers, irc, twitter accounts if applicable.

## License

A short snippet describing the license (MIT, Apache, etc.)
