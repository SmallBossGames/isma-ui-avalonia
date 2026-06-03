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

    [ObservableProperty]
    private ResultProcessingViewModel _resultProcessing = new();

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
            IsStableInUse = parameters.IntegrationMethod.IsStableInUse
        };

        EventDetection = new EventDetectionViewModel
        {
            IsStepLimitInUse = parameters.EventDetection.IsEventDetectionInUse,
            Gamma = parameters.EventDetection.Gamma,
            LowBorder = parameters.EventDetection.LowBorder
        };

        ResultSaving = new ResultSavingViewModel
        {
            SavingTarget = parameters.ResultSaving.SavingTarget
        };

        ResultProcessing = new ResultProcessingViewModel
        {
            IsSimplifyInUse = parameters.ResultProcessing.IsSimplifyInUse,
            SelectedSimplifyMethod = parameters.ResultProcessing.SelectedSimplifyMethod,
            Tolerance = parameters.ResultProcessing.Tolerance
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
                IsStableInUse = IntegrationMethod.IsStableInUse
            },
            EventDetection = new EventDetectionParameters
            {
                IsEventDetectionInUse = EventDetection.IsStepLimitInUse,
                Gamma = EventDetection.Gamma,
                LowBorder = EventDetection.LowBorder
            },
            ResultSaving = new ResultSavingParameters
            {
                SavingTarget = ResultSaving.SavingTarget
            },
            ResultProcessing = new ResultProcessingParameters
            {
                IsSimplifyInUse = ResultProcessing.IsSimplifyInUse,
                SelectedSimplifyMethod = ResultProcessing.SelectedSimplifyMethod,
                Tolerance = ResultProcessing.Tolerance
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
            IsStableInUse = parameters.IntegrationMethod.IsStableInUse
        };

        EventDetection = new EventDetectionViewModel
        {
            IsStepLimitInUse = parameters.EventDetection.IsEventDetectionInUse,
            Gamma = parameters.EventDetection.Gamma,
            LowBorder = parameters.EventDetection.LowBorder
        };

        ResultSaving = new ResultSavingViewModel
        {
            SavingTarget = parameters.ResultSaving.SavingTarget
        };

        ResultProcessing = new ResultProcessingViewModel
        {
            IsSimplifyInUse = parameters.ResultProcessing.IsSimplifyInUse,
            SelectedSimplifyMethod = parameters.ResultProcessing.SelectedSimplifyMethod,
            Tolerance = parameters.ResultProcessing.Tolerance
        };
    }
}
