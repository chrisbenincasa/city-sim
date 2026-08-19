namespace Borough.Tests.Corpus;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;

/// <summary>
/// The tiers that are declared are ones that exist, and instruments stay the small half.
/// </summary>
/// <remarks>
/// <para>
/// ⚠ <b>This does NOT require every test to declare a tier, and the omission is the decision.</b>
/// <c>plans/0032</c> asks that an undeclared tier must not ship, on the ground that <em>a tier
/// nothing checks is a per-test status stored in a second place</em> — <c>plans/0012</c>
/// <b>Cause 1</b>. That reasoning is right about the risk and wrong about the remedy here, because
/// there is a **sane default**: absence means <see cref="Tier.Assertion"/>, and an assertion is held
/// to <see cref="TierBudget"/> automatically. So an undeclared test is not an unchecked test — it is
/// the *most* checked one. ***A default that the guard applies is not a second copy of a status; a
/// default that the guard skips is.***
/// </para>
/// <para>
/// <b>What the exhaustive form would have cost is the argument against it.</b> 142 test files
/// declaring a tier today, and every future file for ever, to protect against a case
/// <see cref="TierBudgetTests"/> already catches by timing. The friction is paid on every file and
/// the protection is duplicated — which is <c>adr/0018</c>'s test for bespoke infrastructure applied
/// to a convention rather than to a library.
/// </para>
/// <para>
/// <b>What survives is the half a budget cannot do</b>: a tier that is declared must be one of the
/// two that exist, and instruments must stay a minority. Both are properties of the labelling rather
/// than of the clock.
/// </para>
/// </remarks>
public sealed class TierDeclarationTests
{
    /// <summary>
    /// There are exactly two tiers, and every declaration names one of them.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A third tier is a duration bucket wearing a purpose's clothes</b>, which is the axis
    /// <c>plans/0032</c> refused: <em>small/medium/large describes the symptom.</em> A typo is the
    /// other thing this catches, and it is the likelier one — <c>[Trait("tier", "instruments")]</c>
    /// silently filters into neither tier, so the test runs in the fast lane while its author
    /// believes it does not.
    /// </remarks>
    [Fact]
    [Trait(Tier.Key, Tier.Assertion)]
    public void Every_declared_tier_is_one_that_exists()
    {
        List<string> unknown = [];

        foreach (MethodInfo method in TestMethods())
        {
            foreach (string tier in TiersOf(method).Where(t => t is not (Tier.Assertion or Tier.Instrument)))
            {
                unknown.Add($"{method.DeclaringType!.Name}.{method.Name} declares '{tier}'");
            }
        }

        Assert.True(
            unknown.Count == 0,
            "There are exactly two tiers, assertion and instrument (Tier.cs). A third is a duration "
            + "bucket wearing a purpose's clothes, which is the axis plans/0032 refused:\n  "
            + string.Join("\n  ", unknown.Take(25)));
    }

    /// <summary>
    /// Instruments are a small minority, which is what makes the split worth having.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>A ratio rather than a count, and a tripwire rather than a target.</b> If instruments ever
    /// became a large share, the fast filter would stop being fast and the tiering would have quietly
    /// stopped working — with every test green, because a tier is not a correctness property.
    /// ***A split that nothing bounds converges on being no split.*** The bound is loose on purpose:
    /// it asserts the shape has not inverted, and does not choose a number.
    /// </remarks>
    [Fact]
    [Trait(Tier.Key, Tier.Assertion)]
    public void Instruments_are_the_small_half()
    {
        MethodInfo[] all = [.. TestMethods()];
        int instruments = all.Count(method => TiersOf(method).Contains(Tier.Instrument));

        Assert.True(
            instruments * 4 < all.Length,
            $"{instruments} of {all.Length} tests are instruments. Past a quarter the assertion "
            + "filter stops being the fast one and the tiering has stopped paying for itself.");
    }

    /// <summary>Every <c>[Fact]</c> and <c>[Theory]</c> in this assembly.</summary>
    private static IEnumerable<MethodInfo> TestMethods() =>
        typeof(TierDeclarationTests).Assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false })
            .SelectMany(type => type.GetMethods(BindingFlags.Public | BindingFlags.Instance))
            .Where(method =>
                method.GetCustomAttributes<FactAttribute>(inherit: true).Any()
                || method.GetCustomAttributes<TheoryAttribute>(inherit: true).Any());

    /// <summary>
    /// The tiers on a test, method first and class second.
    /// </summary>
    /// <remarks>
    /// <b>A method's declaration wins outright rather than merging with its class's</b>, because the
    /// case this exists for is one instrument inside a class of assertions — and a union would give
    /// that method <em>both</em> tiers, putting it in the fast filter and defeating the point.
    /// </remarks>
    private static string[] TiersOf(MethodInfo method)
    {
        string[] onMethod = Values(method.GetCustomAttributesData());

        return onMethod.Length > 0
            ? onMethod
            : Values(method.DeclaringType!.GetCustomAttributesData());
    }

    /// <summary>
    /// The <c>tier</c> values in a member's <c>[Trait]</c> attributes.
    /// </summary>
    /// <remarks>
    /// ⚠ <b>Read off <see cref="CustomAttributeData"/> rather than off an instantiated attribute,
    /// and that is required rather than stylistic.</b> xUnit 2's <c>TraitAttribute</c> takes its name
    /// and value as constructor arguments and <b>stores neither</b> — there is no <c>Name</c> or
    /// <c>Value</c> property to read, so the obvious reflection returns null for every trait in the
    /// assembly and this guard would pass by finding nothing rather than by finding everything.
    /// ***A check that reads the wrong surface reports the same green as a check that passes.***
    /// </remarks>
    private static string[] Values(IEnumerable<CustomAttributeData> attributes) =>
        [.. attributes
            .Where(attribute => typeof(TraitAttribute).IsAssignableFrom(attribute.AttributeType))
            .Where(attribute => attribute.ConstructorArguments.Count == 2)
            .Where(attribute => attribute.ConstructorArguments[0].Value as string == Tier.Key)
            .Select(attribute => attribute.ConstructorArguments[1].Value as string)
            .Where(value => value is not null)
            .Select(value => value!)];
}
