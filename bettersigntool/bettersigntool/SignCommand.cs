using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ManyConsole;

namespace bettersigntool
{
    public class SignCommand : ConsoleCommand
    {
        /// <summary>
        /// Description of the content being signed
        /// </summary>
        public string Description
        {
            get;
            set;
        }

        /// <summary>
        /// URL for the description of the content being signed
        /// </summary>
        public string Url
        {
            get;
            set;
        }

        /// <summary>
        /// PFX to sign with
        /// </summary>
        public string CertificateFile
        {
            get;
            set;
        }

        /// <summary>
        /// Password of the PFX file
        /// </summary>
        public string PfxPassword
        {
            get;
            set;
        }

        /// <summary>
        /// Input file to sign
        /// </summary>
        public string InputFile
        {
            get;
            set;
        }

        /// <summary>
        /// List of input files to sign
        /// </summary>
        public string InputList
        {
            get;
            set;
        }

        /// <summary>
        /// Timestamp server URL
        /// </summary>
        public string TimestampServer
        {
            get;
            set;
        }

        /// <summary>
        /// The path to signtool
        /// </summary>
        public string SigntoolPath
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the file digest.
        /// </summary>
        /// <value>
        /// The file digest.
        /// </value>
        public string FileDigest
        {
            get;
            set;
        }

        /// <summary>
        /// The digest (hash) for performing the timestamping. 
        /// </summary>
        public string TimestampDigest
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the name of the subject.
        /// </summary>
        /// <value>
        /// The name of the subject.
        /// </value>
        public string SubjectName
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets the RFC3161 time server.
        /// </summary>
        /// <value>
        /// The RFC3161 time server.
        /// </value>
        public string Rfc3161TimeServer
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether [append signature].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [append signature]; otherwise, <c>false</c>.
        /// </value>
        public bool AppendSignature
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="SignCommand"/> is verbose.
        /// </summary>
        /// <value>
        ///   <c>true</c> if verbose; otherwise, <c>false</c>.
        /// </value>
        public bool Verbose
        {
            get;
            set;
        }

        /// <summary>
        /// Whether to show errors on failure (may leak passwords)
        /// </summary>
        public bool ShowErrors
        {
            get;
            set;
        }

        /// <summary>
        /// Maximum number of attempts per file
        /// </summary>
        public int MaximumFileAttempts { get; set; } = 3;

        /// <summary>
        /// After the initial failure of signing, we wait this long before trying again
        /// </summary>
        public TimeSpan InitialRetryWait
        {
            get;
            set;
        }

        /// <summary>
        /// For each retry we do an exponential backoff in delay. This is the exponent
        /// </summary>
        public double BackoffExponent
        {
            get;
            set;
        }

        public SignCommand()
        {
            IsCommand("sign", "Sign an assembly using a specified key file and timestamp server");

            this.SkipsCommandSummaryBeforeRunning();
            
            HasRequiredOption("I|input=", "The input file to sign. If the input is a .txt, it " +
                                         "is assumed to be a list of input files to sign.", i => InputFile = i);

            HasRequiredOption("d|description=", "Content description, often matches company/vendor name.",
                d => Description = d);

            HasRequiredOption("du|url=", "Content URL, often matches company/vendor site URL.",
                du => Url = du);

            HasRequiredOption("f|certfile=", "The signing certificate in a file. Only PFX is supported.",
                f => CertificateFile = f);

            HasOption("p|pfxpass=", "The password of the PFX certificate file.",
                p => PfxPassword = p);

            HasOption("t|timeserv=", "The URL of the timestamp server to use. If omitted, Versign is used.",
                t => TimestampServer = t);

            HasOption("e|errors", "Whether to dump full errors and stdout (may reveal passwords).",
                e => ShowErrors = (e != null));

            HasOption("Z|sdkpath=",
                "The signtool.exe SDK path.  If omitted, falls back to the 64-bit Windows 8.1 SDK.",
                ssp => SigntoolPath = ssp);

            HasOption("fd|filedigest=",
                "Specifies the file digest algorithm to use to create file signatures. The default algorithm is Secure Hash Algorithm (SHA-1).",
                fd => FileDigest = fd);

            HasOption("td|timedigest=",
                "Specifies the file digest algorithm to use to timestamp the file(s). The default algorithm is Secure Hash Algorithm (SHA-1).",
                td => TimestampDigest = td);

            HasOption("n|subjectname=",
                "Specifies the name of the subject of the signing certificate. This value can be a substring of the entire subject name.",
                n => SubjectName = n);

            HasOption("tr|rfc3161timeserver=",
                "Specifies the RFC 3161 time stamp server's URL. If this option (or /t) is not specified, the signed file will not be time stamped. A warning is generated if time stamping fails. This switch cannot be used with the /t switch.",
                tr => Rfc3161TimeServer = tr);

            HasOption("as|appendsignature",
                "Appends this signature. If no primary signature is present, this signature is made the primary signature.",
                a => AppendSignature = String.IsNullOrEmpty(a) ? false : true);

            HasOption("v|verbose",
                "Displays verbose output for successful execution, failed execution, and warning messages..",
                v => Verbose = String.IsNullOrEmpty(v) ? false : true);

            HasOption("ma|MaxFileAttempts=",
                "The maximum number of attempts to sign each file, should a timeout occur. If ommited, 3 is used.",
                ma => MaximumFileAttempts = int.Parse(ma));

            // Fallback to Verisign
            //
            if (String.IsNullOrEmpty(TimestampServer))
            {
                TimestampServer = "http://timestamp.verisign.com/scripts/timstamp.dll";
            }

            // Fallback to Win 8.1 SDK
            //
            if (String.IsNullOrEmpty(SigntoolPath))
            {
                SigntoolPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                    @"Windows Kits\8.1\bin\x64\signtool.exe");
            }

