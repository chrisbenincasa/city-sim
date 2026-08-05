using System.Text;
using Borough.Core.Determinism;
using Borough.Formats;

namespace Borough.Tests.Formats;

/// <summary>
/// The content hash that names a Ruleset, and what counts as the same content.
/// </summary>
/// <remarks>
/// <b>This is what lets <c>--strict</c> mean anything.</b> <c>05 §7</c> makes a replay against a
/// different Ruleset a different simulation whose State Hash will diverge — arithmetic rather than a
/// bug — so the runner refuses it. A hash that were wrong in either direction would be worse than no
/// hash at all: too sensitive and every legitimate replay is refused, too blunt and the refusal never
/// fires when it should.
/// </remarks>
public sealed class RulesetHashTests
{
    [Fact]
    public void The_same_content_hashes_the_same()
    {
        Assert.Equal(Hash("rate = 3\n"), Hash("rate = 3\n"));
    }

    [Fact]
    public void Different_content_hashes_differently()
    {
        Assert.NotEqual(Hash("rate = 3\n"), Hash("rate = 4\n"));
    }

    /// <summary>
    /// <b>The check the whole scheme rests on.</b> A Ruleset is text in a repository cloned on
    /// Windows and on Linux. Without this, the same file carries two content hashes depending on the
    /// machine, and <c>--strict</c> refuses to replay a log against the very Ruleset it was recorded
    /// against — a failure invisible in a diff, which would be blamed on the log.
    /// </summary>
    [Fact]
    public void Line_endings_are_not_content()
    {
        Assert.Equal(Hash("a = 1\nb = 2\n"), Hash("a = 1\r\nb = 2\r\n"));
    }

    /// <summary>
    /// Whitespace and comments <em>are</em> content, and that is honest rather than lazy: nothing
    /// here has parsed the Rules, so nothing here can claim a reformatting was semantically empty.
    /// Slice 8 is where a Ruleset is understood well enough to say otherwise.
    /// </summary>
    [Fact]
    public void Whitespace_and_comments_are_content()
    {
        Assert.NotEqual(Hash("rate = 3\n"), Hash("rate  =  3\n"));
        Assert.NotEqual(Hash("rate = 3\n"), Hash("# the rate\nrate = 3\n"));
    }

    /// <summary>
    /// No Ruleset and an empty one are the same value, so that every log written before slice 8 does
    /// not change the day a Ruleset first has content.
    /// </summary>
    [Fact]
    public void The_empty_ruleset_is_the_absent_one()
    {
        Assert.Equal(ContentHash.None, Hash(string.Empty));
    }

    /// <summary>
    /// A lone carriage return is content, because it is not a line ending. Only the pair is stripped
    /// — a rule that treated a bare CR as a newline would silently alter a file containing one.
    /// </summary>
    [Fact]
    public void A_carriage_return_that_is_not_a_line_ending_survives()
    {
        Assert.NotEqual(Hash("a = 1\n"), Hash("a\r = 1\n"));
    }

    /// <summary>
    /// Trailing zero bytes cannot be added or removed without moving the hash — the tail is folded
    /// with its length precisely so that a truncated file cannot hash as an intact one.
    /// </summary>
    [Fact]
    public void Trailing_padding_moves_the_hash()
    {
        Assert.NotEqual(
            ContentHash.Of([1, 2, 3]),
            ContentHash.Of([1, 2, 3, 0]));
    }

    /// <summary>
    /// Content of every length up to two whole words hashes distinctly, which is the boundary the
    /// eight-bytes-at-a-time fold is most likely to get wrong.
    /// </summary>
    [Fact]
    public void Every_length_across_the_word_boundary_is_distinct()
    {
        var seen = new HashSet<ulong>();

        for (int length = 0; length <= 17; length++)
        {
            Assert.True(seen.Add(ContentHash.Of(new byte[length])),
                $"content of {length} zero bytes collides with a shorter one.");
        }
    }

    private static ulong Hash(string content) =>
        RulesetFile.HashOfContent(Encoding.UTF8.GetBytes(content));
}
