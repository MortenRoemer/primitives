namespace MortenRoemer.Primitives.Text;

internal static class AllocationLimit
{
    public const int InBytes = 512;
    public const int InChars = InBytes / sizeof(char);
    public const int InInt32 = InBytes / sizeof(int);

}