            InitialRetryWait = TimeSpan.FromSeconds(3);
            BackoffExponent = 2;
        }
        
        public override int Run(string[] remainingArguments)
        {
            if (!File.Exists(SigntoolPath))
            {
                Console.WriteLine($"The signtool path {SigntoolPath} is invalid/incorrect");
                return -1;
            }

            // If the file we're signing is a .txt, then we're actually signing a *list* of files
            //
            if (FileList.IsFileList(InputFile))
            {
                int errors = 0;

                CountdownEvent countdown = new CountdownEvent(1);

                List<string> files = FileList.Load(InputFile);

                foreach (string file in files)
                {
                    // Skip files that don't exist (non error condition)
                    //
                    if (!File.Exists(file))
                    {
                        Console.WriteLine($"File {file} does not exist: Skipping");
                        continue;
                    }

                    countdown.AddCount();

                    // Sign each file potentially in parallel
                    //
                    Task.Run(() =>
                    {
                        if (!DoSign(file))
                        {
                            Interlocked.Increment(ref errors);
                        }

                        countdown.Signal();
                    });
                }

                // Wait on file signing completion
                //
                countdown.Signal();
                countdown.Wait();

                if (errors > 0)
                {
                    Console.WriteLine($"** {errors} out of {files.Count} files were not successfully signed **");
                }
                else
                {
                    Console.WriteLine($"OK: {files.Count} files successfully signed");
                }
                
                return errors;
            }
            else
            {
                // Sign a single assembly (not a list)
                //

                return DoSign(InputFile) ? 0 : 1;
            }
        }

        /// <summary>
        /// Perform signing for a single file 
        /// </summary>
        /// <param name="filename">the filename of the assembly to sign</param>
        /// <returns>success/failure of the signing </returns>
        private bool DoSign(string filename)
        {
            double retryWait = InitialRetryWait.TotalSeconds;
            Random random = new Random();
            
            for (var attempt = 1; attempt <= MaximumFileAttempts; attempt++)
            {
                if (attempt > 1) 
                {
                    // exponential backoff with jitter to between half and max delay for this attempt
                    retryWait *= BackoffExponent;
                    TimeSpan jitteredRetry = TimeSpan.FromSeconds(retryWait / 2 + random.NextDouble() * retryWait / 2);
                    
                    Console.WriteLine($"Performing attempt #{attempt} of {MaximumFileAttempts} after {jitteredRetry.TotalSeconds}s...");
                    Thread.Sleep((int)jitteredRetry.TotalMilliseconds);
                }

                if (RunSigntool(filename))
                {
                    Console.WriteLine($"Signed OK: {filename}");
                    
                    // Instantaneous success (no retries required)
                    return true;
                }
            }
            
            Console.WriteLine($"Failed to sign {filename}: Maximum of {MaximumFileAttempts} attempts exceeded");

            return false;
        }

        /// <summary>
        /// Run the signtool process against a single given assembly filename
        /// </summary>
        /// <param name="filename">assembly filename to sign</param>
        /// <returns>success/failure flag for the signing of the assembly</returns>
        private bool RunSigntool(string filename)
        {
            List<string> arguments = new List<string>
            {
                "sign",
                $"/d \"{Description}\"",
                $"/du \"{Url}\"",
                $"/f \"{CertificateFile}\"",
                $"/p \"{PfxPassword}\""
            };

            if (AppendSignature)
            {
                arguments.Add("/as");
            }

            if (Verbose)
            {
                arguments.Add("/v");
            }

            if (!String.IsNullOrEmpty(SubjectName))
            {
                arguments.Add($"/n \"{SubjectName}\"");
            }

            if (String.IsNullOrEmpty(Rfc3161TimeServer))
            {
                arguments.Add($"/t \"{TimestampServer}\"");
            }
            else
            { 
                arguments.Add($"/tr \"{Rfc3161TimeServer}\"");
            }

            if (!String.IsNullOrEmpty(FileDigest))
            {
                arguments.Add($"/fd \"{FileDigest}\"");
            }

            if (!String.IsNullOrEmpty(TimestampDigest))
            {
                arguments.Add($"/td \"{TimestampDigest}\"");
            }

            arguments.Add($"\"{filename}\"");

            string flatArguments = String.Join(" ", arguments);
            string flatProcessAndArguments = $"\"{SigntoolPath}\" {flatArguments}";

            if (ShowErrors)
            {
                Console.WriteLine(flatProcessAndArguments);
            }
            
            using (Process p = Process.Start(new ProcessStartInfo($"\"{SigntoolPath}\"", flatArguments)
            {
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }))
            {
                string stdout = p.StandardOutput.ReadToEnd();
                string stderr = p.StandardError.ReadToEnd();

                if (!p.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds))
                {
                    try
                    {
                        p.Kill();
                    }
                    catch (Exception ex)
                    {
                        throw new InvalidOperationException("Execution timed out and process could not be teminated", ex);
                    }

                    throw new InvalidOperationException("Execution timed out");
                }
                else
                {
                    if (p.ExitCode == 0)
                    {
                        if (ShowErrors)
                        {
                            Console.WriteLine(stdout);
                        }

                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"A signtool execution for filename {filename} failed");

                        if (ShowErrors)
                        {
                            Console.WriteLine("--------------------------------");
                            Console.WriteLine(flatProcessAndArguments);
                            Console.WriteLine("--------------------------------");
                            Console.WriteLine(stdout);
                            Console.WriteLine(stderr);
                            Console.WriteLine("--------------------------------");
                            Console.WriteLine($"Raw exit code: {p.ExitCode}");
                        }

                        return false;
                    }
                }
            }
        }
    }
}
