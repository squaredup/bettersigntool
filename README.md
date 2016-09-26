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
bettersigntool can take a .txt file and sign all of the filenames listed inside of it. The paths should be relative to the location of the .txt file itself 

The .txt extension is automatically detected on the input file and the tool behaves accordingly.

e.g.

**myfile.txt**

	bin/file.dll
	bin/file2.dll


**Calling arguments**

	bettersigntool -d "Organisation Name" -du "http://mycompany.com" -f "C:\my.pfx" -p "password" -I myfile.txt

Both `file.dll` and `file2.dll` will be signed as a result of the above

## Files filter
An different way to use bettersigntool instead of _Batch mode_ (`-I` switch) is to use both `-fl` `-fp` switches.

These switches specifies which is the path of the folder containing the files to sign and a comma-separated values of filters used to look for files to sign within the specified folder.

`-fp`: Specifies the dir path used to look for files to sign.

`-fl`: Comma separated filters used to search for files to sign. This parameter used to filter files can contain a combination of valid literal path and wildcard (* and ?) characters, but doesn't support regular expressions.

e.g.

**C:\filetosign\**

	bin/mycompany1file1.dll
    bin/mycompany1file2.dll
	bin/externalfile.dll
	bin/mycompany2file.exe
	bin/externalfile2.exe
	bin/ini.txt


**Calling arguments**

	bettersigntool -d "Organisation Name" -du "http://mycompany.com" -f "C:\my.pfx" -p "password" -fp "C:\filetosign\" -fl "mycompany1file*.dll, mycompany2file*.exe"

`mycompany1file1.dll`, `mycompany1file2.dll` and `mycompany1file.exe` will be signed as a result of the above


## Failure mode
As mentioned above, bettersigntool supports retry with exponential back-off. Currently the settings for this are **not** exposed as command line options (but will be).

The behaviour is currently fixed as:
* Signing will be attempted a *total* of 3 times (i.e. 1 initial attempt, 2 retries)
* Initial retry delay is 3 seconds (3000ms)
* Back-off occurs with an exponent of 1.5 (wait 3 seconds, 5.196 seconds, finally 11.844 seconds)
