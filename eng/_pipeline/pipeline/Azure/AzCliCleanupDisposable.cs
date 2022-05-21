using System;
using Nuke.Common;

namespace Vandertil.Blog.Pipeline.Azure
{
    internal class AzCliCleanupDisposable : IDisposable
    {
        private readonly string _commandToExecute;
        private bool _disposed;

        public AzCliCleanupDisposable(string commandToExecute)
        {
            _commandToExecute = commandToExecute;
            _disposed = false;
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }

            try
            {
                AzCli.Az(_commandToExecute);
            }
            catch (Exception e)
            {
                Serilog.Log.Warning(e, "Exception occured during cleanup.");

                // Do not throw from Dispose.
            }

            _disposed = true;
        }
    }
}
