# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.1] - 2021-01-10

### Changed

- Debug symbols are no longer in the NuGet package and are now published to the NuGet symbol location that is built in to Visual Studio. See the readme to load debug symbols for prerelease builds from MyGet.

## [1.0.0] - 2020-02-01

### Added

- Initial release, targeting .NET Standard 2.0. Ability to track a `Task`, invoke a `Func<Task>`, post a synchronous or async callback to the current or specified synchronization context, and wait for all of the above.
