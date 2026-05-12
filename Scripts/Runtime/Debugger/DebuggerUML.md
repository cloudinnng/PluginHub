```mermaid
classDiagram
    class IDebuggerWindow {
        +void OnStart()
        +void OnDraw()
    }

    IDebuggerWindow <|-- ConsoleWindow
    IDebuggerWindow <|-- ScrollableDebuggerWindowBase
    ScrollableDebuggerWindowBase <|-- InfoWindow
    ScrollableDebuggerWindowBase <|-- CustomWindow
    ScrollableDebuggerWindowBase <|-- UtilitiesWindow
    MonoBehaviour <|-- Debugger
```