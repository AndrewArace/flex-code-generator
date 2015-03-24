# Project Wishlist #
  * Intelligently interpret packages, and build directory tree (currently only puts everything in one directory)
  * Add automatic inclusion of "import" tags to the top of the .as file
  * Detect reserved words in ActionScript code that lead to hard-to-figure-out compile errors, such as a variable with the same name as a Flex class (e.g. _var File:String;_ should generate a warning. Suggest _var MyFile:String;_)