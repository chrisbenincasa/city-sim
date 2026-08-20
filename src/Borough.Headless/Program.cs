namespace Borough.Headless;

/// <summary>
/// The headless runner: run a session, print its State Hash trace.
/// </summary>
/// <remarks>
/// <para>
/// <b>This is the primary interface for the whole of Phase 1 and most of Phase 2</b>, and it is the
/// project most likely to be dismissed as a nicety and the one that decides whether this simulation
/// ever gets balanced. A city-builder is tuned by running it thousands of times and reading numbers;
/// there is no version of that which goes through a renderer.
/// </para>
/// <para>
/// <b>Every string here is the shell's.</b> <c>adr/0002</c>'s rule is that <c>Borough.Core</c>
/// returns ids and numbers and the shell owns every string a human reads — and the real leak vector
/// was never <c>using Godot;</c>, it is a method that returns a formatted string because a panel
/// wanted one. The one exception is the file formats, which live in <c>Borough.Formats</c> because
/// both shells must agree on them (<c>adr/0039</c>) and neither may hand-roll a log line.
/// </para>
/// <para>
/// <b>It must never require Godot to be installed</b>, which is the cheapest continuous check that
/// the boundary still holds.
/// </para>
/// </remarks>
internal static class Program
{
    private static int Main(string[] arguments)
    {
        if (!Options.TryParse(arguments, out Options options, out string? complaint))
        {
            if (!string.IsNullOrEmpty(complaint))
            {
                Console.Error.WriteLine(complaint);
                Console.Error.WriteLine();
            }

            Console.Error.WriteLine(Options.Usage);
            return string.IsNullOrEmpty(complaint) ? 0 : 2;
        }

        try
        {
            switch (options.Mode)
            {
                case Mode.Run:
                    return Session.Run(options);

                case Mode.Layer:
                    return Session.DumpLayer(options);

                case Mode.Zones:
                    return Session.DumpZones(options);

                case Mode.Roads:
                    return Session.DumpRoads(options);

                case Mode.Trips:
                    return Session.DumpTrips(options);

                case Mode.Commute:
                    return Session.DumpCommute(options);

                case Mode.Traffic:
                    return Session.DumpTraffic(options);

                case Mode.Evidence:
                    return Session.DumpEvidence(options);

                case Mode.Money:
                    return Session.DumpMoney(options);

                case Mode.Parking:
                    return Session.DumpParking(options);

                case Mode.LandValue:
                    return Session.DumpLandValue(options);

                case Mode.Report:
                default:
                    return Report.Print(options);
            }
        }
        catch (Exception failure) when (failure is IOException
                                            or FormatException
                                            or UnauthorizedAccessException)
        {
            // The three ways an artefact on disk disappoints: it is not there, it is not readable,
            // or it is not what it says it is. None of them is a defect in the simulation, and a
            // stack trace would suggest otherwise to whoever is reading the output.
            Console.Error.WriteLine(failure.Message);
            return 1;
        }
    }
}
