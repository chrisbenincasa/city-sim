using Borough.Core.Quantities;

namespace Borough.Core.Input;

/// <summary>
/// What a <c>street</c> command would do if it applied now: its refusal, the total it pays the
/// displaced and the occupied Buildings it clears, as Building slots.
/// </summary>
/// <remarks>
/// A lay refusal leaves the price at zero and the list empty, because no corridor was measured. A
/// treasury refusal keeps both, so the player sees what they cannot afford.
/// </remarks>
[ColdPath("asked by the shell per hover; no path from Step reaches it.")]
public readonly record struct StreetPreview(Refusal Refusal, Money Price, IReadOnlyList<int> Buildings);
