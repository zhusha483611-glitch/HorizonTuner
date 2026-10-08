using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HorizonTuner.Models;
using HorizonTuner.Resources.Config;
using HorizonTuner.Services.Dialogs;

namespace HorizonTuner.ViewModels.Windows;

public enum PresetSortMode
{
    Hot,
    MostUsed,
    RecentlyUsed,
    Name,
    CreatedTime
}

public enum PresetViewMode
{
    List,
    Card
}

public sealed record PresetSortOption(PresetSortMode Mode, string Label);

public partial class PresetListItemViewModel : ObservableObject
{
    public VelocityPreset Preset { get; }

    [ObservableProperty]
    private double _hotScore;

    public PresetListItemViewModel(VelocityPreset preset)
    {
        Preset = preset;
    }

    public string Id => Preset.Id;
    public string Name => Preset.Name;
    public int UseCount => Preset.UseCount;
    public DateTime CreatedTime => Preset.CreatedTime;
    public DateTime? LastUsedTimeUtc => Preset.LastUsedTimeUtc;

    public string CreatedTimeText => CreatedTime.ToString("yyyy-MM-dd HH:mm");

    public string LastUsedTimeText =>
        LastUsedTimeUtc is { } utc
            ? utc.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "从未";

    public void RefreshComputed(DateTime nowUtc)
    {
        HotScore = PresetHotness.CalculateHotScore(Preset, nowUtc);
    }

    public void NotifyPresetChanged()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(UseCount));
        OnPropertyChanged(nameof(CreatedTime));
        OnPropertyChanged(nameof(LastUsedTimeUtc));
        OnPropertyChanged(nameof(CreatedTimeText));
        OnPropertyChanged(nameof(LastUsedTimeText));
    }
}

public static class PresetHotness
{
    public static double CalculateHotScore(VelocityPreset preset, DateTime nowUtc)
    {
        if (preset.UseCount <= 0)
        {
            return 0;
        }

        var lastUtc = preset.LastUsedTimeUtc ?? preset.CreatedTime.ToUniversalTime();
        var days = Math.Max(0, (nowUtc - lastUtc).TotalDays);

        var decay = 1d / (1d + days / 3d);
        var baseScore = 100d * (1d - Math.Exp(-preset.UseCount / 10d));
        var score = baseScore * decay;
        return Math.Clamp(score, 0d, 100d);
    }
}

public partial class PresetManagerViewModel : ObservableObject
{
    private readonly HandlingAutoConfig _config;
    private readonly Action<VelocityPreset> _applyPresetCallback;
    private readonly Action? _requestClose;
    private readonly IPresetManagerDialogService _dialogs;
    private bool _suppressEditChangeTracking;
    private DispatcherTimer? _saveFeedbackTimer;

    public ObservableCollection<PresetListItemViewModel> Presets { get; }
    public ICollectionView PresetsView { get; }

    public IReadOnlyList<PresetSortOption> SortOptions { get; } =
    [
        new(PresetSortMode.Hot, "热门"),
        new(PresetSortMode.MostUsed, "最常用"),
        new(PresetSortMode.RecentlyUsed, "最近使用"),
        new(PresetSortMode.Name, "名称"),
        new(PresetSortMode.CreatedTime, "创建时间")
    ];

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private PresetSortMode _selectedSortMode = PresetSortMode.Hot;

    [ObservableProperty]
    private PresetViewMode _viewMode = PresetViewMode.List;

    [ObservableProperty]
    private PresetListItemViewModel? _selectedPreset;

    [ObservableProperty]
    private double _editStage1Gamma;

    [ObservableProperty]
    private double _editStage2Gamma;

    [ObservableProperty]
    private double _editStage3Gamma;

    [ObservableProperty]
    private double _editStage1ScalePercent;

    [ObservableProperty]
    private double _editStage2ScalePercent;

    [ObservableProperty]
    private double _editStage3ScalePercent;

    [ObservableProperty]
    private bool _hasUnsavedChanges;

    [ObservableProperty]
    private bool _isStage1GammaDirty;

    [ObservableProperty]
    private bool _isStage2GammaDirty;

    [ObservableProperty]
    private bool _isStage3GammaDirty;

    [ObservableProperty]
    private bool _isStage1ScaleDirty;

    [ObservableProperty]
    private bool _isStage2ScaleDirty;

    [ObservableProperty]
    private bool _isStage3ScaleDirty;

    [ObservableProperty]
    private string _saveFeedbackText = string.Empty;

    public bool HasSelectedPreset => SelectedPreset != null;

