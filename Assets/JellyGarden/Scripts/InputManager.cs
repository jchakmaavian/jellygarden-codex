using UnityEngine;
using UnityEngine.InputSystem;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance
    {
        get
        {
            if (instance == null)
            {
                GameObject go = new GameObject("InputManager");
                instance = go.AddComponent<InputManager>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }
    
    public InputActionAsset inputActionAsset;
    private InputActionMap gameplayMap;
    private InputActionMap mapMap;
    private InputActionMap debugMap;
    
    private InputAction primaryTouchAction;
    private InputAction positionAction;
    private InputAction cancelAction;
    private InputAction cameraPanAction;
    private InputAction panPositionAction;
    
    private Vector2 lastPosition;
    private bool wasTouchPressed;
    private bool wasTouchReleased;
    private bool isTouching;
    
    public bool IsTouching => isTouching;
    public Vector2 TouchPosition => lastPosition;
    public bool WasTouchPressed => wasTouchPressed;
    public bool WasTouchReleased => wasTouchReleased;
    
    public class GameplayActions
    {
        private InputAction primaryTouch;
        private InputAction position;
        private InputAction cancel;
        
        public GameplayActions(InputAction primaryTouch, InputAction position, InputAction cancel)
        {
            this.primaryTouch = primaryTouch;
            this.position = position;
            this.cancel = cancel;
        }
        
        public InputAction PrimaryTouch => primaryTouch;
        public InputAction Position => position;
        public InputAction Cancel => cancel;
    }
    
    public class MapActions
    {
        private InputAction cameraPan;
        private InputAction panPosition;
        
        public MapActions(InputAction cameraPan, InputAction panPosition)
        {
            this.cameraPan = cameraPan;
            this.panPosition = panPosition;
        }
        
        public InputAction CameraPan => cameraPan;
        public InputAction PanPosition => panPosition;
    }
    
    public class DebugActions
    {
        private InputAction skip;
        private InputAction win;
        private InputAction lose;
        private InputAction reload;
        private InputAction quit;
        private InputAction debugToggle;
        
        public DebugActions(InputAction skip, InputAction win, InputAction lose, InputAction reload, InputAction quit, InputAction debugToggle)
        {
            this.skip = skip;
            this.win = win;
            this.lose = lose;
            this.reload = reload;
            this.quit = quit;
            this.debugToggle = debugToggle;
        }
        
        public InputAction Skip => skip;
        public InputAction Win => win;
        public InputAction Lose => lose;
        public InputAction Reload => reload;
        public InputAction Quit => quit;
        public InputAction DebugToggle => debugToggle;
    }
    
    public GameplayActions Gameplay { get; private set; }
    public MapActions Map { get; private set; }
    public DebugActions Debug { get; private set; }
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        InitializeInputActions();
    }
    
    private void InitializeInputActions()
    {
        if (inputActionAsset == null)
        {
            inputActionAsset = Resources.Load<InputActionAsset>("InputActions");
        }
        
        if (inputActionAsset == null)
        {
            UnityEngine.Debug.LogError("InputActions asset not found in Resources folder!");
            return;
        }
        
        gameplayMap = inputActionAsset.FindActionMap("Gameplay");
        mapMap = inputActionAsset.FindActionMap("Map");
        debugMap = inputActionAsset.FindActionMap("Debug");
        
        if (gameplayMap == null || mapMap == null || debugMap == null)
        {
            UnityEngine.Debug.LogError("One or more action maps not found in InputActions asset!");
            return;
        }
        
        primaryTouchAction = gameplayMap.FindAction("PrimaryTouch");
        positionAction = gameplayMap.FindAction("Position");
        cancelAction = gameplayMap.FindAction("Cancel");
        
        cameraPanAction = mapMap.FindAction("CameraPan");
        panPositionAction = mapMap.FindAction("PanPosition");
        
        if (primaryTouchAction == null || positionAction == null || cancelAction == null || 
            cameraPanAction == null || panPositionAction == null)
        {
            UnityEngine.Debug.LogError("One or more actions not found in action maps!");
            return;
        }
        
        Gameplay = new GameplayActions(primaryTouchAction, positionAction, cancelAction);
        Map = new MapActions(cameraPanAction, panPositionAction);
        
        var skipAction = debugMap.FindAction("Skip");
        var winAction = debugMap.FindAction("Win");
        var loseAction = debugMap.FindAction("Lose");
        var reloadAction = debugMap.FindAction("Reload");
        var quitAction = debugMap.FindAction("Quit");
        var debugToggleAction = debugMap.FindAction("DebugToggle");
        
        if (skipAction == null || winAction == null || loseAction == null || 
            reloadAction == null || quitAction == null || debugToggleAction == null)
        {
            UnityEngine.Debug.LogError("One or more debug actions not found!");
            return;
        }
        
        Debug = new DebugActions(skipAction, winAction, loseAction, reloadAction, quitAction, debugToggleAction);
        
        primaryTouchAction.performed += OnTouchPerformed;
        primaryTouchAction.canceled += OnTouchCanceled;
        positionAction.performed += OnPositionChanged;
        
        cameraPanAction.performed += OnPanPerformed;
        cameraPanAction.canceled += OnPanCanceled;
        panPositionAction.performed += OnPanPositionChanged;
    }
    
    private void OnEnable()
    {
        if (gameplayMap != null)
            gameplayMap.Enable();
        if (mapMap != null)
            mapMap.Enable();
        if (debugMap != null)
            debugMap.Enable();
    }
    
    private void OnDisable()
    {
        if (gameplayMap != null)
            gameplayMap.Disable();
        if (mapMap != null)
            mapMap.Disable();
        if (debugMap != null)
            debugMap.Disable();
    }
    
    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
        
        if (primaryTouchAction != null)
        {
            primaryTouchAction.performed -= OnTouchPerformed;
            primaryTouchAction.canceled -= OnTouchCanceled;
        }
        if (positionAction != null)
        {
            positionAction.performed -= OnPositionChanged;
        }
        if (cameraPanAction != null)
        {
            cameraPanAction.performed -= OnPanPerformed;
            cameraPanAction.canceled -= OnPanCanceled;
        }
        if (panPositionAction != null)
        {
            panPositionAction.performed -= OnPanPositionChanged;
        }
    }
    
    private void LateUpdate()
    {
        wasTouchPressed = false;
        wasTouchReleased = false;
    }
    
    private void OnTouchPerformed(InputAction.CallbackContext context)
    {
        isTouching = true;
        wasTouchPressed = true;
        // The press and the position are separate actions and the Input System
        // does not promise which one it dispatches first. When the press lands
        // first, lastPosition is still the previous touch's — so whatever reads
        // TouchPosition on the press frame anchors to the wrong point. Reading
        // the position action here makes the press frame correct either way.
        if (positionAction != null)
            lastPosition = positionAction.ReadValue<Vector2>();
    }
    
    private void OnTouchCanceled(InputAction.CallbackContext context)
    {
        isTouching = false;
        wasTouchReleased = true;
    }
    
    private void OnPositionChanged(InputAction.CallbackContext context)
    {
        lastPosition = context.ReadValue<Vector2>();
    }
    
    private void OnPanPerformed(InputAction.CallbackContext context)
    {
        isTouching = true;
        wasTouchPressed = true;
        // Same ordering problem as OnTouchPerformed, on the map's pan action.
        if (panPositionAction != null)
            lastPosition = panPositionAction.ReadValue<Vector2>();
    }
    
    private void OnPanCanceled(InputAction.CallbackContext context)
    {
        isTouching = false;
        wasTouchReleased = true;
    }
    
    private void OnPanPositionChanged(InputAction.CallbackContext context)
    {
        lastPosition = context.ReadValue<Vector2>();
    }
}
