using System;
using ArenaDefender;

try
{
    using var game = new ArenaDefenderGame();
    game.Run();
}
catch (Exception ex)
{
    // Last-resort handler: a crash during setup or inside the game loop should still print
    // something readable, not just dump an unhandled exception.
    Console.Error.WriteLine("Arena Defender terminated unexpectedly.");
    Console.Error.WriteLine(ex);
    return 1;
}

return 0;
