using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
#endif

namespace PluginHub.Runtime
{
    /// <summary>
    /// 输入系统兼容层，自动适配 Unity 的新旧输入系统
    /// 提供统一的 API 接口，内部根据项目配置自动选择使用新输入系统或旧输入管理器
    /// </summary>
    /// <example>
    /// 使用示例：
    /// <code>
    /// // 检测鼠标左键点击
    /// if (InputEx.GetMouseButtonDown(0))
    /// {
    ///     Debug.Log("鼠标左键被点击");
    /// }
    /// 
    /// // 检测键盘按键
    /// if (InputEx.GetKey(KeyCode.Space))
    /// {
    ///     Debug.Log("空格键被按住");
    /// }
    /// 
    /// // 获取鼠标位置
    /// Vector2 mousePos = InputEx.mousePosition;
    /// </code>
    /// </example>
    public static class InputEx
    {
        #region 鼠标输入

        /// <summary>
        /// 检测鼠标按键是否被按住
        /// </summary>
        /// <param name="button">鼠标按键索引 (0=左键, 1=右键, 2=中键)</param>
        /// <returns>如果按键被按住返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测鼠标左键是否被按住
        /// if (InputEx.GetMouseButton(0))
        /// {
        ///     Debug.Log("鼠标左键被按住");
        /// }
        /// </code>
        /// </example>
        public static bool GetMouseButton(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            
            return button switch
            {
                0 => Mouse.current.leftButton.isPressed,
                1 => Mouse.current.rightButton.isPressed,
                2 => Mouse.current.middleButton.isPressed,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButton(button);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测鼠标按键在当前帧是否被按下
        /// </summary>
        /// <param name="button">鼠标按键索引 (0=左键, 1=右键, 2=中键)</param>
        /// <returns>如果按键在当前帧被按下返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测鼠标左键是否在当前帧被按下
        /// if (InputEx.GetMouseButtonDown(0))
        /// {
        ///     Debug.Log("鼠标左键被点击");
        /// }
        /// </code>
        /// </example>
        public static bool GetMouseButtonDown(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            
            return button switch
            {
                0 => Mouse.current.leftButton.wasPressedThisFrame,
                1 => Mouse.current.rightButton.wasPressedThisFrame,
                2 => Mouse.current.middleButton.wasPressedThisFrame,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonDown(button);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测鼠标按键在当前帧是否被释放
        /// </summary>
        /// <param name="button">鼠标按键索引 (0=左键, 1=右键, 2=中键)</param>
        /// <returns>如果按键在当前帧被释放返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测鼠标左键是否在当前帧被释放
        /// if (InputEx.GetMouseButtonUp(0))
        /// {
        ///     Debug.Log("鼠标左键被释放");
        /// }
        /// </code>
        /// </example>
        public static bool GetMouseButtonUp(int button)
        {
#if ENABLE_INPUT_SYSTEM
            if (Mouse.current == null) return false;
            
            return button switch
            {
                0 => Mouse.current.leftButton.wasReleasedThisFrame,
                1 => Mouse.current.rightButton.wasReleasedThisFrame,
                2 => Mouse.current.middleButton.wasReleasedThisFrame,
                _ => false
            };
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetMouseButtonUp(button);
#else
            return false;
#endif
        }

        /// <summary>
        /// 获取鼠标在屏幕上的位置（像素坐标）
        /// 原点在左下角，坐标范围从 (0,0) 到 (Screen.width, Screen.height)
        /// </summary>
        /// <example>
        /// <code>
        /// // 获取鼠标位置
        /// Vector2 mousePos = InputEx.mousePosition;
        /// Debug.Log($"鼠标位置: {mousePos}");
        /// </code>
        /// </example>
        public static Vector2 mousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return Vector2.zero;
                return Mouse.current.position.ReadValue();
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mousePosition;
#else
                return Vector2.zero;
#endif
            }
        }

        /// <summary>
        /// 获取鼠标滚轮的增量值
        /// Y 轴表示垂直滚动（正值向上，负值向下），X 轴表示水平滚动（如果支持）
        /// </summary>
        /// <example>
        /// <code>
        /// // 检测鼠标滚轮滚动
        /// Vector2 scroll = InputEx.mouseScrollDelta;
        /// if (scroll.y != 0)
        /// {
        ///     Debug.Log($"滚轮滚动: {scroll.y}");
        /// }
        /// </code>
        /// </example>
        public static Vector2 mouseScrollDelta
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Mouse.current == null) return Vector2.zero;
                // 新输入系统返回的是像素值，需要除以120来匹配旧系统的行为
                return Mouse.current.scroll.ReadValue() / 120f;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.mouseScrollDelta;
#else
                return Vector2.zero;
#endif
            }
        }

