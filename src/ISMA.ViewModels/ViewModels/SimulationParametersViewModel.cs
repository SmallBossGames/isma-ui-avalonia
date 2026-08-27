using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Contracts;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

public delegate void SetMethodsAction(IReadOnlyList<string> methods);

public partial class SimulationParametersViewModel : ObservableObject
{
    [ObservableProperty]
    private CauchyInitialsViewModel _cauchyInitials = new();

    [ObservableProperty]
    private MethodSettingsViewModel _integrationMethod = new();

    [ObservableProperty]
    private EventDetectionViewModel _eventDetection = new();

    [ObservableProperty]
    private ResultSavingViewModel _resultSaving = new();

    private readonly ISimulationServerFacade? _serverFacade;
    private SetMethodsAction? _setMethodsAction;

    private ObservableCollection<string> _integrationMethods = new();

    public ObservableCollection<string> IntegrationMethods
    {
        get => _integrationMethods;
        set => SetProperty(ref _integrationMethods, value);
    }

    public void SetSetMethodsAction(SetMethodsAction action)
    {
        _setMethodsAction = action;
        LoadSimulationMethodsAsync();
    }

    public SimulationParametersViewModel(ISimulationServerFacade? serverFacade = null)
    {
        _serverFacade = serverFacade;
    }

    public SimulationParametersViewModel(ISimulationServerFacade? serverFacade, SimulationParameters parameters)
    {
        _serverFacade = serverFacade;
        CauchyInitials = new CauchyInitialsViewModel
        {
            StartTime = parameters.CauchyInitials.StartTime,
            EndTime = parameters.CauchyInitials.EndTime,
            InitialStep = parameters.CauchyInitials.InitialStep
        };

        IntegrationMethod = new MethodSettingsViewModel
        {
            SelectedMethod = parameters.IntegrationMethod.SelectedMethod,
            Accuracy = parameters.IntegrationMethod.Accuracy,
            IsAccuracyInUse = parameters.IntegrationMethod.IsAccuracyInUse,
            IsStableAllowedInUse = parameters.IntegrationMethod.IsStableAllowedInUse,
            IsStableInUse = parameters.IntegrationMethod.IsStableInUse,
            IsParallelInUse = parameters.IntegrationMethod.IsParallelInUse,
            Server = parameters.IntegrationMethod.Server,
            Port = parameters.IntegrationMethod.Port
        };

        EventDetection = new EventDetectionViewModel
        {
            IsEventDetectionInUse = parameters.EventDetection.IsEventDetectionInUse,
            IsStepLimitInUse = parameters.EventDetection.IsStepLimitInUse,
            Gamma = parameters.EventDetection.Gamma,
            LowBorder = parameters.EventDetection.LowBorder
        };

        ResultSaving = new ResultSavingViewModel
        {
            SavingTarget = parameters.ResultSaving.SavingTarget
        };
    }

    private void LoadSimulationMethodsAsync()
    {
        if (_serverFacade == null || _setMethodsAction == null)
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                var methods = await _serverFacade.GetSimulationMethods();
                _setMethodsAction(methods);
            }
            catch
            {
                // Server not available
            }
        });
    }

    public SimulationParameters Snapshot()
    {
        return new SimulationParameters
        {
            CauchyInitials = new CauchyInitials
            {
                StartTime = CauchyInitials.StartTime,
                EndTime = CauchyInitials.EndTime,
                InitialStep = CauchyInitials.InitialStep
            },
            IntegrationMethod = new IntegrationMethodParameters
            {
                SelectedMethod = IntegrationMethod.SelectedMethod,
                Accuracy = IntegrationMethod.Accuracy,
                IsAccuracyInUse = IntegrationMethod.IsAccuracyInUse,
                IsStableAllowedInUse = IntegrationMethod.IsStableAllowedInUse,
                IsStableInUse = IntegrationMethod.IsStableInUse,
                IsParallelInUse = IntegrationMethod.IsParallelInUse,
                Server = IntegrationMethod.Server,
                Port = IntegrationMethod.Port
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = EventDetection.IsEventDetectionInUse,
                IsStepLimitInUse = EventDetection.IsStepLimitInUse,
                Gamma = EventDetection.Gamma,
                LowBorder = EventDetection.LowBorder
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = ResultSaving.SavingTarget
            }
        };
    }

    public void Commit(SimulationParameters parameters)
    {
        CauchyInitials = new CauchyInitialsViewModel
        {
            StartTime = parameters.CauchyInitials.StartTime,
            EndTime = parameters.CauchyInitials.EndTime,
            InitialStep = parameters.CauchyInitials.InitialStep
        };

        IntegrationMethod = new MethodSettingsViewModel
        {
            SelectedMethod = parameters.IntegrationMethod.SelectedMethod,
            Accuracy = parameters.IntegrationMethod.Accuracy,
            IsAccuracyInUse = parameters.IntegrationMethod.IsAccuracyInUse,
            IsStableAllowedInUse = parameters.IntegrationMethod.IsStableAllowedInUse,
            IsStableInUse = parameters.IntegrationMethod.IsStableInUse,
            IsParallelInUse = parameters.IntegrationMethod.IsParallelInUse,
            Server = parameters.IntegrationMethod.Server,
            Port = parameters.IntegrationMethod.Port
        };

        EventDetection = new EventDetectionViewModel
        {
            IsEventDetectionInUse = parameters.EventDetection.IsEventDetectionInUse,
            IsStepLimitInUse = parameters.EventDetection.IsStepLimitInUse,
            Gamma = parameters.EventDetection.Gamma,
            LowBorder = parameters.EventDetection.LowBorder
        };

        ResultSaving = new ResultSavingViewModel
        {
            SavingTarget = parameters.ResultSaving.SavingTarget
        };
}
}
