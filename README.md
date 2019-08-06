# bettersigntool

A wrapper around signtool with added features

* Batch mode (supply a .txt of filenames)
* Parallel signing
* Automatic retry with exponential back-off

Currently only supports the **sign** operation with a PFX and password

## Basic usage

Almost all switches are named the same as the default signtool (`/du` is `-du`, for example). The only exception is the input filename, which must be prefixed with `-I` (capital i)

	bettersigntool -d "Organisation Name" -du "http://mycompany.com" -f "C:\my.pfx" -p "password" -I myfile.dll

By default, the timeserver is Verisign, but you can override this by specifying the `-t` argument, as per the default signtool, i.e.:

	-t "http://timestamp.myspecialtimeserver.com"

## Batch mode

bettersigntool can take a .txt file and sign all of the filenames listed inside of it. The paths should be relative to the location of the .txt file itself.

The .txt extension is automatically detected on the input file and the tool behaves accordingly.

e.g.

### myfile.txt

	bin/file.dll
	bin/file2.dll

### Calling arguments

	bettersigntool -d "Organisation Name" -du "http://mycompany.com" -f "C:\my.pfx" -p "password" -I myfile.txt

Both `file.dll` and `file2.dll` will be signed as a result of the above

## Failure mode

As mentioned above, bettersigntool supports retry with exponential back-off. By default the tool will attempt 2 retries (three attempts total) per file but you can set this number via the optional `-ma` parameter.

The default behaviour is currently:

* Signing will be attempted a *total* of 3 times (i.e. 1 initial attempt, 2 retries)
* Initial base is 6 seconds (6000ms), with an exponent of 2 for each subsequent attempt
* Each back-off delay is jittered to between 50-100% of base
* Initial delay is between 3-6 seconds (6000ms)
* Second delay is between 6 and 12 seconds
