﻿using System.IO;
using System.Threading.Tasks;
using Csi.Helpers.Azure;
using Microsoft.Extensions.Logging;
using Util.Extensions.Logging.Step;

namespace Csi.Plugins.AzureFile
{
    sealed class SmbShareAttacherLinux : ISmbShareAttacher
    {
        private readonly IExternalRunner cmdRunner;
        private readonly ILogger logger;

        public SmbShareAttacherLinux(IExternalRunnerFactory cmdRunnerFactory, ILogger<SmbShareAttacherLinux> logger)
        {
            this.cmdRunner = cmdRunnerFactory.Create(true, false) ;
            this.logger = logger;
        }

        public async Task AttachAsync(string targetPath, string unc, SmbShareCredential smbShareCredential)
        {
            using (var _s = logger.StepDebug(nameof(AttachAsync)))
            {
                // Ensure dir exists
                Directory.CreateDirectory(targetPath);
                var cmd = getLinuxConnectCmd(unc, targetPath, smbShareCredential);
                await cmdRunner.RunExecutable(cmd.Command, cmd.Arguments);

                _s.Commit();
            }
        }

        public async Task DetachAsync(string targetPath)
        {
            using (var _s = logger.StepDebug(nameof(AttachAsync)))
            {
                var cmd = getLinuxDisconnectCmd(targetPath);
                await cmdRunner.RunExecutable(cmd.Command, cmd.Arguments);

                _s.Commit();
            }
        }

        private static CmdEntry getLinuxConnectCmd(
            string unc,
            string targetPath,
            SmbShareCredential smbShareCredential)
        {
            return new CmdEntry
            {
                Command = "mount",
                Arguments = new[]
                {
                    normalizeUnc(unc),
                    targetPath,
                    "-t", "cifs",
                    "-o",
                    $"username={smbShareCredential.Username},password={smbShareCredential.Password}"
                    + ",vers=3.0,dir_mode=0777,file_mode=0777,sec=ntlmssp",
                }
            };
        }

        private static string normalizeUnc(string unc) => unc.Replace('\\', '/');

        public static CmdEntry getLinuxDisconnectCmd(string targetPath)
        {
            return new CmdEntry
            {
                Command = "umount",
                // TODO verify mount from unc?
                Arguments = new[] { targetPath }
            };
        }
    }

    sealed class CmdEntry
    {
        public string Command { get; set; }
        public string[] Arguments { get; set; }
    }
}
