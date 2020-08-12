using System;

namespace BlogComments.GitHub
{
    /// <summary>
    /// Identifies the owner of a block of memory who is responsible for disposing of the underlying memory appropriately.
    /// </summary>
    /// <typeparam name="T">The type of elements to store in memory.</typeparam>
    public interface IReadOnlyMemoryOwner<T> : IDisposable
    {
        /// <summary>
        /// The memory belonging to this owner.
        /// </summary>
        public ReadOnlyMemory<T> Memory { get; }
    }
}
