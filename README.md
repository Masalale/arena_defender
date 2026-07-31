# Arena Defender

A 2D survival arena shooter written in C# with MonoGame for .NET 8. Waves of enemies
arrive from every edge of the arena, faster and harder the further you get, and you
hold the arena for as long as you can. The run ends when your last life is gone.

## Controls

- Move: `W` `A` `S` `D` or the arrow keys
- Aim: move the mouse, or aim in the direction you are moving
- Fire: left mouse button
- Pause and resume: `Escape`
- Start a run, or play again: `Enter`
- Pick a pause menu item: `↑` `↓`, then `Enter`

## Build and run

Requires the .NET 8 SDK or newer. Nothing else.

```bash
dotnet run --project src/ArenaDefender.Game
```

To run the tests:

```bash
dotnet test
```

## Assets

- Background: https://opengameart.org/content/space-backgrounds-7
- Sprites: https://kenney.nl/assets/simple-space

Both are CC0, so no attribution is required.