        #endregion

        #region 键盘输入

        /// <summary>
        /// 检测按键是否被按住
        /// </summary>
        /// <param name="key">按键码</param>
        /// <returns>如果按键被按住返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测空格键是否被按住
        /// if (InputEx.GetKey(KeyCode.Space))
        /// {
        ///     Debug.Log("空格键被按住");
        /// }
        /// </code>
        /// </example>
        public static bool GetKey(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            Key inputKey = KeyCodeToKey(key);
            if (inputKey == Key.None) return false;
            
            return Keyboard.current[inputKey].isPressed;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(key);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测按键在当前帧是否被按下
        /// </summary>
        /// <param name="key">按键码</param>
        /// <returns>如果按键在当前帧被按下返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测空格键是否在当前帧被按下
        /// if (InputEx.GetKeyDown(KeyCode.Space))
        /// {
        ///     Debug.Log("空格键被按下");
        /// }
        /// </code>
        /// </example>
        public static bool GetKeyDown(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            Key inputKey = KeyCodeToKey(key);
            if (inputKey == Key.None) return false;
            
            return Keyboard.current[inputKey].wasPressedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(key);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测按键在当前帧是否被释放
        /// </summary>
        /// <param name="key">按键码</param>
        /// <returns>如果按键在当前帧被释放返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 检测空格键是否在当前帧被释放
        /// if (InputEx.GetKeyUp(KeyCode.Space))
        /// {
        ///     Debug.Log("空格键被释放");
        /// }
        /// </code>
        /// </example>
        public static bool GetKeyUp(KeyCode key)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            Key inputKey = KeyCodeToKey(key);
            if (inputKey == Key.None) return false;
            
            return Keyboard.current[inputKey].wasReleasedThisFrame;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(key);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测按键是否被按住（使用按键名称）
        /// </summary>
        /// <param name="name">按键名称（如 "Space"、"A"、"LeftShift" 等）</param>
        /// <returns>如果按键被按住返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 使用字符串名称检测按键
        /// if (InputEx.GetKey("Space"))
        /// {
        ///     Debug.Log("空格键被按住");
        /// }
        /// </code>
        /// </example>
        public static bool GetKey(string name)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            // 尝试解析按键名称
            if (System.Enum.TryParse<Key>(name, true, out Key key))
            {
                return Keyboard.current[key].isPressed;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKey(name);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测按键在当前帧是否被按下（使用按键名称）
        /// </summary>
        /// <param name="name">按键名称（如 "Space"、"A"、"LeftShift" 等）</param>
        /// <returns>如果按键在当前帧被按下返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 使用字符串名称检测按键按下
        /// if (InputEx.GetKeyDown("Space"))
        /// {
        ///     Debug.Log("空格键被按下");
        /// }
        /// </code>
        /// </example>
        public static bool GetKeyDown(string name)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            // 尝试解析按键名称
            if (System.Enum.TryParse<Key>(name, true, out Key key))
            {
                return Keyboard.current[key].wasPressedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyDown(name);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测按键在当前帧是否被释放（使用按键名称）
        /// </summary>
        /// <param name="name">按键名称（如 "Space"、"A"、"LeftShift" 等）</param>
        /// <returns>如果按键在当前帧被释放返回 true，否则返回 false</returns>
        /// <example>
        /// <code>
        /// // 使用字符串名称检测按键释放
        /// if (InputEx.GetKeyUp("Space"))
        /// {
        ///     Debug.Log("空格键被释放");
        /// }
        /// </code>
        /// </example>
        public static bool GetKeyUp(string name)
        {
#if ENABLE_INPUT_SYSTEM
            if (Keyboard.current == null) return false;
            
            // 尝试解析按键名称
            if (System.Enum.TryParse<Key>(name, true, out Key key))
            {
                return Keyboard.current[key].wasReleasedThisFrame;
            }
            return false;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetKeyUp(name);
#else
            return false;
#endif
        }

        /// <summary>
        /// 检测是否有任意按键或鼠标按钮当前被按住
        /// </summary>
        /// <example>
        /// <code>
        /// // 检测是否有任意输入
        /// if (InputEx.anyKey)
        /// {
        ///     Debug.Log("有按键或鼠标按钮被按住");
        /// }
        /// </code>
        /// </example>
        public static bool anyKey
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.anyKey.isPressed) ||
                       (Mouse.current != null && (Mouse.current.leftButton.isPressed || 
                                                   Mouse.current.rightButton.isPressed || 
                                                   Mouse.current.middleButton.isPressed));
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.anyKey;
#else
                return false;
#endif
            }
        }

        /// <summary>
        /// 检测是否有任意按键或鼠标按钮在当前帧被按下
        /// </summary>
        /// <example>
        /// <code>
        /// // 检测是否有新的输入
        /// if (InputEx.anyKeyDown)
        /// {
        ///     Debug.Log("有按键或鼠标按钮被按下");
        /// }
        /// </code>
        /// </example>
        public static bool anyKeyDown
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                return (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame) ||
                       (Mouse.current != null && (Mouse.current.leftButton.wasPressedThisFrame || 
                                                   Mouse.current.rightButton.wasPressedThisFrame || 
                                                   Mouse.current.middleButton.wasPressedThisFrame));
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.anyKeyDown;
#else
                return false;
#endif
            }
        }

        #endregion

        #region 触摸输入

        /// <summary>
        /// 获取当前触摸点的数量
        /// </summary>
        /// <example>
        /// <code>
        /// // 获取触摸点数量
        /// int count = InputEx.touchCount;
        /// Debug.Log($"当前有 {count} 个触摸点");
        /// </code>
        /// </example>
        public static int touchCount
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Touchscreen.current == null) return 0;
                
                int count = 0;
                foreach (var touch in Touchscreen.current.touches)
                {
                    if (touch.isInProgress)
                        count++;
                }
                return count;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.touchCount;
