using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using attainment.Infrastructure;
using attainment.Models;
using Microsoft.Extensions.Logging;

namespace attainment.ViewModels;

public sealed class ExamCreationViewModel : INotifyPropertyChanged
{
    private readonly IAi _ai;
    private readonly IPdf _pdf;
    private readonly ExamRepository _examRepository;
    private readonly ILogger<ExamCreationViewModel> _logger;
    private CancellationTokenSource? _generationCancellation;

    private Resource? _resource;
    private string _promptText = string.Empty;
    private string _resultText = string.Empty;
    private string _parseError = string.Empty;
    private string _operationError = string.Empty;
    private string _statusMessage = string.Empty;
    private bool _hasParseError;
    private bool _hasParsedExam;
    private bool _isLoading;
    private bool _useBase64;
    private int _numberOfQuestions = 5;
    private Exam? _parsedExam;
    private ObservableCollection<QuestionVM> _previewQuestions = [];
    private bool _isCorrectionMode;

    public ExamCreationViewModel(
        IAi ai,
        IPdf pdf,
        ExamRepository examRepository,
        ILogger<ExamCreationViewModel> logger)
    {
        _ai = ai;
        _pdf = pdf;
        _examRepository = examRepository;
        _logger = logger;
        ToggleCorrectionCommand = new RelayCommand(_ => ToggleCorrection(), _ => ParsedExam is not null && !IsLoading);
        GenerateExamCommand = new AsyncRelayCommand(GenerateExamAsync, CanGenerateExam);
        CancelGenerationCommand = new RelayCommand(_ => _generationCancellation?.Cancel(), _ => IsLoading);
        ExportExamCommand = new AsyncRelayCommand(ExportExamAsync, () => ParsedExam is not null && !IsLoading);
        RebuildPromptText();
    }

    public Resource? Resource
    {
        get => _resource;
        set
        {
            if (Equals(_resource, value)) return;
            _resource = value;
            OnPropertyChanged();
            RebuildPromptText();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string PromptText
    {
        get => _promptText;
        set
        {
            if (_promptText == value) return;
            _promptText = value;
            OnPropertyChanged();
        }
    }

    public bool UseBase64
    {
        get => _useBase64;
        set
        {
            if (_useBase64 == value) return;
            _useBase64 = value;
            OnPropertyChanged();
        }
    }

    public int NumberOfQuestions
    {
        get => _numberOfQuestions;
        set
        {
            var validValue = Math.Clamp(value, 1, 100);
            if (_numberOfQuestions == validValue) return;
            _numberOfQuestions = validValue;
            OnPropertyChanged();
            RebuildPromptText();
        }
    }

    public string ResultText
    {
        get => _resultText;
        private set
        {
            if (_resultText == value) return;
            _resultText = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasResultText));
            ParseExamFromResult();
        }
    }

