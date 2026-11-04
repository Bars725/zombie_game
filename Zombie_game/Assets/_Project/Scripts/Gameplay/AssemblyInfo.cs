using System.Runtime.CompilerServices;

// Tests and editor tooling may call internal setup methods (e.g. WeaponDefinition.Initialize).
[assembly: InternalsVisibleTo("Game.Tests.EditMode")]
[assembly: InternalsVisibleTo("Game.EditorTools")]
