namespace Borough.Core.Rules;

/// <summary>
/// The Rulesets one session references, by content hash.
/// </summary>
/// <remarks>
/// <para>
/// <b>It exists because <c>Core</c> cannot turn a hash into Rules</b> (<c>02 §1</c>, <c>adr/0048</c>):
/// resolving one would mean reading a file, naming a parser, or both. So the Rulesets a session
/// references are supplied to the Simulation up front, already validated, and a reload is a *lookup*
/// rather than a load. A session that reloads twice is played against three Rulesets, which is the
/// sentence <c>InputLog.cs:131</c> had already written down before anything could act on it.
/// </para>
/// <para>
/// <b>A linear scan over an array rather than a hash map, and the array is the point.</b> A session
/// references two or three Rulesets — a designer who reloaded a hundred times in one run has a
/// hundred-entry array and a lookup nobody will ever measure. What it buys is that there is no
/// enumeration order to get wrong (<c>05 §4</c> lint 3) and nothing to allocate, and that the
/// catalogue reads in the order the session referenced them, which is the order a human debugging a
/// trace thinks in.
/// </para>
/// <para>
/// <b>The first entry is the one the world opened with</b>, and <see cref="Simulation"/> checks that
/// it is the Ruleset the <c>World</c> actually holds. A catalogue whose opening entry disagreed with
/// the world would make the first reload swap *away* from Rules the city had never been running.
/// </para>
/// </remarks>
public sealed class RulesetCatalogue
{
    private readonly ulong[] _hashes;
    private readonly Ruleset[] _rules;

    private RulesetCatalogue(ulong[] hashes, Ruleset[] rules)
    {
        _hashes = hashes;
        _rules = rules;
    }

    /// <summary>
    /// A session that cannot reload: no Ruleset is named, so no transition can be resolved.
    /// </summary>
    /// <remarks>
    /// <b>Empty rather than absent, so the refusal has somewhere to come from.</b> The alternative —
    /// a null catalogue meaning <em>ignore the hash</em> — turns a reload recorded in a log into a
    /// silent no-op, which is the failure <c>adr/0015</c>'s own revisit trigger names: a change that
    /// is neither applied nor refused. A run that meets a transition with this catalogue is told it
    /// was given no Rules to swap to.
    /// </remarks>
    public static RulesetCatalogue None { get; } = new([], []);

    /// <summary>How many Rulesets this session references.</summary>
    public int Count => _hashes.Length;

    /// <summary>The Ruleset the world opened with, or <see cref="Ruleset.Empty"/> for an empty catalogue.</summary>
    public Ruleset Opening => _rules.Length == 0 ? Ruleset.Empty : _rules[0];

    /// <summary>The content hash of <see cref="Opening"/>.</summary>
    public ulong OpeningHash => _hashes.Length == 0 ? 0 : _hashes[0];

    /// <summary>
    /// A catalogue over the Rulesets a session references, opening one first.
    /// </summary>
    /// <param name="hashes">The content hashes, in the order the session references them.</param>
    /// <param name="rules">The Rulesets they name, in the same order.</param>
    public static RulesetCatalogue Of(ulong[] hashes, Ruleset[] rules)
    {
        ArgumentNullException.ThrowIfNull(hashes);
        ArgumentNullException.ThrowIfNull(rules);

        if (hashes.Length != rules.Length)
        {
            throw new ArgumentException(
                $"a catalogue has {hashes.Length} hashes against {rules.Length} Rulesets. Each names "
                + "exactly one.",
                nameof(rules));
        }

        for (int i = 0; i < hashes.Length; i++)
        {
            ArgumentNullException.ThrowIfNull(rules[i]);

            for (int j = 0; j < i; j++)
            {
                if (hashes[j] == hashes[i])
                {
                    // Two entries under one hash is either the same file twice — harmless but a sign
                    // the caller is guessing — or two different Rulesets claiming one identity, which
                    // would make a transition ambiguous and a replay non-deterministic. Neither is
                    // worth resolving by picking the first.
                    throw new ArgumentException(
                        $"two Rulesets in this catalogue carry the content hash 0x{hashes[i]:X16}. A "
                        + "content hash names one Ruleset; a duplicate makes a reload ambiguous.",
                        nameof(hashes));
                }
            }
        }

        return new RulesetCatalogue((ulong[])hashes.Clone(), (Ruleset[])rules.Clone());
    }

    /// <summary>A session that runs one Ruleset and never reloads.</summary>
    public static RulesetCatalogue Fixed(ulong hash, Ruleset rules) => Of([hash], [rules]);

    /// <summary>The Ruleset a content hash names, or false if this session never referenced it.</summary>
    public bool TryResolve(ulong hash, out Ruleset rules)
    {
        for (int i = 0; i < _hashes.Length; i++)
        {
            if (_hashes[i] == hash)
            {
                rules = _rules[i];
                return true;
            }
        }

        rules = Ruleset.Empty;
        return false;
    }
}