    public bool HasResultText => !string.IsNullOrWhiteSpace(ResultText);

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value) return;
            _isLoading = value;
            OnPropertyChanged();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public Exam? ParsedExam
    {
        get => _parsedExam;
        private set
        {
            if (Equals(_parsedExam, value)) return;
            _parsedExam = value;
            OnPropertyChanged();
            HasParsedExam = value is not null;
            BuildPreviewQuestions();
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public string ParseError
    {
        get => _parseError;
        private set
        {
            if (_parseError == value) return;
            _parseError = value;
            OnPropertyChanged();
            HasParseError = !string.IsNullOrWhiteSpace(value);
        }
    }

    public bool HasParseError
    {
        get => _hasParseError;
        private set
        {
            if (_hasParseError == value) return;
            _hasParseError = value;
            OnPropertyChanged();
        }
    }

    public bool HasParsedExam
    {
        get => _hasParsedExam;
        private set
        {
            if (_hasParsedExam == value) return;
            _hasParsedExam = value;
            OnPropertyChanged();
        }
    }

    public string OperationError
    {
        get => _operationError;
        private set
        {
            if (_operationError == value) return;
            _operationError = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOperationError));
        }
    }

    public bool HasOperationError => !string.IsNullOrWhiteSpace(OperationError);

    public string StatusMessage
    {
        get => _statusMessage;
        private set
        {
            if (_statusMessage == value) return;
            _statusMessage = value;
            OnPropertyChanged();
        }
    }

    public ObservableCollection<QuestionVM> PreviewQuestions
    {
        get => _previewQuestions;
        private set
        {
            if (ReferenceEquals(_previewQuestions, value)) return;
            _previewQuestions = value;
            OnPropertyChanged();
        }
    }

    public bool IsCorrectionMode
    {
        get => _isCorrectionMode;
        private set
        {
            if (_isCorrectionMode == value) return;
            _isCorrectionMode = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CorrectionButtonLabel));
        }
    }

    public string CorrectionButtonLabel => IsCorrectionMode ? "Reset" : "Correction";

    public ICommand ToggleCorrectionCommand { get; }
    public ICommand GenerateExamCommand { get; }
    public ICommand CancelGenerationCommand { get; }
    public ICommand ExportExamCommand { get; }

    public event PropertyChangedEventHandler? PropertyChanged;

    private bool CanGenerateExam()
    {
        return !IsLoading && Resource?.FilePath is { Length: > 0 } path && File.Exists(path);
    }

    private async Task GenerateExamAsync()
    {
        if (!CanGenerateExam() || Resource?.FilePath is not { } path)
        {
            return;
        }

        _generationCancellation?.Dispose();
        _generationCancellation = new CancellationTokenSource();
        var cancellationToken = _generationCancellation.Token;

        try
        {
            IsLoading = true;
            OperationError = string.Empty;
            StatusMessage = "Preparing source material...";
            ResultText = string.Empty;
            var file = new FileInfo(path);
            var prompt = PromptText;

            if (!UseBase64)
            {
                var resourceText = await _pdf.ConvertAsync(file, cancellationToken);
                prompt += Environment.NewLine + resourceText;
            }

            StatusMessage = "Generating exam...";
            ResultText = await _ai.PromptAsync(prompt, UseBase64 ? file : null, cancellationToken);
            StatusMessage = ParsedExam is null ? "Generation completed with validation errors." : "Exam generated.";
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Generation cancelled.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exam generation failed for resource {ResourceId}", Resource?.Id);
            OperationError = "The exam could not be generated. Check the file and OpenAI settings, then try again.";
            StatusMessage = "Generation failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task ExportExamAsync()
    {
        if (ParsedExam is null) return;

        try
        {
            IsLoading = true;
            OperationError = string.Empty;
            StatusMessage = "Exporting exam...";
            var path = await _pdf.ExportExamAsync(ParsedExam);
            StatusMessage = $"Exam exported to {path}";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exam export failed for resource {ResourceId}", Resource?.Id);
            OperationError = "The exam could not be exported. Check the destination and try again.";
            StatusMessage = "Export failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void ToggleCorrection()
    {
        if (IsCorrectionMode)
        {
            foreach (var question in PreviewQuestions)
            {
                question.Reset();
            }
        }

        IsCorrectionMode = !IsCorrectionMode;
    }

    private void BuildPreviewQuestions()
    {
        IsCorrectionMode = false;
        PreviewQuestions = ParsedExam?.Questions is { Length: > 0 } questions
            ? new ObservableCollection<QuestionVM>(questions.Select(question => new QuestionVM(question)))
            : [];
    }

    private void RebuildPromptText()
    {
        PromptText = Prompts.Prompt(NumberOfQuestions);
    }

    private void ParseExamFromResult()
    {
        ParsedExam = null;
        ParseError = string.Empty;
        if (string.IsNullOrWhiteSpace(ResultText)) return;

        try
        {
            ParsedExam = _examRepository.Parse(ResultText.Trim());
        }
        catch (ExamValidationException ex)
        {
            ParseError = string.Join(Environment.NewLine, ex.Errors);
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class QuestionVM : INotifyPropertyChanged
{
    private int? _selectedOptionNumber;

    public QuestionVM(Question question)
    {
        Content = question.Content;
        CorrectOption = question.CorrectOption;
        Explanation = question.Explanation;
        Options = new ObservableCollection<OptionVM>(
            question.Options.Select(option => new OptionVM(option.Number, option.Content, this)));
    }

    public string Content { get; }
    public int CorrectOption { get; }
    public string Explanation { get; }
    public ObservableCollection<OptionVM> Options { get; }

    public int? SelectedOptionNumber
    {
        get => _selectedOptionNumber;
        private set
        {
            if (_selectedOptionNumber == value) return;
            _selectedOptionNumber = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsAnswered));
            OnPropertyChanged(nameof(IsCorrect));
        }
    }

    public bool IsAnswered => SelectedOptionNumber.HasValue;
    public bool IsCorrect => SelectedOptionNumber == CorrectOption;

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void OnOptionToggled(OptionVM option, bool isSelected)
    {
        if (isSelected)
        {
            foreach (var otherOption in Options.Where(otherOption => !ReferenceEquals(otherOption, option)))
            {
                otherOption.SetSelectedSilently(false);
            }

            SelectedOptionNumber = option.Number;
        }
        else if (SelectedOptionNumber == option.Number)
        {
            SelectedOptionNumber = null;
        }
    }

    public void Reset()
    {
        foreach (var option in Options)
        {
            option.SetSelectedSilently(false);
        }

        SelectedOptionNumber = null;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public sealed class OptionVM : INotifyPropertyChanged
{
    private readonly QuestionVM _parent;
    private bool _isSelected;

    public OptionVM(int number, string content, QuestionVM parent)
    {
        Number = number;
        Content = content;
        _parent = parent;
    }

    public int Number { get; }
    public string Content { get; }

    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            _parent.OnOptionToggled(this, value);
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    internal void SetSelectedSilently(bool value)
    {
        if (_isSelected == value) return;
        _isSelected = value;
        OnPropertyChanged(nameof(IsSelected));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public class RelayCommand(
    Action<object?> execute,
    Func<object?, bool>? canExecute = null) : ICommand
{
    public bool CanExecute(object? parameter) => canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}

public sealed class AsyncRelayCommand(
    Func<Task> execute,
    Func<bool>? canExecute = null) : ICommand
{
    private bool _isExecuting;

    public bool CanExecute(object? parameter) => !_isExecuting && (canExecute?.Invoke() ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter)) return;

        try
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            await execute();
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public event EventHandler? CanExecuteChanged
    {
        add => CommandManager.RequerySuggested += value;
        remove => CommandManager.RequerySuggested -= value;
    }
}