    public string SummaryText
    {
        get
        {
            var totalUses = Presets.Sum(p => p.UseCount);
            var lastUsed = Presets
                .Where(p => p.LastUsedTimeUtc.HasValue)
                .OrderByDescending(p => p.LastUsedTimeUtc)
                .FirstOrDefault();

            var lastUsedText = lastUsed?.Name ?? "无";
            return $"预设 {Presets.Count} / 20   总使用 {totalUses}   最近使用 {lastUsedText}";
        }
    }

    public PresetManagerViewModel(
        Action<VelocityPreset> applyPresetCallback,
        IPresetManagerDialogService dialogs,
        Action? requestClose = null)
    {
        _config = HandlingAutoConfigManager.Get();
        _applyPresetCallback = applyPresetCallback;
        _dialogs = dialogs;
        _requestClose = requestClose;

        Presets = new ObservableCollection<PresetListItemViewModel>(
            _config.CustomVelocityPresets.Select(p => new PresetListItemViewModel(p)));

        Presets.CollectionChanged += Presets_OnCollectionChanged;

        PresetsView = CollectionViewSource.GetDefaultView(Presets);
        PresetsView.Filter = PresetFilter;

        RefreshView();
    }

    private void Presets_OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        OnPropertyChanged(nameof(SummaryText));
    }

    partial void OnSearchTextChanged(string value)
    {
        RefreshView();
    }

    partial void OnSelectedSortModeChanged(PresetSortMode value)
    {
        RefreshView();
    }

    partial void OnSelectedPresetChanged(PresetListItemViewModel? value)
    {
        OnPropertyChanged(nameof(HasSelectedPreset));
        ApplyCommand.NotifyCanExecuteChanged();
        ApplyAndCloseCommand.NotifyCanExecuteChanged();
        RenameCommand.NotifyCanExecuteChanged();
        DeleteCommand.NotifyCanExecuteChanged();
        SaveEditsCommand.NotifyCanExecuteChanged();
        RevertEditsCommand.NotifyCanExecuteChanged();
        SaveAsNewCommand.NotifyCanExecuteChanged();

        if (value == null)
        {
            ClearEdits();
            return;
        }

        LoadEditsFromPreset(value.Preset);
    }

    partial void OnEditStage1GammaChanged(double value) => OnEditValuesChanged();
    partial void OnEditStage2GammaChanged(double value) => OnEditValuesChanged();
    partial void OnEditStage3GammaChanged(double value) => OnEditValuesChanged();
    partial void OnEditStage1ScalePercentChanged(double value) => OnEditValuesChanged();
    partial void OnEditStage2ScalePercentChanged(double value) => OnEditValuesChanged();
    partial void OnEditStage3ScalePercentChanged(double value) => OnEditValuesChanged();

    private void OnEditValuesChanged()
    {
        if (_suppressEditChangeTracking)
        {
            return;
        }

        UpdateHasUnsavedChanges();
    }

    private void UpdateHasUnsavedChanges()
    {
        if (SelectedPreset == null)
        {
            HasUnsavedChanges = false;
            IsStage1GammaDirty = false;
            IsStage2GammaDirty = false;
            IsStage3GammaDirty = false;
            IsStage1ScaleDirty = false;
            IsStage2ScaleDirty = false;
            IsStage3ScaleDirty = false;
            SaveEditsCommand.NotifyCanExecuteChanged();
            RevertEditsCommand.NotifyCanExecuteChanged();
            return;
        }

        var p = SelectedPreset.Preset;
        IsStage1GammaDirty = !NearlyEqual(EditStage1Gamma, p.Stage1Gamma);
        IsStage2GammaDirty = !NearlyEqual(EditStage2Gamma, p.Stage2Gamma);
        IsStage3GammaDirty = !NearlyEqual(EditStage3Gamma, p.Stage3Gamma);
        IsStage1ScaleDirty = !NearlyEqual(EditStage1ScalePercent, p.Stage1Scale * 100d);
        IsStage2ScaleDirty = !NearlyEqual(EditStage2ScalePercent, p.Stage2Scale * 100d);
        IsStage3ScaleDirty = !NearlyEqual(EditStage3ScalePercent, p.Stage3Scale * 100d);

        var changed = IsStage1GammaDirty || IsStage2GammaDirty || IsStage3GammaDirty ||
                      IsStage1ScaleDirty || IsStage2ScaleDirty || IsStage3ScaleDirty;

        HasUnsavedChanges = changed;
        if (changed)
        {
            SaveFeedbackText = string.Empty;
            StopSaveFeedbackTimer();
        }
        SaveEditsCommand.NotifyCanExecuteChanged();
        RevertEditsCommand.NotifyCanExecuteChanged();
    }

    private static bool NearlyEqual(double a, double b)
    {
        return Math.Abs(a - b) < 0.0001;
    }

    private void LoadEditsFromPreset(VelocityPreset preset)
    {
        _suppressEditChangeTracking = true;
        try
        {
            EditStage1Gamma = preset.Stage1Gamma;
            EditStage2Gamma = preset.Stage2Gamma;
            EditStage3Gamma = preset.Stage3Gamma;
            EditStage1ScalePercent = preset.Stage1Scale * 100d;
            EditStage2ScalePercent = preset.Stage2Scale * 100d;
            EditStage3ScalePercent = preset.Stage3Scale * 100d;
        }
        finally
        {
            _suppressEditChangeTracking = false;
        }

        HasUnsavedChanges = false;
        IsStage1GammaDirty = false;
        IsStage2GammaDirty = false;
        IsStage3GammaDirty = false;
        IsStage1ScaleDirty = false;
        IsStage2ScaleDirty = false;
        IsStage3ScaleDirty = false;
        SaveFeedbackText = string.Empty;
        StopSaveFeedbackTimer();
        SaveEditsCommand.NotifyCanExecuteChanged();
        RevertEditsCommand.NotifyCanExecuteChanged();
    }

    private void ClearEdits()
    {
        _suppressEditChangeTracking = true;
        try
        {
            EditStage1Gamma = 0;
            EditStage2Gamma = 0;
            EditStage3Gamma = 0;
            EditStage1ScalePercent = 0;
            EditStage2ScalePercent = 0;
            EditStage3ScalePercent = 0;
        }
        finally
        {
            _suppressEditChangeTracking = false;
        }

        HasUnsavedChanges = false;
        IsStage1GammaDirty = false;
        IsStage2GammaDirty = false;
        IsStage3GammaDirty = false;
        IsStage1ScaleDirty = false;
        IsStage2ScaleDirty = false;
        IsStage3ScaleDirty = false;
        SaveFeedbackText = string.Empty;
        StopSaveFeedbackTimer();
        SaveEditsCommand.NotifyCanExecuteChanged();
        RevertEditsCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private void SetViewMode(PresetViewMode mode)
    {
        ViewMode = mode;
    }

    private bool CanOperateSelected()
    {
        return SelectedPreset != null;
    }

    [RelayCommand(CanExecute = nameof(CanOperateSelected))]
    private void Apply()
    {
        ApplyInternal();
    }

    [RelayCommand(CanExecute = nameof(CanOperateSelected))]
    private void ApplyAndClose()
    {
        if (ApplyInternal())
        {
            _requestClose?.Invoke();
        }
    }

    private bool ApplyInternal()
    {
        if (SelectedPreset == null)
        {
            _dialogs.ShowInfo("请先选择一个预设", "提示");
            return false;
        }

        if (!TrySaveEditsIfNeeded())
        {
            return false;
        }

        _applyPresetCallback(SelectedPreset.Preset);
        SelectedPreset.RefreshComputed(DateTime.UtcNow);
        SelectedPreset.NotifyPresetChanged();
        RefreshView();
        OnPropertyChanged(nameof(SummaryText));
        return true;
    }

    [RelayCommand(CanExecute = nameof(CanOperateSelected))]
    private void Rename()
    {
        if (SelectedPreset == null)
        {
            _dialogs.ShowInfo("请先选择一个预设", "提示");
            return;
        }

        if (!_dialogs.TryGetText("重命名预设", "请输入新的预设名称：", SelectedPreset.Name, out var newNameRaw))
        {
            return;
        }

        var newName = newNameRaw.Trim();
        if (string.IsNullOrWhiteSpace(newName))
        {
            _dialogs.ShowInfo("预设名称不能为空", "提示");
            return;
        }

        var uniqueName = VelocityPresetNaming.MakeUniqueName(
            _config.CustomVelocityPresets.Where(p => !ReferenceEquals(p, SelectedPreset.Preset)),
            newName);

        SelectedPreset.Preset.Name = uniqueName;
        HandlingAutoConfigManager.Save(_config);
        SelectedPreset.NotifyPresetChanged();
        RefreshView();
        OnPropertyChanged(nameof(SummaryText));
    }

    [RelayCommand(CanExecute = nameof(CanOperateSelected))]
    private void Delete()
    {
        if (SelectedPreset == null)
        {
            _dialogs.ShowInfo("请先选择一个预设", "提示");
            return;
        }

        if (!_dialogs.ConfirmDelete(SelectedPreset.Name))
        {
            return;
        }

        _config.CustomVelocityPresets.Remove(SelectedPreset.Preset);
        Presets.Remove(SelectedPreset);
        HandlingAutoConfigManager.Save(_config);
        SelectedPreset = null;
        RefreshView();
        OnPropertyChanged(nameof(SummaryText));
    }

    private bool CanSaveEdits()
    {
        return SelectedPreset != null && HasUnsavedChanges;
    }

    [RelayCommand(CanExecute = nameof(CanSaveEdits))]
    private void SaveEdits()
    {
        SaveEditsInternal(showErrorDialogs: true);
    }

    [RelayCommand(CanExecute = nameof(CanSaveEdits))]
    private void RevertEdits()
    {
        if (SelectedPreset == null)
        {
            return;
        }

        LoadEditsFromPreset(SelectedPreset.Preset);
    }

    [RelayCommand(CanExecute = nameof(CanOperateSelected))]
    private void SaveAsNew()
    {
        if (SelectedPreset == null)
        {
            _dialogs.ShowInfo("请先选择一个预设", "提示");
            return;
        }

        var edit = GetCurrentEdit();
        if (!VelocityPresetEditing.TryValidate(edit, out var error))
        {
            _dialogs.ShowInfo(error, "提示");
            return;
        }

        var nameBase = $"{SelectedPreset.Name} 副本";
        var uniqueName = VelocityPresetNaming.MakeUniqueName(_config.CustomVelocityPresets, nameBase);

        var profile = VelocityPresetEditing.MergeCurveProfile(SelectedPreset.Preset, edit);
        var preset = VelocityPresetEditing.CreateNew(uniqueName, profile, DateTime.Now);

        _config.CustomVelocityPresets.Add(preset);
        var item = new PresetListItemViewModel(preset);
        Presets.Add(item);
        HandlingAutoConfigManager.Save(_config);
        SelectedPreset = item;
        RefreshView();
        OnPropertyChanged(nameof(SummaryText));
    }

    private bool TrySaveEditsIfNeeded()
    {
        if (!HasUnsavedChanges)
        {
            return true;
        }

        return SaveEditsInternal(showErrorDialogs: true);
    }

    private bool SaveEditsInternal(bool showErrorDialogs)
    {
        if (SelectedPreset == null)
        {
            return false;
        }

        var edit = GetCurrentEdit();
        if (!VelocityPresetEditing.TryValidate(edit, out var error))
        {
            if (showErrorDialogs)
            {
                _dialogs.ShowInfo(error, "提示");
            }

            return false;
        }

        var p = SelectedPreset.Preset;
        VelocityPresetEditing.ApplyEdits(p, edit);

        HandlingAutoConfigManager.Save(_config);

        SelectedPreset.NotifyPresetChanged();
        LoadEditsFromPreset(p);
        ShowSaveFeedback("已保存");
        RefreshView();
        OnPropertyChanged(nameof(SummaryText));
        return true;
    }

    private VelocityPresetEdit GetCurrentEdit()
    {
        return new VelocityPresetEdit(
            EditStage1Gamma,
            EditStage2Gamma,
            EditStage3Gamma,
            EditStage1ScalePercent,
            EditStage2ScalePercent,
            EditStage3ScalePercent);
    }

    private void ShowSaveFeedback(string text)
    {
        SaveFeedbackText = text;
        StopSaveFeedbackTimer();

        _saveFeedbackTimer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(1500)
        };
        _saveFeedbackTimer.Tick += (_, _) =>
        {
            SaveFeedbackText = string.Empty;
            StopSaveFeedbackTimer();
        };
        _saveFeedbackTimer.Start();
    }

    private void StopSaveFeedbackTimer()
    {
        if (_saveFeedbackTimer == null)
        {
            return;
        }

        _saveFeedbackTimer.Stop();
        _saveFeedbackTimer = null;
    }

    private bool PresetFilter(object obj)
    {
        if (obj is not PresetListItemViewModel item)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(SearchText))
        {
            return true;
        }

        return item.Name.Contains(SearchText.Trim(), StringComparison.OrdinalIgnoreCase);
    }

    public void RefreshView()
    {
        var nowUtc = DateTime.UtcNow;
        foreach (var item in Presets)
        {
            item.RefreshComputed(nowUtc);
        }

        ApplySort();
        PresetsView.Refresh();
    }

    private void ApplySort()
    {
        PresetsView.SortDescriptions.Clear();

        switch (SelectedSortMode)
        {
            case PresetSortMode.Hot:
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.HotScore), ListSortDirection.Descending));
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.UseCount), ListSortDirection.Descending));
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.LastUsedTimeUtc), ListSortDirection.Descending));
                break;
            case PresetSortMode.MostUsed:
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.UseCount), ListSortDirection.Descending));
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.LastUsedTimeUtc), ListSortDirection.Descending));
                break;
            case PresetSortMode.RecentlyUsed:
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.LastUsedTimeUtc), ListSortDirection.Descending));
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.UseCount), ListSortDirection.Descending));
                break;
            case PresetSortMode.Name:
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.Name), ListSortDirection.Ascending));
                break;
            case PresetSortMode.CreatedTime:
                PresetsView.SortDescriptions.Add(new SortDescription(nameof(PresetListItemViewModel.CreatedTime), ListSortDirection.Descending));
                break;
        }
    }
}
