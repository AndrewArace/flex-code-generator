# flex-code-generator
Automatically exported from code.google.com/p/flex-code-generator

A quick-and-dirty command-line tool to generate ActionScript (.as) files to represent the classes in a .NET assembly.

This was needed by a relatively large API I had developed when we decided to use FluorineFX for the binary (AMF-.NET) communication between an Adobe Flex front-end application and a .NET back-end.

The Flex code requires a matching API structure in ActionScript (with headers matching the .NET namespace objects). This tool simply converts the public .NET classes via reflection on an assembly into ActionScript