#else
                return 0;
#endif
            }
        }

        /// <summary>
        /// 获取指定索引的触摸信息
        /// </summary>
        /// <param name="index">触摸点索引（从 0 开始）</param>
        /// <returns>触摸信息结构体</returns>
        /// <example>
        /// <code>
        /// // 获取第一个触摸点的信息
        /// if (InputEx.touchCount > 0)
        /// {
        ///     Touch touch = InputEx.GetTouch(0);
        ///     Debug.Log($"触摸位置: {touch.position}, 阶段: {touch.phase}");
        /// }
        /// </code>
        /// </example>
        public static Touch GetTouch(int index)
        {
#if ENABLE_INPUT_SYSTEM
            if (Touchscreen.current == null || index < 0 || index >= Touchscreen.current.touches.Count)
                return new Touch();
            
            var touchControl = Touchscreen.current.touches[index];
            
            // 转换新输入系统的触摸数据到旧系统的 Touch 结构
            Touch touch = new Touch
            {
                fingerId = touchControl.touchId.ReadValue(),
                position = touchControl.position.ReadValue(),
                deltaPosition = touchControl.delta.ReadValue(),
                deltaTime = Time.deltaTime,
                tapCount = touchControl.tapCount.ReadValue(),
                phase = ConvertTouchPhase(touchControl.phase.ReadValue())
            };
            
            return touch;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetTouch(index);
#else
            return new Touch();
#endif
        }

        /// <summary>
        /// 获取所有触摸点的数组
        /// </summary>
        /// <example>
        /// <code>
        /// // 遍历所有触摸点
        /// Touch[] allTouches = InputEx.touches;
        /// foreach (Touch touch in allTouches)
        /// {
        ///     Debug.Log($"触摸 ID: {touch.fingerId}, 位置: {touch.position}");
        /// }
        /// </code>
        /// </example>
        public static Touch[] touches
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                if (Touchscreen.current == null) return new Touch[0];
                
                int count = touchCount;
                Touch[] result = new Touch[count];
                for (int i = 0; i < count; i++)
                {
                    result[i] = GetTouch(i);
                }
                return result;
#elif ENABLE_LEGACY_INPUT_MANAGER
                return Input.touches;
#else
                return new Touch[0];
