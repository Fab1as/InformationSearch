using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace InformationSearch
{
    internal class ConsoleProcessWrapper : IDisposable
    {
        private static readonly object LockObject = new object();

        private readonly Process _process;
        private readonly StreamReader _streamReader;

        public ConsoleProcessWrapper(ProcessStartInfo startInfo)
        {
            _process = CreateProcess(startInfo);
            _streamReader = new StreamReader(_process.StandardOutput.BaseStream, Encoding.UTF8);
        }

        public string GetProcessOutput(string text)
        {
            return GetProcessOutput(_process, _streamReader, text);
        }

        private string GetProcessOutput(Process myStemProcess, StreamReader streamReader, string text)
        {
            lock (LockObject)
            {
                var buffer = Encoding.UTF8.GetBytes(text + "\r\n");
                myStemProcess.StandardInput.BaseStream.Write(buffer, 0, buffer.Length);
                myStemProcess.StandardInput.BaseStream.Flush();
                return streamReader.ReadLine();
            }
        }

        private Process CreateProcess(ProcessStartInfo startInfo)
        {
            var process = new Process { StartInfo = startInfo };
            process.Start();
            return process;
        }

        public void Dispose()
        {
            _process?.Dispose();
            _streamReader?.Dispose();
        }
    }
}
