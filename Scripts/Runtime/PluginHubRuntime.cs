using System;
using System.Collections;
using UnityEngine;

namespace PluginHub.Runtime
{
    public static class PluginHubRuntime
    {
        public static bool IsCtrlPressed => (Event.current != null) && (Event.current.control || (Event.current.modifiers & EventModifiers.Control) != 0);
        public static bool IsShiftPressed => (Event.current != null) && (Event.current.shift || (Event.current.modifiers & EventModifiers.Shift) != 0);
        public static bool IsAltPressed => (Event.current != null) && (Event.current.alt || (Event.current.modifiers & EventModifiers.Alt) != 0);

        public static IEnumerator DelayAction(float delay, Action action)
        {
            yield return new WaitForSeconds(delay);
            action?.Invoke();
        }
    }
}