#endif
            }
        }

        #endregion

        #region 虚拟轴

        /// <summary>
        /// 获取虚拟轴的值（带平滑处理）
        /// 对于标准轴（Horizontal、Vertical），新输入系统会直接读取 WASD/方向键
        /// </summary>
        /// <param name="axisName">轴名称（如 "Horizontal"、"Vertical"、"Mouse X"、"Mouse Y"）</param>
        /// <returns>轴的值，范围通常在 -1 到 1 之间</returns>
        /// <example>
        /// <code>
        /// // 获取水平和垂直输入
        /// float horizontal = InputEx.GetAxis("Horizontal");
        /// float vertical = InputEx.GetAxis("Vertical");
        /// transform.Translate(new Vector3(horizontal, 0, vertical) * speed * Time.deltaTime);
        /// </code>
        /// </example>
        public static float GetAxis(string axisName)
        {
#if ENABLE_INPUT_SYSTEM
            // 对于标准轴，使用新输入系统的直接读取
            if (Keyboard.current != null)
            {
                if (axisName == "Horizontal")
                {
                    float value = 0;
                    if (Keyboard.current.aKey.isPressed) value -= 1;
                    if (Keyboard.current.dKey.isPressed) value += 1;
                    if (Keyboard.current.leftArrowKey.isPressed) value -= 1;
                    if (Keyboard.current.rightArrowKey.isPressed) value += 1;
                    return Mathf.Clamp(value, -1f, 1f);
                }
                else if (axisName == "Vertical")
                {
                    float value = 0;
                    if (Keyboard.current.sKey.isPressed) value -= 1;
                    if (Keyboard.current.wKey.isPressed) value += 1;
                    if (Keyboard.current.downArrowKey.isPressed) value -= 1;
                    if (Keyboard.current.upArrowKey.isPressed) value += 1;
                    return Mathf.Clamp(value, -1f, 1f);
                }
                else if (axisName == "Mouse X")
                {
                    return Mouse.current != null ? Mouse.current.delta.x.ReadValue() : 0f;
                }
                else if (axisName == "Mouse Y")
                {
                    return Mouse.current != null ? Mouse.current.delta.y.ReadValue() : 0f;
                }
            }
            return 0f;
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxis(axisName);
#else
            return 0f;
#endif
        }

        /// <summary>
        /// 获取虚拟轴的原始值（不带平滑处理）
        /// </summary>
        /// <param name="axisName">轴名称（如 "Horizontal"、"Vertical"）</param>
        /// <returns>轴的原始值，通常为 -1、0 或 1</returns>
        /// <example>
        /// <code>
        /// // 获取原始输入（无平滑）
        /// float horizontalRaw = InputEx.GetAxisRaw("Horizontal");
        /// if (horizontalRaw > 0)
        ///     Debug.Log("向右移动");
        /// else if (horizontalRaw < 0)
        ///     Debug.Log("向左移动");
        /// </code>
        /// </example>
        public static float GetAxisRaw(string axisName)
        {
#if ENABLE_INPUT_SYSTEM
            // GetAxisRaw 和 GetAxis 在新输入系统中实现相同
            // 因为我们直接读取按键状态，没有平滑处理
            return GetAxis(axisName);
#elif ENABLE_LEGACY_INPUT_MANAGER
            return Input.GetAxisRaw(axisName);
#else
            return 0f;
#endif
        }

        #endregion

        #region 私有辅助方法

