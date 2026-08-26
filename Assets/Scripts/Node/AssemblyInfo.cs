using System.Runtime.CompilerServices;

// Lets EditMode tests set NodeBase.Data directly instead of going through a real ScriptableObject
// wiring flow.
[assembly: InternalsVisibleTo("NestLabs.Tests.EditMode")]
