using System.Globalization;
using FluentAssertions;
using MortenRoemer.Primitives.Text;

namespace Tests;

public sealed class CharBufferTest
{
    [Fact]
    public void IndexerShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer[2].Should().Be('r');

        buffer[6] = 'T';
        buffer.ToString().Should().Be("Lorum Tpsum");
    }
    
    [Fact]
    public void AppendShouldWork()
    {
        using var buffer = new CharBuffer(8);
        
        buffer.Append('a');
        buffer.Length.Should().Be(1);
        buffer.ToString().Should().Be("a");

        buffer.Append("ananas");
        buffer.Length.Should().Be(7);
        buffer.ToString().Should().Be("aananas");

        buffer.Append(1, "g", CultureInfo.InvariantCulture);
        buffer.Length.Should().Be(8);
        buffer.ToString().Should().Be("aananas1");
    }

    [Fact]
    public void ClearShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        buffer.Clear();
        
        buffer.Length.Should().Be(0);
        buffer.ToString().Should().Be(string.Empty);
    }

    [Fact]
    public void ContainsShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");

        buffer.Contains('a').Should().BeFalse();
        buffer.Contains('o').Should().BeTrue();
        buffer.Contains("abc").Should().BeFalse();
        buffer.Contains("rum").Should().BeTrue();
    }

    [Fact]
    public void CopyToShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        Span<char> destination = stackalloc char[buffer.Length];
        buffer.CopyTo(destination);
        
        buffer.ToString().Should().Be(destination.ToString());
    }

    [Fact]
    public void EndsWithShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.EndsWith('a').Should().BeFalse();
        buffer.EndsWith('m').Should().BeTrue();
        buffer.EndsWith("abc").Should().BeFalse();
        buffer.EndsWith("sum").Should().BeTrue();
    }

    [Fact]
    public void EnsureCapacityShouldWork()
    {
        using var buffer = new CharBuffer(128);
        buffer.Capacity.Should().Be(128);
        
        buffer.EnsureCapacity(2);
        buffer.Capacity.Should().Be(128);
        
        buffer.EnsureCapacity(129);
        buffer.Capacity.Should().BeGreaterThanOrEqualTo(129);
    }

    [Fact]
    public void GetHashCodeShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.GetHashCode().Should().NotBe(0);
    }
    
    [Fact]
    public void IndexOfShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.IndexOf('a').Should().BeNull();
        buffer.IndexOf('m').Should().Be(4);
        buffer.IndexOf("abc").Should().BeNull();
        buffer.IndexOf("um").Should().Be(3);
    }

    [Fact]
    public void InsertShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        buffer.Insert(5, 'a');
        buffer.Insert(12, " sit dolor amet");
        
        buffer.ToString().Should().Be("Loruma Ipsum sit dolor amet");
    }

    [Fact]
    public void LastIndexOfShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.LastIndexOf('a').Should().BeNull();
        buffer.LastIndexOf('m').Should().Be(10);
        buffer.LastIndexOf("abc").Should().BeNull();
        buffer.LastIndexOf("um").Should().Be(9);
    }

    [Fact]
    public void PadLeftShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.PadLeft(2);
        buffer.ToString().Should().Be("  Lorum Ipsum");

        buffer.PadLeft(2, '-');
        buffer.ToString().Should().Be("--  Lorum Ipsum");
    }
    
    [Fact]
    public void PadRightShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.PadRight(2);
        buffer.ToString().Should().Be("Lorum Ipsum  ");

        buffer.PadRight(2, '-');
        buffer.ToString().Should().Be("Lorum Ipsum  --");
    }

    [Fact]
    public void RemoveShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");

        buffer.Remove(5, 3);
        buffer.ToString().Should().Be("Lorumsum");
        
        buffer.Remove(5);
        buffer.ToString().Should().Be("Lorum");
    }

    [Fact]
    public void ReplaceShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");

        buffer.Replace('u', 'a');
        buffer.ToString().Should().Be("Loram Ipsam");

        buffer.Replace("am", "nunquam");
        buffer.ToString().Should().Be("Lornunquam Ipsnunquam");
    }

    [Fact]
    public void StartsWithShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        
        buffer.StartsWith('a').Should().BeFalse();
        buffer.StartsWith('L').Should().BeTrue();
        buffer.StartsWith("abc").Should().BeFalse();
        buffer.StartsWith("Lor").Should().BeTrue();
    }

    [Fact]
    public void SubstringShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");

        buffer.SubString(2, 3).Should().Be("rum");
    }

    [Fact]
    public void ToLowerShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        buffer.ToLower(CultureInfo.InvariantCulture);
        
        buffer.ToString().Should().Be("lorum ipsum");
    }
    
    [Fact]
    public void ToUpperShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        buffer.ToUpper(CultureInfo.InvariantCulture);
        
        buffer.ToString().Should().Be("LORUM IPSUM");
    }

    [Fact]
    public void TrimShouldWork()
    {
        using var buffer = new CharBuffer("  Lorum Ipsum  ");
        buffer.Trim();
        
        buffer.ToString().Should().Be("Lorum Ipsum");

        buffer.Clear();
        buffer.Append("--  Lorum Ipsum  --");
        buffer.Trim("- ");
        
        buffer.ToString().Should().Be("Lorum Ipsum");
    }
    
    [Fact]
    public void TrimEndShouldWork()
    {
        using var buffer = new CharBuffer("  Lorum Ipsum  ");
        buffer.TrimEnd();
        
        buffer.ToString().Should().Be("  Lorum Ipsum");

        buffer.Clear();
        buffer.Append("--  Lorum Ipsum  --");
        buffer.TrimEnd("- ");
        
        buffer.ToString().Should().Be("--  Lorum Ipsum");
    }
    
    [Fact]
    public void TrimStartShouldWork()
    {
        using var buffer = new CharBuffer("  Lorum Ipsum  ");
        buffer.TrimStart();
        
        buffer.ToString().Should().Be("Lorum Ipsum  ");

        buffer.Clear();
        buffer.Append("--  Lorum Ipsum  --");
        buffer.TrimStart("- ");
        
        buffer.ToString().Should().Be("Lorum Ipsum  --");
    }

    [Fact]
    public void TryCopyToShouldWork()
    {
        using var buffer = new CharBuffer("Lorum Ipsum");
        Span<char> smallDestination = stackalloc char[2];
        Span<char> largeDestination = stackalloc char[buffer.Length];

        buffer.TryCopyTo(smallDestination).Should().BeFalse();
        
        buffer.TryCopyTo(largeDestination).Should().BeTrue();
        largeDestination.ToString().Should().Be("Lorum Ipsum");
    }
}