simple implementation of the lz77 algorithm for the special case of compressing 7bit ascii

## Installation

simply compile the Program.cs file. tested with mono and probably works with your favorite c# compiler

## Usage

run the binary and follow the help menu instructions

## compression efficiency

not many test have been run. running it on the google.com/index.html file compresses it to about 25% of original size.

## notes

decompressing a compressed file containing anything other than 7bit ascii bytes will produce garbage.. sorry

## License
do whatever you please with this file
