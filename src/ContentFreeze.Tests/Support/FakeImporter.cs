using System.Runtime.CompilerServices;

// The namespace is the point: the guard recognises uSync by the namespace of the code on the stack.
namespace uSync.Fake;

internal static class FakeImporter
{
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Import<T>(Func<T> write) => write();
}