#if ENABLE_INPUT_SYSTEM
        /// <summary>
        /// 将旧输入系统的 KeyCode 转换为新输入系统的 Key
        /// </summary>
        private static Key KeyCodeToKey(KeyCode keyCode)
        {
            return keyCode switch
            {
                // 字母键
                KeyCode.A => Key.A,
                KeyCode.B => Key.B,
                KeyCode.C => Key.C,
                KeyCode.D => Key.D,
                KeyCode.E => Key.E,
                KeyCode.F => Key.F,
                KeyCode.G => Key.G,
                KeyCode.H => Key.H,
                KeyCode.I => Key.I,
                KeyCode.J => Key.J,
                KeyCode.K => Key.K,
                KeyCode.L => Key.L,
                KeyCode.M => Key.M,
                KeyCode.N => Key.N,
                KeyCode.O => Key.O,
                KeyCode.P => Key.P,
                KeyCode.Q => Key.Q,
                KeyCode.R => Key.R,
                KeyCode.S => Key.S,
                KeyCode.T => Key.T,
                KeyCode.U => Key.U,
                KeyCode.V => Key.V,
                KeyCode.W => Key.W,
                KeyCode.X => Key.X,
                KeyCode.Y => Key.Y,
                KeyCode.Z => Key.Z,
                
                // 数字键
                KeyCode.Alpha0 => Key.Digit0,
                KeyCode.Alpha1 => Key.Digit1,
                KeyCode.Alpha2 => Key.Digit2,
                KeyCode.Alpha3 => Key.Digit3,
                KeyCode.Alpha4 => Key.Digit4,
                KeyCode.Alpha5 => Key.Digit5,
                KeyCode.Alpha6 => Key.Digit6,
                KeyCode.Alpha7 => Key.Digit7,
                KeyCode.Alpha8 => Key.Digit8,
                KeyCode.Alpha9 => Key.Digit9,
                
                // 小键盘
                KeyCode.Keypad0 => Key.Numpad0,
                KeyCode.Keypad1 => Key.Numpad1,
                KeyCode.Keypad2 => Key.Numpad2,
                KeyCode.Keypad3 => Key.Numpad3,
                KeyCode.Keypad4 => Key.Numpad4,
                KeyCode.Keypad5 => Key.Numpad5,
                KeyCode.Keypad6 => Key.Numpad6,
                KeyCode.Keypad7 => Key.Numpad7,
                KeyCode.Keypad8 => Key.Numpad8,
                KeyCode.Keypad9 => Key.Numpad9,
                KeyCode.KeypadDivide => Key.NumpadDivide,
                KeyCode.KeypadMultiply => Key.NumpadMultiply,
                KeyCode.KeypadMinus => Key.NumpadMinus,
                KeyCode.KeypadPlus => Key.NumpadPlus,
                KeyCode.KeypadEnter => Key.NumpadEnter,
                KeyCode.KeypadPeriod => Key.NumpadPeriod,
                
                // 功能键
                KeyCode.F1 => Key.F1,
                KeyCode.F2 => Key.F2,
                KeyCode.F3 => Key.F3,
                KeyCode.F4 => Key.F4,
                KeyCode.F5 => Key.F5,
                KeyCode.F6 => Key.F6,
                KeyCode.F7 => Key.F7,
                KeyCode.F8 => Key.F8,
                KeyCode.F9 => Key.F9,
                KeyCode.F10 => Key.F10,
                KeyCode.F11 => Key.F11,
                KeyCode.F12 => Key.F12,
                
                // 方向键
                KeyCode.UpArrow => Key.UpArrow,
                KeyCode.DownArrow => Key.DownArrow,
                KeyCode.LeftArrow => Key.LeftArrow,
                KeyCode.RightArrow => Key.RightArrow,
                
                // 修饰键
                KeyCode.LeftShift => Key.LeftShift,
                KeyCode.RightShift => Key.RightShift,
                KeyCode.LeftControl => Key.LeftCtrl,
                KeyCode.RightControl => Key.RightCtrl,
                KeyCode.LeftAlt => Key.LeftAlt,
                KeyCode.RightAlt => Key.RightAlt,
                KeyCode.LeftCommand => Key.LeftCommand,
                KeyCode.RightCommand => Key.RightCommand,
                KeyCode.LeftWindows => Key.LeftWindows,
                KeyCode.RightWindows => Key.RightWindows,
                
                // 特殊键
                KeyCode.Space => Key.Space,
                KeyCode.Return => Key.Enter,
                KeyCode.Escape => Key.Escape,
                KeyCode.Backspace => Key.Backspace,
                KeyCode.Tab => Key.Tab,
                KeyCode.CapsLock => Key.CapsLock,
                KeyCode.Delete => Key.Delete,
                KeyCode.Insert => Key.Insert,
                KeyCode.Home => Key.Home,
                KeyCode.End => Key.End,
                KeyCode.PageUp => Key.PageUp,
                KeyCode.PageDown => Key.PageDown,
                
                // 符号键
                KeyCode.Minus => Key.Minus,
                KeyCode.Equals => Key.Equals,
                KeyCode.LeftBracket => Key.LeftBracket,
                KeyCode.RightBracket => Key.RightBracket,
                KeyCode.Backslash => Key.Backslash,
                KeyCode.Semicolon => Key.Semicolon,
                KeyCode.Quote => Key.Quote,
                KeyCode.Comma => Key.Comma,
                KeyCode.Period => Key.Period,
                KeyCode.Slash => Key.Slash,
                KeyCode.BackQuote => Key.Backquote,
                
                // 其他
                KeyCode.Print => Key.PrintScreen,
                KeyCode.ScrollLock => Key.ScrollLock,
                KeyCode.Pause => Key.Pause,
                KeyCode.Numlock => Key.NumLock,
                
                // 默认
                _ => Key.None
            };
        }

        /// <summary>
        /// 转换新输入系统的触摸阶段到旧系统的 TouchPhase
        /// </summary>
        private static UnityEngine.TouchPhase ConvertTouchPhase(UnityEngine.InputSystem.TouchPhase newPhase)
        {
            return newPhase switch
            {
                UnityEngine.InputSystem.TouchPhase.Began => UnityEngine.TouchPhase.Began,
                UnityEngine.InputSystem.TouchPhase.Moved => UnityEngine.TouchPhase.Moved,
                UnityEngine.InputSystem.TouchPhase.Stationary => UnityEngine.TouchPhase.Stationary,
                UnityEngine.InputSystem.TouchPhase.Ended => UnityEngine.TouchPhase.Ended,
                UnityEngine.InputSystem.TouchPhase.Canceled => UnityEngine.TouchPhase.Canceled,
                _ => UnityEngine.TouchPhase.Canceled
            };
        }
#endif

        #endregion
    }
}
