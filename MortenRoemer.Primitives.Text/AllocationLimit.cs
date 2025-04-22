using MortenRoemer.ThreadSafety;

namespace MortenRoemer.Primitives.Text;

[ImmutableMemoryAccess(Reason = "This type should only contain constants that are frequently used")]
internal static class AllocationLimit
{
    public const int InBytes = 512;
    public const int InChars = InBytes / sizeof(char);
    public const int InInt32 = InBytes / sizeof(int);

}