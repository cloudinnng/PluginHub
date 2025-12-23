using UnityEngine;

namespace PluginHub.Runtime.Runtime
{
    public static class PluginHubRuntime
    {
        public static bool IsCtrlPressed => (Event.current != null) && (Event.current.control || (Event.current.modifiers & EventModifiers.Control) != 0);
        public static bool IsShiftPressed => (Event.current != null) && (Event.current.shift || (Event.current.modifiers & EventModifiers.Shift) != 0);
        public static bool IsAltPressed => (Event.current != null) && (Event.current.alt || (Event.current.modifiers & EventModifiers.Alt) != 0);
    }
}