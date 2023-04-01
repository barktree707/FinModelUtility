﻿using System.Diagnostics;

using asserts;

using fin.io;
using fin.log;

namespace uni.util.cmd {
  public class ProcessUtil {
    public static Process ExecuteBlocking(
        IFile exeFile,
        params string[] args) {
      var processSetup = new ProcessSetup(exeFile, args) {
          Method = ProcessExecutionMethod.BLOCK,
      };
      return ProcessUtil.Execute(processSetup);
    }

    public static Process ExecuteBlockingSilently(
        IFile exeFile,
        params string[] args) {
      var processSetup = new ProcessSetup(exeFile, args) {
          Method = ProcessExecutionMethod.BLOCK,
          WithLogging = false,
      };
      return ProcessUtil.Execute(processSetup);
    }

    public enum ProcessExecutionMethod {
      MANUAL,
      BLOCK,
      TIMEOUT,
      ASYNC
    }

    public class ProcessSetup {
      public IFile ExeFile { get; set; }
      public string[] Args { get; set; }

      public ProcessExecutionMethod Method { get; set; } =
        ProcessExecutionMethod.BLOCK;

      public bool WithLogging { get; set; } = true;

      public ProcessSetup(IFile exeFile, params string[] args) {
        this.ExeFile = exeFile;
        this.Args = args;
      }
    }

    public static Process Execute(ProcessSetup processSetup) {
      var exeFile = processSetup.ExeFile;
      Asserts.True(
          exeFile.Exists,
          $"Attempted to execute a program that doesn't exist: {exeFile}");

      var args = processSetup.Args;
      var argString = "";
      for (var i = 0; i < args.Length; ++i) {
        // TODO: Is this safe?
        var arg = args[i];

        if (i > 0) {
          argString += " ";
        }
        argString += arg;
      }

      var processStartInfo =
          new ProcessStartInfo($"\"{exeFile.FullName}\"", argString) {
              CreateNoWindow = true,
              RedirectStandardOutput = true,
              RedirectStandardError = true,
              RedirectStandardInput = true,
              UseShellExecute = false,
          };

      var process = Asserts.CastNonnull(Process.Start(processStartInfo));
      ChildProcessTracker.AddProcess(process);

      var logger = Logging.Create(exeFile.FullName);
      if (processSetup.WithLogging) {
        process.OutputDataReceived += (_, args) => {
          if (args.Data != null) {
            logger!.LogInformation("  " + args.Data);
          }
        };
        process.ErrorDataReceived += (_, args) => {
          if (args.Data != null) {
            logger!.LogError("  " + args.Data);
          }
        };
      } else {
        process.OutputDataReceived += (_, _) => {};
        process.ErrorDataReceived += (_, _) => {};
      }

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      switch (processSetup.Method) {
        case ProcessExecutionMethod.MANUAL: {
          break;
        }

        case ProcessExecutionMethod.BLOCK: {
          process.WaitForExit();
          break;
        }

        default:
          throw new NotImplementedException();
      }

      // TODO: https://stackoverflow.com/questions/139593/processstartinfo-hanging-on-waitforexit-why
      /*
      using var outputWaitHandle = new AutoResetEvent(false);
      using var errorWaitHandle = new AutoResetEvent(false);

      process.OutputDataReceived += (sender, e) => {
        if (e.Data == null) {
          // ReSharper disable once AccessToDisposedClosure
          outputWaitHandle.Set();
        } else {
          logger.LogInformation(e.Data);
        }
      };
      process.ErrorDataReceived += (sender, e) => {
        if (e.Data == null) {
          // ReSharper disable once AccessToDisposedClosure
          errorWaitHandle.Set();
        } else {
          logger.LogError(e.Data);
        }
      };

      process.Start();

      process.BeginOutputReadLine();
      process.BeginErrorReadLine();

      // TODO: Allow passing in timeouts
      if (outputWaitHandle.WaitOne() &&
          errorWaitHandle.WaitOne()) {
        process.WaitForExit();
        // Process completed. Check process.ExitCode here.
      } else {
        // Timed out.
      }*/

      return process;
    }
  }
}