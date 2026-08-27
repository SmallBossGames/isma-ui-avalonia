namespace ISMA.App.Automation;

/// <summary>
/// Centralized automation identifiers for all UI controls.
/// Used by integration tests to locate and interact with controls.
/// </summary>
public static class AutomationIds
{
    // MainWindow
    public const string MenuBar = "MenuBar";
    public const string ToolBar = "ToolBar";
    public const string EditorTabPane = "EditorTabPane";
    public const string ErrorList = "ErrorList";
    public const string ProcessBar = "ProcessBar";
    public const string SettingsPanel = "SettingsPanel";

    // Menu items - File
    public const string MenuFile = "MenuFile";
    public const string MenuNewText = "MenuNewText";
    public const string MenuNewBlueprint = "MenuNewBlueprint";
    public const string MenuOpen = "MenuOpen";
    public const string MenuSave = "MenuSave";
    public const string MenuSaveAll = "MenuSaveAll";
    public const string MenuClose = "MenuClose";
    public const string MenuCloseAll = "MenuCloseAll";
    public const string MenuExit = "MenuExit";

    // Menu items - Edit
    public const string MenuEdit = "MenuEdit";
    public const string MenuCut = "MenuCut";
    public const string MenuCopy = "MenuCopy";
    public const string MenuPaste = "MenuPaste";

    // Menu items - Simulation
    public const string MenuSimulation = "MenuSimulation";
    public const string MenuVerify = "MenuVerify";
    public const string MenuRun = "MenuRun";
    public const string MenuStoreSettings = "MenuStoreSettings";
    public const string MenuLoadSettings = "MenuLoadSettings";

    // Toolbar buttons
    public const string ToolBarNewText = "ToolBarNewText";
    public const string ToolBarNewBlueprint = "ToolBarNewBlueprint";
    public const string ToolBarOpen = "ToolBarOpen";
    public const string ToolBarSave = "ToolBarSave";
    public const string ToolBarSaveAll = "ToolBarSaveAll";
    public const string ToolBarCut = "ToolBarCut";
    public const string ToolBarCopy = "ToolBarCopy";
    public const string ToolBarPaste = "ToolBarPaste";
    public const string ToolBarVerify = "ToolBarVerify";
    public const string ToolBarStoreSettings = "ToolBarStoreSettings";
    public const string ToolBarLoadSettings = "ToolBarLoadSettings";

    // Process bar
    public const string ProcessBarRun = "ProcessBarRun";
    public const string ProcessBarTasks = "ProcessBarTasks";

    // Blueprint editor toolbar
    public const string BlueprintNewState = "BlueprintNewState";
    public const string BlueprintAddTransition = "BlueprintAddTransition";
    public const string BlueprintRemoveState = "BlueprintRemoveState";
    public const string BlueprintRemoveTransition = "BlueprintRemoveTransition";

    // Text editor
    public const string TextEditor = "Editor";

    // Settings panel - Cauchy Initials tab
    public const string SettingsCauchyInitialsStartTime = "Settings-CauchyInitials-StartTime";
    public const string SettingsCauchyInitialsEndTime = "Settings-CauchyInitials-EndTime";
    public const string SettingsCauchyInitialsInitialStep = "Settings-CauchyInitials-InitialStep";

    // Settings panel - Integration tab
    public const string SettingsIntegrationMethod = "Settings-IntegrationMethod-SelectedMethod";
    public const string SettingsIntegrationAccuracy = "Settings-IntegrationMethod-Accuracy";

    // Settings panel - Event Detection tab
    public const string SettingsEventDetectionGamma = "Settings-EventDetection-Gamma";
    public const string SettingsEventDetectionLowBorder = "Settings-EventDetection-LowBorder";
    public const string SettingsEventDetectionStepLimit = "Settings-EventDetection-IsStepLimitInUse";

}
