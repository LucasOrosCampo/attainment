using System.ComponentModel;
using System.Runtime.CompilerServices;
using attainment.Infrastructure;
using attainment.Models;

namespace attainment.ViewModels;

public class ExamCreationViewModel(IAi ai) : INotifyPropertyChanged
{
    public IAi Ai => ai;

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
            }
        }
    }

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
}
