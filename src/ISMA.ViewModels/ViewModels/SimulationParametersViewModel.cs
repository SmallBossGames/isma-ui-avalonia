using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using ISMA.Domain.Models;

namespace ISMA.ViewModels.ViewModels;

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

    private ObservableCollection<string> _integrationMethods = new();

    public ObservableCollection<string> IntegrationMethods
    {
        get => _integrationMethods;
        set => SetProperty(ref _integrationMethods, value);
    }

    public SimulationParametersViewModel()
    {
    }

    public SimulationParametersViewModel(SimulationParameters parameters)
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

        ResultProcessing = new ResultProcessingViewModel
        {
            IsSimplifyInUse = parameters.ResultProcessing.IsSimplifyInUse,
            SelectedSimplifyMethod = parameters.ResultProcessing.SelectedSimplifyMethod,
            Tolerance = parameters.ResultProcessing.Tolerance
        };
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

        ResultProcessing = new ResultProcessingViewModel
        {
            IsSimplifyInUse = parameters.ResultProcessing.IsSimplifyInUse,
            SelectedSimplifyMethod = parameters.ResultProcessing.SelectedSimplifyMethod,
            Tolerance = parameters.ResultProcessing.Tolerance
        };
    }
}
