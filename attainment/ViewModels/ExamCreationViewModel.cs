using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using attainment.Infrastructure;
using attainment.Models;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace attainment.ViewModels;

public class ExamCreationViewModel: INotifyPropertyChanged
{
    public IAi Ai { get; }
    public IPdf Pdf { get; }
    private string _resourceText = string.Empty;
    
    private Exam? _parsedExam;
    public Exam? ParsedExam
    {
        get => _parsedExam;
        private set
        {
            if (!Equals(_parsedExam, value))
            {
                _parsedExam = value;
                OnPropertyChanged();
                HasParsedExam = _parsedExam != null;
                // Build preview question VMs when a new parsed exam arrives
                BuildPreviewQuestions();
            }
        }
    }

    private string _parseError = string.Empty;
    public string ParseError
    {
        get => _parseError;
        private set
        {
            if (_parseError != value)
            {
                _parseError = value;
                OnPropertyChanged();
                HasParseError = !string.IsNullOrEmpty(_parseError);
            }
        }
    }

    private bool _hasParseError;
    public bool HasParseError
    {
        get => _hasParseError;
        private set
        {
            if (_hasParseError != value)
            {
                _hasParseError = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _hasParsedExam;
    public bool HasParsedExam
    {
        get => _hasParsedExam;
        private set
        {
            if (_hasParsedExam != value)
            {
                _hasParsedExam = value;
                OnPropertyChanged();
            }
        }
    }

    public ExamCreationViewModel(IAi ai, IPdf pdf)
    {
        Ai = ai;
        Pdf = pdf;
        PropertyChanged += OnResourcePropertyChanged;
        ToggleCorrectionCommand = new RelayCommand(_ => ToggleCorrection());
        ExportExamCommand = new RelayCommand(async _ => await ExportExamAsync(), _ => ParsedExam != null && !IsLoading);
    }

    private void OnResourcePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(Resource))
        {
            if (Resource?.FilePath is { } path)
            {
                var pdfFile = new FileInfo(path);
                _resourceText = Pdf.Convert(pdfFile) ?? string.Empty;
            }
            else
            {
                _resourceText = string.Empty;
            }

            RebuildPromptText();
        }
    }
    
    private Resource? _resource;
    public Resource? Resource
    {
        get => _resource;
        set
        {
            if (!Equals(_resource, value))
            {
                _resource = value;
                OnPropertyChanged();
            }
        }
    }

    private string _promptText = string.Empty;
    public string PromptText
    {
        get => _promptText;
        set
        {
            if (_promptText != value)
            {
                _promptText = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _useBase64;
    public bool UseBase64
    {
        get => _useBase64;
        set
        {
            if (_useBase64 != value)
            {
                _useBase64 = value;
                OnPropertyChanged();
                // When toggled, rebuild prompt to include/exclude raw file text
                RebuildPromptText();
            }
        }
    }

    private int _numberOfQuestions = 5;
    public int NumberOfQuestions
    {
        get => _numberOfQuestions;
        set
        {
            if (_numberOfQuestions != value)
            {
                _numberOfQuestions = value;
                OnPropertyChanged();
                // Rebuild prompt whenever the number changes
                RebuildPromptText();
            }
        }
    }

    private string _resultText = string.Empty;
    public string ResultText
    {
        get => _resultText;
        set
        {
            if (_resultText != value)
            {
                _resultText = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasResultText));
                ParseExamFromResult();
            }
        }
    }

    public bool HasResultText => !string.IsNullOrEmpty(_resultText);

    private bool _isLoading;
    public bool IsLoading
    {
        get => _isLoading;
        set
        {
            if (_isLoading != value)
            {
                _isLoading = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

    // Preview selection/correction state
    private ObservableCollection<QuestionVM> _previewQuestions = new();
    public ObservableCollection<QuestionVM> PreviewQuestions
    {
        get => _previewQuestions;
        private set
        {
            if (!ReferenceEquals(_previewQuestions, value))
            {
                _previewQuestions = value;
                OnPropertyChanged();
            }
        }
    }

    private bool _isCorrectionMode;
    public bool IsCorrectionMode
    {
        get => _isCorrectionMode;
        set
        {
            if (_isCorrectionMode != value)
            {
                _isCorrectionMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CorrectionButtonLabel));
            }
        }
    }

    public string CorrectionButtonLabel => IsCorrectionMode ? "Reset" : "Correction";

    public ICommand ToggleCorrectionCommand { get; }
    public ICommand ExportExamCommand { get; }

    private void ToggleCorrection()
    {
        // Toggle mode; when leaving correction mode, hide explanations/colors (no need to change selections)
        IsCorrectionMode = !IsCorrectionMode;
    }

    private async Task ExportExamAsync()
    {
        if (ParsedExam == null) return;
        try
        {
            IsLoading = true;
            await Task.Run(() =>
            {
                Pdf.ExportExam(ParsedExam);
            });
        }
        finally
        {
            IsLoading = false;
        }
    }

    private void BuildPreviewQuestions()
    {
        // Reset mode when new exam arrives
        IsCorrectionMode = false;

        if (ParsedExam?.Questions == null || ParsedExam.Questions.Length == 0)
        {
            PreviewQuestions = new ObservableCollection<QuestionVM>();
            return;
        }

        var qvms = ParsedExam.Questions.Select(q => new QuestionVM(q)).ToList();
        PreviewQuestions = new ObservableCollection<QuestionVM>(qvms);
    }

    private void RebuildPromptText()
    {
        var header = Prompts.Prompt(NumberOfQuestions);
        // If UseBase64 is ON, do not append the resource text to the prompt.
        if (!UseBase64 && !string.IsNullOrEmpty(_resourceText))
        {
            PromptText = header + "\n" + _resourceText;
        }
        else
        {
            PromptText = header;
        }
    }

    private void ParseExamFromResult()
    {
        // Reset state first
        ParsedExam = null;
        ParseError = string.Empty;

        var text = _resultText?.Trim();
        if (string.IsNullOrEmpty(text))
        {
            return; // nothing to parse yet
        }

        try
        {
            var exam = ExamRepository.Parse(text);
            if (exam?.Questions == null || exam.Questions.Length == 0)
            {
                ParseError = "Failed to create exam: no questions found.";
                return;
            }
            ParsedExam = exam;
        }
        catch (Exception ex)
        {
            ParseError = $"Failed to create exam: {ex.Message}";
        }
    }
}

public class QuestionVM : INotifyPropertyChanged
{
    public string Content { get; }
    public int CorrectOption { get; }
    public string Explanation { get; }

    public ObservableCollection<OptionVM> Options { get; }

    private int? _selectedOptionNumber;
    public int? SelectedOptionNumber
    {
        get => _selectedOptionNumber;
        set
        {
            if (_selectedOptionNumber != value)
            {
                _selectedOptionNumber = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsAnswered));
                OnPropertyChanged(nameof(IsCorrect));
            }
        }
    }

    public bool IsAnswered => SelectedOptionNumber.HasValue;
    public bool IsCorrect => SelectedOptionNumber.HasValue && SelectedOptionNumber.Value == CorrectOption;

    public QuestionVM(Question q)
    {
        Content = q.Content;
        CorrectOption = q.CorrectOption;
        Explanation = q.Explanation;
        Options = new ObservableCollection<OptionVM>(q.Options.Select(o => new OptionVM(o.Number, o.Content, this)));
    }

    internal void OnOptionToggled(OptionVM option, bool isSelected)
    {
        if (isSelected)
        {
            // Deselect others, select this
            foreach (var opt in Options)
            {
                if (!ReferenceEquals(opt, option) && opt.IsSelected)
                {
                    opt.SetSelectedSilently(false);
                }
            }
            SelectedOptionNumber = option.Number;
        }
        else
        {
            // If the same option was unselected, clear selection
            if (SelectedOptionNumber == option.Number)
            {
                SelectedOptionNumber = null;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class OptionVM : INotifyPropertyChanged
{
    public int Number { get; }
    public string Content { get; }
    private readonly QuestionVM _parent;

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                OnPropertyChanged();
                _parent.OnOptionToggled(this, _isSelected);
            }
        }
    }

    internal void SetSelectedSilently(bool value)
    {
        if (_isSelected != value)
        {
            _isSelected = value;
            OnPropertyChanged(nameof(IsSelected));
        }
    }

    public OptionVM(int number, string content, QuestionVM parent)
    {
        Number = number;
        Content = content;
        _parent = parent;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Func<object?, bool>? _canExecute;

    public RelayCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
    {
        _execute = execute;
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;
    public void Execute(object? parameter) => _execute(parameter);

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }
}
